using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Browser_Reviewer
{
    public class Helpers
    {
        //Variable de conexion a la base de datos del rpoyecto tabla resume
        public static string historyConnectionString = "";

        public static string chromeViewerConnectionString = "";

        public static string BrowserType = "";

        //Variables para el Menu acordeon, 4 diccionarios que se pasaran como parametro

        public static Dictionary<string, List<string>> browserUrls = new Dictionary<string, List<string>>();

        //Guarda un string p.e firefox , chrome, etc y una lista de strings que son las url de cada uno
        
        //public static Dictionary<string, List<string>> savebrowserUrls = new Dictionary<string, List<string>>();

        public static Dictionary<string, int> browsersWithDownloads = new Dictionary<string, int>();

       // public static Dictionary<string, int> savebrowsersWithDownloads = new Dictionary<string, int>();

        public static Dictionary<string, int> browsersWithBookmarks = new Dictionary<string, int>();

        //public static Dictionary<string, int> savebrowsersWithBookmarks = new Dictionary<string, int>();

        //public static List<string> browsersWithAutofill = new List<string>();

        public static Dictionary<string, int> browsersWithAutofill = new Dictionary<string, int>();

        //public static List<string> savebrowsersWithAutofill = new List<string>();

        //Hasta aqui variables Menu acordeon

        public static Image icono;

        public static int utcOffset = 0;

        public static string db_name = "";

        public static bool searchTermExists = false;

        public static bool searchTermRegExp = false;

        public static string searchTerm = "";

        public static bool searchTimeCondition = false;


        
        public static string realFirefoxPlacesPath = "";
        
        public static string realFirefoxFormPath = "";

        public static string comment = "";

        public static int itemscount = 0;

        public static int itemscountinSearch = 0;

        //Diccionario para guardar el numero de hits de busqueda en historial por navegador

        public static Dictionary<string, int> historyHits = new Dictionary<string, int>();

        //Diccionario para guardar el numero de hits de busqueda en downloads por navegador

        public static Dictionary<string, int> downloadsHits = new Dictionary<string, int>();

        //Diccionario para guardar la referencia a los labels geenrados dinamicamente en history  

        public static Dictionary<string, Label> navegadorLabels = new Dictionary<string, Label>();

        //Diccionario para guardar la referencia a los labels geenrados dinamicamente en Downloads

        public static Dictionary<string, Label> downloadsLabels = new Dictionary<string, Label>();

        //Diccionario para guardar la referencia a los labels geenrados dinamicamente en Bookmarks

        public static Dictionary<string, Label> bookmarksLabels = new Dictionary<string, Label>();

        //Diccionario para guardar la referencia a los labels geenrados dinamicamente en Autofill

        public static Dictionary<string, Label> AutofillksLabels = new Dictionary<string, Label>();


        //Valriables para los Labels

        public static DataTable dataTable;

        public static SQLiteDataAdapter dataAdapter;

        public static DataTable labelsTable;

        //Variables para el tiempo

        public static DateTime StartDate = DateTime.Now.AddYears(-20);
        public static DateTime EndDate = DateTime.Now;
        public static string sd;
        public static string ed;


        //Valiables para cadenas sql de confdicion de tiempo

        public static string sqltimecondition = ""; // vacío por defecto
        public static string sqlDownloadtimecondition = "";
        public static string sqlBookmarkstimecondition = "";
        public static string sqlAutofilltimecondition = "";



        //VAriables para los Labels, encontrar la tabla



       public static Dictionary<string, List<string>> TablesAndFields = new Dictionary<string, List<string>>
    {
        { "results", new List<string> {
            "id", "Browser", "Category", "Potential_activity", "Visit_id", "Url", "Title",
            "Visit_time", "Visit_duration", "Last_visit_time", "Visit_count", "Typed_count",
            "From_url", "Transition", "File", "Label", "Comment"
        }},

        { "firefox_results", new List<string> {
            "id", "Browser", "Category", "Potential_activity", "Visit_id", "Place_id", "Url", "Title",
            "Visit_time", "Last_visit_time", "Visit_count", "Transition", "Visit_type", "Frecency",
            "File", "Label", "Comment"
        }},

        { "firefox_downloads", new List<string> {
            "id", "Browser", "Download_id", "Current_path", "End_time", "Last_visit_time",
            "Received_bytes", "Total_bytes", "Source_url", "Title", "State", "File", "Label", "Comment"
        }},

        { "chrome_downloads", new List<string> {
            "id", "Browser", "Current_path", "Target_path", "Start_time", "End_time",
            "Received_bytes", "Total_bytes", "State", "opened", "referrer", "Site_url", "Tab_url",
            "Mime_type", "File", "Label", "Comment"
        }},

        { "bookmarks_Firefox", new List<string> {
            "id", "Browser", "Type", "Title", "URL", "DateAdded", "LastModified", "Parent_name", "File", "Label", "Comment"
        }},

        { "bookmarks_Chrome", new List<string> {
            "id", "Browser", "Type", "Title", "URL", "DateAdded", "LastModified", "Parent_name", "File", "Label", "Comment"

        }},

        { "autofill_data", new List<string> {
            "id", "Browser", "FieldName", "Value", "TimesUsed",  "FirstUsed",  "LastUsed",
            "File", "Label", "Comment"
        }},

        //Diccionarios para las vistas de All History / dwld / bkmarks, autofill

        { "AllWebHistory", new List<string> {
            "id", "Browser", "Category", "Potential_activity", "Visit_id", "Url",  "Title",  "Visit_time",
            "File", "Label", "Comment"
        }},

        { "AllDownloads", new List<string> {
            "id", "Browser", "Current_path", "End_time", "Received_bytes", "Total_bytes",
            "File", "Label", "Comment"
        }},

        { "AllBookmarks", new List<string> {
            "id", "Browser", "Type", "Title", "URL", "DateAdded", "LastModified", "Parent_name",
            "File", "Label", "Comment"
        }},

        { "AllAutoFill", new List<string> {
            "id", "Browser", "FieldName", "Value", "TimesUsed", "FirstUsed", "LastUsed",
            "File", "Label", "Comment"
        }}
    };



    }
}
