using System.Net.Http;
using System.Text;

namespace Browser_Reviewer
{
    public static class UrlhausIntel
    {
        public const string ExactCategory = "URLhaus / Malware Distribution";

        private const string CsvOnlineUrl = "https://urlhaus.abuse.ch/downloads/csv_online/";
        private static readonly object loadLock = new object();
        private static readonly HashSet<string> exactUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static bool loaded;

        public static string? Categorize(string url)
        {
            EnsureLoaded();

            if (exactUrls.Count == 0)
                return null;

            string normalizedUrl = NormalizeUrl(url);
            if (!string.IsNullOrWhiteSpace(normalizedUrl) && exactUrls.Contains(normalizedUrl))
                return ExactCategory;

            return null;
        }

        public static void EnsureLoaded()
        {
            if (loaded)
                return;

            lock (loadLock)
            {
                if (loaded)
                    return;

                try
                {
                    string cachePath = GetCachePath();
                    Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);

                    if (!File.Exists(cachePath) || DateTime.UtcNow - File.GetLastWriteTimeUtc(cachePath) > TimeSpan.FromHours(12))
                    {
                        DownloadSnapshot(cachePath);
                    }

                    LoadSnapshot(cachePath);
                }
                catch
                {
                    exactUrls.Clear();
                }
                finally
                {
                    loaded = true;
                }
            }
        }

        private static string GetCachePath()
        {
            return Path.Combine(AppContext.BaseDirectory, "ThreatIntel", "urlhaus_online.csv");
        }

        private static void DownloadSnapshot(string cachePath)
        {
            using HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("BrowserReviewer/1.0");
            string content = client.GetStringAsync(CsvOnlineUrl).GetAwaiter().GetResult();
            File.WriteAllText(cachePath, content, Encoding.UTF8);
        }

        private static void LoadSnapshot(string cachePath)
        {
            exactUrls.Clear();

            foreach (string rawLine in File.ReadLines(cachePath))
            {
                string line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                List<string> fields = ParseCsvLine(line);
                if (fields.Count < 3)
                    continue;

                string url = fields[2];
                string normalizedUrl = NormalizeUrl(url);
                if (!string.IsNullOrWhiteSpace(normalizedUrl))
                    exactUrls.Add(normalizedUrl);
            }
        }

        private static List<string> ParseCsvLine(string line)
        {
            List<string> fields = new List<string>();
            StringBuilder current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            fields.Add(current.ToString());
            return fields;
        }

        private static string NormalizeUrl(string url)
        {
            string value = (url ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            try
            {
                UriBuilder builder = new UriBuilder(value) { Fragment = string.Empty };
                string normalized = builder.Uri.AbsoluteUri.TrimEnd('/');
                return normalized;
            }
            catch
            {
                return value.TrimEnd('/');
            }
        }

    }
}
