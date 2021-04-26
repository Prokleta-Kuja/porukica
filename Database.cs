using System.Collections.Generic;
using porukica.Models;

namespace porukica
{
    public static class Database
    {
        public static readonly Dictionary<string, TextModel> Texts = new();
        public static readonly Dictionary<string, FileModel> Files = new();
    }
}