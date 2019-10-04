using RestDB.Interfaces;
using RestDB.Interfaces.DatabaseLayer;
using RestDB.Interfaces.FileLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OwinContainers = OwinFramework.Utility.Containers;

namespace RestDB.FileLayer.Pages
{
    /* RULES
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
     */

    internal class VersionedPageCache : IVersionedPageCache
    {
        readonly IStartUpLog _startUpLog;
        readonly IFileSet _fileSet;
        readonly IPagePool _pagePool;
        readonly IDictionary<ulong, VersionHead> _versions;
        readonly IDictionary<ulong, TransactionHead> _transactions;
        readonly IDictionary<ulong, PageHead> _pages;

        public VersionedPageCache(
            IFileSet fileSet, 
            IPagePoolFactory pagePoolFactory, 
            IStartUpLog startUpLog)
        {
            _startUpLog = startUpLog;
            _fileSet = fileSet;
            _versions = new Dictionary<ulong, VersionHead>();
            _transactions = new Dictionary<ulong, TransactionHead>();
            _pages = new Dictionary<ulong, PageHead>();

            startUpLog.Write("Creating a new page cache for " + _fileSet);

            _pagePool = pagePoolFactory.Create(fileSet.PageSize);
        }

        public void Dispose()
        {
            _startUpLog.Write("Closing page cache for " + _fileSet);
            _fileSet.Dispose();
        }

        public override string ToString()
        {
            return "page cache on " + _fileSet;
        }

        IVersionedPageCache IVersionedPageCache.BeginTransaction(ITransaction transaction)
        {
            var head = new TransactionHead
            {
                Transaction = transaction
            };

            lock (_transactions) _transactions.Add(transaction.TransactionId, head);

            lock (_versions)
            {
                VersionHead version;
                if (_versions.TryGetValue(transaction.BeginVersionNumber, out version))
                {
                    Interlocked.Increment(ref version.TransactionCount);
                }
                else
                {
                    version = new VersionHead
                    {
                        VersionNumber = transaction.BeginVersionNumber,
                        TransactionCount = 1,
                        Pages = new OwinContainers.LinkedList<PageVersion>()
                    };
                    _versions.Add(transaction.BeginVersionNumber, version);
                }
            }

            return this;
        }

        Task IVersionedPageCache.CommitTransaction(ITransaction transaction)
        {
            var updates = CleanupTransaction(transaction);

            if (updates == null || updates.Count == 0)
                return null;

            var versionHead = new VersionHead
            {
                VersionNumber = transaction.CommitVersionNumber,
                Pages = new OwinContainers.LinkedList<PageVersion>(),
            };

            lock (_versions)
                _versions.Add(transaction.CommitVersionNumber, versionHead);

            IPage page = null;
            foreach(var update in updates.OrderBy(u => u.PageNumber).ThenBy(u => u.SequenceNumber))
            {
                if (page == null || update.PageNumber != page.PageNumber)
                {
                    PageHead pageHead;
                    lock (_pages)
                    {
                        if (!_pages.TryGetValue(update.PageNumber, out pageHead))
                        {
                            var newPage = _pagePool.Get(update.PageNumber);
                            if (!_fileSet.Read(newPage))
                                newPage.Data.Initialize();

                            pageHead = new PageHead { PageNumber = update.PageNumber, Page = newPage };
                            _pages.Add(update.PageNumber, pageHead);
                        }
                    }

                    var oldPage = pageHead.Page;
                    page = _pagePool.Get(update.PageNumber);
                    pageHead.Page.Data.CopyTo(page.Data, 0);
                    pageHead.Page = page;

                    var pageVersion = new PageVersion
                    {
                        VersionNumber = versionHead.VersionNumber,
                        Page = oldPage,
                        PageHead = pageHead
                    };

                    pageVersion.PageVersionsElement = versionHead.Pages.Append(pageVersion);
                }

                update.Data.CopyTo(page.Data, update.Offset);
            }

            return _fileSet.WriteAndCommit(transaction, updates);
        }

        void IVersionedPageCache.RollbackTransaction(ITransaction transaction)
        {
            CleanupTransaction(transaction);
        }

        Task IVersionedPageCache.FinalizeTransaction(ITransaction transaction)
        {
            return _fileSet.FinalizeTransaction(transaction);
        }

        private List<PageUpdate> CleanupTransaction(ITransaction transaction)
        {
            if (transaction == null) return null;

            TransactionHead head;
            lock (_transactions)
            {
                if (_transactions.TryGetValue(transaction.TransactionId, out head))
                    _transactions.Remove(transaction.TransactionId);
            }

            lock (_versions)
            {
                VersionHead version;
                if (_versions.TryGetValue(transaction.BeginVersionNumber, out version))
                {
                    Interlocked.Decrement(ref version.TransactionCount);
                }
            }

            if (head != null && head.ModifiedPages != null)
            {
                foreach (var page in head.ModifiedPages.Values)
                    page.Dispose();
            }

            return head.Updates;
        }

        IPage IVersionedPageCache.Get(ITransaction transaction, ulong pageNumber)
        {
            if (transaction != null)
            {
                TransactionHead head;
                lock (_transactions)
                    _transactions.TryGetValue(transaction.TransactionId, out head);

                if (head != null)
                {
                    lock(head)
                    {
                        if (head.ModifiedPages != null)
                        {
                            IPage modifiedPage;
                            if (head.ModifiedPages.TryGetValue(pageNumber, out modifiedPage))
                                return modifiedPage.Reference();
                        }
                    }
                }
            }

            PageHead pageHead;
            lock (_pages)
            {
                if (!_pages.TryGetValue(pageNumber, out pageHead))
                {
                    var newPage = _pagePool.Get(pageNumber);
                    if (!_fileSet.Read(newPage))
                        newPage.Data.Initialize();

                    pageHead = new PageHead { PageNumber = pageNumber, Page = newPage };
                    _pages.Add(pageNumber, pageHead);

                    return newPage.Reference();
                }
            }

            var page = pageHead.Page;

            if (transaction != null && pageHead.Versions != null)
            {
                var pageVersion = pageHead.Versions.FirstElementOrDefault(pv => pv.VersionNumber <= transaction.BeginVersionNumber);

                if (pageVersion == null)
                    pageVersion = pageHead.Versions.LastElement();
                else
                    pageVersion = pageVersion.Prior;

                if (pageVersion != null)
                    page = pageVersion.Data.Page;
            }

            return page.Reference();
        }

        IVersionedPageCache IVersionedPageCache.Update(ITransaction transaction, IEnumerable<PageUpdate> updates)
        {
            if (transaction == null)
            {
                _fileSet.Write(transaction, updates);
                return this;
            }

            TransactionHead head;
            lock (_transactions)
            {
                if (!_transactions.TryGetValue(transaction.TransactionId, out head))
                    throw new FileLayerException("Attempt to write changes before a transaction was started or after it ended");
            }

            lock (head)
            {
                if (head.ModifiedPages == null)
                    head.ModifiedPages = new Dictionary<ulong, IPage>();

                if (head.Updates == null)
                    head.Updates = new List<PageUpdate>();
            }

            foreach (var update in updates.OrderBy(u => u.SequenceNumber))
            {
                IPage page;
                lock (head)
                {
                    head.Updates.Add(update);

                    if (!head.ModifiedPages.TryGetValue(update.PageNumber, out page))
                    {
                        page = _pagePool.Get(update.PageNumber);

                        using (var originalPage = ((IVersionedPageCache)this).Get(transaction, update.PageNumber))
                            originalPage.Data.CopyTo(page.Data, 0);

                        head.ModifiedPages.Add(update.PageNumber, page);
                    }
                }
                update.Data.CopyTo(page.Data, update.Offset);
            }

            return this;
        }

        IPage IVersionedPageCache.NewPage(ulong pageNumber)
        {
            var page = _pagePool.Get(pageNumber, true);
            lock (_pages) _pages.Add(pageNumber, new PageHead { PageNumber = pageNumber, Page = page });
            return page.Reference();
        }

        private class VersionHead
        {
            public ulong VersionNumber;
            public int TransactionCount;
            public OwinContainers.LinkedList<PageVersion> Pages;
        }

        private class PageHead
        {
            public ulong PageNumber;
            public IPage Page;
            public OwinContainers.LinkedList<PageVersion> Versions;
        }

        private class PageVersion
        {
            public PageHead PageHead;
            public OwinContainers.LinkedList<PageVersion>.ListElement PageVersionsElement;
            public ulong VersionNumber;
            public IPage Page;
        }

        private class TransactionHead
        {
            public ITransaction Transaction;
            public IDictionary<ulong, IPage> ModifiedPages;
            public List<PageUpdate> Updates;
        }
    }
}