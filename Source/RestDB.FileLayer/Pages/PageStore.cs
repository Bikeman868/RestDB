using RestDB.Interfaces.FileLayer;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace RestDB.FileLayer.Pages
{
    internal class PageStore : IPageStore
    {
        private readonly IVersionedPageCache _pageCache;
        private readonly IDictionary<ushort, ulong> _indexPages;
        private readonly IPage _indexHead;
        private readonly IPage _freePageHead;

        private long _highestPageNumber;

        IVersionedPageCache IPageStore.Pages => _pageCache;

        public PageStore(IVersionedPageCache pageCache)
        {
            _pageCache = pageCache;
            _indexPages = new Dictionary<ushort, ulong>();

            // The index master page is always page 0
            // This page contains the starting page numbers for other indexes
            _indexHead = _pageCache.Get(0, null);

            if (_indexHead == null)
            {
                _indexHead = _pageCache.NewPage(0);
            }
            else
            {
                var offset = 0;
                ushort objectType;
                ulong startPage;
                do
                {
                    objectType = BitConverter.ToUInt16(_indexHead.Data, offset);
                    startPage = BitConverter.ToUInt64(_indexHead.Data, offset + 2);
                    if (objectType > 0)
                        _indexPages[objectType] = startPage;
                    else
                        break;
                    offset += 10;
                } while (offset < _indexHead.Data.Length);
            }

            // The free page map is always page 1
            // This is the first in a chain of pages used to manage 
            // unused space in the file set
            _freePageHead = _pageCache.Get(1, null);

            if (_freePageHead == null)
            {
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
            }
        }

        IPage IPageStore.Allocate()
        {
            var pageNumber = AllocatePageNumber();

            // TODO: ...

            return null;
        }

        IPage IPageStore.GetFirstIndexPage(ushort objectType)
        {
            if (objectType == 0)
                return _freePageHead.Reference();

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

            return _pageCache.Get(pageNumber, null);
        }

        void IPageStore.Release(ulong pageNumber)
        {
            // TODO: reuse pages
        }

        private ulong AllocatePageNumber()
        {
            var pageNumber = (ulong)Interlocked.Increment(ref _highestPageNumber);

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
    }
}
