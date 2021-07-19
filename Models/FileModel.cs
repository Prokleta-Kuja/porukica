using System.IO;

namespace porukica.Models
{
    public class FileModel : TextModel
    {
        public FileModel(string secret, string text, FileInfo fi) : base(secret, text)
        {
            Path = fi.FullName;
            Url = System.IO.Path.Combine(C.DOWNLOAD_DIR, fi.Directory.Name, fi.Name);
            Name = fi.Name;
        }

        public string Path { get; set; }
        public string Url { get; set; }
        public string Name { get; set; }
        public long Size { get; set; }
        public string Hash { get; set; }
    }
}