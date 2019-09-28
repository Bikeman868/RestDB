using System;

namespace RestDB.Interfaces.FileLayer
{
    public class UnsupportedVersionException: FileLayerException
    {
        public uint VersionNumber { get; set; }
        public uint HighestSupportedVersionNumber { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; }

        public UnsupportedVersionException(
            uint versionNumber,
            uint highestSupportedVersionNumber,
            string fileType,
            string fileName) 
            : base(fileType + " " + fileName + " was written by a newer version of the software and can not be read by this version. "+
                  "The higest supported version number is " + highestSupportedVersionNumber + " but this file has a version number of " + versionNumber)
        {
        }
    }
}
