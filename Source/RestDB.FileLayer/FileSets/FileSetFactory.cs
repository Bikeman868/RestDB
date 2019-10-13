using RestDB.Interfaces;
using RestDB.Interfaces.FileLayer;
using System.Collections.Generic;

namespace RestDB.FileLayer.FileSets
{
    internal class FileSetFactory : IFileSetFactory
    {
        private readonly IPagePoolFactory _pagePoolFactory;
        private readonly IStartupLog _startUpLog;

        public FileSetFactory(
            IStartupLog startUpLog,
            IPagePoolFactory pagePoolFactory)
        {
            _startUpLog = startUpLog;
            _pagePoolFactory = pagePoolFactory;
        }

        IFileSet IFileSetFactory.Open(IEnumerable<IDataFile> dataFiles, IEnumerable<ILogFile> logFiles)
        {
            return new FileSet(dataFiles, logFiles, _pagePoolFactory, _startUpLog);
        }
    }
}
