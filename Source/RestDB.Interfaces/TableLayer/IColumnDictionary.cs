using System;
using System.Collections.Generic;
using System.Text;

namespace RestDB.Interfaces.TableLayer
{
    public interface IColumnDictionary
    {
        IColumnDefinition this[string columnName] { get; }
    }
}
