namespace RestDB.Interfaces.FileLayer
{
    /// <summary>
    /// POCO class that is used to capture a range of bytes that were modified on a page
    /// in a page.
    /// </summary>
    public class PageUpdate
    {
        /// <summary>
        /// Defines the order in which updates were written
        /// </summary>
        public uint SequenceNumber;

        /// <summary>
        /// The page number of the page to change
        /// </summary>
        public ulong PageNumber;

        /// <summary>
        /// The byte offset into the page where the modification starts
        /// </summary>
        public uint Offset;

        /// <summary>
        /// The bytes of data to write into the page
        /// </summary>
        public byte[] Data;
    }
}