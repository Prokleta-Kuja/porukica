using System;

namespace porukica
{
    public class Settings
    {
        public string AUTHORIZATION_TEXT { get; set; }
        public string AUTHORIZATION_FILE { get; set; } = "let me upload";
        public int MAX_TIMEOUT_H { get; set; } = 24;
        public int? MAX_TIMEOUT_M { get; set; }
        public int? MAX_TIMEOUT_S { get; set; }
        public int MAX_FILE_SIZE_MB { get; set; } = 1;
        public int UPLOAD_TIMEOUT_M { get; set; } = 45;

        public bool ValidAuthorizationText(string auth)
            => string.IsNullOrWhiteSpace(AUTHORIZATION_TEXT) || AUTHORIZATION_TEXT.Equals(auth);
        public bool ValidAuthorizationFile(string auth)
            => string.IsNullOrWhiteSpace(AUTHORIZATION_FILE) || AUTHORIZATION_FILE.Equals(auth);
        public bool ValidTimeout(TimeSpan ts) => ts <= MaxTimeout;
        public TimeSpan MaxTimeout => MAX_TIMEOUT_S.HasValue
                ? TimeSpan.FromSeconds(MAX_TIMEOUT_S.Value)
                : MAX_TIMEOUT_M.HasValue
                    ? TimeSpan.FromMinutes(MAX_TIMEOUT_M.Value)
                    : TimeSpan.FromHours(MAX_TIMEOUT_H);
        public long MaxFileSize => MAX_FILE_SIZE_MB * 1024L * 1024L;
    }
}