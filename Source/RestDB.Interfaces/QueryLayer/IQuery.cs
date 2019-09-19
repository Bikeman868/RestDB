namespace RestDB.Interfaces.QueryLayer
{
    public interface IQuery
    {
        void Exceute(IQueryContext queryContext);
    }
}