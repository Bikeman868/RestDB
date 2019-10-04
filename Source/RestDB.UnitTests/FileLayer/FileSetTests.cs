using Moq.Modules;
using NUnit.Framework;
using RestDB.FileLayer.DataFiles;
using RestDB.FileLayer.FileSets;
using RestDB.FileLayer.LogFiles;
using RestDB.Interfaces;
using RestDB.Interfaces.DatabaseLayer;
using RestDB.Interfaces.FileLayer;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace RestDB.UnitTests.FileLayer
{
    public class FileSetTests : TestBase
    {
        const uint _pageSize = 64;

        IPagePoolFactory _pagePoolFactory;
        IPagePool _pagePool;
        IStartUpLog _startUpLog;

        FileInfo _logFileInfo1;
        FileInfo _logFileInfo2;
        FileInfo _logFileInfo3;

        FileInfo _dataFileInfo1;
        FileInfo _dataFileInfo2;
        FileInfo _dataFileInfo3;

        IFileSet _fileSet;

        [SetUp]
        public void SetUp()
        {
            _startUpLog = SetupMock<IStartUpLog>();
            _pagePoolFactory = SetupMock<IPagePoolFactory>();
            _pagePool = _pagePoolFactory.Create(_pageSize);

            _logFileInfo1 = new FileInfo("C:\\temp\\test1.ldf");
            _logFileInfo2 = new FileInfo("C:\\temp\\test2.ldf");
            _logFileInfo3 = new FileInfo("C:\\temp\\test3.ldf");

            _dataFileInfo1 = new FileInfo("C:\\temp\\test1.mdf");
            _dataFileInfo2 = new FileInfo("C:\\temp\\test2.mdf");
            _dataFileInfo3 = new FileInfo("C:\\temp\\test3.mdf");
        }

        [TearDown]
        public void TearDown()
        {
            Console.WriteLine();
            if (_fileSet != null)
                _fileSet.Dispose();
        }

        [Test]
        public void should_write_directly_to_data()
        {
            _fileSet = new FileSet(
                new IDataFile[] { new DataFile(_dataFileInfo1, _pageSize, _startUpLog) },
                new ILogFile[] { new LogFile(_logFileInfo1, true, _startUpLog) },
                _pagePoolFactory,
                _startUpLog);

            _fileSet.Write(
                null,
                Enumerable.Repeat(
                    new PageUpdate
                    {
                        SequenceNumber = 0,
                        PageNumber = 1,
                        Offset = 20,
                        Data = new byte[] { 5, 6, 7 }
                    },
                    1));

            using (var page = _pagePool.Get(1))
            {
                _fileSet.Read(page);

                Assert.AreEqual(5, page.Data[20]);
                Assert.AreEqual(6, page.Data[21]);
                Assert.AreEqual(7, page.Data[22]);
            }
        }

        [Test]
        public void should_write_within_transaction()
        {
            _fileSet = new FileSet(
                new IDataFile[] { new DataFile(_dataFileInfo1, _pageSize, _startUpLog) },
                new ILogFile[] { new LogFile(_logFileInfo1, true, _startUpLog) },
                _pagePoolFactory,
                _startUpLog);

            var databaseFactory = SetupMock<IDatabaseFactory>();
            var pageStoreFactory = SetupMock<IPageStoreFactory>();

            var pageStore = pageStoreFactory.Open(_fileSet);
            var database = databaseFactory.Open(pageStore);
            var transaction = database.BeginTransaction();

            _fileSet.Write(
                transaction,
                Enumerable.Repeat(
                    new PageUpdate
                    {
                        SequenceNumber = 0,
                        PageNumber = 1,
                        Offset = 20,
                        Data = new byte[] { 5, 6, 7 }
                    },
                    1));

            // Before the transaction is committed the page should be in its
            // original state

            using (var originalPage = _pagePool.Get(1))
            {
                _fileSet.Read(originalPage);

                Assert.AreEqual(0, originalPage.Data[20]);
                Assert.AreEqual(0, originalPage.Data[21]);
                Assert.AreEqual(0, originalPage.Data[22]);

                _fileSet.CommitTransaction(transaction).Wait();
                _fileSet.FinalizeTransaction(transaction).Wait();

                // After commiting and finalizing the transaction the page should be
                // changed in the data file

                using (var newPage = _pagePool.Get(1))
                {
                    _fileSet.Read(newPage);

                    Assert.AreEqual(5, newPage.Data[20]);
                    Assert.AreEqual(6, newPage.Data[21]);
                    Assert.AreEqual(7, newPage.Data[22]);
                }

                // Anyone with a reference to the original page should not see any change

                Assert.AreEqual(0, originalPage.Data[20]);
                Assert.AreEqual(0, originalPage.Data[21]);
                Assert.AreEqual(0, originalPage.Data[22]);
            }
        }

        [Test]
        public void should_write_multiple_files_within_transaction()
        {
            _fileSet = new FileSet(
                new IDataFile[] { new DataFile(_dataFileInfo1, _pageSize, _startUpLog), new DataFile(_dataFileInfo2, _pageSize, _startUpLog) },
                new ILogFile[] { new LogFile(_logFileInfo1, true, _startUpLog), new LogFile(_logFileInfo2, true, _startUpLog) },
                _pagePoolFactory,
                _startUpLog);

            var databaseFactory = SetupMock<IDatabaseFactory>();
            var pageStoreFactory = SetupMock<IPageStoreFactory>();

            var pageStore = pageStoreFactory.Open(_fileSet);
            var database = databaseFactory.Open(pageStore);
            var transaction1 = database.BeginTransaction();
            var transaction2 = database.BeginTransaction();
            var transaction3 = database.BeginTransaction();

            _fileSet.Write(
                transaction1,
                new[] {
                    new PageUpdate
                    {
                        SequenceNumber = 0,
                        PageNumber = 1,
                        Offset = 20,
                        Data = new byte[] { 1, 2, 3 }
                    }
                });

            _fileSet.Write(
                transaction2,
                new[] {
                    new PageUpdate
                    {
                        SequenceNumber = 0,
                        PageNumber = 1,
                        Offset = 25,
                        Data = new byte[] { 4, 5, 6 }
                    }
                });

            _fileSet.Write(
                transaction3,
                new[] {
                    new PageUpdate
                    {
                        SequenceNumber = 0,
                        PageNumber = 1,
                        Offset = 5,
                        Data = new byte[] { 7, 8, 9 }
                    },
                    new PageUpdate
                    {
                        SequenceNumber = 0,
                        PageNumber = 1,
                        Offset = 30,
                        Data = new byte[] { 10, 11, 12 }
                    }
                });

            // Before the transaction is committed the page should be in its
            // original state

            using (var originalPage = _pagePool.Get(1))
            {
                _fileSet.Read(originalPage);

                for (var i = 0; i < _pageSize; i++)
                    Assert.AreEqual(0, originalPage.Data[i]);

                _fileSet.CommitTransaction(transaction1).Wait();
                _fileSet.CommitTransaction(transaction2).Wait();
                _fileSet.CommitTransaction(transaction3).Wait();
                _fileSet.FinalizeTransaction(transaction1).Wait();
                _fileSet.FinalizeTransaction(transaction2).Wait();
                _fileSet.FinalizeTransaction(transaction3).Wait();

                // After commiting and finalizing the transactions the page should be
                // changed in the data file

                using (var newPage = _pagePool.Get(1))
                {
                    _fileSet.Read(newPage);

                    Assert.AreEqual(1, newPage.Data[20]);
                    Assert.AreEqual(2, newPage.Data[21]);
                    Assert.AreEqual(3, newPage.Data[22]);

                    Assert.AreEqual(4, newPage.Data[25]);
                    Assert.AreEqual(5, newPage.Data[26]);
                    Assert.AreEqual(6, newPage.Data[27]);

                    Assert.AreEqual(7, newPage.Data[5]);
                    Assert.AreEqual(8, newPage.Data[6]);
                    Assert.AreEqual(9, newPage.Data[7]);

                    Assert.AreEqual(10, newPage.Data[30]);
                    Assert.AreEqual(11, newPage.Data[31]);
                    Assert.AreEqual(12, newPage.Data[32]);
                }

                // Anyone with a reference to the original page should not see any change

                for (var i = 0; i < _pageSize; i++)
                    Assert.AreEqual(0, originalPage.Data[i]);
            }
        }

        [Test]
        public void should_roll_forward_committed_transactions_on_restart()
        {
            _fileSet = new FileSet(
                new IDataFile[] { new DataFile(_dataFileInfo1, _pageSize, _startUpLog), new DataFile(_dataFileInfo2, _pageSize, _startUpLog) },
                new ILogFile[] { new LogFile(_logFileInfo1, true, _startUpLog), new LogFile(_logFileInfo2, true, _startUpLog) },
                _pagePoolFactory,
                _startUpLog);

            var databaseFactory = SetupMock<IDatabaseFactory>();
            var pageStoreFactory = SetupMock<IPageStoreFactory>();

            var pageStore = pageStoreFactory.Open(_fileSet);
            var database = databaseFactory.Open(pageStore);

            var transaction1 = database.BeginTransaction();
            _fileSet.Write(
                transaction1,
                new[] {
                    new PageUpdate
                    {
                        SequenceNumber = 0,
                        PageNumber = 1,
                        Offset = 20,
                        Data = new byte[] { 1, 2, 3 }
                    }
                });
            database.CommitTransaction(transaction1);

            var transaction2 = database.BeginTransaction();
            _fileSet.Write(
                transaction2,
                new[] {
                    new PageUpdate
                    {
                        SequenceNumber = 0,
                        PageNumber = 1,
                        Offset = 25,
                        Data = new byte[] { 4, 5, 6 }
                    }
                });
            database.CommitTransaction(transaction2);

            var transaction3 = database.BeginTransaction();
            _fileSet.Write(
                transaction3,
                new[] {
                    new PageUpdate
                    {
                        SequenceNumber = 0,
                        PageNumber = 1,
                        Offset = 5,
                        Data = new byte[] { 7, 8, 9 }
                    },
                    new PageUpdate
                    {
                        SequenceNumber = 0,
                        PageNumber = 1,
                        Offset = 30,
                        Data = new byte[] { 10, 11, 12 }
                    }
                });
            database.CommitTransaction(transaction3);

            // Before the transaction is committed the page should be in its original state

            using (var originalPage = _pagePool.Get(1))
            {
                _fileSet.Read(originalPage);

                for (var i = 0; i < _pageSize; i++)
                    Assert.AreEqual(0, originalPage.Data[i]);
            }

            // Commit transactions to the log files but do not update the data files

            _fileSet.CommitTransaction(transaction1).Wait();
            _fileSet.CommitTransaction(transaction2).Wait();
            _fileSet.CommitTransaction(transaction3).Wait();

            // Shut down and close all the files

            _fileSet.Dispose();

            // Reopen all of the files

            _fileSet = new FileSet(
                new IDataFile[] { new DataFile(_dataFileInfo1, _startUpLog), new DataFile(_dataFileInfo2, _startUpLog) },
                new ILogFile[] { new LogFile(_logFileInfo1, false, _startUpLog), new LogFile(_logFileInfo2, false, _startUpLog) },
                _pagePoolFactory,
                _startUpLog);

            // Roll forward committed transactions

            ulong[] mustRollBackVersions;
            ulong[] canRollForwardVersions;
            _fileSet.GetIncompleteTransactions(out mustRollBackVersions, out canRollForwardVersions);

            _fileSet.RollBack(mustRollBackVersions);
            _fileSet.RollForward(canRollForwardVersions);

            // After rolling forward the page should be changed in the data file

            using (var newPage = _pagePool.Get(1))
            {
                _fileSet.Read(newPage);

                Assert.AreEqual(1, newPage.Data[20]);
                Assert.AreEqual(2, newPage.Data[21]);
                Assert.AreEqual(3, newPage.Data[22]);

                Assert.AreEqual(4, newPage.Data[25]);
                Assert.AreEqual(5, newPage.Data[26]);
                Assert.AreEqual(6, newPage.Data[27]);

                Assert.AreEqual(7, newPage.Data[5]);
                Assert.AreEqual(8, newPage.Data[6]);
                Assert.AreEqual(9, newPage.Data[7]);

                Assert.AreEqual(10, newPage.Data[30]);
                Assert.AreEqual(11, newPage.Data[31]);
                Assert.AreEqual(12, newPage.Data[32]);
            }

            // There should be no incomplete transactions now

            _fileSet.GetIncompleteTransactions(out mustRollBackVersions, out canRollForwardVersions);

            Assert.AreEqual(0, mustRollBackVersions.Length);
            Assert.AreEqual(0, canRollForwardVersions.Length);
        }

        [Test]
        public void should_roll_back_uncommitted_transactions_on_restart()
        {
            _fileSet = new FileSet(
                new IDataFile[] { new DataFile(_dataFileInfo1, _pageSize, _startUpLog), new DataFile(_dataFileInfo2, _pageSize, _startUpLog) },
                new ILogFile[] { new LogFile(_logFileInfo1, true, _startUpLog), new LogFile(_logFileInfo2, true, _startUpLog) },
                _pagePoolFactory,
                _startUpLog);

            var databaseFactory = SetupMock<IDatabaseFactory>();
            var pageStoreFactory = SetupMock<IPageStoreFactory>();

            var pageStore = pageStoreFactory.Open(_fileSet);
            var database = databaseFactory.Open(pageStore);

            var transaction1 = database.BeginTransaction();
            _fileSet.Write(
                transaction1,
                new[] {
                new PageUpdate
                {
                    SequenceNumber = 0,
                    PageNumber = 1,
                    Offset = 20,
                    Data = new byte[] { 1, 2, 3 }
                }
                });
            database.CommitTransaction(transaction1);

            var transaction2 = database.BeginTransaction();
            _fileSet.Write(
                transaction2,
                new[] {
                new PageUpdate
                {
                    SequenceNumber = 0,
                    PageNumber = 1,
                    Offset = 25,
                    Data = new byte[] { 4, 5, 6 }
                }
                });
            database.CommitTransaction(transaction2);

            var transaction3 = database.BeginTransaction();
            _fileSet.Write(
                transaction3,
                new[] {
                new PageUpdate
                {
                    SequenceNumber = 0,
                    PageNumber = 1,
                    Offset = 5,
                    Data = new byte[] { 7, 8, 9 }
                },
                new PageUpdate
                {
                    SequenceNumber = 0,
                    PageNumber = 1,
                    Offset = 30,
                    Data = new byte[] { 10, 11, 12 }
                }
                });
            database.CommitTransaction(transaction3);

            // Before the transaction is committed the page should be in its original state

            using (var originalPage = _pagePool.Get(1))
            {
                _fileSet.Read(originalPage);

                for (var i = 0; i < _pageSize; i++)
                    Assert.AreEqual(0, originalPage.Data[i]);
            }

            // Commit transactions to the log files but do not update the data files

            _fileSet.CommitTransaction(transaction1).Wait();
            _fileSet.CommitTransaction(transaction2).Wait();
            _fileSet.CommitTransaction(transaction3).Wait();

            // Shut down and close all the files

            _fileSet.Dispose();

            // Reopen all of the files

            _fileSet = new FileSet(
                new IDataFile[] { new DataFile(_dataFileInfo1, _startUpLog), new DataFile(_dataFileInfo2, _startUpLog) },
                new ILogFile[] { new LogFile(_logFileInfo1, false, _startUpLog), new LogFile(_logFileInfo2, false, _startUpLog) },
                _pagePoolFactory,
                _startUpLog);

            // Roll back all transactions

            ulong[] mustRollBackVersions;
            ulong[] canRollForwardVersions;
            _fileSet.GetIncompleteTransactions(out mustRollBackVersions, out canRollForwardVersions);

            _fileSet.RollBack(mustRollBackVersions);
            _fileSet.RollBack(canRollForwardVersions);

            // After rolling back the page should be unchanged in the data file

            using (var newPage = _pagePool.Get(1))
            {
                _fileSet.Read(newPage);

                for (var i = 0; i < _pageSize; i++)
                    Assert.AreEqual(0, newPage.Data[i]);
            }

            // There should be no incomplete transactions now

            _fileSet.GetIncompleteTransactions(out mustRollBackVersions, out canRollForwardVersions);

            Assert.AreEqual(0, mustRollBackVersions.Length);
            Assert.AreEqual(0, canRollForwardVersions.Length);
        }
    }
}