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
using Syncfusion.WinForms.GridCommon.ScrollAxis;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System;
//using System.Data.Entity.Infrastructure;




namespace Browser_Reviewer
{

   

    public class MyTools
    {

        // Estructura WIN32_FIND_DATA utilizada para almacenar información sobre archivos y carpetas.
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

        // Importamos las funciones de bajo nivel de la API de Windows.
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr FindFirstFile(string lpFileName, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool FindClose(IntPtr hFindFile);

        // Constante para identificar un directorio
        const uint FILE_ATTRIBUTE_DIRECTORY = 0x10;





            




           

            // Definir un único StreamWriter para mantener el archivo abierto durante toda la ejecución
            private static StreamWriter logWriter;

            public async Task ListFilesAndDirectories(string path, RichTextBox textBox_logConsole)
            {
                // Obtener el nombre del archivo de base de datos y crear el archivo de log con el mismo nombre, pero con extensión .log
                string dbNameWithoutExtension = Path.GetFileNameWithoutExtension(Helpers.db_name);
                string logFilePath = Path.Combine(Path.GetDirectoryName(Helpers.db_name), $"{dbNameWithoutExtension}.log");

                // Abrir el archivo de log una vez y mantenerlo abierto
                if (logWriter == null)
                {
                    logWriter = new StreamWriter(logFilePath, true); // true para añadir al log si ya existe
                }

                try
                {
                    await Task.Run(async () =>
                    {
                        try
                        {
                            WIN32_FIND_DATA findData;
                            IntPtr hFind = FindFirstFile(path + @"\*", out findData); // Se busca el primer archivo o directorio en la ruta especificada.

                            //if (hFind != IntPtr.Zero)
                            if (hFind != new IntPtr(-1)) // INVALID_HANDLE_VALUE
                            {
                                do
                                {
                                    string currentFileName = findData.cFileName;

                                    // Ignoramos los directorios especiales "." y "..".
                                    if (currentFileName != "." && currentFileName != "..")
                                    {
                                        string fullPath = Path.Combine(path, currentFileName);

                                        // Escribir en el log directamente
                                        await WriteToLog(fullPath);

                                        // Verificamos si el archivo es un directorio
                                        bool isDirectory = (findData.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) != 0;

                                        if (isDirectory)
                                        {
                                            // Llamada recursiva para procesar subdirectorios
                                            await ListFilesAndDirectories(fullPath, textBox_logConsole);
                                        }
                                        else
                                        {
                                            try
                                            {
                                               
                                                //Procesar cache de chrome, no terminado aun, se comenta empezando aqui

                                                //if (Path.GetFileName(fullPath) == "index" && fullPath.Contains(@"Cache\Cache_Data"))
                                                //{
                                                //    LogToConsole(textBox_logConsole, "Archivo Index de Cache...");
                                                //    // El archivo es el archivo de índice y está en el directorio Cache\Cache_Data
                                                //    ProcessChromeCacheIndex(fullPath, textBox_logConsole);
                                                //}

                                                //Procesar cache de chrome, no terminado aun, se comenta finalizando aqui



                                                //Historial de Chrome
                                                // Procesar archivos normalmente
                                                if (Path.GetFileName(fullPath) == "History" && IsSQLite3(fullPath, textBox_logConsole))
                                                {
                                                    if (SetBrowserType(fullPath))
                                                    {
                                                        Helpers.historyConnectionString = $"Data Source={fullPath};Version=3;";
                                                        ProcessAndWriteRecordsChromeLike(Helpers.historyConnectionString, Helpers.chromeViewerConnectionString, Helpers.BrowserType, fullPath, textBox_logConsole);
                                                        ProcessAndWriteDownloadsChrome(Helpers.historyConnectionString, Helpers.chromeViewerConnectionString, Helpers.BrowserType, fullPath, textBox_logConsole);
                                                        LogToConsole(textBox_logConsole, $"Processing : {fullPath}", Color.Green);

                                                        string directoryPath = Path.GetDirectoryName(fullPath);
                                                        string bookmarksFilePath = Path.Combine(directoryPath, "Bookmarks");

                                                        if (File.Exists(bookmarksFilePath))
                                                        {
                                                            LogToConsole(textBox_logConsole, $"Found Bookmarks file: {bookmarksFilePath}", Color.Green);
                                                            ProcessAndWriteBookmarksChrome(Helpers.chromeViewerConnectionString, Helpers.BrowserType, bookmarksFilePath, textBox_logConsole);
                                                        }
                                                        else
                                                        {
                                                            LogToConsole(textBox_logConsole, "Bookmarks file not found in the same directory.", Color.Red);
                                                        }

                                                        ProcessAndWriteAutofillChrome(Helpers.chromeViewerConnectionString, Helpers.BrowserType, fullPath, textBox_logConsole);
                                                    }
                                                }

                                                // Procesar normalmente los archivos places.sqlite y formhistory.sqlite
                                                if ((Path.GetFileName(fullPath) == "places.sqlite" || Path.GetFileName(fullPath) == "formhistory.sqlite") && IsSQLite3(fullPath, textBox_logConsole))
                                                {
                                                    if (SetBrowserType(fullPath))
                                                    {
                                                        Helpers.historyConnectionString = $"Data Source={fullPath};Version=3;";
                                                        ProcessAndWriteRecordsFirefox(Helpers.historyConnectionString, Helpers.chromeViewerConnectionString, Helpers.BrowserType, fullPath, textBox_logConsole);
                                                        ProcessAndWriteDownloadsFirefox(Helpers.historyConnectionString, Helpers.chromeViewerConnectionString, Helpers.BrowserType, fullPath, textBox_logConsole);
                                                        ProcessAndWriteBookmarksFirefox(Helpers.historyConnectionString, Helpers.chromeViewerConnectionString, Helpers.BrowserType, fullPath, textBox_logConsole);
                                                        ProcessAndWriteAutofillFirefox(Helpers.chromeViewerConnectionString, Helpers.BrowserType, fullPath, textBox_logConsole);
                                                        LogToConsole(textBox_logConsole, $"Processing : {fullPath}", Color.Green);
                                                    }
                                                }

                                                //if (Path.GetFileName(fullPath) == "WebCacheV01.dat")
                                                //{
                                                //    LogToConsole(textBox_logConsole, $"WebCacheV01.dat file: {fullPath}");
                                                //}

                                                


                                            }
                                            catch (UnauthorizedAccessException)
                                            {
                                                LogToConsole(textBox_logConsole, $"Access denied to the file: {fullPath}", Color.Red);
                                            }
                                            catch (PathTooLongException)
                                            {
                                                LogToConsole(textBox_logConsole, $"Path too long: {fullPath}", Color.Red);
                                            }
                                            catch (Exception ex)
                                            {
                                                LogToConsole(textBox_logConsole, $"Error processing the file {fullPath}: {ex.Message}", Color.Red);

                                                // Si ocurre un error, copiar places.sqlite y formhistory.sqlite al directorio temporal
                                                string directoryPath = Path.GetDirectoryName(fullPath);
                                                Helpers.realFirefoxFormPath = Path.Combine(directoryPath, "formhistory.sqlite");
                                                Helpers.realFirefoxPlacesPath = fullPath;

                                                CopyAndProcessToTemp(directoryPath, textBox_logConsole);
                                            }
                                        }
                                    }
                                }
                                while (FindNextFile(hFind, out findData)); // Buscamos el siguiente archivo o directorio.

                                FindClose(hFind); // Cerramos el handle de búsqueda.
                                
                            }
                            else
                            {
                                textBox_logConsole.Invoke((Action)(() =>
                                {
                                    LogToConsole(textBox_logConsole, "The directory could not be found or there was an error.",Color.Red);
                                }));
                            }
                        }
                        catch (Exception ex)
                        {
                            textBox_logConsole.Invoke((Action)(() =>
                            {
                                LogToConsole(textBox_logConsole, $"Error processing the directory: {ex.Message}", Color.Red);
                            }));
                        }
                    });
                }
                catch (Exception ex)
                {
                    LogToConsole(textBox_logConsole, $"General error in the task: {ex.Message}", Color.Red);
                }
            }


        //Funcion para uso en CLI
        public async Task CLI_ListFilesAndDirectories(string path)
        {
            try
            {
                // Enumeración con Win32
                WIN32_FIND_DATA findData;
                IntPtr hFind = FindFirstFile(Path.Combine(path, "*"), out findData);



                //Para comprobar error usa == new IntPtr(-1).

                //Para comprobar éxito usa != new IntPtr(-1).
                // Ojo: hay que comparar contra INVALID_HANDLE_VALUE (-1)
                if (hFind == new IntPtr(-1))
                {
                    Console.WriteLine($"The directory \"{path}\" could not be found or there was an error.");
                    return;
                }

                try
                {
                    do
                    {
                        string currentFileName = findData.cFileName;

                        // Ignorar "." y ".."
                        if (currentFileName == "." || currentFileName == "..")
                            continue;

                        string fullPath = Path.Combine(path, currentFileName);
                        //Console.WriteLine($"Processing : {fullPath}");

                        bool isDirectory = (findData.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) != 0;

                        if (isDirectory)
                        {
                            // Recursivo
                            await CLI_ListFilesAndDirectories(fullPath);
                        }
                        else
                        {
                            try
                            {
                                // === Chrome-like (History) ===
                                if (Path.GetFileName(fullPath) == "History" && CLI_IsSQLite3(fullPath))
                                {
                                    if (SetBrowserType(fullPath))
                                    {
                                        Helpers.historyConnectionString = $"Data Source={fullPath};Version=3;";
                                        ProcessAndWriteRecordsChromeLike(Helpers.historyConnectionString, Helpers.chromeViewerConnectionString, Helpers.BrowserType, fullPath);
                                        ProcessAndWriteDownloadsChrome(Helpers.historyConnectionString, Helpers.chromeViewerConnectionString, Helpers.BrowserType, fullPath);

                                        string directoryPath = Path.GetDirectoryName(fullPath)!;
                                        string bookmarksFilePath = Path.Combine(directoryPath, "Bookmarks");

                                        if (File.Exists(bookmarksFilePath))
                                        {
                                            Console.WriteLine($"Found Bookmarks file: {bookmarksFilePath}");
                                            ProcessAndWriteBookmarksChrome(Helpers.chromeViewerConnectionString, Helpers.BrowserType, bookmarksFilePath);
                                        }
                                        else
                                        {
                                            Console.WriteLine("Bookmarks file not found in the same directory.");
                                        }

                                        ProcessAndWriteAutofillChrome(Helpers.chromeViewerConnectionString, Helpers.BrowserType, fullPath);
                                    }
                                }

                                // === Firefox-like (places.sqlite, formhistory.sqlite) ===
                                if ((Path.GetFileName(fullPath) == "places.sqlite" || Path.GetFileName(fullPath) == "formhistory.sqlite") && CLI_IsSQLite3(fullPath))
                                {
                                    if (SetBrowserType(fullPath))
                                    {
                                        Helpers.historyConnectionString = $"Data Source={fullPath};Version=3;";
                                        ProcessAndWriteRecordsFirefox(Helpers.historyConnectionString, Helpers.chromeViewerConnectionString, Helpers.BrowserType, fullPath);
                                        ProcessAndWriteDownloadsFirefox(Helpers.historyConnectionString, Helpers.chromeViewerConnectionString, Helpers.BrowserType, fullPath);
                                        ProcessAndWriteBookmarksFirefox(Helpers.historyConnectionString, Helpers.chromeViewerConnectionString, Helpers.BrowserType, fullPath);
                                        ProcessAndWriteAutofillFirefox(Helpers.chromeViewerConnectionString, Helpers.BrowserType, fullPath);
                                        //Console.WriteLine($"Processing : {fullPath}");
                                    }
                                }

                                // === IE/Edge legacy cache ===
                                //if (Path.GetFileName(fullPath) == "WebCacheV01.dat")
                                //{
                                //    Console.WriteLine($"WebCacheV01.dat file: {fullPath}");
                                //}
                            }
                            catch (UnauthorizedAccessException)
                            {
                                Console.WriteLine($"Access denied to the file: {fullPath}");
                            }
                            catch (PathTooLongException)
                            {
                                Console.WriteLine($"Path too long: {fullPath}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error processing the file {fullPath}: {ex.Message}");

                                // Estrategia de copia a temp (Firefox)
                                string directoryPath = Path.GetDirectoryName(fullPath)!;
                                Helpers.realFirefoxFormPath = Path.Combine(directoryPath, "formhistory.sqlite");
                                Helpers.realFirefoxPlacesPath = fullPath;
                                CLI_CopyAndProcessToTemp(directoryPath);
                            }
                        }
                    }
                    while (FindNextFile(hFind, out findData));
                }
                finally
                {
                    FindClose(hFind);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing the directory: {ex.Message}");
            }
        }


        // Función para escribir en el log
        private async Task WriteToLog(string fullPath)
            {
                try
                {
                    logWriter.WriteLine(fullPath);
                    await logWriter.FlushAsync(); // Forzar la escritura inmediata
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error writing to log: {ex.Message}");
                }
            }

            // Función para cerrar el archivo de log cuando termine todo el procesamiento
            public static void CloseLog()
            {
                if (logWriter != null)
                {
                    logWriter.Close();
                    logWriter = null;
                }
            }






        //private void ProcessChromeCacheIndex(string indexFilePath, TextBox textBox_logConsole)
        //{

        //    LogToConsole(textBox_logConsole, "Entrando en la funcion ProcessChromeIndex");
        //    try
        //    {
        //        // Abrir el archivo de índice para lectura
        //        using (FileStream fs = new FileStream(indexFilePath, FileMode.Open, FileAccess.Read))
        //        using (BinaryReader reader = new BinaryReader(fs))
        //        {
        //            // Leer cabecera del archivo de índice
        //            // La estructura exacta de la cabecera dependerá de la versión del caché
        //            int headerSize = reader.ReadInt32(); // Leer el tamaño del encabezado
        //            int tableSize = reader.ReadInt32(); // Leer el tamaño de la tabla hash

        //            // Leer entradas del índice
        //            for (int i = 0; i < 10; i++)
        //            {
        //                // Leer las entradas del índice
        //                long urlOffset = reader.ReadInt64(); // Offset hacia la URL en el archivo de índice
        //                long dataOffset = reader.ReadInt64(); // Offset hacia los datos en los archivos data_#

        //                // Aquí se puede agregar más lógica para extraer la URL y buscar los datos
        //                LogToConsole(textBox_logConsole, $"Entry found: URL offset {urlOffset}, Data offset {dataOffset}");

        //                // Procesar los datos en el offset del archivo `data_#`
        //                // Aquí necesitarás leer los archivos `data_#` basados en este offset
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        LogToConsole(textBox_logConsole, $"Error reading cache index file {indexFilePath}: {ex.Message}");
        //    }
        //}

        private void ProcessChromeCacheIndex(string indexFilePath, RichTextBox textBox_logConsole)
        {
            LogToConsole(textBox_logConsole, "Entrando en la función ProcessChromeCacheIndex");
            try
            {
                // Abrir el archivo de índice para lectura
                using (FileStream fs = new FileStream(indexFilePath, FileMode.Open, FileAccess.Read))
                using (BinaryReader reader = new BinaryReader(fs))
                {
                    // Leer cabecera del archivo de índice
                    int headerSize = reader.ReadInt32(); // Leer el tamaño del encabezado
                    int tableSize = reader.ReadInt32();  // Leer el tamaño de la tabla hash

                    // Leer las primeras 10 entradas del índice (puedes ajustar este número según sea necesario)
                    for (int i = 0; i < 10; i++)
                    {
                        long urlOffset = reader.ReadInt64();  // Offset hacia la URL en el archivo de índice
                        long dataOffset = reader.ReadInt64(); // Offset hacia los datos en los archivos data_#

                        // Extraer la URL desde el archivo de índice utilizando el urlOffset
                        string url = ReadUrlAtOffset(reader, urlOffset);

                        // Imprimir la URL y el dataOffset en el log
                        LogToConsole(textBox_logConsole, $"URL: {url}, Data offset: {dataOffset}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogToConsole(textBox_logConsole, $"Error leyendo el archivo de índice: {ex.Message}");
            }
        }

        // Función auxiliar para leer la URL en una ubicación específica del archivo
        private string ReadUrlAtOffset(BinaryReader reader, long urlOffset)
        {
            reader.BaseStream.Seek(urlOffset, SeekOrigin.Begin);  // Ir al offset de la URL
            List<byte> urlBytes = new List<byte>();

            // Leer la URL byte a byte hasta encontrar un byte nulo (0x00) que indica el final de la cadena
            byte b;
            while ((b = reader.ReadByte()) != 0x00)
            {
                urlBytes.Add(b);
            }

            // Convertir los bytes de la URL a una cadena
            return Encoding.UTF8.GetString(urlBytes.ToArray());
        }







        // Método para copiar los archivos places.sqlite y formhistory.sqlite al directorio temporal y procesarlos solo si ocurre un error
        private void CopyAndProcessToTemp(string directoryPath, RichTextBox textBox_logConsole)
        {
            try
            {
                // Definir las rutas de los archivos en el directorio original
                string placesPath = Path.Combine(directoryPath, "places.sqlite");
                string formHistoryPath = Path.Combine(directoryPath, "formhistory.sqlite");

                // Obtener el directorio temporal
                string tempDir = Path.GetTempPath();
                string tempPlacesPath = Path.Combine(tempDir, "places.sqlite");
                string tempFormHistoryPath = Path.Combine(tempDir, "formhistory.sqlite");

                // Copiar places.sqlite si existe
                if (File.Exists(placesPath))
                {
                    File.Copy(placesPath, tempPlacesPath, true);
                    LogToConsole(textBox_logConsole, $"places.sqlite copied to temporary location: {tempPlacesPath}");
                }

                // Copiar formhistory.sqlite si existe
                if (File.Exists(formHistoryPath))
                {
                    File.Copy(formHistoryPath, tempFormHistoryPath, true);
                    LogToConsole(textBox_logConsole, $"formhistory.sqlite copied to temporary location: {tempFormHistoryPath}");
                }

                // Procesar los archivos desde la ubicación temporal si fueron copiados solo places.sqlite, formhistory mas abajo
                if (File.Exists(tempPlacesPath))
                {
                    Helpers.historyConnectionString = $"Data Source={tempPlacesPath};Version=3;";
                    ProcessAndWriteRecordsFirefox(Helpers.historyConnectionString, Helpers.chromeViewerConnectionString, Helpers.BrowserType, tempPlacesPath);
                    ProcessAndWriteDownloadsFirefox(Helpers.historyConnectionString, Helpers.chromeViewerConnectionString, Helpers.BrowserType, tempPlacesPath);
                    ProcessAndWriteBookmarksFirefox(Helpers.historyConnectionString, Helpers.chromeViewerConnectionString, Helpers.BrowserType, tempPlacesPath);
                    //ProcessAndWriteAutofillFirefox(Helpers.chromeViewerConnectionString, Helpers.BrowserType, tempPlacesPath);
                    LogToConsole(textBox_logConsole, $"Processing places.sqlite from temporary location: {tempPlacesPath}");
                    //Helpers.realFirefoxPlacesPath = "";
                }

                if (File.Exists(tempFormHistoryPath))
                {
                    // Procesar formhistory.sqlite
                    LogToConsole(textBox_logConsole, $"Processing formhistory.sqlite from temporary location: {tempFormHistoryPath}");
                    ProcessAndWriteAutofillFirefox(Helpers.chromeViewerConnectionString, Helpers.BrowserType, tempFormHistoryPath);
                    //Helpers.realFirefoxFormPath = "";
                }
            }
            catch (Exception copyEx)
            {
                LogToConsole(textBox_logConsole, $"Error copying files to temporary location: {copyEx.Message}");
            }
        }

        private void CLI_CopyAndProcessToTemp(string directoryPath)
        {
            try
            {
                // Definir las rutas de los archivos en el directorio original
                string placesPath = Path.Combine(directoryPath, "places.sqlite");
                string formHistoryPath = Path.Combine(directoryPath, "formhistory.sqlite");

                // Obtener el directorio temporal
                string tempDir = Path.GetTempPath();
                string tempPlacesPath = Path.Combine(tempDir, "places.sqlite");
                string tempFormHistoryPath = Path.Combine(tempDir, "formhistory.sqlite");

                // Copiar places.sqlite si existe
                if (File.Exists(placesPath))
                {
                    File.Copy(placesPath, tempPlacesPath, true);
                    Console.WriteLine($"places.sqlite copied to temporary location: {tempPlacesPath}");
                }

                // Copiar formhistory.sqlite si existe
                if (File.Exists(formHistoryPath))
                {
                    File.Copy(formHistoryPath, tempFormHistoryPath, true);
                    Console.WriteLine($"formhistory.sqlite copied to temporary location: {tempFormHistoryPath}");
                }

                // Procesar los archivos desde la ubicación temporal si fueron copiados solo places.sqlite, formhistory mas abajo
                if (File.Exists(tempPlacesPath))
                {
                    Helpers.historyConnectionString = $"Data Source={tempPlacesPath};Version=3;";
                    ProcessAndWriteRecordsFirefox(Helpers.historyConnectionString, Helpers.chromeViewerConnectionString, Helpers.BrowserType, tempPlacesPath);
                    ProcessAndWriteDownloadsFirefox(Helpers.historyConnectionString, Helpers.chromeViewerConnectionString, Helpers.BrowserType, tempPlacesPath);
                    ProcessAndWriteBookmarksFirefox(Helpers.historyConnectionString, Helpers.chromeViewerConnectionString, Helpers.BrowserType, tempPlacesPath);
                    //ProcessAndWriteAutofillFirefox(Helpers.chromeViewerConnectionString, Helpers.BrowserType, tempPlacesPath);
                    Console.WriteLine($"Processing places.sqlite from temporary location: {tempPlacesPath}");
                    //Helpers.realFirefoxPlacesPath = "";
                }

                if (File.Exists(tempFormHistoryPath))
                {
                    // Procesar formhistory.sqlite
                    Console.WriteLine($"Processing formhistory.sqlite from temporary location: {tempFormHistoryPath}");
                    ProcessAndWriteAutofillFirefox(Helpers.chromeViewerConnectionString, Helpers.BrowserType, tempFormHistoryPath);
                    //Helpers.realFirefoxFormPath = "";
                }
            }
            catch (Exception copyEx)
            {
                Console.WriteLine($"Error copying files to temporary location: {copyEx.Message}");
            }
        }




        private void PrintFilePermissions(string filePath, RichTextBox textBox_logConsole)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(filePath); // Usamos FileInfo en lugar de File
                FileSecurity fileSecurity = fileInfo.GetAccessControl(); // Aquí usamos GetAccessControl con FileInfo
                AuthorizationRuleCollection acl = fileSecurity.GetAccessRules(true, true, typeof(NTAccount));

                textBox_logConsole.Invoke((Action)(() =>
                {
                    LogToConsole(textBox_logConsole, $"Permissions for the file: {filePath}");
                }));

                foreach (FileSystemAccessRule rule in acl)
                {
                    textBox_logConsole.Invoke((Action)(() =>
                    {
                        LogToConsole(textBox_logConsole, $"  User/Group: {rule.IdentityReference.Value}");
                        LogToConsole(textBox_logConsole, $"  Permissions: {rule.FileSystemRights}");
                        LogToConsole(textBox_logConsole, $"  Access Type: {rule.AccessControlType}");
                        LogToConsole(textBox_logConsole, $"  Inherited: {rule.IsInherited}");
                    }));
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                textBox_logConsole.Invoke((Action)(() =>
                {
                    LogToConsole(textBox_logConsole, $"Cannot access the permissions of the file {filePath}: {ex.Message}");
                }));
            }
            catch (Exception ex)
            {
                textBox_logConsole.Invoke((Action)(() =>
                {
                    LogToConsole(textBox_logConsole, $"Error retrieving the permissions of {filePath}: {ex.Message}");
                }));
            }
        }


       
        public void ShowQueryOnDataGridView(SfDataGrid dataGrid, string connectionString, string query, Label labelItemCount, RichTextBox logConsole)
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
                            // Pasar el parámetro de búsqueda correctamente
                            if (Helpers.searchTermExists)
                            {
                                if (Helpers.searchTermRegExp)
                                {
                                    command.Parameters.AddWithValue("@searchTerm", Helpers.searchTerm); // REGEXP (sin %)
                                }
                                else
                                {
                                    command.Parameters.AddWithValue("@searchTerm", $"%{Helpers.searchTerm}%"); // LIKE (con %)
                                }
                            }

                            using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(command))
                            {
                                DataTable dataTable = new DataTable();
                                adapter.Fill(dataTable);

                                // Asignar datos al DataGrid
                                dataGrid.DataSource = dataTable;

                                // Actualizar el contador de elementos
                                Helpers.itemscount = dataGrid.RowCount - 1;
                                labelItemCount.Text = $"Items count: {dataGrid.RowCount - 1}"; // Restamos 1 para no contar la fila de encabezado.

                                // Asegurar que haya al menos una fila antes de establecer el foco
                                if (dataGrid.View != null && dataGrid.View.Records.Count > 0)
                                {
                                    dataGrid.SelectedIndex = 0; // Selecciona la primera fila
                                    dataGrid.MoveToCurrentCell(new RowColumnIndex(0, 0)); // Mueve el foco a la primera celda
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    //MessageBox.Show($"Error en la consulta:\n{ex.Message}", "ChromeViewer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    if (logConsole != null)
                        LogToConsole(logConsole, $"Query error: {ex.Message}", Color.Red);
                    else
                        Console.WriteLine($"Query error: {ex.Message}");

                }
            }

           
        }




        //Funcion que crea la base de datos para la aplicacion
        public void CreateDatabase(string connectionString)
        {

            //Tabla para resultados history de Chrome Like

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
                                                    Browser TEXT,
                                                    Category TEXT,
                                                    Potential_activity TEXT,
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

                //Tabla para resultados history de Firefox

                string createTableFirefox = @"CREATE TABLE IF NOT EXISTS firefox_results (
                                                     id INTEGER PRIMARY KEY AUTOINCREMENT,
                                                     Browser TEXT,
                                                     Category TEXT,
                                                     Potential_activity TEXT,
                                                     Visit_id INTEGER,
                                                     Place_id INTEGER,
                                                     Url TEXT NOT NULL,
                                                     Title TEXT,
                                                     Visit_time DATETIME,
                                                     Last_visit_time DATETIME,
                                                     Visit_count INTEGER,
                                                     Transition TEXT,
                                                     Visit_type INTEGER,   -- Campo adicional específico de Firefox
                                                     Frecency INTEGER,     -- Campo adicional específico de Firefox
                                                     File TEXT,
                                                     Label TEXT,
                                                     Comment TEXT,
                                                     FOREIGN KEY (Label) REFERENCES Labels(Label_name)
                                                     )";

                //Tabla para resultados downloads de Chrome Like
                string createTableChromeDownloads = @"CREATE TABLE IF NOT EXISTS chrome_downloads (
                                                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                                                    Browser TEXT,
                                                    Download_id INTEGER,
                                                    Current_path TEXT,
                                                    Target_path TEXT,
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

                //Tabla para resultados Downloads de Firefox
                string createTableFirefoxDownloads = @"CREATE TABLE IF NOT EXISTS firefox_downloads (
                                                     id INTEGER PRIMARY KEY AUTOINCREMENT,
                                                     Browser TEXT,
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

                //Tabla para resultados Bookmarks de Chrome Like

                string createTableChromeBookmarks = @"CREATE TABLE IF NOT EXISTS bookmarks_Chrome (
                                                    id INTEGER PRIMARY KEY AUTOINCREMENT,
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



                //Tabla para resultados Bookmarks de Firefox
                string createTableFirefoxBookmarks = @"CREATE TABLE IF NOT EXISTS bookmarks_Firefox (
                                                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                                                    Browser TEXT,
                                                    Bookmark_id INTEGER,
                                                    Type TEXT,
                                                    FK INTEGER,
                                                    Parent INTEGER,
                                                    Parent_name TEXT, -- Nuevo campo para el nombre de la carpeta padre
                                                    Title TEXT,
                                                    DateAdded DATETIME, -- Convertido a DATETIME
                                                    LastModified DATETIME, -- Convertido a DATETIME
                                                    URL TEXT,
                                                    PageTitle TEXT,
                                                    VisitCount INTEGER,
                                                    LastVisitDate DATETIME, -- Convertido a DATETIME
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

                

            }
        }

       
        public static void ProcessAndWriteDownloadsChrome(string historyConnectionString, string chromeViewerConnectionString, string browserType, string filePath, RichTextBox logConsole = null)
        {
            if (string.IsNullOrEmpty(historyConnectionString) || string.IsNullOrEmpty(chromeViewerConnectionString))
            {
                //MessageBox.Show("Provide valid database connection strings.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                    string query = @"SELECT id, current_path, target_path, start_time, end_time, received_bytes, total_bytes, 
                            state, opened, referrer, site_url, tab_url, mime_type FROM downloads";

                    using (SQLiteCommand command = new SQLiteCommand(query, downloadsConnection))
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        using (var transaction = resultsConnection.BeginTransaction())
                        {
                            while (reader.Read())
                            {
                                int downloadId = reader.GetInt32(0);
                                string currentPath = reader.GetString(1);
                                string targetPath = reader.GetString(2);
                                long startTimeMicroseconds = reader.GetInt64(3);
                                long endTimeMicroseconds = reader.GetInt64(4);
                                long receivedBytes = reader.GetInt64(5);
                                long totalBytes = reader.GetInt64(6);
                                int state = reader.GetInt32(7);
                                int opened = reader.GetInt32(8);
                                string referrer = reader.GetString(9);
                                string siteUrl = reader.GetString(10);
                                string tabUrl = reader.GetString(11);
                                string mimeType = reader.GetString(12);

                                // Convertir tiempos
                                DateTime epoch = new DateTime(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                                DateTime startTime = epoch.AddTicks(startTimeMicroseconds * 10);
                                DateTime endTime = epoch.AddTicks(endTimeMicroseconds * 10);

                                // Interpretar estado
                                string stateDescription = InterpretDownloadState(state);

                                // Interpretar opened (1 = "Yes", 0 = "No")
                                string openedDescription = opened == 1 ? "Yes" : "No";

                                // Insertar datos en la tabla chrome_downloads
                                string insertQuery = @"INSERT INTO chrome_downloads 
                                               (Browser, Download_id, Current_path, Target_path, Start_time, End_time, 
                                                Received_bytes, Total_bytes, State, opened, referrer, Site_url, Tab_url, 
                                                Mime_type, File)
                                               VALUES 
                                               (@Browser, @Download_id, @Current_path, @Target_path, @Start_time, @End_time, 
                                                @Received_bytes, @Total_bytes, @State, @opened, @referrer, @Site_url, 
                                                @Tab_url, @Mime_type, @File)";

                                using (SQLiteCommand insertCommand = new SQLiteCommand(insertQuery, resultsConnection))
                                {
                                    insertCommand.Parameters.AddWithValue("@Browser", browserType);
                                    insertCommand.Parameters.AddWithValue("@Download_id", downloadId);
                                    insertCommand.Parameters.AddWithValue("@Current_path", currentPath);
                                    insertCommand.Parameters.AddWithValue("@Target_path", targetPath);
                                    insertCommand.Parameters.AddWithValue("@Start_time", startTime.ToString("yyyy-MM-dd HH:mm:ss.ffffff"));
                                    insertCommand.Parameters.AddWithValue("@End_time", endTime.ToString("yyyy-MM-dd HH:mm:ss.ffffff"));
                                    insertCommand.Parameters.AddWithValue("@Received_bytes", receivedBytes);
                                    insertCommand.Parameters.AddWithValue("@Total_bytes", totalBytes);
                                    insertCommand.Parameters.AddWithValue("@State", stateDescription);
                                    insertCommand.Parameters.AddWithValue("@opened", openedDescription);
                                    insertCommand.Parameters.AddWithValue("@referrer", referrer);
                                    insertCommand.Parameters.AddWithValue("@Site_url", siteUrl);
                                    insertCommand.Parameters.AddWithValue("@Tab_url", tabUrl);
                                    insertCommand.Parameters.AddWithValue("@Mime_type", mimeType);
                                    insertCommand.Parameters.AddWithValue("@File", filePath);

                                    insertCommand.ExecuteNonQuery();
                                }
                            }

                            transaction.Commit();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"An error occurred while processing downloads: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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


        // Se debe incluir el campo opened
        //public static void ProcessAndWriteDownloadsChrome(string historyConnectionString, string chromeViewerConnectionString, string browserType, string filepath)
        //{
        //    if (!string.IsNullOrEmpty(historyConnectionString))
        //    {
        //        using (SQLiteConnection historyConnection = new SQLiteConnection(historyConnectionString))
        //        {
        //            historyConnection.Open();

        //            // Leer registros de downloads
        //            string queryDownloads = @"SELECT id, current_path, target_path, start_time, end_time, received_bytes, total_bytes, state, site_url, tab_url, mime_type 
        //                              FROM downloads";
        //            using (SQLiteCommand commandDownloads = new SQLiteCommand(queryDownloads, historyConnection))
        //            using (SQLiteDataReader readerDownloads = commandDownloads.ExecuteReader())
        //            {
        //                using (SQLiteConnection chromeViewerConnection = new SQLiteConnection(chromeViewerConnectionString))
        //                {
        //                    chromeViewerConnection.Open();
        //                    using (var transaction = chromeViewerConnection.BeginTransaction())  // Inicia la transacción
        //                    {
        //                        while (readerDownloads.Read())
        //                        {
        //                            int downloadId = readerDownloads.GetInt32(0);
        //                            string currentPath = readerDownloads.GetString(1);
        //                            string targetPath = readerDownloads.GetString(2);
        //                            long startTimeMicroseconds = readerDownloads.GetInt64(3);
        //                            long? endTimeMicroseconds = readerDownloads.IsDBNull(4) ? (long?)null : readerDownloads.GetInt64(4);
        //                            long receivedBytes = readerDownloads.GetInt64(5);
        //                            long totalBytes = readerDownloads.GetInt64(6);
        //                            int state = readerDownloads.GetInt32(7);
        //                            string siteUrl = readerDownloads.IsDBNull(8) ? null : readerDownloads.GetString(8);
        //                            string tabUrl = readerDownloads.IsDBNull(9) ? null : readerDownloads.GetString(9);
        //                            string mimeType = readerDownloads.IsDBNull(10) ? null : readerDownloads.GetString(10);

        //                            DateTime epoch = new DateTime(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        //                            DateTime startTime = epoch.AddTicks(startTimeMicroseconds * 10); // Convertir microsegundos a ticks
        //                            DateTime? endTime = endTimeMicroseconds.HasValue ? epoch.AddTicks(endTimeMicroseconds.Value * 10) : (DateTime?)null;

        //                            // Escribir el resultado en chrome_downloads
        //                            string insertDownloads = @"INSERT INTO chrome_downloads (Browser, Download_id, Current_path, Target_path, Start_time, End_time, Received_bytes, Total_bytes, State, Site_url, Tab_url, Mime_type, File)
        //                                               VALUES (@Browser, @Download_id, @Current_path, @Target_path, @Start_time, @End_time, @Received_bytes, @Total_bytes, @State, @Site_url, @Tab_url, @Mime_type, @File)";
        //                            using (SQLiteCommand commandInsertDownloads = new SQLiteCommand(insertDownloads, chromeViewerConnection))
        //                            {
        //                                commandInsertDownloads.Parameters.AddWithValue("@Browser", browserType);
        //                                commandInsertDownloads.Parameters.AddWithValue("@Download_id", downloadId);
        //                                commandInsertDownloads.Parameters.AddWithValue("@Current_path", currentPath);
        //                                commandInsertDownloads.Parameters.AddWithValue("@Target_path", targetPath);
        //                                commandInsertDownloads.Parameters.AddWithValue("@Start_time", startTime.ToString("yyyy-MM-dd HH:mm:ss.ffffff"));
        //                                commandInsertDownloads.Parameters.AddWithValue("@End_time", endTime.HasValue ? endTime.Value.ToString("yyyy-MM-dd HH:mm:ss.ffffff") : DBNull.Value.ToString());
        //                                commandInsertDownloads.Parameters.AddWithValue("@Received_bytes", receivedBytes);
        //                                commandInsertDownloads.Parameters.AddWithValue("@Total_bytes", totalBytes);
        //                                commandInsertDownloads.Parameters.AddWithValue("@State", state);
        //                                commandInsertDownloads.Parameters.AddWithValue("@Site_url", (object)siteUrl ?? DBNull.Value);
        //                                commandInsertDownloads.Parameters.AddWithValue("@Tab_url", (object)tabUrl ?? DBNull.Value);
        //                                commandInsertDownloads.Parameters.AddWithValue("@Mime_type", (object)mimeType ?? DBNull.Value);
        //                                commandInsertDownloads.Parameters.AddWithValue("@File", filepath);

        //                                commandInsertDownloads.ExecuteNonQuery();
        //                            }
        //                        }
        //                        transaction.Commit(); // Finaliza la transacción
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    else
        //    {
        //        MessageBox.Show("Select a Google Chrome history file first", "ChromeViewer", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //    }
        //}







        public static void ProcessAndWriteDownloadsFirefox(string historyConnectionString, string chromeViewerConnectionString, string browserType, string filepath, RichTextBox logConsole = null)
        {
            if (!string.IsNullOrEmpty(historyConnectionString))
            {
                using (SQLiteConnection historyConnection = new SQLiteConnection(historyConnectionString))
                {
                    historyConnection.Open();

                    // Leer registros relevantes de descargas en Firefox desde moz_annos
                    string queryDownloads = @"
                    SELECT 
                        ma.place_id,
                        mp.url, 
                        mp.title, 
                        mp.last_visit_date, 
                        ma.anno_attribute_id, 
                        ma.content 
                    FROM 
                        moz_annos ma 
                    JOIN 
                        moz_places mp ON ma.place_id = mp.id
                    WHERE 
                        ma.anno_attribute_id IN (1, 2)";

                    using (SQLiteCommand commandDownloads = new SQLiteCommand(queryDownloads, historyConnection))
                    using (SQLiteDataReader readerDownloads = commandDownloads.ExecuteReader())
                    {
                        using (SQLiteConnection chromeViewerConnection = new SQLiteConnection(chromeViewerConnectionString))
                        {
                            chromeViewerConnection.Open();
                            using (var transaction = chromeViewerConnection.BeginTransaction()) // Inicia la transacción
                            {
                                // Diccionario para agrupar información por place_id
                                var downloadData = new Dictionary<int, (string url, string title, long? lastVisitDate, string fileName, long? endTime, long? fileSize, string state)>();

                                while (readerDownloads.Read())
                                {
                                    int placeId = readerDownloads.GetInt32(0);
                                    string url = readerDownloads.IsDBNull(1) ? null : readerDownloads.GetString(1);
                                    string title = readerDownloads.IsDBNull(2) ? null : readerDownloads.GetString(2);
                                    long? lastVisitDate = readerDownloads.IsDBNull(3) ? (long?)null : readerDownloads.GetInt64(3);
                                    int annoAttributeId = readerDownloads.GetInt32(4);
                                    string content = readerDownloads.IsDBNull(5) ? null : readerDownloads.GetString(5);

                                    if (!downloadData.ContainsKey(placeId))
                                    {
                                        downloadData[placeId] = (url, title, lastVisitDate, null, null, null, null);
                                    }

                                    if (annoAttributeId == 1)
                                    {
                                        // Es el nombre del archivo
                                        downloadData[placeId] = (url, title, lastVisitDate, content, downloadData[placeId].endTime, downloadData[placeId].fileSize, downloadData[placeId].state);
                                    }
                                    else if (annoAttributeId == 2)
                                    {
                                        // Es el JSON con los detalles de la descarga
                                        var downloadDetails = System.Text.Json.JsonDocument.Parse(content).RootElement;

                                        long? endTime = downloadDetails.TryGetProperty("endTime", out var endTimeElement) && endTimeElement.ValueKind == System.Text.Json.JsonValueKind.Number
                                            ? endTimeElement.GetInt64()
                                            : (long?)null;

                                        long? fileSize = downloadDetails.TryGetProperty("fileSize", out var fileSizeElement) && fileSizeElement.ValueKind == System.Text.Json.JsonValueKind.Number
                                            ? fileSizeElement.GetInt64()
                                            : (long?)null;

                                        string state = downloadDetails.TryGetProperty("state", out var stateElement) && stateElement.ValueKind == System.Text.Json.JsonValueKind.Number
                                            ? ConvertStateToDescription(stateElement.GetInt32())
                                            : "Unknown";

                                        downloadData[placeId] = (url, title, lastVisitDate, downloadData[placeId].fileName, endTime, fileSize, state);
                                    }
                                }

                                // Insertar los datos procesados en la tabla firefox_downloads
                                foreach (var data in downloadData)
                                {
                                    int placeId = data.Key;
                                    var (url, title, lastVisitDateUnix, fileName, endTimeUnix, fileSize, state) = data.Value;

                                    DateTime? lastVisitDate = lastVisitDateUnix.HasValue
                                        ? (DateTime?)new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(lastVisitDateUnix.Value / 1000)
                                        : null;

                                    if (endTimeUnix.HasValue)
                                    {
                                        // Convertir de Unix time (milisegundos desde 1970) a DateTime
                                        DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                                        DateTime endTime = epoch.AddMilliseconds(endTimeUnix.Value);

                                        string insertDownloads = @"INSERT INTO firefox_downloads 
                                                        (Browser, Download_id, Current_path, End_time, Last_visit_time, Received_bytes, Total_bytes, Source_url, Title, State, File)
                                                        VALUES 
                                                        (@Browser, @Download_id, @Current_path, @End_time, @Last_visit_time, @Received_bytes, @Total_bytes, @Source_url, @Title, @State, @File)";

                                        using (SQLiteCommand commandInsertDownloads = new SQLiteCommand(insertDownloads, chromeViewerConnection))
                                        {
                                            commandInsertDownloads.Parameters.AddWithValue("@Browser", browserType);
                                            commandInsertDownloads.Parameters.AddWithValue("@Download_id", placeId);
                                            commandInsertDownloads.Parameters.AddWithValue("@Current_path", fileName);
                                            commandInsertDownloads.Parameters.AddWithValue("@End_time", endTime.ToString("yyyy-MM-dd HH:mm:ss.ffffff"));
                                            commandInsertDownloads.Parameters.AddWithValue("@Last_visit_time", lastVisitDate.HasValue ? lastVisitDate.Value.ToString("yyyy-MM-dd HH:mm:ss.ffffff") : (object)DBNull.Value);
                                            commandInsertDownloads.Parameters.AddWithValue("@Received_bytes", fileSize.HasValue ? fileSize.Value : (object)DBNull.Value);
                                            commandInsertDownloads.Parameters.AddWithValue("@Total_bytes", fileSize.HasValue ? fileSize.Value : (object)DBNull.Value);
                                            commandInsertDownloads.Parameters.AddWithValue("@Source_url", url);
                                            commandInsertDownloads.Parameters.AddWithValue("@Title", title);
                                            commandInsertDownloads.Parameters.AddWithValue("@State", state);
                                            if (Helpers.realFirefoxPlacesPath != "")
                                            {
                                                commandInsertDownloads.Parameters.AddWithValue("@File", Helpers.realFirefoxPlacesPath);
                                            }
                                            else
                                            {
                                                commandInsertDownloads.Parameters.AddWithValue("@File", filepath);
                                            }                                          
                                            commandInsertDownloads.ExecuteNonQuery();
                                        }
                                    }
                                }
                                transaction.Commit(); // Finaliza la transacción
                            }
                        }
                    }
                }
            }
            else
            {
                //MessageBox.Show("Select a Firefox history file first", "ChromeViewer", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                1 => "Completed",      // Ajustado para que 1 sea "Completed"
                2 => "Paused",
                3 => "Canceled",       // Ajustado para que 3 sea "Canceled"
                4 => "In Progress",    // Cambiado de "Canceled" a "In Progress"
                5 => "Failed",
                _ => "Unknown"
            };
        }

       


        public static void ProcessAndWriteBookmarksFirefox(string historyConnectionString, string chromeViewerConnectionString, string browserType, string filepath, RichTextBox logConsole = null)
        {
            if (!string.IsNullOrEmpty(historyConnectionString))
            {
                using (SQLiteConnection historyConnection = new SQLiteConnection(historyConnectionString))
                {
                    historyConnection.Open();

                    // Leer registros relevantes de marcadores en Firefox
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
                            using (var transaction = chromeViewerConnection.BeginTransaction()) // Inicia la transacción
                            {
                                // Insertar los datos procesados en la tabla bookmarks_Firefox
                                while (readerBookmarks.Read())
                                {
                                    int bookmarkId = readerBookmarks.GetInt32(0);
                                    int bookmarkTypeNumeric = readerBookmarks.GetInt32(1);
                                    string bookmarkType = bookmarkTypeNumeric switch
                                    {
                                        1 => "Bookmark",   // 1 es para Bookmarks
                                        2 => "Folder",     // 2 es para Carpetas
                                        3 => "Separator",  // 3 es para Separadores
                                        _ => "Unknown"
                                    };

                                    int? fk = readerBookmarks.IsDBNull(2) ? (int?)null : readerBookmarks.GetInt32(2);
                                    int parent = readerBookmarks.GetInt32(3);
                                    string bookmarkTitle = readerBookmarks.IsDBNull(4) ? null : readerBookmarks.GetString(4);

                                    // Convertir Unix time a DateTime de manera similar a ProcessAndWriteDownloadsFirefox
                                    DateTime? dateAdded = readerBookmarks.IsDBNull(5) ? (DateTime?)null : new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(readerBookmarks.GetInt64(5) / 1000);
                                    DateTime? lastModified = readerBookmarks.IsDBNull(6) ? (DateTime?)null : new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(readerBookmarks.GetInt64(6) / 1000);
                                    string bookmarkUrl = readerBookmarks.IsDBNull(7) ? null : readerBookmarks.GetString(7);
                                    string pageTitle = readerBookmarks.IsDBNull(8) ? null : readerBookmarks.GetString(8);
                                    int? visitCount = readerBookmarks.IsDBNull(9) ? (int?)null : readerBookmarks.GetInt32(9);
                                    DateTime? lastVisitDate = readerBookmarks.IsDBNull(10) ? (DateTime?)null : new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(readerBookmarks.GetInt64(10) / 1000);

                                    string parentName = readerBookmarks.IsDBNull(11) ? null : readerBookmarks.GetString(11);

                                    int? annoId = readerBookmarks.IsDBNull(12) ? (int?)null : readerBookmarks.GetInt32(12);
                                    string annoContent = readerBookmarks.IsDBNull(13) ? null : readerBookmarks.GetString(13);
                                    string annoName = readerBookmarks.IsDBNull(14) ? null : readerBookmarks.GetString(14);

                                    string insertBookmarks = @"INSERT INTO bookmarks_Firefox 
                                (Browser, Bookmark_id, Type, FK, Parent, Parent_name, Title, DateAdded, LastModified, URL, PageTitle, VisitCount, LastVisitDate, AnnoId, AnnoContent, AnnoName, File)
                            VALUES 
                                (@Browser, @Bookmark_id, @Type, @FK, @Parent, @Parent_name, @Title, @DateAdded, @LastModified, @URL, @PageTitle, @VisitCount, @LastVisitDate, @AnnoId, @AnnoContent, @AnnoName, @File)";

                                    using (SQLiteCommand commandInsertBookmarks = new SQLiteCommand(insertBookmarks, chromeViewerConnection))
                                    {
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
                                transaction.Commit(); // Finaliza la transacción
                            }
                        }
                    }
                }
            }
            else
            {
                //MessageBox.Show("Select a Firefox history file first", "ChromeViewer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (logConsole != null)
                    LogToConsole(logConsole, $"Select a Firefox history file first", Color.Red);
                else Console.WriteLine($"Select a Firefox history file first");
            }
        }



        public static void ProcessAndWriteRecordsChromeLike(string historyConnectionString, string chromeViewerConnectionString, string browserType, string filepath, RichTextBox logConsole = null)
        {
            if (!string.IsNullOrEmpty(historyConnectionString))
            {
                using (SQLiteConnection historyConnection = new SQLiteConnection(historyConnectionString))
                {
                    historyConnection.Open();

                    // Leer registros de visits
                    string queryVisits = "SELECT id, url, visit_time, from_visit, visit_duration, transition FROM visits";
                    using (SQLiteCommand commandVisits = new SQLiteCommand(queryVisits, historyConnection))
                    using (SQLiteDataReader readerVisits = commandVisits.ExecuteReader())
                    {
                        using (SQLiteConnection chromeViewerConnection = new SQLiteConnection(chromeViewerConnectionString))
                        {
                            chromeViewerConnection.Open();
                            using (var transaction = chromeViewerConnection.BeginTransaction())  // Inicia la transacción
                            {
                                while (readerVisits.Read())
                                {
                                    int visitId = readerVisits.GetInt32(0);
                                    int urlId = readerVisits.GetInt32(1);
                                    long visitTimeMicroseconds = readerVisits.GetInt64(2);
                                    int? fromVisit = readerVisits.IsDBNull(3) ? (int?)null : readerVisits.GetInt32(3);
                                    long? visitDurationMicroseconds = readerVisits.IsDBNull(4) ? (long?)null : readerVisits.GetInt64(4);
                                    int transition = readerVisits.GetInt32(5);

                                    DateTime epoch = new DateTime(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                                    DateTime visitTime = epoch.AddTicks(visitTimeMicroseconds * 10); // Convertir microsegundos a ticks

                                    // visit_duration representa una duración en microsegundos.
                                    TimeSpan? visitDuration = visitDurationMicroseconds.HasValue ? TimeSpan.FromTicks(visitDurationMicroseconds.Value * 10) : (TimeSpan?)null;

                                    // Obtener la URL de from_visit
                                    string fromUrl = null;
                                    if (fromVisit.HasValue)
                                    {
                                        string queryFromVisit = "SELECT url FROM urls WHERE id = (SELECT url FROM visits WHERE id = @fromVisit)";
                                        using (SQLiteCommand commandFromVisit = new SQLiteCommand(queryFromVisit, historyConnection))
                                        {
                                            commandFromVisit.Parameters.AddWithValue("@fromVisit", fromVisit.Value);
                                            object result = commandFromVisit.ExecuteScalar();
                                            fromUrl = result?.ToString();
                                        }
                                    }

                                    // Buscar información relacionada en urls
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

                                                DateTime lastVisitTime = epoch.AddTicks(chromiumTimeMicroseconds * 10); // Convertir microsegundos a ticks

                                                string Category = Evaluatecategory(url);
                                                string potentialActivity = EvalPotentialActivity(url);

                                                // Crear diccionario para generar los menús en acordeón
                                                if (!Helpers.browserUrls.ContainsKey(browserType))
                                                {
                                                    Helpers.browserUrls[browserType] = new List<string>();
                                                }
                                                Helpers.browserUrls[browserType].Add(url);

                                                // Interpretar el valor de transition
                                                string transitionType = GetTransitionDescription(transition);

                                                // Escribir el resultado en results
                                                string insertResults = @"INSERT INTO results (Browser, Category, Potential_activity, Visit_id, Url, Title, Visit_count, Last_visit_time, Visit_time, From_url, Visit_duration, Transition, Typed_count, File)
                                                            VALUES (@Browser, @Category, @Potential_activity, @visitId, @url, @title, @visitCount, @lastVisitTime, @visitTime, @fromUrl, @visitDuration, @transition, @typedCount, @File)";
                                                using (SQLiteCommand commandInsertResults = new SQLiteCommand(insertResults, chromeViewerConnection))
                                                {
                                                    commandInsertResults.Parameters.AddWithValue("@Browser", browserType);
                                                    commandInsertResults.Parameters.AddWithValue("@Category", Category);
                                                    commandInsertResults.Parameters.AddWithValue("@Potential_activity", potentialActivity);
                                                    commandInsertResults.Parameters.AddWithValue("@visitId", visitId);
                                                    commandInsertResults.Parameters.AddWithValue("@url", url);
                                                    commandInsertResults.Parameters.AddWithValue("@title", title);
                                                    commandInsertResults.Parameters.AddWithValue("@visitTime", visitTime.ToString("yyyy-MM-dd HH:mm:ss.ffffff"));
                                                    commandInsertResults.Parameters.AddWithValue("@visitDuration", visitDuration.HasValue ? visitDuration.Value.ToString(@"dd\.hh\:mm\:ss\.fffffff") : DBNull.Value.ToString());
                                                    commandInsertResults.Parameters.AddWithValue("@lastVisitTime", lastVisitTime.ToString("yyyy-MM-dd HH:mm:ss.ffffff"));
                                                    commandInsertResults.Parameters.AddWithValue("@visitCount", visitCount);
                                                    commandInsertResults.Parameters.AddWithValue("@typedCount", typedCount);
                                                    commandInsertResults.Parameters.AddWithValue("@fromUrl", (object)fromUrl ?? DBNull.Value);
                                                    commandInsertResults.Parameters.AddWithValue("@transition", transitionType);
                                                    commandInsertResults.Parameters.AddWithValue("@File", filepath);

                                                    commandInsertResults.ExecuteNonQuery();
                                                }
                                            }
                                        }
                                    }
                                }
                                transaction.Commit(); // Finaliza la transacción
                            }
                        }
                    }
                }
            }
            else
            {
                //MessageBox.Show("Select a Google Chrome history file first", "ChromeViewer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (logConsole != null)
                    LogToConsole(logConsole, $"Select a Google Chrome history file first", Color.Red);
                else Console.WriteLine($"Select a Google Chrome history file first");
            }
        }


        




        public static string GetTransitionDescription(int transition)
        {
            // Obtener el tipo de transición básico
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

            // Identificar calificadores adicionales
            List<string> qualifiers = new List<string>();
            if ((transition & 0x00800000) != 0) qualifiers.Add("IS_REDIRECT");
            if ((transition & 0x01000000) != 0) qualifiers.Add("FORWARD_BACK");
            if ((transition & 0x02000000) != 0) qualifiers.Add("FROM_ADDRESS_BAR");
            if ((transition & 0x04000000) != 0) qualifiers.Add("HOME_PAGE");
            if ((transition & 0x08000000) != 0) qualifiers.Add("FROM_API");

            // Combinar el tipo de transición con los calificadores
            string result = transitionType;
            if (qualifiers.Count > 0)
            {
                result += " (" + string.Join(", ", qualifiers) + ")";
            }

            return result;
        }

        //public static void LogToConsole(RichTextBox textBox_logConsole,  string message)
        //{
            
        //    string timestampedMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}";
        //    //string timestampedMessage = $"{DateTime.Now:HH:mm:ss} {message}";

        //    if (textBox_logConsole.InvokeRequired)
        //    {
        //        textBox_logConsole.Invoke(new Action(() => LogToConsole(textBox_logConsole,message)));
        //    }
        //    else
        //    {

        //        textBox_logConsole.AppendText(timestampedMessage + Environment.NewLine);
        //        textBox_logConsole.SelectionStart = textBox_logConsole.Text.Length;
        //        textBox_logConsole.ScrollToCaret();
        //        //label_status.Text = message;
        //        //WriteToLogFile(timestampedMessage);
        //    }

        //}


     


        public static void LogToConsole(RichTextBox textBox_logConsole, string message, Color? color = null)
        {
            string timestampedMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}{Environment.NewLine}";

            if (textBox_logConsole.InvokeRequired)
            {
                // Capturamos la variable local para evitar issues con el cierre (closure)
                Color? c = color;
                textBox_logConsole.Invoke((Action)(() => LogToConsole(textBox_logConsole, message, c)));
                return;
            }

            // Posiciona el caret al final y aplica el color deseado solo a lo que se va a agregar
            int start = textBox_logConsole.TextLength;
            textBox_logConsole.SelectionStart = start;
            textBox_logConsole.SelectionLength = 0;
            textBox_logConsole.SelectionColor = color ?? textBox_logConsole.ForeColor;

            textBox_logConsole.AppendText(timestampedMessage);

            // (Opcional) restaurar el color de selección al predeterminado
            textBox_logConsole.SelectionColor = textBox_logConsole.ForeColor;

            // Scroll al final
            textBox_logConsole.SelectionStart = textBox_logConsole.TextLength;
            textBox_logConsole.ScrollToCaret();
        }




       

       
        private async Task TraverseDirectoryAsync(string currentDir, List<string> sqliteFiles, RichTextBox textBox_logConsole)
        {
            try
            {
                foreach (var filePath in Directory.EnumerateFiles(currentDir, "*", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        //Chequear tambien la estructura de las tablas
                        if (Path.GetFileName(filePath) == "History" && IsSQLite3(filePath, textBox_logConsole))
                        {
                            if (SetBrowserType(filePath))
                            { 
                                sqliteFiles.Add(filePath);
                                Helpers.historyConnectionString = $"Data Source={filePath};Version=3;";
                                ProcessAndWriteRecordsChromeLike(Helpers.historyConnectionString, Helpers.chromeViewerConnectionString, Helpers.BrowserType, filePath);
                                ProcessAndWriteDownloadsChrome(Helpers.historyConnectionString, Helpers.chromeViewerConnectionString, Helpers.BrowserType, filePath);
                                LogToConsole(textBox_logConsole, $"Processing : {filePath}");

                                string directoryPath = Path.GetDirectoryName(filePath);
                                string bookmarksFilePath = Path.Combine(directoryPath, "Bookmarks");

                                if (File.Exists(bookmarksFilePath))
                                {
                                    LogToConsole(textBox_logConsole, $"Found Bookmarks file: {bookmarksFilePath}");
                                    ProcessAndWriteBookmarksChrome(Helpers.chromeViewerConnectionString, Helpers.BrowserType, bookmarksFilePath);

                                   
                                }
                                else
                                {
                                    LogToConsole(textBox_logConsole, "Bookmarks file not found in the same directory.");
                                }
                                ProcessAndWriteAutofillChrome(Helpers.chromeViewerConnectionString,Helpers.BrowserType,filePath);
                            }
                        }
                        //Chequear tambien la estructura de las tablas
                        if (Path.GetFileName(filePath) == "places.sqlite" && IsSQLite3(filePath, textBox_logConsole))
                        {
                            if (SetBrowserType(filePath))
                            {
                                sqliteFiles.Add(filePath);
                                Helpers.historyConnectionString = $"Data Source={filePath};Version=3;";
                                ProcessAndWriteRecordsFirefox(Helpers.historyConnectionString, Helpers.chromeViewerConnectionString, Helpers.BrowserType, filePath);
                                ProcessAndWriteDownloadsFirefox(Helpers.historyConnectionString, Helpers.chromeViewerConnectionString, Helpers.BrowserType, filePath);
                                ProcessAndWriteBookmarksFirefox(Helpers.historyConnectionString, Helpers.chromeViewerConnectionString, Helpers.BrowserType, filePath);
                                ProcessAndWriteAutofillFirefox(Helpers.chromeViewerConnectionString,Helpers.BrowserType,filePath);
                                LogToConsole(textBox_logConsole, $"Processing : {filePath}");
                            }
                        }


                        if (Path.GetFileName(filePath) == "WebCacheV01.dat")    
                        {
                            // Aqui llamar a una funcion similar a  ProcessAndWriteRecordsChromeLike pero que el segundo parametro sea WebCacheV01.dat
                            //ProcessAndWriteRecordsEsent(filePath, Helpers.chromeViewerConnectionString, Helpers.BrowserType, filePath);
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

        private bool IsSQLite3(string filePath, RichTextBox textBox_logConsole)
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






        public static void ProcessAndWriteRecordsFirefox(string historyConnectionString, string chromeViewerConnectionString, string browserType, string filepath, RichTextBox logConsole = null)
        {
            if (!string.IsNullOrEmpty(historyConnectionString))
            {
                using (SQLiteConnection historyConnection = new SQLiteConnection(historyConnectionString))
                {
                    historyConnection.Open();

                    string queryVisits = @"
                                            SELECT hv.id, hv.place_id, hv.visit_date, hv.visit_type, p.url, p.title, p.visit_count, p.last_visit_date, p.frecency 
                                            FROM moz_historyvisits hv 
                                            JOIN moz_places p ON hv.place_id = p.id";

                    using (SQLiteCommand commandVisits = new SQLiteCommand(queryVisits, historyConnection))
                    using (SQLiteDataReader readerVisits = commandVisits.ExecuteReader())
                    {
                        using (SQLiteConnection chromeViewerConnection = new SQLiteConnection(chromeViewerConnectionString))
                        {
                            
                            chromeViewerConnection.Open();

                            using (SQLiteTransaction transaction = chromeViewerConnection.BeginTransaction())
                            {
                                try
                                {
                                    while (readerVisits.Read())
                                    {
                                        int visitId = readerVisits.GetInt32(0);
                                        int placeId = readerVisits.GetInt32(1);
                                        long visitTimeMicroseconds = readerVisits.GetInt64(2);
                                        int visitType = readerVisits.GetInt32(3);
                                        string url = readerVisits.GetString(4);
                                        string title = readerVisits.IsDBNull(5) ? null : readerVisits.GetString(5);
                                        int visitCount = readerVisits.GetInt32(6);
                                        long lastVisitTimeMicroseconds = readerVisits.IsDBNull(7) ? 0 : readerVisits.GetInt64(7);
                                        int frecency = readerVisits.GetInt32(8);

                                        DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                                        DateTime visitTime = epoch.AddMilliseconds(visitTimeMicroseconds / 1000.0);
                                        DateTime lastVisitTime = epoch.AddMilliseconds(lastVisitTimeMicroseconds / 1000.0);

                                        string Category = Evaluatecategory(url);
                                        string transition = GetTransitionDescriptionFirefox(visitType);

                                        // Determina la actividad potencial usando la función creada
                                        string potentialActivity = EvalPotentialActivity(url);

                                        if (!Helpers.browserUrls.ContainsKey(browserType))
                                        {
                                            Helpers.browserUrls[browserType] = new List<string>();
                                        }
                                        
                                        Helpers.browserUrls[browserType].Add(url);


                                        // Inserción en la tabla firefox_results
                                        string insertResults = @"INSERT INTO firefox_results (Browser, Category, Potential_activity, Visit_id, Place_id, Url, Title, Visit_time,  Last_visit_time, Visit_count,  Transition, File, Visit_type, Frecency)
                                                VALUES (@Browser, @Category, @Potential_activity, @visitId, @placeId, @url, @title, @visitTime, @lastVisitTime, @visitCount, @transition, @File, @visitType, @frecency)";
                                        using (SQLiteCommand commandInsertResults = new SQLiteCommand(insertResults, chromeViewerConnection))
                                        {
                                            commandInsertResults.Parameters.AddWithValue("@Browser", browserType);
                                            commandInsertResults.Parameters.AddWithValue("@Category", Category); // Puedes ajustar esto según el uso de Evaluatecategory
                                            commandInsertResults.Parameters.AddWithValue("@Potential_activity", potentialActivity);
                                            commandInsertResults.Parameters.AddWithValue("@visitId", visitId);
                                            commandInsertResults.Parameters.AddWithValue("@placeId", placeId);
                                            commandInsertResults.Parameters.AddWithValue("@url", url);
                                            commandInsertResults.Parameters.AddWithValue("@title", title);
                                            commandInsertResults.Parameters.AddWithValue("@visitTime", visitTime.ToString("yyyy-MM-dd HH:mm:ss.ffffff"));
                                           // commandInsertResults.Parameters.AddWithValue("@visitDuration", DBNull.Value.ToString()); // Firefox no tiene duración directa
                                            commandInsertResults.Parameters.AddWithValue("@lastVisitTime", lastVisitTime.ToString("yyyy-MM-dd HH:mm:ss.ffffff"));
                                            commandInsertResults.Parameters.AddWithValue("@visitCount", visitCount);
                                            //commandInsertResults.Parameters.AddWithValue("@typedCount", "N/A"); // No hay un equivalente directo en Firefox
                                            //commandInsertResults.Parameters.AddWithValue("@fromUrl", DBNull.Value); // No disponible en Firefox
                                            commandInsertResults.Parameters.AddWithValue("@transition", transition);
                                            if (Helpers.realFirefoxPlacesPath != "") {
                                                commandInsertResults.Parameters.AddWithValue("@File", Helpers.realFirefoxPlacesPath);
                                            }
                                            else
                                            {
                                                commandInsertResults.Parameters.AddWithValue("@File", filepath);
                                            }                                            
                                            commandInsertResults.Parameters.AddWithValue("@visitType", visitType);
                                            commandInsertResults.Parameters.AddWithValue("@frecency", frecency);
                                            commandInsertResults.ExecuteNonQuery();
                                        }
                                    }

                                    transaction.Commit();
                                }
                                catch (Exception ex)
                                {
                                    transaction.Rollback();
                                    //MessageBox.Show($"Error: {ex.Message}", "ChromeViewer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    if (logConsole != null)
                                        LogToConsole(logConsole, $"Error: {ex.Message}", Color.Red);
                                    else Console.WriteLine($"Error: {ex.Message}");
                                }

                            }
                        }
                    }
                }
            }
            else
            {
                //MessageBox.Show("Select a Firefox history file first", "ChromeViewer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (logConsole != null)
                    LogToConsole(logConsole, $"Select a Firefox history file first", Color.Red);
                else Console.WriteLine($"Select a Firefox history file first");
            }
        }


        


        // Extraer el dominio base de la url

        private static string GetDomainFromUrl(string url)
        {
            try
            {
                Uri uri = new Uri(url);
                string domain = uri.Host;

                // Eliminar "www." si está presente
                if (domain.StartsWith("www."))
                {
                    domain = domain.Substring(4);
                }

                return domain;
            }
            catch (UriFormatException)
            {
                return null; // O manejar de otra manera si la URL es inválida
            }
        }



        public static string Evaluatecategory(string url)
        {

            if (url.StartsWith("file:///"))
            {
                return "Local Files";
            }


            string domain = GetDomainFromUrl(url);

            if (domain == null)
            {
                return "Unknown"; // O maneja esto de otra manera
            }

            if (IsLanAddress(domain))
            {
                return "Lan Addresses Browsing";
            }

            foreach (var category in CategoryData.categoryDomains)
            {
                // Existe un dominio exactamente igual en alguna categoría?
                if (category.Value.Any(d => domain.Equals(d, StringComparison.OrdinalIgnoreCase)))
                {
                    return category.Key;
                }
            }
            
            // Si no hay coincidencia exacta, verifica las otras condiciones
            foreach (var category in CategoryData.categoryDomains)
            {
                if (category.Value.Any(d => domain.EndsWith($".{d}", StringComparison.OrdinalIgnoreCase)))
                //if (category.Value.Any(d => domain.EndsWith($".{d}", StringComparison.OrdinalIgnoreCase) || domain.StartsWith($"{d}.", StringComparison.OrdinalIgnoreCase)))
                {
                    return category.Key;
                }
            }

            return "Other"; // Si no coincide con ninguna categoría
        }

        private static bool IsLanAddress(string domain)
        {
            // Verifica si el dominio es una dirección IP y si está en los rangos de LAN
            if (System.Net.IPAddress.TryParse(domain, out var ipAddress))
            {
                byte[] bytes = ipAddress.GetAddressBytes();

                // 10.0.0.0 - 10.255.255.255
                if (bytes[0] == 10)
                    return true;

                // 172.16.0.0 - 172.31.255.255
                if (bytes[0] == 172 && (bytes[1] >= 16 && bytes[1] <= 31))
                    return true;

                // 192.168.0.0 - 192.168.255.255
                if (bytes[0] == 192 && bytes[1] == 168)
                    return true;
            }

            return false;
        }



        // Evaluar activida potencial


        private static string EvalPotentialActivity(string url)
        {
            // Primero, extraemos el dominio y el path de la URL
            Uri uri;
            try
            {
                uri = new Uri(url);
            }
            catch (UriFormatException)
            {
                return "Unknown Activity"; // Si la URL es inválida, retornamos una actividad desconocida
            }

            string domain = uri.Host;
            string path = uri.AbsolutePath.ToLower();



            // Evaluación de actividades potenciales según el dominio y el path

            if (domain.Contains("google.com"))
            {
                if (domain.Contains("drive") && path.Contains("folders"))
                {
                    return "Google Folder Viewing"; // drive.google.com indica acceso a almacenamiento en la nube
                }
                else if (domain.Contains("drive"))
                {
                    return "Google Drive Accessing "; // drive.google.com indica acceso a almacenamiento en la nube
                }
                else if (domain.Contains("docs") && path.Contains("document"))
                {
                    return "Google Docs"; // docs.google.com o paths con 'document' indican edición de documentos
                }
                else if (domain.Contains("docs") && path.Contains("spreadsheets"))
                {
                    return "Google Spreadsheets"; // docs.google.com o paths con 'document' indican edición de documentos
                }
                else if (domain.Contains("docs") && path.Contains("presentation"))
                {
                    return "Google Presentation"; // docs.google.com o paths con 'document' indican edición de documentos
                }
                else if (domain.Contains("mail") || path.Contains("mail"))
                {
                    return "Google Checking Email"; // mail.google.com indica chequeo de correo
                }
                else if (domain.Contains("gemini"))
                {
                    return "Google Gemini Chat"; // google.com/search indica búsqueda web
                }
                else if (domain.Contains("news"))
                {
                    return "Google News"; // google.com/search indica búsqueda web
                }
                else if (domain.Contains("meet"))
                {
                    return "Google Meet"; // google.com/search indica búsqueda web
                }
                else if (domain.Contains("calendar"))
                {
                    return "Google Calendar"; // google.com/search indica búsqueda web
                }
                else if (domain.Contains("contacts"))
                {
                    return "Google Contacts"; // google.com/search indica búsqueda web
                }
                else if (domain.Contains("play"))
                {
                    return "Google Play"; // google.com/search indica búsqueda web
                }
                else if (domain.Contains("translate"))
                {
                    return "Google Translate"; // google.com/search indica búsqueda web
                }
                else if (domain.Contains("photos"))
                {
                    return "Google Photos"; // google.com/search indica búsqueda web
                }
                else if (domain.Contains("myadcenter"))
                {
                    return "Google Ad Center"; // google.com/search indica búsqueda web
                }
                else if ((domain.Contains("shopping")) || (path.StartsWith("/shopping")))
                {
                    return "Google Shopping"; // google.com/search indica búsqueda web
                }
                else if (path.StartsWith("/search")) 
                {
                    return "Google Search"; // google.com/search indica búsqueda web
                }                
                else if (path.StartsWith("/maps/"))
                {
                    return "Google Maps"; // google.com/search indica búsqueda web
                }                
                else if (path.Contains("/#chat"))
                {
                    return "Google Chat"; // google.com/search indica búsqueda web
                }                
                else if (path.StartsWith("/finance"))
                {
                    return "Google Finance"; // google.com/search indica búsqueda web
                }
                else if (path.Contains("/travel/flights/"))
                {
                    return "Google Flights"; // google.com/search indica búsqueda web
                }
                else if (path.StartsWith("/travel/"))
                {
                    return "Google Travel"; // google.com/search indica búsqueda web
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
                    return "Downloading Software"; // Paths que contienen 'downloads' indican descargas
                }
                else if (domain.Contains("office") || path.Contains("office"))
                {
                    return "Using Online Office Suite"; // Microsoft Office online
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
                    return "Twitter Login"; // Página de inicio de sesión de Twitter
                }
                else if (path.Contains("/home"))
                {
                    return "Viewing Twitter Feed"; // Visualización del feed de Twitter
                }
                else if (path.Contains("/hashtag"))
                {
                    return "Browsing Twitter Hashtags"; // Navegación por hashtags en Twitter
                }
                else
                {
                    return "Social Networking on Twitter"; // Actividad general en Twitter
                }
            }

            if (domain.Contains("amazon.com") || domain.Contains("ebay.com"))
            {
                return "Shopping Online"; // Compras en línea
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
                    return "Browsing YouTube"; // Si es YouTube pero no coincide con los paths específicos, es entretenimiento general
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
                    return "Chatbot Platform"; // Si es ChatGPT pero no coincide con los paths específicos, es el uso general de la plataforma
                }
            }


            if (domain.Contains("netflix.com"))
            {
                return "Streaming Video"; // Actividad general de streaming en Netflix
            }



            // Si no coincide con ninguna regla específica, retornamos una actividad genérica
            return "Browsing Website";
        }




        private static string GetTransitionDescriptionFirefox(int visitType)
        {
            // Mapear el valor de visit_type a una descripción legible.
            switch (visitType)
            {
                case 1:
                    return "Link";  // La visita fue originada al hacer clic en un enlace desde otra página
                case 2:
                    return "Typed";  // La URL fue escrita manualmente en la barra de direcciones o seleccionada desde el autocompletado
                case 3:
                    return "Bookmark";  // La URL fue visitada al seleccionar un marcador
                case 4:
                    return "Embed";  // La URL fue cargada como parte de un recurso embebido (por ejemplo, una imagen o un iframe)
                case 5:
                    return "Redirect (Permanent)";  // La visita fue el resultado de una redirección permanente (301)
                case 6:
                    return "Redirect (Temporary)";  // La visita fue el resultado de una redirección temporal (302)
                case 7:
                    return "Download";  // La URL se visitó como parte de una descarga de archivos
                case 8:
                    return "Framed Link";  // Navegación dentro de un frame
                default:
                    return "Unknown";  // Cualquier otro valor que no esté cubierto por los casos anteriores
            }
        }






        



        private bool SetBrowserType(string filePath)
        {
            string lowerFilePath = filePath.ToLower();
            bool found = false;

            if (lowerFilePath.Contains("chrome"))
            {
                Helpers.BrowserType = "Chrome";
                found = true;
            }
            else if (lowerFilePath.Contains("brave"))
            {
                Helpers.BrowserType = "Brave";
                found = true;
            }
            else if (lowerFilePath.Contains("edge"))
            {
                Helpers.BrowserType = "Edge";
                found = true;
            }
            else if (lowerFilePath.Contains("webcache"))
            {
                Helpers.BrowserType = "Edge";
                found = true;
            }
            else if (lowerFilePath.Contains("opera"))
            {
                Helpers.BrowserType = "Opera";
                found = true;
            }
            else if (lowerFilePath.Contains("yandex"))
            {
                Helpers.BrowserType = "Yandex";
                found = true;
            }
            else if (lowerFilePath.Contains("vivaldi"))
            {
                Helpers.BrowserType = "Vivaldi";
                found = true;
            }
            else if (lowerFilePath.Contains("places.sqlite"))
            {
                Helpers.BrowserType = "Firefox";
                found = true;
            }
            else
            {
                Helpers.BrowserType = "Unknown";
            }

            return found;
        }


        public void MostrarPorCategoria(Label labelStatus, SfDataGrid sfDataGrid, string connectionString, string tableName, string categoria, string navegador, Label labelItemCount, RichTextBox Console)
        {
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                
                string query = "";
                int utcOffset = Helpers.utcOffset;  // Obtener el valor actual de utcOffset

                if (Helpers.searchTermExists)
                {
                    // Determinar la condición de búsqueda (LIKE o REGEXP)
                    string searchCondition = Helpers.searchTermRegExp
                        ? "(Url REGEXP @searchTerm OR Title REGEXP @searchTerm)"
                        : "(Url LIKE @searchTerm OR Title LIKE @searchTerm)";

                    if (string.IsNullOrEmpty(categoria))
                    {
                        // Mostrar todos los registros del navegador específico
                        if (tableName == "firefox_results")
                        {
                            query = $@"SELECT id, Browser, Category, Potential_activity, Visit_id, Place_id, Url, Title, 
                                      STRFTIME('%Y-%m-%d %H:%M:%f', Visit_time, '{utcOffset} hours') AS Visit_time,
                                      STRFTIME('%Y-%m-%d %H:%M:%f', Last_visit_time, '{utcOffset} hours') AS Last_visit_time,
                                      Visit_count, Transition, Visit_type, Frecency, File, Label, Comment
                              FROM {tableName} WHERE Browser = @Browser AND {searchCondition} {Helpers.sqltimecondition}";
                        }
                        else if (tableName == "results")
                        {
                            query = $@"SELECT id, Browser, Category, Potential_activity, Visit_id, Url, Title, 
                                      STRFTIME('%Y-%m-%d %H:%M:%f', Visit_time, '{utcOffset} hours') AS Visit_time,
                                      STRFTIME('%Y-%m-%d %H:%M:%f', Last_visit_time, '{utcOffset} hours') AS Last_visit_time,
                                      Visit_duration, Visit_count, Typed_count, From_url, Transition, File, Label, Comment
                                      FROM {tableName} WHERE Browser = @Browser AND {searchCondition} {Helpers.sqltimecondition}";
                        }
                    }
                    else
                    {
                        // Filtrar por categoría y navegador específico
                        if (tableName == "firefox_results")
                        {
                            query = $@"SELECT id, Browser, Category, Potential_activity, Visit_id, Place_id, Url, Title, 
                                      STRFTIME('%Y-%m-%d %H:%M:%f', Visit_time, '{utcOffset} hours') AS Visit_time,
                                      STRFTIME('%Y-%m-%d %H:%M:%f', Last_visit_time, '{utcOffset} hours') AS Last_visit_time,
                                      Visit_count, Transition, Visit_type, Frecency, File, Label, Comment
                                      FROM {tableName} WHERE Category = @Category AND Browser = @Browser AND {searchCondition} {Helpers.sqltimecondition}";
                        }
                        else if (tableName == "results")
                        {
                            query = $@"SELECT id, Browser, Category, Potential_activity, Visit_id, Url, Title, 
                                      STRFTIME('%Y-%m-%d %H:%M:%f', Visit_time, '{utcOffset} hours') AS Visit_time,
                                      STRFTIME('%Y-%m-%d %H:%M:%f', Last_visit_time, '{utcOffset} hours') AS Last_visit_time,
                                      Visit_duration, Visit_count, Typed_count, From_url, Transition, File, Label, Comment
                                      FROM {tableName} WHERE Category = @Category AND Browser = @Browser AND {searchCondition} {Helpers.sqltimecondition}";
                        }
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(categoria))
                    {
                        if (Helpers.searchTimeCondition)
                        {
                            Helpers.sqltimecondition = $" AND (Visit_time >= '{Helpers.sd}' AND Visit_time <= '{Helpers.ed}')";
                        }
                        

                        // Mostrar todos los registros del navegador específico
                        if (tableName == "firefox_results")
                        {
                            query = $@"SELECT id, Browser, Category, Potential_activity, Visit_id, Place_id, Url, Title, 
                                      STRFTIME('%Y-%m-%d %H:%M:%f', Visit_time, '{utcOffset} hours') AS Visit_time,
                                      STRFTIME('%Y-%m-%d %H:%M:%f', Last_visit_time, '{utcOffset} hours') AS Last_visit_time,
                                      Visit_count, Transition, Visit_type, Frecency, File, Label, Comment
                                      FROM {tableName} WHERE Browser = @Browser {Helpers.sqltimecondition}";
                        }
                        else if (tableName == "results")
                        {
                            query = $@"SELECT id, Browser, Category, Potential_activity, Visit_id, Url, Title, 
                                      STRFTIME('%Y-%m-%d %H:%M:%f', Visit_time, '{utcOffset} hours') AS Visit_time,
                                      STRFTIME('%Y-%m-%d %H:%M:%f', Last_visit_time, '{utcOffset} hours') AS Last_visit_time,
                                      Visit_duration, Visit_count, Typed_count, From_url, Transition, File, Label, Comment
                                      FROM {tableName} WHERE Browser = @Browser {Helpers.sqltimecondition}";
                        }
                    }
                    else
                    {
                        if (Helpers.searchTimeCondition)
                        {
                            Helpers.sqltimecondition = $"(Visit_time >= '{Helpers.sd}' AND Visit_time <= '{Helpers.ed}')";
                            // Filtrar por categoría y navegador específico
                            if (tableName == "firefox_results")
                            {
                                query = $@"SELECT id, Browser, Category, Potential_activity, Visit_id, Place_id, Url, Title, 
                              STRFTIME('%Y-%m-%d %H:%M:%f', Visit_time, '{utcOffset} hours') AS Visit_time,
                              STRFTIME('%Y-%m-%d %H:%M:%f', Last_visit_time, '{utcOffset} hours') AS Last_visit_time,
                              Visit_count, Transition, Visit_type, Frecency, File, Label, Comment
                              FROM {tableName} WHERE Category = @Category AND Browser = @Browser AND {Helpers.sqltimecondition}";
                            }
                            else if (tableName == "results")
                            {
                                query = $@"SELECT id, Browser, Category, Potential_activity, Visit_id, Url, Title, 
                              STRFTIME('%Y-%m-%d %H:%M:%f', Visit_time, '{utcOffset} hours') AS Visit_time,
                              STRFTIME('%Y-%m-%d %H:%M:%f', Last_visit_time, '{utcOffset} hours') AS Last_visit_time,
                              Visit_duration, Visit_count, Typed_count, From_url, Transition, File, Label, Comment
                              FROM {tableName} WHERE Category = @Category AND Browser = @Browser AND {Helpers.sqltimecondition}";
                            }

                        }
                        else 
                        { 
                            // Filtrar por categoría y navegador específico
                                if (tableName == "firefox_results")
                            {
                                query = $@"SELECT id, Browser, Category, Potential_activity, Visit_id, Place_id, Url, Title, 
                              STRFTIME('%Y-%m-%d %H:%M:%f', Visit_time, '{utcOffset} hours') AS Visit_time,
                              STRFTIME('%Y-%m-%d %H:%M:%f', Last_visit_time, '{utcOffset} hours') AS Last_visit_time,
                              Visit_count, Transition, Visit_type, Frecency, File, Label, Comment
                              FROM {tableName} WHERE Category = @Category AND Browser = @Browser";
                            }
                            else if (tableName == "results")
                            {
                                query = $@"SELECT id, Browser, Category, Potential_activity, Visit_id, Url, Title, 
                              STRFTIME('%Y-%m-%d %H:%M:%f', Visit_time, '{utcOffset} hours') AS Visit_time,
                              STRFTIME('%Y-%m-%d %H:%M:%f', Last_visit_time, '{utcOffset} hours') AS Last_visit_time,
                              Visit_duration, Visit_count, Typed_count, From_url, Transition, File, Label, Comment
                              FROM {tableName} WHERE Category = @Category AND Browser = @Browser";
                            }
                        }

                    }
                }

                //LogToConsole(Console, query);

                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Browser", navegador);

                    if (!string.IsNullOrEmpty(categoria))
                    {
                        command.Parameters.AddWithValue("@Category", categoria);
                    }

                    if (Helpers.searchTermExists)
                    {
                        if (Helpers.searchTermRegExp)
                        {
                            // REGEXP: No se agregan los comodines %
                            command.Parameters.AddWithValue("@searchTerm", Helpers.searchTerm);
                        }
                        else
                        {
                            // LIKE: Se agregan % para buscar coincidencias parciales
                            command.Parameters.AddWithValue("@searchTerm", $"%{Helpers.searchTerm}%");
                        }
                    }

                    using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(command))
                    {
                        DataTable dataTable = new DataTable();
                        adapter.Fill(dataTable);
                        sfDataGrid.DataSource = dataTable;

                        // Actualizar el número de elementos en el label
                        labelItemCount.Text = $"Items count: {sfDataGrid.RowCount - 1}"; // Restamos 1 para no contar la fila de encabezado.

                        // Asegurar que haya al menos una fila antes de establecer el foco
                        if (sfDataGrid.View != null && sfDataGrid.View.Records.Count > 0)
                        {
                            sfDataGrid.SelectedIndex = 0; // Selecciona la primera fila
                            sfDataGrid.MoveToCurrentCell(new RowColumnIndex(0, 0)); // Mueve el foco a la primera celda
                        }
                    }
                }


                //if (Helpers.searchTermExists)
                //{
                //    if (string.IsNullOrEmpty(categoria))
                //    {
                //        // Mostrar todos los registros del navegador específico
                //        if (tableName == "firefox_results")
                //        {
                //            query = $@"SELECT id, Browser, Category, Potential_activity, Visit_id, Place_id, Url, Title, 
                //                       STRFTIME('%Y-%m-%d %H:%M:%f', Visit_time, '{utcOffset} hours') AS Visit_time,
                //                       STRFTIME('%Y-%m-%d %H:%M:%f', Last_visit_time, '{utcOffset} hours') AS Last_visit_time,
                //                       Visit_count, Transition, Visit_type, Frecency, File
                //                FROM {tableName} WHERE Browser = @Browser AND (Url LIKE @searchTerm OR Title LIKE @searchTerm)";
                //        }
                //        else if (tableName == "results")
                //        {
                //            query = $@"SELECT id, Browser, Category, Potential_activity, Visit_id, Url, Title, 
                //                       STRFTIME('%Y-%m-%d %H:%M:%f', Visit_time, '{utcOffset} hours') AS Visit_time,
                //                       STRFTIME('%Y-%m-%d %H:%M:%f', Last_visit_time, '{utcOffset} hours') AS Last_visit_time,
                //                       Visit_duration, Visit_count, Typed_count, From_url, Transition, File
                //                FROM {tableName} WHERE Browser = @Browser AND (Url LIKE @searchTerm OR Title LIKE @searchTerm)";
                //        }
                //    }
                //    else
                //    {
                //        // Filtrar por categoría y navegador específico
                //        if (tableName == "firefox_results")
                //        {
                //            query = $@"SELECT id, Browser, Category, Potential_activity, Visit_id, Place_id, Url, Title, 
                //                       STRFTIME('%Y-%m-%d %H:%M:%f', Visit_time, '{utcOffset} hours') AS Visit_time,
                //                       STRFTIME('%Y-%m-%d %H:%M:%f', Last_visit_time, '{utcOffset} hours') AS Last_visit_time,
                //                       Visit_count, Transition, Visit_type, Frecency, File
                //                FROM {tableName} WHERE Category = @Category AND Browser = @Browser AND (Url LIKE @searchTerm OR Title LIKE @searchTerm)";
                //        }
                //        else if (tableName == "results")
                //        {
                //            query = $@"SELECT id, Browser, Category, Potential_activity, Visit_id, Url, Title, 
                //                       STRFTIME('%Y-%m-%d %H:%M:%f', Visit_time, '{utcOffset} hours') AS Visit_time,
                //                       STRFTIME('%Y-%m-%d %H:%M:%f', Last_visit_time, '{utcOffset} hours') AS Last_visit_time,
                //                       Visit_duration, Visit_count, Typed_count, From_url, Transition, File
                //                FROM {tableName} WHERE Category = @Category AND Browser = @Browser AND (Url LIKE @searchTerm OR Title LIKE @searchTerm)";
                //        }
                //    }
                //}
                //else
                //{
                //    if (string.IsNullOrEmpty(categoria))
                //    {
                //        // Mostrar todos los registros del navegador específico
                //        if (tableName == "firefox_results")
                //        {
                //            query = $@"SELECT id, Browser, Category, Potential_activity, Visit_id, Place_id, Url, Title, 
                //                       STRFTIME('%Y-%m-%d %H:%M:%f', Visit_time, '{utcOffset} hours') AS Visit_time,
                //                       STRFTIME('%Y-%m-%d %H:%M:%f', Last_visit_time, '{utcOffset} hours') AS Last_visit_time,
                //                       Visit_count, Transition, Visit_type, Frecency, File
                //                FROM {tableName} WHERE Browser = @Browser";
                //        }
                //        else if (tableName == "results")
                //        {
                //            query = $@"SELECT id, Browser, Category, Potential_activity, Visit_id, Url, Title, 
                //                       STRFTIME('%Y-%m-%d %H:%M:%f', Visit_time, '{utcOffset} hours') AS Visit_time,
                //                       STRFTIME('%Y-%m-%d %H:%M:%f', Last_visit_time, '{utcOffset} hours') AS Last_visit_time,
                //                       Visit_duration, Visit_count, Typed_count, From_url, Transition, File
                //                FROM {tableName} WHERE Browser = @Browser";
                //        }
                //    }
                //    else
                //    {
                //        // Filtrar por categoría y navegador específico
                //        if (tableName == "firefox_results")
                //        {
                //            query = $@"SELECT id, Browser, Category, Potential_activity, Visit_id, Place_id, Url, Title, 
                //                       STRFTIME('%Y-%m-%d %H:%M:%f', Visit_time, '{utcOffset} hours') AS Visit_time,
                //                       STRFTIME('%Y-%m-%d %H:%M:%f', Last_visit_time, '{utcOffset} hours') AS Last_visit_time,
                //                       Visit_count, Transition, Visit_type, Frecency, File
                //                FROM {tableName} WHERE Category = @Category AND Browser = @Browser";
                //        }
                //        else if (tableName == "results")
                //        {
                //            query = $@"SELECT id, Browser, Category, Potential_activity, Visit_id, Url, Title, 
                //                       STRFTIME('%Y-%m-%d %H:%M:%f', Visit_time, '{utcOffset} hours') AS Visit_time,
                //                       STRFTIME('%Y-%m-%d %H:%M:%f', Last_visit_time, '{utcOffset} hours') AS Last_visit_time,
                //                       Visit_duration, Visit_count, Typed_count, From_url, Transition, File
                //                FROM {tableName} WHERE Category = @Category AND Browser = @Browser";
                //        }
                //    }
                //}

                //if (Helpers.searchTermExists)
                //{
                //    if (string.IsNullOrEmpty(categoria))
                //    {
                //        // Mostrar todos los registros del navegador específico
                //        if (tableName == "firefox_results")
                //        {
                //            query = utcOffset == 0 ?
                //                $@"SELECT id, Browser, Category, Potential_activity, Visit_id, Place_id, Url, Title, 
                //           STRFTIME('%Y-%m-%d %H:%M:%f', Visit_time) AS Visit_time,
                //           STRFTIME('%Y-%m-%d %H:%M:%f', Last_visit_time) AS Last_visit_time,
                //           Visit_count, Transition, Visit_type, Frecency, File
                //    FROM {tableName} WHERE Browser = @Browser AND (Url LIKE @searchTerm OR Title LIKE @searchTerm)" :
                //                $@"SELECT id, Browser, Category, Potential_activity, Visit_id, Place_id, Url, Title, 
                //           DATETIME(Visit_time, '{utcOffset} hours') AS Visit_time,
                //           DATETIME(Last_visit_time, '{utcOffset} hours') AS Last_visit_time,
                //           Visit_count, Transition, Visit_type, Frecency, File
                //    FROM {tableName} WHERE Browser = @Browser AND (Url LIKE @searchTerm OR Title LIKE @searchTerm)";
                //        }
                //        else if (tableName == "results")
                //        {
                //            query = utcOffset == 0 ?
                //                $@"SELECT id, Browser, Category, Potential_activity, Visit_id, Url, Title, 
                //           STRFTIME('%Y-%m-%d %H:%M:%f', Visit_time) AS Visit_time,
                //           STRFTIME('%Y-%m-%d %H:%M:%f', Last_visit_time) AS Last_visit_time,
                //           Visit_duration, Visit_count, Typed_count, From_url, Transition, File
                //    FROM {tableName} WHERE Browser = @Browser AND (Url LIKE @searchTerm OR Title LIKE @searchTerm)" :
                //                $@"SELECT id, Browser, Category, Potential_activity, Visit_id, Url, Title, 
                //           DATETIME(Visit_time, '{utcOffset} hours') AS Visit_time,
                //           DATETIME(Last_visit_time, '{utcOffset} hours') AS Last_visit_time,
                //           Visit_duration, Visit_count, Typed_count, From_url, Transition, File
                //    FROM {tableName} WHERE Browser = @Browser AND (Url LIKE @searchTerm OR Title LIKE @searchTerm)";
                //        }
                //    }
                //    else
                //    {
                //        // Filtrar por categoría y navegador específico
                //        if (tableName == "firefox_results")
                //        {
                //            query = utcOffset == 0 ?
                //                $@"SELECT id, Browser, Category, Potential_activity, Visit_id, Place_id, Url, Title, 
                //           STRFTIME('%Y-%m-%d %H:%M:%f', Visit_time) AS Visit_time,
                //           STRFTIME('%Y-%m-%d %H:%M:%f', Last_visit_time) AS Last_visit_time,
                //           Visit_count, Transition, Visit_type, Frecency, File
                //    FROM {tableName} WHERE Category = @Category AND Browser = @Browser AND (Url LIKE @searchTerm OR Title LIKE @searchTerm)" :
                //                $@"SELECT id, Browser, Category, Potential_activity, Visit_id, Place_id, Url, Title, 
                //           DATETIME(Visit_time, '{utcOffset} hours') AS Visit_time,
                //           DATETIME(Last_visit_time, '{utcOffset} hours') AS Last_visit_time,
                //           Visit_count, Transition, Visit_type, Frecency, File
                //    FROM {tableName} WHERE Category = @Category AND Browser = @Browser AND (Url LIKE @searchTerm OR Title LIKE @searchTerm)";
                //        }
                //        else if (tableName == "results")
                //        {
                //            query = utcOffset == 0 ?
                //                $@"SELECT id, Browser, Category, Potential_activity, Visit_id, Url, Title, 
                //           STRFTIME('%Y-%m-%d %H:%M:%f', Visit_time) AS Visit_time,
                //           STRFTIME('%Y-%m-%d %H:%M:%f', Last_visit_time) AS Last_visit_time,
                //           Visit_duration, Visit_count, Typed_count, From_url, Transition, File
                //    FROM {tableName} WHERE Category = @Category AND Browser = @Browser AND (Url LIKE @searchTerm OR Title LIKE @searchTerm)" :
                //                $@"SELECT id, Browser, Category, Potential_activity, Visit_id, Url, Title, 
                //           DATETIME(Visit_time, '{utcOffset} hours') AS Visit_time,
                //           DATETIME(Last_visit_time, '{utcOffset} hours') AS Last_visit_time,
                //           Visit_duration, Visit_count, Typed_count, From_url, Transition, File
                //    FROM {tableName} WHERE Category = @Category AND Browser = @Browser AND (Url LIKE @searchTerm OR Title LIKE @searchTerm)";
                //        }
                //    }
                //}

                //else
                //{ 

                //    if (string.IsNullOrEmpty(categoria))
                //    {
                //        // Mostrar todos los registros del navegador específico
                //        if (tableName == "firefox_results")
                //        {
                //            query = utcOffset == 0 ?
                //                $@"SELECT id, Browser, Category, Potential_activity, Visit_id, Place_id, Url, Title, 
                //           STRFTIME('%Y-%m-%d %H:%M:%f', Visit_time) AS Visit_time,
                //           STRFTIME('%Y-%m-%d %H:%M:%f', Last_visit_time) AS Last_visit_time,
                //           Visit_count, Transition, Visit_type, Frecency, File
                //    FROM {tableName} WHERE Browser = @Browser" :
                //                $@"SELECT id, Browser, Category, Potential_activity, Visit_id, Place_id, Url, Title, 
                //           DATETIME(Visit_time, '{utcOffset} hours') AS Visit_time,
                //           DATETIME(Last_visit_time, '{utcOffset} hours') AS Last_visit_time,
                //           Visit_count, Transition, Visit_type, Frecency, File
                //    FROM {tableName} WHERE Browser = @Browser";
                //        }
                //        else if (tableName == "results")
                //        {
                //            query = utcOffset == 0 ?
                //                $@"SELECT id, Browser, Category, Potential_activity, Visit_id, Url, Title, 
                //           STRFTIME('%Y-%m-%d %H:%M:%f', Visit_time) AS Visit_time,
                //           STRFTIME('%Y-%m-%d %H:%M:%f', Last_visit_time) AS Last_visit_time,
                //           Visit_duration, Visit_count, Typed_count, From_url, Transition, File
                //    FROM {tableName} WHERE Browser = @Browser" :
                //                $@"SELECT id, Browser, Category, Potential_activity, Visit_id, Url, Title, 
                //           DATETIME(Visit_time, '{utcOffset} hours') AS Visit_time,
                //           DATETIME(Last_visit_time, '{utcOffset} hours') AS Last_visit_time,
                //           Visit_duration, Visit_count, Typed_count, From_url, Transition, File
                //    FROM {tableName} WHERE Browser = @Browser";
                //        }
                //    }
                //    else
                //    {
                //        // Filtrar por categoría y navegador específico
                //        if (tableName == "firefox_results")
                //        {
                //            query = utcOffset == 0 ?
                //                $@"SELECT id, Browser, Category, Potential_activity, Visit_id, Place_id, Url, Title, 
                //           STRFTIME('%Y-%m-%d %H:%M:%f', Visit_time) AS Visit_time,
                //           STRFTIME('%Y-%m-%d %H:%M:%f', Last_visit_time) AS Last_visit_time,
                //           Visit_count, Transition, Visit_type, Frecency, File
                //    FROM {tableName} WHERE Category = @Category AND Browser = @Browser" :
                //                $@"SELECT id, Browser, Category, Potential_activity, Visit_id, Place_id, Url, Title, 
                //           DATETIME(Visit_time, '{utcOffset} hours') AS Visit_time,
                //           DATETIME(Last_visit_time, '{utcOffset} hours') AS Last_visit_time,
                //           Visit_count, Transition, Visit_type, Frecency, File
                //    FROM {tableName} WHERE Category = @Category AND Browser = @Browser";
                //        }
                //        else if (tableName == "results")
                //        {
                //            query = utcOffset == 0 ?
                //                $@"SELECT id, Browser, Category, Potential_activity, Visit_id, Url, Title, 
                //           STRFTIME('%Y-%m-%d %H:%M:%f', Visit_time) AS Visit_time,
                //           STRFTIME('%Y-%m-%d %H:%M:%f', Last_visit_time) AS Last_visit_time,
                //           Visit_duration, Visit_count, Typed_count, From_url, Transition, File
                //    FROM {tableName} WHERE Category = @Category AND Browser = @Browser" :
                //                $@"SELECT id, Browser, Category, Potential_activity, Visit_id, Url, Title, 
                //           DATETIME(Visit_time, '{utcOffset} hours') AS Visit_time,
                //           DATETIME(Last_visit_time, '{utcOffset} hours') AS Last_visit_time,
                //           Visit_duration, Visit_count, Typed_count, From_url, Transition, File
                //    FROM {tableName} WHERE Category = @Category AND Browser = @Browser";
                //        }
                //    }
                //}

                //using (SQLiteCommand command = new SQLiteCommand(query, connection))
                //{
                //    command.Parameters.AddWithValue("@Browser", navegador);

                //    if (!string.IsNullOrEmpty(categoria))
                //    {
                //        command.Parameters.AddWithValue("@Category", categoria);
                //    }

                //    if (Helpers.searchTermExists)
                //    {
                //        command.Parameters.AddWithValue("@searchTerm", "%" + Helpers.searchTerm + "%");
                //    }

                //    using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(command))
                //    {
                //        DataTable dataTable = new DataTable();
                //        adapter.Fill(dataTable);
                //        sfDataGrid.DataSource = dataTable;
                //        labelItemCount.Text = $"Items count: {sfDataGrid.RowCount - 1}"; // Restamos 1 para no contar la fila de encabezado.

                //        // Asegurar que haya al menos una fila antes de establecer el foco
                //        if (sfDataGrid.View != null && sfDataGrid.View.Records.Count > 0)
                //        {
                //            sfDataGrid.SelectedIndex = 0; // Selecciona la primera fila
                //            sfDataGrid.MoveToCurrentCell(new RowColumnIndex(0, 0)); // Mueve el foco a la primera celda
                //        }

                //    }
                //}

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






        //public void FillDictionaryFromDatabase(Dictionary<string, List<string>> browserUrls)
        //{
        //    using (SQLiteConnection connection = new SQLiteConnection(Helpers.chromeViewerConnectionString))
        //    {
        //        connection.Open();

        //        // Consultar ambas tablas: results y firefox_results
        //        string query = "SELECT Browser, Url FROM results UNION ALL SELECT Browser, Url FROM firefox_results";

        //        using (SQLiteCommand command = new SQLiteCommand(query, connection))
        //        {
        //            using (SQLiteDataReader reader = command.ExecuteReader())
        //            {
        //                while (reader.Read())
        //                {
        //                    string browser = reader.GetString(0);
        //                    string url = reader.GetString(1);

        //                    if (!browserUrls.ContainsKey(browser))
        //                    {
        //                        browserUrls[browser] = new List<string>();
        //                    }

        //                    browserUrls[browser].Add(url);
        //                }
        //            }
        //        }
        //    }
        //}

        public Dictionary<string, List<string>> FillDictionaryFromDatabase()
        {
            // Inicializar el diccionario
            Helpers.browserUrls = new Dictionary<string, List<string>>();

            using (SQLiteConnection connection = new SQLiteConnection(Helpers.chromeViewerConnectionString))
            {
                connection.Open();

                // Consultar ambas tablas: results y firefox_results
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







        //public bool ProcessAndWriteRecordsEsent(
        //string esentFilePath,
        //string chromeViewerConnectionString,
        //string browserType,
        //string filepath)
        //{
        //    if (!File.Exists(esentFilePath))
        //    {
        //        return false;
        //    }

        //    var instance = new Instance("WebCacheV01");
        //    instance.Parameters.Recovery = false;
        //    instance.Parameters.NoInformationEvent = true;
        //    instance.Parameters.CircularLog = true;
        //    instance.Init();

        //    try
        //    {
        //        using (var session = new Session(instance))
        //        {
        //            JET_DBID dbid;
        //            Api.JetAttachDatabase(session, esentFilePath, AttachDatabaseGrbit.None);
        //            Api.JetOpenDatabase(session, esentFilePath, "", out dbid, OpenDatabaseGrbit.None);

        //            using (var transaction = new Transaction(session))
        //            {
        //                bool success = ExtractHistory(session, dbid, "Container_4", chromeViewerConnectionString, browserType);
        //                transaction.Commit(CommitTransactionGrbit.LazyFlush);
        //                return success;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // LogToConsole method will not be used as per instruction.
        //        return false;
        //    }
        //    finally
        //    {
        //        instance.Term();
        //    }
        //}

        //private bool ExtractHistory(Session session, JET_DBID dbid, string tableName, string chromeViewerConnectionString, string browserType)
        //{
        //    JET_TABLEID tableid;
        //    Api.JetOpenTable(session, dbid, tableName, null, 0, OpenTableGrbit.None, out tableid);

        //    var columns = Api.GetColumnDictionary(session, tableid);

        //    var urlColumn = columns["Url"];
        //    var accessTimeColumn = columns["AccessedTime"];

        //    bool success = false;

        //    using (var sqliteConnection = new SQLiteConnection(chromeViewerConnectionString))
        //    {
        //        sqliteConnection.Open();

        //        string insertQuery = "INSERT INTO BrowsingHistory (Url, AccessedTime, BrowserType) VALUES (@Url, @AccessedTime, @BrowserType)";
        //        using (var sqliteCommand = new SQLiteCommand(insertQuery, sqliteConnection))
        //        {
        //            JET_err moveResult;
        //            do
        //            {
        //                moveResult = Api.JetMove(session, tableid, JET_Move.Next, MoveGrbit.None);
        //                if (moveResult == JET_err.NoCurrentRecord)
        //                {
        //                    break;
        //                }

        //                string url = Api.RetrieveColumnAsString(session, tableid, urlColumn);
        //                DateTime? accessedTime = Api.RetrieveColumnAsDateTime(session, tableid, accessTimeColumn);

        //                if (url != null && accessedTime.HasValue)
        //                {
        //                    sqliteCommand.Parameters.Clear();
        //                    sqliteCommand.Parameters.AddWithValue("@Url", url);
        //                    sqliteCommand.Parameters.AddWithValue("@AccessedTime", accessedTime.Value);
        //                    sqliteCommand.Parameters.AddWithValue("@BrowserType", browserType);
        //                    sqliteCommand.ExecuteNonQuery();
        //                    success = true;
        //                }

        //            } while (moveResult == JET_err.Success);
        //        }
        //    }

        //    Api.JetCloseTable(session, tableid);

        //    return success;
        //}




        public static Dictionary<string, int> GetBrowsersWithDownloads()
        {
            Dictionary<string, int> browsersWithDownloads = new Dictionary<string, int>();

            string queryChrome = "SELECT Browser, COUNT(*) FROM chrome_downloads GROUP BY Browser;";
            string queryFirefox = "SELECT Browser, COUNT(*) FROM firefox_downloads GROUP BY Browser;";

            using (SQLiteConnection connection = new SQLiteConnection(Helpers.chromeViewerConnectionString))
            {
                connection.Open();

                // Verificar navegadores en chrome_downloads
                using (SQLiteCommand command = new SQLiteCommand(queryChrome, connection))
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string browser = reader.GetString(0);
                        int count = reader.GetInt32(1);

                        if (Helpers.browsersWithDownloads.ContainsKey(browser))
                        {
                            browsersWithDownloads[browser] += count;
                        }
                        else
                        {
                            Helpers.browsersWithDownloads[browser] = count;
                        }
                    }
                }

                // Verificar navegadores en firefox_downloads
                using (SQLiteCommand command = new SQLiteCommand(queryFirefox, connection))
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string browser = reader.GetString(0);
                        int count = reader.GetInt32(1);

                        if (Helpers.browsersWithDownloads.ContainsKey(browser))
                        {
                            Helpers.browsersWithDownloads[browser] += count;
                        }
                        else
                        {
                            Helpers.browsersWithDownloads[browser] = count;
                        }
                    }
                }
            }

            return Helpers.browsersWithDownloads;
        }


        public static Dictionary<string, int> GetBrowsersWithBookmarks()
        {
            // Inicializar el diccionario
            Dictionary<string, int> browsersWithBookmarks = new Dictionary<string, int>();

            string queryChrome = "SELECT Browser, COUNT(*) FROM bookmarks_Chrome GROUP BY Browser;";
            string queryFirefox = "SELECT Browser, COUNT(*) FROM bookmarks_Firefox GROUP BY Browser;";

            using (SQLiteConnection connection = new SQLiteConnection(Helpers.chromeViewerConnectionString))
            {
                connection.Open();

                // Verificar navegadores en bookmarks_Chrome
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

                // Verificar navegadores en bookmarks_Firefox
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
            Dictionary<string, int> browsersWithAutofill = new Dictionary<string, int>();

            string queryAutofill = "SELECT Browser FROM autofill_data;";

            using (SQLiteConnection connection = new SQLiteConnection(Helpers.chromeViewerConnectionString))
            {
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand(queryAutofill, connection))
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string browser = reader.GetString(0);

                        if (browsersWithAutofill.ContainsKey(browser))
                        {
                            browsersWithAutofill[browser]++;
                        }
                        else
                        {
                            browsersWithAutofill[browser] = 1;
                        }
                    }
                }
            }

            return browsersWithAutofill;
        }


        //public static List<string> GetBrowsersWithAutofill()
        //{
        //    List<string> browsersWithAutofill = new List<string>();

        //    string queryAutofill = "SELECT DISTINCT Browser FROM autofill_data;";

        //    using (SQLiteConnection connection = new SQLiteConnection(Helpers.chromeViewerConnectionString))
        //    {
        //        connection.Open();

        //        // Verificar navegadores en autofill_data
        //        using (SQLiteCommand command = new SQLiteCommand(queryAutofill, connection))
        //        using (SQLiteDataReader reader = command.ExecuteReader())
        //        {
        //            while (reader.Read())
        //            {
        //                string browser = reader.GetString(0);
        //                if (!browsersWithAutofill.Contains(browser))
        //                {
        //                    browsersWithAutofill.Add(browser);
        //                }
        //            }
        //        }
        //    }

        //    return browsersWithAutofill;
        //}

        //public static void ProcessAndWriteBookmarksChrome(string chromeViewerConnectionString, string browserType, string filePath, RichTextBox logConsole = null)
        //{
        //    if (File.Exists(filePath))
        //    {
        //        try
        //        {
        //            // Leer el archivo como un flujo de bytes para manejar posibles problemas de codificación
        //            byte[] fileBytes = File.ReadAllBytes(filePath);
        //            string jsonContent = Encoding.UTF8.GetString(fileBytes);

        //            // Eliminar el BOM si está presente
        //            jsonContent = jsonContent.TrimStart(new char[] { '\uFEFF' });

        //            // Intentar deserializar el JSON sin validación previa
        //            var bookmarksData = JsonConvert.DeserializeObject<JObject>(jsonContent);

        //            if (bookmarksData != null && bookmarksData.ContainsKey("roots"))
        //            {
        //                var roots = bookmarksData["roots"] as JObject;

        //                using (SQLiteConnection connection = new SQLiteConnection(chromeViewerConnectionString))
        //                {
        //                    connection.Open();
        //                    using (var transaction = connection.BeginTransaction()) // Inicia la transacción
        //                    {
        //                        foreach (var root in roots)
        //                        {
        //                            var rootFolder = root.Value as JObject;
        //                            ProcessFolderChrome(rootFolder, null, connection, browserType, filePath);
        //                        }
        //                        transaction.Commit(); // Finaliza la transacción
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                //MessageBox.Show("No se encontró la clave 'roots' en el archivo JSON.", "ChromeViewer", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //                if (logConsole != null)
        //                {
        //                    logConsole.AppendText("No se encontró la clave 'roots' en el archivo JSON.\n");
        //                }
        //                else Console.WriteLine("No se encontró la clave 'roots' en el archivo JSON.");

        //            }
        //        }
        //        catch (JsonReaderException jex)
        //        {
        //            MessageBox.Show($"Error de JSON: {jex.Message}", "ChromeViewer", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //        }
        //        catch (Exception ex)
        //        {
        //            MessageBox.Show($"Error general: {ex.Message}", "ChromeViewer", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //        }
        //    }
        //    else
        //    {
        //        MessageBox.Show("Bookmarks file not found.", "ChromeViewer", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //    }
        //}


        public static void ProcessAndWriteBookmarksChrome(string chromeViewerConnectionString, string browserType, string filePath, RichTextBox logConsole = null)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    // Read the file as byte stream to handle potential encoding issues
                    byte[] fileBytes = File.ReadAllBytes(filePath);
                    string jsonContent = Encoding.UTF8.GetString(fileBytes);

                    // Remove BOM if present
                    jsonContent = jsonContent.TrimStart(new char[] { '\uFEFF' });

                    // Try to deserialize JSON without prior validation
                    var bookmarksData = JsonConvert.DeserializeObject<JObject>(jsonContent);

                    if (bookmarksData != null && bookmarksData.ContainsKey("roots"))
                    {
                        var roots = bookmarksData["roots"] as JObject;

                        using (SQLiteConnection connection = new SQLiteConnection(chromeViewerConnectionString))
                        {
                            connection.Open();
                            using (var transaction = connection.BeginTransaction())
                            {
                                foreach (var root in roots)
                                {
                                    var rootFolder = root.Value as JObject;
                                    ProcessFolderChrome(rootFolder, null, connection, browserType, filePath);
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




        private static void ProcessFolderChrome(JObject folder, string parentName, SQLiteConnection connection, string browserType, string filePath)
        {
            if (folder == null) return;

            string folderName = folder["name"]?.ToString();
            string dateAdded = folder["date_added"]?.ToString();
            string dateLastUsed = folder["date_last_used"]?.ToString();
            string dateModified = folder["date_modified"]?.ToString();
            string guid = folder["guid"]?.ToString();
            string folderId = folder["id"]?.ToString();
            string type = folder["type"]?.ToString();

            DateTime? dateAddedConverted = ChromeDateToDateTime(dateAdded);
            DateTime? dateLastUsedConverted = ChromeDateToDateTime(dateLastUsed);
            DateTime? dateModifiedConverted = ChromeDateToDateTime(dateModified);

            if (type == "folder")
            {
                string insertFolderQuery = @"INSERT INTO bookmarks_Chrome 
                                     (Browser, Type, Title, DateAdded, DateLastUsed, LastModified, Parent_name, Guid, ChromeId, File)
                                     VALUES 
                                     (@Browser, @Type, @Title, @DateAdded, @DateLastUsed, @LastModified, @Parent_name, @Guid, @ChromeId, @File)";

                using (SQLiteCommand command = new SQLiteCommand(insertFolderQuery, connection))
                {
                    command.Parameters.AddWithValue("@Browser", browserType);
                    command.Parameters.AddWithValue("@Type", "Folder");
                    command.Parameters.AddWithValue("@Title", folderName);
                    command.Parameters.AddWithValue("@DateAdded", dateAddedConverted.HasValue ? dateAddedConverted.Value.ToString("yyyy-MM-dd HH:mm:ss") : (object)DBNull.Value);
                    command.Parameters.AddWithValue("@DateLastUsed", dateLastUsedConverted.HasValue ? dateLastUsedConverted.Value.ToString("yyyy-MM-dd HH:mm:ss") : (object)DBNull.Value);
                    command.Parameters.AddWithValue("@LastModified", dateModifiedConverted.HasValue ? dateModifiedConverted.Value.ToString("yyyy-MM-dd HH:mm:ss") : (object)DBNull.Value);
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

                        ProcessFolderChrome(childObject, folderName, connection, browserType, filePath);
                    }
                }
            }
            else if (type == "url")
            {
                string url = folder["url"]?.ToString();

                string insertBookmarkQuery = @"INSERT INTO bookmarks_Chrome 
                                       (Browser, Type, Title, URL, DateAdded, DateLastUsed, LastModified, Parent_name, Guid, ChromeId, File)
                                       VALUES 
                                       (@Browser, @Type, @Title, @URL, @DateAdded, @DateLastUsed, @LastModified, @Parent_name, @Guid, @ChromeId, @File)";

                using (SQLiteCommand command = new SQLiteCommand(insertBookmarkQuery, connection))
                {
                    command.Parameters.AddWithValue("@Browser", browserType);
                    command.Parameters.AddWithValue("@Type", "Bookmark");
                    command.Parameters.AddWithValue("@Title", folderName);
                    command.Parameters.AddWithValue("@URL", url);
                    command.Parameters.AddWithValue("@DateAdded", dateAddedConverted.HasValue ? dateAddedConverted.Value.ToString("yyyy-MM-dd HH:mm:ss") : (object)DBNull.Value);
                    command.Parameters.AddWithValue("@DateLastUsed", dateLastUsedConverted.HasValue ? dateLastUsedConverted.Value.ToString("yyyy-MM-dd HH:mm:ss") : (object)DBNull.Value);
                    command.Parameters.AddWithValue("@LastModified", dateModifiedConverted.HasValue ? dateModifiedConverted.Value.ToString("yyyy-MM-dd HH:mm:ss") : (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Parent_name", parentName ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Guid", guid);
                    command.Parameters.AddWithValue("@ChromeId", folderId);
                    command.Parameters.AddWithValue("@File", filePath);
                    command.ExecuteNonQuery();
                }
            }
        }

        private static DateTime? ChromeDateToDateTime(string chromeDate)
        {
            if (long.TryParse(chromeDate, out long microseconds))
            {
                DateTime epoch = new DateTime(1601, 1, 1);
                return epoch.AddTicks(microseconds * 10); // Convertir a ticks
            }
            return null;
        }



        //Pruebas para autofill

        //Chrome

        public static void ProcessAndWriteAutofillChrome(string chromeViewerConnectionString, string browserType, string filePath, RichTextBox logConsole = null)
        {
            string autofillDbPath = Path.Combine(Path.GetDirectoryName(filePath), "Web Data");

            if (File.Exists(autofillDbPath))
            {
                using (SQLiteConnection chromeAutofillConnection = new SQLiteConnection($"Data Source={autofillDbPath};Version=3;"))
                {
                    chromeAutofillConnection.Open();

                    string queryAutofill = @"SELECT name, value, count, date_last_used FROM autofill";

                    using (SQLiteCommand commandAutofill = new SQLiteCommand(queryAutofill, chromeAutofillConnection))
                    using (SQLiteDataReader readerAutofill = commandAutofill.ExecuteReader())
                    {
                        using (SQLiteConnection chromeViewerConnection = new SQLiteConnection(chromeViewerConnectionString))
                        {
                            chromeViewerConnection.Open();

                            while (readerAutofill.Read())
                            {
                                string fieldName = readerAutofill.GetString(0);
                                string value = readerAutofill.GetString(1);
                                int count = readerAutofill.GetInt32(2);
                                long dateLastUsedMicroseconds = readerAutofill.GetInt64(3);

                                // Convertir "Chrome time" (microsegundos desde 1601) a DateTime
                                DateTime lastUsed = new DateTime(1601, 1, 1).AddMilliseconds(dateLastUsedMicroseconds / 1000.0);

                                string insertAutofill = @"INSERT INTO autofill_data (Browser, FieldName, Value, Count, LastUsed, File)
                                          VALUES (@Browser, @FieldName, @Value, @Count, @LastUsed, @File)";

                                using (SQLiteCommand insertCommand = new SQLiteCommand(insertAutofill, chromeViewerConnection))
                                {
                                    insertCommand.Parameters.AddWithValue("@Browser", browserType);
                                    insertCommand.Parameters.AddWithValue("@FieldName", fieldName);
                                    insertCommand.Parameters.AddWithValue("@Value", value);
                                    insertCommand.Parameters.AddWithValue("@Count", count);
                                    insertCommand.Parameters.AddWithValue("@LastUsed", lastUsed.ToString("yyyy-MM-dd HH:mm:ss"));
                                    insertCommand.Parameters.AddWithValue("@File", autofillDbPath);

                                    insertCommand.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("[Browser Reviewer - Error]: Chrome autocomplete data file not found.");
            }
        }



        



        public static void ProcessAndWriteAutofillFirefox(string chromeViewerConnectionString, string browserType, string filePath, RichTextBox logConsole = null)
        {
            string autofillDbPath = Path.Combine(Path.GetDirectoryName(filePath), "formhistory.sqlite");

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

                                        // Interpretar firstUsed y lastUsed como microsegundos y dividir por 1000
                                        long firstUsedMicroseconds = readerAutofill.GetInt64(3);
                                        long lastUsedMicroseconds = readerAutofill.GetInt64(4);

                                        // Convertir microsegundos a DateTime dividiendo por 1000 para convertir a milisegundos
                                        DateTime firstUsedDate = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(firstUsedMicroseconds / 1000.0);
                                        DateTime lastUsedDate = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(lastUsedMicroseconds / 1000.0);

                                        string insertAutofill = @"INSERT INTO autofill_data (Browser, FieldName, Value, TimesUsed, FirstUsed, LastUsed, File)
                                              VALUES (@Browser, @FieldName, @Value, @TimesUsed, @FirstUsed, @LastUsed, @File)";

                                        using (SQLiteCommand insertCommand = new SQLiteCommand(insertAutofill, firefoxViewerConnection))
                                        {
                                            insertCommand.Parameters.AddWithValue("@Browser", browserType);
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
                                    //MessageBox.Show($"Error: {ex.Message}", "ChromeViewer", MessageBoxButtons.OK, MessageBoxIcon.Error);
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




        //Funciones que cuentan el numero de hits y los ponen en los labels


        // Fuincion actualizacion labels de history para mostror numero de hits

        public void UpdateNavegadorLabel(string navegadorKey, string newText)
        {
            if (Helpers.navegadorLabels.TryGetValue(navegadorKey, out Label lbl))
            {
                lbl.Text = newText;
                //lbl.BackColor = newBackColor;
            }
        }

        // Fuincion actualizacion labels de Downloads para mostror numero de hits

        public void UpdateDownloadsLabel(string navegadorKey, string newText)
        {
            if (Helpers.downloadsLabels.TryGetValue(navegadorKey, out Label lbl))
            {
                lbl.Text = newText;
                //lbl.BackColor = newBackColor;
            }
        }



        // Fuincion actualizacion labels de Bookmarks para mostror numero de hits

        public void UpdateBookmarksLabel(string navegadorKey, string newText)
        {
            if (Helpers.bookmarksLabels.TryGetValue(navegadorKey, out Label lbl))
            {
                lbl.Text = newText;
                //lbl.BackColor = newBackColor;
            }
        }







        //Obtener el numero de registros de navegacion por navegador

        public int NumUrlsWithBrowser(string browserName)
        {
            int count = 0;

            string query = @"
            SELECT COUNT(*) 
            FROM (
                SELECT * FROM results WHERE Browser = @browserName
                UNION
                SELECT * FROM firefox_results WHERE Browser = @browserName
            ) AS CombinedResults;
        ";

            using (var connection = new SQLiteConnection(Helpers.chromeViewerConnectionString))
            {
                connection.Open();

                using (var command = new SQLiteCommand(query, connection))
                {
                    // Asignar el parámetro para evitar inyección SQL
                    command.Parameters.AddWithValue("@browserName", browserName);

                    // Ejecutar la consulta y obtener el resultado
                    count = Convert.ToInt32(command.ExecuteScalar());
                }
            }

            return count;
        }

        //Obtener el numero de registros de descargas por navegador

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
                    // Asignar el parámetro para evitar inyección SQL
                    command.Parameters.AddWithValue("@browserName", browserName);

                    // Ejecutar la consulta y obtener el resultado
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
                    // Asignar el parámetro para evitar inyección SQL
                    command.Parameters.AddWithValue("@browserName", browserName);

                    // Ejecutar la consulta y obtener el resultado
                    count = Convert.ToInt32(command.ExecuteScalar());
                }
            }

            return count;
        }




    }

}
