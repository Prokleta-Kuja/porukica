using System;

namespace porukica
{
    public static class C
    {
        public const long MAX_FILE_SIZE = 1024 * 1024 * 1024;
        public const long UPLOAD_BUFFER_SIZE = 1024 * 32; // X KB
        public const string UPLOAD_DIR = "wwwroot/uploads";
        public const string DOWNLOAD_DIR = "uploads";

        public static string BytesToString(long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString() + suf[place];
        }
    }
}