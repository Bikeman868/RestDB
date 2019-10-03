using RestDB.Interfaces.DatabaseLayer;
using System;
using System.Collections.Generic;

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
        /// Tells the page cache that a new transaction has started and that
        /// any changes committed by other transactions should not be visible 
        /// to this one.
        /// </summary>
        IVersionedPageCache BeginTransaction(ITransaction transaction);

        /// <summary>
        /// Ends the transaction applying all changes to the underlying file system
        /// and discarding the cached pending writes associated with this transaction
        /// </summary>
        IVersionedPageCache CommitTransaction(ITransaction transaction);

        /// <summary>
        /// Ends the transaction discarding all changes to the underlying file system
        /// </summary>
        IVersionedPageCache RollbackTransaction(ITransaction transaction);

        /// <summary>
        /// Retrieves a page from cache or backing store within the context
        /// of a transaction. If the transaction has modified the page then
        /// the returned page will contain these modifications.
        /// </summary>
        /// <param name="pageNumber">The page number to return</param>
        /// <param name="transaction">The transaction context</param>
        /// <returns>A page from the cache or null if there is no such page.
        /// The page must have Dispose() called when doe accessing it</returns>
        IPage Get(ITransaction transaction, ulong pageNumber);

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