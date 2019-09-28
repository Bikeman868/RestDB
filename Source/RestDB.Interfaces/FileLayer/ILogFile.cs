using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RestDB.Interfaces.FileLayer
{
    public interface ILogFile : IDisposable
    {
        /// <summary>
        /// Empties this log file.
        /// </summary>
        bool Truncate();

        /// <summary>
        /// Removes old transactions and shrinks the log file
        /// </summary>
        /// <param name="oldestVersionNumber">The oldest version number to keep</param>
        bool Shrink(ulong oldestVersionNumber);

        /// <summary>
        /// This is called when a transaction completes and has made modifications to the data.
        /// </summary>
        /// <param name="transaction">The transaction that made the changes</param>
        /// <param name="updates">The changes that were made</param>
        /// <returns>The offset into the log file of this log file entry</returns>
        ulong CommitStart(ITransaction transaction, IEnumerable<PageUpdate> updates);

        /// <summary>
        /// This is called when all of the changes for a given transaction have been
        /// successfully written to the log files of all of the tables. At this point
        /// all of the changes to the database have been captured in log files so this
        /// transaction can be rolled forward after a system crash.
        /// </summary>
        /// <param name="offset">You obtain this offset by calling the CommitStart() method</param>
        void CommitComplete(ulong offset);

        /// <summary>
        /// This is called when all of the changes for a given transaction have been
        /// successfully applied to the main data file and this log entry is no longer
        /// needed to roll the database forward when restarting after a crash.
        /// </summary>
        /// <param name="offset">You obtain this offset by calling the CommitStart() method</param>
        void CommitApplied(ulong offset);

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
        ulong ReadNext(
            ulong offset,
            out LogEntryStatus status,
            out ulong versionNumber,
            out uint updateCount,
            out ulong updateSize);

        /// <summary>
        /// Reads all of the updates for a transaction beginning at the specified offset
        /// </summary>
        /// <param name="offset">The start offset of the transaction in the log</param>
        /// <returns>A list of the data file updates for this version/transaction</returns>
        List<PageUpdate> GetUpdates(ulong offset);
    }
}