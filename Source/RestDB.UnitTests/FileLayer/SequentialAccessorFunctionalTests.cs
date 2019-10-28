using Moq.Modules;
using NUnit.Framework;
using RestDB.FileLayer.Accessors;
using RestDB.Interfaces;
using RestDB.Interfaces.DatabaseLayer;
using RestDB.Interfaces.FileLayer;
using System;
using System.Linq;
using System.Text;

namespace RestDB.UnitTests.FileLayer
{
    public class SequentialAccessorFunctionalTests : TestBase
    {
        protected IDatabase _database;
        protected IAccessorFactory _accessorFactory;

        protected IPageStore _pageStore;
        protected ISequentialRecordAccessor _accessor;

        public void Setup()
        {
            var databaseFactory = SetupMock<IDatabaseFactory>();
            _database = databaseFactory.Open(null);

            _pageStore = SetupMock<IPageStore>();

            _accessorFactory = new AccessorFactory();
        }

        [TearDown]
        public void TearDown()
        {
            Console.WriteLine();

            if (_pageStore != null)
                _pageStore.Dispose();

            Reset();
        }

        public void should_read_and_write_records()
        {
            const ushort objectType = 128;

            var strings = new[]
            {
                "The short brown lazy dog couldn't jump over anything",
                "One, two, buckle my shoe",
                "YARD (Yet Another Relational Database)",
                "This is a test, one, two, three, testing",
                "Blah blah blah",
                "This should be on a second index page",
                "OK, I think that's enough now"
            };

            var transaction = _database.BeginTransaction(null);
            _pageStore.BeginTransaction(transaction);

            _accessor.Clear(objectType, transaction);
            foreach (var s in strings)
            {
                var buffer = Encoding.UTF8.GetBytes(s);
                var recordLocation = _accessor.Append(objectType, transaction, (uint)buffer.LongLength);
                _accessor.Write(transaction, recordLocation, buffer);
            }

            _database.CommitTransaction(transaction);
            _pageStore.CommitTransaction(transaction);
            _pageStore.FinalizeTransaction(transaction);

            transaction = _database.BeginTransaction(null);
            _pageStore.BeginTransaction(transaction);

            Action<PageLocation, string> check = (location, expected) =>
            {
                Assert.IsNotNull(location);

                var data = location.ReadAll(transaction, CacheHints.None);
                var actual = Encoding.UTF8.GetString(data);

                Assert.AreEqual(expected, actual);
            };

            var record = _accessor.LocateFirst(objectType, transaction, out object indexLocation);
            Assert.IsNotNull(record);

            foreach (var s in strings)
            {
                check(record, s);
                record = _accessor.LocateNext(objectType, transaction, indexLocation);
            }

            Assert.IsNull(record);

            _database.RollbackTransaction(transaction);
            _pageStore.RollbackTransaction(transaction);
        }

        public void should_enumerate_records()
        {
            const ushort objectType = 128;

            var strings = new[]
            {
                "The short brown lazy dog couldn't jump over anything",
                "One, two, buckle my shoe",
                "YARD (Yet Another Relational Database)",
                "This is a test, one, two, three, testing",
                "Blah blah blah",
                "This should be on a second index page",
                "OK, I think that's enough now"
            };

            var transaction = _database.BeginTransaction(null);
            _pageStore.BeginTransaction(transaction);

            var expectedCount = 0;
            foreach (var s in strings)
            {
                Assert.AreEqual(expectedCount++, _accessor.Enumerate(objectType, transaction).Count());
                var buffer = Encoding.UTF8.GetBytes(s);
                _accessor.Write(transaction, _accessor.Append(objectType, transaction, (uint)buffer.LongLength), buffer);
            }

            _database.RollbackTransaction(transaction);
            _pageStore.RollbackTransaction(transaction);
        }

        public void should_delete_records()
        {
            const ushort objectType = 128;

            var strings = new[]
            {
                "The short brown lazy dog couldn't jump over anything",
                "One, two, buckle my shoe",
                "YARD (Yet Another Relational Database)",
                "This is a test, one, two, three, testing",
                "Blah blah blah",
                "This should be on a second index page",
                "OK, I think that's enough now"
            };

            var transaction = _database.BeginTransaction(null);
            _pageStore.BeginTransaction(transaction);

            foreach (var s in strings)
            {
                var buffer = Encoding.UTF8.GetBytes(s);
                _accessor.Write(transaction, _accessor.Append(objectType, transaction, (uint)buffer.LongLength), buffer);
            }

            _accessor.LocateFirst(objectType, transaction, out object location);
            _accessor.Delete(objectType, transaction, location);

            Assert.AreEqual(strings.Length - 1, _accessor.Enumerate(objectType, transaction).Count());

            _accessor.LocateFirst(objectType, transaction, out location);
            _accessor.LocateNext(objectType, transaction, location);
            _accessor.Delete(objectType, transaction, location);

            Assert.AreEqual(strings.Length - 2, _accessor.Enumerate(objectType, transaction).Count());

            var index = 1;
            foreach(var record in _accessor.Enumerate(objectType, transaction))
            {
                var data = record.ReadAll(transaction, CacheHints.None);
                var recordData = Encoding.UTF8.GetString(data);

                Assert.AreEqual(strings[index++], recordData);

                if (index == 2) index++;
            }

            _database.CommitTransaction(transaction);
            _pageStore.CommitTransaction(transaction);
            _pageStore.FinalizeTransaction(transaction);
        }
    }
}
