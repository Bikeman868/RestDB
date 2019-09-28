using System;

namespace RestDB.Interfaces.FileLayer
{
    public class FileLayerException: Exception
    {
        public FileLayerException(string message) : base(message)
        {
        }
    }
}
