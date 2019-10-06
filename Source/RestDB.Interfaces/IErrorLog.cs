using System;
using System.Collections.Generic;
using System.Text;

namespace RestDB.Interfaces
{
    /// <summary>
    /// Defines an interface for logging errors - permissions problems, corrupt files, out of disk space etc
    /// </summary>
    public interface IErrorLog
    {
        IErrorLog Write(string message, Exception exception = null);
    }
}
