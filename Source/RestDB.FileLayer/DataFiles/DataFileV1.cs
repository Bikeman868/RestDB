using RestDB.Interfaces.FileLayer;
using System;
using System.IO;

namespace RestDB.FileLayer.DataFiles
{
    internal partial class DataFile
    {
        private class DataFileV1: IDataFile
        {
            const UInt32 _headerSize = 32;

            readonly UInt32 _pageSize;
            readonly FileStream _fileStream;
            readonly object _lock = new object();

            public DataFileV1(FileStream fileStream)
            {
                _fileStream = fileStream;

                var buffer = new byte[4];
                _fileStream.Seek(4, SeekOrigin.Begin);
                fileStream.Read(buffer, 0, 4);
                _pageSize = BitConverter.ToUInt32(buffer, 0);
            }

            public DataFileV1(FileStream fileStream, UInt32 pageSize)
            {
                _fileStream = fileStream;
                _pageSize = pageSize;

                _fileStream.Seek(4, SeekOrigin.Begin);
                _fileStream.Write(BitConverter.GetBytes(pageSize), 0, 4);
            }

            uint IDataFile.PageSize => _pageSize;

            void IDisposable.Dispose()
            {
                if (_fileStream != null)
                    _fileStream.Close();
            }

            private long PageOffset(ulong pageNumber)
            {
                return (long)(_headerSize + pageNumber * _pageSize);
            }

            bool IDataFile.Read(IPage page)
            {
                if (page.Data == null || page.Data.Length != _pageSize)
                    throw new FileLayerException(
                        "Can not read from " + _fileStream.Name + 
                        " into this page, the page is the wrong size");

                lock (_lock)
                {
                    _fileStream.Seek(PageOffset(page.PageNumber), SeekOrigin.Begin);
                    var count = _fileStream.Read(page.Data, 0, (int)_pageSize);
                    return count == _pageSize;
                }
            }

            bool IDataFile.Write(IPage page)
            {
                if (page.Data == null || page.Data.Length != _pageSize)
                    throw new FileLayerException(
                        "Can not write to " + _fileStream.Name +
                        " from this page, the page is the wrong size");

                lock (_lock)
                {
                    _fileStream.Seek(PageOffset(page.PageNumber), SeekOrigin.Begin);
                    _fileStream.Write(page.Data, 0, (int)_pageSize);
                }

                return true;
            }
        }
    }
}
