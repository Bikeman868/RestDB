using System;
using System.Collections.Generic;
using System.Text;

namespace RestDB.Interfaces.TableLayer
{
    /// <summary>
    /// Represents a row of data in a table
    /// </summary>
    public interface IRow
    {
        /// <summary>
        /// The unique row number within this table
        /// </summary>
        ulong RowNumber { get; }

        /// <summary>
        /// The column definitions that were current at the time that
        /// this row object was constructed. Maybe different than the 
        /// current column definitions for the table. Tables can have
        /// different column structure for each row in the table
        /// </summary>
        IColumnDefinition[] Columns { get; }

        /// <summary>
        /// The values of the fields in this row. The indexes of this
        /// array always corrspond to the Columns array and these two arrays
        /// are always the same lenth. The objects in this array are structs
        /// or strings with the same type as defined by the data type of the
        /// column definition.
        /// </summary>
        Object[] Fields { get; }
    }
}
