using System;
using System.Collections.Generic;
using System.Text;

namespace RestDB.Interfaces.FileLayer
{
    /// <summary>
    /// Encapsulates a pair of files that together provide recoverable write operations.
    /// At startup the Recover() method can be called to fix any issues caused by a 
    /// sudden failure that resulted in data only being partially written. Guarantees that
    /// all pages are fully written to disk or not written to disk at all.
    /// </summary>
    public interface IFileSet
    {
        /// <summary>
        /// Required to initialize the file set. Note that the PageSize of the
        /// data file and the log file must match
        /// </summary>
        /// <param name="dataFile">The file that holds the data records</param>
        /// <param name="logFile">The file that holds the log entries</param>
        void Init(IDataFile dataFile, ILogFile logFile);

        /// <summary>
        /// Returns the page size of data in this file set
        /// </summary>
        int PageSize { get; }

        /// <summary>
        /// Call this at startup to recover partially written data and
        /// fix errors in the data file using the log file
        /// </summary>
        void Recover();

        /// <summary>
        /// Writes a page into the data file and log file in a way that
        /// makes the write recoverable if there is a sudden system failure
        /// </summary>
        /// <param name="page">The data to write into the file</param>
        /// <param name="versionNumber">The database version number of the
        /// transaction that made these changes. If the system fails part way through
        /// a write operation either all of the pages with the same version number will be
        /// written to the data file or none of these pages will be written</param>
        /// <returns>True if the write operation succeeds. Fails only when the
        /// log file is full or unwritable</returns>
        bool Write(IPage page, long versionNumber);

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
        /// <param name="versionNumber">The version number of the transaction that
        /// has finished writing changes</param>
        void EndTransaction(long versionNumber);

        /// <summary>
        /// Tries to read data from the data file
        /// </summary>
        /// <param name="pageNumber">The page number offset into the file</param>
        /// <param name="data">The buffer to read into. Must be PageSize in length</param>
        /// <returns>True if the read operation succeeded. If false then the data array
        /// may or may not have been overwritten</returns>
        bool Read(int pageNumber, byte[] data);
    }
}
