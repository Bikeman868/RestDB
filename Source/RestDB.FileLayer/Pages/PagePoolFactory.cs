using RestDB.Interfaces.FileLayer;
using System;
using System.Collections.Generic;
using System.Text;

namespace RestDB.FileLayer.Pages
{
    internal class PagePoolFactory : IPagePoolFactory
    {
        IPagePool IPagePoolFactory.Create(uint pageSize)
        {
            return new PagePool(pageSize);
        }
    }
}
