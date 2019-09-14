﻿using System;
using System.Collections.Generic;
using System.Text;

namespace RestDB.Interfaces.FileLayer
{
    /// <summary>
    /// Wraps a file set, adding caching and merging of pending writes
    /// into read operations within transaction contexts
    /// </summary>
    public interface IVersionedPageCacheFactory
    {
        /// <summary>
        /// Creates a cache on top of a file set
        /// </summary>
        IVersionedPageCache Create(IFileSet fileSet);
    }
}