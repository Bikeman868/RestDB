using RestDB.Interfaces.DatabaseLayer;
using RestDB.Interfaces.FileLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestDB.FileLayer.FileSets
{
    internal class FileSet : IFileSet
    {
        readonly IPagePool _pagePool;
        readonly IDataFile _dataFile;
        readonly ILogFile _logFile;
        readonly IDictionary<ITransaction, TransactionDetail> _transactions;

        public FileSet(IDataFile dataFile, ILogFile logFile, IPagePoolFactory pagePoolFactory)
        {
            _dataFile = dataFile;
            _logFile = logFile;
            _pagePool = pagePoolFactory.Create(dataFile.PageSize);

            _transactions = new Dictionary<ITransaction, TransactionDetail>();
        }

        public void Dispose()
        {
            _logFile.Dispose();
            _dataFile.Dispose();
        }

        uint IFileSet.PageSize => _dataFile.PageSize;

        void IFileSet.GetIncompleteTransactions(
            out ulong[] rollBackVersions, 
            out ulong[] rollForwardVersions)
        {
            var mustRollBack = new List<ulong>();
            var canRollForward = new List<ulong>();

            var offset = 0UL;
            do
            {
                LogEntryStatus status;
                ulong version;
                uint count;
                ulong size;
                offset = _logFile.ReadNext(offset, out status, out version, out count, out size);

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

            rollBackVersions = mustRollBack.OrderBy(v => v).ToArray();
            rollForwardVersions = canRollForward.OrderBy(v => v).ToArray();
        }

        void IFileSet.RollForward(ulong versionNumber)
        {
            // TODO: implement resillience
            throw new NotImplementedException();
        }

        void IFileSet.RollBack(ulong versionNumber)
        {
            // TODO: implement resillience
            throw new NotImplementedException();
        }

        bool IFileSet.Read(IPage page)
        {
            return _dataFile.Read(page);
        }

        bool IFileSet.Write(ITransaction transaction, PageUpdate update)
        {
            if (transaction == null)
            {
                var page = _pagePool.Get(update.PageNumber);

                if (!_dataFile.Read(page))
                    page.Data.Initialize();

                update.Data.CopyTo(page.Data, update.Offset);

                _dataFile.Write(page);

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
                transactionDetail.PendingUpdates.Add(update);

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
                transactionDetail.LogFileOffset = _logFile.CommitStart(
                    transaction, transactionDetail.PendingUpdates);
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
                foreach(var update in updatesByPage)
                {
                    if (page == null || page.PageNumber != update.PageNumber)
                    {
                        if (page != null)
                        {
                            _dataFile.Write(page);
                            page.Dispose();
                        }

                        page = _pagePool.Get(update.PageNumber);
                        _dataFile.Read(page);
                    }

                    update.Data.CopyTo(page.Data, update.Offset);
                }

                if (page != null)
                {
                    _dataFile.Write(page);
                    page.Dispose();
                }

                _logFile.CommitComplete(transactionDetail.LogFileOffset);
            });

            return task;
        }

        void IFileSet.RollBackTransaction(ITransaction transaction)
        {
            lock (_transactions)
                _transactions.Remove(transaction);
        }

        private class TransactionDetail
        {
            public List<PageUpdate> PendingUpdates;
            public ulong LogFileOffset;
        }
    }
}
