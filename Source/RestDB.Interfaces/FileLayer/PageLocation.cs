using RestDB.Interfaces.DatabaseLayer;
using System;

namespace RestDB.Interfaces.FileLayer
{
    /// <summary>
    /// POCO class for passing a location within pages storage
    /// </summary>
    public class PageLocation
    {
        /// <summary>
        /// The page store where these pages are stored
        /// </summary>
        public IPageStore PageStore;

        /// <summary>
        /// The page number of the page containing the first byte
        /// </summary>
        public ulong PageNumber;

        /// <summary>
        /// The offset into this page in bytes of the first byte
        /// </summary>
        public uint Offset;

        /// <summary>
        /// The length of the data. If the offset + length is greater than
        /// the page size then the record continues on further pages
        /// </summary>
        public ulong Length;

        /// <summary>
        /// When the record spans multiple pages this is a list of the additional
        /// pages to access in order. The record completely fills all but the last
        /// page in this list. You can determine how much of the last page to read
        /// from the Offset, Length and the page size.
        /// When the record is contained in a single page this property is null.
        /// </summary>
        public ulong[] ContunuationPages;
    }

    public static class PageLocationExtensions
    {
        /// <summary>
        /// Reads a record spanning multiple pages into a single byte array
        /// </summary>
        /// <param name="location">The location to read data from</param>
        /// <param name="transaction">The transaction context for reading the pages</param>
        public static byte[] ReadAll(
            this PageLocation location,
            ITransaction transaction,
            CacheHints hints)
        {
            var record = new byte[location.Length];

            if (location.ContunuationPages == null)
            {
                using (var page = location.PageStore.Get(transaction, location.PageNumber, hints))
                {
                    Array.Copy(page.Data, location.Offset, record, 0, record.Length);
                }
            }
            else
            {
                throw new NotImplementedException();
            }

            return record;
        }
    }
}
