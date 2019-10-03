using RestDB.Interfaces;
using RestDB.Interfaces.FileLayer;
using System;
using System.Collections.Generic;
using System.Text;

namespace RestDB.FileLayer.Pages
{
    internal class PagePoolFactory : IPagePoolFactory
    {
        readonly IStartUpLog _startUpLog;

        readonly IDictionary<uint, PagePool> _pagePools;

        public PagePoolFactory(IStartUpLog startUpLog)
        {
            _startUpLog = startUpLog;
            _pagePools = new Dictionary<uint, PagePool>();
        }

        IPagePool IPagePoolFactory.Create(uint pageSize)
        {
            lock (_pagePools)
            {
                PagePool pool;
                if (_pagePools.TryGetValue(pageSize, out pool))
                    return pool;

                pool = new PagePool(pageSize, _startUpLog);

                _pagePools.Add(pageSize, pool);

                return pool;
            }
        }
    }
}
