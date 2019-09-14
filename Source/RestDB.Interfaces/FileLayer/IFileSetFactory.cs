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
        /// Required to initialize the file set. Note that the PageSize of the
        /// data file and the log file must match
        /// </summary>
        /// <param name="dataFile">The file that holds the data records</param>
        /// <param name="logFile">The file that holds the log entries</param>
        IFileSet Create(IDataFile dataFile, ILogFile logFile);
    }
}
