namespace RestDB.Interfaces.FileLayer
{
    /// <summary>
    /// POCO class for passing a location within pages storage
    /// </summary>
    public class PageLocation
    {
        /// <summary>
        /// The page number of this location
        /// </summary>
        public ulong PageNumber;

        /// <summary>
        /// The offset into this page in bytes
        /// </summary>
        public uint Offset;

        /// <summary>
        /// The length of the data at this offset
        /// </summary>
        public uint Length;
    }
}
