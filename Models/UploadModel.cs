using System;
using System.IO;

namespace porukica.Models
{
    public class UploadModel
    {
        public UploadModel(TimeSpan expireAfter, FileInfo fi)
        {
            Path = fi.FullName;
            Url = System.IO.Path.Combine(C.DOWNLOAD_DIR, fi.Directory.Name, fi.Name);
            Name = fi.Name;
            ExpireAfter = expireAfter;
        }

        public string Path { get; set; }
        public string Url { get; set; }
        public string Name { get; set; }
        public long Size { get; set; }
        public TimeSpan ExpireAfter { get; set; }
    }
}