using RestDB.Interfaces.FileLayer;
using System;
using System.Collections.Generic;
using System.Text;

namespace RestDB.FileLayer.FileSets
{
    internal class FileSetFactory : IFileSetFactory
    {
        IFileSet IFileSetFactory.Open(IDataFile dataFile, ILogFile logFile)
        {
            return new FileSet(dataFile, logFile);
        }
    }
}
