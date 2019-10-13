using RestDB.Interfaces.DatabaseLayer;

namespace RestDB.Interfaces.FileLayer
{
    /// <summary>
    ///  Provides methods for manipulating a large set of records that
    ///  can be randomly accessed in the IPageStore. This is used for 
    ///  storing indexs and table rows or columns
    /// </summary>
    public interface IRandomRecordAccessor
    {
        /// <summary>
        /// Returns the number of records of this type in the page store that have not been deleted
        /// </summary>
        /// <param name="objectType">See GetFirstIndexPage method of IPageStore</param>
        /// <param name="transaction">The transaction context for accessing the page store</param>
        ulong Count(ushort objectType, ITransaction transaction);

        /// <summary>
        /// Returns the highest record number in the page store + 1. The next call to Allocate will return
        /// this value as the record number.
        /// </summary>
        /// <param name="objectType">See GetFirstIndexPage method of IPageStore</param>
        /// <param name="transaction">The transaction context for accessing the page store</param>
        ulong NextRecordNumber(ushort objectType, ITransaction transaction);

        /// <summary>
        /// Deletes all of the records in the page set
        /// </summary>
        /// <param name="objectType">See GetFirstIndexPage method of IPageStore</param>
        /// <param name="transaction">The transaction context for accessing the page store</param>
        void Clear(ushort objectType, ITransaction transaction);

        /// <summary>
        /// Gets the location of a record in the page store
        /// </summary>
        /// <param name="objectType">See GetFirstIndexPage method of IPageStore</param>
        /// <param name="transaction">The transaction context for reading the page store</param>
        /// <param name="recordNumber">The zero based index of the record to return</param>
        /// <returns>The location of the first byte of the record and the record length. If the
        /// offset + length is greater than the page size then you must read multiple pages
        /// to retrieve the entire record.
        /// If this is a deleted record, or the record number is too large then null is returned</returns>
        PageLocation Locate(
            ushort objectType, 
            ITransaction transaction, 
            ulong recordNumber);

        /// <summary>
        /// Allocates space for a record in the page store
        /// </summary>
        /// <param name="objectType">See GetFirstIndexPage method of IPageStore</param>
        /// <param name="transaction">The transaction context for accessing the page store</param>
        /// <param name="recordNumber">The zero based index of the record to return</param>
        /// <param name="recordLength">The number of bytes to allocate. If this is a 
        /// fixed length accessor then this parameter is ignored</param>
        /// <returns>The location of the first byte of the record and the record length. If the
        /// offset + length is greater than the page size then you must write into multiple pages
        /// to store the entire record</returns>
        PageLocation Allocate(
            ushort objectType,
            ITransaction transaction,
            out uint recordNumber,
            uint recordLength);

        /// <summary>
        /// Reduces the amount of space reserved for a record. If this is a fixed length
        /// accessor then this does nothing
        /// </summary>
        /// <param name="objectType">See GetFirstIndexPage method of IPageStore</param>
        /// <param name="transaction">The transaction context for accessing the page store</param>
        /// <param name="recordNumber">The zero based index of the record to return</param>
        /// <param name="recordLength">The new byte size of the record</param>
        PageLocation Resize(
            ushort objectType,
            ITransaction transaction,
            out uint recordNumber,
            uint recordLength);

        /// <summary>
        /// Deletes a record from the page store possibly freeing up the space it occupied
        /// depending on what type of accessor is implementing the interface.
        /// </summary>
        /// <param name="objectType">See GetFirstIndexPage method of IPageStore</param>
        /// <param name="transaction">The transaction context for updating the index</param>
        /// <param name="recordNumber">The zero based index of the record to return</param>
        void Delete(
            ushort objectType,
            ITransaction transaction,
            uint recordNumber);
    }
}
