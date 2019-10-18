using Moq;
using Moq.Modules;
using NUnit.Framework;
using RestDB.Interfaces.FileLayer;
using System.Collections.Generic;

namespace RestDB.UnitTests.Mocks.FileLayer
{
    public class MockPagePoolFactory : MockImplementationProvider<IPagePoolFactory>
    {
        protected override void SetupMock(IMockProducer mockProducer, Mock<IPagePoolFactory> mock)
        {
            mock.Setup(o => o.Create(It.IsAny<uint>()))
                .Returns<uint>(pageSize => new PagePool(pageSize));
        }

        private class PagePool : IPagePool
        {
            List<Page> _pages = new List<Page>();

            public PagePool(uint pageSize)
            {
                PageSize = pageSize;
            }

            public uint PageSize { get; private set; }

            public IPage Get(ulong pageNumber, bool clear)
            {
                var page = new Page
                {
                    PageNumber = pageNumber,
                    Data = new byte[PageSize]
                };

                if (clear) page.Data.Initialize();

                _pages.Add(page);

                return page;
            }

            public void Verify()
            {
                foreach (var page in _pages)
                    Assert.AreEqual(0, page.ReferenceCount);
            }

            private class Page : IPage
            {
                public ulong PageNumber { get; set; }

                public byte[] Data { get; set; }

                public int ReferenceCount = 1;

                public void Dispose()
                {
                    ReferenceCount--;
                }

                public IPage Reference()
                {
                    ReferenceCount++;
                    return this;
                }
            }
        }
    }
}
