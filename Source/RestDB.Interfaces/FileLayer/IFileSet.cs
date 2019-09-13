using System;
using System.Collections.Generic;
using System.Text;

namespace RestDB.Interfaces.FileLayer
{
    /// <summary>
    /// Encapsulates a pair of files that together provide recoverable write operations.
    /// At startup the Recover() method can be called to fix any issues caused by a 
    /// sudden failure that resulted in data only being partially written. Guarantees that
    /// all blocks are fully written to disk or not written to disk at all.
    /// </summary>
    public interface IFileSet
    {
        /// <summary>
        /// Required to initialize the file set. Note that the BlockSize of the
        /// data file and the log file must match
        /// </summary>
        /// <param name="dataFile">The file that holds the data records</param>
        /// <param name="logFile">The file that holds the log entries</param>
        void Init(IDataFile dataFile, ILogFile logFile);

        /// <summary>
        /// Returns the block size of data in this file set
        /// </summary>
        Int32 BlockSize { get; }

        /// <summary>
        /// Call this at startup to recover partially written data and
        /// fix errors in the data file using the log file
        /// </summary>
        void Recover();

        /// <summary>
        /// Writes a block into the data file and log file in a way that
        /// makes the write recoverable if there is a sudden system failure
        /// </summary>
        /// <returns>True if the write operation succeeds. Fails only when the
        /// log file is full or unwritable</returns>
        bool Write(IBlock block);

        /// <summary>
        /// Tries to read data from the data file
        /// </summary>
        /// <param name="BlockNumber">The block number offset into the file</param>
        /// <param name="daat">The buffer to read into. Must be BlockSize in length</param>
        /// <returns>True if the read operation succeeded. If false then the data array
        /// may or may not have been overwritten</returns>
        bool Read(Int32 BlockNumber, byte[] data);
    }
}
