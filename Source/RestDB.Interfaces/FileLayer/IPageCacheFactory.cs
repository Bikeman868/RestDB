using RestDB.Interfaces.DatabaseLayer;

namespace RestDB.Interfaces.FileLayer
{
    /// <summary>
    /// Wraps a file set, adding caching and merging of pending writes
    /// into read operations within transaction contexts
    /// </summary>
    public interface IPageCacheFactory
    {
        /// <summary>
        /// Creates a cache on top of a file set
        /// </summary>
        IPageCache Create(IDatabase database, IFileSet fileSet);
    }
}