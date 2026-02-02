using System;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;

namespace UniversalTextApp
{

    public class MainForm : Form
    {
        // UI
        RichTextBox editor;
        StatusStrip status;
        ToolStripStatusLabel statusLabel;
        MenuStrip menu;

        // State
        string currentFile = "";
        bool isDirty = false;

        // History
        List<string> recentFiles = new List<string>();
        string historyStore = "recent_files.txt";

        public MainForm()
        {
            LoadHistory();
            BuildUI();
            ApplyDarkTheme();
            UpdateStatus();
        }

        // ================= UI =================

        void BuildUI()
        {
            Text = "Incidence Logging System";
            WindowState = FormWindowState.Maximized;
            Font = new Font("Segoe UI", 10);

            menu = new MenuStrip { BackColor = Color.FromArgb(35, 35, 35), ForeColor = Color.White };

            var file = new ToolStripMenuItem("File");
            var edit = new ToolStripMenuItem("Edit");
            var view = new ToolStripMenuItem("View");
            var tools = new ToolStripMenuItem("Tools");

            file.DropDownItems.Add("New Case", null, NewFile);
            file.DropDownItems.Add("Open File", null, OpenFile);
            file.DropDownItems.Add("Save Draft", null, SaveFile);
            file.DropDownItems.Add("Save As", null, SaveAsFile);
            file.DropDownItems.Add("Historic Logs", null, ShowHistory);
            file.DropDownItems.Add("Exit", null, ExitApp);

            edit.DropDownItems.Add("Undo", null, (s, e) => editor.Undo());
            edit.DropDownItems.Add("Redo", null, (s, e) => editor.Redo());
            edit.DropDownItems.Add("Cut", null, (s, e) => editor.Cut());
            edit.DropDownItems.Add("Copy", null, (s, e) => editor.Copy());
            edit.DropDownItems.Add("Paste", null, (s, e) => editor.Paste());
            edit.DropDownItems.Add("Select All", null, (s, e) => editor.SelectAll());
            edit.DropDownItems.Add("Find & Replace", null, FindReplace);

            view.DropDownItems.Add("Dark Mode", null, (s, e) => ApplyDarkTheme());
            view.DropDownItems.Add("Light Mode", null, (s, e) => ApplyLightTheme());

            tools.DropDownItems.Add("New Structured Log", null, OpenLogForm);
            tools.DropDownItems.Add("Statistics", null, ShowStats);

            menu.Items.AddRange(new[] { file, edit, view, tools });
            Controls.Add(menu);

            editor = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                Font = new Font("Consolas", 11),
                AcceptsTab = true
            };
            editor.TextChanged += (s, e) => { isDirty = true; UpdateStatus(); };
            Controls.Add(editor);

            status = new StatusStrip();
            statusLabel = new ToolStripStatusLabel();
            status.Items.Add(statusLabel);
            Controls.Add(status);

            MainMenuStrip = menu;
        }

        // ================= FILE OPS =================

        void NewFile(object s, EventArgs e)
        {
            if (!ConfirmUnsaved()) return;
            editor.Clear();
            currentFile = "";
            isDirty = false;
        }

        void OpenFile(object s, EventArgs e)
        {
            if (!ConfirmUnsaved()) return;
            using (var d = new OpenFileDialog())
            {
                d.Filter = "Incidence Report|*.txt|All Files|*.*";
                if (d.ShowDialog() == DialogResult.OK)
                {
                    editor.Text = File.ReadAllText(d.FileName);
                    currentFile = d.FileName;
                    AddHistory(d.FileName);
                    isDirty = false;
                }
            }
        }

        void SaveFile(object s, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentFile))
            {
                SaveAsFile(s, e); return;
            }
            File.WriteAllText(currentFile, editor.Text);
            AddHistory(currentFile);
            isDirty = false;
        }

        void SaveAsFile(object s, EventArgs e)
        {
            using (var d = new SaveFileDialog())
            {
                d.Filter = "Text Files|*.txt";
                if (d.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(d.FileName, editor.Text);
                    currentFile = d.FileName;
                    AddHistory(d.FileName);
                    isDirty = false;
                }
            }
        }

        void ExitApp(object s, EventArgs e)
        {
            if (ConfirmUnsaved())
                Application.Exit();
        }

        // ================= HISTORY =================

        void AddHistory(string path)
        {
            recentFiles.Remove(path);
            recentFiles.Insert(0, path);
            if (recentFiles.Count > 10)
                recentFiles = recentFiles.Take(10).ToList();

            File.WriteAllLines(historyStore, recentFiles);
        }

        void LoadHistory()
        {
            if (File.Exists(historyStore))
                recentFiles = File.ReadAllLines(historyStore).ToList();
        }

        void ShowHistory(object s, EventArgs e)
        {
            if (recentFiles.Count == 0)
            {
                MessageBox.Show("No history available."); return;
            }

            var menu = new ContextMenuStrip();
            foreach (var f in recentFiles)
            {
                menu.Items.Add(f, null, (x, y) => {
                    if (File.Exists(f))
                    {
                        editor.Text = File.ReadAllText(f);
                        currentFile = f;
                        isDirty = false;
                    }
                });
            }
            menu.Show(Cursor.Position);
        }

        // ================= LOG FORM =================

        void OpenLogForm(object s, EventArgs e)
        {
            var f = new StructuredLogForm();
            if (f.ShowDialog() == DialogResult.OK)
            {
                editor.AppendText(f.GeneratedLog + "\n");
                isDirty = true;
            }
        }

        // ================= FEATURES =================

        void FindReplace(object s, EventArgs e)
        {
            string f = Prompt("Find:");
            if (string.IsNullOrEmpty(f)) return;
            string r = Prompt("Replace with:");
            editor.Text = editor.Text.Replace(f, r);
        }

        void ShowStats(object s, EventArgs e)
        {
            int lines = editor.Lines.Length;
            int words = editor.Text.Split(
                new[] { ' ', '\n' },
                StringSplitOptions.RemoveEmptyEntries).Length;

            MessageBox.Show($"Lines: {lines}\nWords: {words}");
        }

        // ================= THEMES =================

        void ApplyDarkTheme()
        {
            BackColor = Color.FromArgb(30, 30, 30);
            editor.BackColor = Color.FromArgb(30, 30, 30);
            editor.ForeColor = Color.Gainsboro;
        }

        void ApplyLightTheme()
        {
            BackColor = Color.White;
            editor.BackColor = Color.White;
            editor.ForeColor = Color.Black;
        }

        // ================= UTILS =================

        void UpdateStatus()
        {
            statusLabel.Text = $"Lines: {editor.Lines.Length}";
        }

        bool ConfirmUnsaved()
        {
            if (!isDirty) return true;
            return MessageBox.Show(
                "Unsaved changes detected. Continue?",
                "Warning",
                MessageBoxButtons.YesNo) == DialogResult.Yes;
        }

        string Prompt(string m)
        {
            return Microsoft.VisualBasic.Interaction.InputBox(m, "Input");
        }

        // ================= INNER LOG FORM =================

        class StructuredLogForm : Form
        {
            public string GeneratedLog { get; private set; }

            TextBox title, status;
            RichTextBox desc;

            public StructuredLogForm()
            {
                Text = "Incidence Log Entry";
                Size = new Size(500, 350);
                StartPosition = FormStartPosition.CenterParent;
                FormBorderStyle = FormBorderStyle.FixedDialog;

                var layout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    RowCount = 4,
                    ColumnCount = 2
                };

                layout.Controls.Add(new Label { Text = "Title" }, 0, 0);
                title = new TextBox(); layout.Controls.Add(title, 1, 0);

                layout.Controls.Add(new Label { Text = "Status" }, 0, 1);
                status = new TextBox(); layout.Controls.Add(status, 1, 1);

                layout.Controls.Add(new Label { Text = "Description" }, 0, 2);
                desc = new RichTextBox { Height = 120 };
                layout.Controls.Add(desc, 1, 2);

                var btn = new Button { Text = "Generate Log", Dock = DockStyle.Fill };
                btn.Click += (s, e) => Generate();
                layout.Controls.Add(btn, 1, 3);

                Controls.Add(layout);
            }

            void Generate()
            {
                GeneratedLog =
$@"----- LOG ENTRY -----
Date   : {DateTime.Now}
Title  : {title.Text}
Status : {status.Text}
Details:
{desc.Text}
---------------------";
                DialogResult = DialogResult.OK;
                Close();
            }
        }
    }
}
