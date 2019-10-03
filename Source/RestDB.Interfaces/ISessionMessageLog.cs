using System;
using System.Collections.Generic;
using System.Text;

namespace RestDB.Interfaces
{
    /// <summary>
    /// Message log that is specific to the user session and is returned
    /// to the user who executed the request to provide information about the
    /// execution of their request
    /// </summary>
    public interface ISessionMessageLog
    {
        ISessionMessageLog Write(string message);
    }
}
