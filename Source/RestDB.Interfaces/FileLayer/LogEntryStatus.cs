namespace RestDB.Interfaces.FileLayer
{
    public enum LogEntryStatus
    {
        /// <summary>
        /// The system failed whilst writing the log entry and no updates
        /// were made to the data file
        /// </summary>
        NotStarted,

        /// <summary>
        /// The system failed after fully writing the log file entry. The data
        /// file may or may not have been updated.
        /// </summary>
        Failed,

        /// <summary>
        /// The data was written to the log file and the data file. No recovery is needed.
        /// </summary>
        Completed
    }
}