using RestDB.Interfaces.DatabaseLayer;
using System;
using System.Collections.Generic;
using System.Text;

namespace RestDB.Interfaces.FileLayer
{
    /// <summary>
    ///  Provides methods for manipulating a list of variable length records that
    ///  are stored in an IPageStore
    /// </summary>
    public interface IAccessorFactory
    {
        /// <summary>
        /// Creates a page store accessor that can manipulate a list of small variable
        /// length records.Each record must be smaller than the page size of the page store.
        /// </summary>
        /// <param name="pageStore">The page store to access</param>
        ISequentialRecordAccessor SmallSequentialAccessor(IPageStore pageStore);

        /// <summary>
        /// Creates a page store accessor that can manipulate a list of large variable
        /// length records.Each record can be larger than the page size of the page store.
        /// </summary>
        /// <param name="pageStore">The page store to access</param>
        ISequentialRecordAccessor LargeSequentialAccessor(IPageStore pageStore);

        /// <summary>
        /// Creates a page store accessor that can manipulate an array of small fixed
        /// length records.Each record must be smaller than the page size of the page store.
        /// </summary>
        /// <param name="pageStore">The page store to access</param>
        IRandomRecordAccessor SmallFixedRandomAccessor(IPageStore pageStore, uint recordSize);

        /// <summary>
        /// Creates a page store accessor that can manipulate an array of large fixed
        /// length records.Each record can be larger than the page size of the page store.
        /// </summary>
        /// <param name="pageStore">The page store to access</param>
        IRandomRecordAccessor LargeFixedRandomAccessor(IPageStore pageStore, uint recordSize);

        /// <summary>
        /// Creates a page store accessor that can manipulate an array of small variable
        /// length records.Each record must be smaller than the page size of the page store.
        /// </summary>
        /// <param name="pageStore">The page store to access</param>
        IRandomRecordAccessor SmallVariableRandomAccessor(IPageStore pageStore);

        /// <summary>
        /// Creates a page store accessor that can manipulate an array of large variable
        /// length records.Each record can be larger than the page size of the page store.
        /// </summary>
        /// <param name="pageStore">The page store to access</param>
        IRandomRecordAccessor LargeVariableRandomAccessor(IPageStore pageStore);
    }
}
