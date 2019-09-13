using System;
using System.Collections.Generic;
using System.Text;

namespace RestDB.Interfaces.FileLayer
{
    /// <summary>
    /// Provides a caching mechanism for blocks of data from a single file. 
    /// When blocks are modified all prior versions that are referenced are 
    /// kept so that transactions can have a static view of the data
    /// </summary>
    public interface IVersionedBlockCache
    {
        /// <summary>
        /// Initializes the cache with a backing store. This backing store
        /// will be read when there is a cache miss. Block updates are versioned
        /// to support transactions
        /// </summary>
        void Init(IFileSet fileSet);

        /// <summary>
        /// Increments the reference count for this version number keeping
        /// all modified blocks at this version or below in memory until the
        /// transaction completes. Calls the BeginTransaction must be in a try/finally
        /// construct that guarantees to call EndTransaction once for each
        /// call to BeginTransaction
        /// </summary>
        void BeginTransaction(Int64 versionNumber);

        /// <summary>
        /// Decrements the reference count for this version number. When the
        /// reference count goes to zero for a specific version and all versions 
        /// below it, then all modified blocks with this version or lower can be 
        /// discarded because no transactions can reference them.
        /// </summary>
        void EndTransaction(Int64 versionNumber);

        /// <summary>
        /// Retrieves a block from cache or backing store based on a specific
        /// version number. The highest version number of the block will be 
        /// retuned that is less than or equal to the version number supplied
        /// </summary>
        /// <param name="blockNumber">The block number to return</param>
        /// <param name="versionNumber">Only modified blocks with this version number or
        /// less should be returned. If the block is not modified then the current
        /// version is returned</param>
        IBlock Get(Int32 blockNumber, Int64 versionNumber);

        /// <summary>
        /// Updates the cache with a replacement block at a specific version number
        /// </summary>
        /// <param name="block">The new block data with modifications</param>
        /// <param name="versionNumber">The version number of this change</param>
        /// <param name="modifiedRanges">A list of the stat+length of bytes that were modified
        /// within the block. Changes will be merged in version number order before being
        /// written back to the file system.</param>
        void Put(IBlock block, Int64 versionNumber, IEnumerable<Tuple<Int32, Int32>> modifiedRanges);
    }
}
