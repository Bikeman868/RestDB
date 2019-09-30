using RestDB.Interfaces;
using RestDB.Interfaces.FileLayer;
using System;
using System.Collections.Generic;
using System.Threading;
using OwinContainers = OwinFramework.Utility.Containers;

namespace RestDB.FileLayer.Pages
{
    /* RULES
     * 
     * - The page cache can be out of sync with the underlying file set.
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
        readonly IFileSet _fileSet;
        readonly IPagePool _pagePool;
        readonly IDictionary<ulong, VersionHead> _versions;
        readonly IDictionary<ulong, TransactionHead> _transactions;
        readonly IDictionary<ulong, PageHead> _pages;

        public VersionedPageCache(IFileSet fileSet, IPagePoolFactory pagePoolFactory)
        {
            _fileSet = fileSet;
            _versions = new Dictionary<ulong, VersionHead>();
            _transactions = new Dictionary<ulong, TransactionHead>();
            _pages = new Dictionary<ulong, PageHead>();
            _pagePool = pagePoolFactory.Create(fileSet.PageSize);
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

        IVersionedPageCache IVersionedPageCache.EndTransaction(ITransaction transaction)
        {
            throw new NotImplementedException();
        }

        IPage IVersionedPageCache.Get(ulong pageNumber, ITransaction transaction)
        {
            PageHead pageHead;
            lock (_pages)
            {
                if (!_pages.TryGetValue(pageNumber, out pageHead))
                {
                    var newPage = _pagePool.Get(pageNumber);
                    _fileSet.Read(newPage);
                    pageHead = new PageHead { PageNumber = pageNumber, Page = newPage };
                    _pages.Add(pageNumber, pageHead);
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

            }
            else
            {

            }
            throw new NotImplementedException();
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
            public OwinContainers.LinkedList<PageVersion>.ListElement PageVersionslement;
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