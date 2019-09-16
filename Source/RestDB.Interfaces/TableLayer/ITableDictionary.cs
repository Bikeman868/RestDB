namespace RestDB.Interfaces.TableLayer
{
    public interface ITableDictionary
    {
        ITable this[string indexName] { get; }
    }
}