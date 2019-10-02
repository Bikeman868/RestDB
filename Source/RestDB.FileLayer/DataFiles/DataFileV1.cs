using RestDB.Interfaces.FileLayer;
using System;
using System.IO;

namespace RestDB.FileLayer.DataFiles
{
    internal class DataFileV1: IDataFile
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

        bool IDataFile.Read(ulong pageNumber, byte[] data, uint offset)
        {
            if (data == null || data.Length + offset > _pageSize)
                throw new FileLayerException(
                    "Can not read from " + _fileStream.Name + 
                    ", read operation goes beyond the end of the page");

            lock (_lock)
            {
                _fileStream.Seek(PageOffset(pageNumber) + offset, SeekOrigin.Begin);
                return _fileStream.Read(data, 0, data.Length) == data.Length;
            }
        }

        bool IDataFile.Write(ulong pageNumber, byte[] data, uint offset)
        {
            if (data == null || data.Length + offset > _pageSize)
                throw new FileLayerException(
                    "Can not write to " + _fileStream.Name +
                    ", the operation would write beyond the end of the page");

            lock (_lock)
            {
                _fileStream.Seek(PageOffset(pageNumber) + offset, SeekOrigin.Begin);
                _fileStream.Write(data, 0, data.Length);
            }

            return true;
        }
    }
}
