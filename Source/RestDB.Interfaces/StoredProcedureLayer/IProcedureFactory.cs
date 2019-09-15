using RestDB.Interfaces.FileLayer;
using System;
using System.Collections.Generic;
using System.Text;

namespace RestDB.Interfaces.StoredProcedureLayer
{
    public interface IProcedureFactory
    {
        /// <summary>
        /// Opens an existing procedure by loading it from a page store
        /// </summary>
        /// <param name="name">The name of the procedure</param>
        /// <param name="pageStore">The page store containing this procedure</param>
        IProcedure Open(string name, IPageStore pageStore);

        /// <summary>
        /// Creates a new procedure and saves it into a page store
        /// </summary>
        /// <param name="name">The name of the procedure to create</param>
        /// <param name="pageStore">The page store to save this procedure to</param>
        IProcedure Create(string name, IPageStore pageStore);
    }
}
