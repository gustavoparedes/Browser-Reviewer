using System.Data.SQLite;
using System.Runtime.InteropServices;

namespace Browser_Reviewer
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                if (HasHelpFlag(args))
                {
                    ConsoleHelper.EnsureConsole();
                    Console.WriteLine(GetHelpText());
                    Environment.Exit(0);
                    return;
                }

                if (IsCliInvocation(args))
                {
                    ConsoleHelper.EnsureConsole();
                    int code = RunCli(args);
                    Environment.Exit(code);
                    return;
                }

                ApplicationConfiguration.Initialize();
                using (var startupForm = new StartupForm())
                {
                    if (startupForm.ShowDialog() != DialogResult.OK || startupForm.Request.Action == StartupAction.Exit)
                        return;

                    Application.Run(new Form1(startupForm.Request));
                }
            }
            catch (Exception ex)
            {
                try
                {
                    ConsoleHelper.EnsureConsole();
                    Console.Error.WriteLine($"[Browser Reviewer]: Unexpected failure - {ex}");
                }
                catch
                {
                    Console.Error.WriteLine($"[Browser Reviewer]: Error - {ex}");
                }

                Environment.Exit(1);
            }
        }

        private static int RunCli(string[] args)
        {
            CliOptions options;
            try
            {
                options = ParseCliOptions(args);
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine("[Browser Reviewer - CLI]: Error - " + ex.Message);
                Console.WriteLine(GetHelpText());
                return 2;
            }

            string dbArg = options.DatabasePath;
            string rootPath = options.ScanPath;

            if (string.IsNullOrWhiteSpace(dbArg))
            {
                Console.Error.WriteLine("[Browser Reviewer - CLI]: Error - database name/path cannot be empty.");
                Console.WriteLine(GetHelpText());
                return 2;
            }

            if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
            {
                Console.Error.WriteLine($"[Browser Reviewer - CLI]: Error - scan path does not exist: {rootPath}");
                Console.WriteLine(GetHelpText());
                return 3;
            }

            string dbFullPath = Path.IsPathRooted(dbArg) ? dbArg : Path.Combine(Environment.CurrentDirectory, dbArg);
            if (!dbFullPath.EndsWith(".bre", StringComparison.OrdinalIgnoreCase))
                dbFullPath += ".bre";

            try
            {
                dbFullPath = Path.GetFullPath(dbFullPath);
                rootPath = Path.GetFullPath(rootPath);

                string? targetDir = Path.GetDirectoryName(dbFullPath);
                if (string.IsNullOrWhiteSpace(targetDir))
                    targetDir = Environment.CurrentDirectory;

                Directory.CreateDirectory(targetDir);

                if (File.Exists(dbFullPath))
                {
                    if (!options.Overwrite)
                    {
                        Console.Error.WriteLine($"[Browser Reviewer - CLI]: Error - output database already exists: {dbFullPath}");
                        Console.Error.WriteLine("[Browser Reviewer - CLI]: Use --overwrite to replace it, or choose a different .bre path.");
                        return 8;
                    }

                    File.Delete(dbFullPath);
                }

                string testPath = dbFullPath + ".writecheck.tmp";
                using (var fs = new FileStream(testPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    fs.WriteByte(0x00);
                }

                File.Delete(testPath);
            }
            catch (UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"[Browser Reviewer - CLI]: Error - no write permission at destination: {dbFullPath}");
                return 4;
            }
            catch (PathTooLongException)
            {
                Console.Error.WriteLine($"[Browser Reviewer - CLI]: Error - path is too long: {dbFullPath}");
                return 5;
            }
            catch (IOException ioex)
            {
                Console.Error.WriteLine($"[Browser Reviewer - CLI]: I/O error while validating the output file: {dbFullPath}");
                Console.Error.WriteLine(ioex.Message);
                return 6;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Browser Reviewer - CLI]: Error - invalid output file path: {dbFullPath}");
                Console.Error.WriteLine(ex.Message);
                return 7;
            }

            string connStr = $"Data Source={dbFullPath};Version=3;";
            var tools = new MyTools();
            Helpers.db_name = dbFullPath;
            Helpers.chromeViewerConnectionString = connStr;
            tools.CreateDatabase(Helpers.chromeViewerConnectionString);

            Console.WriteLine("[Browser Reviewer - CLI]: Starting extraction...");
            Console.WriteLine("[Browser Reviewer - CLI]: Database: " + dbFullPath);
            Console.WriteLine("[Browser Reviewer - CLI]: Scan root: " + rootPath);
            Console.WriteLine("[Browser Reviewer - CLI]: Artifacts: history, downloads, bookmarks, autofill, cookies, cache, sessions, extensions, saved logins, local storage, session storage, indexedDB");

            try
            {
                tools.CLI_ListFilesAndDirectories(rootPath).GetAwaiter().GetResult();
                PrintCliSummary(connStr);
                Console.WriteLine("[Browser Reviewer - CLI]: Finished successfully.");
                Console.WriteLine();
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[Browser Reviewer - CLI]: Error - " + ex.Message);
                Console.Error.WriteLine(ex);
                return 10;
            }
            finally
            {
                MyTools.CloseLog();
            }
        }

        private static bool IsCliInvocation(string[] args)
        {
            if (args == null || args.Length == 0)
                return false;

            if (args.Length >= 2)
                return true;

            return args.Any(arg =>
                string.Equals(arg, "--cli", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "--out", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "--db", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "--scan", StringComparison.OrdinalIgnoreCase));
        }

        private static CliOptions ParseCliOptions(string[] args)
        {
            var options = new CliOptions();
            var positional = new List<string>();

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i]?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(arg))
                    continue;

                if (string.Equals(arg, "--cli", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (string.Equals(arg, "--overwrite", StringComparison.OrdinalIgnoreCase) || string.Equals(arg, "-f", StringComparison.OrdinalIgnoreCase))
                {
                    options.Overwrite = true;
                    continue;
                }

                if (string.Equals(arg, "--out", StringComparison.OrdinalIgnoreCase) || string.Equals(arg, "--db", StringComparison.OrdinalIgnoreCase) || string.Equals(arg, "-o", StringComparison.OrdinalIgnoreCase))
                {
                    options.DatabasePath = ReadOptionValue(args, ref i, arg);
                    continue;
                }

                if (string.Equals(arg, "--scan", StringComparison.OrdinalIgnoreCase) || string.Equals(arg, "--root", StringComparison.OrdinalIgnoreCase) || string.Equals(arg, "-s", StringComparison.OrdinalIgnoreCase))
                {
                    options.ScanPath = ReadOptionValue(args, ref i, arg);
                    continue;
                }

                if (arg.StartsWith("-", StringComparison.Ordinal))
                    throw new ArgumentException($"unknown option: {arg}");

                positional.Add(arg);
            }

            if (string.IsNullOrWhiteSpace(options.DatabasePath) && positional.Count > 0)
                options.DatabasePath = positional[0];

            if (string.IsNullOrWhiteSpace(options.ScanPath) && positional.Count > 1)
                options.ScanPath = positional[1];

            if (positional.Count > 2)
                throw new ArgumentException("too many positional arguments.");

            return options;
        }

        private static string ReadOptionValue(string[] args, ref int index, string optionName)
        {
            if (index + 1 >= args.Length || string.IsNullOrWhiteSpace(args[index + 1]) || args[index + 1].StartsWith("-", StringComparison.Ordinal))
                throw new ArgumentException($"{optionName} requires a value.");

            index++;
            return args[index].Trim();
        }

        private static void PrintCliSummary(string connectionString)
        {
            var tables = new (string Table, string Label)[]
            {
                ("results", "Chrome-like history"),
                ("firefox_results", "Firefox history"),
                ("chrome_downloads", "Chrome-like downloads"),
                ("firefox_downloads", "Firefox downloads"),
                ("bookmarks_Chrome", "Chrome-like bookmarks"),
                ("bookmarks_Firefox", "Firefox bookmarks"),
                ("autofill_data", "Autofill"),
                ("cookies_data", "Cookies"),
                ("cache_data", "Cache"),
                ("session_data", "Sessions"),
                ("extension_data", "Extensions"),
                ("saved_logins_data", "Saved logins"),
                ("local_storage_data", "Local Storage"),
                ("session_storage_data", "Session Storage"),
                ("indexeddb_data", "IndexedDB")
            };

            Console.WriteLine();
            Console.WriteLine("[Browser Reviewer - CLI]: Extraction summary");

            int total = 0;
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();

            foreach (var item in tables)
            {
                int count = CountRows(connection, item.Table);
                total += count;
                Console.WriteLine($"  {item.Label}: {count}");
            }

            Console.WriteLine($"  Total web artifacts: {total}");
            Console.WriteLine();
        }

        private static int CountRows(SQLiteConnection connection, string tableName)
        {
            using var command = new SQLiteCommand($"SELECT COUNT(*) FROM {tableName};", connection);
            object result = command.ExecuteScalar();
            return Convert.ToInt32(result);
        }

        private static bool HasHelpFlag(string[] args)
        {
            if (args == null || args.Length == 0)
                return false;

            foreach (var a in args)
            {
                if (string.Equals(a, "/?", StringComparison.OrdinalIgnoreCase)) return true;
                if (string.Equals(a, "-?", StringComparison.OrdinalIgnoreCase)) return true;
                if (string.Equals(a, "-h", StringComparison.OrdinalIgnoreCase)) return true;
                if (string.Equals(a, "--help", StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false;
        }

        private static string GetHelpText() => @"
Browser Reviewer v1.0 - CLI

Usage:
  br.exe <BaseNameOrPath(.bre)> <RootDirectoryToScan>
  br.exe --cli --out <BaseNameOrPath(.bre)> --scan <RootDirectoryToScan> [--overwrite]

Parameters:
  <BaseNameOrPath(.bre)>   Name or full path of the .bre database file to create.
                           If no extension is provided, .bre will be added automatically.
  <RootDirectoryToScan>    Root folder where browser artifacts will be searched.
  --overwrite              Replace an existing .bre file instead of stopping.

Extracted web artifacts:
  History, downloads, bookmarks, autofill, cookies, cache, sessions, extensions,
  saved logins metadata, local storage, session storage, and IndexedDB.

Examples:
  br.exe MyCase ""D:\Evidence\UserProfile""
  br.exe ""C:\Cases\Case123.bre"" ""E:\Mounts\Image01""
  br.exe --cli --out ""C:\Cases\Case123.bre"" --scan ""E:\Mounts\Image01"" --overwrite

Help flags:
  /?   -?   -h   --help
";

        private sealed class CliOptions
        {
            public string DatabasePath { get; set; } = string.Empty;

            public string ScanPath { get; set; } = string.Empty;

            public bool Overwrite { get; set; }
        }
    }

    internal static class ConsoleHelper
    {
        private const int ATTACH_PARENT_PROCESS = -1;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        public static void EnsureConsole()
        {
            if (GetConsoleWindow() != IntPtr.Zero)
                return;

            if (!AttachConsole(ATTACH_PARENT_PROCESS))
                return;

            var stdOut = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
            Console.SetOut(stdOut);

            var stdErr = new StreamWriter(Console.OpenStandardError()) { AutoFlush = true };
            Console.SetError(stdErr);

            var stdIn = new StreamReader(Console.OpenStandardInput());
            Console.SetIn(stdIn);
        }
    }
}
