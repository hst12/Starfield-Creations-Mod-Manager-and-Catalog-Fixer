using Microsoft.VisualBasic;
using Microsoft.Win32;
using Narod.SteamGameFinder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SevenZipExtractor;
using Starfield_Tools.Common;
using Starfield_Tools.Load_Order_Editor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using YamlDotNet.Serialization;
using File = System.IO.File;

namespace Starfield_Tools
{
    public partial class frmLoadOrder : Form
    {
        private CancellationTokenSource cancellationTokenSource;

        public const byte Steam = 0, MS = 1, Custom = 2, SFSE = 3;
        public static string StarfieldGamePath;
        public static bool NoWarn;
        public static int returnStatus;
        public static ActivityLog activityLog;

        private Rectangle dragBoxFromMouseDown;
        private int rowIndexFromMouseDown, rowIndexOfItemUnderMouseToDrop, GameVersion = Steam;

        private readonly Tools tools = new();

        private string LastProfile, tempstr;

        private bool Profiles = false, GridSorted = false, AutoUpdate = false, ActiveOnly = false, AutoSort = false, isModified = false, LooseFiles, log;
        private Tools.Configuration Groups = new();

        public frmLoadOrder(string parameter)
        {
            InitializeComponent();
#if DEBUG
            this.Text = Application.ProductName + " " + File.ReadAllText(Path.Combine(Tools.CommonFolder, "App Version.txt")) + " Debug";
            testToolStripMenuItem.Visible = true; // Show test menu in debug mode
#endif

            LastProfile ??= Properties.Settings.Default.LastProfile;

            if (Properties.Settings.Default.Log)
            {
                tempstr = Properties.Settings.Default.LogFileDirectory;
                if (tempstr == "")
                    tempstr = Tools.LocalAppDataPath;
                activityLog = new ActivityLog(Path.Combine(tempstr, "Activity Log.txt")); // Create activity log if enabled
                log = true;
            }

            this.KeyPreview = true; // Ensure the form captures key presses

            Tools.CheckGame(); // Exit if Starfield appdata folder not found

            foreach (var arg in Environment.GetCommandLineArgs()) // Handle command line arguments
            {
                if (arg.Equals("-noauto", StringComparison.InvariantCultureIgnoreCase))
                {
                    ChangeSettings(false); // Disable auto settings
                    sbar3("Auto Settings Disabled");
                }

                if (arg.Equals("-reset", StringComparison.InvariantCultureIgnoreCase))
                    ResetPreferences();
            }

            string PluginsPath = Path.Combine(Tools.StarfieldAppData, "Plugins.txt");
            if (!File.Exists(PluginsPath))
            {
                MessageBox.Show(@"Missing Plugins.txt file

Click Ok to create a blank Plugins.txt file
Click File->Restore if you have a backup of your Plugins.txt file
Alternatively, run the game once to have it create a Plugins.txt file for you.", "Plugins.txt not found");

                File.WriteAllText(PluginsPath, "# This file is used by Starfield to keep track of your downloaded content.\n# Please do not modify this file.\n");
            }

            frmStarfieldTools StarfieldTools = new();

            if (Properties.Settings.Default.AutoCheck)
            {
                // Check the catalog
                tempstr = StarfieldTools.CatalogStatus;
                if (tempstr != null && StarfieldTools.CatalogStatus.Contains("Error"))
                    StarfieldTools.Show(); // Show catalog fixer if catalog broken
            }

            bool BackupStatus = false;

            try
            {
                // Check if loose files are enabled
                string LooseFilesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"My Games\Starfield"),
                    filePath = Path.Combine(LooseFilesDir, "StarfieldCustom.ini");
                if (File.Exists(filePath))
                {
                    var StarfieldCustomINI = File.ReadAllLines(filePath);
                    foreach (var lines in StarfieldCustomINI)
                    {
                        if (lines.Contains("bInvalidateOlderFiles"))
                        {
                            Properties.Settings.Default.LooseFiles = true;
                            //SaveSettings();
                            LooseFiles = true;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (log)
                    activityLog.WriteLog("Error reading file" + ex.Message);
#if DEBUG
                MessageBox.Show(ex.Message, "Error opening file");
#endif
            }

            this.KeyUp += new System.Windows.Forms.KeyEventHandler(KeyEvent); // Handle <enter> for search

            menuStrip1.Font = Properties.Settings.Default.FontSize; // Get font size
            this.Font = Properties.Settings.Default.FontSize;

            // Initialise settings

            GameVersion = Properties.Settings.Default.GameVersion;

            if (GameVersion != MS)
            {
                StarfieldGamePath = Properties.Settings.Default.StarfieldGamePath;
                if (StarfieldGamePath == "")
                {
                    GetSteamGamePath(); // Detect Steam path
                    if (StarfieldGamePath == "")
                    {
                        StarfieldGamePath = tools.SetStarfieldGamePath();
                        Properties.Settings.Default.StarfieldGamePath = StarfieldGamePath;
                        //SaveSettings();
                    }
                }
            }
            else
            {
                if (Properties.Settings.Default.GamePathMS == "")
                    tools.SetStarfieldGamePathMS();
                StarfieldGamePath = Properties.Settings.Default.GamePathMS;
            }

            if (!File.Exists(Path.Combine(StarfieldGamePath, "CreationKit.exe"))) // Hide option to launch CK if not found
                creationKitToolStripMenuItem.Visible = false;

            // Unhide Star UI Configurator menu if found

            if (!File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"My Games\Starfield\Data\StarUI Configurator.bat")))
                starUIConfiguratorToolStripMenuItem.Visible = false;

            if (Properties.Settings.Default.AutoDelccc)
            {
                toolStripMenuAutoDelccc.Checked = true;
                sbarCCCOn();
                if (Delccc())
                    toolStripStatus1.Text = ("Starfield.ccc deleted");
            }
            else
                sbarCCCOff();

            // Setup game version
            switch (GameVersion)
            {
                case Steam:
                    toolStripMenuSteam.Checked = true;
                    break;

                case MS:
                    toolStripMenuMS.Checked = true;
                    break;

                case Custom:
                    toolStripMenuCustom.Checked = true;
                    break;

                case SFSE:
                    gameVersionSFSEToolStripMenuItem.Checked = true;
                    break;
            }

            // Detect other apps
            if (!File.Exists(Path.Combine(StarfieldGamePath, "sfse_loader.exe")))
                gameVersionSFSEToolStripMenuItem.Visible = false;
            GameVersionDisplay();

            if (Properties.Settings.Default.MO2Path == "")
                mO2ToolStripMenuItem.Visible = false;

            if (string.IsNullOrEmpty(Properties.Settings.Default.LOOTPath) &&
    File.Exists(@"C:\Program Files\LOOT\LOOT.exe")) // Try to detect LOOT if installed in default location
            {
                Properties.Settings.Default.LOOTPath = @"C:\Program Files\LOOT\LOOT.exe";
                //SaveSettings();
            }

            // Setup other preferences

            // Set the color mode based on the theme
            var colorMode = Properties.Settings.Default.DarkMode switch
            {
                0 => SystemColorMode.Classic,
                1 => SystemColorMode.Dark,
                2 => SystemColorMode.System,
                _ => SystemColorMode.Classic // Default fallback
            };

            Application.SetColorMode(colorMode);

            // Update menu item selection
            var menuItems = new Dictionary<int, ToolStripMenuItem>
{
    { 0, lightToolStripMenuItem },
    { 1, darkToolStripMenuItem },
    { 2, systemToolStripMenuItem }
};

            if (menuItems.TryGetValue(Properties.Settings.Default.DarkMode, out var menuItem))
            {
                menuItem.Checked = true;
            }

            // Apply UI changes for dark mode conditions
            if (colorMode == SystemColorMode.Dark ||
               (colorMode == SystemColorMode.System && System.Windows.Forms.Application.SystemColorMode == SystemColorMode.Dark))
            {
                dataGridView1.EnableHeadersVisualStyles = false;
                dataGridView1.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.Green; // Background color of selected cells
                dataGridView1.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.White; // Text color of selected cells
                statusStrip1.BackColor = System.Drawing.Color.Black;
            }
            else
            {
                dataGridView1.EnableHeadersVisualStyles = true;
            }

            // Create BlockedMods.txt if necessary
            try
            {
                if (!Directory.Exists(Tools.LocalAppDataPath))
                    Directory.CreateDirectory(Tools.LocalAppDataPath);
                if (!File.Exists(Path.Combine(Tools.LocalAppDataPath, "BlockedMods.txt")))
                    File.Create(Path.Combine(Tools.LocalAppDataPath, "BlockedMods.txt")).Close();
            }
            catch (Exception ex)
            {
                if (log)
                    activityLog.WriteLog("Error creating BlockedMods.txt: " + ex.Message);
                MessageBox.Show(ex.Message);
            }

            SetMenus();
            SetupColumns();

            // Hide menu items if LOOTPath is still unset
            bool lootPathIsEmpty = string.IsNullOrEmpty(Properties.Settings.Default.LOOTPath);
            toolStripMenuLOOTToggle.Visible = !lootPathIsEmpty;
            autoSortToolStripMenuItem.Visible = !lootPathIsEmpty;
            toolStripMenuLoot.Visible = !lootPathIsEmpty;
            toolStripMenuLootSort.Visible = !lootPathIsEmpty;

            // Do a 1-time backup of Plugins.txt if it doesn't exist
            try
            {
                if (!File.Exists(Path.Combine(Tools.StarfieldAppData, "Plugins.txt.bak")))
                {
                    File.Copy(PluginsPath, Tools.StarfieldAppData + @"\Plugins.txt.bak");
                    sbar2("Plugins.txt backed up to Plugins.txt.bak");
                }
            }
            catch (Exception ex)
            {
                if (log)
                    activityLog.WriteLog("Error backing up Plugins.txt: " + ex.Message);
#if DEBUG
                MessageBox.Show(ex.Message, "Error backing up Plugins.txt");
#endif
            }

            // Do a 1-time backup of StarfieldCustom.ini if it doesn't exist
            tempstr = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"My Games\Starfield\StarfieldCustom.ini");
            if (!File.Exists(tempstr + ".bak") && File.Exists(tempstr))
            {
                sbar2("StarfieldCustom.ini backed up to StarfieldCustom.ini.bak");
                File.Copy(tempstr, tempstr + ".bak");
            }

            if (Properties.Settings.Default.LOOTEnabled)
                ReadLOOTGroups();

            // Display Loose Files status
            sbarCCC(looseFilesDisabledToolStripMenuItem.Checked ? "Loose files enabled" : "Loose files disabled");

            // Initialise profiles
            if (Properties.Settings.Default.ProfileOn)
            {
                toolStripMenuProfilesOn.Checked = true;
                Profiles = true;

                chkProfile.Checked = true;
            }
            else
            {
                Profiles = false;
            }

            foreach (var arg in Environment.GetCommandLineArgs()) // Handle command line arguments
            {
                //MessageBox.Show(arg); // Show all arguments for debugging
                if (arg.Equals("-run", StringComparison.InvariantCultureIgnoreCase))
                {
                    RunGame();
                    Application.Exit();
                }

                if (arg.StartsWith("-profile", StringComparison.InvariantCultureIgnoreCase))
                {
                    tempstr = Path.Combine(Properties.Settings.Default.ProfileFolder, Environment.GetCommandLineArgs()[2]);
                    LastProfile = Environment.GetCommandLineArgs()[2];
                }

                if (arg.StartsWith("-install")) // For future use (maybe) install mod from Nexus web link
                {
                    string strippedCommandLine = Environment.GetCommandLineArgs()[2];

                    InstallMod(strippedCommandLine);
                }

                if (arg.Equals("-dev"))
                    testToolStripMenuItem.Visible = true;
            }

            cmbProfile.Enabled = Profiles;
            if (Profiles)
                GetProfiles();
            else
                InitDataGrid();

            // Creations update
            if (Properties.Settings.Default.CreationsUpdate)
            {
                prepareForCreationsUpdateToolStripMenuItem.Checked = false;
                Properties.Settings.Default.CreationsUpdate = false;
                SaveSettings();
                BackupStatus = StarfieldTools.BackupCatalog();
                tempstr = BackupStatus ? "Catalog backed up" : "Catalog backup is up to date";
                Properties.Settings.Default.AutoRestore = true;
                MessageBox.Show(tempstr + "\nAuto Restore turned on\n\nYou can now play the game normally until the next time you want to update\n\n" +
                    "Remember to choose the Prepare for Creations Update option again before you update or add new mods", "Creations update complete");
                if (log)
                    activityLog.WriteLog("Creations update complete, backup status: " + BackupStatus);
            }

            // Apply bold styling when ActiveOnly is enabled
            btnActiveOnly.Font = new System.Drawing.Font(btnActiveOnly.Font, ActiveOnly ? FontStyle.Bold : FontStyle.Regular);

            // Reset defaults if AutoReset is enabled
            if (Properties.Settings.Default.AutoReset)
                ResetDefaults();

            // Handle AutoUpdate logic
            if (AutoUpdate)
            {
                int changes = SyncPlugins();

                if (changes > 0)
                {
                    sbar4($"Changes: {changes}");
                    if (AutoSort)
                        RunLOOT(true);

                    InitDataGrid();
                }
            }
        }

        public class ActivityLog
        {
            private readonly string logFilePath;

            public ActivityLog(string filePath)
            {
                logFilePath = filePath;
                WriteLog($"Starting log - {logFilePath}\n");
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
                    MessageBox.Show($"Error writing to log file: {ex.Message}");
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

            public void Dispose()
            {
            }
        }

        private void SetMenus()
        {
            var settings = Properties.Settings.Default;

            // Assign values
            toggleToolStripMenuItem.Checked = settings.Log;
            if (activityLog == null && settings.Log)
                EnableLog();

            toolStripMenuProfilesOn.Checked = settings.ProfileOn;
            compareProfilesToolStripMenuItem.Checked = settings.CompareProfiles;
            looseFilesDisabledToolStripMenuItem.Checked = LooseFiles || settings.LooseFiles;
            autoSortToolStripMenuItem.Checked = AutoSort = settings.AutoSort;
            activeOnlyToolStripMenuItem.Checked = ActiveOnly = settings.ActiveOnly;
            autoUpdateModsToolStripMenuItem.Checked = AutoUpdate = settings.AutoUpdate;
            toolStripMenuAutoDelccc.Checked = settings.AutoDelccc;
            autoResetToolStripMenuItem.Checked = settings.AutoReset;
            showTimeToolStripMenuItem.Checked = timer2.Enabled = settings.Showtime;
            activateNewModsToolStripMenuItem.Checked = settings.ActivateNew;
            disableAllWarningToolStripMenuItem.Checked = NoWarn = settings.NoWarn;
            toolStripMenuLOOTToggle.Checked = settings.LOOTEnabled;
            prepareForCreationsUpdateToolStripMenuItem.Checked = settings.CreationsUpdate;
            modStatsToolStripMenuItem.Checked = settings.ModStats;
            blockedToolStripMenuItem.Checked = settings.Blocked;
            blockedModsToolStripMenuItem.Checked = settings.BlockedView;
            if (Properties.Settings.Default.VortexPath != "")
                vortexToolStripMenuItem.Visible = true;
            runProgramToolStripMenuItem.Checked = settings.RunProgram;
            resizeToolStripMenuItem.Checked = settings.Resize;
            enableSplashScreenToolStripMenuItem.Checked = Properties.Settings.Default.LoadScreenEnabled;
        }

        private void SetupColumns()
        {
            SetColumnVisibility(Properties.Settings.Default.TimeStamp, timeStampToolStripMenuItem, dataGridView1.Columns["TimeStamp"]);
            SetColumnVisibility(Properties.Settings.Default.Achievements, toolStripMenuAchievements, dataGridView1.Columns["Achievements"]);
            SetColumnVisibility(Properties.Settings.Default.CreationsID, toolStripMenuCreationsID, dataGridView1.Columns["CreationsID"]);
            SetColumnVisibility(Properties.Settings.Default.Files, toolStripMenuFiles, dataGridView1.Columns["Files"]);
            SetColumnVisibility(Properties.Settings.Default.Group, toolStripMenuGroup, dataGridView1.Columns["Group"]);
            SetColumnVisibility(Properties.Settings.Default.Index, toolStripMenuIndex, dataGridView1.Columns["Index"]);
            SetColumnVisibility(Properties.Settings.Default.FileSize, toolStripMenuFileSize, dataGridView1.Columns["FileSize"]);
            SetColumnVisibility(Properties.Settings.Default.URL, uRLToolStripMenuItem, dataGridView1.Columns["URL"]);
            SetColumnVisibility(Properties.Settings.Default.Version, toolStripMenuVersion, dataGridView1.Columns["Version"]);
            SetColumnVisibility(Properties.Settings.Default.AuthorVersion, toolStripMenuAuthorVersion, dataGridView1.Columns["AuthorVersion"]);
            SetColumnVisibility(Properties.Settings.Default.Description, toolStripMenuDescription, dataGridView1.Columns["Description"]);
            SetColumnVisibility(Properties.Settings.Default.Blocked, blockedToolStripMenuItem, dataGridView1.Columns["Blocked"]);
        }

        private static void SetColumnVisibility(bool condition, ToolStripMenuItem menuItem, DataGridViewColumn column)
        {
            menuItem.Checked = condition;
            column.Visible = condition;
        }

        private void RefreshDataGrid()
        {
            if (isModified && Tools.ConfirmAction("Save Changes?", "Load order has been modified", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                SavePlugins();
            if (!Profiles)
                InitDataGrid();

            GetProfiles();
            GridSorted = false;
            isModified = false;
            GameVersionDisplay();
            toolStripStatusStats.ForeColor = DefaultForeColor;
            sbar3("Refresh complete");
            sbar4("");
        }

        private void ReadLOOTGroups() // Read LOOT Groups
        {
            try
            {
                var deserializer = new DeserializerBuilder().Build();
                string yamlContent = File.ReadAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "LOOT\\games\\Starfield\\userlist.yaml"));
                Groups = deserializer.Deserialize<Tools.Configuration>(yamlContent);
            }
            catch (Exception ex)
            {
#if DEBUG
                MessageBox.Show(ex.Message, "Yaml decoding error\nLOOT userlist.yaml possibly corrupt", MessageBoxButtons.OK, MessageBoxIcon.Stop);
#endif
                sbar3(ex.Message);
                if (log)
                    activityLog.WriteLog("Error decoding LOOT userlist.yaml: " + ex.Message);
            }
        }

        private void KeyEvent(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.F5:
                    RefreshDataGrid();
                    break;

                case Keys.Escape:
                    txtSearchBox.Clear();
                    txtSearchBox.Focus(); // Clear search box and focus it when Escape is pressed
                    break;
            }
            if (e.Control && e.KeyCode == Keys.F)
            {
                txtSearchBox.Focus(); // Focus the search box when Ctrl+F is pressed
                //SearchMod(); // Ctrl+F to search
            }
        }

        private static bool CheckStarfieldCustom()
        {
            return Tools.FileCompare(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                     @"My Games\Starfield\StarfieldCustom.ini"), Path.Combine(Tools.CommonFolder, "StarfieldCustom.ini"));
        }

        private void InitDataGrid()
        {
#if DEBUG
            if (log)
            {
                StackTrace stackTrace = new StackTrace();
                StackFrame frame = stackTrace.GetFrame(1); // Get the caller
                activityLog.WriteLog($"InitDatagrid called from {frame.GetMethod().Name}");
            }
#endif
            int EnabledCount = 0, IndexCount = 1, esmCount = 0, espCount = 0, ba2Count, mainCount = 0, i, versionDelimiter, dotIndex;
            string loText = Path.Combine(Tools.StarfieldAppData, "Plugins.txt"),
                   LOOTPath = Properties.Settings.Default.LOOTPath,
                   StatText = "", pluginName, rawVersion;

            List<string> CreationsPlugin = new(), CreationsTitle = new(), CreationsFiles = new(), CreationsVersion = new();
            List<bool> AchievementSafe = new();
            List<long> TimeStamp = new(), FileSize = new();
            List<string> CreationsID = new(), blockedMods = Tools.BlockedMods();
            DateTime start = new(1970, 1, 1, 0, 0, 0, 0);

            // Cache the visibility of columns checked repeatedly.
            bool isGroupVisible = dataGridView1.Columns["Group"]?.Visible ?? false;
            bool isDescriptionVisible = dataGridView1.Columns["Description"]?.Visible ?? false;
            bool isVersionVisible = dataGridView1.Columns["Version"]?.Visible ?? false;
            bool isAuthorVersionVisible = dataGridView1.Columns["AuthorVersion"]?.Visible ?? false;
            bool isTimeStampVisible = dataGridView1.Columns["TimeStamp"]?.Visible ?? false;
            bool isAchievementsVisible = dataGridView1.Columns["Achievements"]?.Visible ?? false;
            bool isFilesVisible = dataGridView1.Columns["Files"]?.Visible ?? false;
            bool isFileSizeVisible = dataGridView1.Columns["FileSize"]?.Visible ?? false;
            bool isIndexVisible = dataGridView1.Columns["Index"]?.Visible ?? false;
            bool modEnabled;
            string json = File.ReadAllText(Tools.GetCatalogPath()); // Read Catalog
            var bethFilesSet = new HashSet<string>(tools.BethFiles); // Read files to exclude
            string[] lines = File.ReadAllLines(loText); // Read Plugins.txt

            sbar("Loading...");
            sbar3("");
            statusStrip1.Refresh();

            btnSave.Enabled = true;
            saveToolStripMenuItem.Enabled = true;

            dataGridView1.SuspendLayout(); // Suspend UI updates to avoid redraw for every row addition.
            dataGridView1.Rows.Clear();
            SetColumnVisibility(false, toolStripMenuCreationsID, dataGridView1.Columns["CreationsID"]); // Temporarily turn off these columns
            SetColumnVisibility(false, uRLToolStripMenuItem, dataGridView1.Columns["URL"]);

            try
            {
                var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, Tools.Creation>>(json);
                data.Remove("ContentCatalog"); // Remove header

                foreach (var kvp in data)
                {
                    var item = kvp.Value;
                    var files = item.Files;

                    // Add files that end with .esm or .esp (ignoring case)
                    CreationsPlugin.AddRange(files.Where(file =>
                        file.EndsWith(".esm", StringComparison.OrdinalIgnoreCase) ||
                        file.EndsWith(".esp", StringComparison.OrdinalIgnoreCase)));

                    CreationsTitle.Add(item.Title);
                    CreationsVersion.Add(item.Version);
                    CreationsFiles.Add(string.Join(", ", files));
                    AchievementSafe.Add(item.AchievementSafe);
                    TimeStamp.Add(item.Timestamp);
                    CreationsID.Add(kvp.Key);
                    FileSize.Add(item.FilesSize);
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                MessageBox.Show(ex.Message);
#endif
                if (log)
                    activityLog.WriteLog("Error reading catalog: " + ex.Message);
                sbar(ex.Message);
            }

            // -- Pre-build a dictionary for quick lookup from plugin name (.esm and .esp) to index --
            var creationLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (i = 0; i < CreationsPlugin.Count; i++)
            {
                dotIndex = CreationsPlugin[i].LastIndexOf('.');
                if (dotIndex > 0)
                {
                    string baseName = CreationsPlugin[i][..dotIndex];
                    creationLookup[baseName + ".esm"] = i;
                    creationLookup[baseName + ".esp"] = i;
                }
            }

            progressBar1.Maximum = lines.Length;
            progressBar1.Value = 0;
            progressBar1.Show();

            string previousGroup = null;

            List<DataGridViewRow> rowBuffer = new List<DataGridViewRow>();

            foreach (var line in lines)
            {
                // Skip empty lines, excluded lines or comments.
                if (string.IsNullOrEmpty(line) || bethFilesSet.Contains(line) || line[0] == '#')
                    continue;

                progressBar1.Value++;
                modEnabled = line[0] == '*';
                pluginName = modEnabled ? line[1..] : line;

                // Initialize details.
                string description = string.Empty,
                       modFiles = string.Empty,
                       modVersion = string.Empty,
                       authorVersion = string.Empty,
                       aSafe = string.Empty,
                       modTimeStamp = string.Empty,
                       modID = string.Empty,
                       url = string.Empty;
                long modFileSize = 0;

                // Attempt a quick lookup.
                if (creationLookup.TryGetValue(pluginName, out int idx))
                {
                    description = CreationsTitle[idx];

                    rawVersion = CreationsVersion[idx];
                    versionDelimiter = rawVersion.IndexOf('.');
                    if (versionDelimiter > 0)
                    {
                        authorVersion = rawVersion[(versionDelimiter + 1)..];

                        try
                        {
                            if (versionDelimiter > 0 && versionDelimiter <= rawVersion.Length)
                            {
                                string timePart = rawVersion[..versionDelimiter];
                                if (double.TryParse(timePart, out double seconds))
                                {
                                    modVersion = start.AddSeconds(seconds).Date.ToString("yyyy-MM-dd");
                                }
                                else
                                {
                                    // Handle failed parsing
                                    modVersion = "Invalid version format";
                                }
                            }
                            else
                            {
                                // Handle unexpected delimiter position
                                modVersion = "Delimiter out of bounds";
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log or handle unexpected exceptions
                            modVersion = $"Error: {ex.Message}";
                        }
                    }
                    modFiles = CreationsFiles[idx];
                    aSafe = AchievementSafe[idx] ? "Yes" : "";
                    modTimeStamp = Tools.ConvertTime(TimeStamp[idx]).ToString();
                    modID = CreationsID[idx];
                    modFileSize = FileSize[idx] / 1024;
                    url = $"https://creations.bethesda.net/en/starfield/details/{(modID.Length > 3 ? modID[3..] : modID)}";
                }

                // Buffer the row before adding.

                var row = new DataGridViewRow();
                row.CreateCells(dataGridView1); // Create cells based on current column structure

                // Update group information if available.
                if (!string.IsNullOrEmpty(LOOTPath) && Groups.groups != null && isGroupVisible)
                {
                    var group = Groups.plugins.FirstOrDefault(p => p.name.Equals(pluginName, StringComparison.OrdinalIgnoreCase));
                    if (group != null)
                    {
                        row.Cells[4].Value = group.group; // Group

                        // If a group URL exists, override URL and description.
                        if (group.url?.FirstOrDefault() is { } urlInfo)
                        {
                            url = urlInfo.link;
                            description = urlInfo.name;
                        }
                    }
                }

                // Process blocked mods.
                if (blockedMods.Contains(pluginName))
                {
                    modEnabled = false;
                    row.Cells[13].Value = true; // Blocked
                }

                EnabledCount += modEnabled ? 1 : 0;

                // Special handling for Bethesda Game Studios mods.
                if (pluginName.StartsWith("sfbgs", StringComparison.OrdinalIgnoreCase))
                {
                    string currentGroupX = row.Cells[4].Value?.ToString() ?? "Bethesda Game Studios Creations"; //Group = column 4
                    row.Cells[4].Value = $"{currentGroupX} (Bethesda)";
                }

                // Update required cells.

                row.Cells[1].Value = modEnabled; // Enabled = column 1
                row.Cells[2].Value = pluginName; // PluginName = column 2
                row.Cells[10].Value = modID; // CreationsID = column 10
                row.Cells[12].Value = url; // URL = column 12

                // Update additional columns if visible
                if (isDescriptionVisible)
                    row.Cells[3].Value = description; // Description = column 3
                if (isVersionVisible)
                    row.Cells[5].Value = modVersion; // Version = column 5
                if (isAuthorVersionVisible)
                    row.Cells[6].Value = authorVersion; // AuthorVersion = column 6
                if (isTimeStampVisible)
                    row.Cells[7].Value = modTimeStamp; // TimeStamp = column 7
                if (isAchievementsVisible)
                    row.Cells[8].Value = aSafe; // Achievements = column 8
                if (isFilesVisible)
                    row.Cells[9].Value = modFiles; // Files = column 9
                if (isFileSizeVisible)
                    row.Cells[11].Value = modFileSize != 0 ? modFileSize : null; // FileSize = column 11
                if (isIndexVisible)
                    row.Cells[0].Value = IndexCount++; // Index = column 0

                rowBuffer.Add(row);
            } // End of main loop
            foreach (var row in rowBuffer)
                dataGridView1.Rows.AddRange(row);

            // -- Process mod stats if the Starfield game path is set --
            if (!string.IsNullOrEmpty(StarfieldGamePath) && Properties.Settings.Default.ModStats)
            {
                try
                {
                    // Cache file paths and load BGS archives once
                    var dataDirectory = Path.Combine(StarfieldGamePath, "Data");
                    var bgsArchives = File.ReadLines(Path.Combine(Tools.CommonFolder, "BGS Archives.txt"))
                        .Where(line => line.Length > 4)
                        .Select(line => line[..^4].ToLowerInvariant())
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    // Load enabled plugins once
                    var enabledPlugins = File.ReadLines(loText)
                        .Where(line => line.StartsWith('*') && line.Length > 1)
                        .Select(line => line[1..])
                        .Where(plugin => plugin.Contains('.'))
                        .Select(plugin => plugin.Split('.')[0].ToLowerInvariant())
                        .Where(plugin => !bgsArchives.Contains(plugin))
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    // Process all BA2 files in one enumeration
                    var allBa2Files = Directory.EnumerateFiles(dataDirectory, "*.ba2", SearchOption.TopDirectoryOnly);
                    var archives = new List<string>();
                    var mainArchivePlugins = new List<string>();
                    var textureArchivePlugins = new List<string>();

                    foreach (var file in allBa2Files)
                    {
                        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(file);
                        if (string.IsNullOrEmpty(fileNameWithoutExt))
                            continue;

                        var lowerFileName = fileNameWithoutExt.ToLowerInvariant();

                        // Skip BGS archives
                        if (bgsArchives.Contains(lowerFileName))
                            continue;

                        archives.Add(lowerFileName);

                        // Check for main archives and extract mod name
                        if (lowerFileName.Contains(" - main", StringComparison.OrdinalIgnoreCase))
                        {
                            var modName = lowerFileName.Replace(" - main", string.Empty, StringComparison.OrdinalIgnoreCase);
                            mainArchivePlugins.Add(modName);
                        }

                        // Check for texture archives and extract mod name
                        if (lowerFileName.Contains(" - textures", StringComparison.OrdinalIgnoreCase))
                        {
                            var modName = lowerFileName.Replace(" - textures", string.Empty, StringComparison.OrdinalIgnoreCase);
                            textureArchivePlugins.Add(modName);
                        }
                    }

                    // Calculate all counts
                    ba2Count = Directory.EnumerateFiles(dataDirectory, "*.ba2", SearchOption.TopDirectoryOnly).Count();
                    esmCount = Directory.EnumerateFiles(dataDirectory, "*.esm", SearchOption.TopDirectoryOnly).Count();

                    espCount = Directory.EnumerateFiles(dataDirectory, "*.esp", SearchOption.TopDirectoryOnly)
                        .Select(file => Path.GetFileNameWithoutExtension(file))
                        .Where(plugin => !string.IsNullOrEmpty(plugin))
                        .Count(plugin => enabledPlugins.Contains(plugin.ToLowerInvariant()));

                    mainCount = mainArchivePlugins.Count(mod => enabledPlugins.Contains(mod));
                    var textureCount = textureArchivePlugins.Count(mod => enabledPlugins.Contains(mod));

                    // Build status text
                    var statusBuilder = new StringBuilder();
                    statusBuilder.Append($"Creations {CreationsPlugin.Count}, Other {dataGridView1.RowCount - CreationsPlugin.Count}, ");
                    statusBuilder.Append($"Enabled: {EnabledCount}, esm: {esmCount}, Archives: {ba2Count}, ");
                    statusBuilder.Append($"Enabled - Main: {mainCount}, Textures: {textureCount}");

                    if (espCount > 0)
                        statusBuilder.Append($", esp files: {espCount}");

                    StatText = statusBuilder.ToString();

                    // Validate plugin consistency
                    var otherPluginCount = dataGridView1.RowCount - CreationsPlugin.Count;
                    Debug.Assert(otherPluginCount >= 0, "Plugins mismatch");

                    if (otherPluginCount < 0)
                    {
                        sbar4("Catalog/Plugins mismatch - Run game to solve");
                    }
                }
                catch (Exception ex)
                {
                    sbar("Starfield path needs to be set for mod stats");
#if DEBUG
                    MessageBox.Show($"Mod stats error: {ex.Message}");
#endif
                }
            }

            // Restore column visibility based on user settings.
            SetColumnVisibility(Properties.Settings.Default.CreationsID, toolStripMenuCreationsID, dataGridView1.Columns["CreationsID"]);
            SetColumnVisibility(Properties.Settings.Default.URL, uRLToolStripMenuItem, dataGridView1.Columns["URL"]);

            if (ActiveOnly && dataGridView1.Rows.Count > 1000)
            {
                sbar("Too many rows to filter");
                ActiveOnly = Properties.Settings.Default.ActiveOnly = activeOnlyToolStripMenuItem.Checked = false;
            }

            if (ActiveOnly)
            {
                sbar("Hiding inactive mods...");
                statusStrip1.Refresh();
                foreach (DataGridViewRow row in dataGridView1.Rows)
                    if (!(bool)row.Cells["ModEnabled"].Value)
                        row.Visible = false;
            }

            if (Properties.Settings.Default.Resize)
                ResizeFormToFitDataGridView(this);

            progressBar1.Value = progressBar1.Maximum;
            progressBar1.Hide();

            sbar(StatText);
            dataGridView1.ResumeLayout(); // Resume layout
            dataGridView1.EndEdit();
        }

        private void GetProfiles()
        {
            string ProfileFolder;
            if (!Profiles)
                return;
            cmbProfile.Items.Clear();
            ProfileFolder = Properties.Settings.Default.ProfileFolder;
            ProfileFolder = string.IsNullOrEmpty(ProfileFolder)
        ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        : ProfileFolder;

            //LastProfile ??= Properties.Settings.Default.LastProfile;
            try
            {
                foreach (var profileName in Directory.EnumerateFiles(ProfileFolder, "*.txt", SearchOption.TopDirectoryOnly))
                {
                    cmbProfile.Items.Add(profileName[(profileName.LastIndexOf('\\') + 1)..]);
                }

                int index = cmbProfile.Items.IndexOf(Properties.Settings.Default.LastProfile);

                if (index != -1)
                {
                    cmbProfile.SelectedIndex = index;
                    LastProfile = cmbProfile.Items[index].ToString();
                }
            }
            catch (Exception ex)
            {
                if (log)
                    activityLog.WriteLog("Error reading profiles: " + ex.Message);
                MessageBox.Show(ex.Message);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void SaveLO(string PluginFileName)
        {
            if (GridSorted)
            {
                MessageBox.Show("Save disabled");
                return;
            }

            try
            {
                using (StreamWriter writer = new(PluginFileName))
                {
                    writer.WriteLine("# This file is used by Starfield to keep track of your downloaded content.");
                    writer.WriteLine("# Please do not modify this file.");

                    foreach (DataGridViewRow row in dataGridView1.Rows)
                    {
                        var pluginName = row.Cells["PluginName"].Value as string;
                        if (string.IsNullOrEmpty(pluginName))
                            continue;

                        bool modEnabled = row.Cells["ModEnabled"].Value as bool? ?? false;

                        // Disable mod if it exists in BlockedMods
                        modEnabled &= !Tools.BlockedMods().Contains(pluginName);

                        writer.Write(modEnabled ? "*" : "");
                        writer.WriteLine(pluginName);
                    }
                }
            }
            catch (Exception ex)
            {
                if (log)
                    activityLog.WriteLog($"Error saving plugins file {PluginFileName}: {ex.Message}");
                MessageBox.Show(ex.Message, "Error saving plugins file", MessageBoxButtons.OK, MessageBoxIcon.Error);

                FileInfo fileInfo = new FileInfo(PluginFileName);

                if (fileInfo.Exists)
                {
                    bool isReadOnly = fileInfo.IsReadOnly;
                    MessageBox.Show($"{PluginFileName} is read-only", "Unable to save plugins", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show($"{PluginFileName} does not exist.");
                }
            }

            sbar2($"{Path.GetFileName(PluginFileName)} saved");
            isModified = false;
            if (log)
                activityLog.WriteLog($"{Path.GetFileName(PluginFileName)} saved");
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void MoveUp()
        {
            int rowIndex = dataGridView1.SelectedCells[0].OwningRow.Index;
            int colIndex = 1;

            if (rowIndex == 0)
                return; // Already at the top

            if (ActiveOnly)
                ActiveOnlyToggle();

            DataGridViewRow selectedRow = dataGridView1.Rows[rowIndex];
            dataGridView1.Rows.Remove(selectedRow);
            dataGridView1.Rows.Insert(rowIndex - 1, selectedRow);
            dataGridView1.ClearSelection();
            dataGridView1.Rows[rowIndex - 1].Selected = true;
            dataGridView1.Rows[rowIndex - 1].Cells[colIndex].Selected = true;
            if (log)
                activityLog.WriteLog($"Moved {selectedRow.Cells["PluginName"].Value} up the list from {selectedRow.Index + 2} to {selectedRow.Index + 1}");
            isModified = true;
        }

        private void btnUp_Click(object sender, EventArgs e)
        {
            MoveUp();
        }

        private void MoveDown()
        {
            int rowIndex = dataGridView1.SelectedCells[0].OwningRow.Index;
            int colIndex = 1;

            if (rowIndex == dataGridView1.Rows.Count - 1)
                return; // Already at the bottom

            if (ActiveOnly)
                ActiveOnlyToggle();

            DataGridViewRow selectedRow = dataGridView1.Rows[rowIndex];
            dataGridView1.Rows.Remove(selectedRow);
            dataGridView1.Rows.Insert(rowIndex + 1, selectedRow);
            dataGridView1.ClearSelection();
            dataGridView1.Rows[rowIndex + 1].Selected = true;
            dataGridView1.Rows[rowIndex + 1].Cells[colIndex].Selected = true;
            if (log)
                activityLog.WriteLog($"Moved {selectedRow.Cells["PluginName"].Value} down the list from {selectedRow.Index} to {selectedRow.Index + 1}");
            isModified = true;
        }

        private void btnDown_Click(object sender, EventArgs e)
        {
            MoveDown();
        }

        private void BackupPlugins()
        {
            string sourceFileName = Path.Combine(Tools.StarfieldAppData, "Plugins.txt");
            string destFileName = sourceFileName + ".bak";

            if (isModified)
            {
                MessageBox.Show("Plugins have been modified\nClick Save to save changes", "Backup not done");
                return;
            }

            try
            {
                File.Copy(sourceFileName, destFileName, true); // overwrite
                sbar("Plugins.txt backed up");
                if (log)
                    activityLog.WriteLog($"Backup of {Path.GetFileName(sourceFileName)} done to {Path.GetFileName(destFileName)}");
            }
            catch (Exception ex)
            {
                if (log)
                    activityLog.WriteLog($"Backup of {Path.GetFileName(sourceFileName)} failed: {ex.Message}");
                MessageBox.Show($"Error: {ex.Message}", "Backup failed");
            }
        }

        private void RestorePlugins()
        {
            string sourceFileName = Path.Combine(Tools.StarfieldAppData, "Plugins.txt.bak");
            string destFileName = Path.Combine(Tools.StarfieldAppData, "Plugins.txt");

            try
            {
                // Copy the file
                File.Copy(sourceFileName, destFileName, true); // overwrite
                InitDataGrid();

                toolStripStatusStats.ForeColor = DefaultForeColor;
                SavePlugins();
                sbar("Restore done");
                if (log)
                    activityLog.WriteLog($"Restore of {Path.GetFileName(sourceFileName)} done to {Path.GetFileName(destFileName)}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Restore failed");
                if (log)
                    activityLog.WriteLog($"Restore of {Path.GetFileName(sourceFileName)} failed: {ex.Message}");
            }
        }

        private void MoveTop()
        {
            if (ActiveOnly)
                ActiveOnlyToggle();
            int rowIndex = dataGridView1.SelectedCells[0].RowIndex;
            DataGridViewRow selectedRow = dataGridView1.Rows[rowIndex];
            int colIndex = 1;

            dataGridView1.Rows.Remove(selectedRow);
            dataGridView1.Rows.Insert(0, selectedRow);
            dataGridView1.ClearSelection();
            dataGridView1.Rows[0].Cells[colIndex].Selected = true;
            if (dataGridView1.Rows.Count > 0) // Ensure the DataGridView has rows
            {
                dataGridView1.FirstDisplayedScrollingRowIndex = 0; // Scroll to the first row
            }
            if (log)
                activityLog.WriteLog($"Moved {selectedRow.Cells["PluginName"].Value} to top of list");
            isModified = true;
        }

        private void MoveBottom()
        {
            if (ActiveOnly)
                ActiveOnlyToggle();
            int rowIndex = dataGridView1.SelectedCells[0].RowIndex;
            int colIndex = 1;
            DataGridViewRow selectedRow = dataGridView1.Rows[rowIndex];

            dataGridView1.Rows.Remove(selectedRow);
            dataGridView1.Rows.Insert(dataGridView1.Rows.Count, selectedRow);
            dataGridView1.ClearSelection();
            dataGridView1.Rows[^1].Cells[colIndex].Selected = true;
            int lastRowIndex = dataGridView1.Rows.Count - 1; // Get the index of the last row

            if (lastRowIndex >= 0) // Ensure the DataGridView has rows
            {
                dataGridView1.FirstDisplayedScrollingRowIndex = lastRowIndex; // Scroll to the last row
            }
            if (log)
                activityLog.WriteLog($"Moved {selectedRow.Cells["PluginName"].Value} to bottom of list");
            isModified = true;
        }

        private void btnBottom_Click(object sender, EventArgs e)
        {
            MoveBottom();
        }

        private void btnTop_Click(object sender, EventArgs e)
        {
            MoveTop();
        }

        private void dataGridView1_Sorted(object sender, EventArgs e)
        {
            sbar("Plugins sorted - saving changes disabled - Refresh to enable saving");
            toolStripStatusStats.ForeColor = System.Drawing.Color.Red;
            btnSave.Enabled = false;
            saveToolStripMenuItem.Enabled = false;
            GridSorted = true;
        }

        private void DisableAll()
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
                row.Cells["ModEnabled"].Value = false;

            sbar2("All mods disabled");
            isModified = true;
            SavePlugins();
            if (log)
                activityLog.WriteLog("All mods disabled");
        }

        private void EnableAll()
        {
            if (ActiveOnly)
                ActiveOnlyToggle();

            foreach (DataGridViewRow row in dataGridView1.Rows)
                row.Cells["ModEnabled"].Value = true;

            sbar2("All mods enabled");
            isModified = true;
            SavePlugins();
            if (log)
                activityLog.WriteLog("All mods enabled");
        }

        private void FontSelect()
        {
            if (fontDialog1.ShowDialog() != DialogResult.Cancel)
            {
                this.Font = fontDialog1.Font;
                menuStrip1.Font = fontDialog1.Font;
            }
            this.CenterToScreen();
            Properties.Settings.Default.FontSize = this.Font;
        }

        private void SearchMod()
        {
            // Exit if the search box is empty.
            if (string.IsNullOrEmpty(txtSearchBox.Text))
                return;

            // Lowercase the search query for case-insensitive matching.
            string searchQuery = txtSearchBox.Text.ToLowerInvariant();

            // Return early if no current cell is selected.
            if (dataGridView1.CurrentCell is null)
                return;

            int currentIndex = dataGridView1.CurrentCell.RowIndex;
            int totalRows = dataGridView1.RowCount;

            // Loop through all rows starting after the current one, then wrap around.
            for (int offset = 1; offset <= totalRows; offset++)
            {
                int rowIndex = (currentIndex + offset) % totalRows;
                var cellValue = dataGridView1.Rows[rowIndex].Cells["PluginName"].Value;
                var cellDescription = dataGridView1.Rows[rowIndex].Cells["Description"].Value;
                string cellText = cellValue?.ToString().ToLowerInvariant() ?? string.Empty;
                string cellDescriptionText = cellDescription?.ToString().ToLowerInvariant() ?? string.Empty;

                if (cellText.Contains(searchQuery) || cellDescriptionText.Contains(searchQuery))
                {
                    // Report the result.
                    string foundText = cellValue?.ToString() ?? "";
                    sbar2($"Found {txtSearchBox.Text} in {foundText}");

                    // Set current cell if the cell is visible; otherwise, notify the mod is inactive.
                    if (dataGridView1.Rows[rowIndex].Cells["PluginName"].Visible)
                        dataGridView1.CurrentCell = dataGridView1.Rows[rowIndex].Cells["PluginName"];
                    else
                        sbar2("Mod found but is inactive");

                    return;
                }
            }

            // Notify that the search query was not found.
            sbar2($"{txtSearchBox.Text} not found");
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Tools.ShowAbout();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveSettings();
            System.Windows.Forms.Application.Exit();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SavePlugins();
        }

        private void toolStripMenuBackup_Click(object sender, EventArgs e)
        {
            BackupPlugins();
        }

        private void toolStripMenuRestore_Click(object sender, EventArgs e)
        {
            RestorePlugins();
        }

        private void toolStripMenuEnableAll_Click(object sender, EventArgs e)
        {
            if (Tools.ConfirmAction("This will reset your current load order", "Enable all mods?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes || NoWarn)
                EnableAll();
        }

        private void toolStripMenuDisableAll_Click(object sender, EventArgs e)
        {
            if (Tools.ConfirmAction("This will reset your current load order", "Disable all mods?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes || NoWarn)
                DisableAll();
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FontSelect();
        }

        private void toolStripMenuCreations_Click(object sender, EventArgs e)
        {
            Tools.OpenUrl("https://creations.bethesda.net/en/starfield/all?platforms=PC&sort=latest_uploaded");
        }

        private void toolStripMenuNexus_Click(object sender, EventArgs e)
        {
            Tools.OpenUrl("https://www.nexusmods.com/games/starfield/mods");
        }

        private void txtSearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                SearchMod();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.SaveFileDialog saveDialog = new()
            {
                InitialDirectory = Properties.Settings.Default.ProfileFolder ??
                                   Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Filter = "Txt File|*.txt",
                Title = "Save Profile"
            };

            if (saveDialog.ShowDialog() != DialogResult.OK || string.IsNullOrEmpty(saveDialog.FileName))
                return;

            isModified = true;
            SaveLO(saveDialog.FileName);

            SaveProfileSettings(saveDialog.FileName);
        }

        private void SaveProfileSettings(string fileName)
        {
            Properties.Settings.Default.ProfileFolder = Path.GetDirectoryName(fileName);
            SaveSettings();
            SwitchProfile(fileName);
            GetProfiles();
            isModified = false;
        }

        private void SwitchProfile(string ProfileName)
        {
            if (log)
            {
#if DEBUG
                StackTrace stackTrace = new StackTrace();
                StackFrame frame = stackTrace.GetFrame(1); // Get the caller
                activityLog.WriteLog($"SwitchProfile called from {frame.GetMethod().Name} switching to {ProfileName}");
#else
                activityLog.WriteLog($"Switching profile to {ProfileName}");
#endif
            }
            if (!File.Exists(ProfileName))
                return;

            if (Properties.Settings.Default.CompareProfiles)
            {
                var currentProfile = File.ReadAllLines(Path.Combine(Tools.StarfieldAppData, "Plugins.txt")).ToList();
                var newProfile = File.ReadAllLines(ProfileName).ToList();

                var Difference = newProfile.Except(currentProfile)
                                           .Where(s => s.StartsWith('*'))
                                           .Select(s => $"New Profile added: {s.Replace("*", string.Empty).Replace("#", string.Empty)}")
                                           .Concat(currentProfile.Except(newProfile)
                                           .Where(s => s.StartsWith('*'))
                                           .Select(s => $"Previous Profile removed: {s.Replace("*", string.Empty).Replace("#", string.Empty)}"))
                                           .ToList();

                if (Difference.Count > 0)
                {
                    var existingForm = Application.OpenForms.OfType<frmProfileCompare>().FirstOrDefault(); // Check if the form is already open
                    existingForm?.Close(); // Close the existing form
                    Form fpc = new frmProfileCompare(Difference);// Create and show a new instance of the form
                    fpc.Show();
                    if (log)
                        foreach (var item in Difference)
                            activityLog.WriteLog($"Profile compare - {item}");
                }
            }

            try
            {
                File.Copy(ProfileName, Path.Combine(Tools.StarfieldAppData, "Plugins.txt"), true);
                Properties.Settings.Default.LastProfile = ProfileName[(ProfileName.LastIndexOf('\\') + 1)..];
                SaveSettings();
                isModified = false;
                InitDataGrid();
            }
            catch (Exception ex)
            {
                if (log)
                    activityLog.WriteLog($"Error switching profile: {ex.Message}");
                sbar2("Error switching profile");
                MessageBox.Show(ex.Message, "Error switching profile", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string profileFolder = Properties.Settings.Default.ProfileFolder
                                   ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            System.Windows.Forms.OpenFileDialog openPlugins = new()
            {
                InitialDirectory = profileFolder,
                Filter = "Txt File|*.txt",
                Title = "Load Profile"
            };

            if (openPlugins.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(openPlugins.FileName))
            {
                if (Profiles)
                {
                    Properties.Settings.Default.ProfileFolder = Path.GetDirectoryName(openPlugins.FileName) ?? profileFolder;
                    SwitchProfile(openPlugins.FileName);
                    GetProfiles();
                    SaveSettings();
                }
                else
                {
                    SwitchProfile(openPlugins.FileName);
                }
            }
        }

        private void DeleteLines()
        {
            foreach (DataGridViewRow row in dataGridView1.SelectedRows)
            {
                // Check if the row is not a new row
                if (!row.IsNewRow)
                {
                    if (log)
                        activityLog.WriteLog($"Deleting {row.Cells["PluginName"].Value} from Plugins.txt");
                    dataGridView1.Rows.Remove(row);
                }
            }

            isModified = true;
        }

        private void toolStripMenuDelete_Click(object sender, EventArgs e)
        {
            DeleteLines();
        }

        private void dataGridView1_KeyDown(object sender, KeyEventArgs e) // Keyboard shortcuts
        {
            switch (e.KeyCode)
            {
                case Keys.Delete:
                    if (dataGridView1.CurrentRow != null) // Prevent null reference exception
                        dataGridView1.Rows.RemoveAt(dataGridView1.CurrentRow.Index);
                    break;

                case Keys.S:
                    MoveDown();
                    break;

                case Keys.W:
                    MoveUp();
                    break;

                case Keys.A:
                    MoveTop();
                    break;

                case Keys.D:
                    MoveBottom();
                    break;

                case Keys.Space:
                    EnableDisable();
                    break;

                case Keys.R:
                    RunGame();
                    break;

                case Keys.F12:
                    MessageBox.Show("F12");
                    break;
            }
        }

        private void toolStripMenuScanMods_Click(object sender, EventArgs e)
        {
            AddMissing();
        }

        private void toolStripMenuSetPath_Click(object sender, EventArgs e)
        {
            if (GameVersion != MS)
                StarfieldGamePath = tools.SetStarfieldGamePath();
            else
                StarfieldGamePath = tools.SetStarfieldGamePathMS();
        }

        private void toolStripMenuCleanup_Click(object sender, EventArgs e)
        {
            RemoveMissing();
        }

        private int AddMissing()
        {
            int addedFiles = 0;
            if (!CheckGamePath() || string.IsNullOrEmpty(StarfieldGamePath))
                return 0;

            string directory = Path.Combine(StarfieldGamePath, "Data");
            List<string> pluginFiles = tools.GetPluginList(); // Add .esm files

            try
            {
                pluginFiles.AddRange(Directory.EnumerateFiles(directory, "*.esp", SearchOption.TopDirectoryOnly) // Add .esp files
                                              .Select(Path.GetFileName));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading plugin files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 0;
            }

            var existingPlugins = new HashSet<string>(
                dataGridView1.Rows.Cast<DataGridViewRow>()
                                 .Select(row => row.Cells["PluginName"].Value?.ToString())
                                 .Where(value => !string.IsNullOrEmpty(value))
            );

            var filesToAdd = pluginFiles.Except(existingPlugins)
                                        .Except(tools.BethFiles)
                                        .ToList();

            foreach (var file in filesToAdd)
            {
                int rowIndex = dataGridView1.Rows.Add();
                var row = dataGridView1.Rows[rowIndex];

                row.Cells["ModEnabled"].Value = file.Contains(".esm") && Properties.Settings.Default.ActivateNew; // Activate if preference set
                row.Cells["PluginName"].Value = file;

                addedFiles++;
                if (log)
                    activityLog.WriteLog($"Adding {file} to Plugins.txt");
            }

            if (addedFiles > 0)
            {
                isModified = true;
                SavePlugins(); // Save changes to Plugins.txt
            }

            sbar4($"Plugins added: {addedFiles}");
            if (log)
                activityLog.WriteLog($"Plugins added: {addedFiles}");
            return addedFiles;
        }

        private int RemoveMissing() // Remove missing .esm/.esp entries from Plugins.txt
        {
            int removedFiles = 0;

            if (!CheckGamePath() || string.IsNullOrEmpty(StarfieldGamePath))
                return 0; // Can't proceed without game path

            string directory = Path.Combine(StarfieldGamePath, "Data");
            List<string> pluginFiles = tools.GetPluginList(); // Get existing plugin files

            try
            {
                pluginFiles.AddRange(Directory.EnumerateFiles(directory, "*.esp", SearchOption.TopDirectoryOnly)
                                              .Select(Path.GetFileName));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading plugin files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 0;
            }

            var existingPlugins = new HashSet<string>(
                dataGridView1.Rows.Cast<DataGridViewRow>()
                                 .Select(row => row.Cells["PluginName"].Value?.ToString())
                                 .Where(value => !string.IsNullOrEmpty(value))
            );

            var filesToRemove = existingPlugins.Except(pluginFiles)
                                               .Union(tools.BethFiles) // Preserve Union here to remove both missing and base game files
                                               .ToList();

            foreach (var fileToRemove in filesToRemove)
            {
                var rowsToRemove = dataGridView1.Rows.Cast<DataGridViewRow>()
                                                    .Where(row => row.Cells["PluginName"].Value?.ToString() == fileToRemove)
                                                    .ToList();

                foreach (var row in rowsToRemove)
                {
                    if (log)
                        activityLog.WriteLog($"Removing {fileToRemove} from Plugins.txt");
                    dataGridView1.Rows.Remove(row);
                    removedFiles++;
                }
            }

            if (removedFiles > 0)
            {
                isModified = true;
                SavePlugins(); // Save changes to Plugins.txt
            }

            sbar4($"Plugins removed: {removedFiles}");
            if (log)
                activityLog.WriteLog($"Plugins removed: {removedFiles}");

            return removedFiles;
        }

        private int SyncPlugins()
        {
            // 1) Validate game path
            if (!CheckGamePath() || string.IsNullOrEmpty(StarfieldGamePath))
                return 0;

            sbar3("Updating...");
            statusStrip1.Refresh();
            dataGridView1.SuspendLayout();

            // 2) Gather all on-disk .esm + .esp plugin filenames with parallel processing
            var pluginFiles = tools.GetPluginList();
            string dataDir = Path.Combine(StarfieldGamePath, "Data");

            try
            {
                // Use parallel enumeration for large directories
                var espFiles = Directory.EnumerateFiles(dataDir, "*.esp", SearchOption.TopDirectoryOnly)
                                       .AsParallel()
                                       .Select(Path.GetFileName)
                                       .ToList();
                pluginFiles.AddRange(espFiles);
            }
            catch (Exception ex)
            {
                if (log)
                    activityLog.WriteLog($"Error reading plugin files: {ex.Message}");
                MessageBox.Show(
                    $"Error reading plugin files: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return 0;
            }

            // Pre-allocate with estimated capacity and use fastest comparer
            var onDisk = new HashSet<string>(pluginFiles.Count, StringComparer.Ordinal);
            var bethFilesSet = new HashSet<string>(tools.BethFiles.Count(), StringComparer.Ordinal);
            var inGrid = new HashSet<string>(dataGridView1.Rows.Count, StringComparer.Ordinal);
            var seenInGrid = new HashSet<string>(dataGridView1.Rows.Count, StringComparer.Ordinal);

            // Populate sets with bulk operations
            foreach (var file in pluginFiles) onDisk.Add(file);
            foreach (var file in tools.BethFiles) bethFilesSet.Add(file);

            // Single pass using unsafe array access patterns
            var rows = dataGridView1.Rows;
            var rowCount = rows.Count;
            var rowsToRemove = new List<DataGridViewRow>(rowCount / 4); // Pre-allocate estimate
            var logEntries = log ? new List<string>(rowCount / 2) : null; // Batch logging

            int dupRemoved = 0;
            int removed = 0;

            // Cache frequently used values
            var pluginNameIndex = dataGridView1.Columns["PluginName"].Index;

            // Process all rows
            for (int i = 0; i < rowCount; i++)
            {
                var row = rows[i];
                var cellValue = row.Cells[pluginNameIndex].Value;

                if (cellValue == null) continue;

                var pluginName = cellValue as string;
                if (string.IsNullOrEmpty(pluginName)) continue;

                // Duplicate check with immediate action
                if (!seenInGrid.Add(pluginName))
                {
                    rowsToRemove.Add(row);
                    dupRemoved++;
                    logEntries?.Add($"Removing duplicate entry {pluginName}");
                    continue;
                }

                // Removal check (not on disk OR is Beth file)
                if (!onDisk.Contains(pluginName) || bethFilesSet.Contains(pluginName))
                {
                    rowsToRemove.Add(row);
                    removed++;
                    logEntries?.Add($"Removing {pluginName} from Plugins.txt");
                }
                else
                {
                    inGrid.Add(pluginName);
                }
            }

            // Batch write logs to avoid I/O overhead
            if (log && logEntries?.Count > 0)
            {
                activityLog.WriteLog(string.Join(Environment.NewLine, logEntries));
            }

            // Removal using batch operations
            if (rowsToRemove.Count > 0)
            {
                // Sort indices descending for safe removal
                rowsToRemove.Sort((r1, r2) => r2.Index.CompareTo(r1.Index));

                var removalCounter = rowsToRemove.Count;

                // Batch UI updates every 10 removals to reduce overhead
                for (int i = 0; i < rowsToRemove.Count; i++)
                {
                    if (i % 10 == 0)
                    {
                        sbar($"Removing {removalCounter}");
                        statusStrip1.Refresh();
                    }
                    rows.Remove(rowsToRemove[i]);
                    removalCounter--;
                }
            }

            // 8) Addition with pre-computed values
            int added = 0;
            var activateNew = Properties.Settings.Default.ActivateNew;
            var modEnabledIndex = dataGridView1.Columns["ModEnabled"].Index;
            var addLogEntries = log ? new List<string>() : null;

            // Pre-filter and batch process additions
            var toAdd = new List<string>(onDisk.Count);
            foreach (var file in onDisk)
            {
                if (!inGrid.Contains(file) && !bethFilesSet.Contains(file))
                {
                    toAdd.Add(file);
                }
            }

            // Batch add rows with minimal overhead
            if (toAdd.Count > 0)
            {
                // Pre-allocate row capacity if supported
                var currentCapacity = rows.Count;

                foreach (var file in toAdd)
                {
                    int idx = rows.Add();
                    var row = rows[idx];

                    // Direct cell access for maximum speed
                    row.Cells[modEnabledIndex].Value = (file.Length > 4 &&
                        string.Equals(file.Substring(file.Length - 4), ".esm", StringComparison.Ordinal))
                        && activateNew;
                    row.Cells[pluginNameIndex].Value = file;

                    addLogEntries?.Add($"Adding {file} to Plugins.txt");
                    added++;
                }

                // Batch write addition logs
                if (log && addLogEntries?.Count > 0)
                {
                    activityLog.WriteLog(string.Join(Environment.NewLine, addLogEntries));
                }
            }

            // 9) Persist + summary log
            int totalChanges = dupRemoved + added + removed;
            if (totalChanges > 0)
            {
                if (log)
                    activityLog.WriteLog($"Plugins added: {added}, removed: {removed}, duplicates removed: {dupRemoved}");
                isModified = true;
                SavePlugins();
            }

            sbar3("Update complete");
            dataGridView1.ResumeLayout();
            return totalChanges;
        }

        private void toolStripMenuAutoClean_Click(object sender, EventArgs e)
        {
            sbar3($"Changes made: {SyncPlugins().ToString()}");
        }

        private void SaveWindowSettings()
        {
            Properties.Settings.Default.WindowLocation = this.Location; // Save window pos and size
            Properties.Settings.Default.WindowSize = this.Size;
        }

        private void frmLoadOrder_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveWindowSettings();

            if (isModified)
                SavePlugins();
            SaveSettings();
            if (log)
                activityLog.WriteLog("Shutting down");
        }

        private static void SaveSettings()
        {
            Properties.Settings.Default.Save();
        }

        private void cmbProfile_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (log)
            {
#if DEBUG
                StackTrace stackTrace = new StackTrace();
                StackFrame frame = stackTrace.GetFrame(1); // Get the caller
                activityLog.WriteLog($"cmbProfile_SelectedIndexChanged called from {frame.GetMethod().Name}");
#endif
            }
            SwitchProfile(Path.Combine(Properties.Settings.Default.ProfileFolder, (string)cmbProfile.SelectedItem));
        }

        private void InstallMod(string InstallMod = "")
        {
            string extractPath = Path.Combine(Path.GetTempPath(), "hstTools"), esmFile = "";
            bool SFSEMod = false, looseFileMod = false;
            int filesInstalled = 0;

            if (!CheckGamePath()) // Bail out if game path not set
                return;

            // Clean the extract directory if it exists.
            if (Directory.Exists(extractPath))
            {
                try { Directory.Delete(extractPath, true); }
                catch (Exception ex)
                {
                    if (log)
                        activityLog.WriteLog($"Error deleting temp directory: {ex.Message}");
                }
            }

            // Obtain the mod file path either from the parameter or by showing a file dialog.
            string modFilePath = InstallMod;
#if DEBUG
            //MessageBox.Show($"Installing {InstallMod}");
#endif
            if (string.IsNullOrEmpty(modFilePath))
            {
                using (System.Windows.Forms.OpenFileDialog openMod = new System.Windows.Forms.OpenFileDialog
                {
                    InitialDirectory = Properties.Settings.Default.DownloadsDirectory,
                    Filter = "Archive Files (*.zip;*.7z;*.rar)|*.zip;*.7z;*.rar|All Files (*.*)|*.*",
                    Title = "Install Mod - Loose files not supported except for SFSE plugins"
                })
                {
                    if (openMod.ShowDialog() == DialogResult.OK)
                        modFilePath = openMod.FileName;
                }
            }

            if (string.IsNullOrEmpty(modFilePath))
                return;

            if (log)
                activityLog.WriteLog($"Starting mod install: {modFilePath}");

            // Show a loading screen while extracting.
            Form loadScreen = new frmLoading("Extracting mod...");
            loadScreen.Show();

            try
            {
                // Use ArchiveFile to extract mod content to temp folder
                string[] searchPatterns = { "*.7z", "*.rar", "*.zip" };

                using (ArchiveFile archiveFile = new ArchiveFile(modFilePath))
                {
                    sbar2($"Extracting: {modFilePath}");
                    statusStrip1.Refresh();
                    if (log)
                        activityLog.WriteLog($"Extracting: {modFilePath}");
                    archiveFile.Extract(extractPath);
                    if (Directory.Exists(Path.Combine(extractPath, "fomod")))
                    {
                        if (Tools.ConfirmAction("Attempt installation anyway?", "Fomod detected - mod will probably not install correctly", MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question) != DialogResult.Yes)
                        {
                            if (Directory.Exists(extractPath))
                                Directory.Delete(extractPath, true);
                            loadScreen.Close();
                            return;
                        }
                    }

                    // Check for embedded archive
                    if (Directory.Exists(extractPath))
                    {
                        foreach (string pattern in searchPatterns)
                        {
                            string[] archiveFiles = Directory.GetFiles(extractPath, pattern, SearchOption.AllDirectories);

                            if (archiveFiles.Length > 0)
                            {
                                foreach (string file in archiveFiles)
                                {
                                    using (ArchiveFile archiveFile2 = new ArchiveFile(file))
                                    {
                                        sbar2($"Extracting embedded archive: {file}");
                                        statusStrip1.Refresh();
                                        archiveFile2.Extract(extractPath);
                                        if (log)
                                            activityLog.WriteLog($"Extracting embedded archive: {file}");
                                    }
                                }
                            }
                        }
                    }
                }

                // Update the downloads directory setting for future use.
                Properties.Settings.Default.DownloadsDirectory = Path.GetDirectoryName(modFilePath);
                SaveSettings();
            }
            catch (Exception ex)
            {
                if (log)
                    activityLog.WriteLog($"Error extracting mod: {ex.Message}");
                MessageBox.Show(ex.Message);
                loadScreen.Close();
                return;
            }

            if (Directory.EnumerateFiles(extractPath, "*.esm", SearchOption.AllDirectories).Any())
                esmFile = Directory.GetFiles(extractPath, "*.esm", SearchOption.AllDirectories).FirstOrDefault();

            // Move .esm and .ba2 files to the game's Data folder.
            filesInstalled += MoveExtractedFiles("*.esm", "esm");
            filesInstalled += MoveExtractedFiles("*.ba2", "archive");

            // Install SFSE plugin if found.
            try
            {
                string[] sfseDirs = Directory.GetDirectories(extractPath, "SFSE", SearchOption.AllDirectories);
                if (sfseDirs.Length > 0)
                    SFSEMod = true;

                foreach (string dir in sfseDirs)
                {
                    tempstr = Path.Combine(StarfieldGamePath, "Data", "SFSE");
                    CopyDirectory(dir, tempstr);
                    filesInstalled++;
                }
            }
            catch (Exception ex)
            {
                if (log)
                    activityLog.WriteLog($"Error installing SFSE mod: {ex.Message}");
                MessageBox.Show($"An error occurred: {ex.Message}");
            }

            // Install Loose files
            List<string> looseFileDirs = Tools.LooseFolderDirsOnly; // Get the list of loose file directories from the Tools class

            // Define the target directory
            var targetDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"My Games\Starfield\Data");

            // Ensure the target directory exists
            Directory.CreateDirectory(targetDir);

            // Recursively search for each directory and copy its contents
            foreach (var dirName in looseFileDirs)
            {
#if DEBUG
                Debug.WriteLine($"Searching for {dirName} in {extractPath}");
#endif
                var directoriesFound = Directory.GetDirectories(extractPath, dirName, SearchOption.AllDirectories);

                foreach (var sourceDir in directoriesFound)
                {
                    CopyDirectory(sourceDir, Path.Combine(targetDir, dirName));
                    if (log)
                        activityLog.WriteLog($"Copying {sourceDir} to {Path.Combine(targetDir, dirName)}");
                    filesInstalled++;
                }
                if (directoriesFound.Length > 0)
                    looseFileMod = true;
            }

            loadScreen.Close();

            // Clean up any temporary extraction files.
            if (Directory.Exists(extractPath))
                Directory.Delete(extractPath, true);

            // Display status messages based on what was installed.

            if (looseFileMod)
            {
                LooseFilesOnOff(true);
                sbar3($"Directories installed (loose files): {filesInstalled}");
                if (log)
                    activityLog.WriteLog($"Directories installed (loose files): {filesInstalled}");
                if (Tools.ConfirmAction("Do you want to convert them", "Loose Files found", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    if (esmFile != "")
                        ConvertLooseFiles(Path.GetFileNameWithoutExtension(esmFile));
                    else
                        ConvertLooseFiles();
                    sbar3("Converted loose files to archives");
                    if (log && returnStatus > 0)
                        activityLog.WriteLog("Converted loose files to archives");
                }
            }
            if (SFSEMod)
            {
                sbar3("SFSE mod installed");
                if (log)
                    activityLog.WriteLog("SFSE mod installed");
            }
            if (filesInstalled > 0)
                UpdatePlugins();

            if (log)
                activityLog.WriteLog($"Mod files installed: {filesInstalled}");

            sbar2("");
            return;

            // Helper local function that moves extracted files with confirmation if a destination file exists.
            int MoveExtractedFiles(string searchPattern, string fileTypeLabel)
            {
                int count = 0;
                foreach (string modFile in Directory.EnumerateFiles(extractPath, searchPattern, SearchOption.AllDirectories))
                {
                    string fileName = Path.GetFileName(modFile);
                    string destinationPath = Path.Combine(StarfieldGamePath, "Data", fileName);

                    if (File.Exists(destinationPath))
                    {
                        if (Tools.ConfirmAction($"Overwrite {fileTypeLabel} {destinationPath}", "Replace mod?", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            File.Move(modFile, destinationPath, true);
                            if (log)
                                activityLog.WriteLog($"Moving {modFile} to {destinationPath}");
                            count++;
                        }
                        else
                        {
                            // If the user declines to overwrite, break out of this file loop
                            break;
                        }
                    }
                    else
                    {
                        File.Move(modFile, destinationPath, true);
                        if (log)
                            activityLog.WriteLog($"Installing {modFile}");
                        count++;
                    }
                }
                return count;
            }

            // Local static recursive function to copy an entire directory.
            static void CopyDirectory(string sourceDir, string destinationDir)
            {
                //ActivityLog activityLog2 = new ActivityLog(Path.Combine(Tools.LocalAppDataPath, "Activity Log.txt"));

                // Get information about the source directory
                var dir = new DirectoryInfo(sourceDir);

                // Check if the source directory exists
                if (!dir.Exists)
                {
                    throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");
                }

                // Create the destination directory
                Directory.CreateDirectory(destinationDir);

                // Copy files in the source directory to the destination directory
                foreach (var file in dir.GetFiles())
                {
                    string targetFilePath = Path.Combine(destinationDir, file.Name);
                    if (Properties.Settings.Default.Log)
                        activityLog.WriteLog($"Copying {file.FullName} to {targetFilePath}");
                    file.CopyTo(targetFilePath, overwrite: true);
                }

                // Recursively copy subdirectories
                foreach (var subDir in dir.GetDirectories())
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir);
                }
            }
        }

        private void toolStripMenuInstall_Click(object sender, EventArgs e)
        {
            InstallMod();
        }

        private void chkProfile_CheckedChanged(object sender, EventArgs e)
        {
            Profiles = chkProfile.Checked;
            cmbProfile.Enabled = chkProfile.Checked;
        }

        private void toolStripMenuExportMods_Click(object sender, EventArgs e)
        {
            var exportDialog = new System.Windows.Forms.SaveFileDialog
            {
                Filter = "Txt File|*.txt",
                Title = "Export Active Plugins",
                FileName = "Plugins.txt"
            };

            if (exportDialog.ShowDialog() != DialogResult.OK)
                return;

            var exportMods = new List<string>();
            string currentGroup = string.Empty;

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                //if (row.Cells["ModEnabled"].Value is bool enabled && enabled)
                if (row.Visible)
                {
                    string group = row.Cells["Group"].Value?.ToString();
                    if (!string.IsNullOrEmpty(group) && group != currentGroup)
                    {
                        currentGroup = group;
                        exportMods.Add("\n# " + currentGroup);
                    }

                    string pluginName = row.Cells["PluginName"].Value?.ToString() ?? string.Empty;
                    exportMods.Add("*" + pluginName);
                }
            }

            if (exportMods.Count == 0)
            {
                sbar3("Nothing to export");
                return;
            }

            // Remove the extra newline from the very first group header if needed.
            if (exportMods[0].StartsWith("\n# "))
                exportMods[0] = exportMods[0].Substring(1);

            string header = "# Exported active mod list from hst Starfield Tools";
            if (Profiles)
                header += " using profile " + cmbProfile.Text;

            using (StreamWriter writer = new StreamWriter(exportDialog.FileName))
            {
                writer.WriteLine(header);
                writer.WriteLine();
                foreach (string line in exportMods)
                {
                    writer.WriteLine(line);
                }
            }

            sbar3("Export done");
            if (Tools.ConfirmAction("Open exported file", "", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                Process.Start("explorer.exe", exportDialog.FileName);
        }

        private void toolStripMenuDeleteLine_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.RemoveAt(dataGridView1.CurrentRow.Index);
        }

        private void toolStripMenuExploreData_Click(object sender, EventArgs e)
        {
            Tools.OpenFolder(Path.Combine(StarfieldGamePath, "Data"));
        }

        private void toolStripMenuExploreAppData_Click(object sender, EventArgs e)
        {
            Tools.OpenFolder(Tools.StarfieldAppData);
        }

        private void dataGridView1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
                contextMenuDatagrid.Show(this, new Point(e.X, e.Y));

            // Get the index of the item the mouse is below
            rowIndexFromMouseDown = dataGridView1.HitTest(e.X, e.Y).RowIndex;
            if (rowIndexFromMouseDown != -1)
            {
                // Remember the point where the mouse down occurred
                System.Drawing.Size dragSize = SystemInformation.DragSize;
                dragBoxFromMouseDown = new Rectangle(new Point(e.X - (dragSize.Width / 2), e.Y - (dragSize.Height / 2)), dragSize);
            }
            else
            {
                dragBoxFromMouseDown = Rectangle.Empty;
            }
        }

        private void toolStripMenuUp_Click(object sender, EventArgs e)
        {
            MoveUp();
        }

        private void toolStripMenuDown_Click(object sender, EventArgs e)
        {
            MoveDown();
        }

        private void toolStripMenuTop_Click(object sender, EventArgs e)
        {
            MoveTop();
        }

        private void toolStripMenuBottom_Click(object sender, EventArgs e)
        {
            MoveBottom();
        }

        private void toolStripMenuDelContext_Click(object sender, EventArgs e)
        {
            DeleteLines();
        }

        private void UninstallMod()
        {
            if (dataGridView1.SelectedRows.Count == 0)
            {
                sbar2("No rows selected for uninstallation.");
                return;
            }

            if (!CheckGamePath()) // Game path folder needs to be set
                return;

            List<string> files = new();

            // Create a copy of selected rows to avoid collection-modification issues.
            var selectedRows = dataGridView1.SelectedRows.Cast<DataGridViewRow>().ToList();
            string dataDirectory = Path.Combine(StarfieldGamePath, "Data");

            foreach (var row in selectedRows)
            {
                // Get the mod name from the PluginName cell (before the first dot).
                string pluginName = row.Cells["PluginName"].Value?.ToString() ?? string.Empty;
                int dotIndex = pluginName.LastIndexOf('.');
                if (dotIndex < 0)
                    continue;

                string modName = pluginName.Substring(0, dotIndex);

                if (log)
                    activityLog.WriteLog($"Starting uninstall for mod: {modName}");

                if (Tools.ConfirmAction(
                        $"This will delete all files related to the '{modName}' mod",
                        $"Delete {modName} - Are you sure?",
                        MessageBoxButtons.YesNo) == DialogResult.Yes || NoWarn)
                {
                    isModified = true;
                    dataGridView1.Rows.Remove(row);

                    // Build the base file path.
                    string modBasePath = Path.Combine(dataDirectory, modName);

                    // Special handling: if a .esp file exists, delete it, save and skip further deletions.
                    string espFile = modBasePath + ".esp";
                    if (File.Exists(espFile))
                    {
                        File.Delete(espFile);
                        if (log)
                            activityLog.WriteLog($"Deleted: {espFile}");
                        SavePlugins();
                        sbar3("esp uninstalled - esm and archive files skipped");
                        continue;
                    }

                    // Define the file extensions for the mod files to delete.

                    var extensions = new string[]
                    {
                ".esm",
                " - textures.ba2",
                " - Textures_xbox.ba2",
                " - main.ba2",
                " - voices_de.ba2",
                " - voices_en.ba2",
                " - voices_es.ba2",
                " - voices_fr.ba2",
                " - voices_ja.ba2"
                    };

                    // Loop over each extension and delete the file if it exists.
                    foreach (var ext in extensions)
                    {
                        string filePath = modBasePath + ext;
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                            if (log)
                                activityLog.WriteLog($"Deleted: {filePath}");
                        }
                    }

                    // match files like 'modname - textures.ba2', 'modname - textures01.ba2', 'modname - textures02.ba2', etc.
                    string directoryPath = StarfieldGamePath + "\\Data\\";
                    string[] textureFiles = Directory.GetFiles(directoryPath, modName + "* - textures*.ba2");

                    foreach (string file in textureFiles)
                    {
                        File.Delete(file);
                        if (log)
                            activityLog.WriteLog($"Deleted: {file}");
                    }

                    SavePlugins();
                    sbar3($"Mod '{modName}' uninstalled.");
                }
                else
                {
                    sbar2($"Un-install of '{modName}' cancelled.");
                }
            }
        }

        private void toolStripMenuUninstallContext_Click(object sender, EventArgs e)
        {
            UninstallMod();
        }

        private void EnableDisable()
        {
            if (GridSorted)
                return;

            if (Tools.BlockedMods().Contains((string)dataGridView1.CurrentRow.Cells["PluginName"].Value))
            {
                sbar2("Mod is blocked");
                return;
            }
            isModified = true;
            foreach (var row in dataGridView1.SelectedRows)
            {
                DataGridViewRow currentRow = (DataGridViewRow)row;
                currentRow.Cells["ModEnabled"].Value = !(bool)(currentRow.Cells["ModEnabled"].Value);
            }

            if (log)
                activityLog.WriteLog($"Enable/Disable mod: {dataGridView1.CurrentRow.Cells["PluginName"].Value}, {dataGridView1.CurrentRow.Cells["ModEnabled"].Value}");
            SavePlugins();
        }

        private void toolStripMenuEnableDisable_Click(object sender, EventArgs e)
        {
            EnableDisable();
        }

        private void toolStripMenuRefresh_Click(object sender, EventArgs e)
        {
            RefreshDataGrid();
        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            EnableDisable();
        }

        private void toolStripMenuRunMS_Click(object sender, EventArgs e)
        {
            RunGame();
        }

        private void toolStripMenuInstallMod_Click(object sender, EventArgs e)
        {
            InstallMod();
        }

        private void SavePlugins()
        {
            string pluginPath = Path.Combine(Tools.StarfieldAppData, @"Plugins.txt");
            SaveLO(pluginPath);

            if (Profiles && !string.IsNullOrEmpty(cmbProfile.Text))
            {
                string profilePath = Path.Combine(Properties.Settings.Default.ProfileFolder, cmbProfile.Text);

                if (!Tools.FileCompare(pluginPath, profilePath))
                {
                    SaveLO(profilePath); // Save profile if updated
                    toolStripStatus2.Text += $", {cmbProfile.Text} profile saved";
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            SavePlugins();
            SaveSettings();
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            RunGame();
        }

        private static void ShowSplashScreen()
        {
            Form SS = new frmSplashScreen();
            SS.Show();
        }

        private void RunGame()
        {
            bool result;

            tempstr = Properties.Settings.Default.RunProgramPath;
            if (Properties.Settings.Default.RunProgram && !string.IsNullOrEmpty(tempstr))
                if (File.Exists(tempstr))
                {
                    if (Process.GetProcessesByName(Path.GetFileNameWithoutExtension(tempstr)).Length == 0)
                        Process.Start(tempstr); // Start the configured program before running the game.
                }
                else
                    MessageBox.Show($"Run program path not found: {tempstr}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            Properties.Settings.Default.GameVersion = GameVersion;
            SaveSettings();
            Form SS = new frmSplashScreen();

            sbar("Starting game...");
            if (GameVersion != MS)
            {
                if (Properties.Settings.Default.LoadScreenEnabled)
                    SS.Show();
            }

            if (isModified)
                SavePlugins();

            result = Tools.StartGame(GameVersion);
            if (log)
                activityLog.WriteLog($"Game started: {GameVersion}, Status: {result}");

            if (!result)
            {
                timer1.Stop();
                SS.Close();
            }
            else
                timer1.Start();
        }

        private void toolStripMenAddRemoveContext_Click(object sender, EventArgs e)
        {
            SyncPlugins();
        }

        private void toolStripMenuBGSStarfield_Click(object sender, EventArgs e)
        {
            Tools.OpenUrl("https://discord.com/channels/784542837596225567/1083043812949110825");
        }

        private void toolStripMenuBGSX_Click(object sender, EventArgs e)
        {
            Tools.OpenUrl("https://x.com/StarfieldGame");
        }

        private void toolStripMenuViewWebSite_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow selectedRow in dataGridView1.SelectedRows)
            {
                if (selectedRow.Cells["URL"].Value is string url && !string.IsNullOrEmpty(url))
                {
                    Tools.OpenUrl(url); // Open website in default browser
                }
                else
                {
                    sbar3($"No link for mod in row {selectedRow.Index + 1}");
                }
            }
        }

        private void toolStripMenuUninstall_Click(object sender, EventArgs e)
        {
            UninstallMod();
        }

        private void btnQuit_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Application.Exit();
        }

        private void toolStripMenuGitHub_Click(object sender, EventArgs e)
        {
            Tools.OpenUrl("https://github.com/hst12/Starfield-Tools---ContentCatalog.txt-fixer");
        }

        private void toolStripMenuAchievements_Click(object sender, EventArgs e)
        {
            toolStripMenuAchievements.Checked = !toolStripMenuAchievements.Checked;
            dataGridView1.Columns["Achievements"].Visible = toolStripMenuAchievements.Checked;
            Properties.Settings.Default.Achievements = toolStripMenuAchievements.Checked;
        }

        private void toolStripMenuCreationsID_Click(object sender, EventArgs e)
        {
            toolStripMenuCreationsID.Checked = !toolStripMenuCreationsID.Checked;
            dataGridView1.Columns["CreationsID"].Visible = toolStripMenuCreationsID.Checked;
            Properties.Settings.Default.CreationsID = toolStripMenuCreationsID.Checked;
        }

        private void toolStripMenuFiles_Click(object sender, EventArgs e)
        {
            toolStripMenuFiles.Checked = !toolStripMenuFiles.Checked;
            dataGridView1.Columns["Files"].Visible = toolStripMenuFiles.Checked;
            Properties.Settings.Default.Files = toolStripMenuFiles.Checked;
        }

        private void toolStripMenuLoadingScreen_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";
            openFileDialog1.Title = "Choose a loadscreen image";
            DialogResult LoadScreen = openFileDialog1.ShowDialog();
            if (LoadScreen == DialogResult.OK)
            {
                if (openFileDialog1.FileName != "")
                    Properties.Settings.Default.LoadScreenFilename = openFileDialog1.FileName;
            }
        }

        private void RunLOOT(bool LOOTMode) // True for autosort
        {
            bool profilesActive = Profiles;
            string lootPath = Properties.Settings.Default.LOOTPath;

            if (log)
                activityLog.WriteLog($"Running LOOT: {LOOTMode}");

            if (isModified) SavePlugins();

            // Try detecting LOOT if installed in default location
            if (string.IsNullOrEmpty(lootPath) && File.Exists(@"C:\Program Files\LOOT\LOOT.exe"))
            {
                lootPath = @"C:\Program Files\LOOT\LOOT.exe";
                Properties.Settings.Default.LOOTPath = lootPath;
                SaveSettings();
            }

            // Prompt for LOOT path if not found
            if (string.IsNullOrEmpty(lootPath) && !SetLOOTPath())
            {
                sbar2("LOOT path is required to run LOOT");
                return;
            }

            lootPath = Properties.Settings.Default.LOOTPath;
            string cmdLine = (GameVersion != MS) ? "--game Starfield" : "--game \"Starfield (MS Store)\"";
            if (LOOTMode) cmdLine += " --auto-sort";

            // Temporarily disable profiles
            Profiles = false;
            cmbProfile.Enabled = false;
            chkProfile.Checked = false;

            // Start LOOT process and wait for it to close
            ProcessStartInfo startInfo = new()
            {
                FileName = lootPath,
                Arguments = cmdLine,
                WorkingDirectory = Path.GetDirectoryName(lootPath) ?? string.Empty
            };

            using (Process process = Process.Start(startInfo))
            {
                process.WaitForExit();
                ReadLOOTGroups();
            }

            if (Properties.Settings.Default.AutoDelccc) Delccc();
            //RefreshDataGrid();
            InitDataGrid();

            // Remove base game files if LOOT added them
            tools.BethFiles.ForEach(bethFile =>
            {
                var rowToRemove = dataGridView1.Rows
                    .Cast<DataGridViewRow>()
                    .FirstOrDefault(row => row.Cells["PluginName"].Value as string == bethFile);

                if (rowToRemove != null) dataGridView1.Rows.Remove(rowToRemove);
            });

            // Re-enable profiles if previously active
            Profiles = profilesActive;
            isModified = true;
            SavePlugins();
            cmbProfile.Enabled = Profiles;
            chkProfile.Checked = Profiles;
        }

        private void toolStripMenuLoot_Click(object sender, EventArgs e)
        {
            RunLOOT(true);
        }

        private bool SetLOOTPath()
        {
            openFileDialog1.InitialDirectory = Properties.Settings.Default.LOOTPath;
            openFileDialog1.Filter = "Executable Files|*.exe";
            openFileDialog1.Title = "Set the path to the LOOT executable";
            openFileDialog1.FileName = "LOOT.exe";

            if (openFileDialog1.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(openFileDialog1.FileName))
            {
                Properties.Settings.Default.LOOTPath = openFileDialog1.FileName;
                SaveSettings();
                MessageBox.Show($"LOOT path set to {openFileDialog1.FileName}", "Restart the app for changes to take effect");
                return true;
            }
            return false;
        }

        private void toolStripMenuLootPath_Click(object sender, EventArgs e)
        {
            SetLOOTPath();
        }

        private void sbar(string StatusBarMessage)
        {
            toolStripStatusStats.Text = StatusBarMessage;
        }

        private void sbar2(string StatusBarMessage)
        {
            toolStripStatus2.Text = StatusBarMessage;
        }

        private void sbar3(string StatusBarMessage)
        {
            toolStripStatus3.Text = StatusBarMessage;
        }

        private void sbar4(string StatusBarMessage)
        {
            toolStripStatusLabel4.Text = StatusBarMessage;
        }

        private void sbar5(string StatusMessage)
        {
            toolStripStatusLabel5.Text = StatusMessage;
        }

        private void sbarCCC(string sbarMessage)
        {
            toolStripStatus1.Text = sbarMessage;
        }

        private void sbarCCCOn()
        {
            toolStripStatus1.Text = "Auto delete Starfield.ccc: On";
        }

        private void sbarCCCOff()
        {
            toolStripStatus1.Text = "Auto delete Starfield.ccc: Off";
        }

        private void toolStripMenuLoot_Click_1(object sender, EventArgs e)
        {
            RunLOOT(false);
        }

        private void toolStripMenuEditPlugins_Click(object sender, EventArgs e)
        {
            Tools.OpenFile(Path.Combine(Tools.StarfieldAppData, "Plugins.txt"));
        }

        private void toolStripMenuGroup_Click(object sender, EventArgs e)
        {
            toolStripMenuGroup.Checked = !toolStripMenuGroup.Checked;
            dataGridView1.Columns["Group"].Visible = toolStripMenuGroup.Checked;
            Properties.Settings.Default.Group = toolStripMenuGroup.Checked;
        }

        private void toolStripMenuLootAutoSort_Click(object sender, EventArgs e)
        {
            RunLOOT(true);
        }

        private void toolStripMenuExploreGameDocs_Click(object sender, EventArgs e)
        {
            try
            {
                Tools.OpenFolder(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"My Games\Starfield"));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Run the game to correct this", "Error opening Starfield game documents folder");
                if (log)
                    activityLog.WriteLog("Error opening Starfield game documents folder: " + ex.Message);
            }
        }

        private bool Delccc()
        {
            try
            {
                string Starfieldccc = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"My Games\Starfield\Starfield.ccc");
                if (File.Exists(Starfieldccc))
                {
                    File.Delete(Starfieldccc);
                    sbar3("Starfield.ccc deleted");
                    if (log)
                        activityLog.WriteLog("Starfield.ccc deleted");
                    return true;
                }
                else
                {
                    sbar3("Starfield.ccc not found");
                    return false;
                }
            }
            catch (Exception ex)
            {
                if (log)
                    activityLog.WriteLog("Error deleting Starfield.ccc: " + ex.Message);
#if DEBUG
                MessageBox.Show(ex.Message);
#endif
                sbar3("Error deleting Starfield.ccc " + ex.Message);
                return false;
            }
        }

        private void toolStripMenuDeleteCCC_Click(object sender, EventArgs e)
        {
            Delccc();
        }

        private void toolStripMenuAutoDelccc_Click(object sender, EventArgs e)
        {
            toolStripMenuAutoDelccc.Checked = !toolStripMenuAutoDelccc.Checked;
            if (toolStripMenuAutoDelccc.Checked)
                sbarCCCOn();
            else
                sbarCCCOff();
            Properties.Settings.Default.AutoDelccc = toolStripMenuAutoDelccc.Checked;
        }

        private void toolStripMenuProfilesOn_Click(object sender, EventArgs e)
        {
            toolStripMenuProfilesOn.Checked = !toolStripMenuProfilesOn.Checked;
            Properties.Settings.Default.ProfileOn = toolStripMenuProfilesOn.Checked;
            Profiles = toolStripMenuProfilesOn.Checked;
            chkProfile.Checked = toolStripMenuProfilesOn.Checked;

            if (Profiles) GetProfiles();
        }

        private void toolStripMenuLoadScreenPreview_Click(object sender, EventArgs e)
        {
            ShowSplashScreen();
        }

        private void toolStripMenuIndex_Click(object sender, EventArgs e)
        {
            toolStripMenuIndex.Checked = !toolStripMenuIndex.Checked;
            dataGridView1.Columns["Index"].Visible = toolStripMenuIndex.Checked;
            Properties.Settings.Default.Index = toolStripMenuIndex.Checked;
        }

        private void dataGridView1_MouseMove(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                // If the mouse moves outside the rectangle, start the drag
                try
                {
                    if (dragBoxFromMouseDown != Rectangle.Empty && !dragBoxFromMouseDown.Contains(e.X, e.Y))
                    {
                        DragDropEffects dropEffect = dataGridView1.DoDragDrop(dataGridView1.Rows[rowIndexFromMouseDown], DragDropEffects.Move);
                    }
                }
                catch (Exception ex)
                {
#if DEBUG
                    MessageBox.Show(ex.Message);
#endif
                }
            }
        }

        private void dataGridView1_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void dataGridView1_DragDrop(object sender, DragEventArgs e)
        {
            int ModCounter = 0;

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var item in files)
                {
                    ModCounter++;
                    InstallMod(item);
                    isModified = true;
                    SavePlugins();
                }
                if (AutoSort)
                    RunLOOT(true);
                sbar3(ModCounter + " Mods installed");
                return;
            }

            // Convert screen coordinates to client coordinates
            Point clientPoint = dataGridView1.PointToClient(new Point(e.X, e.Y));
            rowIndexOfItemUnderMouseToDrop = dataGridView1.HitTest(clientPoint.X, clientPoint.Y).RowIndex;

            // If the drag operation was a move, remove and insert the row
            if (e.Effect == DragDropEffects.Move)
            {
                DataGridViewRow rowToMove = e.Data.GetData(typeof(DataGridViewRow)) as DataGridViewRow;
                dataGridView1.Rows.RemoveAt(rowIndexFromMouseDown);
                dataGridView1.Rows.Insert(rowIndexOfItemUnderMouseToDrop, rowToMove);
                isModified = true;
                SavePlugins();
            }
        }

        private void toolStripMenuCustom_Click(object sender, EventArgs e)
        {
            if (!GameSwitchWarning())
                return;

            string CustomEXEFolder;

            CustomEXEFolder = Properties.Settings.Default.CustomEXE;

            System.Windows.Forms.OpenFileDialog OpenEXE = new()
            {
                InitialDirectory = CustomEXEFolder,
                Filter = "exe File|*.exe",
                Title = "Select custom game executable"
            };

            DialogResult result = OpenEXE.ShowDialog();
            if (DialogResult.OK == result)
            {
                if (OpenEXE.FileName != "")
                {
                    Properties.Settings.Default.CustomEXE = OpenEXE.FileName;
                    SaveSettings();
                }
            }

            toolStripMenuCustom.Checked = !toolStripMenuCustom.Checked;
            if (toolStripMenuCustom.Checked)
            {
                GameVersion = Custom;
                UpdateGameVersion();
                toolStripMenuSteam.Checked = false;
                toolStripMenuMS.Checked = false;
                gameVersionSFSEToolStripMenuItem.Checked = false;

                Properties.Settings.Default.GameVersion = GameVersion;
                SaveSettings();
            }
        }

        private void toolStripMenuRunCustom_Click(object sender, EventArgs e)
        {
            RunGame();
        }

        private string CheckCatalog()
        {
            frmStarfieldTools StarfieldTools = new();
            if (log)
                activityLog.WriteLog("Starting Catalog Checker");
            StarfieldTools.Show();
            sbar4(StarfieldTools.CatalogStatus);
            return StarfieldTools.CatalogStatus;
        }

        private void toolStripMenuCatalog_Click(object sender, EventArgs e)
        {
            CheckCatalog();
        }

        private void toolStripMenuShortcuts_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("explorer.exe", Path.Combine(Tools.DocumentationFolder, "Shortcuts.txt"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error opening Shortcuts.txt");
            }
        }

        private void GameVersionDisplay()
        {
            string version = GameVersion switch
            {
                Steam => "Game version - Steam",
                MS => "Game version - MS",
                Custom => $"Game version - Custom - {Properties.Settings.Default.CustomEXE}",
                SFSE => "Game version - SFSE",
                _ => "Unknown game version"
            };

            sbar2(version);
        }

        private bool ResetStarfieldCustomINI(bool ConfirmOverwrite)  // true for confirmation
        {
            if (ConfirmOverwrite)
            {
                DialogResult DialogResult = MessageBox.Show("This will overwrite your StarfieldCustom.ini to a recommended version", "Are you sure?",
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Stop);
                if (DialogResult != DialogResult.OK)
                    return false;
            }

            try
            {
                if (!Tools.FileCompare(Path.Combine(Tools.CommonFolder, "StarfieldCustom.ini"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    @"My Games\Starfield\StarfieldCustom.ini"))) // Check if StarfieldCustom.ini needs resetting
                {
                    File.Copy(Path.Combine(Tools.CommonFolder, "StarfieldCustom.ini"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        @"My Games\Starfield\StarfieldCustom.ini"), true);
                    sbar3("StarfieldCustom.ini restored");
                    if (log)
                        activityLog.WriteLog("StarfieldCustom.ini restored");
                    return true;
                }
                else
                    return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error restoring StarfieldCustom.ini");
                if (log)
                    activityLog.WriteLog("Error restoring StarfieldCustom.ini: " + ex.Message);
                return false;
            }
        }

        private void toolStripMenuResetStarfieldCustom_Click(object sender, EventArgs e)
        {
            ResetStarfieldCustomINI(true);
        }

        private void editStarfieldCustominiToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Tools.OpenFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"My Games\Starfield\StarfieldCustom.ini"));
        }

        private void editContentCatalogtxtToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Tools.OpenFile(Tools.GetCatalogPath());
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();
            this.Close();
        }

        private void toolStripMenuRun_Click(object sender, EventArgs e)
        {
            RunGame();
        }

        private static bool GameSwitchWarning()
        {
            return (Tools.ConfirmAction("Do you want to proceed?\nAn app restart may be required to enable certain features.",
                "Switching to a no mods profile is suggested before proceeding",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question, true) == DialogResult.Yes);
        }

        private void toolStripMenuSteam_Click(object sender, EventArgs e)
        {
            if (GameVersion == MS)
                if (!GameSwitchWarning())
                    return;

            toolStripMenuSteam.Checked = true;
            toolStripMenuMS.Checked = false;
            toolStripMenuCustom.Checked = false;
            gameVersionSFSEToolStripMenuItem.Checked = false;
            GameVersion = Steam;
            UpdateGameVersion();
        }

        private void toolStripMenuMS_Click(object sender, EventArgs e)
        {
            if (!GameSwitchWarning())
                return;
            toolStripMenuMS.Checked = true;
            toolStripMenuSteam.Checked = false;
            toolStripMenuCustom.Checked = false;
            gameVersionSFSEToolStripMenuItem.Checked = false;
            GameVersion = MS;
            UpdateGameVersion();
        }

        private void toolStripMenuFileSize_Click(object sender, EventArgs e)
        {
            toolStripMenuFileSize.Checked = !toolStripMenuFileSize.Checked;
            dataGridView1.Columns["FileSize"].Visible = toolStripMenuFileSize.Checked;
            Properties.Settings.Default.FileSize = toolStripMenuFileSize.Checked;
        }

        private void compareStarfieldCustominiToBackupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            sbar3(CheckStarfieldCustom() ? "Same" : "Modified");
        }

        private void toolStripMenuExploreCommon_Click(object sender, EventArgs e)
        {
            Tools.OpenFolder(Tools.CommonFolder);
            sbar3("Restart the application for any changes to take effect");
        }

        private void timeStampToolStripMenuItem_Click(object sender, EventArgs e)
        {
            timeStampToolStripMenuItem.Checked = !timeStampToolStripMenuItem.Checked;
            if (timeStampToolStripMenuItem.Checked)
                dataGridView1.Columns["TimeStamp"].Visible = true;
            else
                dataGridView1.Columns["TimeStamp"].Visible = false;
            Properties.Settings.Default.TimeStamp = timeStampToolStripMenuItem.Checked;
        }

        private void resetLoadScreenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.LoadScreenFilename = "";
            SaveSettings();
        }

        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshDataGrid();
        }

        private static int CheckAndDeleteINI(string FileName)
        {
            string FolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"My Games\Starfield");
            if (File.Exists(Path.Combine(FolderPath, FileName)))
            {
                File.Delete(Path.Combine(FolderPath, FileName));
                return 1;
            }
            else
                return 0;
        }

        private int UndoVortexChanges(bool ConfirmPrompt)  // true to confirm
        {
            int ChangeCount = 0;

            if (ConfirmPrompt)
            {
                DialogResult DialogResult = MessageBox.Show("Are you sure?", "This will remove all changes made by Vortex",
        MessageBoxButtons.OKCancel, MessageBoxIcon.Stop);
                if (DialogResult != DialogResult.OK)
                    return 0;
            }

            string FolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"My Games\Starfield");
            if (File.Exists(Path.Combine(FolderPath, "StarfieldCustom.ini.base")))
            {
                File.Copy(Path.Combine(FolderPath, "StarfieldCustom.ini.base"), Path.Combine(FolderPath, "StarfieldCustom.ini"), true);
                File.Delete(Path.Combine(FolderPath, "StarfieldCustom.ini.base"));
                ChangeCount++;
            }
            if (File.Exists(Path.Combine(FolderPath, "StarfieldPrefs.ini.base")))
            {
                File.Copy(Path.Combine(FolderPath, "StarfieldPrefs.ini.base"), Path.Combine(FolderPath, "StarfieldPrefs.ini"), true);
                File.Delete(Path.Combine(FolderPath, "StarfieldPrefs.ini.base"));
                ChangeCount++;
            }
            ChangeCount += CheckAndDeleteINI("Starfield.ini");
            ChangeCount += CheckAndDeleteINI("Starfield.ini.baked");
            ChangeCount += CheckAndDeleteINI("StarfieldCustom.ini.baked");
            ChangeCount += CheckAndDeleteINI("StarfieldPrefs.ini.baked");
            ChangeCount += CheckAndDeleteINI("Starfield.ini.base");
            LooseFiles = false;
            LooseFilesOnOff(false);
            LooseFilesMenuUpdate();
            if (Delccc())
                ChangeCount++;
            if (ChangeCount > 0)
            {
                sbar3(ChangeCount + " Change(s) made to Vortex created files");
                if (log)
                    activityLog.WriteLog($"{ChangeCount} Vortex changes undone");
            }
            return ChangeCount;
        }

        private void undoVortexChangesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UndoVortexChanges(true);
        }

        private void UpdatePlugins()
        {
            int changes = SyncPlugins();
            if (AutoSort && changes > 0)
                RunLOOT(true);

            sbar3($"Changes made: {changes}");
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            UpdatePlugins();
        }

        private void LooseFilesOnOff(bool enable)
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Starfield", "StarfieldCustom.ini");

            if (Tools.FileCompare(filePath, Path.Combine(Tools.CommonFolder, "StarfieldCustom.ini")) && enable == false) // Return if loose files are already disabled
                return;

            if (enable)
            {
                var existingLines = File.Exists(filePath) ? File.ReadLines(filePath).Select(line => line.Trim()).ToHashSet() : new HashSet<string>();

                string[] linesToAppend = { "[Archive]", "bInvalidateOlderFiles=1" };

                if (linesToAppend.Any(line => !existingLines.Contains(line)))
                {
                    File.AppendAllLines(filePath, linesToAppend.Where(line => !existingLines.Contains(line)));
                    LooseFiles = true;
                    sbarCCC("Loose Files Enabled");
                    if (log)
                        activityLog.WriteLog("Loose files enabled in StarfieldCustom.ini");
                }
            }
            else if (File.Exists(filePath))
            {
                var updatedLines = File.ReadLines(filePath)
                    .Where(line => !new HashSet<string> { "[Archive]", "bInvalidateOlderFiles=1", "sResourceDataDirsFinal=" }.Contains(line.Trim()))
                    .ToList();

                File.WriteAllLines(filePath, updatedLines);
                LooseFiles = false;
                sbarCCC("Loose Files Disabled");
                if (log)
                    activityLog.WriteLog("Loose files disabled in StarfieldCustom.ini");
            }
        }

        private void looseFilesDisabledToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LooseFiles = looseFilesDisabledToolStripMenuItem.Checked = !LooseFiles;
            LooseFilesOnOff(LooseFiles);
            Properties.Settings.Default.LooseFiles = LooseFiles;
        }

        private void autoUpdateModsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AutoUpdate = autoUpdateModsToolStripMenuItem.Checked = !AutoUpdate;
            Properties.Settings.Default.AutoUpdate = AutoUpdate;
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            RefreshDataGrid();
        }

        private void btnLoot_Click(object sender, EventArgs e)
        {
            RunLOOT(true);
        }

        private void gameVersionSFSEToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (GameVersion == MS)
                if (GameSwitchWarning())
                    return;

            if (File.Exists(Path.Combine(StarfieldGamePath, "sfse_loader.exe")))
            {
                gameVersionSFSEToolStripMenuItem.Checked = true;
                toolStripMenuSteam.Checked = false;
                toolStripMenuMS.Checked = false;
                toolStripMenuCustom.Checked = false;
                GameVersion = SFSE;
                StarfieldGamePath = Properties.Settings.Default.StarfieldGamePath;
                UpdateGameVersion();
            }
            else
            {
                MessageBox.Show("SFSE doesn't seem to be installed or Starfield path not set", "Unable to switch to SFSE", MessageBoxButtons.OK, MessageBoxIcon.Error);
                GameVersion = Steam;
                toolStripMenuSteam.Checked = true;
                toolStripMenuMS.Checked = false;
                toolStripMenuCustom.Checked = false;
                gameVersionSFSEToolStripMenuItem.Checked = false;
            }
        }

        private int RemoveDuplicates()
        {
            string loText = Path.Combine(Tools.StarfieldAppData, "Plugins.txt");
            if (!File.Exists(loText)) return 0;

            var plugins = new HashSet<string>(
                File.ReadLines(loText)
                    .Where(line => !string.IsNullOrEmpty(line) && line[0] != '#') // Remove comments
            );

            int originalCount = dataGridView1.RowCount;
            if (originalCount != plugins.Count)
            {
                File.WriteAllLines(loText, plugins);
                InitDataGrid();
                isModified = true;
                SavePlugins();
            }

            int removedCount = originalCount - dataGridView1.RowCount;
            if (log)
                activityLog.WriteLog($"Duplicates Removed: {removedCount}");
            sbar4($"Duplicates removed: {removedCount}");
            return removedCount;
        }

        private void removeDuplicatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RemoveDuplicates();
        }

        private void ActiveOnlyToggle()
        {
            ActiveOnly = !activeOnlyToolStripMenuItem.Checked;
            activeOnlyToolStripMenuItem.Checked = ActiveOnly;
            Properties.Settings.Default.ActiveOnly = ActiveOnly;
            bool isEnabled;

            if (ActiveOnly && dataGridView1.Rows.Count > 1000)
            {
                sbar("Too many rows to filter");
                ActiveOnly = Properties.Settings.Default.ActiveOnly = activeOnlyToolStripMenuItem.Checked = false;
                return;
            }

            sbar4("Loading...");
            statusStrip1.Refresh();

            bool showAll = !ActiveOnly;

            dataGridView1.SuspendLayout();
            /*int counter = dataGridView1.Rows.Count;*/
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                isEnabled = row.Cells["ModEnabled"].Value as bool? ?? false;
                row.Visible = showAll || isEnabled;
                /*if (!isEnabled && !ActiveOnly)
                    row.DefaultCellStyle.BackColor = System.Drawing.Color.LightCyan;
                else
                    row.DefaultCellStyle.BackColor = System.Drawing.Color.White;
                counter--;
                sbar($"Filtering {counter}");
                statusStrip1.Refresh();*/
            }
            dataGridView1.ResumeLayout();
            sbar4(showAll ? "All mods shown" : "Active mods only");

            if (resizeToolStripMenuItem.Checked)
                ResizeFormToFitDataGridView(this);
            btnActiveOnly.Font = new System.Drawing.Font(btnActiveOnly.Font, ActiveOnly ? FontStyle.Bold : FontStyle.Regular);
        }

        private void activeOnlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ActiveOnlyToggle();
        }

        private void LooseFilesMenuUpdate()
        {
            LooseFiles = looseFilesDisabledToolStripMenuItem.Checked = LooseFiles;
            sbarCCC(LooseFiles ? "Loose files enabled" : "Loose files disabled");
            Properties.Settings.Default.LooseFiles = LooseFiles;
        }

        private void vortexPathToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!DetectVortex())
            {
                openFileDialog1.Filter = "Executable Files|*.exe";
                openFileDialog1.Title = "Set the path to the Vortex executable";
                openFileDialog1.FileName = "Vortex.exe";
                DialogResult VortexPath = openFileDialog1.ShowDialog();
                if (VortexPath == DialogResult.OK && openFileDialog1.FileName != "")
                {
                    Properties.Settings.Default.VortexPath = openFileDialog1.FileName;
                    vortexToolStripMenuItem.Visible = true;
                }
            }
        }

        private void uRLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            uRLToolStripMenuItem.Checked = !uRLToolStripMenuItem.Checked;
            if (uRLToolStripMenuItem.Checked)
                dataGridView1.Columns["URL"].Visible = true;
            else
                dataGridView1.Columns["URL"].Visible = false;
            Properties.Settings.Default.URL = uRLToolStripMenuItem.Checked;
        }

        private bool DetectVortex()
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
                        vortexToolStripMenuItem.Visible = true;
                        return true;
                    }
                }
            }
            return false;
        }

        private void vortexToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.VortexPath != "")
            {
                try
                {
                    var result = Process.Start(Properties.Settings.Default.VortexPath);
                    if (result != null)
                    {
                        SaveSettings();
                        if (log)
                            activityLog.WriteLog("Starting Vortex");
                        System.Windows.Forms.Application.Exit();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else
                MessageBox.Show("Vortex doesn't seem to be installed.");
        }

        private void autoSortToolStripMenuItem_Click(object sender, EventArgs e)
        {
            autoSortToolStripMenuItem.Checked = !autoSortToolStripMenuItem.Checked;
            Properties.Settings.Default.AutoSort = autoSortToolStripMenuItem.Checked;
            AutoSort = Properties.Settings.Default.AutoSort;
        }

        private void documentationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Tools.OpenUrl(@"Documentation\Index.htm");
        }

        private void CompareProfiles()
        {
            compareProfilesToolStripMenuItem.Checked = !compareProfilesToolStripMenuItem.Checked;
            Properties.Settings.Default.CompareProfiles = compareProfilesToolStripMenuItem.Checked;
            SaveSettings();
        }

        private void compareProfilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CompareProfiles();
        }

        private void lightToolStripMenuItem_Click(object sender, EventArgs e)
        {
            lightToolStripMenuItem.Checked = !lightToolStripMenuItem.Checked;
            System.Windows.Forms.Application.SetColorMode(SystemColorMode.Classic);
            Properties.Settings.Default.DarkMode = 0;
            darkToolStripMenuItem.Checked = false;
            systemToolStripMenuItem.Checked = false;
            MessageBox.Show("Restart app recommended to apply changes");
        }

        private void darkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            darkToolStripMenuItem.Checked = !darkToolStripMenuItem.Checked;
            dataGridView1.EnableHeadersVisualStyles = false;
            System.Windows.Forms.Application.SetColorMode(SystemColorMode.Dark);
            Properties.Settings.Default.DarkMode = 1;
            lightToolStripMenuItem.Checked = false;
            systemToolStripMenuItem.Checked = false;
            MessageBox.Show("Restart app recommended to apply changes");
        }

        private void systemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            systemToolStripMenuItem.Checked = !systemToolStripMenuItem.Checked;
            System.Windows.Forms.Application.SetColorMode(SystemColorMode.System);
            Properties.Settings.Default.DarkMode = 2;
            lightToolStripMenuItem.Checked = false;
            darkToolStripMenuItem.Checked = false;
            MessageBox.Show("Restart app recommended to apply changes");
        }

        private void btnActiveOnly_Click(object sender, EventArgs e)
        {
            ActiveOnlyToggle();
        }

        private void mO2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string MO2Path = Properties.Settings.Default.MO2Path;
            if (MO2Path != "")
            {
                try
                {
                    var result = Process.Start(MO2Path);
                    if (result != null)
                    {
                        SaveSettings();
                        if (log)
                            activityLog.WriteLog("Starting Mod Organizer 2");
                        System.Windows.Forms.Application.Exit();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else
                MessageBox.Show("MO2 doesn't seem to be installed or path not configured.");
        }

        private void modOrganizer2PathToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Executable Files|*.exe";
            openFileDialog1.Title = "Set the path to the MO2 executable";
            openFileDialog1.FileName = "ModOrganizer.exe";
            DialogResult MO2Path = openFileDialog1.ShowDialog();
            if (MO2Path == DialogResult.OK && openFileDialog1.FileName != "")
            {
                Properties.Settings.Default.MO2Path = openFileDialog1.FileName;
                mO2ToolStripMenuItem.Visible = true;
            }
        }

        private int ResetDefaults()
        {
            string LooseFilesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"My Games\Starfield"), // Check if loose files are enabled
        filePath = Path.Combine(LooseFilesDir, "StarfieldCustom.ini");
            int ChangeCount = 0;

            if (File.Exists(filePath)) // Disable loose files
            {
                ChangeCount += UndoVortexChanges(false);

                var lines = File.ReadAllLines(filePath).ToList();
                if (lines.Contains("bInvalidateOlderFiles=1"))
                {
                    LooseFilesOnOff(false);
                    ChangeCount++;
                }

                if (Delccc()) // Delete Starfield.ccc
                    ChangeCount++;

                if (ResetStarfieldCustomINI(false)) // Apply recommended settings
                    ChangeCount++;

                sbar3(ChangeCount.ToString() + " Change(s) made to ini files");
            }
            sbar5("Auto Reset");
            return ChangeCount;
        }

        private void autoResetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!autoResetToolStripMenuItem.Checked)
            {
                DialogResult DialogResult =
                    MessageBox.Show("This will run every time the app is started - Are you sure?", "This will reset settings made by other mod managers.",
                                MessageBoxButtons.OKCancel, MessageBoxIcon.Stop);
                if (DialogResult != DialogResult.OK)
                    return;
            }

            autoResetToolStripMenuItem.Checked = !autoResetToolStripMenuItem.Checked;
            if (autoResetToolStripMenuItem.Checked)
                ResetDefaults();
            else
                sbar5("");

            Properties.Settings.Default.AutoReset = autoResetToolStripMenuItem.Checked;
            SaveSettings();
        }

        private void creationKitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            const string userRoot = "HKEY_CURRENT_USER";
            const string subkey = @"Software\Valve\Steam";
            const string keyName = userRoot + "\\" + subkey;

            string executable = StarfieldGamePath;
            if (executable != null)
            {
                try
                {
                    SaveSettings();
                    string stringValue = (string)Registry.GetValue(keyName, "SteamExe", ""); // Get Steam path from Registry
                    var processInfo = new ProcessStartInfo(stringValue, "-applaunch 2722710");
                    var process = Process.Start(processInfo);
                    if (log)
                        activityLog.WriteLog("Starting Creation Kit");
                    System.Windows.Forms.Application.Exit();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else
            {
                MessageBox.Show("Starfield path not set");
            }
        }

        private void resetToVanillaStarfieldSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Tools.ConfirmAction("Reset ini settings?", "Reset to recommended settings", MessageBoxButtons.YesNo) == DialogResult.Yes)
                ResetDefaults();
        }

        private void ChangeSettings(bool NewSetting)
        {
            Properties.Settings.Default.ProfileOn = NewSetting;
            Profiles = NewSetting;
            chkProfile.Checked = NewSetting;
            Properties.Settings.Default.AutoSort = NewSetting;
            AutoSort = NewSetting;
            AutoUpdate = Properties.Settings.Default.AutoUpdate;
            Properties.Settings.Default.AutoUpdate = NewSetting;
            Properties.Settings.Default.AutoReset = NewSetting;
            Properties.Settings.Default.AutoDelccc = NewSetting;
            Properties.Settings.Default.CompareProfiles = NewSetting;
            Properties.Settings.Default.ActivateNew = NewSetting;
            Properties.Settings.Default.LOOTEnabled = NewSetting;
            Properties.Settings.Default.ModStats = NewSetting;

            SaveSettings();
            SetMenus();
        }

        private void enableAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Tools.ConfirmAction("Enable All settings?", "This will turn on a most of the Tools menu settings and reset ini settings", MessageBoxButtons.OKCancel,
                MessageBoxIcon.Exclamation) == DialogResult.OK)
            {
                ChangeSettings(true);
                if (log)
                    activityLog.WriteLog("Enabling all settings");
                ResetDefaults();
                ShowRecommendedColumns();
                modStatsToolStripMenuItem.Checked = true;
                Properties.Settings.Default.ModStats = true;
            }
            if (Properties.Settings.Default.LOOTEnabled)
                ReadLOOTGroups();

            if (!ActiveOnly)
                ActiveOnlyToggle();
        }

        private void disableAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ActiveOnly)
                ActiveOnlyToggle();
            ChangeSettings(false);
            disableAllWarnings();

            if (log)
                activityLog.WriteLog("Disabling all settings");
            DisableLog();
            sbar5("");
        }

        private void uIToEditStarfieldCustominiToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmStarfieldCustomINI fci = new();
            fci.ShowDialog();
            string PluginsPath = Path.Combine(Tools.StarfieldAppData, "Plugins.txt"),
        LooseFilesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"My Games\Starfield"), // Check if loose files are enabled
        filePath = Path.Combine(LooseFilesDir, "StarfieldCustom.ini");
            LooseFiles = false;
            try
            {
                var StarfieldCustomINI = File.ReadAllLines(filePath);
                foreach (var lines in StarfieldCustomINI)
                {
                    if (lines.Contains("bInvalidateOlderFiles"))
                    {
                        Properties.Settings.Default.LooseFiles = true;
                        SaveSettings();
                        LooseFiles = true;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                if (log)
                    activityLog.WriteLog("Error reading StarfieldCustom.ini: " + ex.Message);
#if DEBUG
                MessageBox.Show(ex.Message);
#endif
            }
            if (LooseFiles)
                sbarCCC("Loose Files Enabled");
            else
                sbarCCC("Loose Files Disabled");
            looseFilesDisabledToolStripMenuItem.Checked = LooseFiles;
        }

        private void UpdateGameVersion() // Display game version
        {
            Properties.Settings.Default.GameVersion = GameVersion;
            if (Properties.Settings.Default.StarfieldGamePath == "")
            {
                StarfieldGamePath = tools.SetStarfieldGamePath();
                if (GameVersion != MS)
                {
                    Properties.Settings.Default.StarfieldGamePath = StarfieldGamePath;
                }
                else
                {
                    Properties.Settings.Default.GamePathMS = StarfieldGamePath;
                }
            }

            if (Properties.Settings.Default.GamePathMS == "" || GameVersion == MS)
            {
                StarfieldGamePath = tools.SetStarfieldGamePathMS();
                Properties.Settings.Default.GamePathMS = StarfieldGamePath;
                SaveSettings();
            }

            SaveSettings();
            if (GameVersion != MS)
                StarfieldGamePath = Properties.Settings.Default.StarfieldGamePath;
            else
            {
                StarfieldGamePath = Properties.Settings.Default.GamePathMS;
                RefreshDataGrid();
            }
            if (log)
                activityLog.WriteLog($"Game version set to {GameVersion}");
            GameVersionDisplay();
        }

        private void toolStripMenuVersion_Click(object sender, EventArgs e) // View version column
        {
            toolStripMenuVersion.Checked = !toolStripMenuVersion.Checked;
            if (toolStripMenuVersion.Checked)
                dataGridView1.Columns["Version"].Visible = true;
            else
                dataGridView1.Columns["Version"].Visible = false;
            Properties.Settings.Default.Version = toolStripMenuVersion.Checked;
        }

        private void toolStripMenuAuthorVersion_Click(object sender, EventArgs e) // View author column
        {
            toolStripMenuAuthorVersion.Checked = !toolStripMenuAuthorVersion.Checked;
            dataGridView1.Columns["AuthorVersion"].Visible = toolStripMenuAuthorVersion.Checked;
            Properties.Settings.Default.AuthorVersion = toolStripMenuAuthorVersion.Checked;
        }

        private void enableAchievementSafeOnlyToolStripMenuItem_Click(object sender, EventArgs e) // Experimental. Should probably remove
        {
            if (Tools.ConfirmAction("Do you want to continue", "Warning - this will alter your current load order to achievement friendly mods only",
                MessageBoxButtons.YesNo) == DialogResult.No)
                return;
            if (dataGridView1.Columns["Achievements"].Visible == false)
            {
                Properties.Settings.Default.Achievements = true;
                SaveSettings();
                SetupColumns();
            }
            RefreshDataGrid();
            bool ActiveOnlyStatus = ActiveOnly;
            DisableAll();
            if (ActiveOnly)
                ActiveOnlyToggle();

            for (int i = 0; i < dataGridView1.Rows.Count; i++)
            {
                if ((string)dataGridView1.Rows[i].Cells["Achievements"].Value == "Yes")
                    dataGridView1.Rows[i].Cells["ModEnabled"].Value = true;
            }

            if (ActiveOnlyStatus)
                ActiveOnlyToggle();
            sbar2("All achievement friendly mods enabled");
            isModified = true;
            SavePlugins();
            if (log)
                activityLog.WriteLog("Achievement friendly mods enabled");
        }

        private void openAllActiveModWebPagesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int i;
            string url;

            if (Tools.ConfirmAction("Are you sure you want to open all mod web pages?", "This might take a while and a lot of memory",
                MessageBoxButtons.YesNo) == DialogResult.Yes || NoWarn)
            {
                for (i = 0; i < dataGridView1.RowCount; i++)
                {
                    url = (string)dataGridView1.Rows[i].Cells["URL"].Value;
                    if ((bool)dataGridView1.Rows[i].Cells["ModEnabled"].Value == true && url != "")
                        Tools.OpenUrl(url);
                }
            }
        }

        private void timer2_Tick(object sender, EventArgs e) // Used for date / time display, ticks once per second.
        {
            DateTime now = DateTime.Now;
            sbar5(now.ToString("ddd, d MMM yyyy - hh:mm tt", CultureInfo.CurrentCulture.DateTimeFormat));
        }

        private void showTimeToolStripMenuItem_Click_1(object sender, EventArgs e) // Display date and time
        {
            timer2.Enabled = !timer2.Enabled;
            showTimeToolStripMenuItem.Checked = timer2.Enabled;
            Properties.Settings.Default.Showtime = showTimeToolStripMenuItem.Checked;
            if (!timer2.Enabled)
                sbar5("");
        }

        private static void CreateZipFromFiles(List<string> files, string zipPath)
        {
            using (FileStream zipToOpen = new FileStream(zipPath, FileMode.Create))
            {
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
                {
                    foreach (string file in files)
                    {
                        ZipArchiveEntry entry = archive.CreateEntryFromFile(file, Path.GetFileName(file));
                    }
                }
            }
        }

        private bool CheckGamePath() // Check if game path is set
        {
            if (String.IsNullOrEmpty(StarfieldGamePath))
                StarfieldGamePath = tools.SetStarfieldGamePath(); // Prompt user to set game path if not set
            if (StarfieldGamePath == "")
            {
                MessageBox.Show("Unable to continue without Starfield game path");
                return false;
            }
            else
                return true;
        }

        private void archiveModToolStripMenuItem_Click_1(object sender, EventArgs e) // Make a zip of a mod and copy it to specified folder
        {
            List<string> files = new();
            if (!CheckGamePath()) // Abort if game path not set
                return;

            string directoryPath = Path.Combine(StarfieldGamePath, "Data");

            using FolderBrowserDialog folderBrowserDialog = new();
            folderBrowserDialog.Description = "Choose folder to archive the mods to";
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                string selectedFolderPath = folderBrowserDialog.SelectedPath;

                Form LoadScreen = new frmLoading("Archiving mods..."); // Show popup while archive process runs
                LoadScreen.Show();

                foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                {
                    if (row.Cells["PluginName"].Value is not string ModNameRaw) continue;
                    string ModName = ModNameRaw[..ModNameRaw.IndexOf('.')]; // Get current mod name
                    string ModFile = Path.Combine(directoryPath, ModName); // Add esp, esm, and archives to files list

                    if (File.Exists(ModFile + ".esp"))
                        files.Add(ModFile + ".esp");

                    if (File.Exists(ModFile + ".esm"))
                        files.Add(ModFile + ".esm");

                    if (File.Exists(ModFile + " - textures.ba2"))
                        files.Add(ModFile + " - textures.ba2");

                    if (File.Exists(ModFile + " - main.ba2"))
                        files.Add(ModFile + " - main.ba2");

                    if (File.Exists(ModFile + " - voices_en.ba2"))
                        files.Add(ModFile + " - voices_en.ba2");

                    string zipPath = Path.Combine(selectedFolderPath, ModName) + ".zip"; // Choose path to Zip it

                    void makeArchive()
                    {
                        sbar3($"Creating archive for {ModName}...");
                        statusStrip1.Refresh();
                        CreateZipFromFiles(files, zipPath); // Make zip
                        sbar3($"{ModName} archived");
                        if (log)
                            activityLog.WriteLog($"Created archive for {ModName} at {zipPath}");
                        statusStrip1.Refresh();
                        files.Clear();
                    }

                    // Check if archive already exists, bail out on user cancel
                    if (File.Exists(zipPath))
                    {
                        DialogResult dlgResult = Tools.ConfirmAction("Overwrite archive?", $"Archive exists - {ModName}", MessageBoxButtons.YesNoCancel);

                        if (dlgResult == DialogResult.Cancel)
                        {
                            LoadScreen.Close();
                            sbar3("Archive creation cancelled");
                            return;
                        }

                        if (dlgResult == DialogResult.No)
                            sbar3($"Archive for {ModName} not created");
                        if (dlgResult == DialogResult.Yes)
                            makeArchive();
                    }
                    else
                        makeArchive();
                }

                LoadScreen.Close();
                sbar3("Mods archived");
            }
        }

        private async Task UpdateBackupAsync(CancellationToken token)
        {
            List<string> files = new();
            int modsArchived = 0;

            if (!CheckGamePath()) // Abort if game path not set
                return;

            string directoryPath = Path.Combine(StarfieldGamePath, "Data");

            using FolderBrowserDialog folderBrowserDialog = new();
            folderBrowserDialog.Description = "Choose folder to archive the mods to";
            folderBrowserDialog.InitialDirectory = Properties.Settings.Default.BackupDirectory;
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                sbar("Press F12 to stop operation");
                string selectedFolderPath = folderBrowserDialog.SelectedPath;
                Properties.Settings.Default.BackupDirectory = selectedFolderPath;
                SaveSettings();

                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (token.IsCancellationRequested)
                    {
                        sbar3("Archive creation cancelled");
                        return;
                    }

                    Application.DoEvents();

                    if (row.Cells["PluginName"].Value is not string ModNameRaw) continue;
                    string ModName = ModNameRaw[..ModNameRaw.LastIndexOf('.')]; // Get current mod name
                    string ModFile = Path.Combine(directoryPath, ModName); // Add esp, esm, and archives to files list

                    if (File.Exists(ModFile + ".esp"))
                        files.Add(ModFile + ".esp");

                    if (File.Exists(ModFile + ".esm"))
                        files.Add(ModFile + ".esm");

                    if (File.Exists(ModFile + " - textures.ba2"))
                        files.Add(ModFile + " - textures.ba2");

                    if (File.Exists(ModFile + " - main.ba2"))
                        files.Add(ModFile + " - main.ba2");

                    if (File.Exists(ModFile + " - voices_en.ba2"))
                        files.Add(ModFile + " - voices_en.ba2");

                    string zipPath = Path.Combine(selectedFolderPath, ModName) + ".zip"; // Choose path to Zip it

                    // Check if archive already exists, bail out on user cancel
                    if (!File.Exists(zipPath))
                    {
                        sbar($"Creating archive for {ModName}...");
                        statusStrip1.Refresh();
                        if (log)
                            activityLog.WriteLog($"Creating archive for {ModName} at {zipPath}");
                        CreateZipFromFiles(files, zipPath); // Make zip
                        sbar($"{ModName} archived");
                        statusStrip1.Refresh();
                        modsArchived++;
                    }
                    files.Clear();
                }
                sbar(modsArchived + " Mod(s) archived");
                if (log)
                    activityLog.WriteLog($"{modsArchived} mods archived to {selectedFolderPath}");
            }
        }

        private int CheckArchives()
        {
            List<string> BGSArchives = Tools.BGSArchives();
            List<string> archives = [];
            List<string> plugins = [];
            List<string> toDelete = [];

            if (StarfieldGamePath == "")
                return 0;

            // Build a list of all plugins excluding base game files
            plugins = tools.GetPluginList().Select(s => s[..^4].ToLower()).ToList();

            foreach (string file in Directory.EnumerateFiles(Path.Combine(StarfieldGamePath, "Data"), "*.ba2", SearchOption.TopDirectoryOnly)) // Build a list of all archives
            {
                archives.Add(Path.GetFileName(file).ToLower());
            }

            try
            {
                List<string> modArchives = archives.Except(BGSArchives) // Exclude BGS Archives
                    .Select(s => s.ToLower().Replace(".ba2", string.Empty)) // Remove ".ba2" from archive names
                    .ToList();

                foreach (var archive in modArchives) // Check if archive is orphaned
                {
                    if (!plugins.Any(plugin => archive.StartsWith(plugin))) // If no plugin starts with the archive name
                        toDelete.Add(Path.Combine(StarfieldGamePath, "Data", archive) + ".ba2");
                }

                if (toDelete.Count > 0)
                {
                    Form Orphaned = new frmOrphaned(toDelete);
                    Orphaned.Show();
                    return toDelete.Count;
                }
                else
                {
                    sbar3("No orphaned archives found");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                if (log)
                    activityLog.WriteLog("Error checking archives: " + ex.Message);
                MessageBox.Show("Error: " + ex.Message);
                return 0;
            }
        }

        private void checkArchivesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CheckArchives();
        }

        private void activateNewModsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            activateNewModsToolStripMenuItem.Checked = !activateNewModsToolStripMenuItem.Checked;
            Properties.Settings.Default.ActivateNew = activateNewModsToolStripMenuItem.Checked;
            sbar3("test");
        }

        private void toolStripMenuItemDeletePlugins_Click(object sender, EventArgs e)
        {
            if (Tools.ConfirmAction("Are you sure you want to delete Plugins.txt?", "This will delete Plugins.txt", MessageBoxButtons.YesNo) == DialogResult.No)
                return;
            File.Delete(Path.Combine(Tools.StarfieldAppData, "Plugins.txt"));
        }

        private void toolStripMenuAddToProfile_Click(object sender, EventArgs e) // Add selected mods to a different profile
        {
            List<string> profiles = new();

            if (cmbProfile.Items.Count == 0 || cmbProfile.SelectedItem == null)
            {
                MessageBox.Show("No valid profiles found");
                return;
            }

            foreach (var item in cmbProfile.Items)
            {
                profiles.Add(item.ToString());
            }
            profiles.Remove(cmbProfile.SelectedItem.ToString()); // Remove current profile from list

            foreach (DataGridViewRow selectedRow in dataGridView1.SelectedRows)
            {
                try
                {
                    if (selectedRow.Cells["PluginName"].Value != null) // Ensure the cell value is not null
                    {
                        frmAddModToProfile addMod = new(profiles, selectedRow.Cells["PluginName"].Value.ToString());
                        addMod.ShowDialog(cmbProfile);
                    }
                }
                catch (Exception ex)
                {
                    if (log)
                        activityLog.WriteLog("Error adding mod to profile: " + ex.Message);
#if DEBUG
                    MessageBox.Show("Error: " + ex.Message);
#endif
                }

                if (Tools.ConfirmAction("Run update/sort on all profiles", "Update All Profiles?",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    UpdateAllProfiles();
            }
        }

        private void ResetWindowSize()
        {
            Rectangle resolution = Screen.PrimaryScreen.Bounds; // Resize window to 75% of screen width
            double screenWidth = resolution.Width;
            double screenHeight = resolution.Height;
            this.Width = (int)(screenWidth * 0.85);
            this.Height = (int)(screenHeight * 0.85);
            this.StartPosition = FormStartPosition.CenterScreen;
            if (log)
                activityLog.WriteLog("Window size reset to default");
        }

        private void frmLoadOrder_Load(object sender, EventArgs e) // Do some initialisation when the form loads
        {
            this.Location = Properties.Settings.Default.WindowLocation;
            this.Size = Properties.Settings.Default.WindowSize;

            // Check if the form's bounds are within any screen's working area
            foreach (Screen screen in Screen.AllScreens)
            {
                if (!screen.WorkingArea.Contains(this.Bounds))
                {
                    Rectangle screenBounds = Screen.FromControl(this).WorkingArea;

                    // Adjust the form's position to ensure it stays within the screen bounds
                    int newX = Math.Max(screenBounds.Left, Math.Min(this.Left, screenBounds.Right - this.Width));
                    int newY = Math.Max(screenBounds.Top, Math.Min(this.Top, screenBounds.Bottom - this.Height));
                    this.Location = new Point(newX, newY);
                    Properties.Settings.Default.WindowLocation = this.Location;
                    Properties.Settings.Default.WindowSize = this.Size;
                    SaveSettings();
                }
            }

            int minWidth = 1500; // Set your minimum width
            int minHeight = 800; // Set your minimum height

            if (this.Width < minWidth || this.Height < minHeight)
            {
                this.Size = new System.Drawing.Size(minWidth, minHeight);
                this.Location = new Point(
        (Screen.PrimaryScreen.WorkingArea.Width - this.Width) / 2,
        (Screen.PrimaryScreen.WorkingArea.Height - this.Height) / 2
    );
            }

            progressBar1.Width = 400; // Set the width of the progress bar
            progressBar1.Height = 50; // Set the height of the progress bar
            progressBar1.Location = new Point((this.ClientSize.Width - progressBar1.Width) / 2, (this.ClientSize.Height - progressBar1.Height) / 2);
        }

        private void toolStripMenuResetWindow_Click(object sender, EventArgs e)
        {
            ResetWindowSize();
        }

        private void disableAllWarnings()
        {
            disableAllWarningToolStripMenuItem.Checked = Properties.Settings.Default.NoWarn = NoWarn = false;
            if (log)
                activityLog.WriteLog("Disable all warnings set to " + NoWarn.ToString());
        }

        private void disableAllWarningToolStripMenuItem_Click(object sender, EventArgs e)
        {
            disableAllWarningToolStripMenuItem.Checked = !disableAllWarningToolStripMenuItem.Checked;
            Properties.Settings.Default.NoWarn = disableAllWarningToolStripMenuItem.Checked;
            NoWarn = disableAllWarningToolStripMenuItem.Checked;
            if (log)
                activityLog.WriteLog("Disable all warnings set to " + NoWarn.ToString());
        }

        private void toolStripMenuExportCSV_Click(object sender, EventArgs e) // Export DataGridView to CSV file
        {
            int i, j, ExportedLines = 0;

            System.Windows.Forms.SaveFileDialog ExportActive = new()
            {
                Filter = "CSV File|*.csv",
                Title = "Export to CSV",
                FileName = "Plugins.csv",
            };

            DialogResult dlgResult = ExportActive.ShowDialog();
            if (dlgResult == DialogResult.OK)
            {
                try
                {
                    using (StreamWriter sw = new StreamWriter(ExportActive.FileName))
                    {
                        // Write the headers
                        for (i = 0; i < dataGridView1.Columns.Count; i++)
                        {
                            sw.Write(dataGridView1.Columns[i].HeaderText);
                            if (i < dataGridView1.Columns.Count - 1) sw.Write(",");
                        }
                        sw.WriteLine();

                        // Write the data
                        for (i = 0; i < dataGridView1.Rows.Count; i++) // Rows
                        {
                            if (ActiveOnly && !(bool)dataGridView1.Rows[i].Cells["ModEnabled"].Value)
                                continue;

                            ExportedLines++;
                            for (j = 0; j < dataGridView1.Columns.Count; j++) // Columns
                            {
                                sw.Write(dataGridView1.Rows[i].Cells[j].Value);
                                if (j < dataGridView1.Columns.Count - 1) sw.Write(",");
                            }
                            sw.WriteLine();
                        }
                    }

                    if (Tools.ConfirmAction("Open exported file?", "Export Complete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        Process.Start("explorer.exe", ExportActive.FileName);
                }
                catch (Exception ex)
                {
                    if (log)
                        activityLog.WriteLog("Error while exporting data: " + ex.Message);
                    MessageBox.Show("Error while exporting data: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                if (ExportedLines == 0)
                {
                    sbar3("Nothing to export");
                    return;
                }
            }
        }

        private void toolStripMenuLOOTToggle_Click(object sender, EventArgs e)
        {
            toolStripMenuLOOTToggle.Checked = !toolStripMenuLOOTToggle.Checked;
            Properties.Settings.Default.LOOTEnabled = toolStripMenuLOOTToggle.Checked;
            if (toolStripMenuLOOTToggle.Checked)
            {
                ReadLOOTGroups();
                InitDataGrid();
            }
        }

        private void toolStripMenuDescription_Click(object sender, EventArgs e)
        {
            toolStripMenuDescription.Checked = !toolStripMenuDescription.Checked;
            dataGridView1.Columns["Description"].Visible = toolStripMenuDescription.Checked;
            Properties.Settings.Default.Description = toolStripMenuDescription.Checked;
        }

        private void toolStripMenuItemHideAll_Click(object sender, EventArgs e) // Hide all columns in the DataGridView except active status and plugin name
        {
            var items = new[]
        {
    ("TimeStamp", timeStampToolStripMenuItem),
    ("Achievements", toolStripMenuAchievements),
    ("CreationsID", toolStripMenuCreationsID),
    ("Files", toolStripMenuFiles),
    ("Group", toolStripMenuGroup),
    ("Index", toolStripMenuIndex),
    ("FileSize", toolStripMenuFileSize),
    ("URL", uRLToolStripMenuItem),
    ("Version", toolStripMenuVersion),
    ("AuthorVersion", toolStripMenuAuthorVersion),
    ("Description", toolStripMenuDescription),
    ("Blocked", blockedToolStripMenuItem)
};

            foreach (var (columnName, menuItem) in items)
            {
                SetColumnVisibility(false, menuItem, dataGridView1.Columns[columnName]);
                Properties.Settings.Default[columnName] = false;
            }
        }

        private void ShowRecommendedColumns()
        {
            var enableItems = new[]
       {
    ("Group", toolStripMenuGroup),
    ("Version", toolStripMenuVersion),
    ("AuthorVersion", toolStripMenuAuthorVersion),
    ("Description", toolStripMenuDescription),
    ("Blocked", blockedToolStripMenuItem)
};
            var disableItems = new[]
            {
                ("TimeStamp", timeStampToolStripMenuItem),
                ("Achievements", toolStripMenuAchievements),
                ("CreationsID", toolStripMenuCreationsID),
                ("Files", toolStripMenuFiles),
                ("Index", toolStripMenuIndex),
                ("FileSize", toolStripMenuFileSize),
                ("URL", uRLToolStripMenuItem)
            };

            foreach (var (columnName, menuItem) in enableItems)
            {
                SetColumnVisibility(true, menuItem, dataGridView1.Columns[columnName]);
                Properties.Settings.Default[columnName] = true;
            }

            foreach (var (columnName, menuItem) in disableItems)
            {
                SetColumnVisibility(false, menuItem, dataGridView1.Columns[columnName]);
                Properties.Settings.Default[columnName] = false;
            }

            RefreshDataGrid();
        }

        private void toolStripMenuShowRecommended_Click(object sender, EventArgs e)
        {
            ShowRecommendedColumns();
        }

        private void btnCatalog_Click(object sender, EventArgs e)
        {
            CheckCatalog();
        }

        private void editBlockedModstxtToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Tools.OpenFile(Path.Combine(Tools.LocalAppDataPath, "BlockedMods.txt"));
            MessageBox.Show("Click OK to refresh");
            isModified = true;
            RefreshDataGrid();
        }

        private void blockedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            blockedToolStripMenuItem.Checked = !blockedToolStripMenuItem.Checked;
            dataGridView1.Columns["Blocked"].Visible = blockedToolStripMenuItem.Checked;
            Properties.Settings.Default.Blocked = blockedToolStripMenuItem.Checked;
        }

        private void blockUnblockToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<string> blockedMods = new(Tools.BlockedMods());

            // Loop through each selected row
            foreach (DataGridViewRow currentRow in dataGridView1.SelectedRows)
            {
                // Skip new rows
                if (currentRow.IsNewRow)
                    continue;

                // Initialize 'Blocked' cell if it's null
                if (currentRow.Cells["Blocked"].Value == null)
                {
                    currentRow.Cells["Blocked"].Value = false;
                }

                // Toggle blocked status
                currentRow.Cells["Blocked"].Value = !(bool)(currentRow.Cells["Blocked"].Value);

                if ((bool)currentRow.Cells["Blocked"].Value) // Add mod to blocked list
                {
                    blockedMods.Add(currentRow.Cells["PluginName"].Value.ToString());
                    currentRow.Cells["ModEnabled"].Value = false;
                    sbar2(currentRow.Cells["PluginName"].Value.ToString() + " blocked");
                    if (log)
                        activityLog.WriteLog($"Blocked {currentRow.Cells["PluginName"].Value.ToString()}");
                }
                else // Remove mod from blocked list
                {
                    blockedMods.Remove(currentRow.Cells["PluginName"].Value.ToString());
                    sbar2(currentRow.Cells["PluginName"].Value.ToString() + " unblocked");
                    if (log)
                        activityLog.WriteLog($"Unblocked {currentRow.Cells["PluginName"].Value.ToString()}");
                }
            }

            // Write to BlockedMods.txt and update isModified flag. No blank lines.
            File.WriteAllLines(Path.Combine(Tools.LocalAppDataPath, "BlockedMods.txt"), blockedMods.Distinct().Where(s => !string.IsNullOrEmpty(s)));
            isModified = true;
            SavePlugins();
        }

        private void prepareForCreationsUpdateToolStripMenuItem_Click(object sender, EventArgs e) // Workaround for Creations update re-downloading mods
        {
            if (!Properties.Settings.Default.CreationsUpdate) // Catalog Auto Restore off etc.
            {
                prepareForCreationsUpdateToolStripMenuItem.Checked = true;
                Properties.Settings.Default.CreationsUpdate = true;
                Properties.Settings.Default.AutoRestore = false;
                Properties.Settings.Default.AutoCheck = true;

                if (Tools.ConfirmAction("1. Start the game and update Creations mods.\n2. Don't Load a Save Game\n3. Quit the game and run this app again\n\n" +
                    "To Cancel this option," +
                    " click this menu option again\n\nRun the game now?", "Steps to Update Creations Mods", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                    true) == DialogResult.Yes)
                {
                    if (log)
                        activityLog.WriteLog("Creations Update started, running game now");
                    RunGame(); ;
                }
            }
            else
            {
                prepareForCreationsUpdateToolStripMenuItem.Checked = false; // Cancel Creations update
                Properties.Settings.Default.CreationsUpdate = false;
                Properties.Settings.Default.AutoRestore = true;
                MessageBox.Show("Catalog Auto Restore set to on", "Creations Update Cancelled");
                if (log)
                    activityLog.WriteLog("Creations Update Cancelled");
            }
        }

        private void dataGridView1_DragEnter(object sender, DragEventArgs e) // Handle drag and drop of files into the DataGridView
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void profileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Tools.OpenFolder(Properties.Settings.Default.ProfileFolder);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error opening profile folder: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void frmLoadOrder_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F12)
            {
                sbar3("Operation Stopped.");
                try
                {
                    cancellationTokenSource?.Cancel(); // Signal cancellation
                }
                catch (Exception ex)
                {
#if DEBUG
                    MessageBox.Show(ex.Message);
#endif
                }
            }
        }

        private async Task UpdateArchiveModsAsync()
        {
            cancellationTokenSource = new CancellationTokenSource();
            try
            {
                await UpdateBackupAsync(cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Task was aborted!");
            }
            finally
            {
                cancellationTokenSource.Dispose();
            }
        }

        private void updateArchivedModsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateArchiveModsAsync();
        }

        private static int DeleteLooseFileFolders()
        {
            frmDeleteLooseFiles fdl = new frmDeleteLooseFiles();

            try
            {
                fdl.ShowDialog();
            }
            catch { }

            return returnStatus;
        }

        private void deleteLooseFileFoldersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            sbar3($"Folders Deleted: {DeleteLooseFileFolders().ToString()}");
        }

        private void starUIConfiguratorToolStripMenuItem_Click(object sender, EventArgs e) // Launch StarUI Configurator if installed
        {
            string workingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\My Games\Starfield\Data\";
            string StarUI = workingDirectory + @"StarUI Configurator.bat";
            if (!String.IsNullOrEmpty(StarUI))
            {
                try
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = StarUI,
                        WorkingDirectory = workingDirectory,
                        UseShellExecute = true // Ensure this is true for WorkingDirectory to work
                    };

                    Process process = Process.Start(startInfo);
                    if (log)
                        activityLog.WriteLog("Starting StarUI Configurator");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else
                MessageBox.Show("StarUI Configurator doesn't seem to be installed correctly.");
        }

        private int RestoreStarfieldINI()
        {
            string StarfieldiniPath = Path.Combine(StarfieldGamePath, "Starfield.ini");

            if (!Tools.FileCompare(StarfieldiniPath, Path.Combine(Tools.CommonFolder, "Starfield.ini")))
            {
                try
                {
                    File.Copy(Path.Combine(Tools.CommonFolder, "Starfield.ini"), Path.Combine(StarfieldGamePath, "Starfield.ini"), true); // Restore Starfield.ini
                    sbar3("Starfield.ini restored");
                    if (log)
                        activityLog.WriteLog("Starfield.ini restored to default settings");
                    return 1;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return 0;
                }
            }
            else
            {
                sbar3("Starfield.ini Matches Default");
                if (log)
                    activityLog.WriteLog("Starfield.ini Matches Default");
                return 0;
            }
        }

        private void restoreStarfieldiniToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RestoreStarfieldINI();
        }

        private void resetEverythingToolStripMenuItem_Click(object sender, EventArgs e) // Reset all game settings and delete loose file folders, preserves app settings.
        {
            int actionCount;

            if (Tools.ConfirmAction("This will reset all game settings and delete all loose files folders", "Are you sure?", MessageBoxButtons.YesNo,
                MessageBoxIcon.Exclamation, true) == DialogResult.No)
                return;
            if (log)
                activityLog.WriteLog("Starting reset everything.");
            actionCount = RestoreStarfieldINI();
            actionCount += DeleteLooseFileFolders();
            actionCount += ResetDefaults();
            actionCount += CheckArchives();
            sbar3(actionCount.ToString() + " Change(s) made");
            if (log)
                activityLog.WriteLog("Reset everything: " + actionCount.ToString() + " Change(s) made");
        }

        public void ResetPreferences() // Reset user preferences
        {
            string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appPreferencesPath = Path.Combine(localAppDataPath, "Starfield_Tools");

            if (Tools.ConfirmAction("Are you sure you want to reset user preferences?", "This will delete all user settings and preferences", MessageBoxButtons.YesNo,
                MessageBoxIcon.Exclamation, true) == DialogResult.No) // Override Nowarn
                return;

            if (Directory.Exists(appPreferencesPath))
            {
                Directory.Delete(appPreferencesPath, true); // true to delete subdirectories and files
                if (log)
                    activityLog.WriteLog("User preferences reset successfully.");
                MessageBox.Show("Please Restart the app", "User preferences reset successfully.");

                Environment.Exit(0); // Close the application
            }
            else
            {
                MessageBox.Show("No preferences found to reset.");
            }
        }

        private void resetAppPreferencesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ResetPreferences();
        }

        private void modStatsToolStripMenuItem_Click(object sender, EventArgs e) // Toggle Mod Stats visibility
        {
            modStatsToolStripMenuItem.Checked = !modStatsToolStripMenuItem.Checked;
            Properties.Settings.Default.ModStats = modStatsToolStripMenuItem.Checked;
            if (modStatsToolStripMenuItem.Checked)
                RefreshDataGrid();
        }

        private void BackupProfiles()
        {
            if (Properties.Settings.Default.ProfileFolder == "")
            {
                MessageBox.Show("No profile folder set");
                return;
            }

            if (!Directory.Exists(Path.Combine(Properties.Settings.Default.ProfileFolder, "Backup")))
            {
                Directory.CreateDirectory(Path.Combine(Properties.Settings.Default.ProfileFolder, "Backup"));
            }

            foreach (var item in Directory.EnumerateFiles(Properties.Settings.Default.ProfileFolder, "*.txt", SearchOption.TopDirectoryOnly))
            {
                string fileName = Path.GetFileName(item);
                string destinationPath = Path.Combine(Properties.Settings.Default.ProfileFolder, "Backup", fileName);
                File.Copy(item, destinationPath, true);
                if (log)
                    activityLog.WriteLog($"Backed up {item} to backup folder {destinationPath}.");
            }
            sbar("Profiles backed up to Backup folder");
        }

        private void backupProfilesToolStripMenuItem_Click(object sender, EventArgs e) // Backup profiles to Backup folder in Profile folder
        {
            BackupProfiles();
        }

        private void restoreProfilesToolStripMenuItem_Click(object sender, EventArgs e) // Restore profiles from Backup folder
        {
            if (Properties.Settings.Default.ProfileFolder == "" || !Directory.Exists(Path.Combine(Properties.Settings.Default.ProfileFolder, "Backup")))
            {
                MessageBox.Show("No profile or backup folder set");
                return;
            }

            if (Tools.ConfirmAction("Restore Backup", "Restore Backup", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.Cancel)
            {
                sbar("Restore cancelled");
                return;
            }

            if (Directory.Exists(Path.Combine(Properties.Settings.Default.ProfileFolder, "Backup")))
            {
                foreach (var item in Directory.EnumerateFiles(Path.Combine(Properties.Settings.Default.ProfileFolder, "Backup"), "*.txt", SearchOption.TopDirectoryOnly))
                {
                    string fileName = Path.GetFileName(item);
                    string destinationPath = Path.Combine(Properties.Settings.Default.ProfileFolder, fileName);
                    File.Copy(item, destinationPath, true);
                    if (log)
                        activityLog.WriteLog($"Restored {item} from backup folder {destinationPath}.");
                }
                sbar("Profiles restored from Backup folder");
                RefreshDataGrid();
            }
            else
            {
                sbar("No backup folder found");
            }
        }

        private void BackupBlockedMods()
        {
            using FolderBrowserDialog folderBrowserDialog = new();
            folderBrowserDialog.Description = "Choose folder to use to backup BlockedMods.txt";
            folderBrowserDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); // Set initial directory to Documents Directory
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                string selectedFolderPath = folderBrowserDialog.SelectedPath;
                string blockedModsFilePath = Path.Combine(Tools.LocalAppDataPath, "BlockedMods.txt");
                string destinationPath = Path.Combine(selectedFolderPath, "BlockedMods.txt");
                if (!File.Exists(blockedModsFilePath))
                {
                    MessageBox.Show("BlockedMods.txt not found");
                    return;
                }
                File.Copy(blockedModsFilePath, destinationPath, true);
                sbar("BlockedMods.txt backed up successfully.");
                if (log)
                    activityLog.WriteLog($"BlockedMods.txt backed up to {selectedFolderPath}");
            }
        }

        private void mnuBackupBlockedMods_Click(object sender, EventArgs e) // Backup BlockedMods.txt to a user selected folder
        {
            BackupBlockedMods();
        }

        private void mnuRestoreBlockedMods_Click(object sender, EventArgs e) // Restore BlockedMods.txt from backup folder
        {
            using FolderBrowserDialog folderBrowserDialog = new();
            folderBrowserDialog.Description = "Choose folder to restore BlockedMods.txt from";
            folderBrowserDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); // Set initial directory to Documents Directory
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                string selectedFolderPath = folderBrowserDialog.SelectedPath;

                string backupFilePath = Path.Combine(selectedFolderPath, "BlockedMods.txt");
                string destinationPath = Path.Combine(Tools.LocalAppDataPath, "BlockedMods.txt");

                if (!File.Exists(backupFilePath))
                {
                    MessageBox.Show("BlockedMods.txt not found in the selected folder.");
                    return;
                }

                try
                {
                    File.Copy(backupFilePath, destinationPath, true);
                    RefreshDataGrid();
                    sbar("BlockedMods.txt restored successfully.");
                    if (log)
                        activityLog.WriteLog($"BlockedMods.txt restored from {selectedFolderPath}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred while restoring BlockedMods.txt: {ex.Message}", "Restore Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void deleteStarfieldCustominiToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var StarfieldCustomINIPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\My Games\Starfield\StarfieldCustom.ini";
                if (File.Exists(StarfieldCustomINIPath))
                {
                    File.Delete(StarfieldCustomINIPath);
                    sbar3("StarfieldCustom.ini deleted");
                }
                else
                    sbar3("StarfieldCustom.ini not found");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void UpdateAllProfiles()
        {
            // Cache current UI settings
            var activeProfile = cmbProfile.SelectedItem?.ToString();
            bool wasActiveOnly = ActiveOnly;
            bool wasCompareMode = Properties.Settings.Default.CompareProfiles;
            bool wasModStats = Properties.Settings.Default.ModStats;
            int totalChanges = 0;

            if (log)
                activityLog.WriteLog("Updating all profiles");

            // Temporarily disable CompareProfiles, ModStats and ActiveOnly filters
            Properties.Settings.Default.CompareProfiles = false;
            Properties.Settings.Default.ModStats = false;
            if (ActiveOnly)
                ActiveOnlyToggle();

            // Grab a snapshot of profile names
            var profiles = cmbProfile.Items
                                      .Cast<object>()
                                      .Select(o => o.ToString())
                                      .Where(n => !string.IsNullOrEmpty(n))
                                      .ToArray();

            if (profiles.Length == 0 || activeProfile == null)
            {
                MessageBox.Show("No valid profiles found");
                if (log)
                    activityLog.WriteLog("No valid profiles found");

                // Restore original state
                if (wasActiveOnly) ActiveOnlyToggle();
                Properties.Settings.Default.CompareProfiles = wasCompareMode;
                return;
            }

            // Profile folder path
            string folder = Properties.Settings.Default.ProfileFolder;

            // Iterate once per profile
            foreach (var name in profiles)
            {
                string path = Path.Combine(folder, name);
                SwitchProfile(path);
                RefreshDataGrid();

                totalChanges += SyncPlugins();

                if (AutoSort)
                    RunLOOT(true);
            }

            // Restore the original profile & UI state
            SwitchProfile(Path.Combine(folder, activeProfile));
            RefreshDataGrid();

            if (wasActiveOnly)
                ActiveOnlyToggle();

            // Restore CompareProfiles and persist settings
            Properties.Settings.Default.CompareProfiles = wasCompareMode;
            Properties.Settings.Default.ModStats = wasModStats;
            SaveSettings();

            sbar3($"Changes made: {totalChanges}");
            if (log)
                activityLog.WriteLog($"UpdateAllProfiles – total changes: {totalChanges}");
            progressBar1.Hide();
        }

        private static void GetSteamGamePath()
        {
            try
            {
                SteamGameLocator steamGameLocator = new SteamGameLocator();
                StarfieldGamePath = steamGameLocator.getGameInfoByFolder("Starfield").steamGameLocation;
                Properties.Settings.Default.StarfieldGamePath = StarfieldGamePath;
                SaveSettings();
            }
            catch (Exception ex)
            {
#if DEBUG
                MessageBox.Show(ex.Message);
#endif
            }
        }

        private void updateAllProfilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateAllProfiles();
        }

        private void sFSEPluginsToolStripMenuItem_Click(object sender, EventArgs e) // Open SFSE Plugins Directory
        {
            string SFSEPlugins = Path.Combine(StarfieldGamePath, @"Data\SFSE\Plugins");
            if (Directory.Exists(SFSEPlugins))
                Tools.OpenFolder(SFSEPlugins);
            else
                MessageBox.Show("Unable to find SFSE Plugins Directory");
        }

        private void webPageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Tools.OpenUrl("https://www.nexusmods.com/starfield/mods/10432?tab=files");
        }

        private void downloadsToolStripMenuItem_Click(object sender, EventArgs e) // Open Downloads Directory
        {
            string downloadsDirectory = Properties.Settings.Default.DownloadsDirectory;
            if (!string.IsNullOrEmpty(downloadsDirectory))
                Tools.OpenFolder(downloadsDirectory);
            else
                MessageBox.Show("It will be set after a mod has been installed.", "Downloads directory not set.", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void toggleToolStripMenuItem_Click(object sender, EventArgs e) // Log Toggle
        {
            toggleToolStripMenuItem.Checked = Properties.Settings.Default.Log = log = !toggleToolStripMenuItem.Checked;
            if (activityLog == null)
                EnableLog();
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e) // Log Delete
        {
            if (activityLog == null)
                return;
            activityLog.DeleteLog();
            if (log)
                EnableLog();
        }

        private void ShowLog() // Show Activity Log
        {
            tempstr = Properties.Settings.Default.LogFileDirectory;
            if (tempstr == "")
                tempstr = Tools.LocalAppDataPath;
            string pathToFile = Path.Combine(tempstr, "Activity Log.txt");
            if (File.Exists(pathToFile))
                Process.Start("explorer", pathToFile);
            else
                sbar3("Activity Log not found.");
        }

        private void viewLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowLog();
        }

        private void nexusTrackingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Tools.OpenUrl("https://www.nexusmods.com/starfield/mods/trackingcentre?tab=tracked+content+updates");
        }

        private void nexusUpdatedModsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Tools.OpenUrl("https://www.nexusmods.com/games/starfield/mods?sort=updatedAt");
        }

        private void DisableLog()
        {
            if (activityLog != null)
            {
                activityLog.Dispose();
                activityLog = null;
            }
            log = false;
            Properties.Settings.Default.Log = false;
            toggleToolStripMenuItem.Checked = false;
        }

        private void EnableLog()
        {
            tempstr = Properties.Settings.Default.LogFileDirectory;
            if (tempstr == "")
                tempstr = Tools.LocalAppDataPath;
            activityLog = new ActivityLog(Path.Combine(tempstr, "Activity Log.txt"));
            log = true;
        }

        private void btnLog_Click(object sender, EventArgs e)
        {
            ShowLog();
        }

        private void appAppDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Tools.OpenFolder(Tools.LocalAppDataPath);
        }

        private void ConvertLooseFiles(string esm = "")
        {
            if (log)
                activityLog.WriteLog("Converting loose files to archive(s)");
            returnStatus = 0;
            frmConvertLooseFiles frmCLF = new frmConvertLooseFiles(esm);
            frmCLF.StartPosition = FormStartPosition.CenterScreen;
            try
            {
                frmCLF.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error converting loose files. {ex.Message}.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            if (returnStatus > 0)
            {
                if (Tools.ConfirmAction("Delete Them?", "Loose File Folders Remain") == DialogResult.OK)
                    DeleteLooseFileFolders();
                LooseFilesOnOff(false);
            }
        }

        private void convertLooseFilesModToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConvertLooseFiles();
            if (returnStatus > 0)
                UpdatePlugins(); // Refresh the plugins list after conversion
        }

        private void deleteBlockedModstxtToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                File.Delete(Path.Combine(Tools.LocalAppDataPath, "BlockedMods.txt"));
                Tools.BlockedMods(); // Refresh blocked mods list
                InitDataGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error deleting BlockedMods.txt: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void blockedModsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            blockedModsToolStripMenuItem.Checked = !blockedModsToolStripMenuItem.Checked;
            Properties.Settings.Default.BlockedView = blockedModsToolStripMenuItem.Checked;
        }

        private void removeMissingModsFromBlockedModstxtToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var blockedMods = Tools.BlockedMods();
            List<string> plugins = tools.GetPluginList(); // Add .esm files
            var missingMods = plugins.Intersect(blockedMods);

            try
            {
                if (File.Exists(Path.Combine(Tools.LocalAppDataPath, "BlockedMods.txt")))
                {
                    File.WriteAllLines(Path.Combine(Tools.LocalAppDataPath, "BlockedMods.txt"), missingMods);
                    sbar3("Removed missing mods from BlockedMods.txt");
                    if (log)
                        activityLog.WriteLog("Removed missing mods from BlockedMods.txt");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error writing BlockedMods.txt: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void editLOOTUserlistyamlToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string LOOTPath = Properties.Settings.Default.LOOTPath;
            if (string.IsNullOrEmpty(LOOTPath)) // Check if LOOT path is set
                return;

            Tools.OpenFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"LOOT\games\Starfield\Userlist.yaml"));
            MessageBox.Show("Click OK to refresh");
            ReadLOOTGroups();
#if DEBUG
            /*if (Groups.groups != null)
            {
                foreach (var group in Groups.groups)
                    Debug.WriteLine($"Found group: {group.name}");
            }*/

            var x = Groups.groups.OrderBy(g => g.name).ToList();
            foreach (var item in x)
                Debug.WriteLine(item.name);

#endif
            RefreshDataGrid();
        }

        private void modBackupsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(Properties.Settings.Default.BackupDirectory))
                Tools.OpenFolder(Properties.Settings.Default.BackupDirectory);
            else
                MessageBox.Show("Backup directory will be set after backing up a mod", "Backup Directory Not Set", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void setDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select a directory for the log file";
                dialog.UseDescriptionForTitle = true;
                dialog.InitialDirectory = Properties.Settings.Default.LogFileDirectory;
                DialogResult result = dialog.ShowDialog();

                if (result != DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                    return;

                Properties.Settings.Default.LogFileDirectory = dialog.SelectedPath;
                SaveSettings();
                EnableLog();
            }
        }

        private void showAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewColumn column in dataGridView1.Columns)
            {
                column.Visible = true;
            }
            SetColumnVisibility(true, timeStampToolStripMenuItem, dataGridView1.Columns["TimeStamp"]);
            SetColumnVisibility(true, toolStripMenuAchievements, dataGridView1.Columns["Achievements"]);
            SetColumnVisibility(true, toolStripMenuCreationsID, dataGridView1.Columns["CreationsID"]);
            SetColumnVisibility(true, toolStripMenuFiles, dataGridView1.Columns["Files"]);
            SetColumnVisibility(true, toolStripMenuGroup, dataGridView1.Columns["Group"]);
            SetColumnVisibility(true, toolStripMenuIndex, dataGridView1.Columns["Index"]);
            SetColumnVisibility(true, toolStripMenuFileSize, dataGridView1.Columns["FileSize"]);
            SetColumnVisibility(true, uRLToolStripMenuItem, dataGridView1.Columns["URL"]);
            SetColumnVisibility(true, toolStripMenuVersion, dataGridView1.Columns["Version"]);
            SetColumnVisibility(true, toolStripMenuAuthorVersion, dataGridView1.Columns["AuthorVersion"]);
            SetColumnVisibility(true, toolStripMenuDescription, dataGridView1.Columns["Description"]);
            SetColumnVisibility(true, blockedToolStripMenuItem, dataGridView1.Columns["Blocked"]);
        }

        private void gameSelectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmGameSelect gameSelectForm = new frmGameSelect();
            gameSelectForm.StartPosition = FormStartPosition.CenterScreen;
            gameSelectForm.Show();
        }

        private void BackupContentCatalog()
        {
            using FolderBrowserDialog folderBrowserDialog = new();
            folderBrowserDialog.Description = "Choose folder to use to backup ContentCatalog.txt";
            folderBrowserDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); // Set initial directory to Documents Directory
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                string selectedFolderPath = folderBrowserDialog.SelectedPath;
                string FilePath = Path.Combine(Tools.StarfieldAppData, "ContentCatalog.txt");
                string destinationPath = Path.Combine(selectedFolderPath, "ContentCatalog.txt");
                if (!File.Exists(FilePath))
                {
                    MessageBox.Show("ContentCatalog.txt not found");
                    return;
                }
                File.Copy(FilePath, destinationPath, true);
                sbar("ContentCatalog.txt backed up.");
                if (log)
                    activityLog.WriteLog($"ContentCatalog.txt backed up to {selectedFolderPath}");
            }
        }

        private void backupContentCatalogtxtToolStripMenuItem_Click(object sender, EventArgs e)
        {
            BackupContentCatalog();
        }

        private void restoreContentCatalogtxtToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using FolderBrowserDialog folderBrowserDialog = new();
            folderBrowserDialog.Description = "Choose folder to restore ContentCatalog.txt from";
            folderBrowserDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); // Set initial directory to Documents Directory
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                string selectedFolderPath = folderBrowserDialog.SelectedPath;

                string backupFilePath = Path.Combine(selectedFolderPath, "ContentCatalog.txt");
                string destinationPath = Path.Combine(Tools.StarfieldAppData, "ContentCatalog.txt");

                if (!File.Exists(backupFilePath))
                {
                    MessageBox.Show("ContentCatalog.txt not found in the selected folder.");
                    return;
                }

                try
                {
                    File.Copy(backupFilePath, destinationPath, true);
                    RefreshDataGrid();
                    sbar("ContentCatalog.txt restored successfully.");
                    if (log)
                        activityLog.WriteLog($"ContentCatalog.txt restored from {selectedFolderPath}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred while restoring ContentCatalog.txt: {ex.Message}", "Restore Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void mnuExportPDF_Click(object sender, EventArgs e)
        {
            var tableData = new List<List<string>>();
            var groupData = new List<string>(); // Store group information separately

            var visibleColumns = dataGridView1.Columns
                .Cast<DataGridViewColumn>()
                .Where(col => col.Visible && (!ActiveOnly || col.Name != "ModEnabled"))
                .ToList();

            // Find and remove the group column from visible columns
            var groupColumn = visibleColumns.FirstOrDefault(col => col.Name.ToLower().Contains("group") || col.HeaderText.ToLower().Contains("group"));
            if (groupColumn != null)
            {
                visibleColumns.Remove(groupColumn);
            }

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (!row.IsNewRow && row.Visible)
                {
                    var rowData = new List<string>();

                    // Store group information
                    string groupValue = "";
                    if (groupColumn != null)
                    {
                        groupValue = row.Cells[groupColumn.Index].Value?.ToString() ?? "";
                    }
                    groupData.Add(groupValue);

                    foreach (var column in visibleColumns)
                    {
                        var cell = row.Cells[column.Index];
                        rowData.Add(cell.Value?.ToString() ?? "");
                    }

                    tableData.Add(rowData);
                }
            }

            // Group the data by group value while preserving order
            var groupedData = new Dictionary<string, List<List<string>>>();
            var groupOrder = new List<string>(); // Track the order of groups as they appear

            for (int i = 0; i < tableData.Count; i++)
            {
                string group = groupData[i];
                if (string.IsNullOrEmpty(group))
                    group = "Ungrouped";

                if (!groupedData.ContainsKey(group))
                {
                    groupedData[group] = new List<List<string>>();
                    groupOrder.Add(group); // Track the first occurrence of each group
                }

                groupedData[group].Add(tableData[i]);
            }

            var columnWidths = new List<int>();

            using (Graphics g = dataGridView1.CreateGraphics())
            {
                foreach (var column in visibleColumns)
                {
                    // Prefer DefaultCellStyle.Font or fallback to grid's font
                    var font = column.DefaultCellStyle.Font ?? dataGridView1.Font;
                    int maxWidth = TextRenderer.MeasureText(g, column.HeaderText ?? "", font).Width;

                    foreach (DataGridViewRow row in dataGridView1.Rows)
                    {
                        if (!row.IsNewRow && row.Visible)
                        {
                            string value = row.Cells[column.Index].Value?.ToString() ?? "";
                            int cellWidth = TextRenderer.MeasureText(g, value, font).Width;
                            maxWidth = Math.Max(maxWidth, cellWidth);
                        }
                    }

                    // Add padding and optionally clamp
                    columnWidths.Add(Math.Min(maxWidth + 12, 300)); // cap to 300pt max width
                }
            }

            // Generate PDF
            QuestPDF.Settings.License = LicenseType.Community;

            // Prompt for file name and path
            System.Windows.Forms.SaveFileDialog ExportActive = new()
            {
                Filter = "PDF File|*.pdf",
                Title = "Export to PDF",
                FileName = "Plugins.pdf",
            };

            DialogResult dlgResult = ExportActive.ShowDialog();
            if (dlgResult == DialogResult.OK)
            {
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.MaxSize(PageSizes.A0.Landscape());
                        page.Margin(20);
                        page.DefaultTextStyle(x => x.FontSize(12).FontColor(Colors.Black));

                        page.Footer().AlignLeft().Text(text =>
                        {
                            text.Span($"Exported on {DateTime.Now:yyyy-MM-dd}").FontSize(8).FontColor(Colors.Grey.Darken1);
                            text.Span(" - Page ").FontSize(8).FontColor(Colors.Grey.Darken1);
                            text.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Darken1);
                            text.Span(" of ").FontSize(8).FontColor(Colors.Grey.Darken1);
                            text.TotalPages().FontSize(8).FontColor(Colors.Grey.Darken1);
                        });

                        page.Content().Column(col =>
                        {
                            // Main title on first page
                            col.Item().Text($"Mod List Export {cmbProfile.Text}").FontSize(28).Bold().FontColor(Colors.Blue.Darken2).AlignCenter();
                            col.Item().PaddingBottom(20);

                            // Define single color per group
                            var groupColors = new[]
                            {
                        Colors.Blue.Darken2,      // Professional navy
                        Colors.Green.Darken2,     // Forest green
                        Colors.Purple.Darken2,    // Deep purple
                        Colors.Orange.Darken2,    // Burnt orange
                        Colors.Red.Darken2,       // Dark red
                        Colors.Teal.Darken2,      // Dark teal
                        Colors.Brown.Darken2,     // Dark brown
                        Colors.Indigo.Darken2     // Deep indigo
                    };

                            // Create one continuous table with all data
                            col.Item().Table(table =>
                            {
                                // Define columns
                                table.ColumnsDefinition(columns =>
                                {
                                    foreach (var width in columnWidths)
                                    {
                                        columns.ConstantColumn(width);
                                    }
                                });

                                // Header row that will repeat on each page
                                table.Header(header =>
                                {
                                    foreach (DataGridViewColumn column in visibleColumns)
                                    {
                                        header.Cell().Element(c => c.Background(Colors.Grey.Lighten3)
                                            .BorderBottom(2)
                                            .BorderColor(Colors.Blue.Darken2)
                                            .PaddingVertical(8)
                                            .PaddingHorizontal(8))
                                            .Text(text => text.Span(column.HeaderText).Bold().FontColor(Colors.Blue.Darken2));
                                    }
                                });

                                // Data rows for all groups - iterate in original order
                                int colorIndex = 0;
                                foreach (var groupName in groupOrder)
                                {
                                    var groupColor = groupColors[colorIndex % groupColors.Length];
                                    colorIndex++;

                                    // Group subheading row - single color background with white text
                                    table.Cell().ColumnSpan((uint)visibleColumns.Count)
                                        .Element(c => c.Background(groupColor)
                                            .PaddingVertical(10)
                                            .PaddingHorizontal(12))
                                        .Text(text => text.Span(groupName).FontSize(12).Bold().FontColor(Colors.White));

                                    // Data rows for this group - black text on white background
                                    foreach (var row in groupedData[groupName])
                                    {
                                        foreach (var cell in row)
                                        {
                                            table.Cell().Element(CellStyle).Text(text => text.Span(cell).FontColor(Colors.Black));
                                        }
                                    }
                                }

                                // Styling helper for data cells - clean white background with subtle borders
                                static IContainer CellStyle(IContainer container) => container
                                    .Background(Colors.White)
                                    .Padding(8)
                                    //.BorderBottom(1)
                                    .BorderColor(Colors.Grey.Lighten1);
                            });
                        });
                    });
                }).GeneratePdf(ExportActive.FileName);
            }

            // Open the PDF if the user confirms
            if (Tools.ConfirmAction("Open PDF", "Open the exported file", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                Tools.OpenFile(ExportActive.FileName);
        }

        private void runProgramToolStripMenuItem_Click(object sender, EventArgs e)
        {
            runProgramToolStripMenuItem.Checked = Properties.Settings.Default.RunProgram = !runProgramToolStripMenuItem.Checked;
        }

        private void programOptionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.Title = "Select the program to run";
            if (!string.IsNullOrEmpty(Properties.Settings.Default.RunProgramPath))
            {
                openFileDialog1.InitialDirectory = Path.GetDirectoryName(Properties.Settings.Default.RunProgramPath);
                openFileDialog1.FileName = Path.GetFileName(Properties.Settings.Default.RunProgramPath);
            }
            openFileDialog1.ShowDialog();
            if (!string.IsNullOrEmpty(openFileDialog1.FileName))
                Properties.Settings.Default.RunProgramPath = openFileDialog1.FileName;
        }

        private void modContentsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string modName;

            if (dataGridView1.CurrentRow == null || dataGridView1.CurrentRow.IsNewRow)
            {
                MessageBox.Show("Please select a mod first.");
                return;
            }

            var selectedRows = dataGridView1.SelectedRows.Cast<DataGridViewRow>().ToList();

            foreach (DataGridViewRow row in selectedRows)
            {
                modName = row.Cells["PluginName"].Value.ToString();

                frmModContents fmc = new(modName);
                fmc.Show();
            }
        }

        private void savesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string savesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Starfield", "Saves");
            if (Directory.Exists(savesPath))
            {
                Tools.OpenFolder(savesPath);
            }
            else
            {
                MessageBox.Show("Save game directory not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void readfilePathToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.OpenFileDialog openReadfile = new()
            {
                InitialDirectory = Properties.Settings.Default.ReadfilePath,
                Filter = "Exe Files|*.exe",
                Title = "Locate readfile.exe"
            };

            if (openReadfile.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(openReadfile.FileName))
            {
                Properties.Settings.Default.ReadfilePath = openReadfile.FileName;
                SaveSettings();
                sbar("Readfile path set to: " + openReadfile.FileName);
                if (log)
                    activityLog.WriteLog("Readfile path set to: " + openReadfile.FileName);
            }
        }

        private void configurationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmCacheConfig frmCacheConfig = new frmCacheConfig();
            frmCacheConfig.Show();
        }

        private void ResizeFormToFitDataGridView(Form parentForm)
        {
            int minHeight = 800, maxHeight, minWidth = 800, maxWidth;
            maxHeight = Screen.PrimaryScreen.WorkingArea.Height - 250;
            maxWidth = Screen.PrimaryScreen.WorkingArea.Width - 250;

            int totalRowHeight = dataGridView1.ColumnHeadersHeight;
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Visible)
                    totalRowHeight += row.Height;
            }

            // Extra padding for spacing, borders, etc.
            int padding = 250;
            int desiredHeight = totalRowHeight + padding;
            //int desiredWidth = totalRowWidth;

            // Clamp to min/max limits
            int clampedHeight = Math.Max(minHeight, Math.Min(desiredHeight, maxHeight));

            parentForm.Height = clampedHeight;

            int totalColumnWidth = 0;

            // Sum up the widths of all visible columns
            foreach (DataGridViewColumn col in dataGridView1.Columns)
            {
                if (col.Visible)
                    totalColumnWidth += col.Width;
            }

            // Add padding for row headers, vertical scrollbar, and borders
            int extraPadding = SystemInformation.VerticalScrollBarWidth
                             + dataGridView1.RowHeadersWidth
                             + (dataGridView1.BorderStyle == BorderStyle.None ? 0 : 2);

            // Calculate total client width needed
            int requiredClientWidth = totalColumnWidth + extraPadding;

            // Adjust the form's client size
            parentForm.Width = Math.Max(minWidth, Math.Min(requiredClientWidth, maxWidth));
            this.CenterToScreen();
        }

        private void resizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            resizeToolStripMenuItem.Checked = Properties.Settings.Default.Resize = !resizeToolStripMenuItem.Checked;
            if (resizeToolStripMenuItem.Checked)
                ResizeFormToFitDataGridView(this);
        }

        private void frmLoadOrder_Resize(object sender, EventArgs e)
        {
            progressBar1.Location = new Point((this.ClientSize.Width - progressBar1.Width) / 2, (this.ClientSize.Height - progressBar1.Height) / 2);
        }

        private void dataGridView1_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            sbar(e.Exception.Message);
        }

        private void allTheThingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            BackupPlugins();
            BackupBlockedMods();
            BackupContentCatalog();
            BackupProfiles();
        }

        private void enableSplashScreenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableSplashScreenToolStripMenuItem.Checked = Properties.Settings.Default.LoadScreenEnabled = !enableSplashScreenToolStripMenuItem.Checked;
            SaveSettings();
        }

        private void runBatchFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(Properties.Settings.Default.ReadFileBatchPath))
                Tools.OpenFile(Properties.Settings.Default.ReadFileBatchPath);
            else
                MessageBox.Show("Batch file path not set. Please set it in the settings.", "Batch File Not Set", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void scriptLogsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tempstr = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games\\Starfield\\Logs\\Script");
            if (!Directory.Exists(tempstr))
            {
                MessageBox.Show("Script logs directory not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Tools.OpenFolder(tempstr);
        }

        private void generateBGSArchivestxtToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<string> plugins = tools.GetPluginList().Select(p => Path.GetFileNameWithoutExtension(p)).ToList();
            List<string> allArchives = Directory.EnumerateFiles(Path.Combine(StarfieldGamePath, "Data"), "*.ba2").Select(p => Path.GetFileNameWithoutExtension(p)).ToList();
            List<string> bgsArchives = new List<string>();

            foreach (string fileName in allArchives)
            {
                foreach (string plugin in plugins)
                {
                    if (fileName.StartsWith(plugin))
                    {
                        bgsArchives.Add(fileName);
                        Debug.WriteLine($"Found  archive: {fileName}, plugin: {plugin}");
                    }
                    //break; // Found a match, skip to next fileName
                }
            }
        }

        private void steamDBToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Tools.OpenUrl("https://steamdb.info/app/1716740/depots/");
        }

        private void renameModToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<string> files = new();
            if (!CheckGamePath()) // Abort if game path not set
            {
                MessageBox.Show("Game path not set");
                return;
            }

            if (Tools.ConfirmAction("This can break stuff.", "Rename mod - Use with caution",MessageBoxButtons.OKCancel,MessageBoxIcon.Exclamation) != DialogResult.OK)
                return;

            string directoryPath = Path.Combine(StarfieldGamePath, "Data");
            var row = dataGridView1.CurrentRow;
            string ModName = row.Cells["PluginName"].Value.ToString();
            ModName = ModName[..ModName.LastIndexOf('.')]; // Strip extension
            string ModFile = Path.Combine(directoryPath, ModName);

            // Collect existing mod-related files
            string[] fixedExtensions = { ".esp", ".esm", " - main.ba2", " - voices_en.ba2" };
            foreach (var ext in fixedExtensions)
            {
                string fullPath = ModFile + ext;
                if (File.Exists(fullPath))
                    files.Add(fullPath);
            }

            // Handle dynamic texture files like " - textures*.ba2"
            string pattern = ModName + " - textures*.ba2";
            /*foreach (var pattern in texturePatterns)
            {*/
            string[] matchedFiles = Directory.GetFiles(directoryPath, Path.GetFileName(pattern));
            files.AddRange(matchedFiles);
            //}

            foreach (var item in files)
                Debug.WriteLine("Found: " + item);

            string userInput = Interaction.InputBox("New Name:", "Rename Mod", ModName);
            if (string.IsNullOrWhiteSpace(userInput))
                return;

            // Rename each file
            foreach (var oldPath in files)
            {
                string extensionPart = oldPath.Substring(ModFile.Length); // Get suffix like ".esp" or " - textures01.ba2"
                string newPath = Path.Combine(directoryPath, userInput + extensionPart);

                try
                {
                    File.Move(oldPath, newPath);
                    if (log)
                        activityLog.WriteLog($"Renamed: {Path.GetFileName(oldPath)} to {Path.GetFileName(newPath)}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to rename {Path.GetFileName(oldPath)}:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            SyncPlugins();
            sbar($"Mod {ModName} renamed to: {userInput}");
        }
    }
}