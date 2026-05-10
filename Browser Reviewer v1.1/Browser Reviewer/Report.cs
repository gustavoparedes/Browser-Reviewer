using System.Data;
using System.Data.SQLite;
using System.Net;
using System.Text;


namespace Browser_Reviewer
{
    public partial class Form_Report : Form
    {
        public Form_Report()
        {
            InitializeComponent();
            AppIcon.Apply(this);
        }

        private void Report_Load(object sender, EventArgs e)
        {
            LoadLabelsFromDataBase();
            this.FormBorderStyle = FormBorderStyle.FixedSingle; // o FixedDialog
            this.MaximizeBox = true;   // deja activo el botón Maximizar
            this.MinimizeBox = true;   // deja activo el botón Minimizar
            this.SizeGripStyle = SizeGripStyle.Hide; // oculta el grip de la esquina
        }






        private void LoadLabelsFromDataBase()
        {
            using (SQLiteConnection connection = new SQLiteConnection(Helpers.chromeViewerConnectionString))
            {
                connection.Open();

                // Traer solo los labels
                using (SQLiteCommand cmd = new SQLiteCommand("SELECT Label_name FROM Labels", connection))
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    checkedListBox1.Items.Clear(); // limpiar antes de cargar

                    while (reader.Read())
                    {
                        string? labelName = reader["Label_name"].ToString();
                        if (!string.IsNullOrWhiteSpace(labelName))
                        {
                            checkedListBox1.Items.Add(labelName, false); // false = no marcado por defecto
                        }
                    }
                }
            }
        }

        private void button_Generate_Click(object sender, EventArgs e)
        {

            GenerateReportFromSelectedLabels();


        }




        // Llama a este método (por ejemplo desde button_Report_Click)
        private void GenerateReportFromSelectedLabels()
        {



            // 1) Tomar labels seleccionados
            var selectedLabels = checkedListBox1.CheckedItems.Cast<string>().ToList();
            if (selectedLabels.Count == 0)
            {
                MessageBox.Show("Select at least one label to generate the report.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 2) Tablas a consultar (todas comparten el campo Label según tu diseño)
            string[] tables = new[]
            {
        "autofill_data",
        "bookmarks_Chrome",
        "bookmarks_Firefox",
        "chrome_downloads",
        "firefox_downloads",
        "firefox_results",
        "results",
        "cookies_data",
        "cache_data",
        "session_data",
        "extension_data",
        "saved_logins_data",
        "local_storage_data",
        "session_storage_data",
        "indexeddb_data"
    };

            // 3) Ejecutar consultas y guardar DataTables por tabla
            var perTableResults = new Dictionary<string, DataTable>();
            using (var cn = new SQLiteConnection(Helpers.chromeViewerConnectionString))
            {
                cn.Open();
                foreach (var table in tables)
                {
                    var dt = QueryTableByLabels(cn, table, selectedLabels);
                    perTableResults[table] = dt;
                }
            }

            // 4) Generar HTML con todas las secciones
            string html = BuildHtmlReport(perTableResults, selectedLabels);

            // 5) Preguntar dónde guardar el archivo
            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "HTML files (*.html)|*.html|All files (*.*)|*.*";
                sfd.FileName = $"BrowserReviewer_Report_{DateTime.Now:yyyyMMdd_HHmmss}.html";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(sfd.FileName, html, Encoding.UTF8);

                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = sfd.FileName,
                            UseShellExecute = true
                        });
                    }
                    catch
                    {
                        MessageBox.Show("The report was saved but could not be opened automatically.",
                                        "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }

                    MessageBox.Show($"Report generated:\n{sfd.FileName}",
                                    "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }





        }

        // --- Helpers ---

        // Consulta una tabla por labels: SELECT * FROM {table} WHERE Label IN (@p0,@p1,...)
        private DataTable QueryTableByLabels(SQLiteConnection cn, string table, List<string> labels)
        {
            var dt = new DataTable();
            // Construir "IN (@p0,@p1,...)" de forma segura
            var inClause = BuildInClause("Label", labels, out var parameters);

            string sql = $"SELECT * FROM {table} WHERE {inClause}";
            using (var cmd = new SQLiteCommand(sql, cn))
            {
                // Agregar parámetros
                foreach (var p in parameters)
                    cmd.Parameters.AddWithValue(p.Key, p.Value);

                using (var da = new SQLiteDataAdapter(cmd))
                    da.Fill(dt);
            }
            return dt;
        }

        // Construye "Label IN (@p0,@p1,...)" y devuelve los parámetros
        private string BuildInClause(string column, List<string> values, out Dictionary<string, object> parameters)
        {
            parameters = new Dictionary<string, object>(values.Count);
            var placeholders = new List<string>(values.Count);

            for (int i = 0; i < values.Count; i++)
            {
                string pname = $"@p{i}";
                placeholders.Add(pname);
                parameters[pname] = values[i];
            }

            return $"{column} IN ({string.Join(",", placeholders)})";
        }


      


        private string BuildHtmlReport(Dictionary<string, DataTable> perTable, List<string> selectedLabels)
        {
            int GetPriority(string table)
            {
                if (table.Contains("results", StringComparison.OrdinalIgnoreCase))
                    return 1; // History
                if (table.Contains("downloads", StringComparison.OrdinalIgnoreCase))
                    return 2; // Downloads
                if (table.Contains("bookmarks", StringComparison.OrdinalIgnoreCase))
                    return 3; // Bookmarks
                if (table.Contains("autofill", StringComparison.OrdinalIgnoreCase))
                    return 4; // Autofill
                if (table.Contains("logins", StringComparison.OrdinalIgnoreCase))
                    return 5;
                if (table.Contains("local_storage", StringComparison.OrdinalIgnoreCase))
                    return 6;
                if (table.Contains("session_storage", StringComparison.OrdinalIgnoreCase))
                    return 7;
                if (table.Contains("indexeddb", StringComparison.OrdinalIgnoreCase))
                    return 8;
                return 99;   // Otros
            }

            var sb = new StringBuilder();
            int totalRecords = perTable.Values.Sum(t => t.Rows.Count);
            int populatedSections = perTable.Values.Count(t => t.Rows.Count > 0);
            string generatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string tzStr = $"UTC{(Helpers.utcOffset >= 0 ? "+" : "")}{Helpers.utcOffset}";

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\">");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset=\"UTF-8\" />");
            sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />");
            sb.AppendLine("<title>Browser Reviewer - Label Review Report</title>");
            sb.AppendLine("<style>");
            sb.AppendLine(@"
:root{
  --ink:#1f2732;
  --muted:#66717f;
  --line:#dce3ed;
  --panel:#ffffff;
  --soft:#f4f7fb;
  --brand:#182434;
  --accent:#3769b1;
  --accent2:#2f855a;
  --note:#fff7df;
  --head-h:38px;
}
*{box-sizing:border-box}
body{
  margin:0;
  font-family:'Segoe UI',system-ui,-apple-system,Roboto,Arial,sans-serif;
  color:var(--ink);
  background:#eef2f7;
}
.cover{
  background:linear-gradient(180deg,#182434 0%,#24364d 100%);
  color:#fff;
  padding:34px 36px 28px;
  border-bottom:5px solid var(--accent);
}
.eyebrow{font-size:12px;font-weight:700;letter-spacing:.08em;text-transform:uppercase;color:#bfd1ea;margin-bottom:10px}
h1{font-size:32px;line-height:1.15;margin:0 0 8px}
.subtitle{max-width:980px;color:#dbe5f1;font-size:15px;margin:0}
.meta-grid{display:grid;grid-template-columns:repeat(4,minmax(160px,1fr));gap:12px;margin-top:24px}
.metric{background:rgba(255,255,255,.08);border:1px solid rgba(255,255,255,.16);padding:13px 14px;border-radius:8px}
.metric .k{font-size:11px;text-transform:uppercase;color:#bfd1ea;font-weight:700}
.metric .v{font-size:22px;font-weight:700;margin-top:4px}
.report-shell{padding:24px 32px 34px}
.panel{background:var(--panel);border:1px solid var(--line);border-radius:8px;margin-bottom:18px}
.panel-pad{padding:16px 18px}
.section-title{font-size:18px;margin:0 0 10px}
.chips{display:flex;flex-wrap:wrap;gap:7px}
.badge{display:inline-flex;align-items:center;background:#edf4ff;border:1px solid #cbdaf2;color:#173a66;padding:4px 9px;border-radius:999px;font-size:12px;font-weight:600}
.toolbar{display:flex;gap:12px;align-items:center;justify-content:space-between;position:sticky;top:0;z-index:20;background:#eef2f7;padding:8px 0 12px}
.toolbar input{width:min(520px,100%);padding:9px 11px;border:1px solid #c9d3df;border-radius:6px;background:#fff;font-size:14px}
.nav{display:flex;gap:8px;flex-wrap:wrap}
.nav a{color:#244e83;text-decoration:none;background:#fff;border:1px solid #d6e0ec;border-radius:6px;padding:7px 9px;font-size:12px}
.nav a:hover{background:#edf4ff}
.artifact-section{overflow:hidden}
.section-head{display:flex;justify-content:space-between;gap:16px;align-items:flex-start;padding:16px 18px;background:#fff;border-bottom:1px solid var(--line)}
.section-head h2{font-size:20px;margin:0 0 4px}
.section-head .hint{color:var(--muted);font-size:13px}
.count{font-weight:700;color:#fff;background:var(--brand);border-radius:999px;padding:5px 10px;font-size:12px;white-space:nowrap}
.empty{padding:18px;color:var(--muted);font-style:italic;background:#fbfcfe}
.table-wrap{overflow:auto;background:#fff}
table{border-collapse:separate;border-spacing:0;width:100%;min-width:920px;table-layout:fixed}
th,td{padding:8px 10px;border-bottom:1px solid #edf0f4;text-align:left;vertical-align:top}
thead th{position:sticky;background:#f7f9fc;z-index:3;border-bottom:1px solid var(--line)}
thead tr.headers th{top:0;font-weight:700;color:#283545;cursor:pointer;user-select:none}
thead tr.filters th{top:var(--head-h);z-index:2;background:#f2f6fb}
thead tr.filters input.column-filter{width:100%;box-sizing:border-box;padding:6px 7px;font-size:12px;border:1px solid #ccd6e2;border-radius:5px;background:#fff;color:#222}
tbody tr:nth-child(even){background:#fbfcfe}
tbody tr:hover{background:#f4f8ff}
.box{border:1px solid #d8e0ea;background:#fff;border-radius:5px;padding:6px;overflow-wrap:anywhere}
a{color:#0b63b6;text-decoration:none}a:hover{text-decoration:underline}
.filter-menu{position:absolute;top:calc(100% - 1px);left:0;width:max(100%,280px);max-width:560px;max-height:260px;resize:both;overflow:auto;background:#fff;border:1px solid #cfd8e4;border-radius:6px;box-shadow:0 10px 26px rgba(15,23,42,.18);padding:4px;display:none;z-index:50}
.filter-option{display:block;width:100%;text-align:left;border:0;border-radius:4px;background:#fff;color:#222;padding:6px 8px;font-size:12px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;cursor:pointer}
.filter-option:hover,.filter-option:focus{background:#eef4ff;outline:none}
.filter-empty-note{padding:7px 8px;color:var(--muted);font-size:12px}
thead tr.headers th.sort-asc::after{content:'▲';position:absolute;right:8px;color:#66717f;font-size:10px}
thead tr.headers th.sort-desc::after{content:'▼';position:absolute;right:8px;color:#66717f;font-size:10px}
footer{padding:22px 32px;color:var(--muted);font-size:12px}
@media print{
  body{background:#fff}
  .toolbar{display:none}
  .cover{break-after:page}
  .panel{break-inside:avoid}
  thead th{position:static}
}
@media (max-width:900px){
  .meta-grid{grid-template-columns:repeat(2,minmax(140px,1fr))}
  .report-shell{padding:18px}
  .toolbar{position:static;align-items:stretch;flex-direction:column}
}
");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");

            sb.AppendLine("<header class=\"cover\">");
            sb.AppendLine("<div class=\"eyebrow\">Browser Reviewer</div>");
            sb.AppendLine("<h1>Label Review Report</h1>");
            sb.AppendLine("<p class=\"subtitle\">Focused report of records tagged by the selected labels, grouped by browser artifact type for review and delivery.</p>");
            sb.AppendLine("<div class=\"meta-grid\">");
            sb.AppendLine($"<div class=\"metric\"><div class=\"k\">Records</div><div class=\"v\">{totalRecords}</div></div>");
            sb.AppendLine($"<div class=\"metric\"><div class=\"k\">Sections</div><div class=\"v\">{populatedSections}</div></div>");
            sb.AppendLine($"<div class=\"metric\"><div class=\"k\">Labels</div><div class=\"v\">{selectedLabels.Count}</div></div>");
            sb.AppendLine($"<div class=\"metric\"><div class=\"k\">Time zone</div><div class=\"v\">{WebUtility.HtmlEncode(tzStr)}</div></div>");
            sb.AppendLine("</div>");
            sb.AppendLine("</header>");

            sb.AppendLine("<main class=\"report-shell\">");
            sb.AppendLine("<section class=\"panel panel-pad\">");
            sb.AppendLine("<h2 class=\"section-title\">Report Context</h2>");
            sb.AppendLine($"<p><strong>Case database:</strong> {WebUtility.HtmlEncode(Helpers.db_name)}</p>");
            sb.AppendLine($"<p><strong>Generated at:</strong> {WebUtility.HtmlEncode(generatedAt)}</p>");
            sb.AppendLine("<div class=\"chips\">");
            foreach (var lbl in selectedLabels)
                sb.AppendLine($"<span class=\"badge\">{WebUtility.HtmlEncode(lbl)}</span>");
            sb.AppendLine("</div>");
            sb.AppendLine("</section>");

            var orderedSections = perTable
                .Where(kv => kv.Value.Rows.Count > 0)
                .OrderBy(kv => GetPriority(kv.Key))
                .ThenBy(kv => BuildSectionTitle(kv.Key, kv.Value), StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            sb.AppendLine("<div class=\"toolbar\">");
            sb.AppendLine("<input id=\"globalSearch\" type=\"search\" placeholder=\"Search across this report...\">");
            sb.AppendLine("<nav class=\"nav\">");
            foreach (var kv in orderedSections)
            {
                string displayName = BuildSectionTitle(kv.Key, kv.Value);
                sb.AppendLine($"<a href=\"#{Escape(HtmlId(kv.Key))}\">{WebUtility.HtmlEncode(displayName)} ({kv.Value.Rows.Count})</a>");
            }
            sb.AppendLine("</nav>");
            sb.AppendLine("</div>");

            // ORDENAR secciones por prioridad y mostrar nombre amigable
            foreach (var kv in orderedSections)
            {
                DataTable dt = kv.Value;
                string displayName = BuildSectionTitle(kv.Key, dt);

                sb.AppendLine($"<section class=\"panel artifact-section\" id=\"{Escape(HtmlId(kv.Key))}\">");
                sb.AppendLine("<div class=\"section-head\">");
                sb.AppendLine($"<div><h2>{WebUtility.HtmlEncode(displayName)}</h2><div class=\"hint\">{WebUtility.HtmlEncode(BuildSectionSubtitle(kv.Key, dt))}</div></div>");
                sb.AppendLine($"<span class=\"count\">{dt.Rows.Count} records</span>");
                sb.AppendLine("</div>");

                // Envolvemos la tabla (sin scroll interno)
                string tableHtml = DataTableToHtmlWithFilters(dt); // Debe renderizar <table> con <thead> (tr.headers + tr.filters)
                sb.AppendLine(tableHtml);

                sb.AppendLine("</section>");
            }

            sb.AppendLine("</main>");
            sb.AppendLine("<footer>Generated by Browser Reviewer - " + WebUtility.HtmlEncode(generatedAt) + "</footer>");

            // ===== Script: global search + searchable column filters + sorting =====
            sb.AppendLine(@"
<script>
(function(){
  var globalSearch = document.getElementById('globalSearch');

  function guessType(values){
    for (var i=0;i<values.length;i++){
      var v = (values[i]||'').toString().trim();
      if (!v) continue;
      var n = v.replace(/,/g,'');
      if (!isNaN(n) && n!=='') return 'number';
      var d = Date.parse(v);
      if (!isNaN(d)) return 'date';
      return 'string';
    }
    return 'string';
  }

  function compare(a, b, type, dir){
    var mul = (dir === 'desc') ? -1 : 1;
    if (type === 'number'){
      var na = parseFloat((a||'').replace(/,/g,'')) || 0;
      var nb = parseFloat((b||'').replace(/,/g,'')) || 0;
      return (na - nb) * mul;
    }
    if (type === 'date'){
      var da = Date.parse(a||'');
      var db = Date.parse(b||'');
      if (isNaN(da) && isNaN(db)) return 0;
      if (isNaN(da)) return -1 * mul;
      if (isNaN(db)) return  1 * mul;
      return (da - db) * mul;
    }
    return (a||'').localeCompare(b||'', undefined, { sensitivity: 'base' }) * mul;
  }

  function getCellFilterText(cell){
    if (!cell) return '';
    return (cell.getAttribute('data-filter-value') || cell.innerText || '').trim();
  }

  function closeFilterMenus(except){
    document.querySelectorAll('.filter-menu').forEach(function(menu){
      if (menu !== except) menu.style.display = 'none';
    });
  }

  document.addEventListener('click', function(ev){
    if (!ev.target.closest('tr.filters')) closeFilterMenus();
  });

  function renderFilterMenu(input, menu, values, hasEmpty, applyFilters){
    var term = input.value.trim().toLowerCase();
    var visibleValues = values.filter(function(v){
      return !term || v.toLowerCase().indexOf(term) !== -1;
    }).slice(0, 250);

    menu.innerHTML = '';

    var allButton = document.createElement('button');
    allButton.type = 'button';
    allButton.className = 'filter-option';
    allButton.textContent = '(All)';
    allButton.addEventListener('mousedown', function(ev){
      ev.preventDefault();
      input.value = '';
      applyFilters();
      closeFilterMenus();
    });
    menu.appendChild(allButton);

    if (hasEmpty && (!term || '(empty)'.indexOf(term) !== -1)){
      var emptyButton = document.createElement('button');
      emptyButton.type = 'button';
      emptyButton.className = 'filter-option';
      emptyButton.textContent = '(empty)';
      emptyButton.addEventListener('mousedown', function(ev){
        ev.preventDefault();
        input.value = '(empty)';
        applyFilters();
        closeFilterMenus();
      });
      menu.appendChild(emptyButton);
    }

    visibleValues.forEach(function(v){
      var button = document.createElement('button');
      button.type = 'button';
      button.className = 'filter-option';
      button.textContent = v;
      button.title = v;
      button.addEventListener('mousedown', function(ev){
        ev.preventDefault();
        input.value = v;
        applyFilters();
        closeFilterMenus();
      });
      menu.appendChild(button);
    });

    if (visibleValues.length === 0 && !(hasEmpty && (!term || '(empty)'.indexOf(term) !== -1))){
      var note = document.createElement('div');
      note.className = 'filter-empty-note';
      note.textContent = 'No matches';
      menu.appendChild(note);
    }

    menu.style.display = 'block';
  }

  document.querySelectorAll('section .table-wrap > table').forEach(function(tbl){
    var thead = tbl.querySelector('thead'); if (!thead) return;
    var headerRow = thead.querySelector('tr.headers');
    var filterRow = thead.querySelector('tr.filters');
    var tbody = tbl.querySelector('tbody'); if (!tbody) return;

    // 1) Calcular altura de la fila de títulos y setear --head-h (por tabla)
    if (headerRow){
      function setHeadHeight(){
        var h = headerRow.getBoundingClientRect().height;
        tbl.style.setProperty('--head-h', (h || 38) + 'px');
      }
      setHeadHeight();
      window.addEventListener('resize', setHeadHeight);
    }

    var allRows = Array.from(tbody.rows);
    var originalOrder = Array.from(allRows);

    if (filterRow){
      function applyFilters(){
        var inputs = Array.from(filterRow.querySelectorAll('input.column-filter'));
        var globalTerm = globalSearch ? globalSearch.value.trim().toLowerCase() : '';
        allRows.forEach(function(tr){
          var tds = tr.children, visible = true;
          if (globalTerm && tr.innerText.toLowerCase().indexOf(globalTerm) === -1){
            visible = false;
          }
          for (var i=0; visible && i<inputs.length; i++){
            var input = inputs[i];
            var val = input ? input.value.trim().toLowerCase() : '';
            if (!val) continue;
            var cellText = getCellFilterText(tds[i]);
            var normalizedCell = cellText.toLowerCase();
            if (val === '(empty)' || val === '__empty__'){
              if (cellText !== '') { visible = false; break; }
            } else {
              if (normalizedCell.indexOf(val) === -1){ visible = false; break; }
            }
          }
          tr.style.display = visible ? '' : 'none';
        });
      }

      for (var colIndex=0; colIndex<filterRow.cells.length; colIndex++){
        var cell = filterRow.cells[colIndex];
        if (!cell) continue;

        cell.innerHTML = '';
        var input = document.createElement('input');
        input.type = 'search';
        input.className = 'column-filter';
        input.placeholder = 'Filter...';
        input.setAttribute('aria-label', 'Filter this column');
        cell.appendChild(input);

        var menu = document.createElement('div');
        menu.className = 'filter-menu';
        cell.appendChild(menu);

        var setVals = new Set();
        var hasEmpty = false;
        allRows.forEach(function(tr){
          var text = getCellFilterText(tr.children[colIndex]);
          if (text === '') hasEmpty = true; else setVals.add(text);
        });

        var vals = Array.from(setVals).sort(function(a,b){
          return a.localeCompare(b, undefined, { sensitivity:'base', numeric:true });
        });

        (function(input, menu, vals, hasEmpty){
          input.addEventListener('focus', function(){
            closeFilterMenus(menu);
            renderFilterMenu(input, menu, vals, hasEmpty, applyFilters);
          });
          input.addEventListener('input', function(){
            applyFilters();
            renderFilterMenu(input, menu, vals, hasEmpty, applyFilters);
          });
          input.addEventListener('keydown', function(ev){
            if (ev.key === 'Escape') closeFilterMenus();
          });
        })(input, menu, vals, hasEmpty);
      }

      if (globalSearch) globalSearch.addEventListener('input', applyFilters);
    }

    // 3) Orden por click en encabezados
    if (headerRow){
      var ths = headerRow.querySelectorAll('th');
      ths.forEach(function(th, colIdx){
        th.addEventListener('click', function(){
          // Limpiar estado en otros th
          ths.forEach(function(x){ if (x !== th) x.classList.remove('sort-asc','sort-desc'); });

          var current = th.classList.contains('sort-asc') ? 'asc' :
                        th.classList.contains('sort-desc') ? 'desc' : null;

          var next = current === 'asc' ? 'desc' : (current === 'desc' ? null : 'asc');

          if (!next){
            th.classList.remove('sort-asc','sort-desc');
            // Restaurar orden original
            originalOrder.forEach(function(tr){ tbody.appendChild(tr); });
            return;
          }

          th.classList.toggle('sort-asc',  next === 'asc');
          th.classList.toggle('sort-desc', next === 'desc');

          // Ordenar solo filas visibles
          var rowsVisible = Array.from(tbody.rows).filter(function(r){ return r.style.display !== 'none'; });
          var values = rowsVisible.map(function(r){
            var td = r.children[colIdx];
            return (td ? td.innerText : '') || '';
          });

          var type = guessType(values);
          rowsVisible.sort(function(r1, r2){
            var a = r1.children[colIdx]?.innerText || '';
            var b = r2.children[colIdx]?.innerText || '';
            return compare(a, b, type, next);
          });

          // Agregar visibles ordenadas primero, luego las ocultas (manteniendo ocultas al final)
          var rowsHidden = Array.from(tbody.rows).filter(function(r){ return r.style.display === 'none'; });
          rowsVisible.concat(rowsHidden).forEach(function(r){ tbody.appendChild(r); });
        });
      });
    }
  });
})();
</script>");
            sb.AppendLine("</body></html>");

            return sb.ToString();
        }




        private string DataTableToHtmlWithFilters(DataTable dt)
        {
            var sb = new StringBuilder();

            if (dt == null || dt.Rows.Count == 0)
            {
                sb.AppendLine("<div class='empty'>No data available</div>");
                return sb.ToString();
            }

            string tableId = "tbl_" + Guid.NewGuid().ToString("N");

            sb.AppendLine("<div class='table-wrap'>");
            sb.AppendLine($"<table id='{tableId}'>");
            sb.AppendLine("<thead>");

            // Fila de encabezados (clickable sort)
            sb.AppendLine("<tr class='headers'>");
            foreach (DataColumn col in dt.Columns)
                sb.AppendLine($"<th>{Escape(col.ColumnName)}</th>");
            sb.AppendLine("</tr>");

            // Fila de filtros
            sb.AppendLine("<tr class='filters'>");
            foreach (DataColumn col in dt.Columns)
                sb.AppendLine("<th></th>");
            sb.AppendLine("</tr>");

            sb.AppendLine("</thead>");
            sb.AppendLine("<tbody>");

            // Filas
            foreach (DataRow row in dt.Rows)
            {
                sb.AppendLine("<tr>");
                foreach (DataColumn col in dt.Columns)
                {
                    string raw = row[col]?.ToString() ?? string.Empty;
                    string encoded = Escape(raw);

                    bool isUrlCol = col.ColumnName.Equals("Url", StringComparison.OrdinalIgnoreCase) ||
                                    col.ColumnName.Equals("URL", StringComparison.OrdinalIgnoreCase);
                    bool looksUrl = raw.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                                    raw.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

                    if ((isUrlCol || looksUrl) && Uri.IsWellFormedUriString(raw, UriKind.Absolute))
                    {
                        string shortText = ShortenUrlText(raw, 64);
                        sb.AppendLine(
                            $"<td data-filter-value=\"{Escape(raw)}\"><div class='box'>" +
                            $"<a href=\"{Escape(raw)}\" target=\"_blank\" title=\"{Escape(raw)}\">{Escape(shortText)}</a>" +
                            "</div></td>"
                        );
                    }
                    else
                    {
                        sb.AppendLine($"<td data-filter-value=\"{encoded}\"><div class='box'>{encoded}</div></td>");
                    }
                }
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</tbody></table>");
            sb.AppendLine("</div>");

            return sb.ToString();
        }




        // =====================
        // 3) Helpers
        // =====================
        private static string Escape(string? text)
        {
            return System.Net.WebUtility.HtmlEncode(text ?? string.Empty);
        }

        private static string HtmlId(string? text)
        {
            string value = new string((text ?? string.Empty)
                .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
                .ToArray())
                .Trim('-')
                .ToLowerInvariant();

            return string.IsNullOrWhiteSpace(value) ? "section" : value;
        }

        private static string BuildSectionSubtitle(string tableName, DataTable dt)
        {
            string artifact = FriendlyArtifactName(tableName);
            var browsers = ExtractDistinctColumnValues(dt, "Browser", 4);

            if (browsers.Count == 0)
                return $"Tagged {artifact} records";

            string browserText = string.Join(", ", browsers);
            int totalBrowsers = CountDistinctColumnValues(dt, "Browser");
            if (totalBrowsers > browsers.Count)
                browserText += $" +{totalBrowsers - browsers.Count} more";

            return $"Tagged {artifact} records from {browserText}";
        }

        private static string BuildSectionTitle(string tableName, DataTable dt)
        {
            string artifact = FriendlyArtifactName(tableName);
            var browsers = ExtractDistinctColumnValues(dt, "Browser", 3);

            if (browsers.Count == 0)
                return PluralizeArtifactTitle(artifact);

            string browserText = string.Join(", ", browsers);
            int totalBrowsers = CountDistinctColumnValues(dt, "Browser");
            if (totalBrowsers > browsers.Count)
                browserText += $" +{totalBrowsers - browsers.Count} more";

            return $"{PluralizeArtifactTitle(artifact)} ({browserText})";
        }

        private static string PluralizeArtifactTitle(string artifact)
        {
            return artifact switch
            {
                "History" => "History",
                "Cache" => "Cache",
                "Autofill" => "Autofill",
                "Local Storage" => "Local Storage",
                "Session Storage" => "Session Storage",
                "IndexedDB" => "IndexedDB",
                "Browser artifact" => "Browser Artifacts",
                _ => artifact.EndsWith("s", StringComparison.OrdinalIgnoreCase) ? artifact : artifact + "s"
            };
        }

        private static string FriendlyArtifactName(string tableName)
        {
            string key = tableName ?? string.Empty;
            if (key.Contains("results", StringComparison.OrdinalIgnoreCase)) return "History";
            if (key.Contains("downloads", StringComparison.OrdinalIgnoreCase)) return "Download";
            if (key.Contains("bookmarks", StringComparison.OrdinalIgnoreCase)) return "Bookmark";
            if (key.Contains("autofill", StringComparison.OrdinalIgnoreCase)) return "Autofill";
            if (key.Contains("cookies", StringComparison.OrdinalIgnoreCase)) return "Cookie";
            if (key.Contains("cache", StringComparison.OrdinalIgnoreCase)) return "Cache";
            if (key.Contains("session_storage", StringComparison.OrdinalIgnoreCase)) return "Session Storage";
            if (key.Contains("session", StringComparison.OrdinalIgnoreCase)) return "Session";
            if (key.Contains("extension", StringComparison.OrdinalIgnoreCase)) return "Extension";
            if (key.Contains("logins", StringComparison.OrdinalIgnoreCase)) return "Saved Login";
            if (key.Contains("local_storage", StringComparison.OrdinalIgnoreCase)) return "Local Storage";
            if (key.Contains("indexeddb", StringComparison.OrdinalIgnoreCase)) return "IndexedDB";
            return "Browser artifact";
        }

        private static List<string> ExtractDistinctColumnValues(DataTable dt, string columnName, int limit)
        {
            if (dt == null || !dt.Columns.Contains(columnName))
                return new List<string>();

            return dt.Rows.Cast<DataRow>()
                .Select(row => row[columnName]?.ToString() ?? string.Empty)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.CurrentCultureIgnoreCase)
                .Take(limit)
                .ToList();
        }

        private static int CountDistinctColumnValues(DataTable dt, string columnName)
        {
            if (dt == null || !dt.Columns.Contains(columnName))
                return 0;

            return dt.Rows.Cast<DataRow>()
                .Select(row => row[columnName]?.ToString() ?? string.Empty)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
        }

        /// <summary>
        /// Devuelve un texto corto y legible para una URL (host + path/query), con elipsis si excede maxLen.
        /// Mantiene el href completo en el enlace y en el title.
        /// </summary>
        private static string ShortenUrlText(string url, int maxLen = 64)
        {
            if (string.IsNullOrWhiteSpace(url)) return string.Empty;

            try
            {
                var u = new Uri(url, UriKind.Absolute);
                string text = u.Host + u.PathAndQuery;
                if (text.EndsWith("/")) text = text.TrimEnd('/');

                if (text.Length > maxLen)
                {
                    int head = Math.Max(10, maxLen / 2);
                    int tail = Math.Max(8, maxLen - head - 1);
                    return text.Substring(0, head) + "…" + text.Substring(text.Length - tail);
                }
                return text;
            }
            catch
            {
                // Si no parsea como URL, recorte simple
                if (url.Length > maxLen)
                    return url.Substring(0, maxLen - 1) + "…";
                return url;
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            bool marcar = checkBox1.Checked; // si está marcado seleccionamos todo

            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                checkedListBox1.SetItemChecked(i, marcar);
            }
        }


      




    }
}
