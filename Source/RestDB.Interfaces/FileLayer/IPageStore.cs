using System;

namespace RestDB.Interfaces.FileLayer
{
    /// <summary>
    /// Provides resilient versioned persistent storage of pages of
    /// data where each page is the same size. The pages will be stored
    /// using a pair of files to ensure that partial writes can be recovered
    /// on restart after a system failure. This class keeps track of the
    /// pages that are in use or available in the page store. Page zero contains
    /// an index of indexes. Each index provides references to pages that contain
    /// all of the objects of a specific type. For example by looking at page 0
    /// I can find the page number of the Stored Procedure index, then I can read 
    /// this page to get a list of the Stored Procedures in this page store.
    /// </summary>
    public interface IPageStore: IPageCache
    {
        /// <summary>
        /// Returns the first page in a chain of pages that contains an index of 
        /// pages for a specific type of object. The application can structure 
        /// this page however it likes including references to continuation pages 
        /// allowing for unlimited size indexes.
        /// </summary>
        /// <param name="objectType">Page 0 of every data file contains a table
        /// of starting index pages for each object type. This function returns those
        /// index pages by object type number. The following object type numbers
        /// are reserved:
        /// 0     The used/free page map for the data file
        /// 1     List of user-defined types
        /// 2     List of table definitions
        /// 3     List of index definitions
        /// 4     List of views/projections
        /// 5     List of Stored procedures
        /// 6     List of scheduled jobs
        /// 7-127 Reserved for later developments
        /// 128+  Application defined</param>
        /// <returns>The first page of data that contains the index of objects
        /// of a particular type</returns>
        ulong GetFirstIndexPage(ushort objectType);

        /// <summary>
        /// Allocates a new page in the page store and returns its page number
        /// </summary>
        ulong Allocate();

        /// <summary>
        /// Marks a page as available for reuse. This page
        /// could be returned immediately on the next call to 
        /// Allocate()
        /// </summary>
        void Release(ulong pageNumber);
    }
}