using Browser_Reviewer.Resources;
using System.Data.SQLite;
using System.Data;
using Syncfusion.WinForms.DataGrid.Events;
using Syncfusion.WinForms.GridCommon.ScrollAxis;
using System.Text.RegularExpressions;
using Syncfusion.Data.Extensions;
using Syncfusion.WinForms.DataGrid;
using Syncfusion.WinForms.DataGridConverter;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf;






namespace Browser_Reviewer

{
    public partial class Form1 : Form
    {
        private MyTools Tools;

        private ContextMenuStrip labelContextMenu;


        // Fix for the errors related to 'webBrowser.Dock = DockStyle.Fill;'

        // The issue is that the `webBrowser` initialization and its property assignment are placed outside of a method or constructor.
        // This is not valid in C#. The initialization and property assignment should be moved into the constructor or a method.

        public Form1()
        {
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Put your license here");
            InitializeComponent();
            InitializeNumericUpDown();



            sfDataGrid1.SelectionChanged += SfDataGrid1_SelectionChanged;
            numericUpDown1.ValueChanged += NumericUpDown1_ValueChanged;
            sfDataGrid1.FilterChanged += SfDataGrid_FilterChanged;
            sfDataGrid1.AutoGeneratingColumn += SfDataGrid_AutoGeneratingColumn;
            sfDataGrid1.QueryCellStyle += SfDataGrid1_QueryCellStyle;

            Tools = new MyTools();

            SQLiteFunction.RegisterFunction(typeof(MyRegEx));

            dateTimePicker_start.Format = DateTimePickerFormat.Custom;
            dateTimePicker_start.CustomFormat = "yyyy-MM-dd HH:mm:ss";
            dateTimePicker_start.Value = Helpers.StartDate;

            dateTimePicker_end.Format = DateTimePickerFormat.Custom;
            dateTimePicker_end.CustomFormat = "yyyy-MM-dd HH:mm:ss";
            dateTimePicker_start.Value = Helpers.EndDate;
        }



        private void SfDataGrid1_QueryCellStyle(object sender, QueryCellStyleEventArgs e)
        {
            if (e.Column.MappingName == "Label")
            {
                var node = sfDataGrid1.GetRecordEntryAtRowIndex(e.RowIndex);
                if (node is Syncfusion.Data.RecordEntry record)
                {
                    var rowView = record.Data as DataRowView;
                    if (rowView != null)
                    {
                        var labelName = rowView["Label"]?.ToString();
                        if (!string.IsNullOrEmpty(labelName))
                        {
                            //Protección contra tabla vacía o sin columnas
                            if (Helpers.labelsTable == null || !Helpers.labelsTable.Columns.Contains("Label_name"))
                                return;

                            var match = Helpers.labelsTable.Select($"Label_name = '{labelName.Replace("'", "''")}'").FirstOrDefault();
                            if (match != null)
                            {
                                int colorValue = Convert.ToInt32(match["Label_color"]);
                                Color bgColor = Color.FromArgb(colorValue);
                                e.Style.BackColor = bgColor;
                                e.Style.TextColor = GetContrastingTextColor(bgColor);
                            }
                        }
                    }
                }
            }
        }



        private Color GetContrastingTextColor(Color bgColor)
        {
            double luminancia = (0.299 * bgColor.R + 0.587 * bgColor.G + 0.114 * bgColor.B) / 255;
            return luminancia > 0.5 ? Color.Black : Color.White;
        }





        //Funcion para cargar la tabla labels a una variable global Helpers.labelsTable
        private void setLabels()
        {
            using (SQLiteConnection connection = new SQLiteConnection(Helpers.chromeViewerConnectionString))
            {
                connection.Open();

                // Cargar los datos al DataTable
                Helpers.dataAdapter = new SQLiteDataAdapter("SELECT Label_name, Label_color FROM Labels", connection);
                SQLiteCommandBuilder builder = new SQLiteCommandBuilder(Helpers.dataAdapter);

                Helpers.labelsTable = new DataTable();
                Helpers.dataAdapter.Fill(Helpers.labelsTable);

            }
        }

        //private async void ConfigureWebViewContextMenu()
        //{
        //    await webView21_detailPanel.EnsureCoreWebView2Async();

        //    webView21_detailPanel.CoreWebView2.ContextMenuRequested += (s, args) =>
        //    {
        //        var items = args.MenuItems;

        //        // Eliminar opción "Inspect"
        //        for (int i = items.Count - 1; i >= 0; i--)
        //        {
        //            var item = items[i];
        //            if (item.Kind == CoreWebView2ContextMenuItemKind.Command &&
        //                item.Name.ToLower().Contains("inspect"))
        //            {
        //                items.RemoveAt(i);
        //            }
        //        }
        //    };
        //}



        private void Form1_Load(object sender, EventArgs e)
        {

            SetupContextMenu();
            //ConfigureWebViewContextMenu();
            //InitializeWebView2();
            SetupRichTextBoxContextMenu();
        }


        //private async Task InitializeWebView2()
        //{
        //    string localDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BrowserReviewer", "WebView2UserData");
        //    var env = await CoreWebView2Environment.CreateAsync(null, localDataPath);

        //    await webView21_detailPanel.EnsureCoreWebView2Async(env);
        //}


        //Funciones para menu emergente con btn derecho Mouse.


        private void SetupRichTextBoxContextMenu()
        {
            var menu = new ContextMenuStrip();
            //menu.Items.Add("Cut", null, (s, e) => richTextBox1.Cut());
            menu.Items.Add("Copy", null, (s, e) => richTextBox1.Copy());
            //menu.Items.Add("Paste", null, (s, e) => richTextBox1.Paste());
            menu.Items.Add("Select All", null, (s, e) => richTextBox1.SelectAll());

            richTextBox1.ContextMenuStrip = menu;
        }

        private void SetupContextMenu()
        {
            labelContextMenu = new ContextMenuStrip();

            // Suscribimos el evento Opening, que se dispara justo antes de mostrar el menú
            labelContextMenu.Opening += LabelContextMenu_Opening;

            // Asignar a la grilla
            sfDataGrid1.ContextMenuStrip = labelContextMenu;
        }

        private void LabelContextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            labelContextMenu.Items.Clear();


            // Item "Open Label Manager"
            ToolStripMenuItem openLabelManagerItem = new ToolStripMenuItem("Open Label Manager");
            openLabelManagerItem.Click += (s, ev) => OpenLabelManager();
            labelContextMenu.Items.Add(openLabelManagerItem);


            // Submenú "Add Label"
            var addLabelItem = new ToolStripMenuItem("Add Label");

            if (Helpers.labelsTable != null && Helpers.labelsTable.Rows.Count > 0)
            {
                foreach (System.Data.DataRow row in Helpers.labelsTable.Rows)
                {
                    string labelName = row["Label_name"].ToString();

                    var labelItem = new ToolStripMenuItem(labelName);
                    labelItem.Click += (s, ev) => AsignarLabelAFilaSeleccionada(labelName);

                    addLabelItem.DropDownItems.Add(labelItem);
                }
            }
            else
            {
                addLabelItem.DropDownItems.Add("(No labels loaded)");
            }

            labelContextMenu.Items.Add(addLabelItem);



            //Remove label , solo del itel(s) seleccionados

            // Item "Remove Label"
            ToolStripMenuItem removeLabelItem = new ToolStripMenuItem("Remove Label");
            removeLabelItem.Click += (s, ev) => RemoverLabelAFilasSeleccionadas();
            labelContextMenu.Items.Add(removeLabelItem);




            // Item "Add Comment"
            ToolStripMenuItem addCommentItem = new ToolStripMenuItem("Add Comment");
            addCommentItem.Click += (s, ev) => OpenComments();
            labelContextMenu.Items.Add(addCommentItem);


        }


        private void AsignarLabelAFilaSeleccionada(string labelName)
        {
            var selectedRows = sfDataGrid1.SelectedItems.Cast<object>().ToList();

            foreach (var record in selectedRows)
            {
                var row = record as DataRowView;

                if (row != null && row.Row.Table.Columns.Contains("Label"))
                {
                    // 1. Actualizar visualmente el Label
                    row["Label"] = labelName;

                    // 2. Detectar columnas presentes para determinar tabla
                    var columnasPresentes = row.DataView.Table.Columns
                                                .Cast<DataColumn>()
                                                .Select(c => c.ColumnName)
                                                .ToList();

                    string tablaDetectada = "Desconocida";

                    foreach (var kvp in Helpers.TablesAndFields)
                    {
                        var camposEsperados = kvp.Value.OrderBy(c => c).ToList();
                        var columnasOrdenadas = columnasPresentes.OrderBy(c => c).ToList();

                        if (camposEsperados.SequenceEqual(columnasOrdenadas))
                        {
                            tablaDetectada = kvp.Key;
                            //MyTools.LogToConsole(Console, tablaDetectada);
                            break;
                        }
                    }

                    // 3. Resolver tabla real si viene de una vista combinada
                    if (tablaDetectada == "AllWebHistory" || tablaDetectada == "AllDownloads" || tablaDetectada == "AllBookmarks"
                        || tablaDetectada == "bookmarks_Chrome" || tablaDetectada == "bookmarks_Firefox")
                    {
                        var browser = row["Browser"]?.ToString()?.ToLowerInvariant();

                        if (tablaDetectada == "AllWebHistory")
                            tablaDetectada = browser == "firefox" ? "firefox_results" : "results";
                        else if (tablaDetectada == "AllDownloads")
                            tablaDetectada = browser == "firefox" ? "firefox_downloads" : "chrome_downloads";
                        else if (tablaDetectada == "AllBookmarks")
                            tablaDetectada = browser == "firefox" ? "bookmarks_Firefox" : "bookmarks_Chrome";
                        else if (tablaDetectada == "bookmarks_Chrome")
                            tablaDetectada = browser == "firefox" ? "bookmarks_Firefox" : "bookmarks_Chrome";
                        else if (tablaDetectada == "bookmarks_Firefox")
                            tablaDetectada = browser == "firefox" ? "bookmarks_Firefox" : "bookmarks_Chrome";
                    }

                    // 4. Actualizar la base de datos
                    var id = Convert.ToInt32(row["id"]);

                    using (SQLiteConnection conn = new SQLiteConnection(Helpers.chromeViewerConnectionString))
                    {
                        conn.Open();
                        using (SQLiteCommand cmd = new SQLiteCommand($"UPDATE {tablaDetectada} SET Label = @label WHERE id = @id", conn))
                        {
                            cmd.Parameters.AddWithValue("@label", labelName);
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }

            // Refrescar el grid para que se dispare QueryCellStyle
            sfDataGrid1.Refresh();
        }


        private void AplicarComentarioAFilasSeleccionadas()
        {
            var selectedRows = sfDataGrid1.SelectedItems.Cast<object>().ToList();

            foreach (var record in selectedRows)
            {
                var row = record as DataRowView;

                if (row != null && row.Row.Table.Columns.Contains("Comment"))
                {
                    // 1. Actualizar visualmente
                    row["Comment"] = Helpers.comment;

                    // 2. Detectar a qué tabla pertenece
                    var columnasPresentes = row.DataView.Table.Columns
                                                .Cast<DataColumn>()
                                                .Select(c => c.ColumnName)
                                                .ToList();

                    string tablaDetectada = "Desconocida";

                    foreach (var kvp in Helpers.TablesAndFields)
                    {
                        var camposEsperados = kvp.Value.OrderBy(c => c).ToList();
                        var columnasOrdenadas = columnasPresentes.OrderBy(c => c).ToList();

                        if (camposEsperados.SequenceEqual(columnasOrdenadas))
                        {
                            tablaDetectada = kvp.Key;
                            break;
                        }
                    }

                    // Resolver vistas combinadas
                    if (tablaDetectada == "AllWebHistory" || tablaDetectada == "AllDownloads" || tablaDetectada == "AllBookmarks"
                        || tablaDetectada == "bookmarks_Chrome" || tablaDetectada == "bookmarks_Firefox")
                    {
                        var browser = row["Browser"]?.ToString()?.ToLowerInvariant();

                        if (tablaDetectada == "AllWebHistory")
                            tablaDetectada = browser == "firefox" ? "firefox_results" : "results";
                        else if (tablaDetectada == "AllDownloads")
                            tablaDetectada = browser == "firefox" ? "firefox_downloads" : "chrome_downloads";
                        else if (tablaDetectada == "AllBookmarks")
                            tablaDetectada = browser == "firefox" ? "bookmarks_Firefox" : "bookmarks_Chrome";
                        else if (tablaDetectada == "bookmarks_Chrome")
                            tablaDetectada = browser == "firefox" ? "bookmarks_Firefox" : "bookmarks_Chrome";
                        else if (tablaDetectada == "bookmarks_Firefox")
                            tablaDetectada = browser == "firefox" ? "bookmarks_Firefox" : "bookmarks_Chrome";
                    }

                    // 3. Actualizar la base de datos
                    var id = Convert.ToInt32(row["id"]);

                    using (SQLiteConnection conn = new SQLiteConnection(Helpers.chromeViewerConnectionString))
                    {
                        conn.Open();
                        using (SQLiteCommand cmd = new SQLiteCommand($"UPDATE {tablaDetectada} SET Comment = @comment WHERE id = @id", conn))
                        {
                            if (string.IsNullOrWhiteSpace(Helpers.comment))
                                cmd.Parameters.AddWithValue("@comment", DBNull.Value);
                            else
                                cmd.Parameters.AddWithValue("@comment", Helpers.comment);
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }

            // Refrescar vista
            sfDataGrid1.Refresh();
        }



        private void RemoverLabelAFilasSeleccionadas()
        {
            var selectedRows = sfDataGrid1.SelectedItems.Cast<object>().ToList();

            foreach (var record in selectedRows)
            {
                var row = record as DataRowView;

                if (row != null && row.Row.Table.Columns.Contains("Label"))
                {
                    // 1. Eliminar visualmente
                    row["Label"] = DBNull.Value;

                    // 2. Detectar tabla según columnas
                    var columnasPresentes = row.DataView.Table.Columns
                                                .Cast<DataColumn>()
                                                .Select(c => c.ColumnName)
                                                .ToList();

                    string tablaDetectada = "Desconocida";

                    foreach (var kvp in Helpers.TablesAndFields)
                    {
                        var camposEsperados = kvp.Value.OrderBy(c => c).ToList();
                        var columnasOrdenadas = columnasPresentes.OrderBy(c => c).ToList();

                        if (camposEsperados.SequenceEqual(columnasOrdenadas))
                        {
                            tablaDetectada = kvp.Key;
                            break;
                        }
                    }

                    // Resolver tabla real si es una vista combinada
                    if (tablaDetectada == "AllWebHistory" || tablaDetectada == "AllDownloads" || tablaDetectada == "AllBookmarks"
                        || tablaDetectada == "bookmarks_Chrome" || tablaDetectada == "bookmarks_Firefox")
                    {
                        var browser = row["Browser"]?.ToString()?.ToLowerInvariant();

                        if (tablaDetectada == "AllWebHistory")
                            tablaDetectada = browser == "firefox" ? "firefox_results" : "results";
                        else if (tablaDetectada == "AllDownloads")
                            tablaDetectada = browser == "firefox" ? "firefox_downloads" : "chrome_downloads";
                        else if (tablaDetectada == "AllBookmarks")
                            tablaDetectada = browser == "firefox" ? "bookmarks_Firefox" : "bookmarks_Chrome";
                        else if (tablaDetectada == "bookmarks_Chrome")
                            tablaDetectada = browser == "firefox" ? "bookmarks_Firefox" : "bookmarks_Chrome";
                        else if (tablaDetectada == "bookmarks_Firefox")
                            tablaDetectada = browser == "firefox" ? "bookmarks_Firefox" : "bookmarks_Chrome";
                    }

                    // 3. Actualizar la base de datos
                    var id = Convert.ToInt32(row["id"]);

                    using (SQLiteConnection conn = new SQLiteConnection(Helpers.chromeViewerConnectionString))
                    {
                        conn.Open();
                        using (SQLiteCommand cmd = new SQLiteCommand($"UPDATE {tablaDetectada} SET Label = NULL WHERE id = @id", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }

            // Refrescar el grid para aplicar estilos
            sfDataGrid1.Refresh();
        }




        // Hasta aqui funciones menu emergente btn derecho mouse


        private void SfDataGrid_AutoGeneratingColumn(object sender, AutoGeneratingColumnArgs e)
        {
            // Lista de columnas que NO deben permitir filtrado
            var columnasSinFiltro = new List<string> { "id", "Url", "Visit_time", "Last_visit_time", "Visit_duration", "Start_time", "End_time", "DateAdded", "LastModified", "LastUsed" };

            if (columnasSinFiltro.Contains(e.Column.MappingName))
            {
                e.Column.AllowFiltering = false;
            }
        }


        private void cToolStripMenuItem_Click(object sender, EventArgs e)
        {
            initAll();

            disableButtons();
            //richTextBox1.Clear();
        }








        private void SfDataGrid_FilterChanged(object sender, Syncfusion.WinForms.DataGrid.Events.FilterChangedEventArgs e)
        {
            // Actualizar el número de elementos visibles después de aplicar un filtro
            labelItemCount.Text = $"Items count: {sfDataGrid1.View.Records.Count}";
        }



        //private async void SfDataGrid1_SelectionChanged(object sender, Syncfusion.WinForms.DataGrid.Events.SelectionChangedEventArgs e)
        //{
        //    if (sfDataGrid1.SelectedItem is DataRowView dataRowView)
        //    {
        //        string html = BuildHtmlFromRow(dataRowView);
        //        await webView21_detailPanel.EnsureCoreWebView2Async(); // ¡Siempre!
        //        webView21_detailPanel.NavigateToString(html);
        //    }
        //}

        //private string BuildHtmlFromRow(DataRowView row)
        //{
        //    StringBuilder sb = new StringBuilder();

        //    sb.AppendLine("<html><head><style>");
        //    sb.AppendLine("body { font-family: 'Segoe UI'; font-size: 14px; padding: 10px; }");
        //    sb.AppendLine(".row { display: flex; align-items: flex-start; margin-bottom: 10px; }");
        //    sb.AppendLine(".icon { width: 20px; height: 20px; margin-right: 8px; margin-top: 2px; }");
        //    sb.AppendLine(".labelblock { flex: 1; }");
        //    sb.AppendLine(".label { font-weight: bold; color: #444; }");
        //    sb.AppendLine(".value { color: #000; display: block; margin-top: 2px; word-break: break-word; }");
        //    sb.AppendLine("a { color: #0078D7; text-decoration: none; }");
        //    sb.AppendLine("a:hover { text-decoration: underline; }");
        //    sb.AppendLine("</style></head><body>");

        //    foreach (DataColumn column in row.DataView.Table.Columns)
        //    {

        //        string name = column.ColumnName;
        //        //MyTools.LogToConsole(Console, $"Columna detectada: {name}");
        //        string label = BeautifyColumnName(name, out string iconBase64);

        //        string value = row[name]?.ToString() ?? "N/A";

        //        // Escapar contenido HTML
        //        value = System.Net.WebUtility.HtmlEncode(value);

        //        // Detectar links
        //        if (name == "Url" && Uri.IsWellFormedUriString(value, UriKind.Absolute))
        //        {
        //            value = $"<a href='{value}' target='_blank'>{value}</a>";
        //        }

        //        sb.AppendLine("<div class='row'>");

        //        if (!string.IsNullOrEmpty(iconBase64))
        //        {
        //            sb.AppendLine($"<img class='icon' src='data:image/png;base64,{iconBase64}' />");
        //        }

        //        sb.AppendLine("<div class='labelblock'>");
        //        sb.AppendLine($"<span class='label'>{label}:</span><br />");
        //        sb.AppendLine($"<span class='value'>{value}</span>");
        //        sb.AppendLine("</div>");

        //        sb.AppendLine("</div>");
        //    }

        //    sb.AppendLine("</body></html>");



        //    string finalHtml = sb.ToString();

        //    if (Helpers.searchTermExists)
        //    {
        //        finalHtml = HighlightSearchTerms(finalHtml, Helpers.searchTerm, Helpers.searchTermRegExp);
        //    }

        //    //finalHtml += $"<hr><div style='color: gray;'>Generado a las {DateTime.Now}</div>";


        //    return finalHtml;

        //}





        //private string BeautifyColumnName(string columnName, out string iconBase64)

        //{
        //    iconBase64 = columnName switch
        //    {
        //        "id" => ImageToBase64(Resource1.id_icon),
        //        "Browser" => ImageToBase64(Resource1.browser_icon),
        //        "Category" => ImageToBase64(Resource1.category_icon),
        //        "Potential_activity" => ImageToBase64(Resource1.activity_icon),
        //        "Visit_id" => ImageToBase64(Resource1.visit_icon),
        //        "Url" => ImageToBase64(Resource1.url_icon),
        //        "URL" => ImageToBase64(Resource1.url_icon),
        //        "Title" => ImageToBase64(Resource1.title_icon),
        //        "Visit_time" => ImageToBase64(Resource1.clock_icon),
        //        "Last_visit_time" => ImageToBase64(Resource1.clock_icon),
        //        "Visit_duration" => ImageToBase64(Resource1.clock_icon),
        //        "Start_time" => ImageToBase64(Resource1.clock_icon),
        //        "End_time" => ImageToBase64(Resource1.clock_icon),
        //        "DateAdded" => ImageToBase64(Resource1.clock_icon),
        //        "LastModified" => ImageToBase64(Resource1.clock_icon),
        //        "Visit_count" => ImageToBase64(Resource1.count_icon),
        //        "Typed_count" => ImageToBase64(Resource1.count_icon),
        //        "File" => ImageToBase64(Resource1.file_icon),
        //        "Label" => ImageToBase64(Resource1.label_icon),
        //        "Comment" => ImageToBase64(Resource1.comment_icon),
        //        _ => ImageToBase64(Resource1.generic_icon)
        //    };

        //    return columnName switch
        //    {
        //        "id" => "ID",
        //        "Browser" => "Browser",
        //        "Category" => "Category",
        //        "Potential_activity" => "Potential Activity",
        //        "Visit_id" => "Visit ID",
        //        "Url" => "URL",
        //        "URL" => "URL",
        //        "Title" => "Title",
        //        "Visit_time" => "Visit Time",
        //        "Last_visit_time" => "Last Visit Time",
        //        "Visit_duration" => "Visit Duration",
        //        "Start_time" => "Start Time",
        //        "End_time" => "End Time",
        //        "DateAdded" => "Date Added",
        //        "LastModified" => "Last Modified",
        //        "Visit_count" => "Visit Count",
        //        "Typed_count" => "Typed Count",
        //        "File" => "File",
        //        "Label" => "Label",
        //        "Comment" => "Comment",
        //        _ => columnName
        //    };
        //}

        //private string ImageToBase64(Image image)
        //{
        //    using (MemoryStream ms = new MemoryStream())
        //    {
        //        image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        //        return Convert.ToBase64String(ms.ToArray());
        //    }
        //}


        //private void SfDataGrid1_SelectionChanged(object sender, Syncfusion.WinForms.DataGrid.Events.SelectionChangedEventArgs e)
        //{
        //    if (sfDataGrid1.SelectedItem is DataRowView dataRowView)
        //    {
        //        richTextBox1.Clear(); // Limpiar el contenido previo

        //        foreach (DataColumn column in dataRowView.DataView.Table.Columns)
        //        {
        //            string columnName = BeautifyColumnName(column.ColumnName);
        //            string columnValue = dataRowView[column.ColumnName]?.ToString() ?? "N/A";

        //            AppendFormattedLine(columnName, columnValue);
        //        }

        //        highlight_Text(); // Mantén tu función si resalta texto especial
        //    }
        //}


        //private void AppendFormattedLine(string label, string value)
        //{
        //    richTextBox1.SelectionFont = new Font(richTextBox1.Font, FontStyle.Bold);
        //    richTextBox1.SelectionColor = Color.Black;
        //    richTextBox1.AppendText($"{label}: ");

        //    richTextBox1.SelectionFont = new Font(richTextBox1.Font, FontStyle.Regular);
        //    richTextBox1.SelectionColor = Color.Black;
        //    richTextBox1.AppendText($"{value}\n\n");

        //    // Reset color por si acaso
        //    richTextBox1.SelectionColor = Color.Black;
        //}


        //private string BeautifyColumnName(string columnName)
        //{
        //    return columnName switch
        //    {
        //        "id" => "🆔 ID",
        //        "Browser" => "🌐 Browser",
        //        "Category" => "📂 Category",
        //        "Potential_activity" => "🔍 Potential Activity",
        //        "Visit_id" => "🔗 Visit ID",
        //        "Url" => "🌍 URL",
        //        "Title" => "📄 Title",
        //        "Visit_time" => "🕒 Visit Time",
        //        "File" => "📁 File",
        //        "Label" => "🏷️ Label",
        //        "Comment" => "💬 Comment",
        //        _ => columnName
        //    };
        //}




        private void SfDataGrid1_SelectionChanged(object sender, Syncfusion.WinForms.DataGrid.Events.SelectionChangedEventArgs e)
        {
            if (sfDataGrid1.SelectedItem != null)
            {
                DataRowView dataRowView = sfDataGrid1.SelectedItem as DataRowView;
                if (dataRowView != null)
                {
                    richTextBox1.Clear(); // Limpiar el contenido previo del RichTextBox

                    foreach (DataColumn column in dataRowView.DataView.Table.Columns)
                    {
                        string columnName = column.ColumnName;
                        string columnValue = dataRowView[columnName]?.ToString() ?? "N/A";

                        // Escribir el ColumnName en negrita
                        richTextBox1.SelectionFont = new Font(richTextBox1.Font, FontStyle.Bold);
                        richTextBox1.AppendText($"{columnName}: ");

                        // Escribir el ColumnValue en azul
                        richTextBox1.SelectionFont = new Font(richTextBox1.Font, FontStyle.Regular);
                        richTextBox1.SelectionColor = Color.Blue;
                        richTextBox1.AppendText($"{columnValue}\n");

                        richTextBox1.AppendText("\n");

                        // Restablecer el color y estilo para el próximo uso
                        richTextBox1.SelectionColor = richTextBox1.ForeColor;
                        richTextBox1.SelectionFont = new Font(richTextBox1.Font, FontStyle.Regular);
                    }
                }
                highlight_Text();
            }
        }


        private void NumericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            // Actualiza la variable global utcOffset
            Helpers.utcOffset = (int)numericUpDown1.Value;
            //MyTools.LogToConsole(Console, Helpers.utcOffset.ToString());
        }






        private void InitializeNumericUpDown()
        {
            // Establecer el rango de UTC
            numericUpDown1.Minimum = -12;  // UTC-12:00
            numericUpDown1.Maximum = 14;   // UTC+14:00
            numericUpDown1.Increment = 1;  // Incremento de 1 hora
            numericUpDown1.DecimalPlaces = 0;  // Sin decimales, solo números enteros

            // Establecer el valor predeterminado, por ejemplo, UTC 0
            numericUpDown1.Value = 0;  // UTC 0
        }






        [SQLiteFunction(Name = "REGEXP", Arguments = 2, FuncType = FunctionType.Scalar)]
        public class MyRegEx : SQLiteFunction
        {



            public override object Invoke(object[] args)
            {
                try
                {
                    if (args[0] == null || args[1] == null)
                        return false;

                    string pattern = Convert.ToString(args[0]);
                    string input = Convert.ToString(args[1]);

                    return Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                }
                catch
                {
                    return false;
                }
            }

        }










        void enableButtons()
        {

            button_LabelManager.Enabled = true;
            button_SearchWebActivity.Enabled = true;
            groupBox_customSearch.Enabled = true;
            groupBox_Main.Enabled = true;
            richTextBox1.Enabled = true;
            button_exportPDF.Enabled = true;
            Console.Enabled = true;
        }

        void disableButtons()
        {
            button_LabelManager.Enabled = false;
            button_SearchWebActivity.Enabled = false;
            groupBox_customSearch.Enabled = false;
            groupBox_Main.Enabled = false;
            richTextBox1.Enabled = false;
            button_exportPDF.Enabled = false;
            Console.Enabled = false;

        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {

            //string db_name = "";
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                // Configurar propiedades del SaveFileDialog
                //saveFileDialog.Filter = "qlog files (*.qlog)|*.qlog|All files (*.*)|*.*";
                saveFileDialog.Filter = "Browser Reviewer files (*.bre)|*.bre";
                saveFileDialog.Title = "Create Project";
                saveFileDialog.FileName = "Default.bre"; // Nombre predeterminado del archivo

                // Mostrar el cuadro de diálogo y obtener el resultado
                DialogResult result = saveFileDialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    initAll();
                    try
                    {
                        Helpers.db_name = saveFileDialog.FileName;
                        Helpers.chromeViewerConnectionString = $"Data Source={Helpers.db_name};Version=3;";
                        this.Text = "Browser Reviewer v0.1 is working on:   " + Helpers.db_name;
                        Tools.CreateDatabase(Helpers.chromeViewerConnectionString);
                        enableButtons();
                        Helpers.labelsTable = new DataTable();
                        search_textBox.Text = "";
                        Helpers.searchTermExists = false;
                        Helpers.searchTermRegExp = false;
                        Helpers.searchTimeCondition = false;
                        this.sfDataGrid1.SearchController.ClearSearch();
                        groupBox_customSearch.BackColor = SystemColors.Control;
                        checkBox_RegExp.Checked = false;
                        checkBox_enableTime.Checked = false;
                        Helpers.sqltimecondition = ""; // vacío por defecto
                        Helpers.sqlDownloadtimecondition = "";
                        Helpers.sqlBookmarkstimecondition = "";
                        Helpers.sqlAutofilltimecondition = "";

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error when creating the project: {ex.Message}", "Browser Reviewer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

            }
        }

        private void openToolStripMenuItem_Click_1(object sender, EventArgs e)
        {


            int utcOffset = Helpers.utcOffset;  // Obtener el valor actual de utcOffset
            string sqlquery;


            if (utcOffset == 0)
            {
                sqlquery = @"SELECT
                            r.id AS id,
                            r.Browser AS Browser,
                            r.Category AS Category,
                            r.Potential_activity AS Potential_activity,
                            r.Visit_id AS Visit_id,
                            r.Url AS Url,
                            r.Title AS Title,
                            STRFTIME('%Y-%m-%d %H:%M:%f', r.Visit_time) AS Visit_time,
                            r.File AS File,
                            r.Label AS Label,
                            r.Comment AS Comment
                        FROM 
                            results r
                        UNION ALL
                        SELECT 
                            f.id AS id,
                            f.Browser AS Browser,
                            f.Category AS Category,
                            f.Potential_activity AS Potential_activity,
                            f.Visit_id AS Visit_id,
                            f.Url AS Url,
                            f.Title AS Title,
                            STRFTIME('%Y-%m-%d %H:%M:%f', f.Visit_time) AS Visit_time,
                            f.File AS File,
                            f.Label AS Label,
                            f.Comment AS Comment
                        FROM 
                            firefox_results f;";
            }
            else
            {
                string offset = utcOffset > 0 ? $"+{utcOffset} hours" : $"{utcOffset} hours";

                sqlquery = $@"SELECT 
                            r.id AS id,
                            r.Browser AS Browser,
                            r.Category AS Category,
                            r.Potential_activity AS Potential_activity,
                            r.Visit_id AS Visit_id,
                            r.Url AS Url,
                            r.Title AS Title,
                            STRFTIME('%Y-%m-%d %H:%M:%f', r.Visit_time, '{offset}') AS Visit_time,
                            r.File AS File,
                            r.Label AS Label,
                            r.Comment AS Comment
                        FROM 
                            results r
                        UNION ALL
                        SELECT 
                            f.id AS id,
                            f.Browser AS Browser,
                            f.Category AS Category,
                            f.Potential_activity AS Potential_activity,
                            f.Visit_id AS Visit_id,
                            f.Url AS Url,
                            f.Title AS Title,
                            STRFTIME('%Y-%m-%d %H:%M:%f', f.Visit_time, '{offset}') AS Visit_time,
                            f.File AS File,
                            f.Label AS Label,
                            f.Comment AS Comment
                        FROM 
                            firefox_results f;";
            }



            using (OpenFileDialog OpenFileDialog = new OpenFileDialog())
            {
                OpenFileDialog.Filter = "Browser Reviewer files (*.bre)|*.bre";
                OpenFileDialog.Title = "Open Project";
                DialogResult result = OpenFileDialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    initAll();

                    try
                    {
                        Helpers.db_name = OpenFileDialog.FileName;
                        Helpers.chromeViewerConnectionString = $"Data Source={Helpers.db_name};Version=3;";
                        this.Text = "Browser Reviewer v0.1 is working on:   " + Helpers.db_name;
                        //Llenar la variable Helpers.labelsTable para el menu emergente de add label
                        setLabels();

                        Helpers.browserUrls = Tools.FillDictionaryFromDatabase();

                        Helpers.browsersWithDownloads = MyTools.GetBrowsersWithDownloads();

                        Helpers.browsersWithBookmarks = MyTools.GetBrowsersWithBookmarks();

                        Helpers.browsersWithAutofill = MyTools.GetBrowsersWithAutofill();


                        AcordeonMenu(Helpers.browserUrls, Helpers.browsersWithDownloads, Helpers.browsersWithBookmarks, Helpers.browsersWithAutofill);

                        Tools.ShowQueryOnDataGridView(sfDataGrid1, Helpers.chromeViewerConnectionString, sqlquery, labelItemCount);
                        enableButtons();

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error opening the project:\n{ex.ToString()}", "ChromeViewer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                }

            }
            enableButtons();
            labelStatus.Text = "All web history from all browsers";

        }


        private async void button_SearchWebActivity_Click(object sender, EventArgs e)
        {
            int utcOffset = Helpers.utcOffset;  // Obtener el valor actual de utcOffset
            string sqlquery;

            if (utcOffset == 0)
            {
                sqlquery = @"SELECT
                            r.id AS id,
                            r.Browser AS Browser,
                            r.Category AS Category,
                            r.Potential_activity AS Potential_activity,
                            r.Visit_id AS Visit_id,
                            r.Url AS Url,
                            r.Title AS Title,
                            STRFTIME('%Y-%m-%d %H:%M:%f', r.Visit_time) AS Visit_time,
                            r.File AS File,
                            r.Label AS Label,
                            r.Comment AS Comment
                        FROM 
                            results r
                        UNION ALL
                        SELECT 
                            f.id AS id,
                            f.Browser AS Browser,
                            f.Category AS Category,
                            f.Potential_activity AS Potential_activity,
                            f.Visit_id AS Visit_id,
                            f.Url AS Url,
                            f.Title AS Title,
                            STRFTIME('%Y-%m-%d %H:%M:%f', f.Visit_time) AS Visit_time,
                            f.File AS File,
                            f.Label AS Label,
                            f.Comment AS Comment
                        FROM 
                            firefox_results f;";
            }
            else
            {
                string offset = utcOffset > 0 ? $"+{utcOffset} hours" : $"{utcOffset} hours";

                sqlquery = $@"SELECT 
                            r.id AS id,
                            r.Browser AS Browser,
                            r.Category AS Category,
                            r.Potential_activity AS Potential_activity,
                            r.Visit_id AS Visit_id,
                            r.Url AS Url,
                            r.Title AS Title,
                            STRFTIME('%Y-%m-%d %H:%M:%f', r.Visit_time, '{offset}') AS Visit_time,
                            r.File AS File,
                            r.Label AS Label,
                            r.Comment AS Comment
                        FROM 
                            results r
                        UNION ALL
                        SELECT 
                            f.id AS id,
                            f.Browser AS Browser,
                            f.Category AS Category,
                            f.Potential_activity AS Potential_activity,
                            f.Visit_id AS Visit_id,
                            f.Url AS Url,
                            f.Title AS Title,
                            STRFTIME('%Y-%m-%d %H:%M:%f', f.Visit_time, '{offset}') AS Visit_time,
                            f.File AS File,
                            f.Label AS Label,
                            f.Comment AS Comment
                        FROM 
                            firefox_results f;";
            }





            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {

                folderBrowserDialog.Description = "Select a path to search SQLite databases 3";
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    //initAll();
                    disableButtons();
                    MyTools.LogToConsole(Console, $"Processing...");

                    string rootDirectory = folderBrowserDialog.SelectedPath;

                    await Tools.ListFilesAndDirectories(rootDirectory, Console);

                    MyTools.CloseLog();
                    MyTools.LogToConsole(Console, $"Processing Finished.");
                    enableButtons();

                }
                else
                {
                    return;
                }
            }

            Tools.ShowQueryOnDataGridView(sfDataGrid1, Helpers.chromeViewerConnectionString, sqlquery, labelItemCount);
            Helpers.browsersWithDownloads = MyTools.GetBrowsersWithDownloads();
            Helpers.browsersWithBookmarks = MyTools.GetBrowsersWithBookmarks();
            Helpers.browsersWithAutofill = MyTools.GetBrowsersWithAutofill();
            AcordeonMenu(Helpers.browserUrls, Helpers.browsersWithDownloads, Helpers.browsersWithBookmarks, Helpers.browsersWithAutofill);



            labelStatus.Text = "All web history from all browsers";

            button_SearchWebActivity.Enabled = false;

        }






        private void AcordeonMenu(Dictionary<string, List<string>> navegadorCategorias, Dictionary<string, int> browsersWithDownloads, Dictionary<string, int> browsersWithBookmarks, Dictionary<string, int> browsersWithDatafill)
        {
            Font fm = new Font(Font.FontFamily, 11);
            Font fs = new Font(Font.FontFamily, 10);

            FlowLayoutPanel flwMain = new FlowLayoutPanel()
            {
                BackColor = this.BackColor,
                BorderStyle = BorderStyle.FixedSingle,
                FlowDirection = FlowDirection.TopDown,
                Dock = DockStyle.Left,
                Width = 330,
                Margin = new Padding(0, 325, 0, 0),
                WrapContents = false,
                AutoScroll = true,
            };

            // Crear el botón "All Web History"
            Label lblAllHistoryButton = new Label()
            {
                //BackColor = Color.SteelBlue,
                BackColor = Color.White,
                Font = new Font(fm, FontStyle.Bold),
                ForeColor = Color.Black,
                Height = 48,
                Margin = new Padding(3, 1, 3, 1),
                Padding = new Padding(12, 3, 0, 3),
                Text = "All Web History",
                TextAlign = ContentAlignment.MiddleCenter,
                Width = flwMain.Width
            };

            lblAllHistoryButton.Click += (sender, e) =>
            {
                int utcOffset = Helpers.utcOffset;
                string sqlquery;


                if (utcOffset == 0)
                {
                    sqlquery = @"SELECT
                                r.id AS id,
                                r.Browser AS Browser,
                                r.Category AS Category,
                                r.Potential_activity AS Potential_activity,
                                r.Visit_id AS Visit_id,
                                r.Url AS Url,
                                r.Title AS Title,
                                STRFTIME('%Y-%m-%d %H:%M:%f', r.Visit_time) AS Visit_time,
                                r.File AS File,
                                r.Label AS Label,
                                r.Comment AS Comment
                            FROM 
                                results r
                            UNION ALL
                            SELECT 
                                f.id AS id,
                                f.Browser AS Browser,
                                f.Category AS Category,
                                f.Potential_activity AS Potential_activity,
                                f.Visit_id AS Visit_id,
                                f.Url AS Url,
                                f.Title AS Title,
                                STRFTIME('%Y-%m-%d %H:%M:%f', f.Visit_time) AS Visit_time,
                                f.File AS File,
                                f.Label AS Label,
                                f.Comment AS Comment
                            FROM 
                                firefox_results f;";
                }
                else
                {
                    string offset = utcOffset > 0 ? $"+{utcOffset} hours" : $"{utcOffset} hours";

                    sqlquery = $@"SELECT 
                                r.id AS id,
                                r.Browser AS Browser,
                                r.Category AS Category,
                                r.Potential_activity AS Potential_activity,
                                r.Visit_id AS Visit_id,
                                r.Url AS Url,
                                r.Title AS Title,
                                STRFTIME('%Y-%m-%d %H:%M:%f', r.Visit_time, '{offset}') AS Visit_time,
                                r.File AS File,
                                r.Label AS Label,
                                r.Comment AS Comment
                            FROM 
                                results r
                            UNION ALL
                            SELECT 
                                f.id AS id,
                                f.Browser AS Browser,
                                f.Category AS Category,
                                f.Potential_activity AS Potential_activity,
                                f.Visit_id AS Visit_id,
                                f.Url AS Url,
                                f.Title AS Title,
                                STRFTIME('%Y-%m-%d %H:%M:%f', f.Visit_time, '{offset}') AS Visit_time,
                                f.File AS File,
                                f.Label AS Label,
                                f.Comment AS Comment
                            FROM 
                                firefox_results f;";
                }

                Tools.ShowQueryOnDataGridView(sfDataGrid1, Helpers.chromeViewerConnectionString, sqlquery, labelItemCount);
                labelStatus.Text = "All web history from all browsers";
            };

            lblAllHistoryButton.MouseHover += (sender, e) => { lblAllHistoryButton.BackColor = Color.LightBlue; };
            lblAllHistoryButton.MouseLeave += (sender, e) => { lblAllHistoryButton.BackColor = Color.White; };



            flwMain.Controls.Add(lblAllHistoryButton);

            foreach (var navegador in navegadorCategorias.OrderBy(category => category.Key)) //ordenar aqui
            {
                Image icono = null;
                switch (navegador.Key)
                {
                    case "Chrome":
                        icono = Resource1.Chrome.ToBitmap();
                        break;
                    case "Brave":
                        icono = Resource1.Brave.ToBitmap();
                        break;
                    case "Edge":
                        icono = Resource1.Edge.ToBitmap();
                        break;
                    case "Opera":
                        icono = Resource1.Opera.ToBitmap();
                        break;
                    case "Yandex":
                        icono = Resource1.Yandex.ToBitmap();
                        break;
                    case "Vivaldi":
                        icono = Resource1.Vivaldi.ToBitmap();
                        break;
                    case "Firefox":
                        icono = Resource1.Firefox.ToBitmap();
                        break;
                    default:
                        icono = Resource1.Unknown.ToBitmap();
                        break;
                }

                Label lblNavegador = new Label()
                {
                    BackColor = Color.SteelBlue,
                    Font = fm,
                    ForeColor = Color.White,
                    Height = 48,
                    Image = icono,
                    ImageAlign = ContentAlignment.MiddleLeft,
                    Margin = new Padding(3, 1, 3, 1),
                    Padding = new Padding(12, 3, 0, 3),
                    Text = navegador.Key,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Width = flwMain.Width
                };

                lblNavegador.MouseHover += (sender, e) => { lblNavegador.BackColor = Color.LightBlue; };
                lblNavegador.MouseLeave += (sender, e) => { lblNavegador.BackColor = Color.SteelBlue; };



                // Agregar al diccionario
                Helpers.navegadorLabels[navegador.Key] = lblNavegador;

                // Contar el numero de registros por navegador

                //if (Helpers.searchTermExists || checkBox_enableTime.Checked)
                if (Helpers.searchTermExists || Helpers.searchTimeCondition)
                {
                    if (Helpers.historyHits.TryGetValue(navegador.Key, out int count))
                    {
                        Helpers.itemscount = count;
                    }
                    else
                    {
                        Helpers.itemscount = 0; // Si la clave no existe, asigna 0
                    }
                }
                else
                {
                    Helpers.itemscount = Tools.NumUrlsWithBrowser(navegador.Key);
                }

                //Actualizar propiedad text del label
                Tools.UpdateNavegadorLabel(navegador.Key, navegador.Key + " " + Helpers.itemscount + " hits");



                FlowLayoutPanel flwSubNavegador = new FlowLayoutPanel()
                {
                    AutoSize = true,
                    BackColor = Color.Beige,
                    FlowDirection = FlowDirection.TopDown,
                    Dock = DockStyle.Left,
                    Visible = false,
                    Width = 360
                };

                icono = Resource1.AllHistory.ToBitmap();

                Label lblAllHistory = new Label()
                {
                    BackColor = Color.Transparent,
                    Font = fs,
                    ForeColor = Color.Black,
                    Image = icono,
                    ImageAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(32, 1, 0, 1),
                    Text = "All History",
                    TextAlign = ContentAlignment.MiddleCenter,
                    Width = flwMain.Width,
                    AutoSize = false,
                    Height = Math.Max(icono.Height + 5, fs.Height + 10)
                };

                lblAllHistory.Click += (sender, e) =>
                {
                    string tableName = navegador.Key.ToLower().Contains("firefox") ? "firefox_results" : "results";
                    MyTools tools = new MyTools();
                    tools.MostrarPorCategoria(labelStatus, sfDataGrid1, Helpers.chromeViewerConnectionString, tableName, null, navegador.Key, labelItemCount);
                };

                lblAllHistory.MouseHover += (sender, e) => { lblAllHistory.BackColor = Color.LightBlue; };
                lblAllHistory.MouseLeave += (sender, e) => { lblAllHistory.BackColor = Color.Transparent; };



                flwSubNavegador.Controls.Add(lblAllHistory);

                Dictionary<string, List<string>> categorias = new Dictionary<string, List<string>>();

                foreach (var url in navegador.Value)
                {
                    string categoria = MyTools.Evaluatecategory(url);

                    if (!categorias.ContainsKey(categoria))
                    {
                        categorias[categoria] = new List<string>();
                    }

                    categorias[categoria].Add(url);
                }



                foreach (var categoria in categorias.OrderBy(c => c.Key))
                {

                    //Aqui insertar codigo para iconos a nivel categorias

                    icono = null;
                    switch (categoria.Key)
                    {
                        case "Ad Tracking and Analytics":
                            icono = Resource1.AdTrackingAnalytics.ToBitmap();
                            break;
                        case "AI":
                            icono = Resource1.AI.ToBitmap();
                            break;
                        case "Banking":
                            icono = Resource1.Banking.ToBitmap();
                            break;
                        case "Cloud Storage Services":
                            icono = Resource1.CloudStorageServices.ToBitmap();
                            break;
                        case "Code Hosting":
                            icono = Resource1.CodeHosting.ToBitmap();
                            break;
                        case "Entertainment":
                            icono = Resource1.Entertainment.ToBitmap();
                            break;
                        case "Firefox":
                            icono = Resource1.Firefox.ToBitmap();
                            break;
                        case "Facebook":
                            icono = Resource1.Facebook.ToBitmap();
                            break;
                        case "File Encryption Tools":
                            icono = Resource1.FileEncryptionTools.ToBitmap();
                            break;
                        case "Google":
                            icono = Resource1.Google.ToBitmap();
                            break;
                        case "Lan Addresses Browsing":
                            icono = Resource1.LanAddressesBrowsing.ToBitmap();
                            break;
                        case "Local Files":
                            icono = Resource1.LocalFiles.ToBitmap();
                            break;
                        case "News":
                            icono = Resource1.News.ToBitmap();
                            break;
                        case "Online Office Suite":
                            icono = Resource1.OnlineOfficeSuite.ToBitmap();
                            break;
                        case "Other":
                            icono = Resource1.Other.ToBitmap();
                            break;
                        case "Search Engine":
                            icono = Resource1.SearchEngine.ToBitmap();
                            break;
                        case "Shopping":
                            icono = Resource1.Shopping.ToBitmap();
                            break;
                        case "Social Media":
                            icono = Resource1.SocialMedia.ToBitmap();
                            break;
                        case "Technical Forums":
                            icono = Resource1.TechicalForums.ToBitmap();
                            break;
                        case "Webmail":
                            icono = Resource1.Webmail.ToBitmap();
                            break;
                        case "YouTube":
                            icono = Resource1.YouTube.ToBitmap();
                            break;
                        case "Gaming Platforms":
                            icono = Resource1.GamingPlatforms.ToBitmap();
                            break;
                        case "Adult Content Sites":
                            icono = Resource1.AdultContentSites.ToBitmap();
                            break;
                        case "Cryptocurrency Platforms":
                            icono = Resource1.CryptocurrencyPlatforms.ToBitmap();
                            break;
                        case "Hacking and Cybersecurity Sites":
                            icono = Resource1.HackingCybersecuritySites.ToBitmap();
                            break;
                        case "Airlines":
                            icono = Resource1.Airlines.ToBitmap();
                            break;
                        case "Hotels and Rentals":
                            icono = Resource1.Hotels.ToBitmap();
                            break;

                        default:
                            icono = Resource1.Unknown.ToBitmap();
                            break;
                    }





                    Label lblCategoria = new Label()
                    {
                        BackColor = Color.Transparent,
                        Font = fs,
                        ForeColor = Color.Black,
                        Image = icono,
                        ImageAlign = ContentAlignment.MiddleLeft,
                        Padding = new Padding(32, 1, 0, 1),
                        Text = $"{categoria.Key} ({categoria.Value.Count} hits)",
                        TextAlign = ContentAlignment.MiddleCenter,
                        Width = flwMain.Width,
                        AutoSize = false, // Para evitar que el Label se redimensione inesperadamente
                        Height = Math.Max(icono.Height + 5, fs.Height + 10) // Asegurar que la altura del Label sea suficiente
                    };




                    lblCategoria.Click += (sender, e) =>
                    {
                        string tableName = navegador.Key.ToLower().Contains("firefox") ? "firefox_results" : "results";
                        MyTools tools = new MyTools();
                        tools.MostrarPorCategoria(labelStatus, sfDataGrid1, Helpers.chromeViewerConnectionString, tableName, categoria.Key, navegador.Key, labelItemCount);
                    };

                    lblCategoria.MouseHover += (sender, e) => { lblCategoria.BackColor = Color.LightBlue; };
                    lblCategoria.MouseLeave += (sender, e) => { lblCategoria.BackColor = Color.Transparent; };

                    flwSubNavegador.Controls.Add(lblCategoria);
                }

                flwMain.Controls.Add(lblNavegador);
                flwMain.Controls.Add(flwSubNavegador);

                lblNavegador.Click += (sender, e) => { flwSubNavegador.Visible = !flwSubNavegador.Visible; };


            }

            // Aquí añadimos los controles relacionados con los downloads
            AddDownloadLabels(flwMain, Helpers.browsersWithDownloads);
            AddBookmarkLabels(flwMain, Helpers.browsersWithBookmarks);
            AddAutofillLabels(flwMain, Helpers.browsersWithAutofill);



            groupBox_Main.Controls.Add(flwMain);
        }



        private void AddDownloadLabels(FlowLayoutPanel flwMain, Dictionary<string, int> browsersWithDownloads)
        {
            Font fm = new Font(Font.FontFamily, 11);
            Font fs = new Font(Font.FontFamily, 10);

            // Crear el botón "All Downloads"
            Label lblAllDownloadsButton = new Label()
            {
                BackColor = Color.White,
                Font = new Font(fm, FontStyle.Bold),
                ForeColor = Color.Black,
                Height = 48,
                Margin = new Padding(3, 1, 3, 1),
                Padding = new Padding(12, 3, 0, 3),
                Text = "All Downloads",
                TextAlign = ContentAlignment.MiddleCenter,
                Width = flwMain.Width
            };

            lblAllDownloadsButton.Click += (sender, e) =>
            {
                int utcOffset = Helpers.utcOffset;
                string sqlquery;

                if (utcOffset == 0)
                {
                    sqlquery = @"SELECT 
                                 id,
                                 Browser,
                                 Current_path,
                                 STRFTIME('%Y-%m-%d %H:%M:%f', End_time) AS End_time,
                                 Received_bytes,
                                 Total_bytes,
                                 File, Label, Comment
                             FROM 
                                 chrome_downloads
                             UNION ALL
                             SELECT 
                                 id,
                                 Browser,
                                 Current_path,
                                 STRFTIME('%Y-%m-%d %H:%M:%f', End_time) AS End_time,
                                 Received_bytes,
                                 Total_bytes,
                                 File, Label, Comment
                             FROM 
                                 firefox_downloads;";
                }
                else
                {
                    string offset = utcOffset > 0 ? $"+{utcOffset} hours" : $"{utcOffset} hours";

                    sqlquery = $@"SELECT 
                                 id,
                                 Browser,
                                 Current_path,
                                 STRFTIME('%Y-%m-%d %H:%M:%f', End_time, '{offset}') AS End_time,
                                 Received_bytes,
                                 Total_bytes,
                                 File, Label, Comment
                             FROM 
                                 chrome_downloads
                             UNION ALL
                             SELECT 
                                 id,
                                 Browser,
                                 Current_path,
                                 STRFTIME('%Y-%m-%d %H:%M:%f', End_time, '{offset}') AS End_time,
                                 Received_bytes,
                                 Total_bytes,
                                 File, Label, Comment
                             FROM 
                                 firefox_downloads;";
                }



                //MyTools.LogToConsole(Console, sqlquery);
                Tools.ShowQueryOnDataGridView(sfDataGrid1, Helpers.chromeViewerConnectionString, sqlquery, labelItemCount);
                labelStatus.Text = "All downloads from all browsers";
            };

            lblAllDownloadsButton.MouseHover += (sender, e) => { lblAllDownloadsButton.BackColor = Color.LightBlue; };
            lblAllDownloadsButton.MouseLeave += (sender, e) => { lblAllDownloadsButton.BackColor = Color.White; };

            flwMain.Controls.Add(lblAllDownloadsButton);

            foreach (var entry in Helpers.browsersWithDownloads.OrderBy(b => b.Key))
            {
                string navegador = entry.Key;
                int count = entry.Value;
                Image icono = null;

                switch (navegador)
                {
                    case "Chrome":
                        icono = Resource1.Chrome.ToBitmap();
                        break;
                    case "Brave":
                        icono = Resource1.Brave.ToBitmap();
                        break;
                    case "Edge":
                        icono = Resource1.Edge.ToBitmap();
                        break;
                    case "Opera":
                        icono = Resource1.Opera.ToBitmap();
                        break;
                    case "Yandex":
                        icono = Resource1.Yandex.ToBitmap();
                        break;
                    case "Vivaldi":
                        icono = Resource1.Vivaldi.ToBitmap();
                        break;
                    case "Firefox":
                        icono = Resource1.Firefox.ToBitmap();
                        break;
                    default:
                        icono = Resource1.Unknown.ToBitmap();
                        break;
                }

                Label lblNavegadorDownloads = new Label()
                {
                    BackColor = Color.SteelBlue,
                    Font = fm,
                    ForeColor = Color.White,
                    Height = 48,
                    Image = icono,
                    ImageAlign = ContentAlignment.MiddleLeft,
                    Margin = new Padding(3, 1, 3, 1),
                    Padding = new Padding(12, 3, 0, 3),
                    Text = $"{navegador} {count} hits",
                    TextAlign = ContentAlignment.MiddleCenter,
                    Width = flwMain.Width
                };

                // Guarda la referencia de la etiqueta
                Helpers.downloadsLabels[navegador] = lblNavegadorDownloads;

                lblNavegadorDownloads.MouseHover += (sender, e) => { lblNavegadorDownloads.BackColor = Color.LightBlue; };
                lblNavegadorDownloads.MouseLeave += (sender, e) => { lblNavegadorDownloads.BackColor = Color.SteelBlue; };

                lblNavegadorDownloads.Click += (sender, e) =>
                {
                    int utcOffset = Helpers.utcOffset;
                    string sqlquery;
                    string searchCondition;
                    //Aqui agregar soporte regexp y rango tiempo

                    if (Helpers.searchTermExists)
                    {
                        // Determinar la condición de búsqueda (LIKE o REGEXP)
                        searchCondition = Helpers.searchTermRegExp
                            ? "Current_path REGEXP @searchTerm"
                            : "Current_path LIKE @searchTerm";

                        if (navegador == "Firefox")
                        {
                            if (utcOffset == 0)
                            {
                                sqlquery = $@"SELECT 
                                            id,
                                            Browser,
                                            Download_id,
                                            Current_path,
                                            STRFTIME('%Y-%m-%d %H:%M:%f', End_time) AS End_time,
                                            STRFTIME('%Y-%m-%d %H:%M:%f', Last_visit_time) AS Last_visit_time,
                                            Received_bytes,
                                            Total_bytes,
                                            Source_url,
                                            Title,
                                            State,
                                            File, Label, Comment
                                        FROM 
                                            firefox_downloads
                                        WHERE Browser = 'Firefox' AND {searchCondition} {Helpers.sqlDownloadtimecondition};";
                            }
                            else
                            {
                                string offset = utcOffset > 0 ? $"+{utcOffset} hours" : $"{utcOffset} hours";

                                sqlquery = $@"SELECT 
                                            id,
                                            Browser,
                                            Download_id,
                                            Current_path,
                                            STRFTIME('%Y-%m-%d %H:%M:%f', End_time, '{offset}') AS End_time,
                                            STRFTIME('%Y-%m-%d %H:%M:%f', Last_visit_time, '{offset}') AS Last_visit_time,
                                            Received_bytes,
                                            Total_bytes,
                                            Source_url,
                                            Title,
                                            State,
                                            File, Label, Comment
                                        FROM 
                                            firefox_downloads
                                        WHERE Browser = 'Firefox' AND {searchCondition} {Helpers.sqlDownloadtimecondition};";
                            }
                        }
                        else
                        {
                            if (utcOffset == 0)
                            {
                                sqlquery = $@"SELECT 
                                            id,
                                            Browser,
                                            Current_path,
                                            Target_path,
                                            STRFTIME('%Y-%m-%d %H:%M:%f', Start_time) AS Start_time,
                                            STRFTIME('%Y-%m-%d %H:%M:%f', End_time) AS End_time,
                                            Received_bytes,
                                            Total_bytes,
                                            State,
                                            Opened,
                                            Referrer,
                                            Site_url,
                                            Tab_url,
                                            Mime_type,
                                            File, Label, Comment
                                        FROM 
                                            chrome_downloads
                                        WHERE Browser = '{navegador}' AND {searchCondition} {Helpers.sqlDownloadtimecondition};";
                            }
                            else
                            {
                                string offset = utcOffset > 0 ? $"+{utcOffset} hours" : $"{utcOffset} hours";

                                sqlquery = $@"SELECT 
                                            id,
                                            Browser,
                                            Current_path,
                                            Target_path,
                                            STRFTIME('%Y-%m-%d %H:%M:%f', Start_time, '{offset}') AS Start_time,
                                            STRFTIME('%Y-%m-%d %H:%M:%f', End_time, '{offset}') AS End_time,
                                            Received_bytes,
                                            Total_bytes,
                                            State,
                                            Opened,
                                            Referrer,
                                            Site_url,
                                            Tab_url,
                                            Mime_type,
                                            File, Label, Comment
                                        FROM 
                                            chrome_downloads
                                        WHERE Browser = '{navegador}' AND {searchCondition};";
                            }
                        }
                    }
                    else
                    {   //Prueba condicion tiempo 

                        //if (checkBox_enableTime.Checked)
                        if (Helpers.searchTimeCondition)
                        {
                            Helpers.sqlDownloadtimecondition = $" AND (End_time >= '{Helpers.sd}' AND End_time <= '{Helpers.ed}')";

                        }

                        if (navegador == "Firefox")
                        {
                            if (utcOffset == 0)
                            {
                                sqlquery = $@"SELECT 
                                            id,
                                            Browser,
                                            Download_id,
                                            Current_path,
                                            STRFTIME('%Y-%m-%d %H:%M:%f', End_time) AS End_time,
                                            STRFTIME('%Y-%m-%d %H:%M:%f', Last_visit_time) AS Last_visit_time,
                                            Received_bytes,
                                            Total_bytes,
                                            Source_url,
                                            Title,
                                            State,
                                            File, Label, Comment
                                        FROM 
                                            firefox_downloads
                                        WHERE Browser = 'Firefox' {Helpers.sqlDownloadtimecondition};";
                            }
                            else
                            {
                                string offset = utcOffset > 0 ? $"+{utcOffset} hours" : $"{utcOffset} hours";

                                sqlquery = $@"SELECT 
                                            id,
                                            Browser,
                                            Download_id,
                                            Current_path,
                                            STRFTIME('%Y-%m-%d %H:%M:%f', End_time, '{offset}') AS End_time,
                                            STRFTIME('%Y-%m-%d %H:%M:%f', Last_visit_time, '{offset}') AS Last_visit_time,
                                            Received_bytes,
                                            Total_bytes,
                                            Source_url,
                                            Title,
                                            State,
                                            File, Label, Comment
                                        FROM 
                                            firefox_downloads
                                        WHERE Browser = 'Firefox' {Helpers.sqlDownloadtimecondition};";
                            }
                        }
                        else
                        {
                            if (utcOffset == 0)
                            {
                                sqlquery = $@"SELECT 
                                            id,
                                            Browser,
                                            Current_path,
                                            Target_path,
                                            STRFTIME('%Y-%m-%d %H:%M:%f', Start_time) AS Start_time,
                                            STRFTIME('%Y-%m-%d %H:%M:%f', End_time) AS End_time,
                                            Received_bytes,
                                            Total_bytes,
                                            State,
                                            Opened,
                                            Referrer,
                                            Site_url,
                                            Tab_url,
                                            Mime_type,
                                            File, Label, Comment
                                        FROM 
                                            chrome_downloads
                                        WHERE Browser = '{navegador}' {Helpers.sqlDownloadtimecondition};";
                            }
                            else
                            {
                                string offset = utcOffset > 0 ? $"+{utcOffset} hours" : $"{utcOffset} hours";

                                sqlquery = $@"SELECT 
                                            id,
                                            Browser,
                                            Current_path,
                                            Target_path,
                                            STRFTIME('%Y-%m-%d %H:%M:%f', Start_time, '{offset}') AS Start_time,
                                            STRFTIME('%Y-%m-%d %H:%M:%f', End_time, '{offset}') AS End_time,
                                            Received_bytes,
                                            Total_bytes,
                                            State,
                                            Opened,
                                            Referrer,
                                            Site_url,
                                            Tab_url,
                                            Mime_type,
                                            File, Label, Comment
                                        FROM 
                                            chrome_downloads
                                        WHERE Browser = '{navegador}' {Helpers.sqlDownloadtimecondition};";
                            }
                        }
                    }





                    //MyTools.LogToConsole(Console, sqlquery);
                    Tools.ShowQueryOnDataGridView(sfDataGrid1, Helpers.chromeViewerConnectionString, sqlquery, labelItemCount);
                    labelStatus.Text = $"Downloads from {navegador}";


                };

                flwMain.Controls.Add(lblNavegadorDownloads);
            }
        }





        private void AddBookmarkLabels(FlowLayoutPanel flwMain, Dictionary<string, int> browsersWithBookmarks)
        {
            Font fm = new Font(Font.FontFamily, 11);
            Font fs = new Font(Font.FontFamily, 10);

            // Crear el botón "All Bookmarks"
            Label lblAllBookmarksButton = new Label()
            {
                BackColor = Color.White,
                Font = new Font(fm, FontStyle.Bold),
                ForeColor = Color.Black,
                Height = 48,
                Margin = new Padding(3, 1, 3, 1),
                Padding = new Padding(12, 3, 0, 3),
                Text = "All Bookmarks",
                TextAlign = ContentAlignment.MiddleCenter,
                Width = flwMain.Width
            };

            lblAllBookmarksButton.Click += (sender, e) =>
            {
                int utcOffset = Helpers.utcOffset;
                string sqlquery;

                if (utcOffset == 0)
                {
                    // Consulta para todos los bookmarks sin ajuste de tiempo
                    sqlquery = @"
                                SELECT 
                                    id,
                                    Browser,
                                    Type,
                                    Title,
                                    URL,
                                    STRFTIME('%Y-%m-%d %H:%M:%f', DateAdded) AS DateAdded,
                                    STRFTIME('%Y-%m-%d %H:%M:%f', LastModified) AS LastModified,
                                    Parent_name,
                                    File, Label, Comment
                                FROM 
                                    bookmarks_Firefox
                                UNION ALL
                                SELECT 
                                    id,
                                    Browser,
                                    Type,
                                    Title,
                                    URL,
                                    STRFTIME('%Y-%m-%d %H:%M:%f', DateAdded) AS DateAdded,
                                    STRFTIME('%Y-%m-%d %H:%M:%f', LastModified) AS LastModified,
                                    Parent_name,
                                    File, Label, Comment
                                FROM 
                                    bookmarks_Chrome;";
                }
                else
                {
                    string offset = utcOffset > 0 ? $"+{utcOffset} hours" : $"{utcOffset} hours";

                    sqlquery = $@"
                                SELECT 
                                    id,
                                    Browser,
                                    Type,
                                    Title,
                                    URL,
                                    STRFTIME('%Y-%m-%d %H:%M:%f', DateAdded, '{offset}') AS DateAdded,
                                    STRFTIME('%Y-%m-%d %H:%M:%f', LastModified, '{offset}') AS LastModified,
                                    Parent_name,
                                    File, Label, Comment
                                FROM 
                                    bookmarks_Firefox
                                UNION ALL
                                SELECT 
                                    id,
                                    Browser,
                                    Type,
                                    Title,
                                    URL,
                                    STRFTIME('%Y-%m-%d %H:%M:%f', DateAdded, '{offset}') AS DateAdded,
                                    STRFTIME('%Y-%m-%d %H:%M:%f', LastModified, '{offset}') AS LastModified,
                                    Parent_name,
                                    File, Label, Comment
                                FROM 
                                    bookmarks_Chrome;";
                }



                Tools.ShowQueryOnDataGridView(sfDataGrid1, Helpers.chromeViewerConnectionString, sqlquery, labelItemCount);
                labelStatus.Text = "All bookmarks from all browsers";

            };

            lblAllBookmarksButton.MouseHover += (sender, e) => { lblAllBookmarksButton.BackColor = Color.LightBlue; };
            lblAllBookmarksButton.MouseLeave += (sender, e) => { lblAllBookmarksButton.BackColor = Color.White; };

            flwMain.Controls.Add(lblAllBookmarksButton);

            foreach (var entry in browsersWithBookmarks.OrderBy(b => b.Key))
            {
                string navegador = entry.Key;
                int count = entry.Value;
                Image icono = null;

                switch (navegador)
                {
                    case "Chrome":
                        icono = Resource1.Chrome.ToBitmap();
                        break;
                    case "Brave":
                        icono = Resource1.Brave.ToBitmap();
                        break;
                    case "Edge":
                        icono = Resource1.Edge.ToBitmap();
                        break;
                    case "Opera":
                        icono = Resource1.Opera.ToBitmap();
                        break;
                    case "Yandex":
                        icono = Resource1.Yandex.ToBitmap();
                        break;
                    case "Vivaldi":
                        icono = Resource1.Vivaldi.ToBitmap();
                        break;
                    case "Firefox":
                        icono = Resource1.Firefox.ToBitmap();
                        break;
                    default:
                        icono = Resource1.Unknown.ToBitmap();
                        break;
                }

                Label lblNavegadorBookmarks = new Label()
                {
                    BackColor = Color.SteelBlue,
                    Font = fm,
                    ForeColor = Color.White,
                    Height = 48,
                    Image = icono,
                    ImageAlign = ContentAlignment.MiddleLeft,
                    Margin = new Padding(3, 1, 3, 1),
                    Padding = new Padding(12, 3, 0, 3),
                    Text = $"{navegador} {count} hits",
                    TextAlign = ContentAlignment.MiddleCenter,
                    Width = flwMain.Width
                };

                Helpers.bookmarksLabels[navegador] = lblNavegadorBookmarks;

                lblNavegadorBookmarks.MouseHover += (sender, e) => { lblNavegadorBookmarks.BackColor = Color.LightBlue; };
                lblNavegadorBookmarks.MouseLeave += (sender, e) => { lblNavegadorBookmarks.BackColor = Color.SteelBlue; };

                lblNavegadorBookmarks.Click += (sender, e) =>
                {
                    int utcOffset = Helpers.utcOffset;
                    string sqlquery;

                    if (Helpers.searchTermExists)
                    {
                        // Determinar la condición de búsqueda (LIKE o REGEXP)
                        string searchCondition = Helpers.searchTermRegExp
                            ? "(Title REGEXP @searchTerm OR URL REGEXP @searchTerm OR Parent_name REGEXP @searchTerm)"
                            : "(Title LIKE @searchTerm OR URL LIKE @searchTerm OR Parent_name LIKE @searchTerm)";

                        if (navegador == "Firefox")
                        {
                            sqlquery = utcOffset == 0
                                ? $@"SELECT 
                                    id, Browser, Type, Title, URL, 
                                    STRFTIME('%Y-%m-%d %H:%M:%f', DateAdded) AS DateAdded,
                                    STRFTIME('%Y-%m-%d %H:%M:%f', LastModified) AS LastModified,
                                    Parent_name, File, Label, Comment
                                    FROM bookmarks_Firefox WHERE Browser = 'Firefox' AND {searchCondition} {Helpers.sqlBookmarkstimecondition};"
                                                    : $@"SELECT 
                                    id, Browser, Type, Title, URL, 
                                    STRFTIME('%Y-%m-%d %H:%M:%f', DateAdded, '{utcOffset} hours') AS DateAdded,
                                    STRFTIME('%Y-%m-%d %H:%M:%f', LastModified, '{utcOffset} hours') AS LastModified,
                                    Parent_name, File, Label, Comment
                                    FROM bookmarks_Firefox WHERE Browser = 'Firefox' AND {searchCondition} {Helpers.sqlBookmarkstimecondition};";
                        }
                        else
                        {
                            sqlquery = utcOffset == 0
                                ? $@"SELECT 
                                    id, Browser, Type, Title, URL, 
                                    STRFTIME('%Y-%m-%d %H:%M:%f', DateAdded) AS DateAdded,
                                    STRFTIME('%Y-%m-%d %H:%M:%f', LastModified) AS LastModified,
                                    Parent_name, File, Label, Comment
                                    FROM bookmarks_Chrome WHERE Browser = '{navegador}' AND {searchCondition} {Helpers.sqlBookmarkstimecondition};"
                                                    : $@"SELECT 
                                    id, Browser, Type, Title, URL, 
                                    STRFTIME('%Y-%m-%d %H:%M:%f', DateAdded, '{utcOffset} hours') AS DateAdded,
                                    STRFTIME('%Y-%m-%d %H:%M:%f', LastModified, '{utcOffset} hours') AS LastModified,
                                    Parent_name, File, Label, Comment
                                    FROM bookmarks_Chrome WHERE Browser = '{navegador}' AND {searchCondition} {Helpers.sqlBookmarkstimecondition};";
                        }
                    }
                    else
                    {
                        //if (checkBox_enableTime.Checked)
                        if (Helpers.searchTimeCondition)
                        {
                            Helpers.sqlBookmarkstimecondition = $" AND ((DateAdded >= '{Helpers.sd}' AND DateAdded <= '{Helpers.ed}') OR (LastModified >= '{Helpers.sd}' AND LastModified <= '{Helpers.ed}'))";
                        }
                        if (navegador == "Firefox")
                        {
                            sqlquery = utcOffset == 0
                                ? $@"SELECT 
                                    id, Browser, Type, Title, URL, 
                                    STRFTIME('%Y-%m-%d %H:%M:%f', DateAdded) AS DateAdded,
                                    STRFTIME('%Y-%m-%d %H:%M:%f', LastModified) AS LastModified,
                                    Parent_name, File, Label, Comment
                                FROM bookmarks_Firefox WHERE Browser = 'Firefox' {Helpers.sqlBookmarkstimecondition};"
                                                            : $@"SELECT 
                                    id, Browser, Type, Title, URL, 
                                    STRFTIME('%Y-%m-%d %H:%M:%f', DateAdded, '{utcOffset} hours') AS DateAdded,
                                    STRFTIME('%Y-%m-%d %H:%M:%f', LastModified, '{utcOffset} hours') AS LastModified,
                                    Parent_name, File, Label, Comment
                                FROM bookmarks_Firefox WHERE Browser = 'Firefox' {Helpers.sqlBookmarkstimecondition};";
                        }
                        else
                        {
                            sqlquery = utcOffset == 0
                                ? $@"SELECT 
                                    id, Browser, Type, Title, URL, 
                                    STRFTIME('%Y-%m-%d %H:%M:%f', DateAdded) AS DateAdded,
                                    STRFTIME('%Y-%m-%d %H:%M:%f', LastModified) AS LastModified,
                                    Parent_name, File, Label, Comment
                                FROM bookmarks_Chrome WHERE Browser = '{navegador}' {Helpers.sqlBookmarkstimecondition};"
                                                            : $@"SELECT 
                                    id, Browser, Type, Title, URL, 
                                    STRFTIME('%Y-%m-%d %H:%M:%f', DateAdded, '{utcOffset} hours') AS DateAdded,
                                    STRFTIME('%Y-%m-%d %H:%M:%f', LastModified, '{utcOffset} hours') AS LastModified,
                                    Parent_name, File, Label, Comment
                                FROM bookmarks_Chrome WHERE Browser = '{navegador}' {Helpers.sqlBookmarkstimecondition};";
                        }
                    }






                    Tools.ShowQueryOnDataGridView(sfDataGrid1, Helpers.chromeViewerConnectionString, sqlquery, labelItemCount);
                    labelStatus.Text = $"Bookmarks from {navegador}";

                };

                flwMain.Controls.Add(lblNavegadorBookmarks);
            }
        }





        //Prueba con autofill_data

        private void AddAutofillLabels(FlowLayoutPanel flwMain, Dictionary<string, int> browsersWithAutofill)
        {

            Font fm = new Font(Font.FontFamily, 11);
            Font fs = new Font(Font.FontFamily, 10);

            // Crear el botón "All Autofill"
            Label lblAllAutofillButton = new Label()
            {
                BackColor = Color.White,
                Font = new Font(fm, FontStyle.Bold),
                ForeColor = Color.Black,
                Height = 48,
                Margin = new Padding(3, 1, 3, 1),
                Padding = new Padding(12, 3, 0, 3),
                Text = "All Autofill Data",
                TextAlign = ContentAlignment.MiddleCenter,
                Width = flwMain.Width
            };

            lblAllAutofillButton.Click += (sender, e) =>
            {
                int utcOffset = Helpers.utcOffset;
                string sqlquery;

                if (utcOffset == 0)
                {
                    // Consulta para todos los datos de autofill sin ajuste de tiempo
                    sqlquery = @"
                                SELECT 
                                    id,
                                    Browser,
                                    FieldName,
                                    Value,
                                    TimesUsed,
                                    STRFTIME('%Y-%m-%d %H:%M:%f', FirstUsed) AS FirstUsed,
                                    STRFTIME('%Y-%m-%d %H:%M:%f', LastUsed) AS LastUsed,
                                    File, Label, Comment
                                FROM 
                                    autofill_data;";
                }
                else
                {
                    string offset = utcOffset > 0 ? $"+{utcOffset} hours" : $"{utcOffset} hours";

                    // Consulta para todos los datos de autofill con ajuste de tiempo sin perder milésimas
                    sqlquery = $@"
                                SELECT 
                                    id,
                                    Browser,
                                    FieldName,
                                    Value,
                                    TimesUsed,
                                    STRFTIME('%Y-%m-%d %H:%M:%f', FirstUsed, '{offset}') AS FirstUsed,
                                    STRFTIME('%Y-%m-%d %H:%M:%f', LastUsed, '{offset}') AS LastUsed,
                                    File, Label, Comment
                                FROM 
                                    autofill_data;";
                }




                Tools.ShowQueryOnDataGridView(sfDataGrid1, Helpers.chromeViewerConnectionString, sqlquery, labelItemCount);
                labelStatus.Text = "All autofill data from all browsers";
            };

            lblAllAutofillButton.MouseHover += (sender, e) => { lblAllAutofillButton.BackColor = Color.LightBlue; };
            lblAllAutofillButton.MouseLeave += (sender, e) => { lblAllAutofillButton.BackColor = Color.White; };

            flwMain.Controls.Add(lblAllAutofillButton);

            foreach (var entry in Helpers.browsersWithAutofill.OrderBy(b => b.Key))
            {
                string navegador = entry.Key;
                int count = entry.Value;

                Image icono = null;
                switch (navegador)
                {
                    case "Chrome":
                        icono = Resource1.Chrome.ToBitmap();
                        break;
                    case "Brave":
                        icono = Resource1.Brave.ToBitmap();
                        break;
                    case "Edge":
                        icono = Resource1.Edge.ToBitmap();
                        break;
                    case "Opera":
                        icono = Resource1.Opera.ToBitmap();
                        break;
                    case "Yandex":
                        icono = Resource1.Yandex.ToBitmap();
                        break;
                    case "Vivaldi":
                        icono = Resource1.Vivaldi.ToBitmap();
                        break;
                    case "Firefox":
                        icono = Resource1.Firefox.ToBitmap();
                        break;
                    default:
                        icono = Resource1.Unknown.ToBitmap();
                        break;
                }

                Label lblNavegadorAutofill = new Label()
                {
                    BackColor = Color.SteelBlue,
                    Font = fm,
                    ForeColor = Color.White,
                    Height = 48,
                    Image = icono,
                    ImageAlign = ContentAlignment.MiddleLeft,
                    Margin = new Padding(3, 1, 3, 1),
                    Padding = new Padding(12, 3, 0, 3),
                    Text = $"{navegador} {count} hits",
                    TextAlign = ContentAlignment.MiddleCenter,
                    Width = flwMain.Width
                };

                lblNavegadorAutofill.MouseHover += (sender, e) => { lblNavegadorAutofill.BackColor = Color.LightBlue; };
                lblNavegadorAutofill.MouseLeave += (sender, e) => { lblNavegadorAutofill.BackColor = Color.SteelBlue; };

                lblNavegadorAutofill.Click += (sender, e) =>
                {
                    int utcOffset = Helpers.utcOffset;
                    string sqlquery;

                    if (Helpers.searchTermExists)
                    {
                        string searchCondition = Helpers.searchTermRegExp
                                        ? "(FieldName REGEXP @searchTerm OR Value REGEXP @searchTerm)"
                                        : "(FieldName LIKE @searchTerm OR Value LIKE @searchTerm)";


                        if (utcOffset == 0)
                        {
                            // Consulta sin ajuste de tiempo
                            sqlquery = $@"SELECT id,
                                                Browser,
                                                FieldName,
                                                Value,
                                                TimesUsed,
                                                STRFTIME('%Y-%m-%d %H:%M:%f', FirstUsed) AS FirstUsed,
                                                STRFTIME('%Y-%m-%d %H:%M:%f', LastUsed) AS LastUsed,
                                                File, Label, Comment
                                            FROM 
                                                autofill_data
                                            WHERE Browser = '{navegador}' AND {searchCondition} {Helpers.sqlAutofilltimecondition};";
                        }
                        else
                        {
                            string offset = utcOffset > 0 ? $"+{utcOffset} hours" : $"{utcOffset} hours";

                            // Consulta con ajuste de tiempo manteniendo las milésimas
                            sqlquery = $@"SELECT id,
                                                Browser,
                                                FieldName,
                                                Value,
                                                TimesUsed,
                                                STRFTIME('%Y-%m-%d %H:%M:%f', FirstUsed, '{offset}') AS FirstUsed,
                                                STRFTIME('%Y-%m-%d %H:%M:%f', LastUsed, '{offset}') AS LastUsed,
                                                File, Label, Comment
                                            FROM 
                                                autofill_data
                                            WHERE Browser = '{navegador}' AND {searchCondition} {Helpers.sqlAutofilltimecondition};";
                        }

                    }
                    else
                    {
                        if (utcOffset == 0)
                        {
                            if (Helpers.searchTimeCondition)
                            {
                                Helpers.sqlAutofilltimecondition = $" AND ((FirstUsed >= '{Helpers.sd}' AND FirstUsed <= '{Helpers.ed}') OR (LastUsed >= '{Helpers.sd}' AND LastUsed <= '{Helpers.ed}'))";
                            }
                            // Consulta sin ajuste de tiempo
                            sqlquery = $@"SELECT id,
                                                Browser,
                                                FieldName,
                                                Value,
                                                TimesUsed,
                                                STRFTIME('%Y-%m-%d %H:%M:%f', FirstUsed) AS FirstUsed,
                                                STRFTIME('%Y-%m-%d %H:%M:%f', LastUsed) AS LastUsed,
                                                File, Label, Comment
                                            FROM 
                                                autofill_data
                                            WHERE Browser = '{navegador}' {Helpers.sqlAutofilltimecondition};";
                        }
                        else
                        {
                            string offset = utcOffset > 0 ? $"+{utcOffset} hours" : $"{utcOffset} hours";

                            // Consulta con ajuste de tiempo manteniendo las milésimas
                            sqlquery = $@"SELECT id,
                                                Browser,
                                                FieldName,
                                                Value,
                                                TimesUsed,
                                                STRFTIME('%Y-%m-%d %H:%M:%f', FirstUsed, '{offset}') AS FirstUsed,
                                                STRFTIME('%Y-%m-%d %H:%M:%f', LastUsed, '{offset}') AS LastUsed,
                                                File, Label, Comment
                                            FROM 
                                                autofill_data
                                            WHERE Browser = '{navegador}' {Helpers.sqlAutofilltimecondition};";
                        }
                    }


                    Tools.ShowQueryOnDataGridView(sfDataGrid1, Helpers.chromeViewerConnectionString, sqlquery, labelItemCount);
                    labelStatus.Text = $"Autofill data from {navegador}";
                    //MyTools.LogToConsole(Console, sqlquery);
                };

                flwMain.Controls.Add(lblNavegadorAutofill);
            }
        }









        public void initAll()
        {

            Helpers.historyConnectionString = "";

            Helpers.chromeViewerConnectionString = "";


            Helpers.utcOffset = 0;

            Helpers.db_name = "";
            Helpers.searchTerm = "";
            richTextBox1.Text = "";
            search_textBox.Text = "";
            Helpers.searchTermExists = false;
            Helpers.searchTermRegExp = false;
            Helpers.searchTimeCondition = false;
            this.sfDataGrid1.SearchController.ClearSearch();
            groupBox_customSearch.BackColor = SystemColors.Control;
            checkBox_RegExp.Checked = false;
            checkBox_enableTime.Checked = false;
            Helpers.sqltimecondition = "";
            Helpers.sqlDownloadtimecondition = "";
            Helpers.sqlBookmarkstimecondition = "";
            Helpers.sqlAutofilltimecondition = "";

            Helpers.browserUrls.Clear();          // Vacía el diccionario
            Helpers.browsersWithDownloads.Clear(); // Vacía el diccionario
            Helpers.browsersWithBookmarks.Clear(); // Vacía la lista
            Helpers.browsersWithAutofill.Clear();  // Vacía la lista
            sfDataGrid1.DataSource = null;
            AcordeonMenu(Helpers.browserUrls, Helpers.browsersWithDownloads, Helpers.browsersWithBookmarks, Helpers.browsersWithAutofill);

        }



        private void searchBtn_Click_1(object sender, EventArgs e)
        {


            Helpers.browserUrls.Clear();
            Helpers.browsersWithDownloads.Clear();
            Helpers.browsersWithBookmarks.Clear();
            Helpers.browsersWithAutofill.Clear();


            Helpers.searchTerm = search_textBox.Text;
            Helpers.searchTermExists = !string.IsNullOrWhiteSpace(Helpers.searchTerm);
            Helpers.sqltimecondition = ""; // vacío por defecto
            Helpers.sqlDownloadtimecondition = "";
            Helpers.sqlBookmarkstimecondition = "";
            Helpers.sqlAutofilltimecondition = "";


            int utcOffset = Helpers.utcOffset;

            if (!Helpers.searchTermExists && !checkBox_enableTime.Checked)
            {
                return;
            }


            if (checkBox_enableTime.Checked)
            {
                if (Helpers.StartDate > Helpers.EndDate)
                {
                    MessageBox.Show("The start date must be less than the end date.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dateTimePicker_end.Value = DateTime.Now;
                    dateTimePicker_start.Value = DateTime.Now.AddYears(-20);
                    return;
                }
                else
                {
                    Helpers.searchTimeCondition = true;
                    Helpers.sd = AdjustToUtc(Helpers.StartDate, utcOffset);
                    Helpers.ed = AdjustToUtc(Helpers.EndDate, utcOffset);
                    if (Helpers.searchTermExists)
                    {
                        Helpers.sqltimecondition = $" AND (Visit_time >= '{Helpers.sd}' AND Visit_time <= '{Helpers.ed}')";
                        Helpers.sqlDownloadtimecondition = $" AND (End_time >= '{Helpers.sd}' AND End_time <= '{Helpers.ed}')";
                        Helpers.sqlBookmarkstimecondition = $" AND ((DateAdded >= '{Helpers.sd}' AND DateAdded <= '{Helpers.ed}') OR (LastModified >= '{Helpers.sd}' AND LastModified <= '{Helpers.ed}'))";
                        Helpers.sqlAutofilltimecondition = $" AND ((FirstUsed >= '{Helpers.sd}' AND FirstUsed <= '{Helpers.ed}') OR (LastUsed >= '{Helpers.sd}' AND LastUsed <= '{Helpers.ed}'))";
                    }
                    else
                    {
                        Helpers.sqltimecondition = $"(Visit_time >= '{Helpers.sd}' AND Visit_time <= '{Helpers.ed}')";
                        Helpers.sqlDownloadtimecondition = $"(End_time >= '{Helpers.sd}' AND End_time <= '{Helpers.ed}')";
                        Helpers.sqlBookmarkstimecondition = $"((DateAdded >= '{Helpers.sd}' AND DateAdded <= '{Helpers.ed}') OR (LastModified >= '{Helpers.sd}' AND LastModified <= '{Helpers.ed}'))";
                        Helpers.sqlAutofilltimecondition = $"((FirstUsed >= '{Helpers.sd}' AND FirstUsed <= '{Helpers.ed}') OR (LastUsed >= '{Helpers.sd}' AND LastUsed <= '{Helpers.ed}'))";
                    }

                }
            }

            if (checkBox_RegExp.Checked) Helpers.searchTermRegExp = true;
            else Helpers.searchTermRegExp = false;
            labelStatus.Text = $"Search Results: {Helpers.searchTerm}";
            if ((Helpers.searchTermRegExp) && (!IsValidRegex(Helpers.searchTerm = search_textBox.Text)))
            {
                MessageBox.Show($"Invalid RegExp", "Browser Reviewer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                search_textBox.Text = "";
                return;
            }


            //Azul claro para indicr visualmente que hay puesto un filtro
            groupBox_customSearch.BackColor = Color.FromArgb(230, 240, 255);


            //Cadena de consulta para traer todos los campos que contoenen el termino de busqueda en el hisorial

            string searchCondition = "";
            string searchConditionFirefox = "";

            if (Helpers.searchTermExists)
            {
                searchCondition = Helpers.searchTermRegExp
                    ? "(r.Url REGEXP @searchTerm OR r.Title REGEXP @searchTerm)"
                    : "(r.Url LIKE @searchTerm OR r.Title LIKE @searchTerm)";

                searchConditionFirefox = Helpers.searchTermRegExp
                    ? "(f.Url REGEXP @searchTerm OR f.Title REGEXP @searchTerm)"
                    : "(f.Url LIKE @searchTerm OR f.Title LIKE @searchTerm)";
            }

            // Construir expresiones STRFTIME según utcOffset
            string offsetString = utcOffset == 0 ? "" : $", '{(utcOffset > 0 ? "+" : "")}{utcOffset} hours'";
            string visitTimeExprR = $"STRFTIME('%Y-%m-%d %H:%M:%f', r.Visit_time{offsetString})";
            string visitTimeExprF = $"STRFTIME('%Y-%m-%d %H:%M:%f', f.Visit_time{offsetString})";

            // Query final
            string sqlquery = $@"
                                SELECT  
                                    r.id AS id,
                                    r.Browser AS Browser,
                                    r.Category AS Category,
                                    r.Potential_activity AS Potential_activity,
                                    r.Visit_id AS Visit_id,
                                    r.Url AS Url,
                                    r.Title AS Title,
                                    {visitTimeExprR} AS Visit_time,
                                    r.File AS File, Label, Comment
                                FROM 
                                    results r
                                WHERE 
                                    {searchCondition}
                                    {Helpers.sqltimecondition}
                                UNION ALL
                                SELECT 
                                    f.id AS id,
                                    f.Browser AS Browser,
                                    f.Category AS Category,
                                    f.Potential_activity AS Potential_activity,
                                    f.Visit_id AS Visit_id,
                                    f.Url AS Url,
                                    f.Title AS Title,
                                    {visitTimeExprF} AS Visit_time,
                                    f.File AS File, Label, Comment
                                FROM 
                                    firefox_results f
                                WHERE 
                                    {searchConditionFirefox}
                                    {Helpers.sqltimecondition};";




            //Hace la consulta y los muestra en la grid


            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(Helpers.chromeViewerConnectionString))
                {
                    connection.Open();
                    using (SQLiteCommand command = new SQLiteCommand(sqlquery, connection))
                    {
                        // Verificar si es una búsqueda con REGEXP o LIKE
                        if (Helpers.searchTermRegExp)
                        {
                            // REGEXP necesita el patrón sin modificaciones
                            command.Parameters.AddWithValue("@searchTerm", Helpers.searchTerm);
                        }
                        else
                        {
                            // LIKE necesita % para búsqueda parcial
                            command.Parameters.AddWithValue("@searchTerm", $"%{Helpers.searchTerm}%");
                        }

                        using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(command))
                        {
                            DataTable dataTable = new DataTable();
                            adapter.Fill(dataTable);
                            sfDataGrid1.DataSource = dataTable;

                            // Actualizar el número de elementos en el label
                            labelItemCount.Text = $"Items count: {sfDataGrid1.RowCount - 1}"; // Restamos 1 para no contar la fila de encabezado.

                            if (sfDataGrid1.View != null && sfDataGrid1.View.Records.Count > 0)
                            {
                                sfDataGrid1.SelectedIndex = 0; // Selecciona la primera fila
                                sfDataGrid1.MoveToCurrentCell(new RowColumnIndex(0, 0)); // Mueve el foco a la primera celda
                            }
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                MessageBox.Show($"Error connecting to database {ex.Message}", "Browser Reviewer", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }



            //Primera consulta para rellenar el dicciomario primer parametro del menu acorden para redibujarlo acorde con las busquedas.

            // Determinar la condición de búsqueda con LIKE o REGEXP
            string searchConditionHistoryMenu = "";
            if (Helpers.searchTermExists)
            {
                searchConditionHistoryMenu = Helpers.searchTermRegExp
                ? "(Url REGEXP @searchTerm OR Title REGEXP @searchTerm)"
                : "(Url LIKE @searchTerm OR Title LIKE @searchTerm)";
            }



            // Construir la consulta SQL
            string sqlqueryHistoryMenu = $@"
                                            SELECT Browser, Url FROM results
                                            WHERE {searchConditionHistoryMenu} {Helpers.sqltimecondition}
                                            UNION ALL
                                            SELECT Browser, Url FROM firefox_results
                                            WHERE {searchConditionHistoryMenu} {Helpers.sqltimecondition}";
            //MyTools.LogToConsole(Console, sqlqueryHistoryMenu);

            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(Helpers.chromeViewerConnectionString))
                {
                    connection.Open();

                    using (SQLiteCommand command = new SQLiteCommand(sqlqueryHistoryMenu, connection))
                    {
                        if (Helpers.searchTermExists)
                        {
                            // Determinar cómo pasar el parámetro según el tipo de búsqueda
                            if (Helpers.searchTermRegExp)
                            {
                                // REGEXP no necesita comodines
                                command.Parameters.AddWithValue("@searchTerm", Helpers.searchTerm);
                            }
                            else
                            {
                                // LIKE necesita %
                                command.Parameters.AddWithValue("@searchTerm", $"%{Helpers.searchTerm}%");
                            }
                        }
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

                // Actualizar la cantidad de coincidencias por navegador
                Helpers.historyHits = Helpers.browserUrls.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error connecting to database {ex.Message}", "Browser Reviewer", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }





            //Segunda consulta, rellenamos ahora el segundo parametro, una lista de navegadores en cuyos campos sea tipo chrome o Firefox contenga el termino de busqueda

            // Determinar la condición de búsqueda (LIKE o REGEXP)
            if (Helpers.searchTermExists)
            {
                searchCondition = Helpers.searchTermRegExp
                    ? "Current_path REGEXP @searchTerm"
                    : "Current_path LIKE @searchTerm";
            }
            // Construir las consultas SQL
            string sqlqueryChromeDwMenu = $@"SELECT Browser, COUNT(*) 
                    FROM chrome_downloads 
                    WHERE {searchCondition} {Helpers.sqlDownloadtimecondition}
                    GROUP BY Browser;";

            string sqlqueryFirefoxDwMenu = $@"SELECT Browser, COUNT(*) 
                    FROM firefox_downloads 
                    WHERE {searchCondition} {Helpers.sqlDownloadtimecondition}
                    GROUP BY Browser;";


            //MyTools.LogToConsole(Console,$"Select para Chrome Dw: {sqlqueryChromeDwMenu}");
            //MyTools.LogToConsole(Console, $"Select para Firefox Dw: {sqlqueryFirefoxDwMenu}");

            using (SQLiteConnection connection = new SQLiteConnection(Helpers.chromeViewerConnectionString))
            {
                connection.Open();

                // Verificar navegadores en chrome_downloads
                using (SQLiteCommand command = new SQLiteCommand(sqlqueryChromeDwMenu, connection))
                {
                    if (Helpers.searchTermExists)
                    {
                        // Pasar el parámetro de búsqueda correctamente
                        if (Helpers.searchTermRegExp)
                        {
                            command.Parameters.AddWithValue("@searchTerm", Helpers.searchTerm);
                        }
                        else
                        {
                            command.Parameters.AddWithValue("@searchTerm", $"%{Helpers.searchTerm}%");
                        }
                    }


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

                // Verificar navegadores en firefox_downloads
                using (SQLiteCommand command = new SQLiteCommand(sqlqueryFirefoxDwMenu, connection))
                {
                    // Pasar el parámetro de búsqueda correctamente
                    if (Helpers.searchTermRegExp)
                    {
                        command.Parameters.AddWithValue("@searchTerm", Helpers.searchTerm);
                    }
                    else
                    {
                        command.Parameters.AddWithValue("@searchTerm", $"%{Helpers.searchTerm}%");
                    }

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
            }

            //foreach (var kvp in Helpers.browsersWithDownloads)
            //{
            //    MyTools.LogToConsole(Console,$"Browser: {kvp.Key}, Descargas: {kvp.Value}");
            //}




            //Tercera consulta para 3er parametro

            if (Helpers.searchTermExists)
            {
                searchCondition = Helpers.searchTermRegExp
                    ? "(Title REGEXP @searchTerm OR URL REGEXP @searchTerm OR Parent_name REGEXP @searchTerm)"
                    : "(Title LIKE @searchTerm OR URL LIKE @searchTerm OR Parent_name LIKE @searchTerm)";
            }
            // Determinar la condición de búsqueda (LIKE o REGEXP)


            // Construir las consultas SQL
            string sqlqueryChromeBkMkMenu = $@"SELECT Browser, COUNT(*) 
                    FROM bookmarks_Chrome 
                    WHERE {searchCondition} {Helpers.sqlBookmarkstimecondition}
                    GROUP BY Browser;";

            string sqlqueryFirefoxBkMkMenu = $@"SELECT Browser, COUNT(*) 
                    FROM bookmarks_Firefox 
                    WHERE {searchCondition} {Helpers.sqlBookmarkstimecondition}
                    GROUP BY Browser;";

            using (SQLiteConnection connection = new SQLiteConnection(Helpers.chromeViewerConnectionString))
            {
                connection.Open();

                // Verificar navegadores en bookmarks_Chrome
                using (SQLiteCommand command = new SQLiteCommand(sqlqueryChromeBkMkMenu, connection))
                {
                    // Pasar el parámetro de búsqueda correctamente
                    if (Helpers.searchTermRegExp)
                    {
                        command.Parameters.AddWithValue("@searchTerm", Helpers.searchTerm); // REGEXP (sin %)
                    }
                    else
                    {
                        command.Parameters.AddWithValue("@searchTerm", $"%{Helpers.searchTerm}%"); // LIKE (con %)
                    }

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string browser = reader.GetString(0);
                            int count = reader.GetInt32(1);

                            if (Helpers.browsersWithBookmarks.ContainsKey(browser))
                            {
                                Helpers.browsersWithBookmarks[browser] += count;
                            }
                            else
                            {
                                Helpers.browsersWithBookmarks[browser] = count;
                            }
                        }
                    }
                }

                // Verificar navegadores en bookmarks_Firefox
                using (SQLiteCommand command = new SQLiteCommand(sqlqueryFirefoxBkMkMenu, connection))
                {
                    // Pasar el parámetro de búsqueda correctamente
                    if (Helpers.searchTermRegExp)
                    {
                        command.Parameters.AddWithValue("@searchTerm", Helpers.searchTerm); // REGEXP (sin %)
                    }
                    else
                    {
                        command.Parameters.AddWithValue("@searchTerm", $"%{Helpers.searchTerm}%"); // LIKE (con %)
                    }

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string browser = reader.GetString(0);
                            int count = reader.GetInt32(1);

                            if (Helpers.browsersWithBookmarks.ContainsKey(browser))
                            {
                                Helpers.browsersWithBookmarks[browser] += count;
                            }
                            else
                            {
                                Helpers.browsersWithBookmarks[browser] = count;
                            }
                        }
                    }
                }
            }





            //Cuarta consulta 4to parametro


            if (Helpers.searchTermExists)
            {
                searchCondition = Helpers.searchTermRegExp
                                ? "(FieldName REGEXP @searchTerm OR Value REGEXP @searchTerm)"
                                : "(FieldName LIKE @searchTerm OR Value LIKE @searchTerm)";
            }


            string sqlqueryBrowserAutoFillMenu = $@"
                                                    SELECT Browser, COUNT(*) as Count
                                                    FROM autofill_data
                                                    WHERE {searchCondition} {Helpers.sqlAutofilltimecondition}
                                                    GROUP BY Browser;
                                                ";

            using (SQLiteConnection connection = new SQLiteConnection(Helpers.chromeViewerConnectionString))
            {
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand(sqlqueryBrowserAutoFillMenu, connection))
                {
                    //command.Parameters.AddWithValue("@searchTerm", "%" + Helpers.searchTerm + "%");

                    if (Helpers.searchTermRegExp)
                    {
                        command.Parameters.AddWithValue("@searchTerm", Helpers.searchTerm); // REGEXP (sin %)
                    }
                    else
                    {
                        command.Parameters.AddWithValue("@searchTerm", $"%{Helpers.searchTerm}%"); // LIKE (con %)
                    }

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string browser = reader.GetString(0);
                            int count = reader.GetInt32(1);

                            Helpers.browsersWithAutofill[browser] = count;
                        }
                    }
                }
            }

            //MyTools.LogToConsole(Console, sqlqueryBrowserAutoFillMenu);

            if (!Helpers.searchTermRegExp)
            {
                this.sfDataGrid1.SearchController.Search(Helpers.searchTerm);
            }
            else
            {
                this.sfDataGrid1.SearchController.ClearSearch();
            }

            AcordeonMenu(Helpers.browserUrls, Helpers.browsersWithDownloads, Helpers.browsersWithBookmarks, Helpers.browsersWithAutofill);

        }


        private string AdjustToUtc(DateTime dateTime, int utcOffset)
        {
            // Invierte el offset y aplica el ajuste
            TimeSpan offset = TimeSpan.FromHours(utcOffset * -1);
            DateTime adjustedDateTime = dateTime.Add(offset);

            // Retorna la fecha ajustada en formato UTC
            return adjustedDateTime.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private bool IsValidRegex(string pattern)
        {
            try
            {
                Regex.Match("", pattern); // Solo intenta compilar la expresión
                return true; // Si no lanza excepción, la regex es válida
            }
            catch (ArgumentException)
            {
                return false; // Si hay error, la regex no es válida
            }
        }


        private string HighlightSearchTerms(string html, string searchTerm, bool isRegex)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return html;

            try
            {
                if (isRegex)
                {
                    return Regex.Replace(html, searchTerm, match =>
                        $"<span style=\"background-color: yellow\">{match.Value}</span>",
                        RegexOptions.IgnoreCase);
                }
                else
                {
                    return Regex.Replace(html,
                        Regex.Escape(searchTerm),
                        $"<span style=\"background-color: yellow\">{searchTerm}</span>",
                        RegexOptions.IgnoreCase);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al resaltar el término: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return html;
            }
        }



        private void highlight_Text()
        {
            if (Helpers.searchTermExists)
            {
                string text = Helpers.searchTerm;

                // Si se usa REGEXP, resaltar con expresiones regulares
                if (Helpers.searchTermRegExp)
                {
                    HighlightWithRegex(text, Color.Yellow);
                }
                else
                {
                    HighlightPlainText(text, Color.Yellow);
                }
            }
        }

        // Método para resaltar texto normal (LIKE)
        private void HighlightPlainText(string text, Color highlightColor)
        {
            int startIndex = 0;

            while (startIndex < richTextBox1.TextLength)
            {
                int wordStartIndex = richTextBox1.Find(text, startIndex, RichTextBoxFinds.None);
                if (wordStartIndex != -1)
                {
                    richTextBox1.SelectionStart = wordStartIndex;
                    richTextBox1.SelectionLength = text.Length;
                    richTextBox1.SelectionBackColor = highlightColor;
                    richTextBox1.Refresh();

                    startIndex = wordStartIndex + text.Length;
                }
                else
                {
                    break;
                }
            }
        }

        //Método para resaltar texto con REGEXP
        private void HighlightWithRegex(string pattern, Color highlightColor)
        {
            try
            {
                string content = richTextBox1.Text;

                // Encuentra todas las coincidencias de la expresión regular
                //MatchCollection matches = Regex.Matches(content, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                MatchCollection matches = Regex.Matches(content, pattern, RegexOptions.IgnoreCase);

                foreach (Match match in matches)
                {
                    richTextBox1.SelectionStart = match.Index;
                    richTextBox1.SelectionLength = match.Length;
                    richTextBox1.SelectionBackColor = highlightColor;
                    richTextBox1.Refresh();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Regular expression error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void button_Show_Selected_Click(object sender, EventArgs e)
        {


            string split = "*******************************************";

            foreach (var record in sfDataGrid1.SelectedItems)
            {
                var dataRowView = record as DataRowView;
                if (dataRowView != null)
                {
                    var columnasPresentes = dataRowView.DataView.Table.Columns
                                                .Cast<DataColumn>()
                                                .Select(c => c.ColumnName)
                                                .ToList();

                    string tablaDetectada = "Desconocida";

                    foreach (var kvp in Helpers.TablesAndFields)
                    {
                        var camposEsperados = kvp.Value.OrderBy(c => c).ToList();
                        var columnasOrdenadas = columnasPresentes.OrderBy(c => c).ToList();

                        if (camposEsperados.SequenceEqual(columnasOrdenadas))
                        {
                            tablaDetectada = kvp.Key;
                            break;
                        }
                    }

                    if (tablaDetectada == "AllWebHistory" || tablaDetectada == "AllDownloads" || tablaDetectada == "AllBookmarks")
                    {
                        var browser = dataRowView["Browser"]?.ToString()?.ToLowerInvariant();

                        if (tablaDetectada == "AllWebHistory")
                            tablaDetectada = browser == "firefox" ? "firefox_results" : "results";

                        else if (tablaDetectada == "AllDownloads")
                            tablaDetectada = browser == "firefox" ? "firefox_downloads" : "chrome_downloads";

                        else if (tablaDetectada == "AllBookmarks")
                            tablaDetectada = browser == "firefox" ? "bookmarks_Firefox" : "bookmarks_Chrome";
                    }

                    var idValor = dataRowView["id"];
                    //MyTools.LogToConsole(Console, $"Origen: {tablaDetectada}" + Environment.NewLine);
                    //MyTools.LogToConsole(Console, $"ID: {idValor}" + Environment.NewLine);
                    //MyTools.LogToConsole(Console, split + Environment.NewLine);

                }
            }




        }

        private void search_textBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void search_textBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                searchBtn.PerformClick();
                e.SuppressKeyPress = true;
            }
        }

        private void exitToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void clearsearchBtn_Click(object sender, EventArgs e)
        {
            search_textBox.Text = "";
            Helpers.searchTermExists = false;
            Helpers.searchTermRegExp = false;
            Helpers.searchTimeCondition = false;
            this.sfDataGrid1.SearchController.ClearSearch();
            labelStatus.Text = "All web history from all browsers";
            groupBox_customSearch.BackColor = SystemColors.Control;
            checkBox_RegExp.Checked = false;
            checkBox_enableTime.Checked = false;
            Helpers.sqltimecondition = "";
            Helpers.sqlDownloadtimecondition = "";
            Helpers.sqlBookmarkstimecondition = "";
            Helpers.sqlAutofilltimecondition = "";


            int utcOffset = Helpers.utcOffset;
            string sqlquery;



            if (utcOffset == 0)
            {
                // Consulta sin ajustes de tiempo, manteniendo las fracciones de segundo
                sqlquery = @"SELECT
                                r.id as id,
                                r.Browser AS Browser,
                                r.Category AS Category,
                                r.Potential_activity AS Potential_activity,
                                r.Visit_id AS Visit_id,
                                r.Url AS Url,
                                r.Title AS Title,
                                STRFTIME('%Y-%m-%d %H:%M:%f', r.Visit_time) AS Visit_time,
                                r.File AS File, Label, Comment
                            FROM 
                                results r
                            UNION ALL
                            SELECT 
                                f.id as id,
                                f.Browser AS Browser,
                                f.Category AS Category,
                                f.Potential_activity AS Potential_activity,
                                f.Visit_id AS Visit_id,
                                f.Url AS Url,
                                f.Title AS Title,
                                STRFTIME('%Y-%m-%d %H:%M:%f', f.Visit_time) AS Visit_time,
                                f.File AS File, Label, Comment
                            FROM 
                                firefox_results f;";
            }
            else
            {
                // Ajustar el tiempo en función de utcOffset
                string offset = utcOffset > 0 ? $"+{utcOffset} hours" : $"{utcOffset} hours";

                sqlquery = $@"SELECT 
                                r.id AS id,
                                r.Browser AS Browser,
                                r.Category AS Category,
                                r.Potential_activity AS Potential_activity,
                                r.Visit_id AS Visit_id,
                                r.Url AS Url,
                                r.Title AS Title,
                                STRFTIME('%Y-%m-%d %H:%M:%f', r.Visit_time, '{offset}') AS Visit_time,
                                r.File AS File, Label, Comment
                            FROM 
                                results r
                            UNION ALL
                            SELECT 
                                f.id AS id,
                                f.Browser AS Browser,
                                f.Category AS Category,
                                f.Potential_activity AS Potential_activity,
                                f.Visit_id AS Visit_id,
                                f.Url AS Url,
                                f.Title AS Title,
                                STRFTIME('%Y-%m-%d %H:%M:%f', f.Visit_time, '{offset}') AS Visit_time,
                                f.File AS File, Label, Comment
                            FROM 
                                firefox_results f;";
            }





            Helpers.browserUrls.Clear();          // Vacía el diccionario
            Helpers.browsersWithDownloads.Clear(); // Vacía el diccionario
            Helpers.browsersWithBookmarks.Clear(); // Vacía la lista
            Helpers.browsersWithAutofill.Clear();

            Helpers.browserUrls = Tools.FillDictionaryFromDatabase();

            Helpers.browsersWithDownloads = MyTools.GetBrowsersWithDownloads();

            Helpers.browsersWithBookmarks = MyTools.GetBrowsersWithBookmarks();

            Helpers.browsersWithAutofill = MyTools.GetBrowsersWithAutofill();
            AcordeonMenu(Helpers.browserUrls, Helpers.browsersWithDownloads, Helpers.browsersWithBookmarks, Helpers.browsersWithAutofill);
            Tools.ShowQueryOnDataGridView(sfDataGrid1, Helpers.chromeViewerConnectionString, sqlquery, labelItemCount);

        }

        private void OpenComments()
        {
            Form OpenForm = Application.OpenForms.OfType<Form_Comments>().FirstOrDefault();
            if (OpenForm != null)
            {
                OpenForm.BringToFront();  // Si se encuentra, tráelo al frente
            }
            else
            {
                Form_Comments Form_Comment = new Form_Comments();
                Form_Comment.FormClosed += (s, args) => AplicarComentarioAFilasSeleccionadas();
                Form_Comment.Show();
            }
        }

        private void OpenLabelManager()
        {

            Form OpenForm = Application.OpenForms.OfType<Form_LabelManager>().FirstOrDefault();

            if (OpenForm != null)
            {
                OpenForm.BringToFront();  // Si se encuentra, tráelo al frente
            }
            else
            {
                Form_LabelManager Form_Label = new Form_LabelManager();
                Form_Label.Show();
            }
        }

        private void button_LabelManager_Click(object sender, EventArgs e)
        {
            OpenLabelManager();
        }

        private void checkBox_enableTime_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_enableTime.Checked)
            {
                dateTimePicker_start.Enabled = true;
                dateTimePicker_end.Enabled = true;
                label_startDate.Enabled = true;
                label_endDate.Enabled = true;
            }
            else
            {
                dateTimePicker_start.Enabled = false;
                dateTimePicker_end.Enabled = false;
                label_startDate.Enabled = false;
                label_endDate.Enabled = false;
            }


        }

        private void dateTimePicker_end_ValueChanged(object sender, EventArgs e)
        {
            Helpers.EndDate = dateTimePicker_end.Value;

        }

        private void dateTimePicker_start_ValueChanged_1(object sender, EventArgs e)
        {
            Helpers.StartDate = dateTimePicker_start.Value;

        }

        private void button_exportPDF_Click(object sender, EventArgs e)
        {

            ExportGridAsDetailedPdf(sfDataGrid1);

        }



        private void ExportGridAsDetailedPdf(SfDataGrid dataGrid)
        {

            if (dataGrid.View.Records.Count == 0)
            {
                MessageBox.Show("There is no data to export.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "PDF Files (*.pdf)|*.pdf";
                saveFileDialog.Title = "Save Report";
                saveFileDialog.FileName = "Browser Reviewer Report.pdf";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        PdfDocument document = new PdfDocument();
                        //PdfPage dummyPage = document.Pages.Add(); // Necesaria para usar .Template
                        //float pageWidth = dummyPage.GetClientSize().Width;
                        float pageWidth = 595f; // Tamaño estándar A4 (8.27in * 72 dpi)


                        // Encabezado
                        PdfPageTemplateElement headerTemplate = new PdfPageTemplateElement(pageWidth, 40);
                        PdfFont headerFontTop = new PdfStandardFont(PdfFontFamily.Helvetica, 10, PdfFontStyle.Bold);
                        string headerText = $"Browser Reviewer     {Helpers.db_name}";
                        headerTemplate.Graphics.DrawLine(PdfPens.Gray, new PointF(0, 0), new PointF(pageWidth, 0));
                        headerTemplate.Graphics.DrawString(headerText, headerFontTop, PdfBrushes.DarkSlateGray, new PointF(10, 15));
                        headerTemplate.Graphics.DrawLine(PdfPens.Gray, new PointF(0, 35), new PointF(pageWidth, 35));
                        document.Template.Top = headerTemplate;

                        // Pie de página
                        PdfPageTemplateElement footerTemplate = new PdfPageTemplateElement(pageWidth, 40);
                        PdfFont footerFont = new PdfStandardFont(PdfFontFamily.Helvetica, 9);
                        string footerText = $"Generated using Browser Reviewer – {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                        footerTemplate.Graphics.DrawLine(PdfPens.Gray, new PointF(0, 0), new PointF(pageWidth, 0));
                        footerTemplate.Graphics.DrawString(footerText, footerFont, PdfBrushes.Gray, new PointF(10, 15));


                        document.Template.Bottom = footerTemplate;

                        // Fuentes para contenido
                        PdfFont labelFont = new PdfStandardFont(PdfFontFamily.Helvetica, 12, PdfFontStyle.Bold);
                        PdfFont valueFont = new PdfStandardFont(PdfFontFamily.Helvetica, 10);

                        // Una página por registro
                        foreach (var record in dataGrid.View.Records)
                        {
                            var row = record.Data as DataRowView;
                            if (row == null) continue;

                            PdfPage page = document.Pages.Add();
                            PdfGraphics graphics = page.Graphics;

                            float y = 20;

                            foreach (DataColumn col in row.DataView.Table.Columns)
                            {
                                string label = col.ColumnName;
                                string value = row[col.ColumnName]?.ToString() ?? "(sin datos)";

                                graphics.DrawString($"{label}:", labelFont, PdfBrushes.Black, new PointF(10, y));
                                y += 14;
                                graphics.DrawString(value, valueFont, PdfBrushes.DarkBlue, new PointF(20, y));
                                y += 25;
                            }



                        }

                        using (FileStream output = new FileStream(saveFileDialog.FileName, FileMode.Create))
                        {
                            document.Save(output);
                        }

                        document.Close(true);
                        MessageBox.Show("Export finished", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Export error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void autoLabel1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://www.internet-solutions.com.co/",
                UseShellExecute = true
            });
        }
    }
}
