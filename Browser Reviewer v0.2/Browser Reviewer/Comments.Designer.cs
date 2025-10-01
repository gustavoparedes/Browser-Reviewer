namespace Browser_Reviewer
{
    partial class Form_Comments
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_Comments));
            textBox_comments = new TextBox();
            label1 = new Label();
            button_saveComment = new Button();
            SuspendLayout();
            // 
            // textBox_comments
            // 
            textBox_comments.Location = new Point(61, 66);
            textBox_comments.Multiline = true;
            textBox_comments.Name = "textBox_comments";
            textBox_comments.Size = new Size(340, 198);
            textBox_comments.TabIndex = 0;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.Location = new Point(195, 36);
            label1.Name = "label1";
            label1.Size = new Size(74, 17);
            label1.TabIndex = 1;
            label1.Text = "Comments";
            // 
            // button_saveComment
            // 
            button_saveComment.Location = new Point(194, 286);
            button_saveComment.Name = "button_saveComment";
            button_saveComment.Size = new Size(75, 23);
            button_saveComment.TabIndex = 2;
            button_saveComment.Text = "Save comment";
            button_saveComment.UseVisualStyleBackColor = true;
            button_saveComment.Click += button_saveComment_Click;
            // 
            // Form_Comments
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(490, 338);
            Controls.Add(button_saveComment);
            Controls.Add(label1);
            Controls.Add(textBox_comments);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "Form_Comments";
            Text = "Comments";
            Load += Form_Comments_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox textBox_comments;
        private Label label1;
        private Button button_saveComment;
    }
}