using RestDB.Interfaces;
using RestDB.Interfaces.DatabaseLayer;
using RestDB.Interfaces.FileLayer;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RestDB.FileLayer.Pages
{
    internal class PageStore : IPageStore
    {
        private readonly IPageCache _pageCache;
        private readonly IStartupLog _startUpLog;

        private readonly IDictionary<ushort, ulong> _indexPages;
        private readonly IPage _indexHead;
        private readonly IPage _freePageHead;

        private long _highestPageNumber;

        public PageStore(IPageCache pageCache, IStartupLog startUpLog)
        {
            _pageCache = pageCache;
            _startUpLog = startUpLog;
            _indexPages = new Dictionary<ushort, ulong>();

            // The index master page is always page 0
            // This page contains the starting page numbers for other indexes
            _indexHead = _pageCache.Get(null, 0, CacheHints.None);

            if (_indexHead == null)
            {
                startUpLog.WriteLine("Creating a new page store in a set of empty data files from " + _pageCache);
                _indexHead = _pageCache.NewPage(0);
            }
            else
            {
                startUpLog.WriteLine("Opening a page store on " + _pageCache);

                var offset = 0;
                ushort objectType;
                ulong startPage;
                do
                {
                    objectType = BitConverter.ToUInt16(_indexHead.Data, offset);
                    startPage = BitConverter.ToUInt64(_indexHead.Data, offset + 2);
                    if (objectType > 0)
                    {
                        startUpLog.WriteLine("- the index for type " + objectType + " objects starts at page " + startPage);
                        _indexPages[objectType] = startPage;
                    }
                    else break;
                    offset += 10;
                } while (offset < _indexHead.Data.Length);
            }

            // The free page map is always page 1
            // This is the first in a chain of pages used to manage 
            // unused space in the file set
            _freePageHead = _pageCache.Get(null, 1, CacheHints.None);

            if (_freePageHead == null)
            {
                startUpLog.WriteLine("Initializing a new free page map in this page store");

                _freePageHead = _pageCache.NewPage(1);
                _highestPageNumber = 1;

                _pageCache.Update(null, new[]
                    {
                        new PageUpdate
                        {
                            PageNumber = _freePageHead.PageNumber,
                            Data = BitConverter.GetBytes((ulong)_highestPageNumber)
                        }
                    });
            }
            else
            {
                _highestPageNumber = (long)BitConverter.ToUInt64(_freePageHead.Data, 0);
                startUpLog.WriteLine("This page store contains " + _highestPageNumber + " pages");
            }
        }

        public void Dispose()
        {
            _startUpLog.WriteLine("Disposing of " + this);

            if (_pageCache != null)
                _pageCache.Dispose();
        }

        public override string ToString()
        {
            return "page store on " + _pageCache;
        }

        ulong IPageStore.Allocate()
        {
            return AllocatePageNumber();
        }

        ulong IPageStore.GetFirstIndexPage(ushort objectType)
        {
            if (objectType == 0)
                return _freePageHead.PageNumber;

            ulong pageNumber;

            lock(_indexPages)
            {
                if (!_indexPages.TryGetValue(objectType, out pageNumber))
                {
                    pageNumber = AllocatePageNumber();
                    _indexPages[objectType] = pageNumber;

                    var indexPageData = new byte[_indexHead.Data.Length];

                    var offset = 0U;
                    foreach(var kvp in _indexPages)
                    {
                        if (offset + 9 > indexPageData.Length)
                            throw new FileLayerException("The page size of " + indexPageData.Length + 
                                " is too small to accomodate all of the different object types in the file set");

                        BitConverter.GetBytes(kvp.Key).CopyTo(indexPageData, offset);
                        BitConverter.GetBytes(kvp.Value).CopyTo(indexPageData, offset + 2);
                        offset += 10;
                    }

                    _pageCache.Update(null, new[] 
                    {
                        new PageUpdate
                        {
                            PageNumber = _indexHead.PageNumber,
                            Data = indexPageData
                        }
                    });
                }
            }

            return pageNumber;
        }

        void IPageStore.Release(ulong pageNumber)
        {
            // TODO: reuse pages
        }

        private ulong AllocatePageNumber()
        {
            var pageNumber = (ulong)Interlocked.Increment(ref _highestPageNumber);

            _pageCache.NewPage(pageNumber);

            _pageCache.Update(null, new[]
            {
                new PageUpdate
                {
                    PageNumber = _freePageHead.PageNumber,
                    Data = BitConverter.GetBytes(pageNumber)
                }
            });

            return pageNumber;
        }

        #region Page Cache MixIn

        IPageCache IPageCache.BeginTransaction(ITransaction transaction)
        {
            return _pageCache.BeginTransaction(transaction);
        }

        Task IPageCache.CommitTransaction(ITransaction transaction)
        {
            return _pageCache.CommitTransaction(transaction);
        }

        Task IPageCache.FinalizeTransaction(ITransaction transaction)
        {
            return _pageCache.FinalizeTransaction(transaction);
        }

        void IPageCache.RollbackTransaction(ITransaction transaction)
        {
            _pageCache.RollbackTransaction(transaction);
        }

        void IPageCache.Lock(ITransaction transaction, ulong pageNumber)
        {
            _pageCache.Lock(transaction, pageNumber);
        }

        void IPageCache.Unlock(ITransaction transaction, ulong pageNumber)
        {
            _pageCache.Unlock(transaction, pageNumber);
        }

        IPage IPageCache.Get(ITransaction transaction, ulong pageNumber, CacheHints hints)
        {
            return _pageCache.Get(transaction, pageNumber, hints);
        }

        IPageCache IPageCache.Update(ITransaction transaction, IEnumerable<PageUpdate> updates)
        {
            return _pageCache.Update(transaction, updates);
        }

        IPage IPageCache.NewPage(ulong pageNumber)
        {
            return _pageCache.NewPage(pageNumber);
        }

        uint IPageCache.PageSize => _pageCache.PageSize;

        #endregion
    }
}
