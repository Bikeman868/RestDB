using Moq.Modules;
using NUnit.Framework;
using RestDB.FileLayer.DataFiles;
using RestDB.FileLayer.FileSets;
using RestDB.FileLayer.LogFiles;
using RestDB.Interfaces.DatabaseLayer;
using RestDB.Interfaces.FileLayer;
using System;
using System.IO;
using System.Threading;

namespace RestDB.UnitTests.FileLayer
{
    public class FileSetTests: TestBase
    {
        const uint _pageSize = 64;
        IPagePool _pagePool;
        FileInfo _logFileInfo;
        FileInfo _dataFileInfo;
        ILogFile _logFile;
        IDataFile _dataFile;
        IFileSet _fileSet;

        [SetUp]
        public void SetUp()
        {
            var pagePoolFactory = SetupMock<IPagePoolFactory>();
            _pagePool = pagePoolFactory.Create(_pageSize);

            _logFileInfo = new FileInfo("C:\\temp\\test.ldf");
            _logFile = new LogFile(_logFileInfo, true);

            _dataFileInfo = new FileInfo("C:\\temp\\test.mdf");
            _dataFile = new DataFile(_dataFileInfo, _pageSize);

            _fileSet = new FileSet(_dataFile, _logFile, pagePoolFactory);
        }

        [TearDown]
        public void TearDown()
        {
            if (_fileSet != null)
                _fileSet.Dispose();
        }

        [Test]
        public void should_write_directly_to_data()
        {
            _fileSet.Write(null, new PageUpdate
            {
                SequenceNumber = 0,
                PageNumber = 1,
                Offset = 20,
                Data = new byte[] { 5, 6, 7 }
            });

            var page = _pagePool.Get(1);
            _fileSet.Read(page);

            Assert.AreEqual(5, page.Data[20]);
            Assert.AreEqual(6, page.Data[21]);
            Assert.AreEqual(7, page.Data[22]);
        }

        [Test]
        public void should_write_within_transaction()
        {
            var databaseFactory = SetupMock<IDatabaseFactory>();
            var pageStoreFactory = SetupMock<IPageStoreFactory>();

            var pageStore = pageStoreFactory.Open(_fileSet);
            var database = databaseFactory.Open(pageStore);
            var transaction = database.BeginTransaction();

            _fileSet.Write(
                transaction, 
                new PageUpdate
                {
                    SequenceNumber = 0,
                    PageNumber = 1,
                    Offset = 20,
                    Data = new byte[] { 5, 6, 7 }
                });

            // Before the transaction is committed the page should be in its
            // original state

            var originalPage = _pagePool.Get(1);
            _fileSet.Read(originalPage);

            Assert.AreEqual(0, originalPage.Data[20]);
            Assert.AreEqual(0, originalPage.Data[21]);
            Assert.AreEqual(0, originalPage.Data[22]);

            _fileSet
                .CommitTransaction(transaction)
                .ContinueWith(t => _fileSet.FinalizeTransaction(transaction))
                .Wait();

            // After committing the transaction the page should be modified

            var newPage = _pagePool.Get(1);
            _fileSet.Read(originalPage);

            Assert.AreEqual(5, newPage.Data[20]);
            Assert.AreEqual(6, newPage.Data[21]);
            Assert.AreEqual(7, newPage.Data[22]);
        }
    }
}