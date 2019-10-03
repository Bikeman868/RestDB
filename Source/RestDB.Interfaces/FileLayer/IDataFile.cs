using System;

namespace RestDB.Interfaces.FileLayer
{
    /// <summary>
    /// Provides a mechanism for reading and writing to a file
    /// that contains pages of data that are fixed length
    /// </summary>
    public interface IDataFile : IDisposable
    {
        /// <summary>
        /// The size of the data pages in this file
        /// </summary>
        uint PageSize { get; }

        /// <summary>
        /// Tries to write data into the file. Uses locks to only allow
        /// one thread at a time to write to the file
        /// </summary>
        /// <returns>True if the write operation succeeded</returns>
        bool Write(ulong pageNumber, byte[] data, uint offset = 0);

        /// <summary>
        /// Tries to read data from the file. Uses locks to only allow
        /// one thread at a time to access the file
        /// </summary>
        /// <returns>True if the read operation succeeded. If false is
        /// returned then the contents of the page data is undetermined</returns>
        bool Read(ulong pageNumber, byte[] data, uint offset = 0);
    }
}