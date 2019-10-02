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

            mock.Setup(o => o.Write(It.IsAny<ulong>(), It.IsAny<byte[]>(), It.IsAny<uint>()))
                .Returns<ulong, byte[], uint>((pageNumber, data, offset) =>
                {
                    byte[] pageData;
                    if (!_pages.TryGetValue(pageNumber, out pageData))
                    {
                        pageData = new byte[_pageSize];
                        pageData.Initialize();
                        _pages[pageNumber] = pageData;
                    }

                    data.CopyTo(pageData, offset);
                    return true;
                });

            mock.Setup(o => o.Read(It.IsAny<ulong>(), It.IsAny<byte[]>(), It.IsAny<uint>()))
                .Returns<ulong, byte[], uint>((pageNumber, data, offset) =>
                {
                    byte[] pageData;
                    if (!_pages.TryGetValue(pageNumber, out pageData))
                        return false;

                    for (var i = 0; i < data.Length; i++)
                        data[i] = pageData[i + offset];

                    return true;
                });
        }
    }
}
