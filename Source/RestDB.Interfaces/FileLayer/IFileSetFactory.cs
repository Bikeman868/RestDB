using System;
using System.Collections.Generic;
using System.Text;

namespace RestDB.Interfaces.FileLayer
{
    /// <summary>
    /// Constructs objects that wraps a pair of files that together provide recoverable write operations.
    /// </summary>
    public interface IFileSetFactory
    {
        /// <summary>
        /// Opens a pair of files that contain the data and log information
        /// for a page store
        /// </summary>
        /// <param name="dataFile">The file that holds the data records</param>
        /// <param name="logFile">The file that holds the transaction log</param>
        IFileSet Open(IDataFile dataFile, ILogFile logFile);
    }
}
