using RestDB.Interfaces.FileLayer;
using RestDB.Interfaces.StoredProcedureLayer;
using RestDB.Interfaces.TableLayer;
using System;
using System.Collections.Generic;
using System.Text;

namespace RestDB.Interfaces.DatabaseLayer
{
    public interface IDatabase
    {
        /// <summary>
        /// The name that can be used to refer to this database in query languages
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Returns a list of page stores that contain all of the 
        /// objects in this database. The database itself will be in
        /// one of these page stores. The tables, indexes, procedures etc
        /// can be placed arbitrarily placed in any of the page stores 
        /// associated with the database. The database owns the page store,
        /// it can not be shared accross databases.
        /// </summary>
        IPageStore[] PageStores { get; }

        /// <summary>
        /// Returns a list of the user defined types in this database.
        /// </summary>
        IDataType[] UserDefinedTypes { get; }

        /// <summary>
        /// Returns the tables in this database
        /// </summary>
        ITable[] Tables { get; }

        /// <summary>
        /// Returns the stored procedures and functions in this database
        /// </summary>
        IProcedure[] Procedures { get; }

        /// <summary>
        /// Returns a list of the jobs that are scheduled for this database
        /// </summary>
        IJob[] Jobs { get; }

        /// <summary>
        /// Adds a new page store to the database making it available to 
        /// store objects within this database.
        /// </summary>
        void AddPageStore(IPageStore pageStore);

        /// <summary>
        /// Deletes a page store form the database. Also deletes the files
        /// associated with this page store and all of the tables, indexes,
        /// procedures etc that are contained in this page store.
        /// </summary>
        void DeletePageStore(IPageStore pageStore);

        /// <summary>
        /// Adds a table to this database. You can use the ITableFactory
        /// to create the table prior to adding it to the database. Tables
        /// added to the database will be reloaded automatically when the
        /// database is reopened.
        /// </summary>
        void AddTable(ITable table);

        /// <summary>
        /// Removes this table from the database and deletes all of the
        /// data stored in this table. Also removes all indexes and their
        /// data too.
        /// </summary>
        void DeleteTable(ITable table);

        /// <summary>
        /// Increments the version number of the database and returns
        /// hte incremented value. If called simultaneously by multiple
        /// threads returns a different value to each thread.
        /// </summary>
        ulong IncrementVersion();

        /// <summary>
        /// The current version number of the database. This number is
        /// recorded in transactions that want a consistent view of the
        /// database at a specific revision level.
        /// </summary>
        ulong CurrentVersion { get; }

        /// <summary>
        /// Starts a transaction. The transaction identifies a snapshot of
        /// the database at a point in time, and captures changes that were
        /// made since that time in this transaction so that they can be
        /// applied all at once.
        /// </summary>
        ITransaction BeginTransaction();

        /// <summary>
        /// Commits the changes made within the transaction and makes these
        /// visible to any new transactions that are started after this point.
        /// Transactions that are not committed are rolled back automatically
        /// if the process that owns the transaction fails, or the system crashes
        /// unexpectedly.
        /// </summary>
        void CommitTransaction(ITransaction transaction);

        /// <summary>
        /// Discards all the changes made in the context of a transaction.
        /// These changes are guaranteed not to be applied to the database 
        /// files
        /// </summary>
        void RollbackTransaction(ITransaction transaction);
    }
}
