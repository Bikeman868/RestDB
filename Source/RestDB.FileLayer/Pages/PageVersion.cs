using RestDB.Interfaces.FileLayer;
using System;
using OwinContainers = OwinFramework.Utility.Containers;

namespace RestDB.FileLayer.Pages
{
    /// <summary>
    /// Represents a specific version of a specific page. The matrix of page numbers and database
    /// versions is sparse and dynamically populated. Once there are no more transactions on a 
    /// particular version, all the pages for that version are recycled
    /// </summary>
    internal class PageVersion: IDisposable
    {
        public ulong VersionNumber { get; private set; }
        public IPage Page { get; private set; }

        private PageHead _pageHead;
        private OwinContainers.LinkedList<PageVersion>.ListElement _pageVersionsElement;

        /// <summary>
        /// Constructa a new page version
        /// </summary>
        /// <param name="versionNumber">The version number of the database</param>
        /// <param name="page">How the page looks in this version of the database. This page
        /// will be disposed when the PageVersion is disposed</param>
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
}