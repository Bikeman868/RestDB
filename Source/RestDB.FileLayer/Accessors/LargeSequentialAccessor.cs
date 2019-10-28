using RestDB.Interfaces;
using RestDB.Interfaces.DatabaseLayer;
using RestDB.Interfaces.FileLayer;
using System;
using System.Collections;
using System.Collections.Generic;

/*
 * This accessor stores records in the page store as follows:
 * 
 * Some pages are index pages and others are data pages. The
 * Index pages are laid out as follows:
 * 
 * 8 bytes - page number of next index page or 0 if this is the last page of the index
 * This is followed by a number of index entries, each of which comprises:
 * 1 byte - unsigned length of the index entry (including this byte) or 0 if this is the end of the index
 * n-1 bytes - index entry where n is the value of the prior byte. 
 *             Note that each index entry is contained within a single page.
 *             Note that the index entry offset points to the length byte
 * 
 * Each index entry starts with a byte that identifies the type of index entry as:
 * 0 = end of the record index
 * 1 = start of a new record
 * 2 = start of a deleted record
 * 3 = continuation pages for the current record
 * 4 = space filler at the end of the page
 * 
 * When the index entry type byte is 1 or 2 the it is followed by:
 * 8-byte unsigned value which is the total length of the data record
 * 8-byte unsigned value which is the page number containing the start of the data
 * 4-byte unsigned value which is the offset into the start page of the first byte of data
 * 
 * When the index entry type byte is 3 it is followed by:
 * 1 byte unsigned value indicating how many pages follow
 * n x 8-byte page numbers where n is the value of the predeeding byte
 * If the record occupies more than 256 pages then this index entry will be followed by more type 3 index entries
 * 
 * When the index entry type is 4 then the rest of the page should be skipped. The index continues on the next page
 * 
 * Data pages contain just the record data with nothing in between. The first and last page
 * may only partially contain this record. The middle pages are entirely filled with the record
 * 
 */

namespace RestDB.FileLayer.Accessors
{
    /// <summary>
    /// Implements ISequentialRecordAccessor for records of any length. If records are
    /// less than the page size of the file set then you should use the SmallSequentialAccessor 
    /// instead
    /// </summary>
    internal class LargeSequentialAccessor : ISequentialRecordAccessor
    {
        private IPageStore _pageStore;
        private uint _pageSize;

        public LargeSequentialAccessor(IPageStore pageStore)
        {
            _pageStore = pageStore;
            _pageSize = pageStore.PageSize;
        }

        PageLocation ISequentialRecordAccessor.Append(ushort objectType, ITransaction transaction, ulong recordSize)
        {
            if (recordSize == 0) return null;

            // Find the last index page

            var indexPageNumber = _pageStore.GetFirstIndexPage(objectType);

            while (true)
            {
                _pageStore.Lock(transaction, indexPageNumber);

                using (var indexPage = _pageStore.Get(transaction, indexPageNumber, CacheHints.MetaData | CacheHints.WithLock))
                {
                    var nextIndexPageNumber = BitConverter.ToUInt64(indexPage.Data, 0);
                    if (nextIndexPageNumber == 0UL)
                        break;
                    indexPageNumber = nextIndexPageNumber;
                }
            }

            var indexEntryOffset = 8U;
            var lastRecordSize = 0UL;
            var lastRecordEndingPageNumber = 0UL;
            var lastRecordBytesOnLastPage = 0U;

            using (var lastIndexPage = _pageStore.Get(transaction, indexPageNumber, CacheHints.MetaData | CacheHints.WithLock))
            {
                while (lastIndexPage.Data[indexEntryOffset] != 0)
                {
                    var indexEntryType = lastIndexPage.Data[indexEntryOffset + 1];

                    if (indexEntryType == 1 || indexEntryType == 2)
                    {
                        // This is the start of a record
                        lastRecordSize = BitConverter.ToUInt64(lastIndexPage.Data, (int)indexEntryOffset + 2);
                        ulong lastRecordStartingPageNumber = BitConverter.ToUInt64(lastIndexPage.Data, (int)indexEntryOffset + 10);
                        uint lastRecordOffset = BitConverter.ToUInt32(lastIndexPage.Data, (int)indexEntryOffset + 18);
                        lastRecordBytesOnLastPage = (uint)((lastRecordSize  + lastRecordOffset) % _pageSize);
                        ulong lastRecordPageCount = (lastRecordSize + lastRecordOffset + _pageSize - 1) / _pageSize;
                        lastRecordEndingPageNumber = lastRecordStartingPageNumber;
                    }
                    else if (indexEntryType == 3 && lastRecordSize != 0)
                    {
                        // This is a record continuation
                        var pageCount = lastIndexPage.Data[indexEntryOffset + 2];
                        lastRecordEndingPageNumber = BitConverter.ToUInt64(lastIndexPage.Data, (int)indexEntryOffset + 3 + (pageCount - 1) * 8);
                    }

                    // Find the last record and last index entry on this page

                    uint priorIndexEntryOffset = indexEntryOffset;
                    indexEntryOffset += lastIndexPage.Data[indexEntryOffset];
                }
            }

            var thisRecordStartingPageNumber = (lastRecordEndingPageNumber > 0 && lastRecordBytesOnLastPage < _pageSize)
                ? lastRecordEndingPageNumber 
                : _pageStore.Allocate();

            var thisRecordOffset = thisRecordStartingPageNumber == lastRecordEndingPageNumber
                ? lastRecordBytesOnLastPage
                : 0U;

            var updates = new List<PageUpdate>();
            var sequence = 1U;

            var additionalPageNumbers = AddRecordIndex(
                updates,
                ref sequence,
                indexPageNumber,
                indexEntryOffset,
                recordSize,
                thisRecordStartingPageNumber,
                thisRecordOffset);

            _pageStore.Update(transaction, updates);

            return new PageLocation
            {
                PageStore = _pageStore,
                PageNumber = thisRecordStartingPageNumber,
                Offset = thisRecordOffset,
                Length = recordSize,
                ContunuationPages = additionalPageNumbers
            };
        }

        void ISequentialRecordAccessor.Clear(ushort objectType, ITransaction transaction)
        {
            var pageNumber = _pageStore.GetFirstIndexPage(objectType);

            _pageStore.Lock(transaction, pageNumber);

            // TODO: Release all of the pages in the page store

            _pageStore.Update(
                transaction,
                new[]
                {
                    new PageUpdate
                    {
                        SequenceNumber = 1,
                        PageNumber = pageNumber,
                        Data = BitConverter.GetBytes(0UL)
                    },
                    new PageUpdate
                    {
                        SequenceNumber = 2,
                        PageNumber = pageNumber,
                        Offset = 8U,
                        Data = IndexEntry0()
                    },
                });
        }

        void ISequentialRecordAccessor.Delete(ushort objectType, ITransaction transaction, object indexLocation)
        {
            // TODO: Scan the index to see if there are pages containing only deleted records and release them in the page store

            var indexPageLocation = (PageLocation)indexLocation;

            _pageStore.Lock(transaction, indexPageLocation.PageNumber);

            _pageStore.Update(
                transaction,
                new[]
                {
                    new PageUpdate
                    {
                        SequenceNumber = 0,
                        PageNumber = indexPageLocation.PageNumber,
                        Offset = indexPageLocation.Offset + 1,
                        Data = new byte[]{ 2 }
                    }
                });
        }

        PageLocation ISequentialRecordAccessor.LocateFirst(ushort objectType, ITransaction transaction, out object indexLocation)
        {
            var indexPageLocation = new PageLocation
            {
                PageStore = _pageStore,
                PageNumber = _pageStore.GetFirstIndexPage(objectType),
                Offset = 8U
            };
            indexLocation = indexPageLocation;

            var recordLocation = new PageLocation
            {
                PageStore = _pageStore
            };

            using (var indexPage = _pageStore.Get(transaction, indexPageLocation.PageNumber, CacheHints.MetaData))
            {
                indexPageLocation.Length = indexPage.Data[8];
                var indexEntryType = indexPage.Data[9];

                if (indexPageLocation.Length == 0 || indexEntryType == 0) return null;

                if (indexEntryType == 2) // First record was deleted
                    return ((ISequentialRecordAccessor)this).LocateNext(objectType, transaction, indexLocation);

                if (indexEntryType != 1) throw new FileLayerException(
                    "The large sequential record structure is corrupt, the index entry type for the first " +
                    "index entry is " + indexEntryType);

                recordLocation.Length = BitConverter.ToUInt64(indexPage.Data, 10);
                recordLocation.PageNumber = BitConverter.ToUInt64(indexPage.Data, 18);
                recordLocation.Offset = BitConverter.ToUInt32(indexPage.Data, 26);
            }

            recordLocation.ContunuationPages = GetAdditionalPages(transaction, indexPageLocation.PageNumber, indexPageLocation.Offset);
            return recordLocation;
        }

        PageLocation ISequentialRecordAccessor.LocateNext(ushort objectType, ITransaction transaction, object indexLocation)
        {
            var indexPageLocation = (PageLocation)indexLocation;
            var indexEntryType = byte.MaxValue;

            var recordLocation = new PageLocation
            {
                PageStore = _pageStore
            };

            while (indexEntryType != 1)
            {
                if (indexEntryType == 0 || indexPageLocation.PageNumber == 0UL) return null;

                using (var indexPage = _pageStore.Get(transaction, indexPageLocation.PageNumber, CacheHints.MetaData))
                {
                    if (indexEntryType == 4)
                    {
                        indexPageLocation.PageNumber = BitConverter.ToUInt64(indexPage.Data, 0);
                        indexPageLocation.Offset = 8U;

                        if (indexPageLocation.PageNumber == 0) return null;

                        using (var nextIndexPage = _pageStore.Get(transaction, indexPageLocation.PageNumber, CacheHints.MetaData))
                            indexEntryType = nextIndexPage.Data[indexPageLocation.Offset + 1];
                    }
                    else
                    {
                        var indexEntryLength = indexPage.Data[indexPageLocation.Offset];
                        indexPageLocation.Offset += indexEntryLength;
                        if (indexPageLocation.Offset + 2 >= _pageSize)
                        {
                            indexEntryType = 4;
                        }
                        else
                        {
                            indexEntryType = indexPage.Data[indexPageLocation.Offset + 1];
                        }
                    }
                }
            }

            using (var indexPage = _pageStore.Get(transaction, indexPageLocation.PageNumber, CacheHints.MetaData))
            {
                recordLocation.Length = BitConverter.ToUInt64(indexPage.Data, (int)(indexPageLocation.Offset + 2U));
                recordLocation.PageNumber = BitConverter.ToUInt64(indexPage.Data, (int)(indexPageLocation.Offset + 10U));
                recordLocation.Offset = BitConverter.ToUInt32(indexPage.Data, (int)(indexPageLocation.Offset + 18U));
            }

            recordLocation.ContunuationPages = GetAdditionalPages(transaction, indexPageLocation.PageNumber, indexPageLocation.Offset);

            return recordLocation;
        }

        IEnumerable<PageLocation> ISequentialRecordAccessor.Enumerate(ushort objectType, ITransaction transaction)
        {
            return new SequentialRecordEnumerator(this, objectType, transaction);
        }

        /// <summary>
        /// Create a list of updates needed to add a record to the index
        /// </summary>
        /// <param name="updates">The update list to append</param>
        /// <param name="sequence">The starting sequence number for updates</param>
        /// <param name="indexPageNumber">The page number of the start of the index entry</param>
        /// <param name="indexOffset">The offset to the length byte for the index entry</param>
        /// <param name="recordSize">The total size of the record to be written</param>
        /// <param name="recordStartPageNumber">The page number to start the record or 0 to allocate a new page</param>
        /// <param name="recordStartOffset">The offset into recordStartPageNumber to start writing the record</param>
        /// <returns></returns>
        private ulong[] AddRecordIndex(
            List<PageUpdate> updates,
            ref uint sequence,
            ulong indexPageNumber,
            uint indexOffset,
            ulong recordSize,
            ulong recordStartPageNumber,
            uint recordStartOffset)
        {
            var startOnNewPage = recordStartPageNumber == 0;

            if (startOnNewPage)
            {
                recordStartPageNumber = _pageStore.Allocate();
                recordStartOffset = 0U;
            }

            var additionalPagesNeeded = (recordSize + recordStartOffset - 1U) / _pageSize;
            var additionalPageNumbers = additionalPagesNeeded > 0 ? new ulong[additionalPagesNeeded] : null;
            var additionalPageIndex = 0;

            var startIndexEntry = IndexEntry1(recordSize, recordStartPageNumber, recordStartOffset);

            AppendIndexEntry(updates, ref sequence, ref indexPageNumber, ref indexOffset, startIndexEntry);

            indexOffset += (uint)startIndexEntry.Length;

            while (additionalPagesNeeded > 0)
            {
                byte pageCount = 255;
                if (additionalPagesNeeded < 255)
                    pageCount = (byte)additionalPagesNeeded;

                var firstAdditionalPage = _pageStore.Allocate(pageCount);

                var pageNumbers = new List<ulong>();
                for (uint i = 0; i < pageCount; i++)
                {
                    pageNumbers.Add(firstAdditionalPage + i);
                    additionalPageNumbers[additionalPageIndex++] = firstAdditionalPage + i;
                }
                var continuationIndexEntry = IndexEntry3(pageNumbers);

                AppendIndexEntry(updates, ref sequence, ref indexPageNumber, ref indexOffset, continuationIndexEntry);

                indexOffset += (uint)continuationIndexEntry.Length;
                additionalPagesNeeded -= pageCount;
            }

            return additionalPageNumbers;
        }

        /// <summary>
        /// Appends an index entry to the record index allocating a new index page if needed
        /// </summary>
        private void AppendIndexEntry(
            List<PageUpdate> updates, 
            ref uint sequence, 
            ref ulong indexPageNumber, 
            ref uint indexOffset, 
            byte[] indexEntry)
        {
            if (indexOffset + indexEntry.Length > _pageSize)
            {
                var endIndexEntry = IndexEntry4();

                if (indexOffset + endIndexEntry.Length <= _pageSize)
                {
                    updates.Add(new PageUpdate
                    {
                        SequenceNumber = sequence++,
                        PageNumber = indexPageNumber,
                        Offset = indexOffset,
                        Data = endIndexEntry
                    });
                }

                var nextIndexPageNumber = _pageStore.Allocate();

                updates.Add(new PageUpdate
                {
                    SequenceNumber = sequence++,
                    PageNumber = indexPageNumber,
                    Offset = 0,
                    Data = BitConverter.GetBytes(nextIndexPageNumber)
                });

                indexPageNumber = nextIndexPageNumber;
                indexOffset = 8U;
            }

            updates.Add(new PageUpdate
            {
                SequenceNumber = sequence++,
                PageNumber = indexPageNumber,
                Offset = indexOffset,
                Data = indexEntry
            });
        }

        /// <summary>
        /// Reads an index entry and returns a list of the additional pages used to hold the record
        /// </summary>
        /// <returns>null if there are no additional pages</returns>
        private ulong[] GetAdditionalPages(ITransaction transaction, ulong indexPageNumber, uint indexOffset)
        {
            List<ulong> additionalPages = null;
            var indexEntryType = (byte)0;

            while (true)
            {
                using (var priorIndexPage = _pageStore.Get(transaction, indexPageNumber, CacheHints.MetaData))
                {
                    indexOffset += priorIndexPage.Data[indexOffset];
                    if (indexOffset + 2 > _pageSize || priorIndexPage.Data[indexOffset + 1] == 4)
                    {
                        indexPageNumber = BitConverter.ToUInt64(priorIndexPage.Data, 0);
                        indexOffset = 8U;

                        if (indexPageNumber == 0)
                        {
                            indexEntryType = 0;
                        }
                        else
                        {
                            using (var nextIndexPage = _pageStore.Get(transaction, indexPageNumber, CacheHints.MetaData))
                            {
                                indexEntryType = nextIndexPage.Data[9];
                            }
                        }
                    }
                    else
                    {
                        indexEntryType = priorIndexPage.Data[indexOffset + 1];
                    }
                }

                if (indexEntryType == 3)
                {
                    if (additionalPages == null) additionalPages = new List<ulong>();

                    using (var indexPage = _pageStore.Get(transaction, indexPageNumber, CacheHints.MetaData))
                    {
                        var pageCount = indexPage.Data[indexOffset + 2];
                        for (byte pageIndex = 0; pageIndex < pageCount; pageIndex++)
                            additionalPages.Add(BitConverter.ToUInt64(indexPage.Data, (int)(indexOffset + 3U + pageIndex * 8U)));
                    }
                }
                else
                {
                    return additionalPages == null ? null : additionalPages.ToArray();
                }
            }
        }

        private byte[] IndexEntry0()
        {
            return new[] { (byte)0 };
        }

        private byte[] IndexEntry1(ulong recordLength, ulong firstPageNumber, uint firstPageOffset)
        {
            var indexEntry = new byte[22];

            indexEntry[0] = (byte)indexEntry.Length;
            indexEntry[1] = 1;

            BitConverter.GetBytes(recordLength).CopyTo(indexEntry, 2);
            BitConverter.GetBytes(firstPageNumber).CopyTo(indexEntry, 10);
            BitConverter.GetBytes(firstPageOffset).CopyTo(indexEntry, 18);

            return indexEntry;
        }

        private byte[] IndexEntry3(IList<ulong> pageNumbers)
        {
            if (pageNumbers.Count > 255)
                throw new FileLayerException("The large sequential accessor can only record up to 255 page numbers in each type 3 index entry");

            var indexEntry = new byte[3 + pageNumbers.Count * 8];

            indexEntry[0] = (byte)indexEntry.Length;
            indexEntry[1] = 3;
            indexEntry[2] = (byte)pageNumbers.Count;

            for(var i = 0; i < pageNumbers.Count; i++)
                BitConverter.GetBytes(pageNumbers[i]).CopyTo(indexEntry, 3 + 8 * i);

            return indexEntry;
        }

        private byte[] IndexEntry4()
        {
            return new[] { (byte)1, (byte)4 };
        }
    }
}
