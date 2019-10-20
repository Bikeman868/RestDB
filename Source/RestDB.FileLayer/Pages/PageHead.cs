using RestDB.Interfaces.DatabaseLayer;
using RestDB.Interfaces.FileLayer;
using System;
using System.Threading;
using OwinContainers = OwinFramework.Utility.Containers;

namespace RestDB.FileLayer.Pages
{
    /// <summary>
    /// Contains a list of prior versions of a page that are still accessible by transactions
    /// </summary>
    internal class PageHead: IDisposable
    {
        public ulong PageNumber { get; private set; }
        public OwinContainers.LinkedList<PageVersion> Versions { get; private set; }

        private TransactionHead _lockingTransaction;
        private uint _lockCount;
        private ManualResetEventSlim _unlockEvent;

        /// <summary>
        /// Constructs a new page head. The page head handles page locks and versions
        /// </summary>
        /// <param name="pageNumber">The page number of the page</param>
        /// <param name="page">The current version of the page from storage</param>
        public PageHead(ulong pageNumber, IPage page)
        {
            PageNumber = pageNumber;
            Versions = new OwinContainers.LinkedList<PageVersion>();

            var pageVersion = new PageVersion(0, page);
            pageVersion.Added(this, Versions.Append(pageVersion));
        }

        public void Dispose()
        {
        }

        public void Lock(TransactionHead transaction)
        {
            // All page locking is done by the root of the transaction heirachy. Locks are
            // only released when the outermost transaction completes
            transaction = transaction.Root;

            // TODO: If there are transactions with modified versions of this
            //       page then we should wait for these transactions to complete
            //       and prevent any new write operations to the page

            while (true)
            {
                lock (Versions)
                {
                    if (_unlockEvent == null)
                        _unlockEvent = new ManualResetEventSlim(false);

                    if (_lockingTransaction == null)
                    {
                        _lockingTransaction = transaction;
                        _lockCount = 1;
                        _unlockEvent.Reset();
                        return;
                    }
                    else if (_lockingTransaction == transaction)
                    {
                        _lockCount++;
                        return;
                    }
                }

                _unlockEvent.Wait();
            }
        }

        public void Unlock(TransactionHead transaction, bool unlockAll)
        {
            // All page locking is done by the root of the transaction heirachy. Locks are
            // only released when the outermost transaction completes
            transaction = transaction.Root;

            if (transaction != _lockingTransaction)
                throw new FileLayerException("You cannot unlock a page that you do not have locked");

            if (unlockAll)
                _lockCount = 0;
            else _lockCount--;

            if (_lockCount == 0)
            {
                _lockingTransaction = null;
                _unlockEvent.Set();
            }
        }

        public IPage GetVersion(ulong? versionNumber)
        {
            var pageVersionElement = versionNumber.HasValue
                ? Versions.FirstElementOrDefault(pv => pv.VersionNumber <= versionNumber.Value)
                : Versions.FirstElementOrDefault();

            if (pageVersionElement == null)
                throw new FileLayerException(
                    "No suitable version in the PageCache. Theoretically this can never "+
                    "happen so there is a bug in the code. Page number " + PageNumber + " version " + versionNumber);

            return pageVersionElement.Data.Page.Reference();
        }

        /// <summary>
        /// Adds a new version of this page
        /// </summary>
        public PageVersion AddVersion(PageVersion pageVersion)
        {
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
}