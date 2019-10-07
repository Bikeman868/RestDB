using RestDB.Interfaces.FileLayer;
using RestDB.Interfaces.StoredProcedureLayer;
using RestDB.Interfaces.TableLayer;

namespace RestDB.Interfaces.DatabaseLayer
{
    public interface IDatabase
    {
        /// <summary>
        /// The name that can be used to refer to this database in query languages
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Waits for all pending transactions to be committed or rolled back
        /// then flushes all updates to the database files and closes those files.
        /// After closing the database no new transactions can be started.
        /// </summary>
        void Close();

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
        IDataType[] DataTypes { get; }

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
        /// Retieve tables by name
        /// </summary>
        ITableDictionary Table { get; }

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
        /// Adds a custom data type to this database.
        /// </summary>
        void AddDataType(IDataType dataType);

        /// <summary>
        /// Removes this data type from the database. You should only
        /// do this if there are no tables that have columns that reference
        /// this data type.
        /// </summary>
        void DeleteDataType(IDataType dataType);

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
        /// Adds a procedure to this database. The procedure name must be
        /// unique within this database
        /// </summary>
        void AddProcedure(IProcedure procedure);

        /// <summary>
        /// Removes this procedure from the database.
        /// </summary>
        void DeleteProcedure(IProcedure procedure);

        /// <summary>
        /// Adds a procedure to this database. The procedure name must be
        /// unique within this database
        /// </summary>
        void AddJob(IJob job);

        /// <summary>
        /// Removes this job from the database.
        /// </summary>
        void DeleteJob(IJob job);

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
        /// Increments the CurrentVersion only after the transaction has updated 
        /// all the page caches.
        /// Only one transaction at a time can commit, other threads will block
        /// until the commit completes and the version number is updated. The
        /// writing to the log file is async, so this part does not block.
        /// </summary>
        void CommitTransaction(ITransaction transaction);

        /// <summary>
        /// Discards all the changes made in the context of a transaction.
        /// These changes are guaranteed not to be applied to the database 
        /// files.
        /// </summary>
        void RollbackTransaction(ITransaction transaction);
    }
}