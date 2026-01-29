using System;
using System.IO;
using System.Text;
using System.Linq;

namespace NavalSonarClassifier
{
    class Program
    {
        static string currentFilePath = "";
        static StringBuilder buffer = new StringBuilder();
        static StringBuilder clipboard = new StringBuilder();
        static bool isDirty = false;

        static void Main()
        {
            Console.Title = "NavalSonarClassifier - SONAR Console Editor";
            SetDarkMode();

            while (true)
            {
                ShowMenu();
                HandleMenuChoice();
            }
        }

        // ================= MENU =================

        static void ShowMenu()
        {
            Console.Clear();
            Console.WriteLine("================================================");
            Console.WriteLine(" NAVAL SONAR CLASSIFIER - CONSOLE TEXT EDITOR");
            Console.WriteLine("================================================\n");

            Console.WriteLine("1. New File");
            Console.WriteLine("2. Open File");
            Console.WriteLine("3. View Content");
            Console.WriteLine("4. Edit / Append Text");
            Console.WriteLine("5. Save File");
            Console.WriteLine("6. Rename File (Save As)");
            Console.WriteLine("7. Insert Timestamp");
            Console.WriteLine("8. Insert SONAR Template");
            Console.WriteLine("9. Find & Replace");
            Console.WriteLine("10. Log Statistics");
            Console.WriteLine("11. Copy (line range)");
            Console.WriteLine("12. Cut (line range)");
            Console.WriteLine("13. Paste");
            Console.WriteLine("14. Theme Settings");
            Console.WriteLine("0. Exit\n");

            Console.WriteLine("Current File: " +
                (string.IsNullOrEmpty(currentFilePath) ? "None" : currentFilePath));

            if (isDirty)
                Console.WriteLine("⚠ Unsaved changes present");

            Console.Write("\nEnter choice: ");
        }

        static void HandleMenuChoice()
        {
            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1": NewFile(); break;
                case "2": OpenFile(); break;
                case "3": ViewContent(); break;
                case "4": EditContent(); break;
                case "5": SaveFile(); break;
                case "6": RenameFile(); break;
                case "7": InsertTimestamp(); break;
                case "8": SonarTemplate(); break;
                case "9": FindReplace(); break;
                case "10": LogStatistics(); break;
                case "11": Copy(); break;
                case "12": Cut(); break;
                case "13": Paste(); break;
                case "14": ThemeMenu(); break;
                case "0": ExitApp(); break;
                default: Message("Invalid choice."); break;
            }
        }

        // ================= FILE OPS =================

        static void NewFile()
        {
            WarnUnsaved();
            Console.Write("Enter new filename: ");
            string name = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(name)) return;

            string downloads = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";
            currentFilePath = Path.Combine(downloads, name);

            buffer.Clear();
            File.WriteAllText(currentFilePath, "");
            isDirty = false;

            Message("New file created.");
        }

        static void OpenFile()
        {
            WarnUnsaved();
            Console.Write("Enter full file path: ");
            string path = Console.ReadLine();

            if (!File.Exists(path))
            {
                Message("File not found.");
                return;
            }

            buffer.Clear();
            buffer.Append(File.ReadAllText(path));
            currentFilePath = path;
            isDirty = false;

            Message("File opened.");
        }

        static void SaveFile()
        {
            if (string.IsNullOrEmpty(currentFilePath))
            {
                Message("No file open.");
                return;
            }

            File.WriteAllText(currentFilePath, buffer.ToString());
            isDirty = false;
            Message("File saved.");
        }

        static void RenameFile()
        {
            if (string.IsNullOrEmpty(currentFilePath))
            {
                Message("No file to rename.");
                return;
            }

            Console.Write("Enter new filename: ");
            string newName = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(newName)) return;

            string dir = Path.GetDirectoryName(currentFilePath);
            string newPath = Path.Combine(dir, newName);

            File.WriteAllText(newPath, buffer.ToString());
            File.Delete(currentFilePath);

            currentFilePath = newPath;
            isDirty = false;

            Message("File renamed successfully.");
        }

        // ================= EDIT =================

        static void ViewContent()
        {
            Console.Clear();
            Console.WriteLine("----- FILE CONTENT START -----\n");
            Console.WriteLine(buffer.ToString());
            Console.WriteLine("\n----- FILE CONTENT END -----");
            Pause();
        }

        static void EditContent()
        {
            Console.WriteLine("Enter text (type END on new line to finish):");
            while (true)
            {
                string line = Console.ReadLine();
                if (line == "END") break;
                buffer.AppendLine(line);
                isDirty = true;
            }
        }

        static void FindReplace()
        {
            Console.Write("Find: ");
            string find = Console.ReadLine();
            Console.Write("Replace with: ");
            string replace = Console.ReadLine();

            buffer.Replace(find, replace);
            isDirty = true;
            Message("Find & Replace completed.");
        }

        // ================= CLIPBOARD (FIXED) =================

        static void Copy()
        {
            if (buffer.Length == 0)
            {
                Message("Nothing to copy.");
                return;
            }

            string[] lines = buffer.ToString().Split('\n');

            if (!ReadLineRange(lines.Length, out int start, out int end))
                return;

            clipboard.Clear();
            for (int i = start; i <= end; i++)
                clipboard.AppendLine(lines[i]);

            Message("Copied to clipboard.");
        }

        static void Cut()
        {
            if (buffer.Length == 0)
            {
                Message("Nothing to cut.");
                return;
            }

            string[] lines = buffer.ToString().Split('\n');

            if (!ReadLineRange(lines.Length, out int start, out int end))
                return;

            clipboard.Clear();
            for (int i = start; i <= end; i++)
                clipboard.AppendLine(lines[i]);

            var remaining = lines.Where((_, idx) => idx < start || idx > end);
            buffer.Clear();
            buffer.Append(string.Join("\n", remaining));
            isDirty = true;

            Message("Cut completed.");
        }

        static void Paste()
        {
            if (clipboard.Length == 0)
            {
                Message("Clipboard empty.");
                return;
            }

            buffer.Append(clipboard);
            isDirty = true;
            Message("Pasted from clipboard.");
        }

        static bool ReadLineRange(int maxLines, out int start, out int end)
        {
            start = end = -1;

            Console.Write($"Enter start line (1–{maxLines}): ");
            if (!int.TryParse(Console.ReadLine(), out start)) return false;

            Console.Write("Enter end line: ");
            if (!int.TryParse(Console.ReadLine(), out end)) return false;

            start--; end--;

            if (start < 0 || end >= maxLines || start > end)
            {
                Message("Invalid line range.");
                return false;
            }

            return true;
        }

        // ================= ADVANCED =================

        static void InsertTimestamp()
        {
            buffer.AppendLine("[TIMESTAMP] " + DateTime.Now);
            isDirty = true;
            Message("Timestamp inserted.");
        }

        static void SonarTemplate()
        {
            buffer.AppendLine("---- SONAR CONTACT REPORT ----");
            buffer.AppendLine("Object Type     : ");
            buffer.AppendLine("Range (km)      : ");
            buffer.AppendLine("Bearing (deg)   : ");
            buffer.AppendLine("Threat Level    : ");
            buffer.AppendLine("Operator Notes  : ");
            buffer.AppendLine("--------------------------------");
            isDirty = true;

            Message("SONAR template inserted.");
        }

        static void LogStatistics()
        {
            int lines = buffer.ToString().Split('\n').Length;
            int words = buffer.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

            Console.WriteLine($"Lines: {lines}");
            Console.WriteLine($"Words: {words}");
            Pause();
        }

        // ================= THEMES =================

        static void ThemeMenu()
        {
            Console.WriteLine("1. Light Mode");
            Console.WriteLine("2. Dark Mode");
            Console.WriteLine("3. SONAR Alert Mode");
            Console.Write("Choose: ");

            string c = Console.ReadLine();
            if (c == "1") SetLightMode();
            else if (c == "2") SetDarkMode();
            else if (c == "3") SetAlertMode();
        }

        static void SetLightMode()
        {
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.Clear();
        }

        static void SetDarkMode()
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Clear();
        }

        static void SetAlertMode()
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Clear();
        }

        // ================= UTILS =================

        static void WarnUnsaved()
        {
            if (!isDirty) return;

            Console.Write("Unsaved changes detected. Continue? (y/n): ");
            if (Console.ReadLine()?.ToLower() != "y")
                throw new OperationCanceledException();
        }

        static void ExitApp()
        {
            WarnUnsaved();
            Environment.Exit(0);
        }

        static void Message(string msg)
        {
            Console.WriteLine("\n" + msg);
            Pause();
        }

        static void Pause()
        {
            Console.WriteLine("\nPress ENTER to continue...");
            Console.ReadLine();
        }
    }
}
