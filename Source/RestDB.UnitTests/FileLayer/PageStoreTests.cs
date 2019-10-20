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
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace RestDB.UnitTests.FileLayer
{
    public class PageStoreTests: TestBase
    {
        const uint _pageSize = 64;

        IPagePoolFactory _pagePoolFactory;
        IPagePool _pagePool;
        IStartupLog _startupLog;
        IErrorLog _errorLog;
        IDatabase _database;

        FileInfo _logFileInfo1;
        FileInfo _logFileInfo2;

        FileInfo _dataFileInfo1;
        FileInfo _dataFileInfo2;

        IFileSet _fileSet;
        IPageStore _pageStore;

        [SetUp]
        public void Setup()
        {
            _startupLog = SetupMock<IStartupLog>();
            _errorLog = SetupMock<IErrorLog>();
            _pagePoolFactory = SetupMock<IPagePoolFactory>();
            _pagePool = _pagePoolFactory.Create(_pageSize);

            _logFileInfo1 = new FileInfo("C:\\temp\\test1.ldf");
            _logFileInfo2 = new FileInfo("C:\\temp\\test2.ldf");

            _dataFileInfo1 = new FileInfo("C:\\temp\\test1.mdf");
            _dataFileInfo2 = new FileInfo("C:\\temp\\test2.mdf");

            _fileSet = new FileSet(
                new IDataFile[] { new DataFile(_dataFileInfo1, _pageSize, _startupLog), new DataFile(_dataFileInfo2, _pageSize, _startupLog) },
                new ILogFile[] { new LogFile(_logFileInfo1, true, _startupLog), new LogFile(_logFileInfo2, true, _startupLog) },
                _pagePoolFactory,
                _startupLog);

            var databaseFactory = SetupMock<IDatabaseFactory>();
            _database = databaseFactory.Open(null);

            var pageCache = new PageCache(_fileSet, _database, _pagePoolFactory, _startupLog, _errorLog);
            _pageStore = new PageStore(pageCache, _startupLog);
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
        public void should_update_pages()
        {
            var transaction = _database.BeginTransaction(null);

            _pageStore.BeginTransaction(transaction);

            _pageStore.Update(
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

            using (var page = _pageStore.Get(transaction, 1, CacheHints.None))
            {
                Assert.AreEqual(1, page.Data[10]);
                Assert.AreEqual(2, page.Data[11]);
                Assert.AreEqual(3, page.Data[12]);
            }

            _database.CommitTransaction(transaction);
            _pageStore.CommitTransaction(transaction);
            _pageStore.FinalizeTransaction(transaction);
        }

        [Test]
        public void should_apply_updates_in_sequence()
        {
            const ulong pageNumber = 1UL;

            var transaction = _database.BeginTransaction(null);
            _pageStore.BeginTransaction(transaction);

            _pageStore.Update(
                transaction, new[]
                {
                    new PageUpdate
                    {
                        SequenceNumber = 2,
                        PageNumber = pageNumber,
                        Offset = 11,
                        Data = new byte[]{ 2, 2, 2, 2, 2 }
                    },
                    new PageUpdate
                    {
                        SequenceNumber = 1,
                        PageNumber = pageNumber,
                        Offset = 10,
                        Data = new byte[]{ 1, 1, 1, 1, 1 }
                    },
                    new PageUpdate
                    {
                        SequenceNumber = 3,
                        PageNumber = pageNumber,
                        Offset = 12,
                        Data = new byte[]{ 3, 3, 3, 3, 3 }
                    }
                });

            using (var page = _pageStore.Get(transaction, pageNumber, CacheHints.None))
            {
                Assert.AreEqual(1, page.Data[10]);
                Assert.AreEqual(2, page.Data[11]);
                Assert.AreEqual(3, page.Data[12]);
                Assert.AreEqual(3, page.Data[13]);
                Assert.AreEqual(3, page.Data[14]);
                Assert.AreEqual(3, page.Data[15]);
                Assert.AreEqual(3, page.Data[16]);
            }

            _database.CommitTransaction(transaction);
            _pageStore.CommitTransaction(transaction);
            _pageStore.FinalizeTransaction(transaction);

            transaction = _database.BeginTransaction(null);
            _pageStore.BeginTransaction(transaction);

            using (var page = _pageStore.Get(transaction, pageNumber, CacheHints.None))
            {
                Assert.AreEqual(1, page.Data[10]);
                Assert.AreEqual(2, page.Data[11]);
                Assert.AreEqual(3, page.Data[12]);
                Assert.AreEqual(3, page.Data[13]);
                Assert.AreEqual(3, page.Data[14]);
                Assert.AreEqual(3, page.Data[15]);
                Assert.AreEqual(3, page.Data[16]);
            }

            _database.CommitTransaction(transaction);
            _pageStore.CommitTransaction(transaction);
        }

        [Test]
        public void should_isolate_transactions()
        {
            var transaction1 = _database.BeginTransaction(null);
            var transaction2 = _database.BeginTransaction(null);
            var transaction3 = _database.BeginTransaction(null);

            _pageStore.BeginTransaction(transaction1);
            _pageStore.BeginTransaction(transaction2);

            _pageStore.Update(
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

            _pageStore.BeginTransaction(transaction3);

            using (var page = _pageStore.Get(transaction1, 1, CacheHints.None))
            {
                Assert.AreEqual(0, page.Data[10]);
                Assert.AreEqual(0, page.Data[11]);
                Assert.AreEqual(0, page.Data[12]);
            }

            using (var page = _pageStore.Get(transaction2, 1, CacheHints.None))
            {
                Assert.AreEqual(1, page.Data[10]);
                Assert.AreEqual(2, page.Data[11]);
                Assert.AreEqual(3, page.Data[12]);
            }

            using (var page = _pageStore.Get(transaction3, 1, CacheHints.None))
            { 
                Assert.AreEqual(0, page.Data[10]);
                Assert.AreEqual(0, page.Data[11]);
                Assert.AreEqual(0, page.Data[12]);
            }

            _pageStore.RollbackTransaction(transaction1);
            _pageStore.RollbackTransaction(transaction3);

            _database.CommitTransaction(transaction2);
            _pageStore.CommitTransaction(transaction2);
            _pageStore.FinalizeTransaction(transaction2);

            var transaction4 = _database.BeginTransaction(null);
            _pageStore.BeginTransaction(transaction4);

            using (var page = _pageStore.Get(transaction4, 1, CacheHints.None))
            {
                Assert.AreEqual(1, page.Data[10]);
                Assert.AreEqual(2, page.Data[11]);
                Assert.AreEqual(3, page.Data[12]);
            }

            _pageStore.RollbackTransaction(transaction4);
        }

        [Test]
        public void should_not_isolate_with_no_transaction()
        {
            _pageStore.Update(
                null, new[]
                {
                    new PageUpdate
                    {
                        SequenceNumber = 1,
                        PageNumber = 1,
                        Offset = 10,
                        Data = new byte[]{ 1, 2, 3 }
                    }
                });

            using (var page = _pageStore.Get(null, 1, CacheHints.None))
            {
                Assert.AreEqual(1, page.Data[10]);
                Assert.AreEqual(2, page.Data[11]);
                Assert.AreEqual(3, page.Data[12]);
            }

            var transaction = _database.BeginTransaction(null);
            _pageStore.BeginTransaction(transaction);

            using (var page = _pageStore.Get(transaction, 1, CacheHints.None))
            {
                Assert.AreEqual(1, page.Data[10]);
                Assert.AreEqual(2, page.Data[11]);
                Assert.AreEqual(3, page.Data[12]);
            }

            _database.RollbackTransaction(transaction);
            _pageStore.RollbackTransaction(transaction);
        }

        [Test]
        public void should_snapshot_at_start_of_transaction()
        {
            const ulong pageNumber = 5;

            // Modify some pages within a transaction

            var transaction1 = _database.BeginTransaction(null);
            _pageStore.BeginTransaction(transaction1);
            _pageStore.Update(
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
            _pageStore.CommitTransaction(transaction1);
            _pageStore.FinalizeTransaction(transaction1);

            // Start a transaction and read one of the modified pages

            var transaction2 = _database.BeginTransaction(null);
            _pageStore.BeginTransaction(transaction2);
            using (var page = _pageStore.Get(transaction2, pageNumber, CacheHints.None))
            {
                Assert.AreEqual(98, page.Data[3]);
                Assert.AreEqual(99, page.Data[4]);
                Assert.AreEqual(100, page.Data[5]);
            }

            // Make some more changes to these pages

            for (var i = 0; i < 5; i++)
            {
                var transaction = _database.BeginTransaction(null);
                _pageStore.BeginTransaction(transaction);
                _pageStore.Update(
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
                _pageStore.CommitTransaction(transaction);
                _pageStore.FinalizeTransaction(transaction);
            }

            // Verify that the open transaction has read consistency

            using (var page = _pageStore.Get(transaction2, pageNumber, CacheHints.None))
            {
                Assert.AreEqual(98, page.Data[3]);
                Assert.AreEqual(99, page.Data[4]);
                Assert.AreEqual(100, page.Data[5]);
            }

            // Verify that the open transaction can not see the updates

            using (var page = _pageStore.Get(transaction2, pageNumber + 1, CacheHints.None))
            {
                Assert.AreEqual(98, page.Data[10]);
                Assert.AreEqual(99, page.Data[11]);
                Assert.AreEqual(100, page.Data[12]);
            }
            _pageStore.RollbackTransaction(transaction2);

            // Verify that a new transaction does see the updated values

            var transaction3 = _database.BeginTransaction(null);
            _pageStore.BeginTransaction(transaction3);

            using (var page = _pageStore.Get(transaction3, pageNumber, CacheHints.None))
            {
                Assert.AreEqual(4, page.Data[3]);
                Assert.AreEqual(5, page.Data[4]);
                Assert.AreEqual(6, page.Data[5]);
            }
            using (var page = _pageStore.Get(transaction3, pageNumber + 1, CacheHints.None))
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
            const ulong pageNumber = 1UL;

            var transaction = _database.BeginTransaction(null);
            _pageStore.BeginTransaction(transaction);

            _pageStore.Update(
                transaction, new[]
                {
                    new PageUpdate
                    {
                        SequenceNumber = 1,
                        PageNumber = pageNumber,
                        Offset = 10,
                        Data = new byte[]{ 1, 2, 3 }
                    }
                });

            using (var page = _pageStore.Get(transaction, pageNumber, CacheHints.None))
            {
                Assert.AreEqual(1, page.Data[10]);
                Assert.AreEqual(2, page.Data[11]);
                Assert.AreEqual(3, page.Data[12]);
            }

            using (var page = _pageStore.Get(null, pageNumber, CacheHints.None))
            {
                Assert.AreEqual(0, page.Data[10]);
                Assert.AreEqual(0, page.Data[11]);
                Assert.AreEqual(0, page.Data[12]);
            }

            _database.CommitTransaction(transaction);
            _pageStore.CommitTransaction(transaction);
            _pageStore.FinalizeTransaction(transaction);

            using (var page = _pageStore.Get(null, pageNumber, CacheHints.None))
            {
                Assert.AreEqual(1, page.Data[10]);
                Assert.AreEqual(2, page.Data[11]);
                Assert.AreEqual(3, page.Data[12]);
            }
        }

        [Test]
        public void should_lock_pages()
        {
            const ulong pageNumber = 1;
            const uint pageOffset = 10;
            const int threadCount = 5;

            ThreadStart threadAction = () =>
            {
                var transaction = _database.BeginTransaction(null);
                _pageStore.BeginTransaction(transaction);

                _pageStore.Lock(transaction, pageNumber);

                Thread.Sleep(1);

                uint count;
                using (var page = _pageStore.Get(transaction, pageNumber, CacheHints.ForUpdate))
                    count = BitConverter.ToUInt32(page.Data, (int)pageOffset);

                Thread.Sleep(1);

                _pageStore.Update(
                    transaction, new[]
                    {
                        new PageUpdate
                        {
                            PageNumber = pageNumber,
                            Offset = pageOffset,
                            Data = BitConverter.GetBytes(count + 1U)
                        }
                    });

                _database.CommitTransaction(transaction);
                _pageStore.CommitTransaction(transaction);
                _pageStore.FinalizeTransaction(transaction);
            };

            var threads = new List<Thread>();

            for (var i = 0; i < threadCount; i++)
                threads.Add(new Thread(threadAction));

            foreach (var thread in threads) thread.Start();
            foreach (var thread in threads) thread.Join();

            var readTransaction = _database.BeginTransaction(null);
            _pageStore.BeginTransaction(readTransaction);

            using (var page = _pageStore.Get(readTransaction, pageNumber, CacheHints.None))
                Assert.AreEqual(threadCount, BitConverter.ToUInt32(page.Data, (int)pageOffset));
        }

        [Test]
        public void should_allow_relocking_a_page()
        {
            const ulong pageNumber = 3;

            // Update a page without a lock

            var updateTransaction = _database.BeginTransaction(null);
            _pageStore.BeginTransaction(updateTransaction);

            _pageStore.Update(
                updateTransaction, new[]
                {
                    new PageUpdate
                    {
                        PageNumber = pageNumber,
                        Offset = 3,
                        Data = new byte[]{ 98, 99, 100 }
                    }
                });

            // Lock and update the page multiple times

            for (byte i = 10; i < 15; i++)
            {
                _pageStore.Lock(updateTransaction, pageNumber);

                _pageStore.Update(
                    updateTransaction, new[]
                    {
                        new PageUpdate
                        {
                            PageNumber = pageNumber,
                            Offset = i,
                            Data = new byte[]{ i }
                        }
                    });
            }

            // Commit all the changes to disk

            _database.CommitTransaction(updateTransaction);
            _pageStore.CommitTransaction(updateTransaction);
            _pageStore.FinalizeTransaction(updateTransaction);

            // Check that the changes were written properly

            var readTransaction = _database.BeginTransaction(null);
            _pageStore.BeginTransaction(readTransaction);

            using (var page = _pageStore.Get(readTransaction, pageNumber, CacheHints.None))
            {
                Assert.AreEqual(98, page.Data[3]);
                Assert.AreEqual(99, page.Data[4]);
                Assert.AreEqual(100, page.Data[5]);
                Assert.AreEqual(10, page.Data[10]);
                Assert.AreEqual(11, page.Data[11]);
                Assert.AreEqual(12, page.Data[12]);
                Assert.AreEqual(13, page.Data[13]);
                Assert.AreEqual(14, page.Data[14]);
            }

            _database.RollbackTransaction(readTransaction);
            _pageStore.RollbackTransaction(readTransaction);
        }
    }
}
