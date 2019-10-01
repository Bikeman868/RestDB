using Moq;
using Moq.Modules;
using RestDB.Interfaces.FileLayer;

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

                return page;
            }

            private class Page : IPage
            {
                public ulong PageNumber { get; set; }

                public byte[] Data { get; set; }

                public void Dispose()
                {
                }

                public IPage Reference()
                {
                    return this;
                }
            }
        }
    }
}
