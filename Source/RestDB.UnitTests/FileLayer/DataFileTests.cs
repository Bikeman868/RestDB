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

            var data = new byte[_pageSize];
            Assert.IsFalse(_dataFile.Read(0, data));
        }

        [Test]
        public void should_save_and_return_pages()
        {
            Assert.AreEqual(_pageSize, _dataFile.PageSize);

            for (var pageNumber = 0; pageNumber < 5; pageNumber++)
            {
                var data = new byte[_pageSize];
                data[0] = (byte)(pageNumber * 3);
                Assert.IsTrue(_dataFile.Write((ulong)pageNumber, data));
            }

            _dataFile.Dispose();

            _dataFile = new DataFile(_file);

            Assert.AreEqual(_pageSize, _dataFile.PageSize);

            for (var pageNumber = 0; pageNumber < 5; pageNumber++)
            {
                var data = new byte[_pageSize];
                Assert.IsTrue(_dataFile.Read((ulong)pageNumber, data));
                Assert.AreEqual((byte)(pageNumber * 3), data[0]);
            }
        }
    }
}
