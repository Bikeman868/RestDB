using System;

namespace RestDB.Interfaces.TableLayer
{
    /// <summary>
    /// Creates column definitions
    /// </summary>
    public interface IColumnDefinitionFactory
    {
        /// <summary>
        /// Constructs a new column definition
        /// </summary>
        /// <param name="name">The name of the column</param>
        /// <param name="dataType">The type of data in this column</param>
        /// <param name="maxLength">The maximum number of elements to allow 
        /// in this column</param>
        /// <param name="defaultValue">The default value for this column. 
        /// Pass null to use the default value defined by the data type</param>
        /// <param name="validationFunc">Optional validation function. Pass
        /// null for no validation. If the validation function returns
        /// false then the insert/update fails and no changes are made to the 
        /// record</param>
        /// <param name="calcFunc">Optional calculation function. When this
        /// is provided the column is not stored in the database, but calculated
        /// from the other columns in the table</param>
        IColumnDefinition Create(
            string name,
            IDataType dataType,
            ushort maxLength = 1,
            object defaultValue = null,
            Func<object, bool> validationFunc = null,
            Func<IRow, object> calcFunc = null);
    }
}