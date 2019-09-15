using System;
using System.Collections.Generic;
using System.Text;

namespace RestDB.Interfaces.TableLayer
{
    public interface IIndexDefinitionFactory
    {
        /// <summary>
        /// Creates a new index definition
        /// </summary>
        /// <param name="name">The name of the index</param>
        /// <param name="table">The table to index</param>
        /// <param name="maintained">Pass true to keep the index updated as 
        /// chanegs are made to the data</param>
        /// <param name="columns">The columns to index</param>
        IIndexDefinition Create(
            string name,
            ITable table,
            bool maintained,
            IIndexedColumnDefinition[] columns);
    }
}
