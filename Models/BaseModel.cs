using System;

namespace porukica.Models
{
    public class BaseModel
    {
        public BaseModel(string secret)
        {
            Created = DateTime.UtcNow;
            Secret = secret;
        }

        public DateTime Created { get; set; }
        public string Secret { get; set; }
        public bool ValidForSecret(string secret)
        {
            if (string.IsNullOrWhiteSpace(secret))
                return true;

            if (Secret.Equals(secret, StringComparison.InvariantCultureIgnoreCase))
                return true;

            return false;
        }
    }
}