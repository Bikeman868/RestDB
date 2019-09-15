using System;
using System.Collections.Generic;
using System.Text;

namespace RestDB.Interfaces.TableLayer
{
    public interface IColumnQueryFactory
    {
        /// <summary>
        /// Creates a new column query that can be used to find matching
        /// records in tables and indexes
        /// </summary>
        /// <param name="column">The column to compare</param>
        /// <param name="operation">The comparison operation to perform</param>
        /// <param name="value">The value to compare with</param>
        /// <param name="negate">Pass true to find rows that do not match</param>
        IColumnQuery Create(
            IColumnDefinition column,
            CompareOperation operation,
            object value,
            bool negate = false);
    }
}
