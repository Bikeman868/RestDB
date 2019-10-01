using Moq;
using Moq.Modules;
using RestDB.Interfaces.FileLayer;
using System.Collections.Generic;

namespace RestDB.UnitTests.Mocks.FileLayer
{
    public class MockDataFile : MockImplementationProvider<IDataFile>
    {
        const uint _pageSize = 64;
        Dictionary<ulong, byte[]> _pages;

        protected override void SetupMock(IMockProducer mockProducer, Mock<IDataFile> mock)
        {
            _pages = new Dictionary<ulong, byte[]>();

            mock.Setup(o => o.PageSize)
                .Returns(_pageSize);

            mock.Setup(o => o.Write(It.IsAny<IPage>()))
                .Returns<IPage>(page =>
                {
                    var data = new byte[_pageSize];
                    page.Data.CopyTo(data, 0);
                    _pages[page.PageNumber] = data;
                    return true;
                });

            mock.Setup(o => o.Read(It.IsAny<IPage>()))
                .Returns<IPage>(page =>
                {
                    byte[] data;
                    if (_pages.TryGetValue(page.PageNumber, out data))
                    {
                        data.CopyTo(page.Data, 0);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                });
        }
    }
}
