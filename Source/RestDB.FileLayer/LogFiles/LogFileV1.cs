using RestDB.Interfaces;
using RestDB.Interfaces.FileLayer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace RestDB.FileLayer.LogFiles
{
    internal class LogFileV1: ILogFile
    {
        readonly FileStream _fileStream;
        readonly object _lock = new object();
        long _end;

        public LogFileV1(FileStream fileStream, bool initialize)
        {
            _fileStream = fileStream;
            _end = 4;

            if (initialize)
            {
                _fileStream.Seek(0, SeekOrigin.Begin);
                _fileStream.Write(BitConverter.GetBytes((UInt32)1), 0, 4);
            }
            else
            {
                lock (_lock)
                {
                    var length = (UInt32)0;
                    var buffer = new byte[4];
                    do
                    {
                        _fileStream.Seek(_end, SeekOrigin.Begin);
                        var bytesRead = _fileStream.Read(buffer, 0, 4);
                        length = bytesRead == 4 ? BitConverter.ToUInt32(buffer, 0) : 0U;
                        _end += length;
                    } while (length > 0);
                }
            }
        }

        void IDisposable.Dispose()
        {
            _fileStream.Close();
        }

        bool ILogFile.Truncate()
        {
            lock (_lock)
            {
                _end = 4;
                _fileStream.SetLength(_end);
            }
            return true;
        }

        bool ILogFile.Shrink(ulong oldestVersionNumber)
        {
            lock (_lock)
            {
                // TODO: shrink the log file
            }
            return true;
        }

        ulong ILogFile.CommitStart(ITransaction transaction, IEnumerable<PageUpdate> updates)
        {
            var offset = _end;

            // Header layout is:
            // 4 bytes  length of this log entry
            // 8 bytes  transaction version number
            // 1 byte   status of this log entry
            // 4 bytes  the number of updates commited by this transaction

            var header = new byte[4 + 8 + 1 + 4];
            header.Initialize();
            var size = (uint)header.Length;

            BitConverter.GetBytes(size).CopyTo(header, 0);
            BitConverter.GetBytes(transaction.CommitVersionNumber).CopyTo(header, 4);
            header[12] = (byte)LogEntryStatus.LogStarted;

            lock(_lock)
            {
                _fileStream.Seek(offset, SeekOrigin.Begin);
                _fileStream.Write(header, 0, header.Length);

                var count = 0U;

                foreach(var update in updates)
                {
                    if (update.Data != null && update.Data.Length > 0)
                    {
                        _fileStream.Write(BitConverter.GetBytes(update.SequenceNumber), 0, 4);
                        size += 4;

                        _fileStream.Write(BitConverter.GetBytes(update.PageNumber), 0, 8);
                        size += 8;

                        _fileStream.Write(BitConverter.GetBytes(update.Offset), 0, 4);
                        size += 4;

                        _fileStream.Write(BitConverter.GetBytes(update.Data.Length), 0, 4);
                        size += 4;

                        _fileStream.Write(update.Data, 0, update.Data.Length);
                        size += (uint)update.Data.Length;

                        count++;
                    }
                }

                BitConverter.GetBytes(size).CopyTo(header, 0);
                header[12] = (byte)LogEntryStatus.LoggedThis;
                BitConverter.GetBytes(count).CopyTo(header, 13);

                _fileStream.Seek(offset, SeekOrigin.Begin);
                _fileStream.Write(header, 0, header.Length);

                _end = offset + size;
                return (ulong)offset;
            }
        }

        void ILogFile.CommitLogged(ulong offset)
        {
            lock (_lock)
            {
                _fileStream.Seek((long)offset + 12, SeekOrigin.Begin);
                _fileStream.WriteByte((byte)LogEntryStatus.LoggedAll);
            }
        }

        void ILogFile.CommitComplete(ulong offset)
        {
            lock(_lock)
            {
                _fileStream.Seek((long)offset + 12, SeekOrigin.Begin);
                _fileStream.WriteByte((byte)LogEntryStatus.CompleteThis);
            }
        }

        ulong ILogFile.ReadNext(ulong offset, out LogEntryStatus status, out ulong versionNumber, out uint updateCount, out ulong updateSize)
        {
            if (offset == 0UL) offset = 4UL;

            var header = new byte[4 + 8 + 1 + 4];

            lock (_lock)
            {
                _fileStream.Seek((long)offset, SeekOrigin.Begin);
                if (_fileStream.Read(header, 0, header.Length) != header.Length)
                {
                    status = LogEntryStatus.Eof;
                    versionNumber = 0UL;
                    updateCount = 0U;
                    updateSize = 0UL;
                    return 0UL;
                }
            }

            updateSize = BitConverter.ToUInt32(header, 0);
            versionNumber = BitConverter.ToUInt64(header, 4);
            status = (LogEntryStatus)header[12];
            updateCount = BitConverter.ToUInt32(header, 13);

            return offset + updateSize;
        }

        List<PageUpdate> ILogFile.GetUpdates(ulong offset)
        {
            if (offset == 0UL) offset = 4UL;

            var header = new byte[4 + 8 + 1 + 4];
            lock (_lock)
            {
                _fileStream.Seek((long)offset, SeekOrigin.Begin);

                if (_fileStream.Read(header, 0, header.Length) != header.Length)
                    throw new FileLayerException("Failed to read updates from " +
                        _fileStream.Name + " log file, " +
                        "the supplied offset " + offset + " is beyond the end of the log file");

                var count = BitConverter.ToUInt32(header, 13);

                var pageUpdates = new List<PageUpdate>();
                var updateHead = new byte[20];

                for (var i = 0; i < count; i++)
                {
                    if (_fileStream.Read(updateHead, 0, updateHead.Length) != updateHead.Length)
                        throw new FileLayerException("Failed to read update head " + i + " from " +
                            _fileStream.Name + " log file, " +
                            "the log entry at offset " + offset + " extends beyond the end of the log file");

                    var pageUpdate = new PageUpdate
                    {
                        SequenceNumber = BitConverter.ToUInt32(updateHead, 0),
                        PageNumber = BitConverter.ToUInt64(updateHead, 4),
                        Offset = BitConverter.ToUInt32(updateHead, 12),
                        Data = new byte[BitConverter.ToUInt32(updateHead, 16)]
                    };

                    if (_fileStream.Read(pageUpdate.Data, 0, pageUpdate.Data.Length) != pageUpdate.Data.Length)
                        throw new FileLayerException("Failed to read update data " + i + " from " +
                            _fileStream.Name + " log file, " +
                            "the log entry at offset " + offset + " extends beyond the end of the log file");

                    pageUpdates.Add(pageUpdate);
                }

                return pageUpdates;
            }
        }
    }
}
