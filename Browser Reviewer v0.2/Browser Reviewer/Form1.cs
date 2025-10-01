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
using System.Windows.Forms;
using Syncfusion.Windows.Forms.Tools;
using System.Text;
using System.Net;
using System.ComponentModel;
using Syncfusion.Pdf.Grid;






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
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjGyl/Vkd+XU9FcVRDX3xKf0x/TGpQb19xflBPallYVBYiSV9jS3tTf0VqWXZaeXRRQmJcUE91Xg==");
            InitializeComponent();
            InitializeNumericUpDown();



            sfDataGrid1.SelectionChanged += SfDataGrid1_SelectionChanged;
            numericUpDown1.ValueChanged += NumericUpDown1_ValueChanged;
            sfDataGrid1.FilterChanged += SfDataGrid_FilterChanged;
            sfDataGrid1.AutoGeneratingColumn += SfDataGrid_AutoGeneratingColumn;
            sfDataGrid1.QueryCellStyle += SfDataGrid1_QueryCellStyle;

            Tools = new MyTools();

            SQLiteFunction.RegisterFunction(typeof(MyRegEx));

           
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
            
            SetupRichTextBoxContextMenu();





            //Mejor version hasta el momento

            // Evita parpadeos mientras reubicas
            this.SuspendLayout();
           
            groupBox_customSearch.AutoSize = false;
            groupBox_customSearch.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            // Opcional: quitar padding interno del GroupBox
            groupBox_customSearch.Padding = new Padding(0);

           



            // --- TABLELAYOUT: 13 columnas, 2 filas, SIN padding ni margin ---
            var tableLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 13,
                RowCount = 2,
                Margin = new Padding(0),
                Padding = new Padding(0),      // <- sin espacio interno
                AutoSize = false,
                GrowStyle = TableLayoutPanelGrowStyle.FixedSize
            };

            // Columnas 1–4 = 20% total (5% c/u)
            for (int i = 0; i < 4; i++)
                tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 5f));

            // Columna 5 = 48%
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48f)); // índice 4

            // Columna 6 = 5%
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 5f));  // índice 5

            // Columnas 7–13 = 27% total (~3.857% c/u)
            for (int i = 6; i < 13; i++)
                tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 3.857f));

            // Filas 50/50
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));






            // Botones existentes: dock y sin márgenes
            button_SearchWebActivity.Dock = DockStyle.Fill;
            button_SearchWebActivity.Margin = new Padding(0);
            button_SearchWebActivity.AutoSize = false;

            button_LabelManager.Dock = DockStyle.Fill;
            button_LabelManager.Margin = new Padding(0);
            button_LabelManager.AutoSize = false;

            // Agregar ocupando 2 filas
            tableLayout.Controls.Add(button_SearchWebActivity, 0, 0);
            tableLayout.SetRowSpan(button_SearchWebActivity, 2);

            tableLayout.Controls.Add(button_LabelManager, 1, 0);
            tableLayout.SetRowSpan(button_LabelManager, 2);


            // Asegurar que el label se estire dentro de su celda
            label_UTC_Time.Dock = DockStyle.Fill;
            label_UTC_Time.TextAlign = ContentAlignment.MiddleLeft; // opcional

            // Agregar label en columna 3 (índice 2), fila 0
            tableLayout.Controls.Add(label_UTC_Time, 2, 0);

            // Que ocupe también la columna 4 (índice 3)
            tableLayout.SetColumnSpan(label_UTC_Time, 2);



            // Ajustes para que el numericUpDown se estire bien
            numericUpDown1.Dock = DockStyle.Fill;
            numericUpDown1.TextAlign = HorizontalAlignment.Center; // opcional, centra el valor

            // Agregar en columna 3 (índice 2), fila 1
            tableLayout.Controls.Add(numericUpDown1, 2, 1);

            // Que ocupe también la columna 4 (índice 3)
            //tableLayout.SetColumnSpan(numericUpDown1, 2);


            // Ajustes para que el label se vea bien
            labelStatus.Dock = DockStyle.Fill;
            labelStatus.TextAlign = ContentAlignment.MiddleLeft; // puedes usar MiddleCenter si prefieres centrado

            // Agregar en columna 5 (índice 4), fila 0
            tableLayout.Controls.Add(labelStatus, 4, 0);


            // Ajustes del label
            labelItemCount.Dock = DockStyle.Fill;
            labelItemCount.TextAlign = ContentAlignment.MiddleLeft; // o MiddleCenter según prefieras

            // Agregar en columna 5 (índice 4), fila 1
            tableLayout.Controls.Add(labelItemCount, 4, 1);

            // Ajustes del label
            label_startDate.Dock = DockStyle.Fill;
            label_startDate.TextAlign = ContentAlignment.MiddleLeft; // o MiddleCenter si quieres centrado

            // Agregar en columna 6 (índice 5), fila 0
            tableLayout.Controls.Add(label_startDate, 5, 0);

            // Ajustes del label
            label_endDate.Dock = DockStyle.Fill;
            label_endDate.TextAlign = ContentAlignment.MiddleLeft; // o MiddleCenter si prefieres

            // Agregar en columna 6 (índice 5), fila 1
            tableLayout.Controls.Add(label_endDate, 5, 1);


            // Ajustes del DateTimePicker
            dateTimePicker_start.Dock = DockStyle.Fill;
            dateTimePicker_start.Format = DateTimePickerFormat.Short; // o el formato que prefieras

            // Agregar en columna 7 (índice 6), fila 0
            tableLayout.Controls.Add(dateTimePicker_start, 6, 0);

            // Hacer que ocupe también la columna 8 (índice 7)
            tableLayout.SetColumnSpan(dateTimePicker_start, 2);


            // Ajustes del DateTimePicker
            dateTimePicker_end.Dock = DockStyle.Fill;
            dateTimePicker_end.Format = DateTimePickerFormat.Short; // o el formato que uses

            // Agregar en columna 7 (índice 6), fila 1
            tableLayout.Controls.Add(dateTimePicker_end, 6, 1);

            // Que ocupe también la columna 8 (índice 7)
            tableLayout.SetColumnSpan(dateTimePicker_end, 2);


            // --- CheckBox enableTime ---
            checkBox_enableTime.Dock = DockStyle.Fill;
            checkBox_enableTime.TextAlign = ContentAlignment.MiddleLeft;

            // Columna 9 (índice 8), fila 0
            tableLayout.Controls.Add(checkBox_enableTime, 8, 0);


            // --- CheckBox RegExp ---
            checkBox_RegExp.Dock = DockStyle.Fill;
            checkBox_RegExp.TextAlign = ContentAlignment.MiddleLeft;

            // Columna 10 (índice 9), fila 0
            tableLayout.Controls.Add(checkBox_RegExp, 9, 0);



            // Ajustes del TextBox de búsqueda
            search_textBox.Dock = DockStyle.Fill;

            // Agregar en columna 9 (índice 8), fila 1
            tableLayout.Controls.Add(search_textBox, 8, 1);

            // Que ocupe también las columnas 10 y 11 (índices 9 y 10)
            tableLayout.SetColumnSpan(search_textBox, 3);



            // Ajustes del botón de búsqueda
            searchBtn.Dock = DockStyle.Fill;

            // Agregar en columna 12 (índice 11), fila 1
            tableLayout.Controls.Add(searchBtn, 11, 1);


            // Ajustes del botón Clear
            clearsearchBtn.Dock = DockStyle.Fill;

            // Agregar en columna 13 (índice 12), fila 1
            tableLayout.Controls.Add(clearsearchBtn, 12, 1);





            // Montar
            groupBox_customSearch.Controls.Clear();
            groupBox_customSearch.Controls.Add(tableLayout);









            // ------------------- Layout central (3 columnas) -------------------
            int topOffset = groupBox_customSearch.Bottom + 6; // deja espacio debajo de la barra

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(4, topOffset, 4, 4) // reserva espacio arriba
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20)); // izquierda
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60)); // centro (grid)
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20)); // derecha

            // --- Panel lateral izquierdo ---
            var leftPanel = new Panel { Dock = DockStyle.Fill };
            groupBox_Main.AutoSize = false;
            groupBox_Main.Dock = DockStyle.Fill;
            leftPanel.Controls.Add(groupBox_Main);

            // --- Grid central ---
            sfDataGrid1.Dock = DockStyle.Fill;

            // --- Panel lateral derecho ---
            var rightPanel = new Panel { Dock = DockStyle.Fill };
            groupBox_TextBox.AutoSize = false;
            groupBox_TextBox.Dock = DockStyle.Fill;
            groupBox_TextBox.Controls.Clear();

            // Layout interno del groupBox_TextBox
            var textBoxLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,   // RichText, Botones, Console, Label
                Padding = new Padding(6),
                Margin = new Padding(0)
            };
            textBoxLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // Fila 0: RichText
            textBoxLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            // Fila 1: Botones
            textBoxLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            // Fila 2: Consola (alto fijo 80px)
            textBoxLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
            // Fila 3: Label
            textBoxLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // RichTextBox
            richTextBox1.BorderStyle = BorderStyle.FixedSingle;
            richTextBox1.Dock = DockStyle.Fill;
            richTextBox1.Margin = new Padding(0, 0, 0, 8);
            textBoxLayout.Controls.Add(richTextBox1, 0, 0);


            


            // Botones lado a lado (4 columnas)
            var buttonsLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 4,
                RowCount = 1,
                AutoSize = true,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            // Ajustar estilos de columnas al 25% cada una
            buttonsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            buttonsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            buttonsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            buttonsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            // Normaliza apariencia de los botones
            void Normalize(Button b)
            {
                b.AutoSize = false;
                b.Dock = DockStyle.Fill;
                b.Height = 32;
                b.MinimumSize = new Size(0, 32);
                b.Margin = new Padding(3);
                b.FlatStyle = FlatStyle.System;
                b.UseVisualStyleBackColor = true;
                b.Font = button_exportPDF.Font;  // igualar fuente
            }

            // Normalizar los 4 botones
            Normalize(button_Font);
            Normalize(button_exportPDF);
            Normalize(button_exportHTML);
            Normalize(button_Report);

            // Agregar en columnas separadas (0,1,2,3)
            buttonsLayout.Controls.Add(button_Font, 0, 0);
            buttonsLayout.Controls.Add(button_exportPDF, 1, 0);
            buttonsLayout.Controls.Add(button_exportHTML, 2, 0);
            buttonsLayout.Controls.Add(button_Report, 3, 0);


            textBoxLayout.Controls.Add(buttonsLayout, 0, 1);






            // Console
            Console.Multiline = true;
            Console.ReadOnly = true;
            //Console.ScrollBars = ScrollBars.Vertical;
            Console.Dock = DockStyle.Fill;
            Console.Margin = new Padding(0, 4, 0, 0);
            textBoxLayout.Controls.Add(Console, 0, 2);

            // Label
            autoLabel1.Dock = DockStyle.Top;
            autoLabel1.Margin = new Padding(0, 6, 0, 0);
            textBoxLayout.Controls.Add(autoLabel1, 0, 3);

            groupBox_TextBox.Controls.Add(textBoxLayout);
            rightPanel.Controls.Add(groupBox_TextBox);

            // --- Montar columnas ---
            mainLayout.Controls.Add(leftPanel, 0, 0);
            mainLayout.Controls.Add(sfDataGrid1, 1, 0);
            mainLayout.Controls.Add(rightPanel, 2, 0);

            // ------------------- Aplicar al form -------------------
            this.Controls.Add(mainLayout);

            // Mantener el MenuStrip arriba
            menuStrip1.Dock = DockStyle.Top;
            this.Controls.SetChildIndex(menuStrip1, 0);

            // Ajustar dinámicamente si cambia el alto del groupbox superior
            void AdjustTopPadding(object? s, EventArgs e)
            {
                mainLayout.Padding = new Padding(4, groupBox_customSearch.Bottom + 6, 4, 4);
            }
            groupBox_customSearch.SizeChanged += AdjustTopPadding;

            this.ResumeLayout(true);

            dateTimePicker_start.Format = DateTimePickerFormat.Custom;
            dateTimePicker_start.CustomFormat = "yyyy-MM-dd HH:mm:ss";
            dateTimePicker_start.Value = Helpers.StartDate;

            dateTimePicker_end.Format = DateTimePickerFormat.Custom;
            dateTimePicker_end.CustomFormat = "yyyy-MM-dd HH:mm:ss";
            dateTimePicker_start.Value = Helpers.EndDate;














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
            button_exportHTML.Enabled = true;
            button_Font.Enabled = true;
            button_Report.Enabled = true;
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
            button_exportHTML.Enabled = false;
            button_Font.Enabled = false;
            button_Report.Enabled = false;
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
                        this.Text = "Browser Reviewer v0.2 is working on:   " + Helpers.db_name;
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
                        this.Text = "Browser Reviewer v0.2 is working on:   " + Helpers.db_name;
                        //Llenar la variable Helpers.labelsTable para el menu emergente de add label
                        setLabels();

                        Helpers.browserUrls = Tools.FillDictionaryFromDatabase();

                        Helpers.browsersWithDownloads = MyTools.GetBrowsersWithDownloads();

                        Helpers.browsersWithBookmarks = MyTools.GetBrowsersWithBookmarks();

                        Helpers.browsersWithAutofill = MyTools.GetBrowsersWithAutofill();


                        AcordeonMenu(Helpers.browserUrls, Helpers.browsersWithDownloads, Helpers.browsersWithBookmarks, Helpers.browsersWithAutofill);

                        Tools.ShowQueryOnDataGridView(sfDataGrid1, Helpers.chromeViewerConnectionString, sqlquery, labelItemCount, Console);
                        enableButtons();

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error opening the project:\n{ex.ToString()}", "ChromeViewer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    enableButtons();
                    labelStatus.Text = "All web history from all browsers";
                }

            }
            

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

                folderBrowserDialog.Description = "Search Web Browser Activity";
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    //initAll();
                    disableButtons();
                    MyTools.LogToConsole(Console, $"Processing...", Color.Blue);

                    string rootDirectory = folderBrowserDialog.SelectedPath;

                    await Tools.ListFilesAndDirectories(rootDirectory, Console);

                    MyTools.CloseLog();
                    MyTools.LogToConsole(Console, $"Processing Finished.", Color.Blue);
                    enableButtons();

                }
                else
                {
                    return;
                }
            }

            Tools.ShowQueryOnDataGridView(sfDataGrid1, Helpers.chromeViewerConnectionString, sqlquery, labelItemCount, Console);
            Helpers.browsersWithDownloads = MyTools.GetBrowsersWithDownloads();
            Helpers.browsersWithBookmarks = MyTools.GetBrowsersWithBookmarks();
            Helpers.browsersWithAutofill = MyTools.GetBrowsersWithAutofill();
            AcordeonMenu(Helpers.browserUrls, Helpers.browsersWithDownloads, Helpers.browsersWithBookmarks, Helpers.browsersWithAutofill);



            labelStatus.Text = "All web history from all browsers";

            button_SearchWebActivity.Enabled = false;

        }






        //private void AcordeonMenu(Dictionary<string, List<string>> navegadorCategorias, Dictionary<string, int> browsersWithDownloads, Dictionary<string, int> browsersWithBookmarks, Dictionary<string, int> browsersWithDatafill)
        //{
        //    Font fm = new Font(Font.FontFamily, 11);
        //    Font fs = new Font(Font.FontFamily, 10);

        //    //FlowLayoutPanel flwMain = new FlowLayoutPanel()
        //    //{
        //    //    BackColor = this.BackColor,
        //    //    BorderStyle = BorderStyle.FixedSingle,
        //    //    FlowDirection = FlowDirection.TopDown,
        //    //    Width = 334,
        //    //    Margin = new Padding(0, 325, 0, 0),
        //    //    WrapContents = false,
        //    //    AutoScroll = true,
        //    //};

        //    FlowLayoutPanel flwMain = new FlowLayoutPanel()
        //    {
        //        BackColor = this.BackColor,
        //        BorderStyle = BorderStyle.FixedSingle,
        //        FlowDirection = FlowDirection.TopDown, // columna única
        //        WrapContents = false,                 // no salta a otra columna
        //        AutoScroll = true,

        //        // CLAVES para que crezca con el GroupBox:
        //        AutoSize = false,
        //        Dock = DockStyle.Fill,
        //        Margin = new Padding(0),        // quita el empuje raro
        //        Padding = new Padding(4)         // aire interno opcional
        //    };


        //    flwMain.SizeChanged += (s, e) =>
        //    {
        //        int usable = flwMain.ClientSize.Width - flwMain.Padding.Horizontal;
        //        foreach (Control c in flwMain.Controls)
        //        {
        //            if (c is Label && (c.Tag as string) == "fullwidth")
        //                c.Width = Math.Max(0, usable - c.Margin.Horizontal);
        //        }
        //    };


        //    // Crear el botón "All Web History"
        //    //Label lblAllHistoryButton = new Label()
        //    //{
        //    //    //BackColor = Color.SteelBlue,
        //    //    BackColor = Color.White,
        //    //    Font = new Font(fm, FontStyle.Bold),
        //    //    ForeColor = Color.Black,
        //    //    Height = 48,
        //    //    Margin = new Padding(3, 1, 3, 1),
        //    //    Padding = new Padding(12, 3, 0, 3),
        //    //    Text = "All Web History",
        //    //    TextAlign = ContentAlignment.MiddleCenter,
        //    //    Width = flwMain.Width
        //    //};

        //    var lblAllHistoryButton = new Label
        //    {
        //        BackColor = Color.White,
        //        Font = new Font(fm, FontStyle.Bold),
        //        ForeColor = Color.Black,
        //        AutoSize = false,                 // importante
        //        Height = 48,                      // conservas el alto
        //        Margin = new Padding(3, 1, 3, 1),
        //        Padding = new Padding(12, 3, 0, 3),
        //        Text = "All Web History",
        //        TextAlign = ContentAlignment.MiddleCenter,
        //        Tag = "fullwidth"                 // marca opcional
        //    };

        //    lblAllHistoryButton.Width = flwMain.ClientSize.Width - flwMain.Padding.Horizontal - lblAllHistoryButton.Margin.Horizontal;


        //    lblAllHistoryButton.Click += (sender, e) =>
        //    {
        //        int utcOffset = Helpers.utcOffset;
        //        string sqlquery;


        //        if (utcOffset == 0)
        //        {
        //            sqlquery = @"SELECT
        //                        r.id AS id,
        //                        r.Browser AS Browser,
        //                        r.Category AS Category,
        //                        r.Potential_activity AS Potential_activity,
        //                        r.Visit_id AS Visit_id,
        //                        r.Url AS Url,
        //                        r.Title AS Title,
        //                        STRFTIME('%Y-%m-%d %H:%M:%f', r.Visit_time) AS Visit_time,
        //                        r.File AS File,
        //                        r.Label AS Label,
        //                        r.Comment AS Comment
        //                    FROM 
        //                        results r
        //                    UNION ALL
        //                    SELECT 
        //                        f.id AS id,
        //                        f.Browser AS Browser,
        //                        f.Category AS Category,
        //                        f.Potential_activity AS Potential_activity,
        //                        f.Visit_id AS Visit_id,
        //                        f.Url AS Url,
        //                        f.Title AS Title,
        //                        STRFTIME('%Y-%m-%d %H:%M:%f', f.Visit_time) AS Visit_time,
        //                        f.File AS File,
        //                        f.Label AS Label,
        //                        f.Comment AS Comment
        //                    FROM 
        //                        firefox_results f;";
        //        }
        //        else
        //        {
        //            string offset = utcOffset > 0 ? $"+{utcOffset} hours" : $"{utcOffset} hours";

        //            sqlquery = $@"SELECT 
        //                        r.id AS id,
        //                        r.Browser AS Browser,
        //                        r.Category AS Category,
        //                        r.Potential_activity AS Potential_activity,
        //                        r.Visit_id AS Visit_id,
        //                        r.Url AS Url,
        //                        r.Title AS Title,
        //                        STRFTIME('%Y-%m-%d %H:%M:%f', r.Visit_time, '{offset}') AS Visit_time,
        //                        r.File AS File,
        //                        r.Label AS Label,
        //                        r.Comment AS Comment
        //                    FROM 
        //                        results r
        //                    UNION ALL
        //                    SELECT 
        //                        f.id AS id,
        //                        f.Browser AS Browser,
        //                        f.Category AS Category,
        //                        f.Potential_activity AS Potential_activity,
        //                        f.Visit_id AS Visit_id,
        //                        f.Url AS Url,
        //                        f.Title AS Title,
        //                        STRFTIME('%Y-%m-%d %H:%M:%f', f.Visit_time, '{offset}') AS Visit_time,
        //                        f.File AS File,
        //                        f.Label AS Label,
        //                        f.Comment AS Comment
        //                    FROM 
        //                        firefox_results f;";
        //        }

        //        Tools.ShowQueryOnDataGridView(sfDataGrid1, Helpers.chromeViewerConnectionString, sqlquery, labelItemCount);
        //        labelStatus.Text = "All web history from all browsers";
        //    };

        //    lblAllHistoryButton.MouseHover += (sender, e) => { lblAllHistoryButton.BackColor = Color.LightBlue; };
        //    lblAllHistoryButton.MouseLeave += (sender, e) => { lblAllHistoryButton.BackColor = Color.White; };



        //    flwMain.Controls.Add(lblAllHistoryButton);

        //    foreach (var navegador in navegadorCategorias.OrderBy(category => category.Key)) //ordenar aqui
        //    {
        //        Image icono = null;
        //        switch (navegador.Key)
        //        {
        //            case "Chrome":
        //                icono = Resource1.Chrome.ToBitmap();
        //                break;
        //            case "Brave":
        //                icono = Resource1.Brave.ToBitmap();
        //                break;
        //            case "Edge":
        //                icono = Resource1.Edge.ToBitmap();
        //                break;
        //            case "Opera":
        //                icono = Resource1.Opera.ToBitmap();
        //                break;
        //            case "Yandex":
        //                icono = Resource1.Yandex.ToBitmap();
        //                break;
        //            case "Vivaldi":
        //                icono = Resource1.Vivaldi.ToBitmap();
        //                break;
        //            case "Firefox":
        //                icono = Resource1.Firefox.ToBitmap();
        //                break;
        //            default:
        //                icono = Resource1.Unknown.ToBitmap();
        //                break;
        //        }

        //        //Label lblNavegador = new Label()
        //        //{
        //        //    BackColor = Color.SteelBlue,
        //        //    Font = fm,
        //        //    ForeColor = Color.White,
        //        //    Height = 48,
        //        //    Image = icono,
        //        //    ImageAlign = ContentAlignment.MiddleLeft,
        //        //    Margin = new Padding(3, 1, 3, 1),
        //        //    Padding = new Padding(12, 3, 0, 3),
        //        //    Text = navegador.Key,
        //        //    TextAlign = ContentAlignment.MiddleCenter,
        //        //    Width = flwMain.Width
        //        //};


        //        Label lblNavegador = new Label()
        //        {
        //            BackColor = Color.SteelBlue,
        //            Font = fm,
        //            ForeColor = Color.White,
        //            AutoSize = false,                       // desactiva autosize
        //            Height = 48,                          // alto fijo
        //            Image = icono,
        //            ImageAlign = ContentAlignment.MiddleLeft,
        //            Margin = new Padding(3, 1, 3, 1),
        //            Padding = new Padding(12, 3, 0, 3),
        //            Text = navegador.Key,
        //            TextAlign = ContentAlignment.MiddleCenter,
        //            Tag = "fullwidth"                  // para el reajuste automático
        //        };

        //        lblNavegador.Width = flwMain.ClientSize.Width - flwMain.Padding.Horizontal - lblNavegador.Margin.Horizontal;


        //        lblNavegador.MouseHover += (sender, e) => { lblNavegador.BackColor = Color.LightBlue; };
        //        lblNavegador.MouseLeave += (sender, e) => { lblNavegador.BackColor = Color.SteelBlue; };



        //        // Agregar al diccionario para saber que label creada dinamicamente pertenece adeterminado navegador
        //        Helpers.navegadorLabels[navegador.Key] = lblNavegador;

        //        // Contar el numero de registros por navegador

        //        //if (Helpers.searchTermExists || checkBox_enableTime.Checked)
        //        if (Helpers.searchTermExists || Helpers.searchTimeCondition)
        //        {
        //            if (Helpers.historyHits.TryGetValue(navegador.Key, out int count))
        //            {
        //                Helpers.itemscount = count;
        //            }
        //            else
        //            {
        //                Helpers.itemscount = 0; // Si la clave no existe, asigna 0
        //            }
        //        }
        //        else
        //        {
        //            Helpers.itemscount = Tools.NumUrlsWithBrowser(navegador.Key);
        //        }

        //        //Actualizar propiedad text del label
        //        Tools.UpdateNavegadorLabel(navegador.Key, navegador.Key + " " + Helpers.itemscount + " hits");



        //        //FlowLayoutPanel flwSubNavegador = new FlowLayoutPanel()
        //        //{
        //        //    AutoSize = true,
        //        //    BackColor = Color.Beige,
        //        //    FlowDirection = FlowDirection.TopDown,
        //        //    Dock = DockStyle.Left,
        //        //    Visible = false,
        //        //    Width = flwMain.Width
        //        //};

        //        var flwSubNavegador = new FlowLayoutPanel
        //        {
        //            Name = "flwSubNavegador",
        //            BackColor = Color.Beige,
        //            FlowDirection = FlowDirection.TopDown,
        //            WrapContents = false,
        //            AutoScroll = true,

        //            // clave: NO Dock=Fill; en FlowLayout no se aplica al hijo
        //            AutoSize = false,
        //            Height = 200,                  // dale un alto visible (ajústalo)
        //            Margin = new Padding(0, 4, 0, 4),
        //            Padding = new Padding(4),
        //            Tag = "fullwidth"           // para que tu SizeChanged ajuste el ancho
        //        };

        //        // ancho inicial al agregarlo
        //        flwSubNavegador.Width =
        //            flwMain.ClientSize.Width - flwMain.Padding.Horizontal - flwSubNavegador.Margin.Horizontal;


        //        icono = Resource1.AllHistory.ToBitmap();

        //        //Label lblAllHistory = new Label()
        //        //{
        //        //    BackColor = Color.Transparent,
        //        //    Font = fs,
        //        //    ForeColor = Color.Black,
        //        //    Image = icono,
        //        //    ImageAlign = ContentAlignment.MiddleLeft,
        //        //    Padding = new Padding(32, 1, 0, 1),
        //        //    Text = "All History",
        //        //    TextAlign = ContentAlignment.MiddleCenter,
        //        //    Width = flwMain.Width,
        //        //    AutoSize = false,
        //        //    Height = Math.Max(icono.Height + 5, fs.Height + 10)
        //        //};


        //        Label lblAllHistory = new Label()
        //        {
        //            BackColor = Color.Transparent,
        //            Font = fs,
        //            ForeColor = Color.Black,
        //            Image = icono,
        //            ImageAlign = ContentAlignment.MiddleLeft,
        //            Padding = new Padding(32, 1, 0, 1),
        //            Text = "All History",
        //            TextAlign = ContentAlignment.MiddleCenter,
        //            AutoSize = false,                                   // no dejar que se mida solo
        //            Height = Math.Max(icono.Height + 5, fs.Height + 10),
        //            Margin = new Padding(3, 1, 3, 1),
        //            Tag = "fullwidth"                              // para el auto-ajuste de ancho
        //        };

        //        // ancho inicial
        //        lblAllHistory.Width = flwMain.ClientSize.Width - flwMain.Padding.Horizontal - lblAllHistory.Margin.Horizontal;


        //        lblAllHistory.Click += (sender, e) =>
        //        {
        //            string tableName = navegador.Key.ToLower().Contains("firefox") ? "firefox_results" : "results";
        //            MyTools tools = new MyTools();
        //            tools.MostrarPorCategoria(labelStatus, sfDataGrid1, Helpers.chromeViewerConnectionString, tableName, null, navegador.Key, labelItemCount);
        //        };

        //        lblAllHistory.MouseHover += (sender, e) => { lblAllHistory.BackColor = Color.LightBlue; };
        //        lblAllHistory.MouseLeave += (sender, e) => { lblAllHistory.BackColor = Color.Transparent; };



        //        flwSubNavegador.Controls.Add(lblAllHistory);

        //        Dictionary<string, List<string>> categorias = new Dictionary<string, List<string>>();

        //        foreach (var url in navegador.Value)
        //        {
        //            string categoria = MyTools.Evaluatecategory(url);

        //            if (!categorias.ContainsKey(categoria))
        //            {
        //                categorias[categoria] = new List<string>();
        //            }

        //            categorias[categoria].Add(url);
        //        }



        //        foreach (var categoria in categorias.OrderBy(c => c.Key))
        //        {

        //            //Aqui insertar codigo para iconos a nivel categorias

        //            icono = null;
        //            switch (categoria.Key)
        //            {
        //                case "Ad Tracking and Analytics":
        //                    icono = Resource1.AdTrackingAnalytics.ToBitmap();
        //                    break;
        //                case "AI":
        //                    icono = Resource1.AI.ToBitmap();
        //                    break;
        //                case "Banking":
        //                    icono = Resource1.Banking.ToBitmap();
        //                    break;
        //                case "Cloud Storage Services":
        //                    icono = Resource1.CloudStorageServices.ToBitmap();
        //                    break;
        //                case "Code Hosting":
        //                    icono = Resource1.CodeHosting.ToBitmap();
        //                    break;
        //                case "Entertainment":
        //                    icono = Resource1.Entertainment.ToBitmap();
        //                    break;
        //                case "Firefox":
        //                    icono = Resource1.Firefox.ToBitmap();
        //                    break;
        //                case "Facebook":
        //                    icono = Resource1.Facebook.ToBitmap();
        //                    break;
        //                case "File Encryption Tools":
        //                    icono = Resource1.FileEncryptionTools.ToBitmap();
        //                    break;
        //                case "Google":
        //                    icono = Resource1.Google.ToBitmap();
        //                    break;
        //                case "Lan Addresses Browsing":
        //                    icono = Resource1.LanAddressesBrowsing.ToBitmap();
        //                    break;
        //                case "Local Files":
        //                    icono = Resource1.LocalFiles.ToBitmap();
        //                    break;
        //                case "News":
        //                    icono = Resource1.News.ToBitmap();
        //                    break;
        //                case "Online Office Suite":
        //                    icono = Resource1.OnlineOfficeSuite.ToBitmap();
        //                    break;
        //                case "Other":
        //                    icono = Resource1.Other.ToBitmap();
        //                    break;
        //                case "Search Engine":
        //                    icono = Resource1.SearchEngine.ToBitmap();
        //                    break;
        //                case "Shopping":
        //                    icono = Resource1.Shopping.ToBitmap();
        //                    break;
        //                case "Social Media":
        //                    icono = Resource1.SocialMedia.ToBitmap();
        //                    break;
        //                case "Technical Forums":
        //                    icono = Resource1.TechicalForums.ToBitmap();
        //                    break;
        //                case "Webmail":
        //                    icono = Resource1.Webmail.ToBitmap();
        //                    break;
        //                case "YouTube":
        //                    icono = Resource1.YouTube.ToBitmap();
        //                    break;
        //                case "Gaming Platforms":
        //                    icono = Resource1.GamingPlatforms.ToBitmap();
        //                    break;
        //                case "Adult Content Sites":
        //                    icono = Resource1.AdultContentSites.ToBitmap();
        //                    break;
        //                case "Cryptocurrency Platforms":
        //                    icono = Resource1.CryptocurrencyPlatforms.ToBitmap();
        //                    break;
        //                case "Hacking and Cybersecurity Sites":
        //                    icono = Resource1.HackingCybersecuritySites.ToBitmap();
        //                    break;
        //                case "Airlines":
        //                    icono = Resource1.Airlines.ToBitmap();
        //                    break;
        //                case "Hotels and Rentals":
        //                    icono = Resource1.Hotels.ToBitmap();
        //                    break;

        //                default:
        //                    icono = Resource1.Unknown.ToBitmap();
        //                    break;
        //            }





        //            //Label lblCategoria = new Label()
        //            //{
        //            //    BackColor = Color.Transparent,
        //            //    Font = fs,
        //            //    ForeColor = Color.Black,
        //            //    Image = icono,
        //            //    ImageAlign = ContentAlignment.MiddleLeft,
        //            //    Padding = new Padding(32, 1, 0, 1),
        //            //    Text = $"{categoria.Key} ({categoria.Value.Count} hits)",
        //            //    TextAlign = ContentAlignment.MiddleCenter,
        //            //    Width = flwMain.Width,
        //            //    AutoSize = false, // Para evitar que el Label se redimensione inesperadamente
        //            //    Height = Math.Max(icono.Height + 5, fs.Height + 10) // Asegurar que la altura del Label sea suficiente
        //            //};


        //            Label lblCategoria = new Label()
        //            {
        //                BackColor = Color.Transparent,
        //                Font = fs,
        //                ForeColor = Color.Black,
        //                Image = icono,
        //                ImageAlign = ContentAlignment.MiddleLeft,
        //                Padding = new Padding(32, 1, 0, 1),
        //                Text = $"{categoria.Key} ({categoria.Value.Count} hits)",
        //                TextAlign = ContentAlignment.MiddleCenter,
        //                AutoSize = false,                                   // evita que se redimensione solo
        //                Height = Math.Max(icono.Height + 5, fs.Height + 10),
        //                Margin = new Padding(3, 1, 3, 1),
        //                Tag = "fullwidth"                              // para el ajuste automático de ancho
        //            };

        //            // ancho inicial, basado en el ancho del FlowLayoutPanel
        //            lblCategoria.Width = flwMain.ClientSize.Width - flwMain.Padding.Horizontal - lblCategoria.Margin.Horizontal;




        //            lblCategoria.Click += (sender, e) =>
        //            {
        //                string tableName = navegador.Key.ToLower().Contains("firefox") ? "firefox_results" : "results";
        //                MyTools tools = new MyTools();
        //                tools.MostrarPorCategoria(labelStatus, sfDataGrid1, Helpers.chromeViewerConnectionString, tableName, categoria.Key, navegador.Key, labelItemCount);
        //            };

        //            lblCategoria.MouseHover += (sender, e) => { lblCategoria.BackColor = Color.LightBlue; };
        //            lblCategoria.MouseLeave += (sender, e) => { lblCategoria.BackColor = Color.Transparent; };

        //            flwSubNavegador.Controls.Add(lblCategoria);
        //        }

        //        flwMain.Controls.Add(lblNavegador);
        //        flwMain.Controls.Add(flwSubNavegador);

        //        lblNavegador.Click += (sender, e) => { flwSubNavegador.Visible = !flwSubNavegador.Visible; };


        //    }

        //    // Aquí añadimos los controles relacionados con los downloads
        //    AddDownloadLabels(flwMain, Helpers.browsersWithDownloads);
        //    AddBookmarkLabels(flwMain, Helpers.browsersWithBookmarks);
        //    AddAutofillLabels(flwMain, Helpers.browsersWithAutofill);



        //    groupBox_Main.Controls.Add(flwMain);
        //}


        private void AcordeonMenu(Dictionary<string, List<string>> navegadorCategorias, Dictionary<string, int> browsersWithDownloads, Dictionary<string, int> browsersWithBookmarks, Dictionary<string, int> browsersWithDatafill)
        {
            //Font fm = new Font(Font.FontFamily, 11);
            //Font fs = new Font(Font.FontFamily, 10);

            Font fm = Helpers.FM;
            Font fs = Helpers.FS;

            // 1) Crear el flow principal y MONTARLO DE INMEDIATO en el groupBox
            var flwMain = new FlowLayoutPanel
            {
                BackColor = this.BackColor,
                BorderStyle = BorderStyle.FixedSingle,
                FlowDirection = FlowDirection.TopDown, // columna única
                WrapContents = false,                 // no salta a otra columna
                AutoScroll = true,
                AutoSize = false,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(4)
            };

            groupBox_Main.AutoSize = false;
            groupBox_Main.Dock = DockStyle.Fill;
            groupBox_Main.Controls.Clear();
            groupBox_Main.Controls.Add(flwMain); // <-- Clave: montarlo antes de medir tamaños

            // Ajuste de ancho para hijos "fullwidth" del flwMain
            flwMain.SizeChanged += (s, e) =>
            {
                int usable = flwMain.ClientSize.Width - flwMain.Padding.Horizontal;
                foreach (Control c in flwMain.Controls)
                    if ((c.Tag as string) == "fullwidth")
                        c.Width = Math.Max(0, usable - c.Margin.Horizontal);
            };

            // 2) All Web History (width ahora sí tiene sentido)
            var lblAllHistoryButton = new Label
            {
                BackColor = Color.White,
                Font = new Font(fm, FontStyle.Bold),
                ForeColor = Color.Black,
                AutoSize = false,
                Height = 48,
                Margin = new Padding(3, 1, 3, 1),
                Padding = new Padding(12, 3, 0, 3),
                Text = "All Web History",
                TextAlign = ContentAlignment.MiddleCenter,
                Tag = "fullwidth"
            };
            // ancho inicial
            lblAllHistoryButton.Width =
                flwMain.ClientSize.Width - flwMain.Padding.Horizontal - lblAllHistoryButton.Margin.Horizontal;

            lblAllHistoryButton.Click += (sender, e) =>
            {
                int utcOffset = Helpers.utcOffset;
                string offset = utcOffset == 0 ? null : (utcOffset > 0 ? $"+{utcOffset} hours" : $"{utcOffset} hours");

                string sqlquery = offset == null
                    ? @"SELECT r.id,id, r.Browser, r.Category, r.Potential_activity, r.Visit_id, r.Url, r.Title,
                        STRFTIME('%Y-%m-%d %H:%M:%f', r.Visit_time) AS Visit_time,
                        r.File, r.Label, r.Comment
               FROM results r
               UNION ALL
               SELECT f.id, f.id, f.Browser, f.Category, f.Potential_activity, f.Visit_id, f.Url, f.Title,
                        STRFTIME('%Y-%m-%d %H:%M:%f', f.Visit_time) AS Visit_time,
                        f.File, f.Label, f.Comment
               FROM firefox_results f;"
                    : $@"SELECT r.id, r.Browser, r.Category, r.Potential_activity, r.Visit_id, r.Url, r.Title,
                        STRFTIME('%Y-%m-%d %H:%M:%f', r.Visit_time, '{offset}') AS Visit_time,
                        r.File, r.Label, r.Comment
               FROM results r
               UNION ALL
               SELECT f.id, f.Browser, f.Category, f.Potential_activity, f.Visit_id, f.Url, f.Title,
                        STRFTIME('%Y-%m-%d %H:%M:%f', f.Visit_time, '{offset}') AS Visit_time,
                        f.File, f.Label, f.Comment
               FROM firefox_results f;";

                Tools.ShowQueryOnDataGridView(sfDataGrid1, Helpers.chromeViewerConnectionString, sqlquery, labelItemCount, Console);
                labelStatus.Text = "All web history from all browsers";
            };
            lblAllHistoryButton.MouseHover += (sender, e) => lblAllHistoryButton.BackColor = Color.LightBlue;
            lblAllHistoryButton.MouseLeave += (sender, e) => lblAllHistoryButton.BackColor = Color.White;

            flwMain.Controls.Add(lblAllHistoryButton);

            // 3) Navegadores
            foreach (var navegador in navegadorCategorias.OrderBy(category => category.Key))
            {
                Image icono = navegador.Key switch
                {
                    "Chrome" => Resource1.Chrome.ToBitmap(),
                    "Brave" => Resource1.Brave.ToBitmap(),
                    "Edge" => Resource1.Edge.ToBitmap(),
                    "Opera" => Resource1.Opera.ToBitmap(),
                    "Yandex" => Resource1.Yandex.ToBitmap(),
                    "Vivaldi" => Resource1.Vivaldi.ToBitmap(),
                    "Firefox" => Resource1.Firefox.ToBitmap(),
                    _ => Resource1.Unknown.ToBitmap(),
                };

                var lblNavegador = new Label
                {
                    BackColor = Color.SteelBlue,
                    Font = fm,
                    ForeColor = Color.White,
                    AutoSize = false,
                    Height = 48,
                    Image = icono,
                    ImageAlign = ContentAlignment.MiddleLeft,
                    Margin = new Padding(3, 1, 3, 1),
                    Padding = new Padding(12, 3, 0, 3),
                    Text = navegador.Key,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Tag = "fullwidth"
                };
                lblNavegador.Width =
                    flwMain.ClientSize.Width - flwMain.Padding.Horizontal - lblNavegador.Margin.Horizontal;

                lblNavegador.MouseHover += (s, e) => lblNavegador.BackColor = Color.LightBlue;
                lblNavegador.MouseLeave += (s, e) => lblNavegador.BackColor = Color.SteelBlue;

                Helpers.navegadorLabels[navegador.Key] = lblNavegador;
                Helpers.itemscount = (Helpers.searchTermExists || Helpers.searchTimeCondition)
                    ? (Helpers.historyHits.TryGetValue(navegador.Key, out int count) ? count : 0)
                    : Tools.NumUrlsWithBrowser(navegador.Key);

                Tools.UpdateNavegadorLabel(navegador.Key, navegador.Key + " " + Helpers.itemscount + " hits");

                // 4) Sub-panel del navegador (Flow dentro de Flow)
                var flwSubNavegador = new FlowLayoutPanel
                {
                    Name = "flwSubNavegador",
                    BackColor = Color.Beige,
                    FlowDirection = FlowDirection.TopDown,
                    WrapContents = false,
                    AutoScroll = false,
                    AutoSize = true,
                    Height = 200,                  // alto visible
                    Margin = new Padding(0, 4, 0, 4),
                    Padding = new Padding(4),
                    Tag = "fullwidth"           // para que el flwMain.SizeChanged le ajuste el ancho
                };
                // ancho propio (como hijo de flwMain)
                flwSubNavegador.Width =
                    flwMain.ClientSize.Width - flwMain.Padding.Horizontal - flwSubNavegador.Margin.Horizontal;

                // Handler liviano para que los labels internos de ESTE subpanel también se estiren
                flwSubNavegador.SizeChanged += (s, e) =>
                {
                    int usable = flwSubNavegador.ClientSize.Width - flwSubNavegador.Padding.Horizontal;
                    foreach (Control c in flwSubNavegador.Controls)
                        if ((c.Tag as string) == "fullwidth")
                            c.Width = Math.Max(0, usable - c.Margin.Horizontal);
                };

                // All History de ese navegador
                icono = Resource1.AllHistory.ToBitmap();
                var lblAllHistory = new Label
                {
                    BackColor = Color.Transparent,
                    Font = fs,
                    ForeColor = Color.Black,
                    Image = icono,
                    ImageAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(32, 1, 0, 1),
                    Text = "All History",
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize = false,
                    Height = Math.Max(icono.Height + 5, fs.Height + 10),
                    Margin = new Padding(3, 1, 3, 1),
                    Tag = "fullwidth" // <-- se ajusta con el SizeChanged del sub-flow
                };
                // ancho inicial contra el SUB-flow (no contra flwMain)
                lblAllHistory.Width =
                    flwSubNavegador.ClientSize.Width - flwSubNavegador.Padding.Horizontal - lblAllHistory.Margin.Horizontal;

                lblAllHistory.Click += (sender, e) =>
                {
                    string tableName = navegador.Key.ToLower().Contains("firefox") ? "firefox_results" : "results";
                    new MyTools().MostrarPorCategoria(labelStatus, sfDataGrid1, Helpers.chromeViewerConnectionString,
                                                      tableName, null, navegador.Key, labelItemCount, Console);
                };
                lblAllHistory.MouseHover += (s, e) => lblAllHistory.BackColor = Color.LightBlue;
                lblAllHistory.MouseLeave += (s, e) => lblAllHistory.BackColor = Color.Transparent;

                flwSubNavegador.Controls.Add(lblAllHistory);

                // Categorías
                var categorias = new Dictionary<string, List<string>>();
                foreach (var url in navegador.Value)
                {
                    string categoria = MyTools.Evaluatecategory(url);
                    if (!categorias.ContainsKey(categoria)) categorias[categoria] = new List<string>();
                    categorias[categoria].Add(url);
                }

                foreach (var categoria in categorias.OrderBy(c => c.Key))
                {
                    icono = categoria.Key switch
                    {
                        "Ad Tracking and Analytics" => Resource1.AdTrackingAnalytics.ToBitmap(),
                        "AI" => Resource1.AI.ToBitmap(),
                        "Banking" => Resource1.Banking.ToBitmap(),
                        "Cloud Storage Services" => Resource1.CloudStorageServices.ToBitmap(),
                        "Code Hosting" => Resource1.CodeHosting.ToBitmap(),
                        "Entertainment" => Resource1.Entertainment.ToBitmap(),
                        "Firefox" => Resource1.Firefox.ToBitmap(),
                        "Facebook" => Resource1.Facebook.ToBitmap(),
                        "File Encryption Tools" => Resource1.FileEncryptionTools.ToBitmap(),
                        "Google" => Resource1.Google.ToBitmap(),
                        "Lan Addresses Browsing" => Resource1.LanAddressesBrowsing.ToBitmap(),
                        "Local Files" => Resource1.LocalFiles.ToBitmap(),
                        "News" => Resource1.News.ToBitmap(),
                        "Online Office Suite" => Resource1.OnlineOfficeSuite.ToBitmap(),
                        "Other" => Resource1.Other.ToBitmap(),
                        "Search Engine" => Resource1.SearchEngine.ToBitmap(),
                        "Shopping" => Resource1.Shopping.ToBitmap(),
                        "Social Media" => Resource1.SocialMedia.ToBitmap(),
                        "Technical Forums" => Resource1.TechicalForums.ToBitmap(),
                        "Webmail" => Resource1.Webmail.ToBitmap(),
                        "YouTube" => Resource1.YouTube.ToBitmap(),
                        "Gaming Platforms" => Resource1.GamingPlatforms.ToBitmap(),
                        "Adult Content Sites" => Resource1.AdultContentSites.ToBitmap(),
                        "Cryptocurrency Platforms" => Resource1.CryptocurrencyPlatforms.ToBitmap(),
                        "Hacking and Cybersecurity Sites" => Resource1.HackingCybersecuritySites.ToBitmap(),
                        "Airlines" => Resource1.Airlines.ToBitmap(),
                        "Hotels and Rentals" => Resource1.Hotels.ToBitmap(),
                        _ => Resource1.Unknown.ToBitmap(),
                    };

                    var lblCategoria = new Label
                    {
                        BackColor = Color.Transparent,
                        Font = fs,
                        ForeColor = Color.Black,
                        Image = icono,
                        ImageAlign = ContentAlignment.MiddleLeft,
                        Padding = new Padding(32, 1, 0, 1),
                        Text = $"{categoria.Key} ({categoria.Value.Count} hits)",
                        TextAlign = ContentAlignment.MiddleCenter,
                        AutoSize = false,
                        Height = Math.Max(icono.Height + 5, fs.Height + 10),
                        Margin = new Padding(3, 1, 3, 1),
                        Tag = "fullwidth" // se ajusta con SizeChanged del sub-flow
                    };
                    // ancho inicial contra el SUB-flow
                    lblCategoria.Width =
                        flwSubNavegador.ClientSize.Width - flwSubNavegador.Padding.Horizontal - lblCategoria.Margin.Horizontal;

                    lblCategoria.Click += (sender, e) =>
                    {
                        string tableName = navegador.Key.ToLower().Contains("firefox") ? "firefox_results" : "results";
                        new MyTools().MostrarPorCategoria(labelStatus, sfDataGrid1, Helpers.chromeViewerConnectionString,
                                                          tableName, categoria.Key, navegador.Key, labelItemCount, Console);
                    };
                    lblCategoria.MouseHover += (s, e) => lblCategoria.BackColor = Color.LightBlue;
                    lblCategoria.MouseLeave += (s, e) => lblCategoria.BackColor = Color.Transparent;

                    flwSubNavegador.Controls.Add(lblCategoria);
                }

                flwMain.Controls.Add(lblNavegador);
                flwMain.Controls.Add(flwSubNavegador);

                // toggle de visibilidad (arranca oculto si quieres)
                flwSubNavegador.Visible = false;
                lblNavegador.Click += (s, e) => flwSubNavegador.Visible = !flwSubNavegador.Visible;
            }

            // Extras (si los usas)
            AddDownloadLabels(flwMain, Helpers.browsersWithDownloads);
            AddBookmarkLabels(flwMain, Helpers.browsersWithBookmarks);
            AddAutofillLabels(flwMain, Helpers.browsersWithAutofill);

            // Ajuste final por si el form ya está mostrado:
            // fuerza un pase de tamaños ahora que todo está montado
            flwMain.PerformLayout();
            // dispara manualmente el ajuste de "fullwidth"
            var args = EventArgs.Empty;
            int usable2 = flwMain.ClientSize.Width - flwMain.Padding.Horizontal;
            foreach (Control c in flwMain.Controls) if ((c.Tag as string) == "fullwidth") c.Width = Math.Max(0, usable2 - c.Margin.Horizontal);
        }



        private void AddDownloadLabels(FlowLayoutPanel flwMain, Dictionary<string, int> browsersWithDownloads)
        {
            Font fm = Helpers.FM;
            Font fs = Helpers.FS;

            // Crear el botón "All Downloads"
            //Label lblAllDownloadsButton = new Label()
            //{
            //    BackColor = Color.White,
            //    Font = new Font(fm, FontStyle.Bold),
            //    ForeColor = Color.Black,
            //    Height = 48,
            //    Margin = new Padding(3, 1, 3, 1),
            //    Padding = new Padding(12, 3, 0, 3),
            //    Text = "All Downloads",
            //    TextAlign = ContentAlignment.MiddleCenter,
            //    Width = flwMain.Width
            //};

            Label lblAllDownloadsButton = new Label()
            {
                BackColor = Color.White,
                Font = new Font(fm, FontStyle.Bold),
                ForeColor = Color.Black,
                AutoSize = false,                 // ✅ no se mide solo
                Height = 48,                    // ✅ alto fijo
                Margin = new Padding(3, 1, 3, 1),
                Padding = new Padding(12, 3, 0, 3),
                Text = "All Downloads",
                TextAlign = ContentAlignment.MiddleCenter,
                Tag = "fullwidth"            // ✅ para que el SizeChanged ajuste el ancho
            };

            // ancho inicial al crearlo
            lblAllDownloadsButton.Width =
                flwMain.ClientSize.Width - flwMain.Padding.Horizontal - lblAllDownloadsButton.Margin.Horizontal;


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
                Tools.ShowQueryOnDataGridView(sfDataGrid1, Helpers.chromeViewerConnectionString, sqlquery, labelItemCount, Console);
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

                //Label lblNavegadorDownloads = new Label()
                //{
                //    BackColor = Color.SteelBlue,
                //    Font = fm,
                //    ForeColor = Color.White,
                //    Height = 48,
                //    Image = icono,
                //    ImageAlign = ContentAlignment.MiddleLeft,
                //    Margin = new Padding(3, 1, 3, 1),
                //    Padding = new Padding(12, 3, 0, 3),
                //    Text = $"{navegador} {count} hits",
                //    TextAlign = ContentAlignment.MiddleCenter,
                //    Width = flwMain.Width
                //};

                Label lblNavegadorDownloads = new Label()
                {
                    BackColor = Color.SteelBlue,
                    Font = fm,
                    ForeColor = Color.White,
                    AutoSize = false,                       // ✅ no se mide solo
                    Height = 48,                          // ✅ alto fijo
                    Image = icono,
                    ImageAlign = ContentAlignment.MiddleLeft,
                    Margin = new Padding(3, 1, 3, 1),
                    Padding = new Padding(12, 3, 0, 3),
                    Text = $"{navegador} {count} hits",
                    TextAlign = ContentAlignment.MiddleCenter,
                    Tag = "fullwidth"                  // ✅ para ajuste automático de ancho
                };

                // ancho inicial calculado contra el flwMain
                lblNavegadorDownloads.Width =
                    flwMain.ClientSize.Width - flwMain.Padding.Horizontal - lblNavegadorDownloads.Margin.Horizontal;

                // añadir al FlowLayoutPanel
                flwMain.Controls.Add(lblNavegadorDownloads);


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
                    //Aqui esta el BUG, 
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
                    Tools.ShowQueryOnDataGridView(sfDataGrid1, Helpers.chromeViewerConnectionString, sqlquery, labelItemCount, Console);
                    labelStatus.Text = $"Downloads from {navegador}";


                };

                flwMain.Controls.Add(lblNavegadorDownloads);
            }
        }





        private void AddBookmarkLabels(FlowLayoutPanel flwMain, Dictionary<string, int> browsersWithBookmarks)
        {
            Font fm = Helpers.FM;
            Font fs = Helpers.FS;

            // Crear el botón "All Bookmarks"
            //Label lblAllBookmarksButton = new Label()
            //{
            //    BackColor = Color.White,
            //    Font = new Font(fm, FontStyle.Bold),
            //    ForeColor = Color.Black,
            //    Height = 48,
            //    Margin = new Padding(3, 1, 3, 1),
            //    Padding = new Padding(12, 3, 0, 3),
            //    Text = "All Bookmarks",
            //    TextAlign = ContentAlignment.MiddleCenter,
            //    Width = flwMain.Width
            //};


            Label lblAllBookmarksButton = new Label()
            {
                BackColor = Color.White,
                Font = new Font(fm, FontStyle.Bold),
                ForeColor = Color.Black,
                AutoSize = false,                 // ✅ no se mide solo
                Height = 48,                    // ✅ alto fijo
                Margin = new Padding(3, 1, 3, 1),
                Padding = new Padding(12, 3, 0, 3),
                Text = "All Bookmarks",
                TextAlign = ContentAlignment.MiddleCenter,
                Tag = "fullwidth"            // ✅ para que el SizeChanged lo ajuste
            };

            // ancho inicial contra flwMain
            lblAllBookmarksButton.Width =
                flwMain.ClientSize.Width - flwMain.Padding.Horizontal - lblAllBookmarksButton.Margin.Horizontal;

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



                Tools.ShowQueryOnDataGridView(sfDataGrid1, Helpers.chromeViewerConnectionString, sqlquery, labelItemCount, Console);
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

                //Label lblNavegadorBookmarks = new Label()
                //{
                //    BackColor = Color.SteelBlue,
                //    Font = fm,
                //    ForeColor = Color.White,
                //    Height = 48,
                //    Image = icono,
                //    ImageAlign = ContentAlignment.MiddleLeft,
                //    Margin = new Padding(3, 1, 3, 1),
                //    Padding = new Padding(12, 3, 0, 3),
                //    Text = $"{navegador} {count} hits",
                //    TextAlign = ContentAlignment.MiddleCenter,
                //    Width = flwMain.Width
                //};


                Label lblNavegadorBookmarks = new Label()
                {
                    BackColor = Color.SteelBlue,
                    Font = fm,
                    ForeColor = Color.White,
                    AutoSize = false,                       // ✅ no se mide solo
                    Height = 48,                          // ✅ alto fijo
                    Image = icono,
                    ImageAlign = ContentAlignment.MiddleLeft,
                    Margin = new Padding(3, 1, 3, 1),
                    Padding = new Padding(12, 3, 0, 3),
                    Text = $"{navegador} {count} hits",
                    TextAlign = ContentAlignment.MiddleCenter,
                    Tag = "fullwidth"                  // ✅ para ajuste automático de ancho
                };

                // ancho inicial contra flwMain
                lblNavegadorBookmarks.Width =
                    flwMain.ClientSize.Width - flwMain.Padding.Horizontal - lblNavegadorBookmarks.Margin.Horizontal;

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






                    Tools.ShowQueryOnDataGridView(sfDataGrid1, Helpers.chromeViewerConnectionString, sqlquery, labelItemCount, Console);
                    labelStatus.Text = $"Bookmarks from {navegador}";

                };

                flwMain.Controls.Add(lblNavegadorBookmarks);
            }
        }





        //Prueba con autofill_data

        private void AddAutofillLabels(FlowLayoutPanel flwMain, Dictionary<string, int> browsersWithAutofill)
        {

            Font fm = Helpers.FM;
            Font fs = Helpers.FS;

            // Crear el botón "All Autofill"
            //Label lblAllAutofillButton = new Label()
            //{
            //    BackColor = Color.White,
            //    Font = new Font(fm, FontStyle.Bold),
            //    ForeColor = Color.Black,
            //    Height = 48,
            //    Margin = new Padding(3, 1, 3, 1),
            //    Padding = new Padding(12, 3, 0, 3),
            //    Text = "All Autofill Data",
            //    TextAlign = ContentAlignment.MiddleCenter,
            //    Width = flwMain.Width
            //};


            Label lblAllAutofillButton = new Label()
            {
                BackColor = Color.White,
                Font = new Font(fm, FontStyle.Bold),
                ForeColor = Color.Black,
                AutoSize = false,                 // ✅ no se mide solo
                Height = 48,                    // ✅ alto fijo
                Margin = new Padding(3, 1, 3, 1),
                Padding = new Padding(12, 3, 0, 3),
                Text = "All Autofill Data",
                TextAlign = ContentAlignment.MiddleCenter,
                Tag = "fullwidth"            // ✅ para que el SizeChanged lo reajuste
            };

            // ancho inicial contra flwMain
            lblAllAutofillButton.Width =
                flwMain.ClientSize.Width - flwMain.Padding.Horizontal - lblAllAutofillButton.Margin.Horizontal;

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




                Tools.ShowQueryOnDataGridView(sfDataGrid1, Helpers.chromeViewerConnectionString, sqlquery, labelItemCount, Console);
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

                //Label lblNavegadorAutofill = new Label()
                //{
                //    BackColor = Color.SteelBlue,
                //    Font = fm,
                //    ForeColor = Color.White,
                //    Height = 48,
                //    Image = icono,
                //    ImageAlign = ContentAlignment.MiddleLeft,
                //    Margin = new Padding(3, 1, 3, 1),
                //    Padding = new Padding(12, 3, 0, 3),
                //    Text = $"{navegador} {count} hits",
                //    TextAlign = ContentAlignment.MiddleCenter,
                //    Width = flwMain.Width
                //};


                Label lblNavegadorAutofill = new Label()
                {
                    BackColor = Color.SteelBlue,
                    Font = fm,
                    ForeColor = Color.White,
                    AutoSize = false,                       
                    Height = 48,                         
                    Image = icono,
                    ImageAlign = ContentAlignment.MiddleLeft,
                    Margin = new Padding(3, 1, 3, 1),
                    Padding = new Padding(12, 3, 0, 3),
                    Text = $"{navegador} {count} hits",
                    TextAlign = ContentAlignment.MiddleCenter,
                    Tag = "fullwidth"                  
                };

                // ancho inicial calculado contra flwMain
                lblNavegadorAutofill.Width =
                    flwMain.ClientSize.Width - flwMain.Padding.Horizontal - lblNavegadorAutofill.Margin.Horizontal;

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

                   

                    Tools.ShowQueryOnDataGridView(sfDataGrid1, Helpers.chromeViewerConnectionString, sqlquery, labelItemCount, Console);
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
            Tools.ShowQueryOnDataGridView(sfDataGrid1, Helpers.chromeViewerConnectionString, sqlquery, labelItemCount, Console);

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
                //label_startDate.Enabled = true;
                //label_endDate.Enabled = true;
            }
            else
            {
                dateTimePicker_start.Enabled = false;
                dateTimePicker_end.Enabled = false;
                //label_startDate.Enabled = false;
                //label_endDate.Enabled = false;
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
            if (dataGrid?.View?.Records == null || dataGrid.View.Records.Count == 0)
            {
                MessageBox.Show("There is no data to export.", "Notice",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                Title = "Save Report",
                FileName = $"BrowserReviewer_Detail_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
            };
            if (sfd.ShowDialog() != DialogResult.OK) return;

            try
            {
                using var document = new PdfDocument();
                document.DocumentInformation.Title = "Browser Reviewer - Detail Report";
                document.DocumentInformation.Author = "Browser Reviewer";

                // ===== Header (template, opcional)
                float pageWidth = PdfPageSize.A4.Width;
                var header = new PdfPageTemplateElement(pageWidth, 40);
                var headerFont = new PdfStandardFont(PdfFontFamily.Helvetica, 12, PdfFontStyle.Bold);
                var subFont = new PdfStandardFont(PdfFontFamily.Helvetica, 9);
                header.Graphics.DrawString($"Browser Reviewer    {Helpers.db_name}",
                    headerFont, PdfBrushes.DarkSlateGray, new PointF(0, 4));
                header.Graphics.DrawString(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    subFont, PdfBrushes.Gray, new PointF(0, 22));
                document.Template.Top = header;

                // ===== Datos para el resumen
                var visibleCols = dataGrid.Columns.Where(c => c.Visible).ToList();
                int rowCount = dataGrid.View.Records.Count;
                string sortInfo = GetSortSummary(dataGrid);     // "Col1 ASC, Col2 DESC" o "None"
                string filtInfo = GetFilterSummary(dataGrid);   // "Col = 'X', Col2 contains 'y'" o "None"

                // ===== Página 1: RESUMEN
                {
                    var page = document.Pages.Add();
                    var g = page.Graphics;

                    // Título
                    var titleFont = new PdfStandardFont(PdfFontFamily.Helvetica, 16, PdfFontStyle.Bold);
                    g.DrawString("Export Summary", titleFont, PdfBrushes.Black, new PointF(36, 60));

                    // Cuadro de resumen con PdfGrid (clave/valor)
                    var summary = new DataTable();
                    summary.Columns.Add("Field");
                    summary.Columns.Add("Value");

                    summary.Rows.Add("Rows exported", rowCount.ToString());
                    summary.Rows.Add("Visible columns", visibleCols.Count.ToString());
                    summary.Rows.Add("Sort", sortInfo);
                    summary.Rows.Add("Filters", filtInfo);
                    summary.Rows.Add("Exported at", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    summary.Rows.Add("Time zone", $"UTC{(Helpers.utcOffset >= 0 ? "+" : "")}{Helpers.utcOffset}");

                    //Añadir el UTC aqui a modo de información

                    // (Opcional) lista de nombres de columnas visibles
                    string colList = string.Join(", ", visibleCols.Select(c => c.HeaderText ?? c.MappingName ?? "Column"));
                    summary.Rows.Add("Columns", colList);

                    var sumGrid = new PdfGrid { DataSource = summary };
                    sumGrid.Style.CellPadding = new PdfPaddings(6, 4, 6, 4);

                    // Encabezado del grid de resumen
                    foreach (PdfGridRow h in sumGrid.Headers)
                    {
                        foreach (PdfGridCell cell in h.Cells)
                        {
                            cell.Style = new PdfGridCellStyle
                            {
                                BackgroundBrush = new PdfSolidBrush(new PdfColor(246, 247, 249)),
                                Font = new PdfStandardFont(PdfFontFamily.Helvetica, 10, PdfFontStyle.Bold),
                                Borders = new PdfBorders { All = new PdfPen(new PdfColor(210, 210, 210), 0.8f) }
                            };
                        }
                    }
                    sumGrid.Rows.ApplyStyle(new PdfGridCellStyle
                    {
                        Font = new PdfStandardFont(PdfFontFamily.Helvetica, 10),
                        Borders = new PdfBorders { All = new PdfPen(new PdfColor(224, 224, 224), 0.6f) }
                    });

                    float clientW = page.GetClientSize().Width - 72;
                    sumGrid.Columns[0].Width = clientW * 0.30f; // etiqueta
                    sumGrid.Columns[1].Width = clientW * 0.70f; // valor

                    sumGrid.Draw(page, new PointF(36, 92));
                }

                // ===== Páginas siguientes: detalle por registro (Field/Value)
                foreach (var rec in dataGrid.View.Records)
                {
                    var rv = rec.Data as DataRowView;
                    if (rv == null) continue;

                    var page = document.Pages.Add();

                    var dt = new DataTable();
                    dt.Columns.Add("Field");
                    dt.Columns.Add("Value");

                    foreach (var col in visibleCols)
                    {
                        string name = col.HeaderText ?? col.MappingName ?? "Column";
                        string mapping = col.MappingName ?? col.HeaderText ?? name;

                        object valueObj = null;
                        try { valueObj = rv[mapping]; } catch { /* si no existe, null */ }

                        dt.Rows.Add(name, valueObj?.ToString() ?? "(no data)");
                    }

                    var grid2 = new PdfGrid { DataSource = dt };
                    foreach (PdfGridRow h in grid2.Headers)
                    {
                        foreach (PdfGridCell cell in h.Cells)
                        {
                            cell.Style = new PdfGridCellStyle
                            {
                                BackgroundBrush = new PdfSolidBrush(new PdfColor(246, 247, 249)),
                                Font = new PdfStandardFont(PdfFontFamily.Helvetica, 10, PdfFontStyle.Bold),
                                Borders = new PdfBorders { All = new PdfPen(new PdfColor(210, 210, 210), 0.8f) }
                            };
                        }
                    }
                    grid2.Rows.ApplyStyle(new PdfGridCellStyle
                    {
                        Font = new PdfStandardFont(PdfFontFamily.Helvetica, 10),
                        Borders = new PdfBorders { All = new PdfPen(new PdfColor(224, 224, 224), 0.6f) }
                    });
                    grid2.Style.CellPadding = new PdfPaddings(6, 4, 6, 4);

                    float clientW = page.GetClientSize().Width - 72;
                    grid2.Columns[0].Width = clientW * 0.35f; // Field
                    grid2.Columns[1].Width = clientW * 0.65f; // Value

                    grid2.Draw(page, new PointF(36, 60));
                }

                // ===== Pie de página "Page X of Y" (solución A: al final)
                var footerFont = new PdfStandardFont(PdfFontFamily.Helvetica, 9);
                int totalPages = document.Pages.Count;
                for (int i = 0; i < totalPages; i++)
                {
                    var page = document.Pages[i];
                    var g = page.Graphics;

                    string txt = $"Page {i + 1} of {totalPages}";
                    SizeF size = footerFont.MeasureString(txt);

                    float x = page.GetClientSize().Width - size.Width - 36;
                    float y = page.GetClientSize().Height + 6; // bajo el área de contenido

                    g.DrawString(txt, footerFont, PdfBrushes.Gray, new PointF(x, y));
                }

                // Guardar/abrir
                using (var fs = new FileStream(sfd.FileName, FileMode.Create))
                {
                    document.Save(fs);
                }

                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = sfd.FileName,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"The file was saved but could not be opened automatically.\n{ex.Message}",
                        "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                MessageBox.Show("Export finished", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void button_Font_Click(object sender, EventArgs e)
        {
            //using (var dlg = new FontDialog())
            //{
            //    // Fuente de referencia (por ejemplo, la del formulario o alguna etiqueta)
            //    Font f = dlg.Font;
            //    //dlg.ShowEffects = true;

            //    if (dlg.ShowDialog(this) == DialogResult.OK)
            //    {
            //        // Si quieres cambiar SOLO el tamaño (conservando familia/estilo de cada Label):
            //        float nuevoTamano = dlg.Font.SizeInPoints;
            //        AplicarTamanoSoloAEtique­tas(groupBox_Main, nuevoTamano);  // o el contenedor que corresponda

            //        // Si quieres aplicar la fuente elegida completa (familia+estilo+tamaño) a todas:
            //        // AplicarFuenteCompletaAEtique­tas(flwMain, dlg.Font);

            //        // 2) SfDataGrid: aplicar tamaño (y opcionalmente familia/estilo) a celdas y encabezados
            //        //    Estas propiedades existen en WinForms SfDataGrid:
            //        sfDataGrid1.Style.CellStyle.Font.Size = f.SizeInPoints;
            //        sfDataGrid1.Style.HeaderStyle.Font.Size = f.SizeInPoints;

            //        // --- OPCIONAL (dependiendo de versión): descomenta si tu API expone estas propiedades ---
            //        // sfDataGrid1.Style.CellStyle.Font.Facename   = f.Name;        // familia
            //        // sfDataGrid1.Style.HeaderStyle.Font.Facename = f.Name;
            //        // sfDataGrid1.Style.CellStyle.Font.Bold       = f.Bold;
            //        // sfDataGrid1.Style.CellStyle.Font.Italic     = f.Italic;
            //        // sfDataGrid1.Style.CellStyle.Font.Underline  = f.Underline;
            //        // sfDataGrid1.Style.HeaderStyle.Font.Bold     = true;          // encabezado en negrita, si quieres

            //        // 2.b) Ajusta alturas para que se vea el tamaño nuevo
            //        int textH = TextRenderer.MeasureText("Ag", new Font(f.FontFamily, f.SizeInPoints, f.Style, GraphicsUnit.Point)).Height;
            //        int pad = 8;
            //        sfDataGrid1.RowHeight = Math.Max(textH + pad, 22);
            //        sfDataGrid1.HeaderRowHeight = Math.Max(textH + pad, 24);

            //        sfDataGrid1.Invalidate();
            //        sfDataGrid1.Refresh();

            //        // (opcional) también puedes modificar encabezados específicamente
            //        //sfDataGrid1.Style.HeaderStyle.Font = sfDataGrid1.Font;

            //        // 3) RichTextBox
            //        richTextBox1.Font = new Font(richTextBox1.Font.FontFamily, nuevoTamano, richTextBox1.Font.Style);
            //    }
            //}



            using (var dlg = new FontDialog())
            {
                // Fuente inicial de referencia (por ejemplo, la del formulario actual)
                dlg.Font = this.Font;
                dlg.ShowEffects = true;

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    Font f = dlg.Font;

                    // FM = fuente elegida
                    Helpers.FM = new Font(f.FontFamily, f.SizeInPoints, f.Style, GraphicsUnit.Point);

                    // FS = misma fuente pero un punto menos de tamaño
                    float smaller = Math.Max(f.SizeInPoints - 1, 1); // evita tamaño <= 0
                    Helpers.FS = new Font(f.FontFamily, smaller, f.Style, GraphicsUnit.Point);


                    // 1) Labels dinámicas
                    float nuevoTamano = f.SizeInPoints;
                    AplicarTamanoSoloAEtiquetas(groupBox_Main, nuevoTamano);

                    // 2) SfDataGrid
                    sfDataGrid1.Style.CellStyle.Font.Size = f.SizeInPoints;
                    sfDataGrid1.Style.HeaderStyle.Font.Size = f.SizeInPoints;

                    // Ajustar altura de filas/encabezados para evitar recortes
                    int textH = TextRenderer.MeasureText("Ag", f).Height;
                    int pad = 8;
                    sfDataGrid1.RowHeight = Math.Max(textH + pad, 22);
                    sfDataGrid1.HeaderRowHeight = Math.Max(textH + pad, 24);

                    sfDataGrid1.Invalidate();
                    sfDataGrid1.Refresh();

                    // 3) RichTextBox
                    richTextBox1.Font = new Font(richTextBox1.Font.FontFamily, f.SizeInPoints, f.Style, GraphicsUnit.Point);
                }
            }
        }



        // Cambia SOLO el tamaño manteniendo familia/estilo de cada etiqueta
        private void AplicarTamanoSoloAEtique­tas(Control parent, float sizeInPoints)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is Label lbl)
                    lbl.Font = new Font(lbl.Font.FontFamily, sizeInPoints, lbl.Font.Style, GraphicsUnit.Point);

                // Recursivo por si hay anidados (FlowLayouts dentro de FlowLayouts)
                if (c.HasChildren) AplicarTamanoSoloAEtique­tas(c, sizeInPoints);
            }
        }


        // Aplica la fuente elegida COMPLETA a todas las etiquetas
        private void AplicarFuenteCompletaAEtique­tas(Control parent, Font nuevaFuente)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is Label lbl)
                    lbl.Font = nuevaFuente;

                if (c.HasChildren) AplicarFuenteCompletaAEtique­tas(c, nuevaFuente);
            }
        }












        private void button_exportHTML_Click(object sender, EventArgs e)
        {
            ExportSfDataGridToHtml(sfDataGrid1, "Browser Reviewer Export");
        }



        private void ExportSfDataGridToHtml(SfDataGrid grid, string titulo = "Export")
        {
            if (grid == null || grid.Columns.Count == 0 || grid.View == null)
            {
                MessageBox.Show("No hay datos para exportar.", "Exportar a HTML",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var sfd = new SaveFileDialog
            {
                Filter = "HTML (*.html)|*.html",
                FileName = $"{titulo}_{DateTime.Now:yyyyMMdd_HHmmss}.html"
            })
            {
                if (sfd.ShowDialog(this) != DialogResult.OK) return;

                string html = BuildHtmlFromGrid(grid, titulo);
                File.WriteAllText(sfd.FileName, html, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = sfd.FileName,
                        UseShellExecute = true
                    });
                }
                catch { /* opcional: manejar */ }
            }
        }











        private string BuildHtmlFromGrid(Syncfusion.WinForms.DataGrid.SfDataGrid grid, string titulo)
        {
            var sb = new StringBuilder();

            // ====== Datos para el resumen ======
            var visibleCols = grid.Columns.Where(c => c.Visible).ToList();
            int rowCount = grid.View?.Records?.Count ?? 0;
            string sortInfo = GetSortSummary(grid);     // "Column1 ASC, Column2 DESC" o "None"
            string filtInfo = GetFilterSummary(grid);   // Predicados o "None"

            // ====== CABECERA HTML + CSS ======
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang='en'><head>");
            sb.AppendLine("<meta charset='utf-8'>");
            sb.AppendLine("<meta name='viewport' content='width=device-width, initial-scale=1'>");
            sb.AppendLine("<title>" + WebUtility.HtmlEncode(titulo) + "</title>");
            sb.AppendLine("<style>");
            sb.AppendLine(@"
        :root{ --head-h:38px; }

        body{font-family:'Segoe UI',Tahoma,Arial,sans-serif;font-size:14px;color:#222;padding:16px;}
        .title{font-size:18px;font-weight:600;margin-bottom:6px;}
        .summary{font-size:13px;color:#444;background:#f9f9fb;border:1px solid #e6e6ee;border-radius:6px;padding:8px 10px;margin:8px 0 14px 0;}
        .summary b{color:#222;}

        /* Un solo scroll (el de la página) */
        .table-wrap{
          border:1px solid #ddd; 
          border-radius:8px;
        }

        table{border-collapse:collapse;width:100%;table-layout:fixed;min-width:720px;}
        th,td{border:1px solid #e0e0e0;padding:8px 10px;vertical-align:top;}

        /* sticky base para todas las celdas del thead */
        thead th{
          position:sticky;
          background:#f6f7f9;
          z-index:3;
          border-bottom:1px solid #ddd;
        }

        /* fila de encabezados (títulos) arriba de todo */
        thead tr.headers th{
          top:0;
          font-weight:600;
          text-align:left;
          cursor:pointer; user-select:none;
          position:sticky;
        }

        /* fila de filtros inmediatamente debajo de los títulos (dropdowns) */
        thead tr.filters th{
          top:var(--head-h);
          z-index:2;
          background:#f5f7fb;
          border-bottom:1px solid #ddd;
        }
        thead tr.filters select{
          width:100%;
          box-sizing:border-box;
          padding:4px;
          font-size:12px;
          border:1px solid #d8d8e0;
          border-radius:4px;
          background:#fff;
        }

        /* Indicadores de orden */
        thead tr.headers th.sort-asc::after{content:'▲'; position:absolute; right:8px; color:#666; font-size:10px;}
        thead tr.headers th.sort-desc::after{content:'▼'; position:absolute; right:8px; color:#666; font-size:10px;}

        tr:nth-child(even){background:#fbfbfb;}
        .box{border:1px solid #d0d0d0;background:#fff;border-radius:4px;padding:6px;overflow-wrap:anywhere;}
        a{color:#0067c0;text-decoration:none;} a:hover{text-decoration:underline;}
        ");
            sb.AppendLine("</style></head><body>");

            sb.AppendLine($"<div class='title'>{WebUtility.HtmlEncode(titulo)}</div>");

            // ====== RESUMEN ======
            sb.AppendLine("<div class='summary'>");
            sb.AppendLine("<div><b>Rows exported:</b> " + rowCount + "</div>");
            sb.AppendLine("<div><b>Visible columns:</b> " + visibleCols.Count + "</div>");
            sb.AppendLine("<div><b>Sort:</b> " + WebUtility.HtmlEncode(sortInfo) + "</div>");
            sb.AppendLine("<div><b>Filters:</b> " + WebUtility.HtmlEncode(filtInfo) + "</div>");
            int offset = Helpers.utcOffset; // por ejemplo: -5, 0, +2
            string tzStr = $"UTC{(offset >= 0 ? "+" : "")}{offset}"; 
                                                                                         

            sb.AppendLine($"<div><b>Time zone:</b> {WebUtility.HtmlEncode(tzStr)}</div>");
            sb.AppendLine("<div style='margin-top:4px;color:#666;font-size:12px;'>Exported at " +
                          DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "</div>");
            
            sb.AppendLine("</div>");

            // ====== TABLA ======
            sb.AppendLine("<div class='table-wrap'>");
            sb.AppendLine("<table>");
            sb.AppendLine("<thead>");

            // Encabezados (clickeables para ordenar)
            sb.AppendLine("<tr class='headers'>");
            foreach (var col in visibleCols)
            {
                var header = (col.HeaderText ?? col.MappingName ?? "Column");
                sb.AppendLine("<th>" + WebUtility.HtmlEncode(header) + "</th>");
            }
            sb.AppendLine("</tr>");

            // Fila de filtros -> celdas vacías; JS inserta <select>
            sb.AppendLine("<tr class='filters'>");
            foreach (var _ in visibleCols)
                sb.AppendLine("<th></th>");
            sb.AppendLine("</tr>");

            sb.AppendLine("</thead>");

            // Filas (respeta filtros/orden del grid ya aplicados)
            sb.AppendLine("<tbody>");
            foreach (var rec in grid.View.Records)
            {
                var data = rec.Data;
                sb.AppendLine("<tr>");

                foreach (var col in visibleCols)
                {
                    string mapping = col.MappingName ?? col.HeaderText ?? string.Empty;
                    object? valueObj = GetPropertyValue(data, mapping);
                    string raw = valueObj?.ToString() ?? string.Empty;
                    string encoded = WebUtility.HtmlEncode(raw);

                    bool isUrlColumn = mapping.Equals("Url", StringComparison.OrdinalIgnoreCase) ||
                                       mapping.Equals("URL", StringComparison.OrdinalIgnoreCase);
                    bool looksLikeUrl = raw.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                                        raw.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

                    if ((isUrlColumn || looksLikeUrl) && Uri.IsWellFormedUriString(raw, UriKind.Absolute))
                    {
                        string shortText = ShortenUrlText(raw, 64);
                        sb.AppendLine("<td><div class='box'><a href='" + WebUtility.HtmlEncode(raw) +
                                      "' target='_blank' title='" + WebUtility.HtmlEncode(raw) + "'>" +
                                      WebUtility.HtmlEncode(shortText) + "</a></div></td>");
                    }
                    else
                    {
                        sb.AppendLine("<td><div class='box'>" + encoded + "</div></td>");
                    }
                }

                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</tbody></table>");
            sb.AppendLine("</div>"); // .table-wrap

            // ====== SCRIPT: medir altura de headers + dropdowns únicos + ordenamiento ======
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

          var tbl = document.querySelector('table');
          if(!tbl) return;

          var thead = tbl.querySelector('thead'); if (!thead) return;
          var headerRow = thead.querySelector('tr.headers');
          var filterRow = thead.querySelector('tr.filters');
          var tbody = tbl.querySelector('tbody'); if(!tbody) return;

          // 1) Calcular altura real de la fila de títulos y setear --head-h
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

          // 2) Filtros con dropdowns de valores únicos
          if (filterRow){
            function applyFilters(){
              var selects = Array.from(filterRow.querySelectorAll('select'));
              allRows.forEach(function(tr){
                var tds = tr.children, visible = true;
                for (var i=0; i<selects.length; i++){
                  var sel = selects[i];
                  var val = sel ? sel.value : '';
                  if(!val) continue; // (Todos)
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

            for (var colIndex = 0; colIndex < filterRow.cells.length; colIndex++){
              var cell = filterRow.cells[colIndex];
              if (!cell) continue;

              cell.innerHTML = ''; // limpiar por si había input
              var select = document.createElement('select');
              cell.appendChild(select);

              // Recolectar valores únicos del tbody
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
                ths.forEach(function(x){ if (x !== th) x.classList.remove('sort-asc','sort-desc'); });

                var current = th.classList.contains('sort-asc') ? 'asc' :
                              th.classList.contains('sort-desc') ? 'desc' : null;

                var next = current === 'asc' ? 'desc' : (current === 'desc' ? null : 'asc');

                if (!next){
                  th.classList.remove('sort-asc','sort-desc');
                  originalOrder.forEach(function(tr){ tbody.appendChild(tr); });
                  return;
                }

                th.classList.toggle('sort-asc',  next === 'asc');
                th.classList.toggle('sort-desc', next === 'desc');

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

                var rowsHidden = Array.from(tbody.rows).filter(function(r){ return r.style.display === 'none'; });
                rowsVisible.concat(rowsHidden).forEach(function(r){ tbody.appendChild(r); });
              });
            });
          }
        })();
        </script>");

            // ====== FIN ======
            sb.AppendLine("</body></html>");

            // (opcional) resaltado de búsqueda existente en tu app
            string finalHtml = sb.ToString();
            if (Helpers.searchTermExists)
                finalHtml = HighlightSearchTerms(finalHtml, Helpers.searchTerm, Helpers.searchTermRegExp);

            return finalHtml;
        }

        //Con multiple seleccion en los filtros pero visualmente no me gusto.
        //        private string BuildHtmlFromGrid(Syncfusion.WinForms.DataGrid.SfDataGrid grid, string titulo)
        //        {
        //            var sb = new StringBuilder();

        //            // ====== Datos para el resumen ======
        //            var visibleCols = grid.Columns.Where(c => c.Visible).ToList();
        //            int rowCount = grid.View?.Records?.Count ?? 0;
        //            string sortInfo = GetSortSummary(grid);     // "Column1 ASC, Column2 DESC" o "None"
        //            string filtInfo = GetFilterSummary(grid);   // Predicados o "None"

        //            // ====== CABECERA HTML + CSS ======
        //            sb.AppendLine("<!DOCTYPE html>");
        //            sb.AppendLine("<html lang='en'><head>");
        //            sb.AppendLine("<meta charset='utf-8'>");
        //            sb.AppendLine("<meta name='viewport' content='width=device-width, initial-scale=1'>");
        //            sb.AppendLine("<title>" + WebUtility.HtmlEncode(titulo) + "</title>");
        //            sb.AppendLine("<style>");
        //            sb.AppendLine(@"
        //:root{ --head-h:38px; }

        //body{font-family:'Segoe UI',Tahoma,Arial,sans-serif;font-size:14px;color:#222;padding:16px;}
        //.title{font-size:18px;font-weight:600;margin-bottom:6px;}
        //.summary{font-size:13px;color:#444;background:#f9f9fb;border:1px solid #e6e6ee;border-radius:6px;padding:8px 10px;margin:8px 0 10px 0;}
        //.summary b{color:#222;}

        //.toolbar{display:flex; gap:8px; align-items:center; margin:6px 0 14px;}
        //.toolbar button{
        //  padding:6px 10px; border:1px solid #ccc; background:#fff; border-radius:6px; cursor:pointer;
        //}
        //.toolbar button:hover{background:#f6f6f6;}

        //.table-wrap{
        //  border:1px solid #ddd; 
        //  border-radius:8px;
        //}

        //table{border-collapse:collapse;width:100%;table-layout:fixed;min-width:720px;}
        //th,td{border:1px solid #e0e0e0;padding:8px 10px;vertical-align:top;}

        ///* sticky base para todas las celdas del thead */
        //thead th{
        //  position:sticky;
        //  background:#f6f7f9;
        //  z-index:3;
        //  border-bottom:1px solid #ddd;
        //}

        ///* fila de encabezados (títulos) arriba de todo */
        //thead tr.headers th{
        //  top:0;
        //  font-weight:600;
        //  text-align:left;
        //  cursor:pointer; user-select:none;
        //  position:sticky;
        //}

        ///* fila de filtros inmediatamente debajo de los títulos (dropdowns múltiple) */
        //thead tr.filters th{
        //  top:var(--head-h);
        //  z-index:2;
        //  background:#f5f7fb;
        //  border-bottom:1px solid #ddd;
        //}
        //thead tr.filters select {
        //  appearance: none;       /* oculta flechas nativas */
        //  -webkit-appearance: none;
        //  -moz-appearance: none;
        //  background-image: none; /* sin ícono por defecto */
        //  padding: 4px;
        //}

        ///* Indicadores de orden */
        //thead tr.headers th.sort-asc::after{content:'▲'; position:absolute; right:8px; color:#666; font-size:10px;}
        //thead tr.headers th.sort-desc::after{content:'▼'; position:absolute; right:8px; color:#666; font-size:10px;}

        //tr:nth-child(even){background:#fbfbfb;}
        //.box{border:1px solid #d0d0d0;background:#fff;border-radius:4px;padding:6px;overflow-wrap:anywhere;}
        //a{color:#0067c0;text-decoration:none;} a:hover{text-decoration:underline;}
        //");
        //            sb.AppendLine("</style></head><body>");

        //            sb.AppendLine($"<div class='title'>{WebUtility.HtmlEncode(titulo)}</div>");

        //            // ====== RESUMEN ======
        //            sb.AppendLine("<div class='summary'>");
        //            sb.AppendLine("<div><b>Rows exported:</b> " + rowCount + "</div>");
        //            sb.AppendLine("<div><b>Visible columns:</b> " + visibleCols.Count + "</div>");
        //            sb.AppendLine("<div><b>Sort:</b> " + WebUtility.HtmlEncode(sortInfo) + "</div>");
        //            sb.AppendLine("<div><b>Filters:</b> " + WebUtility.HtmlEncode(filtInfo) + "</div>");
        //            sb.AppendLine("<div style='margin-top:4px;color:#666;font-size:12px;'>Exported at " +
        //                          DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "</div>");
        //            sb.AppendLine("</div>");

        //            // ====== TOOLBAR ======
        //            sb.AppendLine("<div class='toolbar'>");
        //            sb.AppendLine("<button id='btnClearFilters' title='Restablecer todos los filtros'>&#x2715; Limpiar filtros</button>");
        //            sb.AppendLine("</div>");

        //            // ====== TABLA ======
        //            sb.AppendLine("<div class='table-wrap'>");
        //            sb.AppendLine("<table>");
        //            sb.AppendLine("<thead>");

        //            // Encabezados (clickeables para ordenar)
        //            sb.AppendLine("<tr class='headers'>");
        //            foreach (var col in visibleCols)
        //            {
        //                var header = (col.HeaderText ?? col.MappingName ?? "Column");
        //                sb.AppendLine("<th>" + WebUtility.HtmlEncode(header) + "</th>");
        //            }
        //            sb.AppendLine("</tr>");

        //            // Fila de filtros -> celdas vacías; JS insertará <select multiple>
        //            sb.AppendLine("<tr class='filters'>");
        //            foreach (var _ in visibleCols)
        //                sb.AppendLine("<th></th>");
        //            sb.AppendLine("</tr>");

        //            sb.AppendLine("</thead>");

        //            // Filas (respeta filtros/orden del grid ya aplicados)
        //            sb.AppendLine("<tbody>");
        //            foreach (var rec in grid.View.Records)
        //            {
        //                var data = rec.Data;
        //                sb.AppendLine("<tr>");

        //                foreach (var col in visibleCols)
        //                {
        //                    string mapping = col.MappingName ?? col.HeaderText ?? string.Empty;
        //                    object? valueObj = GetPropertyValue(data, mapping);
        //                    string raw = valueObj?.ToString() ?? string.Empty;
        //                    string encoded = WebUtility.HtmlEncode(raw);

        //                    bool isUrlColumn = mapping.Equals("Url", StringComparison.OrdinalIgnoreCase) ||
        //                                       mapping.Equals("URL", StringComparison.OrdinalIgnoreCase);
        //                    bool looksLikeUrl = raw.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
        //                                        raw.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

        //                    if ((isUrlColumn || looksLikeUrl) && Uri.IsWellFormedUriString(raw, UriKind.Absolute))
        //                    {
        //                        string shortText = ShortenUrlText(raw, 64);
        //                        sb.AppendLine("<td><div class='box'><a href='" + WebUtility.HtmlEncode(raw) +
        //                                      "' target='_blank' title='" + WebUtility.HtmlEncode(raw) + "'>" +
        //                                      WebUtility.HtmlEncode(shortText) + "</a></div></td>");
        //                    }
        //                    else
        //                    {
        //                        sb.AppendLine("<td><div class='box'>" + encoded + "</div></td>");
        //                    }
        //                }

        //                sb.AppendLine("</tr>");
        //            }
        //            sb.AppendLine("</tbody></table>");
        //            sb.AppendLine("</div>"); // .table-wrap

        //            // ====== SCRIPT: sticky, multiselección, limpiar filtros y ordenamiento ======
        //            sb.AppendLine(@"
        //<script>
        //(function(){
        //  function guessType(values){
        //    for (var i=0;i<values.length;i++){
        //      var v = (values[i]||'').toString().trim();
        //      if (!v) continue;
        //      var n = v.replace(/,/g,'');
        //      if (!isNaN(n) && n!=='') return 'number';
        //      var d = Date.parse(v);
        //      if (!isNaN(d)) return 'date';
        //      return 'string';
        //    }
        //    return 'string';
        //  }

        //  function compare(a, b, type, dir){
        //    var mul = (dir === 'desc') ? -1 : 1;
        //    if (type === 'number'){
        //      var na = parseFloat((a||'').replace(/,/g,'')) || 0;
        //      var nb = parseFloat((b||'').replace(/,/g,'')) || 0;
        //      return (na - nb) * mul;
        //    }
        //    if (type === 'date'){
        //      var da = Date.parse(a||'');
        //      var db = Date.parse(b||'');
        //      if (isNaN(da) && isNaN(db)) return 0;
        //      if (isNaN(da)) return -1 * mul;
        //      if (isNaN(db)) return  1 * mul;
        //      return (da - db) * mul;
        //    }
        //    return (a||'').localeCompare(b||'', undefined, { sensitivity: 'base' }) * mul;
        //  }

        //  var tbl = document.querySelector('table');
        //  if(!tbl) return;

        //  var thead = tbl.querySelector('thead'); if (!thead) return;
        //  var headerRow = thead.querySelector('tr.headers');
        //  var filterRow = thead.querySelector('tr.filters');
        //  var tbody = tbl.querySelector('tbody'); if(!tbody) return;

        //  // 1) Altura real de la fila de títulos -> --head-h
        //  if (headerRow){
        //    function setHeadHeight(){
        //      var h = headerRow.getBoundingClientRect().height;
        //      tbl.style.setProperty('--head-h', (h || 38) + 'px');
        //    }
        //    setHeadHeight();
        //    window.addEventListener('resize', setHeadHeight);
        //  }

        //  var allRows = Array.from(tbody.rows);
        //  var originalOrder = Array.from(allRows);

        //  // Helper: lee valores seleccionados (excluye '(Todos)' = '')
        //  function selectedValues(select){
        //    var vals = Array.from(select.selectedOptions).map(o => o.value);
        //    if (vals.length === 0) return [];          // sin selección -> no filtra
        //    if (vals.includes('')) return [];          // '(Todos)' seleccionado -> no filtra
        //    return vals;
        //  }

        //  // 2) Construir selects múltiples por columna
        //  var selects = [];
        //  if (filterRow){
        //    for (var colIndex = 0; colIndex < filterRow.cells.length; colIndex++){
        //      var cell = filterRow.cells[colIndex];
        //      if (!cell) continue;

        //      cell.innerHTML = '';
        //      var select = document.createElement('select');
        //      select.multiple = true;
        //      select.size = 1; // UX: compacto; se expandirá al enfocar
        //      select.style.minHeight = '24px';
        //      cell.appendChild(select);
        //      selects.push(select);

        //      // Recolectar valores únicos
        //      var setVals = new Set();
        //      var hasEmpty = false;
        //      allRows.forEach(function(tr){
        //        var text = (tr.children[colIndex]?.innerText || '').trim();
        //        if (text === '') hasEmpty = true; else setVals.add(text);
        //      });
        //      var vals = Array.from(setVals).sort(function(a,b){
        //        return a.localeCompare(b, undefined, { sensitivity:'base', numeric:true });
        //      });

        //      // Opciones
        //      var optAll = document.createElement('option'); // '(Todos)' => value:''
        //      optAll.value = '';
        //      optAll.textContent = '(Todos)';
        //      select.appendChild(optAll);

        //      if (hasEmpty){
        //        var optEmpty = document.createElement('option'); // '(Vacíos)' => sentinel
        //        optEmpty.value = '__EMPTY__';
        //        optEmpty.textContent = '(Vacíos)';
        //        select.appendChild(optEmpty);
        //      }

        //      vals.forEach(function(v){
        //        var opt = document.createElement('option');
        //        opt.value = v;
        //        opt.textContent = v;
        //        select.appendChild(opt);
        //      });

        //      // UX: expandir/colapsar al enfocar
        //      select.addEventListener('focus', function(){
        //        var maxSize = Math.min(this.options.length, 8);
        //        this.size = Math.max(4, maxSize);
        //      });
        //      select.addEventListener('blur', function(){ this.size = 1; });

        //      // Normalizar mezcla con '(Todos)': si selecciona '(Todos)' y otro, dejamos solo '(Todos)' (o lo ignoramos)
        //      select.addEventListener('change', function(){
        //        // si '(Todos)' está seleccionado junto a otros, quitamos '(Todos)'
        //        var hasAll = Array.from(this.selectedOptions).some(o => o.value === '');
        //        if (hasAll && this.selectedOptions.length > 1){
        //          // deseleccionar '(Todos)'
        //          Array.from(this.options).forEach(o => { if (o.value === '') o.selected = false; });
        //        }
        //        applyFilters();
        //      });
        //    }
        //  }

        //  // 3) Aplicar filtros (AND entre columnas; OR entre valores dentro de una columna)
        //  function applyFilters(){
        //    allRows.forEach(function(tr){
        //      var tds = tr.children;
        //      var visible = true;
        //      for (var i=0; i<selects.length; i++){
        //        var sel = selects[i];
        //        if (!sel) continue;
        //        var selVals = selectedValues(sel); // [] => no filtra esa columna
        //        if (selVals.length === 0) continue;

        //        var cellText = (tds[i]?.innerText || '').trim();
        //        var matches = false;

        //        // Soportar '(Vacíos)'
        //        if (selVals.includes('__EMPTY__') && cellText === '') matches = true;

        //        // Coincidencia exacta con cualquiera de los valores
        //        if (!matches){
        //          for (var k=0; k<selVals.length; k++){
        //            var v = selVals[k];
        //            if (v === '__EMPTY__') continue;
        //            if (cellText === v){ matches = true; break; }
        //          }
        //        }

        //        if (!matches){ visible = false; break; }
        //      }
        //      tr.style.display = visible ? '' : 'none';
        //    });
        //  }

        //  // 4) Botón Limpiar filtros
        //  var clearBtn = document.getElementById('btnClearFilters');
        //  if (clearBtn){
        //    clearBtn.addEventListener('click', function(){
        //      selects.forEach(function(sel){
        //        // limpiar selección
        //        Array.from(sel.options).forEach(o => o.selected = false);
        //        // seleccionar '(Todos)' implícito: ninguna selección = sin filtro
        //      });
        //      // restaurar visibilidad
        //      Array.from(tbody.rows).forEach(function(r){ r.style.display = ''; });
        //    });
        //  }

        //  // 5) Orden por click en encabezados
        //  if (headerRow){
        //    var ths = headerRow.querySelectorAll('th');
        //    ths.forEach(function(th, colIdx){
        //      th.addEventListener('click', function(){
        //        ths.forEach(function(x){ if (x !== th) x.classList.remove('sort-asc','sort-desc'); });

        //        var current = th.classList.contains('sort-asc') ? 'asc' :
        //                      th.classList.contains('sort-desc') ? 'desc' : null;

        //        var next = current === 'asc' ? 'desc' : (current === 'desc' ? null : 'asc');

        //        if (!next){
        //          th.classList.remove('sort-asc','sort-desc');
        //          originalOrder.forEach(function(tr){ tbody.appendChild(tr); });
        //          return;
        //        }

        //        th.classList.toggle('sort-asc',  next === 'asc');
        //        th.classList.toggle('sort-desc', next === 'desc');

        //        var rowsVisible = Array.from(tbody.rows).filter(function(r){ return r.style.display !== 'none'; });
        //        var values = rowsVisible.map(function(r){
        //          var td = r.children[colIdx];
        //          return (td ? td.innerText : '') || '';
        //        });

        //        var type = guessType(values);
        //        rowsVisible.sort(function(r1, r2){
        //          var a = r1.children[colIdx]?.innerText || '';
        //          var b = r2.children[colIdx]?.innerText || '';
        //          return compare(a, b, type, next);
        //        });

        //        var rowsHidden = Array.from(tbody.rows).filter(function(r){ return r.style.display === 'none'; });
        //        rowsVisible.concat(rowsHidden).forEach(function(r){ tbody.appendChild(r); });
        //      });
        //    });
        //  }
        //})();
        //</script>");

        //            // ====== FIN ======
        //            sb.AppendLine("</body></html>");

        //            // (opcional) resaltado de búsqueda existente en tu app
        //            string finalHtml = sb.ToString();
        //            if (Helpers.searchTermExists)
        //                finalHtml = HighlightSearchTerms(finalHtml, Helpers.searchTerm, Helpers.searchTermRegExp);

        //            return finalHtml;
        //        }






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
                return url.Length > maxLen ? url[..(maxLen - 1)] + "…" : url;
            }
        }


        private string GetSortSummary(Syncfusion.WinForms.DataGrid.SfDataGrid grid)
        {
            try
            {
                if (grid.SortColumnDescriptions == null || grid.SortColumnDescriptions.Count == 0)
                    return "None";

                var parts = new List<string>();
                foreach (SortColumnDescription scd in grid.SortColumnDescriptions)
                {
                    var dir = scd.SortDirection == ListSortDirection.Ascending ? "ASC" : "DESC";
                    parts.Add($"{scd.ColumnName} {dir}");
                }
                return string.Join(", ", parts);
            }
            catch
            {
                return "None";
            }
        }

        private string GetFilterSummary(Syncfusion.WinForms.DataGrid.SfDataGrid grid)
        {
            // 1) BindingSource.Filter
            if (grid.DataSource is BindingSource bs && !string.IsNullOrWhiteSpace(bs.Filter))
                return bs.Filter;

            // 2) DataView.RowFilter (DataTable/Views)
            if (grid.DataSource is DataView dv && !string.IsNullOrWhiteSpace(dv.RowFilter))
                return dv.RowFilter;

            if (grid.DataSource is DataTable dt && !string.IsNullOrWhiteSpace(dt.DefaultView?.RowFilter))
                return dt.DefaultView.RowFilter;

            // 3) Intento de leer predicados internos (si tu proyecto usa filtros de Syncfusion)
            try
            {
                // Muchas implementaciones exponen grid.View.FilterPredicates (no siempre público).
                var view = grid.View;
                if (view != null)
                {
                    var prop = view.GetType().GetProperty("FilterPredicates");
                    var fp = prop?.GetValue(view) as System.Collections.IEnumerable;
                    if (fp != null)
                    {
                        var parts = new List<string>();
                        foreach (var item in fp)
                        {
                            // Usamos reflexión por compatibilidad de versiones
                            string col = item.GetType().GetProperty("ColumnName")?.GetValue(item)?.ToString() ?? "?";
                            string type = item.GetType().GetProperty("FilterType")?.GetValue(item)?.ToString() ?? "";
                            string pred = item.GetType().GetProperty("FilterBehavior")?.GetValue(item)?.ToString()
                                           ?? item.GetType().GetProperty("PredicateType")?.GetValue(item)?.ToString()
                                           ?? "";
                            string value = item.GetType().GetProperty("FilterValue")?.GetValue(item)?.ToString() ?? "";
                            parts.Add($"{col} {type} {pred} {value}".Trim());
                        }
                        if (parts.Count > 0) return string.Join(" ; ", parts);
                    }
                }
            }
            catch { /* ignorar, depende de versión */ }

            return "None";
        }


        private object? GetPropertyValue(object data, string mappingName)
        {
            if (data == null || string.IsNullOrEmpty(mappingName))
                return null;

            // DataRowView (DataTable)
            if (data is DataRowView drv)
                return drv.Row.Table.Columns.Contains(mappingName) ? drv[mappingName] : null;

            // POCO / propiedades anidadas A.B.C
            try
            {
                object? current = data;
                foreach (var part in mappingName.Split('.'))
                {
                    if (current == null) return null;
                    var pi = TypeDescriptor.GetProperties(current)[part];
                    if (pi == null) return null;
                    current = pi.GetValue(current);
                }
                return current;
            }
            catch { return null; }
        }




        

        private string BeautifyColumnName(string columnKey, out string iconBase64)
        {
            // Normaliza para comparar sin problemas
            string key = (columnKey ?? string.Empty).Trim();

            // Diccionario de íconos por clave (case-insensitive)
            var iconMap = new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase)
            {
                ["id"] = Resource1.id_icon,
                ["browser"] = Resource1.browser_icon,
                ["category"] = Resource1.category_icon,
                ["potential_activity"] = Resource1.activity_icon,
                ["visit_id"] = Resource1.visit_icon,
                ["url"] = Resource1.url_icon,
                ["title"] = Resource1.title_icon,
                ["visit_time"] = Resource1.clock_icon,
                ["last_visit_time"] = Resource1.clock_icon,
                ["visit_duration"] = Resource1.clock_icon,
                ["start_time"] = Resource1.clock_icon,
                ["end_time"] = Resource1.clock_icon,
                ["dateadded"] = Resource1.clock_icon,
                ["lastmodified"] = Resource1.clock_icon,
                ["visit_count"] = Resource1.count_icon,
                ["typed_count"] = Resource1.count_icon,
                ["file"] = Resource1.file_icon,
                ["label"] = Resource1.label_icon,
                ["comment"] = Resource1.comment_icon
            };

            // Etiquetas bonitas (puedes ampliar)
            var prettyMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["id"] = "ID",
                ["browser"] = "Browser",
                ["category"] = "Category",
                ["potential_activity"] = "Potential Activity",
                ["visit_id"] = "Visit ID",
                ["url"] = "URL",
                ["title"] = "Title",
                ["visit_time"] = "Visit Time",
                ["last_visit_time"] = "Last Visit Time",
                ["visit_duration"] = "Visit Duration",
                ["start_time"] = "Start Time",
                ["end_time"] = "End Time",
                ["dateadded"] = "Date Added",
                ["lastmodified"] = "Last Modified",
                ["visit_count"] = "Visit Count",
                ["typed_count"] = "Typed Count",
                ["file"] = "File",
                ["label"] = "Label",
                ["comment"] = "Comment"
            };

            // Ícono
            if (!iconMap.TryGetValue(key, out Image img) || img == null)
                img = Resource1.generic_icon; // fallback genérico

            iconBase64 = ImageToBase64(img);

            // Título
            if (!prettyMap.TryGetValue(key, out string pretty))
                pretty = columnKey; // tal cual si no hay mapeo

            return pretty;
        }

        private string ImageToBase64(Image image)
        {
            if (image == null) return "";
            using (var ms = new MemoryStream())
            {
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        private void button_Report_Click(object sender, EventArgs e)
        {
            OpenReports();
        }

        private void OpenReports()
        {
            Form openForm = Application.OpenForms.OfType<Form_Report>().FirstOrDefault();

            if (openForm != null)
            {
                openForm.BringToFront();  // Si ya está abierto, lo trae al frente
            }
            else
            {
                Form_Report frmReports = new Form_Report();
                frmReports.Show();
            }
        }

    }

}
