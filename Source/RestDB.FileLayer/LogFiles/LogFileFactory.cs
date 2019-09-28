using RestDB.Interfaces.FileLayer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RestDB.FileLayer.LogFiles
{
    internal class LogFileFactory : ILogFileFactory
    {
        ILogFile ILogFileFactory.Open(FileInfo file, bool initialize)
        {
            return new LogFile(file, initialize);
        }
    }
}
