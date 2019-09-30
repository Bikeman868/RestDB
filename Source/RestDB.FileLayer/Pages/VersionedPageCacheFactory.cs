using RestDB.Interfaces.FileLayer;
using System;
using System.Collections.Generic;
using System.Text;

namespace RestDB.FileLayer.Pages
{
    internal class VersionedPageCacheFactory : IVersionedPageCacheFactory
    {
        readonly IPagePoolFactory _pagePoolFactory;

        public VersionedPageCacheFactory(IPagePoolFactory pagePoolFactory)
        {
            _pagePoolFactory = pagePoolFactory;
        }


        IVersionedPageCache IVersionedPageCacheFactory.Create(IFileSet fileSet)
        {
            return new VersionedPageCache(fileSet, _pagePoolFactory);
        }
    }
}
