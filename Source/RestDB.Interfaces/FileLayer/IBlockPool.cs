using System;
using System.Collections.Generic;
using System.Text;

namespace RestDB.Interfaces.FileLayer
{
    /// <summary>
    /// Provides a mechanism for pooling and reusing data blocks to reduce
    /// pressure on the garbage collector. All blocks in the block pool must 
    /// have the same BlockSize
    /// </summary>
    public interface IBlockPool
    {
        /// <summary>
        /// Gets an available block from the pool and initializs its block number
        /// </summary>
        IBlock Get(Int64 blockNumber);

        /// <summary>
        /// Returns a block to the pool so that it can be reused
        /// </summary>
        void Reuse(IBlock block);
    }
}
