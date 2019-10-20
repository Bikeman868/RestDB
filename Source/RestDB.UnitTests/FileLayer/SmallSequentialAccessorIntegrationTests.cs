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
    public class SmallSequentialAccessorIntegrationTests : TestBase
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

            var pageCache = new PageCache(_fileSet, _database, _pagePoolFactory, _startUpLog, _errorLog);
            _pageStore = new PageStore(pageCache, _startUpLog);

            _accessorFactory = new AccessorFactory();
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
        public void should_allow_multiple_transactions_to_append_records()
        {
            const ushort objectType = 128;
            const int threadCount = 5;

            var threads = new List<Thread>();
            var exceptions = new List<Exception>();

            for (var i = 1; i <= threadCount; i++)
            {
                var transactionNumber = i;
                var thread = new Thread(() =>
                {
                    try
                    {
                        var writeTransaction = _database.BeginTransaction(null);
                        _pageStore.BeginTransaction(writeTransaction);

                        _accessor.Append(objectType, writeTransaction, Encoding.UTF8.GetBytes("Transaction " + transactionNumber));

                        Thread.Sleep(50);

                        _database.CommitTransaction(writeTransaction);
                        _pageStore.CommitTransaction(writeTransaction);
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                            exceptions.Add(ex);
                    }
                })
                {
                    Name = "Transaction " + i,
                    IsBackground = true
                };
                threads.Add(thread);
            }

            foreach (var thread in threads) thread.Start();
            foreach (var thread in threads) thread.Join();

            Assert.AreEqual(0, exceptions.Count);

            var transaction = _database.BeginTransaction(null);
            _pageStore.BeginTransaction(transaction);

            var results = new HashSet<string>();

            Action<PageLocation> check = (location) =>
            {
                using (var page = _pageStore.Get(transaction, location.PageNumber, CacheHints.None))
                {
                    var actual = Encoding.UTF8.GetString(page.Data, (int)location.Offset, (int)location.Length);
                    Assert.IsTrue(results.Add(actual));
                }
            };

            var record = _accessor.LocateFirst(objectType, transaction, out object indexLocation);
            Assert.IsNotNull(record);

            for(var i = 1; i <= threadCount; i++)
            {
                check(record);
                record = _accessor.LocateNext(objectType, transaction, indexLocation);
            }

            for (var i = 1; i <= threadCount; i++)
            {
                Assert.IsTrue(results.Contains("Transaction " + i));
            }

            Assert.IsNull(record);
        }
    }
}
