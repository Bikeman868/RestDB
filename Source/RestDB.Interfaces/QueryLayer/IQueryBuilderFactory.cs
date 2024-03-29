﻿using RestDB.Interfaces.DatabaseLayer;

namespace RestDB.Interfaces.QueryLayer
{
    public interface IQueryBuilderFactory
    {
        /// <summary>
        /// Constructs a new query compiler. The compiler provides a
        /// fluid syntax for defining what the query does when you run it.
        /// Call the Compile() method of the compiler to build a query.
        /// </summary>
        /// <param name="database">The database to use for resolving
        /// names into obejcts such as tables and stored procedures etc</param>
        IQueryBuilder Create(IDatabase database);
    }
}