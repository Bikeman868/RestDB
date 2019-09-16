using RestDB.Interfaces.FileLayer;
using RestDB.Interfaces.StoredProcedureLayer;
using RestDB.Interfaces.TableLayer;
using System;
using System.Collections.Generic;
using System.Text;

namespace RestDB.Interfaces.DatabaseLayer
{
    public interface IDatabaseFactory
    {
        /// <summary>
        /// Creates an obejct that provides access to an existing database.
        /// The database will open all of the other page stores associated
        /// with the database and load all of the tables, indexes, stored
        /// procedures etc associated with the database.
        /// </summary>
        /// <param name="pageStore">The page store that contains this
        /// database. The page store can contain only the database, or
        /// it can contain some/all of the objects that are contained in
        /// this database.
        /// A page store can only hols one database, and all objects
        /// in a page store must belog to the same database</param>
        IDatabase Open(IPageStore pageStore);

        /// <summary>
        /// Creates a new empty database. After calling this method you
        /// can call methods on the database object to add tables, indexes,
        /// stored procedures etc to the database.
        /// </summary>
        /// <param name="name">The name of this database</param>
        /// <param name="pageStore">The pagestore to craete this
        /// database in. This page store must be empty, it can not
        /// already contain objects from another database</param>
        IDatabase Create(string name, IPageStore pageStore);
    }
}
