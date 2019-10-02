using System.Collections.Generic;

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
        /// <param name="dataFiles">The files that hold the data records</param>
        /// <param name="logFiles">The files that holds the transaction log</param>
        IFileSet Open(IEnumerable<IDataFile> dataFiles, IEnumerable<ILogFile> logFiles);
    }
}