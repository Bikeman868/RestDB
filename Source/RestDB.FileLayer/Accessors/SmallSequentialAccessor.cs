using RestDB.Interfaces;
using RestDB.Interfaces.DatabaseLayer;
using RestDB.Interfaces.FileLayer;
using System;
using System.Collections.Generic;

namespace RestDB.FileLayer.Accessors
{
    /// <summary>
    /// Implements ISequentialRecordAccessor for a short list of small records.
    /// Each record in the list can not exceed the page size of the page store.
    /// </summary>
    internal class SmallSequentialAccessor : ISequentialRecordAccessor
    {
        private IPageStore _pageStore;

        private const uint _indexEntrySize = 12U;
        private const uint _indexPageHeadSize = 8U;
        private const uint _pageNumberSize = 8U;

        public SmallSequentialAccessor(IPageStore pageStore)
        {
            _pageStore = pageStore;
        }

        PageLocation ISequentialRecordAccessor.Append(
            ushort objectType, 
            ITransaction transaction, 
            byte[] record)
        {
            if (record.Length == 0) return null;

            var pageSize = _pageStore.PageSize;

            if (record.Length > pageSize)
                throw new FileLayerException("Invalid attempt to write " + record.Length + " bytes into a page store with " + pageSize + " byte pages");


            var updates = new List<PageUpdate>();
            var sequence = 0U;

            PageLocation indexLocation = new PageLocation
            {
                PageNumber = _pageStore.GetFirstIndexPage(objectType),
                Offset = _indexPageHeadSize,
                Length = _indexEntrySize
            };

            PageLocation recordLocation = new PageLocation
            {
                Length = (uint)record.Length
            };

            while (true)
            {
                _pageStore.Lock(transaction, indexLocation.PageNumber);

                using (var indexPage = _pageStore.Get(transaction, indexLocation.PageNumber, CacheHints.MetaData))
                {
                    var nextIndexPageNumber = BitConverter.ToUInt64(indexPage.Data, 0);
                    if (nextIndexPageNumber == 0UL)
                    {
                        var nextIndexOffset = _indexPageHeadSize;
                        recordLocation.PageNumber = 0UL;

                        while (true)
                        {
                            var nextRecordPageNumber = BitConverter.ToUInt64(indexPage.Data, (int)nextIndexOffset);
                            var nextRecordOffset = BitConverter.ToUInt32(indexPage.Data, (int)(nextIndexOffset + _pageNumberSize));

                            if (nextRecordPageNumber == 0UL)
                            {
                                indexLocation.Offset = nextIndexOffset;
                                break;
                            }

                            recordLocation.PageNumber = nextRecordPageNumber;
                            recordLocation.Offset = nextRecordOffset;

                            nextIndexOffset += _indexEntrySize;

                            if (nextIndexOffset + _indexEntrySize > pageSize)
                            {
                                var newIndexPageNumber = _pageStore.Allocate();

                                updates.Add(new PageUpdate
                                {
                                    SequenceNumber = sequence++,
                                    PageNumber = indexLocation.PageNumber,
                                    Offset = 0U,
                                    Data = BitConverter.GetBytes(newIndexPageNumber)
                                });

                                indexLocation.PageNumber = newIndexPageNumber;
                                indexLocation.Offset = _indexPageHeadSize;

                                break;
                            }
                        }

                        break;
                    }
                    indexLocation.PageNumber = nextIndexPageNumber;
                }
            }

            if (recordLocation.PageNumber == 0UL || recordLocation.Offset + record.Length > pageSize)
            {
                recordLocation.PageNumber = _pageStore.Allocate();
                recordLocation.Offset = 0U;
            }

            var newIndexEntry = new byte[_indexEntrySize];
            BitConverter.GetBytes(recordLocation.PageNumber).CopyTo(newIndexEntry, 0);
            BitConverter.GetBytes(recordLocation.Offset + (uint)record.Length).CopyTo(newIndexEntry, _pageNumberSize);

            updates.Add(new PageUpdate
            {
                SequenceNumber = sequence++,
                PageNumber = recordLocation.PageNumber,
                Offset = recordLocation.Offset,
                Data = record
            });

            updates.Add(new PageUpdate
            {
                SequenceNumber = sequence++,
                PageNumber = indexLocation.PageNumber,
                Offset = indexLocation.Offset,
                Data = newIndexEntry
            });

            if (indexLocation.Offset + _indexEntrySize + _indexEntrySize <= pageSize)
            {
                updates.Add(new PageUpdate
                {
                    SequenceNumber = sequence++,
                    PageNumber = indexLocation.PageNumber,
                    Offset = indexLocation.Offset + _indexEntrySize,
                    Data = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }
                });
            }

            _pageStore.Update(transaction, updates);

            return recordLocation;
        }

        void ISequentialRecordAccessor.Clear(
            ushort objectType,
            ITransaction transaction)
        {
            _pageStore.Update(
                transaction, 
                new[] 
                {
                    new PageUpdate
                    {
                        PageNumber = _pageStore.GetFirstIndexPage(objectType),
                        Data = new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }
                    }
                });
        }

        void ISequentialRecordAccessor.Delete(
            ushort objectType, 
            ITransaction transaction,
            object indexLocation)
        {
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
                        Offset = indexPageLocation.Offset,
                        Data = new byte[]{ 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }
                    }
                });
        }

        PageLocation ISequentialRecordAccessor.LocateFirst(
            ushort objectType, 
            ITransaction transaction, 
            out object indexLocation)
        {
            var indexPageLocation = new PageLocation
            {
                PageNumber = _pageStore.GetFirstIndexPage(objectType),
                Offset = _indexPageHeadSize,
                Length = _indexEntrySize
            };
            indexLocation = indexPageLocation;

            using (var indexPage = _pageStore.Get(transaction, indexPageLocation.PageNumber, CacheHints.MetaData))
            {
                var pageNumber = BitConverter.ToUInt64(indexPage.Data, (int)indexPageLocation.Offset);
                var nextRecordOffset = BitConverter.ToUInt32(indexPage.Data, (int)(indexPageLocation.Offset + _pageNumberSize));

                if (pageNumber == 0UL) return null;

                if (pageNumber == ulong.MaxValue) // First record was deleted
                    return ((ISequentialRecordAccessor)this).LocateNext(objectType, transaction, indexLocation);

                return new PageLocation
                {
                    PageNumber = pageNumber,
                    Offset = 0U,
                    Length = nextRecordOffset
                };
            }
        }

        PageLocation ISequentialRecordAccessor.LocateNext(
            ushort objectType, 
            ITransaction transaction,
            object indexLocation)
        {
            var indexPageLocation = (PageLocation)indexLocation;
            var newIndexPage = false;

            while (true)
            {
                if (indexPageLocation.PageNumber == 0UL) return null;

                using (var indexPage = _pageStore.Get(transaction, indexPageLocation.PageNumber, CacheHints.MetaData))
                {
                    var nextIndexPageNumber = BitConverter.ToUInt64(indexPage.Data, 0);
                    var priorPageNumber = BitConverter.ToUInt64(indexPage.Data, (int)indexPageLocation.Offset);
                    var priorOffset = BitConverter.ToUInt32(indexPage.Data, (int)(indexPageLocation.Offset + _pageNumberSize));

                    ulong nextPageNumber;
                    uint nextOffset;

                    if (newIndexPage)
                        newIndexPage = false;
                    else
                        indexPageLocation.Offset += _indexEntrySize;

                    if (indexPageLocation.Offset + _indexEntrySize > _pageStore.PageSize)
                    {
                        if (nextIndexPageNumber == 0UL) return null;

                        indexPageLocation.PageNumber = nextIndexPageNumber;
                        indexPageLocation.Offset = _indexPageHeadSize;
                        newIndexPage = true;
                        continue;
                    }

                    nextPageNumber = BitConverter.ToUInt64(indexPage.Data, (int)indexPageLocation.Offset);
                    nextOffset = BitConverter.ToUInt32(indexPage.Data, (int)(indexPageLocation.Offset + _pageNumberSize));

                    if (nextPageNumber == 0UL || nextOffset == 0U) return null; // End of list
                    if (nextPageNumber == ulong.MaxValue) continue; // Skip over deleted record

                    if (nextPageNumber != priorPageNumber)
                    {
                        return new PageLocation
                        {
                            PageNumber = nextPageNumber,
                            Offset = 0,
                            Length = nextOffset
                        };
                    }

                    return new PageLocation
                    {
                        PageNumber = nextPageNumber,
                        Offset = priorOffset,
                        Length = nextOffset - priorOffset
                    };
                }
            }
        }
    }
}
