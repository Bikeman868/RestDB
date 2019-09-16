using System;
using System.Collections.Generic;
using System.Text;

namespace RestDB.Interfaces
{
    /// <summary>
    /// This is used internally to track information related to a transaction context
    /// </summary>
    public interface ITransaction
    {
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
