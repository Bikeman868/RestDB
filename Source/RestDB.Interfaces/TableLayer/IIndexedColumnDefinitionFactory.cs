using System;
using System.Collections.Generic;
using System.Text;

namespace RestDB.Interfaces.TableLayer
{
    public interface IIndexedColumnDefinitionFactory
    {
        /// <summary>
        /// Creates a new index column definition
        /// </summary>
        /// <param name="column">The column to index</param>
        /// <param name="requiredComparisons">The comparisons that must be supported</param>
        /// <param name="enforceUnique">Pass true to make the index force unique values in this column</param>
        IIndexedColumnDefinition Create(
            IColumnDefinition column,
            CompareOperation requiredComparisons,
            bool enforceUnique);
    }
}
