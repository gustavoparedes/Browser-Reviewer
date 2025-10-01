namespace Browser_Reviewer
{
    partial class Form_Report
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            groupBox_Report = new GroupBox();
            button_Generate = new Button();
            checkedListBox1 = new CheckedListBox();
            checkBox1 = new CheckBox();
            groupBox_Report.SuspendLayout();
            SuspendLayout();
            // 
            // groupBox_Report
            // 
            groupBox_Report.Controls.Add(checkBox1);
            groupBox_Report.Controls.Add(button_Generate);
            groupBox_Report.Controls.Add(checkedListBox1);
            groupBox_Report.Location = new Point(73, 12);
            groupBox_Report.Name = "groupBox_Report";
            groupBox_Report.Size = new Size(349, 301);
            groupBox_Report.TabIndex = 0;
            groupBox_Report.TabStop = false;
            groupBox_Report.Text = "Select Labels";
            // 
            // button_Generate
            // 
            button_Generate.Location = new Point(138, 260);
            button_Generate.Name = "button_Generate";
            button_Generate.Size = new Size(75, 23);
            button_Generate.TabIndex = 1;
            button_Generate.Text = "Generate";
            button_Generate.UseVisualStyleBackColor = true;
            button_Generate.Click += button_Generate_Click;
            // 
            // checkedListBox1
            // 
            checkedListBox1.FormattingEnabled = true;
            checkedListBox1.Location = new Point(15, 52);
            checkedListBox1.Name = "checkedListBox1";
            checkedListBox1.Size = new Size(324, 202);
            checkedListBox1.TabIndex = 0;
            // 
            // checkBox1
            // 
            checkBox1.AutoSize = true;
            checkBox1.Location = new Point(15, 27);
            checkBox1.Name = "checkBox1";
            checkBox1.Size = new Size(72, 19);
            checkBox1.TabIndex = 2;
            checkBox1.Text = "Select all";
            checkBox1.UseVisualStyleBackColor = true;
            checkBox1.CheckedChanged += checkBox1_CheckedChanged;
            // 
            // Form_Report
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(490, 338);
            Controls.Add(groupBox_Report);
            Name = "Form_Report";
            Text = "Report";
            Load += Report_Load;
            groupBox_Report.ResumeLayout(false);
            groupBox_Report.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private GroupBox groupBox_Report;
        private CheckedListBox checkedListBox1;
        private Button button_Generate;
        private CheckBox checkBox1;
    }
}