using System;
using System.Collections.Generic;
using System.Text;

namespace RestDB.Interfaces.TableLayer
{
    /// <summary>
    /// Defines methods that are common to tables and indexes
    /// </summary>
    public interface ISearchable
    {
        /// <summary>
        /// Returns an enumerator for all rows with matching column values
        /// </summary>
        /// <param name="transaction">The transaction context to use for row data</param>
        /// <param name="columns">The values to look for in table columns. If this
        /// array is null or empty then all rows in the table are returned</param>
        IEnumerable<IRow> MatchingRows(ITransaction transaction, IColumnQuery[] columns);

        /// <summary>
        /// Enumerates values in the specified column. Tables that are stored
        /// in column order will execute this much more efficiently. Row based tables also
        /// provide this facility but it is much less efficient. For indexes
        /// the column must be one of the columns that are indexed.
        /// </summary>
        /// <typeparam name="T">The type of data to return</typeparam>
        /// <param name="transaction">The transaction context of this request</param>
        /// <param name="columnQuery">Defines the column to search in and the 
        /// values to look for. The column definition knows how to convert it's 
        /// physical representation into various struct, array and string types</param>
        /// <param name="rowPredicate">Optional row filter. Supplying this makes column
        /// based tables and indexes very inefficient but does not impact performance of row based tables</param>
        IEnumerable<T> MatchingFields<T>(
            ITransaction transaction,
            IColumnQuery columnQuery,
            Func<IRow> rowPredicate = null);
    }
}
