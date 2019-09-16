namespace RestDB.Interfaces.FileLayer
{
    /// <summary>
    /// Constructs page stores
    /// </summary>
    public interface IPageStoreFactory
    {
        /// <summary>
        /// Wraps a pair of data/log files in an object that tracks used/free
        /// pages and provides a mechanism for finding indexes to the various
        /// types of data stored in the file set
        /// </summary>
        IPageStore Open(IFileSet fileSet);
    }
}