using System;
using System.Collections.Generic;
using System.Text;

namespace RestDB.Interfaces.FileLayer
{
    /// <summary>
    /// Encapsulates a block of data that can be read/written to a data file
    /// </summary>
    public interface IBlock
    {
        /// <summary>
        /// Unique block number within the file
        /// </summary>
        Int64 BlockNumber { get; set; }

        /// <summary>
        /// The data from this part of the data file. Length must match
        /// the block size of the file it is read/written to.
        /// </summary>
        byte[] Data { get; set; }
    }
}
