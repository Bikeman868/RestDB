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
        /// Creates an access of a page store that can manipulate a list of variable
        /// length records.Each record must be smaller than the page size of the page store.
        /// </summary>
        /// <param name="pageStore">The page store to access</param>
        IVariableLengthRecordListAccessor VariableLengthRecordList(IPageStore pageStore);
    }
}
