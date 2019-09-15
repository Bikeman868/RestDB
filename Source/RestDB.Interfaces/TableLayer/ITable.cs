using RestDB.Interfaces.FileLayer;
using System;
using System.Collections.Generic;
using System.Text;

namespace RestDB.Interfaces.TableLayer
{
    /// <summary>
    /// Encapsulates a table of rows and columns in the database.
    /// This interface does not define whether the data is stored in
    /// rows or in columns or some combination of these. Tables must store
    /// all of their data in a single file set so that write operations on
    /// the table can not be partially completed.
    /// </summary>
    public interface ITable
    {
        /// <summary>
        /// The number of rows of data in the able. Each row contains the
        /// same column schema.
        /// </summary>
        ulong RowCount { get; }

        /// <summary>
        /// The number of columns in each row of the table
        /// </summary>
        ulong ColumnCount { get; }

        /// <summary>
        /// Returns the table schema as a collection of column definitions
        /// </summary>
        IColumnDefinition[] GetColumnDefinitions();

        /// <summary>
        /// Adds a new column to the end of the table schema and sets the
        /// default value for this column into every row in the table
        /// </summary>
        void AddColumnDefinition(IColumnDefinition column);

        /// <summary>
        /// Deletes a column from the table schema and also deletes 
        /// the data for this column in every row of the table
        /// </summary>
        void DeleteColumn(IColumnDefinition column);

        /// <summary>
        /// Replaces an existing column definition retaining all of the data.
        /// Where possible the data will be converted, extended or truncated to
        /// fit the new column defiition.
        /// You can use this just to rename the column, in which case the operation
        /// will not touch the data in the rows.
        /// </summary>
        void ReplaceColumn(IColumnDefinition existingColumn, IColumnDefinition newColumn);

        /// <summary>
        /// Adds a new row to the table. Deleted rows will be reused. If there are no
        /// deleted rows then a new row is added to the end of the table
        /// </summary>
        /// <param name="transaction">The transaction context. Defines the isoation
        /// level and ensures that updates are applied all at once</param>
        /// <param name="columnDataFunc">Pass a function or lambda expression that
        /// will return the value to store in a specific column. This
        /// function should return null to insert the default column value
        /// into the new row</param>
        /// <returns>The row number. Note that after rows are deleted their
        /// row number can be reused for new rows that are added to the table</returns>
        IRow AddRow(ITransaction transaction, Func<IColumnDefinition, object> columnDataFunc);

        /// <summary>
        /// Retrieves all column data for a row in the table
        /// </summary>
        IRow GetRow(ITransaction transaction, ulong rowNumber);

        /// <summary>
        /// Updates an existing row in the table
        /// </summary>
        /// <param name="transaction">The transaction context. Defines the isoation
        /// level and ensures that updates are applied all at once</param>
        /// <param name="rowNumber">The number of the row to update</param>
        /// <param name="columnDataFunc">Pass a function or lambda expression that
        /// will return the value to store in a specific column. This func should
        /// return null to keep the existing value in this column.</param>
        void UpdateRow(ITransaction transaction, ulong rowNumber, Func<IColumnDefinition, object> columnDataFunc);

        /// <summary>
        /// Updates a column within an existing row in the table
        /// </summary>
        /// <param name="transaction">The transaction context. Defines the isoation
        /// level and ensures that updates are applied all at once</param>
        /// <param name="rowNumber">The number of the row to update</param>
        /// <param name="column">The column to update within this row</param>
        /// <param name="value">The new value to store in this column for this row</param>
        void UpdateField<T>(ITransaction transaction, ulong rowNumber, IColumnDefinition column, T value);

        /// <summary>
        /// Marks a row as deleted and makes the space available for new rows
        /// </summary>
        void DeleteRow(ITransaction transaction, ulong rowNumber);

        /// <summary>
        /// Deletes all of the data in the table
        /// </summary>
        void Truncate();

        /// <summary>
        /// Returns an enumerator for all of the rows in the table
        /// </summary>
        IEnumerable<IRow> EnumerateRows(ITransaction transaction);

        /// <summary>
        /// Enumerates all of the rows for the specified column. Tables that are stored
        /// in column order will execute this much more efficiently. Row based tables also
        /// provide this facility but it is much less efficient in that case.
        /// </summary>
        /// <typeparam name="T">The type of data to return</typeparam>
        /// <param name="column">Defines the column to return. The column knows how to 
        /// convert it's physical representation into various struct types</param>
        /// <param name="predicate">Optional row filter. Supplying this makes column
        /// based tables very inefficient but does not impact performance of row based tables</param>
        IEnumerable<T> EnumerateColumn<T>(ITransaction transaction, IColumnDefinition column, Func<IRow> predicate = null);
    }
}
