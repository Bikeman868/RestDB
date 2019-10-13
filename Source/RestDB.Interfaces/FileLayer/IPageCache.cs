using RestDB.Interfaces.DatabaseLayer;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RestDB.Interfaces.FileLayer
{
    /// <summary>
    /// Provides a caching mechanism for pages of data from a single file. 
    /// When pages are modified all prior versions that are referenced are 
    /// kept so that transactions can have a static view of the data for
    /// transaction isolation.
    /// </summary>
    public interface IPageCache: IDisposable
    {
        /// <summary>
        /// Returns the page size of the underlying file set
        /// </summary>
        uint PageSize { get; }

        /// <summary>
        /// Tells the page cache that a new transaction has started and that
        /// any changes committed by other transactions should not be visible 
        /// to this one.
        /// </summary>
        IPageCache BeginTransaction(ITransaction transaction);

        /// <summary>
        /// Ends the transaction saving all the changes to the transaction log file
        /// and discarding the cached pending writes associated with this transaction
        /// </summary>
        /// <returns>A task that will complete after the log file has been succesfully
        /// updated</returns>
        Task CommitTransaction(ITransaction transaction);

        /// <summary>
        /// Call this after commiting the the transaction in every page cache. Calling
        /// this method takes all of the changes that were written to the log file
        /// and applies them to the data file. At this point if the system crashes then
        /// the log file will be applied to the data file again to roll the transaction 
        /// forward
        /// </summary>
        /// <returns>A task that will complete after the data file has been succesfully
        /// updated</returns>
        Task FinalizeTransaction(ITransaction transaction);

        /// <summary>
        /// Ends the transaction discarding all changes. Nothing is written to the
        /// file system in this case.
        /// </summary>
        void RollbackTransaction(ITransaction transaction);

        /// <summary>
        /// Blocks the current thread until exclusive access to the page has been obtained.
        /// If this transaction already has a lock on this page then the lock count is 
        /// incremented. A corresponding number of calls to Unlock() are required to unlock
        /// the page. The page will also be unlocked when the transaction terminates.
        /// Blocks any other transactions trying to write to this page until the lock is 
        /// released.
        /// Note that pages are versioned and updates are applied in the order that transations
        /// are committed so there is rarely any need for locking. The main reason for needing
        /// locks is shared counters or lists that are appended. For example if there is a list
        /// and two transactions both add a record to the list in their respective versions of
        /// the page, when both transactions are committed only one new item will be added to
        /// the list.
        /// </summary>
        /// <param name="transaction">The transaction to associate this lock with</param>
        /// <param name="pageNumber">The page to lock. The page will still be readable
        /// by other transactions but any attempt to write will block the writing
        /// transaction until the page is unlocked.</param>
        void Lock(ITransaction transaction, ulong pageNumber);

        /// <summary>
        /// Decrements the lock count for a page and if the lock count reaches zero then
        /// the lock is released and other transactions are permitted to update the page.
        /// Note that when a transaction is committed or rolled back all locks held
        /// by the transaction are released.
        /// Note that you should only unlock the page if you did not modify it. Locking
        /// a page then modifying it and unlocking without committing the changes makes
        /// the lock pointless in most circumstances.
        /// Note that all locks held by a transaction are released when the transaction
        /// is committed or rolled back
        /// </summary>
        /// <param name="transaction">The transaction that applied the lock</param>
        /// <param name="pageNumber">The page number of the page to unlock</param>
        void Unlock(ITransaction transaction, ulong pageNumber);

        /// <summary>
        /// Retrieves a page from cache or backing store within the context
        /// of a transaction. If the transaction has modified the page then
        /// the returned page will contain these modifications.
        /// </summary>
        /// <param name="transaction">The transaction context</param>
        /// <returns>A page from the cache or null if there is no such page.
        /// The page must have Dispose() called when doe accessing it</returns>
        /// <param name="pageNumber">The page number to return</param>
        /// <param name="hints">Hints that affevt caching strategy</param>
        IPage Get(ITransaction transaction, ulong pageNumber, CacheHints hints);

        /// <summary>
        /// Updates a page within the context of a transaction. Only this transaction
        /// will see these changes until the transaction is committed
        /// </summary>
        /// <param name="transaction">The transaction context of this update. This
        /// parameter can be null in which case the change is applied immediately
        /// to the current page and changes how all transactions see this page that
        /// have not taken a snapshot of it.</param>
        /// <param name="updates">A list of the changes that need to be applied to
        /// this page cache when the transaction completes</param>
        IPageCache Update(ITransaction transaction, IEnumerable<PageUpdate> updates);

        /// <summary>
        /// Extends ths page store by a new page to the page cache. The page will be cleared to all
        /// bytes of zero. The page must not exist already in the page store.
        /// </summary>
        /// <param name="pageNumber">The unique page number of the page to add. 
        /// Must not exist already in the backing store</param>
        IPage NewPage(ulong pageNumber);
    }
}