using System;
using System.Collections.Generic;
using System.IO;

namespace RestDB.Interfaces.FileLayer
{
    /// <summary>
    /// Provides a mechanism for reading and writing to a file
    /// that contains pages of data that are fixed length
    /// </summary>
    public interface IDataFile
    {
        /// <summary>
        /// Opens a physical disk file as a data file. Must have exclusive access to the file
        /// </summary>
        /// <param name="file">The file to read/write</param>
        /// <returns>True if the file was opened with read/write access</returns>
        bool Open(FileInfo file);

        /// <summary>
        /// Creates a new file and initializes it ready for storing data
        /// </summary>
        /// <param name="file">The file to create</param>
        /// <param name="pageSize">The size of data pages that will be stored in this file</param>
        /// <returns>True if the file was created</returns>
        bool Create(FileInfo file, int pageSize);

        /// <summary>
        /// The size of the data pages in this file
        /// </summary>
        int PageSize { get; }

        /// <summary>
        /// Tries to write data into the file
        /// </summary>
        /// <returns>True if the write operation succeeded</returns>
        bool Write(IPage page);

        /// <summary>
        /// Tries to read data from the file
        /// </summary>
        /// <returns>True if the read operation succeeded. If false is
        /// returned then the contents of the page data is undetermined</returns>
        bool Read(IPage page);
    }
}
