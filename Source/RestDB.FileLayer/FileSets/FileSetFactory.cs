using RestDB.Interfaces.FileLayer;
using System;
using System.Collections.Generic;
using System.Text;

namespace RestDB.FileLayer.FileSets
{
    internal class FileSetFactory : IFileSetFactory
    {
        private readonly IPagePoolFactory _pagePoolFactory;

        public FileSetFactory(IPagePoolFactory pagePoolFactory)
        {
            _pagePoolFactory = pagePoolFactory;
        }

        IFileSet IFileSetFactory.Open(IEnumerable<IDataFile> dataFiles, IEnumerable<ILogFile> logFiles)
        {
            return new FileSet(dataFiles, logFiles, _pagePoolFactory);
        }
    }
}
