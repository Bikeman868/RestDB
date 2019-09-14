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
        /// <param name="pageSize">The size of data pages that will be stored in this log file</param>
        /// <returns>True if the file could be created</returns>
        bool Create(FileInfo file, int pageSize);

        /// <summary>
        /// The size of the pages in this file
        /// </summary>
        int PageSize { get; }

        /// <summary>
        /// This is called when a transaction completes and has made modifications to the data.
        /// </summary>
        /// <param name="versionNumber">The version number of the transaction that made the changes</param>
        /// <param name="updates">The changes that were made</param>
        /// <returns>The offset into the log file of this log file entry</returns>
        long Write(long versionNumber, IEnumerable<PageUpdate> updates);

        /// <summary>
        /// This is called when all of the changes for a given transaction have been
        /// successfully applied to the main data file and this log entry is no longer
        /// needed to roll the database forward when restarting after a crash.
        /// </summary>
        /// <param name="offset">You obtain this offset by calling the Write() method</param>
        void Committed(long offset);

        /// <summary>
        /// Skips to the next log file entry and reads the header only
        /// </summary>
        /// <param name="offset">The offset to start reading from. Pass 0 for the first
        /// call, then pass the return value on subsequent calls to read the whole log</param>
        /// <param name="status">Returns the status of the log file entry</param>
        /// <param name="versionNumber">Returns the version number of the transaction</param>
        /// <returns></returns>
        long ReadNext(long offset, out LogEntryStatus status, out long versionNumber);

        /// <summary>
        /// Reads all of the updates for a transaction beginning at the specified offset
        /// </summary>
        /// <param name="offset">The start offset of the transaction in the log</param>
        /// <returns>A list of the data file updates for this version/transaction</returns>
        List<PageUpdate> GetUpdates(long offset);
    }
}
