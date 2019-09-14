using System;
using System.Collections.Generic;
using System.Text;

namespace RestDB.Interfaces.FileLayer
{
    /// <summary>
    /// Encapsulates a page of data that can be read/written to a data file
    /// </summary>
    public interface IPage
    {
        /// <summary>
        /// Unique page number within a data file
        /// </summary>
        long PageNumber { get; set; }

        /// <summary>
        /// The data from this part of the data file. Length must match
        /// the page size of the file it is read/written to.
        /// </summary>
        byte[] Data { get; set; }

        /// <summary>
        /// Increments the reference count of this page
        /// </summary>
        void Reference();

        /// <summary>
        /// Decrements this reference count of this page. If this
        /// reduces the reference count to zero then the page is returned
        /// to the page pool so that it can be reused
        /// </summary>
        void Dereference();
    }
}
