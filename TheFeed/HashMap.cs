using System;
using System.Collections.Generic;
using System.Text;

namespace TheFeed
{
    public class HashMap
    {
        // Maps file paths to their hashes
        public Dictionary<string, string> Map { get; private set; }
        // Maps hashes back to file paths
        public Dictionary<string, string> Reverse { get; private set; }

        public HashMap(string directory, string[] extentions)
        {
            var items = new List<string>();
            foreach (var ext in extentions)
            {
                items.AddRange(Directory.GetFiles(directory, $"*{ext}", SearchOption.AllDirectories));
            }
            
            Map = new Dictionary<string, string>();
            Reverse = new Dictionary<string, string>();
            foreach (var item in items)
            {
                var hash = GetHash(item);
                Map[item] = hash;
                Reverse[hash] = item;
            }
        }

        private string GetHash(string input)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(input);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
}
