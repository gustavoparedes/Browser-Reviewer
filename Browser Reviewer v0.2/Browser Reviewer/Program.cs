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
                // Ayuda (desde consola)
                if (HasHelpFlag(args))
                {
                    ConsoleHelper.EnsureConsole();
                    Console.WriteLine(GetHelpText());
                    Environment.Exit(0);
                    return;
                }

                // Modo CLI
                if (args != null && args.Length >= 2)
                {
                    ConsoleHelper.EnsureConsole();
                    int code = RunCli(args);
                    Environment.Exit(code);
                    return;
                }

                // Modo UI
                ApplicationConfiguration.Initialize();
                Application.Run(new Form1());
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
            string dbArg = args[0]?.Trim() ?? string.Empty;
            string rootPath = args[1]?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(dbArg))
            {
                Console.Error.WriteLine("[Browser Reviewer]: Error - database name/path cannot be empty.");
                Console.WriteLine(GetHelpText());
                return 2;
            }
            if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
            {
                Console.Error.WriteLine($"[Browser Reviewer]: Error - scan path does not exist: {rootPath}");

                Console.WriteLine(GetHelpText());
                return 3;
            }




            // Normaliza ruta y extensión .bre
            string dbFullPath = Path.IsPathRooted(dbArg) ? dbArg : Path.Combine(Environment.CurrentDirectory, dbArg);
            if (!dbFullPath.EndsWith(".bre", StringComparison.OrdinalIgnoreCase)) dbFullPath += ".bre";

            try
            {
                // 1) Validar que la ruta sea convertible a ruta absoluta (detecta caracteres inválidos también)
                dbFullPath = Path.GetFullPath(dbFullPath);

                // 2) Asegurar que el directorio existe (o crearlo)
                string? targetDir = Path.GetDirectoryName(dbFullPath);
                if (string.IsNullOrWhiteSpace(targetDir))
                    targetDir = Environment.CurrentDirectory;

                Directory.CreateDirectory(targetDir); // no falla si ya existe

                // 3) Verificar permiso de escritura creando un archivo temporal (y borrándolo)
                string testPath = dbFullPath + ".writecheck.tmp";
                using (var fs = new FileStream(testPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    // escribir 1 byte para confirmar escritura
                    fs.WriteByte(0x00);
                }
                File.Delete(testPath);
            }
            catch (UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"[Browser Reviewer]: Error - no write permission at destination: {dbFullPath}");

                return 4;
            }
            catch (PathTooLongException)
            {
                Console.Error.WriteLine($"[Browser Reviewer]: Error - path is too long: {dbFullPath}");

                return 5;
            }
            catch (IOException ioex)
            {
                Console.Error.WriteLine($"[Browser Reviewer]: I/O error while validating the output file: {dbFullPath}");

                Console.Error.WriteLine(ioex.Message);
                return 6;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Browser Reviewer]: Error - invalid output file path: {dbFullPath}");
                Console.Error.WriteLine(ex.Message);
                return 7;
            }

            // Construir connection string después de validar que podemos escribir ahí
            string connStr = $"Data Source={dbFullPath};Version=3;";

            // Crear DB .bre
            var tools = new MyTools();
            Helpers.db_name = dbFullPath;                   // si lo usas para logs, etc.
            Helpers.chromeViewerConnectionString = connStr; // CreateDatabase espera connection string
            tools.CreateDatabase(Helpers.chromeViewerConnectionString);
            Console.WriteLine("[Browser Reviewer - CLI]: Starting extraction...");
            Console.WriteLine("[Browser Reviewer - [CLI] Base: " + dbFullPath);
            Console.WriteLine("[Browser Reviewer - [CLI] Root: " + rootPath);

            try
            {
                // Si tu método es async Task, bloqueamos de forma segura:
                tools.CLI_ListFilesAndDirectories(rootPath).GetAwaiter().GetResult();
                Console.WriteLine("[Browser Reviewer - CLI]: Finished successfully.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[Browser Reviewer - CLI]: Error - " + ex.Message);
                Console.Error.WriteLine(ex);
                return 10;
            }
        }

        private static bool HasHelpFlag(string[] args)
        {
            if (args == null || args.Length == 0) return false;
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
                    Browser Reviewer v0.2 - CLI

                    Usage:
                      Browser_Reviewer.exe <BaseNameOrPath(.bre)> <RootDirectoryToScan>

                    Parameters:
                      <BaseNameOrPath(.bre)>   Name or full path of the .bre database file to create.
                                               If no extension is provided, .bre will be added automatically.
                      <RootDirectoryToScan>    Root folder where browser artifacts will be searched.

                    Examples:
                      Browser_Reviewer.exe MyCase ""D:\Evidence\UserProfile""
                      Browser_Reviewer.exe ""C:\Cases\Case123.bre"" ""E:\Mounts\Image01""

                    Help flags:
                      /?   -?   -h   --help
";
    }

        /// <summary>
        /// Adjunta el proceso a la consola del padre (cmd/powershell) si el binario es tipo Windows.
        /// </summary>
        internal static class ConsoleHelper
    {
        private const int ATTACH_PARENT_PROCESS = -1;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        public static void EnsureConsole()
        {
            if (GetConsoleWindow() != IntPtr.Zero) return;      // ya hay consola
            if (!AttachConsole(ATTACH_PARENT_PROCESS)) return;  // no pudo adjuntar

            // Re-enlazar streams para que Console.* funcione
            var stdOut = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
            Console.SetOut(stdOut);

            var stdErr = new StreamWriter(Console.OpenStandardError()) { AutoFlush = true };
            Console.SetError(stdErr);

            var stdIn = new StreamReader(Console.OpenStandardInput());
            Console.SetIn(stdIn);
        }
    }
}
