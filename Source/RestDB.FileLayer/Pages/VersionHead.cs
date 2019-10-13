using RestDB.Interfaces;
using System.Threading;
using OwinContainers = OwinFramework.Utility.Containers;

namespace RestDB.FileLayer.Pages
{
    /// <summary>
    /// Note that the VersionHead owns the PageVersion objects. When there
    /// are no transactions needing a specific version, the version head
    /// deletes all the pages for this version from the page heads
    /// </summary>
    internal class VersionHead
    {
        /// <summary>
        /// The database version number
        /// </summary>
        public ulong VersionNumber { get; private set; }

        /// <summary>
        /// The number of transactions executing against this version of the database
        /// </summary>
        private int _transactionCount;

        /// <summary>
        /// The pages that were modified when this version of the database was created
        /// </summary>
        private OwinContainers.LinkedList<PageVersion> _pageVersions;

        /// <summary>
        /// Constructs a new head for the linked list of page versions that
        /// have the same version number
        /// </summary>
        /// <param name="versionNumber">The version number of the database</param>
        public VersionHead(ulong versionNumber)
        {
            VersionNumber = versionNumber;
            _pageVersions = new OwinContainers.LinkedList<PageVersion>();
        }

        /// <summary>
        /// Deletes all page versions with this version number and recycles
        /// the memory occupied by the page data
        /// </summary>
        public void Dispose()
        {
            foreach (var pageVersionElement in _pageVersions)
                pageVersionElement.Data.Dispose();
        }

        /// <summary>
        /// Increments the count of active transactions
        /// </summary>
        public void TransactionStarted(TransactionHead transaction)
        {
            Interlocked.Increment(ref _transactionCount);
        }

        /// <summary>
        /// Decrements the count of active transactions
        /// </summary>
        public bool TransactionEnded(TransactionHead transaction)
        {
            return Interlocked.Decrement(ref _transactionCount) == 0;
        }

        /// <summary>
        /// Appends a page to this database version. This PageVersion will
        /// be disposed when the PageVersion is disposed.
        /// </summary>
        public void AddPage(PageVersion pageVersion)
        {
            _pageVersions.Append(pageVersion);
        }

        /// <summary>
        /// This is used during the commit of a transaction. It takes all the new
        /// page versions that were constructed during the commit merge and adds them
        /// to a dictionary of pages
        /// </summary>
        /// <param name="pages">The dictionary of pages to add to</param>
        public void AddToPages(PageHeadCollection pages)
        {
            foreach(var pageVersionElement in _pageVersions)
            {
                var pageVersion = pageVersionElement.Data;
                var pageNumber = pageVersion.Page.PageNumber;
                var pageHead = pages.GetPageHead(pageNumber, CacheHints.ForUpdate);
                pageHead.AddVersion(pageVersion);
            }
        }

        /// <summary>
        /// Returns true if there are any active transactions that were started
        /// against this version of the database
        /// </summary>
        public bool IsReferenced { get { return _transactionCount != 0; } }
    }
}