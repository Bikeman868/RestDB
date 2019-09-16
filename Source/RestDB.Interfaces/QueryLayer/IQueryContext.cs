using RestDB.Interfaces.TableLayer;

namespace RestDB.Interfaces.QueryLayer
{
    public interface IQueryContext
    {
        ITransaction Transaction { get; }
        object FieldValue(string fieldName);
        object FieldValue(string qualifier, string fieldName);
        ITableDictionary Table { get; }
    }
}
