using RestDB.Interfaces;
using RestDB.Interfaces.FileLayer;

namespace RestDB.FileLayer.Pages
{
    internal class VersionedPageCacheFactory : IVersionedPageCacheFactory
    {
        private readonly IPagePoolFactory _pagePoolFactory;
        private readonly IStartUpLog _startUpLog;
        private readonly IErrorLog _errorLog;

        public VersionedPageCacheFactory(
            IPagePoolFactory pagePoolFactory,
            IStartUpLog startUpLog,
            IErrorLog errorLog)
        {
            _pagePoolFactory = pagePoolFactory;
            _startUpLog = startUpLog;
            _errorLog = errorLog;
        }

        IVersionedPageCache IVersionedPageCacheFactory.Create(IFileSet fileSet)
        {
            return new VersionedPageCache(fileSet, _pagePoolFactory, _startUpLog, _errorLog);
        }
    }
}
