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
 * 1 byte - unsigned length of the index entry (excluding this byte) or 0 if this is the end of the index
 * n bytes - index entry where n is the value of the prior byte. Each index entry is contained within a single page
 * 
 * Each index entry starts with a byte that identifies the type of index entry as:
 * 0 = end end of the record index
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

        private const uint _indexPageHeadSize = 8U;

        public LargeSequentialAccessor(IPageStore pageStore)
        {
            _pageStore = pageStore;
        }

        PageLocation ISequentialRecordAccessor.Append(ushort objectType, ITransaction transaction, ulong recordSize)
        {
            if (recordSize == 0) return null;

            var pageSize = _pageStore.PageSize;

            var updates = new List<PageUpdate>();
            var sequence = 1U;

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

            // Find the last record and last index entry on this page

            var priorIndexEntryOffset = 0U;
            var indexEntryOffset = _indexPageHeadSize;

            var lastRecordSize = 0UL;
            var lastRecordStartingPageNumber = 0UL;
            var lastRecordEndingPageNumber = 0UL;
            var lastRecordOffset = 0U;
            var lastRecordBytesOnLastPage = 0UL;
            var lastRecordPageCount = 0UL;

            using (var lastIndexPage = _pageStore.Get(transaction, indexPageNumber, CacheHints.MetaData | CacheHints.WithLock))
            {
                while (lastIndexPage.Data[indexEntryOffset] != 0)
                {
                    var indexEntryType = lastIndexPage.Data[indexEntryOffset];

                    if (indexEntryType == 1 || indexEntryType == 2)
                    {
                        // This is the start of a record
                        lastRecordSize = BitConverter.ToUInt64(lastIndexPage.Data, (int)indexEntryOffset + 1);
                        lastRecordStartingPageNumber = BitConverter.ToUInt64(lastIndexPage.Data, (int)indexEntryOffset + 9);
                        lastRecordOffset = BitConverter.ToUInt32(lastIndexPage.Data, (int)indexEntryOffset + 17);
                        lastRecordBytesOnLastPage = (lastRecordSize  + lastRecordOffset) % pageSize;
                        lastRecordPageCount = (lastRecordSize + lastRecordOffset + pageSize - 1) / pageSize;
                        lastRecordEndingPageNumber = lastRecordStartingPageNumber;
                    }
                    else if (indexEntryType == 3 && lastRecordSize != 0)
                    {
                        // This is a record continuation
                        var pageCount = lastIndexPage.Data[indexEntryOffset + 1];
                        lastRecordEndingPageNumber = BitConverter.ToUInt64(lastIndexPage.Data, (int)indexEntryOffset + 2 + (pageCount - 1) * 8);
                    }

                    priorIndexEntryOffset = indexEntryOffset;
                    indexEntryOffset += lastIndexPage.Data[indexEntryOffset] + 1U;
                }
            }

            var thisRecordSize = recordSize;
            var thisRecordStartingPageNumber = lastRecordBytesOnLastPage < pageSize ? lastRecordEndingPageNumber : _pageStore.Allocate();
            var thisRecordEndingPageNumber = 0UL;
            var thisRecordOffset = 0U;
            var thisRecordBytesOnLastPage = 0UL;
            var thisRecordPageCount = 0UL;

            var additionalPageNumbers = AddRecord(
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

        private ulong[] AddRecord(
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

            var additionalPagesNeeded = (recordSize + recordStartOffset - 1U) / _pageStore.PageSize;
            var additionalPageNumbers = new ulong[additionalPagesNeeded];
            var additionalPageIndex = 0;

            var startIndexEntry = new byte[21];
            startIndexEntry[0] = 1;
            BitConverter.GetBytes(recordSize).CopyTo(startIndexEntry, 1);
            BitConverter.GetBytes(recordStartPageNumber).CopyTo(startIndexEntry, 9);
            BitConverter.GetBytes(recordStartOffset).CopyTo(startIndexEntry, 17);
            updates.Add(new PageUpdate
            {
                SequenceNumber = sequence++,
                PageNumber = indexPageNumber,
                Offset = indexOffset,
                Data = startIndexEntry
            });

            indexOffset += (uint)startIndexEntry.Length;

            while (additionalPagesNeeded > 0)
            {
                byte pageCount = 255;
                if (additionalPagesNeeded < 255)
                    pageCount = (byte)additionalPagesNeeded;

                if (indexOffset + 2 + pageCount * 8 > _pageStore.PageSize)
                {
                    var nextIndexPageNumber = _pageStore.Allocate();

                    if (indexOffset < _pageStore.PageSize)
                    {
                        updates.Add(new PageUpdate
                        {
                            SequenceNumber = sequence++,
                            PageNumber = indexPageNumber,
                            Offset = indexOffset,
                            Data = new byte[] { 4 }
                        });
                    }

                    updates.Add(new PageUpdate
                    {
                        SequenceNumber = sequence++,
                        PageNumber = indexPageNumber,
                        Offset = 0,
                        Data = BitConverter.GetBytes(nextIndexPageNumber)
                    });

                    indexPageNumber = nextIndexPageNumber;
                    indexOffset = 0;
                }

                var firstAdditionalPage = _pageStore.Allocate(pageCount);

                var continuationIndexEntry = new byte[2 + pageCount * 8];
                startIndexEntry[0] = 3;
                startIndexEntry[1] = pageCount;

                for (uint i = 0; i < pageCount; i++)
                {
                    BitConverter.GetBytes(firstAdditionalPage + i).CopyTo(startIndexEntry, 2 + i * 8);
                    additionalPageNumbers[additionalPageIndex++] = firstAdditionalPage + i;
                }

                updates.Add(new PageUpdate
                {
                    SequenceNumber = sequence++,
                    PageNumber = indexPageNumber,
                    Offset = indexOffset,
                    Data = continuationIndexEntry
                });

                indexOffset += (uint)continuationIndexEntry.Length;
                additionalPagesNeeded -= pageCount;

                // TODO: Unfinished
            }

            return additionalPageNumbers;
        }

        void ISequentialRecordAccessor.Clear(ushort objectType, ITransaction transaction)
        {
            throw new NotImplementedException();
        }

        void ISequentialRecordAccessor.Delete(ushort objectType, ITransaction transaction, object indexLocation)
        {
            throw new NotImplementedException();
        }

        PageLocation ISequentialRecordAccessor.LocateFirst(ushort objectType, ITransaction transaction, out object indexLocation)
        {
            throw new NotImplementedException();
        }

        PageLocation ISequentialRecordAccessor.LocateNext(ushort objectType, ITransaction transaction, object indexLocation)
        {
            throw new NotImplementedException();
        }

        IEnumerable<PageLocation> ISequentialRecordAccessor.Enumerate(ushort objectType, ITransaction transaction)
        {
            return new SequentialRecordEnumerator(this, objectType, transaction);
        }
    }
}
