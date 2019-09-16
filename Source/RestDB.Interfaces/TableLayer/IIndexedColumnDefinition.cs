namespace RestDB.Interfaces.TableLayer
{
    public interface IIndexedColumnDefinition
    {
        /// <summary>
        /// The column that is indexed
        /// </summary>
        IColumnDefinition Column { get; }

        /// <summary>
        /// The set of comparison operations that are supported on this column
        /// </summary>
        CompareOperation SupportedComparisons { get; }

        /// <summary>
        /// If true then this column is constrained to have unique values
        /// </summary>
        bool UniqueValues { get; }
    }
}