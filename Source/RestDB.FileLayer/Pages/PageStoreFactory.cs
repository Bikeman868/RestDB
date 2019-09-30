using RestDB.Interfaces.FileLayer;
using System;
using System.Collections.Generic;
using System.Text;

namespace RestDB.FileLayer.Pages
{
    internal class PageStoreFactory : IPageStoreFactory
    {
        readonly IVersionedPageCache _pageCache;

        public PageStoreFactory(IVersionedPageCache pageCache)
        {
            _pageCache = pageCache;
        }

        IPageStore IPageStoreFactory.Open(IFileSet fileSet)
        {
            return new PageStore(_pageCache);
        }
    }
}
