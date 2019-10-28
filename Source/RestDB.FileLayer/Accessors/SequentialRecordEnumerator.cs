using RestDB.Interfaces.DatabaseLayer;
using RestDB.Interfaces.FileLayer;
using System;
using System.Collections;
using System.Collections.Generic;

namespace RestDB.FileLayer.Accessors
{
    internal class SequentialRecordEnumerator: IEnumerable<PageLocation>, IEnumerator<PageLocation>
    {
        private ISequentialRecordAccessor _accessor;
        private ulong _firstPageNumber;
        private ITransaction _transaction;
        private PageLocation _current;
        private object _indexLocation;

        public SequentialRecordEnumerator(ISequentialRecordAccessor accessor, ulong firstPageNumber, ITransaction transaction)
        {
            _accessor = accessor;
            _firstPageNumber = firstPageNumber;
            _transaction = transaction;
        }

        void IDisposable.Dispose()
        {
        }

        IEnumerator<PageLocation> IEnumerable<PageLocation>.GetEnumerator()
        {
            ((IEnumerator)this).Reset();
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<PageLocation>)this).GetEnumerator();
        }

        PageLocation IEnumerator<PageLocation>.Current => _current;

        object IEnumerator.Current => _current;

        bool IEnumerator.MoveNext()
        {
            if (_current == null)
            {
                if (_indexLocation != null) return false;
                _current = _accessor.LocateFirst(_firstPageNumber, _transaction, out _indexLocation);
            }
            else
                _current = _accessor.LocateNext(_firstPageNumber, _transaction, _indexLocation);

            return _current != null;
        }

        void IEnumerator.Reset()
        {
            _current = null;
            _indexLocation = null;
        }
    }
}
