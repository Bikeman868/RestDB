﻿using Moq.Modules;
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

            Reset();
        }

        [Test]
        public void should_update_pages()
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

            using (var page = _pageCache.Get(transaction, 1))
            {
                Assert.AreEqual(1, page.Data[10]);
                Assert.AreEqual(2, page.Data[11]);
                Assert.AreEqual(3, page.Data[12]);
            }

            _database.CommitTransaction(transaction);
            _pageCache.CommitTransaction(transaction);
            _pageCache.FinalizeTransaction(transaction);
        }

        [Test]
        public void should_isolate_transactions()
        {
            var transaction1 = _database.BeginTransaction();
            var transaction2 = _database.BeginTransaction();
            var transaction3 = _database.BeginTransaction();

            _pageCache.BeginTransaction(transaction1);
            _pageCache.BeginTransaction(transaction2);

            _pageCache.Update(
                transaction2, new[]
                {
                    new PageUpdate
                    {
                        SequenceNumber = 1,
                        PageNumber = 1,
                        Offset = 10,
                        Data = new byte[]{ 1, 2, 3 }
                    }
                });

            _pageCache.BeginTransaction(transaction3);

            using (var page = _pageCache.Get(transaction1, 1))
            {
                Assert.AreEqual(0, page.Data[10]);
                Assert.AreEqual(0, page.Data[11]);
                Assert.AreEqual(0, page.Data[12]);
            }

            using (var page = _pageCache.Get(transaction2, 1))
            {
                Assert.AreEqual(1, page.Data[10]);
                Assert.AreEqual(2, page.Data[11]);
                Assert.AreEqual(3, page.Data[12]);
            }

            using (var page = _pageCache.Get(transaction3, 1))
            { 
                Assert.AreEqual(0, page.Data[10]);
                Assert.AreEqual(0, page.Data[11]);
                Assert.AreEqual(0, page.Data[12]);
            }

            _pageCache.RollbackTransaction(transaction1);
            _pageCache.RollbackTransaction(transaction3);

            _database.CommitTransaction(transaction2);
            _pageCache.CommitTransaction(transaction2);
            _pageCache.FinalizeTransaction(transaction2);

            var transaction4 = _database.BeginTransaction();

            using (var page = _pageCache.Get(transaction4, 1))
            {
                Assert.AreEqual(1, page.Data[10]);
                Assert.AreEqual(2, page.Data[11]);
                Assert.AreEqual(3, page.Data[12]);
            }

            _pageCache.RollbackTransaction(transaction4);
        }
    }
}