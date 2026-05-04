using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace TheFeed
{
    public class Program
    {
        public static Config Config { get; set; }

        public static Dictionary<string, Feed> Feeds = new Dictionary<string, Feed>();
        static JsonSerializerOptions jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        static HashMap map;
        static string root = AppContext.BaseDirectory;
        static string configPath = Path.Combine(root, "config.config");

        static bool DebugMode = false;
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            if (!File.Exists(configPath))
            {
                Console.WriteLine("Config does not exist. please fill out config");
                Config = new Config
                {
                    Port = 8080,
                    TopDir = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                    inspiretext = "inspire.txt"
                };
                string json = JsonSerializer.Serialize(Config, jsonOptions);
                File.WriteAllText(configPath, json);
                Console.WriteLine("Starting with default config. Please fill out config.config and restart the server for custom settings.");
            }
            Config = JsonSerializer.Deserialize<Config>(File.ReadAllText(configPath));
            if (args.Length > 0)
            {
                if (args[0] == "--debug")
                {
                    DebugMode = true;
                    Console.WriteLine("Debug mode enabled");
                }
                if (args[0] == "-Config")
                {
                    if (args[1] == "reset")
                    {
                        Console.WriteLine("Resetting config...");
                        Config = new Config
                        {
                            Port = 8080,
                            TopDir = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                            inspiretext = "inspire.txt"
                        };
                        string json = JsonSerializer.Serialize(Config, jsonOptions);
                        File.WriteAllText(configPath, json);
                        Console.WriteLine("Config reset. Please fill out config.config and restart the server.");
                        return;
                    }
                }
                if (args[0] == "--DConfig")
                {
                    try
                    {
                        Config.TopDir = args[1];
                        string json = JsonSerializer.Serialize(Config, jsonOptions);
                        File.WriteAllText(configPath, json);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to update config: {ex.Message}");

                    }
                    return;
                }
                if (args[0] == "--PConfig")
                {
                    try
                    {
                        Config.Port = int.Parse(args[1]);
                        string json = JsonSerializer.Serialize(Config, jsonOptions);
                        File.WriteAllText(configPath, json);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to update config: {ex.Message}");

                    }
                    return;
                }
                if (args[0] == "--IConfig")
                {
                    try
                    {
                        Config.inspiretext = args[1];
                        string json = JsonSerializer.Serialize(Config, jsonOptions);
                        File.WriteAllText(configPath, json);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to update config: {ex.Message}");

                    }
                    return;
                }
                if(args[0] == "--TConfig")
                {
                    try
                    {
                        Config.Title = args[1];
                        string json = JsonSerializer.Serialize(Config, jsonOptions);
                        File.WriteAllText(configPath, json);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to update config: {ex.Message}");
                    }
                    return;
                }

                if (args[0] == "--help")
                {
                    Console.WriteLine("Usage:");
                    Console.WriteLine("--debug: Enable debug mode");
                    Console.WriteLine("-Config reset: Reset config to default values");
                    Console.WriteLine("--DConfig [path]: Set the top directory for images");
                    Console.WriteLine("--PConfig [port]: Set the port for the server");
                    Console.WriteLine("--IConfig [file]: Set the inspire text file");
                    Console.WriteLine("--TConfig [title]: Set the title of the feed");
                    return;
                }
            }

            var listener = new TcpListener(IPAddress.Any, Config.Port);
            map = new HashMap(Config.TopDir, new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".avif", ".mp4", ".mov", ".avi", ".mkv" });
            try
            {
                listener.Start();
                Console.WriteLine($"Server listening on http://localhost:{Config.Port}");
                Console.WriteLine("Press Ctrl+C to stop.");
                Console.WriteLine();
                FileInfo[] otherui = new DirectoryInfo("ClientUI").GetFiles("*.html");
                for (int i = 0; i < otherui.Length; i++)
                {
                    Console.WriteLine($"UI Option {i + 1}: http://localhost:{Config.Port}/ui/{otherui[i].Name.Replace(".html", string.Empty)}");
                }
                Console.WriteLine("ctrl + click the links above to open in browser");

                if (DebugMode)
                {
                    Console.WriteLine("Available network interfaces:");
                    // list all available IP addresses
                    IPAddress[] localIPs = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
                    foreach (IPAddress localIP in localIPs)
                    {
                        if(localIP.AddressFamily == AddressFamily.InterNetworkV6)
                        {
                            Console.WriteLine($"http://[{localIP}]:{Config.Port}");

                        }
                        else
                        {
                            Console.WriteLine($"http://{localIP}:{Config.Port}");

                        }
                    }
                }
            }
            catch (SocketException ex)
            {
                Console.Error.WriteLine($"Failed to start server: {ex.Message}");
                Environment.Exit(1);
                return;
            }

            // Main request loop
            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                _ = HandleClient(client, Config.TopDir);
            }
        }

        static async Task HandleClient(TcpClient client, string topDir)
        {
            try
            {
                using (NetworkStream stream = client.GetStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    // Read HTTP request line
                    string requestLine = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(requestLine))
                        return;

                    string[] parts = requestLine.Split(' ');
                    if (parts.Length < 2)
                        return;

                    string method = parts[0];
                    string path = parts[1];

                    // Read headers
                    Dictionary<string, string> headers = new Dictionary<string, string>();
                    string line;
                    while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync()))
                    {
                        var headerParts = line.Split(new[] { ':' }, 2);
                        if (headerParts.Length == 2)
                            headers[headerParts[0].Trim()] = headerParts[1].Trim();
                    }

                    // Extract path only (remove query string)
                    if (path.Contains('?'))
                        path = path.Substring(0, path.IndexOf('?'));


                    if (DebugMode)
                    {
                        Console.WriteLine($"{method} {path}");
                    }
                    
                    // Route request
                    var (responseData, contentType, statusCode) = method == "GET"
                        ? await RouteRequest(path, Config.TopDir)
                        : (GetErrorResponse(405, "Method Not Allowed"), "application/json", 405);

                    // Send HTTP response
                    using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true))
                    {
                        await writer.WriteLineAsync($"HTTP/1.1 {statusCode} OK");
                        await writer.WriteLineAsync($"Content-Type: {contentType}; charset=utf-8");
                        await writer.WriteLineAsync($"Content-Length: {responseData.Length}");
                        await writer.WriteLineAsync("Connection: close");
                        await writer.WriteLineAsync();
                        await writer.FlushAsync();
                    }

                    await stream.WriteAsync(responseData, 0, responseData.Length);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error handling client: {ex.Message}");
            }
            finally
            {
                client.Close();
            }
        }

        static async Task<(byte[], string, int)> RouteRequest(string path, string topDir)
        {
           
            var Segments = path.Split('/');
            
            Feed f = Feeds.TryGetValue(Segments[^1], out var fed) ? fed : new Feed(Config.TopDir, Config.inspiretext, map);
            if (path == "/")
            {
                string newid = Guid.NewGuid().ToString();
                Feeds[newid] = f;
                return (await GetHtmlResponse(GetDefaultClientHtml(newid)), "text/html", 200);
            }
            if (path.StartsWith("/api/post/random"))
            {
                if (Segments[^1]!= "random")
                {
                    Feeds[Segments[^1]] = f;
                }
                return (GetRandomPostJson(f), "application/json", 200);
            }
            if (path.StartsWith("/api/feed"))
            {
                if (Segments[^1] != "feed")
                {
                    Feeds[Segments[^1]] = f;
                }
                return (GetFeedJson(f), "application/json", 200);
            }
            if (path.StartsWith("/api/stats"))
            {
                if (Segments[^1] != "stats")
                {
                    Feeds[Segments[^1]] = f;
                }
                return (GetStatsJson(f), "application/json", 200);
            }
            if (path.StartsWith("/files/"))
                return (await GetFileResponse(path.Substring(7)), $"image/{path.Substring(7).Split('/').Last().Split('.').Last()}", 200);
            if (path.StartsWith("/favico"))
                return (await GetFileResponse(topDir, "icon.ico"), $"image/.ico", 200);
            if (path.StartsWith("/ui/"))
            {
                string newid = Guid.NewGuid().ToString();
                Feeds[newid] = f;
                return (await GetHtmlResponse(GetDefaultClientHtml(newid,path.Substring(4))), "text/html", 200);
            }
            return (GetErrorResponse(404, "Not Found"), "application/json", 404);
        }

        static byte[] GetRandomPostJson(Feed feed)
        {
            var post = feed.GetRandomPost();
            var dto = new
            {
                username = post.Username,
                avatar = post.AvatarPath,
                image = post.ImagePath,
                mediaType = post.MediaType,
                caption = post.Caption,
                timeAgo = post.TimeAgo,
                likes = post.Likes,
                color = post.Color,
                initials = post.Initials
            };
            for (int i = 0; i < Feeds.Count; i++)
            {
                if(Feeds.ElementAt(i).Value == feed)
                    continue;
                if (Feeds.ElementAt(i).Value.TimeSinceLastRequest> TimeSpan.FromHours(1))
                {
                    Feeds.Remove(Feeds.ElementAt(i).Key);
                    i--;
                }
            }
            return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(dto));
        }

        static byte[] GetFeedJson(Feed feed)
        {
            var posts = feed.GetFeedItems();
            var dtos = new List<object>();

            foreach (var post in posts)
            {
                dtos.Add(new
                {
                    username = post.Username,
                    avatar = post.AvatarPath != null ? feed.ToRelativePath(post.AvatarPath) : null,
                    image = feed.ToRelativePath(post.ImagePath),
                    mediaType = post.MediaType,
                    caption = post.Caption,
                    timeAgo = post.TimeAgo,
                    likes = post.Likes,
                    color = post.Color,
                    initials = post.Initials
                });
            }

            return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(dtos));
        }

        static byte[] GetStatsJson(Feed feed)
        {
            var stats = new
            {
                users = feed.GetUserCount(),
                posts = feed.GetPostCount(),
                quotes = feed.GetQuoteCount()
            };
            return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(stats));
        }

        static async Task<byte[]> GetHtmlResponse(string html)
        {
            return Encoding.UTF8.GetBytes(html);
        }

        static async Task<byte[]> GetJsonResponse(byte[] json)
        {
            return json;
        }

        static async Task<byte[]> GetFileResponse(string baseDir, string relativePath)
        {
            try
            {
                string filePath = Uri.UnescapeDataString(Path.Combine(baseDir, relativePath));

                // Prevent directory traversal attacks
                string fullPath = Path.GetFullPath(filePath);
                string fullBase = Path.GetFullPath(baseDir);
                if (!fullPath.StartsWith(fullBase))
                    return GetErrorResponse(403, "Access denied");

                if (!File.Exists(fullPath))
                    return GetErrorResponse(404, "File not found");

                return await File.ReadAllBytesAsync(fullPath);
            }
            catch (Exception ex)
            {
                return GetErrorResponse(500, $"Error: {ex.Message}");
            }
        }
        static async Task<byte[]> GetFileResponse(string hash)
        {
            try
            {
                // Prevent directory traversal attacks
                string fullPath = map.Reverse.TryGetValue(hash, out var path) ? path : null;

                if (!File.Exists(fullPath))
                    return GetErrorResponse(404, "File not found");

                return await File.ReadAllBytesAsync(fullPath);
            }
            catch (Exception ex)
            {
                return GetErrorResponse(500, $"Error: {ex.Message}");
            }
        }

        static byte[] GetErrorResponse(int statusCode, string message)
        {
            var error = new { error = message };
            return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(error));
        }

        static string GetDefaultClientHtml(string newid,string UIT="insta")
        {
            return File.ReadAllText(Path.Combine(root,"ClientUI", UIT + ".html")).Replace("LeFeed", Config.Title).Replace("GUIDO", newid);
        }
    }
}