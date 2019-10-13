using System;
using System.IO;
using Moq.Modules;
using NUnit.Framework;
using RestDB.FileLayer.DataFiles;
using RestDB.Interfaces;
using RestDB.Interfaces.FileLayer;

namespace RestDB.UnitTests.FileLayer
{
    public class DataFileTests : TestBase
    {
        IStartupLog _startupLog;
        FileInfo _file;
        IDataFile _dataFile;
        uint _pageSize = 32;

        [SetUp]
        public void Setup()
        {
            _startupLog = SetupMock<IStartupLog>();
            _file = new FileInfo("C:\\temp\\test.mdf");
            _dataFile = new DataFile(_file, _pageSize, _startupLog);
        }

        [TearDown]
        public void TearDown()
        {
            Console.WriteLine();
            if (_dataFile != null)
                _dataFile.Dispose();
        }

        [Test]
        public void should_have_correct_page_size()
        {
            Assert.AreEqual(_pageSize, _dataFile.PageSize);

            _dataFile.Dispose();

            _dataFile = new DataFile(_file, _startupLog);

            Assert.AreEqual(_pageSize, _dataFile.PageSize);
        }

        [Test]
        public void should_not_return_nonexistent_pages()
        {
            var data = new byte[_pageSize];
            Assert.IsFalse(_dataFile.Read(0, data));
        }

        [Test]
        public void should_save_and_return_pages()
        {
            for (var pageNumber = 0; pageNumber < 5; pageNumber++)
            {
                var data = new byte[_pageSize];
                data[0] = (byte)(pageNumber * 3);
                Assert.IsTrue(_dataFile.Write((ulong)pageNumber, data));
            }

            _dataFile.Dispose();

            _dataFile = new DataFile(_file, _startupLog);

            Assert.AreEqual(_pageSize, _dataFile.PageSize);

            for (var pageNumber = 0; pageNumber < 5; pageNumber++)
            {
                var data = new byte[_pageSize];
                Assert.IsTrue(_dataFile.Read((ulong)pageNumber, data));
                Assert.AreEqual((byte)(pageNumber * 3), data[0]);
            }
        }

        [Test]
        public void should_overwrite_part_of_page()
        {
            var data = new byte[_pageSize];

            for (var i = 0; i < _pageSize; i++)
                data[i] = (byte)i;

            for (var pageNumber = 0; pageNumber < 5; pageNumber++)
                Assert.IsTrue(_dataFile.Write((ulong)pageNumber, data));

            Assert.IsTrue(_dataFile.Write(1UL, new byte[] { 99, 100, 101, 102, 103 }, 10));

            _dataFile.Dispose();

            _dataFile = new DataFile(_file, _startupLog);

            for (var pageNumber = 0; pageNumber < 5; pageNumber++)
            {
                Assert.IsTrue(_dataFile.Read((ulong)pageNumber, data));
                for (var i = 0; i < _pageSize; i++)
                {
                    if (pageNumber == 1 && i >= 10 && i < 15)
                        Assert.AreEqual(89+i, data[i]);
                    else
                        Assert.AreEqual(i, data[i]);
                }
            }
        }
    }
}
