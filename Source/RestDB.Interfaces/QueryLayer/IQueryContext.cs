using RestDB.Interfaces.TableLayer;

namespace RestDB.Interfaces.QueryLayer
{
    public interface IQueryContext
    {
        ITransaction Transaction { get; }
        ITableDictionary Table { get; }
        T FieldValue<T>(string fieldName);
        T FieldValue<T>(string qualifier, string fieldName);
        T Variable<T>(string name);
    }
}
