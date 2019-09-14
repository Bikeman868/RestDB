using System;
using System.Collections.Generic;
using System.Text;

namespace RestDB.Interfaces.FileLayer
{
    /// <summary>
    /// Provides a caching mechanism for pages of data from a single file. 
    /// When pages are modified all prior versions that are referenced are 
    /// kept so that transactions can have a static view of the data for
    /// transaction isolation.
    /// </summary>
    public interface IVersionedPageCache
    {
        /// <summary>
        /// Begins a new transaction that will keep track of changes made
        /// </summary>
        ITransaction BeginTransaction();

        /// <summary>
        /// Ends the transaction applying all changes to the underlying file system
        /// and discarding the cached pending writes associated with this transaction
        /// </summary>
        void EndTransaction(ITransaction transaction);

        /// <summary>
        /// Retrieves a page from cache or backing store within the context
        /// of a transaction. If the transaction has modified the page then
        /// the returned page will contain these modifications.
        /// </summary>
        /// <param name="pageNumber">The page number to return</param>
        /// <param name="transaction">The transaction context</param>
        IPage Get(int pageNumber, ITransaction transaction);

        /// <summary>
        /// Updates the cache with a replacement page at a specific version number
        /// </summary>
        /// <param name="transaction">The transaction context of this update</param>
        /// <param name="updates">A list of the changes that need to be applied to
        /// this pages when the transaction completes</param>
        void Put(ITransaction transaction, IEnumerable<PageUpdate> updates);
    }
}
