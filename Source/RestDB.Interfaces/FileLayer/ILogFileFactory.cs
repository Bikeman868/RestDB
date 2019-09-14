using System;
using System.Collections.Generic;
using System.IO;

namespace RestDB.Interfaces.FileLayer
{
    /// <summary>
    /// Creates object wrappers around log files
    /// </summary>
    public interface ILogFileFactory
    {
        /// <summary>
        /// Opens a physical disk file as a log file. Must have exclusive access to the file.
        /// Creates the file if is does not exist already
        /// </summary>
        /// <param name="file">The file to read/write</param>
        /// <param name="initialize">Pass true to delete and recreate the file 
        /// if it already exists. Pass false to append to the existing file</param>
        /// <returns>True if the file could be opened</returns>
        ILogFile Open(FileInfo file, bool initialize = false);
    }
}
