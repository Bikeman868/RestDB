using System;
using System.Collections.Generic;
using System.IO;

namespace RestDB.Interfaces.FileLayer
{
    public interface ILogFile: IDisposable
    {
        /// <summary>
        /// Empties this log file deleting all transactions from it
        /// </summary>
        /// <returns></returns>
        bool Truncate();

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
        /// <param name="updateCount">Returns the number of updates in this log entry</param>
        /// <param name="updateSize">Returns the size of this update in bytes</param>
        /// <returns></returns>
        long ReadNext(long offset, out LogEntryStatus status, out long versionNumber, out long updateCount, out long updateSize);

        /// <summary>
        /// Reads all of the updates for a transaction beginning at the specified offset
        /// </summary>
        /// <param name="offset">The start offset of the transaction in the log</param>
        /// <returns>A list of the data file updates for this version/transaction</returns>
        List<PageUpdate> GetUpdates(long offset);
    }
}
