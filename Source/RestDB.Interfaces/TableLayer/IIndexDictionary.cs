namespace RestDB.Interfaces.TableLayer
{
    public interface IIndexDictionary
    {
        IIndex this[string indexName] { get; }
    }
}