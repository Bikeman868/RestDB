namespace RestDB.Interfaces.TableLayer
{
    public interface IIndex : ISearchable
    {
        /// <summary>
        /// Defines the columns that are indexed and other index characteristics
        /// </summary>
        IIndexDefinition Definition { get; }

        /// <summary>
        /// For non-maintained indexes, updates the index to reflect the changes
        /// in the table data. For maintained indexes this does nothing.
        /// </summary>
        void Update();
    }
}