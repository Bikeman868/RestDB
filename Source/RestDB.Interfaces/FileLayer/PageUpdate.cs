using System;
using System.Collections.Generic;
using System.Text;

namespace RestDB.Interfaces.FileLayer
{
    /// <summary>
    /// POCO class that is used to capture a range of bytes that were modified on a page
    /// in a page.
    /// </summary>
    public class PageUpdate
    {
        /// <summary>
        /// The page number of the page to change
        /// </summary>
        public long PageNumber;

        /// <summary>
        /// The byte offset into the page where the modification starts
        /// </summary>
        public int Offset;

        /// <summary>
        /// The bytes of data to write into the page
        /// </summary>
        public byte[] Data;
    }
}
