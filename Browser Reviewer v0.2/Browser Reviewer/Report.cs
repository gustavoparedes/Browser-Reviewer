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
                        string labelName = reader["Label_name"].ToString();
                        checkedListBox1.Items.Add(labelName, false); // false = no marcado por defecto
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
        "results"
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
            // 1) Mapa para nombres amigables (ajústalo a gusto)
            var tableDisplayNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["firefox_results"] = "History (Firefox)",
                ["results"] = "History (Chromium)",
                ["chrome_downloads"] = "Downloads (Chrome)",
                ["firefox_downloads"] = "Downloads (Firefox)",
                ["bookmarks_Chrome"] = "Bookmarks (Chrome)",
                ["bookmarks_Firefox"] = "Bookmarks (Firefox)",
                ["autofill_data"] = "Autofill Data"
            };

            // 2) Prioridad por categoría
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
                return 99;   // Otros
            }

            // 3) Nombre amigable con fallback
            string FriendlyTableName(string rawName)
            {
                if (tableDisplayNames.TryGetValue(rawName, out var friendly))
                    return friendly;

                // fallback: "firefox_results" -> "Firefox Results"
                var parts = (rawName ?? string.Empty).Replace('_', ' ').Trim().Split(' ');
                for (int i = 0; i < parts.Length; i++)
                {
                    if (parts[i].Length > 0)
                        parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1).ToLower();
                }
                return string.Join(" ", parts);
            }

            var sb = new StringBuilder();

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"es\">");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset=\"UTF-8\" />");
            sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />");
            sb.AppendLine("<title>Browser Reviewer - Reporte por Labels</title>");
            sb.AppendLine("<style>");
            sb.AppendLine(@"
:root{ --head-h:38px; }

/* Estilos básicos y responsivos */
body{font-family:system-ui,-apple-system,'Segoe UI',Roboto,Ubuntu,'Helvetica Neue',Arial,sans-serif;margin:24px;color:#222;}
h1{font-size:1.6rem;margin:0 0 8px;}
h2{font-size:1.2rem;margin:24px 0 8px;}
.summary{margin-bottom:16px;font-size:.95rem;color:#444;}
.badge{display:inline-block;background:#eef;border:1px solid #cdd; padding:2px 8px;border-radius:12px;margin-right:6px; font-size:.9rem;}
.section-header{display:flex; align-items:center; gap:10px;}
.count{font-weight:600; color:#555;}
.empty{padding:8px 0; color:#666; font-style:italic;}
footer{margin-top:32px; font-size:.85rem; color:#666;}
a{color:#0067c0; text-decoration:none;} a:hover{text-decoration:underline;}
.box{border:1px solid #d0d0d0; background:#fff; border-radius:4px; padding:6px; overflow-wrap:anywhere;}

/* Contenedor (sin scroll interno para evitar doble barra) */
.table-wrap{
  border:1px solid #ddd;
  border-radius:8px;
}

/* Tabla */
table{border-collapse:collapse; width:100%; min-width:720px; table-layout:fixed;}
th,td{padding:8px 10px; border-bottom:1px solid #eee; text-align:left; vertical-align:top;}

/* Sticky base para todas las celdas del thead */
thead th{
  position:sticky;
  background:#fafafa;
  z-index:3;                 /* por encima del cuerpo */
  border-bottom:1px solid #ddd;
}

/* Fila de encabezados (títulos) */
thead tr.headers th{
  top:0;                     /* pegado al tope del viewport (scroll de página) */
  font-weight:600;
  text-align:left;
  cursor:pointer; user-select:none;
  position:sticky;
}

/* Fila de filtros (debajo de los títulos) -> dropdowns */
thead tr.filters th{
  top:var(--head-h);         /* altura real del header calculada por JS */
  z-index:2;
  background:#f5f7fb;
  border-bottom:1px solid #ddd;
}
thead tr.filters select{
  width:100%; box-sizing:border-box; padding:4px; font-size:12px; border:1px solid #d8d8e0; border-radius:4px; background:#fff;
}

/* Indicadores de orden */
thead tr.headers th.sort-asc::after{content:'▲'; position:absolute; right:8px; color:#666; font-size:10px;}
thead tr.headers th.sort-desc::after{content:'▼'; position:absolute; right:8px; color:#666; font-size:10px;}
");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");

            sb.AppendLine("<header>");
            sb.AppendLine("<h1>Browser Reviewer Report</h1>");
            sb.Append("<div class=\"summary\">Selected labels: ");

            //var offset = DateTimeOffset.Now.Offset;
            //string tzStr = $"UTC{(offset >= TimeSpan.Zero ? "+" : "")}{offset:hh\\:mm}"; // p.ej. UTC-05:00
                                                                                         // Si prefieres tu variable existente (horas enteras):
                                                                                         // string tzStr = $"UTC{(Helpers.utcOffset >= 0 ? "+" : "")}{Helpers.utcOffset:00}:00";

            //sb.AppendLine($"<div><b>Time zone:</b> {WebUtility.HtmlEncode(tzStr)}</div>");
            foreach (var lbl in selectedLabels)
                sb.Append($"<span class=\"badge\">{WebUtility.HtmlEncode(lbl)}</span>");
            sb.AppendLine("</div>");
            sb.AppendLine("</header>");

            // ORDENAR secciones por prioridad y mostrar nombre amigable
            foreach (var kv in perTable
                                .OrderBy(kv => GetPriority(kv.Key))
                                .ThenBy(kv => FriendlyTableName(kv.Key), StringComparer.CurrentCultureIgnoreCase))
            {
                string displayName = FriendlyTableName(kv.Key);
                DataTable dt = kv.Value;

                sb.AppendLine("<section>");
                sb.AppendLine($"<div class=\"section-header\"><h2>{WebUtility.HtmlEncode(displayName)}</h2><span class=\"count\">({dt.Rows.Count} records)</span></div>");

                // Envolvemos la tabla (sin scroll interno)
                string tableHtml = DataTableToHtmlWithFilters(dt); // Debe renderizar <table> con <thead> (tr.headers + tr.filters)
                sb.AppendLine("<div class=\"table-wrap\">");
                sb.AppendLine(tableHtml);
                sb.AppendLine("</div>");

                sb.AppendLine("</section>");
            }

            sb.AppendLine("<footer>Generated by Browser Reviewer • " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "</footer>");

            // ===== Script: medir altura headers + dropdowns con valores únicos + ordenamiento =====
            sb.AppendLine(@"
<script>
(function(){
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

  // Todas las tablas del reporte
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

    // 2) Filtros con dropdowns: construir valores únicos por columna
    if (filterRow){
      function applyFilters(){
        var selects = Array.from(filterRow.querySelectorAll('select'));
        allRows.forEach(function(tr){
          var tds = tr.children, visible = true;
          for (var i=0; i<selects.length; i++){
            var sel = selects[i];
            var val = sel ? sel.value : '';
            if (!val) continue; // (Todos)
            var cellText = (tds[i]?.innerText || '').trim();
            if (val === '__EMPTY__'){
              if (cellText !== '') { visible = false; break; }
            } else {
              if (cellText !== val){ visible = false; break; }
            }
          }
          tr.style.display = visible ? '' : 'none';
        });
      }

      for (var colIndex=0; colIndex<filterRow.cells.length; colIndex++){
        var cell = filterRow.cells[colIndex];
        if (!cell) continue;

        // Reemplazar cualquier contenido (inputs anteriores) por un <select>
        cell.innerHTML = '';
        var select = document.createElement('select');
        cell.appendChild(select);

        // Colectar valores únicos del tbody
        var setVals = new Set();
        var hasEmpty = false;
        allRows.forEach(function(tr){
          var text = (tr.children[colIndex]?.innerText || '').trim();
          if (text === '') hasEmpty = true; else setVals.add(text);
        });

        var vals = Array.from(setVals).sort(function(a,b){
          return a.localeCompare(b, undefined, { sensitivity:'base', numeric:true });
        });

        // Opciones
        var optAll = document.createElement('option');
        optAll.value = '';
        optAll.textContent = '(All)';
        select.appendChild(optAll);

        if (hasEmpty){
          var optEmpty = document.createElement('option');
          optEmpty.value = '__EMPTY__';
          optEmpty.textContent = '(Vacíos)';
          select.appendChild(optEmpty);
        }

        vals.forEach(function(v){
          var opt = document.createElement('option');
          opt.value = v;
          opt.textContent = v;
          select.appendChild(opt);
        });

        select.addEventListener('change', applyFilters);
      }
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
                sb.AppendLine("<th><input placeholder='Filtrar…'></th>");
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
                            "<td><div class='box'>" +
                            $"<a href=\"{Escape(raw)}\" target=\"_blank\" title=\"{Escape(raw)}\">{Escape(shortText)}</a>" +
                            "</div></td>"
                        );
                    }
                    else
                    {
                        sb.AppendLine($"<td><div class='box'>{encoded}</div></td>");
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
