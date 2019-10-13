using RestDB.Interfaces.DatabaseLayer;
using RestDB.Interfaces.FileLayer;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RestDB.FileLayer.Pages
{
    /// <summary>
    /// Contains information that is private to the transaction context
    /// </summary>
    internal class TransactionHead: IDisposable
    {
        public ITransaction Transaction { get; private set; }
        public List<PageUpdate> Updates { get; private set; }
        public List<PageHead> LockedPages { get; private set; }
        public TransactionHead Parent { get; private set; }

        private IDictionary<ulong, IPage> _modifiedPages;

        public TransactionHead(ITransaction transaction, TransactionHead parent)
        {
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

            return Parent == null ?  null : Parent.GetModifiedPage(pageNumber);
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

        public void Lock(PageHead page)
        {
            if (LockedPages == null)
            {
                lock (Transaction)
                {
                    if (LockedPages == null)
                        LockedPages = new List<PageHead>();
                }
            }

            bool needsLock;

            lock (LockedPages)
            {
                needsLock = !LockedPages.Contains(page);
                if (needsLock) LockedPages.Add(page);
            }

            if (needsLock) page.Lock(this);
        }
    }
}