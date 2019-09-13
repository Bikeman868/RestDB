using System;

namespace RestDB.Interfaces.FileLayer
{
    /// <summary>
    /// Provides resilliant versioned persistent storage of blocks of
    /// data where each block is the same size. The blocks will be stored
    /// using a pair of files to ensure that partial writes can be recovered
    /// on restart after a system failure
    /// </summary>
    public interface IBlockStore
    {
        /// <summary>
        /// Initializes the block store with a backing file
        /// </summary>
        void Init(IFileSet fileSet);

        /// <summary>
        /// Creates a new block in the block store with a unique 
        /// block number
        /// </summary>
        IBlock Allocate();

        /// <summary>
        /// Marks a block as availble for reuse. This block
        /// could be returned immediately on the next call to 
        /// Allocate()
        /// </summary>
        void Release(Int32 blockNumber);

        /// <summary>
        /// Provides access to the blocks in this block store
        /// </summary>
        IVersionedBlockCache Blocks { get; }
    }
}
