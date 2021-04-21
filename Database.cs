using System;
using System.Collections.Generic;

namespace porukica
{
    public static class Database
    {
        public static readonly Dictionary<string, Message> Texts = new();
        public static readonly Dictionary<string, Message> Files = new();
    }
}