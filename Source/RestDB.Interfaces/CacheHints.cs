using System;
using System.Collections.Generic;
using System.Text;

namespace RestDB.Interfaces
{
    [Flags]
    public enum CacheHints
    {
        /// <summary>
        /// No special caching hints provided
        /// </summary>
        None = 0x0,

        /// <summary>
        /// This request is for metadata, for example column definitions for a table
        /// </summary>
        MetaData = 0x1,

        /// <summary>
        /// The data is being requested as part of a full table scan
        /// </summary>
        FullTableScan = 0x2,

        /// <summary>
        /// The data is being requested as part of an index scan
        /// </summary>
        IndexScan,

        /// <summary>
        /// The requested data will be updated later
        /// </summary>
        ForUpdate = 0x8,
    }
}
