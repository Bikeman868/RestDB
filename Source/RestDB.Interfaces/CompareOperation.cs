using System;
using System.Collections.Generic;
using System.Text;

namespace RestDB.Interfaces
{
    [Flags]
    public enum CompareOperation
    {
        /// <summary>
        /// The index supports testing for null values
        /// </summary>
        IsNull = 0x1,

        /// <summary>
        /// The index supports equality comparing
        /// </summary>
        Equal = 0x2,

        /// <summary>
        /// The index supports less than queries
        /// </summary>
        Less = 0x4,

        /// <summary>
        /// The index supports greater than queries
        /// </summary>
        Greater = 0x8,

        /// <summary>
        /// The index supports greater than or equals queries
        /// </summary>
        NotLess = 0x10,

        /// <summary>
        /// The index supports less than or equals queries
        /// </summary>
        NotGreater = 0x20,

        /// <summary>
        /// The index supports similar value queries
        /// </summary>
        Similar = 0x40,

        /// <summary>
        /// The index supports subset queries
        /// </summary>
        Contains = 0x80,

        /// <summary>
        /// The index supports within queries
        /// </summary>
        Range = 0x100,

        /// <summary>
        /// Selects all comparison operations
        /// </summary>
        All = 0x1ff,

        /// <summary>
        /// Selects comparison operations that are typically available
        /// for numeric fields
        /// </summary>
        Number = 0x13f,

        /// <summary>
        /// Selects comparison operations that are typically available
        /// for text fields
        /// </summary>
        Text = 0xff
    }
}
