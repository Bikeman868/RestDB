using RestDB.Interfaces.DatabaseLayer;

namespace RestDB.Interfaces
{
    public interface ITransactionFactory
    {
        /// <summary>
        /// Returns a transaction object that can be used to provide
        /// isolation between concurrent operations on the database
        /// </summary>
        ITransaction Create(IDatabase database);
    }
}