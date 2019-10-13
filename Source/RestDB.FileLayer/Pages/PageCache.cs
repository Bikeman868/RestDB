using RestDB.Interfaces;
using RestDB.Interfaces.DatabaseLayer;
using RestDB.Interfaces.FileLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RestDB.FileLayer.Pages
{
    /* Rules and assumptions
     * 
     * - The page cache can be out of sync with the underlying file set but
     *   nothing else will directly read or write to the file set.
     * - Keep any page in cache that has pending updates from an active transaction
     * - When transactions commit create a snapshot of all modified pages prior
     *   to appying the update and tag them with the commit version of the transaction.
     * - Only purge page versions when there are no more transactions active that were
     *   started prior to this version of the page.
     * - Pages can be purged from the cache only if they have no versions in memory.
     * - Never modify pages directly because higher up layers may have references to them,
     *   instead copy the page, apply changes, save the original page as a specific version
     *   and save the copy as the current page.
     * - The database calls the page cache for every transaction start and end so that it is
     *   aware of every executing transaction
     */

    internal partial class PageCache : IPageCache
    {
        private readonly IStartupLog _startupLog;
        private readonly IErrorLog _errorLog;

        private readonly IFileSet _fileSet;
        private readonly IPagePool _pagePool;
        private readonly IDatabase _database;

        private readonly VersionHeadCollection _versions;
        private readonly TransactionHeadCollection _transactions;
        private readonly PageHeadCollection _pages;

        uint IPageCache.PageSize => _fileSet.PageSize;

        public PageCache(
            IFileSet fileSet,
            IDatabase database,
            IPagePoolFactory pagePoolFactory, 
            IStartupLog startupLog,
            IErrorLog errorLog)
        {
            _startupLog = startupLog;
            _errorLog = errorLog;
            _fileSet = fileSet;
            _database = database;

            startupLog.WriteLine("Creating a new page cache for " + _fileSet);

            _pagePool = pagePoolFactory.Create(fileSet.PageSize);

            _pages = new PageHeadCollection(fileSet, startupLog, errorLog, _pagePool);
            _versions = new VersionHeadCollection(startupLog, errorLog, database);
            _transactions = new TransactionHeadCollection(startupLog, errorLog, _pagePool);
        }

        public void Dispose()
        {
            _startupLog.WriteLine("Closing page cache for " + _fileSet);

            _fileSet.Dispose();
            _transactions.Dispose();
            _versions.Dispose();
            _pages.Dispose();
        }

        public override string ToString()
        {
            return "page cache on " + _fileSet;
        }

        IPageCache IPageCache.BeginTransaction(ITransaction transaction)
        {
            var transactionHead = _transactions.BeginTransaction(transaction);

            if (!transaction.ParentTransactionId.HasValue)
            {
                var version = _versions.GetVersion(transaction.BeginVersionNumber);
                version.TransactionStarted(transactionHead);
            }

            return this;
        }

        private TransactionHead CleanupTransaction(ITransaction transaction)
        {
            if (transaction == null) return null;

            var transactionHead = _transactions.EndTransaction(transaction);

            if (!transaction.ParentTransactionId.HasValue)
            {
                var version = _versions.GetVersion(transaction.BeginVersionNumber);
                version.TransactionEnded(transactionHead);
            }

            return transactionHead;
        }

        void IPageCache.Lock(ITransaction transaction, ulong pageNumber)
        {
            var pageHead = _pages.GetPageHead(pageNumber, CacheHints.WithLock);
            _transactions.Lock(transaction, pageHead);
        }

        void IPageCache.Unlock(ITransaction transaction, ulong pageNumber)
        {
            var pageHead = _pages.GetPageHead(pageNumber, CacheHints.None);
            _transactions.Unlock(transaction, pageHead);
        }

        Task IPageCache.CommitTransaction(ITransaction transaction)
        {
            using (var transactionHead = CleanupTransaction(transaction))
            {
                if (transactionHead == null || transactionHead.Updates == null || transactionHead.Updates.Count == 0)
                    return null;

                var versionHead = _versions.Add(transaction.CommitVersionNumber);

                IPage newPage = null;
                foreach (var update in transactionHead.Updates.OrderBy(u => u.PageNumber).ThenBy(u => u.SequenceNumber))
                {
                    if (newPage == null || update.PageNumber != newPage.PageNumber)
                    {
                        var pageHead = _pages.GetPageHead(update.PageNumber, CacheHints.ForUpdate);
                        var newestVersion = pageHead.GetVersion(null);

                        newPage = _pagePool.Get(update.PageNumber);
                        newestVersion.Data.CopyTo(newPage.Data, 0);

                        versionHead.AddPage(new PageVersion(versionHead.VersionNumber, newPage));
                    }

                    update.Data.CopyTo(newPage.Data, update.Offset);
                }

                versionHead.AddToPages(_pages);

                return _fileSet.WriteAndCommit(transaction, transactionHead.Updates);
            }
        }

        void IPageCache.RollbackTransaction(ITransaction transaction)
        {
            CleanupTransaction(transaction);
        }

        Task IPageCache.FinalizeTransaction(ITransaction transaction)
        {
            if (transaction.ParentTransactionId.HasValue)
                return null;

            return _fileSet.FinalizeTransaction(transaction);
        }

        IPage IPageCache.Get(ITransaction transaction, ulong pageNumber, CacheHints hints)
        {
            return _transactions.GetPage(transaction, pageNumber, hints, _pages);
        }

        IPageCache IPageCache.Update(ITransaction transaction, IEnumerable<PageUpdate> updates)
        {
            if (transaction == null)
                _fileSet.Write(transaction, updates);
            else
                _transactions.Update(transaction, updates, this);

            return this;
        }

        IPage IPageCache.NewPage(ulong pageNumber)
        {
            return _pages.NewPage(pageNumber);
        }
    }
}