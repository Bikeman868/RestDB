using System;

namespace RestDB.Interfaces.FileLayer
{
    /// <summary>
    /// Encapsulates a page of data that can be read/written to a data file
    /// </summary>
    public interface IPage: IDisposable
    {
        /// <summary>
        /// Unique page number within a data file
        /// </summary>
        ulong PageNumber { get; }

        /// <summary>
        /// The data from this part of the data file. Length must match
        /// the page size of the file it is read/written to.
        /// </summary>
        byte[] Data { get; }

        /// <summary>
        /// Increments the reference count of this page. You must Dispose()
        /// of th page once for each call to this method. The Dispose() method
        /// decrements the reference count, when the reference count is decremented 
        /// to zero then the page is returned to the page pool so that it can be 
        /// reused. When pages are created or taken out of the pool they have a 
        /// reference count of 1, so the usage pattern looks like:
        /// Create/Get from pool
        ///   Reference();
        ///   Dispose();
        ///   Reference();
        ///     Reference();
        ///     Dispose();
        ///   Dispose();
        ///  Dispose(); // Page returned to the pool for reuse
        /// </summary>
        IPage Reference();
    }
}