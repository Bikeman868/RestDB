using System;
using System.Collections.Generic;
using System.Text;

namespace RestDB.Interfaces.TableLayer
{
    public interface IIndexDictionary
    {
        IIndex this[string indexName] { get; }
    }
}
