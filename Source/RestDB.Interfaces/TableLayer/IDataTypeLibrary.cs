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
        /// Adds a database specific type
        /// </summary>
        void RegisterCustomType(string name, string databaseName, IDataType dataType);

        /// <summary>
        /// Returns a database specific type from its name
        /// </summary>
        IDataType CustomType(string name, string databaseName);

        /// <summary>
        /// Returns a list of all data types available within an application
        /// </summary>
        /// <param name="databaseName">If you pass a non-existant application name 
        /// then only built-in types are returned</param>
        /// <returns></returns>
        IDataType[] AllTypes(string databaseName = null);
    }
}