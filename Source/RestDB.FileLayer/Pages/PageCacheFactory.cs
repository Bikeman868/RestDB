using RestDB.Interfaces;
using RestDB.Interfaces.DatabaseLayer;
using RestDB.Interfaces.FileLayer;

namespace RestDB.FileLayer.Pages
{
    internal class PageCacheFactory : IPageCacheFactory
    {
        private readonly IPagePoolFactory _pagePoolFactory;
        private readonly IStartupLog _startUpLog;
        private readonly IErrorLog _errorLog;

        public PageCacheFactory(
            IPagePoolFactory pagePoolFactory,
            IStartupLog startUpLog,
            IErrorLog errorLog)
        {
            _pagePoolFactory = pagePoolFactory;
            _startUpLog = startUpLog;
            _errorLog = errorLog;
        }

        IPageCache IPageCacheFactory.Create(IDatabase database, IFileSet fileSet)
        {
            return new PageCache(fileSet, database, _pagePoolFactory, _startUpLog, _errorLog);
        }
    }
}
