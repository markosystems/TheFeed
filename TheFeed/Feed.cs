using System;
using System.Collections.Generic;
using System.Text;

namespace TheFeed
{
    public class Feed
    {
        private readonly string _rootDir;
        private readonly string _inspirePath;
        private readonly string[] _quotes;
        private readonly List<User> _users;
        private List<FeedItem> _feedItems = new List<FeedItem>();
        private List<FeedItem> FeedItems = new List<FeedItem>();
        private readonly Random _rng = new Random();

        private readonly HashSet<string> MediaExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".avif", ".mp4", ".mov", ".avi", ".mkv" };

        public Feed(string rootDir, string inspirePath, HashMap map)
        {
            _rootDir = rootDir;
            _inspirePath = inspirePath;

            // Load quotes
            _quotes = File.Exists(_inspirePath)
                ? File.ReadAllLines(_inspirePath)
                      .Select(l => l.Trim())
                      .Where(l => l.Length > 0)
                      .ToArray()
                : new[] { "Stay inspired.", "Make today count.", "Keep going." };

            _users = new List<User>();
            _feedItems = new List<FeedItem>();

            LoadFeed(map);
        }
        public Feed(string rootDir, string inspirePath, HashMap map, string filter)
        {
            _rootDir = rootDir;
            _inspirePath = inspirePath;

            // Load quotes
            _quotes = File.Exists(_inspirePath)
                ? File.ReadAllLines(_inspirePath)
                      .Select(l => l.Trim())
                      .Where(l => l.Length > 0)
                      .ToArray()
                : new[] { "Stay inspired.", "Make today count.", "Keep going." };

            _users = new List<User>();
            _feedItems = new List<FeedItem>();

            LoadFeed(map, filter);
        }

        private void LoadFeed(HashMap map)
        {
            string[] userDirs = Directory.GetDirectories(_rootDir)
                .OrderBy(d => d)
                .ToArray();

            foreach (string userDir in userDirs)
            {
                string username = Path.GetFileName(userDir);

                // Skip hidden / system dirs
                if (username.StartsWith(".") || username.StartsWith("_")) continue;

                var MediaFiles = Directory.GetFiles(userDir, "*", SearchOption.AllDirectories)
                    .Where(f => MediaExtensions.Contains(Path.GetExtension(f)))
                    .OrderBy(f => f)
                    .ToArray();

                if (MediaFiles.Length == 0) continue;

                // Use first image as avatar, remaining as posts
                string avatarPath = MediaFiles.Length > 1 ? map.Map[MediaFiles[0]] : null;
                var postFiles = MediaFiles.Length > 1 ? MediaFiles.Skip(1).ToArray() : MediaFiles;

                var posts = postFiles.Select(imgPath => new Post(
                    ImagePath: map.Map[imgPath],
                    Caption: PickQuote(),
                    TimeAgo: PickTimeAgo()
                )).ToList();

                var user = new User(username, avatarPath, posts);
                _users.Add(user);

                // Add to flat feed
                foreach (var post in posts)
                {
                    var item = new FeedItem
                    {
                        Username = username,
                        AvatarPath = avatarPath,
                        ImagePath = post.ImagePath,
                        MediaType = Path.GetExtension(map.Reverse[post.ImagePath]).ToLower() switch
                        {
                            ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".avif" => "image",
                            ".mp4" or ".mov" or ".avi" or ".mkv" => "video",
                            _ => "unknown"
                        },
                        Caption = post.Caption,
                        TimeAgo = post.TimeAgo,
                        Likes = _rng.Next(12, 2400),
                        Color = HslFromName(username),
                        Initials = GetInitials(username)
                    };
                    _feedItems.Add(item);
                }
            }

            if (_users.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No users found in: {_rootDir}\n" +
                    "Expected: each top-level subdirectory = one user, containing image files."
                );
            }

            // Shuffle feed
            _feedItems = _feedItems.OrderBy(_ => _rng.Next()).ToList();
            FeedItems.AddRange(_feedItems);
        }

        private void LoadFeed(HashMap map, string filter)
        {
            string[] userDirs = Directory.GetDirectories(_rootDir)
                .OrderBy(d => d).Where(d => d.Contains(filter, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            foreach (string userDir in userDirs)
            {
                string username = Path.GetFileName(userDir);

                // Skip hidden / system dirs
                if (username.StartsWith(".") || username.StartsWith("_")) continue;

                var MediaFiles = Directory.GetFiles(userDir, "*", SearchOption.AllDirectories)
                    .Where(f => MediaExtensions.Contains(Path.GetExtension(f)))
                    .OrderBy(f => f)
                    .ToArray();

                if (MediaFiles.Length == 0) continue;

                // Use first image as avatar, remaining as posts
                string avatarPath = MediaFiles.Length > 1 ? map.Map[MediaFiles[0]] : null;
                var postFiles = MediaFiles.Length > 1 ? MediaFiles.Skip(1).ToArray() : MediaFiles;

                var posts = postFiles.Select(imgPath => new Post(
                    ImagePath: map.Map[imgPath],
                    Caption: PickQuote(),
                    TimeAgo: PickTimeAgo()
                )).ToList();

                var user = new User(username, avatarPath, posts);
                _users.Add(user);

                // Add to flat feed
                foreach (var post in posts)
                {
                    var item = new FeedItem
                    {
                        Username = username,
                        AvatarPath = avatarPath,
                        ImagePath = post.ImagePath,
                        MediaType = Path.GetExtension(map.Reverse[post.ImagePath]).ToLower() switch
                        {
                            ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".avif" => "image",
                            ".mp4" or ".mov" or ".avi" or ".mkv" => "video",
                            _ => "unknown"
                        },
                        Caption = post.Caption,
                        TimeAgo = post.TimeAgo,
                        Likes = _rng.Next(12, 2400),
                        Color = HslFromName(username),
                        Initials = GetInitials(username)
                    };
                    _feedItems.Add(item);
                }
            }

            if (_users.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No users found in: {_rootDir}\n" +
                    "Expected: each top-level subdirectory = one user, containing image files."
                );
            }

            // Shuffle feed
            _feedItems = _feedItems.OrderBy(_ => _rng.Next()).ToList();
            FeedItems.AddRange(_feedItems);
            lastRequest = DateTime.Now;
        }

        public List<FeedItem> GetFeedItems() => _feedItems;
        public int GetPostCount() => _feedItems.Count;
        public int GetUserCount() => _users.Count;
        public int GetQuoteCount() => _quotes.Length;

        public FeedItem GetRandomPost() {
            lastRequest = DateTime.Now;
            var item = FeedItems[_rng.Next(FeedItems.Count)];
            FeedItems.Remove(item); // Ensure we don't repeat until all shown
            if(FeedItems.Count == 0) {
                // Reset feed when all items have been shown
                FeedItems.AddRange(_feedItems);
            }
            return item;
        }

        public List<FeedItem> GetPostsByUser(string username)
        {
            lastRequest = DateTime.Now;
            return _feedItems.Where(f => f.Username == username).ToList();
        }

        private string PickQuote() => _quotes[_rng.Next(_quotes.Length)];

        private string PickTimeAgo()
        {
            string[] timeOptions = { "just now", "2m", "14m", "1h", "3h", "8h", "yesterday", "2d", "5d" };
            return timeOptions[_rng.Next(timeOptions.Length)];
        }

        private static string GetInitials(string name) =>
            string.Concat(name.Split(new[] { '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                              .Take(2)
                              .Select(w => char.ToUpper(w[0])));

        private static string HslFromName(string name)
        {
            int h = Math.Abs(name.GetHashCode()) % 360;
            return $"hsl({h},55%,48%)";
        }

        public string ToRelativePath(string absolutePath)
        {
            // Return path relative to root directory
            if (Path.IsPathRooted(absolutePath))
            {
                return Path.GetRelativePath(_rootDir, absolutePath);
            }
            return absolutePath;
        }

        DateTime lastRequest {  get; set; }
        public TimeSpan TimeSinceLastRequest => DateTime.Now - lastRequest;
    }

    // ── Data models ──────────────────────────────────────────────────────────────

    public record Post(string ImagePath, string Caption, string TimeAgo);
    public record User(string Username, string AvatarPath, List<Post> Posts);

    public class FeedItem
    {
        public string Username { get; set; }
        public string AvatarPath { get; set; }
        public string ImagePath { get; set; }
        public string MediaType { get; set; }
        public string Caption { get; set; }
        public string TimeAgo { get; set; }
        public int Likes { get; set; }
        public string Color { get; set; }
        public string Initials { get; set; }
    }
}
