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
    public interface IVersionedPageCache: IDisposable
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
        IVersionedPageCache BeginTransaction(ITransaction transaction);

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
        /// and applies them to the data file. At this pointif the system crashes then
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
        IVersionedPageCache Update(ITransaction transaction, IEnumerable<PageUpdate> updates);

        /// <summary>
        /// Extends ths page store by a new page to the page cache. The page will be cleared to all
        /// bytes of zero. The page must not exist already in the page store.
        /// </summary>
        /// <param name="pageNumber">The unique page number of the page to add. 
        /// Must not exist already in the backing store</param>
        IPage NewPage(ulong pageNumber);
    }
}