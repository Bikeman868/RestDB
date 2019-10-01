using System;

namespace RestDB.Interfaces.DatabaseLayer
{
    /// <summary>
    /// This is used internally to track information related to a transaction context
    /// </summary>
    public interface ITransaction : IComparable<ITransaction>
    {
        /// <summary>
        /// Returns a unique ID assiciated with this transaction
        /// </summary>
        ulong TransactionId { get; }

        /// <summary>
        /// The database version that as current when this transaction started
        /// </summary>
        ulong BeginVersionNumber { get; }

        /// <summary>
        /// The version number that was allocated to this transaction when it was
        /// committed. Before the transaction commits this has the same value as 
        /// the BeginVersionNumber property
        /// </summary>
        ulong CommitVersionNumber { get; set; }
    }
}