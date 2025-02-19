using System;

namespace S3FileCleanup.Shared.Models
{
    public class FileMetadata
    {
        public string FileName { get; set; }
        public string BucketName { get; set; }
        public string Key { get; set; }
        public DateTime EventDate { get; set; }
    }
}
