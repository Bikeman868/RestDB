using RestDB.Interfaces;
using RestDB.Interfaces.DatabaseLayer;
using RestDB.Interfaces.FileLayer;
using System;
using System.Collections.Generic;
using System.IO;

namespace RestDB.FileLayer.LogFiles
{
    internal class LogFile: ILogFile
    {
        readonly ILogFile _versionLogFile;
        readonly FileInfo _file;
        readonly IStartUpLog _startupLog;

        public LogFile(FileInfo file, bool initialize, IStartUpLog startupLog)
        {
            _file = file;
            _startupLog = startupLog;

            startupLog.Write((initialize ? "Initializing" : "Opening") + " log file " +  file.FullName);

            if (!file.Exists)
            {
                startupLog.Write("Log file does not exist, we will try to create it");
                initialize = true;
            }

            if (initialize)
            {
                if (!file.Directory.Exists)
                {
                    startupLog.Write("The log file directory " + file.Directory.FullName + " does not exist, we will try to create it");
                    file.Directory.Create();
                }

                startupLog.Write("Creating a version 1 log file in " + file.FullName);
                var fileStream = file.Open(FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                _versionLogFile = new LogFileV1(fileStream, true);
            }
            else
            {
                var fileStream = file.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

                var buffer = new byte[4];
                fileStream.Read(buffer, 0, 4);
                var version = BitConverter.ToUInt32(buffer, 0);

                startupLog.Write("Log file " + file.FullName + " is version " + version + " format");

                if (version == 1)
                    _versionLogFile = new LogFileV1(fileStream, false);
                else
                {
                    fileStream.Close();
                    startupLog.Write("Log file version " + version + " is not supported in this version of the software, please install the latest software", true);
                    throw new UnsupportedVersionException(version, 1, "Log file", file.FullName);
                }
            }
        }

        void IDisposable.Dispose()
        {
            _startupLog.Write("Closing log file " + _file.FullName);
            _versionLogFile.Dispose();
        }

        bool ILogFile.Truncate()
        {
            return _versionLogFile.Truncate();
        }

        bool ILogFile.Shrink(ulong? oldestVersionNumber, bool deleteCompleted)
        {
            return _versionLogFile.Shrink(oldestVersionNumber, deleteCompleted);
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

        void ILogFile.RolledBack(ulong offset)
        {
            _versionLogFile.RolledBack(offset);
        }

        List<PageUpdate> ILogFile.GetUpdates(ulong offset)
        {
            return _versionLogFile.GetUpdates(offset);
        }

        ulong ILogFile.ReadNext(ulong offset, out LogEntryStatus status, out ulong versionNumber, out uint updateCount, out ulong updateSize)
        {
            return _versionLogFile.ReadNext(offset, out status, out versionNumber, out updateCount, out updateSize);
        }

        public override string ToString()
        {
            return "log file " + _file.FullName;
        }
    }
}
