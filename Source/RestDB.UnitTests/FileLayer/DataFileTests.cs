using System.IO;
using Moq.Modules;
using NUnit.Framework;
using RestDB.FileLayer.DataFiles;
using RestDB.Interfaces.FileLayer;

namespace RestDB.UnitTests.FileLayer
{
    public class DataFileTests : TestBase
    {
        FileInfo _file;
        IDataFile _dataFile;
        uint _pageSize = 32;

        [SetUp]
        public void Setup()
        {
            _file = new FileInfo("C:\\temp\\test.mdf");
            _dataFile = new DataFile(_file, _pageSize);
        }

        [TearDown]
        public void TearDown()
        {
            if (_dataFile != null)
                _dataFile.Dispose();
        }

        [Test]
        public void should_not_return_nonexistent_pages()
        {
            Assert.AreEqual(_pageSize, _dataFile.PageSize);

            var page = new Page
            {
                PageNumber = 0,
                Data = new byte[_pageSize]
            };

            Assert.IsFalse(_dataFile.Read(page));
        }

        [Test]
        public void should_save_and_return_pages()
        {
            Assert.AreEqual(_pageSize, _dataFile.PageSize);

            for (var pageNumber = 0; pageNumber < 5; pageNumber++)
            {
                var page = new Page
                {
                    PageNumber = (ulong)pageNumber,
                    Data = new byte[_pageSize]
                };

                page.Data[0] = (byte)(pageNumber * 3);
                Assert.IsTrue(_dataFile.Write(page));
            }

            _dataFile.Dispose();

            _dataFile = new DataFile(_file);

            Assert.AreEqual(_pageSize, _dataFile.PageSize);

            for (var pageNumber = 0; pageNumber < 5; pageNumber++)
            {
                var page = new Page
                {
                    PageNumber = (ulong)pageNumber,
                    Data = new byte[_pageSize]
                };
                Assert.IsTrue(_dataFile.Read(page));
                Assert.AreEqual((byte)(pageNumber * 3), page.Data[0]);
            }
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
