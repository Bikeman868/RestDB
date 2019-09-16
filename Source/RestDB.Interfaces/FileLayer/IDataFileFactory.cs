using System.IO;

namespace RestDB.Interfaces.FileLayer
{
    /// <summary>
    /// Creates a new data file object to provide access to a data file
    /// </summary>
    public interface IDataFileFactory
    {
        /// <summary>
        /// Opens a physical disk file as a data file. Must have exclusive access to the file
        /// </summary>
        /// <param name="file">The file to read/write</param>
        /// <returns>True if the file was opened with read/write access</returns>
        IDataFile Open(FileInfo file);

        /// <summary>
        /// Creates a new file and initializes it ready for storing data. The file must not already exist
        /// </summary>
        /// <param name="file">The file to create</param>
        /// <param name="pageSize">The size of data pages that will be stored in this file</param>
        /// <returns>True if the file was created</returns>
        IDataFile Create(FileInfo file, uint pageSize);
    }
}