using System.IO;
using Moq.Modules;
using NUnit.Framework;
using RestDB.FileLayer.LogFiles;
using RestDB.Interfaces;
using RestDB.Interfaces.DatabaseLayer;
using RestDB.Interfaces.FileLayer;

namespace RestDB.UnitTests.FileLayer
{
    public class LogFileTests : TestBase
    {
        IStartUpLog _startupLog;
        FileInfo _file;
        ILogFile _logFile;

        [SetUp]
        public void Setup()
        {
            _startupLog = SetupMock<IStartUpLog>();
            _file = new FileInfo("C:\\temp\\test.ldf");
            _logFile = new LogFile(_file, true, _startupLog);
        }

        [TearDown]
        public void TearDown()
        {
            if (_logFile != null)
                _logFile.Dispose();
        }

        [Test]
        public void should_log_commits()
        {
            var transaction1 = new Transaction { TransactionId = 1, BeginVersionNumber = 10, CommitVersionNumber = 12 };

            _logFile.CommitStart(transaction1, new[] 
            {
                new PageUpdate { SequenceNumber = 1, PageNumber = 1, Offset = 5, Data = new byte[]{ 1, 2, 3 } },
                new PageUpdate { SequenceNumber = 2, PageNumber = 1, Offset = 12, Data = new byte[]{ 9, 8 } }
            });

            var transaction2 = new Transaction { TransactionId = 2, BeginVersionNumber = 11, CommitVersionNumber = 15 };

            _logFile.CommitStart(transaction2, new[]
            {
                new PageUpdate { SequenceNumber = 1, PageNumber = 2, Offset = 6, Data = new byte[]{ 1, 2, 3 } }
            });

            _logFile.Dispose();

            _logFile = new LogFile(_file, false, _startupLog);

            LogEntryStatus status;
            ulong versionNumber;
            uint updateCount;
            ulong updateSize;

            var offset1 = _logFile.ReadNext(0UL, out status, out versionNumber, out updateCount, out updateSize);

            Assert.AreEqual(LogEntryStatus.LoggedThis, status);
            Assert.AreEqual(12, versionNumber);
            Assert.AreEqual(2U, updateCount);

            var updates = _logFile.GetUpdates(0);

            Assert.AreEqual(2, updates.Count);
            Assert.AreEqual(1, updates[0].SequenceNumber);
            Assert.AreEqual(1, updates[0].PageNumber);
            Assert.AreEqual(5, updates[0].Offset);
            Assert.AreEqual(3, updates[0].Data.Length);
            Assert.AreEqual(1, updates[0].Data[0]);
            Assert.AreEqual(2, updates[0].Data[1]);
            Assert.AreEqual(3, updates[0].Data[2]);
            Assert.AreEqual(2, updates[1].SequenceNumber);
            Assert.AreEqual(1, updates[1].PageNumber);
            Assert.AreEqual(12, updates[1].Offset);
            Assert.AreEqual(2, updates[1].Data.Length);
            Assert.AreEqual(9, updates[1].Data[0]);
            Assert.AreEqual(8, updates[1].Data[1]);

            var offset2 = _logFile.ReadNext(offset1, out status, out versionNumber, out updateCount, out updateSize);

            Assert.AreEqual(LogEntryStatus.LoggedThis, status);
            Assert.AreEqual(15, versionNumber);
            Assert.AreEqual(1U, updateCount);

            updates = _logFile.GetUpdates(offset1);

            Assert.AreEqual(1, updates.Count);
            Assert.AreEqual(1, updates[0].SequenceNumber);
            Assert.AreEqual(2, updates[0].PageNumber);
            Assert.AreEqual(6, updates[0].Offset);
            Assert.AreEqual(3, updates[0].Data.Length);
            Assert.AreEqual(1, updates[0].Data[0]);
            Assert.AreEqual(2, updates[0].Data[1]);
            Assert.AreEqual(3, updates[0].Data[2]);
        }

        [Test]
        public void should_change_commit_status()
        {
            var transaction1 = new Transaction { TransactionId = 1, BeginVersionNumber = 10, CommitVersionNumber = 12 };

            var offset1 = _logFile.CommitStart(transaction1, new[]
            {
                new PageUpdate { SequenceNumber = 1, PageNumber = 1, Offset = 5, Data = new byte[]{ 1, 2, 3 } },
                new PageUpdate { SequenceNumber = 2, PageNumber = 1, Offset = 12, Data = new byte[]{ 9, 8 } }
            });

            var transaction2 = new Transaction { TransactionId = 2, BeginVersionNumber = 11, CommitVersionNumber = 15 };

            var offset2 = _logFile.CommitStart(transaction2, new[]
            {
                new PageUpdate { SequenceNumber = 1, PageNumber = 2, Offset = 6, Data = new byte[]{ 1, 2, 3 } }
            });

            _logFile.CommitLogged(offset1);
            _logFile.CommitComplete(offset2);

            _logFile.Dispose();

            _logFile = new LogFile(_file, false, _startupLog);

            LogEntryStatus status;
            ulong versionNumber;
            uint updateCount;
            ulong updateSize;

            offset2 = _logFile.ReadNext(0UL, out status, out versionNumber, out updateCount, out updateSize);

            Assert.AreEqual(LogEntryStatus.LoggedAll, status);
            Assert.AreEqual(12, versionNumber);
            Assert.AreEqual(2U, updateCount);

            var offset3 = _logFile.ReadNext(offset2, out status, out versionNumber, out updateCount, out updateSize);

            Assert.AreEqual(LogEntryStatus.CompleteThis, status);
            Assert.AreEqual(15, versionNumber);
            Assert.AreEqual(1U, updateCount);
        }

        private class Transaction : ITransaction
        {
            public ulong TransactionId { get; set; }

            public ulong BeginVersionNumber { get; set; }

            public ulong CommitVersionNumber { get; set; }

            public int CompareTo(ITransaction other)
            {
                return TransactionId.CompareTo(other.TransactionId);
            }
        }
    }
}
