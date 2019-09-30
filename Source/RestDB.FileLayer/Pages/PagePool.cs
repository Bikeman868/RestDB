using OwinFramework.Utility.Containers;
using RestDB.Interfaces.FileLayer;
using System;
using System.Text;

namespace RestDB.FileLayer.Pages
{
    internal class PagePool : IPagePool
    {
        readonly uint _pageSize;
        readonly LinkedList<Page> _pages;

        uint IPagePool.PageSize => _pageSize;

        public PagePool(uint pageSize)
        {
            _pageSize = pageSize;
            _pages = new LinkedList<Page>();
        }

        IPage IPagePool.Get(ulong pageNumber, bool clear)
        {
            var page = _pages.PopLast();

            if (page == null)
                page = new Page(_pageSize, p => _pages.Append(p));

            return page.Initialize(pageNumber, clear);
        }
    }
}
