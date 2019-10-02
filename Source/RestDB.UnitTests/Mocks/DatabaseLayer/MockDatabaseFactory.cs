using Moq;
using Moq.Modules;
using RestDB.Interfaces.DatabaseLayer;
using RestDB.Interfaces.FileLayer;
using RestDB.Interfaces.StoredProcedureLayer;
using RestDB.Interfaces.TableLayer;
using System;

namespace RestDB.UnitTests.Mocks.DatabaseLayer
{
    public class MockDatabaseFactory : MockImplementationProvider<IDatabaseFactory>
    {
        protected override void SetupMock(IMockProducer mockProducer, Mock<IDatabaseFactory> mock)
        {
            mock.Setup(o => o.Create(It.IsAny<string>(), It.IsAny<IPageStore>()))
                .Returns<string, IPageStore>((name, pageStore) => new Database(name, pageStore));

            mock.Setup(o => o.Open(It.IsAny<IPageStore>()))
                .Returns<IPageStore>(pageStore => new Database("Database", pageStore));
        }

        private class Database : IDatabase
        {
            private ulong _nextTransactionId;
            private ulong _version;
            private readonly IPageStore _pageStore;

            public Database(string name, IPageStore pageStore)
            {
                Name = name;
                _pageStore = pageStore;
            }

            public string Name { get; set; }

            public IPageStore[] PageStores => new[] { _pageStore };

            public IDataType[] DataTypes => new IDataType[0];

            public ITable[] Tables => new ITable[0];

            public IProcedure[] Procedures => new IProcedure[0];

            public IJob[] Jobs => new IJob[0];

            public ITableDictionary Table => throw new NotImplementedException();

            public ulong CurrentVersion => _version;

            public void AddDataType(IDataType dataType)
            {
            }

            public void AddJob(IJob job)
            {
            }

            public void AddPageStore(IPageStore pageStore)
            {
            }

            public void AddProcedure(IProcedure procedure)
            {
            }

            public void AddTable(ITable table)
            {
            }

            public ITransaction BeginTransaction()
            {
                return new Transaction
                {
                    TransactionId = ++_nextTransactionId,
                    BeginVersionNumber = _version,
                    CommitVersionNumber = _version
                };
            }

            public void Close()
            {
            }

            public void CommitTransaction(ITransaction transaction)
            {
                _version++;
                transaction.CommitVersionNumber = _version;
            }

            public void DeleteDataType(IDataType dataType)
            {
            }

            public void DeleteJob(IJob job)
            {
            }

            public void DeletePageStore(IPageStore pageStore)
            {
            }

            public void DeleteProcedure(IProcedure procedure)
            {
            }

            public void DeleteTable(ITable table)
            {
            }

            public ulong IncrementVersion()
            {
                return ++_version;
            }

            public void RollbackTransaction(ITransaction transaction)
            {
                throw new NotImplementedException();
            }

            private class Transaction : ITransaction
            {
                public ulong TransactionId { get; set; }

                public ulong BeginVersionNumber { get; set; }

                public ulong CommitVersionNumber { get; set; }

                public int CompareTo(ITransaction other)
                {
                    return TransactionId.CompareTo(other.TransactionId);
                }
            }
        }
    }
}
