using RestDB.Interfaces.FileLayer;
using System;
using System.Collections.Generic;
using System.Text;

namespace RestDB.Interfaces.TableLayer
{
    public interface ITableFactory
    {
        /// <summary>
        /// Opens an existing table by loading it from a page store
        /// </summary>
        /// <param name="name">The name of the table</param>
        /// <param name="pageStore">The page store containing this table</param>
        ITable Open(string name, IPageStore pageStore);

        /// <summary>
        /// Creates a new table and saves it into a page store
        /// </summary>
        /// <param name="name">The name of the table to create</param>
        /// <param name="pageStore">The page store to save this table to</param>
        ITable Create(string name, IPageStore pageStore);
    }
}
