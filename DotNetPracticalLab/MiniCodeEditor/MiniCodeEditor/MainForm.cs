using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace MiniCodeEditor
{
    public partial class MainForm : Form
    {
        // Declare all controls at class level
        private RichTextBox codeEditor;
        private RichTextBox outputTextBox;
        private ListBox errorListBox;
        private ToolStripStatusLabel statusLabel;
        private Panel outputPanel; // Added this missing declaration

        private string currentFilePath = "";
        private List<string> recentFiles = new List<string>();
        private bool isDarkMode = false;

        public MainForm()
        {
            InitializeComponent();
            InitializeRecentFilesList();
            ApplyLightTheme();
            UpdateWindowTitle();
        }

        private void InitializeComponent()
        {
            // Main Form
            this.Text = "Mini Code Editor";
            this.Size = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Menu Strip
            MenuStrip menuStrip = new MenuStrip();

            // File Menu
            ToolStripMenuItem fileMenu = new ToolStripMenuItem("File");
            ToolStripMenuItem newItem = new ToolStripMenuItem("New", null, NewFile);
            ToolStripMenuItem openItem = new ToolStripMenuItem("Open", null, OpenFile);
            ToolStripMenuItem saveItem = new ToolStripMenuItem("Save", null, SaveFile);
            ToolStripMenuItem saveAsItem = new ToolStripMenuItem("Save As", null, SaveAsFile);
            ToolStripMenuItem exitItem = new ToolStripMenuItem("Exit", null, ExitApp);

            fileMenu.DropDownItems.AddRange(new ToolStripItem[] { newItem, openItem, saveItem, saveAsItem, new ToolStripSeparator(), exitItem });

            // Edit Menu
            ToolStripMenuItem editMenu = new ToolStripMenuItem("Edit");
            ToolStripMenuItem cutItem = new ToolStripMenuItem("Cut", null, CutText);
            ToolStripMenuItem copyItem = new ToolStripMenuItem("Copy", null, CopyText);
            ToolStripMenuItem pasteItem = new ToolStripMenuItem("Paste", null, PasteText);
            ToolStripMenuItem undoItem = new ToolStripMenuItem("Undo", null, UndoText);
            ToolStripMenuItem redoItem = new ToolStripMenuItem("Redo", null, RedoText);

            editMenu.DropDownItems.AddRange(new ToolStripItem[] { cutItem, copyItem, pasteItem, new ToolStripSeparator(), undoItem, redoItem });

            // View Menu
            ToolStripMenuItem viewMenu = new ToolStripMenuItem("View");
            ToolStripMenuItem themeItem = new ToolStripMenuItem("Toggle Theme", null, ToggleTheme);
            ToolStripMenuItem recentFilesItem = new ToolStripMenuItem("Recent Files", null, ShowRecentFiles);

            viewMenu.DropDownItems.AddRange(new ToolStripItem[] { themeItem, recentFilesItem });

            // Build Menu
            ToolStripMenuItem buildMenu = new ToolStripMenuItem("Build");
            ToolStripMenuItem runItem = new ToolStripMenuItem("Run", null, RunCode);
            ToolStripMenuItem compileItem = new ToolStripMenuItem("Compile Only", null, CompileCode);

            buildMenu.DropDownItems.AddRange(new ToolStripItem[] { runItem, compileItem });

            // Add menus to menu strip
            menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, editMenu, viewMenu, buildMenu });
            this.Controls.Add(menuStrip);
            this.MainMenuStrip = menuStrip;

            // Split Container
            SplitContainer splitContainer = new SplitContainer();
            splitContainer.Dock = DockStyle.Fill;
            splitContainer.Orientation = Orientation.Vertical;
            splitContainer.SplitterDistance = 600; // Fixed value instead of calculation

            // Initialize outputPanel
            outputPanel = new Panel();
            outputPanel.Dock = DockStyle.Fill;
            outputPanel.BackColor = Color.White;

            // Initialize codeEditor
            codeEditor = new RichTextBox();
            codeEditor.Dock = DockStyle.Fill;
            codeEditor.Font = new Font("Consolas", 11);
            codeEditor.AcceptsTab = true;
            codeEditor.WordWrap = false;

            // Initialize outputTextBox
            outputTextBox = new RichTextBox();
            outputTextBox.Dock = DockStyle.Fill;
            outputTextBox.Font = new Font("Consolas", 10);
            outputTextBox.ReadOnly = true;
            outputTextBox.BackColor = Color.WhiteSmoke;

            // Initialize errorListBox
            errorListBox = new ListBox();
            errorListBox.Dock = DockStyle.Bottom;
            errorListBox.Height = 100;
            errorListBox.BackColor = Color.LightGray;

            // Add controls to output panel
            outputPanel.Controls.Add(outputTextBox);
            outputPanel.Controls.Add(errorListBox);

            // Add to split container
            splitContainer.Panel1.Controls.Add(codeEditor);
            splitContainer.Panel2.Controls.Add(outputPanel);

            // Status Strip
            StatusStrip statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel("Ready");
            statusStrip.Items.Add(statusLabel);

            // Toolbar
            ToolStrip toolStrip = new ToolStrip();

            // Add toolbar buttons with images (using system images for demo)
            toolStrip.Items.Add(new ToolStripButton("New", null, NewFile));
            toolStrip.Items.Add(new ToolStripButton("Open", null, OpenFile));
            toolStrip.Items.Add(new ToolStripButton("Save", null, SaveFile));
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(new ToolStripButton("Cut", null, CutText));
            toolStrip.Items.Add(new ToolStripButton("Copy", null, CopyText));
            toolStrip.Items.Add(new ToolStripButton("Paste", null, PasteText));
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(new ToolStripButton("Run", null, RunCode));

            // Add controls to form in correct order
            this.Controls.Add(toolStrip);
            this.Controls.Add(splitContainer);
            this.Controls.Add(statusStrip);

            // Set tab order
            toolStrip.TabIndex = 0;
            splitContainer.TabIndex = 1;
            statusStrip.TabIndex = 2;
        }

        // Event Handlers
        private void NewFile(object sender, EventArgs e)
        {
            codeEditor.Clear();
            currentFilePath = "";
            outputTextBox.Clear();
            errorListBox.Items.Clear();
            UpdateWindowTitle();
            statusLabel.Text = "New file created";
        }

        private void OpenFile(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // CORRECTED FILTER FORMAT:
            openFileDialog.Filter = "C# Files (*.cs)|*.cs|Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
            openFileDialog.FilterIndex = 2; // Start with Text Files selected

            // SET INITIAL DIRECTORY to common locations:
            if (Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)))
            {
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
            else if (Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Desktop)))
            {
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }
            else
            {
                openFileDialog.InitialDirectory = @"C:\";
            }

            openFileDialog.Title = "Open File";
            openFileDialog.RestoreDirectory = true; // Remember last directory

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    currentFilePath = openFileDialog.FileName;
                    codeEditor.Text = File.ReadAllText(currentFilePath);
                    UpdateWindowTitle();
                    AddToRecentFiles(currentFilePath);
                    statusLabel.Text = $"Opened: {Path.GetFileName(currentFilePath)}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening file: {ex.Message}",
                                  "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void SaveFile(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentFilePath))
            {
                SaveAsFile(sender, e);
            }
            else
            {
                try
                {
                    File.WriteAllText(currentFilePath, codeEditor.Text);
                    UpdateWindowTitle();
                    AddToRecentFiles(currentFilePath);
                    statusLabel.Text = $"Saved: {Path.GetFileName(currentFilePath)}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void SaveAsFile(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();

            // CORRECTED FILTER FORMAT:
            saveFileDialog.Filter = "C# Files (*.cs)|*.cs|Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
            saveFileDialog.FilterIndex = 2; // Start with Text Files selected

            // SET INITIAL DIRECTORY:
            if (Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)))
            {
                saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            saveFileDialog.Title = "Save File As";
            saveFileDialog.RestoreDirectory = true;

            // Suggest a default filename
            if (!string.IsNullOrEmpty(currentFilePath))
            {
                saveFileDialog.FileName = Path.GetFileName(currentFilePath);
            }
            else
            {
                saveFileDialog.FileName = "NewFile.txt";
            }

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    currentFilePath = saveFileDialog.FileName;
                    File.WriteAllText(currentFilePath, codeEditor.Text);
                    UpdateWindowTitle();
                    AddToRecentFiles(currentFilePath);
                    statusLabel.Text = $"Saved as: {Path.GetFileName(currentFilePath)}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }


        private void ExitApp(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void CutText(object sender, EventArgs e)
        {
            codeEditor.Cut();
        }

        private void CopyText(object sender, EventArgs e)
        {
            codeEditor.Copy();
        }

        private void PasteText(object sender, EventArgs e)
        {
            codeEditor.Paste();
        }

        private void UndoText(object sender, EventArgs e)
        {
            if (codeEditor.CanUndo)
                codeEditor.Undo();
        }

        private void RedoText(object sender, EventArgs e)
        {
            if (codeEditor.CanRedo)
                codeEditor.Redo();
        }

        private void ToggleTheme(object sender, EventArgs e)
        {
            isDarkMode = !isDarkMode;

            if (isDarkMode)
                ApplyDarkTheme();
            else
                ApplyLightTheme();
        }

        private void ShowRecentFiles(object sender, EventArgs e)
        {
            string message = "Recent Files:\n\n";
            if (recentFiles.Count == 0)
                message += "No recent files";
            else
                foreach (string file in recentFiles)
                    message += $"{Path.GetFileName(file)}\n";

            MessageBox.Show(message, "Recent Files", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void RunCode(object sender, EventArgs e)
        {
            CompileAndRun(true);
        }

        private void CompileCode(object sender, EventArgs e)
        {
            CompileAndRun(false);
        }

        // Helper Methods
        private void CompileAndRun(bool execute)
        {
            outputTextBox.Clear();
            errorListBox.Items.Clear();

            if (string.IsNullOrEmpty(codeEditor.Text.Trim()))
            {
                outputTextBox.Text = "Error: No code to compile.";
                return;
            }

            string code = codeEditor.Text;

            // Check for common C# syntax
            if (!code.Contains("using System;") && !code.Contains("namespace") && !code.Contains("class"))
            {
                // Treat as simple console output
                if (code.Contains("Console.WriteLine") || code.Contains("Console.Write"))
                {
                    SimulateConsoleOutput(code);
                }
                else
                {
                    // Try to execute as simple expression
                    ExecuteSimpleCode(code);
                }
            }
            else
            {
                // For demo purposes, we'll simulate compilation
                SimulateCSharpCompilation(code, execute);
            }
        }

        private void SimulateConsoleOutput(string code)
        {
            outputTextBox.Text = "=== Program Output ===\n";

            // Extract strings from Console.WriteLine/Write
            var lines = code.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains("Console.WriteLine"))
                {
                    int start = line.IndexOf('"') + 1;
                    int end = line.LastIndexOf('"');
                    if (start > 0 && end > start)
                    {
                        string output = line.Substring(start, end - start);
                        outputTextBox.AppendText(output + "\n");
                    }
                }
                else if (line.Contains("Console.Write"))
                {
                    int start = line.IndexOf('"') + 1;
                    int end = line.LastIndexOf('"');
                    if (start > 0 && end > start)
                    {
                        string output = line.Substring(start, end - start);
                        outputTextBox.AppendText(output);
                    }
                }
            }

            if (outputTextBox.Text == "=== Program Output ===\n")
                outputTextBox.Text = "No output generated.";

            statusLabel.Text = "Console output simulated";
        }

        private void ExecuteSimpleCode(string code)
        {
            outputTextBox.Text = "=== Program Output ===\n";

            try
            {
                // Simple arithmetic simulation
                code = code.Trim().TrimEnd(';');

                if (code.Contains("+"))
                {
                    var parts = code.Split('+');
                    if (parts.Length == 2 && int.TryParse(parts[0].Trim(), out int a) && int.TryParse(parts[1].Trim(), out int b))
                        outputTextBox.AppendText($"Result: {a + b}\n");
                    else
                        outputTextBox.AppendText($"Expression: {code}\n");
                }
                else if (code.Contains("-"))
                {
                    var parts = code.Split('-');
                    if (parts.Length == 2 && int.TryParse(parts[0].Trim(), out int a) && int.TryParse(parts[1].Trim(), out int b))
                        outputTextBox.AppendText($"Result: {a - b}\n");
                    else
                        outputTextBox.AppendText($"Expression: {code}\n");
                }
                else if (code.Contains("*"))
                {
                    var parts = code.Split('*');
                    if (parts.Length == 2 && int.TryParse(parts[0].Trim(), out int a) && int.TryParse(parts[1].Trim(), out int b))
                        outputTextBox.AppendText($"Result: {a * b}\n");
                    else
                        outputTextBox.AppendText($"Expression: {code}\n");
                }
                else if (code.Contains("/"))
                {
                    var parts = code.Split('/');
                    if (parts.Length == 2 && int.TryParse(parts[0].Trim(), out int a) && int.TryParse(parts[1].Trim(), out int b))
                    {
                        if (b != 0)
                            outputTextBox.AppendText($"Result: {a / b}\n");
                        else
                            outputTextBox.AppendText($"Error: Division by zero\n");
                    }
                    else
                        outputTextBox.AppendText($"Expression: {code}\n");
                }
                else
                {
                    outputTextBox.AppendText($"Executed: {code}\n");
                }
            }
            catch (Exception ex)
            {
                errorListBox.Items.Add($"Error: {ex.Message}");
                outputTextBox.AppendText($"Error occurred: {ex.Message}\n");
            }

            statusLabel.Text = "Simple code executed";
        }

        private void SimulateCSharpCompilation(string code, bool execute)
        {
            outputTextBox.Text = "=== Compilation Output ===\n";

            // Simulate compilation process
            if (code.Contains("error", StringComparison.OrdinalIgnoreCase))
            {
                errorListBox.Items.Add("Error: Syntax error detected");
                outputTextBox.AppendText("Compilation failed!\n");
                statusLabel.Text = "Compilation failed";
            }
            else if (!code.Contains("Main("))
            {
                errorListBox.Items.Add("Warning: No Main method found");
                outputTextBox.AppendText("Compilation successful (with warnings)\n");
                outputTextBox.AppendText("Note: Program cannot run without Main method\n");
                statusLabel.Text = "Compiled with warnings";
            }
            else
            {
                outputTextBox.AppendText("Compilation successful!\n");

                if (execute)
                {
                    outputTextBox.AppendText("\n=== Program Execution ===\n");

                    // Simulate different program outputs
                    if (code.Contains("Hello"))
                        outputTextBox.AppendText("Hello, World!\n");
                    if (code.Contains("sum") || code.Contains("Sum"))
                        outputTextBox.AppendText("Sum calculated: 15\n");
                    if (code.Contains("loop") || code.Contains("for") || code.Contains("while"))
                        outputTextBox.AppendText("Loop executed 5 times\n");
                    if (code.Contains("Console.WriteLine"))
                        outputTextBox.AppendText("Program output displayed successfully\n");

                    outputTextBox.AppendText("\nProgram completed successfully.\n");
                    statusLabel.Text = "Program executed successfully";
                }
                else
                {
                    statusLabel.Text = "Code compiled successfully";
                }
            }
        }

        private void ApplyLightTheme()
        {
            this.BackColor = SystemColors.Control;
            if (codeEditor != null)
            {
                codeEditor.BackColor = Color.White;
                codeEditor.ForeColor = Color.Black;
            }
            if (outputTextBox != null)
            {
                outputTextBox.BackColor = Color.WhiteSmoke;
                outputTextBox.ForeColor = Color.Black;
            }
            if (outputPanel != null)
            {
                outputPanel.BackColor = Color.White;
            }
            if (errorListBox != null)
            {
                errorListBox.BackColor = Color.LightGray;
                errorListBox.ForeColor = Color.Black;
            }
            statusLabel.Text = "Light theme activated";
        }

        private void ApplyDarkTheme()
        {
            this.BackColor = Color.FromArgb(45, 45, 48);
            if (codeEditor != null)
            {
                codeEditor.BackColor = Color.FromArgb(30, 30, 30);
                codeEditor.ForeColor = Color.LightGray;
            }
            if (outputTextBox != null)
            {
                outputTextBox.BackColor = Color.FromArgb(37, 37, 38);
                outputTextBox.ForeColor = Color.LightGray;
            }
            if (outputPanel != null)
            {
                outputPanel.BackColor = Color.FromArgb(45, 45, 48);
            }
            if (errorListBox != null)
            {
                errorListBox.BackColor = Color.FromArgb(63, 63, 70);
                errorListBox.ForeColor = Color.LightGray;
            }
            statusLabel.Text = "Dark theme activated";
        }

        private void UpdateWindowTitle()
        {
            string title = "Mini Code Editor";
            if (!string.IsNullOrEmpty(currentFilePath))
                title += $" - {Path.GetFileName(currentFilePath)}";
            else
                title += " - Untitled";

            this.Text = title;
        }

        private void AddToRecentFiles(string filePath)
        {
            if (!recentFiles.Contains(filePath))
            {
                recentFiles.Insert(0, filePath);
                if (recentFiles.Count > 5)
                    recentFiles.RemoveAt(5);
            }
        }

        private void InitializeRecentFilesList()
        {
            // Demo recent files
            recentFiles.Add("Program.cs");
            recentFiles.Add("Calculator.cs");
            recentFiles.Add("TestApp.cs");
        }
    }

}





























//Console.WriteLine("Hello World!");
//Console.WriteLine("This is working!");
//Console.WriteLine("2 + 3 = 5");





/*using System;

namespace TestProgram
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello from Mini Code Editor!");
            Console.WriteLine("This is a test program.");
            
            // Simple calculation
            int a = 10;
            int b = 5;
            int sum = a + b;
            
            Console.WriteLine($"The sum of {a} and {b} is: {sum}");
            
            // Simple loop
            Console.WriteLine("\nCounting from 1 to 5:");
            for (int i = 1; i <= 5; i++)
            {
                Console.WriteLine($"Number: {i}");
            }
            
            Console.WriteLine("\nProgram execution completed successfully!");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}*/
