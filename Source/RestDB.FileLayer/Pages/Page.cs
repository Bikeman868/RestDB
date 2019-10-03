using RestDB.Interfaces.FileLayer;
using System;
using System.Threading;

namespace RestDB.FileLayer.Pages
{
    internal class Page : IPage
    {
        ulong IPage.PageNumber => _pageNumber;
        byte[] IPage.Data => _data;

        private int _referenceCount;
        private ulong _pageNumber;
        private byte[] _data;
        private readonly Action<Page> _dispose;

        public Page(uint pageSize, Action<Page> dispose)
        {
            _data = new byte[pageSize];
            _dispose = dispose;
        }

        public override string ToString()
        {
            return "page " + _pageNumber + " containing " + _data.Length + " bytes";
        }

        public IPage Initialize(ulong pageNumber, bool clear)
        {
            _pageNumber = pageNumber;

            if (clear)
                _data.Initialize();

            _referenceCount = 1;

            return this;
        }

        void IDisposable.Dispose()
        {
            if (Interlocked.Decrement(ref _referenceCount) == 0)
                _dispose(this);
        }

        IPage IPage.Reference()
        {
            Interlocked.Increment(ref _referenceCount);
            return this;
        }
    }
}
