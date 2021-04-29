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
        public bool ValidForSecret(string providedSecret)
        {
            if (string.IsNullOrWhiteSpace(Secret))
                return true;

            return Secret.Equals(providedSecret);
        }
    }
}