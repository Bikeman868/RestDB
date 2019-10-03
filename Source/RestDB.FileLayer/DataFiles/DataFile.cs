using RestDB.Interfaces;
using RestDB.Interfaces.FileLayer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RestDB.FileLayer.DataFiles
{
    internal partial class DataFile : IDataFile
    {
        readonly FileInfo _file;
        readonly IStartUpLog _startupLog;
        readonly IDataFile _versionDataFile;

        public DataFile(FileInfo file, uint pageSize, IStartUpLog startupLog)
        {
            _file = file;
            _startupLog = startupLog;

            startupLog.Write("Creating/overwriting version 1 data file " + file.FullName + " with page size " + pageSize);

            var fileStream = file.Open(FileMode.Create, FileAccess.ReadWrite, FileShare.None);

            var version = 1U;
            fileStream.Write(BitConverter.GetBytes(version), 0, 4);

            _versionDataFile = new DataFileV1(fileStream, pageSize);
        }

        public DataFile(FileInfo file, IStartUpLog startupLog)
        {
            _file = file;
            _startupLog = startupLog;

            startupLog.Write("Opening existing data file " + file.FullName);

            var fileStream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            var buffer = new byte[4];
            fileStream.Read(buffer, 0, 4);
            var version = BitConverter.ToUInt32(buffer, 0);

            startupLog.Write("Data file " + file.FullName + " is version " + version);

            if (version == 1)
                _versionDataFile = new DataFileV1(fileStream);
            else
            {
                startupLog.Write("Data file version " + version + " is not supported in this version of the software, please install the latest software", true);
                fileStream.Close();
                throw new UnsupportedVersionException(version, 1, "Data file", file.FullName);
            }
        }

        uint IDataFile.PageSize => _versionDataFile.PageSize;

        void IDisposable.Dispose()
        {
            _startupLog.Write("Closing data file " + _file.FullName);
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

        public override string ToString()
        {
            return "data file " + _file.FullName;
        }
    }
}
