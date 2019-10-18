using Moq.Modules;
using NUnit.Framework;
using RestDB.FileLayer.Accessors;
using RestDB.FileLayer.DataFiles;
using RestDB.FileLayer.FileSets;
using RestDB.FileLayer.LogFiles;
using RestDB.FileLayer.Pages;
using RestDB.Interfaces;
using RestDB.Interfaces.DatabaseLayer;
using RestDB.Interfaces.FileLayer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace RestDB.UnitTests.FileLayer
{
    public class SmallSequentialAccessorTests : TestBase
    {
        const uint _pageSize = 64;

        IPagePoolFactory _pagePoolFactory;
        IPagePool _pagePool;
        IStartupLog _startUpLog;
        IErrorLog _errorLog;
        IDatabase _database;
        IAccessorFactory _accessorFactory;

        FileInfo _dataFileInfo;
        IDataFile _dataFile;
        FileInfo _logFileInfo;
        ILogFile _logFile;

        IFileSet _fileSet;
        IPageCache _pageCache;
        IPageStore _pageStore;
        ISequentialRecordAccessor _accessor;

        [SetUp]
        public void Setup()
        {
            _startUpLog = SetupMock<IStartupLog>();
            _errorLog = SetupMock<IErrorLog>();
            _pagePoolFactory = SetupMock<IPagePoolFactory>();
            _pagePool = _pagePoolFactory.Create(_pageSize);

            _dataFileInfo = new FileInfo("C:\\temp\\test.mdf");
            _dataFile = new DataFile(_dataFileInfo, _pageSize, _startUpLog);
            _logFileInfo = new FileInfo("C:\\temp\\test.ldf");
            _logFile = new LogFile(_logFileInfo, true, _startUpLog);
            _fileSet = new FileSet(
                new[] { _dataFile }, 
                new[] { _logFile }, 
                _pagePoolFactory, 
                _startUpLog);

            var databaseFactory = SetupMock<IDatabaseFactory>();
            _database = databaseFactory.Open(null);

            _pageCache = new PageCache(_fileSet, _database, _pagePoolFactory, _startUpLog, _errorLog);

            _accessorFactory = new AccessorFactory();

            _pageStore = new PageStore(_pageCache, _startUpLog);
            _accessor = _accessorFactory.SmallSequentialAccessor(_pageStore);
        }

        [TearDown]
        public void TearDown()
        {
            Console.WriteLine();

            if (_pageStore != null)
                _pageStore.Dispose();

            Reset();
        }

        [Test]
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
            _pageCache.BeginTransaction(transaction);

            _accessor.Clear(objectType, transaction);
            foreach(var s in strings)
                _accessor.Append(objectType, transaction, Encoding.UTF8.GetBytes(s));

            _database.CommitTransaction(transaction);
            _pageCache.CommitTransaction(transaction);
            _pageCache.FinalizeTransaction(transaction);

            transaction = _database.BeginTransaction(null);
            _pageCache.BeginTransaction(transaction);

            Action<PageLocation, string> check = (location, expected) =>
            {
                using (var page = _pageCache.Get(transaction, location.PageNumber, CacheHints.None))
                {
                    var actual = Encoding.UTF8.GetString(page.Data, (int)location.Offset, (int)location.Length);
                    Assert.AreEqual(expected, actual);
                }
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
            _pageCache.RollbackTransaction(transaction);
        }

        [Test]
        public void should_delete_small_sequential_records()
        {
        }

        [Test]
        public void should_allow_multiple_transactions_to_append_records()
        {
            const ushort objectType = 128;

            var threads = new List<Thread>();
            var exceptions = new List<Exception>();

            for (var i = 1; i < 4; i++)
            {
                var transactionNumber = i;
                var thread = new Thread(() =>
                {
                    try
                    {
                        var writeTransaction = _database.BeginTransaction(null);
                        _pageCache.BeginTransaction(writeTransaction);

                        Thread.Sleep(transactionNumber);

                        _accessor.Append(objectType, writeTransaction, Encoding.UTF8.GetBytes("Transaction " + transactionNumber));

                        Thread.Sleep(5);

                        _database.CommitTransaction(writeTransaction);
                        _pageCache.CommitTransaction(writeTransaction);
                    }
                    catch(Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                });
                threads.Add(thread);
            }

            foreach (var thread in threads) thread.Start();
            foreach (var thread in threads) thread.Join();

            Assert.AreEqual(0, exceptions.Count);

            var transaction = _database.BeginTransaction(null);
            _pageCache.BeginTransaction(transaction);

            var results = new HashSet<string>();

            Action<PageLocation> check = (location) =>
            {
                using (var page = _pageCache.Get(transaction, location.PageNumber, CacheHints.None))
                {
                    var actual = Encoding.UTF8.GetString(page.Data, (int)location.Offset, (int)location.Length);
                    Assert.IsTrue(results.Add(actual));
                }
            };

            var record = _accessor.LocateFirst(objectType, transaction, out object indexLocation);
            Assert.IsNotNull(record);

            for(var i = 1; i < 4; i++)
            {
                check(record);
                record = _accessor.LocateNext(objectType, transaction, indexLocation);
            }

            for (var i = 1; i < 4; i++)
            {
                Assert.IsTrue(results.Contains("Transaction " + i));
            }

            Assert.IsNull(record);
        }
    }
}
