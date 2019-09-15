using System;
using System.Collections.Generic;
using System.Text;

namespace RestDB.Interfaces.TableLayer
{
    public interface IColumn
    {
        /// <summary>
        /// Defines how this column works
        /// </summary>
        IColumnDefinition Definition { get; }

        /// <summary>
        /// Returns the byte offset into the row data buffer where this
        /// column data resides
        /// </summary>
        ushort Offset { get; }
    }
}
