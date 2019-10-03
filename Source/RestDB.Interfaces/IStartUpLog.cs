using System;
using System.Collections.Generic;
using System.Text;

namespace RestDB.Interfaces
{
    /// <summary>
    /// Defines an interface to a logging mechanism that captures a detailed log
    /// of startup and shutdown events. Includes the files that were opened, the state
    /// of those files, transactions that were rolled back or forward at stratup etc.
    /// </summary>
    public interface IStartUpLog
    {
        IStartUpLog Write(string message, bool isError = false);
    }
}
