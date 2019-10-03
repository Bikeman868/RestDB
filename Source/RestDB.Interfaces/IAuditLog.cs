using System;
using System.Collections.Generic;
using System.Text;

namespace RestDB.Interfaces
{
    /// <summary>
    /// Defines a mechansim to record all meta data changes for audit trial purposes
    /// </summary>
    public interface IAuditLog
    {
        IAuditLog Write(DateTime when, string who, string what, string databaseName, string objectType, string objectName);
    }
}
