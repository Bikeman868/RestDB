using RestDB.Interfaces.FileLayer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RestDB.FileLayer.DataFiles
{
    internal partial class DataFile : IDataFile
    {
        IDataFile _versionDataFile;

        public DataFile(FileInfo file, uint pageSize)
        {
            var fileStream = file.Open(FileMode.Create, FileAccess.ReadWrite, FileShare.None);

            var version = (UInt32)1;
            fileStream.Write(BitConverter.GetBytes(version), 0, 4);

            _versionDataFile = new DataFileV1(fileStream, pageSize);
        }

        public DataFile(FileInfo file)
        {
            var fileStream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            var buffer = new byte[4];
            fileStream.Read(buffer, 0, 4);
            var version = BitConverter.ToUInt32(buffer, 0);

            if (version == 1)
                _versionDataFile = new DataFileV1(fileStream);
            else
            {
                fileStream.Close();
                throw new UnsupportedVersionException(version, 1, "Data file", file.FullName);
            }
        }

        uint IDataFile.PageSize => _versionDataFile.PageSize;

        void IDisposable.Dispose()
        {
            _versionDataFile.Dispose();
        }

        bool IDataFile.Read(ulong pageNumber, byte[] data, uint offset)
        {
            return _versionDataFile.Read(pageNumber, data, offset);
        }

        bool IDataFile.Write(ulong pageNumber, byte[] data, uint offset)
        {
            return _versionDataFile.Write(pageNumber, data, offset);
        }
    }
}
