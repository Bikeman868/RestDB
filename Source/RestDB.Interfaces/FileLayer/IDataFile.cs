using System;
using System.Collections.Generic;
using System.IO;

namespace RestDB.Interfaces.FileLayer
{
    /// <summary>
    /// Provides a mechanism for reading and writing to a file
    /// that contains blocks of data that are fixed length
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
        /// <param name="blockSize">The size of data blocks that will be stored in this file</param>
        /// <returns>True if the file was created</returns>
        bool Create(FileInfo file, Int32 blockSize);

        /// <summary>
        /// The size of the data blocks in this file
        /// </summary>
        Int32 BlockSize { get; }

        /// <summary>
        /// Tries to write data into the file
        /// </summary>
        /// <param name="blockNumber">The block number offset into the file</param>
        /// <param name="data">The bytes to write. Must be BlockSize in length</param>
        /// <returns>True if the write operation succeeded</returns>
        bool Write(Int32 blockNumber, byte[] data);

        /// <summary>
        /// Tries to read data from the file
        /// </summary>
        /// <param name="blockNumber">The block number offset into the file</param>
        /// <param name="data">The buffer to read into. Must be BlockSize in length</param>
        /// <returns>True if the read operation succeeded</returns>
        bool Read(Int32 blockNumber, byte[] data);
    }
}
