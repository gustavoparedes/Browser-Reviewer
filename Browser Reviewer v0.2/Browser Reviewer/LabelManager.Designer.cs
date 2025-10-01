namespace Browser_Reviewer
{
    partial class Form_LabelManager
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
            dataGridView1 = new DataGridView();
            button_addLabel = new Button();
            button_deleteLabel = new Button();
            groupBox_label = new GroupBox();
            button_saveLabel = new Button();
            label1 = new Label();
            button_ok = new Button();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            groupBox_label.SuspendLayout();
            SuspendLayout();
            // 
            // dataGridView1
            // 
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Location = new Point(12, 51);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.Size = new Size(289, 187);
            dataGridView1.TabIndex = 0;
            dataGridView1.CellContentClick += dataGridView1_CellContentClick;
            // 
            // button_addLabel
            // 
            button_addLabel.Image = Resources.Resource1.Label_add_32;
            button_addLabel.Location = new Point(338, 51);
            button_addLabel.Name = "button_addLabel";
            button_addLabel.Size = new Size(50, 50);
            button_addLabel.TabIndex = 1;
            button_addLabel.UseVisualStyleBackColor = true;
            button_addLabel.Click += button_addLabel_Click;
            // 
            // button_deleteLabel
            // 
            button_deleteLabel.Image = Resources.Resource1.Label_delete_32;
            button_deleteLabel.Location = new Point(338, 133);
            button_deleteLabel.Name = "button_deleteLabel";
            button_deleteLabel.Size = new Size(50, 50);
            button_deleteLabel.TabIndex = 2;
            button_deleteLabel.UseVisualStyleBackColor = true;
            button_deleteLabel.Click += button_deleteLabel_Click;
            // 
            // groupBox_label
            // 
            groupBox_label.Controls.Add(label1);
            groupBox_label.Controls.Add(dataGridView1);
            groupBox_label.Controls.Add(button_ok);
            groupBox_label.Controls.Add(button_saveLabel);
            groupBox_label.Controls.Add(button_addLabel);
            groupBox_label.Controls.Add(button_deleteLabel);
            groupBox_label.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            groupBox_label.Location = new Point(34, 12);
            groupBox_label.Name = "groupBox_label";
            groupBox_label.Size = new Size(418, 314);
            groupBox_label.TabIndex = 4;
            groupBox_label.TabStop = false;
            // 
            // button_saveLabel
            // 
            button_saveLabel.Image = Resources.Resource1.Label_save_32;
            button_saveLabel.Location = new Point(338, 214);
            button_saveLabel.Name = "button_saveLabel";
            button_saveLabel.Size = new Size(50, 50);
            button_saveLabel.TabIndex = 4;
            button_saveLabel.UseVisualStyleBackColor = true;
            button_saveLabel.Click += button_saveLabel_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.Location = new Point(6, 18);
            label1.Name = "label1";
            label1.Size = new Size(108, 17);
            label1.TabIndex = 5;
            label1.Text = "Available Labels";
            // 
            // button_ok
            // 
            button_ok.Location = new Point(136, 258);
            button_ok.Name = "button_ok";
            button_ok.Size = new Size(75, 23);
            button_ok.TabIndex = 6;
            button_ok.Text = "Ok";
            button_ok.UseVisualStyleBackColor = true;
            button_ok.Click += button_ok_Click;
            // 
            // Form_LabelManager
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(490, 338);
            Controls.Add(groupBox_label);
            Name = "Form_LabelManager";
            Text = "Label Manager";
            Load += LabelManager_Load;
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            groupBox_label.ResumeLayout(false);
            groupBox_label.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private DataGridView dataGridView1;
        private Button button_addLabel;
        private Button button_deleteLabel;
        private GroupBox groupBox_label;
        private Label label1;
        private Button button_saveLabel;
        private Button button_ok;
    }
}