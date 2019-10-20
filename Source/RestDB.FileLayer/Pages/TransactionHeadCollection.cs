using RestDB.Interfaces;
using RestDB.Interfaces.DatabaseLayer;
using RestDB.Interfaces.FileLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace RestDB.FileLayer.Pages
{
    internal class TransactionHeadCollection
    {
        private readonly IStartupLog _startupLog;
        private readonly IErrorLog _errorLog;
        private readonly IPagePool _pagePool;
        private readonly Thread _cleanupThread;
        private readonly IDictionary<ulong, TransactionHead> _transactions;

        private bool _disposing;

        public TransactionHeadCollection(
            IStartupLog startupLog,
            IErrorLog errorLog,
            IPagePool pagePool)
        {
            _transactions = new Dictionary<ulong, TransactionHead>();
            _startupLog = startupLog;
            _errorLog = errorLog;
            _pagePool = pagePool;

            _cleanupThread = new Thread(() =>
            {
                _startupLog.WriteLine("Transaction clean up thread starting");

                while (!_disposing)
                {
                    try
                    {
                        Thread.Sleep(50);

                        // TODO: Kill long running and deadlocked transactions
                    }
                    catch (ThreadAbortException)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        _errorLog.WriteLine("Exception in page cache stale page cleanup thread. " + ex.Message, ex);
                    }
                }

                _startupLog.WriteLine("Transaction clean up thread exiting");
            })
            {
                IsBackground = true,
                Name = "Transaction collection cleanup",
                Priority = ThreadPriority.AboveNormal
            };

            _cleanupThread.Start();
        }

        public void Dispose()
        {
            _startupLog.WriteLine("Disposing of transaction collection");
            _disposing = true;

            _cleanupThread.Join(200);

            lock (_transactions)
            {
                foreach (var transactionHead in _transactions.Values)
                    transactionHead.Dispose();

                _transactions.Clear();
            }
        }
        public override string ToString()
        {
            return "transaction head collection";
        }

        public TransactionHead Lock(ITransaction transaction, PageHead pageHead)
        {
            TransactionHead transactionHead;

            lock (_transactions)
                if (!_transactions.TryGetValue(transaction.TransactionId, out transactionHead))
                    throw new FileLayerException("You must begin the transaction before you can lock pages with it");

            transactionHead.Lock(pageHead);

            return transactionHead;
        }
        public TransactionHead Unlock(ITransaction transaction, PageHead pageHead)
        {
            TransactionHead transactionHead;

            lock (_transactions)
                if (!_transactions.TryGetValue(transaction.TransactionId, out transactionHead))
                    throw new FileLayerException("You must begin the transaction before you can lock pages with it");

            // transactionHead.Unlock(pageHead);

            return transactionHead;
        }

        public TransactionHead BeginTransaction(ITransaction transaction)
        {
            TransactionHead parentHead = null;

            if (transaction.ParentTransactionId.HasValue)
            {
                lock (_transactions)
                {
                    if (!_transactions.TryGetValue(transaction.ParentTransactionId.Value, out parentHead))
                        throw new FileLayerException("You can not start a child transaction when the parent transaction is not active");
                }
            }

            var transactionHead = new TransactionHead(transaction, parentHead, _pagePool);
            lock (_transactions) _transactions.Add(transaction.TransactionId, transactionHead);
            return transactionHead;
        }

        public TransactionHead EndTransaction(ITransaction transaction)
        {
            lock (_transactions)
            {
                if (_transactions.TryGetValue(transaction.TransactionId, out TransactionHead transactionHead))
                {
                    _transactions.Remove(transaction.TransactionId);
                    return transactionHead;
                }
            }
            return null;
        }

        public IPage GetPage(ITransaction transaction, ulong pageNumber, CacheHints hints, PageHeadCollection pages)
        {
            if (transaction == null)
                return pages.GetPageHead(pageNumber, hints).GetVersion(null);

            TransactionHead transactionHead;
            lock (_transactions)
                if (!_transactions.TryGetValue(transaction.TransactionId, out transactionHead))
                    throw new FileLayerException("You can not get pages for a transaction until you begin the transaction");

            var modifiedPage = transactionHead.GetModifiedPage(pageNumber);
            if (modifiedPage != null) return modifiedPage;

            var pageHead = pages.GetPageHead(pageNumber, hints);

            return pageHead.GetVersion(transactionHead.Root.Transaction.BeginVersionNumber);
        }

        public void Update(ITransaction transaction, IEnumerable<PageUpdate> updates, PageHeadCollection pages)
        {
            if (transaction == null)
            {
                IPage page = null;

                foreach (var update in updates.OrderBy(u => u.PageNumber).ThenBy(u => u.SequenceNumber))
                {
                    if (page == null || page.PageNumber != update.PageNumber)
                    {
                        if (page != null) page.Dispose();
                        page = pages.GetPageHead(update.PageNumber, CacheHints.ForUpdate).GetVersion(null);
                    }
                    update.Data.CopyTo(page.Data, update.Offset);
                }

                if (page != null) page.Dispose();

                return;
            }

            TransactionHead transactionHead;

            lock (_transactions)
                if (!_transactions.TryGetValue(transaction.TransactionId, out transactionHead))
                    throw new FileLayerException("You can not apply updates to a transaction context before the transaction has begun or after it has ended");

            transactionHead.AddUpdates(updates, pages);
        }
    }
}