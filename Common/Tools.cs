using Microsoft.Win32;
using Starfield_Tools.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Starfield_Tools.Common // Various functions used by the app
{
    internal class Tools
    {
        public static string CommonFolder { get; set; }
        public static string DocumentationFolder { get; set; }
        public static string LocalAppDataPath { get; set; }
        public string StarFieldPath { get; set; }
        public string StarfieldGamePath { get; set; }

        public string StarfieldGamePathMS { get; set; }
        public List<string> BethFiles { get; set; }
        public static string CatalogVersion { get; set; }
        public static string StarfieldAppData { get; set; }
        public List<string> PluginList { get; set; }

        public static readonly List<string> Suffixes = new List<string>
{
    " - main",
    " - textures",
    " - textures_xbox",
    " - voices_en",
    " - localization",
    " - shaders",
    " - voices_de",
    " - voices_en",
    " - voices_es",
    " - voices_fr",
    " - voices_ja"
};

        public static readonly List<string> LooseFolders = new List<string>
{ "meshes",
"interface",
"textures\\actors",
"textures\\architecture",
"textures\\clothes",
"textures\\common",
"textures\\decals",
"textures\\effects",
"textures\\interface",
"textures\\items",
"textures\\setdressing",
"textures\\ships",
"geometries",
"scripts",
"materials",
"sound" ,
"naf"
};

        public static readonly List<string> LooseFolderDirsOnly = new List<string>
        {
            "meshes",
"interface",
"textures",
"geometries",
"scripts",
"materials",
"sound" ,
"naf"
        };

        public Tools() // Constructor
        {
            CommonFolder = Path.Combine(Environment.CurrentDirectory, "Common"); // Used to read misc txt files used by the app

            LocalAppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Starfield_Tools");

            DocumentationFolder = Path.Combine(Environment.CurrentDirectory, "Documentation");

            try
            {
                BethFiles = new(File.ReadAllLines(Path.Combine(CommonFolder, "BGS Exclude.txt"))); // Exclude these files from Plugin list
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "BGS Exclude file missing. Repair or re-install the app", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                Environment.Exit(1);
            }

            try
            {
                CatalogVersion = File.ReadAllText(Path.Combine(CommonFolder, "Catalog Version.txt"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Catalog Version file missing. Repair or re-install the app", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                Environment.Exit(1);
            }
            try
            {
                StarfieldAppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Starfield");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Starfield AppData folder missing. Repair the game", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                Environment.Exit(1);
            }
        }

        public static List<string> BlockedMods()
        {
            try
            {
                if (!File.Exists(Path.Combine(LocalAppDataPath, "BlockedMods.txt")))
                {
                    File.Create(Path.Combine(LocalAppDataPath, "BlockedMods.txt"));
                    return null;
                }
                else
                    return (File.ReadAllLines(Path.Combine(LocalAppDataPath, "BlockedMods.txt")).ToList()); // Don't enable these mods
            }
            catch (Exception ex)
            {
#if DEBUG
                MessageBox.Show(ex.Message, "BlockedMods file missing. Repair or re-install the app", MessageBoxButtons.OK, MessageBoxIcon.Stop);
#endif
                return null;
            }
        }

        public static string MakeHeaderBlank() // Used to build ContentCatalog.txt header
        {
            string HeaderString = "";

            try
            {
                HeaderString = File.ReadAllText(Path.Combine(CommonFolder, "header.txt")); // Read the header from file
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Missing Header.txt file - unable to continue. Re-install or repair the tool");
                Application.Exit();
            }
            return HeaderString;
        }

        public static string MakeHeader() // Used to build ContentCatalog.txt header
        {
            string HeaderString = MakeHeaderBlank();
            HeaderString = HeaderString[..^5] + ",";
            return HeaderString;
        }

        public static void OpenUrl(string url) // Launch web browser from argument
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error opening URL", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }

        public static DateTime ConvertTime(double TimeToConvert) // Convert catalog time format to human readable
        {
            DateTime start = new(1970, 1, 1, 0, 0, 0, 0);
            try
            {
                start = start.AddSeconds(TimeToConvert);
            }
            catch
            {
                start = new(1970, 1, 1, 0, 0, 0, 0); // Return 1970 if error converting
            }
            return start;
        }

        public static string GetCatalogPath()
        {
            return StarfieldAppData + @"\ContentCatalog.txt";
        }

        public class Group // LOOT
        {
            public string name { get; set; }
            public List<string> after { get; set; }
        }

        public class Plugin // LOOT
        {
            public string name { get; set; }
            public string display { get; set; }
            public string group { get; set; }
            public List<string> after { get; set; }
            public List<string> inc { get; set; }
            public List<Req> req { get; set; }
            public List<Msg> msg { get; set; }
            public List<Url> url { get; set; }
        }

        public class Req // LOOT
        {
            public string name { get; set; }
            public string display { get; set; }
        }

        public class Msg // LOOT
        {
            public string type { get; set; }
            public string content { get; set; }
        }

        public class Url // LOOT
        {
            public string link { get; set; }
            public string name { get; set; }
        }

        public class Prelude// LOOT
        {
            public List<MessageAnchor> common { get; set; }
        }

        public class MessageAnchor// LOOT
        {
            public string type { get; set; }
            public string content { get; set; }
            public List<string> subs { get; set; }
            public string condition { get; set; }
        }

        public class Globals// LOOT
        {
            public string type { get; set; }
            public string content { get; set; }
            public List<string> subs { get; set; }
            public string condition { get; set; }
        }

        public class Configuration // LOOT
        {
            public Prelude prelude { get; set; }
            public List<string> bash_tags { get; set; }
            public List<Group> groups { get; set; }
            public List<Plugin> plugins { get; set; }
            public List<Plugin> common { get; set; }
            public List<Globals> globals { get; set; }
        }

        public class Creation // ContentCatalog.txt format
        {
            public bool AchievementSafe { get; set; }
            public string[] Files { get; set; }
            public long FilesSize { get; set; }
            public long Timestamp { get; set; }
            public string Title { get; set; }
            public string Version { get; set; }
        }

        public static bool FileCompare(string file1, string file2)
        {
            // If both file paths refer to the same file, no need to compare further.
            if (string.Equals(file1, file2, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            try
            {
                // Read the entire contents of each file into byte arrays.
                byte[] file1Bytes = File.ReadAllBytes(file1);
                byte[] file2Bytes = File.ReadAllBytes(file2);

                // Quick check: if the lengths differ, the files are not identical.
                if (file1Bytes.Length != file2Bytes.Length)
                {
                    return false;
                }

                // compare byte-by-byte.
                for (int i = 0; i < file1Bytes.Length; i++)
                {
                    if (file1Bytes[i] != file2Bytes[i])
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
#if DEBUG
        MessageBox.Show("Tools.FileCompare error: " + ex.Message);
#endif
                return false;
            }
        }

        public static void CheckGame() // Check if Starfield appdata folder exists
        {
            if (!Directory.Exists(StarfieldAppData))
            {
                MessageBox.Show("Unable to continue. Is Starfield installed correctly?", "Starfield AppData directory not found",
                    MessageBoxButtons.OK, MessageBoxIcon.Stop);
                Environment.Exit(1);
            }
        }

        public static void ShowAbout()
        {
            Form AboutBox = new frmAbout();
            Rectangle resolution = Screen.PrimaryScreen.Bounds;
            int screenWidth = resolution.Width;
            int screenHeight = resolution.Height;
            AboutBox.Width = screenWidth / 2;
            AboutBox.Height = screenHeight / 2;
            AboutBox.StartPosition = FormStartPosition.CenterScreen;
            AboutBox.Show();
        }

        public string SetStarfieldGamePath() // Prompt for Starfield game path
        {
            MessageBox.Show("Please select the path to the game installation folder where Starfield.exe is located", "Select Game Path",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                if (!string.IsNullOrEmpty(frmLoadOrder.StarfieldGamePath))
                    openFileDialog.InitialDirectory = frmLoadOrder.StarfieldGamePath;
                else
                {
                    openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                }

                openFileDialog.Title = "Set the path to the Starfield executable - Starfield.exe";
                openFileDialog.FileName = "Starfield.exe";
                openFileDialog.Filter = "Starfield.exe|Starfield.exe";

                if (openFileDialog.ShowDialog() != DialogResult.OK)
                {
                    return string.Empty;
                }

                string selectedFile = openFileDialog.FileName;
                if (!File.Exists(selectedFile))
                {
                    MessageBox.Show("Starfield.exe not found in the selected path",
                        "Please select the correct folder", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return string.Empty;
                }

                StarfieldGamePath = Path.GetDirectoryName(selectedFile);
                Settings.Default.StarfieldGamePath = StarfieldGamePath;
                Settings.Default.Save();
                return StarfieldGamePath;
            }
        }

        public string SetStarfieldGamePathMS()
        {
            string selectedPath = Properties.Settings.Default.GamePathMS;
            MessageBox.Show("Please select the path to the game installation folder where Starfield.exe is located", "Select Game Path", MessageBoxButtons.OK, MessageBoxIcon.Information);

            using (FolderBrowserDialog folderBrowserDialog = new())
            {
                folderBrowserDialog.InitialDirectory = selectedPath;
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    selectedPath = folderBrowserDialog.SelectedPath;
                    StarfieldGamePathMS = selectedPath;
                    Settings.Default.GamePathMS = selectedPath;
                    Settings.Default.Save();
                    return selectedPath;
                }
                else
                    return "";
            }
        }

        public static bool StartStarfieldCustom() // Start game with custom exe
        {
            string cmdLine = Properties.Settings.Default.CustomEXE;
            if (cmdLine == null)
                return false;

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = cmdLine,
                    WorkingDirectory = Properties.Settings.Default.StarfieldGamePath,
                    UseShellExecute = false //
                };
                Process.Start(startInfo);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + cmdLine, "Error");
                return false;
            }
        }

        public static bool StartStarfieldSFSE() // Start game with SFSE loader
        {
            string cmdLine = Path.Combine(Properties.Settings.Default.StarfieldGamePath, "sfse_loader.exe");
            if (cmdLine == null)
                return false;

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = cmdLine,
                    WorkingDirectory = Properties.Settings.Default.StarfieldGamePath,
                    UseShellExecute = false //
                };
                Process.Start(startInfo);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + cmdLine, "Error");
                return false;
            }
        }

        public static bool StartStarfieldMS() // Start game with MS Store version
        {
            //string cmdLine = @"shell:AppsFolder\BethesdaSoftworks.ProjectGold_3275kfvn8vcwc!Game";
            string cmdLine = Path.Combine(Properties.Settings.Default.GamePathMS, "Starfield.exe");

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = cmdLine,
                    WorkingDirectory = Properties.Settings.Default.GamePathMS,
                    UseShellExecute = false //
                };
                Process.Start(startInfo);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + cmdLine, "Error");
                return false;
            }
        }

        public static bool StartGame(int GameVersion) // Determine which version of the game to start
        {
            return GameVersion switch
            {
                0 => StartStarfieldSteam(),
                1 => StartStarfieldMS(),
                2 => StartStarfieldCustom(),
                3 => StartStarfieldSFSE(),
                _ => false // Default case for invalid GameVersion values
            };
        }

        public static bool StartStarfieldSteam() // Start game with Steam version
        {
            const string userRoot = "HKEY_CURRENT_USER";
            const string subkey = @"Software\Valve\Steam";
            const string keyName = userRoot + "\\" + subkey;

            try
            {
                string stringValue = (string)Registry.GetValue(keyName, "SteamExe", ""); // Get Steam path from Registry
                var processInfo = new ProcessStartInfo(stringValue, "-applaunch 1716740");
                var process = Process.Start(processInfo);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Unable to start game");
                return false;
            }
        }

        public static DialogResult ConfirmAction(string ActionText, string ActionTitle = "",
     MessageBoxButtons buttons = MessageBoxButtons.OKCancel,
     MessageBoxIcon icon = MessageBoxIcon.Stop,
     bool overRide = false) // overRide - always show dialog
        {
            if (frmLoadOrder.NoWarn && !overRide)
                return DialogResult.OK;

            return MessageBox.Show(ActionText, ActionTitle, buttons, icon);
        }

        public List<string> GetPluginList() // Get list of plugins from Starfield Data folder
        {
            try
            {
                string dataPath = Path.Combine(frmLoadOrder.StarfieldGamePath, "Data");
                return Directory.EnumerateFiles(dataPath, "*.esm", SearchOption.TopDirectoryOnly)
                                .Select(Path.GetFileName)
                                .Where(fileName => !BethFiles.Contains(fileName))
                                .ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error reading plugins", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return new List<string>();
            }
        }

        public static List<string> BGSArchives()
        {
            List<string> bgsArchives = new();
            using (StreamReader sr = new StreamReader(Path.Combine(CommonFolder, "BGS Archives.txt")))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    bgsArchives.Add(line);
                }
            }
            return bgsArchives;
        }

        public static void OpenFolder(string folder)
        {
            Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
        }

        public class ActivityLog
        {
            private readonly string logFilePath;

            public ActivityLog(string filePath)
            {
                logFilePath = filePath;
            }

            public void WriteLog(string message)
            {
                try
                {
                    // Insert message at the top of the file
                    string[] existingLines = File.Exists(logFilePath) ? File.ReadAllLines(logFilePath) : new string[0];
                    List<string> updatedLines = new List<string> { DateTime.Now.ToString() + ": " + message }; // Prepend the new entry
                    updatedLines.AddRange(existingLines);
                    File.WriteAllLines(logFilePath, updatedLines);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error writing to log file: {ex.Message}");
                }
            }

            public string ReadLog()
            {
                try
                {
                    using (StreamReader reader = new StreamReader(logFilePath))
                    {
                        return reader.ReadToEnd();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error reading log file: {ex.Message}");
                    return string.Empty;
                }
            }

            public void DeleteLog()
            {
                try
                {
                    if (File.Exists(logFilePath))
                    {
                        File.Delete(logFilePath);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting log file: {ex.Message}");
                }
            }
        }
    }
}