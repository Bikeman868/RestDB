using OwinFramework.Utility.Containers;
using RestDB.Interfaces;
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

        public PagePool(uint pageSize, IStartupLog startUpLog)
        {
            startUpLog.WriteLine("Creating a page pool for " + pageSize + " byte pages");

            _pageSize = pageSize;
            _pages = new LinkedList<Page>();
        }

        public override string ToString()
        {
            return "page pool for " + _pageSize + " byte pages";
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
