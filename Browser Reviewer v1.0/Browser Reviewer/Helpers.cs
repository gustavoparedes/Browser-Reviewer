using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;
using Browser_Reviewer.Resources;

namespace Browser_Reviewer
{
    public class Helpers
    {
        public static string historyConnectionString = "";

        public static string chromeViewerConnectionString = "";

        public static string BrowserType = "";
        public static string BrowserFamily = "";
        public static string BrowserContainerKey = "";

        private static readonly object BrowserIdentityLock = new object();
        private static readonly Dictionary<string, string> UnknownBrowserAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, int> UnknownAliasBaseUsage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Image> ExternalBrowserImageCache = new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, string[]> BrowserBrandingFiles = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["LibreWolf"] = new[] { "LibreWolf.ico", "LibreWolf.png" },
            ["Waterfox"] = new[] { "Waterfox.ico", "Waterfox.png", "WaterfoxAlt.png" },
            ["Floorp"] = new[] { "Floorp.ico", "Floorp.png" },
            ["Mullvad Browser"] = new[] { "MullvadBrowser.ico", "MullvadBrowser.png" },
            ["Tor Browser"] = new[] { "TorBrowser.ico", "TorBrowser.png" },
            ["Zen Browser"] = new[] { "ZenBrowser.ico", "ZenBrowser.png" },
            ["Thunderbird"] = new[] { "Thunderbird.png", "Thunderbird.ico" },
            ["Chromium"] = new[] { "Chromium.ico", "Chromium.png" },
            ["Arc"] = new[] { "Arc.ico", "Arc.png" },
            ["Thorium"] = new[] { "Thorium.ico", "Thorium.png" },
            ["CocCoc"] = new[] { "CocCoc.ico", "CocCoc.png" },
            ["Cent Browser"] = new[] { "CentBrowser.ico", "CentBrowser.png" },
            ["Whale"] = new[] { "Whale.ico", "Whale.png" },
            ["Maxthon"] = new[] { "Maxthon.ico", "Maxthon.png" },
            ["Comodo Dragon"] = new[] { "ComodoDragon.ico", "ComodoDragon.png" },
            ["Epic Privacy Browser"] = new[] { "EpicPrivacyBrowser.ico", "EpicPrivacyBrowser.png" },
            ["Avast Secure Browser"] = new[] { "AvastSecureBrowser.ico", "AvastSecureBrowser.png" },
            ["DeepL"] = new[] { "DeepL.png", "DeepL.ico" },
            ["Visual Studio WebView2"] = new[] { "VisualStudioWebView2.png", "VisualStudioWebView2.ico" },
            ["Outlook WebView2"] = new[] { "OutlookWebView2.png", "OutlookWebView2.ico" },
            ["OneDrive WebView2"] = new[] { "OneDriveWebView2.png" },
            ["OpenAI Codex WebView2"] = new[] { "OpenAICodexWebView2.png" },
            ["Steam Embedded Chromium"] = new[] { "SteamEmbeddedChromium.png", "SteamEmbeddedChromium.ico" },
            ["Roblox Studio WebView2"] = new[] { "RobloxStudioWebView2.png", "RobloxStudioWebView2.ico" },
            ["TeamViewer WebView2"] = new[] { "TeamViewerWebView2.png", "TeamViewerWebView2.ico" },
            ["Amazon WorkSpaces WebView2"] = new[] { "AmazonWorkSpacesWebView2.png", "AmazonWorkSpacesWebView2.ico" },
            ["Windows Search WebView2"] = new[] { "WindowsSearchWebView2.png", "WindowsSearchWebView2.ico" }
        };

        private static readonly (string Name, string Family, string[] Tokens)[] KnownBrowserDefinitions =
        {
            ("LibreWolf", "Firefox-like", new[] { "librewolf" }),
            ("Waterfox", "Firefox-like", new[] { "waterfox" }),
            ("Floorp", "Firefox-like", new[] { "floorp" }),
            ("Mullvad Browser", "Firefox-like", new[] { "mullvad browser", "\\mullvad\\" }),
            ("Tor Browser", "Firefox-like", new[] { "tor browser", "\\tor\\browser\\" }),
            ("Zen Browser", "Firefox-like", new[] { "zen browser", "\\zen browser\\" }),
            ("Thunderbird", "Firefox-like", new[] { "\\thunderbird\\", "thunderbird\\profiles" }),
            ("Firefox", "Firefox-like", new[] { "firefox", "\\mozilla\\" }),
            ("DeepL", "Chromium-like", new[] { "\\deepl_se\\", "\\deepl\\" }),
            ("Windows Search WebView2", "Chromium-like", new[] { "microsoft.windows.search_cw5n1h2txyewy", "\\windows.search\\" }),
            ("Visual Studio WebView2", "Chromium-like", new[] { "\\microsoft\\visualstudio\\webview2cache\\", "\\microsoft\\visualstudio\\" }),
            ("Outlook WebView2", "Chromium-like", new[] { "\\microsoft\\olk\\ebwebview\\" }),
            ("OneDrive WebView2", "Chromium-like", new[] { "\\microsoft\\onedrive\\ebwebview\\" }),
            ("OpenAI Codex WebView2", "Chromium-like", new[] { "\\packages\\openai.codex_", "\\localcache\\roaming\\codex\\", "\\openai.codex_" }),
            ("Steam Embedded Chromium", "Chromium-like", new[] { "\\steam\\htmlcache\\" }),
            ("Roblox Studio WebView2", "Chromium-like", new[] { "\\roblox\\robloxstudio\\webview2\\" }),
            ("TeamViewer WebView2", "Chromium-like", new[] { "\\teamviewer\\edgebrowsercontrol\\" }),
            ("Amazon WorkSpaces WebView2", "Chromium-like", new[] { "\\amazon web services\\amazon workspaces\\webview2\\" }),
            ("Brave", "Chromium-like", new[] { "brave" }),
            ("Edge", "Chromium-like", new[] { "msedge", "microsoft\\edge", "\\edge\\", "webcache" }),
            ("Opera", "Chromium-like", new[] { "opera gx", "opera" }),
            ("Yandex", "Chromium-like", new[] { "yandex" }),
            ("Vivaldi", "Chromium-like", new[] { "vivaldi" }),
            ("Arc", "Chromium-like", new[] { "\\arc\\", "arc user data" }),
            ("Thorium", "Chromium-like", new[] { "thorium" }),
            ("Chromium", "Chromium-like", new[] { "chromium" }),
            ("Chrome", "Chromium-like", new[] { "google\\chrome", "\\chrome\\", "chrome user data" }),
            ("CocCoc", "Chromium-like", new[] { "coccoc" }),
            ("Cent Browser", "Chromium-like", new[] { "centbrowser", "cent browser" }),
            ("Whale", "Chromium-like", new[] { "naver\\whale", "\\whale\\" }),
            ("Maxthon", "Chromium-like", new[] { "maxthon" }),
            ("Comodo Dragon", "Chromium-like", new[] { "comodo dragon" }),
            ("Epic Privacy Browser", "Chromium-like", new[] { "epic privacy browser" }),
            ("Avast Secure Browser", "Chromium-like", new[] { "avast\\browser", "avast secure browser" })
        };

        private static readonly HashSet<string> NeutralBrowserLabels = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Unknown",
            "Firefox-like",
            "Chromium-like"
        };
        private static readonly Regex GeneratedUnknownAliasRegex = new Regex(@"^Unknown\s+\d+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string? DetectKnownBrowserNameFromPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            string normalizedPath = NormalizeBrowserPath(path);

            foreach (var definition in KnownBrowserDefinitions)
            {
                if (definition.Tokens.Any(token => normalizedPath.Contains(token)))
                    return definition.Name;
            }

            return null;
        }

        public static void ResetBrowserIdentityState()
        {
            lock (BrowserIdentityLock)
            {
                UnknownBrowserAliases.Clear();
                UnknownAliasBaseUsage.Clear();
            }
        }

        public static bool TryGetBrowserFamily(string? browser, out string family)
        {
            family = string.Empty;
            if (string.IsNullOrWhiteSpace(browser))
                return false;

            string normalizedBrowser = NormalizeBrowserLabel(browser);

            if (normalizedBrowser.Equals("Firefox-like", StringComparison.OrdinalIgnoreCase))
            {
                family = "Firefox-like";
                return true;
            }

            if (normalizedBrowser.Equals("Chromium-like", StringComparison.OrdinalIgnoreCase))
            {
                family = "Chromium-like";
                return true;
            }

            foreach (var definition in KnownBrowserDefinitions)
            {
                if (normalizedBrowser.Equals(definition.Name, StringComparison.OrdinalIgnoreCase))
                {
                    family = definition.Family;
                    return true;
                }
            }

            return false;
        }

        public static BrowserIdentity ResolveBrowserIdentity(string? filePath, string? browserFamily, string? browser = null)
        {
            string normalizedFamily = TryGetBrowserFamily(browserFamily, out string resolvedFamily)
                ? resolvedFamily
                : TryGetBrowserFamily(browser, out resolvedFamily)
                    ? resolvedFamily
                    : "Unknown";

            string sourcePath = GetIdentitySourcePath(filePath, normalizedFamily);
            string containerKey = GetBrowserContainerKey(sourcePath, normalizedFamily);
            string? sanitizedBrowser = string.IsNullOrWhiteSpace(browser) ? null : NormalizeBrowserLabel(browser);
            bool browserMatchesFamily = IsKnownBrowserLabel(sanitizedBrowser)
                && (!TryGetBrowserFamily(sanitizedBrowser, out string browserResolvedFamily) || browserResolvedFamily == normalizedFamily || normalizedFamily == "Unknown");
            string? knownName = browserMatchesFamily
                ? sanitizedBrowser
                : DetectKnownBrowserNameFromPath(containerKey);

            string displayName = !string.IsNullOrWhiteSpace(knownName)
                ? FormatDisplayBrowserName(knownName)
                : GetOrCreateUnknownAlias(containerKey);

            return new BrowserIdentity(normalizedFamily, containerKey, knownName, displayName, sourcePath);
        }

        private static string FormatDisplayBrowserName(string browserName)
        {
            if (string.IsNullOrWhiteSpace(browserName))
                return browserName;

            return browserName.Replace(" WebView2", " WView2", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeBrowserLabel(string browserName)
        {
            if (string.IsNullOrWhiteSpace(browserName))
                return browserName;

            return browserName.Trim().Replace(" WView2", " WebView2", StringComparison.OrdinalIgnoreCase);
        }

        public static string ResolveDisplayBrowserName(string? browser, string? filePath = null, string? browserFamily = null)
        {
            return ResolveBrowserIdentity(filePath, browserFamily, browser).DisplayName;
        }

        public static Image GetBrowserImage(string? browser, string? filePath = null, string? browserFamily = null)
        {
            BrowserIdentity identity = ResolveBrowserIdentity(filePath, browserFamily, browser);
            string displayName = identity.KnownName ?? identity.DisplayName;

            Image? externalImage = TryLoadExternalBrowserImage(displayName);
            if (externalImage != null)
                return externalImage;

            return displayName switch
            {
                "Chrome" => Resource1.Chrome.ToBitmap(),
                "Chromium" => Resource1.Chrome.ToBitmap(),
                "Brave" => Resource1.Brave.ToBitmap(),
                "Edge" => Resource1.Edge.ToBitmap(),
                "Opera" => Resource1.Opera.ToBitmap(),
                "Yandex" => Resource1.Yandex.ToBitmap(),
                "Vivaldi" => Resource1.Vivaldi.ToBitmap(),
                "Firefox" => Resource1.Firefox.ToBitmap(),
                "LibreWolf" => Resource1.Firefox.ToBitmap(),
                "Waterfox" => Resource1.Firefox.ToBitmap(),
                "Floorp" => Resource1.Firefox.ToBitmap(),
                "Mullvad Browser" => Resource1.Firefox.ToBitmap(),
                "Tor Browser" => Resource1.Firefox.ToBitmap(),
                "Zen Browser" => Resource1.Firefox.ToBitmap(),
                "Thunderbird" => Resource1.Firefox.ToBitmap(),
                _ when identity.Family == "Chromium-like" && !string.IsNullOrWhiteSpace(identity.KnownName) => Resource1.Chrome.ToBitmap(),
                _ when identity.Family == "Firefox-like" && !string.IsNullOrWhiteSpace(identity.KnownName) => Resource1.Firefox.ToBitmap(),
                _ => Resource1.Unknown.ToBitmap(),
            };
        }

        public static string GetIdentitySourcePath(string? path, string? browserFamily = null)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            string normalizedFamily = TryGetBrowserFamily(browserFamily, out string resolvedFamily) ? resolvedFamily : string.Empty;
            string normalizedPath = NormalizeBrowserPath(path);

            if (normalizedFamily == "Firefox-like")
            {
                if (normalizedPath.Contains("\\appdata\\local\\temp\\") && !string.IsNullOrWhiteSpace(realFirefoxPlacesPath))
                    return realFirefoxPlacesPath;

                string fileName = Path.GetFileName(path);
                if (fileName.Equals("formhistory.sqlite", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(realFirefoxFormPath))
                    return realFirefoxFormPath;

                if (fileName.Equals("places.sqlite", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(realFirefoxPlacesPath))
                    return realFirefoxPlacesPath;
            }

            return path;
        }

        private static bool IsKnownBrowserLabel(string? browser)
        {
            if (string.IsNullOrWhiteSpace(browser))
                return false;

            string trimmed = browser.Trim();
            if (NeutralBrowserLabels.Contains(trimmed))
                return false;

            if (GeneratedUnknownAliasRegex.IsMatch(trimmed))
                return false;

            return true;
        }

        public static string GetBrowserContainerKey(string? path, string? browserFamily = null)
        {
            string sourcePath = GetIdentitySourcePath(path, browserFamily);
            if (string.IsNullOrWhiteSpace(sourcePath))
                return "unknown-container";

            string normalizedPath = NormalizeBrowserPath(sourcePath);
            string family = TryGetBrowserFamily(browserFamily, out string resolvedFamily)
                ? resolvedFamily
                : DetectKnownBrowserNameFromPath(sourcePath) is string knownName && TryGetBrowserFamily(knownName, out resolvedFamily)
                    ? resolvedFamily
                    : string.Empty;

            if (family == "Firefox-like")
                return ExtractFirefoxContainerKey(sourcePath, normalizedPath);

            if (family == "Chromium-like")
                return ExtractChromiumContainerKey(sourcePath, normalizedPath);

            return NormalizePathSafe(Path.GetDirectoryName(sourcePath) ?? sourcePath);
        }

        private static string NormalizeBrowserPath(string path)
        {
            return path.Replace('/', '\\').ToLowerInvariant();
        }

        private static string ExtractFirefoxContainerKey(string path, string normalizedPath)
        {
            if (normalizedPath.Contains("\\profiles\\"))
            {
                int profilesIndex = normalizedPath.LastIndexOf("\\profiles\\", StringComparison.OrdinalIgnoreCase);
                if (profilesIndex >= 0)
                {
                    string basePath = path.Substring(0, profilesIndex + "\\profiles\\".Length);
                    string remainder = path.Substring(Math.Min(path.Length, profilesIndex + "\\profiles\\".Length));
                    string[] parts = remainder.Split(['\\'], StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0)
                        return NormalizePathSafe(Path.Combine(basePath, parts[0]));
                }
            }

            return NormalizePathSafe(Path.GetDirectoryName(path) ?? path);
        }

        private static string ExtractChromiumContainerKey(string path, string normalizedPath)
        {
            string collapsedSnapshots = Regex.Replace(path, @"\\Snapshots\\[^\\]+", string.Empty, RegexOptions.IgnoreCase);
            string directoryPath = Directory.Exists(collapsedSnapshots) ? collapsedSnapshots : Path.GetDirectoryName(collapsedSnapshots) ?? collapsedSnapshots;
            string normalizedDirectory = NormalizeBrowserPath(directoryPath);

            string[] markerRoots =
            {
                "\\ebwebview\\default",
                "\\ebwebview",
                "\\edgebrowsercontrol\\persistent",
                "\\webview2cache",
                "\\htmlcache\\default",
                "\\cache\\default",
                "\\user data\\default",
                "\\user data\\profile ",
                "\\user data\\guest profile",
                "\\profiles\\default",
                "\\profiles"
            };

            foreach (string marker in markerRoots)
            {
                int markerIndex = normalizedDirectory.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (markerIndex >= 0)
                {
                    string candidate = directoryPath.Substring(0, markerIndex + marker.Length);
                    return NormalizePathSafe(candidate);
                }
            }

            string fileName = Path.GetFileName(path);
            if (fileName.Equals("History", StringComparison.OrdinalIgnoreCase)
                || fileName.Equals("Bookmarks", StringComparison.OrdinalIgnoreCase)
                || fileName.Equals("Login Data", StringComparison.OrdinalIgnoreCase)
                || fileName.Equals("Web Data", StringComparison.OrdinalIgnoreCase)
                || fileName.Equals("Preferences", StringComparison.OrdinalIgnoreCase)
                || fileName.Equals("Local State", StringComparison.OrdinalIgnoreCase))
            {
                return NormalizePathSafe(Path.GetDirectoryName(path) ?? path);
            }

            return NormalizePathSafe(directoryPath);
        }

        private static string NormalizePathSafe(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "unknown-container";

            string normalized = path.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            try
            {
                normalized = Path.GetFullPath(normalized);
            }
            catch
            {
            }

            return normalized;
        }

        private static Image? TryLoadExternalBrowserImage(string displayName)
        {
            if (!BrowserBrandingFiles.TryGetValue(displayName, out string[]? candidateFiles) || candidateFiles.Length == 0)
                return null;

            lock (BrowserIdentityLock)
            {
                if (ExternalBrowserImageCache.TryGetValue(displayName, out Image? cachedImage))
                    return cachedImage;
            }

            string brandingDirectory = Path.Combine(AppContext.BaseDirectory, "BrowserBrandingIcons");
            foreach (string candidateFile in candidateFiles)
            {
                string fullPath = Path.Combine(brandingDirectory, candidateFile);
                if (!File.Exists(fullPath))
                    continue;

                try
                {
                    using FileStream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                    using MemoryStream buffer = new MemoryStream();
                    stream.CopyTo(buffer);
                    buffer.Position = 0;
                    using Image sourceImage = Image.FromStream(buffer);
                    Image image = new Bitmap(sourceImage);

                    lock (BrowserIdentityLock)
                    {
                        ExternalBrowserImageCache[displayName] = image;
                    }

                    return image;
                }
                catch
                {
                }
            }

            return null;
        }

        private static string GetOrCreateUnknownAlias(string containerKey)
        {
            string normalizedKey = string.IsNullOrWhiteSpace(containerKey) ? "unknown-container" : containerKey;

            lock (BrowserIdentityLock)
            {
                if (UnknownBrowserAliases.TryGetValue(normalizedKey, out string? existingAlias) && !string.IsNullOrWhiteSpace(existingAlias))
                    return existingAlias;

                string aliasBase = BuildUnknownAliasBase(normalizedKey);
                string alias = aliasBase;

                if (UnknownAliasBaseUsage.TryGetValue(aliasBase, out int usageCount))
                {
                    usageCount++;
                    UnknownAliasBaseUsage[aliasBase] = usageCount;
                    alias = $"{aliasBase} {usageCount}";
                }
                else
                {
                    UnknownAliasBaseUsage[aliasBase] = 1;
                }

                UnknownBrowserAliases[normalizedKey] = alias;
                return alias;
            }
        }

        private static string BuildUnknownAliasBase(string containerKey)
        {
            string[] genericSegments =
            {
                "default",
                "cache",
                "cache_data",
                "code cache",
                "gpucache",
                "gpucache",
                "local storage",
                "session storage",
                "indexeddb",
                "ebwebview",
                "user data",
                "profiles",
                "profile",
                "persistent",
                "storage",
                "snapshots",
                "def"
            };

            try
            {
                string trimmed = containerKey.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string[] parts = trimmed.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries);

                for (int i = parts.Length - 1; i >= 0; i--)
                {
                    string segment = parts[i].Trim();
                    if (string.IsNullOrWhiteSpace(segment))
                        continue;

                    if (genericSegments.Contains(segment, StringComparer.OrdinalIgnoreCase))
                        continue;

                    if (Regex.IsMatch(segment, @"^[0-9a-f]{8,}$", RegexOptions.IgnoreCase))
                        continue;

                    if (Regex.IsMatch(segment, @"^[0-9a-f]{8}-[0-9a-f-]{27,}$", RegexOptions.IgnoreCase))
                        continue;

                    if (segment.StartsWith("S-1-5-", StringComparison.OrdinalIgnoreCase))
                        continue;

                    string cleaned = Regex.Replace(segment, @"[_\-]+", " ").Trim();
                    if (string.IsNullOrWhiteSpace(cleaned))
                        continue;

                    return $"Unknown ({cleaned})";
                }
            }
            catch
            {
            }

            return "Unknown";
        }

        public readonly record struct BrowserIdentity(string Family, string ContainerKey, string? KnownName, string DisplayName, string SourcePath);

        public static bool IsFirefoxLikeBrowser(string? browser)
        {
            return TryGetBrowserFamily(browser, out string family) && family == "Firefox-like";
        }

        public static bool IsChromiumLikeBrowser(string? browser)
        {
            return TryGetBrowserFamily(browser, out string family) && family == "Chromium-like";
        }

        public static string HistoryTableForBrowser(string? browser)
        {
            return IsFirefoxLikeBrowser(browser) ? "firefox_results" : "results";
        }

        public static bool RequiresCombinedHistoryQuery(string? browser)
        {
            if (string.IsNullOrWhiteSpace(browser))
                return false;

            return !TryGetBrowserFamily(browser, out _);
        }

        public static string DownloadTableForBrowser(string? browser)
        {
            return IsFirefoxLikeBrowser(browser) ? "firefox_downloads" : "chrome_downloads";
        }

        public static string BookmarkTableForBrowser(string? browser)
        {
            return IsFirefoxLikeBrowser(browser) ? "bookmarks_Firefox" : "bookmarks_Chrome";
        }


        public static Dictionary<string, List<string>> browserUrls = new Dictionary<string, List<string>>();

        

        public static Dictionary<string, int> browsersWithDownloads = new Dictionary<string, int>();

        public static Dictionary<string, int> browserHistoryCounts = new Dictionary<string, int>();

        public static Dictionary<string, Dictionary<string, int>> browserHistoryCategoryCounts = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);


        public static Dictionary<string, int> browsersWithBookmarks = new Dictionary<string, int>();



        public static Dictionary<string, int> browsersWithAutofill = new Dictionary<string, int>();

        public static Dictionary<string, int> browsersWithCookies = new Dictionary<string, int>();

        public static Dictionary<string, int> browsersWithCache = new Dictionary<string, int>();

        public static Dictionary<string, int> browsersWithSessions = new Dictionary<string, int>();

        public static Dictionary<string, int> browsersWithExtensions = new Dictionary<string, int>();

        public static Dictionary<string, int> browsersWithLogins = new Dictionary<string, int>();

        public static Dictionary<string, int> browsersWithLocalStorage = new Dictionary<string, int>();

        public static Dictionary<string, int> browsersWithSessionStorage = new Dictionary<string, int>();

        public static Dictionary<string, int> browsersWithIndexedDb = new Dictionary<string, int>();

        public static  System.Drawing.Font FM = new System.Drawing.Font(System.Drawing.SystemFonts.DefaultFont.FontFamily, 11f);
        public static  System.Drawing.Font FS = new System.Drawing.Font(System.Drawing.SystemFonts.DefaultFont.FontFamily, 10f);





        public static Image? icono;

        public static int utcOffset = 0;

        public static string db_name = "";

        public static bool searchTermExists = false;

        public static bool searchTermRegExp = false;

        public static bool searchLabelsOnly = false;

        public static string searchTerm = "";

        public static bool searchTimeCondition = false;


        
        public static string realFirefoxPlacesPath = "";
        
        public static string realFirefoxFormPath = "";

        public static string comment = "";

        public static int itemscount = 0;

        public static int itemscountinSearch = 0;


        public static Dictionary<string, int> historyHits = new Dictionary<string, int>();


        public static Dictionary<string, int> downloadsHits = new Dictionary<string, int>();


        public static Dictionary<string, Label> navegadorLabels = new Dictionary<string, Label>();


        public static Dictionary<string, Label> downloadsLabels = new Dictionary<string, Label>();


        public static Dictionary<string, Label> bookmarksLabels = new Dictionary<string, Label>();


        public static Dictionary<string, Label> AutofillksLabels = new Dictionary<string, Label>();

        public static Dictionary<string, Label> cookiesLabels = new Dictionary<string, Label>();

        public static Dictionary<string, Label> cacheLabels = new Dictionary<string, Label>();

        public static Dictionary<string, Label> sessionLabels = new Dictionary<string, Label>();

        public static Dictionary<string, Label> extensionLabels = new Dictionary<string, Label>();

        public static Dictionary<string, Label> loginLabels = new Dictionary<string, Label>();

        public static Dictionary<string, Label> localStorageLabels = new Dictionary<string, Label>();

        public static Dictionary<string, Label> sessionStorageLabels = new Dictionary<string, Label>();

        public static Dictionary<string, Label> indexedDbLabels = new Dictionary<string, Label>();



        public static DataTable? dataTable;

        public static SQLiteDataAdapter? dataAdapter;

        public static DataTable? labelsTable;


        public static DateTime StartDate = DateTime.Now.AddYears(-20);
        public static DateTime EndDate = DateTime.Now;
        public static string sd = "";
        public static string ed = "";



        public static string sqltimecondition = "";
        public static string sqlDownloadtimecondition = "";
        public static string sqlBookmarkstimecondition = "";
        public static string sqlAutofilltimecondition = "";
        public static string sqlCookiestimecondition = "";
        public static string sqlCachetimecondition = "";






       public static Dictionary<string, List<string>> TablesAndFields = new Dictionary<string, List<string>>
    {
        { "results", new List<string> {
            "id", "Artifact_type", "Potential_activity", "Browser", "Category", "Visit_id", "Url", "Title",
            "Visit_time", "Visit_duration", "Last_visit_time", "Visit_count", "Typed_count",
            "From_url", "Transition", "File", "Label", "Comment"
        }},

        { "firefox_results", new List<string> {
            "id", "Artifact_type", "Potential_activity", "Browser", "Category", "Visit_id", "Place_id", "Url", "Title",
            "Visit_time", "Last_visit_time", "Visit_count", "From_visit", "Transition", "Navigation_context",
            "User_action_likelihood", "Visit_type", "Frecency",
            "File", "Label", "Comment"
        }},

        { "firefox_downloads", new List<string> {
            "id", "Artifact_type", "Potential_activity", "Browser", "Download_id", "Current_path", "End_time", "Last_visit_time",
            "Received_bytes", "Total_bytes", "Source_url", "Title", "State", "File", "Label", "Comment"
        }},

        { "chrome_downloads", new List<string> {
            "id", "Artifact_type", "Potential_activity", "Browser", "Current_path", "Target_path", "Url_chain", "Start_time", "End_time",
            "Received_bytes", "Total_bytes", "State", "opened", "referrer", "Site_url", "Tab_url",
            "Mime_type", "File", "Label", "Comment"
        }},

        { "bookmarks_Firefox", new List<string> {
            "id", "Artifact_type", "Potential_activity", "Browser", "Bookmark_id", "Type", "FK", "Parent",
            "Parent_name", "Title", "DateAdded", "LastModified", "URL", "PageTitle", "VisitCount",
            "LastVisitDate", "AnnoId", "AnnoContent", "AnnoName", "File", "Label", "Comment"
        }},

        { "bookmarks_Chrome", new List<string> {
            "id", "Artifact_type", "Potential_activity", "Browser", "Type", "Title", "URL", "DateAdded",
            "DateLastUsed", "LastModified", "Parent_name", "Guid", "ChromeId", "File", "Label", "Comment"

        }},

        { "autofill_data", new List<string> {
            "id", "Artifact_type", "Potential_activity", "Browser", "FieldName", "Value", "Count", "LastUsed", "TimesUsed", "FirstUsed",
            "File", "Label", "Comment"
        }},

        { "cookies_data", new List<string> {
            "id", "Artifact_type", "Potential_activity", "Browser", "Host", "Name", "Value", "Path", "Created", "Expires", "LastAccessed",
            "IsSecure", "IsHttpOnly", "IsPersistent", "SameSite", "SourceScheme", "SourcePort",
            "IsEncrypted", "File", "Label", "Comment"
        }},

        { "cache_data", new List<string> {
            "id", "Artifact_type", "Potential_activity", "Browser", "Url", "Host", "ContentType", "CacheType", "HttpStatus", "Server",
            "FileSize", "Created", "Modified", "LastAccessed", "CacheFile", "CacheKey",
            "BodySize", "BodySha256", "BodyStored", "BodyPreview", "DetectedFileType", "DetectedExtension",
            "File", "Label", "Comment"
        }},

        { "saved_logins_data", new List<string> {
            "id", "Artifact_type", "Potential_activity", "Browser", "Url", "Action_url", "Signon_realm",
            "Username", "Username_field", "Password_field", "Scheme", "Times_used", "Created", "Last_used",
            "Password_changed", "Is_blacklisted", "Is_federated", "Password_present", "Encrypted_password_sha256",
            "Encrypted_password_size", "Decryption_status", "Credential_artifact_value", "Store", "Login_guid",
            "File", "Label", "Comment"
        }},

        { "local_storage_data", new List<string> {
            "id", "Artifact_type", "Potential_activity", "Browser", "Origin", "Host", "Storage_key", "Value_preview",
            "Value_size", "Value_sha256", "Source_kind", "Source_file", "Created", "Modified", "LastAccessed",
            "Parser_notes", "File", "Label", "Comment"
        }},

        { "session_storage_data", new List<string> {
            "id", "Artifact_type", "Potential_activity", "Browser", "Origin", "Host", "Storage_key", "Value_preview",
            "Value_size", "Value_sha256", "Source_kind", "Source_file", "Created", "Modified", "LastAccessed",
            "Parser_notes", "File", "Label", "Comment"
        }},

        { "indexeddb_data", new List<string> {
            "id", "Artifact_type", "Potential_activity", "Browser", "Origin", "Host", "Storage_key", "Value_preview",
            "Value_size", "Value_sha256", "Source_kind", "Source_file", "Created", "Modified", "LastAccessed",
            "Parser_notes", "File", "Label", "Comment"
        }},

        { "session_data", new List<string> {
            "id", "Artifact_type", "Potential_activity", "Browser", "WindowIndex", "TabIndex", "EntryIndex", "Selected",
            "Url", "Title", "OriginalUrl", "Referrer", "LastAccessed", "Created", "SessionFile", "SourceType",
            "File", "Label", "Comment"
        }},

        { "extension_data", new List<string> {
            "id", "Artifact_type", "Potential_activity", "Browser", "ExtensionId", "Name", "Version", "Description", "Author",
            "HomepageUrl", "UpdateUrl", "InstallTime", "LastUpdateTime", "Enabled", "Permissions",
            "HostPermissions", "ManifestVersion", "ExtensionPath", "SourceFile", "File", "Label", "Comment"
        }},


        { "AllWebHistory", new List<string> {
            "id", "Artifact_type", "Potential_activity", "Navigation_context", "User_action_likelihood",
            "Browser", "Category", "Visit_id", "Url",  "Title",  "Visit_time", "File", "Label", "Comment"
        }},

        { "AllDownloads", new List<string> {
            "id", "Artifact_type", "Potential_activity", "Browser", "Current_path", "Target_path", "Url_chain",
            "End_time", "Received_bytes", "Total_bytes", "File", "Label", "Comment"
        }},

        { "AllBookmarks", new List<string> {
            "id", "Artifact_type", "Potential_activity", "Browser", "Type", "Title", "URL", "DateAdded", "LastModified", "Parent_name",
            "File", "Label", "Comment"
        }},

        { "AllAutoFill", new List<string> {
            "id", "Artifact_type", "Potential_activity", "Browser", "FieldName", "Value", "TimesUsed", "FirstUsed", "LastUsed",
            "File", "Label", "Comment"
        }},

        { "AllCookies", new List<string> {
            "id", "Artifact_type", "Potential_activity", "Browser", "Host", "Name", "Value", "Path", "Created", "Expires", "LastAccessed",
            "IsSecure", "IsHttpOnly", "IsPersistent", "SameSite", "SourceScheme", "SourcePort",
            "IsEncrypted", "File", "Label", "Comment"
        }},

        { "AllCache", new List<string> {
            "id", "Artifact_type", "Potential_activity", "Browser", "Url", "Host", "ContentType", "CacheType", "HttpStatus", "Server",
            "FileSize", "Created", "Modified", "LastAccessed", "CacheFile", "CacheKey",
            "BodySize", "BodySha256", "BodyStored", "BodyPreview", "DetectedFileType", "DetectedExtension",
            "File", "Label", "Comment"
        }},

        { "AllSessions", new List<string> {
            "id", "Artifact_type", "Potential_activity", "Browser", "WindowIndex", "TabIndex", "EntryIndex", "Selected",
            "Url", "Title", "OriginalUrl", "Referrer", "LastAccessed", "Created", "SessionFile", "SourceType",
            "File", "Label", "Comment"
        }},

        { "AllExtensions", new List<string> {
            "id", "Artifact_type", "Potential_activity", "Browser", "ExtensionId", "Name", "Version", "Description", "Author",
            "HomepageUrl", "UpdateUrl", "InstallTime", "LastUpdateTime", "Enabled", "Permissions",
            "HostPermissions", "ManifestVersion", "ExtensionPath", "SourceFile", "File", "Label", "Comment"
        }},

        { "AllSavedLogins", new List<string> {
            "id", "Artifact_type", "Potential_activity", "Browser", "Url", "Action_url", "Signon_realm",
            "Username", "Username_field", "Password_field", "Scheme", "Times_used", "Created", "Last_used",
            "Password_changed", "Is_blacklisted", "Is_federated", "Password_present", "Encrypted_password_sha256",
            "Encrypted_password_size", "Decryption_status", "Credential_artifact_value", "Store", "Login_guid",
            "File", "Label", "Comment"
        }},

        { "AllLocalStorage", new List<string> {
            "id", "Artifact_type", "Potential_activity", "Browser", "Origin", "Host", "Storage_key", "Value_preview",
            "Value_size", "Value_sha256", "Source_kind", "Source_file", "Created", "Modified", "LastAccessed",
            "Parser_notes", "File", "Label", "Comment"
        }},

        { "AllSessionStorage", new List<string> {
            "id", "Artifact_type", "Potential_activity", "Browser", "Origin", "Host", "Storage_key", "Value_preview",
            "Value_size", "Value_sha256", "Source_kind", "Source_file", "Created", "Modified", "LastAccessed",
            "Parser_notes", "File", "Label", "Comment"
        }},

        { "AllIndexedDb", new List<string> {
            "id", "Artifact_type", "Potential_activity", "Browser", "Origin", "Host", "Storage_key", "Value_preview",
            "Value_size", "Value_sha256", "Source_kind", "Source_file", "Created", "Modified", "LastAccessed",
            "Parser_notes", "File", "Label", "Comment"
        }}
    };



    }
}

