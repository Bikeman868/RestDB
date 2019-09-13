using System;
using System.Collections.Generic;
using System.IO;

namespace RestDB.Interfaces.FileLayer
{
    public interface ILogFile
    {
        /// <summary>
        /// Opens a physical disk file as a log file. Must have exclusive access to the file
        /// </summary>
        /// <param name="file">The file to read/write</param>
        /// <returns>True if the file could be opened</returns>
        bool Open(FileInfo file);

        /// <summary>
        /// Creates a new file and initializes it ready for storing a log
        /// </summary>
        /// <param name="file">The file to create</param>
        /// <param name="blockSize">The size of data blocks that will be stored in this log file</param>
        /// <returns>True if the file could be created</returns>
        bool Create(FileInfo file, Int32 blockSize);

        /// <summary>
        /// The size of the blocks in this file
        /// </summary>
        Int32 BlockSize { get; }

        /// <summary>
        /// Writes an entry in the log indicating that we about to start writing a 
        /// block to the data file
        /// </summary>
        /// <param name="blockNumber">The block number of the block that is about to be written</param>
        /// <param name="data">The bytes that are going to be written. Must be BlockSize in length</param>
        /// <returns>An offset into the file of the tail marker</returns>
        Int64 WriteStart(Int32 blockNumber, byte[] data);

        /// <summary>
        /// Writes an entry in the log indicating that we sucessfully wrote data to
        /// the data file and this log entry can be ignored during recovery
        /// </summary>
        /// <param name="tailMarkerOffset">Pass the value returned by the WriteStart method</param>
        void WriteEnd(Int64 tailMarkerOffset);

        /// <summary>
        /// Reads the next log file entry
        /// </summary>
        /// <param name="offset">The offset to start reading from. Pass 0 for the first
        /// call, then pass the return value on subsequent calls to read the whole log</param>
        /// <param name="status">Returns the status of the log file entry</param>
        /// <param name="blockNumber">Returns the block number that was written</param>
        /// <param name="data">Returns the data that was written, must have BlockSize length</param>
        /// <returns></returns>
        Int64 ReadNext(Int64 offset, out LogEntryStatus status, out Int32 blockNumber, byte[] data);
    }
}
