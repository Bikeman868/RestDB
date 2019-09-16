using System;

namespace RestDB.Interfaces.TableLayer
{
    /// <summary>
    /// Defines a column in a table. This definition works for both row based and
    /// column based tables
    /// </summary>
    public interface IColumnDefinition
    {
        /// <summary>
        /// A unique identifier for this column
        /// </summary>
        string Id { get; }

        /// <summary>
        /// The name used to refer to this column in query languages
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Returns the type of data stored in this column
        /// </summary>
        IDataType Type { get; }

        /// <summary>
        /// The maximum number of elements that can be stoored. Only
        /// applies to string and array columns
        /// </summary>
        ushort MaxLength { get; }

        /// <summary>
        /// Can be used to validate values before updating fields
        /// </summary>
        Func<object, bool> IsValid { get; }

        /// <summary>
        /// Can be used to calculate the value of this column from
        /// the stored columns in the row. If this property is not null then
        /// this column is not stored in the database
        /// </summary>
        Func<IRow, object> Calculate { get; }
    }
}