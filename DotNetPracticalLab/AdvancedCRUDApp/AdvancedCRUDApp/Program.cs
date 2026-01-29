using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using System.Diagnostics;

#nullable disable

namespace GenericCRUDApp
{
    // ============ CONFIGURATION CLASS - CHANGE THIS FOR ANY DOMAIN ============
    public static class AppConfig
    {
        // ========== CUSTOMIZE THESE FOR YOUR DOMAIN ==========
        public const string APP_TITLE = "Record Management System";
        public const string TABLE_NAME = "patients";
        public const string PRIMARY_KEY = "p_id";

        // Field Configuration: Label, DatabaseColumn, DataType, IsRequired, ValidationRegex (null if none)
        public static readonly FieldConfig[] FIELDS = new FieldConfig[]
        {
            new FieldConfig("Patient Name", "p_name", FieldType.Text, true, null, "Enter full name"),
            new FieldConfig("Report Type", "report", FieldType.Text, true, null, "Enter report type"),
            new FieldConfig("Report ID", "r_number", FieldType.Number, true, null, "Enter report number"),
            new FieldConfig("Value", "r_value", FieldType.Number, true, null, "Enter report type value"),
            new FieldConfig("Department Code", "d_code", FieldType.Text, true, @"^[A-Z]{2}-\d{4}$", "Format: XX-1234"),
            new FieldConfig("Date", "date", FieldType.Date, true, null, "Select date")
        };

        // Numeric field for calculations (use database column name)
        public const string NUMERIC_FIELD_FOR_CALC = "r_value";
        public const string CATEGORY_FIELD_FOR_CALC = "report";

        // Calculation multipliers based on category
        public static double GetMultiplier(string category)
        {
            return category.ToLower() switch
            {
                "type1" => 1.2,
                "type2" => 1.5,
                "type3" => 1.3,
                "type4" => 1.1,
                _ => 1.0
            };
        }

        // ========== PLACEHOLDERS - CONFIGURE THESE ==========
        public const string DB_SERVER = "localhost";
        public const string DB_NAME = "hospital_db";
        public const string DB_USER = "root";
        public const string DB_PASSWORD = "X1aoL4nhu@007";
        public const string LOGO_PATH = @"C:\Users\Annika Dubey\Downloads\logo.png";
        public const string EXPORT_FOLDER = @"C:\Users\Annika Dubey\Downloads\Export Folder";
        public const string HISTORY_FILE = @"C:\Users\Annika Dubey\Downloads\Export Folder\DeletedRecords.txt";
    }

    // ============ FIELD CONFIGURATION ============
    public enum FieldType { Text, Number, Date }

    public class FieldConfig
    {
        public string Label { get; set; }
        public string DbColumn { get; set; }
        public FieldType Type { get; set; }
        public bool Required { get; set; }
        public string ValidationRegex { get; set; }
        public string Tooltip { get; set; }

        public FieldConfig(string label, string dbColumn, FieldType type, bool required, string validationRegex, string tooltip)
        {
            Label = label;
            DbColumn = dbColumn;
            Type = type;
            Required = required;
            ValidationRegex = validationRegex;
            Tooltip = tooltip;
        }
    }

    // ============ CUSTOM EXCEPTIONS ============
    public class InvalidValueException : Exception
    {
        public InvalidValueException(string message) : base(message) { }
    }

    public class DuplicateCodeException : Exception
    {
        public DuplicateCodeException(string message) : base(message) { }
    }

    // ============ MAIN FORM ============
    public partial class MainForm : Form
    {
        private string connectionString;
        private DataGridView dgv;
        private Control[] fieldControls;
        private Button btnAdd, btnUpdate, btnDelete, btnClear, btnExport, btnHistory;
        private Label lblStatus;
        private Panel headerPanel, formPanel;
        private ToolTip tooltip;
        private PictureBox logo;
        private bool formLoaded = false;

        public MainForm()
        {
            // Set connection string first
            connectionString = $"Server={AppConfig.DB_SERVER};Database={AppConfig.DB_NAME};User ID={AppConfig.DB_USER};Password={AppConfig.DB_PASSWORD};";

            // Initialize components
            InitializeComponent();

            // Setup cursor
            SetupCustomCursor();

            // Set form loaded flag
            formLoaded = true;

            // Load data after form is shown
            this.Shown += (s, e) => LoadData();
        }

        private void InitializeComponent()
        {
            // Initialize ToolTip FIRST before using it
            tooltip = new ToolTip
            {
                IsBalloon = true,
                ToolTipIcon = ToolTipIcon.Info,
                AutoPopDelay = 5000,
                InitialDelay = 500,
                ReshowDelay = 100
            };

            this.Text = AppConfig.APP_TITLE;
            this.Size = new Size(1200, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(240, 242, 245);
            this.Font = new Font("Segoe UI", 9F);
            this.KeyPreview = true;
            this.KeyDown += MainForm_KeyDown;
            this.Icon = SystemIcons.Application;

            // Header Panel with Gradient
            headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 90,
                BackColor = Color.FromArgb(41, 128, 185)
            };
            headerPanel.Paint += HeaderPanel_Paint;

            // Logo
            logo = new PictureBox
            {
                Location = new Point(25, 20),
                Size = new Size(50, 50),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };

            try
            {
                if (System.IO.File.Exists(AppConfig.LOGO_PATH))
                {
                    logo.Image = Image.FromFile(AppConfig.LOGO_PATH);
                }
                else
                {
                    // Create a default logo
                    Bitmap bmp = new Bitmap(50, 50);
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.White);
                        g.FillEllipse(new SolidBrush(Color.FromArgb(52, 152, 219)), 5, 5, 40, 40);
                        g.DrawString("DB", new Font("Segoe UI", 14F, FontStyle.Bold), Brushes.White, 10, 12);
                    }
                    logo.Image = bmp;
                }
            }
            catch
            {
                logo.BackColor = Color.White;
            }
            headerPanel.Controls.Add(logo);

            // Title Label
            Label lblTitle = new Label
            {
                Text = AppConfig.APP_TITLE.ToUpper(),
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(90, 20),
                AutoSize = true
            };
            headerPanel.Controls.Add(lblTitle);

            // Subtitle
            Label lblSubtitle = new Label
            {
                Text = "Efficient Data Management with Advanced Features",
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = Color.FromArgb(236, 240, 241),
                Location = new Point(90, 55),
                AutoSize = true
            };
            headerPanel.Controls.Add(lblSubtitle);

            this.Controls.Add(headerPanel);

            // Form Panel with Shadow Effect
            formPanel = new Panel
            {
                Location = new Point(25, 115),
                Size = new Size(360, 560),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };
            formPanel.Paint += FormPanel_Paint;

            // Form Title
            Label lblFormTitle = new Label
            {
                Text = "📝 Record Details",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94),
                Location = new Point(15, 15),
                AutoSize = true
            };
            formPanel.Controls.Add(lblFormTitle);

            // Dynamically create fields
            fieldControls = new Control[AppConfig.FIELDS.Length];
            int y = 55;
            int spacing = 75;

            for (int i = 0; i < AppConfig.FIELDS.Length; i++)
            {
                var field = AppConfig.FIELDS[i];

                Label lbl = CreateLabel(field.Label + ":", 15, y);
                lbl.ForeColor = Color.FromArgb(52, 73, 94);
                formPanel.Controls.Add(lbl);

                Control control = null;
                switch (field.Type)
                {
                    case FieldType.Text:
                    case FieldType.Number:
                        TextBox txt = new TextBox
                        {
                            Location = new Point(15, y + 25),
                            Size = new Size(330, 28),
                            Font = new Font("Segoe UI", 10F),
                            Tag = field,
                            BorderStyle = BorderStyle.FixedSingle
                        };
                        txt.GotFocus += (s, e) => txt.BackColor = Color.FromArgb(255, 250, 205);
                        txt.LostFocus += (s, e) => txt.BackColor = Color.White;
                        control = txt;
                        break;

                    case FieldType.Date:
                        DateTimePicker dtp = new DateTimePicker
                        {
                            Location = new Point(15, y + 25),
                            Size = new Size(330, 28),
                            Format = DateTimePickerFormat.Short,
                            Font = new Font("Segoe UI", 10F),
                            Tag = field
                        };
                        control = dtp;
                        break;
                }

                if (control != null)
                {
                    formPanel.Controls.Add(control);
                    tooltip.SetToolTip(control, field.Tooltip);
                    fieldControls[i] = control;
                }

                y += spacing;
            }

            // Buttons Panel
            Panel buttonPanel = new Panel
            {
                Location = new Point(15, y + 10),
                Size = new Size(330, 100),
                BackColor = Color.Transparent
            };

            int btnY = 0;
            btnAdd = CreateStyledButton("➕ Add", 0, btnY, 100, Color.FromArgb(46, 204, 113), "F1");
            btnAdd.Click += BtnAdd_Click;
            buttonPanel.Controls.Add(btnAdd);

            btnUpdate = CreateStyledButton("✏️ Update", 115, btnY, 100, Color.FromArgb(52, 152, 219), "F2");
            btnUpdate.Click += BtnUpdate_Click;
            buttonPanel.Controls.Add(btnUpdate);

            btnDelete = CreateStyledButton("🗑️ Delete", 230, btnY, 100, Color.FromArgb(231, 76, 60), "F3");
            btnDelete.Click += BtnDelete_Click;
            buttonPanel.Controls.Add(btnDelete);

            btnY += 45;
            btnClear = CreateStyledButton("🔄 Clear", 0, btnY, 100, Color.FromArgb(149, 165, 166), "F4");
            btnClear.Click += BtnClear_Click;
            buttonPanel.Controls.Add(btnClear);

            btnExport = CreateStyledButton("💾 Export", 115, btnY, 100, Color.FromArgb(230, 126, 34), "F5");
            btnExport.Click += BtnExport_Click;
            buttonPanel.Controls.Add(btnExport);

            btnHistory = CreateStyledButton("📜 History", 230, btnY, 100, Color.FromArgb(155, 89, 182), "F6");
            btnHistory.Click += BtnHistory_Click;
            buttonPanel.Controls.Add(btnHistory);

            formPanel.Controls.Add(buttonPanel);
            this.Controls.Add(formPanel);

            // Initialize DataGridView
            InitializeDataGridView();

            // Status Bar
            Panel statusBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 35,
                BackColor = Color.FromArgb(236, 240, 241)
            };

            lblStatus = new Label
            {
                Location = new Point(25, 8),
                AutoSize = true,
                ForeColor = Color.FromArgb(52, 73, 94),
                Font = new Font("Segoe UI", 9F),
                Text = "⚡ Initializing..."
            };
            statusBar.Controls.Add(lblStatus);

            Label lblVersion = new Label
            {
                Text = "v1.0 | Made with ❤️",
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(127, 140, 141),
                Location = new Point(1050, 10),
                AutoSize = true
            };
            statusBar.Controls.Add(lblVersion);

            this.Controls.Add(statusBar);
        }

        private void InitializeDataGridView()
        {
            // Create DataGridView
            dgv = new DataGridView
            {
                Location = new Point(405, 115),
                Size = new Size(770, 560),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 40
            };

            // Configure styles
            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(52, 73, 94),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                Padding = new Padding(5)
            };

            dgv.DefaultCellStyle = new DataGridViewCellStyle
            {
                SelectionBackColor = Color.FromArgb(52, 152, 219),
                SelectionForeColor = Color.White,
                Font = new Font("Segoe UI", 9F),
                Padding = new Padding(5)
            };

            dgv.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(245, 247, 250)
            };

            dgv.CellClick += Dgv_CellClick;
            dgv.CellDoubleClick += Dgv_CellDoubleClick;

            // Add to form
            this.Controls.Add(dgv);
        }

        private void SetupCustomCursor()
        {
            this.Cursor = Cursors.Default;
        }

        private void HeaderPanel_Paint(object sender, PaintEventArgs e)
        {
            using (LinearGradientBrush brush = new LinearGradientBrush(
                headerPanel.ClientRectangle,
                Color.FromArgb(41, 128, 185),
                Color.FromArgb(109, 213, 250),
                LinearGradientMode.Horizontal))
            {
                e.Graphics.FillRectangle(brush, headerPanel.ClientRectangle);
            }
        }

        private void FormPanel_Paint(object sender, PaintEventArgs e)
        {
            Panel p = sender as Panel;
            // Draw shadow effect
            using (Pen pen = new Pen(Color.FromArgb(189, 195, 199), 1))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
            }
        }

        private Label CreateLabel(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94),
                AutoSize = true
            };
        }

        private Button CreateStyledButton(string text, int x, int y, int width, Color color, string shortcut)
        {
            Button btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 38),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TabStop = true
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(color, 0.3f);
            btn.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(color, 0.1f);

            tooltip.SetToolTip(btn, $"Click or press {shortcut}");

            return btn;
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            // Function key shortcuts
            switch (e.KeyCode)
            {
                case Keys.F1: BtnAdd_Click(null, null); break;
                case Keys.F2: BtnUpdate_Click(null, null); break;
                case Keys.F3: BtnDelete_Click(null, null); break;
                case Keys.F4: BtnClear_Click(null, null); break;
                case Keys.F5: BtnExport_Click(null, null); break;
                case Keys.F6: BtnHistory_Click(null, null); break;
            }

            // Alt shortcuts
            if (e.Alt)
            {
                for (int i = 0; i < AppConfig.FIELDS.Length; i++)
                {
                    char firstChar = char.ToUpper(AppConfig.FIELDS[i].Label[0]);
                    if (e.KeyCode == (Keys)Enum.Parse(typeof(Keys), firstChar.ToString()))
                    {
                        if (fieldControls[i] != null)
                        {
                            fieldControls[i].Focus();
                        }
                        break;
                    }
                }
            }
        }

        // ============ CRUD OPERATIONS ============
        private void LoadData()
        {
            if (!formLoaded || this.IsDisposed)
                return;

            try
            {
                // Update status before loading
                SafeUpdateStatus("⚡ Loading data...");

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = $"SELECT * FROM {AppConfig.TABLE_NAME} ORDER BY {AppConfig.PRIMARY_KEY} DESC";
                    MySqlDataAdapter adapter = new MySqlDataAdapter(query, conn);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    // Update UI on the main thread
                    this.Invoke(new Action(() =>
                    {
                        UpdateUIWithData(dt);
                    }));
                }
            }
            catch (MySqlException ex)
            {
                string errorMessage = $"Database Connection Error!\n\n{ex.Message}\n\nPlease check:\n• MySQL service is running\n• Database exists\n• Credentials are correct";
                this.Invoke(new Action(() => ShowError(errorMessage)));
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error loading data: {ex.Message}";
                this.Invoke(new Action(() => ShowError(errorMessage)));
            }
        }

        private void UpdateUIWithData(DataTable dt)
        {
            try
            {
                // Check if controls exist
                if (dgv == null || lblStatus == null || this.IsDisposed)
                    return;

                // Set DataSource
                dgv.DataSource = dt;

                // Update status
                SafeUpdateStatus($"⚡ Ready | Total Records: {dt.Rows.Count} | Press F1-F6 for quick actions");

                // Format columns
                if (dgv.Columns != null && dgv.Columns.Count > 0)
                {
                    foreach (DataGridViewColumn col in dgv.Columns)
                    {
                        if (col.Name == AppConfig.PRIMARY_KEY)
                        {
                            col.Width = 80;
                            col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log but don't crash
                Debug.WriteLine($"Error updating UI: {ex.Message}");
                SafeUpdateStatus($"⚠️ Error: {ex.Message}");
            }
        }

        private void SafeUpdateStatus(string message)
        {
            if (lblStatus != null && !lblStatus.IsDisposed && !this.IsDisposed)
            {
                lblStatus.Text = message;
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            try
            {
                ValidateInputs();

                // Arithmetic Computation 1: Calculate health/ratio
                double calculatedValue = PerformCalculation1();

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // Build dynamic INSERT query
                    string columns = string.Join(", ", AppConfig.FIELDS.Select(f => f.DbColumn));
                    string parameters = string.Join(", ", AppConfig.FIELDS.Select(f => "@" + f.DbColumn));
                    string query = $"INSERT INTO {AppConfig.TABLE_NAME} ({columns}) VALUES ({parameters})";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        AddParameters(cmd);
                        cmd.ExecuteNonQuery();
                    }
                }

                ShowSuccess($"✅ Record Added Successfully!\n\n📊 Calculated Health Ratio: {calculatedValue:F2}%");
                LoadData();
                ClearFields();
            }
            catch (InvalidValueException ex)
            {
                ShowWarning($"⚠️ Validation Error\n\n{ex.Message}");
            }
            catch (DuplicateCodeException ex)
            {
                ShowWarning($"⚠️ Duplicate Entry\n\n{ex.Message}");
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1062)
                    ShowError("❌ Duplicate Entry!\n\nThis record already exists in the database.");
                else
                    ShowError($"❌ Database Error\n\n{ex.Message}");
            }
            catch (FormatException)
            {
                ShowWarning("⚠️ Invalid Format\n\nPlease enter valid values for all fields!");
            }
            catch (Exception ex)
            {
                ShowError($"❌ Unexpected Error\n\n{ex.Message}");
            }
        }

        private void BtnUpdate_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgv == null || dgv.SelectedRows.Count == 0)
                {
                    ShowWarning("⚠️ No Selection\n\nPlease select a record from the table to update!");
                    return;
                }

                ValidateInputs();

                int recordId = Convert.ToInt32(dgv.SelectedRows[0].Cells[AppConfig.PRIMARY_KEY].Value);

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // Build dynamic UPDATE query
                    string setClause = string.Join(", ", AppConfig.FIELDS.Select(f => $"{f.DbColumn}=@{f.DbColumn}"));
                    string query = $"UPDATE {AppConfig.TABLE_NAME} SET {setClause} WHERE {AppConfig.PRIMARY_KEY}=@id";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", recordId);
                        AddParameters(cmd);
                        cmd.ExecuteNonQuery();
                    }
                }

                ShowSuccess("✅ Record Updated Successfully!");
                LoadData();
                ClearFields();
            }
            catch (InvalidValueException ex)
            {
                ShowWarning($"⚠️ Validation Error\n\n{ex.Message}");
            }
            catch (DuplicateCodeException ex)
            {
                ShowWarning($"⚠️ Duplicate Entry\n\n{ex.Message}");
            }
            catch (MySqlException ex)
            {
                ShowError($"❌ Database Error\n\n{ex.Message}");
            }
            catch (Exception ex)
            {
                ShowError($"❌ Error\n\n{ex.Message}");
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgv == null || dgv.SelectedRows.Count == 0)
                {
                    ShowWarning("⚠️ No Selection\n\nPlease select a record to delete!");
                    return;
                }

                var result = MessageBox.Show(
                    "⚠️ Are you sure you want to delete this record?\n\nThis action cannot be undone, but the record will be saved in history.",
                    "Confirm Deletion",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);

                if (result == DialogResult.Yes)
                {
                    int recordId = Convert.ToInt32(dgv.SelectedRows[0].Cells[AppConfig.PRIMARY_KEY].Value);
                    string deletedRecord = "";

                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();

                        // Get record details before deletion
                        string selectQuery = $"SELECT * FROM {AppConfig.TABLE_NAME} WHERE {AppConfig.PRIMARY_KEY}=@id";
                        using (MySqlCommand selectCmd = new MySqlCommand(selectQuery, conn))
                        {
                            selectCmd.Parameters.AddWithValue("@id", recordId);
                            using (MySqlDataReader reader = selectCmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    deletedRecord = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ID: {recordId} | ";
                                    foreach (var field in AppConfig.FIELDS)
                                    {
                                        deletedRecord += $"{field.Label}: {reader[field.DbColumn]} | ";
                                    }
                                    deletedRecord = deletedRecord.TrimEnd('|', ' ');
                                }
                            }
                        }

                        // Delete record
                        string deleteQuery = $"DELETE FROM {AppConfig.TABLE_NAME} WHERE {AppConfig.PRIMARY_KEY}=@id";
                        using (MySqlCommand deleteCmd = new MySqlCommand(deleteQuery, conn))
                        {
                            deleteCmd.Parameters.AddWithValue("@id", recordId);
                            deleteCmd.ExecuteNonQuery();
                        }
                    }

                    SaveDeletedRecord(deletedRecord);
                    ShowSuccess("✅ Record Deleted Successfully!\n\n📜 Record saved to deletion history.");
                    LoadData();
                    ClearFields();
                }
            }
            catch (Exception ex)
            {
                ShowError($"❌ Error Deleting Record\n\n{ex.Message}");
            }
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            ClearFields();
            ShowInfo("🔄 Fields Cleared!\n\nReady for new entry.");
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgv == null || dgv.Rows.Count == 0)
                {
                    ShowWarning("⚠️ No Data\n\nThere is no data to export!");
                    return;
                }

                Directory.CreateDirectory(AppConfig.EXPORT_FOLDER);
                string filename = Path.Combine(AppConfig.EXPORT_FOLDER, $"{AppConfig.TABLE_NAME}_Export_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

                using (StreamWriter sw = new StreamWriter(filename))
                {
                    sw.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
                    sw.WriteLine("║             " + AppConfig.APP_TITLE.ToUpper().PadRight(63) + "║");
                    sw.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
                    sw.WriteLine();
                    sw.WriteLine($"📅 Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    sw.WriteLine($"📊 Total Records: {dgv.Rows.Count}");
                    sw.WriteLine(new string('═', 80));
                    sw.WriteLine();

                    // Arithmetic Computation 2: Calculate total and average
                    double total = PerformCalculation2();

                    int recordNum = 1;
                    foreach (DataGridViewRow row in dgv.Rows)
                    {
                        sw.WriteLine($"┌─ Record #{recordNum} " + new string('─', 67));
                        foreach (var field in AppConfig.FIELDS)
                        {
                            string value = row.Cells[field.DbColumn].Value?.ToString() ?? "N/A";
                            sw.WriteLine($"│ {field.Label,-20}: {value}");
                        }
                        sw.WriteLine($"└" + new string('─', 79));
                        sw.WriteLine();
                        recordNum++;
                    }

                    sw.WriteLine(new string('═', 80));
                    sw.WriteLine("📈 STATISTICS");
                    sw.WriteLine(new string('═', 80));
                    sw.WriteLine($"Total Records      : {dgv.Rows.Count}");
                    sw.WriteLine($"Total {AppConfig.NUMERIC_FIELD_FOR_CALC,-12}: {total:N2}");
                    sw.WriteLine($"Average {AppConfig.NUMERIC_FIELD_FOR_CALC,-11}: {(dgv.Rows.Count > 0 ? total / dgv.Rows.Count : 0):F2}");
                    sw.WriteLine($"Max {AppConfig.NUMERIC_FIELD_FOR_CALC,-15}: {GetMaxValue():N2}");
                    sw.WriteLine($"Min {AppConfig.NUMERIC_FIELD_FOR_CALC,-15}: {GetMinValue():N2}");
                    sw.WriteLine(new string('═', 80));
                    sw.WriteLine();
                    sw.WriteLine("End of Report");
                }

                ShowSuccess($"💾 Export Successful!\n\nData exported to:\n{filename}\n\n📊 {dgv.Rows.Count} records exported");

                // Ask if user wants to open the file
                var openResult = MessageBox.Show("Would you like to open the exported file?", "Open File", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (openResult == DialogResult.Yes)
                {
                    System.Diagnostics.Process.Start("notepad.exe", filename);
                }
            }
            catch (Exception ex)
            {
                ShowError($"❌ Export Failed\n\n{ex.Message}");
            }
        }

        private void BtnHistory_Click(object sender, EventArgs e)
        {
            try
            {
                if (!File.Exists(AppConfig.HISTORY_FILE))
                {
                    ShowInfo("📜 No History\n\nNo deletion history found yet.");
                    return;
                }

                string content = File.ReadAllText(AppConfig.HISTORY_FILE);

                Form historyForm = new Form
                {
                    Text = "📜 Deletion History",
                    Size = new Size(1000, 650),
                    StartPosition = FormStartPosition.CenterParent,
                    BackColor = Color.FromArgb(240, 242, 245),
                    ShowIcon = false,
                    MinimizeBox = false,
                    MaximizeBox = true
                };

                Panel headerPanel = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 60,
                    BackColor = Color.FromArgb(155, 89, 182)
                };

                Label titleLabel = new Label
                {
                    Text = "📜 DELETION HISTORY",
                    Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                    ForeColor = Color.White,
                    Location = new Point(20, 15),
                    AutoSize = true
                };
                headerPanel.Controls.Add(titleLabel);

                TextBox txtHistory = new TextBox
                {
                    Multiline = true,
                    ScrollBars = ScrollBars.Both,
                    Location = new Point(20, 80),
                    Size = new Size(940, 480),
                    Text = content,
                    ReadOnly = true,
                    Font = new Font("Consolas", 9F),
                    BackColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle
                };

                Button btnClose = new Button
                {
                    Text = "Close",
                    Location = new Point(850, 575),
                    Size = new Size(110, 35),
                    BackColor = Color.FromArgb(155, 89, 182),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnClose.FlatAppearance.BorderSize = 0;
                btnClose.Click += (s, ev) => historyForm.Close();

                historyForm.Controls.Add(headerPanel);
                historyForm.Controls.Add(txtHistory);
                historyForm.Controls.Add(btnClose);
                historyForm.ShowDialog();
            }
            catch (Exception ex)
            {
                ShowError($"❌ Error Loading History\n\n{ex.Message}");
            }
        }

        private void Dgv_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dgv != null)
            {
                DataGridViewRow row = dgv.Rows[e.RowIndex];
                for (int i = 0; i < AppConfig.FIELDS.Length; i++)
                {
                    var field = AppConfig.FIELDS[i];
                    var control = fieldControls[i];

                    if (control is TextBox txt)
                        txt.Text = row.Cells[field.DbColumn].Value?.ToString() ?? "";
                    else if (control is DateTimePicker dtp)
                        dtp.Value = Convert.ToDateTime(row.Cells[field.DbColumn].Value);
                }

                SafeUpdateStatus($"📝 Record selected | ID: {row.Cells[AppConfig.PRIMARY_KEY].Value} | Click Update or Delete");
            }
        }

        private void Dgv_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                BtnUpdate_Click(null, null);
            }
        }

        // ============ VALIDATION & UTILITIES ============
        private void ValidateInputs()
        {
            for (int i = 0; i < AppConfig.FIELDS.Length; i++)
            {
                var field = AppConfig.FIELDS[i];
                var control = fieldControls[i];
                string value = GetControlValue(control);

                if (field.Required && string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException($"{field.Label} is required!");

                // Regex validation
                if (!string.IsNullOrEmpty(field.ValidationRegex))
                {
                    if (!Regex.IsMatch(value.Trim().ToUpper(), field.ValidationRegex))
                        throw new ArgumentException($"{field.Label} format is invalid!\n\nExpected format: {GetRegexExample(field.ValidationRegex)}");
                }

                // Number validation
                if (field.Type == FieldType.Number)
                {
                    int numValue = int.Parse(value);
                    if (numValue <= 0 || numValue > 1000000)
                        throw new InvalidValueException($"{field.Label} must be between 1 and 1,000,000!");
                }
            }

            // Check for duplicate code
            var codeField = AppConfig.FIELDS.FirstOrDefault(f => !string.IsNullOrEmpty(f.ValidationRegex));
            if (codeField != null)
            {
                int idx = Array.IndexOf(AppConfig.FIELDS, codeField);
                string code = GetControlValue(fieldControls[idx]).Trim().ToUpper();
                if (CheckDuplicateCode(code, codeField.DbColumn))
                    throw new DuplicateCodeException($"{codeField.Label} '{code}' already exists in the database!");
            }
        }

        private string GetRegexExample(string pattern)
        {
            if (pattern.Contains(@"\d{4}"))
                return "XX-1234 (2 letters, hyphen, 4 digits)";
            return "See tooltip for format";
        }

        private bool CheckDuplicateCode(string code, string columnName)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = $"SELECT COUNT(*) FROM {AppConfig.TABLE_NAME} WHERE {columnName} = @code";

                    if (dgv != null && dgv.SelectedRows.Count > 0)
                    {
                        int currentId = Convert.ToInt32(dgv.SelectedRows[0].Cells[AppConfig.PRIMARY_KEY].Value);
                        query += $" AND {AppConfig.PRIMARY_KEY} != @id";
                    }

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@code", code);
                        if (dgv != null && dgv.SelectedRows.Count > 0)
                            cmd.Parameters.AddWithValue("@id", Convert.ToInt32(dgv.SelectedRows[0].Cells[AppConfig.PRIMARY_KEY].Value));

                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        return count > 0;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        // Arithmetic Computation 1: Calculate ratio/efficiency
        private double PerformCalculation1()
        {
            try
            {
                var numField = AppConfig.FIELDS.FirstOrDefault(f => f.DbColumn == AppConfig.NUMERIC_FIELD_FOR_CALC);
                var catField = AppConfig.FIELDS.FirstOrDefault(f => f.DbColumn == AppConfig.CATEGORY_FIELD_FOR_CALC);

                if (numField == null || catField == null) return 0;

                int numIdx = Array.IndexOf(AppConfig.FIELDS, numField);
                int catIdx = Array.IndexOf(AppConfig.FIELDS, catField);

                int numValue = int.Parse(GetControlValue(fieldControls[numIdx]));
                string category = GetControlValue(fieldControls[catIdx]);

                double baseRatio = (numValue / 10000.0) * 100;
                double multiplier = AppConfig.GetMultiplier(category);

                return Math.Min(baseRatio * multiplier, 100);
            }
            catch
            {
                return 0;
            }
        }

        // Arithmetic Computation 2: Calculate total from DataGridView
        private double PerformCalculation2()
        {
            double total = 0;
            try
            {
                if (dgv == null) return 0;

                foreach (DataGridViewRow row in dgv.Rows)
                {
                    if (row.Cells[AppConfig.NUMERIC_FIELD_FOR_CALC].Value != null)
                    {
                        total += Convert.ToDouble(row.Cells[AppConfig.NUMERIC_FIELD_FOR_CALC].Value);
                    }
                }
            }
            catch { }
            return total;
        }

        private double GetMaxValue()
        {
            double max = 0;
            try
            {
                if (dgv == null) return 0;

                foreach (DataGridViewRow row in dgv.Rows)
                {
                    if (row.Cells[AppConfig.NUMERIC_FIELD_FOR_CALC].Value != null)
                    {
                        double val = Convert.ToDouble(row.Cells[AppConfig.NUMERIC_FIELD_FOR_CALC].Value);
                        if (val > max) max = val;
                    }
                }
            }
            catch { }
            return max;
        }

        private double GetMinValue()
        {
            double min = double.MaxValue;
            try
            {
                if (dgv == null) return 0;

                foreach (DataGridViewRow row in dgv.Rows)
                {
                    if (row.Cells[AppConfig.NUMERIC_FIELD_FOR_CALC].Value != null)
                    {
                        double val = Convert.ToDouble(row.Cells[AppConfig.NUMERIC_FIELD_FOR_CALC].Value);
                        if (val < min) min = val;
                    }
                }
            }
            catch { }
            return min == double.MaxValue ? 0 : min;
        }

        private void AddParameters(MySqlCommand cmd)
        {
            for (int i = 0; i < AppConfig.FIELDS.Length; i++)
            {
                var field = AppConfig.FIELDS[i];
                var control = fieldControls[i];
                object value = null;

                if (control is TextBox txt)
                {
                    value = field.Type == FieldType.Number ?
                        (object)int.Parse(txt.Text.Trim()) :
                        txt.Text.Trim().ToUpper();
                }
                else if (control is DateTimePicker dtp)
                {
                    value = dtp.Value.Date;
                }

                cmd.Parameters.AddWithValue("@" + field.DbColumn, value);
            }
        }

        private string GetControlValue(Control control)
        {
            if (control is TextBox txt)
                return txt.Text;
            else if (control is DateTimePicker dtp)
                return dtp.Value.ToString();
            return "";
        }

        private void SaveDeletedRecord(string record)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(AppConfig.HISTORY_FILE));
                File.AppendAllText(AppConfig.HISTORY_FILE, record + Environment.NewLine);
            }
            catch { }
        }

        private void ClearFields()
        {
            foreach (var control in fieldControls)
            {
                if (control is TextBox txt)
                    txt.Clear();
                else if (control is DateTimePicker dtp)
                    dtp.Value = DateTime.Now;
            }
            if (dgv != null)
                dgv.ClearSelection();
        }

        // ============ NOTIFICATION SYSTEM ============
        private void ShowSuccess(string message)
        {
            CustomMessageBox.Show(message, "Success", MessageBoxIcon.Information, Color.FromArgb(46, 204, 113));
        }

        private void ShowError(string message)
        {
            CustomMessageBox.Show(message, "Error", MessageBoxIcon.Error, Color.FromArgb(231, 76, 60));
        }

        private void ShowWarning(string message)
        {
            CustomMessageBox.Show(message, "Warning", MessageBoxIcon.Warning, Color.FromArgb(243, 156, 18));
        }

        private void ShowInfo(string message)
        {
            CustomMessageBox.Show(message, "Information", MessageBoxIcon.Information, Color.FromArgb(52, 152, 219));
        }
    }

    // ============ CUSTOM MESSAGE BOX ============
    public static class CustomMessageBox
    {
        public static void Show(string message, string title, MessageBoxIcon icon, Color headerColor)
        {
            Form popup = new Form
            {
                Size = new Size(450, 250),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.None,
                BackColor = Color.White,
                Padding = new Padding(0),
                ShowInTaskbar = false
            };

            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = headerColor
            };

            string iconText = icon switch
            {
                MessageBoxIcon.Information => "ℹ️",
                MessageBoxIcon.Warning => "⚠️",
                MessageBoxIcon.Error => "❌",
                _ => "📢"
            };

            Label lblIcon = new Label
            {
                Text = iconText,
                Font = new Font("Segoe UI", 20F),
                Location = new Point(15, 10),
                AutoSize = true
            };
            header.Controls.Add(lblIcon);

            Label lblTitle = new Label
            {
                Text = title,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Location = new Point(60, 13),
                AutoSize = true
            };
            header.Controls.Add(lblTitle);

            Label lblMessage = new Label
            {
                Text = message,
                Location = new Point(30, 70),
                Size = new Size(390, 120),
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(52, 73, 94)
            };

            Button btnOk = new Button
            {
                Text = "OK",
                Size = new Size(120, 40),
                Location = new Point(165, 195),
                BackColor = headerColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) => popup.Close();
            btnOk.MouseEnter += (s, e) => btnOk.BackColor = ControlPaint.Light(headerColor, 0.3f);
            btnOk.MouseLeave += (s, e) => btnOk.BackColor = headerColor;

            popup.Controls.Add(header);
            popup.Controls.Add(lblMessage);
            popup.Controls.Add(btnOk);

            popup.Paint += (s, e) =>
            {
                e.Graphics.DrawRectangle(new Pen(headerColor, 3), 0, 0, popup.Width - 1, popup.Height - 1);
            };

            popup.ShowDialog();
        }
    }

    // ============ PROGRAM ENTRY POINT ============
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}