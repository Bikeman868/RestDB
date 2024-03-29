﻿namespace RestDB.Interfaces.FileLayer
{
    /// <summary>
    /// Creates pools of reusable pages  that have the same PageSize
    /// </summary>
    public interface IPagePoolFactory
    {
        /// <summary>
        /// Creates a new page pool or returns an existing pool with the
        /// appropriate PageSize
        /// </summary>
        /// <param name="pageSize">The size of pages to pool and reuse</param>
        IPagePool Create(uint pageSize);
    }
}