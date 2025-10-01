namespace Browser_Reviewer
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            menuStrip1 = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            newToolStripMenuItem = new ToolStripMenuItem();
            openToolStripMenuItem = new ToolStripMenuItem();
            cToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            exitToolStripMenuItem = new ToolStripMenuItem();
            button_SearchWebActivity = new Button();
            label_UTC_Time = new Label();
            numericUpDown1 = new NumericUpDown();
            search_textBox = new TextBox();
            searchBtn = new Button();
            groupBox_Main = new GroupBox();
            sfDataGrid1 = new Syncfusion.WinForms.DataGrid.SfDataGrid();
            labelItemCount = new Label();
            labelStatus = new Label();
            clearsearchBtn = new Button();
            groupBox_customSearch = new GroupBox();
            button_LabelManager = new Button();
            label_endDate = new Label();
            label_startDate = new Label();
            checkBox_enableTime = new CheckBox();
            dateTimePicker_end = new DateTimePicker();
            dateTimePicker_start = new DateTimePicker();
            checkBox_RegExp = new CheckBox();
            richTextBox1 = new RichTextBox();
            button_exportPDF = new Button();
            autoLabel1 = new Syncfusion.Windows.Forms.Tools.AutoLabel();
            groupBox_TextBox = new GroupBox();
            button_Report = new Button();
            button_Font = new Button();
            button_exportHTML = new Button();
            Console = new RichTextBox();
            menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDown1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)sfDataGrid1).BeginInit();
            groupBox_customSearch.SuspendLayout();
            groupBox_TextBox.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(1769, 24);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            menuStrip1.ItemClicked += menuStrip1_ItemClicked;
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { newToolStripMenuItem, openToolStripMenuItem, cToolStripMenuItem, toolStripSeparator1, exitToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(37, 20);
            fileToolStripMenuItem.Text = "File";
            // 
            // newToolStripMenuItem
            // 
            newToolStripMenuItem.Name = "newToolStripMenuItem";
            newToolStripMenuItem.Size = new Size(103, 22);
            newToolStripMenuItem.Text = "New";
            newToolStripMenuItem.Click += newToolStripMenuItem_Click;
            // 
            // openToolStripMenuItem
            // 
            openToolStripMenuItem.Name = "openToolStripMenuItem";
            openToolStripMenuItem.Size = new Size(103, 22);
            openToolStripMenuItem.Text = "Open";
            openToolStripMenuItem.Click += openToolStripMenuItem_Click_1;
            // 
            // cToolStripMenuItem
            // 
            cToolStripMenuItem.Name = "cToolStripMenuItem";
            cToolStripMenuItem.Size = new Size(103, 22);
            cToolStripMenuItem.Text = "Close";
            cToolStripMenuItem.Click += cToolStripMenuItem_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(100, 6);
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(103, 22);
            exitToolStripMenuItem.Text = "Exit";
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click_1;
            // 
            // button_SearchWebActivity
            // 
            button_SearchWebActivity.Enabled = false;
            button_SearchWebActivity.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            button_SearchWebActivity.Image = Resources.Resource1.Internet_32;
            button_SearchWebActivity.ImageAlign = ContentAlignment.TopCenter;
            button_SearchWebActivity.Location = new Point(129, 9);
            button_SearchWebActivity.Name = "button_SearchWebActivity";
            button_SearchWebActivity.Size = new Size(117, 50);
            button_SearchWebActivity.TabIndex = 1;
            button_SearchWebActivity.Text = "Scan Web Activity";
            button_SearchWebActivity.TextAlign = ContentAlignment.BottomCenter;
            button_SearchWebActivity.UseVisualStyleBackColor = true;
            button_SearchWebActivity.Click += button_SearchWebActivity_Click;
            // 
            // label_UTC_Time
            // 
            label_UTC_Time.AutoSize = true;
            label_UTC_Time.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label_UTC_Time.ForeColor = SystemColors.ActiveCaptionText;
            label_UTC_Time.Location = new Point(245, 12);
            label_UTC_Time.Name = "label_UTC_Time";
            label_UTC_Time.Size = new Size(100, 17);
            label_UTC_Time.TabIndex = 3;
            label_UTC_Time.Text = "UTC Time + / -";
            label_UTC_Time.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // numericUpDown1
            // 
            numericUpDown1.Location = new Point(252, 38);
            numericUpDown1.Name = "numericUpDown1";
            numericUpDown1.Size = new Size(88, 23);
            numericUpDown1.TabIndex = 4;
            // 
            // search_textBox
            // 
            search_textBox.Location = new Point(1409, 46);
            search_textBox.Name = "search_textBox";
            search_textBox.Size = new Size(214, 23);
            search_textBox.TabIndex = 5;
            search_textBox.TextChanged += search_textBox_TextChanged;
            search_textBox.KeyDown += search_textBox_KeyDown;
            // 
            // searchBtn
            // 
            searchBtn.Location = new Point(1629, 47);
            searchBtn.Name = "searchBtn";
            searchBtn.Size = new Size(51, 23);
            searchBtn.TabIndex = 6;
            searchBtn.Text = "Search";
            searchBtn.UseVisualStyleBackColor = true;
            searchBtn.Click += searchBtn_Click_1;
            // 
            // groupBox_Main
            // 
            groupBox_Main.Location = new Point(5, 101);
            groupBox_Main.Name = "groupBox_Main";
            groupBox_Main.Size = new Size(339, 709);
            groupBox_Main.TabIndex = 7;
            groupBox_Main.TabStop = false;
            // 
            // sfDataGrid1
            // 
            sfDataGrid1.AccessibleName = "Table";
            sfDataGrid1.AllowDraggingColumns = true;
            sfDataGrid1.AllowEditing = false;
            sfDataGrid1.AllowFiltering = true;
            sfDataGrid1.AllowResizingColumns = true;
            sfDataGrid1.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            sfDataGrid1.Location = new Point(350, 109);
            sfDataGrid1.Name = "sfDataGrid1";
            sfDataGrid1.NavigationMode = Syncfusion.WinForms.DataGrid.Enums.NavigationMode.Row;
            sfDataGrid1.SelectionMode = Syncfusion.WinForms.DataGrid.Enums.GridSelectionMode.Extended;
            sfDataGrid1.ShowToolTip = true;
            sfDataGrid1.Size = new Size(1058, 695);
            sfDataGrid1.Style.BorderColor = Color.FromArgb(100, 100, 100);
            sfDataGrid1.Style.CheckBoxStyle.CheckedBackColor = Color.FromArgb(0, 120, 215);
            sfDataGrid1.Style.CheckBoxStyle.CheckedBorderColor = Color.FromArgb(0, 120, 215);
            sfDataGrid1.Style.CheckBoxStyle.IndeterminateBorderColor = Color.FromArgb(0, 120, 215);
            sfDataGrid1.Style.HyperlinkStyle.DefaultLinkColor = Color.FromArgb(0, 120, 215);
            sfDataGrid1.TabIndex = 8;
            sfDataGrid1.Text = "sfDataGrid1";
            // 
            // labelItemCount
            // 
            labelItemCount.AutoSize = true;
            labelItemCount.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            labelItemCount.Location = new Point(351, 46);
            labelItemCount.Name = "labelItemCount";
            labelItemCount.Size = new Size(74, 17);
            labelItemCount.TabIndex = 9;
            labelItemCount.Text = "Item Count:";
            // 
            // labelStatus
            // 
            labelStatus.AutoSize = true;
            labelStatus.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            labelStatus.Location = new Point(351, 12);
            labelStatus.Name = "labelStatus";
            labelStatus.Size = new Size(69, 21);
            labelStatus.TabIndex = 10;
            labelStatus.Text = "Status...";
            // 
            // clearsearchBtn
            // 
            clearsearchBtn.Location = new Point(1686, 46);
            clearsearchBtn.Name = "clearsearchBtn";
            clearsearchBtn.Size = new Size(59, 23);
            clearsearchBtn.TabIndex = 13;
            clearsearchBtn.Text = "Clear";
            clearsearchBtn.UseVisualStyleBackColor = true;
            clearsearchBtn.Click += clearsearchBtn_Click;
            // 
            // groupBox_customSearch
            // 
            groupBox_customSearch.Controls.Add(button_LabelManager);
            groupBox_customSearch.Controls.Add(button_SearchWebActivity);
            groupBox_customSearch.Controls.Add(numericUpDown1);
            groupBox_customSearch.Controls.Add(label_endDate);
            groupBox_customSearch.Controls.Add(label_startDate);
            groupBox_customSearch.Controls.Add(label_UTC_Time);
            groupBox_customSearch.Controls.Add(checkBox_enableTime);
            groupBox_customSearch.Controls.Add(dateTimePicker_end);
            groupBox_customSearch.Controls.Add(labelItemCount);
            groupBox_customSearch.Controls.Add(dateTimePicker_start);
            groupBox_customSearch.Controls.Add(checkBox_RegExp);
            groupBox_customSearch.Controls.Add(labelStatus);
            groupBox_customSearch.Controls.Add(search_textBox);
            groupBox_customSearch.Controls.Add(clearsearchBtn);
            groupBox_customSearch.Controls.Add(searchBtn);
            groupBox_customSearch.Enabled = false;
            groupBox_customSearch.Location = new Point(5, 27);
            groupBox_customSearch.Name = "groupBox_customSearch";
            groupBox_customSearch.Size = new Size(1752, 76);
            groupBox_customSearch.TabIndex = 14;
            groupBox_customSearch.TabStop = false;
            // 
            // button_LabelManager
            // 
            button_LabelManager.Enabled = false;
            button_LabelManager.Image = Resources.Resource1.Label_32;
            button_LabelManager.ImageAlign = ContentAlignment.TopCenter;
            button_LabelManager.Location = new Point(6, 9);
            button_LabelManager.Name = "button_LabelManager";
            button_LabelManager.Size = new Size(117, 50);
            button_LabelManager.TabIndex = 15;
            button_LabelManager.Text = "Label Manager";
            button_LabelManager.TextAlign = ContentAlignment.BottomCenter;
            button_LabelManager.UseVisualStyleBackColor = true;
            button_LabelManager.Click += button_LabelManager_Click;
            // 
            // label_endDate
            // 
            label_endDate.AutoSize = true;
            label_endDate.Location = new Point(1116, 47);
            label_endDate.Name = "label_endDate";
            label_endDate.Size = new Size(54, 15);
            label_endDate.TabIndex = 20;
            label_endDate.Text = "End Date";
            // 
            // label_startDate
            // 
            label_startDate.AutoSize = true;
            label_startDate.Location = new Point(1116, 22);
            label_startDate.Name = "label_startDate";
            label_startDate.Size = new Size(58, 15);
            label_startDate.TabIndex = 19;
            label_startDate.Text = "Start Date";
            // 
            // checkBox_enableTime
            // 
            checkBox_enableTime.AutoSize = true;
            checkBox_enableTime.Location = new Point(1415, 21);
            checkBox_enableTime.Name = "checkBox_enableTime";
            checkBox_enableTime.Size = new Size(52, 19);
            checkBox_enableTime.TabIndex = 18;
            checkBox_enableTime.Text = "Time";
            checkBox_enableTime.UseVisualStyleBackColor = true;
            checkBox_enableTime.CheckedChanged += checkBox_enableTime_CheckedChanged;
            // 
            // dateTimePicker_end
            // 
            dateTimePicker_end.Enabled = false;
            dateTimePicker_end.Location = new Point(1194, 47);
            dateTimePicker_end.Name = "dateTimePicker_end";
            dateTimePicker_end.Size = new Size(209, 23);
            dateTimePicker_end.TabIndex = 17;
            dateTimePicker_end.ValueChanged += dateTimePicker_end_ValueChanged;
            // 
            // dateTimePicker_start
            // 
            dateTimePicker_start.Enabled = false;
            dateTimePicker_start.Location = new Point(1194, 19);
            dateTimePicker_start.Name = "dateTimePicker_start";
            dateTimePicker_start.Size = new Size(209, 23);
            dateTimePicker_start.TabIndex = 16;
            dateTimePicker_start.ValueChanged += dateTimePicker_start_ValueChanged_1;
            // 
            // checkBox_RegExp
            // 
            checkBox_RegExp.AutoSize = true;
            checkBox_RegExp.Location = new Point(1509, 20);
            checkBox_RegExp.Name = "checkBox_RegExp";
            checkBox_RegExp.Size = new Size(65, 19);
            checkBox_RegExp.TabIndex = 14;
            checkBox_RegExp.Text = "RegExp";
            checkBox_RegExp.UseVisualStyleBackColor = true;
            // 
            // richTextBox1
            // 
            richTextBox1.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            richTextBox1.Location = new Point(6, 22);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.Size = new Size(324, 573);
            richTextBox1.TabIndex = 15;
            richTextBox1.Text = "";
            // 
            // button_exportPDF
            // 
            button_exportPDF.Enabled = false;
            button_exportPDF.Location = new Point(100, 620);
            button_exportPDF.Name = "button_exportPDF";
            button_exportPDF.Size = new Size(78, 23);
            button_exportPDF.TabIndex = 17;
            button_exportPDF.Text = "Export PDF";
            button_exportPDF.UseVisualStyleBackColor = true;
            button_exportPDF.Click += button_exportPDF_Click;
            // 
            // autoLabel1
            // 
            autoLabel1.Location = new Point(84, 646);
            autoLabel1.Name = "autoLabel1";
            autoLabel1.Size = new Size(174, 15);
            autoLabel1.TabIndex = 18;
            autoLabel1.Text = "www.internet-solutions.com.co";
            autoLabel1.Click += autoLabel1_Click;
            // 
            // groupBox_TextBox
            // 
            groupBox_TextBox.Controls.Add(Console);
            groupBox_TextBox.Controls.Add(button_Report);
            groupBox_TextBox.Controls.Add(button_Font);
            groupBox_TextBox.Controls.Add(button_exportHTML);
            groupBox_TextBox.Controls.Add(button_exportPDF);
            groupBox_TextBox.Controls.Add(richTextBox1);
            groupBox_TextBox.Controls.Add(autoLabel1);
            groupBox_TextBox.Location = new Point(1414, 101);
            groupBox_TextBox.Name = "groupBox_TextBox";
            groupBox_TextBox.Size = new Size(336, 709);
            groupBox_TextBox.TabIndex = 19;
            groupBox_TextBox.TabStop = false;
            // 
            // button_Report
            // 
            button_Report.Enabled = false;
            button_Report.Location = new Point(265, 620);
            button_Report.Name = "button_Report";
            button_Report.Size = new Size(78, 23);
            button_Report.TabIndex = 21;
            button_Report.Text = "Report";
            button_Report.UseVisualStyleBackColor = true;
            button_Report.Click += button_Report_Click;
            // 
            // button_Font
            // 
            button_Font.Enabled = false;
            button_Font.Location = new Point(16, 620);
            button_Font.Name = "button_Font";
            button_Font.Size = new Size(78, 23);
            button_Font.TabIndex = 20;
            button_Font.Text = "Font";
            button_Font.UseVisualStyleBackColor = true;
            button_Font.Click += button_Font_Click;
            // 
            // button_exportHTML
            // 
            button_exportHTML.Enabled = false;
            button_exportHTML.Location = new Point(184, 620);
            button_exportHTML.Name = "button_exportHTML";
            button_exportHTML.Size = new Size(78, 23);
            button_exportHTML.TabIndex = 19;
            button_exportHTML.Text = "Export HTML";
            button_exportHTML.UseVisualStyleBackColor = true;
            button_exportHTML.Click += button_exportHTML_Click;
            // 
            // Console
            // 
            Console.Location = new Point(16, 664);
            Console.Name = "Console";
            Console.Size = new Size(314, 30);
            Console.TabIndex = 22;
            Console.Text = "";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1769, 822);
            Controls.Add(groupBox_TextBox);
            Controls.Add(groupBox_customSearch);
            Controls.Add(sfDataGrid1);
            Controls.Add(groupBox_Main);
            Controls.Add(menuStrip1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = menuStrip1;
            Name = "Form1";
            Text = "Browser Reviewer v0.2";
            Load += Form1_Load;
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDown1).EndInit();
            ((System.ComponentModel.ISupportInitialize)sfDataGrid1).EndInit();
            groupBox_customSearch.ResumeLayout(false);
            groupBox_customSearch.PerformLayout();
            groupBox_TextBox.ResumeLayout(false);
            groupBox_TextBox.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem newToolStripMenuItem;
        private ToolStripMenuItem openToolStripMenuItem;
        private ToolStripMenuItem cToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem exitToolStripMenuItem;
        private Button button_SearchWebActivity;
        private Label label_UTC_Time;
        private NumericUpDown numericUpDown1;
        private TextBox search_textBox;
        private Button searchBtn;
        private GroupBox groupBox_Main;
        private Syncfusion.WinForms.DataGrid.SfDataGrid sfDataGrid1;
        private Label labelItemCount;
        private Label labelStatus;
        private Button clearsearchBtn;
        private GroupBox groupBox_customSearch;
        private CheckBox checkBox_RegExp;
        private Button button_LabelManager;
        private DateTimePicker dateTimePicker_end;
        private DateTimePicker dateTimePicker_start;
        private CheckBox checkBox_enableTime;
        private Label label_endDate;
        private Label label_startDate;
        private RichTextBox richTextBox1;
        private Button button_exportPDF;
        private Syncfusion.Windows.Forms.Tools.AutoLabel autoLabel1;
        private GroupBox groupBox_TextBox;
        private Button button_exportHTML;
        private Button button_Font;
        private Button button_Report;
        private RichTextBox Console;
    }
}
