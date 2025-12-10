using hstCMM.Properties;
using Microsoft.Win32;
using Narod.SteamGameFinder;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace hstCMM.Shared // Various functions used by the app
{
    internal class Tools
    {
        public static readonly List<string> LooseFolderDirsOnly =
        [
            "meshes",
"interface",
"textures",
"geometries",
"scripts",
"materials",
"sound" ,
"naf"
        ];

        public static readonly List<string> LooseFolders =
[ "meshes",
"interface",
"textures\\actors",
"textures\\architecture",
"textures\\clothes",
"textures\\common",
"textures\\displayscreens",
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
];

        public static readonly List<string> Suffixes =
[
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
];

        public Tools() // Constructor
        {
            try
            {
                BethFiles = new(File.ReadAllLines(Path.Combine(CommonFolder,
                    GameLibrary.GetById(Properties.Settings.Default.Game).ExcludeFile + " Exclude.txt"))); // Exclude these files from Plugin list
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Exclude file missing. Repair or re-install the app", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                File.WriteAllText(Path.Combine(CommonFolder, GameLibrary.GetById(Properties.Settings.Default.Game).ExcludeFile + " Exclude.txt"), string.Empty);
                BethFiles = new(File.ReadAllLines(Path.Combine(CommonFolder,
                    GameLibrary.GetById(Properties.Settings.Default.Game).ExcludeFile + " Exclude.txt"))); // Exclude these files from Plugin list
                //Environment.Exit(1);
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
                GameAppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    GameLibrary.GetById(Properties.Settings.Default.Game).AppData);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, $"{GameName} AppData folder missing. Repair the game", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                Environment.Exit(1);
            }
        }

        public static string CatalogVersion { get; set; }
        public static string CommonFolder { get; set; } = Path.Combine(Environment.CurrentDirectory, "Common"); // Used to read misc txt files used by the app
        public static string DocumentationFolder { get; set; } = Path.Combine(Environment.CurrentDirectory, "Documentation");
        public static string GameAppData { get; set; }
        public static string GameName { get; set; } = GameLibrary.GetById(Properties.Settings.Default.Game).GameName;
        public static string LocalAppDataPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "hstCMM");

        public List<string> BethFiles { get; set; }
        public string GameApp { get; set; } = GameLibrary.GetById(Properties.Settings.Default.Game).AppData;
        public string GamePath { get; set; }
        public string GamePathMS { get; set; }
        public List<string> PluginList { get; set; }

        public static List<string> BlockedMods()
        {
            try
            {
                if (!File.Exists(Path.Combine(LocalAppDataPath, "BlockedMods.txt")))
                {
                    File.Create(Path.Combine(LocalAppDataPath, "BlockedMods.txt")).Close();
                    return null;
                }
                else
                    return (File.ReadAllLines(Path.Combine(LocalAppDataPath, "BlockedMods.txt")).ToList()); // Don't enable these mods
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static bool CheckGame() // Check if Game appdata folder exists, true if it does
        {
            if (!Directory.Exists(GameAppData))
            {
                return false;
            }
            else
                return true;
        }

        public static DialogResult ConfirmAction(string ActionText, string ActionTitle = "",
            MessageBoxButtons buttons = MessageBoxButtons.OKCancel, MessageBoxIcon icon = MessageBoxIcon.Stop,
            bool overRide = false) // overRide - always show dialog
        {
            if (frmLoadOrder.NoWarn && !overRide)
                return DialogResult.OK;

            return MessageBox.Show(ActionText, ActionTitle, buttons, icon);
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
                return false;
            }
        }

        public static string GetCatalogPath()
        {
            return Path.Combine(GameAppData, "ContentCatalog.txt");
        }

        public static string MakeHeader() // Used to build ContentCatalog.txt header
        {
            string HeaderString = MakeHeaderBlank();
            HeaderString = HeaderString[..^5] + ",";
            return HeaderString;
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

        public static void OpenFile(string file) // Used to open misc text files
        {
            if (File.Exists(file))
            {
                Process.Start(new ProcessStartInfo(file) { UseShellExecute = true });
            }
            else
            {
                MessageBox.Show("File not found: " + file, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static void OpenFolder(string folder) // Used to open misc folders in Explorer
        {
            Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
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

        public static void ShowAbout()
        {
            Form AboutBox = new frmAbout();
            Rectangle resolution = Screen.PrimaryScreen.Bounds;
            int screenWidth = resolution.Width;
            int screenHeight = resolution.Height;
            AboutBox.Width = screenWidth / 2;
            AboutBox.Height = screenHeight / 2;
            AboutBox.StartPosition = FormStartPosition.CenterScreen;
            AboutBox.ShowAsync();
        }

        public static bool StartGame(int GameVersion) // Determine which version of the game to start
        {
            return GameVersion switch
            {
                0 => StartGameSteam(),
                1 => StartGameMS(),
                2 => StartGameCustom(),
                3 => StartGameSFSE(),
                _ => false // Default case for invalid GameVersion values
            };
        }

        public static bool StartGameCustom() // Start game with custom exe
        {
            string cmdLine = Properties.Settings.Default.CustomEXE;
            if (cmdLine is null)
                return false;

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = cmdLine,
                    WorkingDirectory = Properties.Settings.Default.GamePath,
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

        public static bool StartGameMS() // Start game with MS Store version
        {
            //string cmdLine = @"shell:AppsFolder\BethesdaSoftworks.ProjectGold_3275kfvn8vcwc!Game";
            string cmdLine = Path.Combine(Properties.Settings.Default.GamePathMS, $"{GameName}.exe");

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

        public static bool StartGameSFSE() // Start game with SFSE loader
        {
            string cmdLine = Path.Combine(Properties.Settings.Default.GamePath, "sfse_loader.exe");
            if (cmdLine is null)
                return false;

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = cmdLine,
                    WorkingDirectory = Properties.Settings.Default.GamePath,
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

        public static bool StartGameSteam() // Start game with Steam version
        {
            const string userRoot = "HKEY_CURRENT_USER";
            const string subkey = @"Software\Valve\Steam";
            const string keyName = userRoot + "\\" + subkey;

            try
            {
                string stringValue = (string)Registry.GetValue(keyName, "SteamExe", ""); // Get Steam path from Registry
                SteamGameLocator steamGameLocator = new();
                string gameID = steamGameLocator.getGameInfoByName(GameName).steamGameID;
                var processInfo = new ProcessStartInfo(stringValue, $"-applaunch {gameID}");
                var process = Process.Start(processInfo);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Unable to start game");
                return false;
            }
        }

        public string AppName()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            // Retrieve the AssemblyTitle attribute
            AssemblyTitleAttribute titleAttribute = assembly
                .GetCustomAttribute<AssemblyTitleAttribute>();

            // Print the title (or fallback to assembly name if not set)
            return titleAttribute != null ? titleAttribute.Title : assembly.GetName().Name;
        }

        public List<string> BGSArchives()
        {
            List<string> bgsArchives = new();
            using (StreamReader sr = new StreamReader(Path.Combine(CommonFolder, Tools.GameLibrary.GetById(Properties.Settings.Default.Game).ExcludeFile + " Archives.txt")))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    bgsArchives.Add(line.ToLower());
                }
            }
            return bgsArchives;
        }

        public bool DetectVortex()
        {
            string keyName = @"HKEY_CLASSES_ROOT\nxm\shell\open\command";
            string valueName = ""; // Default value

            if (Properties.Settings.Default.VortexPath == "")
            {
                // Read the registry value
                object value = Registry.GetValue(keyName, valueName, null);
                if (value != null)
                {
                    int startIndex = value.ToString().IndexOf('"') + 1;
                    int endIndex = value.ToString().IndexOf('"', startIndex);

                    if (startIndex > 0 && endIndex > startIndex)
                    {
                        string extracted = value.ToString()[startIndex..endIndex];
                        Properties.Settings.Default.VortexPath = extracted;
                        SaveSettings();
                        //vortexToolStripMenuItem.Visible = true;
                        return true;
                    }
                }
            }
            return false;
        }

        public List<string> GetPluginList(int Game) // Get list of plugins from game Data folder
        {
            string dataPath = Path.Combine(frmLoadOrder.GamePath, "Data");
            List<string> plugins = new();
            string[] patterns;
            try
            {
                if (Game == 0) // Starfield or possibly future BGS games without ESL support
                    patterns = new[] { "*.esm", "*.esp" };
                else
                    patterns = new[] { "*.esm", "*.esl", "*.esp" };
                foreach (var pattern in patterns)
                {
                    var modFiles = Directory.EnumerateFiles(dataPath, pattern, SearchOption.TopDirectoryOnly)
                                    .Select(Path.GetFileName)
                                    .Where(fileName => !BethFiles.Contains(fileName))
                                    .ToList();
                    foreach (var item in modFiles)
                        plugins.Add(item);
                }

                return plugins;
            }
            catch (Exception ex)
            {
                return new List<string>();
            }
        }

        public string GetSteamGamePath(string gameName)
        {
            try
            {
                SteamGameLocator steamGameLocator = new SteamGameLocator();

                Properties.Settings.Default.GamePath = GamePath =
                steamGameLocator.getGameInfoByName(gameName).steamGameLocation.Replace(@"\\", @"\");

                SaveSettings();
                return GamePath;
            }
            catch (Exception ex)
            {
                //LogError(ex.Message);
                return string.Empty;
            }
        }

        public void RestartApp()
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = Application.ExecutablePath,
                Arguments = "-dev",
                UseShellExecute = true
            };
            MessageBox.Show("Click OK to restart the app.", "Restart Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Process.Start(psi);
            Environment.Exit(0); // Ensure graceful shutdown
        }

        public string SetGamePath() // Prompt for game path
        {
            using System.Windows.Forms.OpenFileDialog openFileDialog = new();
            if (!string.IsNullOrEmpty(Properties.Settings.Default.GamePath))
                openFileDialog.InitialDirectory = Properties.Settings.Default.GamePath;
            else
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            openFileDialog.Title = $"Set the path to the {GameName} executable - {GameName}.exe";
            openFileDialog.FileName = $"{GameName}.exe";
            openFileDialog.Filter = $"{GameName}.exe|{GameName}.exe";

            if (openFileDialog.ShowDialog() != DialogResult.OK)
            {
                return string.Empty;
            }

            string selectedFile = openFileDialog.FileName;
            if (!File.Exists(selectedFile))
            {
                MessageBox.Show($"{GameName}.exe not found in the selected path",
                    "Please select the correct folder", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return string.Empty;
            }

            GamePath = Path.GetDirectoryName(selectedFile);
            Settings.Default.GamePath = GamePath;
            Settings.Default.Save();
            return GamePath;
        }

        public string SetGamePathMS()
        {
            string selectedPath = Properties.Settings.Default.GamePathMS;
            MessageBox.Show($"Please select the path to the game installation folder where {GameName}.exe is located",
                "Select Game Path - Choose the Content Folder", MessageBoxButtons.OK, MessageBoxIcon.Information);

            using (FolderBrowserDialog folderBrowserDialog = new())
            {
                folderBrowserDialog.InitialDirectory = selectedPath;
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    selectedPath = folderBrowserDialog.SelectedPath;
                    GamePathMS = selectedPath;
                    Settings.Default.GamePathMS = selectedPath;
                    Settings.Default.Save();
                    return selectedPath;
                }
                else
                    return "";
            }
        }

        private void SaveSettings()
        {
            Properties.Settings.Default.Save();
        }

        public static class GameLibrary
        {
            public static readonly List<GameInfo> Games = new()
            {
                new GameInfo(0, "Starfield", "Starfield","Starfield","Starfield","Starfield.exe", ModFiles.NewModFormat,
                    ModArchives.NewArchiveFormat,"2722710",3,"Starfield"),
                new GameInfo(1, "The Elder Scrolls V: Skyrim Special Edition", "Skyrim Special Edition","Skyrim SE",
                    "Skyrim Special Edition","SkyrimSe.exe", ModFiles.OldModFormat, ModArchives.OldArchiveFormat,"1946180",5,"Skyrim"),
                new GameInfo(2, "Fallout 4", "Fallout4","Fallout 4","Fallout 4","Fallout4.exe", ModFiles.OldModFormat,
                    ModArchives.OldArchiveFormat,"1946160",5,"Fallout4"),
                new GameInfo(3, "Elder Scrolls 6", "ES6","ES6","ES6","ES6.exe", ModFiles.NewModFormat,
                    ModArchives.NewArchiveFormat,"Unknown",3,"ES6"),
                new GameInfo(4, "Fallout 5", "Fallout5","Fallout 5","Fallout 5","Fallout5.exe", ModFiles.NewModFormat,
                    ModArchives.NewArchiveFormat,"Unknown", 3,"Fallout5")
            };

            public static GameInfo GetByExecutable(string exeName) =>
                            Games.FirstOrDefault(g => g.Executable.Equals(exeName, StringComparison.OrdinalIgnoreCase));

            // Lookup helpers
            public static GameInfo GetById(int id) =>
                Games.FirstOrDefault(g => g.Id == id);

            public static GameInfo GetByName(string name) =>
                Games.FirstOrDefault(g => g.GameName.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public static class ModArchives
        {
            public const string NewArchiveFormat = ".ba2";
            public const string OldArchiveFormat = ".bsa";
        }

        public static class ModFiles
        {
            public static readonly string[] NewModFormat = { ".esm", ".esp" };
            public static readonly string[] OldModFormat = { ".esm", ".esp", ".esl" };

            public static bool IsValidModExtension(string extension, bool useNewFormat = true)
            {
                var formats = useNewFormat ? NewModFormat : OldModFormat;
                return formats.Contains(extension.ToLower());
            }
        }

        public class Configuration // LOOT
        {
            public List<string> bash_tags { get; set; }
            public List<Plugin> common { get; set; }
            public List<Globals> globals { get; set; }
            public List<Group> groups { get; set; }
            public List<Plugin> plugins { get; set; }
            public Prelude prelude { get; set; }
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

        public class GameInfo
        {
            public GameInfo(int id, string name, string appdata, string excludefile, string docfolder, string executable,
                string[] modFormats, string archiveFormat, string ckid, int webskipchars, string creationssite)
            {
                Id = id;
                GameName = name; // Display name
                AppData = appdata; // Game AppData folder name
                ExcludeFile = excludefile; // Exclude file name
                DocFolder = docfolder; // My Games folder
                Executable = executable;
                ModFormats = modFormats;
                ArchiveFormat = archiveFormat;
                CKId = ckid; // Creation Kit Steam ID
                WebSkipChars = webskipchars; // Number of chars to skip in Creations URL
                CreationsSite = creationssite; // Creations site name
            }

            public string AppData { get; }
            public string ArchiveFormat { get; }
            public string CKId { get; }
            public string CreationsSite { get; }
            public string DocFolder { get; }
            public string ExcludeFile { get; }
            public string Executable { get; }
            public string GameName { get; }
            public int Id { get; }
            public string[] ModFormats { get; }
            public int WebSkipChars { get; }
        }

        public class Globals// LOOT
        {
            public string condition { get; set; }
            public string content { get; set; }
            public List<string> subs { get; set; }
            public string type { get; set; }
        }

        public class Group // LOOT
        {
            public List<string> after { get; set; }
            public string name { get; set; }
        }

        public class MessageAnchor// LOOT
        {
            public string condition { get; set; }
            public string content { get; set; }
            public List<string> subs { get; set; }
            public string type { get; set; }
        }

        public class Msg // LOOT
        {
            public string content { get; set; }
            public string type { get; set; }
        }

        public class Plugin // LOOT
        {
            public List<string> after { get; set; }
            public string display { get; set; }
            public string group { get; set; }
            public List<string> inc { get; set; }
            public List<Msg> msg { get; set; }
            public string name { get; set; }
            public List<Req> req { get; set; }
            public List<Url> url { get; set; }
        }

        public class Prelude// LOOT
        {
            public List<MessageAnchor> common { get; set; }
        }

        public class Req // LOOT
        {
            public string display { get; set; }
            public string name { get; set; }
        }

        public class Url // LOOT
        {
            public string link { get; set; }
            public string name { get; set; }
        }
    }
}