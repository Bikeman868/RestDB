namespace RestDB.Interfaces.TableLayer
{
    public interface IIndexDefinition
    {
        /// <summary>
        /// A unique identifier for this index
        /// </summary>
        string Id { get; }

        /// <summary>
        /// The name used to refer to this index in query languages
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The table whose rows are indexed
        /// </summary>
        ITable Table { get; }

        /// <summary>
        /// Returns true if this index is continually updated as changes are
        /// made to the table. Returns false iy you have to manually trigger
        /// a rebuilt of the index
        /// </summary>
        bool IsMaintained { get; }

        /// <summary>
        /// The columns to index
        /// </summary>
        IIndexedColumnDefinition[] Columns { get; }
    }
}