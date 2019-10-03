using Moq.Modules;
using NUnit.Framework;
using RestDB.FileLayer.DataFiles;
using RestDB.FileLayer.FileSets;
using RestDB.FileLayer.LogFiles;
using RestDB.FileLayer.Pages;
using RestDB.Interfaces;
using RestDB.Interfaces.DatabaseLayer;
using RestDB.Interfaces.FileLayer;
using System;
using System.IO;

namespace RestDB.UnitTests.FileLayer
{
    public class VersionedPageCacheTests: TestBase
    {
        const uint _pageSize = 64;

        IPagePoolFactory _pagePoolFactory;
        IPagePool _pagePool;
        IStartUpLog _startUpLog;
        IDatabase _database;

        FileInfo _logFileInfo1;
        FileInfo _logFileInfo2;

        FileInfo _dataFileInfo1;
        FileInfo _dataFileInfo2;

        IFileSet _fileSet;
        IVersionedPageCache _pageCache;

        [SetUp]
        public void Setup()
        {
            _startUpLog = SetupMock<IStartUpLog>();
            _pagePoolFactory = SetupMock<IPagePoolFactory>();
            _pagePool = _pagePoolFactory.Create(_pageSize);

            _logFileInfo1 = new FileInfo("C:\\temp\\test1.ldf");
            _logFileInfo2 = new FileInfo("C:\\temp\\test2.ldf");

            _dataFileInfo1 = new FileInfo("C:\\temp\\test1.mdf");
            _dataFileInfo2 = new FileInfo("C:\\temp\\test2.mdf");

            _fileSet = new FileSet(
                new IDataFile[] { new DataFile(_dataFileInfo1, _pageSize, _startUpLog), new DataFile(_dataFileInfo2, _pageSize, _startUpLog) },
                new ILogFile[] { new LogFile(_logFileInfo1, true, _startUpLog), new LogFile(_logFileInfo2, true, _startUpLog) },
                _pagePoolFactory,
                _startUpLog);

            _pageCache = new VersionedPageCache(_fileSet, _pagePoolFactory, _startUpLog);

            var pageStoreFactory = SetupMock<IPageStoreFactory>();
            var fileSetFactory = SetupMock<IFileSetFactory>();
            var pageStore = pageStoreFactory.Open(fileSetFactory.Open(null, null));
            var databaseFactory = SetupMock<IDatabaseFactory>();
            _database = databaseFactory.Open(pageStore);
        }

        [TearDown]
        public void TearDown()
        {
            Console.WriteLine();

            if (_pageCache != null)
                _pageCache.Dispose();
        }

        [Test]
        public void should_updated_pages()
        {
            var transaction = _database.BeginTransaction();

            _pageCache.BeginTransaction(transaction);

            _pageCache.Update(
                transaction, new[] 
                {
                    new PageUpdate
                    {
                        SequenceNumber = 1,
                        PageNumber = 1,
                        Offset = 10,
                        Data = new byte[]{ 1, 2, 3 }
                    }
                });

            var page = _pageCache.Get(transaction, 1);
            Assert.AreEqual(1, page.Data[10]);
            Assert.AreEqual(2, page.Data[11]);
            Assert.AreEqual(3, page.Data[12]);

            _pageCache.CommitTransaction(transaction);
        }
    }
}
