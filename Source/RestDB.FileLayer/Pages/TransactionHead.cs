using RestDB.Interfaces;
using RestDB.Interfaces.DatabaseLayer;
using RestDB.Interfaces.FileLayer;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RestDB.FileLayer.Pages
{
    /// <summary>
    /// Contains information that is private to a transaction. Implements
    /// nested transactions and updates to data within a transaction
    /// </summary>
    internal class TransactionHead: IDisposable
    {
        public ITransaction Transaction { get; private set; }
        public List<PageUpdate> Updates { get; private set; }
        public List<PageHead> LockedPages { get; private set; }
        public TransactionHead Parent { get; private set; }

        private readonly IPagePool _pagePool;
        private IDictionary<ulong, IPage> _modifiedPages;

        public TransactionHead(ITransaction transaction, TransactionHead parent, IPagePool pagePool)
        {
            _pagePool = pagePool;
            Transaction = transaction;
            Parent = parent;
        }

        public void Dispose()
        {
            if (LockedPages != null)
            {
                lock(LockedPages)
                {
                    foreach (var pageHead in LockedPages)
                        pageHead.Unlock(this, true);
                }
            }

            if (_modifiedPages != null)
            {
                lock (_modifiedPages)
                {
                    foreach (var page in _modifiedPages.Values)
                        page.Dispose();
                }
            }
        }

        public TransactionHead Root => Parent == null ? this : Parent.Root;

        public void AddUpdates(IEnumerable<PageUpdate> updates, PageHeadCollection pages)
        {
            EnsureUpdates();

            var start = Updates.Count;
            int end;
            lock (Updates)
            {
                Updates.AddRange(updates.OrderBy(u => u.SequenceNumber).Select(u => { u.SequenceNumber += (uint)start; return u; }));
                end = Updates.Count;
            }

            for (var i = start; i < end; i++)
            {
                PageUpdate update;
                lock (Updates) update = Updates[i];

                IPage modifiedPage = null;
                try
                {
                    lock (Transaction)
                    {
                        modifiedPage = GetModifiedPage(update.PageNumber);
                        if (modifiedPage == null)
                        {
                            modifiedPage = _pagePool.Get(update.PageNumber);

                            var pageHead = pages.GetPageHead(update.PageNumber, CacheHints.ForUpdate);

                            using (var originalPage = pageHead.GetVersion(Root.Transaction.BeginVersionNumber))
                                originalPage.Data.CopyTo(modifiedPage.Data, 0);

                            SetModifiedPage(modifiedPage);
                        }
                        update.Data.CopyTo(modifiedPage.Data, update.Offset);
                    }
                }
                finally
                {
                    modifiedPage.Dispose();
                }
            }
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

            return Parent == null ? null : Parent.GetModifiedPage(pageNumber);
        }

        public void SetModifiedPage(IPage page)
        {
            EnsureModifiedPages();

            lock (_modifiedPages)
            {
                if (_modifiedPages.TryGetValue(page.PageNumber, out IPage existing))
                    existing.Dispose();
                _modifiedPages[page.PageNumber] = page.Reference();
            }
        }

        public bool Lock(PageHead pageHead)
        {
            if (Parent == null)
            {
                EnsureLockedPages();

                lock (LockedPages)
                {
                    if (LockedPages.Contains(pageHead))
                        return false;

                    LockedPages.Add(pageHead);
                }

                pageHead.Lock(this);

                using (var latestVersion = pageHead.GetVersion(null))
                {
                    using (var newPage = _pagePool.Get(pageHead.PageNumber))
                    {
                        latestVersion.Data.CopyTo(newPage.Data, 0);

                        if (Updates != null)
                        {
                            lock (Updates)
                            {
                                foreach (var update in Updates)
                                    if (update.PageNumber == pageHead.PageNumber)
                                        update.Data.CopyTo(newPage.Data, update.Offset);
                            }
                        }
                        SetModifiedPage(newPage);
                    }
                }

                return true;
            }

            if (!Parent.Lock(pageHead))
                return false;

            using (var parentPage = Parent.GetModifiedPage(pageHead.PageNumber))
            {
                using (var newPage = _pagePool.Get(pageHead.PageNumber))
                {
                    parentPage.Data.CopyTo(newPage.Data, 0);

                    if (Updates != null)
                    {
                        lock (Updates)
                        {
                            foreach (var update in Updates)
                                if (update.PageNumber == pageHead.PageNumber)
                                    update.Data.CopyTo(newPage.Data, update.Offset);
                        }
                    }

                    SetModifiedPage(newPage);
                }
            }

            return true;
        }

        public void Unlock(PageHead pageHead)
        {
        }

        private void EnsureUpdates()
        {
            if (Updates == null)
            {
                lock (Transaction)
                {
                    if (Updates == null)
                        Updates = new List<PageUpdate>();
                }
            }
        }

        private void EnsureModifiedPages()
        {
            if (_modifiedPages == null)
            {
                lock (Transaction)
                {
                    if (_modifiedPages == null)
                        _modifiedPages = new Dictionary<ulong, IPage>();
                }
            }
        }

        private void EnsureLockedPages()
        {
            if (LockedPages == null)
            {
                lock (Transaction)
                {
                    if (LockedPages == null)
                        LockedPages = new List<PageHead>();
                }
            }
        }
    }
}