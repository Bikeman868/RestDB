using RestDB.Interfaces;
using RestDB.Interfaces.FileLayer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace RestDB.FileLayer.LogFiles
{
    internal class LogFile: ILogFile
    {
        readonly ILogFile _versionLogFile;

        public LogFile(FileInfo file, bool initialize)
        {
            if (!file.Exists) initialize = true;

            if (initialize)
            {
                if (!file.Directory.Exists)
                    file.Directory.Create();

                var fileStream = file.Open(FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                _versionLogFile = new LogFileV1(fileStream, true);
            }
            else
            {
                var fileStream = file.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

                var buffer = new byte[4];
                fileStream.Read(buffer, 0, 4);
                var version = BitConverter.ToUInt32(buffer, 0);

                if (version == 1)
                    _versionLogFile = new LogFileV1(fileStream, false);
                else
                {
                    fileStream.Close();
                    throw new UnsupportedVersionException(version, 1, "Log file", file.FullName);
                }
            }
        }

        void IDisposable.Dispose()
        {
            _versionLogFile.Dispose();
        }

        bool ILogFile.Truncate()
        {
            return _versionLogFile.Truncate();
        }

        bool ILogFile.Shrink(ulong oldestVersionNumber)
        {
            return _versionLogFile.Shrink(oldestVersionNumber);
        }

        ulong ILogFile.CommitStart(ITransaction transaction, IEnumerable<PageUpdate> updates)
        {
            return _versionLogFile.CommitStart(transaction, updates);
        }

        void ILogFile.CommitComplete(ulong offset)
        {
            _versionLogFile.CommitComplete(offset);
        }

        void ILogFile.CommitLogged(ulong offset)
        {
            _versionLogFile.CommitLogged(offset);
        }

        List<PageUpdate> ILogFile.GetUpdates(ulong offset)
        {
            return _versionLogFile.GetUpdates(offset);
        }

        ulong ILogFile.ReadNext(ulong offset, out LogEntryStatus status, out ulong versionNumber, out uint updateCount, out ulong updateSize)
        {
            return _versionLogFile.ReadNext(offset, out status, out versionNumber, out updateCount, out updateSize);
        }
    }
}
