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

    internal class VersionedPageCache : IVersionedPageCache
    {
        private readonly IStartUpLog _startUpLog;
        private readonly IErrorLog _errorLog;

        private readonly IFileSet _fileSet;
        private readonly IPagePool _pagePool;
        private readonly IDatabase _database;

        private readonly IDictionary<ulong, VersionHead> _versions;
        private readonly IDictionary<ulong, TransactionHead> _transactions;
        private readonly IDictionary<ulong, PageHead> _pages;

        private readonly Thread _versionCleanupThread;
        private readonly Thread _pageCleanupThread;

        private bool _disposing;

        uint IVersionedPageCache.PageSize => _fileSet.PageSize;

        public VersionedPageCache(
            IFileSet fileSet,
            IDatabase database,
            IPagePoolFactory pagePoolFactory, 
            IStartUpLog startUpLog,
            IErrorLog errorLog)
        {
            _startUpLog = startUpLog;
            _errorLog = errorLog;
            _fileSet = fileSet;
            _database = database;

            _versions = new Dictionary<ulong, VersionHead>();
            _transactions = new Dictionary<ulong, TransactionHead>();
            _pages = new Dictionary<ulong, PageHead>();

            startUpLog.Write("Creating a new page cache for " + _fileSet);

            _pagePool = pagePoolFactory.Create(fileSet.PageSize);

            _versionCleanupThread = new Thread(() => 
            {
                _startUpLog.Write("Page cache version clean up thread starting");

                while (!_disposing)
                {
                    try
                    {
                        Thread.Sleep(20);

                        List<VersionHead> versions;
                        lock (_versions) versions = _versions.Values.OrderBy(v => v.VersionNumber).ToList();

                        foreach(var version in versions)
                        {
                            if (version.IsReferenced || version.VersionNumber == _database.CurrentVersion) break;

                            lock (_versions) _versions.Remove(version.VersionNumber);
                            version.Dispose();
                        }
                    }
                    catch (ThreadAbortException)
                    {
                        return;
                    }
                    catch(Exception ex)
                    {
                        _errorLog.Write("Exception in page cache version cleanup thread. " + ex.Message, ex);
                    }
                }

                _startUpLog.Write("Page cache version clean up thread exiting");
            })
            {
                IsBackground = true,
                Name = "Page cache version cleanup",
                Priority = ThreadPriority.AboveNormal
            };

            _pageCleanupThread = new Thread(() =>
            {
                _startUpLog.Write("Page cache stale page clean up thread starting");

                while (!_disposing)
                {
                    try
                    {
                        Thread.Sleep(50);

                        // TODO: Delete pages that have not been touched for a while and
                        //       have no cached versions
                    }
                    catch (ThreadAbortException)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        _errorLog.Write("Exception in page cache stale page cleanup thread. " + ex.Message, ex);
                    }
                }

                _startUpLog.Write("Page cache stale page clean up thread exiting");
            })
            {
                IsBackground = true,
                Name = "Page cache version cleanup",
                Priority = ThreadPriority.AboveNormal
            };

            _versionCleanupThread.Start();
            _pageCleanupThread.Start();
        }

        public void Dispose()
        {
            _startUpLog.Write("Closing page cache for " + _fileSet);
            _disposing = true;

            _fileSet.Dispose();

            _versionCleanupThread.Join(200);
            _pageCleanupThread.Join(200);

            lock (_transactions)
            {
                foreach(var transactionHead in _transactions.Values)
                    transactionHead.Dispose();

                _transactions.Clear();
            }

            lock (_versions)
            {
                foreach (var version in _versions.Values)
                    version.Dispose();

                _versions.Clear();
            }

            lock (_pages)
            {
                foreach (var pageHead in _pages.Values)
                    pageHead.Dispose();

                _pages.Clear();
            }
        }

        public override string ToString()
        {
            return "page cache on " + _fileSet;
        }

        IVersionedPageCache IVersionedPageCache.BeginTransaction(ITransaction transaction)
        {
            var head = new TransactionHead(transaction);

            lock (_transactions) _transactions.Add(transaction.TransactionId, head);

            VersionHead version;

            lock (_versions)
            {
                if (!_versions.TryGetValue(transaction.BeginVersionNumber, out version))
                {
                    version = new VersionHead(transaction.BeginVersionNumber);
                    _versions.Add(transaction.BeginVersionNumber, version);
                }
            }

            version.TransactionStarted(transaction);

            return this;
        }

        private List<PageUpdate> CleanupTransaction(ITransaction transaction)
        {
            if (transaction == null) return null;

            TransactionHead transactionHead;
            lock (_transactions)
            {
                if (_transactions.TryGetValue(transaction.TransactionId, out transactionHead))
                    _transactions.Remove(transaction.TransactionId);
            }

            lock (_versions)
            {
                if (_versions.TryGetValue(transaction.BeginVersionNumber, out VersionHead version))
                    version.TransactionEnded(transaction);
            }

            if (transactionHead != null)
            {
                using (transactionHead)
                    return transactionHead.Updates;
            }

            return null;
        }

        Task IVersionedPageCache.CommitTransaction(ITransaction transaction)
        {
            var updates = CleanupTransaction(transaction);

            if (updates == null || updates.Count == 0)
                return null;

            var versionHead = new VersionHead(transaction.CommitVersionNumber);

            lock (_versions)
                _versions.Add(transaction.CommitVersionNumber, versionHead);

            IPage newPage = null;
            foreach(var update in updates.OrderBy(u => u.PageNumber).ThenBy(u => u.SequenceNumber))
            {
                if (newPage == null || update.PageNumber != newPage.PageNumber)
                {
                    newPage = _pagePool.Get(update.PageNumber);

                    PageVersion currentPageVersion = null;
                    lock (_pages)
                    {
                        if (_pages.TryGetValue(update.PageNumber, out PageHead pageHead))
                            currentPageVersion = pageHead.Versions.FirstOrDefault();
                    }

                    if (currentPageVersion == null)
                    {
                        if (!_fileSet.Read(newPage)) newPage.Data.Initialize();
                    }
                    else
                    {
                        currentPageVersion.Page.Data.CopyTo(newPage.Data, 0);
                    }

                    versionHead.AddPage(new PageVersion(versionHead.VersionNumber, newPage));
                }

                update.Data.CopyTo(newPage.Data, update.Offset);
            }

            versionHead.AddToPages(_pages, GetFromFileSet);

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

        IPage IVersionedPageCache.Get(ITransaction transaction, ulong pageNumber, CacheHints hints)
        {
            if (transaction != null)
            {
                TransactionHead transactionHead;
                lock (_transactions)
                    _transactions.TryGetValue(transaction.TransactionId, out transactionHead);

                if (transactionHead != null)
                {
                    var modifiedPage = transactionHead.GetModifiedPage(pageNumber);
                    if (modifiedPage != null) return modifiedPage;
                }
            }

            PageHead pageHead;
            lock (_pages)
            {
                if (!_pages.TryGetValue(pageNumber, out pageHead))
                {
                    pageHead = new PageHead(pageNumber, GetFromFileSet);
                    _pages.Add(pageNumber, pageHead);
                }
            }

            return pageHead.GetVersion(transaction.BeginVersionNumber);
        }

        private IPage GetFromFileSet(ulong pageNumber)
        {
            var page = _pagePool.Get(pageNumber);
            if (!_fileSet.Read(page)) page.Data.Initialize();
            return page;
        }

        IVersionedPageCache IVersionedPageCache.Update(ITransaction transaction, IEnumerable<PageUpdate> updates)
        {
            if (transaction == null)
            {
                _fileSet.Write(transaction, updates);
                return this;
            }

            TransactionHead transactionHead;
            lock (_transactions)
            {
                if (!_transactions.TryGetValue(transaction.TransactionId, out transactionHead))
                    throw new FileLayerException("Attempt to write changes before a transaction was started or after it ended");
            }

            var updateList = updates.OrderBy(u => u.SequenceNumber).ToList();
            transactionHead.AddUpdates(updateList);

            foreach (var update in updateList)
            {
                IPage modifiedPage;
                lock (transactionHead)
                {
                    modifiedPage = transactionHead.GetModifiedPage(update.PageNumber);
                    if (modifiedPage == null)
                    {
                        modifiedPage = _pagePool.Get(update.PageNumber);

                        using (var originalPage = ((IVersionedPageCache)this).Get(transaction, update.PageNumber, CacheHints.ForUpdate))
                            originalPage.Data.CopyTo(modifiedPage.Data, 0);

                        transactionHead.SetModifiedPage(modifiedPage);
                    }
                }
                update.Data.CopyTo(modifiedPage.Data, update.Offset);
            }

            return this;
        }

        IPage IVersionedPageCache.NewPage(ulong pageNumber)
        {
            using (var page = _pagePool.Get(pageNumber, true))
            {
                lock (_pages) _pages.Add(pageNumber, new PageHead(pageNumber, GetFromFileSet));
                return page.Reference();
            }
        }

        /// <summary>
        /// Note that the VersionHead owns the PageVersion objects. When there
        /// are no transactions needing a specific version, the version head
        /// deletes all the pages for this version from the page heads
        /// </summary>
        private class VersionHead
        {
            public ulong VersionNumber { get; private set; }

            private int _transactionCount;
            private OwinContainers.LinkedList<PageVersion> _pageVersions;

            public VersionHead(ulong versionNumber)
            {
                VersionNumber = versionNumber;
                _pageVersions = new OwinContainers.LinkedList<PageVersion>();
            }

            public void Dispose()
            {
                foreach (var pageVersionElement in _pageVersions)
                    pageVersionElement.Data.Dispose();
            }

            public void TransactionStarted(ITransaction transaction)
            {
                Interlocked.Increment(ref _transactionCount);
            }

            public bool TransactionEnded(ITransaction transaction)
            {
                return Interlocked.Decrement(ref _transactionCount) == 0;
            }

            public void AddPage(PageVersion pageVersion)
            {
                _pageVersions.Append(pageVersion);
            }

            public void AddToPages(IDictionary<ulong, PageHead> pages, Func<ulong, IPage> pageGetter)
            {
                foreach(var pageVersionElement in _pageVersions)
                {
                    var pageVersion = pageVersionElement.Data;
                    var pageNumber = pageVersion.Page.PageNumber;

                    lock(pages)
                    {
                        if (!pages.TryGetValue(pageNumber, out PageHead pageHead))
                        {
                            pageHead = new PageHead(pageNumber, pageGetter);
                            pages.Add(pageNumber, pageHead);
                        }
                        pageHead.AddVersion(pageVersion);
                    }
                }
            }

            public bool IsReferenced { get { return _transactionCount != 0; } }
        }

        /// <summary>
        /// Contains a list of prior versions of a page that are still accessible by transactions
        /// </summary>
        private class PageHead: IDisposable
        {
            public ulong PageNumber { get; private set; }
            public OwinContainers.LinkedList<PageVersion> Versions { get; private set; }

            private Func<ulong, IPage> _pageGetter;

            public PageHead(ulong pageNumber, Func<ulong, IPage> pageGetter)
            {
                PageNumber = pageNumber;
                Versions = new OwinContainers.LinkedList<PageVersion>();
                _pageGetter = pageGetter;
            }

            public void Dispose()
            {
            }

            private void EnsureOriginalVersion()
            {
                if (Versions.IsEmpty)
                {
                    var page = _pageGetter(PageNumber);
                    var pageVersion = new PageVersion(0, page);

                    lock (Versions)
                    {
                        if (Versions.IsEmpty)
                        {
                            pageVersion.Added(this, Versions.Append(pageVersion));
                        }
                    }
                }
            }

            public IPage GetVersion(ulong versionNumber)
            {
                EnsureOriginalVersion();

                var pageVersionElement = Versions.FirstElementOrDefault(pv => pv.VersionNumber <= versionNumber);

                if (pageVersionElement == null)
                    throw new FileLayerException(
                        "No suitable version in the VersionPageCache. Theoretically this can never "+
                        "happen so there is a bug in the code. Page number " + PageNumber + " version " + versionNumber);

                return pageVersionElement.Data.Page.Reference();
            }

            /// <summary>
            /// Adds a new version of this page
            /// </summary>
            public PageVersion AddVersion(PageVersion pageVersion)
            {
                EnsureOriginalVersion();

                lock (Versions)
                {
                    var nextVersion = Versions.FirstElementOrDefault(v => v.VersionNumber >= pageVersion.VersionNumber);
                    pageVersion.Added(this, Versions.InsertBefore(nextVersion, pageVersion));
                }

                return pageVersion;
            }

            /// <summary>
            /// Removes a version of a page that is no longer reachable by any transaction
            /// </summary>
            public void DeleteVersion(OwinContainers.LinkedList<PageVersion>.ListElement versionElement)
            {
                Versions.Delete(versionElement);
            }
        }

        /// <summary>
        /// Represents a specific version of a specific page. The matrix of page numbers and database
        /// versions is sparse and dynamically populated. Once there are no more transactions on a 
        /// particular version, all the pages for that version are recycled
        /// </summary>
        private class PageVersion: IDisposable
        {
            public ulong VersionNumber { get; private set; }
            public IPage Page { get; private set; }

            private PageHead _pageHead;
            private OwinContainers.LinkedList<PageVersion>.ListElement _pageVersionsElement;

            public PageVersion(ulong versionNumber, IPage page)
            {
                VersionNumber = versionNumber;
                Page = page.Reference();
            }

            /// <summary>
            /// This is split out from the constructor to make thread locking more efficient
            /// </summary>
            public void Added(PageHead pageHead, OwinContainers.LinkedList<PageVersion>.ListElement listElement)
            {
                _pageHead = pageHead;
                _pageVersionsElement = listElement;
            }

            public void Dispose()
            {
                if (_pageHead != null)
                    _pageHead.DeleteVersion(_pageVersionsElement);

                Page.Dispose();
            }
        }

        /// <summary>
        /// Contains information that is private to the transaction context
        /// </summary>
        private class TransactionHead: IDisposable
        {
            public ITransaction Transaction { get; private set; }
            public List<PageUpdate> Updates { get; private set; }

            private IDictionary<ulong, IPage> _modifiedPages;

            public TransactionHead(ITransaction transaction)
            {
                Transaction = transaction;
            }

            public void Dispose()
            {
                if (_modifiedPages != null)
                {
                    lock (_modifiedPages)
                    {
                        foreach (var page in _modifiedPages.Values)
                            page.Dispose();
                    }
                }
            }

            public void AddUpdates(IEnumerable<PageUpdate> updates)
            {
                if (Updates == null)
                {
                    lock(Transaction)
                    {
                        if (Updates == null)
                            Updates = new List<PageUpdate>();
                    }
                }

                var start = (uint)Updates.Count;
                lock (Updates) Updates.AddRange(updates.Select(u => { u.SequenceNumber += start; return u; }));
            }

            public IPage GetModifiedPage(ulong pageNumber)
            {
                if (_modifiedPages != null)
                {
                    lock (_modifiedPages)
                    {
                        if (_modifiedPages.TryGetValue(pageNumber, out IPage modifiedPage))
                            return modifiedPage.Reference();
                    }
                }

                return null;
            }

            public void SetModifiedPage(IPage page)
            {
                if (_modifiedPages == null)
                {
                    lock (Transaction)
                    {
                        if (_modifiedPages == null)
                            _modifiedPages = new Dictionary<ulong, IPage>();
                    }
                }

                lock (_modifiedPages)
                    _modifiedPages.Add(page.PageNumber, page.Reference());
            }
        }
    }
}