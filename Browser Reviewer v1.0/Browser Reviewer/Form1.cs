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
using Microsoft.Web.WebView2.WinForms;






namespace Browser_Reviewer

{
    public partial class Form1 : Form
    {
        private MyTools Tools;

        private ContextMenuStrip labelContextMenu = null!;

        private PictureBox cachePreviewPictureBox = null!;

        private WebView2 cachePreviewWebView = null!;

        private TableLayoutPanel textBoxLayout = null!;

        private bool cachePreviewLayoutActive;

        private string cachePreviewTempFile = "";

        private bool startupDialogOpen;

        private readonly StartupRequest initialStartupRequest = new StartupRequest();

        private CheckBox? checkBox_Labels;




        public Form1()
        {
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JFaF5cXGRCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdlWXtfdnZXRmBeV0B0WkpWYEg=");
            InitializeComponent();
            AppIcon.Apply(this);
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
            if (Helpers.searchTermExists && Helpers.searchTermRegExp && GridCellMatchesSearch(e))
            {
                e.Style.BackColor = Color.Yellow;
                e.Style.TextColor = Color.Black;
                return;
            }

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





        private void setLabels()
        {
            using (SQLiteConnection connection = new SQLiteConnection(Helpers.chromeViewerConnectionString))
            {
                connection.Open();

                Helpers.dataAdapter = new SQLiteDataAdapter("SELECT Label_name, Label_color FROM Labels", connection);
                SQLiteCommandBuilder builder = new SQLiteCommandBuilder(Helpers.dataAdapter);

                Helpers.labelsTable = new DataTable();
                Helpers.dataAdapter.Fill(Helpers.labelsTable);

            }
        }






        private void Form1_Load(object sender, EventArgs e)
        {

            SetupContextMenu();

            SetupRichTextBoxContextMenu();






            this.SuspendLayout();

            groupBox_customSearch.AutoSize = false;
            groupBox_customSearch.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            groupBox_customSearch.Padding = new Padding(0);





            var tableLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 14,
                RowCount = 2,
                Margin = new Padding(0),
                Padding = new Padding(0),
                AutoSize = false,
                GrowStyle = TableLayoutPanelGrowStyle.FixedSize
            };

            for (int i = 0; i < 4; i++)
                tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 5f));

            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 43f));

            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 5f));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 5f));

            for (int i = 7; i < 14; i++)
                tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 3.857f));

            tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));






            button_SearchWebActivity.Dock = DockStyle.Fill;
            button_SearchWebActivity.Margin = new Padding(0);
            button_SearchWebActivity.AutoSize = false;

            button_LabelManager.Dock = DockStyle.Fill;
            button_LabelManager.Margin = new Padding(0);
            button_LabelManager.AutoSize = false;

            tableLayout.Controls.Add(button_SearchWebActivity, 0, 0);
            tableLayout.SetRowSpan(button_SearchWebActivity, 2);

            tableLayout.Controls.Add(button_LabelManager, 1, 0);
            tableLayout.SetRowSpan(button_LabelManager, 2);


            label_UTC_Time.Dock = DockStyle.Fill;
            label_UTC_Time.TextAlign = ContentAlignment.MiddleLeft;

            tableLayout.Controls.Add(label_UTC_Time, 2, 0);

            tableLayout.SetColumnSpan(label_UTC_Time, 2);



            numericUpDown1.Dock = DockStyle.Fill;
            numericUpDown1.TextAlign = HorizontalAlignment.Center;

            tableLayout.Controls.Add(numericUpDown1, 2, 1);



            labelStatus.Dock = DockStyle.Fill;
            labelStatus.TextAlign = ContentAlignment.MiddleLeft;

            tableLayout.Controls.Add(labelStatus, 4, 0);


            labelItemCount.Dock = DockStyle.Fill;
            labelItemCount.TextAlign = ContentAlignment.MiddleLeft;

            tableLayout.Controls.Add(labelItemCount, 4, 1);

            var labelTimeline = new Label
            {
                Dock = DockStyle.Fill,
                Font = label_startDate.Font,
                ForeColor = label_startDate.ForeColor,
                Text = "Time Line",
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand
            };
            labelTimeline.Click += (sender, e) => ExportCurrentGridTimelineHtml();
            labelTimeline.MouseHover += (sender, e) => labelTimeline.ForeColor = Color.SteelBlue;
            labelTimeline.MouseLeave += (sender, e) => labelTimeline.ForeColor = label_startDate.ForeColor;
            tableLayout.Controls.Add(labelTimeline, 5, 0);

            label_startDate.Dock = DockStyle.Fill;
            label_startDate.TextAlign = ContentAlignment.MiddleLeft;

            tableLayout.Controls.Add(label_startDate, 6, 0);

            label_endDate.Dock = DockStyle.Fill;
            label_endDate.TextAlign = ContentAlignment.MiddleLeft;

            tableLayout.Controls.Add(label_endDate, 6, 1);


            dateTimePicker_start.Dock = DockStyle.Fill;
            dateTimePicker_start.Format = DateTimePickerFormat.Short;

            tableLayout.Controls.Add(dateTimePicker_start, 7, 0);

            tableLayout.SetColumnSpan(dateTimePicker_start, 2);


            dateTimePicker_end.Dock = DockStyle.Fill;
            dateTimePicker_end.Format = DateTimePickerFormat.Short;

            tableLayout.Controls.Add(dateTimePicker_end, 7, 1);

            tableLayout.SetColumnSpan(dateTimePicker_end, 2);


            checkBox_enableTime.Dock = DockStyle.Fill;
            checkBox_enableTime.TextAlign = ContentAlignment.MiddleLeft;

            tableLayout.Controls.Add(checkBox_enableTime, 9, 0);


            checkBox_RegExp.Dock = DockStyle.Fill;
            checkBox_RegExp.TextAlign = ContentAlignment.MiddleLeft;

            tableLayout.Controls.Add(checkBox_RegExp, 10, 0);

            checkBox_Labels = new CheckBox
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Text = "Labels",
                TextAlign = ContentAlignment.MiddleLeft,
                UseVisualStyleBackColor = true
            };

            tableLayout.Controls.Add(checkBox_Labels, 11, 0);



            search_textBox.Dock = DockStyle.Fill;

            tableLayout.Controls.Add(search_textBox, 9, 1);

            tableLayout.SetColumnSpan(search_textBox, 3);



            searchBtn.Dock = DockStyle.Fill;

            tableLayout.Controls.Add(searchBtn, 12, 1);


            clearsearchBtn.Dock = DockStyle.Fill;

            tableLayout.Controls.Add(clearsearchBtn, 13, 1);





            groupBox_customSearch.Controls.Clear();
            groupBox_customSearch.Controls.Add(tableLayout);









            int topOffset = groupBox_customSearch.Bottom + 6;

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(4, topOffset, 4, 4)
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));

            var leftPanel = new Panel { Dock = DockStyle.Fill };
            groupBox_Main.AutoSize = false;
            groupBox_Main.Dock = DockStyle.Fill;
            leftPanel.Controls.Add(groupBox_Main);

            sfDataGrid1.Dock = DockStyle.Fill;

            var rightPanel = new Panel { Dock = DockStyle.Fill };
            groupBox_TextBox.AutoSize = false;
            groupBox_TextBox.Dock = DockStyle.Fill;
            groupBox_TextBox.Controls.Clear();

            textBoxLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(6),
                Margin = new Padding(0)
            };
            textBoxLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            textBoxLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            textBoxLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));
            textBoxLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            textBoxLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
            textBoxLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            richTextBox1.BorderStyle = BorderStyle.FixedSingle;
            richTextBox1.Dock = DockStyle.Fill;
            richTextBox1.Margin = new Padding(0, 0, 0, 4);
            richTextBox1.ScrollBars = RichTextBoxScrollBars.Vertical;
            textBoxLayout.Controls.Add(richTextBox1, 0, 0);

            cachePreviewPictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 4, 0, 8),
                Visible = false
            };
            textBoxLayout.Controls.Add(cachePreviewPictureBox, 0, 1);

            cachePreviewWebView = new WebView2
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 4, 0, 8),
                Visible = false
            };
            textBoxLayout.Controls.Add(cachePreviewWebView, 0, 1);


            var buttonsLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 4,
                RowCount = 1,
                AutoSize = true,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            buttonsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            buttonsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            buttonsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            buttonsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            void Normalize(Button b)
            {
                b.AutoSize = false;
                b.Dock = DockStyle.Fill;
                b.Height = 32;
                b.MinimumSize = new Size(0, 32);
                b.Margin = new Padding(3);
                b.FlatStyle = FlatStyle.System;
                b.UseVisualStyleBackColor = true;
                b.Font = button_exportPDF.Font;
            }

            Normalize(button_Font);
            Normalize(button_exportPDF);
            Normalize(button_exportHTML);
            Normalize(button_Report);

            buttonsLayout.Controls.Add(button_Font, 0, 0);
            buttonsLayout.Controls.Add(button_exportPDF, 1, 0);
            buttonsLayout.Controls.Add(button_exportHTML, 2, 0);
            buttonsLayout.Controls.Add(button_Report, 3, 0);


            textBoxLayout.Controls.Add(buttonsLayout, 0, 2);






            Console.Multiline = true;
            Console.ReadOnly = true;
            Console.Dock = DockStyle.Fill;
            Console.Margin = new Padding(0, 4, 0, 0);
            textBoxLayout.Controls.Add(Console, 0, 3);

            var footerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 1,
                AutoSize = true,
                Margin = new Padding(0, 6, 0, 0),
                Padding = new Padding(0)
            };
            footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78f));

            autoLabel1.Dock = DockStyle.Fill;
            autoLabel1.Margin = new Padding(0, 0, 6, 0);

            progressBarProcessing.Dock = DockStyle.Fill;
            progressBarProcessing.Margin = new Padding(0);
            progressBarProcessing.Height = 16;

            footerLayout.Controls.Add(autoLabel1, 0, 0);
            footerLayout.Controls.Add(progressBarProcessing, 1, 0);
            textBoxLayout.Controls.Add(footerLayout, 0, 4);

            groupBox_TextBox.Controls.Add(textBoxLayout);
            rightPanel.Controls.Add(groupBox_TextBox);

            mainLayout.Controls.Add(leftPanel, 0, 0);
            mainLayout.Controls.Add(sfDataGrid1, 1, 0);
            mainLayout.Controls.Add(rightPanel, 2, 0);

            this.Controls.Add(mainLayout);

            menuStrip1.Dock = DockStyle.Top;
            this.Controls.SetChildIndex(menuStrip1, 0);

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

            disableButtons();
            BeginInvoke(new System.Action(async () =>
            {
                if (initialStartupRequest.Action == StartupAction.None)
                    await ShowStartupDialogAsync();
                else
                    await ProcessStartupRequestAsync(initialStartupRequest);
            }));














        }


        private void DrawPdfCoverPage(PdfDocument document, IList<GridColumn> visibleCols, int rowCount, string sortInfo, string filtInfo)
        {
            var page = document.Pages.Add();
            var g = page.Graphics;
            float margin = 36;
            float width = page.GetClientSize().Width - (margin * 2);
            float y = 162;

            var navy = new PdfSolidBrush(new PdfColor(24, 36, 52));
            var blue = new PdfSolidBrush(new PdfColor(54, 104, 176));
            var pale = new PdfSolidBrush(new PdfColor(244, 247, 251));
            var muted = new PdfSolidBrush(new PdfColor(92, 102, 114));

            g.DrawRectangle(navy, new RectangleF(0, 0, page.GetClientSize().Width, 132));
            g.DrawRectangle(blue, new RectangleF(0, 128, page.GetClientSize().Width, 5));

            g.DrawString("BROWSER REVIEWER", new PdfStandardFont(PdfFontFamily.Helvetica, 9, PdfFontStyle.Bold), PdfBrushes.White, new RectangleF(margin, 34, width, 14));
            g.DrawString("Browser Artifact Review Report", new PdfStandardFont(PdfFontFamily.Helvetica, 23, PdfFontStyle.Bold), PdfBrushes.White, new RectangleF(margin, 52, width, 32));
            g.DrawString("Focused export of visible records, filters, analyst notes, and source evidence.", new PdfStandardFont(PdfFontFamily.Helvetica, 10), new PdfSolidBrush(new PdfColor(216, 226, 238)), new RectangleF(margin, 88, width, 18));

            float cardW = (width - 20) / 3;
            DrawPdfMetricCard(g, margin, y, cardW, "Records", rowCount.ToString());
            DrawPdfMetricCard(g, margin + cardW + 10, y, cardW, "Visible columns", visibleCols.Count.ToString());
            DrawPdfMetricCard(g, margin + (cardW + 10) * 2, y, cardW, "Time zone", $"UTC{(Helpers.utcOffset >= 0 ? "+" : "")}{Helpers.utcOffset}");
            y += 92;

            DrawPdfSectionTitle(g, margin, y, width, "Report Context");
            y += 24;

            var summary = new DataTable();
            summary.Columns.Add("Field");
            summary.Columns.Add("Value");
            summary.Rows.Add("Case database", Helpers.db_name);
            summary.Rows.Add("Exported at", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            summary.Rows.Add("Sort", sortInfo);
            summary.Rows.Add("Filters", filtInfo);
            summary.Rows.Add("Columns", string.Join(", ", visibleCols.Select(c => c.HeaderText ?? c.MappingName ?? "Column")));

            var sumGrid = BuildPdfReviewGrid(summary, 0.26f, 0.74f, width, true, new PdfColor(238, 243, 250));
            var result = sumGrid.Draw(page, new PointF(margin, y));
            y = result.Bounds.Bottom + 26;

            DrawPdfSectionTitle(g, margin, y, width, "How To Read This Report");
            y += 24;
            g.DrawRectangle(pale, new RectangleF(margin, y, width, 76));
            string note = "Each following page is one exported record. The top band summarizes the artifact, browser, time, and record id. Highlighted sections separate review context, analyst notes, evidence pointers, and the complete visible fields used to produce the report.";
            g.DrawString(note, new PdfStandardFont(PdfFontFamily.Helvetica, 10), muted, new RectangleF(margin + 12, y + 12, width - 24, 54));

            g.DrawString("Generated for review. Validate important findings against the original .bre database and source artifact paths.", new PdfStandardFont(PdfFontFamily.Helvetica, 10), muted, new RectangleF(margin, page.GetClientSize().Height - 48, width, 18));
        }

        private void DrawPdfMetricCard(PdfGraphics g, float x, float y, float width, string label, string value)
        {
            var bg = new PdfSolidBrush(new PdfColor(247, 249, 252));
            var border = new PdfPen(new PdfColor(218, 224, 232), 0.8f);
            g.DrawRectangle(border, bg, new RectangleF(x, y, width, 68));
            g.DrawString(label.ToUpperInvariant(), new PdfStandardFont(PdfFontFamily.Helvetica, 8, PdfFontStyle.Bold), new PdfSolidBrush(new PdfColor(92, 102, 114)), new RectangleF(x + 12, y + 11, width - 24, 12));
            g.DrawString(value, new PdfStandardFont(PdfFontFamily.Helvetica, 18, PdfFontStyle.Bold), new PdfSolidBrush(new PdfColor(24, 36, 52)), new RectangleF(x + 12, y + 30, width - 24, 24));
        }

        private void DrawPdfSectionTitle(PdfGraphics g, float x, float y, float width, string title)
        {
            g.DrawString(title.ToUpperInvariant(), new PdfStandardFont(PdfFontFamily.Helvetica, 10, PdfFontStyle.Bold), new PdfSolidBrush(new PdfColor(24, 36, 52)), new RectangleF(x, y, width, 14));
            g.DrawLine(new PdfPen(new PdfColor(199, 210, 224), 0.8f), new PointF(x, y + 17), new PointF(x + width, y + 17));
        }

        private void DrawReviewRecordPage(PdfDocument document, DataRowView row, IList<GridColumn> visibleCols, int recordNumber, int totalRecords)
        {
            var page = document.Pages.Add();
            var g = page.Graphics;
            float margin = 36;
            float width = page.GetClientSize().Width - (margin * 2);
            float y = 60;

            string id = PdfFirstValue(row, "id", "Source_id");
            string artifact = PdfFirstValue(row, "Artifact_type", "Artifact", "Type");
            string browser = PdfFirstValue(row, "Browser");
            string activity = PdfFirstValue(row, "Potential_activity", "Potential Activity", "Activity", "Category");
            string time = PdfFirstValue(row, "Activity_time", "Visit_time", "LastAccessed", "Created", "Modified", "End_time", "Start_time", "DateAdded", "LastModified", "LastUsed", "FirstUsed", "InstallTime", "LastUpdateTime");
            string title = PdfFirstValue(row, "Title", "Name", "FieldName", "DetectedFileType", "ContentType", "Current_path", "Target_path");
            string url = PdfFirstValue(row, "Url", "URL", "Source_url", "Site_url", "Tab_url", "HomepageUrl", "Origin", "Host", "Url_chain");
            string file = PdfFirstValue(row, "File", "Source_file", "CacheFile", "SessionFile", "ExtensionPath", "Current_path", "Target_path");
            string label = PdfFirstValue(row, "Label");
            string comment = PdfFirstValue(row, "Comment");

            var titleFont = new PdfStandardFont(PdfFontFamily.Helvetica, 15, PdfFontStyle.Bold);
            var smallFont = new PdfStandardFont(PdfFontFamily.Helvetica, 9);
            var chipFont = new PdfStandardFont(PdfFontFamily.Helvetica, 9, PdfFontStyle.Bold);
            var mutedBrush = new PdfSolidBrush(new PdfColor(90, 90, 90));
            var darkBrush = new PdfSolidBrush(new PdfColor(24, 30, 38));

            string heading = $"Record {recordNumber} of {totalRecords}";
            if (!string.IsNullOrWhiteSpace(id))
                heading += $"    ID {id}";
            g.DrawRectangle(new PdfSolidBrush(new PdfColor(24, 36, 52)), new RectangleF(margin, y, width, 72));
            g.DrawRectangle(new PdfSolidBrush(new PdfColor(54, 104, 176)), new RectangleF(margin, y + 68, width, 4));
            g.DrawString(heading, titleFont, PdfBrushes.White, new RectangleF(margin + 14, y + 12, width - 28, 20));

            string subtitle = string.Join("   |   ", new[] { artifact, browser, time }.Where(v => !string.IsNullOrWhiteSpace(v)));
            g.DrawString(string.IsNullOrWhiteSpace(subtitle) ? "Browser artifact" : subtitle, smallFont, new PdfSolidBrush(new PdfColor(218, 226, 235)), new RectangleF(margin + 14, y + 40, width - 28, 16));
            y += 88;

            DrawPdfSectionTitle(g, margin, y, width, "Review Focus");
            y += 24;
            var overview = new DataTable();
            overview.Columns.Add("Review field");
            overview.Columns.Add("Value");
            overview.Rows.Add("Potential Activity", PdfFallback(activity));
            overview.Rows.Add("Title / Name", PdfFallback(title));
            overview.Rows.Add("Browser", PdfFallback(browser));
            overview.Rows.Add("Primary time", PdfFallback(time));

            var overviewGrid = BuildPdfReviewGrid(overview, 0.28f, 0.72f, width, true);
            var overviewResult = overviewGrid.Draw(page, new PointF(margin, y));
            y = overviewResult.Bounds.Bottom + 12;

            if (!string.IsNullOrWhiteSpace(label) || !string.IsNullOrWhiteSpace(comment))
            {
                DrawPdfSectionTitle(g, margin, y, width, "Analyst Notes");
                y += 24;
                var notes = new DataTable();
                notes.Columns.Add("Analyst notes");
                notes.Columns.Add("Value");
                if (!string.IsNullOrWhiteSpace(label)) notes.Rows.Add("Label", label);
                if (!string.IsNullOrWhiteSpace(comment)) notes.Rows.Add("Comment", comment);
                var notesGrid = BuildPdfReviewGrid(notes, 0.22f, 0.78f, width, true, new PdfColor(255, 250, 232));
                var notesResult = notesGrid.Draw(page, new PointF(margin, y));
                y = notesResult.Bounds.Bottom + 12;
            }

            var evidence = new DataTable();
            evidence.Columns.Add("Evidence");
            evidence.Columns.Add("Value");
            if (!string.IsNullOrWhiteSpace(url)) evidence.Rows.Add("URL / Origin", url);
            if (!string.IsNullOrWhiteSpace(file)) evidence.Rows.Add("Source file", file);
            if (evidence.Rows.Count > 0)
            {
                DrawPdfSectionTitle(g, margin, y, width, "Evidence Pointers");
                y += 24;
                var evidenceGrid = BuildPdfReviewGrid(evidence, 0.22f, 0.78f, width, true, new PdfColor(241, 247, 255));
                var evidenceResult = evidenceGrid.Draw(page, new PointF(margin, y));
                y = evidenceResult.Bounds.Bottom + 12;
            }

            var details = BuildPdfDetailsTable(row, visibleCols);
            if (details.Rows.Count > 0)
            {
                DrawPdfSectionTitle(g, margin, y, width, "Complete Visible Fields");
                y += 24;

                var detailsGrid = BuildPdfReviewGrid(details, 0.30f, 0.70f, width, false);
                detailsGrid.Draw(page, new PointF(margin, y));
            }
        }

        private PdfGrid BuildPdfReviewGrid(DataTable table, float fieldWidth, float valueWidth, float clientWidth, bool emphasizeHeader, PdfColor? headerColor = null)
        {
            var grid = new PdfGrid { DataSource = table };
            grid.Style.CellPadding = new PdfPaddings(6, 5, 6, 5);

            var headerBrush = new PdfSolidBrush(headerColor ?? new PdfColor(246, 247, 249));
            foreach (PdfGridRow h in grid.Headers)
            {
                foreach (PdfGridCell cell in h.Cells)
                {
                    cell.Style = new PdfGridCellStyle
                    {
                        BackgroundBrush = headerBrush,
                        Font = new PdfStandardFont(PdfFontFamily.Helvetica, 9, PdfFontStyle.Bold),
                        Borders = new PdfBorders { All = new PdfPen(new PdfColor(210, 210, 210), 0.8f) }
                    };
                }
            }

            grid.Rows.ApplyStyle(new PdfGridCellStyle
            {
                Font = new PdfStandardFont(PdfFontFamily.Helvetica, 9),
                Borders = new PdfBorders { All = new PdfPen(new PdfColor(224, 224, 224), 0.6f) }
            });

            for (int i = 0; i < grid.Rows.Count; i++)
            {
                grid.Rows[i].Cells[0].Style.Font = new PdfStandardFont(PdfFontFamily.Helvetica, 9, emphasizeHeader ? PdfFontStyle.Bold : PdfFontStyle.Regular);
                grid.Rows[i].Cells[0].Style.TextBrush = new PdfSolidBrush(new PdfColor(75, 75, 75));
            }

            grid.Columns[0].Width = clientWidth * fieldWidth;
            grid.Columns[1].Width = clientWidth * valueWidth;
            return grid;
        }

        private DataTable BuildPdfDetailsTable(DataRowView row, IList<GridColumn> visibleCols)
        {
            var table = new DataTable();
            table.Columns.Add("Field");
            table.Columns.Add("Value");

            var priority = new[]
            {
                "id", "Source_id", "Source_table", "Artifact_type", "Artifact", "Potential_activity",
                "Browser", "Category", "Visit_time", "Activity_time", "Start_time", "End_time",
                "Created", "Modified", "LastAccessed", "Title", "Name", "Url", "URL",
                "Host", "File", "Source_file", "Label", "Comment"
            };

            var byMapping = visibleCols
                .Select(c => new { Column = c, Mapping = c.MappingName ?? c.HeaderText ?? string.Empty })
                .Where(x => !string.IsNullOrWhiteSpace(x.Mapping))
                .GroupBy(x => x.Mapping, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Column, StringComparer.OrdinalIgnoreCase);

            var ordered = new List<GridColumn>();
            foreach (string key in priority)
            {
                if (byMapping.TryGetValue(key, out var col))
                    ordered.Add(col);
            }

            ordered.AddRange(visibleCols.Where(c => !ordered.Contains(c)));

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var col in ordered)
            {
                string name = col.HeaderText ?? col.MappingName ?? "Column";
                string mapping = col.MappingName ?? col.HeaderText ?? name;
                if (!seen.Add(mapping))
                    continue;

                string value = PdfValue(row, mapping);
                if (string.IsNullOrWhiteSpace(value))
                    value = "(no data)";

                table.Rows.Add(name, value);
            }

            return table;
        }

        private string PdfFirstValue(DataRowView row, params string[] columns)
        {
            foreach (string column in columns)
            {
                string value = PdfValue(row, column);
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return string.Empty;
        }

        private string PdfValue(DataRowView row, string columnName)
        {
            if (row?.Row?.Table == null || string.IsNullOrWhiteSpace(columnName))
                return string.Empty;

            foreach (DataColumn column in row.Row.Table.Columns)
            {
                if (!string.Equals(column.ColumnName, columnName, StringComparison.OrdinalIgnoreCase))
                    continue;

                object value = row[column.ColumnName];
                return value == DBNull.Value || value == null ? string.Empty : value.ToString() ?? string.Empty;
            }

            return string.Empty;
        }

        private static string PdfFallback(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "(no data)" : value;
        }






        private void SetupRichTextBoxContextMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("Copy", null, (s, e) => richTextBox1.Copy());
            menu.Items.Add("Select All", null, (s, e) => richTextBox1.SelectAll());

            richTextBox1.ContextMenuStrip = menu;
        }

        private void SetupContextMenu()
        {
            labelContextMenu = new ContextMenuStrip();

            labelContextMenu.Opening += LabelContextMenu_Opening;

            sfDataGrid1.ContextMenuStrip = labelContextMenu;
        }

        private void LabelContextMenu_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            labelContextMenu.Items.Clear();

            if (CanGoToSelectedArtifact())
            {
                ToolStripMenuItem goToArtifactItem = new ToolStripMenuItem("Go to artifact");
                goToArtifactItem.Click += (s, ev) => GoToSelectedArtifact();
                labelContextMenu.Items.Add(goToArtifactItem);
                labelContextMenu.Items.Add(new ToolStripSeparator());
            }

            ToolStripMenuItem openLabelManagerItem = new ToolStripMenuItem("Open Label Manager");
            openLabelManagerItem.Click += (s, ev) => OpenLabelManager();
            labelContextMenu.Items.Add(openLabelManagerItem);


            var addLabelItem = new ToolStripMenuItem("Add Label");

            if (Helpers.labelsTable != null && Helpers.labelsTable.Rows.Count > 0)
            {
                foreach (System.Data.DataRow row in Helpers.labelsTable.Rows)
                {
                    string? labelName = row["Label_name"].ToString();
                    if (string.IsNullOrWhiteSpace(labelName))
                    {
                        continue;
                    }

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




            ToolStripMenuItem removeLabelItem = new ToolStripMenuItem("Remove Label");
            removeLabelItem.Click += (s, ev) => RemoverLabelAFilasSeleccionadas();
            labelContextMenu.Items.Add(removeLabelItem);




            ToolStripMenuItem addCommentItem = new ToolStripMenuItem("Add Comment");
            addCommentItem.Click += (s, ev) => OpenComments();
            labelContextMenu.Items.Add(addCommentItem);

            ToolStripMenuItem removeCommentItem = new ToolStripMenuItem("Remove Comment");
            removeCommentItem.Click += (s, ev) => RemoverComentarioAFilasSeleccionadas();
            labelContextMenu.Items.Add(removeCommentItem);


        }

        public Form1(StartupRequest startupRequest) : this()
        {
            initialStartupRequest = startupRequest;
        }

        private bool IsFullTimelineView()
        {
            return string.Equals(labelStatus.Text, "Full time line web activity", StringComparison.OrdinalIgnoreCase);
        }

        private DataRowView? GetSelectedDataRow()
        {
            return sfDataGrid1.SelectedItem as DataRowView
                ?? sfDataGrid1.SelectedItems.Cast<object>().OfType<DataRowView>().FirstOrDefault();
        }

        private bool CanGoToSelectedArtifact()
        {
            DataRowView? row = GetSelectedDataRow();
            if (row == null)
            {
                return false;
            }

            string sourceTable = ResolveSelectedArtifactSourceTable(row);
            if (!IsWritableArtifactTable(sourceTable))
            {
                return false;
            }

            return int.TryParse(GetRowValue(row, "Source_id", GetRowValue(row, "id")), out _);
        }

        private void GoToSelectedArtifact()
        {
            DataRowView? row = GetSelectedDataRow();
            if (row == null)
            {
                return;
            }

            string sourceTable = ResolveSelectedArtifactSourceTable(row);

            if (!int.TryParse(GetRowValue(row, "Source_id", GetRowValue(row, "id")), out int sourceId))
            {
                return;
            }

            string sqlquery = BuildArtifactDetailQuery(sourceTable, sourceId);
            if (string.IsNullOrWhiteSpace(sqlquery))
            {
                return;
            }

            Tools.ShowQueryOnDataGridView(sfDataGrid1, Helpers.chromeViewerConnectionString, sqlquery, labelItemCount, Console);
            labelStatus.Text = $"Artifact detail: {sourceTable}";
        }

        private string ResolveSelectedArtifactSourceTable(DataRowView row)
        {
            string sourceTable = GetRowValue(row, "Source_table");
            if (IsWritableArtifactTable(sourceTable))
            {
                return sourceTable;
            }

            sourceTable = ResolveTimelineSourceTable(GetRowValue(row, "Artifact"), GetRowValue(row, "Browser"));
            return IsWritableArtifactTable(sourceTable) ? sourceTable : string.Empty;
        }

        private string ResolveTimelineSourceTable(string artifact, string browser)
        {
            string artifactKey = artifact?.Trim().ToLowerInvariant() ?? "";

            return artifactKey switch
            {
                "history" => Helpers.HistoryTableForBrowser(browser),
                "download" => Helpers.DownloadTableForBrowser(browser),
                "bookmark" => Helpers.BookmarkTableForBrowser(browser),
                "autofill" => "autofill_data",
                "cookie" => "cookies_data",
                "cache" => "cache_data",
                "session" => "session_data",
                "extension" => "extension_data",
                "saved login" => "saved_logins_data",
                "login" => "saved_logins_data",
                "local storage" => "local_storage_data",
                "session storage" => "session_storage_data",
                "indexeddb" => "indexeddb_data",
                _ => ""
            };
        }

        private string BuildArtifactDetailQuery(string sourceTable, int sourceId)
        {
            return sourceTable switch
            {
                "results" => $@"
                    SELECT id, Artifact_type, Potential_activity, Browser, Category, Visit_id, Url, Title,
                           {SearchSql.DateExpr("Visit_time")} AS Visit_time,
                           {SearchSql.DateExpr("Last_visit_time")} AS Last_visit_time,
                           Visit_count, Typed_count, From_url, Transition, File, Label, Comment
                    FROM results
                    WHERE id = {sourceId};",
                "firefox_results" => $@"
                    SELECT id, Artifact_type, Potential_activity, Browser, Category, Visit_id, Place_id, Url, Title,
                           {SearchSql.DateExpr("Visit_time")} AS Visit_time,
                           {SearchSql.DateExpr("Last_visit_time")} AS Last_visit_time,
                           Visit_count, From_visit, Transition, Navigation_context, User_action_likelihood, Visit_type, Frecency, File, Label, Comment
                    FROM firefox_results
                    WHERE id = {sourceId};",
                "chrome_downloads" => $@"
                    SELECT id, Artifact_type, Potential_activity, Browser, Download_id, Current_path, Target_path, Url_chain,
                           {SearchSql.DateExpr("Start_time")} AS Start_time,
                           {SearchSql.DateExpr("End_time")} AS End_time,
                           Received_bytes, Total_bytes, State, Opened, Referrer, Site_url, Tab_url, Mime_type, File, Label, Comment
                    FROM chrome_downloads
                    WHERE id = {sourceId};",
                "firefox_downloads" => $@"
                    SELECT id, Artifact_type, Potential_activity, Browser, Download_id, Current_path,
                           {SearchSql.DateExpr("End_time")} AS End_time,
                           {SearchSql.DateExpr("Last_visit_time")} AS Last_visit_time,
                           Received_bytes, Total_bytes, Source_url, Title, State, File, Label, Comment
                    FROM firefox_downloads
                    WHERE id = {sourceId};",
                "bookmarks_Chrome" => $@"
                    SELECT id, Artifact_type, Potential_activity, Browser, Type, Title, URL,
                           {SearchSql.DateExpr("DateAdded")} AS DateAdded,
                           {SearchSql.DateExpr("DateLastUsed")} AS DateLastUsed,
                           {SearchSql.DateExpr("LastModified")} AS LastModified,
                           Parent_name, Guid, ChromeId, File, Label, Comment
                    FROM bookmarks_Chrome
                    WHERE id = {sourceId};",
                "bookmarks_Firefox" => $@"
                    SELECT id, Artifact_type, Potential_activity, Browser, Bookmark_id, Type, FK, Parent, Parent_name, Title,
                           {SearchSql.DateExpr("DateAdded")} AS DateAdded,
                           {SearchSql.DateExpr("LastModified")} AS LastModified,
                           URL, PageTitle, VisitCount,
                           {SearchSql.DateExpr("LastVisitDate")} AS LastVisitDate,
                           AnnoId, AnnoContent, AnnoName, File, Label, Comment
                    FROM bookmarks_Firefox
                    WHERE id = {sourceId};",
                "autofill_data" => $@"
                    SELECT id, Artifact_type, Potential_activity, Browser, FieldName, Value, Count,
                           {SearchSql.DateExpr("LastUsed")} AS LastUsed,
                           COALESCE(TimesUsed, Count) AS TimesUsed,
                           {SearchSql.DateExpr("FirstUsed")} AS FirstUsed,
                           File, Label, Comment
                    FROM autofill_data
                    WHERE id = {sourceId};",
                "cookies_data" => $@"
                    SELECT id, Artifact_type, Potential_activity, Browser, Host, Name, Value, Path,
                           {SearchSql.DateExpr("Created")} AS Created,
                           {SearchSql.DateExpr("Expires")} AS Expires,
                           {SearchSql.DateExpr("LastAccessed")} AS LastAccessed,
                           IsSecure, IsHttpOnly, IsPersistent, SameSite, SourceScheme, SourcePort, IsEncrypted, File, Label, Comment
                    FROM cookies_data
                    WHERE id = {sourceId};",
                "cache_data" => BuildCacheSelectQuery($"WHERE id = {sourceId}"),
                "session_data" => BuildSessionSelectQuery($"WHERE id = {sourceId}"),
                "extension_data" => BuildExtensionSelectQuery($"WHERE id = {sourceId}"),
                "saved_logins_data" => BuildLoginSelectQuery($"WHERE id = {sourceId}"),
                "local_storage_data" => BuildLocalStorageSelectQuery($"WHERE id = {sourceId}"),
                "session_storage_data" => BuildSessionStorageSelectQuery($"WHERE id = {sourceId}"),
                "indexeddb_data" => BuildIndexedDbSelectQuery($"WHERE id = {sourceId}"),
                _ => string.Empty
            };
        }

        private string ResolveArtifactTableForUpdate(DataRowView row, string detectedTable)
        {
            string sourceTable = GetRowValue(row, "Source_table");
            if (IsWritableArtifactTable(sourceTable))
                return sourceTable;

            return IsWritableArtifactTable(detectedTable) ? detectedTable : string.Empty;
        }

        private int? ResolveArtifactIdForUpdate(DataRowView row)
        {
            if (int.TryParse(GetRowValue(row, "Source_id"), out int sourceId))
                return sourceId;

            if (int.TryParse(GetRowValue(row, "id"), out int id))
                return id;

            return null;
        }

        private bool IsWritableArtifactTable(string tableName)
        {
            return tableName switch
            {
                "results" => true,
                "firefox_results" => true,
                "chrome_downloads" => true,
                "firefox_downloads" => true,
                "bookmarks_Chrome" => true,
                "bookmarks_Firefox" => true,
                "autofill_data" => true,
                "cookies_data" => true,
                "cache_data" => true,
                "session_data" => true,
                "extension_data" => true,
                "saved_logins_data" => true,
                "local_storage_data" => true,
                "session_storage_data" => true,
                "indexeddb_data" => true,
                _ => false
            };
        }


        private void AsignarLabelAFilaSeleccionada(string labelName)
        {
            var selectedRows = sfDataGrid1.SelectedItems.Cast<object>().ToList();

            foreach (var record in selectedRows)
            {
                var row = record as DataRowView;

                if (row != null && row.Row.Table.Columns.Contains("Label"))
                {
                    row["Label"] = labelName;

                    var columnasPresentes = row.DataView!.Table!.Columns
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

                    if (tablaDetectada == "AllWebHistory" || tablaDetectada == "AllDownloads" || tablaDetectada == "AllBookmarks"
                        || tablaDetectada == "AllAutoFill" || tablaDetectada == "AllCookies" || tablaDetectada == "AllCache" || tablaDetectada == "AllSessions" || tablaDetectada == "AllExtensions" || tablaDetectada == "AllSavedLogins" || tablaDetectada == "AllLocalStorage" || tablaDetectada == "AllSessionStorage" || tablaDetectada == "AllIndexedDb"
                        || tablaDetectada == "bookmarks_Chrome" || tablaDetectada == "bookmarks_Firefox")
                    {
                        var browser = row["Browser"]?.ToString()?.ToLowerInvariant();

                        if (tablaDetectada == "AllWebHistory")
                            tablaDetectada = Helpers.HistoryTableForBrowser(browser);
                        else if (tablaDetectada == "AllDownloads")
                            tablaDetectada = Helpers.DownloadTableForBrowser(browser);
                        else if (tablaDetectada == "AllBookmarks")
                            tablaDetectada = Helpers.BookmarkTableForBrowser(browser);
                        else if (tablaDetectada == "AllAutoFill")
                            tablaDetectada = "autofill_data";
                        else if (tablaDetectada == "AllCookies")
                            tablaDetectada = "cookies_data";
                        else if (tablaDetectada == "AllCache")
                            tablaDetectada = "cache_data";
                        else if (tablaDetectada == "AllSessions")
                            tablaDetectada = "session_data";
                        else if (tablaDetectada == "AllExtensions")
                            tablaDetectada = "extension_data";
                        else if (tablaDetectada == "AllSavedLogins")
                            tablaDetectada = "saved_logins_data";
                        else if (tablaDetectada == "AllLocalStorage")
                            tablaDetectada = "local_storage_data";
                        else if (tablaDetectada == "AllSessionStorage")
                            tablaDetectada = "session_storage_data";
                        else if (tablaDetectada == "AllIndexedDb")
                            tablaDetectada = "indexeddb_data";
                        else if (tablaDetectada == "bookmarks_Chrome")
                            tablaDetectada = Helpers.BookmarkTableForBrowser(browser);
                        else if (tablaDetectada == "bookmarks_Firefox")
                            tablaDetectada = Helpers.BookmarkTableForBrowser(browser);
                    }

                    tablaDetectada = ResolveArtifactTableForUpdate(row, tablaDetectada);
                    int? artifactId = ResolveArtifactIdForUpdate(row);
                    if (string.IsNullOrWhiteSpace(tablaDetectada) || !artifactId.HasValue)
                        continue;

                    var id = artifactId.Value;

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
                    row["Comment"] = Helpers.comment;

                    var columnasPresentes = row.DataView!.Table!.Columns
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

                    if (tablaDetectada == "AllWebHistory" || tablaDetectada == "AllDownloads" || tablaDetectada == "AllBookmarks"
                        || tablaDetectada == "AllAutoFill" || tablaDetectada == "AllCookies" || tablaDetectada == "AllCache" || tablaDetectada == "AllSessions" || tablaDetectada == "AllExtensions" || tablaDetectada == "AllSavedLogins" || tablaDetectada == "AllLocalStorage" || tablaDetectada == "AllSessionStorage" || tablaDetectada == "AllIndexedDb"
                        || tablaDetectada == "bookmarks_Chrome" || tablaDetectada == "bookmarks_Firefox")
                    {
                        var browser = row["Browser"]?.ToString()?.ToLowerInvariant();

                        if (tablaDetectada == "AllWebHistory")
                            tablaDetectada = Helpers.HistoryTableForBrowser(browser);
                        else if (tablaDetectada == "AllDownloads")
                            tablaDetectada = Helpers.DownloadTableForBrowser(browser);
                        else if (tablaDetectada == "AllBookmarks")
                            tablaDetectada = Helpers.BookmarkTableForBrowser(browser);
                        else if (tablaDetectada == "AllAutoFill")
                            tablaDetectada = "autofill_data";
                        else if (tablaDetectada == "AllCookies")
                            tablaDetectada = "cookies_data";
                        else if (tablaDetectada == "AllCache")
                            tablaDetectada = "cache_data";
                        else if (tablaDetectada == "AllSessions")
                            tablaDetectada = "session_data";
                        else if (tablaDetectada == "AllExtensions")
                            tablaDetectada = "extension_data";
                        else if (tablaDetectada == "AllSavedLogins")
                            tablaDetectada = "saved_logins_data";
                        else if (tablaDetectada == "AllLocalStorage")
                            tablaDetectada = "local_storage_data";
                        else if (tablaDetectada == "AllSessionStorage")
                            tablaDetectada = "session_storage_data";
                        else if (tablaDetectada == "AllIndexedDb")
                            tablaDetectada = "indexeddb_data";
                        else if (tablaDetectada == "bookmarks_Chrome")
                            tablaDetectada = Helpers.BookmarkTableForBrowser(browser);
                        else if (tablaDetectada == "bookmarks_Firefox")
                            tablaDetectada = Helpers.BookmarkTableForBrowser(browser);
                    }

                    tablaDetectada = ResolveArtifactTableForUpdate(row, tablaDetectada);
                    int? artifactId = ResolveArtifactIdForUpdate(row);
                    if (string.IsNullOrWhiteSpace(tablaDetectada) || !artifactId.HasValue)
                        continue;

                    var id = artifactId.Value;

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
                    row["Label"] = DBNull.Value;

                    var columnasPresentes = row.DataView!.Table!.Columns
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

                    if (tablaDetectada == "AllWebHistory" || tablaDetectada == "AllDownloads" || tablaDetectada == "AllBookmarks"
                        || tablaDetectada == "AllAutoFill" || tablaDetectada == "AllCookies" || tablaDetectada == "AllCache" || tablaDetectada == "AllSessions" || tablaDetectada == "AllExtensions" || tablaDetectada == "AllSavedLogins" || tablaDetectada == "AllLocalStorage" || tablaDetectada == "AllSessionStorage" || tablaDetectada == "AllIndexedDb"
                        || tablaDetectada == "bookmarks_Chrome" || tablaDetectada == "bookmarks_Firefox")
                    {
                        var browser = row["Browser"]?.ToString()?.ToLowerInvariant();

                        if (tablaDetectada == "AllWebHistory")
                            tablaDetectada = Helpers.HistoryTableForBrowser(browser);
                        else if (tablaDetectada == "AllDownloads")
                            tablaDetectada = Helpers.DownloadTableForBrowser(browser);
                        else if (tablaDetectada == "AllBookmarks")
                            tablaDetectada = Helpers.BookmarkTableForBrowser(browser);
                        else if (tablaDetectada == "AllAutoFill")
                            tablaDetectada = "autofill_data";
                        else if (tablaDetectada == "AllCookies")
                            tablaDetectada = "cookies_data";
                        else if (tablaDetectada == "AllCache")
                            tablaDetectada = "cache_data";
                        else if (tablaDetectada == "AllSessions")
                            tablaDetectada = "session_data";
                        else if (tablaDetectada == "AllExtensions")
                            tablaDetectada = "extension_data";
                        else if (tablaDetectada == "AllSavedLogins")
                            tablaDetectada = "saved_logins_data";
                        else if (tablaDetectada == "AllLocalStorage")
                            tablaDetectada = "local_storage_data";
                        else if (tablaDetectada == "AllSessionStorage")
                            tablaDetectada = "session_storage_data";
                        else if (tablaDetectada == "AllIndexedDb")
                            tablaDetectada = "indexeddb_data";
                        else if (tablaDetectada == "bookmarks_Chrome")
                            tablaDetectada = Helpers.BookmarkTableForBrowser(browser);
                        else if (tablaDetectada == "bookmarks_Firefox")
                            tablaDetectada = Helpers.BookmarkTableForBrowser(browser);
                    }

                    tablaDetectada = ResolveArtifactTableForUpdate(row, tablaDetectada);
                    int? artifactId = ResolveArtifactIdForUpdate(row);
                    if (string.IsNullOrWhiteSpace(tablaDetectada) || !artifactId.HasValue)
                        continue;

                    var id = artifactId.Value;

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

            sfDataGrid1.Refresh();
        }

        private void RemoverComentarioAFilasSeleccionadas()
        {
            string previousComment = Helpers.comment;
            Helpers.comment = string.Empty;
            AplicarComentarioAFilasSeleccionadas();
            Helpers.comment = previousComment;
        }






        private void SfDataGrid_AutoGeneratingColumn(object sender, AutoGeneratingColumnArgs e)
        {
            var columnasSinFiltro = new List<string> { "id", "Url", "Visit_time", "Last_visit_time", "Visit_duration", "Start_time", "End_time", "DateAdded", "LastModified", "LastUsed" };

            if (columnasSinFiltro.Contains(e.Column.MappingName))
            {
                e.Column.AllowFiltering = false;
            }
        }

        private async Task ShowStartupDialogAsync()
        {
            if (startupDialogOpen || IsDisposed)
                return;

            startupDialogOpen = true;

            try
            {
                while (!IsDisposed)
                {
                    using var startup = new StartupForm();
                    DialogResult result = startup.ShowDialog(this);

                    if (result != DialogResult.OK || startup.SelectedAction == StartupAction.Exit)
                    {
                        Close();
                        return;
                    }

                    if (await ProcessStartupRequestAsync(startup.Request))
                        return;
                }
            }
            finally
            {
                startupDialogOpen = false;
            }
        }

        private async Task<bool> ProcessStartupRequestAsync(StartupRequest request)
        {
            if (request.Action == StartupAction.OpenProject)
            {
                return OpenProjectFromPath(request.ProjectPath);
            }

            if (request.Action == StartupAction.NewProject)
            {
                try
                {
                    PrepareNewProject(request.ProjectPath);
                    await ScanWebActivityFromPathAsync(request.ScanPath);
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error when creating the project: {ex.Message}", "Browser Reviewer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }

            return false;
        }

        private async Task<bool> RunNewProjectWizardAsync()
        {
            using var saveFileDialog = new SaveFileDialog
            {
                Filter = "Browser Reviewer files (*.bre)|*.bre",
                Title = "Create Project",
                FileName = "Default.bre"
            };

            if (saveFileDialog.ShowDialog(this) != DialogResult.OK)
                return false;

            using var folderBrowserDialog = new FolderBrowserDialog
            {
                Description = "Select the path to scan for web activity"
            };

            if (folderBrowserDialog.ShowDialog(this) != DialogResult.OK)
                return false;

            try
            {
                PrepareNewProject(saveFileDialog.FileName);
                await ScanWebActivityFromPathAsync(folderBrowserDialog.SelectedPath);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error when creating the project: {ex.Message}", "Browser Reviewer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void PrepareNewProject(string projectPath)
        {
            initAll();
            Helpers.db_name = projectPath;
            Helpers.chromeViewerConnectionString = $"Data Source={Helpers.db_name};Version=3;";
            Tools.CreateDatabase(Helpers.chromeViewerConnectionString);
            this.Text = "Browser Reviewer v1.0 is working on:   " + Helpers.db_name;
            enableButtons();
            Helpers.labelsTable = new DataTable();
            ResetSearchState();
        }

        private bool OpenProjectFromPath(string projectPath)
        {
            if (string.IsNullOrWhiteSpace(projectPath))
                return false;

            initAll();

            try
            {
                Helpers.db_name = projectPath;
                Helpers.chromeViewerConnectionString = $"Data Source={Helpers.db_name};Version=3;";
                Tools.CreateDatabase(Helpers.chromeViewerConnectionString);
                this.Text = "Browser Reviewer v1.0 is working on:   " + Helpers.db_name;
                setLabels();

                ShowFullTimelineWebActivity();
                enableButtons();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening the project:\n{ex}", "Browser Reviewer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void ResetSearchState()
        {
            search_textBox.Text = "";
            Helpers.searchTerm = "";
            Helpers.searchTermExists = false;
            Helpers.searchTermRegExp = false;
            Helpers.searchTimeCondition = false;
            this.sfDataGrid1.SearchController.ClearSearch();
            groupBox_customSearch.BackColor = SystemColors.Control;
            checkBox_RegExp.Checked = false;
            if (checkBox_Labels != null) checkBox_Labels.Checked = false;
            checkBox_enableTime.Checked = false;
            Helpers.searchLabelsOnly = false;
            Helpers.sqltimecondition = "";
            Helpers.sqlDownloadtimecondition = "";
            Helpers.sqlBookmarkstimecondition = "";
            Helpers.sqlAutofilltimecondition = "";
            Helpers.sqlCookiestimecondition = "";
            Helpers.sqlCachetimecondition = "";
        }

        private void RefreshArtifactDictionaries()
        {
            bool loadedSummary = MyTools.TryLoadProcessingSummary(Helpers.chromeViewerConnectionString, Console);
            if (loadedSummary)
            {
                Helpers.browserUrls = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                RefreshNonHistoryArtifactCounts();
                return;
            }

            Helpers.browserUrls = Tools.FillDictionaryFromDatabase();
            Helpers.browserHistoryCounts = Helpers.browserUrls.ToDictionary(pair => pair.Key, pair => pair.Value.Count, StringComparer.OrdinalIgnoreCase);
            Helpers.browserHistoryCategoryCounts = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
            foreach (var browserEntry in Helpers.browserUrls)
            {
                Dictionary<string, int> categoryCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (string url in browserEntry.Value)
                {
                    string category = MyTools.Evaluatecategory(url);
                    categoryCounts[category] = categoryCounts.TryGetValue(category, out int count) ? count + 1 : 1;
                }

                Helpers.browserHistoryCategoryCounts[browserEntry.Key] = categoryCounts;
            }

            Helpers.browsersWithDownloads = MyTools.GetBrowsersWithDownloads();
            Helpers.browsersWithBookmarks = MyTools.GetBrowsersWithBookmarks();
            Helpers.browsersWithAutofill = MyTools.GetBrowsersWithAutofill();
            Helpers.browsersWithCookies = MyTools.GetBrowsersWithCookies();
            Helpers.browsersWithCache = MyTools.GetBrowsersWithCache();
            Helpers.browsersWithSessions = MyTools.GetBrowsersWithSessions();
            Helpers.browsersWithExtensions = MyTools.GetBrowsersWithExtensions();
            Helpers.browsersWithLogins = MyTools.GetBrowsersWithLogins();
            Helpers.browsersWithLocalStorage = MyTools.GetBrowsersWithLocalStorage();
            Helpers.browsersWithSessionStorage = MyTools.GetBrowsersWithSessionStorage();
            Helpers.browsersWithIndexedDb = MyTools.GetBrowsersWithIndexedDb();
        }

        private void RefreshNonHistoryArtifactCounts()
        {
            Helpers.browsersWithDownloads = MyTools.GetBrowsersWithDownloads();
            Helpers.browsersWithBookmarks = MyTools.GetBrowsersWithBookmarks();
            Helpers.browsersWithAutofill = MyTools.GetBrowsersWithAutofill();
            Helpers.browsersWithCookies = MyTools.GetBrowsersWithCookies();
            Helpers.browsersWithCache = MyTools.GetBrowsersWithCache();
            Helpers.browsersWithSessions = MyTools.GetBrowsersWithSessions();
            Helpers.browsersWithExtensions = MyTools.GetBrowsersWithExtensions();
            Helpers.browsersWithLogins = MyTools.GetBrowsersWithLogins();
            Helpers.browsersWithLocalStorage = MyTools.GetBrowsersWithLocalStorage();
            Helpers.browsersWithSessionStorage = MyTools.GetBrowsersWithSessionStorage();
            Helpers.browsersWithIndexedDb = MyTools.GetBrowsersWithIndexedDb();
        }

        private void ShowFullTimelineWebActivity()
        {
            RefreshArtifactDictionaries();
            AcordeonMenu(Helpers.browserUrls, Helpers.browsersWithDownloads, Helpers.browsersWithBookmarks, Helpers.browsersWithAutofill, Helpers.browsersWithCookies, Helpers.browsersWithCache, Helpers.browsersWithSessions, Helpers.browsersWithExtensions);
            Tools.ShowQueryOnDataGridView(sfDataGrid1, Helpers.chromeViewerConnectionString, BuildAllWebArtifactsTimelineQuery(), labelItemCount, Console);
            labelStatus.Text = "Full time line web activity";
        }

        private string BuildAllWebHistoryQuery()
        {
            int utcOffset = Helpers.utcOffset;

            if (utcOffset == 0)
            {
                return @"SELECT
                            r.id AS id,
                            r.Artifact_type AS Artifact_type,
                            r.Potential_activity AS Potential_activity,
                            NULL AS Navigation_context,
                            NULL AS User_action_likelihood,
                            r.Browser AS Browser,
                            r.Category AS Category,
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
                            f.Artifact_type AS Artifact_type,
                            f.Potential_activity AS Potential_activity,
                            f.Navigation_context AS Navigation_context,
                            f.User_action_likelihood AS User_action_likelihood,
                            f.Browser AS Browser,
                            f.Category AS Category,
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

            string offset = utcOffset > 0 ? $"+{utcOffset} hours" : $"{utcOffset} hours";

            return $@"SELECT 
                            r.id AS id,
                            r.Artifact_type AS Artifact_type,
                            r.Potential_activity AS Potential_activity,
                            NULL AS Navigation_context,
                            NULL AS User_action_likelihood,
                            r.Browser AS Browser,
                            r.Category AS Category,
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
                            f.Artifact_type AS Artifact_type,
                            f.Potential_activity AS Potential_activity,
                            f.Navigation_context AS Navigation_context,
                            f.User_action_likelihood AS User_action_likelihood,
                            f.Browser AS Browser,
                            f.Category AS Category,
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

        private async Task ScanWebActivityFromPathAsync(string rootDirectory)
        {
            disableButtons();
            SetProcessingIndicatorVisible(true);
            await Task.Yield();
            BrowserReviewerLogger.ConfigureForDatabase(Helpers.db_name);
            MyTools.LogToConsole(Console, "Scan started.", Color.Blue);

            try
            {
                MyTools.LogToConsole(Console, $"Root path: {rootDirectory}", Color.Blue);
                await Tools.ListFilesAndDirectories(rootDirectory, Console);
                MyTools.LogToConsole(Console, "Scan finished.", Color.Green);
            }
            catch (Exception ex)
            {
                MyTools.LogToConsole(Console, $"Scan failed: {ex.Message}", Color.Red);
                throw;
            }
            finally
            {
                MyTools.CloseLog();
                SetProcessingIndicatorVisible(false);
                enableButtons();
            }

            ShowFullTimelineWebActivity();
            button_SearchWebActivity.Enabled = false;
        }


        private void cToolStripMenuItem_Click(object sender, EventArgs e)
        {
            initAll();

            disableButtons();

            this.Text = "Browser Reviewer v1.0";
            BeginInvoke(new System.Action(async () => await ShowStartupDialogAsync()));
        }








        private void SfDataGrid_FilterChanged(object sender, Syncfusion.WinForms.DataGrid.Events.FilterChangedEventArgs e)
        {
            labelItemCount.Text = $"Items count: {sfDataGrid1.View.Records.Count}";
        }














































        private void SfDataGrid1_SelectionChanged(object sender, Syncfusion.WinForms.DataGrid.Events.SelectionChangedEventArgs e)
        {
            if (sfDataGrid1.SelectedItem != null)
            {
                DataRowView? dataRowView = sfDataGrid1.SelectedItem as DataRowView;
                if (dataRowView != null)
                {
                    if (IsCacheRow(dataRowView))
                    {
                        RenderCachePreview(dataRowView);
                        return;
                    }

                    HideCachePreview();
                    richTextBox1.Clear();
                    AppendTimeInterpretationIfNeeded(dataRowView);

                    foreach (DataColumn column in dataRowView.DataView!.Table!.Columns)
                    {
                        string columnName = column.ColumnName;
                        string columnValue = dataRowView[columnName]?.ToString() ?? "N/A";

                        richTextBox1.SelectionFont = new Font(richTextBox1.Font, FontStyle.Bold);
                        richTextBox1.AppendText($"{columnName}: ");

                        richTextBox1.SelectionFont = new Font(richTextBox1.Font, FontStyle.Regular);
                        richTextBox1.SelectionColor = Color.Blue;
                        richTextBox1.AppendText($"{columnValue}\n");

                        richTextBox1.AppendText("\n");

                        richTextBox1.SelectionColor = richTextBox1.ForeColor;
                        richTextBox1.SelectionFont = new Font(richTextBox1.Font, FontStyle.Regular);
                    }
                }
                highlight_Text();
            }
        }


        private bool IsCacheRow(DataRowView row)
        {
            return row.Row.Table.Columns.Contains("BodyStored") &&
                   row.Row.Table.Columns.Contains("DetectedFileType") &&
                   row.Row.Table.Columns.Contains("BodySha256");
        }


        private void RenderCachePreview(DataRowView row)
        {
            string detectedType = GetRowValue(row, "DetectedFileType");
            byte[]? body = GetCacheBody(row);

            if (IsSupportedImageType(detectedType) && body != null && body.Length > 0 && TryShowCacheImage(body, row))
            {
                ShowCacheMetadata(row);
                return;
            }

            if (IsWebViewCacheType(row) && body != null && body.Length > 0 && TryShowCacheWebView(body, row))
            {
                ShowCacheMetadata(row);
                return;
            }

            if (IsTextCacheType(detectedType))
            {
                ShowCacheMetadata(row);
                if (body != null && body.Length > 0 && TryShowCacheWebView(body, row))
                {
                    return;
                }

                ShowCacheGenericPreview(row);
                return;
            }

            ShowCacheMetadata(row);
            ShowCacheGenericPreview(row);
        }


        private byte[]? GetCacheBody(DataRowView row)
        {
            if (!int.TryParse(GetRowValue(row, "id"), out int id) || string.IsNullOrWhiteSpace(Helpers.chromeViewerConnectionString))
            {
                return null;
            }

            using (SQLiteConnection connection = new SQLiteConnection(Helpers.chromeViewerConnectionString))
            {
                connection.Open();
                using (SQLiteCommand command = new SQLiteCommand("SELECT Body FROM cache_data WHERE id = @id", connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    object? value = command.ExecuteScalar();
                    return value == DBNull.Value || value == null ? null : (byte[])value;
                }
            }
        }


        private bool TryShowCacheImage(byte[] body, DataRowView row)
        {
            try
            {
                EnsureCachePreviewPictureBox();
                SetCachePreviewLayout(true);
                HideCacheWebView();
                using (MemoryStream stream = new MemoryStream(body))
                {
                    Image image = Image.FromStream(stream);
                    cachePreviewPictureBox.Image?.Dispose();
                    cachePreviewPictureBox.Image = new Bitmap(image);
                }

                cachePreviewPictureBox.Visible = true;
                return true;
            }
            catch
            {
                ShowCacheGenericPreview(row);
                return false;
            }
        }


        private bool TryShowCacheWebView(byte[] body, DataRowView row)
        {
            try
            {
                EnsureCachePreviewWebView();
                SetCachePreviewLayout(true);
                HideCachePictureBox();

                string tempFile = WriteCachePreviewTempFile(body, row);
                cachePreviewWebView.Source = new Uri(tempFile);
                cachePreviewWebView.Visible = true;
                return true;
            }
            catch
            {
                ShowCacheGenericPreview(row);
                return false;
            }
        }


        private void ShowCacheGenericPreview(DataRowView row)
        {
            EnsureCachePreviewPictureBox();
            SetCachePreviewLayout(true);
            HideCacheWebView();
            cachePreviewPictureBox.Image?.Dispose();
            cachePreviewPictureBox.Image = BuildGenericCachePreviewImage(row);
            cachePreviewPictureBox.Visible = true;
        }


        private void ResetCachePreview()
        {
            SetCachePreviewLayout(false);
            if (cachePreviewPictureBox != null)
            {
                cachePreviewPictureBox.Image?.Dispose();
                cachePreviewPictureBox.Image = null;
                cachePreviewPictureBox.Visible = false;
            }

            HideCacheWebView();
            DeleteCachePreviewTempFile();
        }


        private void HideCachePreview()
        {
            SetCachePreviewLayout(false);
            HideCacheWebView();
            DeleteCachePreviewTempFile();
            if (cachePreviewPictureBox != null)
            {
                cachePreviewPictureBox.Visible = false;
            }
        }


        private void SetCachePreviewLayout(bool enabled)
        {
            if (textBoxLayout == null || textBoxLayout.RowStyles.Count < 2 || cachePreviewLayoutActive == enabled)
            {
                return;
            }

            cachePreviewLayoutActive = enabled;

            if (enabled)
            {
                textBoxLayout.RowStyles[0].SizeType = SizeType.Percent;
                textBoxLayout.RowStyles[0].Height = 50;
                textBoxLayout.RowStyles[1].SizeType = SizeType.Percent;
                textBoxLayout.RowStyles[1].Height = 50;
            }
            else
            {
                textBoxLayout.RowStyles[0].SizeType = SizeType.Percent;
                textBoxLayout.RowStyles[0].Height = 100;
                textBoxLayout.RowStyles[1].SizeType = SizeType.Absolute;
                textBoxLayout.RowStyles[1].Height = 0;
            }

            textBoxLayout.PerformLayout();
        }


        private void HideCachePictureBox()
        {
            if (cachePreviewPictureBox != null)
            {
                cachePreviewPictureBox.Visible = false;
            }
        }


        private void HideCacheWebView()
        {
            if (cachePreviewWebView != null)
            {
                cachePreviewWebView.Visible = false;
                cachePreviewWebView.Source = new Uri("about:blank");
            }
        }


        private void EnsureCachePreviewPictureBox()
        {
            if (cachePreviewPictureBox != null)
            {
                return;
            }

            cachePreviewPictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };

            Control? parent = richTextBox1.Parent;
            if (parent == null)
            {
                return;
            }

            parent.Controls.Add(cachePreviewPictureBox);
        }


        private void EnsureCachePreviewWebView()
        {
            if (cachePreviewWebView != null)
            {
                return;
            }

            cachePreviewWebView = new WebView2
            {
                Dock = DockStyle.Fill,
                Visible = false
            };

            Control? parent = richTextBox1.Parent;
            if (parent == null)
            {
                return;
            }

            parent.Controls.Add(cachePreviewWebView);
        }


        private string WriteCachePreviewTempFile(byte[] body, DataRowView row)
        {
            DeleteCachePreviewTempFile();

            string extension = GetCachePreviewExtension(row);

            string tempDirectory = Path.Combine(Path.GetTempPath(), "BrowserReviewerCachePreview");
            Directory.CreateDirectory(tempDirectory);
            string tempFile = Path.Combine(tempDirectory, Guid.NewGuid().ToString("N") + extension);
            File.WriteAllBytes(tempFile, body);
            cachePreviewTempFile = tempFile;
            return tempFile;
        }


        private string GetCachePreviewExtension(DataRowView row)
        {
            string extension = GetRowValue(row, "DetectedExtension", ".bin");
            string contentType = GetRowValue(row, "ContentType").ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(extension) || extension.Equals(".bin", StringComparison.OrdinalIgnoreCase))
            {
                if (contentType.Contains("video/mp4")) extension = ".mp4";
                else if (contentType.Contains("video/quicktime")) extension = ".mov";
                else if (contentType.Contains("video/webm")) extension = ".webm";
                else if (contentType.Contains("video/ogg")) extension = ".ogv";
                else if (contentType.Contains("video/x-msvideo")) extension = ".avi";
                else if (contentType.StartsWith("video/")) extension = ".mp4";
                else if (contentType.Contains("image/webp")) extension = ".webp";
                else if (contentType.Contains("application/pdf")) extension = ".pdf";
                else if (contentType.Contains("text/html")) extension = ".html";
                else if (contentType.Contains("javascript")) extension = ".js";
                else if (contentType.Contains("text/css")) extension = ".css";
                else if (contentType.Contains("json")) extension = ".json";
                else if (contentType.Contains("xml")) extension = ".xml";
                else if (contentType.StartsWith("text/")) extension = ".txt";
            }

            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = ".bin";
            }

            if (!extension.StartsWith("."))
            {
                extension = "." + extension;
            }

            return extension;
        }


        private void DeleteCachePreviewTempFile()
        {
            if (string.IsNullOrWhiteSpace(cachePreviewTempFile))
            {
                return;
            }

            try
            {
                if (File.Exists(cachePreviewTempFile))
                {
                    File.Delete(cachePreviewTempFile);
                }
            }
            catch
            {
            }

            cachePreviewTempFile = string.Empty;
        }


        private Image BuildGenericCachePreviewImage(DataRowView row)
        {
            int width = Math.Max(320, richTextBox1.Width);
            int height = Math.Max(360, richTextBox1.Height);
            Bitmap bitmap = new Bitmap(width, height);

            using (Graphics graphics = Graphics.FromImage(bitmap))
            using (Font titleFont = new Font("Segoe UI", 18, FontStyle.Bold))
            using (Font labelFont = new Font("Segoe UI", 10, FontStyle.Bold))
            using (Font valueFont = new Font("Segoe UI", 10, FontStyle.Regular))
            using (Pen borderPen = new Pen(Color.FromArgb(120, 120, 120)))
            using (Brush titleBrush = new SolidBrush(Color.FromArgb(40, 60, 80)))
            using (Brush textBrush = new SolidBrush(Color.FromArgb(30, 30, 30)))
            using (Brush mutedBrush = new SolidBrush(Color.FromArgb(90, 90, 90)))
            {
                graphics.Clear(Color.White);
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                Rectangle iconRect = new Rectangle((width - 96) / 2, 32, 96, 96);
                graphics.DrawRectangle(borderPen, iconRect);
                string extension = row == null ? "CACHE" : GetRowValue(row, "DetectedExtension", "CACHE").ToUpperInvariant();
                graphics.DrawString(extension, labelFont, titleBrush, iconRect, CenteredStringFormat());

                int y = 155;
                graphics.DrawString(row == null ? "Cache Preview" : GetRowValue(row, "DetectedFileType", "Cache Entry"), titleFont, titleBrush, new RectangleF(20, y, width - 40, 36));
                y += 55;

                if (row == null)
                {
                    DrawPreviewLine(graphics, labelFont, valueFont, textBrush, mutedBrush, "Status", "Select a cache row to preview images, text, or metadata.", 24, ref y, width);
                }
                else
                {
                    DrawPreviewLine(graphics, labelFont, valueFont, textBrush, mutedBrush, "Stored", GetRowValue(row, "BodyStored") == "1" ? "Body stored in database" : "Metadata/hash only", 24, ref y, width);
                    DrawPreviewLine(graphics, labelFont, valueFont, textBrush, mutedBrush, "Size", GetRowValue(row, "BodySize", GetRowValue(row, "FileSize")), 24, ref y, width);
                    DrawPreviewLine(graphics, labelFont, valueFont, textBrush, mutedBrush, "SHA-256", GetRowValue(row, "BodySha256"), 24, ref y, width);
                    DrawPreviewLine(graphics, labelFont, valueFont, textBrush, mutedBrush, "Host", GetRowValue(row, "Host"), 24, ref y, width);
                    DrawPreviewLine(graphics, labelFont, valueFont, textBrush, mutedBrush, "URL", GetRowValue(row, "Url"), 24, ref y, width);
                }
            }

            return bitmap;
        }


        private StringFormat CenteredStringFormat()
        {
            return new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
        }


        private void DrawPreviewLine(Graphics graphics, Font labelFont, Font valueFont, Brush textBrush, Brush mutedBrush, string label, string value, int x, ref int y, int width)
        {
            graphics.DrawString(label + ":", labelFont, textBrush, x, y);
            string display = string.IsNullOrWhiteSpace(value) ? "N/A" : value;
            if (display.Length > 80)
            {
                display = display.Substring(0, 80) + "...";
            }

            graphics.DrawString(display, valueFont, mutedBrush, new RectangleF(x, y + 21, width - (x * 2), 44));
            y += 72;
        }


        private void AppendCacheMetadata(DataRowView row, string message)
        {
            richTextBox1.AppendText(message + "\n\n");
            AppendTimeInterpretationIfNeeded(row);

            foreach (DataColumn column in row.DataView!.Table!.Columns)
            {
                richTextBox1.SelectionFont = new Font(richTextBox1.Font, FontStyle.Bold);
                richTextBox1.AppendText($"{column.ColumnName}: ");
                richTextBox1.SelectionFont = new Font(richTextBox1.Font, FontStyle.Regular);
                richTextBox1.SelectionColor = Color.Blue;
                richTextBox1.AppendText($"{GetRowValue(row, column.ColumnName)}\n\n");
                richTextBox1.SelectionColor = richTextBox1.ForeColor;
            }
        }


        private void ShowCacheMetadata(DataRowView row)
        {
            richTextBox1.Clear();
            AppendCacheMetadata(row, "Cache entry metadata");
            highlight_Text();
        }


        private bool IsSupportedImageType(string detectedType)
        {
            string value = (detectedType ?? "").ToLowerInvariant();
            return value == "jpeg" || value == "png" || value == "gif" || value == "bmp";
        }


        private bool IsWebViewCacheType(DataRowView row)
        {
            string detectedType = GetRowValue(row, "DetectedFileType").ToLowerInvariant();
            string extension = GetRowValue(row, "DetectedExtension").ToLowerInvariant();
            string contentType = GetRowValue(row, "ContentType").ToLowerInvariant();

            return detectedType.Contains("webp") ||
                   detectedType.Contains("mp4") ||
                   detectedType.Contains("quicktime") ||
                   detectedType.Contains("webm") ||
                   detectedType.Contains("video") ||
                   detectedType.Contains("pdf") ||
                   detectedType.Contains("html") ||
                   detectedType.Contains("svg") ||
                   detectedType == "text" ||
                   detectedType == "javascript" ||
                   detectedType == "css" ||
                   detectedType == "json" ||
                   detectedType == "xml" ||
                   extension == ".webp" ||
                   extension == ".mp4" ||
                   extension == ".mov" ||
                   extension == ".webm" ||
                   extension == ".pdf" ||
                   extension == ".html" ||
                   extension == ".svg" ||
                   extension == ".txt" ||
                   extension == ".js" ||
                   extension == ".css" ||
                   extension == ".json" ||
                   extension == ".xml" ||
                   contentType.StartsWith("video/") ||
                   contentType == "application/pdf" ||
                   contentType.Contains("image/webp") ||
                   contentType.StartsWith("text/") ||
                   contentType.Contains("javascript") ||
                   contentType.Contains("json") ||
                   contentType.Contains("xml");
        }


        private bool IsTextCacheType(string detectedType)
        {
            string value = (detectedType ?? "").ToLowerInvariant();
            return value == "text" || value == "html" || value == "json" || value == "xml" || value == "javascript" || value == "css";
        }


        private string GetRowValue(DataRowView row, string columnName, string fallback = "")
        {
            if (!row.Row.Table.Columns.Contains(columnName))
            {
                return fallback;
            }

            object value = row[columnName];
            return value == DBNull.Value || value == null ? fallback : value.ToString() ?? fallback;
        }


        private void AppendTimeInterpretationIfNeeded(DataRowView row)
        {
            if (!RowHasTimestampField(row))
                return;

            string offset = FormatUtcOffsetLabel(Helpers.utcOffset);

            richTextBox1.SelectionFont = new Font(richTextBox1.Font, FontStyle.Bold);
            richTextBox1.AppendText("Time interpretation: ");
            richTextBox1.SelectionFont = new Font(richTextBox1.Font, FontStyle.Regular);
            richTextBox1.SelectionColor = Color.DarkGreen;
            richTextBox1.AppendText($"{offset}\n\n");

            richTextBox1.SelectionColor = richTextBox1.ForeColor;
            richTextBox1.SelectionFont = new Font(richTextBox1.Font, FontStyle.Regular);
        }


        private bool RowHasTimestampField(DataRowView row)
        {
            if (row == null)
                return false;

            foreach (DataColumn column in row.DataView!.Table!.Columns)
            {
                if (IsTimestampColumn(column.ColumnName) && !string.IsNullOrWhiteSpace(GetRowValue(row, column.ColumnName)))
                    return true;
            }

            return false;
        }


        private bool IsTimestampColumn(string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName))
                return false;

            string normalized = columnName.Replace("_", "").ToLowerInvariant();
            return normalized == "activitytime"
                || normalized == "secondarytime"
                || normalized == "visittime"
                || normalized == "lastvisittime"
                || normalized == "visitdate"
                || normalized == "lastvisitdate"
                || normalized == "starttime"
                || normalized == "endtime"
                || normalized == "dateadded"
                || normalized == "datelastused"
                || normalized == "lastmodified"
                || normalized == "firstused"
                || normalized == "lastused"
                || normalized == "created"
                || normalized == "modified"
                || normalized == "expires"
                || normalized == "lastaccessed"
                || normalized == "installtime"
                || normalized == "lastupdatetime"
                || normalized == "passwordchanged";
        }


        private string FormatUtcOffsetLabel(int utcOffset)
        {
            if (utcOffset == 0)
                return "UTC+00:00 / no display offset applied";

            string sign = utcOffset > 0 ? "+" : "-";
            return $"UTC{sign}{Math.Abs(utcOffset):00}:00 applied";
        }


        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            DeleteCachePreviewTempFile();
            base.OnFormClosed(e);
        }


        private void NumericUpDown1_ValueChanged(object? sender, EventArgs e)
        {
            Helpers.utcOffset = (int)numericUpDown1.Value;
        }






        private void InitializeNumericUpDown()
        {
            numericUpDown1.Minimum = -12;
            numericUpDown1.Maximum = 14;
            numericUpDown1.Increment = 1;
            numericUpDown1.DecimalPlaces = 0;

            numericUpDown1.Value = 0;
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

                    string pattern = Convert.ToString(args[0]) ?? string.Empty;
                    string input = Convert.ToString(args[1]) ?? string.Empty;

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

        private async void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await RunNewProjectWizardAsync();
        }

        private void openToolStripMenuItem_Click_1(object sender, EventArgs e)
        {


            int utcOffset = Helpers.utcOffset;
            string sqlquery;


            if (utcOffset == 0)
            {
                sqlquery = @"SELECT
                            r.id AS id,
                            r.Artifact_type AS Artifact_type,
                            r.Potential_activity AS Potential_activity,
                            NULL AS Navigation_context,
                            NULL AS User_action_likelihood,
                            r.Browser AS Browser,
                            r.Category AS Category,
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
                            f.Artifact_type AS Artifact_type,
                            f.Potential_activity AS Potential_activity,
                            f.Navigation_context AS Navigation_context,
                            f.User_action_likelihood AS User_action_likelihood,
                            f.Browser AS Browser,
                            f.Category AS Category,
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
                            r.Artifact_type AS Artifact_type,
                            r.Potential_activity AS Potential_activity,
                            NULL AS Navigation_context,
                            NULL AS User_action_likelihood,
                            r.Browser AS Browser,
                            r.Category AS Category,
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
                            f.Artifact_type AS Artifact_type,
                            f.Potential_activity AS Potential_activity,
                            f.Navigation_context AS Navigation_context,
                            f.User_action_likelihood AS User_action_likelihood,
                            f.Browser AS Browser,
                            f.Category AS Category,
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
                        Tools.CreateDatabase(Helpers.chromeViewerConnectionString);
                        this.Text = "Browser Reviewer v1.0 is working on:   " + Helpers.db_name;
                        setLabels();

                        ShowFullTimelineWebActivity();
                        enableButtons();

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error opening the project:\n{ex.ToString()}", "ChromeViewer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    enableButtons();
                }

            }


        }


        private async void button_SearchWebActivity_Click(object sender, EventArgs e)
        {
            int utcOffset = Helpers.utcOffset;
            string sqlquery;

            if (utcOffset == 0)
            {
                sqlquery = @"SELECT
                            r.id AS id,
                            r.Artifact_type AS Artifact_type,
                            r.Potential_activity AS Potential_activity,
                            NULL AS Navigation_context,
                            NULL AS User_action_likelihood,
                            r.Browser AS Browser,
                            r.Category AS Category,
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
                            f.Artifact_type AS Artifact_type,
                            f.Potential_activity AS Potential_activity,
                            f.Navigation_context AS Navigation_context,
                            f.User_action_likelihood AS User_action_likelihood,
                            f.Browser AS Browser,
                            f.Category AS Category,
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
                            r.Artifact_type AS Artifact_type,
                            r.Potential_activity AS Potential_activity,
                            NULL AS Navigation_context,
                            NULL AS User_action_likelihood,
                            r.Browser AS Browser,
                            r.Category AS Category,
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
                            f.Artifact_type AS Artifact_type,
                            f.Potential_activity AS Potential_activity,
                            f.Navigation_context AS Navigation_context,
                            f.User_action_likelihood AS User_action_likelihood,
                            f.Browser AS Browser,
                            f.Category AS Category,
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
                    disableButtons();
                    SetProcessingIndicatorVisible(true);
                    await Task.Yield();
                    BrowserReviewerLogger.ConfigureForDatabase(Helpers.db_name);
                    MyTools.LogToConsole(Console, "Scan started.", Color.Blue);

                    string rootDirectory = folderBrowserDialog.SelectedPath;

                    try
                    {
                        MyTools.LogToConsole(Console, $"Root path: {rootDirectory}", Color.Blue);
                        await Tools.ListFilesAndDirectories(rootDirectory, Console);
                        MyTools.LogToConsole(Console, "Scan finished.", Color.Green);
                    }
                    catch (Exception ex)
                    {
                        MyTools.LogToConsole(Console, $"Scan failed: {ex.Message}", Color.Red);
                        throw;
                    }
                    finally
                    {
                        MyTools.CloseLog();
                        SetProcessingIndicatorVisible(false);
                        enableButtons();
                    }

                }
                else
                {
                    return;
                }
            }

            ShowFullTimelineWebActivity();
            button_SearchWebActivity.Enabled = false;

        }

        private void SetProcessingIndicatorVisible(bool visible)
        {
            progressBarProcessing.Visible = visible;
            progressBarProcessing.Enabled = visible;
            if (visible)
            {
                progressBarProcessing.BringToFront();
                progressBarProcessing.Refresh();
                groupBox_TextBox.Refresh();
                Refresh();
            }
        }


























































































        private void AcordeonMenu(Dictionary<string, List<string>> navegadorCategorias, Dictionary<string, int> browsersWithDownloads, Dictionary<string, int> browsersWithBookmarks, Dictionary<string, int> browsersWithDatafill, Dictionary<string, int> browsersWithCookies, Dictionary<string, int> browsersWithCache, Dictionary<string, int> browsersWithSessions, Dictionary<string, int> browsersWithExtensions)
        {
            Font fm = Helpers.FM;
            Font fs = Helpers.FS;

            var flwMain = new FlowLayoutPanel
            {
                BackColor = this.BackColor,
                BorderStyle = BorderStyle.FixedSingle,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                AutoSize = false,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(4)
            };

            groupBox_Main.SuspendLayout();
            DisposeChildControls(groupBox_Main);
            groupBox_Main.AutoSize = false;
            groupBox_Main.Dock = DockStyle.Fill;
            groupBox_Main.Controls.Add(flwMain);
            flwMain.SuspendLayout();

            flwMain.SizeChanged += (s, e) =>
            {
                int usable = flwMain.ClientSize.Width - flwMain.Padding.Horizontal;
                foreach (Control c in flwMain.Controls)
                    if ((c.Tag as string) == "fullwidth")
                        c.Width = Math.Max(0, usable - c.Margin.Horizontal);
            };

            bool isFilteredView = IsSearchFilterActive();
            bool useHistorySummary = !isFilteredView && Helpers.browserHistoryCounts.Any();
            int historyCount = useHistorySummary
                ? Helpers.browserHistoryCounts.Values.Sum()
                : navegadorCategorias.Sum(category => category.Value.Count);

            AddAllWebArtifactsLabels(flwMain);

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
            lblAllHistoryButton.Width =
                flwMain.ClientSize.Width - flwMain.Padding.Horizontal - lblAllHistoryButton.Margin.Horizontal;

            lblAllHistoryButton.Click += (sender, e) =>
            {
                int utcOffset = Helpers.utcOffset;
                string? offset = utcOffset == 0 ? null : (utcOffset > 0 ? $"+{utcOffset} hours" : $"{utcOffset} hours");
                string resultsWhere = CurrentWhereFilter(
                    SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Category", "Url", "Title"),
                    SearchSql.TimeCondition("Visit_time", "Last_visit_time"));
                string firefoxWhere = CurrentWhereFilter(
                    SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Category", "Url", "Title", "Transition", "Navigation_context", "User_action_likelihood"),
                    SearchSql.TimeCondition("Visit_time", "Last_visit_time"));

                string sqlquery = offset == null
                    ? @"SELECT r.id, r.Artifact_type, r.Potential_activity, NULL AS Navigation_context, NULL AS User_action_likelihood, r.Browser, r.Category, r.Visit_id, r.Url, r.Title,
                        STRFTIME('%Y-%m-%d %H:%M:%f', r.Visit_time) AS Visit_time,
                        r.File, r.Label, r.Comment
               FROM results r
               " + resultsWhere + @"
               UNION ALL
               SELECT f.id, f.Artifact_type, f.Potential_activity, f.Navigation_context, f.User_action_likelihood, f.Browser, f.Category, f.Visit_id, f.Url, f.Title,
                        STRFTIME('%Y-%m-%d %H:%M:%f', f.Visit_time) AS Visit_time,
                        f.File, f.Label, f.Comment
               FROM firefox_results f
               " + firefoxWhere + ";"
                    : $@"SELECT r.id, r.Artifact_type, r.Potential_activity, NULL AS Navigation_context, NULL AS User_action_likelihood, r.Browser, r.Category, r.Visit_id, r.Url, r.Title,
                        STRFTIME('%Y-%m-%d %H:%M:%f', r.Visit_time, '{offset}') AS Visit_time,
                        r.File, r.Label, r.Comment
               FROM results r
               {resultsWhere}
               UNION ALL
               SELECT f.id, f.Artifact_type, f.Potential_activity, f.Navigation_context, f.User_action_likelihood, f.Browser, f.Category, f.Visit_id, f.Url, f.Title,
                        STRFTIME('%Y-%m-%d %H:%M:%f', f.Visit_time, '{offset}') AS Visit_time,
                        f.File, f.Label, f.Comment
               FROM firefox_results f
               {firefoxWhere};";

                Tools.ShowQueryOnDataGridView(sfDataGrid1, Helpers.chromeViewerConnectionString, sqlquery, labelItemCount, Console);
                labelStatus.Text = "All web history from all browsers";
            };
            lblAllHistoryButton.MouseHover += (sender, e) => lblAllHistoryButton.BackColor = Color.LightBlue;
            lblAllHistoryButton.MouseLeave += (sender, e) => lblAllHistoryButton.BackColor = Color.White;

            lblAllHistoryButton.Text = $"All Web History {historyCount} hits";

            if (!isFilteredView || historyCount > 0)
            {
                flwMain.Controls.Add(lblAllHistoryButton);
            }

            IEnumerable<string> historyBrowsers = useHistorySummary
                ? Helpers.browserHistoryCounts.Keys.OrderBy(key => key)
                : navegadorCategorias.Keys.OrderBy(key => key);

            foreach (string browserName in historyBrowsers)
            {
                navegadorCategorias.TryGetValue(browserName, out List<string>? browserUrls);
                browserUrls ??= new List<string>();

                Image icono = Helpers.GetBrowserImage(browserName);

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
                    Text = browserName,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Tag = "fullwidth"
                };
                lblNavegador.Width =
                    flwMain.ClientSize.Width - flwMain.Padding.Horizontal - lblNavegador.Margin.Horizontal;

                lblNavegador.MouseHover += (s, e) => lblNavegador.BackColor = Color.LightBlue;
                lblNavegador.MouseLeave += (s, e) => lblNavegador.BackColor = Color.SteelBlue;

                Helpers.navegadorLabels[browserName] = lblNavegador;
                Helpers.itemscount = isFilteredView
                    ? (Helpers.historyHits.TryGetValue(browserName, out int count) ? count : 0)
                    : (Helpers.browserHistoryCounts.TryGetValue(browserName, out int summaryCount) ? summaryCount : browserUrls.Count);

                Tools.UpdateNavegadorLabel(browserName, browserName + " " + Helpers.itemscount + " hits");

                var flwSubNavegador = new FlowLayoutPanel
                {
                    Name = "flwSubNavegador",
                    BackColor = Color.Beige,
                    FlowDirection = FlowDirection.TopDown,
                    WrapContents = false,
                    AutoScroll = false,
                    AutoSize = true,
                    Height = 200,
                    Margin = new Padding(0, 4, 0, 4),
                    Padding = new Padding(4),
                    Tag = "fullwidth"
                };
                flwSubNavegador.Width =
                    flwMain.ClientSize.Width - flwMain.Padding.Horizontal - flwSubNavegador.Margin.Horizontal;

                flwSubNavegador.SizeChanged += (s, e) =>
                {
                    int usable = flwSubNavegador.ClientSize.Width - flwSubNavegador.Padding.Horizontal;
                    foreach (Control c in flwSubNavegador.Controls)
                        if ((c.Tag as string) == "fullwidth")
                            c.Width = Math.Max(0, usable - c.Margin.Horizontal);
                };

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
                    Tag = "fullwidth"
                };
                lblAllHistory.Width =
                    flwSubNavegador.ClientSize.Width - flwSubNavegador.Padding.Horizontal - lblAllHistory.Margin.Horizontal;

                lblAllHistory.Click += (sender, e) =>
                {
                    string tableName = Helpers.RequiresCombinedHistoryQuery(browserName)
                        ? "history_union"
                        : Helpers.HistoryTableForBrowser(browserName);
                    new MyTools().MostrarPorCategoria(labelStatus, sfDataGrid1, Helpers.chromeViewerConnectionString,
                                                      tableName, null, browserName, labelItemCount, Console);
                };
                lblAllHistory.MouseHover += (s, e) => lblAllHistory.BackColor = Color.LightBlue;
                lblAllHistory.MouseLeave += (s, e) => lblAllHistory.BackColor = Color.Transparent;

                flwSubNavegador.Controls.Add(lblAllHistory);

                Dictionary<string, int> categoryCounts;
                if (useHistorySummary && Helpers.browserHistoryCategoryCounts.TryGetValue(browserName, out Dictionary<string, int>? summaryCategoryCounts))
                {
                    categoryCounts = summaryCategoryCounts;
                }
                else
                {
                    categoryCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    foreach (var url in browserUrls)
                    {
                        string categoria = MyTools.Evaluatecategory(url);
                        categoryCounts[categoria] = categoryCounts.TryGetValue(categoria, out int existingCount) ? existingCount + 1 : 1;
                    }
                }

                foreach (var categoria in categoryCounts.OrderBy(c => c.Key))
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
                        Text = $"{categoria.Key} ({categoria.Value} hits)",
                        TextAlign = ContentAlignment.MiddleCenter,
                        AutoSize = false,
                        Height = Math.Max(icono.Height + 5, fs.Height + 10),
                        Margin = new Padding(3, 1, 3, 1),
                        Tag = "fullwidth"
                    };
                    lblCategoria.Width =
                        flwSubNavegador.ClientSize.Width - flwSubNavegador.Padding.Horizontal - lblCategoria.Margin.Horizontal;

                    lblCategoria.Click += (sender, e) =>
                    {
                        string tableName = Helpers.RequiresCombinedHistoryQuery(browserName)
                            ? "history_union"
                            : Helpers.HistoryTableForBrowser(browserName);
                        new MyTools().MostrarPorCategoria(labelStatus, sfDataGrid1, Helpers.chromeViewerConnectionString,
                                                          tableName, categoria.Key, browserName, labelItemCount, Console);
                    };
                    lblCategoria.MouseHover += (s, e) => lblCategoria.BackColor = Color.LightBlue;
                    lblCategoria.MouseLeave += (s, e) => lblCategoria.BackColor = Color.Transparent;

                    flwSubNavegador.Controls.Add(lblCategoria);
                }

                flwMain.Controls.Add(lblNavegador);
                flwMain.Controls.Add(flwSubNavegador);

                flwSubNavegador.Visible = false;
                lblNavegador.Click += (s, e) => flwSubNavegador.Visible = !flwSubNavegador.Visible;
            }

            if (!isFilteredView || Helpers.browsersWithDownloads.Any()) AddDownloadLabels(flwMain, Helpers.browsersWithDownloads);
            if (!isFilteredView || Helpers.browsersWithBookmarks.Any()) AddBookmarkLabels(flwMain, Helpers.browsersWithBookmarks);
            if (!isFilteredView || Helpers.browsersWithAutofill.Any()) AddAutofillLabels(flwMain, Helpers.browsersWithAutofill);
            if (!isFilteredView || Helpers.browsersWithCookies.Any()) AddCookieLabels(flwMain, Helpers.browsersWithCookies);
            if (!isFilteredView || Helpers.browsersWithCache.Any()) AddCacheLabels(flwMain, Helpers.browsersWithCache);
            if (!isFilteredView || Helpers.browsersWithSessions.Any()) AddSessionLabels(flwMain, Helpers.browsersWithSessions);
            if (!isFilteredView || Helpers.browsersWithExtensions.Any()) AddExtensionLabels(flwMain, Helpers.browsersWithExtensions);
            if (!isFilteredView || Helpers.browsersWithLogins.Any()) AddLoginLabels(flwMain, Helpers.browsersWithLogins);
            if (!isFilteredView || Helpers.browsersWithLocalStorage.Any()) AddLocalStorageLabels(flwMain, Helpers.browsersWithLocalStorage);
            if (!isFilteredView || Helpers.browsersWithSessionStorage.Any()) AddSessionStorageLabels(flwMain, Helpers.browsersWithSessionStorage);
            if (!isFilteredView || Helpers.browsersWithIndexedDb.Any()) AddIndexedDbLabels(flwMain, Helpers.browsersWithIndexedDb);

            flwMain.ResumeLayout(true);
            var args = EventArgs.Empty;
            groupBox_Main.ResumeLayout(true);
            groupBox_Main.PerformLayout();
            flwMain.PerformLayout();
            int usable2 = flwMain.ClientSize.Width - flwMain.Padding.Horizontal;
            foreach (Control c in flwMain.Controls)
                if ((c.Tag as string) == "fullwidth")
                    c.Width = Math.Max(0, usable2 - c.Margin.Horizontal);
        }

        private bool IsSearchFilterActive()
        {
            return Helpers.searchTermExists || Helpers.searchTimeCondition || Helpers.searchLabelsOnly;
        }

        private string CurrentWhereFilter(params string[] conditions)
        {
            return SearchSql.Where(conditions.Append(SearchSql.LabelCondition()).ToArray());
        }

        private string CurrentAndFilter(params string[] conditions)
        {
            return SearchSql.And(conditions.Append(SearchSql.LabelCondition()).ToArray());
        }


        private void AddAllWebArtifactsLabels(FlowLayoutPanel flwMain)
        {
            Font fm = Helpers.FM;
            Font fs = Helpers.FS;
            int allArtifactsCount = GetAllWebArtifactsCount();

            var lblAllArtifactsButton = new Label
            {
                BackColor = Color.White,
                Font = new Font(fm, FontStyle.Bold),
                ForeColor = Color.Black,
                AutoSize = false,
                Height = 48,
                Margin = new Padding(3, 1, 3, 1),
                Padding = new Padding(12, 3, 0, 3),
                Text = $"All Web Artifacts {allArtifactsCount} hits",
                TextAlign = ContentAlignment.MiddleCenter,
                Tag = "fullwidth"
            };
            lblAllArtifactsButton.Width =
                flwMain.ClientSize.Width - flwMain.Padding.Horizontal - lblAllArtifactsButton.Margin.Horizontal;

            var flwSubArtifacts = new FlowLayoutPanel
            {
                Name = "flwSubAllWebArtifacts",
                BackColor = Color.Beige,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = false,
                AutoSize = true,
                Margin = new Padding(0, 4, 0, 4),
                Padding = new Padding(4),
                Tag = "fullwidth",
                Visible = false
            };
            flwSubArtifacts.Width =
                flwMain.ClientSize.Width - flwMain.Padding.Horizontal - flwSubArtifacts.Margin.Horizontal;

            flwSubArtifacts.SizeChanged += (s, e) =>
            {
                int usable = flwSubArtifacts.ClientSize.Width - flwSubArtifacts.Padding.Horizontal;
                foreach (Control c in flwSubArtifacts.Controls)
                    if ((c.Tag as string) == "fullwidth")
                        c.Width = Math.Max(0, usable - c.Margin.Horizontal);
            };

            Image icono = Resource1.AllHistory.ToBitmap();
            var lblFullTimeline = new Label
            {
                BackColor = Color.Transparent,
                Font = fs,
                ForeColor = Color.Black,
                Image = icono,
                ImageAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(32, 1, 0, 1),
                Text = "Full time line web activity",
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                Height = Math.Max(icono.Height + 5, fs.Height + 10),
                Margin = new Padding(3, 1, 3, 1),
                Tag = "fullwidth"
            };
            lblFullTimeline.Width =
                flwSubArtifacts.ClientSize.Width - flwSubArtifacts.Padding.Horizontal - lblFullTimeline.Margin.Horizontal;

            lblFullTimeline.Click += (sender, e) =>
            {
                string sqlquery = BuildAllWebArtifactsTimelineQuery();
                Tools.ShowQueryOnDataGridView(sfDataGrid1, Helpers.chromeViewerConnectionString, sqlquery, labelItemCount, Console);
                labelStatus.Text = "Full time line web activity";
            };

            lblAllArtifactsButton.Click += (sender, e) => flwSubArtifacts.Visible = !flwSubArtifacts.Visible;
            lblAllArtifactsButton.MouseHover += (sender, e) => lblAllArtifactsButton.BackColor = Color.LightBlue;
            lblAllArtifactsButton.MouseLeave += (sender, e) => lblAllArtifactsButton.BackColor = Color.White;
            lblFullTimeline.MouseHover += (sender, e) => lblFullTimeline.BackColor = Color.LightBlue;
            lblFullTimeline.MouseLeave += (sender, e) => lblFullTimeline.BackColor = Color.Transparent;

            flwSubArtifacts.Controls.Add(lblFullTimeline);
            flwMain.Controls.Add(lblAllArtifactsButton);
            flwMain.Controls.Add(flwSubArtifacts);
        }

        private int GetAllWebArtifactsCount()
        {
            if (!IsSearchFilterActive())
            {
                return Helpers.browserHistoryCounts.Values.Sum()
                    + Helpers.browsersWithDownloads.Values.Sum()
                    + Helpers.browsersWithBookmarks.Values.Sum()
                    + Helpers.browsersWithAutofill.Values.Sum()
                    + Helpers.browsersWithCookies.Values.Sum()
                    + Helpers.browsersWithCache.Values.Sum()
                    + Helpers.browsersWithSessions.Values.Sum()
                    + Helpers.browsersWithExtensions.Values.Sum()
                    + Helpers.browsersWithLogins.Values.Sum()
                    + Helpers.browsersWithLocalStorage.Values.Sum()
                    + Helpers.browsersWithSessionStorage.Values.Sum()
                    + Helpers.browsersWithIndexedDb.Values.Sum();
            }

            try
            {
                using SQLiteConnection connection = new SQLiteConnection(Helpers.chromeViewerConnectionString);
                connection.Open();
                using SQLiteCommand command = new SQLiteCommand(BuildAllWebArtifactsCountQuery(), connection);
                SearchSql.AddParameters(command);
                object result = command.ExecuteScalar();
                return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
            }
            catch
            {
                return 0;
            }
        }

        private static void DisposeChildControls(Control parent)
        {
            for (int i = parent.Controls.Count - 1; i >= 0; i--)
            {
                Control child = parent.Controls[i];
                parent.Controls.RemoveAt(i);
                child.Dispose();
            }
        }

        private string BuildAllWebArtifactsCountQuery()
        {
            return $@"
                SELECT COUNT(*) FROM (
                    SELECT 1 FROM results {SearchSql.Where(SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Category", "Url", "Title"), SearchSql.TimeCondition("Visit_time", "Last_visit_time"), SearchSql.LabelCondition())}
                    UNION ALL SELECT 1 FROM firefox_results {SearchSql.Where(SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Category", "Url", "Title", "Transition", "Navigation_context", "User_action_likelihood"), SearchSql.TimeCondition("Visit_time", "Last_visit_time"), SearchSql.LabelCondition())}
                    UNION ALL SELECT 1 FROM chrome_downloads {SearchSql.Where(SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Current_path", "Target_path", "Url_chain", "State", "referrer", "Site_url", "Tab_url", "Mime_type"), SearchSql.TimeCondition("Start_time", "End_time"), SearchSql.LabelCondition())}
                    UNION ALL SELECT 1 FROM firefox_downloads {SearchSql.Where(SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Current_path", "Source_url", "Title", "State"), SearchSql.TimeCondition("End_time", "Last_visit_time"), SearchSql.LabelCondition())}
                    UNION ALL SELECT 1 FROM bookmarks_Chrome {SearchSql.Where(SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Type", "Title", "URL", "Parent_name", "Guid", "ChromeId"), SearchSql.TimeCondition("DateAdded", "DateLastUsed", "LastModified"), SearchSql.LabelCondition())}
                    UNION ALL SELECT 1 FROM bookmarks_Firefox {SearchSql.Where(SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Type", "Parent_name", "Title", "URL", "PageTitle", "AnnoContent", "AnnoName"), SearchSql.TimeCondition("DateAdded", "LastModified", "LastVisitDate"), SearchSql.LabelCondition())}
                    UNION ALL SELECT 1 FROM autofill_data {SearchSql.Where(SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "FieldName", "Value"), SearchSql.TimeCondition("LastUsed", "FirstUsed"), SearchSql.LabelCondition())}
                    UNION ALL SELECT 1 FROM cookies_data {SearchSql.Where(SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Host", "Name", "Value", "Path"), SearchSql.TimeCondition("Created", "Expires", "LastAccessed"), SearchSql.LabelCondition())}
                    UNION ALL SELECT 1 FROM cache_data {SearchSql.Where(SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Url", "Host", "ContentType", "CacheType", "Server", "CacheFile", "CacheKey", "BodyPreview", "DetectedFileType", "DetectedExtension"), SearchSql.TimeCondition("Created", "Modified", "LastAccessed"), SearchSql.LabelCondition())}
                    UNION ALL SELECT 1 FROM session_data {SearchSql.Where(SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Url", "Title", "OriginalUrl", "Referrer", "SessionFile", "SourceType"), SearchSql.TimeCondition("LastAccessed", "Created"), SearchSql.LabelCondition())}
                    UNION ALL SELECT 1 FROM extension_data {SearchSql.Where(SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "ExtensionId", "Name", "Version", "Description", "Author", "HomepageUrl", "UpdateUrl", "Permissions", "HostPermissions", "ExtensionPath", "SourceFile"), SearchSql.TimeCondition("InstallTime", "LastUpdateTime"), SearchSql.LabelCondition())}
                    UNION ALL SELECT 1 FROM saved_logins_data {SearchSql.Where(SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Url", "Action_url", "Signon_realm", "Username", "Username_field", "Password_field", "Scheme", "Decryption_status", "Credential_artifact_value", "Store", "Login_guid"), SearchSql.TimeCondition("Created", "Last_used", "Password_changed"), SearchSql.LabelCondition())}
                    UNION ALL SELECT 1 FROM local_storage_data {SearchSql.Where(SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Origin", "Host", "Storage_key", "Value_preview", "Value_sha256", "Source_kind", "Source_file", "Parser_notes"), SearchSql.TimeCondition("Created", "Modified", "LastAccessed"), SearchSql.LabelCondition())}
                    UNION ALL SELECT 1 FROM session_storage_data {SearchSql.Where(SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Origin", "Host", "Storage_key", "Value_preview", "Value_sha256", "Source_kind", "Source_file", "Parser_notes"), SearchSql.TimeCondition("Created", "Modified", "LastAccessed"), SearchSql.LabelCondition())}
                    UNION ALL SELECT 1 FROM indexeddb_data {SearchSql.Where(SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Origin", "Host", "Storage_key", "Value_preview", "Value_sha256", "Source_kind", "Source_file", "Parser_notes"), SearchSql.TimeCondition("Created", "Modified", "LastAccessed"), SearchSql.LabelCondition())}
                );";
        }

        private string BuildAllWebArtifactsTimelineQuery()
        {
            string visitTime = SearchSql.DateExpr("Visit_time");
            string lastVisitTime = SearchSql.DateExpr("Last_visit_time");
            string startTime = SearchSql.DateExpr("Start_time");
            string endTime = SearchSql.DateExpr("End_time");
            string dateAdded = SearchSql.DateExpr("DateAdded");
            string lastModified = SearchSql.DateExpr("LastModified");
            string firstUsed = SearchSql.DateExpr("FirstUsed");
            string lastUsed = SearchSql.DateExpr("LastUsed");
            string created = SearchSql.DateExpr("Created");
            string expires = SearchSql.DateExpr("Expires");
            string lastAccessed = SearchSql.DateExpr("LastAccessed");
            string modified = SearchSql.DateExpr("Modified");
            string installTime = SearchSql.DateExpr("InstallTime");
            string lastUpdateTime = SearchSql.DateExpr("LastUpdateTime");
            string loginCreated = SearchSql.DateExpr("Created");
            string loginLastUsed = SearchSql.DateExpr("Last_used");
            string passwordChanged = SearchSql.DateExpr("Password_changed");
            string localStorageModified = SearchSql.DateExpr("Modified");
            string localStorageAccessed = SearchSql.DateExpr("LastAccessed");
            string sessionStorageModified = SearchSql.DateExpr("Modified");
            string sessionStorageAccessed = SearchSql.DateExpr("LastAccessed");
            string indexedDbModified = SearchSql.DateExpr("Modified");
            string indexedDbAccessed = SearchSql.DateExpr("LastAccessed");

            return $@"
                SELECT * FROM (
                    SELECT id, Artifact_type, Potential_activity, 'results' AS Source_table, id AS Source_id, Browser, 'History' AS Artifact, {visitTime} AS Activity_time, {lastVisitTime} AS Secondary_time,
                           Url, Title, Category AS Detail, File, Label, Comment
                    FROM results
                    {SearchSql.Where(SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Category", "Url", "Title"), SearchSql.TimeCondition("Visit_time", "Last_visit_time"), SearchSql.LabelCondition())}

                    UNION ALL
                    SELECT id, Artifact_type, Potential_activity, 'firefox_results' AS Source_table, id AS Source_id, Browser, 'History' AS Artifact, {visitTime} AS Activity_time, {lastVisitTime} AS Secondary_time,
                           Url, Title, COALESCE(Navigation_context, Category, '') AS Detail, File, Label, Comment
                    FROM firefox_results
                    {SearchSql.Where(SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Category", "Url", "Title", "Transition", "Navigation_context", "User_action_likelihood"), SearchSql.TimeCondition("Visit_time", "Last_visit_time"), SearchSql.LabelCondition())}

                    UNION ALL
                    SELECT id, Artifact_type, Potential_activity, 'chrome_downloads' AS Source_table, id AS Source_id, Browser, 'Download' AS Artifact, {endTime} AS Activity_time, {startTime} AS Secondary_time,
                           COALESCE(Tab_url, Site_url, referrer, Target_path, Current_path) AS Url, Current_path AS Title,
                           COALESCE(Url_chain, Mime_type, State, '') AS Detail, File, Label, Comment
                    FROM chrome_downloads
                    {SearchSql.Where(SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Current_path", "Target_path", "Url_chain", "State", "referrer", "Site_url", "Tab_url", "Mime_type"), SearchSql.TimeCondition("Start_time", "End_time"), SearchSql.LabelCondition())}

                    UNION ALL
                    SELECT id, Artifact_type, Potential_activity, 'firefox_downloads' AS Source_table, id AS Source_id, Browser, 'Download' AS Artifact, {endTime} AS Activity_time, {lastVisitTime} AS Secondary_time,
                           Source_url AS Url, Current_path AS Title, COALESCE(State, '') AS Detail, File, Label, Comment
                    FROM firefox_downloads
                    {SearchSql.Where(SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Current_path", "Source_url", "Title", "State"), SearchSql.TimeCondition("End_time", "Last_visit_time"), SearchSql.LabelCondition())}

                    UNION ALL
                    SELECT id, Artifact_type, Potential_activity, 'bookmarks_Chrome' AS Source_table, id AS Source_id, Browser, 'Bookmark' AS Artifact, {dateAdded} AS Activity_time, {lastModified} AS Secondary_time,
                           URL AS Url, Title, COALESCE(Type, Parent_name, '') AS Detail, File, Label, Comment
                    FROM bookmarks_Chrome
                    {SearchSql.Where(SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Type", "Title", "URL", "Parent_name", "Guid", "ChromeId"), SearchSql.TimeCondition("DateAdded", "DateLastUsed", "LastModified"), SearchSql.LabelCondition())}

                    UNION ALL
                    SELECT id, Artifact_type, Potential_activity, 'bookmarks_Firefox' AS Source_table, id AS Source_id, Browser, 'Bookmark' AS Artifact, {dateAdded} AS Activity_time, {lastModified} AS Secondary_time,
                           URL AS Url, COALESCE(Title, PageTitle) AS Title, COALESCE(Type, Parent_name, AnnoName, '') AS Detail, File, Label, Comment
                    FROM bookmarks_Firefox
                    {SearchSql.Where(SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Type", "Parent_name", "Title", "URL", "PageTitle", "AnnoContent", "AnnoName"), SearchSql.TimeCondition("DateAdded", "LastModified", "LastVisitDate"), SearchSql.LabelCondition())}

                    UNION ALL
                    SELECT id, Artifact_type, Potential_activity, 'autofill_data' AS Source_table, id AS Source_id, Browser, 'Autofill' AS Artifact, {lastUsed} AS Activity_time, {firstUsed} AS Secondary_time,
                           Value AS Url, FieldName AS Title, COALESCE(CAST(TimesUsed AS TEXT), CAST(Count AS TEXT), '') AS Detail, File, Label, Comment
                    FROM autofill_data
                    {SearchSql.Where(SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "FieldName", "Value"), SearchSql.TimeCondition("LastUsed", "FirstUsed"), SearchSql.LabelCondition())}

                    UNION ALL
                    SELECT id, Artifact_type, Potential_activity, 'cookies_data' AS Source_table, id AS Source_id, Browser, 'Cookie' AS Artifact, {lastAccessed} AS Activity_time, {created} AS Secondary_time,
                           Host AS Url, Name AS Title, Path AS Detail, File, Label, Comment
                    FROM cookies_data
                    {SearchSql.Where(SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Host", "Name", "Value", "Path"), SearchSql.TimeCondition("Created", "Expires", "LastAccessed"), SearchSql.LabelCondition())}

                    UNION ALL
                    SELECT id, Artifact_type, Potential_activity, 'cache_data' AS Source_table, id AS Source_id, Browser, 'Cache' AS Artifact, {lastAccessed} AS Activity_time, {created} AS Secondary_time,
                           Url, COALESCE(DetectedFileType, ContentType, CacheType) AS Title, COALESCE(Host, CacheKey, CacheFile, '') AS Detail, File, Label, Comment
                    FROM cache_data
                    {SearchSql.Where(SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Url", "Host", "ContentType", "CacheType", "Server", "CacheFile", "CacheKey", "BodyPreview", "DetectedFileType", "DetectedExtension"), SearchSql.TimeCondition("Created", "Modified", "LastAccessed"), SearchSql.LabelCondition())}

                    UNION ALL
                    SELECT id, Artifact_type, Potential_activity, 'session_data' AS Source_table, id AS Source_id, Browser, 'Session' AS Artifact, {lastAccessed} AS Activity_time, {created} AS Secondary_time,
                           Url, Title, COALESCE(SourceType, SessionFile, '') AS Detail, File, Label, Comment
                    FROM session_data
                    {SearchSql.Where(SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Url", "Title", "OriginalUrl", "Referrer", "SessionFile", "SourceType"), SearchSql.TimeCondition("LastAccessed", "Created"), SearchSql.LabelCondition())}

                    UNION ALL
                    SELECT id, Artifact_type, Potential_activity, 'extension_data' AS Source_table, id AS Source_id, Browser, 'Extension' AS Artifact, {installTime} AS Activity_time, {lastUpdateTime} AS Secondary_time,
                           HomepageUrl AS Url, Name AS Title, COALESCE(ExtensionId, Version, Description, '') AS Detail, File, Label, Comment
                    FROM extension_data
                    {SearchSql.Where(SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "ExtensionId", "Name", "Version", "Description", "Author", "HomepageUrl", "UpdateUrl", "Permissions", "HostPermissions", "ExtensionPath", "SourceFile"), SearchSql.TimeCondition("InstallTime", "LastUpdateTime"), SearchSql.LabelCondition())}

                    UNION ALL
                    SELECT id, Artifact_type, Potential_activity, 'saved_logins_data' AS Source_table, id AS Source_id, Browser, 'Saved Login' AS Artifact, {loginLastUsed} AS Activity_time, {loginCreated} AS Secondary_time,
                           COALESCE(Url, Action_url, Signon_realm) AS Url, COALESCE(Username, Username_field, Signon_realm) AS Title,
                           COALESCE(Credential_artifact_value, Decryption_status, Store, Scheme, Login_guid, '') AS Detail, File, Label, Comment
                    FROM saved_logins_data
                    {SearchSql.Where(SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Url", "Action_url", "Signon_realm", "Username", "Username_field", "Password_field", "Scheme", "Decryption_status", "Credential_artifact_value", "Store", "Login_guid"), SearchSql.TimeCondition("Created", "Last_used", "Password_changed"), SearchSql.LabelCondition())}

                    UNION ALL
                    SELECT id, Artifact_type, Potential_activity, 'local_storage_data' AS Source_table, id AS Source_id, Browser, 'Local Storage' AS Artifact, {localStorageModified} AS Activity_time, {localStorageAccessed} AS Secondary_time,
                           Origin AS Url, Storage_key AS Title, COALESCE(Host, Source_kind, Parser_notes, '') AS Detail, File, Label, Comment
                    FROM local_storage_data
                    {SearchSql.Where(SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Origin", "Host", "Storage_key", "Value_preview", "Value_sha256", "Source_kind", "Source_file", "Parser_notes"), SearchSql.TimeCondition("Created", "Modified", "LastAccessed"), SearchSql.LabelCondition())}

                    UNION ALL
                    SELECT id, Artifact_type, Potential_activity, 'session_storage_data' AS Source_table, id AS Source_id, Browser, 'Session Storage' AS Artifact, {sessionStorageModified} AS Activity_time, {sessionStorageAccessed} AS Secondary_time,
                           Origin AS Url, Storage_key AS Title, COALESCE(Host, Source_kind, Parser_notes, '') AS Detail, File, Label, Comment
                    FROM session_storage_data
                    {SearchSql.Where(SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Origin", "Host", "Storage_key", "Value_preview", "Value_sha256", "Source_kind", "Source_file", "Parser_notes"), SearchSql.TimeCondition("Created", "Modified", "LastAccessed"), SearchSql.LabelCondition())}

                    UNION ALL
                    SELECT id, Artifact_type, Potential_activity, 'indexeddb_data' AS Source_table, id AS Source_id, Browser, 'IndexedDB' AS Artifact, {indexedDbModified} AS Activity_time, {indexedDbAccessed} AS Secondary_time,
                           Origin AS Url, Storage_key AS Title, COALESCE(Host, Source_kind, Parser_notes, '') AS Detail, File, Label, Comment
                    FROM indexeddb_data
                    {SearchSql.Where(SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Origin", "Host", "Storage_key", "Value_preview", "Value_sha256", "Source_kind", "Source_file", "Parser_notes"), SearchSql.TimeCondition("Created", "Modified", "LastAccessed"), SearchSql.LabelCondition())}
                )
                ORDER BY Activity_time DESC;";
        }


        private void AddDownloadLabels(FlowLayoutPanel flwMain, Dictionary<string, int> browsersWithDownloads)
        {
            Font fm = Helpers.FM;
            Font fs = Helpers.FS;


            Label lblAllDownloadsButton = new Label()
            {
                BackColor = Color.White,
                Font = new Font(fm, FontStyle.Bold),
                ForeColor = Color.Black,
                AutoSize = false,
                Height = 48,
                Margin = new Padding(3, 1, 3, 1),
                Padding = new Padding(12, 3, 0, 3),
                Text = $"All Downloads {browsersWithDownloads.Values.Sum()} hits",
                TextAlign = ContentAlignment.MiddleCenter,
                Tag = "fullwidth"
            };

            lblAllDownloadsButton.Width =
                flwMain.ClientSize.Width - flwMain.Padding.Horizontal - lblAllDownloadsButton.Margin.Horizontal;


            lblAllDownloadsButton.Click += (sender, e) =>
            {
                int utcOffset = Helpers.utcOffset;
                string sqlquery;
                string chromeWhere = CurrentWhereFilter(
                    SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Current_path", "Target_path", "Url_chain", "State", "Referrer", "Site_url", "Tab_url", "Mime_type"),
                    SearchSql.TimeCondition("Start_time", "End_time"));
                string firefoxWhere = CurrentWhereFilter(
                    SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Current_path", "Source_url", "Title", "State"),
                    SearchSql.TimeCondition("End_time", "Last_visit_time"));

                if (utcOffset == 0)
                {
                    sqlquery = $@"SELECT 
                                 id,
                                 Artifact_type,
                                 Potential_activity,
                                 Browser,
                                 Current_path,
                                 Target_path,
                                 Url_chain,
                                 STRFTIME('%Y-%m-%d %H:%M:%f', End_time) AS End_time,
                                 Received_bytes,
                                 Total_bytes,
                                 File, Label, Comment
                             FROM 
                                 chrome_downloads
                             {chromeWhere}
                             UNION ALL
                             SELECT 
                                 id,
                                 Artifact_type,
                                 Potential_activity,
                                 Browser,
                                 Current_path,
                                 NULL AS Target_path,
                                 NULL AS Url_chain,
                                 STRFTIME('%Y-%m-%d %H:%M:%f', End_time) AS End_time,
                                 Received_bytes,
                                 Total_bytes,
                                 File, Label, Comment
                             FROM 
                                 firefox_downloads
                             {firefoxWhere};";
                }
                else
                {
                    string offset = utcOffset > 0 ? $"+{utcOffset} hours" : $"{utcOffset} hours";

                    sqlquery = $@"SELECT 
                                 id,
                                 Artifact_type,
                                 Potential_activity,
                                 Browser,
                                 Current_path,
                                 Target_path,
                                 Url_chain,
                                 STRFTIME('%Y-%m-%d %H:%M:%f', End_time, '{offset}') AS End_time,
                                 Received_bytes,
                                 Total_bytes,
                                 File, Label, Comment
                             FROM 
                                 chrome_downloads
                             {chromeWhere}
                             UNION ALL
                             SELECT 
                                 id,
                                 Artifact_type,
                                 Potential_activity,
                                 Browser,
                                 Current_path,
                                 NULL AS Target_path,
                                 NULL AS Url_chain,
                                 STRFTIME('%Y-%m-%d %H:%M:%f', End_time, '{offset}') AS End_time,
                                 Received_bytes,
                                 Total_bytes,
                                 File, Label, Comment
                             FROM 
                                 firefox_downloads
                             {firefoxWhere};";
                }



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
                Image? icono = null;

                icono = Helpers.GetBrowserImage(navegador);


                Label lblNavegadorDownloads = new Label()
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

                lblNavegadorDownloads.Width =
                    flwMain.ClientSize.Width - flwMain.Padding.Horizontal - lblNavegadorDownloads.Margin.Horizontal;

                flwMain.Controls.Add(lblNavegadorDownloads);


                Helpers.downloadsLabels[navegador] = lblNavegadorDownloads;

                lblNavegadorDownloads.MouseHover += (sender, e) => { lblNavegadorDownloads.BackColor = Color.LightBlue; };
                lblNavegadorDownloads.MouseLeave += (sender, e) => { lblNavegadorDownloads.BackColor = Color.SteelBlue; };

                lblNavegadorDownloads.Click += (sender, e) =>
                {
                    int utcOffset = Helpers.utcOffset;
                    string sqlquery;
                    string searchCondition;
                    string labelCondition = SearchSql.And(SearchSql.LabelCondition());
                    if (Helpers.searchTermExists)
                    {
                        searchCondition = "";

                        if (Helpers.IsFirefoxLikeBrowser(navegador))
                        {
                            searchCondition = SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Current_path", "Source_url", "Title", "State");
                            if (utcOffset == 0)
                            {
                                sqlquery = $@"SELECT 
                                            id,
                                            Artifact_type,
                                            Potential_activity,
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
                                        WHERE Browser = '{navegador}' AND {searchCondition} {Helpers.sqlDownloadtimecondition} {labelCondition};";
                            }
                            else
                            {
                                string offset = utcOffset > 0 ? $"+{utcOffset} hours" : $"{utcOffset} hours";

                                sqlquery = $@"SELECT 
                                            id,
                                            Artifact_type,
                                            Potential_activity,
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
                                        WHERE Browser = '{navegador}' AND {searchCondition} {Helpers.sqlDownloadtimecondition} {labelCondition};";
                            }
                        }
                        else
                        {
                            searchCondition = SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Current_path", "Target_path", "Url_chain", "State", "Referrer", "Site_url", "Tab_url", "Mime_type");
                            if (utcOffset == 0)
                            {
                                sqlquery = $@"SELECT 
                                            id,
                                            Artifact_type,
                                            Potential_activity,
                                            Browser,
                                            Current_path,
                                            Target_path,
                                            Url_chain,
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
                                        WHERE Browser = '{navegador}' AND {searchCondition} {Helpers.sqlDownloadtimecondition} {labelCondition};";
                            }
                            else
                            {
                                string offset = utcOffset > 0 ? $"+{utcOffset} hours" : $"{utcOffset} hours";

                                sqlquery = $@"SELECT 
                                            id,
                                            Artifact_type,
                                            Potential_activity,
                                            Browser,
                                            Current_path,
                                            Target_path,
                                            Url_chain,
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
                                        WHERE Browser = '{navegador}' AND {searchCondition} {Helpers.sqlDownloadtimecondition} {labelCondition};";
                            }
                        }
                    }
                    else
                    {

                        if (Helpers.searchTimeCondition)
                        {
                            Helpers.sqlDownloadtimecondition = $" AND (End_time >= '{Helpers.sd}' AND End_time <= '{Helpers.ed}')";

                        }

                        if (Helpers.IsFirefoxLikeBrowser(navegador))
                        {
                            if (utcOffset == 0)
                            {
                                sqlquery = $@"SELECT 
                                            id,
                                            Artifact_type,
                                            Potential_activity,
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
                                        WHERE Browser = '{navegador}' {Helpers.sqlDownloadtimecondition} {labelCondition};";
                            }
                            else
                            {
                                string offset = utcOffset > 0 ? $"+{utcOffset} hours" : $"{utcOffset} hours";

                                sqlquery = $@"SELECT 
                                            id,
                                            Artifact_type,
                                            Potential_activity,
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
                                        WHERE Browser = '{navegador}' {Helpers.sqlDownloadtimecondition} {labelCondition};";
                            }
                        }
                        else
                        {
                            if (utcOffset == 0)
                            {
                                sqlquery = $@"SELECT 
                                            id,
                                            Artifact_type,
                                            Potential_activity,
                                            Browser,
                                            Current_path,
                                            Target_path,
                                            Url_chain,
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
                                        WHERE Browser = '{navegador}' {Helpers.sqlDownloadtimecondition} {labelCondition};";
                            }
                            else
                            {
                                string offset = utcOffset > 0 ? $"+{utcOffset} hours" : $"{utcOffset} hours";

                                sqlquery = $@"SELECT 
                                            id,
                                            Artifact_type,
                                            Potential_activity,
                                            Browser,
                                            Current_path,
                                            Target_path,
                                            Url_chain,
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
                                        WHERE Browser = '{navegador}' {Helpers.sqlDownloadtimecondition} {labelCondition};";
                            }
                        }
                    }





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



            Label lblAllBookmarksButton = new Label()
            {
                BackColor = Color.White,
                Font = new Font(fm, FontStyle.Bold),
                ForeColor = Color.Black,
                AutoSize = false,
                Height = 48,
                Margin = new Padding(3, 1, 3, 1),
                Padding = new Padding(12, 3, 0, 3),
                Text = $"All Bookmarks {browsersWithBookmarks.Values.Sum()} hits",
                TextAlign = ContentAlignment.MiddleCenter,
                Tag = "fullwidth"
            };

            lblAllBookmarksButton.Width =
                flwMain.ClientSize.Width - flwMain.Padding.Horizontal - lblAllBookmarksButton.Margin.Horizontal;

            lblAllBookmarksButton.Click += (sender, e) =>
            {
                int utcOffset = Helpers.utcOffset;
                string sqlquery;
                string chromeWhere = CurrentWhereFilter(
                    SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Type", "Title", "URL", "Parent_name", "Guid", "ChromeId"),
                    SearchSql.TimeCondition("DateAdded", "DateLastUsed", "LastModified"));
                string firefoxWhere = CurrentWhereFilter(
                    SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Type", "Parent_name", "Title", "URL", "PageTitle", "AnnoContent", "AnnoName"),
                    SearchSql.TimeCondition("DateAdded", "LastModified", "LastVisitDate"));

                if (utcOffset == 0)
                {
                    sqlquery = $@"
                                SELECT 
                                    id,
                                    Artifact_type,
                                    Potential_activity,
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
                                {firefoxWhere}
                                UNION ALL
                                SELECT 
                                    id,
                                    Artifact_type,
                                    Potential_activity,
                                    Browser,
                                    Type,
                                    Title,
                                    URL,
                                    STRFTIME('%Y-%m-%d %H:%M:%f', DateAdded) AS DateAdded,
                                    STRFTIME('%Y-%m-%d %H:%M:%f', LastModified) AS LastModified,
                                    Parent_name,
                                    File, Label, Comment
                                FROM 
                                    bookmarks_Chrome
                                {chromeWhere};";
                }
                else
                {
                    string offset = utcOffset > 0 ? $"+{utcOffset} hours" : $"{utcOffset} hours";

                    sqlquery = $@"
                                SELECT 
                                    id,
                                    Artifact_type,
                                    Potential_activity,
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
                                {firefoxWhere}
                                UNION ALL
                                SELECT 
                                    id,
                                    Artifact_type,
                                    Potential_activity,
                                    Browser,
                                    Type,
                                    Title,
                                    URL,
                                    STRFTIME('%Y-%m-%d %H:%M:%f', DateAdded, '{offset}') AS DateAdded,
                                    STRFTIME('%Y-%m-%d %H:%M:%f', LastModified, '{offset}') AS LastModified,
                                    Parent_name,
                                    File, Label, Comment
                                FROM 
                                    bookmarks_Chrome
                                {chromeWhere};";
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
                Image? icono = null;

                icono = Helpers.GetBrowserImage(navegador);



                Label lblNavegadorBookmarks = new Label()
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

                lblNavegadorBookmarks.Width =
                    flwMain.ClientSize.Width - flwMain.Padding.Horizontal - lblNavegadorBookmarks.Margin.Horizontal;

                Helpers.bookmarksLabels[navegador] = lblNavegadorBookmarks;

                lblNavegadorBookmarks.MouseHover += (sender, e) => { lblNavegadorBookmarks.BackColor = Color.LightBlue; };
                lblNavegadorBookmarks.MouseLeave += (sender, e) => { lblNavegadorBookmarks.BackColor = Color.SteelBlue; };

                lblNavegadorBookmarks.Click += (sender, e) =>
                {
                    int utcOffset = Helpers.utcOffset;
                    string sqlquery;
                    string labelCondition = SearchSql.And(SearchSql.LabelCondition());

                    if (Helpers.searchTermExists)
                    {
                        string searchCondition = SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Type", "Title", "URL", "Parent_name");

                        if (Helpers.IsFirefoxLikeBrowser(navegador))
                        {
                            sqlquery = utcOffset == 0
                                ? $@"SELECT 
                                    id, Artifact_type, Potential_activity, Browser, Type, Title, URL, 
                                    STRFTIME('%Y-%m-%d %H:%M:%f', DateAdded) AS DateAdded,
                                    STRFTIME('%Y-%m-%d %H:%M:%f', LastModified) AS LastModified,
                                    Parent_name, File, Label, Comment
                                    FROM bookmarks_Firefox WHERE Browser = '{navegador}' AND {searchCondition} {Helpers.sqlBookmarkstimecondition} {labelCondition};"
                                                    : $@"SELECT 
                                    id, Artifact_type, Potential_activity, Browser, Type, Title, URL, 
                                    STRFTIME('%Y-%m-%d %H:%M:%f', DateAdded, '{utcOffset} hours') AS DateAdded,
                                    STRFTIME('%Y-%m-%d %H:%M:%f', LastModified, '{utcOffset} hours') AS LastModified,
                                    Parent_name, File, Label, Comment
                                    FROM bookmarks_Firefox WHERE Browser = '{navegador}' AND {searchCondition} {Helpers.sqlBookmarkstimecondition} {labelCondition};";
                        }
                        else
                        {
                            sqlquery = utcOffset == 0
                                ? $@"SELECT 
                                    id, Artifact_type, Potential_activity, Browser, Type, Title, URL, 
                                    STRFTIME('%Y-%m-%d %H:%M:%f', DateAdded) AS DateAdded,
                                    STRFTIME('%Y-%m-%d %H:%M:%f', LastModified) AS LastModified,
                                    Parent_name, File, Label, Comment
                                    FROM bookmarks_Chrome WHERE Browser = '{navegador}' AND {searchCondition} {Helpers.sqlBookmarkstimecondition} {labelCondition};"
                                                    : $@"SELECT 
                                    id, Artifact_type, Potential_activity, Browser, Type, Title, URL, 
                                    STRFTIME('%Y-%m-%d %H:%M:%f', DateAdded, '{utcOffset} hours') AS DateAdded,
                                    STRFTIME('%Y-%m-%d %H:%M:%f', LastModified, '{utcOffset} hours') AS LastModified,
                                    Parent_name, File, Label, Comment
                                    FROM bookmarks_Chrome WHERE Browser = '{navegador}' AND {searchCondition} {Helpers.sqlBookmarkstimecondition} {labelCondition};";
                        }
                    }
                    else
                    {
                        if (Helpers.searchTimeCondition)
                        {
                            Helpers.sqlBookmarkstimecondition = $" AND ((DateAdded >= '{Helpers.sd}' AND DateAdded <= '{Helpers.ed}') OR (LastModified >= '{Helpers.sd}' AND LastModified <= '{Helpers.ed}'))";
                        }
                        if (Helpers.IsFirefoxLikeBrowser(navegador))
                        {
                            sqlquery = utcOffset == 0
                                ? $@"SELECT 
                                    id, Artifact_type, Potential_activity, Browser, Type, Title, URL, 
                                    STRFTIME('%Y-%m-%d %H:%M:%f', DateAdded) AS DateAdded,
                                    STRFTIME('%Y-%m-%d %H:%M:%f', LastModified) AS LastModified,
                                    Parent_name, File, Label, Comment
                                FROM bookmarks_Firefox WHERE Browser = '{navegador}' {Helpers.sqlBookmarkstimecondition} {labelCondition};"
                                                            : $@"SELECT 
                                    id, Artifact_type, Potential_activity, Browser, Type, Title, URL, 
                                    STRFTIME('%Y-%m-%d %H:%M:%f', DateAdded, '{utcOffset} hours') AS DateAdded,
                                    STRFTIME('%Y-%m-%d %H:%M:%f', LastModified, '{utcOffset} hours') AS LastModified,
                                    Parent_name, File, Label, Comment
                                FROM bookmarks_Firefox WHERE Browser = '{navegador}' {Helpers.sqlBookmarkstimecondition} {labelCondition};";
                        }
                        else
                        {
                            sqlquery = utcOffset == 0
                                ? $@"SELECT 
                                    id, Artifact_type, Potential_activity, Browser, Type, Title, URL, 
                                    STRFTIME('%Y-%m-%d %H:%M:%f', DateAdded) AS DateAdded,
                                    STRFTIME('%Y-%m-%d %H:%M:%f', LastModified) AS LastModified,
                                    Parent_name, File, Label, Comment
                                FROM bookmarks_Chrome WHERE Browser = '{navegador}' {Helpers.sqlBookmarkstimecondition} {labelCondition};"
                                                            : $@"SELECT 
                                    id, Artifact_type, Potential_activity, Browser, Type, Title, URL, 
                                    STRFTIME('%Y-%m-%d %H:%M:%f', DateAdded, '{utcOffset} hours') AS DateAdded,
                                    STRFTIME('%Y-%m-%d %H:%M:%f', LastModified, '{utcOffset} hours') AS LastModified,
                                    Parent_name, File, Label, Comment
                                FROM bookmarks_Chrome WHERE Browser = '{navegador}' {Helpers.sqlBookmarkstimecondition} {labelCondition};";
                        }
                    }






                    Tools.ShowQueryOnDataGridView(sfDataGrid1, Helpers.chromeViewerConnectionString, sqlquery, labelItemCount, Console);
                    labelStatus.Text = $"Bookmarks from {navegador}";

                };

                flwMain.Controls.Add(lblNavegadorBookmarks);
            }
        }






        private void AddAutofillLabels(FlowLayoutPanel flwMain, Dictionary<string, int> browsersWithAutofill)
        {

            Font fm = Helpers.FM;
            Font fs = Helpers.FS;



            Label lblAllAutofillButton = new Label()
            {
                BackColor = Color.White,
                Font = new Font(fm, FontStyle.Bold),
                ForeColor = Color.Black,
                AutoSize = false,
                Height = 48,
                Margin = new Padding(3, 1, 3, 1),
                Padding = new Padding(12, 3, 0, 3),
                Text = $"All Autofill Data {browsersWithAutofill.Values.Sum()} hits",
                TextAlign = ContentAlignment.MiddleCenter,
                Tag = "fullwidth"
            };

            lblAllAutofillButton.Width =
                flwMain.ClientSize.Width - flwMain.Padding.Horizontal - lblAllAutofillButton.Margin.Horizontal;

            lblAllAutofillButton.Click += (sender, e) =>
            {
                int utcOffset = Helpers.utcOffset;
                string sqlquery;
                string whereClause = CurrentWhereFilter(
                    SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "FieldName", "Value"),
                    SearchSql.TimeCondition("FirstUsed", "LastUsed"));

                if (utcOffset == 0)
                {
                    sqlquery = $@"
                                SELECT 
                                    id,
                                    Artifact_type,
                                    Potential_activity,
                                    Browser,
                                    FieldName,
                                    Value,
                                    COALESCE(TimesUsed, Count) AS TimesUsed,
                                    STRFTIME('%Y-%m-%d %H:%M:%f', FirstUsed) AS FirstUsed,
                                    STRFTIME('%Y-%m-%d %H:%M:%f', LastUsed) AS LastUsed,
                                    File, Label, Comment
                                FROM 
                                    autofill_data
                                {whereClause};";
                }
                else
                {
                    string offset = utcOffset > 0 ? $"+{utcOffset} hours" : $"{utcOffset} hours";

                    sqlquery = $@"
                                SELECT 
                                    id,
                                    Artifact_type,
                                    Potential_activity,
                                    Browser,
                                    FieldName,
                                    Value,
                                    COALESCE(TimesUsed, Count) AS TimesUsed,
                                    STRFTIME('%Y-%m-%d %H:%M:%f', FirstUsed, '{offset}') AS FirstUsed,
                                    STRFTIME('%Y-%m-%d %H:%M:%f', LastUsed, '{offset}') AS LastUsed,
                                    File, Label, Comment
                                FROM 
                                    autofill_data
                                {whereClause};";
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

                Image? icono = null;
                icono = Helpers.GetBrowserImage(navegador);



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

                lblNavegadorAutofill.Width =
                    flwMain.ClientSize.Width - flwMain.Padding.Horizontal - lblNavegadorAutofill.Margin.Horizontal;

                lblNavegadorAutofill.MouseHover += (sender, e) => { lblNavegadorAutofill.BackColor = Color.LightBlue; };
                lblNavegadorAutofill.MouseLeave += (sender, e) => { lblNavegadorAutofill.BackColor = Color.SteelBlue; };

                lblNavegadorAutofill.Click += (sender, e) =>
                {
                    int utcOffset = Helpers.utcOffset;
                    string sqlquery;
                    string labelCondition = SearchSql.And(SearchSql.LabelCondition());

                    if (Helpers.searchTermExists)
                    {
                        string searchCondition = SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "FieldName", "Value");


                        if (utcOffset == 0)
                        {
                            sqlquery = $@"SELECT id,
                                                Artifact_type,
                                                Potential_activity,
                                                Browser,
                                                FieldName,
                                                Value,
                                                COALESCE(TimesUsed, Count) AS TimesUsed,
                                                STRFTIME('%Y-%m-%d %H:%M:%f', FirstUsed) AS FirstUsed,
                                                STRFTIME('%Y-%m-%d %H:%M:%f', LastUsed) AS LastUsed,
                                                File, Label, Comment
                                            FROM 
                                                autofill_data
                                            WHERE Browser = '{navegador}' AND {searchCondition} {Helpers.sqlAutofilltimecondition} {labelCondition};";
                        }
                        else
                        {
                            string offset = utcOffset > 0 ? $"+{utcOffset} hours" : $"{utcOffset} hours";

                            if (Helpers.searchTimeCondition)
                            {
                                Helpers.sqlAutofilltimecondition = $" AND ((FirstUsed >= '{Helpers.sd}' AND FirstUsed <= '{Helpers.ed}') OR (LastUsed >= '{Helpers.sd}' AND LastUsed <= '{Helpers.ed}'))";
                            }

                            sqlquery = $@"SELECT id,
                                                Artifact_type,
                                                Potential_activity,
                                                Browser,
                                                FieldName,
                                                Value,
                                                COALESCE(TimesUsed, Count) AS TimesUsed,
                                                STRFTIME('%Y-%m-%d %H:%M:%f', FirstUsed, '{offset}') AS FirstUsed,
                                                STRFTIME('%Y-%m-%d %H:%M:%f', LastUsed, '{offset}') AS LastUsed,
                                                File, Label, Comment
                                            FROM 
                                                autofill_data
                                            WHERE Browser = '{navegador}' AND {searchCondition} {Helpers.sqlAutofilltimecondition} {labelCondition};";
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
                            sqlquery = $@"SELECT id,
                                                Artifact_type,
                                                Potential_activity,
                                                Browser,
                                                FieldName,
                                                Value,
                                                COALESCE(TimesUsed, Count) AS TimesUsed,
                                                STRFTIME('%Y-%m-%d %H:%M:%f', FirstUsed) AS FirstUsed,
                                                STRFTIME('%Y-%m-%d %H:%M:%f', LastUsed) AS LastUsed,
                                                File, Label, Comment
                                            FROM 
                                                autofill_data
                                            WHERE Browser = '{navegador}' {Helpers.sqlAutofilltimecondition} {labelCondition};";
                        }
                        else
                        {
                            string offset = utcOffset > 0 ? $"+{utcOffset} hours" : $"{utcOffset} hours";

                            if (Helpers.searchTimeCondition)
                            {
                                Helpers.sqlAutofilltimecondition = $" AND ((FirstUsed >= '{Helpers.sd}' AND FirstUsed <= '{Helpers.ed}') OR (LastUsed >= '{Helpers.sd}' AND LastUsed <= '{Helpers.ed}'))";
                            }

                            sqlquery = $@"SELECT id,
                                                Artifact_type,
                                                Potential_activity,
                                                Browser,
                                                FieldName,
                                                Value,
                                                COALESCE(TimesUsed, Count) AS TimesUsed,
                                                STRFTIME('%Y-%m-%d %H:%M:%f', FirstUsed, '{offset}') AS FirstUsed,
                                                STRFTIME('%Y-%m-%d %H:%M:%f', LastUsed, '{offset}') AS LastUsed,
                                                File, Label, Comment
                                            FROM 
                                                autofill_data
                                            WHERE Browser = '{navegador}' {Helpers.sqlAutofilltimecondition} {labelCondition};";
                        }
                    }



                    Tools.ShowQueryOnDataGridView(sfDataGrid1, Helpers.chromeViewerConnectionString, sqlquery, labelItemCount, Console);
                    labelStatus.Text = $"Autofill data from {navegador}";
                };

                flwMain.Controls.Add(lblNavegadorAutofill);
            }
        }


        private void AddCookieLabels(FlowLayoutPanel flwMain, Dictionary<string, int> browsersWithCookies)
        {
            Font fm = Helpers.FM;

            Label lblAllCookiesButton = new Label()
            {
                BackColor = Color.White,
                Font = new Font(fm, FontStyle.Bold),
                ForeColor = Color.Black,
                AutoSize = false,
                Height = 48,
                Margin = new Padding(3, 1, 3, 1),
                Padding = new Padding(12, 3, 0, 3),
                Text = $"All Cookies {browsersWithCookies.Values.Sum()} hits",
                TextAlign = ContentAlignment.MiddleCenter,
                Tag = "fullwidth"
            };

            lblAllCookiesButton.Width =
                flwMain.ClientSize.Width - flwMain.Padding.Horizontal - lblAllCookiesButton.Margin.Horizontal;

            lblAllCookiesButton.Click += (sender, e) =>
            {
                int utcOffset = Helpers.utcOffset;
                string? offset = utcOffset == 0 ? null : (utcOffset > 0 ? $"+{utcOffset} hours" : $"{utcOffset} hours");

                string createdExpr = offset == null
                    ? "STRFTIME('%Y-%m-%d %H:%M:%f', Created)"
                    : $"STRFTIME('%Y-%m-%d %H:%M:%f', Created, '{offset}')";
                string expiresExpr = offset == null
                    ? "STRFTIME('%Y-%m-%d %H:%M:%f', Expires)"
                    : $"STRFTIME('%Y-%m-%d %H:%M:%f', Expires, '{offset}')";
                string lastAccessedExpr = offset == null
                    ? "STRFTIME('%Y-%m-%d %H:%M:%f', LastAccessed)"
                    : $"STRFTIME('%Y-%m-%d %H:%M:%f', LastAccessed, '{offset}')";

                string whereClause = CurrentWhereFilter(
                    SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Host", "Name", "Value", "Path"),
                    SearchSql.TimeCondition("Created", "Expires", "LastAccessed"));

                string sqlquery = $@"
                    SELECT
                        id, Artifact_type, Potential_activity, Browser, Host, Name, Value, Path,
                        {createdExpr} AS Created,
                        {expiresExpr} AS Expires,
                        {lastAccessedExpr} AS LastAccessed,
                        IsSecure, IsHttpOnly, IsPersistent, SameSite, SourceScheme, SourcePort,
                        IsEncrypted, File, Label, Comment
                    FROM cookies_data
                    {whereClause};";

                Tools.ShowQueryOnDataGridView(sfDataGrid1, Helpers.chromeViewerConnectionString, sqlquery, labelItemCount, Console);
                labelStatus.Text = "All cookies from all browsers";
            };

            lblAllCookiesButton.MouseHover += (sender, e) => { lblAllCookiesButton.BackColor = Color.LightBlue; };
            lblAllCookiesButton.MouseLeave += (sender, e) => { lblAllCookiesButton.BackColor = Color.White; };

            flwMain.Controls.Add(lblAllCookiesButton);

            foreach (var entry in browsersWithCookies.OrderBy(b => b.Key))
            {
                string navegador = entry.Key;
                int count = entry.Value;

                Image? icono = null;
                icono = Helpers.GetBrowserImage(navegador);

                Label lblNavegadorCookies = new Label()
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

                lblNavegadorCookies.Width =
                    flwMain.ClientSize.Width - flwMain.Padding.Horizontal - lblNavegadorCookies.Margin.Horizontal;

                lblNavegadorCookies.MouseHover += (sender, e) => { lblNavegadorCookies.BackColor = Color.LightBlue; };
                lblNavegadorCookies.MouseLeave += (sender, e) => { lblNavegadorCookies.BackColor = Color.SteelBlue; };

                lblNavegadorCookies.Click += (sender, e) =>
                {
                    int utcOffset = Helpers.utcOffset;
                    string? offset = utcOffset == 0 ? null : (utcOffset > 0 ? $"+{utcOffset} hours" : $"{utcOffset} hours");

                    string createdExpr = offset == null
                        ? "STRFTIME('%Y-%m-%d %H:%M:%f', Created)"
                        : $"STRFTIME('%Y-%m-%d %H:%M:%f', Created, '{offset}')";
                    string expiresExpr = offset == null
                        ? "STRFTIME('%Y-%m-%d %H:%M:%f', Expires)"
                        : $"STRFTIME('%Y-%m-%d %H:%M:%f', Expires, '{offset}')";
                    string lastAccessedExpr = offset == null
                        ? "STRFTIME('%Y-%m-%d %H:%M:%f', LastAccessed)"
                        : $"STRFTIME('%Y-%m-%d %H:%M:%f', LastAccessed, '{offset}')";

                    string filterCondition = CurrentAndFilter(
                        SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Host", "Name", "Value", "Path"),
                        SearchSql.TimeCondition("Created", "Expires", "LastAccessed"));

                    string sqlquery = $@"
                        SELECT
                            id, Artifact_type, Potential_activity, Browser, Host, Name, Value, Path,
                            {createdExpr} AS Created,
                            {expiresExpr} AS Expires,
                            {lastAccessedExpr} AS LastAccessed,
                            IsSecure, IsHttpOnly, IsPersistent, SameSite, SourceScheme, SourcePort,
                            IsEncrypted, File, Label, Comment
                        FROM cookies_data
                        WHERE Browser = '{navegador}' {filterCondition};";

                    Tools.ShowQueryOnDataGridView(sfDataGrid1, Helpers.chromeViewerConnectionString, sqlquery, labelItemCount, Console);
                    labelStatus.Text = $"Cookies from {navegador}";
                };

                flwMain.Controls.Add(lblNavegadorCookies);
            }
        }


        private void AddCacheLabels(FlowLayoutPanel flwMain, Dictionary<string, int> browsersWithCache)
        {
            Font fm = Helpers.FM;

            Label lblAllCacheButton = new Label()
            {
                BackColor = Color.White,
                Font = new Font(fm, FontStyle.Bold),
                ForeColor = Color.Black,
                AutoSize = false,
                Height = 48,
                Margin = new Padding(3, 1, 3, 1),
                Padding = new Padding(12, 3, 0, 3),
                Text = $"All Cache {browsersWithCache.Values.Sum()} hits",
                TextAlign = ContentAlignment.MiddleCenter,
                Tag = "fullwidth"
            };

            lblAllCacheButton.Width =
                flwMain.ClientSize.Width - flwMain.Padding.Horizontal - lblAllCacheButton.Margin.Horizontal;

            lblAllCacheButton.Click += (sender, e) =>
            {
                string sqlquery = BuildCacheSelectQuery(CurrentWhereFilter(
                    SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Url", "Host", "ContentType", "CacheType", "Server", "CacheFile", "CacheKey", "BodyPreview", "DetectedFileType", "DetectedExtension"),
                    SearchSql.TimeCondition("Created", "Modified", "LastAccessed")));
                Tools.ShowQueryOnDataGridView(sfDataGrid1, Helpers.chromeViewerConnectionString, sqlquery, labelItemCount, Console);
                labelStatus.Text = "All cache from all browsers";
            };

            lblAllCacheButton.MouseHover += (sender, e) => { lblAllCacheButton.BackColor = Color.LightBlue; };
            lblAllCacheButton.MouseLeave += (sender, e) => { lblAllCacheButton.BackColor = Color.White; };

            flwMain.Controls.Add(lblAllCacheButton);

            foreach (var entry in browsersWithCache.OrderBy(b => b.Key))
            {
                string navegador = entry.Key;
                int count = entry.Value;
                Image icono = GetBrowserIcon(navegador);

                Label lblNavegadorCache = new Label()
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

                lblNavegadorCache.Width =
                    flwMain.ClientSize.Width - flwMain.Padding.Horizontal - lblNavegadorCache.Margin.Horizontal;

                lblNavegadorCache.MouseHover += (sender, e) => { lblNavegadorCache.BackColor = Color.LightBlue; };
                lblNavegadorCache.MouseLeave += (sender, e) => { lblNavegadorCache.BackColor = Color.SteelBlue; };

                lblNavegadorCache.Click += (sender, e) =>
                {
                    string filterCondition = CurrentAndFilter(
                        SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Url", "Host", "ContentType", "CacheType", "Server", "CacheFile", "CacheKey", "BodyPreview", "DetectedFileType", "DetectedExtension"),
                        SearchSql.TimeCondition("Created", "Modified", "LastAccessed"));
                    string sqlquery = BuildCacheSelectQuery($"WHERE Browser = '{navegador}' {filterCondition}");
                    Tools.ShowQueryOnDataGridView(sfDataGrid1, Helpers.chromeViewerConnectionString, sqlquery, labelItemCount, Console);
                    labelStatus.Text = $"Cache from {navegador}";
                };

                flwMain.Controls.Add(lblNavegadorCache);
            }
        }


        private string BuildCacheSelectQuery(string whereClause)
        {
            int utcOffset = Helpers.utcOffset;
            string? offset = utcOffset == 0 ? null : (utcOffset > 0 ? $"+{utcOffset} hours" : $"{utcOffset} hours");
            string createdExpr = offset == null
                ? "STRFTIME('%Y-%m-%d %H:%M:%f', Created)"
                : $"STRFTIME('%Y-%m-%d %H:%M:%f', Created, '{offset}')";
            string modifiedExpr = offset == null
                ? "STRFTIME('%Y-%m-%d %H:%M:%f', Modified)"
                : $"STRFTIME('%Y-%m-%d %H:%M:%f', Modified, '{offset}')";
            string lastAccessedExpr = offset == null
                ? "STRFTIME('%Y-%m-%d %H:%M:%f', LastAccessed)"
                : $"STRFTIME('%Y-%m-%d %H:%M:%f', LastAccessed, '{offset}')";

            return $@"
                SELECT
                    id, Artifact_type, Potential_activity, Browser, Url, Host, ContentType, CacheType, HttpStatus, Server, FileSize,
                    {createdExpr} AS Created,
                    {modifiedExpr} AS Modified,
                    {lastAccessedExpr} AS LastAccessed,
                    CacheFile, CacheKey, BodySize, BodySha256, BodyStored, BodyPreview,
                    DetectedFileType, DetectedExtension, File, Label, Comment
                FROM cache_data
                {whereClause};";
        }

        private void AddSessionLabels(FlowLayoutPanel flwMain, Dictionary<string, int> browsersWithSessions)
        {
            Font fm = Helpers.FM;

            Label lblAllSessionsButton = new Label()
            {
                BackColor = Color.White,
                Font = new Font(fm, FontStyle.Bold),
                ForeColor = Color.Black,
                AutoSize = false,
                Height = 48,
                Margin = new Padding(3, 1, 3, 1),
                Padding = new Padding(12, 3, 0, 3),
                Text = $"All Sessions {browsersWithSessions.Values.Sum()} hits",
                TextAlign = ContentAlignment.MiddleCenter,
                Tag = "fullwidth"
            };

            lblAllSessionsButton.Width =
                flwMain.ClientSize.Width - flwMain.Padding.Horizontal - lblAllSessionsButton.Margin.Horizontal;

            lblAllSessionsButton.Click += (sender, e) =>
            {
                string sqlquery = BuildSessionSelectQuery(CurrentWhereFilter(
                    SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Url", "Title", "OriginalUrl", "Referrer", "SessionFile", "SourceType"),
                    SearchSql.TimeCondition("LastAccessed", "Created")));
                Tools.ShowQueryOnDataGridView(sfDataGrid1, Helpers.chromeViewerConnectionString, sqlquery, labelItemCount, Console);
                labelStatus.Text = "All sessions from all browsers";
            };

            lblAllSessionsButton.MouseHover += (sender, e) => { lblAllSessionsButton.BackColor = Color.LightBlue; };
            lblAllSessionsButton.MouseLeave += (sender, e) => { lblAllSessionsButton.BackColor = Color.White; };

            flwMain.Controls.Add(lblAllSessionsButton);

            foreach (var entry in browsersWithSessions.OrderBy(b => b.Key))
            {
                string navegador = entry.Key;
                int count = entry.Value;
                Image icono = GetBrowserIcon(navegador);

                Label lblNavegadorSessions = new Label()
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

                lblNavegadorSessions.Width =
                    flwMain.ClientSize.Width - flwMain.Padding.Horizontal - lblNavegadorSessions.Margin.Horizontal;

                lblNavegadorSessions.MouseHover += (sender, e) => { lblNavegadorSessions.BackColor = Color.LightBlue; };
                lblNavegadorSessions.MouseLeave += (sender, e) => { lblNavegadorSessions.BackColor = Color.SteelBlue; };

                lblNavegadorSessions.Click += (sender, e) =>
                {
                    string filterCondition = CurrentAndFilter(
                        SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Url", "Title", "OriginalUrl", "Referrer", "SessionFile", "SourceType"),
                        SearchSql.TimeCondition("LastAccessed", "Created"));
                    string sqlquery = BuildSessionSelectQuery($"WHERE Browser = '{navegador}' {filterCondition}");
                    Tools.ShowQueryOnDataGridView(sfDataGrid1, Helpers.chromeViewerConnectionString, sqlquery, labelItemCount, Console);
                    labelStatus.Text = $"Sessions from {navegador}";
                };

                Helpers.sessionLabels[navegador] = lblNavegadorSessions;
                flwMain.Controls.Add(lblNavegadorSessions);
            }
        }

        private string BuildSessionSelectQuery(string whereClause)
        {
            string lastAccessedExpr = SearchSql.DateExpr("LastAccessed");
            string createdExpr = SearchSql.DateExpr("Created");

            return $@"
                SELECT
                    id, Artifact_type, Potential_activity, Browser, WindowIndex, TabIndex, EntryIndex, Selected,
                    Url, Title, OriginalUrl, Referrer,
                    {lastAccessedExpr} AS LastAccessed,
                    {createdExpr} AS Created,
                    SessionFile, SourceType, File, Label, Comment
                FROM session_data
                {whereClause};";
        }

        private void AddExtensionLabels(FlowLayoutPanel flwMain, Dictionary<string, int> browsersWithExtensions)
        {
            Font fm = Helpers.FM;

            Label lblAllExtensionsButton = new Label()
            {
                BackColor = Color.White,
                Font = new Font(fm, FontStyle.Bold),
                ForeColor = Color.Black,
                AutoSize = false,
                Height = 48,
                Margin = new Padding(3, 1, 3, 1),
                Padding = new Padding(12, 3, 0, 3),
                Text = $"All Extensions {browsersWithExtensions.Values.Sum()} hits",
                TextAlign = ContentAlignment.MiddleCenter,
                Tag = "fullwidth"
            };

            lblAllExtensionsButton.Width =
                flwMain.ClientSize.Width - flwMain.Padding.Horizontal - lblAllExtensionsButton.Margin.Horizontal;

            lblAllExtensionsButton.Click += (sender, e) =>
            {
                string sqlquery = BuildExtensionSelectQuery(CurrentWhereFilter(
                    SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "ExtensionId", "Name", "Version", "Description", "Author", "HomepageUrl", "UpdateUrl", "Permissions", "HostPermissions", "ExtensionPath", "SourceFile"),
                    SearchSql.TimeCondition("InstallTime", "LastUpdateTime")));
                Tools.ShowQueryOnDataGridView(sfDataGrid1, Helpers.chromeViewerConnectionString, sqlquery, labelItemCount, Console);
                labelStatus.Text = "All extensions from all browsers";
            };

            lblAllExtensionsButton.MouseHover += (sender, e) => { lblAllExtensionsButton.BackColor = Color.LightBlue; };
            lblAllExtensionsButton.MouseLeave += (sender, e) => { lblAllExtensionsButton.BackColor = Color.White; };
            flwMain.Controls.Add(lblAllExtensionsButton);

            foreach (var entry in browsersWithExtensions.OrderBy(b => b.Key))
            {
                string navegador = entry.Key;
                int count = entry.Value;
                Image icono = GetBrowserIcon(navegador);

                Label lblNavegadorExtensions = new Label()
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

                lblNavegadorExtensions.Width =
                    flwMain.ClientSize.Width - flwMain.Padding.Horizontal - lblNavegadorExtensions.Margin.Horizontal;

                lblNavegadorExtensions.MouseHover += (sender, e) => { lblNavegadorExtensions.BackColor = Color.LightBlue; };
                lblNavegadorExtensions.MouseLeave += (sender, e) => { lblNavegadorExtensions.BackColor = Color.SteelBlue; };

                lblNavegadorExtensions.Click += (sender, e) =>
                {
                    string filterCondition = CurrentAndFilter(
                        SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "ExtensionId", "Name", "Version", "Description", "Author", "HomepageUrl", "UpdateUrl", "Permissions", "HostPermissions", "ExtensionPath", "SourceFile"),
                        SearchSql.TimeCondition("InstallTime", "LastUpdateTime"));
                    string sqlquery = BuildExtensionSelectQuery($"WHERE Browser = '{navegador}' {filterCondition}");
                    Tools.ShowQueryOnDataGridView(sfDataGrid1, Helpers.chromeViewerConnectionString, sqlquery, labelItemCount, Console);
                    labelStatus.Text = $"Extensions from {navegador}";
                };

                Helpers.extensionLabels[navegador] = lblNavegadorExtensions;
                flwMain.Controls.Add(lblNavegadorExtensions);
            }
        }

        private string BuildExtensionSelectQuery(string whereClause)
        {
            string installExpr = SearchSql.DateExpr("InstallTime");
            string updateExpr = SearchSql.DateExpr("LastUpdateTime");

            return $@"
                SELECT
                    id, Artifact_type, Potential_activity, Browser, ExtensionId, Name, Version, Description, Author,
                    HomepageUrl, UpdateUrl,
                    {installExpr} AS InstallTime,
                    {updateExpr} AS LastUpdateTime,
                    Enabled, Permissions, HostPermissions, ManifestVersion, ExtensionPath, SourceFile,
                    File, Label, Comment
                FROM extension_data
                {whereClause};";
        }

        private void AddLoginLabels(FlowLayoutPanel flwMain, Dictionary<string, int> browsersWithLogins)
        {
            Font fm = Helpers.FM;

            Label lblAllLoginsButton = new Label()
            {
                BackColor = Color.White,
                Font = new Font(fm, FontStyle.Bold),
                ForeColor = Color.Black,
                AutoSize = false,
                Height = 48,
                Margin = new Padding(3, 1, 3, 1),
                Padding = new Padding(12, 3, 0, 3),
                Text = $"All Saved Logins {browsersWithLogins.Values.Sum()} hits",
                TextAlign = ContentAlignment.MiddleCenter,
                Tag = "fullwidth"
            };

            lblAllLoginsButton.Width =
                flwMain.ClientSize.Width - flwMain.Padding.Horizontal - lblAllLoginsButton.Margin.Horizontal;

            lblAllLoginsButton.Click += (sender, e) =>
            {
                string sqlquery = BuildLoginSelectQuery(CurrentWhereFilter(
                    SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Url", "Action_url", "Signon_realm", "Username", "Username_field", "Password_field", "Scheme", "Decryption_status", "Credential_artifact_value", "Store", "Login_guid"),
                    SearchSql.TimeCondition("Created", "Last_used", "Password_changed")));
                Tools.ShowQueryOnDataGridView(sfDataGrid1, Helpers.chromeViewerConnectionString, sqlquery, labelItemCount, Console);
                labelStatus.Text = "All saved login metadata from all browsers";
            };

            lblAllLoginsButton.MouseHover += (sender, e) => { lblAllLoginsButton.BackColor = Color.LightBlue; };
            lblAllLoginsButton.MouseLeave += (sender, e) => { lblAllLoginsButton.BackColor = Color.White; };
            flwMain.Controls.Add(lblAllLoginsButton);

            foreach (var entry in browsersWithLogins.OrderBy(b => b.Key))
            {
                string navegador = entry.Key;
                int count = entry.Value;
                Image icono = GetBrowserIcon(navegador);

                Label lblNavegadorLogins = new Label()
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

                lblNavegadorLogins.Width =
                    flwMain.ClientSize.Width - flwMain.Padding.Horizontal - lblNavegadorLogins.Margin.Horizontal;

                lblNavegadorLogins.MouseHover += (sender, e) => { lblNavegadorLogins.BackColor = Color.LightBlue; };
                lblNavegadorLogins.MouseLeave += (sender, e) => { lblNavegadorLogins.BackColor = Color.SteelBlue; };

                lblNavegadorLogins.Click += (sender, e) =>
                {
                    string filterCondition = CurrentAndFilter(
                        SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Url", "Action_url", "Signon_realm", "Username", "Username_field", "Password_field", "Scheme", "Decryption_status", "Credential_artifact_value", "Store", "Login_guid"),
                        SearchSql.TimeCondition("Created", "Last_used", "Password_changed"));
                    string sqlquery = BuildLoginSelectQuery($"WHERE Browser = '{navegador}' {filterCondition}");
                    Tools.ShowQueryOnDataGridView(sfDataGrid1, Helpers.chromeViewerConnectionString, sqlquery, labelItemCount, Console);
                    labelStatus.Text = $"Saved login metadata from {navegador}";
                };

                Helpers.loginLabels[navegador] = lblNavegadorLogins;
                flwMain.Controls.Add(lblNavegadorLogins);
            }
        }

        private string BuildLoginSelectQuery(string whereClause)
        {
            string createdExpr = SearchSql.DateExpr("Created");
            string lastUsedExpr = SearchSql.DateExpr("Last_used");
            string passwordChangedExpr = SearchSql.DateExpr("Password_changed");

            return $@"
                SELECT
                    id, Artifact_type, Potential_activity, Browser, Url, Action_url, Signon_realm,
                    Username, Username_field, Password_field, Scheme, Times_used,
                    {createdExpr} AS Created,
                    {lastUsedExpr} AS Last_used,
                    {passwordChangedExpr} AS Password_changed,
                    Is_blacklisted, Is_federated, Password_present, Encrypted_password_sha256,
                    Encrypted_password_size, Decryption_status, Credential_artifact_value,
                    Store, Login_guid, File, Label, Comment
                FROM saved_logins_data
                {whereClause};";
        }

        private void AddLocalStorageLabels(FlowLayoutPanel flwMain, Dictionary<string, int> browsersWithLocalStorage)
        {
            Font fm = Helpers.FM;

            Label lblAllLocalStorageButton = new Label()
            {
                BackColor = Color.White,
                Font = new Font(fm, FontStyle.Bold),
                ForeColor = Color.Black,
                AutoSize = false,
                Height = 48,
                Margin = new Padding(3, 1, 3, 1),
                Padding = new Padding(12, 3, 0, 3),
                Text = $"All Local Storage {browsersWithLocalStorage.Values.Sum()} hits",
                TextAlign = ContentAlignment.MiddleCenter,
                Tag = "fullwidth"
            };

            lblAllLocalStorageButton.Width =
                flwMain.ClientSize.Width - flwMain.Padding.Horizontal - lblAllLocalStorageButton.Margin.Horizontal;

            lblAllLocalStorageButton.Click += (sender, e) =>
            {
                string sqlquery = BuildLocalStorageSelectQuery(CurrentWhereFilter(
                    SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Origin", "Host", "Storage_key", "Value_preview", "Value_sha256", "Source_kind", "Source_file", "Parser_notes"),
                    SearchSql.TimeCondition("Created", "Modified", "LastAccessed")));
                Tools.ShowQueryOnDataGridView(sfDataGrid1, Helpers.chromeViewerConnectionString, sqlquery, labelItemCount, Console);
                labelStatus.Text = "All local storage from all browsers";
            };

            lblAllLocalStorageButton.MouseHover += (sender, e) => { lblAllLocalStorageButton.BackColor = Color.LightBlue; };
            lblAllLocalStorageButton.MouseLeave += (sender, e) => { lblAllLocalStorageButton.BackColor = Color.White; };
            flwMain.Controls.Add(lblAllLocalStorageButton);

            foreach (var entry in browsersWithLocalStorage.OrderBy(b => b.Key))
            {
                string navegador = entry.Key;
                int count = entry.Value;
                Image icono = GetBrowserIcon(navegador);

                Label lblNavegadorLocalStorage = new Label()
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

                lblNavegadorLocalStorage.Width =
                    flwMain.ClientSize.Width - flwMain.Padding.Horizontal - lblNavegadorLocalStorage.Margin.Horizontal;

                lblNavegadorLocalStorage.MouseHover += (sender, e) => { lblNavegadorLocalStorage.BackColor = Color.LightBlue; };
                lblNavegadorLocalStorage.MouseLeave += (sender, e) => { lblNavegadorLocalStorage.BackColor = Color.SteelBlue; };

                lblNavegadorLocalStorage.Click += (sender, e) =>
                {
                    string filterCondition = CurrentAndFilter(
                        SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Origin", "Host", "Storage_key", "Value_preview", "Value_sha256", "Source_kind", "Source_file", "Parser_notes"),
                        SearchSql.TimeCondition("Created", "Modified", "LastAccessed"));
                    string sqlquery = BuildLocalStorageSelectQuery($"WHERE Browser = '{navegador}' {filterCondition}");
                    Tools.ShowQueryOnDataGridView(sfDataGrid1, Helpers.chromeViewerConnectionString, sqlquery, labelItemCount, Console);
                    labelStatus.Text = $"Local storage from {navegador}";
                };

                Helpers.localStorageLabels[navegador] = lblNavegadorLocalStorage;
                flwMain.Controls.Add(lblNavegadorLocalStorage);
            }
        }

        private string BuildLocalStorageSelectQuery(string whereClause)
        {
            string createdExpr = SearchSql.DateExpr("Created");
            string modifiedExpr = SearchSql.DateExpr("Modified");
            string accessedExpr = SearchSql.DateExpr("LastAccessed");

            return $@"
                SELECT
                    id, Artifact_type, Potential_activity, Browser, Origin, Host, Storage_key, Value_preview,
                    Value_size, Value_sha256, Source_kind, Source_file,
                    {createdExpr} AS Created,
                    {modifiedExpr} AS Modified,
                    {accessedExpr} AS LastAccessed,
                    Parser_notes, File, Label, Comment
                FROM local_storage_data
                {whereClause};";
        }

        private void AddSessionStorageLabels(FlowLayoutPanel flwMain, Dictionary<string, int> browsersWithSessionStorage)
        {
            AddWebStorageLabels(
                flwMain,
                browsersWithSessionStorage,
                Helpers.sessionStorageLabels,
                "All Session Storage",
                "Session storage from",
                "All session storage from all browsers",
                BuildSessionStorageSelectQuery);
        }

        private void AddIndexedDbLabels(FlowLayoutPanel flwMain, Dictionary<string, int> browsersWithIndexedDb)
        {
            AddWebStorageLabels(
                flwMain,
                browsersWithIndexedDb,
                Helpers.indexedDbLabels,
                "All IndexedDB",
                "IndexedDB from",
                "All IndexedDB data from all browsers",
                BuildIndexedDbSelectQuery);
        }

        private void AddWebStorageLabels(
            FlowLayoutPanel flwMain,
            Dictionary<string, int> browserCounts,
            Dictionary<string, Label> labelStore,
            string allText,
            string browserStatusPrefix,
            string allStatusText,
            Func<string, string> queryBuilder)
        {
            Font fm = Helpers.FM;

            Label lblAllButton = new Label()
            {
                BackColor = Color.White,
                Font = new Font(fm, FontStyle.Bold),
                ForeColor = Color.Black,
                AutoSize = false,
                Height = 48,
                Margin = new Padding(3, 1, 3, 1),
                Padding = new Padding(12, 3, 0, 3),
                Text = $"{allText} {browserCounts.Values.Sum()} hits",
                TextAlign = ContentAlignment.MiddleCenter,
                Tag = "fullwidth"
            };

            lblAllButton.Width =
                flwMain.ClientSize.Width - flwMain.Padding.Horizontal - lblAllButton.Margin.Horizontal;

            lblAllButton.Click += (sender, e) =>
            {
                string sqlquery = queryBuilder(CurrentWhereFilter(
                    SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Origin", "Host", "Storage_key", "Value_preview", "Value_sha256", "Source_kind", "Source_file", "Parser_notes"),
                    SearchSql.TimeCondition("Created", "Modified", "LastAccessed")));
                Tools.ShowQueryOnDataGridView(sfDataGrid1, Helpers.chromeViewerConnectionString, sqlquery, labelItemCount, Console);
                labelStatus.Text = allStatusText;
            };

            lblAllButton.MouseHover += (sender, e) => { lblAllButton.BackColor = Color.LightBlue; };
            lblAllButton.MouseLeave += (sender, e) => { lblAllButton.BackColor = Color.White; };
            flwMain.Controls.Add(lblAllButton);

            foreach (var entry in browserCounts.OrderBy(b => b.Key))
            {
                string navegador = entry.Key;
                int count = entry.Value;
                Image icono = GetBrowserIcon(navegador);

                Label lblBrowser = new Label()
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

                lblBrowser.Width =
                    flwMain.ClientSize.Width - flwMain.Padding.Horizontal - lblBrowser.Margin.Horizontal;

                lblBrowser.MouseHover += (sender, e) => { lblBrowser.BackColor = Color.LightBlue; };
                lblBrowser.MouseLeave += (sender, e) => { lblBrowser.BackColor = Color.SteelBlue; };

                lblBrowser.Click += (sender, e) =>
                {
                    string filterCondition = CurrentAndFilter(
                        SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Origin", "Host", "Storage_key", "Value_preview", "Value_sha256", "Source_kind", "Source_file", "Parser_notes"),
                        SearchSql.TimeCondition("Created", "Modified", "LastAccessed"));
                    string sqlquery = queryBuilder($"WHERE Browser = '{navegador}' {filterCondition}");
                    Tools.ShowQueryOnDataGridView(sfDataGrid1, Helpers.chromeViewerConnectionString, sqlquery, labelItemCount, Console);
                    labelStatus.Text = $"{browserStatusPrefix} {navegador}";
                };

                labelStore[navegador] = lblBrowser;
                flwMain.Controls.Add(lblBrowser);
            }
        }

        private string BuildSessionStorageSelectQuery(string whereClause)
        {
            return BuildStorageSelectQuery("session_storage_data", whereClause);
        }

        private string BuildIndexedDbSelectQuery(string whereClause)
        {
            return BuildStorageSelectQuery("indexeddb_data", whereClause);
        }

        private string BuildStorageSelectQuery(string tableName, string whereClause)
        {
            string createdExpr = SearchSql.DateExpr("Created");
            string modifiedExpr = SearchSql.DateExpr("Modified");
            string accessedExpr = SearchSql.DateExpr("LastAccessed");

            return $@"
                SELECT
                    id, Artifact_type, Potential_activity, Browser, Origin, Host, Storage_key, Value_preview,
                    Value_size, Value_sha256, Source_kind, Source_file,
                    {createdExpr} AS Created,
                    {modifiedExpr} AS Modified,
                    {accessedExpr} AS LastAccessed,
                    Parser_notes, File, Label, Comment
                FROM {tableName}
                {whereClause};";
        }


        private Image GetBrowserIcon(string navegador)
        {
            return Helpers.GetBrowserImage(navegador);
        }

        private bool GridCellMatchesSearch(QueryCellStyleEventArgs e)
        {
            if (e.Column == null || string.IsNullOrWhiteSpace(e.Column.MappingName) || string.IsNullOrWhiteSpace(Helpers.searchTerm))
                return false;

            try
            {
                var node = sfDataGrid1.GetRecordEntryAtRowIndex(e.RowIndex);
                if (node is not Syncfusion.Data.RecordEntry record || record.Data is not DataRowView rowView)
                    return false;

                if (rowView.DataView?.Table == null || !rowView.DataView.Table.Columns.Contains(e.Column.MappingName))
                    return false;

                string value = rowView[e.Column.MappingName]?.ToString() ?? string.Empty;
                return Regex.IsMatch(value, Helpers.searchTerm, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            }
            catch
            {
                return false;
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
            if (checkBox_Labels != null) checkBox_Labels.Checked = false;
            checkBox_enableTime.Checked = false;
            Helpers.searchLabelsOnly = false;
            Helpers.sqltimecondition = "";
            Helpers.sqlDownloadtimecondition = "";
            Helpers.sqlBookmarkstimecondition = "";
            Helpers.sqlAutofilltimecondition = "";
            Helpers.sqlCookiestimecondition = "";
            Helpers.sqlCachetimecondition = "";

            Helpers.browserUrls.Clear();
            Helpers.browserHistoryCounts.Clear();
            Helpers.browserHistoryCategoryCounts.Clear();
            Helpers.browsersWithDownloads.Clear();
            Helpers.browsersWithBookmarks.Clear();
            Helpers.browsersWithAutofill.Clear();
            Helpers.browsersWithCookies.Clear();
            Helpers.browsersWithCache.Clear();
            Helpers.browsersWithSessions.Clear();
            Helpers.browsersWithExtensions.Clear();
            Helpers.browsersWithLogins.Clear();
            Helpers.browsersWithLocalStorage.Clear();
            Helpers.browsersWithSessionStorage.Clear();
            Helpers.browsersWithIndexedDb.Clear();
            sfDataGrid1.DataSource = null;
            groupBox_Main.Controls.Clear();
        }




        private void searchBtn_Click_1(object sender, EventArgs e)
        {


            Helpers.browserUrls.Clear();
            Helpers.browserHistoryCounts.Clear();
            Helpers.browserHistoryCategoryCounts.Clear();
            Helpers.browsersWithDownloads.Clear();
            Helpers.browsersWithBookmarks.Clear();
            Helpers.browsersWithAutofill.Clear();
            Helpers.browsersWithCookies.Clear();
            Helpers.browsersWithCache.Clear();
            Helpers.browsersWithSessions.Clear();
            Helpers.browsersWithExtensions.Clear();
            Helpers.browsersWithLogins.Clear();
            Helpers.browsersWithLocalStorage.Clear();
            Helpers.browsersWithSessionStorage.Clear();
            Helpers.browsersWithIndexedDb.Clear();


            Helpers.searchTerm = search_textBox.Text;
            Helpers.searchTermExists = !string.IsNullOrWhiteSpace(Helpers.searchTerm);
            Helpers.searchLabelsOnly = checkBox_Labels != null && checkBox_Labels.Checked;
            Helpers.sqltimecondition = "";
            Helpers.sqlDownloadtimecondition = "";
            Helpers.sqlBookmarkstimecondition = "";
            Helpers.sqlAutofilltimecondition = "";
            Helpers.sqlCookiestimecondition = "";
            Helpers.sqlCachetimecondition = "";


            int utcOffset = Helpers.utcOffset;

            if (!Helpers.searchTermExists && !checkBox_enableTime.Checked && !Helpers.searchLabelsOnly)
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
                        Helpers.sqlCookiestimecondition = $" AND ((Created >= '{Helpers.sd}' AND Created <= '{Helpers.ed}') OR (Expires >= '{Helpers.sd}' AND Expires <= '{Helpers.ed}') OR (LastAccessed >= '{Helpers.sd}' AND LastAccessed <= '{Helpers.ed}'))";
                        Helpers.sqlCachetimecondition = $" AND ((Created >= '{Helpers.sd}' AND Created <= '{Helpers.ed}') OR (Modified >= '{Helpers.sd}' AND Modified <= '{Helpers.ed}') OR (LastAccessed >= '{Helpers.sd}' AND LastAccessed <= '{Helpers.ed}'))";
                    }
                    else
                    {
                        Helpers.sqltimecondition = $"(Visit_time >= '{Helpers.sd}' AND Visit_time <= '{Helpers.ed}')";
                        Helpers.sqlDownloadtimecondition = $"(End_time >= '{Helpers.sd}' AND End_time <= '{Helpers.ed}')";
                        Helpers.sqlBookmarkstimecondition = $"((DateAdded >= '{Helpers.sd}' AND DateAdded <= '{Helpers.ed}') OR (LastModified >= '{Helpers.sd}' AND LastModified <= '{Helpers.ed}'))";
                        Helpers.sqlAutofilltimecondition = $"((FirstUsed >= '{Helpers.sd}' AND FirstUsed <= '{Helpers.ed}') OR (LastUsed >= '{Helpers.sd}' AND LastUsed <= '{Helpers.ed}'))";
                        Helpers.sqlCookiestimecondition = $"((Created >= '{Helpers.sd}' AND Created <= '{Helpers.ed}') OR (Expires >= '{Helpers.sd}' AND Expires <= '{Helpers.ed}') OR (LastAccessed >= '{Helpers.sd}' AND LastAccessed <= '{Helpers.ed}'))";
                        Helpers.sqlCachetimecondition = $"((Created >= '{Helpers.sd}' AND Created <= '{Helpers.ed}') OR (Modified >= '{Helpers.sd}' AND Modified <= '{Helpers.ed}') OR (LastAccessed >= '{Helpers.sd}' AND LastAccessed <= '{Helpers.ed}'))";
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


            groupBox_customSearch.BackColor = Color.FromArgb(230, 240, 255);

            if (Helpers.searchLabelsOnly)
            {
                labelStatus.Text = Helpers.searchTermExists
                    ? $"Labeled artifacts matching: {Helpers.searchTerm}"
                    : "Labeled artifacts from all web activity";
            }



            string sqlquery = BuildAllWebArtifactsTimelineQuery();
            Tools.ShowQueryOnDataGridView(sfDataGrid1, Helpers.chromeViewerConnectionString, sqlquery, labelItemCount, Console);




            string searchCondition;
            string searchConditionHistoryMenu = SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Category", "Url", "Title");
            string historyMenuWhere = SearchSql.Where(searchConditionHistoryMenu, SearchSql.TimeCondition("Visit_time"), SearchSql.LabelCondition());



            string sqlqueryHistoryMenu = $@"
                                            SELECT Browser, Url FROM results
                                            {historyMenuWhere}
                                            UNION ALL
                                            SELECT Browser, Url FROM firefox_results
                                            {historyMenuWhere}";

            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(Helpers.chromeViewerConnectionString))
                {
                    connection.Open();

                    using (SQLiteCommand command = new SQLiteCommand(sqlqueryHistoryMenu, connection))
                    {
                        SearchSql.AddParameters(command);
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

                Helpers.historyHits = Helpers.browserUrls.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error connecting to database {ex.Message}", "Browser Reviewer", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }






            searchCondition = SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Current_path", "Target_path", "Url_chain", "State", "Referrer", "Site_url", "Tab_url", "Mime_type");
            string chromeDownloadsWhere = SearchSql.Where(searchCondition, SearchSql.TimeCondition("End_time"), SearchSql.LabelCondition());
            string firefoxDownloadsWhere = SearchSql.Where(
                SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Current_path", "Source_url", "Title", "State"),
                SearchSql.TimeCondition("End_time"),
                SearchSql.LabelCondition());
            string sqlqueryChromeDwMenu = $@"SELECT Browser, COUNT(*) 
                    FROM chrome_downloads 
                    {chromeDownloadsWhere}
                    GROUP BY Browser;";

            string sqlqueryFirefoxDwMenu = $@"SELECT Browser, COUNT(*) 
                    FROM firefox_downloads 
                    {firefoxDownloadsWhere}
                    GROUP BY Browser;";



            using (SQLiteConnection connection = new SQLiteConnection(Helpers.chromeViewerConnectionString))
            {
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand(sqlqueryChromeDwMenu, connection))
                {
                    SearchSql.AddParameters(command);


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

                using (SQLiteCommand command = new SQLiteCommand(sqlqueryFirefoxDwMenu, connection))
                {
                    SearchSql.AddParameters(command);

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






            searchCondition = SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Type", "Title", "URL", "Parent_name");
            string bookmarksWhere = SearchSql.Where(searchCondition, SearchSql.TimeCondition("DateAdded", "LastModified"), SearchSql.LabelCondition());


            string sqlqueryChromeBkMkMenu = $@"SELECT Browser, COUNT(*) 
                    FROM bookmarks_Chrome 
                    {bookmarksWhere}
                    GROUP BY Browser;";

            string sqlqueryFirefoxBkMkMenu = $@"SELECT Browser, COUNT(*) 
                    FROM bookmarks_Firefox 
                    {bookmarksWhere}
                    GROUP BY Browser;";

            using (SQLiteConnection connection = new SQLiteConnection(Helpers.chromeViewerConnectionString))
            {
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand(sqlqueryChromeBkMkMenu, connection))
                {
                    SearchSql.AddParameters(command);

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

                using (SQLiteCommand command = new SQLiteCommand(sqlqueryFirefoxBkMkMenu, connection))
                {
                    SearchSql.AddParameters(command);

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







            searchCondition = SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "FieldName", "Value");
            string autofillWhere = SearchSql.Where(searchCondition, SearchSql.TimeCondition("FirstUsed", "LastUsed"), SearchSql.LabelCondition());


            string sqlqueryBrowserAutoFillMenu = $@"
                                                    SELECT Browser, COUNT(*) as Count
                                                    FROM autofill_data
                                                    {autofillWhere}
                                                    GROUP BY Browser;
                                                ";

            using (SQLiteConnection connection = new SQLiteConnection(Helpers.chromeViewerConnectionString))
            {
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand(sqlqueryBrowserAutoFillMenu, connection))
                {

                    SearchSql.AddParameters(command);

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


            searchCondition = SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Host", "Name", "Value", "Path");
            string cookiesWhere = SearchSql.Where(searchCondition, SearchSql.TimeCondition("Created", "Expires", "LastAccessed"), SearchSql.LabelCondition());

            string sqlqueryBrowserCookiesMenu = $@"
                                                    SELECT Browser, COUNT(*) as Count
                                                    FROM cookies_data
                                                    {cookiesWhere}
                                                    GROUP BY Browser;
                                                ";

            using (SQLiteConnection connection = new SQLiteConnection(Helpers.chromeViewerConnectionString))
            {
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand(sqlqueryBrowserCookiesMenu, connection))
                {
                    SearchSql.AddParameters(command);

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string browser = reader.GetString(0);
                            int count = reader.GetInt32(1);

                            Helpers.browsersWithCookies[browser] = count;
                        }
                    }
                }
            }


            searchCondition = SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Url", "Host", "ContentType", "Server", "CacheFile", "CacheKey");
            string cacheWhere = SearchSql.Where(searchCondition, SearchSql.TimeCondition("Created", "Modified", "LastAccessed"), SearchSql.LabelCondition());

            string sqlqueryBrowserCacheMenu = $@"
                                                    SELECT Browser, COUNT(*) as Count
                                                    FROM cache_data
                                                    {cacheWhere}
                                                    GROUP BY Browser;
                                                ";

            using (SQLiteConnection connection = new SQLiteConnection(Helpers.chromeViewerConnectionString))
            {
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand(sqlqueryBrowserCacheMenu, connection))
                {
                    SearchSql.AddParameters(command);

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string browser = reader.GetString(0);
                            int count = reader.GetInt32(1);

                            Helpers.browsersWithCache[browser] = count;
                        }
                    }
                }
            }


            searchCondition = SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Url", "Title", "OriginalUrl", "Referrer", "SessionFile", "SourceType");
            string sessionsWhere = SearchSql.Where(searchCondition, SearchSql.TimeCondition("LastAccessed", "Created"), SearchSql.LabelCondition());

            string sqlqueryBrowserSessionsMenu = $@"
                                                    SELECT Browser, COUNT(*) as Count
                                                    FROM session_data
                                                    {sessionsWhere}
                                                    GROUP BY Browser;
                                                ";

            using (SQLiteConnection connection = new SQLiteConnection(Helpers.chromeViewerConnectionString))
            {
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand(sqlqueryBrowserSessionsMenu, connection))
                {
                    SearchSql.AddParameters(command);

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string browser = reader.GetString(0);
                            int count = reader.GetInt32(1);

                            Helpers.browsersWithSessions[browser] = count;
                        }
                    }
                }
            }


            searchCondition = SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "ExtensionId", "Name", "Version", "Description", "Author", "HomepageUrl", "Permissions", "HostPermissions", "ExtensionPath", "SourceFile");
            string extensionsWhere = SearchSql.Where(searchCondition, SearchSql.TimeCondition("InstallTime", "LastUpdateTime"), SearchSql.LabelCondition());

            string sqlqueryBrowserExtensionsMenu = $@"
                                                    SELECT Browser, COUNT(*) as Count
                                                    FROM extension_data
                                                    {extensionsWhere}
                                                    GROUP BY Browser;
                                                ";

            using (SQLiteConnection connection = new SQLiteConnection(Helpers.chromeViewerConnectionString))
            {
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand(sqlqueryBrowserExtensionsMenu, connection))
                {
                    SearchSql.AddParameters(command);

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string browser = reader.GetString(0);
                            int count = reader.GetInt32(1);

                            Helpers.browsersWithExtensions[browser] = count;
                        }
                    }
                }
            }

            searchCondition = SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Url", "Action_url", "Signon_realm", "Username", "Username_field", "Password_field", "Scheme", "Decryption_status", "Credential_artifact_value", "Store", "Login_guid");
            string loginsWhere = SearchSql.Where(searchCondition, SearchSql.TimeCondition("Created", "Last_used", "Password_changed"), SearchSql.LabelCondition());

            string sqlqueryBrowserLoginsMenu = $@"
                                                    SELECT Browser, COUNT(*) as Count
                                                    FROM saved_logins_data
                                                    {loginsWhere}
                                                    GROUP BY Browser;
                                                ";

            using (SQLiteConnection connection = new SQLiteConnection(Helpers.chromeViewerConnectionString))
            {
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand(sqlqueryBrowserLoginsMenu, connection))
                {
                    SearchSql.AddParameters(command);

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string browser = reader.GetString(0);
                            int count = reader.GetInt32(1);

                            Helpers.browsersWithLogins[browser] = count;
                        }
                    }
                }
            }

            searchCondition = SearchSql.TextCondition("Artifact_type", "Potential_activity", "Browser", "Origin", "Host", "Storage_key", "Value_preview", "Value_sha256", "Source_kind", "Source_file", "Parser_notes");
            string localStorageWhere = SearchSql.Where(searchCondition, SearchSql.TimeCondition("Created", "Modified", "LastAccessed"), SearchSql.LabelCondition());

            string sqlqueryBrowserLocalStorageMenu = $@"
                                                    SELECT Browser, COUNT(*) as Count
                                                    FROM local_storage_data
                                                    {localStorageWhere}
                                                    GROUP BY Browser;
                                                ";

            using (SQLiteConnection connection = new SQLiteConnection(Helpers.chromeViewerConnectionString))
            {
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand(sqlqueryBrowserLocalStorageMenu, connection))
                {
                    SearchSql.AddParameters(command);

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string browser = reader.GetString(0);
                            int count = reader.GetInt32(1);

                            Helpers.browsersWithLocalStorage[browser] = count;
                        }
                    }
                }
            }

            string sqlqueryBrowserSessionStorageMenu = $@"
                                                    SELECT Browser, COUNT(*) as Count
                                                    FROM session_storage_data
                                                    {localStorageWhere}
                                                    GROUP BY Browser;
                                                ";

            using (SQLiteConnection connection = new SQLiteConnection(Helpers.chromeViewerConnectionString))
            {
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand(sqlqueryBrowserSessionStorageMenu, connection))
                {
                    SearchSql.AddParameters(command);

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string browser = reader.GetString(0);
                            int count = reader.GetInt32(1);

                            Helpers.browsersWithSessionStorage[browser] = count;
                        }
                    }
                }
            }

            string sqlqueryBrowserIndexedDbMenu = $@"
                                                    SELECT Browser, COUNT(*) as Count
                                                    FROM indexeddb_data
                                                    {localStorageWhere}
                                                    GROUP BY Browser;
                                                ";

            using (SQLiteConnection connection = new SQLiteConnection(Helpers.chromeViewerConnectionString))
            {
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand(sqlqueryBrowserIndexedDbMenu, connection))
                {
                    SearchSql.AddParameters(command);

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string browser = reader.GetString(0);
                            int count = reader.GetInt32(1);

                            Helpers.browsersWithIndexedDb[browser] = count;
                        }
                    }
                }
            }


            if (!Helpers.searchTermRegExp && Helpers.searchTermExists)
            {
                this.sfDataGrid1.SearchController.Search(Helpers.searchTerm);
            }
            else
            {
                this.sfDataGrid1.SearchController.ClearSearch();
            }

            AcordeonMenu(Helpers.browserUrls, Helpers.browsersWithDownloads, Helpers.browsersWithBookmarks, Helpers.browsersWithAutofill, Helpers.browsersWithCookies, Helpers.browsersWithCache, Helpers.browsersWithSessions, Helpers.browsersWithExtensions);

        }


        private string AdjustToUtc(DateTime dateTime, int utcOffset)
        {
            TimeSpan offset = TimeSpan.FromHours(utcOffset * -1);
            DateTime adjustedDateTime = dateTime.Add(offset);

            return adjustedDateTime.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private bool IsValidRegex(string pattern)
        {
            try
            {
                Regex.Match("", pattern);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
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

        private void HighlightWithRegex(string pattern, Color highlightColor)
        {
            try
            {
                string content = richTextBox1.Text;

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
            foreach (var record in sfDataGrid1.SelectedItems)
            {
                var dataRowView = record as DataRowView;
                if (dataRowView != null)
                {
                    var columnasPresentes = dataRowView.DataView!.Table!.Columns
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
                            tablaDetectada = Helpers.HistoryTableForBrowser(browser);

                        else if (tablaDetectada == "AllDownloads")
                            tablaDetectada = Helpers.DownloadTableForBrowser(browser);

                        else if (tablaDetectada == "AllBookmarks")
                            tablaDetectada = Helpers.BookmarkTableForBrowser(browser);
                    }

                    var idValor = dataRowView["id"];

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
            ResetSearchState();
            Helpers.browserUrls.Clear();
            Helpers.browserHistoryCounts.Clear();
            Helpers.browserHistoryCategoryCounts.Clear();
            Helpers.browsersWithDownloads.Clear();
            Helpers.browsersWithBookmarks.Clear();
            Helpers.browsersWithAutofill.Clear();
            Helpers.browsersWithCookies.Clear();
            Helpers.browsersWithCache.Clear();
            Helpers.browsersWithSessions.Clear();
            Helpers.browsersWithExtensions.Clear();
            Helpers.browsersWithLogins.Clear();
            Helpers.browsersWithLocalStorage.Clear();
            Helpers.browsersWithSessionStorage.Clear();
            Helpers.browsersWithIndexedDb.Clear();
            ShowFullTimelineWebActivity();
        }

        private void OpenComments()
        {
            Form? OpenForm = Application.OpenForms.OfType<Form_Comments>().FirstOrDefault();
            if (OpenForm != null)
            {
                OpenForm.BringToFront();
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

            Form? OpenForm = Application.OpenForms.OfType<Form_LabelManager>().FirstOrDefault();

            if (OpenForm != null)
            {
                OpenForm.BringToFront();
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
            }
            else
            {
                dateTimePicker_start.Enabled = false;
                dateTimePicker_end.Enabled = false;
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

                float pageWidth = PdfPageSize.A4.Width;
                var header = new PdfPageTemplateElement(pageWidth, 40);
                var headerFont = new PdfStandardFont(PdfFontFamily.Helvetica, 12, PdfFontStyle.Bold);
                var subFont = new PdfStandardFont(PdfFontFamily.Helvetica, 9);
                header.Graphics.DrawString($"Browser Reviewer    {Helpers.db_name}",
                    headerFont, PdfBrushes.DarkSlateGray, new PointF(0, 4));
                header.Graphics.DrawString(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    subFont, PdfBrushes.Gray, new PointF(0, 22));
                document.Template.Top = header;

                var visibleCols = dataGrid.Columns.Where(c => c.Visible).ToList();
                int rowCount = dataGrid.View.Records.Count;
                string sortInfo = GetSortSummary(dataGrid);
                string filtInfo = GetFilterSummary(dataGrid);

                DrawPdfCoverPage(document, visibleCols, rowCount, sortInfo, filtInfo);

                int recordNumber = 0;
                foreach (var rec in dataGrid.View.Records)
                {
                    var rv = rec.Data as DataRowView;
                    if (rv == null) continue;

                    recordNumber++;
                    DrawReviewRecordPage(document, rv, visibleCols, recordNumber, rowCount);
                }

                var footerFont = new PdfStandardFont(PdfFontFamily.Helvetica, 9);
                int totalPages = document.Pages.Count;
                for (int i = 0; i < totalPages; i++)
                {
                    var page = document.Pages[i];
                    var g = page.Graphics;

                    string txt = $"Page {i + 1} of {totalPages}";
                    SizeF size = footerFont.MeasureString(txt);

                    float x = page.GetClientSize().Width - size.Width - 36;
                    float y = page.GetClientSize().Height + 6;

                    g.DrawString(txt, footerFont, PdfBrushes.Gray, new PointF(x, y));
                }

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











            using (var dlg = new FontDialog())
            {
                dlg.Font = this.Font;
                dlg.ShowEffects = true;

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    Font f = dlg.Font;

                    Helpers.FM = new Font(f.FontFamily, f.SizeInPoints, f.Style, GraphicsUnit.Point);

                    float smaller = Math.Max(f.SizeInPoints - 1, 1);
                    Helpers.FS = new Font(f.FontFamily, smaller, f.Style, GraphicsUnit.Point);


                    float nuevoTamano = f.SizeInPoints;
                    AplicarTamanoSoloAEtiquetas(groupBox_Main, nuevoTamano);

                    sfDataGrid1.Style.CellStyle.Font.Size = f.SizeInPoints;
                    sfDataGrid1.Style.HeaderStyle.Font.Size = f.SizeInPoints;

                    int textH = TextRenderer.MeasureText("Ag", f).Height;
                    int pad = 8;
                    sfDataGrid1.RowHeight = Math.Max(textH + pad, 22);
                    sfDataGrid1.HeaderRowHeight = Math.Max(textH + pad, 24);

                    sfDataGrid1.Invalidate();
                    sfDataGrid1.Refresh();

                    richTextBox1.Font = new Font(richTextBox1.Font.FontFamily, f.SizeInPoints, f.Style, GraphicsUnit.Point);
                }
            }
        }



        private void AplicarTamanoSoloAEtique­tas(Control parent, float sizeInPoints)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is Label lbl)
                    lbl.Font = new Font(lbl.Font.FontFamily, sizeInPoints, lbl.Font.Style, GraphicsUnit.Point);

                if (c.HasChildren) AplicarTamanoSoloAEtique­tas(c, sizeInPoints);
            }
        }


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

        private void ExportCurrentGridTimelineHtml()
        {
            if (sfDataGrid1 == null || sfDataGrid1.View == null || sfDataGrid1.View.Records.Count == 0)
            {
                MessageBox.Show("No hay datos en la grilla para crear la linea de tiempo.", "Time Line",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var sfd = new SaveFileDialog
            {
                Filter = "HTML (*.html)|*.html",
                FileName = $"Browser_Reviewer_TimeLine_{DateTime.Now:yyyyMMdd_HHmmss}.html"
            })
            {
                if (sfd.ShowDialog(this) != DialogResult.OK) return;

                string html = BuildTimelineHtmlFromGrid(sfDataGrid1, labelStatus.Text, sfd.FileName);
                File.WriteAllText(sfd.FileName, html, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

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
                    MessageBox.Show("El HTML fue generado, pero no se pudo abrir automaticamente.", "Time Line",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private string BuildTimelineHtmlFromGrid(SfDataGrid grid, string contextTitle, string outputHtmlPath)
        {
            var rows = ExtractTimelineRows(grid, outputHtmlPath);
            string rowsJson = System.Text.Json.JsonSerializer.Serialize(rows);
            string generatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string tzStr = $"UTC{(Helpers.utcOffset >= 0 ? "+" : "")}{Helpers.utcOffset}";
            string title = string.IsNullOrWhiteSpace(contextTitle) ? "Browser Reviewer Time Line" : contextTitle;
            string safeTitle = WebUtility.HtmlEncode(title);
            string safeGeneratedAt = WebUtility.HtmlEncode(generatedAt);
            string safeTz = WebUtility.HtmlEncode(tzStr);
            string safeSort = WebUtility.HtmlEncode(GetSortSummary(grid));
            string safeFilters = WebUtility.HtmlEncode(GetFilterSummary(grid));

            return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1"">
<title>{safeTitle} - Time Line</title>
<script src=""https://cdn.jsdelivr.net/npm/echarts@5/dist/echarts.min.js""></script>
<style>
:root {{
  --ink:#1d2329;
  --muted:#67717d;
  --line:#d9dee7;
  --panel:#ffffff;
  --soft:#f4f7fb;
  --accent:#2764a8;
  --accent2:#2f855a;
  --warn:#b7791f;
  --danger:#b83232;
}}
* {{ box-sizing:border-box; }}
body {{
  margin:0;
  font-family:'Segoe UI', Arial, sans-serif;
  color:var(--ink);
  background:#eef2f7;
}}
header {{
  padding:22px 28px 18px;
  background:#18202a;
  color:#fff;
  border-bottom:4px solid #5b8def;
}}
h1 {{ margin:0 0 8px; font-size:24px; font-weight:650; }}
.sub {{ color:#d9e3ef; font-size:13px; line-height:1.5; }}
main {{ padding:18px; max-width:1800px; margin:0 auto; }}
.cards {{
  display:grid;
  grid-template-columns:repeat(4,minmax(180px,1fr));
  gap:12px;
  margin-bottom:14px;
}}
.card {{
  background:var(--panel);
  border:1px solid var(--line);
  border-radius:8px;
  padding:14px;
  min-height:88px;
}}
.card .k {{ font-size:12px; color:var(--muted); text-transform:uppercase; letter-spacing:.02em; }}
.card .v {{ margin-top:8px; font-size:26px; font-weight:700; }}
.grid {{
  display:grid;
  grid-template-columns:2fr 1fr;
  gap:14px;
  align-items:stretch;
}}
.panel {{
  background:var(--panel);
  border:1px solid var(--line);
  border-radius:8px;
  padding:14px;
  min-height:360px;
}}
.panel h2 {{
  margin:0 0 10px;
  font-size:16px;
  font-weight:650;
}}
.chart {{ width:100%; height:340px; }}
.primary-chart {{ height:460px; }}
.secondary-chart {{ height:300px; }}
.wide {{ grid-column:1 / -1; }}
.timeline-list {{
  display:grid;
  gap:10px;
  max-height:560px;
  overflow:auto;
  padding-right:4px;
}}
.event {{
  border-left:5px solid var(--accent);
  background:#fff;
  border-radius:8px;
  border-top:1px solid var(--line);
  border-right:1px solid var(--line);
  border-bottom:1px solid var(--line);
  padding:10px 12px;
}}
.event .time {{ font-size:12px; color:var(--muted); }}
.event .title {{ margin-top:4px; font-weight:650; overflow-wrap:anywhere; }}
.event .meta {{ margin-top:6px; color:#44505c; font-size:13px; }}
.event .url {{ margin-top:6px; font-size:12px; overflow-wrap:anywhere; color:#315f9f; }}
.cache-pill {{
  display:inline-flex;
  margin-top:8px;
  border:1px solid #bdd6f0;
  border-radius:8px;
  padding:4px 7px;
  background:#eef6ff;
  color:#24527a;
  font-size:12px;
}}
.cache-link {{
  display:inline-block;
  margin-top:4px;
  color:#2764a8;
  font-weight:600;
}}
.toolbar {{
  display:flex;
  gap:10px;
  flex-wrap:wrap;
  margin-bottom:12px;
}}
input, select {{
  border:1px solid var(--line);
  border-radius:6px;
  padding:8px 10px;
  font-size:13px;
  background:#fff;
}}
input {{ min-width:280px; flex:1; }}
table {{
  width:100%;
  border-collapse:collapse;
  background:#fff;
  font-size:13px;
}}
th, td {{
  border:1px solid var(--line);
  padding:8px;
  vertical-align:top;
  overflow-wrap:anywhere;
}}
th {{
  position:sticky;
  top:0;
  background:#f5f7fb;
  text-align:left;
  z-index:1;
}}
.table-wrap {{ max-height:620px; overflow:auto; border:1px solid var(--line); border-radius:8px; }}
.note {{
  color:var(--muted);
  font-size:12px;
  margin-top:8px;
}}
.drill-panel {{
  margin-bottom:14px;
  min-height:auto;
}}
.focus-line {{
  font-size:14px;
  color:#344252;
  line-height:1.5;
}}
.chips {{
  display:flex;
  gap:8px;
  flex-wrap:wrap;
  margin-top:10px;
}}
.chip {{
  display:inline-flex;
  align-items:center;
  gap:8px;
  border:1px solid #cbd5e1;
  border-radius:8px;
  padding:6px 8px;
  background:#f8fafc;
  font-size:12px;
}}
.chip button, .muted-action {{
  border:1px solid #cbd5e1;
  background:#fff;
  color:#334155;
  border-radius:6px;
  padding:5px 8px;
  cursor:pointer;
}}
.muted-action:hover, .chip button:hover {{
  border-color:var(--accent);
  color:var(--accent);
}}
.event {{
  cursor:pointer;
}}
.event.active {{
  outline:2px solid var(--accent);
  outline-offset:1px;
}}
tr.row-clickable {{
  cursor:pointer;
}}
tr.row-clickable:hover {{
  background:#f8fafc;
}}
.drawer-backdrop {{
  display:none;
  position:fixed;
  inset:0;
  background:rgba(15,23,42,.25);
  z-index:9;
}}
.drawer-backdrop.open {{
  display:block;
}}
.drawer {{
  position:fixed;
  top:0;
  right:-540px;
  width:min(540px, 96vw);
  height:100vh;
  background:#fff;
  border-left:1px solid var(--line);
  box-shadow:-18px 0 40px rgba(15,23,42,.22);
  z-index:10;
  transition:right .18s ease;
  display:flex;
  flex-direction:column;
}}
.drawer.open {{
  right:0;
}}
.drawer header {{
  background:#18202a;
  border-bottom:0;
  padding:16px 18px;
}}
.drawer header h2 {{
  margin:0;
  color:#fff;
  font-size:18px;
}}
.drawer .close {{
  position:absolute;
  top:12px;
  right:12px;
  border:1px solid rgba(255,255,255,.45);
  background:transparent;
  color:#fff;
  border-radius:6px;
  padding:5px 8px;
  cursor:pointer;
}}
.drawer-body {{
  padding:16px 18px;
  overflow:auto;
}}
.plain-language {{
  background:#f8fafc;
  border:1px solid var(--line);
  border-radius:8px;
  padding:12px;
  line-height:1.5;
  margin-bottom:14px;
}}
.media-preview {{
  display:none;
  margin-bottom:14px;
  border:1px solid var(--line);
  border-radius:8px;
  background:#f8fafc;
  padding:10px;
}}
.media-preview.open {{
  display:block;
}}
.media-preview img, .media-preview video {{
  max-width:100%;
  max-height:360px;
  border-radius:6px;
  background:#111827;
}}
.media-preview audio {{
  width:100%;
}}
.media-preview iframe {{
  width:100%;
  height:420px;
  border:1px solid var(--line);
  border-radius:6px;
  background:#fff;
}}
.media-caption {{
  margin-top:8px;
  color:var(--muted);
  font-size:12px;
  overflow-wrap:anywhere;
}}
.detail-grid {{
  display:grid;
  grid-template-columns:130px 1fr;
  gap:8px 10px;
  font-size:13px;
}}
.detail-grid .label {{
  color:var(--muted);
  font-weight:650;
}}
.detail-grid .value {{
  overflow-wrap:anywhere;
}}
.activity-index {{
  min-height:260px;
}}
.activity-search {{
  width:100%;
  min-width:0;
  margin-bottom:10px;
}}
.activity-list {{
  display:grid;
  grid-template-columns:repeat(auto-fill,minmax(260px,1fr));
  gap:8px;
  max-height:360px;
  overflow:auto;
  padding-right:4px;
}}
.activity-item {{
  display:flex;
  justify-content:space-between;
  gap:10px;
  align-items:center;
  border:1px solid var(--line);
  border-radius:8px;
  background:#fff;
  padding:8px 10px;
  cursor:pointer;
  text-align:left;
  color:var(--ink);
}}
.activity-item:hover, .activity-item.active {{
  border-color:var(--accent);
  background:#f8fafc;
}}
.activity-name {{
  font-size:13px;
  overflow-wrap:anywhere;
}}
.activity-count {{
  color:var(--muted);
  font-size:12px;
  white-space:nowrap;
}}
@media (max-width:1100px) {{
  .cards, .grid {{ grid-template-columns:1fr; }}
  .wide {{ grid-column:auto; }}
}}
</style>
</head>
<body>
<header>
  <h1>{safeTitle}</h1>
  <div class=""sub"">
    Browser Reviewer timeline generated from the current grid state. Generated at {safeGeneratedAt}. Time zone display: {safeTz}.<br>
    Sort: {safeSort}. Filters: {safeFilters}.
  </div>
</header>
<main>
  <section class=""cards"">
    <div class=""card""><div class=""k"">Events</div><div class=""v"" id=""totalEvents"">0</div></div>
    <div class=""card""><div class=""k"">Artifacts</div><div class=""v"" id=""artifactCount"">0</div></div>
    <div class=""card""><div class=""k"">Potential Activities</div><div class=""v"" id=""activityCount"">0</div></div>
    <div class=""card""><div class=""k"">Browsers</div><div class=""v"" id=""browserCount"">0</div></div>
  </section>

  <section class=""panel drill-panel"">
    <h2>Review focus</h2>
    <div id=""focusSummary"" class=""focus-line"">Loading timeline...</div>
    <div id=""activeChips"" class=""chips""></div>
  </section>

  <section class=""grid"">
    <div class=""panel wide"">
      <h2>Investigation heatmap</h2>
      <div id=""hourHeat"" class=""chart primary-chart""></div>
      <div class=""note"">Start here. Darker cells mark time windows with more browser activity. Click a cell to drill into that hour.</div>
    </div>
    <div class=""panel"">
      <h2>Daily volume</h2>
      <div id=""dayBar"" class=""chart""></div>
    </div>
    <div class=""panel"">
      <h2>Top potential activity</h2>
      <div id=""activityBar"" class=""chart""></div>
      <div class=""note"">Click an activity to focus the story.</div>
    </div>
    <div class=""panel"">
      <h2>Browser contribution</h2>
      <div id=""browserBar"" class=""chart""></div>
    </div>
    <div class=""panel"">
      <h2>Artifact sources</h2>
      <div id=""artifactPie"" class=""chart""></div>
    </div>
    <div class=""panel wide"">
      <h2>Chronological story</h2>
      <div class=""toolbar"">
        <input id=""search"" placeholder=""Filter by URL, title, browser, artifact, activity..."">
        <select id=""artifactFilter""><option value="""">All artifacts</option></select>
        <select id=""activityFilter""><option value="""">All potential activities</option></select>
        <select id=""browserFilter""><option value="""">All browsers</option></select>
        <button id=""clearDrill"" class=""muted-action"" type=""button"">Clear drill down</button>
      </div>
      <div id=""eventList"" class=""timeline-list""></div>
    </div>
    <div class=""panel wide"">
      <h2>Event-level map</h2>
      <div id=""scatter"" class=""chart secondary-chart""></div>
      <div class=""note"">Each point is one timestamped row. Use this after narrowing the heatmap or filters.</div>
    </div>
    <div class=""panel wide activity-index"">
      <h2>All potential activities</h2>
      <input id=""activitySearch"" class=""activity-search"" placeholder=""Search potential activities..."">
      <div id=""activityIndex"" class=""activity-list""></div>
    </div>
    <div class=""panel wide"">
      <h2>Evidence table</h2>
      <div class=""table-wrap"">
        <table>
          <thead><tr><th>id</th><th>Artifact type</th><th>Potential Activity</th><th>Time</th><th>Browser</th><th>Title</th><th>URL / Detail</th><th>Source</th></tr></thead>
          <tbody id=""tableBody""></tbody>
        </table>
      </div>
    </div>
  </section>
</main>
<div id=""detailBackdrop"" class=""drawer-backdrop""></div>
<aside id=""detailDrawer"" class=""drawer"" aria-hidden=""true"">
  <header>
    <h2>Artifact detail</h2>
    <button id=""closeDetail"" class=""close"" type=""button"">Close</button>
  </header>
  <div class=""drawer-body"">
    <div id=""plainMeaning"" class=""plain-language""></div>
    <div id=""cacheMediaPreview"" class=""media-preview""></div>
    <div id=""detailFields"" class=""detail-grid""></div>
  </div>
</aside>
<script>
const rawRows = {rowsJson};

function escapeHtml(value) {{
  return String(value ?? '').replace(/[&<>""']/g, ch => ({{'&':'&amp;','<':'&lt;','>':'&gt;','""':'&quot;',""'"":'&#39;'}}[ch]));
}}
function parseTime(value) {{
  if (!value) return null;
  let normalized = String(value).trim().replace(' ', 'T');
  normalized = normalized.replace(/(\.\d{{3}})\d+/, '$1');
  const date = new Date(normalized);
  return Number.isNaN(date.getTime()) ? null : date;
}}
function countBy(rows, key) {{
  const map = new Map();
  rows.forEach(r => {{
    const value = r[key] || 'Unknown';
    map.set(value, (map.get(value) || 0) + 1);
  }});
  return Array.from(map.entries()).sort((a,b) => b[1] - a[1]);
}}
function unique(rows, key) {{
  return Array.from(new Set(rows.map(r => r[key] || 'Unknown'))).sort((a,b) => a.localeCompare(b));
}}
function formatDate(date) {{
  if (!date) return '';
  const pad = n => String(n).padStart(2, '0');
  return date.getFullYear() + '-' + pad(date.getMonth()+1) + '-' + pad(date.getDate()) + ' ' + pad(date.getHours()) + ':' + pad(date.getMinutes()) + ':' + pad(date.getSeconds());
}}
function enrichRows(rows) {{
  return rows.map((r, i) => {{
    const date = parseTime(r.time);
    return {{
      ...r,
      browser:r.browser || 'Unknown',
      artifact_type:r.artifact_type || r.artifact || 'Unknown',
      artifact:r.artifact || 'Unknown',
      activity:r.activity || 'Unknown',
      _index:i,
      _date:date,
      _timeMs:date ? date.getTime() : null,
      _day:date ? date.toISOString().slice(0,10) : '',
      _hour:date ? date.getHours() : null
    }};
  }}).sort((a,b) => (b._timeMs || 0) - (a._timeMs || 0));
}}

const rows = enrichRows(rawRows);
const artifactNames = unique(rows, 'artifact');
const activityNames = unique(rows, 'activity');
const browsers = unique(rows, 'browser');
const charts = {{}};
const activeDrill = {{day:'', hour:null}};
let selectedIndex = null;

document.getElementById('totalEvents').textContent = rows.length;
document.getElementById('artifactCount').textContent = artifactNames.length;
document.getElementById('activityCount').textContent = activityNames.length;
document.getElementById('browserCount').textContent = browsers.length;

const artifactFilter = document.getElementById('artifactFilter');
artifactNames.forEach(v => artifactFilter.insertAdjacentHTML('beforeend', `<option value=""${{escapeHtml(v)}}"">${{escapeHtml(v)}}</option>`));
const activityFilter = document.getElementById('activityFilter');
activityNames.forEach(v => activityFilter.insertAdjacentHTML('beforeend', `<option value=""${{escapeHtml(v)}}"">${{escapeHtml(v)}}</option>`));
const browserFilter = document.getElementById('browserFilter');
browsers.forEach(v => browserFilter.insertAdjacentHTML('beforeend', `<option value=""${{escapeHtml(v)}}"">${{escapeHtml(v)}}</option>`));
const activitySearch = document.getElementById('activitySearch');

function chart(id) {{
  if (!charts[id]) charts[id] = echarts.init(document.getElementById(id));
  return charts[id];
}}

function buildCharts(filtered) {{
  const scatter = chart('scatter');
  const artifactPie = chart('artifactPie');
  const activityBar = chart('activityBar');
  const browserBar = chart('browserBar');
  const hourHeat = chart('hourHeat');
  const dayBar = chart('dayBar');

  const lanes = unique(filtered, 'artifact');
  const activities = unique(filtered, 'activity');
  const activityIndex = new Map(activities.map((a,i) => [a,i]));
  const scatterData = filtered.filter(r => r._timeMs).map(r => [
    r._timeMs,
    lanes.indexOf(r.artifact || 'Unknown'),
    r.activity || 'Unknown',
    r.browser || '',
    r.title || '',
    r.url || '',
    r.source || '',
    r._index
  ]);

  scatter.clear();
  artifactPie.clear();
  activityBar.clear();
  browserBar.clear();
  hourHeat.clear();
  dayBar.clear();

  scatter.setOption({{
    tooltip: {{
      trigger:'item',
      formatter: p => {{
        const d = new Date(p.value[0]);
        return `<b>${{escapeHtml(p.value[2])}}</b><br>${{formatDate(d)}}<br>${{escapeHtml(p.value[3])}} / ${{escapeHtml(lanes[p.value[1]])}}<br>${{escapeHtml(p.value[4])}}<br><span style=""color:#6b7280"">${{escapeHtml(p.value[5])}}</span>`;
      }}
    }},
    grid: {{left:110,right:30,top:25,bottom:70}},
    dataZoom:[{{type:'inside'}},{{type:'slider', bottom:20}}],
    xAxis: {{type:'time'}},
    yAxis: {{type:'category', data:lanes, axisLabel:{{interval:0}}}},
    visualMap: {{
      type:'piecewise',
      show:false,
      dimension:2,
      categories:activities
    }},
    series:[{{
      type:'scatter',
      symbolSize: val => Math.max(8, Math.min(22, 8 + (activityIndex.get(val[2]) || 0) % 8)),
      data:scatterData
    }}]
  }});
  scatter.off('click');
  scatter.on('click', p => {{
    const row = rows.find(r => r._index === p.value[7]);
    if (row) openDetail(row);
  }});

  artifactPie.setOption({{
    tooltip:{{trigger:'item'}},
    legend:{{bottom:0,type:'scroll'}},
    series:[{{type:'pie', radius:['35%','70%'], center:['50%','45%'], data:countBy(filtered,'artifact').map(([name,value])=>({{name,value}}))}}]
  }});
  artifactPie.off('click');
  artifactPie.on('click', p => {{
    artifactFilter.value = p.name;
    renderRows();
  }});

  const activityCounts = countBy(filtered,'activity').sort((a,b) => a[0].localeCompare(b[0]));
  activityBar.setOption({{
    tooltip:{{trigger:'axis'}},
    grid:{{left:190,right:45,top:20,bottom:30}},
    dataZoom:[
      {{type:'inside', yAxisIndex:0}},
      {{type:'slider', yAxisIndex:0, right:4, width:16}}
    ],
    xAxis:{{type:'value'}},
    yAxis:{{type:'category', data:activityCounts.map(x=>x[0]), axisLabel:{{interval:0}}}},
    series:[{{type:'bar', data:activityCounts.map(x=>x[1]), itemStyle:{{color:'#2764a8'}}}}]
  }});
  activityBar.off('click');
  activityBar.on('click', p => {{
    activityFilter.value = p.name;
    renderRows();
  }});

  const browserCounts = countBy(filtered,'browser').reverse();
  browserBar.setOption({{
    tooltip:{{trigger:'axis'}},
    grid:{{left:90,right:20,top:20,bottom:30}},
    xAxis:{{type:'value'}},
    yAxis:{{type:'category', data:browserCounts.map(x=>x[0])}},
    series:[{{type:'bar', data:browserCounts.map(x=>x[1]), itemStyle:{{color:'#2f855a'}}}}]
  }});
  browserBar.off('click');
  browserBar.on('click', p => {{
    browserFilter.value = p.name;
    renderRows();
  }});

  const heatMap = new Map();
  filtered.forEach(r => {{
    if (!r._date) return;
    const key = r._day + '|' + r._date.getHours();
    heatMap.set(key, (heatMap.get(key)||0)+1);
  }});
  const heatDays = Array.from(new Set(filtered.filter(r=>r._day).map(r=>r._day))).sort();
  const dayCounts = heatDays.map(day => filtered.filter(r => r._day === day).length);
  const heatData = [];
  heatDays.forEach((day, dayIndex) => {{
    for (let h=0; h<24; h++) heatData.push([h, dayIndex, heatMap.get(day+'|'+h)||0]);
  }});
  dayBar.setOption({{
    tooltip:{{trigger:'axis'}},
    grid:{{left:55,right:20,top:20,bottom:70}},
    dataZoom:[{{type:'inside'}},{{type:'slider', bottom:20}}],
    xAxis:{{type:'category', data:heatDays, axisLabel:{{rotate:35}}}},
    yAxis:{{type:'value'}},
    series:[{{type:'bar', data:dayCounts, itemStyle:{{color:'#2764a8'}}}}]
  }});
  dayBar.off('click');
  dayBar.on('click', p => {{
    activeDrill.day = p.name || '';
    activeDrill.hour = null;
    renderRows();
  }});

  hourHeat.setOption({{
    tooltip:{{formatter:p=>`${{heatDays[p.value[1]]}} hour ${{p.value[0]}}:00<br><b>${{p.value[2]}}</b> events`}},
    grid:{{left:90,right:20,top:20,bottom:45}},
    xAxis:{{type:'category', data:Array.from({{length:24}},(_,i)=>String(i).padStart(2,'0'))}},
    yAxis:{{type:'category', data:heatDays}},
    visualMap:{{min:0,max:Math.max(1,...heatData.map(x=>x[2])), orient:'horizontal', left:'center', bottom:0}},
    series:[{{type:'heatmap', data:heatData, emphasis:{{itemStyle:{{shadowBlur:8, shadowColor:'rgba(0,0,0,.25)'}}}}}}]
  }});
  hourHeat.off('click');
  hourHeat.on('click', p => {{
    if (!p.value || p.value[2] < 1) return;
    activeDrill.day = heatDays[p.value[1]] || '';
    activeDrill.hour = p.value[0];
    renderRows();
  }});
}}

function explain(row) {{
  const artifact = String(row.artifact || '').toLowerCase();
  const activity = row.activity || 'browser activity';
  const browser = row.browser || 'Unknown browser';
  if (artifact.includes('cache')) return `This record means ${{escapeHtml(browser)}} stored web content locally. That can be a page resource, image, script, document, audio, or video used while loading a site. Treat it as supporting evidence and review nearby events to understand whether the user directly viewed the content.`;
  if (artifact.includes('cookie')) return `This record means a website stored data in ${{escapeHtml(browser)}}. Cookies can indicate visits, sessions, preferences, tracking, or authentication context. They are strongest when reviewed with history and cache around the same time.`;
  if (artifact.includes('download')) return `This record points to a file download or download-related browser record. Check the URL, target path, and surrounding timeline to understand what file was obtained and whether it completed.`;
  if (artifact.includes('history')) return `This is browser history. It usually means a page was visited or recorded by the browser at this time. It is one of the clearest indicators of intentional browsing activity.`;
  if (artifact.includes('bookmark')) return `This record indicates a saved page or bookmark. It may represent user intent, browser sync, or an imported profile item, so compare it with visit activity.`;
  if (artifact.includes('autofill')) return `This record comes from browser form data. It may show typed or saved form values such as names, emails, usernames, or addresses. Review it carefully because it can expose user-entered information.`;
  if (artifact.includes('indexeddb')) return `This record comes from IndexedDB, a structured browser database used by web applications. It can preserve application state, messages, tokens, cached objects, or offline data. Treat readable previews as leads and use hashes/source files for traceability.`;
  if (artifact.includes('storage')) return `This record comes from browser web storage. It can preserve site or web app state such as session values, local preferences, identifiers, tokens, carts, or profile data. It is strong supporting evidence when correlated with history, cookies, cache, and sessions.`;
  if (artifact.includes('session')) return `This is session restore data: tabs or windows remembered by the browser. It can show what was open, even when a normal history record is missing.`;
  if (artifact.includes('extension')) return `This record describes a browser extension. Extensions can affect browsing behavior, capture data, or explain unusual artifacts.`;
  return `This record is classified as ${{escapeHtml(activity)}}. Use the timestamp, browser, source, and neighboring events to decide what it means in the user activity story.`;
}}

function mediaPreviewHtml(row) {{
  if (!row.media_href) return '';
  const href = escapeHtml(row.media_href);
  const caption = `${{row.media_type || row.media_kind || 'Cache media'}}${{row.media_name ? ' / ' + row.media_name : ''}}`;
  let preview = '';
  if (row.media_kind === 'image') preview = `<a href=""${{href}}"" target=""_blank"" rel=""noopener""><img src=""${{href}}"" alt=""Exported cache image""></a>`;
  else if (row.media_kind === 'video') preview = `<video controls src=""${{href}}""></video>`;
  else if (row.media_kind === 'audio') preview = `<audio controls src=""${{href}}""></audio>`;
  else if (row.media_kind === 'pdf') preview = `<iframe src=""${{href}}"" title=""Exported cache PDF""></iframe>`;
  else preview = `<a class=""cache-link"" href=""${{href}}"" target=""_blank"" rel=""noopener"">Open exported cache file</a>`;
  return `${{preview}}<div class=""media-caption"">${{escapeHtml(caption)}}<br><a class=""cache-link"" href=""${{href}}"" target=""_blank"" rel=""noopener"">Open exported file</a></div>`;
}}

function openDetail(row) {{
  selectedIndex = row._index;
  document.querySelectorAll('.event').forEach(e => e.classList.toggle('active', Number(e.dataset.index) === selectedIndex));
  document.getElementById('plainMeaning').innerHTML = explain(row);
  const media = document.getElementById('cacheMediaPreview');
  const mediaHtml = mediaPreviewHtml(row);
  media.innerHTML = mediaHtml;
  media.classList.toggle('open', Boolean(mediaHtml));
  const fields = [
    ['Time', formatDate(row._date) || row.time || 'No timestamp'],
    ['Artifact type', row.artifact_type || 'Unknown'],
    ['Artifact', row.artifact || 'Unknown'],
    ['Potential activity', row.activity || 'Unknown'],
    ['Browser', row.browser || 'Unknown'],
    ['Title', row.title || ''],
    ['URL', row.url || ''],
    ['Detail', row.detail || ''],
    ['Exported cache media', row.media_href || ''],
    ['Source', row.source || '']
  ];
  document.getElementById('detailFields').innerHTML = fields.map(([label,value]) => `
    <div class=""label"">${{escapeHtml(label)}}</div>
    <div class=""value"">${{escapeHtml(value)}}</div>`).join('');
  document.getElementById('detailBackdrop').classList.add('open');
  document.getElementById('detailDrawer').classList.add('open');
  document.getElementById('detailDrawer').setAttribute('aria-hidden','false');
}}

function closeDetail() {{
  document.getElementById('detailBackdrop').classList.remove('open');
  document.getElementById('detailDrawer').classList.remove('open');
  document.getElementById('detailDrawer').setAttribute('aria-hidden','true');
}}

function renderFocus(filtered) {{
  const parts = [];
  if (document.getElementById('search').value) parts.push(['Text', document.getElementById('search').value, 'search']);
  if (artifactFilter.value) parts.push(['Artifact', artifactFilter.value, 'artifact']);
  if (activityFilter.value) parts.push(['Activity', activityFilter.value, 'activity']);
  if (browserFilter.value) parts.push(['Browser', browserFilter.value, 'browser']);
  if (activeDrill.day) parts.push(['Time bucket', activeDrill.hour === null ? `${{activeDrill.day}} all day` : `${{activeDrill.day}} ${{String(activeDrill.hour).padStart(2,'0')}}:00`, 'time']);

  const summary = parts.length
    ? `Showing ${{filtered.length}} of ${{rows.length}} events for the current review focus.`
    : `Showing all ${{rows.length}} events. Click a chart segment, bar, heatmap cell, point, event, or table row to drill down.`;
  document.getElementById('focusSummary').textContent = summary;
  document.getElementById('activeChips').innerHTML = parts.map(([label,value,key]) =>
    `<span class=""chip""><b>${{escapeHtml(label)}}</b> ${{escapeHtml(value)}} <button type=""button"" data-filter=""${{key}}"">Remove</button></span>`
  ).join('');
  document.querySelectorAll('#activeChips button').forEach(btn => btn.addEventListener('click', () => {{
    const key = btn.dataset.filter;
    if (key === 'search') document.getElementById('search').value = '';
    if (key === 'artifact') artifactFilter.value = '';
    if (key === 'activity') activityFilter.value = '';
    if (key === 'browser') browserFilter.value = '';
    if (key === 'time') {{ activeDrill.day = ''; activeDrill.hour = null; }}
    renderRows();
  }}));
}}

function renderActivityIndex(baseRows) {{
  const term = activitySearch.value.toLowerCase();
  const counts = countBy(baseRows, 'activity')
    .sort((a,b) => a[0].localeCompare(b[0]))
    .filter(([name]) => !term || name.toLowerCase().includes(term));
  document.getElementById('activityIndex').innerHTML = counts.map(([name,value]) => `
    <button class=""activity-item ${{activityFilter.value === name ? 'active' : ''}}"" type=""button"" data-activity=""${{escapeHtml(name)}}"">
      <span class=""activity-name"">${{escapeHtml(name)}}</span>
      <span class=""activity-count"">${{value}} events</span>
    </button>`
  ).join('');
  document.querySelectorAll('#activityIndex button').forEach(btn => btn.addEventListener('click', () => {{
    activityFilter.value = btn.dataset.activity || '';
    renderRows();
  }}));
}}

function renderRows() {{
  const term = document.getElementById('search').value.toLowerCase();
  const artifact = artifactFilter.value;
  const activity = activityFilter.value;
  const browser = browserFilter.value;
  const baseForActivityIndex = rows.filter(r => {{
    const blob = [r.time,r.browser,r.artifact_type,r.artifact,r.activity,r.title,r.url,r.detail,r.source].join(' ').toLowerCase();
    return (!term || blob.includes(term))
      && (!artifact || r.artifact === artifact)
      && (!browser || r.browser === browser)
      && (!activeDrill.day || (r._day === activeDrill.day && (activeDrill.hour === null || r._hour === activeDrill.hour)));
  }});
  const filtered = rows.filter(r => {{
    const blob = [r.time,r.browser,r.artifact_type,r.artifact,r.activity,r.title,r.url,r.detail,r.source].join(' ').toLowerCase();
    return (!term || blob.includes(term))
      && (!artifact || r.artifact === artifact)
      && (!activity || r.activity === activity)
      && (!browser || r.browser === browser)
      && (!activeDrill.day || (r._day === activeDrill.day && (activeDrill.hour === null || r._hour === activeDrill.hour)));
  }});
  renderFocus(filtered);
  renderActivityIndex(baseForActivityIndex);

  const list = document.getElementById('eventList');
  list.innerHTML = filtered.slice(0,500).map(r => `
    <article class=""event"" data-index=""${{r._index}}"" style=""border-left-color:${{colorFor(r.artifact)}}"">
      <div class=""time"">${{escapeHtml(formatDate(r._date) || r.time || 'No timestamp')}}</div>
      <div class=""title"">${{escapeHtml(r.title || r.url || r.detail || '(untitled artifact)')}}</div>
      <div class=""meta"">${{escapeHtml(r.artifact_type || 'Unknown artifact type')}} / ${{escapeHtml(r.activity || 'Unknown activity')}} / ${{escapeHtml(r.browser || 'Unknown browser')}}</div>
      <div class=""url"">${{escapeHtml(r.url || r.detail || '')}}</div>
      ${{r.media_href ? `<span class=""cache-pill"">Exported cache media: ${{escapeHtml(r.media_type || r.media_kind || 'file')}}</span>` : ''}}
    </article>`).join('');

  const body = document.getElementById('tableBody');
  body.innerHTML = filtered.map(r => `
    <tr class=""row-clickable"" data-index=""${{r._index}}"">
      <td>${{escapeHtml(r.id)}}</td>
      <td>${{escapeHtml(r.artifact_type)}}</td>
      <td>${{escapeHtml(r.activity)}}</td>
      <td>${{escapeHtml(formatDate(r._date) || r.time || '')}}</td>
      <td>${{escapeHtml(r.browser)}}</td>
      <td>${{escapeHtml(r.title)}}</td>
      <td>${{escapeHtml(r.url || r.detail)}}${{r.media_href ? `<br><a class=""cache-link"" href=""${{escapeHtml(r.media_href)}}"" target=""_blank"" rel=""noopener"">Open exported cache media</a>` : ''}}</td>
      <td>${{escapeHtml(r.source)}}</td>
    </tr>`).join('');

  document.querySelectorAll('[data-index]').forEach(node => node.addEventListener('click', () => {{
    const row = rows.find(r => r._index === Number(node.dataset.index));
    if (row) openDetail(row);
  }}));
  buildCharts(filtered);
}}

function colorFor(value) {{
  const palette = ['#2764a8','#2f855a','#b7791f','#805ad5','#c53030','#00838f','#6b46c1','#4a5568'];
  let hash = 0;
  String(value || '').split('').forEach(ch => hash = ((hash << 5) - hash) + ch.charCodeAt(0));
  return palette[Math.abs(hash) % palette.length];
}}

document.getElementById('search').addEventListener('input', renderRows);
artifactFilter.addEventListener('change', renderRows);
activityFilter.addEventListener('change', renderRows);
browserFilter.addEventListener('change', renderRows);
activitySearch.addEventListener('input', () => renderRows());
document.getElementById('clearDrill').addEventListener('click', () => {{
  document.getElementById('search').value = '';
  activitySearch.value = '';
  artifactFilter.value = '';
  activityFilter.value = '';
  browserFilter.value = '';
  activeDrill.day = '';
  activeDrill.hour = null;
  renderRows();
}});
document.getElementById('closeDetail').addEventListener('click', closeDetail);
document.getElementById('detailBackdrop').addEventListener('click', closeDetail);
window.addEventListener('resize', () => Object.values(charts).forEach(c => c.resize()));
renderRows();
</script>
</body>
</html>";
        }

        private List<Dictionary<string, string>> ExtractTimelineRows(SfDataGrid grid, string outputHtmlPath)
        {
            var rows = new List<Dictionary<string, string>>();
            if (grid.DataSource is DataTable table)
            {
                foreach (System.Data.DataRow row in table.Rows)
                {
                    rows.Add(BuildTimelineRow(row, outputHtmlPath));
                }

                return rows;
            }

            if (grid.DataSource is DataView view)
            {
                foreach (DataRowView row in view)
                {
                    rows.Add(BuildTimelineRow(row, outputHtmlPath));
                }

                return rows;
            }

            if (grid.View?.Records == null)
            {
                return rows;
            }

            foreach (var rec in grid.View.Records)
            {
                rows.Add(BuildTimelineRow(rec.Data, outputHtmlPath));
            }

            return rows;
        }

        private Dictionary<string, string> BuildTimelineRow(object data, string outputHtmlPath)
        {
            string id = FirstValue(data, "id", "Id", "ID");
            string artifactType = FirstValue(data, "Artifact_type", "Artifact Type");
            string artifact = FirstValue(data, "Artifact");
            string browser = FirstValue(data, "Browser");
            string activity = FirstValue(data, "Potential_activity", "Potential Activity", "Activity", "Category");
            string time = NormalizeTimelineTime(FirstValue(data, "Activity_time", "Visit_time", "LastAccessed", "Created", "Modified", "End_time", "Start_time", "DateAdded", "LastModified", "LastUsed", "FirstUsed", "InstallTime", "LastUpdateTime"));
            string title = FirstValue(data, "Title", "Name", "FieldName", "DetectedFileType", "ContentType", "Current_path");
            string url = FirstValue(data, "Url", "URL", "Host", "Source_url", "Site_url", "Tab_url", "HomepageUrl", "Value", "Current_path");
            string detail = FirstValue(data, "Detail", "Category", "Path", "CacheKey", "CacheFile", "SourceType", "Description", "State", "Mime_type", "File");
            string source = FirstValue(data, "Source_table", "Artifact", "File");

            if (string.IsNullOrWhiteSpace(artifact))
            {
                artifact = InferArtifactFromColumns(data);
            }

            if (string.IsNullOrWhiteSpace(activity))
            {
                activity = InferPotentialActivity(artifact);
            }

            var row = new Dictionary<string, string>
            {
                ["id"] = id,
                ["time"] = time,
                ["browser"] = browser,
                ["artifact_type"] = artifactType,
                ["artifact"] = artifact,
                ["activity"] = activity,
                ["title"] = title,
                ["url"] = url,
                ["detail"] = detail,
                ["source"] = source
            };

            AttachTimelineCacheAsset(data, row, outputHtmlPath);
            return row;
        }

        private void AttachTimelineCacheAsset(object data, Dictionary<string, string> row, string outputHtmlPath)
        {
            string sourceTable = FirstValue(data, "Source_table");
            bool isCache = string.Equals(sourceTable, "cache_data", StringComparison.OrdinalIgnoreCase)
                || string.Equals(row.GetValueOrDefault("artifact"), "Cache", StringComparison.OrdinalIgnoreCase);
            if (!isCache || string.IsNullOrWhiteSpace(outputHtmlPath) || string.IsNullOrWhiteSpace(Helpers.chromeViewerConnectionString))
            {
                return;
            }

            string idText = FirstValue(data, "Source_id", "id", "Id", "ID");
            if (!int.TryParse(idText, out int cacheId))
            {
                return;
            }

            TimelineCacheAsset? asset = TryExportTimelineCacheAsset(cacheId, outputHtmlPath);
            if (asset == null)
            {
                return;
            }

            row["media_href"] = asset.RelativePath;
            row["media_kind"] = asset.Kind;
            row["media_type"] = asset.DisplayType;
            row["media_name"] = asset.FileName;
        }

        private TimelineCacheAsset? TryExportTimelineCacheAsset(int cacheId, string outputHtmlPath)
        {
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(Helpers.chromeViewerConnectionString))
                {
                    connection.Open();
                    using (SQLiteCommand command = new SQLiteCommand(@"
                        SELECT Body, BodyStored, DetectedFileType, DetectedExtension, ContentType, BodySha256
                        FROM cache_data
                        WHERE id = @id;", connection))
                    {
                        command.Parameters.AddWithValue("@id", cacheId);
                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                return null;
                            }

                            int bodyStored = reader["BodyStored"] == DBNull.Value ? 0 : Convert.ToInt32(reader["BodyStored"]);
                            if (bodyStored != 1 || reader["Body"] == DBNull.Value)
                            {
                                return null;
                            }

                            byte[] body = (byte[])reader["Body"];
                            if (body.Length == 0)
                            {
                                return null;
                            }

                            string detectedType = reader["DetectedFileType"]?.ToString() ?? string.Empty;
                            string extension = reader["DetectedExtension"]?.ToString() ?? string.Empty;
                            string contentType = reader["ContentType"]?.ToString() ?? string.Empty;
                            string kind = GetTimelineMediaKind(detectedType, extension, contentType);
                            if (string.IsNullOrWhiteSpace(kind))
                            {
                                return null;
                            }

                            extension = NormalizeTimelineMediaExtension(extension, detectedType, contentType);
                            string sha = reader["BodySha256"]?.ToString() ?? string.Empty;
                            string shortSha = string.IsNullOrWhiteSpace(sha) ? "nosha" : Regex.Replace(sha, @"[^a-zA-Z0-9]", "").Substring(0, Math.Min(12, Regex.Replace(sha, @"[^a-zA-Z0-9]", "").Length));
                            string assetsDir = GetTimelineCacheAssetsDirectory(outputHtmlPath);
                            Directory.CreateDirectory(assetsDir);

                            string fileName = $"cache_{cacheId}_{shortSha}{extension}";
                            string assetPath = Path.Combine(assetsDir, fileName);
                            if (!File.Exists(assetPath))
                            {
                                File.WriteAllBytes(assetPath, body);
                            }

                            string htmlDir = Path.GetDirectoryName(outputHtmlPath) ?? Environment.CurrentDirectory;
                            string relativePath = Path.GetRelativePath(htmlDir, assetPath).Replace('\\', '/');
                            return new TimelineCacheAsset
                            {
                                RelativePath = relativePath,
                                Kind = kind,
                                DisplayType = string.IsNullOrWhiteSpace(detectedType) ? contentType : detectedType,
                                FileName = fileName
                            };
                        }
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        private string GetTimelineCacheAssetsDirectory(string outputHtmlPath)
        {
            string htmlDir = Path.GetDirectoryName(outputHtmlPath) ?? Environment.CurrentDirectory;
            string reportName = Path.GetFileNameWithoutExtension(outputHtmlPath);
            string safeReportName = Regex.Replace(reportName, @"[^a-zA-Z0-9_\-]+", "_").Trim('_');
            if (string.IsNullOrWhiteSpace(safeReportName))
            {
                safeReportName = "Browser_Reviewer_TimeLine";
            }

            return Path.Combine(htmlDir, $"{safeReportName}_assets", "cache");
        }

        private string GetTimelineMediaKind(string detectedType, string extension, string contentType)
        {
            string text = $"{detectedType} {extension} {contentType}".ToLowerInvariant();
            if (text.Contains("jpeg") || text.Contains("jpg") || text.Contains("png") || text.Contains("gif") || text.Contains("webp") || text.Contains("bmp") || text.Contains("image/")) return "image";
            if (text.Contains("mp4") || text.Contains("quicktime") || text.Contains("webm") || text.Contains("ogg video") || text.Contains("ogv") || text.Contains("avi") || text.Contains("video/")) return "video";
            if (text.Contains("mp3") || text.Contains("wav") || text.Contains("ogg audio") || text.Contains("audio/")) return "audio";
            if (text.Contains("pdf")) return "pdf";
            return string.Empty;
        }

        private string NormalizeTimelineMediaExtension(string extension, string detectedType, string contentType)
        {
            string ext = extension?.Trim().ToLowerInvariant() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(ext) && !ext.StartsWith(".")) ext = "." + ext;
            if (!string.IsNullOrWhiteSpace(ext) && Regex.IsMatch(ext, @"^\.[a-z0-9]{1,8}$")) return ext;

            string text = $"{detectedType} {contentType}".ToLowerInvariant();
            if (text.Contains("jpeg") || text.Contains("jpg")) return ".jpg";
            if (text.Contains("png")) return ".png";
            if (text.Contains("gif")) return ".gif";
            if (text.Contains("webp")) return ".webp";
            if (text.Contains("bmp")) return ".bmp";
            if (text.Contains("quicktime")) return ".mov";
            if (text.Contains("webm")) return ".webm";
            if (text.Contains("ogg video")) return ".ogv";
            if (text.Contains("mp4") || text.Contains("video/")) return ".mp4";
            if (text.Contains("wav")) return ".wav";
            if (text.Contains("ogg audio")) return ".ogg";
            if (text.Contains("mp3") || text.Contains("audio/")) return ".mp3";
            if (text.Contains("pdf")) return ".pdf";
            return ".bin";
        }

        private class TimelineCacheAsset
        {
            public string RelativePath { get; set; } = string.Empty;
            public string Kind { get; set; } = string.Empty;
            public string DisplayType { get; set; } = string.Empty;
            public string FileName { get; set; } = string.Empty;
        }

        private string NormalizeTimelineTime(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            if (DateTime.TryParse(value, out DateTime parsed))
                return parsed.ToString("yyyy-MM-dd HH:mm:ss.fff");

            Match match = Regex.Match(value, @"^(?<prefix>\d{4}-\d{2}-\d{2}[ T]\d{2}:\d{2}:\d{2})(?<fraction>\.\d+)?");
            if (!match.Success)
                return value;

            string fraction = match.Groups["fraction"].Success
                ? match.Groups["fraction"].Value.TrimEnd('0')
                : string.Empty;

            if (fraction.Length > 4)
                fraction = fraction.Substring(0, 4);

            return match.Groups["prefix"].Value.Replace('T', ' ') + fraction;
        }

        private string FirstValue(object data, params string[] names)
        {
            foreach (string name in names)
            {
                object? value = GetPropertyValue(data, name);
                string text = value?.ToString() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }
            }

            return string.Empty;
        }

        private string InferArtifactFromColumns(object data)
        {
            string sourceTable = FirstValue(data, "Source_table");
            if (string.Equals(sourceTable, "cache_data", StringComparison.OrdinalIgnoreCase)) return "Cache";
            if (string.Equals(sourceTable, "cookies_data", StringComparison.OrdinalIgnoreCase)) return "Cookie";
            if (string.Equals(sourceTable, "session_data", StringComparison.OrdinalIgnoreCase)) return "Session";
            if (string.Equals(sourceTable, "extension_data", StringComparison.OrdinalIgnoreCase)) return "Extension";
            if (string.Equals(sourceTable, "saved_logins_data", StringComparison.OrdinalIgnoreCase)) return "Saved Login";
            if (string.Equals(sourceTable, "local_storage_data", StringComparison.OrdinalIgnoreCase)) return "Local Storage";
            if (string.Equals(sourceTable, "autofill_data", StringComparison.OrdinalIgnoreCase)) return "Autofill";
            if (sourceTable.Contains("download", StringComparison.OrdinalIgnoreCase)) return "Download";
            if (sourceTable.Contains("bookmark", StringComparison.OrdinalIgnoreCase)) return "Bookmark";
            if (sourceTable.Contains("result", StringComparison.OrdinalIgnoreCase)) return "History";

            if (!string.IsNullOrWhiteSpace(FirstValue(data, "Visit_id", "Place_id"))) return "History";
            if (!string.IsNullOrWhiteSpace(FirstValue(data, "Download_id", "Received_bytes", "Total_bytes"))) return "Download";
            if (!string.IsNullOrWhiteSpace(FirstValue(data, "Bookmark_id", "ChromeId", "Parent_name"))) return "Bookmark";
            if (!string.IsNullOrWhiteSpace(FirstValue(data, "FieldName", "TimesUsed", "FirstUsed"))) return "Autofill";
            if (!string.IsNullOrWhiteSpace(FirstValue(data, "CacheKey", "CacheFile", "BodySha256"))) return "Cache";
            if (!string.IsNullOrWhiteSpace(FirstValue(data, "Host", "IsHttpOnly", "SameSite"))) return "Cookie";
            if (!string.IsNullOrWhiteSpace(FirstValue(data, "SessionFile", "WindowIndex", "TabIndex"))) return "Session";
            if (!string.IsNullOrWhiteSpace(FirstValue(data, "ExtensionId", "ManifestVersion", "Permissions"))) return "Extension";
            if (!string.IsNullOrWhiteSpace(FirstValue(data, "Signon_realm", "Username_field", "Password_field", "Login_guid"))) return "Saved Login";
            if (!string.IsNullOrWhiteSpace(FirstValue(data, "Storage_key", "Value_sha256", "Source_kind"))) return "Local Storage";
            return "Web Artifact";
        }

        private string InferPotentialActivity(string artifact)
        {
            return artifact switch
            {
                "History" => "Browsing web page",
                "Download" => "Downloading file",
                "Bookmark" => "Saving bookmark",
                "Autofill" => "Autofill data present",
                "Cookie" => "Storing web cookie",
                "Cache" => "Caching web content",
                "Session" => "Restoring browser session",
                "Extension" => "Browser extension present",
                "Saved Login" => "Saved login metadata present",
                "Local Storage" => "Using web application local storage",
                _ => "Web activity"
            };
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

            var visibleCols = grid.Columns.Where(c => c.Visible).ToList();
            int rowCount = grid.View?.Records?.Count ?? 0;
            string sortInfo = GetSortSummary(grid);
            string filtInfo = GetFilterSummary(grid);

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
        thead tr.filters input.column-filter{
          width:100%;
          box-sizing:border-box;
          padding:5px 7px;
          font-size:12px;
          border:1px solid #d8d8e0;
          border-radius:4px;
          background:#fff;
          color:#222;
        }
        .filter-menu{
          position:absolute;
          top:calc(100% - 1px);
          left:0;
          width:max(100%,260px);
          max-width:520px;
          max-height:260px;
          resize:both;
          overflow:auto;
          box-sizing:border-box;
          background:#fff;
          border:1px solid #cfd4df;
          border-radius:5px;
          box-shadow:0 8px 20px rgba(0,0,0,.16);
          padding:4px;
          display:none;
          z-index:50;
        }
        .filter-option{
          display:block;
          width:100%;
          text-align:left;
          border:0;
          border-radius:4px;
          background:#fff;
          color:#222;
          padding:5px 7px;
          font-size:12px;
          white-space:nowrap;
          overflow:hidden;
          text-overflow:ellipsis;
          cursor:pointer;
        }
        .filter-option:hover,
        .filter-option:focus{
          background:#eef4ff;
          outline:none;
        }
        .filter-empty-note{
          padding:6px 7px;
          color:#666;
          font-size:12px;
        }

        /* Indicadores de orden */
        thead tr.headers th.sort-asc::after{content:'?'; position:absolute; right:8px; color:#666; font-size:10px;}
        thead tr.headers th.sort-desc::after{content:'?'; position:absolute; right:8px; color:#666; font-size:10px;}

        tr:nth-child(even){background:#fbfbfb;}
        .box{border:1px solid #d0d0d0;background:#fff;border-radius:4px;padding:6px;overflow-wrap:anywhere;}
        a{color:#0067c0;text-decoration:none;} a:hover{text-decoration:underline;}
        ");
            sb.AppendLine("</style></head><body>");

            sb.AppendLine($"<div class='title'>{WebUtility.HtmlEncode(titulo)}</div>");

            sb.AppendLine("<div class='summary'>");
            sb.AppendLine("<div><b>Rows exported:</b> " + rowCount + "</div>");
            sb.AppendLine("<div><b>Visible columns:</b> " + visibleCols.Count + "</div>");
            sb.AppendLine("<div><b>Sort:</b> " + WebUtility.HtmlEncode(sortInfo) + "</div>");
            sb.AppendLine("<div><b>Filters:</b> " + WebUtility.HtmlEncode(filtInfo) + "</div>");
            int offset = Helpers.utcOffset;
            string tzStr = $"UTC{(offset >= 0 ? "+" : "")}{offset}";


            sb.AppendLine($"<div><b>Time zone:</b> {WebUtility.HtmlEncode(tzStr)}</div>");
            sb.AppendLine("<div style='margin-top:4px;color:#666;font-size:12px;'>Exported at " +
                          DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "</div>");

            sb.AppendLine("</div>");

            sb.AppendLine("<div class='table-wrap'>");
            sb.AppendLine("<table>");
            sb.AppendLine("<thead>");

            sb.AppendLine("<tr class='headers'>");
            foreach (var col in visibleCols)
            {
                var header = (col.HeaderText ?? col.MappingName ?? "Column");
                sb.AppendLine("<th>" + WebUtility.HtmlEncode(header) + "</th>");
            }
            sb.AppendLine("</tr>");

            sb.AppendLine("<tr class='filters'>");
            foreach (var _ in visibleCols)
                sb.AppendLine("<th></th>");
            sb.AppendLine("</tr>");

            sb.AppendLine("</thead>");

            sb.AppendLine("<tbody>");
            if (grid.View?.Records == null)
            {
                sb.AppendLine("</tbody></table></div></body></html>");
                return sb.ToString();
            }

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
                        sb.AppendLine("<td data-filter-value='" + WebUtility.HtmlEncode(raw) + "'><div class='box'><a href='" + WebUtility.HtmlEncode(raw) +
                                      "' target='_blank' title='" + WebUtility.HtmlEncode(raw) + "'>" +
                                      WebUtility.HtmlEncode(shortText) + "</a></div></td>");
                    }
                    else
                    {
                        sb.AppendLine("<td data-filter-value='" + encoded + "'><div class='box'>" + encoded + "</div></td>");
                    }
                }

                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</tbody></table>");
            sb.AppendLine("</div>");

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
            function getCellFilterText(cell){
              if (!cell) return '';
              return (cell.getAttribute('data-filter-value') || cell.innerText || '').trim();
            }

            function applyFilters(){
              var inputs = Array.from(filterRow.querySelectorAll('input.column-filter'));
              allRows.forEach(function(tr){
                var tds = tr.children, visible = true;
                for (var i=0; i<inputs.length; i++){
                  var input = inputs[i];
                  var val = input ? input.value.trim().toLowerCase() : '';
                  if(!val) continue;
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

            function closeFilterMenus(except){
              filterRow.querySelectorAll('.filter-menu').forEach(function(menu){
                if (menu !== except) menu.style.display = 'none';
              });
            }

            document.addEventListener('click', function(ev){
              if (!filterRow.contains(ev.target)) closeFilterMenus();
            });

            function renderFilterMenu(input, menu, values, hasEmpty){
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

            for (var colIndex = 0; colIndex < filterRow.cells.length; colIndex++){
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
              var dataList = menu;

              var setVals = new Set();
              var hasEmpty = false;
              allRows.forEach(function(tr){
                var text = getCellFilterText(tr.children[colIndex]);
                if (text === '') hasEmpty = true; else setVals.add(text);
              });

              var vals = Array.from(setVals).sort(function(a,b){
                return a.localeCompare(b, undefined, { sensitivity:'base', numeric:true });
              });

              if (hasEmpty){
                var optEmpty = document.createElement('option');
                optEmpty.value = '(empty)';
                optEmpty.textContent = '(Vacíos)';
                dataList.appendChild(optEmpty);
              }

              vals.forEach(function(v){
                var opt = document.createElement('option');
                opt.value = v;
                opt.textContent = v;
                dataList.appendChild(opt);
              });

              input.addEventListener('input', applyFilters);
              input.addEventListener('change', applyFilters);
              (function(input, menu, vals, hasEmpty){
                input.addEventListener('focus', function(){
                  closeFilterMenus(menu);
                  renderFilterMenu(input, menu, vals, hasEmpty);
                });
                input.addEventListener('input', function(){
                  renderFilterMenu(input, menu, vals, hasEmpty);
                });
                input.addEventListener('keydown', function(ev){
                  if (ev.key === 'Escape') closeFilterMenus();
                });
              })(input, menu, vals, hasEmpty);
            }
          }

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

            sb.AppendLine("</body></html>");

            string finalHtml = sb.ToString();
            if (Helpers.searchTermExists)
                finalHtml = HighlightSearchTerms(finalHtml, Helpers.searchTerm, Helpers.searchTermRegExp);

            return finalHtml;
        }






























































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
            if (grid.DataSource is BindingSource bs && !string.IsNullOrWhiteSpace(bs.Filter))
                return bs.Filter;

            if (grid.DataSource is DataView dv && !string.IsNullOrWhiteSpace(dv.RowFilter))
                return dv.RowFilter;

            if (grid.DataSource is DataTable dt && !string.IsNullOrWhiteSpace(dt.DefaultView?.RowFilter))
                return dt.DefaultView.RowFilter;

            try
            {
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

            if (data is DataRowView drv)
                return drv.Row.Table.Columns.Contains(mappingName) ? drv[mappingName] : null;

            if (data is System.Data.DataRow dr)
                return dr.Table.Columns.Contains(mappingName) ? dr[mappingName] : null;

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
            string key = (columnKey ?? string.Empty).Trim();

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

            if (!iconMap.TryGetValue(key, out Image? img) || img == null)
                img = Resource1.generic_icon;

            iconBase64 = ImageToBase64(img);

            if (!prettyMap.TryGetValue(key, out string? pretty))
                pretty = columnKey;

            return pretty ?? string.Empty;
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
            Form? openForm = Application.OpenForms.OfType<Form_Report>().FirstOrDefault();

            if (openForm != null)
            {
                openForm.BringToFront();
            }
            else
            {
                Form_Report frmReports = new Form_Report();
                frmReports.Show();
            }
        }

        private void groupBox_Main_Enter(object sender, EventArgs e)
        {

        }
    }

}









