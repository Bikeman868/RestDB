using System;
using System.Collections.Generic;
using System.Text;

namespace RestDB.Interfaces.TableLayer
{
    /// <summary>
    /// Catalog of all supported data types - includes custom
    /// application defined types.
    /// </summary>
    public interface IDataTypeLibrary
    {
        /// <summary>
        /// Returns a built-in data type definition from its name
        /// </summary>
        IDataType BuiltInType(string name);

        /// <summary>
        /// Returns a built-in data type definition from its name the maximum length
        /// </summary>
        IDataType BuiltInType(string name, short maxLength);

        /// <summary>
        /// Returns an application specific type from its name
        /// </summary>
        void RegisterCustomType(string name, string applicationName, IDataType dataType);

        /// <summary>
        /// Returns an application specific type from its name
        /// </summary>
        IDataType CustomType(string name, string applicationName);

        /// <summary>
        /// Returns a list of all data types available within an application
        /// </summary>
        /// <param name="applicationName">If you pass a non-existant application name 
        /// then only built-in types are returned</param>
        /// <returns></returns>
        IDataType[] AllTypes(string applicationName = null);
    }
}
