(function () {
  const families = {
    "Chrome-like browsers": {
      sourceKey: "Chromium",
      icon: "../Chrome.ico",
      includes: { en: "Chrome, Brave, Edge, Opera, Yandex, Vivaldi and compatible Chromium profiles.", es: "Chrome, Brave, Edge, Opera, Yandex, Vivaldi y perfiles Chromium compatibles." },
      hint: { en: "They are grouped because they store browser evidence in very similar places and formats.", es: "Se agrupan porque guardan la evidencia del navegador en lugares y formatos muy parecidos." }
    },
    "Firefox-like browsers": {
      sourceKey: "Firefox",
      icon: "../Morcha-Browsers-Firefox.256.png",
      includes: { en: "Firefox and compatible Mozilla-style profiles.", es: "Firefox y perfiles compatibles de estilo Mozilla." },
      hint: { en: "They are grouped separately because Firefox uses its own profile files and storage formats.", es: "Se agrupan aparte porque Firefox usa sus propios archivos de perfil y formatos de almacenamiento." }
    }
  };

  const artifacts = {
    history: {
      name: { en: "History", es: "Historial" },
      confidence: "High",
      source: { Chromium: "History SQLite: urls, visits.", Firefox: "places.sqlite: moz_places, moz_historyvisits." },
      activity: { en: "Someone or something in the browser opened a web page or web address.", es: "Alguien o algo dentro del navegador abrio una pagina o direccion web." },
      summary: { en: "History is the list of pages the browser remembers as visited.", es: "El historial es la lista de paginas que el navegador recuerda como visitadas." },
      example: { en: "If the URL is https://www.google.com/search?q=weather, the browser reached a Google search page for weather.", es: "Si la URL es https://www.google.com/search?q=clima, el navegador llego a una pagina de busqueda de Google sobre clima." },
      caution: { en: "A history row is strong evidence that the browser reached a page, but not always proof that the user typed it manually.", es: "Un registro de historial es buena evidencia de que el navegador llego a una pagina, pero no siempre prueba que el usuario la escribio manualmente." },
      fields: ["Category", "Visit_id", "Place_id", "Url", "Title", "Visit_time", "Last_visit_time", "Visit_count", "Visit_duration", "Transition", "Navigation_context", "User_action_likelihood", "Typed_count", "From_url", "From_visit", "Visit_type", "Frecency"]
    },
    downloads: {
      name: { en: "Downloads", es: "Descargas" },
      confidence: "High",
      source: { Chromium: "History SQLite: downloads and downloads_url_chains.", Firefox: "places.sqlite download annotations." },
      activity: { en: "The browser started or recorded a file download.", es: "El navegador inicio o registro una descarga de archivo." },
      summary: { en: "Downloads show files the browser tried to save from the web.", es: "Descargas muestra archivos que el navegador intento guardar desde internet." },
      example: { en: "A row with Target_path C:\\Users\\Ana\\Downloads\\report.pdf means the browser tried to save report.pdf in Downloads.", es: "Una fila con Target_path C:\\Users\\Ana\\Downloads\\report.pdf significa que el navegador intento guardar report.pdf en Descargas." },
      caution: { en: "The downloaded file may no longer exist, but the browser record can still show where it came from.", es: "El archivo descargado puede ya no existir, pero el registro del navegador puede mostrar de donde vino." },
      fields: ["Download_id", "Guid", "Target_path", "Current_path", "Url", "Url_chain", "Referrer", "Tab_url", "Mime_type", "Start_time", "End_time", "Received_bytes", "Total_bytes", "State", "Danger_type", "Interrupt_reason", "Opened", "By_ext_id", "By_ext_name"]
    },
    bookmarks: {
      name: { en: "Bookmarks", es: "Marcadores" },
      confidence: "High",
      source: { Chromium: "Bookmarks JSON file.", Firefox: "places.sqlite bookmark tables." },
      activity: { en: "A web page was saved as a favorite/bookmark or organized in a bookmark folder.", es: "Una pagina web fue guardada como favorito/marcador u organizada en una carpeta." },
      summary: { en: "Bookmarks are pages the user or browser saved for later.", es: "Los marcadores son paginas que el usuario o navegador guardo para despues." },
      example: { en: "A bookmark titled Online Banking with a bank URL may show a site the user wanted to keep accessible.", es: "Un marcador llamado Online Banking con URL de banco puede mostrar un sitio que el usuario queria tener a mano." },
      caution: { en: "A bookmark shows interest or saving behavior, but not necessarily that the page was visited at the same moment.", es: "Un marcador muestra interes o guardado, pero no necesariamente que la pagina se visito en ese mismo momento." },
      fields: ["Bookmark_id", "Type", "Parent", "Parent_name", "Title", "URL", "DateAdded", "DateLastUsed", "LastModified", "VisitCount", "LastVisitDate", "Guid", "ChromeId", "AnnoName", "AnnoContent"]
    },
    autofill: {
      name: { en: "Autofill", es: "Autocompletado" },
      confidence: "High",
      source: { Chromium: "Web Data SQLite autofill tables.", Firefox: "formhistory.sqlite." },
      activity: { en: "The browser saved text typed into a web form.", es: "El navegador guardo texto escrito en un formulario web." },
      summary: { en: "Autofill helps fill forms again, such as names, emails, usernames or addresses.", es: "Autocompletado ayuda a llenar formularios otra vez, como nombres, correos, usuarios o direcciones." },
      example: { en: "FieldName username and Value ana@example.com means the browser saved that text for a form field named username.", es: "FieldName username y Value ana@example.com significa que el navegador guardo ese texto para un campo llamado username." },
      caution: { en: "Autofill can contain personal data. Treat it carefully and compare it with visited pages.", es: "Autofill puede contener datos personales. Tratelo con cuidado y comparelo con paginas visitadas." },
      fields: ["FieldName", "Value", "Count", "TimesUsed", "FirstUsed", "LastUsed"]
    },
    cookies: {
      name: { en: "Cookies", es: "Cookies" },
      confidence: "High",
      source: { Chromium: "Cookies SQLite database.", Firefox: "cookies.sqlite." },
      activity: { en: "A website stored a small data item in the browser.", es: "Un sitio web guardo un pequeno dato en el navegador." },
      summary: { en: "Cookies are small records websites use for sessions, preferences, tracking or login state.", es: "Las cookies son pequenos registros que los sitios usan para sesiones, preferencias, seguimiento o estado de login." },
      example: { en: "Host accounts.google.com and Name SID means Google stored a cookie named SID for that host.", es: "Host accounts.google.com y Name SID significa que Google guardo una cookie llamada SID para ese host." },
      caution: { en: "A cookie can be created by the main site, an advertisement, an embedded service or background activity.", es: "Una cookie puede ser creada por el sitio principal, publicidad, un servicio embebido o actividad en segundo plano." },
      fields: ["Host", "Name", "Value", "Path", "Created", "Expires", "LastAccessed", "IsSecure", "IsHttpOnly", "SameSite", "SourcePort"]
    },
    cache: {
      name: { en: "Cache", es: "Cache" },
      confidence: "Medium",
      source: { Chromium: "Chromium cache directories, simple cache and block cache when recoverable.", Firefox: "Firefox cache2 entries directory." },
      activity: { en: "The browser saved a copy of a web resource to load it faster later.", es: "El navegador guardo una copia de un recurso web para cargarlo mas rapido despues." },
      summary: { en: "Cache can contain pieces of pages: images, videos, scripts, documents, HTML and other files.", es: "El cache puede contener partes de paginas: imagenes, videos, scripts, documentos, HTML y otros archivos." },
      example: { en: "A cache row with ContentType image/webp and BodyBlob may allow Browser Reviewer to preview the recovered image.", es: "Una fila de cache con ContentType image/webp y BodyBlob puede permitir que Browser Reviewer muestre la imagen recuperada." },
      caution: { en: "Cache proves the browser stored a resource, but the user may not have directly opened that specific file.", es: "El cache prueba que el navegador guardo un recurso, pero el usuario puede no haber abierto directamente ese archivo especifico." },
      fields: ["Url", "Host", "ContentType", "CacheType", "HttpStatus", "Server", "FileSize", "Created", "LastModified", "LastAccessed", "Expires", "ETag", "ContentEncoding", "BodySize", "BodySha256", "BodyExtension", "BodyPreview", "BodyBlob"]
    },
    sessions: {
      name: { en: "Sessions", es: "Sesiones" },
      confidence: "Medium",
      source: { Chromium: "Current Session, Last Session and compatible session files.", Firefox: "sessionstore.jsonlz4 and sessionstore-backups." },
      activity: { en: "The browser remembered an open tab, window or page for session restore.", es: "El navegador recordo una pestana, ventana o pagina para restaurar la sesion." },
      summary: { en: "Sessions show tabs and pages the browser may reopen after closing or crashing.", es: "Sesiones muestra pestanas y paginas que el navegador podria reabrir despues de cerrar o fallar." },
      example: { en: "If a session row shows webmail.example.com, that page may have been open in a restored browser tab.", es: "Si una fila de sesion muestra webmail.example.com, esa pagina pudo estar abierta en una pestana restaurada." },
      caution: { en: "A session entry may be old. It is a clue about browser state, not automatic proof of recent viewing.", es: "Una entrada de sesion puede ser antigua. Es una pista del estado del navegador, no prueba automatica de visualizacion reciente." },
      fields: ["WindowIndex", "TabIndex", "EntryIndex", "Selected", "Url", "Title", "OriginalUrl", "Referrer", "Timestamp", "SourceType", "SessionFile"]
    },
    extensions: {
      name: { en: "Extensions", es: "Extensiones" },
      confidence: "High",
      source: { Chromium: "Extensions folders, manifest.json, Preferences and Secure Preferences.", Firefox: "extensions.json and add-on metadata." },
      activity: { en: "An extension/add-on was installed, enabled or present in the browser profile.", es: "Una extension/complemento estaba instalada, habilitada o presente en el perfil del navegador." },
      summary: { en: "Extensions add features to the browser and may read pages, manage downloads or store data depending on permissions.", es: "Las extensiones agregan funciones al navegador y pueden leer paginas, manejar descargas o guardar datos segun permisos." },
      example: { en: "An extension with downloads permission could manage downloads, but that permission alone does not prove it downloaded a file.", es: "Una extension con permiso downloads podria manejar descargas, pero ese permiso solo no prueba que descargo un archivo." },
      caution: { en: "Having permission to do something is not proof the extension actually did it.", es: "Tener permiso para hacer algo no prueba que la extension realmente lo hizo." },
      fields: ["ExtensionId", "Name", "Version", "Description", "Author", "HomepageUrl", "UpdateUrl", "Enabled", "InstallTime", "UpdateTime", "Permissions", "HostPermissions", "ManifestVersion", "SourceFile", "ExtensionPath"]
    },
    "saved-logins": {
      name: { en: "Saved logins", es: "Logins guardados" },
      confidence: "Medium",
      source: { Chromium: "Login Data SQLite database.", Firefox: "logins.json and key database presence." },
      activity: { en: "The browser stored or remembered login information for a website.", es: "El navegador guardo o recordo informacion de login para un sitio web." },
      summary: { en: "Saved logins show accounts or login forms remembered by the browser.", es: "Logins guardados muestra cuentas o formularios de acceso recordados por el navegador." },
      example: { en: "Username ana@example.com and Signon_realm https://accounts.example.com/ means the browser remembered login data for that site.", es: "Username ana@example.com y Signon_realm https://accounts.example.com/ significa que el navegador recordo datos de login para ese sitio." },
      caution: { en: "This artifact is highly sensitive. A saved login record does not by itself prove the account was used at a specific time.", es: "Este artefacto es muy sensible. Un login guardado no prueba por si solo que la cuenta se uso en una hora especifica." },
      fields: ["Url", "Action_url", "Signon_realm", "Username", "Username_field", "Password_value", "Password_status", "Created", "LastUsed", "PasswordModified", "TimesUsed", "Scheme", "BlacklistedByUser", "FormSubmitUrl"]
    },
    "local-storage": {
      name: { en: "Local Storage", es: "Local Storage" },
      confidence: "Medium",
      source: { Chromium: "Local Storage LevelDB.", Firefox: "webappsstore.sqlite." },
      activity: { en: "A website saved data locally in the browser.", es: "Un sitio web guardo datos localmente en el navegador." },
      summary: { en: "Local Storage is a website's small local database for settings, tokens, preferences or app state.", es: "Local Storage es una pequena base local del sitio para configuracion, tokens, preferencias o estado de aplicacion." },
      example: { en: "A key named theme with value dark may simply store a site preference; a key named token may be more sensitive.", es: "Una clave llamada theme con valor dark puede ser solo una preferencia; una clave llamada token puede ser mas sensible." },
      caution: { en: "Local Storage can be meaningful, but timestamps are often limited or unreliable.", es: "Local Storage puede ser importante, pero sus fechas suelen ser limitadas o poco confiables." },
      fields: ["Origin", "Host", "Storage_key", "Value_preview", "Value_size", "Created", "Modified", "LastAccessed", "Source_kind", "Source_path"]
    },
    "session-storage": {
      name: { en: "Session Storage", es: "Session Storage" },
      confidence: "Medium",
      source: { Chromium: "Session Storage LevelDB.", Firefox: "Best-effort extraction from sessionstore files." },
      activity: { en: "A website saved temporary data for the current browser session.", es: "Un sitio web guardo datos temporales para la sesion actual del navegador." },
      summary: { en: "Session Storage is like Local Storage, but meant to be temporary for a tab or session.", es: "Session Storage es parecido a Local Storage, pero pensado como temporal para una pestana o sesion." },
      example: { en: "A recovered key called checkoutStep may show temporary web app state during a shopping session.", es: "Una clave recuperada llamada checkoutStep puede mostrar estado temporal de una compra en una aplicacion web." },
      caution: { en: "It can disappear easily and may be incomplete in evidence.", es: "Puede desaparecer facilmente y estar incompleto en la evidencia." },
      fields: ["Origin", "Host", "Storage_key", "Value_preview", "Value_size", "Created", "Modified", "LastAccessed", "Source_kind", "Source_path"]
    },
    indexeddb: {
      name: { en: "IndexedDB", es: "IndexedDB" },
      confidence: "Medium",
      source: { Chromium: "IndexedDB LevelDB and backing files.", Firefox: "storage/default origin idb files." },
      activity: { en: "A website used a larger browser database to store application data.", es: "Un sitio web uso una base de datos mas grande del navegador para guardar datos de aplicacion." },
      summary: { en: "IndexedDB is used by modern web apps for offline data, cached records, messages, files or app state.", es: "IndexedDB es usado por aplicaciones web modernas para datos offline, registros cacheados, mensajes, archivos o estado de la app." },
      example: { en: "A webmail or chat app may store message previews, account state or cached attachments in IndexedDB.", es: "Una app de correo o chat puede guardar vistas de mensajes, estado de cuenta o adjuntos cacheados en IndexedDB." },
      caution: { en: "The data can be complex or binary. Browser Reviewer shows useful previews when full decoding is not possible.", es: "Los datos pueden ser complejos o binarios. Browser Reviewer muestra vistas utiles cuando no es posible decodificar todo." },
      fields: ["Origin", "Host", "Storage_key", "Value_preview", "Value_size", "Created", "Modified", "LastAccessed", "Source_kind", "Source_path"]
    }
  };

  const fieldText = {
    id: ["The row number assigned by Browser Reviewer.", "Numero de fila asignado por Browser Reviewer.", "Use it to refer to one exact record in notes or reports.", "Sirve para citar un registro exacto en notas o informes."],
    Artifact_type: ["The browser and artifact kind shown together.", "El navegador y el tipo de artefacto mostrados juntos.", "It quickly tells you where the row came from, such as cache, history or cookies.", "Dice rapidamente de donde salio la fila, como cache, historial o cookies."],
    Potential_activity: ["A simple activity description created by Browser Reviewer.", "Descripcion simple de actividad creada por Browser Reviewer.", "It helps sort many rows into understandable actions.", "Ayuda a ordenar muchas filas en acciones entendibles."],
    Browser: ["The browser name detected for the record.", "Nombre del navegador detectado para el registro.", "Use it to separate Chrome, Edge, Brave, Firefox and other browser activity.", "Sirve para separar actividad de Chrome, Edge, Brave, Firefox y otros navegadores."],
    File: ["The original evidence file that was read.", "Archivo original de evidencia que fue leido.", "This is one of the most important traceability fields.", "Es uno de los campos mas importantes para trazabilidad."],
    Label: ["A label added by the examiner.", "Etiqueta agregada por el perito.", "Use it to mark useful, suspicious, reviewed or irrelevant records.", "Sirve para marcar registros utiles, sospechosos, revisados o irrelevantes."],
    Comment: ["A note added by the examiner.", "Nota agregada por el perito.", "Use it to explain your interpretation in plain language.", "Sirve para explicar la interpretacion en lenguaje claro."]
  };

  const conceptText = {
    id: ["A local record number assigned by Browser Reviewer so one row can be referenced clearly.", "Numero local asignado por Browser Reviewer para referenciar un registro con claridad."],
    Artifact_type: ["The artifact label combines the browser shown in the tool with the kind of web activity record.", "La etiqueta del artefacto combina el navegador mostrado en la herramienta con el tipo de registro de actividad web."],
    Potential_activity: ["A readable activity label inferred from the artifact and, when possible, its URL or metadata.", "Etiqueta legible de actividad inferida desde el artefacto y, cuando es posible, su URL o metadatos."],
    Browser: ["The browser name reported by the parser for the profile or file being processed.", "Nombre del navegador reportado por el parser para el perfil o archivo procesado."],
    File: ["The evidence path of the file that produced the record.", "Ruta de evidencia del archivo que produjo el registro."],
    Url: ["Uniform Resource Locator, for example https://www.google.com. It is the web address of a page, resource, download, cache entry or service endpoint.", "Localizador Uniforme de Recursos, por ejemplo https://www.google.com. Es la direccion web de una pagina, recurso, descarga, entrada de cache o endpoint de servicio."],
    URL: ["Uniform Resource Locator, for example https://www.google.com. It is the saved web address for a bookmark or related record.", "Localizador Uniforme de Recursos, por ejemplo https://www.google.com. Es la direccion web guardada para un marcador o registro relacionado."],
    From_url: ["The previous or referring URL that led toward the current URL when the browser preserved that relationship.", "URL previa o referente que llevo hacia la URL actual cuando el navegador conservo esa relacion."],
    OriginalUrl: ["The original URL stored by the browser before redirects, normalization or display changes.", "URL original guardada por el navegador antes de redirecciones, normalizacion o cambios de visualizacion."],
    Referrer: ["The page or URL that referred the browser to another page, download or cached resource.", "Pagina o URL que refirio al navegador hacia otra pagina, descarga o recurso cacheado."],
    Tab_url: ["The URL of the browser tab associated with a download or session entry.", "URL de la pestana del navegador asociada a una descarga o entrada de sesion."],
    Host: ["The domain or host name, such as google.com or accounts.example.org.", "Dominio o nombre de host, como google.com o accounts.example.org."],
    Origin: ["A web origin is the scheme, host and port that owns browser storage, such as https://example.com.", "Un origen web es el esquema, host y puerto dueno del almacenamiento, como https://example.com."],
    Path: ["A URL or cookie path scope that limits where the record applies within a site.", "Alcance de ruta URL o cookie que limita donde aplica el registro dentro de un sitio."],
    Name: ["The name assigned to the object, such as a cookie name or extension name.", "Nombre asignado al objeto, como nombre de cookie o nombre de extension."],
    Value: ["The stored value associated with a field, cookie or key. It may be plain text, encoded text or sensitive data.", "Valor almacenado asociado a un campo, cookie o clave. Puede ser texto plano, texto codificado o dato sensible."],
    Title: ["A human-readable title saved by the browser for a page, tab, bookmark or session entry.", "Titulo legible guardado por el navegador para una pagina, pestana, marcador o sesion."],
    Category: ["A Browser Reviewer classification of the site or activity type.", "Clasificacion de Browser Reviewer sobre el tipo de sitio o actividad."],
    Visit_time: ["The timestamp representing when the browser recorded a visit.", "Timestamp que representa cuando el navegador registro una visita."],
    Last_visit_time: ["The latest known visit time for the same page or URL in the source artifact.", "Ultima visita conocida para la misma pagina o URL en el artefacto fuente."],
    Start_time: ["The time a download or activity started according to the browser record.", "Hora en que inicio una descarga o actividad segun el registro del navegador."],
    End_time: ["The time a download or activity ended according to the browser record.", "Hora en que termino una descarga o actividad segun el registro del navegador."],
    Created: ["Creation time when the browser or file format stores a defensible creation timestamp.", "Hora de creacion cuando el navegador o formato guarda un timestamp defendible."],
    Modified: ["Last modification time for a record, value or source file when it is reliable.", "Ultima modificacion de un registro, valor o archivo fuente cuando es confiable."],
    LastModified: ["Last modification time reported by the browser, server metadata or source file.", "Ultima modificacion reportada por el navegador, metadatos del servidor o archivo fuente."],
    LastAccessed: ["Last access time when the source format provides a useful and reliable access timestamp.", "Ultimo acceso cuando el formato fuente ofrece un timestamp util y confiable."],
    Expires: ["Expiration time after which a cookie or cached response should no longer be considered fresh.", "Hora de expiracion despues de la cual una cookie o respuesta cacheada ya no deberia considerarse vigente."],
    Timestamp: ["A time value recovered from the artifact when no more specific timestamp name applies.", "Valor temporal recuperado del artefacto cuando no aplica un nombre de timestamp mas especifico."],
    Visit_count: ["The number of times the browser recorded visits to the same URL.", "Numero de veces que el navegador registro visitas a la misma URL."],
    Typed_count: ["A Chromium counter for how often a URL was typed or selected from the address bar.", "Contador Chromium de cuantas veces una URL fue escrita o seleccionada desde la barra de direcciones."],
    Transition: ["The browser's navigation reason, such as link click, typed address, reload or form submission.", "Razon de navegacion del navegador, como clic en enlace, direccion escrita, recarga o envio de formulario."],
    Navigation_context: ["A plain-language explanation derived from low-level browser transition values.", "Explicacion en lenguaje claro derivada de valores tecnicos de transicion del navegador."],
    User_action_likelihood: ["An examiner-friendly estimate of whether the record likely involved direct user action.", "Estimacion amigable para el perito sobre si el registro probablemente involucra accion directa del usuario."],
    Target_path: ["The intended final filesystem path for a downloaded file.", "Ruta final prevista en el sistema de archivos para un archivo descargado."],
    Current_path: ["The temporary or current path used by the browser while handling a download.", "Ruta temporal o actual usada por el navegador mientras maneja una descarga."],
    Received_bytes: ["The amount of data actually received by the browser for a download.", "Cantidad de datos realmente recibidos por el navegador para una descarga."],
    Total_bytes: ["The total expected size of a download when the browser knew it.", "Tamano total esperado de una descarga cuando el navegador lo conocia."],
    Mime_type: ["A media type such as text/html, image/png or application/pdf that describes content format.", "Tipo de medio como text/html, image/png o application/pdf que describe el formato del contenido."],
    ContentType: ["The HTTP or detected content type for a cached web resource.", "Tipo de contenido HTTP o detectado para un recurso web cacheado."],
    HttpStatus: ["The HTTP response code, such as 200, 301, 404 or 500.", "Codigo de respuesta HTTP, como 200, 301, 404 o 500."],
    Server: ["The server software or infrastructure value recovered from HTTP headers.", "Valor de software o infraestructura de servidor recuperado desde headers HTTP."],
    ETag: ["An HTTP identifier used by servers and browsers to validate a specific version of a resource.", "Identificador HTTP usado por servidores y navegadores para validar una version especifica de un recurso."],
    BodyBlob: ["The recovered binary body of a cached resource stored inside the Browser Reviewer database.", "Cuerpo binario recuperado de un recurso cacheado y guardado dentro de la base de Browser Reviewer."],
    BodyPreview: ["A short readable preview extracted from cached content when safe to display.", "Vista previa corta y legible extraida del contenido cacheado cuando es seguro mostrarla."],
    BodySha256: ["A SHA-256 cryptographic hash calculated from the recovered cached body.", "Hash criptografico SHA-256 calculado desde el cuerpo cacheado recuperado."],
    BodyExtension: ["A best-effort file extension assigned from content type, headers or detected bytes.", "Extension de archivo estimada desde tipo de contenido, headers o bytes detectados."],
    ExtensionId: ["The browser's unique identifier for an installed extension or add-on.", "Identificador unico del navegador para una extension o complemento instalado."],
    Permissions: ["Capabilities requested by an extension, such as access to tabs, storage or web requests.", "Capacidades solicitadas por una extension, como acceso a pestanas, almacenamiento o solicitudes web."],
    HostPermissions: ["The websites or URL patterns an extension declares it can access.", "Sitios web o patrones de URL que una extension declara poder acceder."],
    Username: ["The saved account name or user identifier associated with a login record.", "Nombre de cuenta o identificador de usuario guardado asociado a un registro de login."],
    Password_value: ["The password field content or encrypted representation stored by the browser, depending on parser support.", "Contenido del campo password o representacion cifrada guardada por el navegador, segun soporte del parser."],
    Password_status: ["The parser's statement about whether password data is encrypted, unavailable, recovered or only metadata.", "Declaracion del parser sobre si el dato de password esta cifrado, no disponible, recuperado o es solo metadato."],
    Signon_realm: ["The site or authentication realm to which a saved login belongs.", "Sitio o realm de autenticacion al que pertenece un login guardado."],
    Storage_key: ["The key name used by a web application to store a value in browser storage.", "Nombre de clave usado por una aplicacion web para guardar un valor en almacenamiento del navegador."],
    Value_preview: ["A shortened preview of stored web application data.", "Vista previa reducida de datos almacenados por una aplicacion web."],
    Value_size: ["The size of a recovered value in bytes or characters, depending on parser context.", "Tamano de un valor recuperado en bytes o caracteres, segun el contexto del parser."],
    Source_kind: ["The parser method or source format used to recover the record.", "Metodo del parser o formato fuente usado para recuperar el registro."],
    Source_path: ["The specific file or folder path used as the source for that recovered storage record.", "Ruta especifica de archivo o carpeta usada como fuente para ese registro de almacenamiento."],
    Label: ["A reviewer-created classification added during analysis.", "Clasificacion creada por el revisor durante el analisis."],
    Comment: ["A reviewer-created note added during analysis.", "Nota creada por el revisor durante el analisis."]
  };

  const exampleText = {
    id: ["39", "39"],
    Artifact_type: ["Chrome cache, Firefox history, Edge downloads.", "Chrome cache, Firefox history, Edge downloads."],
    Potential_activity: ["Browsing Website, Downloading file, Web cookie stored, Caching video.", "Browsing Website, Downloading file, Web cookie stored, Caching video."],
    Browser: ["Chrome, Brave, Edge, Firefox.", "Chrome, Brave, Edge, Firefox."],
    File: ["C:\\Users\\Ana\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\History", "C:\\Users\\Ana\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\History"],
    Url: ["https://www.google.com/search?q=weather", "https://www.google.com/search?q=clima"],
    URL: ["https://www.youtube.com/watch?v=example", "https://www.youtube.com/watch?v=example"],
    From_url: ["https://news.example/article", "https://news.example/article"],
    OriginalUrl: ["http://example.com before redirecting to https://example.com.", "http://example.com antes de redirigir a https://example.com."],
    Referrer: ["A download may show that it came from https://example.com/downloads.", "Una descarga puede mostrar que vino de https://example.com/downloads."],
    Tab_url: ["The tab was open on https://mail.example.com when the download started.", "La pestana estaba en https://mail.example.com cuando inicio la descarga."],
    Host: ["google.com, facebook.com, accounts.microsoft.com.", "google.com, facebook.com, accounts.microsoft.com."],
    Origin: ["https://web.whatsapp.com", "https://web.whatsapp.com"],
    Path: ["/, /login, /account.", "/, /login, /account."],
    Name: ["SID, sessionid, PREF, extension name.", "SID, sessionid, PREF, nombre de extension."],
    Value: ["A username, token, cookie value or saved form text.", "Un usuario, token, valor de cookie o texto de formulario guardado."],
    Title: ["Music and Podcasts, Free and On-Demand | Pandora.", "Music and Podcasts, Free and On-Demand | Pandora."],
    Category: ["Search Engine, Social Media, Webmail, Entertainment.", "Search Engine, Social Media, Webmail, Entertainment."],
    Visit_time: ["2019-08-29 18:20:44.774", "2019-08-29 18:20:44.774"],
    Last_visit_time: ["2019-08-29 18:20:51.930", "2019-08-29 18:20:51.930"],
    Start_time: ["The download began at 2019-08-29 18:25:10.", "La descarga empezo en 2019-08-29 18:25:10."],
    End_time: ["The download ended at 2019-08-29 18:25:30.", "La descarga termino en 2019-08-29 18:25:30."],
    Created: ["A cookie was created on 2019-08-29 18:21:00.", "Una cookie fue creada en 2019-08-29 18:21:00."],
    Modified: ["A storage file was last modified on 2019-08-29 19:02:11.", "Un archivo de almacenamiento fue modificado por ultima vez en 2019-08-29 19:02:11."],
    LastModified: ["A cached image says the server last modified it on 2019-08-20.", "Una imagen en cache dice que el servidor la modifico por ultima vez el 2019-08-20."],
    LastAccessed: ["A cookie was last accessed when a site reused it.", "Una cookie tuvo ultimo acceso cuando un sitio la reutilizo."],
    Expires: ["A cookie may expire in 2020 or be session-only.", "Una cookie puede expirar en 2020 o ser solo de sesion."],
    Timestamp: ["A recovered session entry may have one general timestamp.", "Una entrada de sesion recuperada puede tener un timestamp general."],
    Visit_count: ["5 means the browser recorded five visits to that URL.", "5 significa que el navegador registro cinco visitas a esa URL."],
    Typed_count: ["2 means the address was typed or chosen from the address bar twice.", "2 significa que la direccion fue escrita o elegida desde la barra dos veces."],
    Transition: ["typed, link, reload, form submit.", "typed, link, reload, form submit."],
    Navigation_context: ["User typed address, user clicked link, browser reloaded page.", "Usuario escribio direccion, usuario hizo clic, navegador recargo pagina."],
    User_action_likelihood: ["High, Medium or Low depending on browser transition data.", "High, Medium o Low segun datos de transicion del navegador."],
    Target_path: ["C:\\Users\\Ana\\Downloads\\report.pdf", "C:\\Users\\Ana\\Downloads\\report.pdf"],
    Current_path: ["A .crdownload temporary file while Chrome was downloading.", "Un archivo temporal .crdownload mientras Chrome descargaba."],
    Received_bytes: ["1048576 bytes received.", "1048576 bytes recibidos."],
    Total_bytes: ["If total is 1048576 and received is 1048576, it likely completed.", "Si total es 1048576 y recibido es 1048576, probablemente termino."],
    Mime_type: ["application/pdf, image/jpeg, text/html.", "application/pdf, image/jpeg, text/html."],
    ContentType: ["video/mp4, image/webp, text/javascript.", "video/mp4, image/webp, text/javascript."],
    HttpStatus: ["200 means OK, 404 means not found, 301 means redirect.", "200 significa OK, 404 no encontrado, 301 redireccion."],
    Server: ["nginx, Apache, cloudflare.", "nginx, Apache, cloudflare."],
    ETag: ["A value like \"abc123\" that identifies a web resource version.", "Un valor como \"abc123\" que identifica una version del recurso web."],
    BodyBlob: ["The actual cached image, video, PDF or other file content when recovered.", "La imagen, video, PDF u otro contenido real de cache cuando se recupera."],
    BodyPreview: ["First readable text from HTML, JavaScript or JSON.", "Primer texto legible de HTML, JavaScript o JSON."],
    BodySha256: ["A long hash used to identify the exact same recovered file later.", "Un hash largo usado para identificar exactamente el mismo archivo despues."],
    BodyExtension: [".jpg, .png, .mp4, .pdf, .bin.", ".jpg, .png, .mp4, .pdf, .bin."],
    ExtensionId: ["nmmhkkegccagdldgiimedpiccmgmieda.", "nmmhkkegccagdldgiimedpiccmgmieda."],
    Permissions: ["tabs, storage, downloads, cookies.", "tabs, storage, downloads, cookies."],
    HostPermissions: ["*://*.example.com/* or <all_urls>.", "*://*.example.com/* o <all_urls>."],
    Username: ["ana@example.com or ana_user.", "ana@example.com o ana_user."],
    Password_value: ["Encrypted password bytes, blank value, or recovered value depending on support.", "Bytes cifrados, valor vacio o valor recuperado segun soporte."],
    Password_status: ["Encrypted, unavailable, metadata only, recovered.", "Encrypted, unavailable, metadata only, recovered."],
    Signon_realm: ["https://accounts.google.com/", "https://accounts.google.com/"],
    Storage_key: ["theme, token, userId, lastOpenedChat.", "theme, token, userId, lastOpenedChat."],
    Value_preview: ["{\"user\":\"ana\"... or token preview.", "{\"user\":\"ana\"... o vista previa de token."],
    Value_size: ["2048 means the recovered value is about 2 KB.", "2048 significa que el valor recuperado mide cerca de 2 KB."],
    Source_kind: ["Chromium LevelDB, Firefox SQLite, sessionstore JSONLZ4.", "Chromium LevelDB, Firefox SQLite, sessionstore JSONLZ4."],
    Source_path: ["The exact LevelDB, SQLite or profile folder used.", "El LevelDB, SQLite o carpeta de perfil exacta usada."],
    Label: ["Important, Reviewed, Needs correlation.", "Important, Reviewed, Needs correlation."],
    Comment: ["User likely searched for travel before the download.", "El usuario probablemente busco viajes antes de la descarga."]
  };

  function describeField(name) {
    const text = fieldText[name] || [
      "Parser field extracted when available.",
      "Campo extraido por el parser cuando esta disponible.",
      "Interpret together with artifact source, browser and time fields.",
      "Interpretar junto con fuente, navegador y campos temporales."
    ];
    const concept = conceptText[name] || [
      "This is a parser output field. Its exact value depends on the browser family and artifact source.",
      "Este es un campo de salida del parser. Su valor exacto depende de la familia del navegador y de la fuente del artefacto."
    ];
    const example = exampleText[name] || [
      "Example depends on the artifact being reviewed.",
      "El ejemplo depende del artefacto que se esta revisando."
    ];
    return { name, type: inferType(name), concept: { en: concept[0], es: concept[1] }, example: { en: example[0], es: example[1] }, meaning: { en: text[0], es: text[1] }, forensic: { en: text[2], es: text[3] } };
  }

  function inferType(name) {
    if (/time|date|created|expires|modified|accessed|timestamp/i.test(name)) return "datetime";
    if (/id|count|bytes|size|port|index|status|version|type|state|reason/i.test(name)) return "number/text";
    if (/blob|value/i.test(name)) return "text/blob";
    return "text";
  }

  function tr(value) {
    return typeof value === "string" ? { en: value, es: value } : value;
  }

  const docs = {};
  const index = [];
  Object.keys(families).forEach((familyName) => {
    Object.keys(artifacts).forEach((artifactKey) => {
      const b = families[familyName];
      const a = artifacts[artifactKey];
      const sourceKey = b.sourceKey;
      const key = `${familyName.toLowerCase().replace(/\s+/g, "-")}-${artifactKey}`;
      const doc = {
        key,
        browser: familyName,
        family: sourceKey,
        artifactKey,
        icon: b.icon,
        confidence: a.confidence,
        title: { en: `${familyName} - ${a.name.en}`, es: `${familyName} - ${a.name.es}` },
        summary: { en: `${a.summary.en} ${b.hint.en}`, es: `${a.summary.es} ${b.hint.es}` },
        source: {
          en: `${a.source[sourceKey]} Includes: ${b.includes.en}`,
          es: `${a.source[sourceKey]} Incluye: ${b.includes.es}`
        },
        potential: a.activity,
        example: a.example,
        caution: a.caution,
        fields: ["id", "Artifact_type", "Potential_activity", "Browser"].concat(a.fields).concat(["File", "Label", "Comment"]).map(describeField)
      };
      docs[key] = doc;
      index.push({
        key,
        browser: familyName,
        family: sourceKey,
        artifact: artifactKey,
        file: `browser-artifact.html?doc=${encodeURIComponent(key)}`,
        title: doc.title,
        summary: doc.summary,
        fields: doc.fields.length,
        confidence: doc.confidence,
        icon: doc.icon
      });
    });
  });

  window.browserArtifactDocs = docs;
  window.browserArtifactIndex = index;
  window.browserArtifactBrowsers = Object.keys(families);
  window.browserArtifactArtifacts = Object.keys(artifacts).map((key) => ({ key, title: artifacts[key].name }));
})();
