namespace RestDB.Interfaces.TableLayer
{
    public interface IColumnQuery
    {
        /// <summary>
        /// Returns the column that we are looking for data in
        /// </summary>
        IColumnDefinition Column { get; }

        /// <summary>
        /// The type of comparison to make
        /// </summary>
        CompareOperation Operation { get; }

        /// <summary>
        /// When true the comparison is negated, ie select all 
        /// rows that do not match
        /// </summary>
        bool Negate { get; }

        /// <summary>
        /// The value to look for in this column. This is required
        /// for all operations except testing for null.
        /// </summary>
        object Value { get; }
    }
}