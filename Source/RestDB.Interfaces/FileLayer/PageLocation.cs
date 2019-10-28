using RestDB.Interfaces.DatabaseLayer;
using System;

namespace RestDB.Interfaces.FileLayer
{
    /// <summary>
    /// For passing a location within pages storage
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

        /// <summary>
        /// Reads all of the bytes referenced by this location into a buffer and returns it
        /// </summary>
        public byte[] ReadAll(
            ITransaction transaction,
            CacheHints hints)
        {
            var record = new byte[Length];
            ReadInto(transaction, hints, record);
            return record;
        }

        /// <summary>
        /// Reads all of the bytes referenced by this location into the buffer supplied
        /// </summary>
        public void ReadInto(
            ITransaction transaction,
            CacheHints hints,
            byte[] record)
        {
#if DEBUG
            if ((ulong)record.LongLength != Length)
                throw new FileLayerException("The buffer passed is the wrong length");
#endif

            if (ContunuationPages == null)
            {
                using (var page = PageStore.Get(transaction, PageNumber, hints))
                    Array.Copy(page.Data, Offset, record, 0, record.Length);
            }
            else
            {
                var recordOffset = 0UL;
                var bytesToCopy = PageStore.PageSize - Offset;

                using (var page = PageStore.Get(transaction, PageNumber, hints))
                    Array.Copy(page.Data, Offset, record, (long)recordOffset, bytesToCopy);

                foreach(var pageNumber in ContunuationPages)
                {
                    recordOffset += bytesToCopy;

                    bytesToCopy = PageStore.PageSize;

                    if (recordOffset + bytesToCopy > Length)
                        bytesToCopy = (uint)(Length - recordOffset);

                    using (var page = PageStore.Get(transaction, pageNumber, hints))
                        Array.Copy(page.Data, 0, record, (long)recordOffset, bytesToCopy);
                }
            }
        }
    }
}
