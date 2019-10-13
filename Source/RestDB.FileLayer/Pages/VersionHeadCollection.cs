using RestDB.Interfaces;
using RestDB.Interfaces.DatabaseLayer;
using RestDB.Interfaces.FileLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace RestDB.FileLayer.Pages
{
    internal class VersionHeadCollection
    {
        private readonly IDatabase _database;
        private readonly IStartupLog _startupLog;
        private readonly IErrorLog _errorLog;
        private readonly Thread _cleanupThread;
        private readonly IDictionary<ulong, VersionHead> _versions;

        private bool _disposing;

        public VersionHeadCollection(
            IStartupLog startupLog,
            IErrorLog errorLog,
            IDatabase database)
        {
            _versions = new Dictionary<ulong, VersionHead>();
            _startupLog = startupLog;
            _errorLog = errorLog;
            _database = database;

            _cleanupThread = new Thread(() =>
            {
                _startupLog.WriteLine("Version clean up thread starting");

                while (!_disposing)
                {
                    try
                    {
                        Thread.Sleep(50);

                        List<VersionHead> versions;
                        lock (_versions) versions = _versions.Values.OrderBy(v => v.VersionNumber).ToList();

                        foreach (var version in versions)
                        {
                            if (version.IsReferenced || version.VersionNumber == _database.CurrentVersion) break;

                            lock (_versions) _versions.Remove(version.VersionNumber);
                            version.Dispose();
                        }
                    }
                    catch (ThreadAbortException)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        _errorLog.WriteLine("Exception in page collection cleanup thread. " + ex.Message, ex);
                    }
                }

                _startupLog.WriteLine("Version clean up thread exiting");
            })
            {
                IsBackground = true,
                Name = "Version collection cleanup",
                Priority = ThreadPriority.AboveNormal
            };

            _cleanupThread.Start();
        }

        public void Dispose()
        {
            _startupLog.WriteLine("Disposing of page version collection");
            _disposing = true;

            _cleanupThread.Join(200);

            lock (_versions)
            {
                foreach (var versionHead in _versions.Values)
                    versionHead.Dispose();

                _versions.Clear();
            }
        }
        public override string ToString()
        {
            return "version head collection";
        }

        public VersionHead GetVersion(ulong versionNumber)
        {
            lock (_versions)
            {
                if (!_versions.TryGetValue(versionNumber, out VersionHead version))
                {
                    version = new VersionHead(versionNumber);
                    _versions.Add(versionNumber, version);
                }
                return version;
            }
        }

        public VersionHead Add(ulong versionNumber)
        {
            var versionHead = new VersionHead(versionNumber);
            lock (_versions) _versions.Add(versionNumber, versionHead);
            return versionHead;
        }

    }
}