namespace RestDB.Interfaces.FileLayer
{
    /// <summary>
    /// Provides a mechanism for pooling and reusing data pages to reduce
    /// pressure on the garbage collector. All pages in the page pool must 
    /// have the same PageSize
    /// </summary>
    public interface IPagePool
    {
        /// <summary>
        /// The size of the pages in this page pool
        /// </summary>
        uint PageSize { get; }

        /// <summary>
        /// Gets an available page from the pool and initializes its page number.
        /// If the page pool is empty then a new page instance is constructed. You
        /// must dispose of the page when you are done with it to return it back to 
        /// the pool
        /// </summary>
        /// <param name="pageNumber">Initializes the page number property of the page</param>
        /// <param name="clear">Page true to zero the data in the page</param>
        IPage Get(ulong pageNumber, bool clear = false);
    }
}