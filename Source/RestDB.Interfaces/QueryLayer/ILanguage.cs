using RestDB.Interfaces.DatabaseLayer;
using System;
using System.Collections.Generic;
using System.Text;

namespace RestDB.Interfaces.QueryLayer
{
    public interface ILanguage
    {
        void Parse(string query, IQueryBuilder compiler);
    }
}
