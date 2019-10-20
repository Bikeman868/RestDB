﻿using RestDB.Interfaces.DatabaseLayer;
using System.Collections.Generic;

namespace RestDB.Interfaces.FileLayer
{
    /// <summary>
    ///  Provides methods for manipulating a list of variable length records that
    ///  are stored sequentially in an IPageStore. This is used for managing lists
    ///  of databases, tables  indexes, user defined types etc.
    /// </summary>
    public interface ISequentialRecordAccessor
    {
        /// <summary>
        /// Returns an enumerator for a spcific type of object in the context of a transaction
        /// </summary>
        /// <param name="objectType"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        IEnumerable<PageLocation> Enumerate(ushort objectType, ITransaction transaction);

        /// <summary>
        /// Locates the first object in the list
        /// </summary>
        /// <param name="objectType">See GetFirstIndexPage method of IPageStore</param>
        /// <param name="transaction">The transaction context for reading the index</param>
        /// <param name="indexLocation">Returns the location of the first record in the index 
        /// of records</param>
        /// <returns>The location of the first record or null if the list is empty</returns>
        PageLocation LocateFirst(
            ushort objectType, 
            ITransaction transaction, 
            out object indexLocation);

        /// <summary>
        /// Locates the next object in the list
        /// </summary>
        /// <param name="objectType">See GetFirstIndexPage method of IPageStore</param>
        /// <param name="transaction">The transaction context for reading the index</param>
        /// <param name="indexLocation">The location of the current record in the index 
        /// of records</param>
        /// <returns>The location of the next record or null if this is the end of the list</returns>
        PageLocation LocateNext(
            ushort objectType,
            ITransaction transaction,
            object indexLocation);

        /// <summary>
        /// Empties the list. To rewrite the list (for example in a different order,
        /// enumerate the whole list and save it then clear the list, then append the
        /// records in the desired sequence
        /// </summary>
        /// <param name="objectType">See GetFirstIndexPage method of IPageStore</param>
        /// <param name="transaction">The transaction context for updating the index</param>
        void Clear(ushort objectType, ITransaction transaction);

        /// <summary>
        /// Adds a new record to the end of the list
        /// </summary>
        /// <param name="objectType">See GetFirstIndexPage method of IPageStore</param>
        /// <param name="transaction">The transaction context for updating the index</param>
        /// <param name="record">The data to append to the list</param>
        PageLocation Append(
            ushort objectType,
            ITransaction transaction,
            byte[] record);

        /// <summary>
        /// Deletes a record from the list. To replace an existing record delete the
        /// current record and append a new one.
        /// </summary>
        /// <param name="objectType">See GetFirstIndexPage method of IPageStore</param>
        /// <param name="transaction">The transaction context for updating the index</param>
        /// <param name="indexLocation">The location of the record to delete in the 
        /// index of records</param>
        void Delete(
            ushort objectType,
            ITransaction transaction,
            object indexLocation);
    }
}
