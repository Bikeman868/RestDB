using System;
using System.Collections.Generic;
using System.Text;

namespace RestDB.Interfaces.TableLayer
{
    public interface ITableDictionary
    {
        ITable this[string indexName] { get; }
    }
}
