namespace RestDB.Interfaces.QueryLayer
{
    public interface ILanguage
    {
        void Parse(string query, IQueryBuilder compiler);
    }
}