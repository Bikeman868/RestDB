namespace RestDB.Interfaces.FileLayer
{
    public enum LogEntryStatus
    {
        /// <summary>
        /// Indicates that this is the end of the log file
        /// </summary>
        Eof,

        /// <summary>
        /// The system failed whilst writing the log entry and no updates
        /// were made to the data file
        /// </summary>
        LogStarted,

        /// <summary>
        /// The system failed after fully writing the log file entry in this file.
        /// The log entries in other files may or may not have been written.
        /// None of the data files were updated.
        /// </summary>
        LoggedThis,

        /// <summary>
        /// The system failed after fully writing the log file entries into all
        /// log files. At this point the transaction has been fully captured in
        /// log files but may or may not have been applied to the data file.
        /// </summary>
        LoggedAll,

        /// <summary>
        /// The data was written to the log file and this data file. The changes
        /// may or may not have been applied to the other data files.
        /// </summary>
        CompleteThis
    }
}