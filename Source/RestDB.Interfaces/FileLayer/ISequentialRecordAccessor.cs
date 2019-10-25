using RestDB.Interfaces.DatabaseLayer;
using System;
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
        /// Makes space for a record of the specififed size and returns information about 
        /// where to write the record data.
        /// </summary>
        /// <param name="objectType">See GetFirstIndexPage method of IPageStore</param>
        /// <param name="transaction">The transaction context for updating the index</param>
        /// <param name="recordSize">The number of bytes required to hold this records data</param>
        /// <returns>A data structure that says where to write the data for this record. If the
        /// record spans multiple pages then the additional pages are returned and these can be
        /// filled starting at offset 0. The first page and the offset to start writing on that
        /// page are returned as properties of the returned object</returns>
        PageLocation Append(
            ushort objectType,
            ITransaction transaction,
            ulong recordSize);

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

    public static class ISequentialRecordAccessorExtensions
    {
        /// <summary>
        /// After calling Append you can call this method to write the record data. This is convenient
        /// if the record data is already in a single byte array.
        /// </summary>
        /// <param name="transaction">The transaction context for updating the pages</param>
        /// <param name="location">The location to write to as returned from the Append method</param>
        /// <param name="record">The array of bytes to write. This must be the same size as the
        /// length property of the location parameter</param>
        public static void Write(
            this ISequentialRecordAccessor accessor,
            ITransaction transaction,
            PageLocation location,
            byte[] record)
        {
            if (location.Length != (uint)record.LongLength)
                throw new FileLayerException("Incorrect record length writing to sequential accessor");

            var pageSize = location.PageStore.PageSize;
            var bytesRemaining = (ulong)record.LongLength;
            var recordOffset = 0UL;
            var updates = new List<PageUpdate>();
            var sequence = 1U;
            var pageIndex = -1;

            while (bytesRemaining > 0)
            {
                if (pageIndex >= 0)
                {
                    var bytesToCopy = bytesRemaining < pageSize
                        ? bytesRemaining
                        : pageSize;

                    var buffer = new byte[bytesToCopy];
                    Array.Copy(record, (long)recordOffset, buffer, 0, (long)bytesToCopy);

                    updates.Add(
                        new PageUpdate
                        {
                            SequenceNumber = sequence++,
                            PageNumber = location.ContunuationPages[pageIndex],
                            Offset = 0U,
                            Data = buffer
                        });

                    bytesRemaining -= bytesToCopy;
                    recordOffset += bytesToCopy;
                }
                else if (location.Offset + bytesRemaining > pageSize)
                {
                    var bytesToCopy = pageSize - location.Offset;

                    var buffer = new byte[bytesToCopy];
                    Array.Copy(record, 0, buffer, 0, buffer.LongLength);

                    updates.Add(
                        new PageUpdate
                        {
                            SequenceNumber = sequence++,
                            PageNumber = location.PageNumber,
                            Offset = location.Offset,
                            Data = buffer
                        });

                    bytesRemaining -= bytesToCopy;
                    recordOffset = bytesToCopy;
                }
                else
                {
                    updates.Add(
                        new PageUpdate
                        {
                            SequenceNumber = sequence++,
                            PageNumber = location.PageNumber,
                            Offset = location.Offset,
                            Data = record
                        });

                    bytesRemaining = 0;
                }

                pageIndex++;
            }

            location.PageStore.Update(transaction, updates);
        }

        /// <summary>
        /// After calling Append you can call this method to write the record data. This is convenient
        /// if the record data is already in a single byte array.
        /// </summary>
        /// <param name="transaction">The transaction context for updating the pages</param>
        /// <param name="location">The location to write to as returned from the Append method</param>
        /// <param name="recordPieces">The chunks of data to write into the spece reserved for the record. 
        /// The lengths of these chuncks must add up to the record length in the location parameter</param>
        public static void Write(
            this ISequentialRecordAccessor accessor,
            ITransaction transaction,
            PageLocation location,
            IEnumerable<byte[]> recordPieces)
        {
            throw new NotImplementedException();
        }
    }
}
