using RestDB.Interfaces.DatabaseLayer;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RestDB.Interfaces.FileLayer
{
    /// <summary>
    /// Encapsulates a pair of files that together provide recoverable write operations.
    /// At startup methods can be called to find and fix any issues caused by a 
    /// sudden failure that resulted in data only being partially written. Guarantees that
    /// all pages are fully written to disk or not written to disk at all.
    /// </summary>
    public interface IFileSet: IDisposable
    {
        /// <summary>
        /// Returns the page size of data in this file set
        /// </summary>
        uint PageSize { get; }

        /// <summary>
        /// Call this at startup to get a list of all transactions in this data file 
        /// set that were not fully written to the data file but may or my not be fully
        /// committed to the log file.
        /// </summary>
        /// <param name="rollBackVersions">Returns a list of transaction version numbers
        /// that must be rolled back because some data may have been lost. If any
        /// file set returns this status for a transaction it must be rolled back
        /// in all file sets</param>
        /// <param name="rollForwardVersions">Returns a list of transactions that
        /// can be rolled forward in this file set. Only if all file sets return this
        /// status for a transaction should the transaction be rolled forwards</param>
        void GetIncompleteTransactions(out ulong[] rollBackVersions, out ulong[] rollForwardVersions);

        /// <summary>
        /// Call this at startup to recover partially written data and
        /// fix errors in the data file using the log file for specific 
        /// transactions
        /// </summary>
        void RollForward(IEnumerable<ulong> versionNumbers);

        /// <summary>
        /// Call this at startup to undo the changes for a given transaction.
        /// Call this method when any fileset contains partially written data
        /// for specific transactions.
        /// </summary>
        void RollBack(IEnumerable<ulong> versionNumbers);

        /// <summary>
        /// Writes a change to a page into the data file and log file in a way that
        /// makes the write recoverable if there is a sudden system failure
        /// </summary>
        /// <param name="transaction">The database version number of the
        /// transaction that made these changes. If the system fails part way through
        /// a write operation either all of the pages with the same version number will be
        /// written to the data file or none of these pages will be written</param>
        /// <param name="updates">The data to write into the file</param>
        /// <returns>True if the write operation succeeds. Fails when the
        /// log file is full or unwritable, or system shutdown is in progress and
        /// changes are being flushed to disk</returns>
        bool Write(ITransaction transaction, IEnumerable<PageUpdate> updates);

        /// <summary>
        /// Tells the file set that there are no more pages to write for a
        /// specific transaction. When all of the pages with this version number
        /// have been written to the log, these changes can start to be applied
        /// to the data file. If writing to the data file is interrupted then
        /// the operation can be repeated on restart because all of the data is in
        /// the log file.
        /// If there is a system failure prior to calling this method then the
        /// data written to the log file is discarded on restart because it is
        /// incomplete.
        /// </summary>
        /// <param name="transaction">The transaction that has finished writing 
        /// changes</param>
        /// <returns>A task that will complete when the changes have been written
        /// to the log file and the transaction can be rolled forward if the system
        /// crashes after this point.</returns>
        Task CommitTransaction(ITransaction transaction);

        /// <summary>
        /// Tells the file set that the transactions has been commited to all file
        /// set logs and it is OK to apply these changes to the data file because the
        /// transaction will roll forward after this time.
        /// </summary>
        /// <param name="transaction">The transaction to finalize</param>
        /// <returns>A task that will complete when the data file is updated</returns>
        Task FinalizeTransaction(ITransaction transaction);

        /// <summary>
        /// Tells the file set to discard all of the writes that were made by
        /// the specified transaction version number.
        /// </summary>
        /// <param name="transaction">The transaction that wants to roll back changes</param>
        void RollBackTransaction(ITransaction transaction);

        /// <summary>
        /// Tries to read data from the data file. Note that this does not read the log
        /// file so any pending updates are not returned by this method. It is assumed that
        /// there is a caching layer above the file set that merges pending writes into read
        /// operations according to the transaction isolation level.
        /// </summary>
        /// <param name="page">The page to read. Must be PageSize in length</param>
        /// <returns>True if the read operation succeeded. If false then the data array
        /// in the page may or may not have been overwritten</returns>
        bool Read(IPage page);
    }
}