using System.IO;
using RestDB.Interfaces.FileLayer;

namespace RestDB.FileLayer.DataFiles
{
    internal class DataFileFactory : IDataFileFactory
    {
        IDataFile IDataFileFactory.Create(FileInfo file, uint pageSize)
        {
            return new DataFile(file, pageSize);
        }

        IDataFile IDataFileFactory.Open(FileInfo file)
        {
            return new DataFile(file);
        }
    }
}
