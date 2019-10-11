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

namespace RestDB.UnitTests.FileLayer
{
    public class PageStoreTests: TestBase
    {
        const uint _pageSize = 64;

        IPagePoolFactory _pagePoolFactory;
        IPagePool _pagePool;
        IStartUpLog _startUpLog;
        IErrorLog _errorLog;
        IDatabase _database;
        IAccessorFactory _accessorFactory;

        FileInfo _dataFileInfo;
        IDataFile _dataFile;
        FileInfo _logFileInfo;
        ILogFile _logFile;

        IFileSet _fileSet;
        IVersionedPageCache _pageCache;
        IPageStore _pageStore;

        [SetUp]
        public void Setup()
        {
            _startUpLog = SetupMock<IStartUpLog>();
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

            _pageCache = new VersionedPageCache(_fileSet, _database, _pagePoolFactory, _startUpLog, _errorLog);

            _accessorFactory = new AccessorFactory();

            _pageStore = new PageStore(_pageCache, _startUpLog);
        }

        [TearDown]
        public void TearDown()
        {
            Console.WriteLine();
            if (_pageStore != null)
                _pageStore.Dispose();
        }

        [Test]
        public void should_persist_variable_length_records()
        {
            var accessor = _accessorFactory.VariableLengthRecordList(_pageStore);

            const ushort objectType = 128;
            var strings = new[]
            {
                "The short brown lazy dog couldn't jump pver anything",
                "One, two, buckle my shoe",
                "YARD (Yet Another Relational Database)"
            };

            var transaction = _database.BeginTransaction();
            _pageCache.BeginTransaction(transaction);

            accessor.Clear(objectType, transaction);
            foreach(var s in strings)
                accessor.Append(objectType, transaction, Encoding.UTF8.GetBytes(s));

            _database.CommitTransaction(transaction);
            _pageCache.CommitTransaction(transaction);
            _pageCache.FinalizeTransaction(transaction);

            transaction = _database.BeginTransaction();
            _pageCache.BeginTransaction(transaction);

            Action<PageLocation, string> check = (location, expected) =>
            {
                using (var page = _pageCache.Get(transaction, location.PageNumber, CacheHints.None))
                {
                    var actual = Encoding.UTF8.GetString(page.Data, (int)location.Offset, (int)location.Length);
                    Assert.AreEqual(expected, actual);
                }
            };

            var record = accessor.LocateFirst(objectType, transaction, out PageLocation indexLocation);
            Assert.IsNotNull(record);

            foreach (var s in strings)
            {
                check(record, s);
                record = accessor.LocateNext(objectType, transaction, indexLocation);
            }

            Assert.IsNull(record);
        }
    }
}
