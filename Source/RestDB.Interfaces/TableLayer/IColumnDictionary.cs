namespace RestDB.Interfaces.TableLayer
{
    public interface IColumnDictionary
    {
        IColumnDefinition this[string columnName] { get; }
    }
}