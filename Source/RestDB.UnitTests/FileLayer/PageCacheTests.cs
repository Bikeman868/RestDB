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
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace RestDB.UnitTests.FileLayer
{
    public class PageCacheTests: TestBase
    {
        const uint _pageSize = 64;

        IPagePoolFactory _pagePoolFactory;
        IPagePool _pagePool;
        IStartupLog _startUpLog;
        IErrorLog _errorLog;
        IDatabase _database;

        FileInfo _logFileInfo1;
        FileInfo _logFileInfo2;

        FileInfo _dataFileInfo1;
        FileInfo _dataFileInfo2;

        IFileSet _fileSet;
        IPageCache _pageCache;

        [SetUp]
        public void Setup()
        {
            _startUpLog = SetupMock<IStartupLog>();
            _errorLog = SetupMock<IErrorLog>();
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

            var databaseFactory = SetupMock<IDatabaseFactory>();
            _database = databaseFactory.Open(null);

            _pageCache = new PageCache(_fileSet, _database, _pagePoolFactory, _startUpLog, _errorLog);
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
            var transaction = _database.BeginTransaction(null);

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

            using (var page = _pageCache.Get(transaction, 1, CacheHints.None))
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
            var transaction1 = _database.BeginTransaction(null);
            var transaction2 = _database.BeginTransaction(null);
            var transaction3 = _database.BeginTransaction(null);

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

            using (var page = _pageCache.Get(transaction1, 1, CacheHints.None))
            {
                Assert.AreEqual(0, page.Data[10]);
                Assert.AreEqual(0, page.Data[11]);
                Assert.AreEqual(0, page.Data[12]);
            }

            using (var page = _pageCache.Get(transaction2, 1, CacheHints.None))
            {
                Assert.AreEqual(1, page.Data[10]);
                Assert.AreEqual(2, page.Data[11]);
                Assert.AreEqual(3, page.Data[12]);
            }

            using (var page = _pageCache.Get(transaction3, 1, CacheHints.None))
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

            var transaction4 = _database.BeginTransaction(null);
            _pageCache.BeginTransaction(transaction4);

            using (var page = _pageCache.Get(transaction4, 1, CacheHints.None))
            {
                Assert.AreEqual(1, page.Data[10]);
                Assert.AreEqual(2, page.Data[11]);
                Assert.AreEqual(3, page.Data[12]);
            }

            _pageCache.RollbackTransaction(transaction4);
        }

        [Test]
        public void should_snapshot_at_start_of_transaction()
        {
            const ulong pageNumber = 5;

            // Modify some pages within a transaction

            var transaction1 = _database.BeginTransaction(null);
            _pageCache.BeginTransaction(transaction1);
            _pageCache.Update(
                transaction1, new[]
                {
                    new PageUpdate
                    {
                        SequenceNumber = 1,
                        PageNumber = pageNumber,
                        Offset = 3,
                        Data = new byte[]{ 98, 99, 100 }
                    },
                    new PageUpdate
                    {
                        SequenceNumber = 2,
                        PageNumber = pageNumber + 1,
                        Offset = 10,
                        Data = new byte[]{ 98, 99, 100 }
                    },

                });
            _database.CommitTransaction(transaction1);
            _pageCache.CommitTransaction(transaction1);
            _pageCache.FinalizeTransaction(transaction1);

            // Start a transaction and read one of the modified pages

            var transaction2 = _database.BeginTransaction(null);
            _pageCache.BeginTransaction(transaction2);
            using (var page = _pageCache.Get(transaction2, pageNumber, CacheHints.None))
            {
                Assert.AreEqual(98, page.Data[3]);
                Assert.AreEqual(99, page.Data[4]);
                Assert.AreEqual(100, page.Data[5]);
            }

            // Make some more changes to these pages

            for (var i = 0; i < 5; i++)
            {
                var transaction = _database.BeginTransaction(null);
                _pageCache.BeginTransaction(transaction);
                _pageCache.Update(
                    transaction, new[]
                    {
                        new PageUpdate
                        {
                            SequenceNumber = 1,
                            PageNumber = pageNumber,
                            Offset = 3,
                            Data = new byte[]{ (byte)i, (byte)(i+1), (byte)(i+2) }
                        },
                        new PageUpdate
                        {
                            SequenceNumber = 2,
                            PageNumber = pageNumber + 1,
                            Offset = 10,
                            Data = new byte[]{ (byte)i, (byte)(i+1), (byte)(i+2) }
                        }
                    });
                _database.CommitTransaction(transaction);
                _pageCache.CommitTransaction(transaction);
                _pageCache.FinalizeTransaction(transaction);
            }

            // Verify that the open transaction has read consistency

            using (var page = _pageCache.Get(transaction2, pageNumber, CacheHints.None))
            {
                Assert.AreEqual(98, page.Data[3]);
                Assert.AreEqual(99, page.Data[4]);
                Assert.AreEqual(100, page.Data[5]);
            }

            // Verrify that the open transaction can not see the updates

            using (var page = _pageCache.Get(transaction2, pageNumber + 1, CacheHints.None))
            {
                Assert.AreEqual(98, page.Data[10]);
                Assert.AreEqual(99, page.Data[11]);
                Assert.AreEqual(100, page.Data[12]);
            }
            _pageCache.RollbackTransaction(transaction2);

            // Verify that a new transaction does see the updated values

            var transaction3 = _database.BeginTransaction(null);
            _pageCache.BeginTransaction(transaction3);

            using (var page = _pageCache.Get(transaction3, pageNumber, CacheHints.None))
            {
                Assert.AreEqual(4, page.Data[3]);
                Assert.AreEqual(5, page.Data[4]);
                Assert.AreEqual(6, page.Data[5]);
            }
            using (var page = _pageCache.Get(transaction3, pageNumber + 1, CacheHints.None))
            {
                Assert.AreEqual(4, page.Data[10]);
                Assert.AreEqual(5, page.Data[11]);
                Assert.AreEqual(6, page.Data[12]);
            }
        }

        [Test]
        public void should_not_cleanup_referenced_versions()
        {

        }

        [Test]
        public void should_cleanup_old_versions()
        {

        }

        [Test]
        public void should_get_latest_with_no_transaction()
        {

        }

        [Test]
        public void should_lock_pages()
        {
            const ulong pageNumber = 1;
            const uint pageOffset = 10;
            const int threadCount = 2;

            ThreadStart threadAction = () =>
            {
                var transaction = _database.BeginTransaction(null);
                _pageCache.BeginTransaction(transaction);

                _pageCache.Lock(transaction, pageNumber);

                Thread.Sleep(1);

                uint count;
                using (var page = _pageCache.Get(transaction, pageNumber, CacheHints.ForUpdate))
                    count = BitConverter.ToUInt32(page.Data, (int)pageOffset);

                Thread.Sleep(1);

                _pageCache.Update(
                    transaction, new[]
                    {
                        new PageUpdate
                        {
                            SequenceNumber = 1,
                            PageNumber = pageNumber,
                            Offset = pageOffset,
                            Data = BitConverter.GetBytes(count + 1U)
                        }
                    });

                _database.CommitTransaction(transaction);
                _pageCache.CommitTransaction(transaction);
                _pageCache.FinalizeTransaction(transaction);
            };

            var threads = new List<Thread>();

            for (var i = 0; i < threadCount; i++)
                threads.Add(new Thread(threadAction));

            foreach (var thread in threads) thread.Start();
            foreach (var thread in threads) thread.Join();

            var readTransaction = _database.BeginTransaction(null);
            _pageCache.BeginTransaction(readTransaction);

            using (var page = _pageCache.Get(readTransaction, pageNumber, CacheHints.None))
                Assert.AreEqual(threadCount, BitConverter.ToUInt32(page.Data, (int)pageOffset));
        }
    }
}