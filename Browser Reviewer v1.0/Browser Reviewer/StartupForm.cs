using Browser_Reviewer.Resources;
using System.Drawing.Drawing2D;

namespace Browser_Reviewer
{
    public enum StartupAction
    {
        None,
        NewProject,
        OpenProject,
        Exit
    }

    public sealed class StartupRequest
    {
        public StartupAction Action { get; set; }

        public string ProjectPath { get; set; } = "";

        public string ScanPath { get; set; } = "";
    }

    public sealed class StartupForm : Form
    {
        public StartupAction SelectedAction { get; private set; }

        public StartupRequest Request { get; private set; } = new StartupRequest();

        public StartupForm()
        {
            Text = "Browser Reviewer";
            AppIcon.Apply(this);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = true;
            ClientSize = new Size(600, 440);
            BackColor = Color.FromArgb(246, 248, 250);
            Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(34, 30, 34, 30),
                BackColor = BackColor
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 98));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            Controls.Add(root);

            var header = new Panel { Dock = DockStyle.Fill, BackColor = BackColor };
            var icon = new PictureBox
            {
                Image = Resource1.Internet_32,
                SizeMode = PictureBoxSizeMode.CenterImage,
                Location = new Point(0, 6),
                Size = new Size(52, 52)
            };
            var title = new Label
            {
                Text = "Browser Reviewer v1.0",
                Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold, GraphicsUnit.Point, 0),
                ForeColor = Color.FromArgb(24, 32, 43),
                AutoSize = true,
                Location = new Point(64, 2)
            };
            var subtitle = new Label
            {
                Text = "Start a new forensic browser review or continue with an existing .bre project.",
                ForeColor = Color.FromArgb(76, 86, 97),
                AutoSize = false,
                Location = new Point(67, 45),
                Size = new Size(420, 42)
            };
            header.Controls.Add(icon);
            header.Controls.Add(title);
            header.Controls.Add(subtitle);
            root.Controls.Add(header, 0, 0);

            var prompt = new Label
            {
                Text = "What do you want to do?",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point, 0),
                ForeColor = Color.FromArgb(24, 32, 43)
            };
            root.Controls.Add(prompt, 0, 1);

            var actions = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = BackColor,
                Height = 186
            };
            actions.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
            actions.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
            actions.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
            root.Controls.Add(actions, 0, 2);

            actions.Controls.Add(CreateActionButton("New project and scan", "Create a .bre project, choose evidence path, and start scanning.", StartupAction.NewProject), 0, 0);
            actions.Controls.Add(CreateActionButton("Open existing project", "Open a previous .bre file.", StartupAction.OpenProject), 0, 1);
            actions.Controls.Add(CreateActionButton("Exit", "Close Browser Reviewer.", StartupAction.Exit), 0, 2);

            var footer = new Label
            {
                Text = "Web artifacts review workflow",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomRight,
                ForeColor = Color.FromArgb(104, 113, 124)
            };
            root.Controls.Add(footer, 0, 3);

            FormClosing += StartupForm_FormClosing;
        }

        private Button CreateActionButton(string title, string detail, StartupAction action)
        {
            var button = new Button
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 8),
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(18, 0, 0, 0),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(24, 32, 43),
                Text = $"{title}\r\n{detail}",
                Tag = action
            };
            button.FlatAppearance.BorderColor = Color.FromArgb(210, 216, 223);
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(231, 238, 246);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(215, 226, 239);
            button.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            button.Click += ActionButton_Click;
            button.Paint += RoundedButton_Paint;
            return button;
        }

        private void ActionButton_Click(object? sender, EventArgs e)
        {
            if (sender is not Button button || button.Tag is not StartupAction action)
                return;

            if (action == StartupAction.NewProject)
            {
                using var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Browser Reviewer files (*.bre)|*.bre",
                    Title = "Create Project",
                    FileName = "Default.bre"
                };

                if (saveFileDialog.ShowDialog(this) != DialogResult.OK)
                    return;

                using var folderBrowserDialog = new FolderBrowserDialog
                {
                    Description = "Select the path to scan for web activity"
                };

                if (folderBrowserDialog.ShowDialog(this) != DialogResult.OK)
                    return;

                Request = new StartupRequest
                {
                    Action = StartupAction.NewProject,
                    ProjectPath = saveFileDialog.FileName,
                    ScanPath = folderBrowserDialog.SelectedPath
                };
            }
            else if (action == StartupAction.OpenProject)
            {
                using var openFileDialog = new OpenFileDialog
                {
                    Filter = "Browser Reviewer files (*.bre)|*.bre",
                    Title = "Open Project"
                };

                if (openFileDialog.ShowDialog(this) != DialogResult.OK)
                    return;

                Request = new StartupRequest
                {
                    Action = StartupAction.OpenProject,
                    ProjectPath = openFileDialog.FileName
                };
            }
            else
            {
                Request = new StartupRequest { Action = StartupAction.Exit };
            }

            SelectedAction = Request.Action;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void RoundedButton_Paint(object? sender, PaintEventArgs e)
        {
            if (sender is not Button button)
                return;

            using var path = new GraphicsPath();
            int radius = 8;
            Rectangle rect = new Rectangle(0, 0, button.Width - 1, button.Height - 1);
            path.AddArc(rect.Left, rect.Top, radius, radius, 180, 90);
            path.AddArc(rect.Right - radius, rect.Top, radius, radius, 270, 90);
            path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
            path.AddArc(rect.Left, rect.Bottom - radius, radius, radius, 90, 90);
            path.CloseFigure();
            button.Region = new Region(path);
        }

        private void StartupForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (SelectedAction == StartupAction.None)
            {
                SelectedAction = StartupAction.Exit;
                Request = new StartupRequest { Action = StartupAction.Exit };
            }
        }
    }
}
