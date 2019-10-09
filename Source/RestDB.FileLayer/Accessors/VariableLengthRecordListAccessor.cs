using RestDB.Interfaces;
using RestDB.Interfaces.DatabaseLayer;
using RestDB.Interfaces.FileLayer;
using System;

namespace RestDB.FileLayer.Accessors
{
    internal class VariableLengthRecordListAccessor : IVariableLengthRecordListAccessor
    {
        private IPageStore _pageStore;

        public VariableLengthRecordListAccessor(IPageStore pageStore)
        {
            _pageStore = pageStore;
        }

        void IVariableLengthRecordListAccessor.Append(
            ushort objectType, 
            ITransaction transaction, 
            byte[] record)
        {
            throw new NotImplementedException();
        }

        void IVariableLengthRecordListAccessor.Clear(
            ushort objectType, 
            ITransaction transaction)
        {
            _pageStore.Pages.Update(
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

        void IVariableLengthRecordListAccessor.Delete(
            ushort objectType, 
            ITransaction transaction, 
            PageLocation indexLocation)
        {
            throw new NotImplementedException();
        }

        PageLocation IVariableLengthRecordListAccessor.LocateFirst(
            ushort objectType, 
            ITransaction transaction, 
            out PageLocation indexLocation)
        {
            indexLocation = new PageLocation
            {
                PageNumber = _pageStore.GetFirstIndexPage(objectType),
                Offset = 8U,
                Length = 12U
            };

            using (var indexPage = _pageStore.Pages.Get(transaction, indexLocation.PageNumber, CacheHints.MetaData))
            {
                var pageNumber = BitConverter.ToUInt64(indexPage.Data, (int)indexLocation.Offset);
                var offset = BitConverter.ToUInt32(indexPage.Data, (int)(indexLocation.Offset + 8));
                if (pageNumber == 0UL) return null;

                return new PageLocation
                {
                    PageNumber = pageNumber,
                    Offset = 0,
                    Length = offset
                };
            }
        }

        PageLocation IVariableLengthRecordListAccessor.LocateNext(
            ushort objectType, 
            ITransaction transaction, 
            PageLocation indexLocation)
        {
            if (indexLocation.PageNumber == 0UL) return null;

            using (var indexPage = _pageStore.Pages.Get(transaction, indexLocation.PageNumber, CacheHints.MetaData))
            {
                var nextIndexPageNumber = BitConverter.ToUInt64(indexPage.Data, 0);
                var priorPageNumber = BitConverter.ToUInt64(indexPage.Data, (int)indexLocation.Offset);
                var priorOffset = BitConverter.ToUInt32(indexPage.Data, (int)(indexLocation.Offset + 8U));

                ulong nextPageNumber;
                uint nextOffset;

                if (indexLocation.Offset + indexLocation.Length >= _pageStore.PageSize)
                {
                    if (nextIndexPageNumber == 0UL) return null;

                    indexLocation.PageNumber = nextIndexPageNumber;
                    indexLocation.Offset = 8U;

                    using (var nextIndexPage = _pageStore.Pages.Get(transaction, nextIndexPageNumber, CacheHints.MetaData))
                    {
                        nextPageNumber = BitConverter.ToUInt64(nextIndexPage.Data, (int)indexLocation.Offset);
                        nextOffset = BitConverter.ToUInt32(nextIndexPage.Data, (int)(indexLocation.Offset + 8U));
                    }
                }
                else
                {
                    indexLocation.Offset += 12U;
                    nextPageNumber = BitConverter.ToUInt64(indexPage.Data, (int)indexLocation.Offset);
                    nextOffset = BitConverter.ToUInt32(indexPage.Data, (int)(indexLocation.Offset + 8U));
                }

                if (nextPageNumber == 0UL || nextOffset == 0U) return null;
                if (nextPageNumber != priorPageNumber) priorOffset = 0U;

                return new PageLocation
                {
                    PageNumber = nextPageNumber,
                    Offset = nextOffset,
                    Length = nextOffset - priorOffset
                };
            }
        }
    }
}
