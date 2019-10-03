using RestDB.Interfaces;
using RestDB.Interfaces.FileLayer;
using System;
using System.Collections.Generic;
using System.Text;

namespace RestDB.FileLayer.Pages
{
    internal class VersionedPageCacheFactory : IVersionedPageCacheFactory
    {
        readonly IPagePoolFactory _pagePoolFactory;
        readonly IStartUpLog _startUpLog;

        public VersionedPageCacheFactory(
            IPagePoolFactory pagePoolFactory,
            IStartUpLog startUpLog)
        {
            _pagePoolFactory = pagePoolFactory;
            _startUpLog = startUpLog;
        }

        IVersionedPageCache IVersionedPageCacheFactory.Create(IFileSet fileSet)
        {
            return new VersionedPageCache(fileSet, _pagePoolFactory, _startUpLog);
        }
    }
}
