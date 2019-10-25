using Moq.Modules;
using RestDB.Interfaces;
using RestDB.Interfaces.DatabaseLayer;
using RestDB.Interfaces.FileLayer;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RestDB.UnitTests.Mocks.FileLayer
{
    public class MockPageStore : ConcreteImplementationProvider<IPageStore>, IPageStore
    {
        private readonly uint _pageSize = 64;
        private ulong _nextPage = 1;
        private IDictionary<ulong, IPage> _pages;
        private IDictionary<ushort, ulong> _indexPages;
        private IPagePool _pagePool;

        protected override IPageStore GetImplementation(IMockProducer mockProducer)
        {
            _pagePool = mockProducer.SetupMock<IPagePoolFactory>().Create(_pageSize);
            _pages = new Dictionary<ulong, IPage>();
            _indexPages = new Dictionary<ushort, ulong>();

            return this;
        }

        public uint PageSize => _pageSize;

        public ulong Allocate(ushort pageCount)
        {
            var pageNumber = _nextPage;
            _nextPage += pageCount;

            for (ushort i = 0; i < pageCount; i++)
                _pages.Add(pageNumber + i, _pagePool.Get(pageNumber + i, true));

            return pageNumber;
        }

        public IPageCache BeginTransaction(ITransaction transaction)
        {
            return this;
        }

        public Task CommitTransaction(ITransaction transaction)
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            foreach (var page in _pages.Values)
                page.Dispose();
        }

        public Task FinalizeTransaction(ITransaction transaction)
        {
            return Task.CompletedTask;
        }

        public IPage Get(ITransaction transaction, ulong pageNumber, CacheHints hints)
        {
            if (!_pages.TryGetValue(pageNumber, out IPage page))
                return null;

            return page.Reference();
        }

        public ulong GetFirstIndexPage(ushort objectType)
        {
            if (!_indexPages.TryGetValue(objectType, out ulong pageNumber))
            {
                pageNumber = Allocate(1);
                _indexPages.Add(objectType, pageNumber);
            }
            return pageNumber;
        }

        public void Lock(ITransaction transaction, ulong pageNumber)
        {
        }

        public IPage NewPage(ulong pageNumber)
        {
            using (var page = _pagePool.Get(pageNumber, true))
            {
                _pages.Add(pageNumber, page);
                return page.Reference();
            }
        }

        public void Release(ulong pageNumber)
        {
        }

        public void RollbackTransaction(ITransaction transaction)
        {
        }

        public void Unlock(ITransaction transaction, ulong pageNumber)
        {
        }

        public IPageCache Update(ITransaction transaction, IEnumerable<PageUpdate> updates)
        {
            foreach(var update in updates)
            {
                using (var page = Get(transaction, update.PageNumber, CacheHints.ForUpdate))
                        update.Data.CopyTo(page.Data, update.Offset);
            }

            return this;
        }

    }
}
