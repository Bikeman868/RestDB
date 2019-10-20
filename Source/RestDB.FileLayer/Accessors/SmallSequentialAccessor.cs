using RestDB.Interfaces;
using RestDB.Interfaces.DatabaseLayer;
using RestDB.Interfaces.FileLayer;
using System;
using System.Collections;
using System.Collections.Generic;

/*
 * This accessor stores records in the page store as follows:
 * Some pages are index pages and others are data pages. The
 * 
 * Index pages are laid out as follows:
 * 
 * 8 bytes - page number of next index page or 0 if this is the last page of the index
 * 12 bytes - location of first record. 8 bytes for page number and 4 bytes for byte offset of the next free byte on this page
 * 12 bytes - location of second record
 * 12 bytes - ... repeat to the end of the page
 * 12 bytes of zeros indicates the end of the index
 * 
 * We do not write an index page that only contains the end marker. If the next page
 * number is zero then this is the last page even if the record locators go all the
 * way to the end of the page.
 * 
 * When records are deleted the index entry for that record is replaced with 12 bytes of 0xff
 * 
 * Data pages contain just the record data with nothing in between
 * 
 */

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
                throw new FileLayerException("Invalid attempt to write " + record.Length + 
                    " bytes into a page store with " + pageSize + " byte pages");

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

            _pageStore.Update(transaction, updates);

            return recordLocation;
        }

        void ISequentialRecordAccessor.Clear(
            ushort objectType,
            ITransaction transaction)
        {

            // TODO: Release all of the pages in the page store

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
                        Offset = indexPageLocation.Offset,
                        Data = new byte[]{ 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }
                    }
                });
        }

        IEnumerable<PageLocation> ISequentialRecordAccessor.Enumerate(ushort objectType, ITransaction transaction)
        {
            return new Enumerator(this, objectType, transaction);
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

            var priorRecordPageNumber = 0UL;
            var priorRecordNextRecordOffset = 0U;

            while (true)
            {
                if (indexPageLocation.PageNumber == 0UL) return null;

                using (var indexPage = _pageStore.Get(transaction, indexPageLocation.PageNumber, CacheHints.MetaData))
                {
                    var nextIndexPageNumber = BitConverter.ToUInt64(indexPage.Data, 0);

                    if (newIndexPage)
                        newIndexPage = false;
                    else
                    {
                        priorRecordPageNumber = BitConverter.ToUInt64(indexPage.Data, (int)indexPageLocation.Offset);
                        priorRecordNextRecordOffset = BitConverter.ToUInt32(indexPage.Data, (int)(indexPageLocation.Offset + _pageNumberSize));
                        indexPageLocation.Offset += _indexEntrySize;
                    }

                    if (indexPageLocation.Offset + _indexEntrySize > _pageStore.PageSize)
                    {
                        if (nextIndexPageNumber == 0UL) return null;

                        indexPageLocation.PageNumber = nextIndexPageNumber;
                        indexPageLocation.Offset = _indexPageHeadSize;
                        newIndexPage = true;
                        continue;
                    }

                    var nextRecordPageNumber = BitConverter.ToUInt64(indexPage.Data, (int)indexPageLocation.Offset);
                    var nextRecordNextRecordOffset = BitConverter.ToUInt32(indexPage.Data, (int)(indexPageLocation.Offset + _pageNumberSize));

                    if (nextRecordPageNumber == 0UL) return null; // End of list
                    if (nextRecordPageNumber == ulong.MaxValue) continue; // Skip over deleted record

                    if (nextRecordPageNumber != priorRecordPageNumber)
                    {
                        return new PageLocation
                        {
                            PageNumber = nextRecordPageNumber,
                            Offset = 0,
                            Length = nextRecordNextRecordOffset
                        };
                    }

                    return new PageLocation
                    {
                        PageNumber = nextRecordPageNumber,
                        Offset = priorRecordNextRecordOffset,
                        Length = nextRecordNextRecordOffset - priorRecordNextRecordOffset
                    };
                }
            }
        }

        private class Enumerator: IEnumerable<PageLocation>, IEnumerator<PageLocation>
        {
            private ISequentialRecordAccessor _accessor;
            private ushort _objectType;
            private ITransaction _transaction;
            private PageLocation _current;
            private object _indexLocation;

            public Enumerator(SmallSequentialAccessor accessor, ushort objectType, ITransaction transaction)
            {
                _accessor = accessor;
                _objectType = objectType;
                _transaction = transaction;
            }

            void IDisposable.Dispose()
            {
            }

            IEnumerator<PageLocation> IEnumerable<PageLocation>.GetEnumerator()
            {
                ((IEnumerator)this).Reset();
                return this;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable<PageLocation>)this).GetEnumerator();
            }

            PageLocation IEnumerator<PageLocation>.Current => _current;

            object IEnumerator.Current => _current;

            bool IEnumerator.MoveNext()
            {
                if (_current == null)
                {
                    if (_indexLocation != null) return false;
                    _current = _accessor.LocateFirst(_objectType, _transaction, out _indexLocation);
                }
                else
                    _current = _accessor.LocateNext(_objectType, _transaction, _indexLocation);

                return _current != null;
            }

            void IEnumerator.Reset()
            {
                _current = null;
                _indexLocation = null;
            }
        }
    }
}
