using System;

namespace RestDB.Interfaces.FileLayer
{
    /// <summary>
    /// Provides resilient versioned persistent storage of pages of
    /// data where each page is the same size. The pages will be stored
    /// using a pair of files to ensure that partial writes can be recovered
    /// on restart after a system failure
    /// </summary>
    public interface IPageStore
    {
        /// <summary>
        /// Initializes the page store with a backing file
        /// </summary>
        void Init(IFileSet fileSet);

        /// <summary>
        /// Creates a new page in the page store with a unique page number
        /// </summary>
        IPage Allocate();

        /// <summary>
        /// Marks a page as available for reuse. This page
        /// could be returned immediately on the next call to 
        /// Allocate()
        /// </summary>
        void Release(long pageNumber);

        /// <summary>
        /// Provides access to the pages in this page store
        /// </summary>
        IVersionedPageCache Pages { get; }
    }
}
