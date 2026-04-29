using Syncfusion.WinForms.DataGrid;
using System.Data.SQLite;
using System.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using Syncfusion.WinForms.GridCommon.ScrollAxis;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System;
using System.Globalization;
using System.Text.Json;




namespace Browser_Reviewer
{

   

    public class MyTools
    {
        private const string DownloadsParserVersion = "downloads-v2.0";
        private const string ProcessingSummaryVersion = "processing-summary-v1.0";
        private static readonly object ProcessedSourcesLock = new object();
        private static readonly HashSet<string> ProcessedSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, int> SkippedProcessedSourceCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct WIN32_FIND_DATA
        {
            public uint dwFileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr FindFirstFile(string lpFileName, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool FindClose(IntPtr hFindFile);

        const uint FILE_ATTRIBUTE_DIRECTORY = 0x10;





            
            public async Task ListFilesAndDirectories(string path, RichTextBox? textBox_logConsole, bool isRoot = true)
            {
                if (isRoot)
                {
                    ResetProcessedSources();
                    Helpers.ResetBrowserIdentityState();
                }

                BrowserReviewerLogger.ConfigureForDatabase(Helpers.db_name);

                try
                {
                    await Task.Run(async () =>
                    {
                        WIN32_FIND_DATA findData;
                        IntPtr hFind = new IntPtr(-1);
                        try
                        {
                            hFind = FindFirstFile(path + @"\*", out findData);

                            if (hFind != new IntPtr(-1))
                            {
                                do
                                {
                                    string currentFileName = findData.cFileName;

                                    if (currentFileName != "." && currentFileName != "..")
                                    {
                                        string fullPath = Path.Combine(path, currentFileName);

                                        await WriteToLog(fullPath);

                                        bool isDirectory = (findData.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) != 0;

                                        if (isDirectory)
                                        {
                                            ProcessDirectoryArtifact(fullPath, textBox_logConsole);
                                            await ListFilesAndDirectories(fullPath, textBox_logConsole, false);
                                        }
                                        else
                                        {
                                            try
                                            {
                                                ProcessFileArtifact(fullPath, textBox_logConsole);
                                            }
                                            catch (UnauthorizedAccessException)
                                            {
                                                LogToConsole(textBox_logConsole, $"Access denied to file: {fullPath}", Color.Red);
                                            }
                                            catch (PathTooLongException)
                                            {
                                                LogToConsole(textBox_logConsole, $"Path too long: {fullPath}", Color.Red);
                                            }
                                        }
                                    }
                                }
                    while (FindNextFile(hFind, out findData));
                            }
                }
                finally
                {
                    FindClose(hFind);
                }
                    });
            }
            catch (Exception ex)
            {
                LogToConsole(textBox_logConsole, $"Error processing directory {path}: {ex.Message}", Color.Red);
            }

            if (isRoot)
            {
                FlushProcessedSourceSummary(textBox_logConsole);
                RefreshProcessingSummary(Helpers.chromeViewerConnectionString, textBox_logConsole);
            }
        }

        public async Task CLI_ListFilesAndDirectories(string path)
        {
            await ListFilesAndDirectories(path, null, true);
        }

        public static void ResetProcessedSources()
        {
            lock (ProcessedSourcesLock)
            {
                ProcessedSources.Clear();
                SkippedProcessedSourceCounts.Clear();
            }
        }

        private static bool TryRegisterProcessedSource(string artifactKind, string sourcePath, RichTextBox? logConsole = null)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
                return true;

            string normalizedSource = NormalizeProcessedSourcePath(sourcePath);
            string key = $"{artifactKind}|{normalizedSource}";

            lock (ProcessedSourcesLock)
            {
                if (!ProcessedSources.Add(key))
                {
                    if (SkippedProcessedSourceCounts.TryGetValue(artifactKind, out int count))
                        SkippedProcessedSourceCounts[artifactKind] = count + 1;
                    else
                        SkippedProcessedSourceCounts[artifactKind] = 1;

                    return false;
                }
            }

            return true;
        }

        private static void FlushProcessedSourceSummary(RichTextBox? logConsole)
        {
            List<KeyValuePair<string, int>> summary;

            lock (ProcessedSourcesLock)
            {
                if (SkippedProcessedSourceCounts.Count == 0)
                    return;

                summary = SkippedProcessedSourceCounts
                    .Where(entry => entry.Value > 0)
                    .OrderByDescending(entry => entry.Value)
                    .ToList();
            }

            foreach (KeyValuePair<string, int> entry in summary)
            {
                LogToConsole(logConsole, $"Dedup summary: skipped {entry.Value} repeated source(s) for {entry.Key}.", Color.Gray);
            }
        }

        private static string NormalizeProcessedSourcePath(string sourcePath)
        {
            string normalized = sourcePath.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            try
            {
                normalized = Path.GetFullPath(normalized);
            }
            catch
            {
            }

            return normalized;
        }



        private async Task WriteToLog(string fullPath)
            {
                try
                {
                    await BrowserReviewerLogger.TracePathAsync(fullPath);
                }
                catch (Exception ex)
                {
                    BrowserReviewerLogger.Log(null, $"Error writing path trace: {ex.Message}", BrowserReviewerLogLevel.Warning);
                }
            }

            public static void CloseLog()
            {
                BrowserReviewerLogger.Close();
            }

        private void ProcessFileArtifact(string fullPath, RichTextBox? textBox_logConsole)
        {
            if (IsTemporaryBrowserArtifactPath(fullPath))
            {
                LogToConsole(textBox_logConsole, $"Skipping temporary browser artifact: {fullPath}", Color.DarkGray);
                return;
            }

            string fileName = Path.GetFileName(fullPath);
            string? profilePath = GetOwningBrowserProfileDirectory(fullPath);

            if (fileName.Equals("History", StringComparison.OrdinalIgnoreCase) && IsSQLite3(fullPath, textBox_logConsole) && SetBrowserType(fullPath))
            {
                Helpers.historyConnectionString = $"Data Source={fullPath};Version=3;";
                RunArtifactExtraction("History", fullPath, textBox_logConsole, () =>
                    ProcessAndWriteRecordsChromeLike(Helpers.historyConnectionString, Helpers.chromeViewerConnectionString, Helpers.BrowserType, fullPath, textBox_logConsole));
                RunArtifactExtraction("Downloads", fullPath, textBox_logConsole, () =>
                    ProcessAndWriteDownloadsChrome(Helpers.historyConnectionString, Helpers.chromeViewerConnectionString, Helpers.BrowserType, fullPath, textBox_logConsole));
                return;
            }

            if (fileName.Equals("Bookmarks", StringComparison.OrdinalIgnoreCase) && SetBrowserType(fullPath))
            {
                RunArtifactExtraction("Bookmarks", fullPath, textBox_logConsole, () =>
                    ProcessAndWriteBookmarksChrome(Helpers.chromeViewerConnectionString, Helpers.BrowserType, fullPath, textBox_logConsole));
                return;
            }

            if (fileName.Equals("Web Data", StringComparison.OrdinalIgnoreCase) && IsSQLite3(fullPath, textBox_logConsole) && SetBrowserType(fullPath))
            {
                RunArtifactExtraction("Autofill", fullPath, textBox_logConsole, () =>
                    ProcessAndWriteAutofillChrome(Helpers.chromeViewerConnectionString, Helpers.BrowserType, fullPath, textBox_logConsole));
                return;
            }

            if (fileName.Equals("Login Data", StringComparison.OrdinalIgnoreCase) && IsSQLite3(fullPath, textBox_logConsole) && SetBrowserType(fullPath) && !string.IsNullOrWhiteSpace(profilePath))
            {
                RunArtifactExtraction("Saved Logins", fullPath, textBox_logConsole, () =>
                    ProcessAndWriteSavedLogins(Helpers.chromeViewerConnectionString, Helpers.BrowserType, profilePath, textBox_logConsole));
                return;
            }

            if (IsChromiumCookiesArtifactFile(fullPath) && IsSQLite3(fullPath, textBox_logConsole) && SetBrowserType(fullPath))
            {
                RunArtifactExtraction("Cookies", fullPath, textBox_logConsole, () =>
                    ProcessAndWriteCookiesChrome(Helpers.chromeViewerConnectionString, Helpers.BrowserType, fullPath, textBox_logConsole));
                return;
            }

            if (fileName.Equals("places.sqlite", StringComparison.OrdinalIgnoreCase) && IsSQLite3(fullPath, textBox_logConsole) && SetBrowserType(fullPath))
            {
                Helpers.realFirefoxPlacesPath = fullPath;
                string directoryPath = Path.GetDirectoryName(fullPath)!;
                Helpers.realFirefoxFormPath = Path.Combine(directoryPath, "formhistory.sqlite");
                Helpers.historyConnectionString = $"Data Source={fullPath};Version=3;";

                RunArtifactExtraction("History", fullPath, textBox_logConsole, () =>
                    ProcessAndWriteRecordsFirefox(Helpers.historyConnectionString, Helpers.chromeViewerConnectionString, Helpers.BrowserType, fullPath, textBox_logConsole), true);
                RunArtifactExtraction("Downloads", fullPath, textBox_logConsole, () =>
                    ProcessAndWriteDownloadsFirefox(Helpers.historyConnectionString, Helpers.chromeViewerConnectionString, Helpers.BrowserType, fullPath, textBox_logConsole), true);
                RunArtifactExtraction("Bookmarks", fullPath, textBox_logConsole, () =>
                    ProcessAndWriteBookmarksFirefox(Helpers.historyConnectionString, Helpers.chromeViewerConnectionString, Helpers.BrowserType, fullPath, textBox_logConsole), true);
                return;
            }

            if (fileName.Equals("formhistory.sqlite", StringComparison.OrdinalIgnoreCase) && IsSQLite3(fullPath, textBox_logConsole) && SetBrowserType(fullPath))
            {
                string directoryPath = Path.GetDirectoryName(fullPath)!;
                Helpers.realFirefoxFormPath = fullPath;
                string placesPath = Path.Combine(directoryPath, "places.sqlite");
                if (File.Exists(placesPath))
                    Helpers.realFirefoxPlacesPath = placesPath;

                RunArtifactExtraction("Autofill", fullPath, textBox_logConsole, () =>
                    ProcessAndWriteAutofillFirefox(Helpers.chromeViewerConnectionString, Helpers.BrowserType, fullPath, textBox_logConsole), true);
                return;
            }

            if (fileName.Equals("cookies.sqlite", StringComparison.OrdinalIgnoreCase) && IsSQLite3(fullPath, textBox_logConsole) && SetBrowserType(fullPath))
            {
                string directoryPath = Path.GetDirectoryName(fullPath)!;
                string placesPath = Path.Combine(directoryPath, "places.sqlite");
                Helpers.realFirefoxPlacesPath = File.Exists(placesPath) ? placesPath : string.Empty;
                RunArtifactExtraction("Cookies", fullPath, textBox_logConsole, () =>
                    ProcessAndWriteCookiesFirefox(Helpers.chromeViewerConnectionString, Helpers.BrowserType, fullPath, textBox_logConsole), true);
                return;
            }

            if (fileName.Equals("logins.json", StringComparison.OrdinalIgnoreCase) && SetBrowserType(fullPath) && !string.IsNullOrWhiteSpace(profilePath))
            {
                RunArtifactExtraction("Saved Logins", fullPath, textBox_logConsole, () =>
                    ProcessAndWriteSavedLogins(Helpers.chromeViewerConnectionString, Helpers.BrowserType, profilePath, textBox_logConsole));
                return;
            }

            if (fileName.Equals("webappsstore.sqlite", StringComparison.OrdinalIgnoreCase) && IsSQLite3(fullPath, textBox_logConsole) && SetBrowserType(fullPath) && !string.IsNullOrWhiteSpace(profilePath))
            {
                RunArtifactExtraction("Local Storage", fullPath, textBox_logConsole, () =>
                    ProcessAndWriteLocalStorage(Helpers.chromeViewerConnectionString, Helpers.BrowserType, profilePath, textBox_logConsole));
                return;
            }

            if (fileName.Equals("extensions.json", StringComparison.OrdinalIgnoreCase) && SetBrowserType(fullPath) && !string.IsNullOrWhiteSpace(profilePath))
            {
                RunArtifactExtraction("Extensions", fullPath, textBox_logConsole, () =>
                    ProcessAndWriteExtensions(Helpers.chromeViewerConnectionString, Helpers.BrowserType, profilePath, textBox_logConsole));
                return;
            }

            if (IsSessionArtifactFile(fullPath) && SetBrowserType(fullPath))
            {
                RunArtifactExtraction("Sessions", fullPath, textBox_logConsole, () =>
                    ProcessAndWriteSessions(Helpers.chromeViewerConnectionString, Helpers.BrowserType, fullPath, textBox_logConsole));

                if (!string.IsNullOrWhiteSpace(profilePath) &&
                    (LooksLikeFirefoxProfile(profilePath) || Helpers.IsFirefoxLikeBrowser(Helpers.BrowserType)))
                {
                    RunArtifactExtraction("Session Storage", fullPath, textBox_logConsole, () =>
                        ProcessAndWriteSessionStorage(Helpers.chromeViewerConnectionString, Helpers.BrowserType, profilePath, textBox_logConsole));
                }
            }
        }

        private void ProcessDirectoryArtifact(string fullPath, RichTextBox? textBox_logConsole)
        {
            if (IsTemporaryBrowserArtifactPath(fullPath) || !SetBrowserType(fullPath))
                return;

            string? profilePath = GetOwningBrowserProfileDirectory(fullPath);
            if (string.IsNullOrWhiteSpace(profilePath))
                return;

            string directoryName = Path.GetFileName(fullPath);

            if (IsChromiumCacheArtifactDirectory(fullPath))
            {
                RunArtifactExtraction("Cache", fullPath, textBox_logConsole, () =>
                    ProcessAndWriteCacheChrome(Helpers.chromeViewerConnectionString, Helpers.BrowserType, fullPath, textBox_logConsole));
                return;
            }

            if (IsFirefoxCacheArtifactDirectory(fullPath))
            {
                RunArtifactExtraction("Cache", fullPath, textBox_logConsole, () =>
                    ProcessAndWriteCacheFirefox(Helpers.chromeViewerConnectionString, Helpers.BrowserType, fullPath, textBox_logConsole));
                return;
            }

            if (directoryName.Equals("Extensions", StringComparison.OrdinalIgnoreCase))
            {
                RunArtifactExtraction("Extensions", fullPath, textBox_logConsole, () =>
                    ProcessAndWriteExtensions(Helpers.chromeViewerConnectionString, Helpers.BrowserType, profilePath, textBox_logConsole));
                return;
            }

            if (IsChromiumLocalStorageArtifactDirectory(fullPath))
            {
                RunArtifactExtraction("Local Storage", fullPath, textBox_logConsole, () =>
                    ProcessAndWriteLocalStorage(Helpers.chromeViewerConnectionString, Helpers.BrowserType, profilePath, textBox_logConsole));
                return;
            }

            if (directoryName.Equals("Session Storage", StringComparison.OrdinalIgnoreCase))
            {
                RunArtifactExtraction("Session Storage", fullPath, textBox_logConsole, () =>
                    ProcessAndWriteSessionStorage(Helpers.chromeViewerConnectionString, Helpers.BrowserType, profilePath, textBox_logConsole));
                return;
            }

            if (IsIndexedDbArtifactDirectory(fullPath))
            {
                RunArtifactExtraction("IndexedDB", fullPath, textBox_logConsole, () =>
                    ProcessAndWriteIndexedDb(Helpers.chromeViewerConnectionString, Helpers.BrowserType, profilePath, textBox_logConsole));
                return;
            }

            if (directoryName.Equals("Sessions", StringComparison.OrdinalIgnoreCase) || directoryName.Equals("sessionstore-backups", StringComparison.OrdinalIgnoreCase))
            {
                RunArtifactExtraction("Sessions", fullPath, textBox_logConsole, () =>
                    ProcessAndWriteSessions(Helpers.chromeViewerConnectionString, Helpers.BrowserType, fullPath, textBox_logConsole));

                if (LooksLikeFirefoxProfile(profilePath) || Helpers.IsFirefoxLikeBrowser(Helpers.BrowserType))
                {
                    RunArtifactExtraction("Session Storage", fullPath, textBox_logConsole, () =>
                        ProcessAndWriteSessionStorage(Helpers.chromeViewerConnectionString, Helpers.BrowserType, profilePath, textBox_logConsole));
                }
            }
        }

        private void RunArtifactExtraction(string artifactName, string sourcePath, RichTextBox? textBox_logConsole, Action extraction, bool allowFirefoxTempFallback = false)
        {
            try
            {
                extraction();
                LogToConsole(textBox_logConsole, $"Processing {artifactName}: {sourcePath}", Color.Green);
            }
            catch (Exception ex)
            {
                LogToConsole(textBox_logConsole, $"Error processing {artifactName} from {sourcePath}: {ex.Message}", Color.Red);

                if (allowFirefoxTempFallback)
                    TryProcessFirefoxLockedArtifacts(sourcePath, textBox_logConsole);
            }
        }

        private void TryProcessFirefoxLockedArtifacts(string sourcePath, RichTextBox? textBox_logConsole)
        {
            string? directoryPath = Path.GetDirectoryName(sourcePath);
            if (string.IsNullOrWhiteSpace(directoryPath))
                return;

            string placesPath = Path.Combine(directoryPath, "places.sqlite");
            string formPath = Path.Combine(directoryPath, "formhistory.sqlite");
            string cookiesPath = Path.Combine(directoryPath, "cookies.sqlite");

            Helpers.realFirefoxFormPath = formPath;
            Helpers.realFirefoxPlacesPath = File.Exists(placesPath) ? placesPath : string.Empty;

            if (File.Exists(placesPath) || File.Exists(formPath) || File.Exists(cookiesPath))
                CopyAndProcessToTemp(directoryPath, textBox_logConsole);
        }
        private static void DeleteFirefoxTempArtifacts(string tempDirectoryPath, params string[] legacyTempFiles)
        {
            foreach (string legacyTempFile in legacyTempFiles)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(legacyTempFile) && File.Exists(legacyTempFile))
                        File.Delete(legacyTempFile);
                }
                catch
                {
                }
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(tempDirectoryPath) && Directory.Exists(tempDirectoryPath))
                    Directory.Delete(tempDirectoryPath, true);
            }
            catch
            {
            }
        }

        private void CopyAndProcessToTemp(string directoryPath, RichTextBox? textBox_logConsole)
        {
            string tempDir = string.Empty;
            string legacyTempPlacesPath = Path.Combine(Path.GetTempPath(), "places.sqlite");
            string legacyTempFormHistoryPath = Path.Combine(Path.GetTempPath(), "formhistory.sqlite");
            string legacyTempCookiesPath = Path.Combine(Path.GetTempPath(), "cookies.sqlite");

            try
            {
                string placesPath = Path.Combine(directoryPath, "places.sqlite");
                string formHistoryPath = Path.Combine(directoryPath, "formhistory.sqlite");
                string cookiesPath = Path.Combine(directoryPath, "cookies.sqlite");

                tempDir = Path.Combine(Path.GetTempPath(), "BrowserReviewerTemp", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDir);

                string tempPlacesPath = Path.Combine(tempDir, "places.sqlite");
                string tempFormHistoryPath = Path.Combine(tempDir, "formhistory.sqlite");
                string tempCookiesPath = Path.Combine(tempDir, "cookies.sqlite");

                if (File.Exists(placesPath))
                {
                    File.Copy(placesPath, tempPlacesPath, true);
                    LogToConsole(textBox_logConsole, $"places.sqlite copied to temporary location: {tempPlacesPath}");
                }

                if (File.Exists(formHistoryPath))
                {
                    File.Copy(formHistoryPath, tempFormHistoryPath, true);
                    LogToConsole(textBox_logConsole, $"formhistory.sqlite copied to temporary location: {tempFormHistoryPath}");
                }

                if (File.Exists(cookiesPath))
                {
                    File.Copy(cookiesPath, tempCookiesPath, true);
                    LogToConsole(textBox_logConsole, $"cookies.sqlite copied to temporary location: {tempCookiesPath}");
                }

                if (File.Exists(tempPlacesPath))
                {
                    Helpers.historyConnectionString = $"Data Source={tempPlacesPath};Version=3;";
                    ProcessAndWriteRecordsFirefox(Helpers.historyConnectionString, Helpers.chromeViewerConnectionString, Helpers.BrowserType, tempPlacesPath);
                    ProcessAndWriteDownloadsFirefox(Helpers.historyConnectionString, Helpers.chromeViewerConnectionString, Helpers.BrowserType, tempPlacesPath);
                    ProcessAndWriteBookmarksFirefox(Helpers.historyConnectionString, Helpers.chromeViewerConnectionString, Helpers.BrowserType, tempPlacesPath);
                    LogToConsole(textBox_logConsole, $"Processing places.sqlite from temporary location: {tempPlacesPath}");
                }

                if (File.Exists(tempFormHistoryPath))
                {
                    LogToConsole(textBox_logConsole, $"Processing formhistory.sqlite from temporary location: {tempFormHistoryPath}");
                    ProcessAndWriteAutofillFirefox(Helpers.chromeViewerConnectionString, Helpers.BrowserType, tempFormHistoryPath);
                }

                if (File.Exists(tempCookiesPath))
                {
                    LogToConsole(textBox_logConsole, $"Processing cookies.sqlite from temporary location: {tempCookiesPath}");
                    ProcessAndWriteCookiesFirefox(Helpers.chromeViewerConnectionString, Helpers.BrowserType, tempCookiesPath, textBox_logConsole);
                }
            }
            catch (Exception copyEx)
            {
                LogToConsole(textBox_logConsole, $"Error copying files to temporary location: {copyEx.Message}");
            }
            finally
            {
                DeleteFirefoxTempArtifacts(tempDir, legacyTempPlacesPath, legacyTempFormHistoryPath, legacyTempCookiesPath);
            }
        }

        private void PrintFilePermissions(string filePath, RichTextBox? textBox_logConsole)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(filePath);
                FileSecurity fileSecurity = fileInfo.GetAccessControl();
                AuthorizationRuleCollection acl = fileSecurity.GetAccessRules(true, true, typeof(NTAccount));

                LogToConsole(textBox_logConsole, $"Permissions for the file: {filePath}");

                foreach (FileSystemAccessRule rule in acl)
                {
                    LogToConsole(textBox_logConsole, $"  User/Group: {rule.IdentityReference.Value}");
                    LogToConsole(textBox_logConsole, $"  Permissions: {rule.FileSystemRights}");
                    LogToConsole(textBox_logConsole, $"  Access Type: {rule.AccessControlType}");
                    LogToConsole(textBox_logConsole, $"  Inherited: {rule.IsInherited}");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                LogToConsole(textBox_logConsole, $"Cannot access the permissions of the file {filePath}: {ex.Message}");
            }
            catch (Exception ex)
            {
                LogToConsole(textBox_logConsole, $"Error retrieving the permissions of {filePath}: {ex.Message}");
            }
        }


       
        public void ShowQueryOnDataGridView(SfDataGrid dataGrid, string connectionString, string query, Label labelItemCount, RichTextBox? logConsole)
        {


            if (!string.IsNullOrEmpty(connectionString))
            {
                try
                {
                    using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                    {
                        connection.Open();

                        using (SQLiteCommand command = new SQLiteCommand(query, connection))
                        {
                            SearchSql.AddParameters(command);

                            using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(command))
                            {
                                DataTable dataTable = new DataTable();
                                adapter.Fill(dataTable);

                                dataGrid.DataSource = dataTable;

                                Helpers.itemscount = dataGrid.RowCount - 1;
                                labelItemCount.Text = $"Items count: {dataGrid.RowCount - 1}";

                                if (dataGrid.View != null && dataGrid.View.Records.Count > 0)
                                {
                                    dataGrid.SelectedIndex = 0;
                                    dataGrid.MoveToCurrentCell(new RowColumnIndex(0, 0));
                                }

                                ApplySearchHighlight(dataGrid);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (logConsole != null)
                        LogToConsole(logConsole, $"Query error: {ex.Message}", Color.Red);
                    else
                        Console.WriteLine($"Query error: {ex.Message}");

                }
            }

           
        }




        public void CreateDatabase(string connectionString)
        {


            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string setPragma = "PRAGMA foreign_keys = ON;";

                string createTableLabels = @"CREATE TABLE IF NOT EXISTS Labels (
                                            Label_name TEXT PRIMARY KEY NOT NULL,
                                            Label_color INTEGER
                                            )";

                string createTableChrome = @"CREATE TABLE IF NOT EXISTS results (
                                                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                                                    Artifact_type TEXT,
                                                    Potential_activity TEXT,
                                                    Browser TEXT,
                                                    Category TEXT,
                                                    Visit_id INTEGER,
                                                    Url TEXT NOT NULL,
                                                    Title TEXT,
                                                    Visit_time DATETIME,
                                                    Visit_duration TEXT,
                                                    Last_visit_time DATETIME,
                                                    Visit_count INTEGER,
                                                    Typed_count INTEGER,                         
                                                    From_url TEXT,
                                                    Transition TEXT,
                                                    File    Text,
                                                    Label TEXT,
                                                    Comment TEXT,
                                                    FOREIGN KEY (Label) REFERENCES Labels(Label_name)
                                                    )";


                string createTableFirefox = @"CREATE TABLE IF NOT EXISTS firefox_results (
                                                     id INTEGER PRIMARY KEY AUTOINCREMENT,
                                                     Artifact_type TEXT,
                                                     Potential_activity TEXT,
                                                     Browser TEXT,
                                                     Category TEXT,
                                                     Visit_id INTEGER,
                                                     Place_id INTEGER,
                                                     From_visit INTEGER,
                                                     Url TEXT NOT NULL,
                                                     Title TEXT,
                                                     Visit_time DATETIME,
                                                     Last_visit_time DATETIME,
                                                     Visit_count INTEGER,
                                                     Transition TEXT,
                                                     Navigation_context TEXT,
                                                     User_action_likelihood TEXT,
                                                     Visit_type INTEGER,
                                                     Frecency INTEGER,
                                                     File TEXT,
                                                     Label TEXT,
                                                     Comment TEXT,
                                                     FOREIGN KEY (Label) REFERENCES Labels(Label_name)
                                                     )";

                string createTableChromeDownloads = @"CREATE TABLE IF NOT EXISTS chrome_downloads (
                                                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                                                    Artifact_type TEXT,
                                                    Potential_activity TEXT,
                                                    Browser TEXT,
                                                    Parser_version TEXT,
                                                    Parse_confidence TEXT,
                                                    Evidence_source TEXT,
                                                    Parser_notes TEXT,
                                                    Download_id INTEGER,
                                                    Current_path TEXT,
                                                    Target_path TEXT,
                                                    Url_chain TEXT,
                                                    Start_time DATETIME,
                                                    End_time DATETIME,
                                                    Received_bytes INTEGER,
                                                    Total_bytes INTEGER,
                                                    State TEXT,
                                                    opened TEXT,
                                                    referrer TEXT,
                                                    Site_url TEXT,
                                                    Tab_url TEXT,
                                                    Mime_type TEXT,
                                                    File TEXT,
                                                    Label TEXT,
                                                    Comment TEXT,
                                                    FOREIGN KEY (Label) REFERENCES Labels(Label_name)
                                                    )";

                string createTableFirefoxDownloads = @"CREATE TABLE IF NOT EXISTS firefox_downloads (
                                                     id INTEGER PRIMARY KEY AUTOINCREMENT,
                                                     Artifact_type TEXT,
                                                     Potential_activity TEXT,
                                                     Browser TEXT,
                                                     Parser_version TEXT,
                                                     Parse_confidence TEXT,
                                                     Evidence_source TEXT,
                                                     Parser_notes TEXT,
                                                     Download_id INTEGER,
                                                     Current_path TEXT,
                                                     End_time DATETIME,
                                                     Last_visit_time DATETIME,
                                                     Received_bytes INTEGER,
                                                     Total_bytes INTEGER,
                                                     Source_url TEXT,
                                                     Title TEXT,  
                                                     State TEXT,
                                                     File TEXT,
                                                     Label TEXT,
                                                     Comment TEXT,
                                                     FOREIGN KEY (Label) REFERENCES Labels(Label_name)
                                                     )";


                string createTableChromeBookmarks = @"CREATE TABLE IF NOT EXISTS bookmarks_Chrome (
                                                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                                                    Artifact_type TEXT,
                                                    Potential_activity TEXT,
                                                    Browser TEXT,
                                                    Type TEXT,
                                                    Title TEXT,
                                                    URL TEXT,
                                                    DateAdded DATETIME,
                                                    DateLastUsed DATETIME,
                                                    LastModified DATETIME,
                                                    Parent_name TEXT,
                                                    Guid TEXT,
                                                    ChromeId TEXT,
                                                    File TEXT,
                                                    Label TEXT,
                                                    Comment TEXT,
                                                    FOREIGN KEY (Label) REFERENCES Labels(Label_name)
                                                    )";



                string createTableFirefoxBookmarks = @"CREATE TABLE IF NOT EXISTS bookmarks_Firefox (
                                                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                                                    Artifact_type TEXT,
                                                    Potential_activity TEXT,
                                                    Browser TEXT,
                                                    Bookmark_id INTEGER,
                                                    Type TEXT,
                                                    FK INTEGER,
                                                    Parent INTEGER,
                                                    Parent_name TEXT,
                                                    Title TEXT,
                                                    DateAdded DATETIME,
                                                    LastModified DATETIME,
                                                    URL TEXT,
                                                    PageTitle TEXT,
                                                    VisitCount INTEGER,
                                                    LastVisitDate DATETIME,
                                                    AnnoId INTEGER,
                                                    AnnoContent TEXT,
                                                    AnnoName TEXT,
                                                    File TEXT,
                                                    Label TEXT,
                                                    Comment TEXT,
                                                    FOREIGN KEY (Label) REFERENCES Labels(Label_name)
                                                    )";



                string createTableAutoFill = @"CREATE TABLE IF NOT EXISTS autofill_data (
                                                id INTEGER PRIMARY KEY AUTOINCREMENT,
                                                Artifact_type TEXT,
                                                Potential_activity TEXT,
                                                Browser TEXT,
                                                FieldName TEXT,
                                                Value TEXT,
                                                Count INTEGER,
                                                LastUsed DATETIME,
                                                TimesUsed INTEGER,
                                                FirstUsed DATETIME,
                                                File TEXT,
                                                Label TEXT,
                                                Comment TEXT,
                                                FOREIGN KEY (Label) REFERENCES Labels(Label_name)
                                                )";


                string createTableCookies = @"CREATE TABLE IF NOT EXISTS cookies_data (
                                                id INTEGER PRIMARY KEY AUTOINCREMENT,
                                                Artifact_type TEXT,
                                                Potential_activity TEXT,
                                                Browser TEXT,
                                                Host TEXT,
                                                Name TEXT,
                                                Value TEXT,
                                                Path TEXT,
                                                Created DATETIME,
                                                Expires DATETIME,
                                                LastAccessed DATETIME,
                                                IsSecure INTEGER,
                                                IsHttpOnly INTEGER,
                                                IsPersistent INTEGER,
                                                SameSite TEXT,
                                                SourceScheme TEXT,
                                                SourcePort INTEGER,
                                                IsEncrypted INTEGER,
                                                File TEXT,
                                                Label TEXT,
                                                Comment TEXT,
                                                FOREIGN KEY (Label) REFERENCES Labels(Label_name)
                                                )";


                string createTableCache = @"CREATE TABLE IF NOT EXISTS cache_data (
                                                id INTEGER PRIMARY KEY AUTOINCREMENT,
                                                Artifact_type TEXT,
                                                Potential_activity TEXT,
                                                Browser TEXT,
                                                Url TEXT,
                                                Host TEXT,
                                                ContentType TEXT,
                                                CacheType TEXT,
                                                HttpStatus TEXT,
                                                Server TEXT,
                                                FileSize INTEGER,
                                                Created DATETIME,
                                                Modified DATETIME,
                                                LastAccessed DATETIME,
                                                CacheFile TEXT,
                                                CacheKey TEXT,
                                                Body BLOB,
                                                BodySize INTEGER,
                                                BodySha256 TEXT,
                                                BodyStored INTEGER DEFAULT 0,
                                                BodyPreview TEXT,
                                                DetectedFileType TEXT,
                                                DetectedExtension TEXT,
                                                File TEXT,
                                                Label TEXT,
                                                Comment TEXT,
                                                FOREIGN KEY (Label) REFERENCES Labels(Label_name)
                                                )";

                string createTableSessions = @"CREATE TABLE IF NOT EXISTS session_data (
                                                id INTEGER PRIMARY KEY AUTOINCREMENT,
                                                Artifact_type TEXT,
                                                Potential_activity TEXT,
                                                Browser TEXT,
                                                WindowIndex INTEGER,
                                                TabIndex INTEGER,
                                                EntryIndex INTEGER,
                                                Selected INTEGER,
                                                Url TEXT,
                                                Title TEXT,
                                                OriginalUrl TEXT,
                                                Referrer TEXT,
                                                LastAccessed DATETIME,
                                                Created DATETIME,
                                                SessionFile TEXT,
                                                SourceType TEXT,
                                                File TEXT,
                                                Label TEXT,
                                                Comment TEXT,
                                                FOREIGN KEY (Label) REFERENCES Labels(Label_name)
                                                )";

                string createTableExtensions = @"CREATE TABLE IF NOT EXISTS extension_data (
                                                id INTEGER PRIMARY KEY AUTOINCREMENT,
                                                Artifact_type TEXT,
                                                Potential_activity TEXT,
                                                Browser TEXT,
                                                ExtensionId TEXT,
                                                Name TEXT,
                                                Version TEXT,
                                                Description TEXT,
                                                Author TEXT,
                                                HomepageUrl TEXT,
                                                UpdateUrl TEXT,
                                                InstallTime DATETIME,
                                                LastUpdateTime DATETIME,
                                                Enabled INTEGER,
                                                Permissions TEXT,
                                                HostPermissions TEXT,
                                                ManifestVersion INTEGER,
                                                ExtensionPath TEXT,
                                                SourceFile TEXT,
                                                File TEXT,
                                                Label TEXT,
                                                Comment TEXT,
                                                FOREIGN KEY (Label) REFERENCES Labels(Label_name)
                                                )";

                string createTableSavedLogins = @"CREATE TABLE IF NOT EXISTS saved_logins_data (
                                                id INTEGER PRIMARY KEY AUTOINCREMENT,
                                                Artifact_type TEXT,
                                                Potential_activity TEXT,
                                                Browser TEXT,
                                                Url TEXT,
                                                Action_url TEXT,
                                                Signon_realm TEXT,
                                                Username TEXT,
                                                Username_field TEXT,
                                                Password_field TEXT,
                                                Scheme TEXT,
                                                Times_used INTEGER,
                                                Created DATETIME,
                                                Last_used DATETIME,
                                                Password_changed DATETIME,
                                                Is_blacklisted INTEGER,
                                                Is_federated INTEGER,
                                                Password_present INTEGER,
                                                Encrypted_password_sha256 TEXT,
                                                Encrypted_password_size INTEGER,
                                                Decryption_status TEXT,
                                                Credential_artifact_value TEXT,
                                                Store TEXT,
                                                Login_guid TEXT,
                                                File TEXT,
                                                Label TEXT,
                                                Comment TEXT,
                                                FOREIGN KEY (Label) REFERENCES Labels(Label_name)
                                                )";

                string createTableLocalStorage = @"CREATE TABLE IF NOT EXISTS local_storage_data (
                                                id INTEGER PRIMARY KEY AUTOINCREMENT,
                                                Artifact_type TEXT,
                                                Potential_activity TEXT,
                                                Browser TEXT,
                                                Origin TEXT,
                                                Host TEXT,
                                                Storage_key TEXT,
                                                Value_preview TEXT,
                                                Value_size INTEGER,
                                                Value_sha256 TEXT,
                                                Source_kind TEXT,
                                                Source_file TEXT,
                                                Created DATETIME,
                                                Modified DATETIME,
                                                LastAccessed DATETIME,
                                                Parser_notes TEXT,
                                                File TEXT,
                                                Label TEXT,
                                                Comment TEXT,
                                                FOREIGN KEY (Label) REFERENCES Labels(Label_name)
                                                )";

                string createTableSessionStorage = @"CREATE TABLE IF NOT EXISTS session_storage_data (
                                                id INTEGER PRIMARY KEY AUTOINCREMENT,
                                                Artifact_type TEXT,
                                                Potential_activity TEXT,
                                                Browser TEXT,
                                                Origin TEXT,
                                                Host TEXT,
                                                Storage_key TEXT,
                                                Value_preview TEXT,
                                                Value_size INTEGER,
                                                Value_sha256 TEXT,
                                                Source_kind TEXT,
                                                Source_file TEXT,
                                                Created DATETIME,
                                                Modified DATETIME,
                                                LastAccessed DATETIME,
                                                Parser_notes TEXT,
                                                File TEXT,
                                                Label TEXT,
                                                Comment TEXT,
                                                FOREIGN KEY (Label) REFERENCES Labels(Label_name)
                                                )";

                string createTableIndexedDb = @"CREATE TABLE IF NOT EXISTS indexeddb_data (
                                                id INTEGER PRIMARY KEY AUTOINCREMENT,
                                                Artifact_type TEXT,
                                                Potential_activity TEXT,
                                                Browser TEXT,
                                                Origin TEXT,
                                                Host TEXT,
                                                Storage_key TEXT,
                                                Value_preview TEXT,
                                                Value_size INTEGER,
                                                Value_sha256 TEXT,
                                                Source_kind TEXT,
                                                Source_file TEXT,
                                                Created DATETIME,
                                                Modified DATETIME,
                                                LastAccessed DATETIME,
                                                Parser_notes TEXT,
                                                File TEXT,
                                                Label TEXT,
                                                Comment TEXT,
                                                FOREIGN KEY (Label) REFERENCES Labels(Label_name)
                                                )";

                string createProcessingSummary = @"CREATE TABLE IF NOT EXISTS processing_summary (
                                                id INTEGER PRIMARY KEY AUTOINCREMENT,
                                                Section TEXT NOT NULL,
                                                Browser TEXT,
                                                Category TEXT,
                                                ItemCount INTEGER NOT NULL
                                                )";

                string createProcessingSummaryMeta = @"CREATE TABLE IF NOT EXISTS processing_summary_meta (
                                                Summary_key TEXT PRIMARY KEY NOT NULL,
                                                Summary_value TEXT
                                                )";



                using (SQLiteCommand command = new SQLiteCommand(setPragma, connection))
                {
                    command.ExecuteNonQuery();
                }

                using (SQLiteCommand command = new SQLiteCommand(createTableLabels, connection))
                {
                    command.ExecuteNonQuery();
                }

                using (SQLiteCommand command = new SQLiteCommand(createTableChrome, connection))
                {
                    command.ExecuteNonQuery();
                }

                using (SQLiteCommand command = new SQLiteCommand(createTableFirefox, connection))
                {
                    command.ExecuteNonQuery();
                }

                using (SQLiteCommand command = new SQLiteCommand(createTableChromeDownloads, connection))
                {
                    command.ExecuteNonQuery();
                }

                using (SQLiteCommand command = new SQLiteCommand(createTableFirefoxDownloads, connection))
                {
                    command.ExecuteNonQuery();
                }

                using (SQLiteCommand command = new SQLiteCommand(createTableChromeBookmarks, connection))
                {
                    command.ExecuteNonQuery();
                }

                using (SQLiteCommand command = new SQLiteCommand(createTableFirefoxBookmarks, connection))
                {
                    command.ExecuteNonQuery();
                }

                using (SQLiteCommand command = new SQLiteCommand(createTableAutoFill, connection))
                {
                    command.ExecuteNonQuery();
                }

                using (SQLiteCommand command = new SQLiteCommand(createTableCookies, connection))
                {
                    command.ExecuteNonQuery();
                }

                using (SQLiteCommand command = new SQLiteCommand(createTableCache, connection))
                {
                    command.ExecuteNonQuery();
                }

                using (SQLiteCommand command = new SQLiteCommand(createTableSessions, connection))
                {
                    command.ExecuteNonQuery();
                }

                using (SQLiteCommand command = new SQLiteCommand(createTableExtensions, connection))
                {
                    command.ExecuteNonQuery();
                }

                using (SQLiteCommand command = new SQLiteCommand(createTableSavedLogins, connection))
                {
                    command.ExecuteNonQuery();
                }

                using (SQLiteCommand command = new SQLiteCommand(createTableLocalStorage, connection))
                {
                    command.ExecuteNonQuery();
                }

                using (SQLiteCommand command = new SQLiteCommand(createTableSessionStorage, connection))
                {
                    command.ExecuteNonQuery();
                }

                using (SQLiteCommand command = new SQLiteCommand(createTableIndexedDb, connection))
                {
                    command.ExecuteNonQuery();
                }

                using (SQLiteCommand command = new SQLiteCommand(createProcessingSummary, connection))
                {
                    command.ExecuteNonQuery();
                }

                using (SQLiteCommand command = new SQLiteCommand(createProcessingSummaryMeta, connection))
                {
                    command.ExecuteNonQuery();
                }

            }
        }

        private static void EnsureProcessingSummaryTables(SQLiteConnection connection, SQLiteTransaction? transaction = null)
        {
            string createProcessingSummary = @"CREATE TABLE IF NOT EXISTS processing_summary (
                                                id INTEGER PRIMARY KEY AUTOINCREMENT,
                                                Section TEXT NOT NULL,
                                                Browser TEXT,
                                                Category TEXT,
                                                ItemCount INTEGER NOT NULL
                                                )";

            string createProcessingSummaryMeta = @"CREATE TABLE IF NOT EXISTS processing_summary_meta (
                                                Summary_key TEXT PRIMARY KEY NOT NULL,
                                                Summary_value TEXT
                                                )";

            using (SQLiteCommand command = new SQLiteCommand(createProcessingSummary, connection, transaction))
            {
                command.ExecuteNonQuery();
            }

            using (SQLiteCommand command = new SQLiteCommand(createProcessingSummaryMeta, connection, transaction))
            {
                command.ExecuteNonQuery();
            }
        }

        public static void RefreshProcessingSummary(string connectionString, RichTextBox? logConsole = null)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return;

            try
            {
                using SQLiteConnection connection = new SQLiteConnection(connectionString);
                connection.Open();
                using SQLiteTransaction transaction = connection.BeginTransaction();

                EnsureProcessingSummaryTables(connection, transaction);
                ExecuteSummaryNonQuery(connection, transaction, "DELETE FROM processing_summary;");
                ExecuteSummaryNonQuery(connection, transaction, "DELETE FROM processing_summary_meta;");

                SetProcessingSummaryMeta(connection, transaction, "summary_version", ProcessingSummaryVersion);
                SetProcessingSummaryMeta(connection, transaction, "status", "building");
                SetProcessingSummaryMeta(connection, transaction, "generated_at_utc", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"));

                InsertProcessingSummary(connection, transaction, @"
                    SELECT 'history_browser', Browser, NULL, COUNT(*)
                    FROM (
                        SELECT Browser FROM results
                        UNION ALL
                        SELECT Browser FROM firefox_results
                    )
                    GROUP BY Browser;");

                InsertProcessingSummary(connection, transaction, @"
                    SELECT 'history_category', Browser, COALESCE(NULLIF(Category, ''), 'Other'), COUNT(*)
                    FROM (
                        SELECT Browser, Category FROM results
                        UNION ALL
                        SELECT Browser, Category FROM firefox_results
                    )
                    GROUP BY Browser, COALESCE(NULLIF(Category, ''), 'Other');");

                InsertProcessingSummary(connection, transaction, @"
                    SELECT 'downloads', Browser, NULL, COUNT(*)
                    FROM (
                        SELECT Browser FROM chrome_downloads
                        UNION ALL
                        SELECT Browser FROM firefox_downloads
                    )
                    GROUP BY Browser;");

                InsertProcessingSummary(connection, transaction, @"
                    SELECT 'bookmarks', Browser, NULL, COUNT(*)
                    FROM (
                        SELECT Browser FROM bookmarks_Chrome
                        UNION ALL
                        SELECT Browser FROM bookmarks_Firefox
                    )
                    GROUP BY Browser;");

                InsertProcessingSummary(connection, transaction, @"SELECT 'autofill', Browser, NULL, COUNT(*) FROM autofill_data GROUP BY Browser;");
                InsertProcessingSummary(connection, transaction, @"SELECT 'cookies', Browser, NULL, COUNT(*) FROM cookies_data GROUP BY Browser;");
                InsertProcessingSummary(connection, transaction, @"SELECT 'cache', Browser, NULL, COUNT(*) FROM cache_data GROUP BY Browser;");
                InsertProcessingSummary(connection, transaction, @"SELECT 'sessions', Browser, NULL, COUNT(*) FROM session_data GROUP BY Browser;");
                InsertProcessingSummary(connection, transaction, @"SELECT 'extensions', Browser, NULL, COUNT(*) FROM extension_data GROUP BY Browser;");
                InsertProcessingSummary(connection, transaction, @"SELECT 'logins', Browser, NULL, COUNT(*) FROM saved_logins_data GROUP BY Browser;");
                InsertProcessingSummary(connection, transaction, @"SELECT 'local_storage', Browser, NULL, COUNT(*) FROM local_storage_data GROUP BY Browser;");
                InsertProcessingSummary(connection, transaction, @"SELECT 'session_storage', Browser, NULL, COUNT(*) FROM session_storage_data GROUP BY Browser;");
                InsertProcessingSummary(connection, transaction, @"SELECT 'indexeddb', Browser, NULL, COUNT(*) FROM indexeddb_data GROUP BY Browser;");

                SetProcessingSummaryMeta(connection, transaction, "status", "complete");
                transaction.Commit();

                LogToConsole(logConsole, "Processing summary refreshed.", Color.DarkGreen);
            }
            catch (Exception ex)
            {
                LogToConsole(logConsole, $"Processing summary refresh failed: {ex.Message}", Color.DarkRed);
            }
        }

        public static bool TryLoadProcessingSummary(string connectionString, RichTextBox? logConsole = null)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return false;

            try
            {
                using SQLiteConnection connection = new SQLiteConnection(connectionString);
                connection.Open();
                EnsureProcessingSummaryTables(connection);

                string? status = GetProcessingSummaryMeta(connection, "status");
                string? version = GetProcessingSummaryMeta(connection, "summary_version");
                if (!string.Equals(status, "complete", StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(version, ProcessingSummaryVersion, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                Helpers.browserHistoryCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                Helpers.browserHistoryCategoryCounts = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
                Helpers.browsersWithDownloads = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                Helpers.browsersWithBookmarks = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                Helpers.browsersWithAutofill = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                Helpers.browsersWithCookies = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                Helpers.browsersWithCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                Helpers.browsersWithSessions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                Helpers.browsersWithExtensions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                Helpers.browsersWithLogins = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                Helpers.browsersWithLocalStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                Helpers.browsersWithSessionStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                Helpers.browsersWithIndexedDb = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                using SQLiteCommand command = new SQLiteCommand("SELECT Section, Browser, Category, ItemCount FROM processing_summary;", connection);
                using SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    string section = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
                    string browser = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                    string category = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
                    int count = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);

                    switch (section)
                    {
                        case "history_browser":
                            Helpers.browserHistoryCounts[browser] = count;
                            break;
                        case "history_category":
                            if (!Helpers.browserHistoryCategoryCounts.TryGetValue(browser, out Dictionary<string, int>? categoryCounts))
                            {
                                categoryCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                                Helpers.browserHistoryCategoryCounts[browser] = categoryCounts;
                            }

                            categoryCounts[category] = count;
                            break;
                        case "downloads":
                            Helpers.browsersWithDownloads[browser] = count;
                            break;
                        case "bookmarks":
                            Helpers.browsersWithBookmarks[browser] = count;
                            break;
                        case "autofill":
                            Helpers.browsersWithAutofill[browser] = count;
                            break;
                        case "cookies":
                            Helpers.browsersWithCookies[browser] = count;
                            break;
                        case "cache":
                            Helpers.browsersWithCache[browser] = count;
                            break;
                        case "sessions":
                            Helpers.browsersWithSessions[browser] = count;
                            break;
                        case "extensions":
                            Helpers.browsersWithExtensions[browser] = count;
                            break;
                        case "logins":
                            Helpers.browsersWithLogins[browser] = count;
                            break;
                        case "local_storage":
                            Helpers.browsersWithLocalStorage[browser] = count;
                            break;
                        case "session_storage":
                            Helpers.browsersWithSessionStorage[browser] = count;
                            break;
                        case "indexeddb":
                            Helpers.browsersWithIndexedDb[browser] = count;
                            break;
                    }
                }

                LogToConsole(logConsole, "Loaded processing summary.", Color.DarkGreen);
                return true;
            }
            catch (Exception ex)
            {
                LogToConsole(logConsole, $"Processing summary load failed: {ex.Message}", Color.DarkRed);
                return false;
            }
        }

        private static void InsertProcessingSummary(SQLiteConnection connection, SQLiteTransaction transaction, string selectSql)
        {
            using SQLiteCommand command = new SQLiteCommand($@"
                INSERT INTO processing_summary (Section, Browser, Category, ItemCount)
                {selectSql}", connection, transaction);
            command.ExecuteNonQuery();
        }

        private static void ExecuteSummaryNonQuery(SQLiteConnection connection, SQLiteTransaction transaction, string sql)
        {
            using SQLiteCommand command = new SQLiteCommand(sql, connection, transaction);
            command.ExecuteNonQuery();
        }

        private static void SetProcessingSummaryMeta(SQLiteConnection connection, SQLiteTransaction transaction, string key, string value)
        {
            using SQLiteCommand command = new SQLiteCommand(@"
                INSERT INTO processing_summary_meta (Summary_key, Summary_value)
                VALUES (@key, @value)
                ON CONFLICT(Summary_key) DO UPDATE SET Summary_value = excluded.Summary_value;", connection, transaction);
            command.Parameters.AddWithValue("@key", key);
            command.Parameters.AddWithValue("@value", value);
            command.ExecuteNonQuery();
        }

        private static string? GetProcessingSummaryMeta(SQLiteConnection connection, string key)
        {
            using SQLiteCommand command = new SQLiteCommand("SELECT Summary_value FROM processing_summary_meta WHERE Summary_key = @key;", connection);
            command.Parameters.AddWithValue("@key", key);
            object? value = command.ExecuteScalar();
            return value?.ToString();
        }

        public static void ProcessAndWriteDownloadsChrome(string historyConnectionString, string chromeViewerConnectionString, string browserType, string filePath, RichTextBox? logConsole = null)
        {
            if (!TryRegisterProcessedSource("chrome-downloads", filePath, logConsole))
                return;

            if (string.IsNullOrEmpty(historyConnectionString) || string.IsNullOrEmpty(chromeViewerConnectionString))
            {
                if (logConsole != null)
                    LogToConsole(logConsole, "Provide valid database connection strings.", Color.Red);
                else
                    Console.WriteLine("Provide valid database connection strings.");

                return;
            }

            try
            {
                using (SQLiteConnection downloadsConnection = new SQLiteConnection(historyConnectionString))
                using (SQLiteConnection resultsConnection = new SQLiteConnection(chromeViewerConnectionString))
                {
                    downloadsConnection.Open();
                    resultsConnection.Open();

                    HashSet<string> downloadColumns = GetSqliteColumns(downloadsConnection, "downloads");
                    HashSet<string> chainColumns = GetSqliteColumns(downloadsConnection, "downloads_url_chains");

                    if (!downloadColumns.Contains("id"))
                    {
                        return;
                    }

                    bool hasUrlChains = chainColumns.Contains("id") && chainColumns.Contains("url");
                    string urlChainExpression = hasUrlChains
                        ? "(SELECT GROUP_CONCAT(url, ' -> ') FROM downloads_url_chains duc WHERE duc.id = d.id) AS Url_chain"
                        : "NULL AS Url_chain";

                    string query = $@"SELECT 
                            d.id AS id,
                            {SelectColumn(downloadColumns, "current_path", "NULL")},
                            {SelectColumn(downloadColumns, "target_path", "NULL")},
                            {SelectColumn(downloadColumns, "start_time", "0")},
                            {SelectColumn(downloadColumns, "end_time", "0")},
                            {SelectColumn(downloadColumns, "received_bytes", "0")},
                            {SelectColumn(downloadColumns, "total_bytes", "0")},
                            {SelectColumn(downloadColumns, "state", "0")},
                            {SelectColumn(downloadColumns, "opened", "0")},
                            {SelectColumn(downloadColumns, "referrer", "NULL")},
                            {SelectColumn(downloadColumns, "site_url", "NULL")},
                            {SelectColumn(downloadColumns, "tab_url", "NULL")},
                            {SelectColumn(downloadColumns, "mime_type", "NULL")},
                            {urlChainExpression}
                        FROM downloads d";

                    using (SQLiteCommand command = new SQLiteCommand(query, downloadsConnection))
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        using (var transaction = resultsConnection.BeginTransaction())
                        {
                            int inserted = 0;
                            int highConfidence = 0;
                            int mediumConfidence = 0;
                            string parserNotes = hasUrlChains
                                ? "Parsed Chrome downloads table and downloads_url_chains when present."
                                : "Parsed Chrome downloads table; downloads_url_chains table was not present.";

                            while (reader.Read())
                            {
                                int downloadId = GetNullableInt32(reader, "id");
                                string? currentPath = GetNullableString(reader, "current_path");
                                string? targetPath = GetNullableString(reader, "target_path");
                                long startTimeMicroseconds = GetNullableInt64(reader, "start_time");
                                long endTimeMicroseconds = GetNullableInt64(reader, "end_time");
                                long receivedBytes = GetNullableInt64(reader, "received_bytes");
                                long totalBytes = GetNullableInt64(reader, "total_bytes");
                                int state = GetNullableInt32(reader, "state");
                                int opened = GetNullableInt32(reader, "opened");
                                string? referrer = GetNullableString(reader, "referrer");
                                string? siteUrl = GetNullableString(reader, "site_url");
                                string? tabUrl = GetNullableString(reader, "tab_url");
                                string? mimeType = GetNullableString(reader, "mime_type");
                                string? urlChain = GetNullableString(reader, "Url_chain");

                                DateTime? startTime = ChromeTimestampToDateTime(startTimeMicroseconds);
                                DateTime? endTime = ChromeTimestampToDateTime(endTimeMicroseconds);

                                string stateDescription = InterpretDownloadState(state);

                                string openedDescription = opened == 1 ? "Yes" : "No";
                                string parseConfidence = startTime.HasValue || endTime.HasValue || !string.IsNullOrWhiteSpace(currentPath) || !string.IsNullOrWhiteSpace(targetPath)
                                    ? "High"
                                    : "Medium";

                                string insertQuery = @"INSERT INTO chrome_downloads 
                                               (Artifact_type, Potential_activity, Browser, Parser_version, Parse_confidence, Evidence_source, Parser_notes,
                                                Download_id, Current_path, Target_path, Url_chain, Start_time, End_time, 
                                                Received_bytes, Total_bytes, State, opened, referrer, Site_url, Tab_url, 
                                                Mime_type, File)
                                               VALUES 
                                               (@Artifact_type, @Potential_activity, @Browser, @Parser_version, @Parse_confidence, @Evidence_source, @Parser_notes,
                                                @Download_id, @Current_path, @Target_path, @Url_chain, @Start_time, @End_time, 
                                                @Received_bytes, @Total_bytes, @State, @opened, @referrer, @Site_url, 
                                                @Tab_url, @Mime_type, @File)";

                                using (SQLiteCommand insertCommand = new SQLiteCommand(insertQuery, resultsConnection, transaction))
                                {
                                    insertCommand.Parameters.AddWithValue("@Artifact_type", BuildArtifactType(browserType, "downloads"));
                                    insertCommand.Parameters.AddWithValue("@Potential_activity", "Downloading file");
                                    insertCommand.Parameters.AddWithValue("@Browser", browserType);
                                    insertCommand.Parameters.AddWithValue("@Parser_version", DownloadsParserVersion);
                                    insertCommand.Parameters.AddWithValue("@Parse_confidence", parseConfidence);
                                    insertCommand.Parameters.AddWithValue("@Evidence_source", "Chromium History.downloads");
                                    insertCommand.Parameters.AddWithValue("@Parser_notes", parserNotes);
                                    insertCommand.Parameters.AddWithValue("@Download_id", downloadId);
                                    insertCommand.Parameters.AddWithValue("@Current_path", currentPath ?? (object)DBNull.Value);
                                    insertCommand.Parameters.AddWithValue("@Target_path", targetPath ?? (object)DBNull.Value);
                                    insertCommand.Parameters.AddWithValue("@Url_chain", urlChain ?? (object)DBNull.Value);
                                    insertCommand.Parameters.AddWithValue("@Start_time", FormatDateTime(startTime) ?? (object)DBNull.Value);
                                    insertCommand.Parameters.AddWithValue("@End_time", FormatDateTime(endTime) ?? (object)DBNull.Value);
                                    insertCommand.Parameters.AddWithValue("@Received_bytes", receivedBytes);
                                    insertCommand.Parameters.AddWithValue("@Total_bytes", totalBytes);
                                    insertCommand.Parameters.AddWithValue("@State", stateDescription);
                                    insertCommand.Parameters.AddWithValue("@opened", openedDescription);
                                    insertCommand.Parameters.AddWithValue("@referrer", referrer ?? (object)DBNull.Value);
                                    insertCommand.Parameters.AddWithValue("@Site_url", siteUrl ?? (object)DBNull.Value);
                                    insertCommand.Parameters.AddWithValue("@Tab_url", tabUrl ?? (object)DBNull.Value);
                                    insertCommand.Parameters.AddWithValue("@Mime_type", mimeType ?? (object)DBNull.Value);
                                    insertCommand.Parameters.AddWithValue("@File", filePath);

                                    insertCommand.ExecuteNonQuery();
                                    inserted++;
                                    if (parseConfidence == "High") highConfidence++;
                                    else mediumConfidence++;
                                }
                            }

                            transaction.Commit();
                            string runConfidence = inserted == 0
                                ? "Low"
                                : highConfidence >= mediumConfidence ? "High" : "Medium";
                            BrowserReviewerLogger.LogParserRun(
                                "Downloads",
                                browserType,
                                DownloadsParserVersion,
                                runConfidence,
                                hasUrlChains ? "Chromium History.downloads + downloads_url_chains" : "Chromium History.downloads",
                                inserted,
                                filePath,
                                parserNotes);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (logConsole != null)
                    LogToConsole(logConsole, $"An error occurred while processing downloads: {ex.Message}", Color.Red);
                else
                    Console.WriteLine($"An error occurred while processing downloads: {ex.Message}");
            }
        }


        private static string InterpretDownloadState(int state)
        {
            return state switch
            {
                1 => "Downloaded",
                2 => "Cancelled",
                _ => "Unknown"
            };
        }













        public static void ProcessAndWriteDownloadsFirefox(string historyConnectionString, string chromeViewerConnectionString, string browserType, string filepath, RichTextBox? logConsole = null)
        {
            string sourcePath = !string.IsNullOrWhiteSpace(Helpers.realFirefoxPlacesPath) ? Helpers.realFirefoxPlacesPath : filepath;
            if (!TryRegisterProcessedSource("firefox-downloads", sourcePath, logConsole))
                return;

            if (!string.IsNullOrEmpty(historyConnectionString))
            {
                using (SQLiteConnection historyConnection = new SQLiteConnection(historyConnectionString))
                {
                    historyConnection.Open();

                    string queryDownloads = @"
                    SELECT 
                        ma.place_id,
                        mp.url, 
                        mp.title, 
                        mp.last_visit_date, 
                        aa.name AS annotation_name, 
                        ma.content 
                    FROM 
                        moz_annos ma 
                    JOIN
                        moz_anno_attributes aa ON ma.anno_attribute_id = aa.id
                    JOIN 
                        moz_places mp ON ma.place_id = mp.id
                    WHERE 
                        aa.name IN ('downloads/destinationFileURI', 'downloads/metaData')";

                    using (SQLiteCommand commandDownloads = new SQLiteCommand(queryDownloads, historyConnection))
                    using (SQLiteDataReader readerDownloads = commandDownloads.ExecuteReader())
                    {
                        using (SQLiteConnection chromeViewerConnection = new SQLiteConnection(chromeViewerConnectionString))
                        {
                            chromeViewerConnection.Open();
                            using (var transaction = chromeViewerConnection.BeginTransaction())
                            {
                                var downloadData = new Dictionary<int, (string? url, string? title, long? lastVisitDate, string? fileName, long? endTime, long? fileSize, string? state, bool hasDestination, bool hasMetadata, string? metadataError)>();

                                while (readerDownloads.Read())
                                {
                                    int placeId = readerDownloads.GetInt32(0);
                                    string? url = readerDownloads.IsDBNull(1) ? null : readerDownloads.GetString(1);
                                    string? title = readerDownloads.IsDBNull(2) ? null : readerDownloads.GetString(2);
                                    long? lastVisitDate = readerDownloads.IsDBNull(3) ? (long?)null : readerDownloads.GetInt64(3);
                                    string? annotationName = readerDownloads.IsDBNull(4) ? null : readerDownloads.GetString(4);
                                    string? content = readerDownloads.IsDBNull(5) ? null : readerDownloads.GetString(5);

                                    if (!downloadData.ContainsKey(placeId))
                                    {
                                        downloadData[placeId] = (url, title, lastVisitDate, null, null, null, null, false, false, null);
                                    }

                                    if (string.Equals(annotationName, "downloads/destinationFileURI", StringComparison.OrdinalIgnoreCase))
                                    {
                                        downloadData[placeId] = (url, title, lastVisitDate, content, downloadData[placeId].endTime, downloadData[placeId].fileSize, downloadData[placeId].state, true, downloadData[placeId].hasMetadata, downloadData[placeId].metadataError);
                                    }
                                    else if (string.Equals(annotationName, "downloads/metaData", StringComparison.OrdinalIgnoreCase))
                                    {
                                        try
                                        {
                                            var downloadDetails = System.Text.Json.JsonDocument.Parse(content ?? string.Empty).RootElement;

                                            long? endTime = downloadDetails.TryGetProperty("endTime", out var endTimeElement) && endTimeElement.ValueKind == System.Text.Json.JsonValueKind.Number
                                                ? endTimeElement.GetInt64()
                                                : (long?)null;

                                            long? fileSize = downloadDetails.TryGetProperty("fileSize", out var fileSizeElement) && fileSizeElement.ValueKind == System.Text.Json.JsonValueKind.Number
                                                ? fileSizeElement.GetInt64()
                                                : (long?)null;

                                            string state = downloadDetails.TryGetProperty("state", out var stateElement) && stateElement.ValueKind == System.Text.Json.JsonValueKind.Number
                                                ? ConvertStateToDescription(stateElement.GetInt32())
                                                : "Unknown";

                                            downloadData[placeId] = (url, title, lastVisitDate, downloadData[placeId].fileName, endTime, fileSize, state, downloadData[placeId].hasDestination, true, null);
                                        }
                                        catch (Exception ex)
                                        {
                                            downloadData[placeId] = (url, title, lastVisitDate, downloadData[placeId].fileName, downloadData[placeId].endTime, downloadData[placeId].fileSize, downloadData[placeId].state, downloadData[placeId].hasDestination, true, ex.Message);
                                        }
                                    }
                                }

                                int inserted = 0;
                                int highConfidence = 0;
                                int mediumConfidence = 0;
                                bool sawMetadataWarning = false;

                                foreach (var data in downloadData)
                                {
                                    int placeId = data.Key;
                                    var (url, title, lastVisitDateUnix, fileName, endTimeUnix, fileSize, state, hasDestination, hasMetadata, metadataError) = data.Value;

                                    DateTime? lastVisitDate = lastVisitDateUnix.HasValue ? UnixMicrosecondsToDateTime(lastVisitDateUnix.Value) : null;

                                    if (endTimeUnix.HasValue)
                                    {
                                        DateTime? endTime = UnixMillisecondsToDateTime(endTimeUnix.Value);
                                        string parseConfidence = hasDestination && hasMetadata && endTime.HasValue ? "High" : "Medium";
                                        string parserNotes = metadataError == null
                                            ? $"Parsed Firefox downloads annotations. Destination annotation: {hasDestination}. Metadata annotation: {hasMetadata}."
                                            : $"Parsed Firefox downloads annotations with metadata warning: {metadataError}";

                                        string insertDownloads = @"INSERT INTO firefox_downloads 
                                                        (Artifact_type, Potential_activity, Browser, Parser_version, Parse_confidence, Evidence_source, Parser_notes,
                                                         Download_id, Current_path, End_time, Last_visit_time, Received_bytes, Total_bytes, Source_url, Title, State, File)
                                                        VALUES 
                                                        (@Artifact_type, @Potential_activity, @Browser, @Parser_version, @Parse_confidence, @Evidence_source, @Parser_notes,
                                                         @Download_id, @Current_path, @End_time, @Last_visit_time, @Received_bytes, @Total_bytes, @Source_url, @Title, @State, @File)";

                                        using (SQLiteCommand commandInsertDownloads = new SQLiteCommand(insertDownloads, chromeViewerConnection, transaction))
                                        {
                                            commandInsertDownloads.Parameters.AddWithValue("@Artifact_type", BuildArtifactType(browserType, "downloads"));
                                            commandInsertDownloads.Parameters.AddWithValue("@Potential_activity", "Downloading file");
                                            commandInsertDownloads.Parameters.AddWithValue("@Browser", browserType);
                                            commandInsertDownloads.Parameters.AddWithValue("@Parser_version", DownloadsParserVersion);
                                            commandInsertDownloads.Parameters.AddWithValue("@Parse_confidence", parseConfidence);
                                            commandInsertDownloads.Parameters.AddWithValue("@Evidence_source", "Firefox places.sqlite moz_annos downloads annotations");
                                            commandInsertDownloads.Parameters.AddWithValue("@Parser_notes", parserNotes);
                                            commandInsertDownloads.Parameters.AddWithValue("@Download_id", placeId);
                                            commandInsertDownloads.Parameters.AddWithValue("@Current_path", fileName ?? (object)DBNull.Value);
                                            commandInsertDownloads.Parameters.AddWithValue("@End_time", FormatDateTime(endTime) ?? (object)DBNull.Value);
                                            commandInsertDownloads.Parameters.AddWithValue("@Last_visit_time", FormatDateTime(lastVisitDate) ?? (object)DBNull.Value);
                                            commandInsertDownloads.Parameters.AddWithValue("@Received_bytes", fileSize.HasValue ? fileSize.Value : (object)DBNull.Value);
                                            commandInsertDownloads.Parameters.AddWithValue("@Total_bytes", fileSize.HasValue ? fileSize.Value : (object)DBNull.Value);
                                            commandInsertDownloads.Parameters.AddWithValue("@Source_url", url ?? (object)DBNull.Value);
                                            commandInsertDownloads.Parameters.AddWithValue("@Title", title ?? (object)DBNull.Value);
                                            commandInsertDownloads.Parameters.AddWithValue("@State", state ?? (object)DBNull.Value);
                                            if (Helpers.realFirefoxPlacesPath != "")
                                            {
                                                commandInsertDownloads.Parameters.AddWithValue("@File", Helpers.realFirefoxPlacesPath);
                                            }
                                            else
                                            {
                                                commandInsertDownloads.Parameters.AddWithValue("@File", filepath);
                                            }                                          
                                            commandInsertDownloads.ExecuteNonQuery();
                                            inserted++;
                                            if (parseConfidence == "High") highConfidence++;
                                            else mediumConfidence++;
                                            sawMetadataWarning = sawMetadataWarning || metadataError != null;
                                        }
                                    }
                                }
                                transaction.Commit();
                                string sourceFile = Helpers.realFirefoxPlacesPath != "" ? Helpers.realFirefoxPlacesPath : filepath;
                                string runConfidence = inserted == 0
                                    ? "Low"
                                    : highConfidence >= mediumConfidence ? "High" : "Medium";
                                BrowserReviewerLogger.LogParserRun(
                                    "Downloads",
                                    browserType,
                                    DownloadsParserVersion,
                                    runConfidence,
                                    "Firefox places.sqlite moz_annos downloads annotations",
                                    inserted,
                                    sourceFile,
                                    sawMetadataWarning
                                        ? "Parsed Firefox download annotations; at least one metadata annotation could not be decoded."
                                        : "Parsed Firefox download annotations using moz_anno_attributes names.");
                            }
                        }
                    }
                }
            }
            else
            {
                if (logConsole != null)
                    LogToConsole(logConsole, $"Select a Firefox history file first", Color.Red);
                else Console.WriteLine($"Select a Firefox history file first");
            }
        }



       






        private static string ConvertStateToDescription(int state)
        {
            return state switch
            {
                0 => "Not Started",
                1 => "Completed",
                2 => "Paused",
                3 => "Canceled",
                4 => "In Progress",
                5 => "Failed",
                _ => "Unknown"
            };
        }

       


        public static void ProcessAndWriteBookmarksFirefox(string historyConnectionString, string chromeViewerConnectionString, string browserType, string filepath, RichTextBox? logConsole = null)
        {
            string sourcePath = !string.IsNullOrWhiteSpace(Helpers.realFirefoxPlacesPath) ? Helpers.realFirefoxPlacesPath : filepath;
            if (!TryRegisterProcessedSource("firefox-bookmarks", sourcePath, logConsole))
                return;

            if (!string.IsNullOrEmpty(historyConnectionString))
            {
                using (SQLiteConnection historyConnection = new SQLiteConnection(historyConnectionString))
                {
                    historyConnection.Open();

                    string queryBookmarks = @"
                    SELECT 
                        b.id AS bookmark_id,
                        b.type AS bookmark_type,
                        b.fk AS fk,
                        b.parent AS parent,
                        b.title AS bookmark_title,
                        b.dateAdded AS date_added,
                        b.lastModified AS last_modified,
                        p.url AS bookmark_url,
                        p.title AS page_title,
                        p.visit_count AS visit_count,
                        p.last_visit_date AS last_visit_date,
                        pb.title AS parent_name,
                        a.id AS anno_id,
                        a.content AS anno_content,
                        aa.name AS anno_name
                    FROM 
                        moz_bookmarks b
                    LEFT JOIN 
                        moz_places p ON b.fk = p.id
                    LEFT JOIN 
                        moz_bookmarks pb ON b.parent = pb.id
                    LEFT JOIN 
                        moz_items_annos a ON b.id = a.item_id
                    LEFT JOIN 
                        moz_anno_attributes aa ON a.anno_attribute_id = aa.id";

                    using (SQLiteCommand commandBookmarks = new SQLiteCommand(queryBookmarks, historyConnection))
                    using (SQLiteDataReader readerBookmarks = commandBookmarks.ExecuteReader())
                    {
                        using (SQLiteConnection chromeViewerConnection = new SQLiteConnection(chromeViewerConnectionString))
                        {
                            chromeViewerConnection.Open();
                            using (var transaction = chromeViewerConnection.BeginTransaction())
                            {
                                while (readerBookmarks.Read())
                                {
                                    int bookmarkId = readerBookmarks.GetInt32(0);
                                    int bookmarkTypeNumeric = readerBookmarks.GetInt32(1);
                                    string bookmarkType = bookmarkTypeNumeric switch
                                    {
                                        1 => "Bookmark",
                                        2 => "Folder",
                                        3 => "Separator",
                                        _ => "Unknown"
                                    };

                                    int? fk = readerBookmarks.IsDBNull(2) ? (int?)null : readerBookmarks.GetInt32(2);
                                    int parent = readerBookmarks.GetInt32(3);
                                    string? bookmarkTitle = readerBookmarks.IsDBNull(4) ? null : readerBookmarks.GetString(4);

                                    DateTime? dateAdded = readerBookmarks.IsDBNull(5) ? (DateTime?)null : new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(readerBookmarks.GetInt64(5) / 1000);
                                    DateTime? lastModified = readerBookmarks.IsDBNull(6) ? (DateTime?)null : new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(readerBookmarks.GetInt64(6) / 1000);
                                    string? bookmarkUrl = readerBookmarks.IsDBNull(7) ? null : readerBookmarks.GetString(7);
                                    string? pageTitle = readerBookmarks.IsDBNull(8) ? null : readerBookmarks.GetString(8);
                                    int? visitCount = readerBookmarks.IsDBNull(9) ? (int?)null : readerBookmarks.GetInt32(9);
                                    DateTime? lastVisitDate = readerBookmarks.IsDBNull(10) ? (DateTime?)null : new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(readerBookmarks.GetInt64(10) / 1000);

                                    string? parentName = readerBookmarks.IsDBNull(11) ? null : readerBookmarks.GetString(11);

                                    int? annoId = readerBookmarks.IsDBNull(12) ? (int?)null : readerBookmarks.GetInt32(12);
                                    string? annoContent = readerBookmarks.IsDBNull(13) ? null : readerBookmarks.GetString(13);
                                    string? annoName = readerBookmarks.IsDBNull(14) ? null : readerBookmarks.GetString(14);

                                    string insertBookmarks = @"INSERT INTO bookmarks_Firefox 
                                (Artifact_type, Potential_activity, Browser, Bookmark_id, Type, FK, Parent, Parent_name, Title, DateAdded, LastModified, URL, PageTitle, VisitCount, LastVisitDate, AnnoId, AnnoContent, AnnoName, File)
                            VALUES 
                                (@Artifact_type, @Potential_activity, @Browser, @Bookmark_id, @Type, @FK, @Parent, @Parent_name, @Title, @DateAdded, @LastModified, @URL, @PageTitle, @VisitCount, @LastVisitDate, @AnnoId, @AnnoContent, @AnnoName, @File)";

                                    using (SQLiteCommand commandInsertBookmarks = new SQLiteCommand(insertBookmarks, chromeViewerConnection, transaction))
                                    {
                                        commandInsertBookmarks.Parameters.AddWithValue("@Artifact_type", BuildArtifactType(browserType, "bookmarks"));
                                        commandInsertBookmarks.Parameters.AddWithValue("@Potential_activity", bookmarkType == "Folder" ? "Bookmark folder present" : "Saved bookmark");
                                        commandInsertBookmarks.Parameters.AddWithValue("@Browser", browserType);
                                        commandInsertBookmarks.Parameters.AddWithValue("@Bookmark_id", bookmarkId);
                                        commandInsertBookmarks.Parameters.AddWithValue("@Type", bookmarkType);
                                        commandInsertBookmarks.Parameters.AddWithValue("@FK", fk.HasValue ? (object)fk.Value : DBNull.Value);
                                        commandInsertBookmarks.Parameters.AddWithValue("@Parent", parent);
                                        commandInsertBookmarks.Parameters.AddWithValue("@Parent_name", parentName ?? (object)DBNull.Value);
                                        commandInsertBookmarks.Parameters.AddWithValue("@Title", bookmarkTitle ?? (object)DBNull.Value);
                                        commandInsertBookmarks.Parameters.AddWithValue("@DateAdded", dateAdded.HasValue ? dateAdded.Value.ToString("yyyy-MM-dd HH:mm:ss.ffffff") : (object)DBNull.Value);
                                        commandInsertBookmarks.Parameters.AddWithValue("@LastModified", lastModified.HasValue ? lastModified.Value.ToString("yyyy-MM-dd HH:mm:ss.ffffff") : (object)DBNull.Value);
                                        commandInsertBookmarks.Parameters.AddWithValue("@URL", bookmarkUrl ?? (object)DBNull.Value);
                                        commandInsertBookmarks.Parameters.AddWithValue("@PageTitle", pageTitle ?? (object)DBNull.Value);
                                        commandInsertBookmarks.Parameters.AddWithValue("@VisitCount", visitCount.HasValue ? (object)visitCount.Value : DBNull.Value);
                                        commandInsertBookmarks.Parameters.AddWithValue("@LastVisitDate", lastVisitDate.HasValue ? lastVisitDate.Value.ToString("yyyy-MM-dd HH:mm:ss.ffffff") : (object)DBNull.Value);
                                        commandInsertBookmarks.Parameters.AddWithValue("@AnnoId", annoId.HasValue ? (object)annoId.Value : DBNull.Value);
                                        commandInsertBookmarks.Parameters.AddWithValue("@AnnoContent", annoContent ?? (object)DBNull.Value);
                                        commandInsertBookmarks.Parameters.AddWithValue("@AnnoName", annoName ?? (object)DBNull.Value);

                                        if (Helpers.realFirefoxPlacesPath != "")
                                        {
                                            commandInsertBookmarks.Parameters.AddWithValue("@File", Helpers.realFirefoxPlacesPath);
                                        }
                                        else
                                        {
                                            commandInsertBookmarks.Parameters.AddWithValue("@File", filepath);
                                        }
                                        
                                        commandInsertBookmarks.ExecuteNonQuery();
                                    }
                                }
                                transaction.Commit();
                            }
                        }
                    }
                }
            }
            else
            {
                if (logConsole != null)
                    LogToConsole(logConsole, $"Select a Firefox history file first", Color.Red);
                else Console.WriteLine($"Select a Firefox history file first");
            }
        }



        public static void ProcessAndWriteRecordsChromeLike(string historyConnectionString, string chromeViewerConnectionString, string browserType, string filepath, RichTextBox? logConsole = null)
        {
            if (!TryRegisterProcessedSource("chromium-history", filepath, logConsole))
                return;

            if (!string.IsNullOrEmpty(historyConnectionString))
            {
                using (SQLiteConnection historyConnection = new SQLiteConnection(historyConnectionString))
                {
                    historyConnection.Open();

                    string queryVisits = "SELECT id, url, visit_time, from_visit, visit_duration, transition FROM visits";
                    using (SQLiteCommand commandVisits = new SQLiteCommand(queryVisits, historyConnection))
                    using (SQLiteDataReader readerVisits = commandVisits.ExecuteReader())
                    {
                        using (SQLiteConnection chromeViewerConnection = new SQLiteConnection(chromeViewerConnectionString))
                        {
                            chromeViewerConnection.Open();
                            using (var transaction = chromeViewerConnection.BeginTransaction())
                            {
                                while (readerVisits.Read())
                                {
                                    int visitId = readerVisits.GetInt32(0);
                                    int urlId = readerVisits.GetInt32(1);
                                    long visitTimeMicroseconds = readerVisits.GetInt64(2);
                                    int? fromVisit = readerVisits.IsDBNull(3) ? (int?)null : readerVisits.GetInt32(3);
                                    long? visitDurationMicroseconds = readerVisits.IsDBNull(4) ? (long?)null : readerVisits.GetInt64(4);
                                    int transition = readerVisits.GetInt32(5);

                                    DateTime? visitTime = ChromeTimestampToDateTime(visitTimeMicroseconds);

                                    TimeSpan? visitDuration = visitDurationMicroseconds.HasValue ? TimeSpan.FromTicks(visitDurationMicroseconds.Value * 10) : (TimeSpan?)null;

                                    string? fromUrl = null;
                                    if (fromVisit.HasValue)
                                    {
                                        string queryFromVisit = "SELECT url FROM urls WHERE id = (SELECT url FROM visits WHERE id = @fromVisit)";
                                        using (SQLiteCommand commandFromVisit = new SQLiteCommand(queryFromVisit, historyConnection))
                                        {
                                            commandFromVisit.Parameters.AddWithValue("@fromVisit", fromVisit.Value);
                                            object? result = commandFromVisit.ExecuteScalar();
                                            fromUrl = result?.ToString();
                                        }
                                    }

                                    string queryUrls = "SELECT url, title, visit_count, last_visit_time, typed_count FROM urls WHERE id = @urlId";
                                    using (SQLiteCommand commandUrls = new SQLiteCommand(queryUrls, historyConnection))
                                    {
                                        commandUrls.Parameters.AddWithValue("@urlId", urlId);
                                        using (SQLiteDataReader readerUrls = commandUrls.ExecuteReader())
                                        {
                                            if (readerUrls.Read())
                                            {
                                                string url = readerUrls.GetString(0);
                                                string title = readerUrls.GetString(1);
                                                int visitCount = readerUrls.GetInt32(2);
                                                long chromiumTimeMicroseconds = readerUrls.GetInt64(3);
                                                int typedCount = readerUrls.GetInt32(4);

                                                DateTime? lastVisitTime = ChromeTimestampToDateTime(chromiumTimeMicroseconds);

                                                string Category = Evaluatecategory(url);
                                                string potentialActivity = EvalPotentialActivity(url);

                                                if (!Helpers.browserUrls.ContainsKey(browserType))
                                                {
                                                    Helpers.browserUrls[browserType] = new List<string>();
                                                }
                                                Helpers.browserUrls[browserType].Add(url);

                                                string transitionType = GetTransitionDescription(transition);

                                                string insertResults = @"INSERT INTO results (Artifact_type, Potential_activity, Browser, Category, Visit_id, Url, Title, Visit_count, Last_visit_time, Visit_time, From_url, Visit_duration, Transition, Typed_count, File)
                                                            VALUES (@Artifact_type, @Potential_activity, @Browser, @Category, @visitId, @url, @title, @visitCount, @lastVisitTime, @visitTime, @fromUrl, @visitDuration, @transition, @typedCount, @File)";
                                                using (SQLiteCommand commandInsertResults = new SQLiteCommand(insertResults, chromeViewerConnection, transaction))
                                                {
                                                    commandInsertResults.Parameters.AddWithValue("@Artifact_type", BuildArtifactType(browserType, "history"));
                                                    commandInsertResults.Parameters.AddWithValue("@Browser", browserType);
                                                    commandInsertResults.Parameters.AddWithValue("@Category", Category);
                                                    commandInsertResults.Parameters.AddWithValue("@Potential_activity", potentialActivity);
                                                    commandInsertResults.Parameters.AddWithValue("@visitId", visitId);
                                                    commandInsertResults.Parameters.AddWithValue("@url", url);
                                                    commandInsertResults.Parameters.AddWithValue("@title", title);
                                                    commandInsertResults.Parameters.AddWithValue("@visitTime", FormatDateTime(visitTime) ?? (object)DBNull.Value);
                                                    commandInsertResults.Parameters.AddWithValue("@visitDuration", visitDuration.HasValue ? visitDuration.Value.ToString(@"dd\.hh\:mm\:ss\.fffffff") : DBNull.Value.ToString());
                                                    commandInsertResults.Parameters.AddWithValue("@lastVisitTime", FormatDateTime(lastVisitTime) ?? (object)DBNull.Value);
                                                    commandInsertResults.Parameters.AddWithValue("@visitCount", visitCount);
                                                    commandInsertResults.Parameters.AddWithValue("@typedCount", typedCount);
                                                    commandInsertResults.Parameters.AddWithValue("@fromUrl", (object?)fromUrl ?? DBNull.Value);
                                                    commandInsertResults.Parameters.AddWithValue("@transition", transitionType);
                                                    commandInsertResults.Parameters.AddWithValue("@File", filepath);

                                                    commandInsertResults.ExecuteNonQuery();
                                                }
                                            }
                                        }
                                    }
                                }
                                transaction.Commit();
                            }
                        }
                    }
                }
            }
            else
            {
                if (logConsole != null)
                    LogToConsole(logConsole, $"Select a Google Chrome history file first", Color.Red);
                else Console.WriteLine($"Select a Google Chrome history file first");
            }
        }


        




        public static string GetTransitionDescription(int transition)
        {
            int coreTransition = transition & 0xFF;
            string transitionType = coreTransition switch
            {
                0 => "LINK",
                1 => "TYPED",
                2 => "AUTO_BOOKMARK",
                3 => "AUTO_SUBFRAME",
                4 => "MANUAL_SUBFRAME",
                5 => "GENERATED",
                6 => "START_PAGE",
                7 => "FORM_SUBMIT",
                8 => "RELOAD",
                9 => "KEYWORD",
                10 => "KEYWORD_GENERATED",
                _ => "UNKNOWN"
            };

            List<string> qualifiers = new List<string>();
            if ((transition & 0x00800000) != 0) qualifiers.Add("IS_REDIRECT");
            if ((transition & 0x01000000) != 0) qualifiers.Add("FORWARD_BACK");
            if ((transition & 0x02000000) != 0) qualifiers.Add("FROM_ADDRESS_BAR");
            if ((transition & 0x04000000) != 0) qualifiers.Add("HOME_PAGE");
            if ((transition & 0x08000000) != 0) qualifiers.Add("FROM_API");

            string result = transitionType;
            if (qualifiers.Count > 0)
            {
                result += " (" + string.Join(", ", qualifiers) + ")";
            }

            return result;
        }

            





     


        public static void LogToConsole(RichTextBox? textBox_logConsole, string message, Color? color = null)
        {
            BrowserReviewerLogger.Log(textBox_logConsole, message, color);
        }




       

       
        private async Task TraverseDirectoryAsync(string currentDir, List<string> sqliteFiles, RichTextBox? textBox_logConsole)
        {
            try
            {
                foreach (var filePath in Directory.EnumerateFiles(currentDir, "*", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        ProcessFileArtifact(filePath, textBox_logConsole);

                        if (Path.GetFileName(filePath) == "WebCacheV01.dat")
                        {
                            LogToConsole(textBox_logConsole, $"Archivo WebCacheV01.dat : {filePath}");
                        }





                    }
                    catch (UnauthorizedAccessException)
                    {
                        LogToConsole(textBox_logConsole, $"Acceso denegado al archivo: {filePath}");
                    }
                    catch (PathTooLongException)
                    {
                        LogToConsole(textBox_logConsole, $"Ruta demasiado larga: {filePath}");
                    }
                    catch (Exception ex)
                    {
                        LogToConsole(textBox_logConsole, $"Error al procesar el archivo {filePath}: {ex.Message}");
                    }
                }

                foreach (var dir in Directory.EnumerateDirectories(currentDir))
                {
                    try
                    {
                        ProcessDirectoryArtifact(dir, textBox_logConsole);
                        await TraverseDirectoryAsync(dir, sqliteFiles, textBox_logConsole);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        LogToConsole(textBox_logConsole, $"Acceso denegado al directorio: {dir}");
                    }
                    catch (PathTooLongException)
                    {
                        LogToConsole(textBox_logConsole, $"Ruta demasiado larga: {dir}");
                    }
                    catch (Exception ex)
                    {
                        LogToConsole(textBox_logConsole, $"Error al recorrer el directorio {dir}: {ex.Message}");
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                LogToConsole(textBox_logConsole, $"Acceso denegado al directorio: {currentDir}");
            }
            catch (PathTooLongException)
            {
                LogToConsole(textBox_logConsole, $"Ruta demasiado larga: {currentDir}");
            }
            catch (Exception ex)
            {
                LogToConsole(textBox_logConsole, $"Error al recorrer el directorio {currentDir}: {ex.Message}");
            }
        }

        private bool IsSQLite3(string filePath, RichTextBox? textBox_logConsole)
        {
            if (!File.Exists(filePath))
            {
                return false;
            }

            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    byte[] headerBytes = new byte[16];
                    fileStream.Read(headerBytes, 0, 16);
                    string headerString = System.Text.Encoding.ASCII.GetString(headerBytes);
                    return headerString.StartsWith("SQLite format 3");
                }
            }
            catch (Exception ex)
            {
                LogToConsole(textBox_logConsole, $"Error reading the file {filePath}: {ex.Message}");
                return false;
            }
        }

        private bool CLI_IsSQLite3(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return false;
            }

            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    byte[] headerBytes = new byte[16];
                    fileStream.Read(headerBytes, 0, 16);
                    string headerString = System.Text.Encoding.ASCII.GetString(headerBytes);
                    return headerString.StartsWith("SQLite format 3");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading the file {filePath}: {ex.Message}");
                return false;
            }
        }






        public static void ProcessAndWriteRecordsFirefox(string historyConnectionString, string chromeViewerConnectionString, string browserType, string filepath, RichTextBox? logConsole = null)
        {
            string sourcePath = !string.IsNullOrWhiteSpace(Helpers.realFirefoxPlacesPath) ? Helpers.realFirefoxPlacesPath : filepath;

            if (!string.IsNullOrEmpty(historyConnectionString))
            {
                using (SQLiteConnection historyConnection = new SQLiteConnection(historyConnectionString))
                {
                    historyConnection.Open();

                    string queryVisits = @"
                                            SELECT hv.id, hv.place_id, hv.from_visit, hv.visit_date, hv.visit_type, p.url, p.title, p.visit_count, p.last_visit_date, p.frecency 
                                            FROM moz_historyvisits hv 
                                            JOIN moz_places p ON hv.place_id = p.id";

                    using (SQLiteCommand commandVisits = new SQLiteCommand(queryVisits, historyConnection))
                    using (SQLiteDataReader readerVisits = commandVisits.ExecuteReader())
                    {
                        if (!TryRegisterProcessedSource("firefox-history", sourcePath, logConsole))
                            return;

                        using (SQLiteConnection chromeViewerConnection = new SQLiteConnection(chromeViewerConnectionString))
                        {
                            chromeViewerConnection.Open();

                            using (SQLiteTransaction transaction = chromeViewerConnection.BeginTransaction())
                            {
                                int insertedCount = 0;
                                int skippedCount = 0;
                                string? lastRowError = null;

                                try
                                {
                                    while (readerVisits.Read())
                                    {
                                        try
                                        {
                                            int visitId = readerVisits.IsDBNull(0) ? 0 : Convert.ToInt32(readerVisits.GetValue(0));
                                            int placeId = readerVisits.IsDBNull(1) ? 0 : Convert.ToInt32(readerVisits.GetValue(1));
                                            int fromVisit = readerVisits.IsDBNull(2) ? 0 : Convert.ToInt32(readerVisits.GetValue(2));
                                            long visitTimeMicroseconds = readerVisits.IsDBNull(3) ? 0L : Convert.ToInt64(readerVisits.GetValue(3));
                                            int visitType = readerVisits.IsDBNull(4) ? 0 : Convert.ToInt32(readerVisits.GetValue(4));
                                            string? url = readerVisits.IsDBNull(5) ? null : readerVisits.GetString(5);
                                            string? title = readerVisits.IsDBNull(6) ? null : readerVisits.GetString(6);
                                            int visitCount = readerVisits.IsDBNull(7) ? 0 : Convert.ToInt32(readerVisits.GetValue(7));
                                            long lastVisitTimeMicroseconds = readerVisits.IsDBNull(8) ? 0L : Convert.ToInt64(readerVisits.GetValue(8));
                                            int frecency = readerVisits.IsDBNull(9) ? 0 : Convert.ToInt32(readerVisits.GetValue(9));

                                            if (string.IsNullOrWhiteSpace(url) || visitTimeMicroseconds <= 0)
                                            {
                                                skippedCount++;
                                                continue;
                                            }

                                            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                                            DateTime visitTime = epoch.AddMilliseconds(visitTimeMicroseconds / 1000.0);
                                            DateTime? lastVisitTime = lastVisitTimeMicroseconds > 0
                                                ? epoch.AddMilliseconds(lastVisitTimeMicroseconds / 1000.0)
                                                : null;

                                            string Category = Evaluatecategory(url);
                                            string transition = GetTransitionDescriptionFirefox(visitType);
                                            string navigationContext = GetNavigationContextFirefox(visitType);
                                            string userActionLikelihood = GetUserActionLikelihoodFirefox(visitType);

                                            string potentialActivity = EvalPotentialActivity(url);

                                            if (!Helpers.browserUrls.ContainsKey(browserType))
                                            {
                                                Helpers.browserUrls[browserType] = new List<string>();
                                            }

                                            Helpers.browserUrls[browserType].Add(url);


                                            string insertResults = @"INSERT INTO firefox_results (Artifact_type, Potential_activity, Browser, Category, Visit_id, Place_id, From_visit, Url, Title, Visit_time,  Last_visit_time, Visit_count,  Transition, Navigation_context, User_action_likelihood, File, Visit_type, Frecency)
                                                VALUES (@Artifact_type, @Potential_activity, @Browser, @Category, @visitId, @placeId, @fromVisit, @url, @title, @visitTime, @lastVisitTime, @visitCount, @transition, @navigationContext, @userActionLikelihood, @File, @visitType, @frecency)";
                                            using (SQLiteCommand commandInsertResults = new SQLiteCommand(insertResults, chromeViewerConnection, transaction))
                                            {
                                                commandInsertResults.Parameters.AddWithValue("@Artifact_type", BuildArtifactType(browserType, "history"));
                                                commandInsertResults.Parameters.AddWithValue("@Browser", browserType);
                                                commandInsertResults.Parameters.AddWithValue("@Category", Category);
                                                commandInsertResults.Parameters.AddWithValue("@Potential_activity", potentialActivity);
                                                commandInsertResults.Parameters.AddWithValue("@visitId", visitId);
                                                commandInsertResults.Parameters.AddWithValue("@placeId", placeId == 0 ? (object)DBNull.Value : placeId);
                                                commandInsertResults.Parameters.AddWithValue("@fromVisit", fromVisit == 0 ? (object)DBNull.Value : fromVisit);
                                                commandInsertResults.Parameters.AddWithValue("@url", url);
                                                commandInsertResults.Parameters.AddWithValue("@title", (object?)title ?? DBNull.Value);
                                                commandInsertResults.Parameters.AddWithValue("@visitTime", visitTime.ToString("yyyy-MM-dd HH:mm:ss.ffffff"));
                                                commandInsertResults.Parameters.AddWithValue("@lastVisitTime", lastVisitTime.HasValue ? lastVisitTime.Value.ToString("yyyy-MM-dd HH:mm:ss.ffffff") : (object)DBNull.Value);
                                                commandInsertResults.Parameters.AddWithValue("@visitCount", visitCount);
                                                commandInsertResults.Parameters.AddWithValue("@transition", transition);
                                                commandInsertResults.Parameters.AddWithValue("@navigationContext", navigationContext);
                                                commandInsertResults.Parameters.AddWithValue("@userActionLikelihood", userActionLikelihood);
                                                if (Helpers.realFirefoxPlacesPath != "")
                                                {
                                                    commandInsertResults.Parameters.AddWithValue("@File", Helpers.realFirefoxPlacesPath);
                                                }
                                                else
                                                {
                                                    commandInsertResults.Parameters.AddWithValue("@File", filepath);
                                                }
                                                commandInsertResults.Parameters.AddWithValue("@visitType", visitType);
                                                commandInsertResults.Parameters.AddWithValue("@frecency", frecency);
                                                commandInsertResults.ExecuteNonQuery();
                                                insertedCount++;
                                            }
                                        }
                                        catch (Exception rowEx)
                                        {
                                            skippedCount++;
                                            lastRowError = rowEx.Message;
                                            if (logConsole != null)
                                                LogToConsole(logConsole, $"Skipping Firefox history row due to parser error: {rowEx.Message}", Color.DarkOrange);
                                        }
                                    }

                                    transaction.Commit();
                                    BrowserReviewerLogger.LogParserRun(
                                        "History",
                                        browserType,
                                        "firefox-history-row-safe-1",
                                        insertedCount > 0 && skippedCount == 0 ? "High" : insertedCount > 0 ? "Medium" : "Low",
                                        "Firefox places.sqlite moz_historyvisits + moz_places",
                                        insertedCount,
                                        sourcePath,
                                        skippedCount > 0
                                            ? $"Parsed Firefox history with {skippedCount} skipped rows. Last row error: {lastRowError}"
                                            : "Parsed Firefox history visits successfully.");
                                }
                                catch (Exception ex)
                                {
                                    transaction.Rollback();
                                    if (logConsole != null)
                                        LogToConsole(logConsole, $"Error: {ex.Message}", Color.Red);
                                    else Console.WriteLine($"Error: {ex.Message}");
                                    BrowserReviewerLogger.LogParserRun(
                                        "History",
                                        browserType,
                                        "firefox-history-row-safe-1",
                                        "Low",
                                        "Firefox places.sqlite moz_historyvisits + moz_places",
                                        0,
                                        sourcePath,
                                        $"Parser failed and transaction was rolled back: {ex.Message}");
                                }

                            }
                        }
                    }
                }
            }
            else
            {
                if (logConsole != null)
                    LogToConsole(logConsole, $"Select a Firefox history file first", Color.Red);
                else Console.WriteLine($"Select a Firefox history file first");
            }
        }


        



        private static string? GetDomainFromUrl(string url)
        {
            try
            {
                Uri uri = new Uri(url);
                string domain = uri.Host;

                if (domain.StartsWith("www."))
                {
                    domain = domain.Substring(4);
                }

                return domain;
            }
            catch (UriFormatException)
            {
                return null;
            }
        }



        public static string Evaluatecategory(string url)
        {

            if (url.StartsWith("file:///"))
            {
                return "Local Files";
            }


            string? domain = GetDomainFromUrl(url);

            if (domain == null)
            {
                return "Unknown";
            }

            if (IsLanAddress(domain))
            {
                return "Lan Addresses Browsing";
            }

            foreach (var category in CategoryData.categoryDomains)
            {
                if (category.Value.Any(d => domain.Equals(d, StringComparison.OrdinalIgnoreCase)))
                {
                    return category.Key;
                }
            }
            
            foreach (var category in CategoryData.categoryDomains)
            {
                if (category.Value.Any(d => domain.EndsWith($".{d}", StringComparison.OrdinalIgnoreCase)))
                {
                    return category.Key;
                }
            }

            return "Other";
        }

        private static bool IsLanAddress(string domain)
        {
            if (System.Net.IPAddress.TryParse(domain, out var ipAddress))
            {
                byte[] bytes = ipAddress.GetAddressBytes();

                if (bytes[0] == 10)
                    return true;

                if (bytes[0] == 172 && (bytes[1] >= 16 && bytes[1] <= 31))
                    return true;

                if (bytes[0] == 192 && bytes[1] == 168)
                    return true;
            }

            return false;
        }





        private static string EvalPotentialActivity(string url)
        {
            Uri uri;
            try
            {
                uri = new Uri(url);
            }
            catch (UriFormatException)
            {
                return "Unknown Activity";
            }

            string domain = uri.Host;
            string path = uri.AbsolutePath.ToLower();




            if (domain.Contains("google.com"))
            {
                if (domain.Contains("drive") && path.Contains("folders"))
                {
                    return "Google Folder Viewing";
                }
                else if (domain.Contains("drive"))
                {
                    return "Google Drive Accessing ";
                }
                else if (domain.Contains("docs") && path.Contains("document"))
                {
                    return "Google Docs";
                }
                else if (domain.Contains("docs") && path.Contains("spreadsheets"))
                {
                    return "Google Spreadsheets";
                }
                else if (domain.Contains("docs") && path.Contains("presentation"))
                {
                    return "Google Presentation";
                }
                else if (domain.Contains("mail") || path.Contains("mail"))
                {
                    return "Google Checking Email";
                }
                else if (domain.Contains("gemini"))
                {
                    return "Google Gemini Chat";
                }
                else if (domain.Contains("news"))
                {
                    return "Google News";
                }
                else if (domain.Contains("meet"))
                {
                    return "Google Meet";
                }
                else if (domain.Contains("calendar"))
                {
                    return "Google Calendar";
                }
                else if (domain.Contains("contacts"))
                {
                    return "Google Contacts";
                }
                else if (domain.Contains("play"))
                {
                    return "Google Play";
                }
                else if (domain.Contains("translate"))
                {
                    return "Google Translate";
                }
                else if (domain.Contains("photos"))
                {
                    return "Google Photos";
                }
                else if (domain.Contains("myadcenter"))
                {
                    return "Google Ad Center";
                }
                else if ((domain.Contains("shopping")) || (path.StartsWith("/shopping")))
                {
                    return "Google Shopping";
                }
                else if (path.StartsWith("/search")) 
                {
                    return "Google Search";
                }                
                else if (path.StartsWith("/maps/"))
                {
                    return "Google Maps";
                }                
                else if (path.Contains("/#chat"))
                {
                    return "Google Chat";
                }                
                else if (path.StartsWith("/finance"))
                {
                    return "Google Finance";
                }
                else if (path.Contains("/travel/flights/"))
                {
                    return "Google Flights";
                }
                else if (path.StartsWith("/travel/"))
                {
                    return "Google Travel";
                }
                else
                {
                    return "Browsing Google";
                }


            }

            if (domain.Contains("microsoft.com"))
            {
                if (path.Contains("downloads"))
                {
                    return "Downloading Software";
                }
                else if (domain.Contains("office") || path.Contains("office"))
                {
                    return "Using Online Office Suite";
                }
            }

            if (domain.Contains("facebook.com"))
            {
                
                if (path.Contains("/profile"))
                {
                    return "Facebook Viewing Profile"; 
                }
                else if (path.Contains("/groups"))
                {
                    return "Facebook Browsing Groups";
                }
                else if (path.Contains("/photo"))
                {
                    return "Facebook Viewing Photos"; 
                }
                else if (path.Contains("friends/suggestions/"))
                {
                    return "Facebook Friend Suggestions";
                }
                else if (path.Contains("/friends_mutual/"))
                {
                    return "Facebook Friends Mutual with";
                }
                else if (path.Contains("/search"))
                {
                    return "Facebook Searching"; 
                }
                else if (path.Contains("/reel"))
                {
                    return "Facebook Reel"; 
                }
                else if (path.Contains("/video"))
                {
                    return "Facebook Watching Video";
                }
                else if (path.Contains("/marketplace"))
                {
                    return "Facebook Marketplace"; 
                }
                else if (path.Contains("/events"))
                {
                    return "Facebook Events"; 
                }
                else if (path.Contains("/friends"))
                {
                    return "Facebook Friends";
                }
                else if (Regex.IsMatch(path, "/[^/]+$"))
                {
                    return "Facebook Viewing User Wall"; 
                }
                else
                {
                    return "Facebook Social Networking";
                }
            }

            if (domain.Contains("twitter.com"))
            {
                if (path.Contains("/login"))
                {
                    return "Twitter Login";
                }
                else if (path.Contains("/home"))
                {
                    return "Viewing Twitter Feed";
                }
                else if (path.Contains("/hashtag"))
                {
                    return "Browsing Twitter Hashtags";
                }
                else
                {
                    return "Social Networking on Twitter";
                }
            }

            if (domain.Contains("amazon.com") || domain.Contains("ebay.com"))
            {
                return "Shopping Online";
            }

            if (domain.Contains("youtube.com"))
            {
                if (path.Contains("/results"))
                {
                    return "YouTube Search";
                }
                else if (path.Contains("/watch"))
                {
                    return "YouTube Video Watch";
                }
                else if (path.Contains("/shorts"))
                {
                    return "YouTube Video shorts";
                }
                else
                {
                    return "Browsing YouTube";
                }
            }


            if (domain.Contains("chatgpt.com"))
            {
                if (path.Contains("/c"))
                {
                    return "ChatGPT Conversation";
                }
                else if (path.Contains("/gpts"))
                {
                    return "ChatGPT Explore";
                }
                
                else if (path.Contains("/gpts#settings"))
                {
                    return "ChatGPT Settings";
                }
                
                else
                {
                    return "Chatbot Platform";
                }
            }


            if (domain.Contains("netflix.com"))
            {
                return "Streaming Video";
            }



            return "Browsing Website";
        }




        private static string GetTransitionDescriptionFirefox(int visitType)
        {
            switch (visitType)
            {
                case 1:
                    return "Link";
                case 2:
                    return "Typed";
                case 3:
                    return "Bookmark";
                case 4:
                    return "Embed";
                case 5:
                    return "Redirect (Permanent)";
                case 6:
                    return "Redirect (Temporary)";
                case 7:
                    return "Download";
                case 8:
                    return "Framed Link";
                case 9:
                    return "Reload";
                default:
                    return "Unknown";
            }
        }

        private static string GetNavigationContextFirefox(int visitType)
        {
            return visitType switch
            {
                1 => "Link navigation",
                2 => "Typed or address bar navigation",
                3 => "Bookmark navigation",
                4 => "Embedded content loaded",
                5 => "Permanent redirect",
                6 => "Temporary redirect",
                7 => "Download-related navigation",
                8 => "Framed content navigation",
                9 => "Page reload",
                _ => "Unknown navigation context"
            };
        }

        private static string GetUserActionLikelihoodFirefox(int visitType)
        {
            return visitType switch
            {
                1 => "High",
                2 => "High",
                3 => "High",
                7 => "High",
                5 => "Medium",
                6 => "Medium",
                9 => "Medium",
                4 => "Low",
                8 => "Low",
                _ => "Unknown"
            };
        }

        private static string BuildArtifactType(string browserType, string artifactName)
        {
            string browser = string.IsNullOrWhiteSpace(browserType) ? "Unknown" : browserType.Trim();
            return $"{browser} {artifactName}";
        }









        private bool SetBrowserType(string filePath)
        {
            if (IsTemporaryBrowserArtifactPath(filePath))
            {
                Helpers.BrowserFamily = "Unknown";
                Helpers.BrowserContainerKey = string.Empty;
                Helpers.BrowserType = "Unknown";
                return false;
            }

            string profileDirectory = GetOwningBrowserProfileDirectory(filePath) ?? GetProfileDirectory(filePath);
            string browserFamily = string.Empty;

            if (string.IsNullOrWhiteSpace(browserFamily)
                && (LooksLikeFirefoxArtifact(filePath) || LooksLikeFirefoxProfile(profileDirectory)))
            {
                browserFamily = "Firefox-like";
            }

            if (string.IsNullOrWhiteSpace(browserFamily)
                && (LooksLikeChromiumArtifact(filePath) || LooksLikeChromiumProfile(profileDirectory)))
            {
                browserFamily = "Chromium-like";
            }

            if (!string.IsNullOrWhiteSpace(browserFamily))
            {
                Helpers.BrowserFamily = browserFamily;
                Helpers.BrowserContainerKey = Helpers.GetBrowserContainerKey(filePath, browserFamily);
                Helpers.BrowserType = Helpers.ResolveDisplayBrowserName(null, filePath, browserFamily);
                return true;
            }

            Helpers.BrowserFamily = "Unknown";
            Helpers.BrowserContainerKey = Helpers.GetBrowserContainerKey(filePath, browserFamily);
            Helpers.BrowserType = "Unknown";
            return false;
        }

        private static bool IsTemporaryBrowserArtifactPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(path);
            }
            catch
            {
                fullPath = path;
            }

            string normalizedPath = fullPath.Trim().ToLowerInvariant();
            string tempRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).ToLowerInvariant();
            string browserReviewerTempRoot = Path.Combine(tempRoot, "browserreviewertemp");
            string fileName = Path.GetFileName(normalizedPath);

            if (normalizedPath.StartsWith(browserReviewerTempRoot + "\\", StringComparison.OrdinalIgnoreCase))
                return true;

            if (!normalizedPath.StartsWith(tempRoot + "\\", StringComparison.OrdinalIgnoreCase))
                return false;

            return fileName == "places.sqlite"
                || fileName == "formhistory.sqlite"
                || fileName == "cookies.sqlite";
        }

        private static string GetProfileDirectory(string path)
        {
            if (Directory.Exists(path))
                return path;

            return Path.GetDirectoryName(path) ?? string.Empty;
        }

        private static string? GetOwningBrowserProfileDirectory(string path)
        {
            string? currentPath = Directory.Exists(path) ? path : Path.GetDirectoryName(path);

            while (!string.IsNullOrWhiteSpace(currentPath))
            {
                if (LooksLikeFirefoxProfile(currentPath) || LooksLikeChromiumProfile(currentPath))
                    return currentPath;

                if (LooksLikeKnownEmbeddedChromiumContainer(currentPath))
                {
                    string containerPath = Helpers.GetBrowserContainerKey(currentPath, "Chromium-like");
                    if (Directory.Exists(containerPath))
                        return containerPath;
                }

                DirectoryInfo? parent = Directory.GetParent(currentPath);
                currentPath = parent?.FullName;
            }

            return null;
        }

        private static bool PathsEqual(string left, string right)
        {
            string normalizedLeft = NormalizeProcessedSourcePath(left);
            string normalizedRight = NormalizeProcessedSourcePath(right);
            return string.Equals(normalizedLeft, normalizedRight, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsChromiumCookiesArtifactFile(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            if (!fileName.Equals("Cookies", StringComparison.OrdinalIgnoreCase))
                return false;

            DirectoryInfo? parent = Directory.GetParent(filePath);
            return parent != null &&
                   (parent.Name.Equals("Network", StringComparison.OrdinalIgnoreCase)
                    || LooksLikeChromiumProfile(parent.FullName));
        }

        private static bool IsChromiumLocalStorageArtifactDirectory(string directoryPath)
        {
            string directoryName = Path.GetFileName(directoryPath);
            if (directoryName.Equals("Local Storage", StringComparison.OrdinalIgnoreCase))
                return true;

            DirectoryInfo? parent = Directory.GetParent(directoryPath);
            return directoryName.Equals("leveldb", StringComparison.OrdinalIgnoreCase)
                && parent != null
                && parent.Name.Equals("Local Storage", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsIndexedDbArtifactDirectory(string directoryPath)
        {
            string directoryName = Path.GetFileName(directoryPath);
            if (directoryName.Equals("IndexedDB", StringComparison.OrdinalIgnoreCase))
                return true;

            DirectoryInfo? parent = Directory.GetParent(directoryPath);
            return directoryName.Equals("default", StringComparison.OrdinalIgnoreCase)
                && parent != null
                && parent.Name.Equals("storage", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsChromiumCacheArtifactDirectory(string directoryPath)
        {
            string? profilePath = GetOwningBrowserProfileDirectory(directoryPath);
            if (string.IsNullOrWhiteSpace(profilePath))
                return false;

            return GetChromeCacheDirectories(profilePath).Any(candidate => PathsEqual(candidate, directoryPath));
        }

        private static bool IsFirefoxCacheArtifactDirectory(string directoryPath)
        {
            string? profilePath = GetOwningBrowserProfileDirectory(directoryPath);
            if (string.IsNullOrWhiteSpace(profilePath))
                return false;

            return GetFirefoxCacheDirectories(profilePath).Any(candidate => PathsEqual(candidate, directoryPath));
        }

        private static bool LooksLikeKnownEmbeddedChromiumContainer(string path)
        {
            string? knownBrowser = Helpers.DetectKnownBrowserNameFromPath(path);
            if (!Helpers.TryGetBrowserFamily(knownBrowser, out string family) || family != "Chromium-like")
                return false;

            string normalizedPath = path.Replace('/', '\\').ToLowerInvariant();
            return normalizedPath.Contains("\\cache\\")
                || normalizedPath.EndsWith("\\cache", StringComparison.OrdinalIgnoreCase)
                || normalizedPath.Contains("\\code cache\\")
                || normalizedPath.Contains("\\gpucache")
                || normalizedPath.Contains("\\local storage\\")
                || normalizedPath.Contains("\\session storage")
                || normalizedPath.Contains("\\indexeddb")
                || normalizedPath.Contains("\\extensions");
        }

        private static bool LooksLikeFirefoxArtifact(string filePath)
        {
            string fileName = Path.GetFileName(filePath).ToLowerInvariant();
            return fileName == "places.sqlite"
                || fileName == "formhistory.sqlite"
                || fileName == "cookies.sqlite"
                || fileName == "webappsstore.sqlite"
                || fileName == "logins.json"
                || fileName == "key4.db"
                || fileName == "extensions.json"
                || fileName.EndsWith(".jsonlz4")
                || fileName.EndsWith(".baklz4");
        }

        private static bool LooksLikeChromiumArtifact(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            return fileName.Equals("History", StringComparison.OrdinalIgnoreCase)
                || fileName.Equals("Bookmarks", StringComparison.OrdinalIgnoreCase)
                || fileName.Equals("Login Data", StringComparison.OrdinalIgnoreCase)
                || fileName.Equals("Web Data", StringComparison.OrdinalIgnoreCase)
                || fileName.Equals("Preferences", StringComparison.OrdinalIgnoreCase)
                || fileName.Equals("Local State", StringComparison.OrdinalIgnoreCase)
                || fileName.Equals("Last Session", StringComparison.OrdinalIgnoreCase)
                || fileName.Equals("Last Tabs", StringComparison.OrdinalIgnoreCase)
                || fileName.Equals("Current Session", StringComparison.OrdinalIgnoreCase)
                || fileName.Equals("Current Tabs", StringComparison.OrdinalIgnoreCase)
                || fileName.StartsWith("Session_", StringComparison.OrdinalIgnoreCase)
                || fileName.StartsWith("Tabs_", StringComparison.OrdinalIgnoreCase);
        }

        private static bool LooksLikeFirefoxProfile(string profilePath)
        {
            if (string.IsNullOrWhiteSpace(profilePath) || !Directory.Exists(profilePath))
                return false;

            return File.Exists(Path.Combine(profilePath, "places.sqlite"))
                || File.Exists(Path.Combine(profilePath, "formhistory.sqlite"))
                || File.Exists(Path.Combine(profilePath, "cookies.sqlite"))
                || (File.Exists(Path.Combine(profilePath, "logins.json")) && File.Exists(Path.Combine(profilePath, "key4.db")))
                || Directory.Exists(Path.Combine(profilePath, "sessionstore-backups"))
                || Directory.Exists(Path.Combine(profilePath, "storage", "default"))
                || Directory.Exists(Path.Combine(profilePath, "cache2", "entries"));
        }

        private static bool LooksLikeChromiumProfile(string profilePath)
        {
            if (string.IsNullOrWhiteSpace(profilePath) || !Directory.Exists(profilePath))
                return false;

            return File.Exists(Path.Combine(profilePath, "History"))
                || File.Exists(Path.Combine(profilePath, "Bookmarks"))
                || File.Exists(Path.Combine(profilePath, "Login Data"))
                || File.Exists(Path.Combine(profilePath, "Web Data"))
                || Directory.Exists(Path.Combine(profilePath, "Local Storage", "leveldb"))
                || Directory.Exists(Path.Combine(profilePath, "Session Storage"))
                || Directory.Exists(Path.Combine(profilePath, "IndexedDB"))
                || Directory.Exists(Path.Combine(profilePath, "Extensions"))
                || Directory.Exists(Path.Combine(profilePath, "Sessions"))
                || File.Exists(Path.Combine(profilePath, "Network", "Cookies"))
                || File.Exists(Path.Combine(profilePath, "Cookies"));
        }


        public void MostrarPorCategoria(Label labelStatus, SfDataGrid sfDataGrid, string connectionString, string tableName, string? categoria, string navegador, Label labelItemCount, RichTextBox Console)
        {
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string query;
                int utcOffset = Helpers.utcOffset;
                string offset = utcOffset == 0 ? string.Empty : (utcOffset > 0 ? $"+{utcOffset} hours" : $"{utcOffset} hours");
                string visitTimeExpr = string.IsNullOrEmpty(offset)
                    ? "STRFTIME('%Y-%m-%d %H:%M:%f', Visit_time)"
                    : $"STRFTIME('%Y-%m-%d %H:%M:%f', Visit_time, '{offset}')";
                string lastVisitTimeExpr = string.IsNullOrEmpty(offset)
                    ? "STRFTIME('%Y-%m-%d %H:%M:%f', Last_visit_time)"
                    : $"STRFTIME('%Y-%m-%d %H:%M:%f', Last_visit_time, '{offset}')";

                string searchCondition = tableName == "firefox_results"
                    ? SearchSql.TextCondition("Artifact_type", "Potential_activity", "Navigation_context", "User_action_likelihood", "Browser", "Category", "Url", "Title", "Transition")
                    : SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Category", "Url", "Title");
                string categoryCondition = string.IsNullOrEmpty(categoria) ? string.Empty : "Category = @Category";
                string whereClause = SearchSql.Where("Browser = @Browser", categoryCondition, searchCondition, SearchSql.TimeCondition("Visit_time", "Last_visit_time"), SearchSql.LabelCondition());

                if (tableName == "history_union")
                {
                    string firefoxWhereClause = SearchSql.Where(
                        "Browser = @Browser",
                        categoryCondition,
                        SearchSql.TextCondition("Artifact_type", "Potential_activity", "Navigation_context", "User_action_likelihood", "Browser", "Category", "Url", "Title", "Transition"),
                        SearchSql.TimeCondition("Visit_time", "Last_visit_time"),
                        SearchSql.LabelCondition());
                    string chromiumWhereClause = SearchSql.Where(
                        "Browser = @Browser",
                        categoryCondition,
                        SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Category", "Url", "Title"),
                        SearchSql.TimeCondition("Visit_time", "Last_visit_time"),
                        SearchSql.LabelCondition());

                    query = $@"
                        SELECT id, Artifact_type, Potential_activity, Browser, Category, Visit_id, Place_id, Url, Title,
                               {visitTimeExpr} AS Visit_time,
                               {lastVisitTimeExpr} AS Last_visit_time,
                               Visit_count, From_visit, Transition, Navigation_context, User_action_likelihood, Visit_type, Frecency, File, Label, Comment
                        FROM firefox_results {firefoxWhereClause}
                        UNION ALL
                        SELECT id, Artifact_type, Potential_activity, Browser, Category, Visit_id, NULL AS Place_id, Url, Title,
                               {visitTimeExpr} AS Visit_time,
                               {lastVisitTimeExpr} AS Last_visit_time,
                               Visit_count, NULL AS From_visit, Transition, NULL AS Navigation_context, NULL AS User_action_likelihood, NULL AS Visit_type, NULL AS Frecency, File, Label, Comment
                        FROM results {chromiumWhereClause}";
                }
                else if (tableName == "firefox_results")
                {
                    query = $@"SELECT id, Artifact_type, Potential_activity, Browser, Category, Visit_id, Place_id, Url, Title,
                                      {visitTimeExpr} AS Visit_time,
                                      {lastVisitTimeExpr} AS Last_visit_time,
                                      Visit_count, From_visit, Transition, Navigation_context, User_action_likelihood, Visit_type, Frecency, File, Label, Comment
                              FROM {tableName} {whereClause}";
                }
                else if (tableName == "results")
                {
                    query = $@"SELECT id, Artifact_type, Potential_activity, Browser, Category, Visit_id, Url, Title,
                                      {visitTimeExpr} AS Visit_time,
                                      {lastVisitTimeExpr} AS Last_visit_time,
                                      Visit_duration, Visit_count, Typed_count, From_url, Transition, File, Label, Comment
                              FROM {tableName} {whereClause}";
                }
                else
                {
                    return;
                }


                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Browser", navegador);

                    if (!string.IsNullOrEmpty(categoria))
                    {
                        command.Parameters.AddWithValue("@Category", categoria);
                    }

                    SearchSql.AddParameters(command);

                    using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(command))
                    {
                        DataTable dataTable = new DataTable();
                        adapter.Fill(dataTable);
                        sfDataGrid.DataSource = dataTable;

                        labelItemCount.Text = $"Items count: {sfDataGrid.RowCount - 1}";

                        if (sfDataGrid.View != null && sfDataGrid.View.Records.Count > 0)
                        {
                            sfDataGrid.SelectedIndex = 0;
                            sfDataGrid.MoveToCurrentCell(new RowColumnIndex(0, 0));
                        }

                        ApplySearchHighlight(sfDataGrid);
                    }
                }












                if (string.IsNullOrEmpty(categoria))
                {

                    labelStatus.Text = $"{navegador} all history.";
                }
                else
                {
                    labelStatus.Text = $"{navegador} : {categoria}.";
                }
            }
        }











        public Dictionary<string, List<string>> FillDictionaryFromDatabase()
        {
            Helpers.browserUrls = new Dictionary<string, List<string>>();

            using (SQLiteConnection connection = new SQLiteConnection(Helpers.chromeViewerConnectionString))
            {
                connection.Open();

                string query = "SELECT Browser, Url FROM results UNION ALL SELECT Browser, Url FROM firefox_results";

                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string browser = reader.GetString(0);
                            string url = reader.GetString(1);

                            if (!Helpers.browserUrls.ContainsKey(browser))
                            {
                                Helpers.browserUrls[browser] = new List<string>();
                            }

                            Helpers.browserUrls[browser].Add(url);
                        }
                    }
                }
            }

            return Helpers.browserUrls;
        }

























        public static Dictionary<string, int> GetBrowsersWithDownloads()
        {
            Dictionary<string, int> browsersWithDownloads = new Dictionary<string, int>();

            string queryChrome = "SELECT Browser, COUNT(*) FROM chrome_downloads GROUP BY Browser;";
            string queryFirefox = "SELECT Browser, COUNT(*) FROM firefox_downloads GROUP BY Browser;";

            using (SQLiteConnection connection = new SQLiteConnection(Helpers.chromeViewerConnectionString))
            {
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand(queryChrome, connection))
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string browser = reader.GetString(0);
                        int count = reader.GetInt32(1);

                        if (browsersWithDownloads.ContainsKey(browser))
                        {
                            browsersWithDownloads[browser] += count;
                        }
                        else
                        {
                            browsersWithDownloads[browser] = count;
                        }
                    }
                }

                using (SQLiteCommand command = new SQLiteCommand(queryFirefox, connection))
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string browser = reader.GetString(0);
                        int count = reader.GetInt32(1);

                        if (browsersWithDownloads.ContainsKey(browser))
                        {
                            browsersWithDownloads[browser] += count;
                        }
                        else
                        {
                            browsersWithDownloads[browser] = count;
                        }
                    }
                }
            }

            return browsersWithDownloads;
        }


        public static Dictionary<string, int> GetBrowsersWithBookmarks()
        {
            Dictionary<string, int> browsersWithBookmarks = new Dictionary<string, int>();

            string queryChrome = "SELECT Browser, COUNT(*) FROM bookmarks_Chrome GROUP BY Browser;";
            string queryFirefox = "SELECT Browser, COUNT(*) FROM bookmarks_Firefox GROUP BY Browser;";

            using (SQLiteConnection connection = new SQLiteConnection(Helpers.chromeViewerConnectionString))
            {
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand(queryChrome, connection))
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string browser = reader.GetString(0);
                        int count = reader.GetInt32(1);

                        if (browsersWithBookmarks.ContainsKey(browser))
                        {
                            browsersWithBookmarks[browser] += count;
                        }
                        else
                        {
                            browsersWithBookmarks[browser] = count;
                        }
                    }
                }

                using (SQLiteCommand command = new SQLiteCommand(queryFirefox, connection))
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string browser = reader.GetString(0);
                        int count = reader.GetInt32(1);

                        if (browsersWithBookmarks.ContainsKey(browser))
                        {
                            browsersWithBookmarks[browser] += count;
                        }
                        else
                        {
                            browsersWithBookmarks[browser] = count;
                        }
                    }
                }
            }

            return browsersWithBookmarks;
        }


        public static Dictionary<string, int> GetBrowsersWithAutofill()
        {
            return GetBrowserCountsFromTable("autofill_data");
        }


        public static Dictionary<string, int> GetBrowsersWithCookies()
        {
            return GetBrowserCountsFromTable("cookies_data");
        }


        public static Dictionary<string, int> GetBrowsersWithCache()
        {
            return GetBrowserCountsFromTable("cache_data");
        }

        public static Dictionary<string, int> GetBrowsersWithSessions()
        {
            return GetBrowserCountsFromTable("session_data");
        }

        public static Dictionary<string, int> GetBrowsersWithExtensions()
        {
            return GetBrowserCountsFromTable("extension_data");
        }

        public static Dictionary<string, int> GetBrowsersWithLogins()
        {
            return GetBrowserCountsFromTable("saved_logins_data");
        }

        public static Dictionary<string, int> GetBrowsersWithLocalStorage()
        {
            return GetBrowserCountsFromTable("local_storage_data");
        }

        public static Dictionary<string, int> GetBrowsersWithSessionStorage()
        {
            return GetBrowserCountsFromTable("session_storage_data");
        }

        public static Dictionary<string, int> GetBrowsersWithIndexedDb()
        {
            return GetBrowserCountsFromTable("indexeddb_data");
        }

        private static Dictionary<string, int> GetBrowserCountsFromTable(string tableName)
        {
            Dictionary<string, int> browserCounts = new Dictionary<string, int>();
            string query = $"SELECT Browser, COUNT(*) FROM {tableName} GROUP BY Browser;";
            using (SQLiteConnection connection = new SQLiteConnection(Helpers.chromeViewerConnectionString))
            {
                connection.Open();
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string browser = reader.GetString(0);
                        int count = reader.GetInt32(1);
                        browserCounts[browser] = count;
                    }
                }
            }

            return browserCounts;
        }

        public static void ProcessAndWriteLocalStorage(string connectionString, string browserType, string profilePath, RichTextBox? logConsole = null)
        {
            if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(profilePath) || !Directory.Exists(profilePath))
                return;

            if (!TryRegisterProcessedSource("local-storage", profilePath, logConsole))
                return;

            try
            {
                bool isFirefoxLike = LooksLikeFirefoxProfile(profilePath) || Helpers.IsFirefoxLikeBrowser(browserType);
                string resolvedBrowserType = Helpers.ResolveDisplayBrowserName(browserType, profilePath, isFirefoxLike ? "Firefox-like" : "Chromium-like");

                if (isFirefoxLike)
                    ProcessFirefoxLocalStorage(connectionString, resolvedBrowserType, profilePath, logConsole);
                else
                    ProcessChromiumLocalStorage(connectionString, resolvedBrowserType, profilePath, logConsole);
            }
            catch (Exception ex)
            {
                if (logConsole != null)
                    LogToConsole(logConsole, $"Local Storage parser error: {ex.Message}", Color.Red);
            }
        }

        public static void ProcessAndWriteSessionStorage(string connectionString, string browserType, string profilePath, RichTextBox? logConsole = null)
        {
            if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(profilePath) || !Directory.Exists(profilePath))
                return;

            if (!TryRegisterProcessedSource("session-storage", profilePath, logConsole))
                return;

            try
            {
                bool isFirefoxLike = LooksLikeFirefoxProfile(profilePath)
                    || Helpers.IsFirefoxLikeBrowser(browserType)
                    || Helpers.IsFirefoxLikeBrowser(Helpers.BrowserFamily);
                string resolvedBrowserType = Helpers.ResolveDisplayBrowserName(browserType, profilePath, isFirefoxLike ? "Firefox-like" : "Chromium-like");
                int inserted = isFirefoxLike
                    ? ProcessFirefoxSessionStorage(connectionString, resolvedBrowserType, profilePath)
                    : ProcessChromiumFileStorage(connectionString, resolvedBrowserType, profilePath, "session_storage_data", "session storage", "Session Storage web application state", "Session Storage", "Chromium Session Storage LevelDB", "session-storage-v1.0", "Medium", "Best-effort string extraction from Chromium Session Storage LevelDB files. Temporal fields are blank because file timestamps are not reliable browser activity times.");

                if (inserted > 0 && logConsole != null)
                    LogToConsole(logConsole, $"Processed Session Storage for {resolvedBrowserType}: {profilePath} ({inserted} entries)", Color.Green);
            }
            catch (Exception ex)
            {
                if (logConsole != null)
                    LogToConsole(logConsole, $"Session Storage parser error: {ex.Message}", Color.Red);
            }
        }

        public static void ProcessAndWriteIndexedDb(string connectionString, string browserType, string profilePath, RichTextBox? logConsole = null)
        {
            if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(profilePath) || !Directory.Exists(profilePath))
                return;

            if (!TryRegisterProcessedSource("indexeddb", profilePath, logConsole))
                return;

            try
            {
                bool isFirefoxLike = LooksLikeFirefoxProfile(profilePath) || Helpers.IsFirefoxLikeBrowser(browserType);
                string resolvedBrowserType = Helpers.ResolveDisplayBrowserName(browserType, profilePath, isFirefoxLike ? "Firefox-like" : "Chromium-like");
                int inserted = isFirefoxLike
                    ? ProcessFirefoxIndexedDb(connectionString, resolvedBrowserType, profilePath)
                    : ProcessChromiumFileStorage(connectionString, resolvedBrowserType, profilePath, "indexeddb_data", "indexeddb", "IndexedDB web application database state", "IndexedDB", "Chromium IndexedDB files", "indexeddb-v1.0", "Medium", "Best-effort extraction from Chromium IndexedDB LevelDB and backing files. Temporal fields are blank because file timestamps are not reliable browser activity times.");

                if (inserted > 0 && logConsole != null)
                    LogToConsole(logConsole, $"Processed IndexedDB for {resolvedBrowserType}: {profilePath} ({inserted} entries)", Color.Green);
            }
            catch (Exception ex)
            {
                if (logConsole != null)
                    LogToConsole(logConsole, $"IndexedDB parser error: {ex.Message}", Color.Red);
            }
        }

        private static int ProcessFirefoxSessionStorage(string connectionString, string browserType, string profilePath)
        {
            List<string> files = new List<string>();
            files.AddRange(Directory.GetFiles(profilePath, "sessionstore*.json*", SearchOption.TopDirectoryOnly));

            string backupsPath = Path.Combine(profilePath, "sessionstore-backups");
            if (Directory.Exists(backupsPath))
            {
                files.AddRange(Directory.GetFiles(backupsPath, "*.json*", SearchOption.TopDirectoryOnly));
                files.AddRange(Directory.GetFiles(backupsPath, "*.bak*", SearchOption.TopDirectoryOnly));
            }

            return ProcessStorageFiles(
                connectionString,
                browserType,
                files,
                "session_storage_data",
                "session storage",
                "Session Storage web application state",
                "Firefox sessionstore session storage",
                "Session Storage",
                "session-storage-v1.0",
                "Medium",
                "Best-effort extraction from Firefox sessionstore/sessionstore-backups files; compressed jsonlz4 data is searched for strings. Temporal fields are blank because file timestamps are not reliable browser activity times.");
        }

        private static int ProcessFirefoxIndexedDb(string connectionString, string browserType, string profilePath)
        {
            string storagePath = Path.Combine(profilePath, "storage", "default");
            if (!Directory.Exists(storagePath))
                return 0;

            List<string> files = Directory.GetFiles(storagePath, "*.*", SearchOption.AllDirectories)
                .Where(path => path.IndexOf($"{Path.DirectorySeparatorChar}idb{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) >= 0
                    || path.EndsWith(".sqlite", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".files", StringComparison.OrdinalIgnoreCase))
                .ToList();

            return ProcessStorageFiles(
                connectionString,
                browserType,
                files,
                "indexeddb_data",
                "indexeddb",
                "IndexedDB web application database state",
                "Firefox IndexedDB storage/default idb",
                "IndexedDB",
                "indexeddb-v1.0",
                "Medium",
                "Best-effort extraction from Firefox storage/default origin idb files. Temporal fields are blank because file timestamps are not reliable browser activity times.");
        }

        private static int ProcessChromiumFileStorage(
            string connectionString,
            string browserType,
            string profilePath,
            string tableName,
            string artifactSuffix,
            string potentialActivity,
            string relativeDirectory,
            string sourceKind,
            string parserVersion,
            string confidence,
            string notes)
        {
            string directory = Path.Combine(profilePath, relativeDirectory);
            if (!Directory.Exists(directory))
                return 0;

            List<string> files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories)
                .Where(IsLikelyStorageDataFile)
                .ToList();

            return ProcessStorageFiles(connectionString, browserType, files, tableName, artifactSuffix, potentialActivity, sourceKind, relativeDirectory, parserVersion, confidence, notes);
        }

        private static int ProcessStorageFiles(
            string connectionString,
            string browserType,
            IEnumerable<string> files,
            string tableName,
            string artifactSuffix,
            string potentialActivity,
            string sourceKind,
            string artifactName,
            string parserVersion,
            string confidence,
            string notes)
        {
            int inserted = 0;
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using SQLiteConnection targetConnection = new SQLiteConnection(connectionString);
            targetConnection.Open();
            using SQLiteTransaction transaction = targetConnection.BeginTransaction();

            foreach (string file in files.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                byte[] data;
                try
                {
                    FileInfo info = new FileInfo(file);
                    if (!info.Exists || info.Length <= 0 || info.Length > 64L * 1024L * 1024L)
                        continue;
                    data = ReadStorageSourceBytes(file);
                }
                catch
                {
                    continue;
                }

                string origin = InferStorageOriginFromPath(file) ?? string.Empty;
                int perFile = 0;
                foreach (string candidate in ExtractStorageCandidates(data).Take(300))
                {
                    string candidateOrigin = ExtractFirstUrl(new[] { candidate }) ?? origin;
                    string key = ExtractStorageKey(candidate, candidateOrigin);
                    string preview = BuildLocalStoragePreview(candidate);
                    byte[] valueBytes = Encoding.UTF8.GetBytes(candidate);
                    string sha = ComputeSha256Hex(valueBytes);
                    string dedupeKey = $"{tableName}|{file}|{sha}";
                    if (!seen.Add(dedupeKey))
                        continue;

                    InsertStorageRow(
                        targetConnection,
                        transaction,
                        tableName,
                        browserType,
                        artifactSuffix,
                        potentialActivity,
                        candidateOrigin,
                        ExtractHost(candidateOrigin),
                        key,
                        preview,
                        valueBytes.Length,
                        sha,
                        sourceKind,
                        file,
                        null,
                        null,
                        null,
                        notes,
                        file);
                    inserted++;
                    perFile++;
                }

                if (perFile == 0 && tableName.Equals("indexeddb_data", StringComparison.OrdinalIgnoreCase))
                {
                    string sha = ComputeSha256Hex(data);
                    string dedupeKey = $"{tableName}|{file}|{sha}";
                    if (seen.Add(dedupeKey))
                    {
                        InsertStorageRow(
                            targetConnection,
                            transaction,
                            tableName,
                            browserType,
                            artifactSuffix,
                            potentialActivity,
                            origin,
                            ExtractHost(origin),
                            Path.GetFileName(file),
                            $"Binary IndexedDB backing file: {Path.GetFileName(file)}",
                            data.Length,
                            sha,
                            sourceKind,
                            file,
                            null,
                            null,
                            null,
                            $"{notes} No printable candidate was extracted; binary file hash preserved.",
                            file);
                        inserted++;
                    }
                }
            }

            transaction.Commit();

            if (inserted > 0)
            {
                BrowserReviewerLogger.LogParserRun(
                    artifactName,
                    browserType,
                    parserVersion,
                    confidence,
                    sourceKind,
                    inserted,
                    string.Join("; ", files.Take(3)),
                    notes);
            }

            return inserted;
        }

        private static byte[] ReadStorageSourceBytes(string file)
        {
            if (IsFirefoxSessionFile(file))
            {
                string? json = TryReadFirefoxJsonlz4(file);
                if (!string.IsNullOrWhiteSpace(json))
                    return Encoding.UTF8.GetBytes(json);
            }

            return File.ReadAllBytes(file);
        }

        private static void ProcessFirefoxLocalStorage(string connectionString, string browserType, string profilePath, RichTextBox? logConsole)
        {
            string dbPath = Path.Combine(profilePath, "webappsstore.sqlite");
            if (!File.Exists(dbPath))
                return;

            using SQLiteConnection sourceConnection = new SQLiteConnection($"Data Source={dbPath};Version=3;");
            sourceConnection.Open();

            HashSet<string> columns = GetSqliteColumns(sourceConnection, "webappsstore2");
            if (!columns.Contains("key") || !columns.Contains("value"))
                return;

            string originExpr = SelectColumn(columns, "originKey", "NULL");
            string scopeExpr = SelectColumn(columns, "scope", "NULL");
            string keyExpr = SelectColumn(columns, "key", "NULL");
            string valueExpr = SelectColumn(columns, "value", "NULL");

            string query = $@"
                SELECT
                    {originExpr},
                    {scopeExpr},
                    {keyExpr},
                    {valueExpr}
                FROM webappsstore2";

            int inserted = 0;
            using SQLiteCommand selectCommand = new SQLiteCommand(query, sourceConnection);
            using SQLiteDataReader reader = selectCommand.ExecuteReader();
            using SQLiteConnection targetConnection = new SQLiteConnection(connectionString);
            targetConnection.Open();
            using SQLiteTransaction transaction = targetConnection.BeginTransaction();

            DateTime? modified = null;
            DateTime? created = null;
            DateTime? accessed = null;

            while (reader.Read())
            {
                string? originKey = GetNullableString(reader, "originKey");
                string? scope = GetNullableString(reader, "scope");
                string? key = GetNullableString(reader, "key");
                string? value = GetNullableString(reader, "value");
                string origin = DecodeFirefoxOrigin(originKey, scope);
                string preview = BuildLocalStoragePreview(value);
                byte[] valueBytes = string.IsNullOrEmpty(value) ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(value);

                InsertLocalStorageRow(
                    targetConnection,
                    transaction,
                    browserType,
                    origin,
                    ExtractHost(origin),
                    key,
                    preview,
                    valueBytes.Length,
                    ComputeSha256Hex(valueBytes),
                    "Firefox webappsstore.sqlite",
                    dbPath,
                    created,
                    modified,
                    accessed,
                    "Parsed from Firefox webappsstore2 table. Temporal fields are blank because this table does not provide reliable per-record browser timestamps.",
                    dbPath);
                inserted++;
            }

            transaction.Commit();
            if (inserted > 0 && logConsole != null)
                LogToConsole(logConsole, $"Processed Firefox Local Storage: {dbPath} ({inserted} entries)", Color.Green);

            if (inserted > 0)
            {
                BrowserReviewerLogger.LogParserRun(
                    "Local Storage",
                    browserType,
                    "local-storage-v1.0",
                    "High",
                    "Firefox webappsstore.sqlite",
                    inserted,
                    dbPath,
                    "Parsed webappsstore2 key/value records. Filesystem timestamps are intentionally not used as browser activity times.");
            }
        }

        private static void ProcessChromiumLocalStorage(string connectionString, string browserType, string profilePath, RichTextBox? logConsole)
        {
            string levelDbPath = Path.Combine(profilePath, "Local Storage", "leveldb");
            if (!Directory.Exists(levelDbPath))
                return;

            string[] files = Directory.GetFiles(levelDbPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(path => path.EndsWith(".ldb", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (files.Length == 0)
                return;

            int inserted = 0;
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using SQLiteConnection targetConnection = new SQLiteConnection(connectionString);
            targetConnection.Open();
            using SQLiteTransaction transaction = targetConnection.BeginTransaction();

            foreach (string file in files)
            {
                byte[] data;
                try
                {
                    data = File.ReadAllBytes(file);
                }
                catch
                {
                    continue;
                }

                foreach (string candidate in ExtractLocalStorageCandidates(data).Take(250))
                {
                    string origin = ExtractFirstUrl(new[] { candidate }) ?? string.Empty;
                    string key = ExtractChromiumLocalStorageKey(candidate, origin);
                    string preview = BuildLocalStoragePreview(candidate);
                    byte[] valueBytes = Encoding.UTF8.GetBytes(candidate);
                    string sha = ComputeSha256Hex(valueBytes);
                    string dedupeKey = $"{file}|{sha}";
                    if (!seen.Add(dedupeKey))
                        continue;

                    InsertLocalStorageRow(
                        targetConnection,
                        transaction,
                        browserType,
                        origin,
                        ExtractHost(origin),
                        key,
                        preview,
                        valueBytes.Length,
                        sha,
                        "Chromium Local Storage LevelDB",
                        file,
                    null,
                    null,
                    null,
                    "Best-effort string extraction from LevelDB .ldb/.log files. Temporal fields are blank because filesystem timestamps are often export, mount, or parser access times.",
                    file);
                    inserted++;
                }
            }

            transaction.Commit();
            if (inserted > 0 && logConsole != null)
                LogToConsole(logConsole, $"Processed Chromium Local Storage for {browserType}: {levelDbPath} ({inserted} entries)", Color.Green);

            if (inserted > 0)
            {
                BrowserReviewerLogger.LogParserRun(
                    "Local Storage",
                    browserType,
                    "local-storage-v1.0",
                    "Medium",
                    "Chromium Local Storage LevelDB",
                    inserted,
                    levelDbPath,
                    "Best-effort extraction from LevelDB files; records preserve file, hash, size, and preview.");
            }
        }

        private static IEnumerable<string> ExtractLocalStorageCandidates(byte[] data)
        {
            foreach (string value in ExtractStorageCandidates(data))
            {
                yield return value;
            }
        }

        private static IEnumerable<string> ExtractStorageCandidates(byte[] data)
        {
            foreach (string value in ExtractPrintableStrings(data).Concat(ExtractUtf16LePrintableStrings(data)))
            {
                string cleaned = NormalizeLocalStorageText(value);
                if (cleaned.Length < 8)
                    continue;

                if (Regex.IsMatch(cleaned, @"https?://|file://|localStorage|session|indexedDB|idb|token|auth|user|email|login|cart|jwt|profile|message|chat|wallet|account", RegexOptions.IgnoreCase))
                    yield return cleaned;
            }
        }

        private static List<string> ExtractUtf16LePrintableStrings(byte[] bytes)
        {
            List<string> results = new List<string>();
            StringBuilder current = new StringBuilder();

            for (int i = 0; i + 1 < bytes.Length; i += 2)
            {
                ushort value = BitConverter.ToUInt16(bytes, i);
                if ((value >= 32 && value <= 126) || value == 9)
                {
                    current.Append((char)value);
                }
                else
                {
                    AddPrintableString(results, current);
                }
            }

            AddPrintableString(results, current);
            return results;
        }

        private static string DecodeFirefoxOrigin(string? originKey, string? scope)
        {
            if (!string.IsNullOrWhiteSpace(scope) && Uri.TryCreate(scope, UriKind.Absolute, out Uri? scopeUri))
                return scopeUri.GetLeftPart(UriPartial.Authority);

            if (string.IsNullOrWhiteSpace(originKey))
                return scope ?? string.Empty;

            Match match = Regex.Match(originKey, @"^([^:]+):(https?|file):?(\d+)?", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string reversedHost = match.Groups[1].Value.Trim('.');
                string host = string.Join(".", reversedHost.Split('.', StringSplitOptions.RemoveEmptyEntries).Reverse());
                string scheme = match.Groups[2].Value;
                string port = match.Groups[3].Success && !string.IsNullOrWhiteSpace(match.Groups[3].Value) ? $":{match.Groups[3].Value}" : "";
                return $"{scheme}://{host}{port}";
            }

            return originKey;
        }

        private static string ExtractChromiumLocalStorageKey(string candidate, string origin)
        {
            return ExtractStorageKey(candidate, origin);
        }

        private static string ExtractStorageKey(string candidate, string origin)
        {
            string text = candidate ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(origin))
            {
                int index = text.IndexOf(origin, StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    string tail = text.Substring(index + origin.Length).Trim('\0', ' ', '\t', '|', ':', '/', '\\');
                    if (!string.IsNullOrWhiteSpace(tail))
                        return tail.Length > 120 ? tail.Substring(0, 120) : tail;
                }
            }

            return text.Length > 80 ? text.Substring(0, 80) : text;
        }

        private static bool IsLikelyStorageDataFile(string path)
        {
            string extension = Path.GetExtension(path).ToLowerInvariant();
            string name = Path.GetFileName(path).ToLowerInvariant();
            return extension is ".ldb" or ".log" or ".sqlite" or ".sqlite-wal" or ".sqlite-shm" or ".blob" or ".dat"
                || Regex.IsMatch(name, @"^\d+$")
                || name.Contains("leveldb")
                || name.Contains("indexeddb")
                || name.Contains("session");
        }

        private static string? InferStorageOriginFromPath(string path)
        {
            string normalized = path.Replace('\\', '/');
            Match chromiumMatch = Regex.Match(normalized, @"/IndexedDB/([^/]+)\.indexeddb(?:\.leveldb)?/", RegexOptions.IgnoreCase);
            if (chromiumMatch.Success)
                return DecodeStorageOriginToken(chromiumMatch.Groups[1].Value);

            Match firefoxMatch = Regex.Match(normalized, @"/storage/default/([^/]+)/", RegexOptions.IgnoreCase);
            if (firefoxMatch.Success)
                return DecodeStorageOriginToken(firefoxMatch.Groups[1].Value);

            Match urlMatch = Regex.Match(normalized, @"https?_[^/\\]+", RegexOptions.IgnoreCase);
            if (urlMatch.Success)
                return DecodeStorageOriginToken(urlMatch.Value);

            return null;
        }

        private static string? DecodeStorageOriginToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            string decoded = Uri.UnescapeDataString(token)
                .Replace("^", ":")
                .Replace("_0.indexeddb", "")
                .Replace(".indexeddb", "");

            if (decoded.StartsWith("https_", StringComparison.OrdinalIgnoreCase))
                decoded = "https://" + decoded.Substring("https_".Length);
            else if (decoded.StartsWith("http_", StringComparison.OrdinalIgnoreCase))
                decoded = "http://" + decoded.Substring("http_".Length);

            decoded = decoded.Replace("_", ".");
            return decoded;
        }

        private static string NormalizeLocalStorageText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            string cleaned = Regex.Replace(value, @"[\x00-\x08\x0B\x0C\x0E-\x1F]+", " ");
            cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();
            return cleaned;
        }

        private static string BuildLocalStoragePreview(string? value)
        {
            string cleaned = NormalizeLocalStorageText(value);
            return cleaned.Length > 800 ? cleaned.Substring(0, 800) : cleaned;
        }

        private static void InsertLocalStorageRow(
            SQLiteConnection connection,
            SQLiteTransaction transaction,
            string browserType,
            string origin,
            string? host,
            string? storageKey,
            string valuePreview,
            int valueSize,
            string valueSha256,
            string sourceKind,
            string sourceFile,
            DateTime? created,
            DateTime? modified,
            DateTime? lastAccessed,
            string parserNotes,
            string file)
        {
            InsertStorageRow(connection, transaction, "local_storage_data", browserType, "local storage",
                "Local Storage web application state", origin, host, storageKey, valuePreview, valueSize,
                valueSha256, sourceKind, sourceFile, created, modified, lastAccessed, parserNotes, file);
        }

        private static void InsertStorageRow(
            SQLiteConnection connection,
            SQLiteTransaction transaction,
            string tableName,
            string browserType,
            string artifactSuffix,
            string potentialActivity,
            string origin,
            string? host,
            string? storageKey,
            string valuePreview,
            int valueSize,
            string valueSha256,
            string sourceKind,
            string sourceFile,
            DateTime? created,
            DateTime? modified,
            DateTime? lastAccessed,
            string parserNotes,
            string file)
        {
            if (tableName != "local_storage_data" && tableName != "session_storage_data" && tableName != "indexeddb_data")
                throw new ArgumentException("Unexpected storage table.", nameof(tableName));

            using SQLiteCommand command = new SQLiteCommand($@"
                INSERT INTO {tableName}
                (Artifact_type, Potential_activity, Browser, Origin, Host, Storage_key, Value_preview, Value_size,
                 Value_sha256, Source_kind, Source_file, Created, Modified, LastAccessed, Parser_notes, File)
                VALUES
                (@Artifact_type, @Potential_activity, @Browser, @Origin, @Host, @Storage_key, @Value_preview, @Value_size,
                 @Value_sha256, @Source_kind, @Source_file, @Created, @Modified, @LastAccessed, @Parser_notes, @File);", connection, transaction);

            command.Parameters.AddWithValue("@Artifact_type", BuildArtifactType(browserType, artifactSuffix));
            command.Parameters.AddWithValue("@Potential_activity", potentialActivity);
            command.Parameters.AddWithValue("@Browser", browserType);
            command.Parameters.AddWithValue("@Origin", origin ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Host", host ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Storage_key", storageKey ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Value_preview", valuePreview ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Value_size", valueSize);
            command.Parameters.AddWithValue("@Value_sha256", valueSha256 ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Source_kind", sourceKind ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Source_file", sourceFile ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Created", FormatDateTime(created) ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Modified", FormatDateTime(modified) ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@LastAccessed", FormatDateTime(lastAccessed) ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Parser_notes", parserNotes ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@File", file ?? (object)DBNull.Value);
            command.ExecuteNonQuery();
        }

        public static void ProcessAndWriteSavedLogins(string connectionString, string browserType, string profilePath, RichTextBox? logConsole = null)
        {
            if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(profilePath) || !Directory.Exists(profilePath))
                return;

            if (!TryRegisterProcessedSource("saved-logins", profilePath, logConsole))
                return;

            try
            {
                bool isFirefoxLike = LooksLikeFirefoxProfile(profilePath) || Helpers.IsFirefoxLikeBrowser(browserType);
                string resolvedBrowserType = Helpers.ResolveDisplayBrowserName(browserType, profilePath, isFirefoxLike ? "Firefox-like" : "Chromium-like");

                if (isFirefoxLike)
                    ProcessFirefoxSavedLogins(connectionString, resolvedBrowserType, profilePath, logConsole);
                else
                    ProcessChromiumSavedLogins(connectionString, resolvedBrowserType, profilePath, logConsole);
            }
            catch (Exception ex)
            {
                if (logConsole != null)
                    LogToConsole(logConsole, $"Saved logins parser error: {ex.Message}", Color.Red);
            }
        }

        private static void ProcessChromiumSavedLogins(string connectionString, string browserType, string profilePath, RichTextBox? logConsole)
        {
            string loginDbPath = Path.Combine(profilePath, "Login Data");
            if (!File.Exists(loginDbPath))
                return;

            using SQLiteConnection sourceConnection = new SQLiteConnection($"Data Source={loginDbPath};Version=3;");
            sourceConnection.Open();

            HashSet<string> columns = GetSqliteColumns(sourceConnection, "logins");
            if (!columns.Contains("origin_url") && !columns.Contains("signon_realm"))
                return;

            string query = $@"
                SELECT
                    {SelectColumn(columns, "origin_url", "NULL")},
                    {SelectColumn(columns, "action_url", "NULL")},
                    {SelectColumn(columns, "signon_realm", "NULL")},
                    {SelectColumn(columns, "username_value", "NULL")},
                    {SelectColumn(columns, "username_element", "NULL")},
                    {SelectColumn(columns, "password_element", "NULL")},
                    {SelectColumn(columns, "scheme", "NULL")},
                    {SelectColumn(columns, "times_used", "0")},
                    {SelectColumn(columns, "date_created", "0")},
                    {SelectColumn(columns, "date_last_used", "0")},
                    {SelectColumn(columns, "date_password_modified", "0")},
                    {SelectColumn(columns, "blacklisted_by_user", "0")},
                    {SelectColumn(columns, "federation_url", "NULL")},
                    {SelectColumn(columns, "guid", "NULL")},
                    {SelectColumn(columns, "password_value", "NULL")}
                FROM logins";

            int inserted = 0;
            using SQLiteCommand command = new SQLiteCommand(query, sourceConnection);
            using SQLiteDataReader reader = command.ExecuteReader();
            using SQLiteConnection targetConnection = new SQLiteConnection(connectionString);
            targetConnection.Open();
            using SQLiteTransaction transaction = targetConnection.BeginTransaction();

            while (reader.Read())
            {
                string? federationUrl = GetNullableString(reader, "federation_url");
                byte[]? encryptedPassword = GetNullableBytes(reader, "password_value");
                int passwordPresent = encryptedPassword != null && encryptedPassword.Length > 0 ? 1 : 0;
                InsertSavedLoginRow(targetConnection, transaction,
                    browserType,
                    passwordPresent == 1 ? "Saved credential present" : "Saved login metadata",
                    GetNullableString(reader, "origin_url"),
                    GetNullableString(reader, "action_url"),
                    GetNullableString(reader, "signon_realm"),
                    GetNullableString(reader, "username_value"),
                    GetNullableString(reader, "username_element"),
                    GetNullableString(reader, "password_element"),
                    GetNullableString(reader, "scheme"),
                    GetNullableInt32(reader, "times_used"),
                    ChromeTimestampToDateTime(GetNullableInt64(reader, "date_created")),
                    ChromeTimestampToDateTime(GetNullableInt64(reader, "date_last_used")),
                    ChromeTimestampToDateTime(GetNullableInt64(reader, "date_password_modified")),
                    GetNullableInt32(reader, "blacklisted_by_user"),
                    string.IsNullOrWhiteSpace(federationUrl) ? 0 : 1,
                    passwordPresent,
                    ComputeSha256Hex(encryptedPassword),
                    encryptedPassword?.Length ?? 0,
                    "Not attempted",
                    passwordPresent == 1 ? "Saved encrypted password blob present" : "No password blob present",
                    "Chromium Login Data",
                    GetNullableString(reader, "guid"),
                    loginDbPath);
                inserted++;
            }

            transaction.Commit();
            if (inserted > 0 && logConsole != null)
                LogToConsole(logConsole, $"Processed saved logins for {browserType}: {loginDbPath} ({inserted} entries)", Color.Green);

            if (inserted > 0)
            {
                BrowserReviewerLogger.LogParserRun(
                    "Saved Logins",
                    browserType,
                    "saved-logins-v1.0",
                    "High",
                    "Chromium Login Data logins table",
                    inserted,
                    loginDbPath,
                    "Extracts login metadata and hashes encrypted password blobs for traceability. Passwords are not decrypted or stored.");
            }
        }

        private static void ProcessFirefoxSavedLogins(string connectionString, string browserType, string profilePath, RichTextBox? logConsole)
        {
            string loginsPath = Path.Combine(profilePath, "logins.json");
            if (!File.Exists(loginsPath))
            {
                string key4Path = Path.Combine(profilePath, "key4.db");
                string key3Path = Path.Combine(profilePath, "key3.db");
                if (File.Exists(key4Path) || File.Exists(key3Path))
                {
                    string keyFile = File.Exists(key4Path) ? key4Path : key3Path;
                    if (logConsole != null)
                        LogToConsole(logConsole, $"Firefox saved logins not processed: logins.json not found in {profilePath}. Key database exists: {keyFile}", Color.DarkOrange);

                    BrowserReviewerLogger.LogParserRun(
                        "Saved Logins",
                        browserType,
                        "saved-logins-v1.0",
                        "Low",
                        "Firefox profile",
                        0,
                        profilePath,
                        $"Firefox key database exists ({Path.GetFileName(keyFile)}), but logins.json was not present in the scanned profile.");
                }
                return;
            }

            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(loginsPath));
            if (!document.RootElement.TryGetProperty("logins", out JsonElement logins) || logins.ValueKind != JsonValueKind.Array)
                return;

            int inserted = 0;
            using SQLiteConnection targetConnection = new SQLiteConnection(connectionString);
            targetConnection.Open();
            using SQLiteTransaction transaction = targetConnection.BeginTransaction();

            foreach (JsonElement login in logins.EnumerateArray())
            {
                string? encryptedPassword = GetJsonString(login, "encryptedPassword");
                int passwordPresent = string.IsNullOrWhiteSpace(encryptedPassword) ? 0 : 1;
                byte[]? encryptedPasswordBytes = !string.IsNullOrWhiteSpace(encryptedPassword) ? Encoding.UTF8.GetBytes(encryptedPassword) : null;
                InsertSavedLoginRow(targetConnection, transaction,
                    browserType,
                    passwordPresent == 1 ? "Saved credential present" : "Saved login metadata",
                    GetJsonString(login, "hostname"),
                    GetJsonString(login, "formSubmitURL"),
                    GetJsonString(login, "httpRealm"),
                    null,
                    GetJsonString(login, "usernameField"),
                    GetJsonString(login, "passwordField"),
                    null,
                    GetJsonInt32(login, "timesUsed"),
                    UnixMillisecondsToDateTime(GetJsonInt64(login, "timeCreated")),
                    UnixMillisecondsToDateTime(GetJsonInt64(login, "timeLastUsed")),
                    UnixMillisecondsToDateTime(GetJsonInt64(login, "timePasswordChanged")),
                    0,
                    0,
                    passwordPresent,
                    ComputeSha256Hex(encryptedPasswordBytes),
                    encryptedPasswordBytes?.Length ?? 0,
                    "Not attempted",
                    passwordPresent == 1 ? "Saved encrypted password value present" : "No encrypted password value present",
                    "Firefox logins.json",
                    GetJsonString(login, "guid"),
                    loginsPath);
                inserted++;
            }

            transaction.Commit();
            if (inserted > 0 && logConsole != null)
                LogToConsole(logConsole, $"Processed saved logins for Firefox: {loginsPath} ({inserted} entries)", Color.Green);

            if (inserted > 0)
            {
                BrowserReviewerLogger.LogParserRun(
                    "Saved Logins",
                    browserType,
                    "saved-logins-v1.0",
                    "High",
                    "Firefox logins.json logins array",
                    inserted,
                    loginsPath,
                    "Extracts login metadata and hashes encrypted password values for traceability. Encrypted usernames/passwords are not decrypted or stored.");
            }
        }

        private static void InsertSavedLoginRow(
            SQLiteConnection connection,
            SQLiteTransaction transaction,
            string browserType,
            string potentialActivity,
            string? url,
            string? actionUrl,
            string? signonRealm,
            string? username,
            string? usernameField,
            string? passwordField,
            string? scheme,
            int timesUsed,
            DateTime? created,
            DateTime? lastUsed,
            DateTime? passwordChanged,
            int isBlacklisted,
            int isFederated,
            int passwordPresent,
            string? encryptedPasswordSha256,
            int encryptedPasswordSize,
            string decryptionStatus,
            string? credentialArtifactValue,
            string store,
            string? loginGuid,
            string file)
        {
            using SQLiteCommand command = new SQLiteCommand(@"
                INSERT INTO saved_logins_data
                (Artifact_type, Potential_activity, Browser, Url, Action_url, Signon_realm, Username, Username_field,
                 Password_field, Scheme, Times_used, Created, Last_used, Password_changed, Is_blacklisted, Is_federated,
                 Password_present, Encrypted_password_sha256, Encrypted_password_size, Decryption_status, Credential_artifact_value,
                 Store, Login_guid, File)
                VALUES
                (@Artifact_type, @Potential_activity, @Browser, @Url, @Action_url, @Signon_realm, @Username, @Username_field,
                 @Password_field, @Scheme, @Times_used, @Created, @Last_used, @Password_changed, @Is_blacklisted, @Is_federated,
                 @Password_present, @Encrypted_password_sha256, @Encrypted_password_size, @Decryption_status, @Credential_artifact_value,
                 @Store, @Login_guid, @File);", connection, transaction);

            command.Parameters.AddWithValue("@Artifact_type", BuildArtifactType(browserType, "saved logins"));
            command.Parameters.AddWithValue("@Potential_activity", potentialActivity);
            command.Parameters.AddWithValue("@Browser", browserType);
            command.Parameters.AddWithValue("@Url", url ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Action_url", actionUrl ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Signon_realm", signonRealm ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Username", username ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Username_field", usernameField ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Password_field", passwordField ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Scheme", scheme ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Times_used", timesUsed);
            command.Parameters.AddWithValue("@Created", FormatDateTime(created) ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Last_used", FormatDateTime(lastUsed) ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Password_changed", FormatDateTime(passwordChanged) ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Is_blacklisted", isBlacklisted);
            command.Parameters.AddWithValue("@Is_federated", isFederated);
            command.Parameters.AddWithValue("@Password_present", passwordPresent);
            command.Parameters.AddWithValue("@Encrypted_password_sha256", encryptedPasswordSha256 ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Encrypted_password_size", encryptedPasswordSize);
            command.Parameters.AddWithValue("@Decryption_status", decryptionStatus ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Credential_artifact_value", credentialArtifactValue ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Store", store);
            command.Parameters.AddWithValue("@Login_guid", loginGuid ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@File", file);
            command.ExecuteNonQuery();
        }

        private static string? GetJsonString(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out JsonElement value) && value.ValueKind != JsonValueKind.Null
                ? value.ToString()
                : null;
        }

        private static byte[]? GetNullableBytes(SQLiteDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal))
                return null;

            if (reader.GetValue(ordinal) is byte[] bytes)
                return bytes;

            string? value = reader.GetValue(ordinal)?.ToString();
            return string.IsNullOrEmpty(value) ? null : Encoding.UTF8.GetBytes(value);
        }

        private static string ComputeSha256Hex(byte[]? bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return string.Empty;

            return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        }

        private static long GetJsonInt64(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement value))
                return 0;

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out long number))
                return number;

            return long.TryParse(value.ToString(), out number) ? number : 0;
        }

        private static int GetJsonInt32(JsonElement element, string propertyName)
        {
            long value = GetJsonInt64(element, propertyName);
            return value > int.MaxValue ? int.MaxValue : (int)value;
        }

        public static void ProcessAndWriteExtensions(string connectionString, string browserType, string profilePath, RichTextBox? logConsole = null)
        {
            if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(profilePath) || !Directory.Exists(profilePath))
                return;

            if (!TryRegisterProcessedSource("extensions", profilePath, logConsole))
                return;

            try
            {
                bool isFirefoxLike = LooksLikeFirefoxProfile(profilePath)
                    || Helpers.IsFirefoxLikeBrowser(browserType)
                    || Helpers.IsFirefoxLikeBrowser(Helpers.BrowserFamily);
                string resolvedBrowserType = Helpers.ResolveDisplayBrowserName(browserType, profilePath, isFirefoxLike ? "Firefox-like" : "Chromium-like");

                if (isFirefoxLike)
                    ProcessFirefoxExtensions(connectionString, resolvedBrowserType, profilePath, logConsole);
                else
                    ProcessChromiumExtensions(connectionString, resolvedBrowserType, profilePath, logConsole);
            }
            catch (Exception ex)
            {
                if (logConsole != null)
                    LogToConsole(logConsole, $"Extension parser error: {ex.Message}", Color.Red);
            }
        }

        private static void ProcessChromiumExtensions(string connectionString, string browserType, string profilePath, RichTextBox? logConsole)
        {
            string extensionsDir = Path.Combine(profilePath, "Extensions");
            Dictionary<string, JsonElement> settings = ReadChromiumExtensionSettings(profilePath);
            HashSet<string> processedExtensionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int inserted = 0;

            using SQLiteConnection targetConnection = new SQLiteConnection(connectionString);
            targetConnection.Open();
            using SQLiteTransaction transaction = targetConnection.BeginTransaction();

            if (Directory.Exists(extensionsDir))
            {
                foreach (string manifestPath in Directory.EnumerateFiles(extensionsDir, "manifest.json", SearchOption.AllDirectories))
                {
                    try
                    {
                        string versionDir = Path.GetDirectoryName(manifestPath) ?? string.Empty;
                        string extensionId = Directory.GetParent(versionDir)?.Name ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(extensionId))
                            continue;

                        DeleteExtensionRowsForSource(targetConnection, transaction, manifestPath);

                        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
                        JsonElement root = manifest.RootElement;
                        settings.TryGetValue(extensionId, out JsonElement extensionSettings);

                        InsertChromiumExtensionRow(targetConnection, transaction, browserType, profilePath, extensionId, root, extensionSettings, versionDir, manifestPath, manifestPath);
                        processedExtensionIds.Add(extensionId);
                        inserted++;
                    }
                    catch (Exception ex)
                    {
                        if (logConsole != null)
                            LogToConsole(logConsole, $"Extension manifest parser error: {manifestPath} - {ex.Message}", Color.Red);
                    }
                }
            }

            foreach (KeyValuePair<string, JsonElement> setting in settings)
            {
                try
                {
                    if (processedExtensionIds.Contains(setting.Key) ||
                        setting.Value.ValueKind != JsonValueKind.Object ||
                        !setting.Value.TryGetProperty("manifest", out JsonElement manifest) ||
                        manifest.ValueKind != JsonValueKind.Object)
                        continue;

                    string settingsFile = GetChromiumExtensionSettingsSourceFile(profilePath);
                    string extensionPath = GetString(setting.Value, "path");
                    if (!Path.IsPathRooted(extensionPath))
                        extensionPath = Path.Combine(profilePath, "Extensions", extensionPath);

                    DeleteExtensionRowsForSource(targetConnection, transaction, $"{settingsFile}:{setting.Key}");
                    InsertChromiumExtensionRow(targetConnection, transaction, browserType, profilePath, setting.Key, manifest, setting.Value, extensionPath, $"{settingsFile}:{setting.Key}", settingsFile);
                    processedExtensionIds.Add(setting.Key);
                    inserted++;
                }
                catch (Exception ex)
                {
                    if (logConsole != null)
                        LogToConsole(logConsole, $"Extension settings parser error: {setting.Key} - {ex.Message}", Color.Red);
                }
            }

            transaction.Commit();

            if (inserted > 0 && logConsole != null)
                LogToConsole(logConsole, $"Processed extensions for {browserType}: {profilePath} ({inserted} entries)", Color.Green);
        }

        private static void InsertChromiumExtensionRow(SQLiteConnection connection, SQLiteTransaction transaction, string browserType, string profilePath, string extensionId, JsonElement manifest, JsonElement extensionSettings, string extensionPath, string sourceFile, string file)
        {
            int? enabled = null;
            DateTime? installTime = null;
            DateTime? updateTime = null;
            string updateUrl = GetString(manifest, "update_url");

            if (extensionSettings.ValueKind == JsonValueKind.Object)
            {
                enabled = GetInt(extensionSettings, "state", 1) == 1 ? 1 : 0;
                long? installRaw = GetLong(extensionSettings, "install_time");
                long? updateRaw = GetLong(extensionSettings, "last_update_time");
                installTime = installRaw.HasValue ? ChromeTimestampToDateTime(installRaw.Value) : null;
                updateTime = updateRaw.HasValue ? ChromeTimestampToDateTime(updateRaw.Value) : null;
                if (string.IsNullOrWhiteSpace(updateUrl))
                    updateUrl = GetString(extensionSettings, "update_url");
            }

            string name = ResolveChromiumManifestText(manifest, "name", extensionPath);
            string description = ResolveChromiumManifestText(manifest, "description", extensionPath);
            string permissions = JoinJsonArray(manifest, "permissions");
            string hostPermissions = JoinJsonArray(manifest, "host_permissions");

            if (extensionSettings.ValueKind == JsonValueKind.Object &&
                extensionSettings.TryGetProperty("active_permissions", out JsonElement activePermissions) &&
                activePermissions.ValueKind == JsonValueKind.Object)
            {
                string activeApi = JoinJsonArray(activePermissions, "api");
                string explicitHost = JoinJsonArray(activePermissions, "explicit_host");
                string scriptableHost = JoinJsonArray(activePermissions, "scriptable_host");
                permissions = string.Join(", ", new[] { permissions, activeApi }.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct());
                hostPermissions = string.Join(", ", new[] { hostPermissions, explicitHost, scriptableHost }.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct());
            }

            if (string.IsNullOrWhiteSpace(hostPermissions))
                hostPermissions = ExtractHostPermissionsFromPermissions(permissions);

            InsertExtensionRow(connection, transaction, new ExtensionRow
            {
                Browser = browserType,
                PotentialActivity = GetExtensionPotentialActivity(name, permissions, hostPermissions, enabled),
                ExtensionId = extensionId,
                Name = name,
                Version = GetString(manifest, "version"),
                Description = description,
                Author = GetString(manifest, "author"),
                HomepageUrl = GetString(manifest, "homepage_url"),
                UpdateUrl = updateUrl,
                InstallTime = installTime,
                LastUpdateTime = updateTime,
                Enabled = enabled,
                Permissions = permissions,
                HostPermissions = hostPermissions,
                ManifestVersion = GetInt(manifest, "manifest_version", 0),
                ExtensionPath = extensionPath,
                SourceFile = sourceFile,
                File = file
            });
        }

        private static string GetChromiumExtensionSettingsSourceFile(string profilePath)
        {
            string securePreferences = Path.Combine(profilePath, "Secure Preferences");
            if (File.Exists(securePreferences))
                return securePreferences;

            return Path.Combine(profilePath, "Preferences");
        }

        private static Dictionary<string, JsonElement> ReadChromiumExtensionSettings(string profilePath)
        {
            Dictionary<string, JsonElement> result = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            foreach (string fileName in new[] { "Preferences", "Secure Preferences" })
            {
                string path = Path.Combine(profilePath, fileName);
                if (!File.Exists(path))
                    continue;

                try
                {
                    using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
                    if (!document.RootElement.TryGetProperty("extensions", out JsonElement extensions) ||
                        !extensions.TryGetProperty("settings", out JsonElement settings) ||
                        settings.ValueKind != JsonValueKind.Object)
                        continue;

                    foreach (JsonProperty property in settings.EnumerateObject())
                        result[property.Name] = property.Value.Clone();
                }
                catch
                {
                }
            }

            return result;
        }

        private static void ProcessFirefoxExtensions(string connectionString, string browserType, string profilePath, RichTextBox? logConsole)
        {
            string extensionsJson = Path.Combine(profilePath, "extensions.json");
            if (!File.Exists(extensionsJson))
            {
                if (logConsole != null)
                    LogToConsole(logConsole, $"Firefox extensions not processed: extensions.json not found in {profilePath}", Color.DarkOrange);
                return;
            }

            int inserted = 0;

            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(extensionsJson));
            if (!document.RootElement.TryGetProperty("addons", out JsonElement addons) || addons.ValueKind != JsonValueKind.Array)
                return;

            using SQLiteConnection targetConnection = new SQLiteConnection(connectionString);
            targetConnection.Open();
            using SQLiteTransaction transaction = targetConnection.BeginTransaction();
            DeleteExtensionRowsForSource(targetConnection, transaction, extensionsJson);

            foreach (JsonElement addon in addons.EnumerateArray())
            {
                string type = GetString(addon, "type");
                if (!string.IsNullOrWhiteSpace(type) && !type.Contains("extension", StringComparison.OrdinalIgnoreCase))
                    continue;

                string extensionId = GetString(addon, "id");
                string sourceFile = GetString(addon, "path");
                if (string.IsNullOrWhiteSpace(sourceFile))
                    sourceFile = extensionsJson;

                string permissions = JoinJsonArray(addon, "userPermissions");
                string hostPermissions = string.Empty;
                if (addon.TryGetProperty("userPermissions", out JsonElement userPermissions) && userPermissions.ValueKind == JsonValueKind.Object)
                {
                    permissions = JoinJsonArray(userPermissions, "permissions");
                    hostPermissions = JoinJsonArray(userPermissions, "origins");
                }

                long? installDate = GetLong(addon, "installDate");
                long? updateDate = GetLong(addon, "updateDate");

                InsertExtensionRow(targetConnection, transaction, new ExtensionRow
                {
                    Browser = browserType,
                    PotentialActivity = GetExtensionPotentialActivity(GetFirefoxLocalizedString(addon, "name"), permissions, hostPermissions, GetBool(addon, "active") ? 1 : 0),
                    ExtensionId = extensionId,
                    Name = GetFirefoxLocalizedString(addon, "name"),
                    Version = GetString(addon, "version"),
                    Description = GetFirefoxLocalizedString(addon, "description"),
                    Author = GetFirefoxLocalizedString(addon, "creator"),
                    HomepageUrl = GetFirefoxLocalizedString(addon, "homepageURL"),
                    UpdateUrl = GetString(addon, "updateURL"),
                    InstallTime = installDate.HasValue ? UnixMillisecondsToDateTime(installDate.Value) : null,
                    LastUpdateTime = updateDate.HasValue ? UnixMillisecondsToDateTime(updateDate.Value) : null,
                    Enabled = GetBool(addon, "active") ? 1 : 0,
                    Permissions = permissions,
                    HostPermissions = hostPermissions,
                    ManifestVersion = null,
                    ExtensionPath = sourceFile,
                    SourceFile = extensionsJson,
                    File = sourceFile
                });
                inserted++;
            }

            transaction.Commit();

            if (inserted > 0 && logConsole != null)
                LogToConsole(logConsole, $"Processed extensions for {browserType}: {profilePath} ({inserted} entries)", Color.Green);
        }

        private static string ResolveChromiumManifestText(JsonElement manifest, string propertyName, string extensionPath)
        {
            string value = GetString(manifest, propertyName);
            if (!value.StartsWith("__MSG_", StringComparison.OrdinalIgnoreCase) || !value.EndsWith("__", StringComparison.Ordinal))
                return value;

            string messageKey = value.Substring(6, value.Length - 8);
            string locale = GetString(manifest, "current_locale");
            if (string.IsNullOrWhiteSpace(locale))
                locale = GetString(manifest, "default_locale");

            foreach (string candidateLocale in GetChromiumLocaleCandidates(locale))
            {
                string messagesPath = Path.Combine(extensionPath, "_locales", candidateLocale, "messages.json");
                if (!File.Exists(messagesPath))
                    continue;

                try
                {
                    using JsonDocument messages = JsonDocument.Parse(File.ReadAllText(messagesPath));
                    if (messages.RootElement.TryGetProperty(messageKey, out JsonElement message) &&
                        message.TryGetProperty("message", out JsonElement text) &&
                        text.ValueKind == JsonValueKind.String)
                        return text.GetString() ?? value;
                }
                catch
                {
                }
            }

            return value;
        }

        private static IEnumerable<string> GetChromiumLocaleCandidates(string locale)
        {
            if (!string.IsNullOrWhiteSpace(locale))
            {
                yield return locale;
                string normalized = locale.Replace('-', '_');
                if (!normalized.Equals(locale, StringComparison.OrdinalIgnoreCase))
                    yield return normalized;

                int separator = normalized.IndexOf('_');
                if (separator > 0)
                    yield return normalized.Substring(0, separator);
            }

            yield return "en_US";
            yield return "en";
        }

        private static string GetFirefoxLocalizedString(JsonElement addon, string propertyName)
        {
            string direct = GetString(addon, propertyName);
            if (!string.IsNullOrWhiteSpace(direct))
                return direct;

            if (addon.TryGetProperty("defaultLocale", out JsonElement locale) && locale.ValueKind == JsonValueKind.Object)
                return GetString(locale, propertyName);

            return string.Empty;
        }

        private static string JoinJsonArray(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement array))
                return string.Empty;

            if (array.ValueKind == JsonValueKind.Array)
                return string.Join(", ", array.EnumerateArray().Select(item => item.ValueKind == JsonValueKind.String ? item.GetString() : item.ToString()).Where(v => !string.IsNullOrWhiteSpace(v)));

            if (array.ValueKind == JsonValueKind.Object)
                return string.Join(", ", array.EnumerateObject().Select(property => $"{property.Name}: {property.Value}"));

            return array.ValueKind == JsonValueKind.String ? array.GetString() ?? string.Empty : array.ToString();
        }

        private static string ExtractHostPermissionsFromPermissions(string permissions)
        {
            if (string.IsNullOrWhiteSpace(permissions))
                return string.Empty;

            return string.Join(", ", permissions.Split(',').Select(p => p.Trim()).Where(p => p.Contains("://") || p == "<all_urls>"));
        }

        private static string GetExtensionPotentialActivity(string name, string permissions, string hostPermissions, int? enabled)
        {
            string text = $"{name} {permissions} {hostPermissions}".ToLowerInvariant();
            if (text.Contains("wallet") || text.Contains("metamask") || text.Contains("crypto"))
                return "Extension wallet";
            if (text.Contains("password") || text.Contains("lastpass") || text.Contains("bitwarden") || text.Contains("1password"))
                return "Extension password manager";
            if (text.Contains("vpn") || text.Contains("proxy"))
                return "Extension VPN/proxy";
            if (text.Contains("download"))
                return "Extension downloader";
            if (text.Contains("debugger") || text.Contains("developer"))
                return "Extension developer tool";
            if (hostPermissions.Contains("<all_urls>", StringComparison.OrdinalIgnoreCase) || hostPermissions.Contains("*://", StringComparison.OrdinalIgnoreCase))
                return "Extension with broad host access";
            if (permissions.Contains("webRequest", StringComparison.OrdinalIgnoreCase))
                return "Extension with webRequest access";
            if (enabled == 0)
                return "Browser extension disabled";

            return "Browser extension installed";
        }

        private static void DeleteExtensionRowsForSource(string connectionString, string sourceFile)
        {
            using SQLiteConnection connection = new SQLiteConnection(connectionString);
            connection.Open();
            using SQLiteTransaction transaction = connection.BeginTransaction();
            DeleteExtensionRowsForSource(connection, transaction, sourceFile);
            transaction.Commit();
        }

        private static void DeleteExtensionRowsForSource(SQLiteConnection connection, SQLiteTransaction transaction, string sourceFile)
        {
            using SQLiteCommand command = new SQLiteCommand("DELETE FROM extension_data WHERE SourceFile = @SourceFile;", connection, transaction);
            command.Parameters.AddWithValue("@SourceFile", sourceFile);
            command.ExecuteNonQuery();
        }

        private static void InsertExtensionRow(string connectionString, ExtensionRow row)
        {
            using SQLiteConnection connection = new SQLiteConnection(connectionString);
            connection.Open();
            using SQLiteTransaction transaction = connection.BeginTransaction();
            InsertExtensionRow(connection, transaction, row);
            transaction.Commit();
        }

        private static void InsertExtensionRow(SQLiteConnection connection, SQLiteTransaction transaction, ExtensionRow row)
        {
            using SQLiteCommand command = new SQLiteCommand(@"
                INSERT INTO extension_data
                (Artifact_type, Potential_activity, Browser, ExtensionId, Name, Version, Description, Author, HomepageUrl, UpdateUrl,
                 InstallTime, LastUpdateTime, Enabled, Permissions, HostPermissions, ManifestVersion, ExtensionPath, SourceFile, File, Label, Comment)
                VALUES
                (@Artifact_type, @Potential_activity, @Browser, @ExtensionId, @Name, @Version, @Description, @Author, @HomepageUrl, @UpdateUrl,
                 @InstallTime, @LastUpdateTime, @Enabled, @Permissions, @HostPermissions, @ManifestVersion, @ExtensionPath, @SourceFile, @File, NULL, NULL);", connection, transaction);

            command.Parameters.AddWithValue("@Artifact_type", BuildArtifactType(row.Browser, "extensions"));
            command.Parameters.AddWithValue("@Browser", row.Browser);
            command.Parameters.AddWithValue("@Potential_activity", row.PotentialActivity);
            command.Parameters.AddWithValue("@ExtensionId", row.ExtensionId ?? string.Empty);
            command.Parameters.AddWithValue("@Name", string.IsNullOrWhiteSpace(row.Name) ? DBNull.Value : row.Name);
            command.Parameters.AddWithValue("@Version", string.IsNullOrWhiteSpace(row.Version) ? DBNull.Value : row.Version);
            command.Parameters.AddWithValue("@Description", string.IsNullOrWhiteSpace(row.Description) ? DBNull.Value : row.Description);
            command.Parameters.AddWithValue("@Author", string.IsNullOrWhiteSpace(row.Author) ? DBNull.Value : row.Author);
            command.Parameters.AddWithValue("@HomepageUrl", string.IsNullOrWhiteSpace(row.HomepageUrl) ? DBNull.Value : row.HomepageUrl);
            command.Parameters.AddWithValue("@UpdateUrl", string.IsNullOrWhiteSpace(row.UpdateUrl) ? DBNull.Value : row.UpdateUrl);
            command.Parameters.AddWithValue("@InstallTime", FormatDateTime(row.InstallTime) ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@LastUpdateTime", FormatDateTime(row.LastUpdateTime) ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Enabled", row.Enabled.HasValue ? row.Enabled.Value : DBNull.Value);
            command.Parameters.AddWithValue("@Permissions", string.IsNullOrWhiteSpace(row.Permissions) ? DBNull.Value : row.Permissions);
            command.Parameters.AddWithValue("@HostPermissions", string.IsNullOrWhiteSpace(row.HostPermissions) ? DBNull.Value : row.HostPermissions);
            command.Parameters.AddWithValue("@ManifestVersion", row.ManifestVersion.HasValue ? row.ManifestVersion.Value : DBNull.Value);
            command.Parameters.AddWithValue("@ExtensionPath", string.IsNullOrWhiteSpace(row.ExtensionPath) ? DBNull.Value : row.ExtensionPath);
            command.Parameters.AddWithValue("@SourceFile", row.SourceFile);
            command.Parameters.AddWithValue("@File", row.File);
            command.ExecuteNonQuery();
        }

        private static bool GetBool(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out JsonElement value) && value.ValueKind == JsonValueKind.True;
        }

        private sealed class ExtensionRow
        {
            public string Browser { get; set; } = string.Empty;
            public string PotentialActivity { get; set; } = string.Empty;
            public string ExtensionId { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Version { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Author { get; set; } = string.Empty;
            public string HomepageUrl { get; set; } = string.Empty;
            public string UpdateUrl { get; set; } = string.Empty;
            public DateTime? InstallTime { get; set; }
            public DateTime? LastUpdateTime { get; set; }
            public int? Enabled { get; set; }
            public string Permissions { get; set; } = string.Empty;
            public string HostPermissions { get; set; } = string.Empty;
            public int? ManifestVersion { get; set; }
            public string ExtensionPath { get; set; } = string.Empty;
            public string SourceFile { get; set; } = string.Empty;
            public string File { get; set; } = string.Empty;
        }

        public static void ProcessAndWriteSessions(string chromeViewerConnectionString, string browserType, string profileOrSessionPath, RichTextBox? logConsole = null)
        {
            if (string.IsNullOrWhiteSpace(chromeViewerConnectionString) || string.IsNullOrWhiteSpace(profileOrSessionPath))
                return;

            try
            {
                foreach (string sessionFile in GetSessionFiles(profileOrSessionPath, browserType))
                {
                    if (!TryRegisterProcessedSource("session-file", sessionFile, logConsole))
                        continue;

                    if (IsFirefoxSessionFile(sessionFile))
                        ProcessFirefoxSessionFile(chromeViewerConnectionString, browserType, sessionFile, logConsole);
                    else if (IsChromiumSessionFile(sessionFile))
                        ProcessChromiumSessionFile(chromeViewerConnectionString, browserType, sessionFile, logConsole);
                }
            }
            catch (Exception ex)
            {
                if (logConsole != null)
                    LogToConsole(logConsole, $"Session parser error: {ex.Message}", Color.Red);
            }
        }

        private static IEnumerable<string> GetSessionFiles(string profileOrSessionPath, string browserType)
        {
            if (File.Exists(profileOrSessionPath))
            {
                if (IsSessionArtifactFile(profileOrSessionPath))
                    yield return profileOrSessionPath;
                yield break;
            }

            if (!Directory.Exists(profileOrSessionPath))
                yield break;

            foreach (string file in Directory.EnumerateFiles(profileOrSessionPath, "*", SearchOption.TopDirectoryOnly))
            {
                if (IsSessionArtifactFile(file))
                    yield return file;
            }

            string sessionsDir = Path.Combine(profileOrSessionPath, "Sessions");
            if (Directory.Exists(sessionsDir))
            {
                foreach (string file in Directory.EnumerateFiles(sessionsDir, "*", SearchOption.TopDirectoryOnly))
                {
                    if (IsSessionArtifactFile(file))
                        yield return file;
                }
            }

            string sessionstoreBackups = Path.Combine(profileOrSessionPath, "sessionstore-backups");
            if (Directory.Exists(sessionstoreBackups))
            {
                foreach (string file in Directory.EnumerateFiles(sessionstoreBackups, "*", SearchOption.TopDirectoryOnly))
                {
                    if (IsSessionArtifactFile(file))
                        yield return file;
                }
            }
        }

        private static bool IsSessionArtifactFile(string filePath)
        {
            return IsChromiumSessionFile(filePath) || IsFirefoxSessionFile(filePath);
        }

        private static bool IsChromiumSessionFile(string filePath)
        {
            string name = Path.GetFileName(filePath);
            return name.Equals("Current Session", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("Last Session", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("Current Tabs", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("Last Tabs", StringComparison.OrdinalIgnoreCase) ||
                   name.StartsWith("Session_", StringComparison.OrdinalIgnoreCase) ||
                   name.StartsWith("Tabs_", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsFirefoxSessionFile(string filePath)
        {
            string name = Path.GetFileName(filePath);
            return name.Equals("sessionstore.jsonlz4", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("recovery.jsonlz4", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("recovery.baklz4", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("previous.jsonlz4", StringComparison.OrdinalIgnoreCase) ||
                   (name.StartsWith("upgrade", StringComparison.OrdinalIgnoreCase) && name.Contains(".jsonlz4", StringComparison.OrdinalIgnoreCase));
        }

        private static void ProcessChromiumSessionFile(string connectionString, string browserType, string sessionFile, RichTextBox? logConsole)
        {
            byte[] data = File.ReadAllBytes(sessionFile);
            if (data.Length < 8 || BitConverter.ToUInt32(data, 0) != 0x53534E53)
                return;

            int position = 8;
            int inserted = 0;

            using SQLiteConnection targetConnection = new SQLiteConnection(connectionString);
            targetConnection.Open();
            using SQLiteTransaction transaction = targetConnection.BeginTransaction();
            DeleteSessionRowsForFile(targetConnection, transaction, sessionFile);

            while (position + 3 <= data.Length)
            {
                int commandSize = BitConverter.ToUInt16(data, position);
                position += 2;
                if (commandSize <= 1 || position + commandSize > data.Length + 1)
                    break;

                byte commandId = data[position++];
                int contentLength = commandSize - 1;
                if (position + contentLength > data.Length)
                    break;

                byte[] content = new byte[contentLength];
                Buffer.BlockCopy(data, position, content, 0, contentLength);
                position += contentLength;

                if (commandId != 6)
                    continue;

                ChromiumSessionEntry? entry = TryParseChromiumNavigation(content);
                if (entry == null || string.IsNullOrWhiteSpace(entry.Url))
                    continue;

                InsertSessionRow(targetConnection, transaction, new SessionRow
                {
                    Browser = browserType,
                    PotentialActivity = GetSessionPotentialActivity(sessionFile, "Chromium"),
                    WindowIndex = null,
                    TabIndex = entry.TabId,
                    EntryIndex = entry.NavigationIndex,
                    Selected = null,
                    Url = entry.Url,
                    Title = entry.Title,
                    OriginalUrl = entry.OriginalUrl,
                    Referrer = entry.Referrer,
                    LastAccessed = entry.Timestamp,
                    Created = null,
                    SessionFile = sessionFile,
                    SourceType = "Chromium SNSS",
                    File = sessionFile
                });
                inserted++;
            }

            transaction.Commit();

            if (inserted > 0 && logConsole != null)
                LogToConsole(logConsole, $"Processed session file: {sessionFile} ({inserted} entries)", Color.Green);
        }

        private static ChromiumSessionEntry? TryParseChromiumNavigation(byte[] content)
        {
            ChromiumSessionEntry? pickleEntry = TryParseChromiumNavigationPickle(content);
            if (pickleEntry != null)
                return pickleEntry;

            try
            {
                if (content.Length < 16)
                    return null;

                int offset = 4;
                int tabId = (int)BitConverter.ToUInt32(content, offset);
                offset += 4;
                int navigationIndex = (int)BitConverter.ToUInt32(content, offset);
                offset += 4;
                string? url = ReadChromiumUtf8String(content, ref offset);
                string? title = ReadChromiumUtf16String(content, ref offset);
                string? originalUrl = TryReadNextUrl(content, offset);

                if (string.IsNullOrWhiteSpace(url))
                    url = ExtractFirstUrl(content);

                return string.IsNullOrWhiteSpace(url)
                    ? null
                    : new ChromiumSessionEntry(tabId, navigationIndex, url, title, originalUrl, null, null);
            }
            catch
            {
                string? url = ExtractFirstUrl(content);
                return string.IsNullOrWhiteSpace(url) ? null : new ChromiumSessionEntry(null, null, url, null, null, null, null);
            }
        }

        private static ChromiumSessionEntry? TryParseChromiumNavigationPickle(byte[] content)
        {
            ChromiumSessionEntry? entry = TryParseChromiumNavigationPickle(content, true);
            return entry ?? TryParseChromiumNavigationPickle(content, false);
        }

        private static ChromiumSessionEntry? TryParseChromiumNavigationPickle(byte[] content, bool hasPickleHeader)
        {
            ChromiumPickleReader? reader = ChromiumPickleReader.Create(content, hasPickleHeader);
            if (reader == null)
                return null;

            if (!reader.ReadInt32(out int tabId) ||
                !reader.ReadInt32(out int navigationIndex) ||
                !reader.ReadString(out string url) ||
                !reader.ReadString16(out string title) ||
                !reader.ReadString(out _) ||
                !reader.ReadInt32(out _))
            {
                return null;
            }

            string? referrer = null;
            string? originalUrl = null;
            DateTime? timestamp = null;

            if (reader.ReadInt32(out _))
            {
                reader.ReadString(out referrer);
                reader.ReadInt32(out _);
                reader.ReadString(out originalUrl);
                reader.ReadBool(out _);

                if (reader.ReadInt64(out long timestampInternalValue))
                    timestamp = ChromeTimestampToDateTime(timestampInternalValue);
            }

            if (string.IsNullOrWhiteSpace(url))
                url = ExtractFirstUrl(content) ?? string.Empty;

            if (string.IsNullOrWhiteSpace(originalUrl))
                originalUrl = TryReadNextUrl(content, reader.Position);

            return string.IsNullOrWhiteSpace(url)
                ? null
                : new ChromiumSessionEntry(tabId, navigationIndex, url, title, originalUrl, referrer, timestamp);
        }

        private static string? ReadChromiumUtf8String(byte[] data, ref int offset)
        {
            if (offset + 4 > data.Length)
                return null;

            int length = BitConverter.ToInt32(data, offset);
            offset += 4;
            if (length < 0 || length > data.Length - offset)
                return null;

            string value = Encoding.UTF8.GetString(data, offset, length);
            offset += length;
            return value;
        }

        private static string? ReadChromiumUtf16String(byte[] data, ref int offset)
        {
            if (offset + 4 > data.Length)
                return null;

            int length = BitConverter.ToInt32(data, offset);
            offset += 4;
            int byteLength = length * 2;
            if (length < 0 || byteLength < 0 || byteLength > data.Length - offset)
                return null;

            string value = Encoding.Unicode.GetString(data, offset, byteLength);
            offset += byteLength;
            return value;
        }

        private static string? TryReadNextUrl(byte[] data, int startOffset)
        {
            for (int i = Math.Max(0, startOffset); i + 8 < data.Length; i++)
            {
                int local = i;
                string? candidate = ReadChromiumUtf8String(data, ref local);
                if (IsLikelyUrl(candidate))
                    return candidate;
            }

            return null;
        }

        private static string? ExtractFirstUrl(byte[] data)
        {
            string utf8 = Encoding.UTF8.GetString(data);
            Match match = Regex.Match(utf8, @"https?://[^\s""'<>\\\u0000]+", RegexOptions.IgnoreCase);
            return match.Success ? match.Value : null;
        }

        private static bool IsLikelyUrl(string? value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    value.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                    value.StartsWith("file://", StringComparison.OrdinalIgnoreCase));
        }

        private static void ProcessFirefoxSessionFile(string connectionString, string browserType, string sessionFile, RichTextBox? logConsole)
        {
            string? json = TryReadFirefoxJsonlz4(sessionFile);
            if (string.IsNullOrWhiteSpace(json))
                return;

            int inserted = 0;

            using JsonDocument document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("windows", out JsonElement windows) || windows.ValueKind != JsonValueKind.Array)
                return;

            using SQLiteConnection targetConnection = new SQLiteConnection(connectionString);
            targetConnection.Open();
            using SQLiteTransaction transaction = targetConnection.BeginTransaction();
            DeleteSessionRowsForFile(targetConnection, transaction, sessionFile);

            int windowIndex = 0;
            foreach (JsonElement window in windows.EnumerateArray())
            {
                int selectedTab = GetInt(window, "selected", 0);
                if (!window.TryGetProperty("tabs", out JsonElement tabs) || tabs.ValueKind != JsonValueKind.Array)
                {
                    windowIndex++;
                    continue;
                }

                int tabIndex = 0;
                foreach (JsonElement tab in tabs.EnumerateArray())
                {
                    int selectedEntry = GetInt(tab, "index", 1) - 1;
                    long? lastAccessed = GetLong(tab, "lastAccessed");

                    if (tab.TryGetProperty("entries", out JsonElement entries) && entries.ValueKind == JsonValueKind.Array)
                    {
                        int entryIndex = 0;
                        foreach (JsonElement entry in entries.EnumerateArray())
                        {
                            string url = GetString(entry, "url");
                            if (string.IsNullOrWhiteSpace(url))
                            {
                                entryIndex++;
                                continue;
                            }

                            InsertSessionRow(targetConnection, transaction, new SessionRow
                            {
                                Browser = browserType,
                                PotentialActivity = GetSessionPotentialActivity(sessionFile, browserType),
                                WindowIndex = windowIndex,
                                TabIndex = tabIndex,
                                EntryIndex = entryIndex,
                                Selected = selectedTab == tabIndex + 1 && selectedEntry == entryIndex ? 1 : 0,
                                Url = url,
                                Title = GetString(entry, "title"),
                                OriginalUrl = GetString(entry, "originalURI"),
                                Referrer = GetString(entry, "referrer"),
                                LastAccessed = lastAccessed.HasValue ? UnixMillisecondsToDateTime(lastAccessed.Value) : null,
                                Created = null,
                                SessionFile = sessionFile,
                                SourceType = "Firefox JSONLZ4",
                                File = sessionFile
                            });
                            inserted++;
                            entryIndex++;
                        }
                    }

                    tabIndex++;
                }

                windowIndex++;
            }

            transaction.Commit();

            if (inserted > 0 && logConsole != null)
                LogToConsole(logConsole, $"Processed session file: {sessionFile} ({inserted} entries)", Color.Green);
        }

        private static string? TryReadFirefoxJsonlz4(string filePath)
        {
            byte[] data = File.ReadAllBytes(filePath);
            if (data.Length < 12)
                return null;

            byte[] compressed;
            if (Encoding.ASCII.GetString(data, 0, Math.Min(8, data.Length)).StartsWith("mozLz40", StringComparison.Ordinal))
            {
                if (data.Length < 13)
                    return null;

                compressed = data.Skip(12).ToArray();
            }
            else
            {
                compressed = data;
            }

            byte[] decompressed = DecompressLz4Block(compressed);
            string json = Encoding.UTF8.GetString(decompressed).TrimStart('\uFEFF', '\0', ' ', '\r', '\n', '\t');
            return json.StartsWith("{", StringComparison.Ordinal) || json.StartsWith("[", StringComparison.Ordinal)
                ? json
                : null;
        }

        private static byte[] DecompressLz4Block(byte[] input)
        {
            List<byte> output = new List<byte>(input.Length * 3);
            int inputPos = 0;

            while (inputPos < input.Length)
            {
                int token = input[inputPos++];
                int literalLength = token >> 4;
                if (literalLength == 15)
                    literalLength += ReadLz4Length(input, ref inputPos);

                if (inputPos + literalLength > input.Length)
                    literalLength = input.Length - inputPos;

                for (int i = 0; i < literalLength; i++)
                    output.Add(input[inputPos++]);

                if (inputPos >= input.Length)
                    break;

                if (inputPos + 2 > input.Length)
                    break;

                int matchOffset = input[inputPos] | (input[inputPos + 1] << 8);
                inputPos += 2;
                if (matchOffset <= 0 || matchOffset > output.Count)
                    break;

                int matchLength = token & 0x0F;
                if (matchLength == 15)
                    matchLength += ReadLz4Length(input, ref inputPos);
                matchLength += 4;

                int copyPos = output.Count - matchOffset;
                for (int i = 0; i < matchLength; i++)
                    output.Add(output[copyPos + i]);
            }

            return output.ToArray();
        }

        private static int ReadLz4Length(byte[] input, ref int inputPos)
        {
            int length = 0;
            while (inputPos < input.Length)
            {
                int value = input[inputPos++];
                length += value;
                if (value != 255)
                    break;
            }

            return length;
        }

        private static string GetSessionPotentialActivity(string sessionFile, string browserFamily)
        {
            string name = Path.GetFileName(sessionFile).ToLowerInvariant();
            if (name.Contains("recovery"))
                return "Recovered browser session";
            if (name.Contains("previous") || name.Contains("last"))
                return "Previous session tab";
            if (name.Contains("current") || name.Contains("sessionstore"))
                return "Open browser tab";
            if (name.Contains("tabs"))
                return "Restorable tab";

            return Helpers.IsFirefoxLikeBrowser(browserFamily) ? "Firefox session entry" : "Chromium session entry";
        }

        private static void DeleteSessionRowsForFile(string connectionString, string filePath)
        {
            using SQLiteConnection connection = new SQLiteConnection(connectionString);
            connection.Open();
            using SQLiteTransaction transaction = connection.BeginTransaction();
            DeleteSessionRowsForFile(connection, transaction, filePath);
            transaction.Commit();
        }

        private static void DeleteSessionRowsForFile(SQLiteConnection connection, SQLiteTransaction transaction, string filePath)
        {
            using SQLiteCommand command = new SQLiteCommand("DELETE FROM session_data WHERE File = @File;", connection, transaction);
            command.Parameters.AddWithValue("@File", filePath);
            command.ExecuteNonQuery();
        }

        private static void InsertSessionRow(string connectionString, SessionRow row)
        {
            using SQLiteConnection connection = new SQLiteConnection(connectionString);
            connection.Open();
            using SQLiteTransaction transaction = connection.BeginTransaction();
            InsertSessionRow(connection, transaction, row);
            transaction.Commit();
        }

        private static void InsertSessionRow(SQLiteConnection connection, SQLiteTransaction transaction, SessionRow row)
        {
            using SQLiteCommand command = new SQLiteCommand(@"
                INSERT INTO session_data
                (Artifact_type, Potential_activity, Browser, WindowIndex, TabIndex, EntryIndex, Selected, Url, Title, OriginalUrl, Referrer,
                 LastAccessed, Created, SessionFile, SourceType, File, Label, Comment)
                VALUES
                (@Artifact_type, @Potential_activity, @Browser, @WindowIndex, @TabIndex, @EntryIndex, @Selected, @Url, @Title, @OriginalUrl, @Referrer,
                 @LastAccessed, @Created, @SessionFile, @SourceType, @File, NULL, NULL);", connection, transaction);

            command.Parameters.AddWithValue("@Artifact_type", BuildArtifactType(row.Browser, "sessions"));
            command.Parameters.AddWithValue("@Browser", row.Browser);
            command.Parameters.AddWithValue("@Potential_activity", row.PotentialActivity);
            command.Parameters.AddWithValue("@WindowIndex", row.WindowIndex.HasValue ? row.WindowIndex.Value : DBNull.Value);
            command.Parameters.AddWithValue("@TabIndex", row.TabIndex.HasValue ? row.TabIndex.Value : DBNull.Value);
            command.Parameters.AddWithValue("@EntryIndex", row.EntryIndex.HasValue ? row.EntryIndex.Value : DBNull.Value);
            command.Parameters.AddWithValue("@Selected", row.Selected.HasValue ? row.Selected.Value : DBNull.Value);
            command.Parameters.AddWithValue("@Url", row.Url ?? string.Empty);
            command.Parameters.AddWithValue("@Title", string.IsNullOrWhiteSpace(row.Title) ? DBNull.Value : row.Title);
            command.Parameters.AddWithValue("@OriginalUrl", string.IsNullOrWhiteSpace(row.OriginalUrl) ? DBNull.Value : row.OriginalUrl);
            command.Parameters.AddWithValue("@Referrer", string.IsNullOrWhiteSpace(row.Referrer) ? DBNull.Value : row.Referrer);
            command.Parameters.AddWithValue("@LastAccessed", FormatDateTime(row.LastAccessed) ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Created", FormatDateTime(row.Created) ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@SessionFile", row.SessionFile);
            command.Parameters.AddWithValue("@SourceType", row.SourceType);
            command.Parameters.AddWithValue("@File", row.File);
            command.ExecuteNonQuery();
        }

        private static string GetString(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out JsonElement value) && value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? string.Empty
                : string.Empty;
        }

        private static int GetInt(JsonElement element, string propertyName, int defaultValue)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement value))
            {
                return defaultValue;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int result))
            {
                return result;
            }

            if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out result))
            {
                return result;
            }

            return defaultValue;
        }

        private static long? GetLong(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement value))
            {
                return null;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out long result))
            {
                return result;
            }

            if (value.ValueKind == JsonValueKind.String && long.TryParse(value.GetString(), out result))
            {
                return result;
            }

            return null;
        }

        private sealed record ChromiumSessionEntry(int? TabId, int? NavigationIndex, string Url, string? Title, string? OriginalUrl, string? Referrer, DateTime? Timestamp);

        private sealed class ChromiumPickleReader
        {
            private readonly byte[] data;
            private readonly int end;
            private int position;

            private ChromiumPickleReader(byte[] data, int position, int end)
            {
                this.data = data;
                this.position = position;
                this.end = end;
            }

            public int Position => position;

            public static ChromiumPickleReader? Create(byte[] data, bool hasPickleHeader)
            {
                if (hasPickleHeader)
                {
                    if (data.Length < 4)
                        return null;

                    int payloadSize = BitConverter.ToInt32(data, 0);
                    if (payloadSize < 0 || payloadSize > data.Length - 4)
                        return null;

                    return new ChromiumPickleReader(data, 4, 4 + payloadSize);
                }

                return new ChromiumPickleReader(data, 0, data.Length);
            }

            public bool ReadInt32(out int value)
            {
                value = 0;
                if (position + 4 > end)
                    return false;

                value = BitConverter.ToInt32(data, position);
                Advance(4);
                return true;
            }

            public bool ReadInt64(out long value)
            {
                value = 0;
                if (position + 8 > end)
                    return false;

                value = BitConverter.ToInt64(data, position);
                Advance(8);
                return true;
            }

            public bool ReadBool(out bool value)
            {
                value = false;
                if (!ReadInt32(out int raw))
                    return false;

                value = raw != 0;
                return true;
            }

            public bool ReadString(out string value)
            {
                value = string.Empty;
                if (!ReadInt32(out int length) || length < 0 || position + length > end)
                    return false;

                value = Encoding.UTF8.GetString(data, position, length);
                Advance(length);
                return true;
            }

            public bool ReadString16(out string value)
            {
                value = string.Empty;
                if (!ReadInt32(out int length) || length < 0)
                    return false;

                int byteLength;
                try
                {
                    checked
                    {
                        byteLength = length * 2;
                    }
                }
                catch
                {
                    return false;
                }

                if (position + byteLength > end)
                    return false;

                value = Encoding.Unicode.GetString(data, position, byteLength);
                Advance(byteLength);
                return true;
            }

            private void Advance(int length)
            {
                int aligned = length + ((4 - (length % 4)) % 4);
                position = Math.Min(end, position + aligned);
            }
        }

        private sealed class SessionRow
        {
            public string Browser { get; set; } = string.Empty;
            public string PotentialActivity { get; set; } = string.Empty;
            public int? WindowIndex { get; set; }
            public int? TabIndex { get; set; }
            public int? EntryIndex { get; set; }
            public int? Selected { get; set; }
            public string Url { get; set; } = string.Empty;
            public string? Title { get; set; }
            public string? OriginalUrl { get; set; }
            public string? Referrer { get; set; }
            public DateTime? LastAccessed { get; set; }
            public DateTime? Created { get; set; }
            public string SessionFile { get; set; } = string.Empty;
            public string SourceType { get; set; } = string.Empty;
            public string File { get; set; } = string.Empty;
        }














        public static void ProcessAndWriteBookmarksChrome(string chromeViewerConnectionString, string browserType, string filePath, RichTextBox? logConsole = null)
        {
            if (!TryRegisterProcessedSource("chromium-bookmarks", filePath, logConsole))
                return;

            if (File.Exists(filePath))
            {
                try
                {
                    byte[] fileBytes = File.ReadAllBytes(filePath);
                    string jsonContent = Encoding.UTF8.GetString(fileBytes);

                    jsonContent = jsonContent.TrimStart(new char[] { '\uFEFF' });

                    var bookmarksData = JsonConvert.DeserializeObject<JObject>(jsonContent);

                    if (bookmarksData != null && bookmarksData.ContainsKey("roots"))
                    {
                        var roots = bookmarksData["roots"] as JObject;
                        if (roots == null)
                            return;

                        using (SQLiteConnection connection = new SQLiteConnection(chromeViewerConnectionString))
                        {
                            connection.Open();
                            using (var transaction = connection.BeginTransaction())
                            {
                                foreach (var root in roots)
                                {
                                    var rootFolder = root.Value as JObject;
                                    ProcessFolderChrome(rootFolder, null, connection, transaction, browserType, filePath);
                                }
                                transaction.Commit();
                            }
                        }
                    }
                    else
                    {
                        if (logConsole != null)
                            logConsole.AppendText("The key 'roots' was not found in the JSON file.\n");
                        else
                            Console.WriteLine("The key 'roots' was not found in the JSON file.");
                    }
                }
                catch (JsonReaderException jex)
                {
                    if (logConsole != null)
                        logConsole.AppendText($"JSON error: {jex.Message}\n");
                    else
                        Console.WriteLine($"JSON error: {jex.Message}");
                }
                catch (Exception ex)
                {
                    if (logConsole != null)
                        logConsole.AppendText($"General error: {ex.Message}\n");
                    else
                        Console.WriteLine($"General error: {ex.Message}");
                }
            }
            else
            {
                if (logConsole != null)
                    logConsole.AppendText("Bookmarks file not found.\n");
                else
                    Console.WriteLine("Bookmarks file not found.");
            }
        }




        private static void ProcessFolderChrome(JObject? folder, string? parentName, SQLiteConnection connection, SQLiteTransaction transaction, string browserType, string filePath)
        {
            if (folder == null) return;

            string? folderName = folder["name"]?.ToString();
            string? dateAdded = folder["date_added"]?.ToString();
            string? dateLastUsed = folder["date_last_used"]?.ToString();
            string? dateModified = folder["date_modified"]?.ToString();
            string? guid = folder["guid"]?.ToString();
            string? folderId = folder["id"]?.ToString();
            string? type = folder["type"]?.ToString();

            DateTime? dateAddedConverted = ChromeDateToDateTime(dateAdded);
            DateTime? dateLastUsedConverted = ChromeDateToDateTime(dateLastUsed);
            DateTime? dateModifiedConverted = ChromeDateToDateTime(dateModified);

            if (type == "folder")
            {
                string insertFolderQuery = @"INSERT INTO bookmarks_Chrome 
                                     (Artifact_type, Potential_activity, Browser, Type, Title, DateAdded, DateLastUsed, LastModified, Parent_name, Guid, ChromeId, File)
                                     VALUES 
                                     (@Artifact_type, @Potential_activity, @Browser, @Type, @Title, @DateAdded, @DateLastUsed, @LastModified, @Parent_name, @Guid, @ChromeId, @File)";

                using (SQLiteCommand command = new SQLiteCommand(insertFolderQuery, connection, transaction))
                {
                    command.Parameters.AddWithValue("@Artifact_type", BuildArtifactType(browserType, "bookmarks"));
                    command.Parameters.AddWithValue("@Potential_activity", "Bookmark folder present");
                    command.Parameters.AddWithValue("@Browser", browserType);
                    command.Parameters.AddWithValue("@Type", "Folder");
                    command.Parameters.AddWithValue("@Title", folderName);
                    command.Parameters.AddWithValue("@DateAdded", FormatDateTime(dateAddedConverted) ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@DateLastUsed", FormatDateTime(dateLastUsedConverted) ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@LastModified", FormatDateTime(dateModifiedConverted) ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Parent_name", parentName ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Guid", guid);
                    command.Parameters.AddWithValue("@ChromeId", folderId);
                    command.Parameters.AddWithValue("@File", filePath);
                    command.ExecuteNonQuery();
                }

                var children = folder["children"] as JArray;
                if (children != null)
                {
                    foreach (var child in children)
                    {
                        var childObject = child as JObject;
                        if (childObject == null) continue;

                        ProcessFolderChrome(childObject, folderName, connection, transaction, browserType, filePath);
                    }
                }
            }
            else if (type == "url")
            {
                string? url = folder["url"]?.ToString();

                string insertBookmarkQuery = @"INSERT INTO bookmarks_Chrome 
                                       (Artifact_type, Potential_activity, Browser, Type, Title, URL, DateAdded, DateLastUsed, LastModified, Parent_name, Guid, ChromeId, File)
                                       VALUES 
                                       (@Artifact_type, @Potential_activity, @Browser, @Type, @Title, @URL, @DateAdded, @DateLastUsed, @LastModified, @Parent_name, @Guid, @ChromeId, @File)";

                using (SQLiteCommand command = new SQLiteCommand(insertBookmarkQuery, connection, transaction))
                {
                    command.Parameters.AddWithValue("@Artifact_type", BuildArtifactType(browserType, "bookmarks"));
                    command.Parameters.AddWithValue("@Potential_activity", "Saved bookmark");
                    command.Parameters.AddWithValue("@Browser", browserType);
                    command.Parameters.AddWithValue("@Type", "Bookmark");
                    command.Parameters.AddWithValue("@Title", folderName);
                    command.Parameters.AddWithValue("@URL", url);
                    command.Parameters.AddWithValue("@DateAdded", FormatDateTime(dateAddedConverted) ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@DateLastUsed", FormatDateTime(dateLastUsedConverted) ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@LastModified", FormatDateTime(dateModifiedConverted) ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Parent_name", parentName ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Guid", guid);
                    command.Parameters.AddWithValue("@ChromeId", folderId);
                    command.Parameters.AddWithValue("@File", filePath);
                    command.ExecuteNonQuery();
                }
            }
        }

        private static DateTime? ChromeDateToDateTime(string? chromeDate)
        {
            if (long.TryParse(chromeDate, out long microseconds))
            {
                return ChromeTimestampToDateTime(microseconds);
            }
            return null;
        }





        public static void ProcessAndWriteAutofillChrome(string chromeViewerConnectionString, string browserType, string filePath, RichTextBox? logConsole = null)
        {
            string fileName = Path.GetFileName(filePath);
            string? profileDirectory = Path.GetDirectoryName(filePath);
            if (string.IsNullOrWhiteSpace(profileDirectory))
                return;

            string resolvedBrowserType = Helpers.ResolveDisplayBrowserName(browserType, filePath, "Chromium-like");

            string autofillDbPath = fileName.Equals("Web Data", StringComparison.OrdinalIgnoreCase)
                ? filePath
                : Path.Combine(profileDirectory, "Web Data");
            if (!TryRegisterProcessedSource("chromium-autofill", autofillDbPath, logConsole))
                return;

            if (File.Exists(autofillDbPath))
            {
                using (SQLiteConnection chromeAutofillConnection = new SQLiteConnection($"Data Source={autofillDbPath};Version=3;"))
                {
                    chromeAutofillConnection.Open();
                    using (SQLiteConnection chromeViewerConnection = new SQLiteConnection(chromeViewerConnectionString))
                    {
                        chromeViewerConnection.Open();
                        using SQLiteTransaction transaction = chromeViewerConnection.BeginTransaction();
                        HashSet<string> seenAutofill = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        int inserted = 0;

                        try
                        {
                            HashSet<string> autofillColumns = GetSqliteColumns(chromeAutofillConnection, "autofill");
                            if (autofillColumns.Contains("name") && autofillColumns.Contains("value") && autofillColumns.Contains("count"))
                            {
                                string queryAutofill = $@"
                                    SELECT name, value, count,
                                           {SelectColumn(autofillColumns, "date_created", "NULL")},
                                           {SelectColumn(autofillColumns, "date_last_used", "NULL")}
                                    FROM autofill";

                                using (SQLiteCommand commandAutofill = new SQLiteCommand(queryAutofill, chromeAutofillConnection))
                                using (SQLiteDataReader readerAutofill = commandAutofill.ExecuteReader())
                                {
                                    while (readerAutofill.Read())
                                    {
                                        string fieldName = readerAutofill.GetString(0);
                                        string value = readerAutofill.GetString(1);
                                        int count = readerAutofill.GetInt32(2);
                                        long dateCreatedMicroseconds = GetNullableInt64(readerAutofill, "date_created");
                                        long dateLastUsedMicroseconds = GetNullableInt64(readerAutofill, "date_last_used");

                                        DateTime? firstUsed = ChromeTimestampToDateTime(dateCreatedMicroseconds);
                                        DateTime? lastUsed = ChromeTimestampToDateTime(dateLastUsedMicroseconds);

                                        string insertAutofill = @"INSERT INTO autofill_data (Artifact_type, Potential_activity, Browser, FieldName, Value, Count, TimesUsed, FirstUsed, LastUsed, File)
                                              VALUES (@Artifact_type, @Potential_activity, @Browser, @FieldName, @Value, @Count, @TimesUsed, @FirstUsed, @LastUsed, @File)";

                                        using (SQLiteCommand insertCommand = new SQLiteCommand(insertAutofill, chromeViewerConnection, transaction))
                                        {
                                            insertCommand.Parameters.AddWithValue("@Artifact_type", BuildArtifactType(resolvedBrowserType, "autofill"));
                                            insertCommand.Parameters.AddWithValue("@Potential_activity", "Autofill data saved");
                                            insertCommand.Parameters.AddWithValue("@Browser", resolvedBrowserType);
                                            insertCommand.Parameters.AddWithValue("@FieldName", fieldName);
                                            insertCommand.Parameters.AddWithValue("@Value", value);
                                            insertCommand.Parameters.AddWithValue("@Count", count);
                                            insertCommand.Parameters.AddWithValue("@TimesUsed", count);
                                            insertCommand.Parameters.AddWithValue("@FirstUsed", FormatDateTime(firstUsed) ?? (object)DBNull.Value);
                                            insertCommand.Parameters.AddWithValue("@LastUsed", FormatDateTime(lastUsed) ?? (object)DBNull.Value);
                                            insertCommand.Parameters.AddWithValue("@File", autofillDbPath);

                                            string dedupeKey = $"classic|{fieldName}|{value}";
                                            if (seenAutofill.Add(dedupeKey))
                                            {
                                                insertCommand.ExecuteNonQuery();
                                                inserted++;
                                            }
                                        }
                                    }
                                }
                            }

                            try
                            {
                                inserted += ProcessChromiumStructuredAutofill(chromeAutofillConnection, chromeViewerConnection, transaction, resolvedBrowserType, autofillDbPath, seenAutofill);
                            }
                            catch (Exception ex)
                            {
                                if (logConsole != null)
                                    LogToConsole(logConsole, $"Structured Chromium autofill parser warning for {resolvedBrowserType}: {ex.Message}", Color.DarkOrange);
                            }

                            try
                            {
                                inserted += ProcessEdgeStructuredAutofill(chromeAutofillConnection, chromeViewerConnection, transaction, resolvedBrowserType, autofillDbPath, seenAutofill);
                            }
                            catch (Exception ex)
                            {
                                if (logConsole != null)
                                    LogToConsole(logConsole, $"Structured Edge autofill parser warning for {resolvedBrowserType}: {ex.Message}", Color.DarkOrange);
                            }

                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }

                        if (inserted > 0)
                        {
                            if (logConsole != null)
                                LogToConsole(logConsole, $"Processed autofill for {resolvedBrowserType}: {autofillDbPath} ({inserted} entries)", Color.Green);

                            BrowserReviewerLogger.LogParserRun(
                                "Autofill",
                                resolvedBrowserType,
                                "autofill-v2.0",
                                "Medium",
                                "Chromium Web Data autofill tables",
                                inserted,
                                autofillDbPath,
                                "Extracts classic Chromium autofill plus structured Chromium and Edge-specific autofill tables when present.");
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("[Browser Reviewer - Error]: Chrome autocomplete data file not found.");
            }
        }


        private static DateTime? ChromeTimestampToDateTime(long value)
        {
            if (value <= 0) return null;

            try
            {
                DateTime epoch = new DateTime(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                DateTime converted = epoch.AddTicks(value * 10);
                return IsReasonableBrowserTimestamp(converted) ? converted : null;
            }
            catch
            {
                return null;
            }
        }


        private static DateTime? UnixSecondsToDateTime(long value)
        {
            if (value <= 0) return null;

            try
            {
                DateTime converted = DateTimeOffset.FromUnixTimeSeconds(value).UtcDateTime;
                return IsReasonableBrowserTimestamp(converted) ? converted : null;
            }
            catch
            {
                return null;
            }
        }


        private static DateTime? UnixMillisecondsToDateTime(long value)
        {
            if (value <= 0) return null;

            try
            {
                DateTime converted = DateTimeOffset.FromUnixTimeMilliseconds(value).UtcDateTime;
                return IsReasonableBrowserTimestamp(converted) ? converted : null;
            }
            catch
            {
                return null;
            }
        }


        private static DateTime? UnixMicrosecondsToDateTime(long value)
        {
            if (value <= 0) return null;

            try
            {
                DateTime converted = DateTimeOffset.FromUnixTimeMilliseconds(value / 1000).UtcDateTime;
                return IsReasonableBrowserTimestamp(converted) ? converted : null;
            }
            catch
            {
                return null;
            }
        }


        private static bool IsReasonableBrowserTimestamp(DateTime value)
        {
            return value.Year >= 1990 && value <= DateTime.UtcNow.AddYears(2);
        }


        private static string? FormatDateTime(DateTime? value)
        {
            return value.HasValue ? value.Value.ToString("yyyy-MM-dd HH:mm:ss.ffffff") : null;
        }

        private static HashSet<string> GetSqliteColumns(SQLiteConnection connection, string tableName)
        {
            HashSet<string> columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using (SQLiteCommand command = new SQLiteCommand($"PRAGMA table_info({tableName});", connection))
            using (SQLiteDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    string? columnName = reader["name"].ToString();
                    if (!string.IsNullOrWhiteSpace(columnName))
                        columns.Add(columnName);
                }
            }

            return columns;
        }


        private static string SelectColumn(HashSet<string> columns, string columnName, string fallback)
        {
            return columns.Contains(columnName) ? columnName : $"{fallback} AS {columnName}";
        }

        private static string SelectQualifiedColumn(HashSet<string> columns, string qualifier, string columnName, string fallback)
        {
            return columns.Contains(columnName) ? $"{qualifier}.{columnName}" : fallback;
        }

        private static string SelectFirstAvailableColumn(HashSet<string> columns, string[] candidates, string alias, string fallback = "NULL")
        {
            foreach (string candidate in candidates)
            {
                if (columns.Contains(candidate))
                    return $"{candidate} AS {alias}";
            }

            return $"{fallback} AS {alias}";
        }

        private static string SelectFirstAvailableQualifiedColumn(HashSet<string> columns, string qualifier, string[] candidates, string alias, string fallback = "NULL")
        {
            foreach (string candidate in candidates)
            {
                if (columns.Contains(candidate))
                    return $"{qualifier}.{candidate} AS {alias}";
            }

            return $"{fallback} AS {alias}";
        }


        private static string? GetNullableString(SQLiteDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetValue(ordinal)?.ToString();
        }


        private static long GetNullableInt64(SQLiteDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt64(reader.GetValue(ordinal));
        }


        private static int GetNullableInt32(SQLiteDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal));
        }


        public static void ProcessAndWriteCookiesChrome(string chromeViewerConnectionString, string browserType, string filePath, RichTextBox? logConsole = null)
        {
            string? profilePath = GetOwningBrowserProfileDirectory(filePath);
            if (string.IsNullOrWhiteSpace(profilePath))
                return;

            string fileName = Path.GetFileName(filePath);
            string cookiesDbPath = fileName.Equals("Cookies", StringComparison.OrdinalIgnoreCase)
                ? filePath
                : File.Exists(Path.Combine(profilePath, "Network", "Cookies"))
                    ? Path.Combine(profilePath, "Network", "Cookies")
                    : Path.Combine(profilePath, "Cookies");

            string resolvedBrowserType = Helpers.ResolveDisplayBrowserName(browserType, cookiesDbPath, "Chromium-like");

            if (!TryRegisterProcessedSource("chromium-cookies", cookiesDbPath, logConsole))
                return;

            if (!File.Exists(cookiesDbPath))
            {
                return;
            }

            using (SQLiteConnection sourceConnection = new SQLiteConnection($"Data Source={cookiesDbPath};Version=3;"))
            {
                sourceConnection.Open();
                HashSet<string> columns = GetSqliteColumns(sourceConnection, "cookies");

                if (!columns.Contains("host_key") || !columns.Contains("name"))
                {
                    return;
                }

                string queryCookies = $@"
                    SELECT
                        {SelectColumn(columns, "creation_utc", "0")},
                        host_key,
                        name,
                        {SelectColumn(columns, "value", "''")},
                        {SelectColumn(columns, "encrypted_value", "NULL")},
                        {SelectColumn(columns, "path", "''")},
                        {SelectColumn(columns, "expires_utc", "0")},
                        {SelectColumn(columns, "is_secure", "0")},
                        {SelectColumn(columns, "is_httponly", "0")},
                        {SelectColumn(columns, "last_access_utc", "0")},
                        {SelectColumn(columns, "is_persistent", "0")},
                        {SelectColumn(columns, "samesite", "NULL")},
                        {SelectColumn(columns, "source_scheme", "NULL")},
                        {SelectColumn(columns, "source_port", "NULL")}
                    FROM cookies";

                using (SQLiteCommand commandCookies = new SQLiteCommand(queryCookies, sourceConnection))
                using (SQLiteDataReader readerCookies = commandCookies.ExecuteReader())
                using (SQLiteConnection targetConnection = new SQLiteConnection(chromeViewerConnectionString))
                {
                    targetConnection.Open();

                    using (SQLiteTransaction transaction = targetConnection.BeginTransaction())
                    {
                        try
                        {
                            while (readerCookies.Read())
                            {
                                string? value = GetNullableString(readerCookies, "value");
                                byte[]? encryptedValue = readerCookies["encrypted_value"] as byte[];
                                bool isEncrypted = encryptedValue != null && encryptedValue.Length > 0;
                                object storedValue = string.IsNullOrEmpty(value) && isEncrypted
                                    ? "[encrypted]"
                                    : value ?? (object)DBNull.Value;

                                string insertCookie = @"INSERT INTO cookies_data
                                    (Artifact_type, Potential_activity, Browser, Host, Name, Value, Path, Created, Expires, LastAccessed,
                                     IsSecure, IsHttpOnly, IsPersistent, SameSite, SourceScheme, SourcePort,
                                     IsEncrypted, File)
                                    VALUES
                                    (@Artifact_type, @Potential_activity, @Browser, @Host, @Name, @Value, @Path, @Created, @Expires, @LastAccessed,
                                     @IsSecure, @IsHttpOnly, @IsPersistent, @SameSite, @SourceScheme, @SourcePort,
                                     @IsEncrypted, @File)";

                                using (SQLiteCommand insertCommand = new SQLiteCommand(insertCookie, targetConnection, transaction))
                                {
                                    insertCommand.Parameters.AddWithValue("@Artifact_type", BuildArtifactType(resolvedBrowserType, "cookies"));
                                    insertCommand.Parameters.AddWithValue("@Potential_activity", "Web cookie stored");
                                    insertCommand.Parameters.AddWithValue("@Browser", resolvedBrowserType);
                                    insertCommand.Parameters.AddWithValue("@Host", GetNullableString(readerCookies, "host_key") ?? (object)DBNull.Value);
                                    insertCommand.Parameters.AddWithValue("@Name", GetNullableString(readerCookies, "name") ?? (object)DBNull.Value);
                                    insertCommand.Parameters.AddWithValue("@Value", storedValue);
                                    insertCommand.Parameters.AddWithValue("@Path", GetNullableString(readerCookies, "path") ?? (object)DBNull.Value);
                                    insertCommand.Parameters.AddWithValue("@Created", FormatDateTime(ChromeTimestampToDateTime(GetNullableInt64(readerCookies, "creation_utc"))) ?? (object)DBNull.Value);
                                    insertCommand.Parameters.AddWithValue("@Expires", FormatDateTime(ChromeTimestampToDateTime(GetNullableInt64(readerCookies, "expires_utc"))) ?? (object)DBNull.Value);
                                    insertCommand.Parameters.AddWithValue("@LastAccessed", FormatDateTime(ChromeTimestampToDateTime(GetNullableInt64(readerCookies, "last_access_utc"))) ?? (object)DBNull.Value);
                                    insertCommand.Parameters.AddWithValue("@IsSecure", GetNullableInt32(readerCookies, "is_secure"));
                                    insertCommand.Parameters.AddWithValue("@IsHttpOnly", GetNullableInt32(readerCookies, "is_httponly"));
                                    insertCommand.Parameters.AddWithValue("@IsPersistent", GetNullableInt32(readerCookies, "is_persistent"));
                                    insertCommand.Parameters.AddWithValue("@SameSite", GetNullableString(readerCookies, "samesite") ?? (object)DBNull.Value);
                                    insertCommand.Parameters.AddWithValue("@SourceScheme", GetNullableString(readerCookies, "source_scheme") ?? (object)DBNull.Value);
                                    insertCommand.Parameters.AddWithValue("@SourcePort", readerCookies.IsDBNull(readerCookies.GetOrdinal("source_port")) ? (object)DBNull.Value : GetNullableInt32(readerCookies, "source_port"));
                                    insertCommand.Parameters.AddWithValue("@IsEncrypted", isEncrypted ? 1 : 0);
                                    insertCommand.Parameters.AddWithValue("@File", cookiesDbPath);
                                    insertCommand.ExecuteNonQuery();
                                }
                            }

                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            if (logConsole != null)
                            {
                                LogToConsole(logConsole, $"Error processing Chrome cookies: {ex.Message}", Color.Red);
                            }
                        }
                    }
                }
            }
        }


        public static void ProcessAndWriteCookiesFirefox(string chromeViewerConnectionString, string browserType, string filePath, RichTextBox? logConsole = null)
        {
            string? profilePath = Path.GetDirectoryName(filePath);
            if (string.IsNullOrWhiteSpace(profilePath))
                return;

            string cookiesDbPath = Path.Combine(profilePath, "cookies.sqlite");
            string sourceFilePath = cookiesDbPath;

            if (!string.IsNullOrEmpty(Helpers.realFirefoxPlacesPath))
            {
                string? realProfilePath = Path.GetDirectoryName(Helpers.realFirefoxPlacesPath);
                if (!string.IsNullOrEmpty(realProfilePath))
                {
                    string realCookiesPath = Path.Combine(realProfilePath, "cookies.sqlite");
                    if (File.Exists(realCookiesPath))
                    {
                        sourceFilePath = realCookiesPath;
                    }
                }
            }

            if (!File.Exists(cookiesDbPath))
            {
                return;
            }

            using (SQLiteConnection sourceConnection = new SQLiteConnection($"Data Source={cookiesDbPath};Version=3;"))
            {
                sourceConnection.Open();

                if (!TryRegisterProcessedSource("firefox-cookies", sourceFilePath, logConsole))
                    return;

                HashSet<string> columns = GetSqliteColumns(sourceConnection, "moz_cookies");

                if (!columns.Contains("host") || !columns.Contains("name"))
                {
                    return;
                }

                string queryCookies = $@"
                    SELECT
                        host,
                        name,
                        {SelectColumn(columns, "value", "''")},
                        {SelectColumn(columns, "path", "''")},
                        {SelectColumn(columns, "expiry", "0")},
                        {SelectColumn(columns, "lastAccessed", "0")},
                        {SelectColumn(columns, "creationTime", "0")},
                        {SelectColumn(columns, "isSecure", "0")},
                        {SelectColumn(columns, "isHttpOnly", "0")},
                        {SelectColumn(columns, "isSession", "0")},
                        {SelectColumn(columns, "sameSite", "NULL")}
                    FROM moz_cookies";

                using (SQLiteCommand commandCookies = new SQLiteCommand(queryCookies, sourceConnection))
                using (SQLiteDataReader readerCookies = commandCookies.ExecuteReader())
                using (SQLiteConnection targetConnection = new SQLiteConnection(chromeViewerConnectionString))
                {
                    targetConnection.Open();

                    using (SQLiteTransaction transaction = targetConnection.BeginTransaction())
                    {
                        try
                        {
                            while (readerCookies.Read())
                            {
                                string insertCookie = @"INSERT INTO cookies_data
                                    (Artifact_type, Potential_activity, Browser, Host, Name, Value, Path, Created, Expires, LastAccessed,
                                     IsSecure, IsHttpOnly, IsPersistent, SameSite, IsEncrypted, File)
                                    VALUES
                                    (@Artifact_type, @Potential_activity, @Browser, @Host, @Name, @Value, @Path, @Created, @Expires, @LastAccessed,
                                     @IsSecure, @IsHttpOnly, @IsPersistent, @SameSite, @IsEncrypted, @File)";

                                using (SQLiteCommand insertCommand = new SQLiteCommand(insertCookie, targetConnection, transaction))
                                {
                                    insertCommand.Parameters.AddWithValue("@Artifact_type", BuildArtifactType(browserType, "cookies"));
                                    insertCommand.Parameters.AddWithValue("@Potential_activity", "Web cookie stored");
                                    insertCommand.Parameters.AddWithValue("@Browser", browserType);
                                    insertCommand.Parameters.AddWithValue("@Host", GetNullableString(readerCookies, "host") ?? (object)DBNull.Value);
                                    insertCommand.Parameters.AddWithValue("@Name", GetNullableString(readerCookies, "name") ?? (object)DBNull.Value);
                                    insertCommand.Parameters.AddWithValue("@Value", GetNullableString(readerCookies, "value") ?? (object)DBNull.Value);
                                    insertCommand.Parameters.AddWithValue("@Path", GetNullableString(readerCookies, "path") ?? (object)DBNull.Value);
                                    insertCommand.Parameters.AddWithValue("@Created", FormatDateTime(UnixMicrosecondsToDateTime(GetNullableInt64(readerCookies, "creationTime"))) ?? (object)DBNull.Value);
                                    insertCommand.Parameters.AddWithValue("@Expires", FormatDateTime(UnixSecondsToDateTime(GetNullableInt64(readerCookies, "expiry"))) ?? (object)DBNull.Value);
                                    insertCommand.Parameters.AddWithValue("@LastAccessed", FormatDateTime(UnixMicrosecondsToDateTime(GetNullableInt64(readerCookies, "lastAccessed"))) ?? (object)DBNull.Value);
                                    insertCommand.Parameters.AddWithValue("@IsSecure", GetNullableInt32(readerCookies, "isSecure"));
                                    insertCommand.Parameters.AddWithValue("@IsHttpOnly", GetNullableInt32(readerCookies, "isHttpOnly"));
                                    insertCommand.Parameters.AddWithValue("@IsPersistent", GetNullableInt32(readerCookies, "isSession") == 0 ? 1 : 0);
                                    insertCommand.Parameters.AddWithValue("@SameSite", GetNullableString(readerCookies, "sameSite") ?? (object)DBNull.Value);
                                    insertCommand.Parameters.AddWithValue("@IsEncrypted", 0);
                                    insertCommand.Parameters.AddWithValue("@File", sourceFilePath);
                                    insertCommand.ExecuteNonQuery();
                                }
                            }

                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            if (logConsole != null)
                            {
                                LogToConsole(logConsole, $"Error processing Firefox cookies: {ex.Message}", Color.Red);
                            }
                        }
                    }
                }
            }
        }


        public static void ProcessAndWriteCacheChrome(string chromeViewerConnectionString, string browserType, string filePath, RichTextBox? logConsole = null)
        {
            string? profilePath = GetOwningBrowserProfileDirectory(filePath);
            if (string.IsNullOrWhiteSpace(profilePath))
                return;

            foreach (string cacheDir in GetChromeCacheDirectories(profilePath).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!TryRegisterProcessedSource("chromium-cache", cacheDir, logConsole))
                    continue;

                string displayBrowser = Helpers.ResolveDisplayBrowserName(browserType, cacheDir, "Chromium-like");
                ProcessCacheDirectory(chromeViewerConnectionString, displayBrowser, cacheDir, "Chromium Cache", logConsole);
            }
        }

        private static int ProcessChromiumStructuredAutofill(
            SQLiteConnection sourceConnection,
            SQLiteConnection targetConnection,
            SQLiteTransaction transaction,
            string browserType,
            string sourceFile,
            HashSet<string> seenAutofill)
        {
            int inserted = 0;
            HashSet<string> profileColumns = GetSqliteColumns(sourceConnection, "autofill_profiles");
            Dictionary<string, (int UseCount, DateTime? FirstUsed, DateTime? LastUsed)> profileMetadata =
                new Dictionary<string, (int UseCount, DateTime? FirstUsed, DateTime? LastUsed)>(StringComparer.OrdinalIgnoreCase);

            if (profileColumns.Count > 0)
            {
                string profileQuery = $@"
                    SELECT
                        {SelectQualifiedColumn(profileColumns, "p", "guid", "NULL")} AS guid,
                        {SelectQualifiedColumn(profileColumns, "p", "company_name", "NULL")} AS company_name,
                        {SelectQualifiedColumn(profileColumns, "p", "street_address", "NULL")} AS street_address,
                        {SelectQualifiedColumn(profileColumns, "p", "dependent_locality", "NULL")} AS dependent_locality,
                        {SelectQualifiedColumn(profileColumns, "p", "city", "NULL")} AS city,
                        {SelectQualifiedColumn(profileColumns, "p", "state", "NULL")} AS state,
                        {SelectFirstAvailableQualifiedColumn(profileColumns, "p", ["zipcode", "zip_code"], "zipcode")},
                        {SelectQualifiedColumn(profileColumns, "p", "sorting_code", "NULL")} AS sorting_code,
                        {SelectQualifiedColumn(profileColumns, "p", "country_code", "NULL")} AS country_code,
                        {SelectQualifiedColumn(profileColumns, "p", "language_code", "NULL")} AS language_code,
                        {SelectQualifiedColumn(profileColumns, "p", "label", "NULL")} AS label,
                        {SelectQualifiedColumn(profileColumns, "p", "origin", "NULL")} AS origin,
                        {SelectQualifiedColumn(profileColumns, "p", "use_count", "0")} AS use_count,
                        {SelectQualifiedColumn(profileColumns, "p", "use_date", "0")} AS use_date,
                        {SelectQualifiedColumn(profileColumns, "p", "date_modified", "0")} AS date_modified
                    FROM autofill_profiles p";

                using SQLiteCommand profileCommand = new SQLiteCommand(profileQuery, sourceConnection);
                using SQLiteDataReader profileReader = profileCommand.ExecuteReader();
                while (profileReader.Read())
                {
                    string guid = profileReader["guid"]?.ToString() ?? string.Empty;
                    int useCount = SafeInt32(profileReader["use_count"]);
                    DateTime? firstUsed = ParseFlexibleAutofillTimestamp(profileReader["date_modified"]);
                    DateTime? lastUsed = ParseFlexibleAutofillTimestamp(profileReader["use_date"]) ?? firstUsed;

                    if (!string.IsNullOrWhiteSpace(guid))
                        profileMetadata[guid] = (useCount, firstUsed, lastUsed);

                    inserted += InsertStructuredAutofillField(targetConnection, transaction, browserType, sourceFile, seenAutofill, "profile.company_name", profileReader["company_name"], useCount, firstUsed, lastUsed);
                    inserted += InsertStructuredAutofillField(targetConnection, transaction, browserType, sourceFile, seenAutofill, "profile.street_address", profileReader["street_address"], useCount, firstUsed, lastUsed);
                    inserted += InsertStructuredAutofillField(targetConnection, transaction, browserType, sourceFile, seenAutofill, "profile.dependent_locality", profileReader["dependent_locality"], useCount, firstUsed, lastUsed);
                    inserted += InsertStructuredAutofillField(targetConnection, transaction, browserType, sourceFile, seenAutofill, "profile.city", profileReader["city"], useCount, firstUsed, lastUsed);
                    inserted += InsertStructuredAutofillField(targetConnection, transaction, browserType, sourceFile, seenAutofill, "profile.state", profileReader["state"], useCount, firstUsed, lastUsed);
                    inserted += InsertStructuredAutofillField(targetConnection, transaction, browserType, sourceFile, seenAutofill, "profile.zipcode", profileReader["zipcode"], useCount, firstUsed, lastUsed);
                    inserted += InsertStructuredAutofillField(targetConnection, transaction, browserType, sourceFile, seenAutofill, "profile.sorting_code", profileReader["sorting_code"], useCount, firstUsed, lastUsed);
                    inserted += InsertStructuredAutofillField(targetConnection, transaction, browserType, sourceFile, seenAutofill, "profile.country_code", profileReader["country_code"], useCount, firstUsed, lastUsed);
                    inserted += InsertStructuredAutofillField(targetConnection, transaction, browserType, sourceFile, seenAutofill, "profile.language_code", profileReader["language_code"], useCount, firstUsed, lastUsed);
                    inserted += InsertStructuredAutofillField(targetConnection, transaction, browserType, sourceFile, seenAutofill, "profile.label", profileReader["label"], useCount, firstUsed, lastUsed);
                    inserted += InsertStructuredAutofillField(targetConnection, transaction, browserType, sourceFile, seenAutofill, "profile.origin", profileReader["origin"], useCount, firstUsed, lastUsed);
                }
            }

            inserted += InsertStructuredAutofillJoinTable(sourceConnection, targetConnection, transaction, browserType, sourceFile, seenAutofill,
                profileMetadata, "autofill_profile_names", "full_name", "profile.full_name");
            inserted += InsertStructuredAutofillJoinTable(sourceConnection, targetConnection, transaction, browserType, sourceFile, seenAutofill,
                profileMetadata, "autofill_profile_names", "first_name", "profile.first_name");
            inserted += InsertStructuredAutofillJoinTable(sourceConnection, targetConnection, transaction, browserType, sourceFile, seenAutofill,
                profileMetadata, "autofill_profile_names", "middle_name", "profile.middle_name");
            inserted += InsertStructuredAutofillJoinTable(sourceConnection, targetConnection, transaction, browserType, sourceFile, seenAutofill,
                profileMetadata, "autofill_profile_names", "last_name", "profile.last_name");
            inserted += InsertStructuredAutofillJoinTable(sourceConnection, targetConnection, transaction, browserType, sourceFile, seenAutofill,
                profileMetadata, "autofill_profile_emails", "email", "profile.email");
            inserted += InsertStructuredAutofillJoinTable(sourceConnection, targetConnection, transaction, browserType, sourceFile, seenAutofill,
                profileMetadata, "autofill_profile_phones", "number", "profile.phone");

            HashSet<string> contactColumns = GetSqliteColumns(sourceConnection, "contact_info");
            HashSet<string> contactTokenColumns = GetSqliteColumns(sourceConnection, "contact_info_type_tokens");
            if (contactColumns.Count > 0 && contactTokenColumns.Count > 0 &&
                contactColumns.Contains("guid") && contactTokenColumns.Contains("guid") &&
                contactTokenColumns.Contains("type") && contactTokenColumns.Contains("value"))
            {
                Dictionary<string, (int UseCount, DateTime? FirstUsed, DateTime? LastUsed)> contactMetadata =
                    new Dictionary<string, (int UseCount, DateTime? FirstUsed, DateTime? LastUsed)>(StringComparer.OrdinalIgnoreCase);

                string contactQuery = $@"
                    SELECT
                        guid,
                        {SelectQualifiedColumn(contactColumns, "c", "use_count", "0")} AS use_count,
                        {SelectQualifiedColumn(contactColumns, "c", "use_date", "0")} AS use_date,
                        {SelectQualifiedColumn(contactColumns, "c", "date_modified", "0")} AS date_modified
                    FROM contact_info c";

                using (SQLiteCommand contactCommand = new SQLiteCommand(contactQuery, sourceConnection))
                using (SQLiteDataReader contactReader = contactCommand.ExecuteReader())
                {
                    while (contactReader.Read())
                    {
                        string guid = contactReader["guid"]?.ToString() ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(guid))
                            continue;

                        int useCount = SafeInt32(contactReader["use_count"]);
                        DateTime? firstUsed = ParseFlexibleAutofillTimestamp(contactReader["date_modified"]);
                        DateTime? lastUsed = ParseFlexibleAutofillTimestamp(contactReader["use_date"]) ?? firstUsed;
                        contactMetadata[guid] = (useCount, firstUsed, lastUsed);
                    }
                }

                using SQLiteCommand tokenCommand = new SQLiteCommand("SELECT guid, type, value FROM contact_info_type_tokens", sourceConnection);
                using SQLiteDataReader tokenReader = tokenCommand.ExecuteReader();
                while (tokenReader.Read())
                {
                    string guid = tokenReader["guid"]?.ToString() ?? string.Empty;
                    string tokenType = tokenReader["type"]?.ToString() ?? "contact";
                    object value = tokenReader["value"];
                    (int UseCount, DateTime? FirstUsed, DateTime? LastUsed) metadata = contactMetadata.TryGetValue(guid, out var existing)
                        ? existing
                        : (0, null, null);
                    inserted += InsertStructuredAutofillField(targetConnection, transaction, browserType, sourceFile, seenAutofill, $"contact.{tokenType}", value, metadata.UseCount, metadata.FirstUsed, metadata.LastUsed);
                }
            }

            return inserted;
        }

        private static int InsertStructuredAutofillJoinTable(
            SQLiteConnection sourceConnection,
            SQLiteConnection targetConnection,
            SQLiteTransaction transaction,
            string browserType,
            string sourceFile,
            HashSet<string> seenAutofill,
            Dictionary<string, (int UseCount, DateTime? FirstUsed, DateTime? LastUsed)> profileMetadata,
            string tableName,
            string valueColumn,
            string fieldName)
        {
            HashSet<string> columns = GetSqliteColumns(sourceConnection, tableName);
            if (columns.Count == 0 || !columns.Contains("guid") || !columns.Contains(valueColumn))
                return 0;

            string query = $@"SELECT guid, {valueColumn} AS field_value FROM {tableName}";

            int inserted = 0;
            using SQLiteCommand command = new SQLiteCommand(query, sourceConnection);
            using SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                string guid = reader["guid"]?.ToString() ?? string.Empty;
                (int UseCount, DateTime? FirstUsed, DateTime? LastUsed) metadata = profileMetadata.TryGetValue(guid, out var existing)
                    ? existing
                    : (0, null, null);
                inserted += InsertStructuredAutofillField(targetConnection, transaction, browserType, sourceFile, seenAutofill, fieldName, reader["field_value"], metadata.UseCount, metadata.FirstUsed, metadata.LastUsed);
            }

            return inserted;
        }

        private static int InsertStructuredAutofillField(
            SQLiteConnection targetConnection,
            SQLiteTransaction transaction,
            string browserType,
            string sourceFile,
            HashSet<string> seenAutofill,
            string fieldName,
            object value,
            int count,
            DateTime? firstUsed,
            DateTime? lastUsed)
        {
            string? text = value == null || value == DBNull.Value ? null : value.ToString();
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            string dedupeKey = $"structured|{fieldName}|{text}";
            if (!seenAutofill.Add(dedupeKey))
                return 0;

            string insertAutofill = @"INSERT INTO autofill_data (Artifact_type, Potential_activity, Browser, FieldName, Value, Count, TimesUsed, FirstUsed, LastUsed, File)
                  VALUES (@Artifact_type, @Potential_activity, @Browser, @FieldName, @Value, @Count, @TimesUsed, @FirstUsed, @LastUsed, @File)";

            using SQLiteCommand insertCommand = new SQLiteCommand(insertAutofill, targetConnection, transaction);
            insertCommand.Parameters.AddWithValue("@Artifact_type", BuildArtifactType(browserType, "autofill"));
            insertCommand.Parameters.AddWithValue("@Potential_activity", "Autofill data saved");
            insertCommand.Parameters.AddWithValue("@Browser", browserType);
            insertCommand.Parameters.AddWithValue("@FieldName", fieldName);
            insertCommand.Parameters.AddWithValue("@Value", text);
            insertCommand.Parameters.AddWithValue("@Count", count);
            insertCommand.Parameters.AddWithValue("@TimesUsed", count);
            insertCommand.Parameters.AddWithValue("@FirstUsed", FormatDateTime(firstUsed) ?? (object)DBNull.Value);
            insertCommand.Parameters.AddWithValue("@LastUsed", FormatDateTime(lastUsed) ?? (object)DBNull.Value);
            insertCommand.Parameters.AddWithValue("@File", sourceFile);
            insertCommand.ExecuteNonQuery();
            return 1;
        }

        private static int ProcessEdgeStructuredAutofill(
            SQLiteConnection sourceConnection,
            SQLiteConnection targetConnection,
            SQLiteTransaction transaction,
            string browserType,
            string sourceFile,
            HashSet<string> seenAutofill)
        {
            HashSet<string> edgeValueColumns = GetSqliteColumns(sourceConnection, "autofill_edge_field_values");
            HashSet<string> edgeClientColumns = GetSqliteColumns(sourceConnection, "autofill_edge_field_client_info");
            if (edgeValueColumns.Count == 0 || edgeClientColumns.Count == 0 ||
                !edgeValueColumns.Contains("field_id") || !edgeValueColumns.Contains("value") ||
                !edgeClientColumns.Contains("field_id"))
            {
                return 0;
            }

            Dictionary<string, (string Label, string Domain)> clientInfo =
                new Dictionary<string, (string Label, string Domain)>(StringComparer.OrdinalIgnoreCase);

            string clientInfoQuery = $@"
                SELECT
                    field_id,
                    {SelectQualifiedColumn(edgeClientColumns, "c", "label", "NULL")} AS label,
                    {SelectQualifiedColumn(edgeClientColumns, "c", "domain_value", "NULL")} AS domain_value
                FROM autofill_edge_field_client_info c";

            using (SQLiteCommand clientCommand = new SQLiteCommand(clientInfoQuery, sourceConnection))
            using (SQLiteDataReader clientReader = clientCommand.ExecuteReader())
            {
                while (clientReader.Read())
                {
                    string fieldId = clientReader["field_id"]?.ToString() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(fieldId))
                        continue;

                    clientInfo[fieldId] = (
                        clientReader["label"]?.ToString() ?? string.Empty,
                        clientReader["domain_value"]?.ToString() ?? string.Empty);
                }
            }

            string query = $@"
                SELECT
                    field_id,
                    value,
                    {SelectQualifiedColumn(edgeValueColumns, "v", "count", "0")} AS value_count,
                    {SelectQualifiedColumn(edgeValueColumns, "v", "date_created", "0")} AS date_created,
                    {SelectQualifiedColumn(edgeValueColumns, "v", "date_last_used", "0")} AS date_last_used
                FROM autofill_edge_field_values v";

            int inserted = 0;
            using SQLiteCommand command = new SQLiteCommand(query, sourceConnection);
            using SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                string fieldId = reader["field_id"]?.ToString() ?? "";
                (string Label, string Domain) metadata = clientInfo.TryGetValue(fieldId, out var existing)
                    ? existing
                    : (string.Empty, string.Empty);
                string label = metadata.Label;
                string domain = metadata.Domain;
                string normalizedLabel = NormalizeAutofillFieldLabel(label);
                string fieldName = !string.IsNullOrWhiteSpace(normalizedLabel)
                    ? $"edge.{normalizedLabel}"
                    : !string.IsNullOrWhiteSpace(domain)
                        ? $"edge.{SanitizeAutofillToken(domain)}"
                        : $"edge.field.{SanitizeAutofillToken(fieldId)}";

                int useCount = SafeInt32(reader["value_count"]);
                DateTime? firstUsed = ParseFlexibleAutofillTimestamp(reader["date_created"]);
                DateTime? lastUsed = ParseFlexibleAutofillTimestamp(reader["date_last_used"]) ?? firstUsed;
                inserted += InsertStructuredAutofillField(targetConnection, transaction, browserType, sourceFile, seenAutofill, fieldName, reader["value"], useCount, firstUsed, lastUsed);
            }

            return inserted;
        }

        private static string NormalizeAutofillFieldLabel(string? label)
        {
            if (string.IsNullOrWhiteSpace(label))
                return string.Empty;

            string trimmed = label.Trim().ToLowerInvariant();
            return trimmed switch
            {
                "your name" => "full_name",
                "name" => "full_name",
                "full name" => "full_name",
                "first name" => "first_name",
                "last name" => "last_name",
                "email" => "email",
                "email address" => "email",
                "phone" => "phone",
                "phone number" => "phone",
                "address" => "street_address",
                "street address" => "street_address",
                "city" => "city",
                "state" => "state",
                "zip" => "zipcode",
                "zip code" => "zipcode",
                "postal code" => "zipcode",
                _ => SanitizeAutofillToken(trimmed)
            };
        }

        private static string SanitizeAutofillToken(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "unknown";

            StringBuilder builder = new StringBuilder();
            foreach (char c in value.Trim().ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(c))
                    builder.Append(c);
                else if (builder.Length == 0 || builder[^1] != '_')
                    builder.Append('_');
            }

            return builder.ToString().Trim('_');
        }

        private static DateTime? ParseFlexibleAutofillTimestamp(object value)
        {
            if (value == null || value == DBNull.Value)
                return null;

            long numericValue;
            try
            {
                numericValue = Convert.ToInt64(value);
            }
            catch
            {
                return null;
            }

            if (numericValue <= 0)
                return null;

            if (numericValue >= 100000000000000)
                return ChromeTimestampToDateTime(numericValue) ?? UnixMicrosecondsToDateTime(numericValue);

            if (numericValue >= 1000000000000)
                return UnixMillisecondsToDateTime(numericValue);

            return UnixSecondsToDateTime(numericValue);
        }

        private static int SafeInt32(object value)
        {
            if (value == null || value == DBNull.Value)
                return 0;

            try
            {
                return Convert.ToInt32(value);
            }
            catch
            {
                return 0;
            }
        }


        public static void ProcessAndWriteCacheFirefox(string chromeViewerConnectionString, string browserType, string filePath, RichTextBox? logConsole = null)
        {
            string? profilePath = GetOwningBrowserProfileDirectory(filePath);
            if (string.IsNullOrWhiteSpace(profilePath))
                return;

            List<string> cacheDirs = GetFirefoxCacheDirectories(profilePath).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (cacheDirs.Count == 0)
            {
                string profileName = string.IsNullOrWhiteSpace(profilePath) ? "" : Path.GetFileName(profilePath);
                if (logConsole != null)
                    LogToConsole(logConsole, $"Firefox cache directory not found for profile: {profileName} ({profilePath})", Color.Red);
                return;
            }

            foreach (string cacheDir in cacheDirs)
            {
                if (!TryRegisterProcessedSource("firefox-cache", cacheDir, logConsole))
                    continue;

                string displayBrowser = Helpers.ResolveDisplayBrowserName(browserType, cacheDir, "Firefox-like");
                ProcessCacheDirectory(chromeViewerConnectionString, displayBrowser, cacheDir, "Firefox Cache", logConsole);
            }
        }


        private static IEnumerable<string> GetChromeCacheDirectories(string profilePath)
        {
            if (string.IsNullOrEmpty(profilePath))
            {
                yield break;
            }

            string[] candidates =
            {
                Path.Combine(profilePath, "Cache", "Cache_Data"),
                Path.Combine(profilePath, "Cache"),
                Path.Combine(profilePath, "Code Cache", "js"),
                Path.Combine(profilePath, "Code Cache", "wasm"),
                Path.Combine(profilePath, "GPUCache")
            };

            foreach (string candidate in candidates)
            {
                if (Directory.Exists(candidate))
                {
                    yield return candidate;
                }
            }
        }


        private static IEnumerable<string> GetFirefoxCacheDirectories(string profilePath)
        {
            if (string.IsNullOrEmpty(profilePath))
            {
                yield break;
            }

            string profileName = Path.GetFileName(profilePath);
            string localProfilePath = profilePath.Replace(
                Path.Combine("AppData", "Roaming"),
                Path.Combine("AppData", "Local"),
                StringComparison.OrdinalIgnoreCase);
            string localProfilesRoot = GetFirefoxLocalProfilesRoot(profilePath);

            string[] candidates =
            {
                Path.Combine(profilePath, "cache2", "entries"),
                Path.Combine(localProfilePath, "cache2", "entries"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Mozilla", "Firefox", "Profiles", profileName, "cache2", "entries")
            };

            foreach (string candidate in candidates)
            {
                if (Directory.Exists(candidate))
                {
                    yield return candidate;
                }
            }
        }

        private static string GetFirefoxLocalProfilesRoot(string profilePath)
        {
            int appDataIndex = profilePath.IndexOf($"{Path.DirectorySeparatorChar}AppData{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);
            if (appDataIndex < 0)
            {
                return string.Empty;
            }

            string appDataRoot = profilePath.Substring(0, appDataIndex + $"{Path.DirectorySeparatorChar}AppData".Length);
            return Path.Combine(appDataRoot, "Local", "Mozilla", "Firefox", "Profiles");
        }

        private static void ProcessCacheDirectory(string chromeViewerConnectionString, string browserType, string cacheDir, string cacheType, RichTextBox? logConsole)
        {
            if (!Directory.Exists(cacheDir))
            {
                return;
            }

            if (cacheType == "Chromium Cache" && TryProcessChromiumBlockCacheDirectory(chromeViewerConnectionString, browserType, cacheDir, logConsole))
            {
                return;
            }

            using (SQLiteConnection targetConnection = new SQLiteConnection(chromeViewerConnectionString))
            {
                targetConnection.Open();

                using (SQLiteTransaction transaction = targetConnection.BeginTransaction())
                {
                    try
                    {
                        int inserted = 0;
                        foreach (string cacheFile in Directory.EnumerateFiles(cacheDir, "*", SearchOption.AllDirectories))
                        {
                            FileInfo fileInfo = new FileInfo(cacheFile);

                            if (!ShouldProcessCacheFile(fileInfo))
                            {
                                continue;
                            }

                            CacheEntryMetadata metadata = ExtractCacheEntryMetadata(cacheFile);
                            CacheBodyData bodyData = AnalyzeCacheBody(cacheFile, metadata);

                            if (string.IsNullOrWhiteSpace(metadata.Url) &&
                                string.IsNullOrWhiteSpace(metadata.ContentType) &&
                                string.IsNullOrWhiteSpace(metadata.Server) &&
                                bodyData.BodySize == 0)
                            {
                                continue;
                            }

                            InsertCacheEntry(targetConnection, transaction, browserType, cacheType, fileInfo, metadata, bodyData, cacheDir);
                            inserted++;
                        }

                        transaction.Commit();
                        if (inserted > 0 && logConsole != null)
                        {
                            LogToConsole(logConsole, $"Processed cache directory: {cacheDir} ({inserted} entries)", Color.Green);
                        }
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        if (logConsole != null)
                        {
                            LogToConsole(logConsole, $"Error processing cache {cacheDir}: {ex.Message}", Color.Red);
                        }
                    }
                }
            }
        }


        private static bool ShouldProcessCacheFile(FileInfo fileInfo)
        {
            if (!fileInfo.Exists || fileInfo.Length == 0)
            {
                return false;
            }

            string name = fileInfo.Name.ToLowerInvariant();
            if (name == "index" || name.StartsWith("the-real-index") || name.EndsWith(".tmp"))
            {
                return false;
            }

            return fileInfo.Length <= 50 * 1024 * 1024;
        }


        private static bool TryProcessChromiumBlockCacheDirectory(string chromeViewerConnectionString, string browserType, string cacheDir, RichTextBox? logConsole)
        {
            string indexPath = Path.Combine(cacheDir, "index");
            string data1Path = Path.Combine(cacheDir, "data_1");

            if (!File.Exists(indexPath) || !File.Exists(data1Path))
            {
                return false;
            }

            using (SQLiteConnection targetConnection = new SQLiteConnection(chromeViewerConnectionString))
            {
                targetConnection.Open();

                using (SQLiteTransaction transaction = targetConnection.BeginTransaction())
                {
                    try
                    {
                        int inserted = 0;
                        HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                        foreach (ChromiumBlockCacheEntry entry in EnumerateChromiumBlockCacheEntries(cacheDir))
                        {
                            if (string.IsNullOrWhiteSpace(entry.Key) || !seen.Add(entry.Key))
                            {
                                continue;
                            }

                            List<string> headerStrings = ExtractPrintableStrings(entry.HeaderBytes);
                            string joinedHeaders = string.Join("\n", headerStrings);
                            string? url = ExtractFirstUrl(new[] { entry.Key }) ?? ExtractFirstUrl(headerStrings);
                            string? host = ExtractHost(url);

                            CacheEntryMetadata metadata = new CacheEntryMetadata
                            {
                                Url = url,
                                Host = host,
                                ContentType = ExtractHeaderValue(joinedHeaders, "content-type"),
                                HttpStatus = ExtractHttpStatus(joinedHeaders),
                                Server = ExtractHeaderValue(joinedHeaders, "server"),
                                CacheKey = entry.Key,
                                Created = entry.Created,
                                Modified = ExtractHttpHeaderDate(joinedHeaders, "last-modified") ?? entry.LastModified ?? entry.Created,
                                LastAccessed = entry.LastUsed ?? entry.Created
                            };

                            CacheBodyData bodyData = AnalyzeCacheBodyBytes(entry.BodyBytes, metadata);
                            InsertCacheEntry(targetConnection, transaction, browserType, "Chromium Block Cache", entry.TotalSize, entry.SourceFile, metadata, bodyData, cacheDir);
                            inserted++;
                        }

                        transaction.Commit();
                        return inserted > 0;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        if (logConsole != null)
                        {
                            LogToConsole(logConsole, $"Error processing Chromium block cache {cacheDir}: {ex.Message}", Color.Red);
                        }

                        return false;
                    }
                }
            }
        }


        private static IEnumerable<ChromiumBlockCacheEntry> EnumerateChromiumBlockCacheEntries(string cacheDir)
        {
            string entriesPath = Path.Combine(cacheDir, "data_1");
            byte[] entriesFile = File.ReadAllBytes(entriesPath);
            const int blockFileHeaderSize = 8192;
            const int entryStoreSize = 256;
            const int keyOffset = 96;
            const int inlineKeyCapacity = entryStoreSize - keyOffset;

            for (int offset = blockFileHeaderSize; offset + entryStoreSize <= entriesFile.Length; offset += entryStoreSize)
            {
                uint hash = BitConverter.ToUInt32(entriesFile, offset);
                uint rankingsAddress = BitConverter.ToUInt32(entriesFile, offset + 8);
                int state = BitConverter.ToInt32(entriesFile, offset + 20);
                ulong creationRaw = BitConverter.ToUInt64(entriesFile, offset + 24);
                int keyLength = BitConverter.ToInt32(entriesFile, offset + 32);

                if (hash == 0 || state < 0 || state > 2 || keyLength <= 0 || keyLength > 4096)
                {
                    continue;
                }

                DateTime? created = ChromeTimestampToDateTime((long)creationRaw);
                if (!created.HasValue)
                {
                    continue;
                }

                string key = ReadChromiumCacheKey(cacheDir, entriesFile, offset, keyLength, BitConverter.ToUInt32(entriesFile, offset + 36), inlineKeyCapacity);
                if (string.IsNullOrWhiteSpace(key) || !key.Contains("://"))
                {
                    continue;
                }

                int[] dataSizes = new int[4];
                uint[] dataAddresses = new uint[4];
                for (int i = 0; i < 4; i++)
                {
                    dataSizes[i] = BitConverter.ToInt32(entriesFile, offset + 40 + (i * 4));
                    dataAddresses[i] = BitConverter.ToUInt32(entriesFile, offset + 56 + (i * 4));
                }

                ChromiumRankingsTimestamps rankings = ReadChromiumRankingsTimestamps(cacheDir, rankingsAddress);
                byte[] headers = ReadChromiumCacheAddress(cacheDir, dataAddresses[0], dataSizes[0]);
                byte[] body = ReadChromiumCacheAddress(cacheDir, dataAddresses[1], dataSizes[1]);

                yield return new ChromiumBlockCacheEntry
                {
                    Key = key,
                    Created = created,
                    LastUsed = rankings.LastUsed,
                    LastModified = rankings.LastModified,
                    HeaderBytes = headers,
                    BodyBytes = body,
                    TotalSize = dataSizes.Where(size => size > 0).Sum(),
                    SourceFile = $"{entriesPath}#{(offset - blockFileHeaderSize) / entryStoreSize}"
                };
            }
        }


        private static string ReadChromiumCacheKey(string cacheDir, byte[] entriesFile, int offset, int keyLength, uint longKeyAddress, int inlineKeyCapacity)
        {
            byte[] keyBytes;

            if (keyLength <= inlineKeyCapacity)
            {
                keyBytes = new byte[keyLength];
                Array.Copy(entriesFile, offset + 96, keyBytes, 0, keyLength);
            }
            else
            {
                keyBytes = ReadChromiumCacheAddress(cacheDir, longKeyAddress, keyLength);
            }

            return Encoding.UTF8.GetString(keyBytes).TrimEnd('\0');
        }


        private static ChromiumRankingsTimestamps ReadChromiumRankingsTimestamps(string cacheDir, uint address)
        {
            byte[] bytes = ReadChromiumCacheAddress(cacheDir, address, 36);
            if (bytes.Length < 16)
            {
                return new ChromiumRankingsTimestamps();
            }

            return new ChromiumRankingsTimestamps
            {
                LastUsed = ChromeTimestampToDateTime((long)BitConverter.ToUInt64(bytes, 0)),
                LastModified = ChromeTimestampToDateTime((long)BitConverter.ToUInt64(bytes, 8))
            };
        }


        private static byte[] ReadChromiumCacheAddress(string cacheDir, uint address, int requestedSize)
        {
            if (address == 0 || requestedSize <= 0 || (address & 0x80000000) == 0)
            {
                return Array.Empty<byte>();
            }

            int fileType = (int)((address >> 28) & 0x7);

            if (fileType == 0)
            {
                int fileNumber = (int)(address & 0x0FFFFFFF);
                string filePath = Path.Combine(cacheDir, $"f_{fileNumber:x6}");
                return ReadBytesFromFile(filePath, 0, requestedSize);
            }

            int blockNumber = (int)(address & 0xFFFF);
            int fileSelector = (int)((address >> 16) & 0xFF);
            int blockCount = (int)(((address >> 24) & 0x3) + 1);
            int blockSize = fileType switch
            {
                1 => 36,
                2 => 256,
                3 => 1024,
                4 => 4096,
                _ => 0
            };

            if (blockSize == 0)
            {
                return Array.Empty<byte>();
            }

            string blockFile = Path.Combine(cacheDir, $"data_{fileSelector}");
            int length = Math.Min(requestedSize, blockSize * blockCount);
            long offset = 8192L + ((long)blockNumber * blockSize);
            return ReadBytesFromFile(blockFile, offset, length);
        }


        private static byte[] ReadBytesFromFile(string filePath, long offset, int length)
        {
            if (!File.Exists(filePath) || length <= 0)
            {
                return Array.Empty<byte>();
            }

            FileInfo fileInfo = new FileInfo(filePath);
            if (offset < 0 || offset >= fileInfo.Length)
            {
                return Array.Empty<byte>();
            }

            int bytesToRead = (int)Math.Min(length, fileInfo.Length - offset);
            byte[] bytes = new byte[bytesToRead];

            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                stream.Position = offset;
                int read = stream.Read(bytes, 0, bytes.Length);
                if (read == bytes.Length)
                {
                    return bytes;
                }

                byte[] trimmed = new byte[read];
                Array.Copy(bytes, trimmed, read);
                return trimmed;
            }
        }


        private static CacheEntryMetadata ExtractCacheEntryMetadata(string filePath)
        {
            byte[] bytes = ReadCacheSample(filePath, 1024 * 1024);
            List<string> strings = ExtractPrintableStrings(bytes);
            string joined = string.Join("\n", strings);
            string? url = ExtractFirstUrl(strings);
            string? host = ExtractHost(url);

            return new CacheEntryMetadata
            {
                Url = url,
                Host = host,
                ContentType = ExtractHeaderValue(joined, "content-type"),
                HttpStatus = ExtractHttpStatus(joined),
                Server = ExtractHeaderValue(joined, "server"),
                CacheKey = ExtractCacheKey(strings),
                Created = NormalizeCacheActivityTimestamp(
                              ExtractUnixMetadataTimestamp(strings, "response-time") ??
                              ExtractUnixMetadataTimestamp(strings, "request-time") ??
                              ExtractHttpHeaderDate(joined, "date")),
                Modified = ExtractHttpHeaderDate(joined, "last-modified") ??
                           ExtractUnixMetadataTimestamp(strings, "last-modified"),
                LastAccessed = NormalizeCacheActivityTimestamp(
                                  ExtractUnixMetadataTimestamp(strings, "last-fetched") ??
                                  ExtractUnixMetadataTimestamp(strings, "last-accessed") ??
                                  ExtractUnixMetadataTimestamp(strings, "fetch-time"))
            };
        }


        private static DateTime? NormalizeCacheActivityTimestamp(DateTime? value)
        {
            if (!value.HasValue)
            {
                return null;
            }

            DateTime utc = value.Value.Kind == DateTimeKind.Utc ? value.Value : value.Value.ToUniversalTime();

            // Some Firefox cache2 entries expose placeholder/default dates through headers or metadata.
            // Do not promote those into activity fields; keep resource dates in Modified when available.
            if (utc == new DateTime(2001, 1, 1, 8, 0, 0, DateTimeKind.Utc))
            {
                return null;
            }

            return utc;
        }


        private static byte[] ReadCacheSample(string filePath, int maxBytes)
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                int bytesToRead = (int)Math.Min(stream.Length, maxBytes);
                byte[] buffer = new byte[bytesToRead];
                int read = stream.Read(buffer, 0, bytesToRead);

                if (read == buffer.Length)
                {
                    return buffer;
                }

                byte[] trimmed = new byte[read];
                Array.Copy(buffer, trimmed, read);
                return trimmed;
            }
        }


        private static List<string> ExtractPrintableStrings(byte[] bytes)
        {
            List<string> results = new List<string>();
            StringBuilder current = new StringBuilder();

            foreach (byte value in bytes)
            {
                if ((value >= 32 && value <= 126) || value == 9)
                {
                    current.Append((char)value);
                }
                else
                {
                    AddPrintableString(results, current);
                }
            }

            AddPrintableString(results, current);
            return results;
        }


        private static void AddPrintableString(List<string> results, StringBuilder current)
        {
            if (current.Length >= 4)
            {
                results.Add(current.ToString());
            }

            current.Clear();
        }


        private static string? ExtractFirstUrl(IEnumerable<string> strings)
        {
            Regex urlRegex = new Regex(@"https?://[^\s""'<>\\\x00]+", RegexOptions.IgnoreCase);

            foreach (string value in strings)
            {
                Match match = urlRegex.Match(value);
                if (match.Success)
                {
                    return match.Value.TrimEnd('.', ',', ';', ')', ']', '}');
                }
            }

            return null;
        }


        private static string? ExtractHost(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            {
                return uri.Host;
            }

            return null;
        }


        private static string? ExtractHeaderValue(string text, string headerName)
        {
            Match match = Regex.Match(text, $@"(?im)^{Regex.Escape(headerName)}\s*:\s*(.+)$");
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }


        private static string? ExtractHttpStatus(string text)
        {
            Match match = Regex.Match(text, @"HTTP/\d(?:\.\d)?\s+(\d{3}[^\r\n]*)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }


        private static DateTime? ExtractHttpHeaderDate(string text, string headerName)
        {
            string? value = ExtractHeaderValue(text, headerName);
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            value = Regex.Replace(value, @"\s+", " ").Trim();

            if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTimeOffset parsed))
            {
                DateTime utc = parsed.UtcDateTime;
                return IsReasonableBrowserTimestamp(utc) ? utc : null;
            }

            return null;
        }


        private static DateTime? ExtractUnixMetadataTimestamp(IReadOnlyList<string> strings, string key)
        {
            Regex inlinePattern = new Regex($@"(?i)\b{Regex.Escape(key)}\b\s*[:=]\s*(\d{{9,16}})");

            for (int i = 0; i < strings.Count; i++)
            {
                string value = strings[i];
                Match inlineMatch = inlinePattern.Match(value);
                if (inlineMatch.Success && TryConvertUnixTimestamp(inlineMatch.Groups[1].Value, out DateTime inlineDate))
                {
                    return inlineDate;
                }

                if (value.Equals(key, StringComparison.OrdinalIgnoreCase) && i + 1 < strings.Count)
                {
                    Match numericMatch = Regex.Match(strings[i + 1], @"\d{9,16}");
                    if (numericMatch.Success && TryConvertUnixTimestamp(numericMatch.Value, out DateTime pairedDate))
                    {
                        return pairedDate;
                    }
                }
            }

            return null;
        }


        private static bool TryConvertUnixTimestamp(string rawValue, out DateTime value)
        {
            value = default;

            if (!long.TryParse(rawValue, out long timestamp) || timestamp <= 0)
            {
                return false;
            }

            DateTime? converted = rawValue.Length switch
            {
                <= 10 => UnixSecondsToDateTime(timestamp),
                <= 13 => UnixMillisecondsToDateTime(timestamp),
                _ => UnixMicrosecondsToDateTime(timestamp)
            };

            if (!converted.HasValue)
            {
                return false;
            }

            value = converted.Value;
            return true;
        }


        private static string? ExtractCacheKey(IEnumerable<string> strings)
        {
            foreach (string value in strings)
            {
                if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    return value.Length > 500 ? value.Substring(0, 500) : value;
                }
            }

            return null;
        }


        private static CacheBodyData AnalyzeCacheBody(string filePath, CacheEntryMetadata metadata)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            CacheBodyData result = new CacheBodyData
            {
                BodySize = fileInfo.Length,
                BodyStored = 0
            };

            byte[] sample = ReadCacheSample(filePath, 8192);
            DetectedCacheFile detected = DetectCacheFileType(sample, metadata.ContentType);
            result.DetectedFileType = detected.FileType;
            result.DetectedExtension = detected.Extension;

            if (fileInfo.Length > 0 && fileInfo.Length <= 50 * 1024 * 1024)
            {
                using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (SHA256 sha256 = SHA256.Create())
                {
                    result.BodySha256 = BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
                }
            }

            if (!ShouldStoreCacheBody(fileInfo, detected))
            {
                result.BodyPreview = BuildBodyPreview(sample, detected);
                return result;
            }

            byte[] body = File.ReadAllBytes(filePath);
            result.Body = body;
            result.BodyStored = 1;
            result.BodyPreview = BuildBodyPreview(body, detected);

            return result;
        }


        private static CacheBodyData AnalyzeCacheBodyBytes(byte[] body, CacheEntryMetadata metadata)
        {
            byte[] safeBody = body ?? Array.Empty<byte>();
            CacheBodyData result = new CacheBodyData
            {
                BodySize = safeBody.LongLength,
                BodyStored = 0
            };

            byte[] sample = safeBody.Length > 8192 ? safeBody.Take(8192).ToArray() : safeBody;
            DetectedCacheFile detected = DetectCacheFileType(sample, metadata.ContentType);
            result.DetectedFileType = detected.FileType;
            result.DetectedExtension = detected.Extension;

            if (safeBody.Length > 0 && safeBody.Length <= 50 * 1024 * 1024)
            {
                using (SHA256 sha256 = SHA256.Create())
                {
                    result.BodySha256 = BitConverter.ToString(sha256.ComputeHash(safeBody)).Replace("-", "").ToLowerInvariant();
                }
            }

            if (safeBody.Length <= 0 || safeBody.Length > 25 * 1024 * 1024 || !detected.IsForensicallyValuable)
            {
                result.BodyPreview = BuildBodyPreview(sample, detected);
                return result;
            }

            result.Body = safeBody;
            result.BodyStored = 1;
            result.BodyPreview = BuildBodyPreview(safeBody, detected);
            return result;
        }


        private static bool ShouldStoreCacheBody(FileInfo fileInfo, DetectedCacheFile detected)
        {
            const long maxStoredBodyBytes = 25 * 1024 * 1024;

            if (fileInfo.Length <= 0 || fileInfo.Length > maxStoredBodyBytes)
            {
                return false;
            }

            return detected.IsForensicallyValuable;
        }


        private static DetectedCacheFile DetectCacheFileType(byte[] bytes, string? contentType)
        {
            DetectedCacheFile fromSignature = DetectCacheFileTypeFromSignature(bytes);
            if (fromSignature.IsForensicallyValuable || !string.IsNullOrEmpty(fromSignature.FileType))
            {
                return fromSignature;
            }

            string normalized = (contentType ?? "").ToLowerInvariant();
            bool looksLikeTrustedText = LooksLikeTrustedText(bytes);

            if (looksLikeTrustedText && normalized.Contains("html")) return new DetectedCacheFile("HTML", ".html", true, true);
            if (looksLikeTrustedText && normalized.Contains("json")) return new DetectedCacheFile("JSON", ".json", true, true);
            if (looksLikeTrustedText && normalized.Contains("xml")) return new DetectedCacheFile("XML", ".xml", true, true);
            if (looksLikeTrustedText && normalized.Contains("javascript")) return new DetectedCacheFile("JavaScript", ".js", true, true);
            if (looksLikeTrustedText && normalized.Contains("css")) return new DetectedCacheFile("CSS", ".css", true, true);
            if (looksLikeTrustedText && normalized.Contains("text/")) return new DetectedCacheFile("Text", ".txt", true, true);
            if (normalized.Contains("image/jpeg")) return new DetectedCacheFile("JPEG", ".jpg", true, false);
            if (normalized.Contains("image/png")) return new DetectedCacheFile("PNG", ".png", true, false);
            if (normalized.Contains("image/gif")) return new DetectedCacheFile("GIF", ".gif", true, false);
            if (normalized.Contains("image/webp")) return new DetectedCacheFile("WEBP", ".webp", true, false);
            if (normalized.Contains("application/pdf")) return new DetectedCacheFile("PDF", ".pdf", true, false);
            if (normalized.Contains("audio/mpeg")) return new DetectedCacheFile("MP3", ".mp3", true, false);
            if (normalized.Contains("audio/wav") || normalized.Contains("audio/x-wav")) return new DetectedCacheFile("WAV", ".wav", true, false);
            if (normalized.Contains("audio/ogg")) return new DetectedCacheFile("OGG Audio", ".ogg", true, false);
            if (normalized.Contains("audio/")) return new DetectedCacheFile("Audio", ".mp3", true, false);
            if (normalized.Contains("video/mp4")) return new DetectedCacheFile("MP4", ".mp4", true, false);
            if (normalized.Contains("video/quicktime")) return new DetectedCacheFile("QuickTime", ".mov", true, false);
            if (normalized.Contains("video/webm")) return new DetectedCacheFile("WEBM", ".webm", true, false);
            if (normalized.Contains("video/ogg")) return new DetectedCacheFile("OGG Video", ".ogv", true, false);
            if (normalized.Contains("video/x-msvideo")) return new DetectedCacheFile("AVI", ".avi", true, false);
            if (normalized.Contains("video/")) return new DetectedCacheFile("Video", ".mp4", true, false);

            return new DetectedCacheFile("Unknown", null, false, false);
        }


        private static DetectedCacheFile DetectCacheFileTypeFromSignature(byte[] bytes)
        {
            if (bytes.Length >= 4 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
                return new DetectedCacheFile("JPEG", ".jpg", true, false);
            if (bytes.Length >= 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
                return new DetectedCacheFile("PNG", ".png", true, false);
            if (bytes.Length >= 6 && Encoding.ASCII.GetString(bytes, 0, 6).StartsWith("GIF"))
                return new DetectedCacheFile("GIF", ".gif", true, false);
            if (bytes.Length >= 12 && Encoding.ASCII.GetString(bytes, 0, 4) == "RIFF" && Encoding.ASCII.GetString(bytes, 8, 4) == "WEBP")
                return new DetectedCacheFile("WEBP", ".webp", true, false);
            if (bytes.Length >= 4 && Encoding.ASCII.GetString(bytes, 0, 4) == "%PDF")
                return new DetectedCacheFile("PDF", ".pdf", true, false);
            if (bytes.Length >= 4 && bytes[0] == 0x50 && bytes[1] == 0x4B && bytes[2] == 0x03 && bytes[3] == 0x04)
                return new DetectedCacheFile("ZIP/Office", ".zip", true, false);
            if (bytes.Length >= 12 && Encoding.ASCII.GetString(bytes, 4, 4) == "ftyp")
                return new DetectedCacheFile("MP4/QuickTime", ".mp4", true, false);
            if (bytes.Length >= 12 && Encoding.ASCII.GetString(bytes, 0, 4) == "RIFF" && Encoding.ASCII.GetString(bytes, 8, 4) == "AVI ")
                return new DetectedCacheFile("AVI", ".avi", true, false);
            if (bytes.Length >= 12 && Encoding.ASCII.GetString(bytes, 0, 4) == "RIFF" && Encoding.ASCII.GetString(bytes, 8, 4) == "WAVE")
                return new DetectedCacheFile("WAV", ".wav", true, false);
            if (bytes.Length >= 3 && bytes[0] == 0x49 && bytes[1] == 0x44 && bytes[2] == 0x33)
                return new DetectedCacheFile("MP3", ".mp3", true, false);
            if (bytes.Length >= 2 && bytes[0] == 0x42 && bytes[1] == 0x4D)
                return new DetectedCacheFile("BMP", ".bmp", true, false);
            if (LooksLikeText(bytes))
                return new DetectedCacheFile("Text", ".txt", true, true);

            return new DetectedCacheFile(null, null, false, false);
        }


        private static bool LooksLikeText(byte[] bytes)
        {
            if (bytes.Length == 0)
            {
                return false;
            }

            int printable = 0;
            int checkedBytes = Math.Min(bytes.Length, 512);

            for (int i = 0; i < checkedBytes; i++)
            {
                byte value = bytes[i];
                if (value == 9 || value == 10 || value == 13 || (value >= 32 && value <= 126))
                {
                    printable++;
                }
            }

            return printable >= checkedBytes * 0.85;
        }

        private static bool LooksLikeTrustedText(byte[] bytes)
        {
            if (!LooksLikeText(bytes))
                return false;

            int checkedBytes = Math.Min(bytes.Length, 512);
            int suspicious = 0;

            for (int i = 0; i < checkedBytes; i++)
            {
                byte value = bytes[i];

                if (value == 0 || value == 0xFF || value == 0xFE)
                    suspicious++;
            }

            if (suspicious > Math.Max(2, checkedBytes / 50))
                return false;

            string sample;
            try
            {
                sample = Encoding.UTF8.GetString(bytes, 0, checkedBytes);
            }
            catch
            {
                return false;
            }

            sample = sample.Trim();
            if (string.IsNullOrWhiteSpace(sample))
                return false;

            int replacementChars = sample.Count(c => c == '\uFFFD');
            if (replacementChars > Math.Max(2, sample.Length / 40))
                return false;

            return true;
        }


        private static string? BuildBodyPreview(byte[] bytes, DetectedCacheFile detected)
        {
            if (!detected.IsText || bytes == null || bytes.Length == 0)
            {
                return null;
            }

            if (!LooksLikeTrustedText(bytes))
            {
                return null;
            }

            string text = Encoding.UTF8.GetString(bytes);
            text = Regex.Replace(text, @"\s+", " ").Trim();
            return text.Length > 500 ? text.Substring(0, 500) : text;
        }


        private static void InsertCacheEntry(SQLiteConnection connection, SQLiteTransaction transaction, string browserType, string cacheType, FileInfo fileInfo, CacheEntryMetadata metadata, CacheBodyData bodyData, string cacheDir)
        {
            InsertCacheEntry(connection, transaction, browserType, cacheType, fileInfo.Length, fileInfo.FullName, metadata, bodyData, cacheDir);
        }


        private static void InsertCacheEntry(SQLiteConnection connection, SQLiteTransaction transaction, string browserType, string cacheType, long fileSize, string cacheFile, CacheEntryMetadata metadata, CacheBodyData bodyData, string cacheDir)
        {
            string insertCache = @"INSERT INTO cache_data
                (Artifact_type, Potential_activity, Browser, Url, Host, ContentType, CacheType, HttpStatus, Server, FileSize,
                 Created, Modified, LastAccessed, CacheFile, CacheKey, Body, BodySize, BodySha256,
                 BodyStored, BodyPreview, DetectedFileType, DetectedExtension, File)
                VALUES
                (@Artifact_type, @Potential_activity, @Browser, @Url, @Host, @ContentType, @CacheType, @HttpStatus, @Server, @FileSize,
                 @Created, @Modified, @LastAccessed, @CacheFile, @CacheKey, @Body, @BodySize, @BodySha256,
                 @BodyStored, @BodyPreview, @DetectedFileType, @DetectedExtension, @File)";

            using (SQLiteCommand insertCommand = new SQLiteCommand(insertCache, connection, transaction))
            {
                insertCommand.Parameters.AddWithValue("@Artifact_type", BuildArtifactType(browserType, "cache"));
                insertCommand.Parameters.AddWithValue("@Browser", browserType);
                insertCommand.Parameters.AddWithValue("@Potential_activity", GetCachePotentialActivity(metadata, bodyData));
                insertCommand.Parameters.AddWithValue("@Url", metadata.Url ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@Host", metadata.Host ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@ContentType", metadata.ContentType ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@CacheType", cacheType);
                insertCommand.Parameters.AddWithValue("@HttpStatus", metadata.HttpStatus ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@Server", metadata.Server ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@FileSize", fileSize);
                insertCommand.Parameters.AddWithValue("@Created", FormatDateTime(metadata.Created ?? metadata.LastAccessed) ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@Modified", FormatDateTime(metadata.Modified ?? metadata.Created) ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@LastAccessed", FormatDateTime(metadata.LastAccessed) ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@CacheFile", cacheFile);
                insertCommand.Parameters.AddWithValue("@CacheKey", metadata.CacheKey ?? (object)DBNull.Value);
                insertCommand.Parameters.Add("@Body", DbType.Binary).Value = bodyData.Body ?? (object)DBNull.Value;
                insertCommand.Parameters.AddWithValue("@BodySize", bodyData.BodySize);
                insertCommand.Parameters.AddWithValue("@BodySha256", bodyData.BodySha256 ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@BodyStored", bodyData.BodyStored);
                insertCommand.Parameters.AddWithValue("@BodyPreview", bodyData.BodyPreview ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@DetectedFileType", bodyData.DetectedFileType ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@DetectedExtension", bodyData.DetectedExtension ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@File", cacheFile);
                insertCommand.ExecuteNonQuery();
            }
        }


        private static string GetCachePotentialActivity(CacheEntryMetadata metadata, CacheBodyData bodyData)
        {
            string type = (bodyData.DetectedFileType ?? "").ToLowerInvariant();
            string contentType = (metadata.ContentType ?? "").ToLowerInvariant();

            if (type.Contains("video") || type.Contains("mp4") || type.Contains("quicktime") || type.Contains("webm") || contentType.StartsWith("video/"))
            {
                return "Caching video";
            }

            if (type.Contains("audio") || type.Contains("mp3") || type.Contains("wav") || contentType.StartsWith("audio/"))
            {
                return "Caching audio";
            }

            if (type.Contains("jpeg") || type.Contains("png") || type.Contains("gif") || type.Contains("webp") || type.Contains("bmp") || contentType.StartsWith("image/"))
            {
                return "Caching image";
            }

            if (type.Contains("pdf") || contentType.Contains("application/pdf"))
            {
                return "Caching document";
            }

            if (type.Contains("zip") || contentType.Contains("zip") || contentType.Contains("office") || contentType.Contains("compressed"))
            {
                return "Caching archive";
            }

            if (type.Contains("javascript") || type == "css" || contentType.Contains("javascript") || contentType.Contains("text/css"))
            {
                return "Caching web script or style";
            }

            if (type.Contains("html") || contentType.Contains("html"))
            {
                return "Caching web page";
            }

            if (type.Contains("json") || type.Contains("xml") || contentType.Contains("json") || contentType.Contains("xml"))
            {
                return "Caching web data";
            }

            if (type.Contains("text") || contentType.StartsWith("text/"))
            {
                return "Caching text content";
            }

            if (!string.IsNullOrWhiteSpace(metadata.Url))
            {
                return "Caching web resource";
            }

            return "Caching browser resource";
        }


        private class CacheEntryMetadata
        {
            public string? Url { get; set; }
            public string? Host { get; set; }
            public string? ContentType { get; set; }
            public string? HttpStatus { get; set; }
            public string? Server { get; set; }
            public string? CacheKey { get; set; }
            public DateTime? Created { get; set; }
            public DateTime? Modified { get; set; }
            public DateTime? LastAccessed { get; set; }
        }


        private class ChromiumBlockCacheEntry
        {
            public string Key { get; set; } = string.Empty;
            public DateTime? Created { get; set; }
            public DateTime? LastUsed { get; set; }
            public DateTime? LastModified { get; set; }
            public byte[] HeaderBytes { get; set; } = Array.Empty<byte>();
            public byte[] BodyBytes { get; set; } = Array.Empty<byte>();
            public long TotalSize { get; set; }
            public string SourceFile { get; set; } = string.Empty;
        }


        private class ChromiumRankingsTimestamps
        {
            public DateTime? LastUsed { get; set; }
            public DateTime? LastModified { get; set; }
        }


        private class CacheBodyData
        {
            public byte[] Body { get; set; } = Array.Empty<byte>();
            public long BodySize { get; set; }
            public string BodySha256 { get; set; } = string.Empty;
            public int BodyStored { get; set; }
            public string? BodyPreview { get; set; }
            public string? DetectedFileType { get; set; }
            public string? DetectedExtension { get; set; }
        }


        private class DetectedCacheFile
        {
            public DetectedCacheFile(string? fileType, string? extension, bool isForensicallyValuable, bool isText)
            {
                FileType = fileType;
                Extension = extension;
                IsForensicallyValuable = isForensicallyValuable;
                IsText = isText;
            }

            public string? FileType { get; }
            public string? Extension { get; }
            public bool IsForensicallyValuable { get; }
            public bool IsText { get; }
        }

        private static void ApplySearchHighlight(SfDataGrid dataGrid)
        {
            if (dataGrid?.SearchController == null)
                return;

            if (!Helpers.searchTermExists || Helpers.searchTermRegExp)
            {
                dataGrid.SearchController.ClearSearch();
                dataGrid.Refresh();
                return;
            }

            dataGrid.SearchController.ClearSearch();
            dataGrid.SearchController.Search(Helpers.searchTerm);
            dataGrid.Refresh();
        }



        



        public static void ProcessAndWriteAutofillFirefox(string chromeViewerConnectionString, string browserType, string filePath, RichTextBox? logConsole = null)
        {
            string? profilePath = Path.GetDirectoryName(filePath);
            if (string.IsNullOrWhiteSpace(profilePath))
                return;

            string resolvedBrowserType = Helpers.ResolveDisplayBrowserName(browserType, filePath, "Firefox-like");

            string autofillDbPath = Path.Combine(profilePath, "formhistory.sqlite");
            string sourcePath = !string.IsNullOrWhiteSpace(Helpers.realFirefoxFormPath) ? Helpers.realFirefoxFormPath : autofillDbPath;
            if (!TryRegisterProcessedSource("firefox-autofill", sourcePath, logConsole))
                return;

            if (File.Exists(autofillDbPath))
            {
                using (SQLiteConnection firefoxAutofillConnection = new SQLiteConnection($"Data Source={autofillDbPath};Version=3;"))
                {
                    firefoxAutofillConnection.Open();

                    string queryAutofill = @"SELECT fieldname, value, timesUsed, firstUsed, lastUsed FROM moz_formhistory";

                    using (SQLiteCommand commandAutofill = new SQLiteCommand(queryAutofill, firefoxAutofillConnection))
                    using (SQLiteDataReader readerAutofill = commandAutofill.ExecuteReader())
                    {
                        using (SQLiteConnection firefoxViewerConnection = new SQLiteConnection(chromeViewerConnectionString))
                        {
                            firefoxViewerConnection.Open();

                            using (SQLiteTransaction transaction = firefoxViewerConnection.BeginTransaction())
                            {
                                try
                                {
                                    while (readerAutofill.Read())
                                    {
                                        string fieldName = readerAutofill.GetString(0);
                                        string value = readerAutofill.GetString(1);
                                        int timesUsed = readerAutofill.GetInt32(2);

                                        long firstUsedMicroseconds = readerAutofill.GetInt64(3);
                                        long lastUsedMicroseconds = readerAutofill.GetInt64(4);

                                        DateTime firstUsedDate = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(firstUsedMicroseconds / 1000.0);
                                        DateTime lastUsedDate = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(lastUsedMicroseconds / 1000.0);

                                        string insertAutofill = @"INSERT INTO autofill_data (Artifact_type, Potential_activity, Browser, FieldName, Value, TimesUsed, FirstUsed, LastUsed, File)
                                              VALUES (@Artifact_type, @Potential_activity, @Browser, @FieldName, @Value, @TimesUsed, @FirstUsed, @LastUsed, @File)";

                                        using (SQLiteCommand insertCommand = new SQLiteCommand(insertAutofill, firefoxViewerConnection, transaction))
                                        {
                                            insertCommand.Parameters.AddWithValue("@Artifact_type", BuildArtifactType(resolvedBrowserType, "autofill"));
                                            insertCommand.Parameters.AddWithValue("@Potential_activity", "Autofill data saved");
                                            insertCommand.Parameters.AddWithValue("@Browser", resolvedBrowserType);
                                            insertCommand.Parameters.AddWithValue("@FieldName", fieldName);
                                            insertCommand.Parameters.AddWithValue("@Value", value);
                                            insertCommand.Parameters.AddWithValue("@TimesUsed", timesUsed);
                                            insertCommand.Parameters.AddWithValue("@FirstUsed", firstUsedDate.ToString("yyyy-MM-dd HH:mm:ss.ffffff"));
                                            insertCommand.Parameters.AddWithValue("@LastUsed", lastUsedDate.ToString("yyyy-MM-dd HH:mm:ss.ffffff"));

                                            if (Helpers.realFirefoxFormPath != "")
                                            {
                                                insertCommand.Parameters.AddWithValue("@File", Helpers.realFirefoxFormPath);
                                            }
                                            else
                                            {
                                                insertCommand.Parameters.AddWithValue("@File", autofillDbPath);
                                            }
                                            
                                            insertCommand.ExecuteNonQuery();
                                        }
                                    }

                                    transaction.Commit();
                                }
                                catch (Exception ex)
                                {
                                    transaction.Rollback();
                                    if (logConsole != null)
                                        logConsole.AppendText($"Error: {ex.Message}\n");
                                    else
                                        Console.WriteLine($"Error: {ex.Message}");
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine($"Firefox autocomplete data file not found: {autofillDbPath}. [Browser Reviewer]");
            }
        }







        public void UpdateNavegadorLabel(string navegadorKey, string newText)
        {
            if (Helpers.navegadorLabels.TryGetValue(navegadorKey, out Label? lbl))
            {
                lbl.Text = newText;
            }
        }


        public void UpdateDownloadsLabel(string navegadorKey, string newText)
        {
            if (Helpers.downloadsLabels.TryGetValue(navegadorKey, out Label? lbl))
            {
                lbl.Text = newText;
            }
        }




        public void UpdateBookmarksLabel(string navegadorKey, string newText)
        {
            if (Helpers.bookmarksLabels.TryGetValue(navegadorKey, out Label? lbl))
            {
                lbl.Text = newText;
            }
        }








        public int NumUrlsWithBrowser(string browserName)
        {
            int count = 0;

            string query = @"
            SELECT COUNT(*) 
            FROM (
                SELECT 1 FROM results WHERE Browser = @browserName
                UNION ALL
                SELECT 1 FROM firefox_results WHERE Browser = @browserName
            ) AS CombinedResults;
        ";

            using (var connection = new SQLiteConnection(Helpers.chromeViewerConnectionString))
            {
                connection.Open();

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@browserName", browserName);

                    count = Convert.ToInt32(command.ExecuteScalar());
                }
            }

            return count;
        }


        public int NumDownloadsWithBrowser(string browserName)
        {
            int count = 0;

            string query = @"
            SELECT COUNT(*)
            FROM (
                SELECT Browser FROM firefox_downloads WHERE Browser = @browserName
                UNION ALL
                SELECT Browser FROM chrome_downloads WHERE Browser = @browserName
            ) AS CombinedDownloads;";

            using (var connection = new SQLiteConnection(Helpers.chromeViewerConnectionString))
            {
                connection.Open();

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@browserName", browserName);

                    count = Convert.ToInt32(command.ExecuteScalar());
                }
            }

            return count;
        }

        public int NumBookmarksWithBrowser(string browserName)
        {
            int count = 0;

            string query = @"
                                SELECT COUNT(*)
                                FROM (
                                    SELECT Browser FROM bookmarks_Firefox WHERE Browser = @browserName
                                    UNION ALL
                                    SELECT Browser FROM bookmarks_Chrome WHERE Browser = @browserName
                                ) AS CombinedBookmarks;";

            using (var connection = new SQLiteConnection(Helpers.chromeViewerConnectionString))
            {
                connection.Open();

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@browserName", browserName);

                    count = Convert.ToInt32(command.ExecuteScalar());
                }
            }

            return count;
        }




    }

}
