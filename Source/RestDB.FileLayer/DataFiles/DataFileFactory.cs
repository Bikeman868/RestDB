using System.IO;
using RestDB.Interfaces;
using RestDB.Interfaces.FileLayer;

namespace RestDB.FileLayer.DataFiles
{
    internal class DataFileFactory : IDataFileFactory
    {
        readonly IStartUpLog _startupLog;

        public DataFileFactory(IStartUpLog startupLog)
        {
            _startupLog = startupLog;
        }

        IDataFile IDataFileFactory.Create(FileInfo file, uint pageSize)
        {
            return new DataFile(file, pageSize, _startupLog);
        }

        IDataFile IDataFileFactory.Open(FileInfo file)
        {
            return new DataFile(file, _startupLog);
        }
    }
}
