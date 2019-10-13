using RestDB.Interfaces;
using RestDB.Interfaces.FileLayer;
using System;
using System.Collections.Generic;
using System.Text;

namespace RestDB.FileLayer.Pages
{
    internal class PageStoreFactory : IPageStoreFactory
    {
        readonly IPageCache _pageCache;
        readonly IStartupLog _startUpLog;

        public PageStoreFactory(
            IPageCache pageCache,
            IStartupLog startUpLog)
        {
            _pageCache = pageCache;
            _startUpLog = startUpLog;
        }

        IPageStore IPageStoreFactory.Open(IFileSet fileSet)
        {
            return new PageStore(_pageCache, _startUpLog);
        }
    }
}
