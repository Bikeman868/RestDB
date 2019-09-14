using System;
using System.Collections.Generic;
using System.Text;

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
        /// Initializes the page pool
        /// </summary>
        /// <param name="pageSize">The size of pages to pool and reuse</param>
        void Init(int pageSize);

        /// <summary>
        /// Gets an available page from the pool and initializes its page number.
        /// If the page pool is empty then a new page instance is constructed
        /// </summary>
        /// <param name="pageNumber">Initializes the page number property of the page</param>
        /// <param name="clear">Page true to zero the data in the page</param>
        IPage Get(long pageNumber, bool clear = false);
    }
}
