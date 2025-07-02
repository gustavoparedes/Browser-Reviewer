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
            Console = new TextBox();
            clearsearchBtn = new Button();
            groupBox_customSearch = new GroupBox();
            label_endDate = new Label();
            label_startDate = new Label();
            checkBox_enableTime = new CheckBox();
            dateTimePicker_end = new DateTimePicker();
            dateTimePicker_start = new DateTimePicker();
            checkBox_RegExp = new CheckBox();
            button_LabelManager = new Button();
            richTextBox1 = new RichTextBox();
            button_exportPDF = new Button();
            autoLabel1 = new Syncfusion.Windows.Forms.Tools.AutoLabel();
            menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDown1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)sfDataGrid1).BeginInit();
            groupBox_customSearch.SuspendLayout();
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
            button_SearchWebActivity.Location = new Point(0, 12);
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
            label_UTC_Time.Location = new Point(239, 11);
            label_UTC_Time.Name = "label_UTC_Time";
            label_UTC_Time.Size = new Size(100, 17);
            label_UTC_Time.TabIndex = 3;
            label_UTC_Time.Text = "UTC Time + / -";
            // 
            // numericUpDown1
            // 
            numericUpDown1.Location = new Point(246, 37);
            numericUpDown1.Name = "numericUpDown1";
            numericUpDown1.Size = new Size(88, 23);
            numericUpDown1.TabIndex = 4;
            // 
            // search_textBox
            // 
            search_textBox.Location = new Point(1409, 38);
            search_textBox.Name = "search_textBox";
            search_textBox.Size = new Size(214, 23);
            search_textBox.TabIndex = 5;
            search_textBox.TextChanged += search_textBox_TextChanged;
            search_textBox.KeyDown += search_textBox_KeyDown;
            // 
            // searchBtn
            // 
            searchBtn.Location = new Point(1629, 39);
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
            groupBox_Main.Size = new Size(334, 709);
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
            sfDataGrid1.Location = new Point(344, 109);
            sfDataGrid1.Name = "sfDataGrid1";
            sfDataGrid1.NavigationMode = Syncfusion.WinForms.DataGrid.Enums.NavigationMode.Row;
            sfDataGrid1.SelectionMode = Syncfusion.WinForms.DataGrid.Enums.GridSelectionMode.Extended;
            sfDataGrid1.ShowToolTip = true;
            sfDataGrid1.Size = new Size(1064, 656);
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
            labelItemCount.Location = new Point(345, 45);
            labelItemCount.Name = "labelItemCount";
            labelItemCount.Size = new Size(74, 17);
            labelItemCount.TabIndex = 9;
            labelItemCount.Text = "Item Count:";
            // 
            // labelStatus
            // 
            labelStatus.AutoSize = true;
            labelStatus.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            labelStatus.Location = new Point(345, 11);
            labelStatus.Name = "labelStatus";
            labelStatus.Size = new Size(69, 21);
            labelStatus.TabIndex = 10;
            labelStatus.Text = "Status...";
            // 
            // Console
            // 
            Console.Enabled = false;
            Console.Location = new Point(344, 771);
            Console.Multiline = true;
            Console.Name = "Console";
            Console.ScrollBars = ScrollBars.Both;
            Console.Size = new Size(1064, 39);
            Console.TabIndex = 12;
            // 
            // clearsearchBtn
            // 
            clearsearchBtn.Location = new Point(1686, 38);
            clearsearchBtn.Name = "clearsearchBtn";
            clearsearchBtn.Size = new Size(59, 23);
            clearsearchBtn.TabIndex = 13;
            clearsearchBtn.Text = "Clear";
            clearsearchBtn.UseVisualStyleBackColor = true;
            clearsearchBtn.Click += clearsearchBtn_Click;
            // 
            // groupBox_customSearch
            // 
            groupBox_customSearch.Controls.Add(label_endDate);
            groupBox_customSearch.Controls.Add(label_startDate);
            groupBox_customSearch.Controls.Add(checkBox_enableTime);
            groupBox_customSearch.Controls.Add(dateTimePicker_end);
            groupBox_customSearch.Controls.Add(dateTimePicker_start);
            groupBox_customSearch.Controls.Add(checkBox_RegExp);
            groupBox_customSearch.Controls.Add(button_LabelManager);
            groupBox_customSearch.Controls.Add(search_textBox);
            groupBox_customSearch.Controls.Add(clearsearchBtn);
            groupBox_customSearch.Controls.Add(searchBtn);
            groupBox_customSearch.Controls.Add(labelStatus);
            groupBox_customSearch.Controls.Add(labelItemCount);
            groupBox_customSearch.Controls.Add(label_UTC_Time);
            groupBox_customSearch.Controls.Add(button_SearchWebActivity);
            groupBox_customSearch.Controls.Add(numericUpDown1);
            groupBox_customSearch.Enabled = false;
            groupBox_customSearch.Location = new Point(5, 27);
            groupBox_customSearch.Name = "groupBox_customSearch";
            groupBox_customSearch.Size = new Size(1752, 68);
            groupBox_customSearch.TabIndex = 14;
            groupBox_customSearch.TabStop = false;
            // 
            // label_endDate
            // 
            label_endDate.AutoSize = true;
            label_endDate.Enabled = false;
            label_endDate.Location = new Point(1116, 39);
            label_endDate.Name = "label_endDate";
            label_endDate.Size = new Size(54, 15);
            label_endDate.TabIndex = 20;
            label_endDate.Text = "End Date";
            // 
            // label_startDate
            // 
            label_startDate.AutoSize = true;
            label_startDate.Enabled = false;
            label_startDate.Location = new Point(1116, 14);
            label_startDate.Name = "label_startDate";
            label_startDate.Size = new Size(58, 15);
            label_startDate.TabIndex = 19;
            label_startDate.Text = "Start Date";
            // 
            // checkBox_enableTime
            // 
            checkBox_enableTime.AutoSize = true;
            checkBox_enableTime.Location = new Point(1415, 13);
            checkBox_enableTime.Name = "checkBox_enableTime";
            checkBox_enableTime.Size = new Size(88, 19);
            checkBox_enableTime.TabIndex = 18;
            checkBox_enableTime.Text = "Enable time";
            checkBox_enableTime.UseVisualStyleBackColor = true;
            checkBox_enableTime.CheckedChanged += checkBox_enableTime_CheckedChanged;
            // 
            // dateTimePicker_end
            // 
            dateTimePicker_end.Enabled = false;
            dateTimePicker_end.Location = new Point(1194, 39);
            dateTimePicker_end.Name = "dateTimePicker_end";
            dateTimePicker_end.Size = new Size(209, 23);
            dateTimePicker_end.TabIndex = 17;
            dateTimePicker_end.ValueChanged += dateTimePicker_end_ValueChanged;
            // 
            // dateTimePicker_start
            // 
            dateTimePicker_start.Enabled = false;
            dateTimePicker_start.Location = new Point(1194, 11);
            dateTimePicker_start.Name = "dateTimePicker_start";
            dateTimePicker_start.Size = new Size(209, 23);
            dateTimePicker_start.TabIndex = 16;
            dateTimePicker_start.ValueChanged += dateTimePicker_start_ValueChanged_1;
            // 
            // checkBox_RegExp
            // 
            checkBox_RegExp.AutoSize = true;
            checkBox_RegExp.Location = new Point(1509, 12);
            checkBox_RegExp.Name = "checkBox_RegExp";
            checkBox_RegExp.Size = new Size(65, 19);
            checkBox_RegExp.TabIndex = 14;
            checkBox_RegExp.Text = "RegExp";
            checkBox_RegExp.UseVisualStyleBackColor = true;
            // 
            // button_LabelManager
            // 
            button_LabelManager.Enabled = false;
            button_LabelManager.Image = Resources.Resource1.Label_32;
            button_LabelManager.ImageAlign = ContentAlignment.TopCenter;
            button_LabelManager.Location = new Point(123, 12);
            button_LabelManager.Name = "button_LabelManager";
            button_LabelManager.Size = new Size(117, 50);
            button_LabelManager.TabIndex = 15;
            button_LabelManager.Text = "Label Manager";
            button_LabelManager.TextAlign = ContentAlignment.BottomCenter;
            button_LabelManager.UseVisualStyleBackColor = true;
            button_LabelManager.Click += button_LabelManager_Click;
            // 
            // richTextBox1
            // 
            richTextBox1.Location = new Point(1414, 109);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.Size = new Size(334, 656);
            richTextBox1.TabIndex = 15;
            richTextBox1.Text = "";
            // 
            // button_exportPDF
            // 
            button_exportPDF.Enabled = false;
            button_exportPDF.Location = new Point(1544, 771);
            button_exportPDF.Name = "button_exportPDF";
            button_exportPDF.Size = new Size(100, 23);
            button_exportPDF.TabIndex = 17;
            button_exportPDF.Text = "Generate PDF";
            button_exportPDF.UseVisualStyleBackColor = true;
            button_exportPDF.Click += button_exportPDF_Click;
            // 
            // autoLabel1
            // 
            autoLabel1.Location = new Point(1514, 798);
            autoLabel1.Name = "autoLabel1";
            autoLabel1.Size = new Size(174, 15);
            autoLabel1.TabIndex = 18;
            autoLabel1.Text = "www.internet-solutions.com.co";
            autoLabel1.Click += autoLabel1_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1769, 822);
            Controls.Add(autoLabel1);
            Controls.Add(button_exportPDF);
            Controls.Add(richTextBox1);
            Controls.Add(groupBox_customSearch);
            Controls.Add(Console);
            Controls.Add(sfDataGrid1);
            Controls.Add(groupBox_Main);
            Controls.Add(menuStrip1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = menuStrip1;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Form1";
            Text = "Browser Reviewer v0.1";
            Load += Form1_Load;
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDown1).EndInit();
            ((System.ComponentModel.ISupportInitialize)sfDataGrid1).EndInit();
            groupBox_customSearch.ResumeLayout(false);
            groupBox_customSearch.PerformLayout();
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
        private TextBox Console;
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
    }
}
