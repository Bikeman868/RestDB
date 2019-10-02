using RestDB.Interfaces.DatabaseLayer;
using RestDB.Interfaces.FileLayer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestDB.FileLayer.FileSets
{
    internal class FileSet : IFileSet
    {
        readonly IPagePool _pagePool;
        readonly IDataFile[] _dataFiles;
        readonly ILogFile[] _logFiles;
        readonly uint _pageSize;
        readonly IDictionary<ITransaction, TransactionDetail> _transactions;

        int _logFileRoundRobin;

        public FileSet(
            IEnumerable<IDataFile> dataFiles, 
            IEnumerable<ILogFile> logFiles, 
            IPagePoolFactory pagePoolFactory)
        {
            _dataFiles = dataFiles.ToArray();
            _logFiles = logFiles.ToArray();

            if (_dataFiles.Length < 1) throw new FileLayerException("You must have at least 1 data file");
            if (_logFiles.Length < 1) throw new FileLayerException("You must have at least 1 log file");

            _pageSize = _dataFiles[0].PageSize;
            if (_dataFiles.Any(df => df.PageSize != _pageSize))
                throw new FileLayerException("All of the data files must have the same page size");

            if (_dataFiles.Length > 64) throw new FileLayerException("The maximum number of data files is 64");

            _pagePool = pagePoolFactory.Create(_pageSize);

            _transactions = new Dictionary<ITransaction, TransactionDetail>();
        }

        public void Dispose()
        {
            foreach (var logFile in _logFiles) logFile.Dispose();
            foreach(var dataFile in _dataFiles) dataFile.Dispose();
        }

        uint IFileSet.PageSize => _pageSize;

        void IFileSet.GetIncompleteTransactions(
            out ulong[] rollBackVersions, 
            out ulong[] rollForwardVersions)
        {
            var mustRollBack = new List<ulong>();
            var canRollForward = new List<ulong>();

            foreach (var logFile in _logFiles)
            {
                var offset = 0UL;
                do
                {
                    LogEntryStatus status;
                    ulong version;
                    uint count;
                    ulong size;
                    offset = logFile.ReadNext(offset, out status, out version, out count, out size);

                    switch (status)
                    {
                        case LogEntryStatus.LogStarted:
                            mustRollBack.Add(version);
                            break;
                        case LogEntryStatus.LoggedThis:
                        case LogEntryStatus.LoggedAll:
                            canRollForward.Add(version);
                            break;
                    }
                } while (offset > 0);
            }

            rollBackVersions = mustRollBack.OrderBy(v => v).ToArray();
            rollForwardVersions = canRollForward.OrderBy(v => v).ToArray();
        }

        void IFileSet.RollForward(IEnumerable<ulong> versionNumbers)
        {
            var rollForwardList = new HashSet<ulong>(versionNumbers);
            if (rollForwardList.Count == 0) return;

            foreach (var logFile in _logFiles)
            {
                var offset = 0UL;
                do
                {
                    LogEntryStatus status;
                    ulong version;
                    uint count;
                    ulong size;
                    var next = logFile.ReadNext(offset, out status, out version, out count, out size);

                    if (status != LogEntryStatus.Eof && count > 0 && rollForwardList.Contains(version))
                    {
                        var updates = logFile.GetUpdates(offset);
                        if (updates == null) continue;

                        foreach (var update in updates)
                        {
                            ulong filePageNumber;
                            int fileIndex;
                            GetPageLocation(update.PageNumber, out filePageNumber, out fileIndex);

                            if (!_dataFiles[fileIndex].Write(filePageNumber, update.Data, update.Offset))
                                throw new FileLayerException("Data file write failed when rolling forward transaction " + version);
                        }

                        logFile.CommitComplete(offset);
                    }

                    offset = status == LogEntryStatus.Eof ? 0UL : next;
                } while (offset > 0);
            }
        }

        void IFileSet.RollBack(IEnumerable<ulong> versionNumbers)
        {
            var rollBackList = new HashSet<ulong>(versionNumbers);
            if (rollBackList.Count == 0) return;

            foreach (var logFile in _logFiles)
            {
                var offset = 0UL;
                do
                {
                    LogEntryStatus status;
                    ulong version;
                    uint count;
                    ulong size;
                    var next = logFile.ReadNext(offset, out status, out version, out count, out size);

                    if (status != LogEntryStatus.Eof && rollBackList.Contains(version))
                        logFile.RolledBack(offset);

                    offset = status == LogEntryStatus.Eof ? 0UL : next;
                } while (offset > 0);
            }
        }

        bool IFileSet.Read(IPage page)
        {
            ulong filePageNumber;
            int fileIndex;
            GetPageLocation(page.PageNumber, out filePageNumber, out fileIndex);

            return _dataFiles[fileIndex].Read(filePageNumber, page.Data);
        }

        bool IFileSet.Write(ITransaction transaction, IEnumerable<PageUpdate> updates)
        {
            if (transaction == null)
            {
                foreach (var update in updates)
                {
                    ulong filePageNumber;
                    int fileIndex;
                    GetPageLocation(update.PageNumber, out filePageNumber, out fileIndex);

                    if (!_dataFiles[fileIndex].Write(filePageNumber, update.Data, update.Offset))
                        return false;
                }
                return true;
            }

            TransactionDetail transactionDetail;
            lock (_transactions)
            {
                if (!_transactions.TryGetValue(transaction, out transactionDetail))
                {
                    transactionDetail = new TransactionDetail
                    {
                        PendingUpdates = new List<PageUpdate>()
                    };
                    _transactions.Add(transaction, transactionDetail);
                }
            }

            lock (transactionDetail)
                transactionDetail.PendingUpdates.AddRange(updates);

            return true;
        }

        Task IFileSet.CommitTransaction(ITransaction transaction)
        {
            TransactionDetail transactionDetail;
            lock (_transactions)
            {
                if (!_transactions.TryGetValue(transaction, out transactionDetail))
                    return Task.CompletedTask;
            }

            return Task.Run(() => 
            {
                transactionDetail.LogFileIndex = NextLogFileIndex();
                transactionDetail.LogFileOffset = _logFiles[transactionDetail.LogFileIndex].CommitStart(transaction, transactionDetail.PendingUpdates);
            });
        }

        Task IFileSet.FinalizeTransaction(ITransaction transaction)
        {
            TransactionDetail transactionDetail;
            lock (_transactions)
            {
                if (!_transactions.TryGetValue(transaction, out transactionDetail))
                    return Task.CompletedTask;
                _transactions.Remove(transaction);
            }

            // TODO: needs to be a single thread with a queue to ensure transactions are committed in order
            // TODO: maybe group updates by page across multiple transactions

            var task = Task.Run(() =>
            {
                var updatesByPage = transactionDetail.PendingUpdates
                    .OrderBy(u => u.PageNumber)
                    .ThenBy(u => u.SequenceNumber);

                IPage page = null;
                ulong filePageNumber;
                int fileIndex;

                foreach(var update in updatesByPage)
                {
                    if (page == null || page.PageNumber != update.PageNumber)
                    {
                        if (page != null)
                        {
                            GetPageLocation(page.PageNumber, out filePageNumber, out fileIndex);
                            _dataFiles[fileIndex].Write(filePageNumber, page.Data);
                            page.Dispose();
                        }

                        page = _pagePool.Get(update.PageNumber);
                        GetPageLocation(update.PageNumber, out filePageNumber, out fileIndex);
                        _dataFiles[fileIndex].Read(filePageNumber, page.Data);
                    }

                    update.Data.CopyTo(page.Data, update.Offset);
                }

                if (page != null)
                {
                    GetPageLocation(page.PageNumber, out filePageNumber, out fileIndex);
                    _dataFiles[fileIndex].Write(filePageNumber, page.Data);
                    page.Dispose();
                }

                _logFiles[transactionDetail.LogFileIndex].CommitComplete(transactionDetail.LogFileOffset);
            });

            return task;
        }

        void IFileSet.RollBackTransaction(ITransaction transaction)
        {
            lock (_transactions)
                _transactions.Remove(transaction);
        }

        private void GetPageLocation(ulong pageNumber, out ulong filePageNumber, out int fileIndex)
        {
            var fileCount = (uint)_dataFiles.Length;
            fileIndex = (int)(pageNumber % fileCount);
            filePageNumber = pageNumber / fileCount;
        }

        private int NextLogFileIndex()
        {
            return _logFileRoundRobin = (_logFileRoundRobin + 1) % _logFiles.Length;
        }

        private class TransactionDetail
        {
            public List<PageUpdate> PendingUpdates;
            public int LogFileIndex;
            public ulong LogFileOffset;
        }
    }
}
