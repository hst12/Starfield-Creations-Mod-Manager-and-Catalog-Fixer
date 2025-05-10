using Microsoft.Win32;
using Narod.SteamGameFinder;
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
using System.Reflection;
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
        private Tools.ActivityLog activityLog;

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
#endif

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

            frmStarfieldTools StarfieldTools = new(); // Check the catalog
            tempstr = StarfieldTools.CatalogStatus;
            sbar4(tempstr);
            if (tempstr != null && StarfieldTools.CatalogStatus.Contains("Error"))
                StarfieldTools.Show(); // Show catalog fixer if catalog broken

            bool BackupStatus = false;

            try
            {
                string LooseFilesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"My Games\Starfield"), // Check if loose files are enabled
filePath = Path.Combine(LooseFilesDir, "StarfieldCustom.ini");
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
#if DEBUG
                MessageBox.Show(ex.Message, "Error opening StarfieldCustom.ini");
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
                        SaveSettings();
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
                SaveSettings();
            }

            // Hide menu items if LOOTPath is still unset
            bool lootPathIsEmpty = string.IsNullOrEmpty(Properties.Settings.Default.LOOTPath);
            toolStripMenuLOOTToggle.Visible = !lootPathIsEmpty;
            autoSortToolStripMenuItem.Visible = !lootPathIsEmpty;
            toolStripMenuLoot.Visible = !lootPathIsEmpty;
            toolStripMenuLootSort.Visible = !lootPathIsEmpty;

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

            SetupColumns();

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
                dataGridView1.DefaultCellStyle.SelectionBackColor = Color.Green; // Background color of selected cells
                dataGridView1.DefaultCellStyle.SelectionForeColor = Color.White; // Text color of selected cells
                statusStrip1.BackColor = Color.Black;
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
                MessageBox.Show(ex.Message);
            }

            // Initialise menu check marks from Properties.Settings.Default
            SetMenus();

            // Do a 1-time backup of Plugins.txt if it doesn't exist
            try
            {
                if (!File.Exists(Path.Combine(Tools.StarfieldAppData, "Plugins.txt.bak")))
                {
                    File.Copy(PluginsPath, Tools.StarfieldAppData + "Plugins.txt.bak");
                    sbar2("Plugins.txt backed up to Plugins.txt.bak");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Unable to find Plugins.txt");
            }

            // Do a 1-time backup of StarfieldCustom.ini if it doesn't exist
            tempstr = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"My Games\Starfield\StarfieldCustom.ini");
            if (!File.Exists(tempstr + ".bak") && File.Exists(tempstr))
            {
                sbar2("StarfieldCustom.ini backed up to StarfieldCustom.ini.bak");
                File.Copy(tempstr, tempstr + ".bak");
            }

            ReadLOOTGroups();

            // Initialise profiles
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
            }

            foreach (var arg in Environment.GetCommandLineArgs()) // Handle command line arguments
            {
                //MessageBox.Show(arg); // Show all arguments for debugging
                if (arg.Equals("-run", StringComparison.InvariantCultureIgnoreCase))
                {
                    RunGame();
                    Application.Exit();
                }
#if DEBUG
                if (arg.StartsWith("-profile", StringComparison.InvariantCultureIgnoreCase))
                {
                    MessageBox.Show(Environment.GetCommandLineArgs()[2]);
                    SwitchProfile(Path.Combine(Properties.Settings.Default.ProfileFolder, Environment.GetCommandLineArgs()[2]));
                    GetProfiles();
                }

                if (arg.StartsWith("-install")) // For future use (maybe) install mod from Nexus web link
                {
                    string strippedCommandLine = Environment.CommandLine
                        .Split(' ')
                        .Skip(1) // Skip the executable path
                        .Where(part => part != "-install ") // Remove the "-install" argument
                        .Aggregate("", (current, next) => current + " " + next)
                        .Trim();

                    Debug.WriteLine(strippedCommandLine);
                    InstallMod(strippedCommandLine);
                }
#endif
            }
        }

        private void SetupColumns()
        {
            var columnSettings = new (bool setting, ToolStripMenuItem menuItem, DataGridViewColumn column)[]
            {
                (Properties.Settings.Default.TimeStamp, timeStampToolStripMenuItem, dataGridView1.Columns["TimeStamp"]),
                (Properties.Settings.Default.Achievements, toolStripMenuAchievements, dataGridView1.Columns["Achievements"]),
                (Properties.Settings.Default.CreationsID, toolStripMenuCreationsID, dataGridView1.Columns["CreationsID"]),
                (Properties.Settings.Default.Files, toolStripMenuFiles, dataGridView1.Columns["Files"]),
                (Properties.Settings.Default.Group, toolStripMenuGroup, dataGridView1.Columns["Group"]),
                (Properties.Settings.Default.Index, toolStripMenuIndex, dataGridView1.Columns["Index"]),
                (Properties.Settings.Default.FileSize, toolStripMenuFileSize, dataGridView1.Columns["FileSize"]),
                (Properties.Settings.Default.URL, uRLToolStripMenuItem, dataGridView1.Columns["URL"]),
                (Properties.Settings.Default.Version, toolStripMenuVersion, dataGridView1.Columns["Version"]),
                (Properties.Settings.Default.AuthorVersion, toolStripMenuAuthorVersion, dataGridView1.Columns["AuthorVersion"]),
                (Properties.Settings.Default.Description, toolStripMenuDescription, dataGridView1.Columns["Description"]),
                (Properties.Settings.Default.Blocked, blockedToolStripMenuItem, dataGridView1.Columns["Blocked"])
            };

            foreach (var (setting, menuItem, column) in columnSettings)
            {
                SetColumnVisibility(setting, menuItem, column);
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
            if (Properties.Settings.Default.VortexPath != "")
                vortexToolStripMenuItem.Visible = true;

            // Display Loose Files status
            sbarCCC(looseFilesDisabledToolStripMenuItem.Checked ? "Loose files enabled" : "Loose files disabled");

            // Apply bold styling when ActiveOnly is enabled
            btnActiveOnly.Font = new Font(btnActiveOnly.Font, ActiveOnly ? FontStyle.Bold : FontStyle.Regular);

            // Reset defaults if AutoReset is enabled
            if (settings.AutoReset)
                ResetDefaults();

            // Handle AutoUpdate logic
            if (!AutoUpdate) return;

            int addedMods = AddMissing();
            int removedMods = RemoveMissing();

            if (addedMods + removedMods > 0)
            {
                sbar4($"Added: {addedMods}, Removed: {removedMods}");
                SavePlugins();

                if (AutoSort)
                    RunLOOT(true);

                InitDataGrid();
            }
        }

        private static void SetColumnVisibility(bool condition, ToolStripMenuItem menuItem, DataGridViewColumn column)
        {
            menuItem.Checked = condition;
            column.Visible = condition;
        }

        private async Task RefreshDataGrid()
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
                    @"LOOT\games\Starfield\userlist.yaml"));
                Groups = deserializer.Deserialize<Tools.Configuration>(yamlContent);
            }
            catch (Exception ex)
            {
#if DEBUG
                MessageBox.Show(ex.Message, "Yaml decoding error\nLOOT userlist.yaml possibly corrupt", MessageBoxButtons.OK, MessageBoxIcon.Stop);
#endif
                sbar3(ex.Message);
            }
        }

        private void KeyEvent(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
                RefreshDataGrid();
        }

        private static bool CheckStarfieldCustom()
        {
            return Tools.FileCompare(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                     @"My Games\Starfield\StarfieldCustom.ini"), Path.Combine(Tools.CommonFolder, "StarfieldCustom.ini"));
        }

        private void InitDataGrid()
        {
            int EnabledCount = 0, IndexCount = 1, esmCount = 0, espCount = 0, ba2Count, mainCount, i, versionDelimiter, dotIndex;
            string loText = Path.Combine(Tools.StarfieldAppData, "Plugins.txt"),
                   LOOTPath = Properties.Settings.Default.LOOTPath,
                   StatText = "", directory, pluginName, rawVersion;

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

            dataGridView1.Rows.Clear();
            SetColumnVisibility(false, toolStripMenuCreationsID, dataGridView1.Columns["CreationsID"]); // Temporarily turn off these columns
            SetColumnVisibility(false, uRLToolStripMenuItem, dataGridView1.Columns["URL"]);
            dataGridView1.SuspendLayout(); // Suspend UI updates to avoid redraw for every row addition.

            try
            {
                var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, Tools.Creation>>(json);
                data.Remove("ContentCatalog"); // Remove header

                foreach (var kvp in data)
                {
                    try
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
                    catch (Exception ex)
                    {
                        sbar(ex.Message);
#if DEBUG
                        MessageBox.Show(ex.Message);
#endif
                    }
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                MessageBox.Show(ex.Message);
#endif
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
                        if (double.TryParse(rawVersion[..versionDelimiter], out double seconds))
                            modVersion = start.AddSeconds(seconds).Date.ToString("yyyy-MM-dd");
                    }
                    modFiles = CreationsFiles[idx];
                    aSafe = AchievementSafe[idx] ? "Yes" : "";
                    modTimeStamp = Tools.ConvertTime(TimeStamp[idx]).ToString();
                    modID = CreationsID[idx];
                    modFileSize = FileSize[idx] / 1024;
                    url = $"https://creations.bethesda.net/en/starfield/details/{(modID.Length > 3 ? modID[3..] : modID)}";
                }

                // Add new row.
                var newRowIndex = dataGridView1.Rows.Add();
                var row = dataGridView1.Rows[newRowIndex];

                // Update group information if available.
                if (!string.IsNullOrEmpty(LOOTPath) && Groups.groups != null && isGroupVisible)
                {
                    var group = Groups.plugins.FirstOrDefault(p => p.name.Equals(pluginName, StringComparison.OrdinalIgnoreCase));
                    if (group != null)
                    {
                        row.Cells["Group"].Value = group.group;
                        // If a group URL exists, override our URL and description.
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
                    row.Cells["Blocked"].Value = true;
                }

                EnabledCount += modEnabled ? 1 : 0;

                // Special handling for Bethesda Game Studios mods.
                if (pluginName.StartsWith("sfbgs", StringComparison.OrdinalIgnoreCase))
                {
                    string currentGroup = row.Cells["Group"].Value?.ToString() ?? "Bethesda Game Studios Creations";
                    row.Cells["Group"].Value = $"{currentGroup} (Bethesda)";
                }

                // Update required cells.
                row.Cells["ModEnabled"].Value = modEnabled;
                row.Cells["PluginName"].Value = pluginName;
                row.Cells["CreationsID"].Value = modID;
                row.Cells["URL"].Value = url;

                // Update additional columns if visible
                if (isDescriptionVisible)
                    row.Cells["Description"].Value = description;
                if (isVersionVisible)
                    row.Cells["Version"].Value = modVersion;
                if (isAuthorVersionVisible)
                    row.Cells["AuthorVersion"].Value = authorVersion;
                if (isTimeStampVisible)
                    row.Cells["TimeStamp"].Value = modTimeStamp;
                if (isAchievementsVisible)
                    row.Cells["Achievements"].Value = aSafe;
                if (isFilesVisible)
                    row.Cells["Files"].Value = modFiles;
                if (isFileSizeVisible)
                    row.Cells["FileSize"].Value = modFileSize != 0 ? modFileSize : null;
                if (isIndexVisible)
                    row.Cells["Index"].Value = IndexCount++;
            } // End of main loop

            progressBar1.Value = progressBar1.Maximum;
            dataGridView1.ResumeLayout(); // Resume layout after all rows have been processed.
            progressBar1.Hide();

            // -- Process mod stats if the Starfield game path is set --
            if (!string.IsNullOrEmpty(StarfieldGamePath) && Properties.Settings.Default.ModStats)
            {
                try
                {
                    var BGSArchives = File.ReadLines(Path.Combine(Tools.CommonFolder, "BGS Archives.txt"))
    .Select(line => line[..^4].ToLower())
    .ToHashSet();

                    var archives = Directory.EnumerateFiles(Path.Combine(StarfieldGamePath, "Data"), "*.ba2")
    .Select(file => Path.GetFileNameWithoutExtension(file)?.ToLower())
    .Except(BGSArchives)
    .ToList();

                    var plugins = File.ReadLines(loText)
                        .Where(line => line.StartsWith('*'))
                        .Select(line => line[1..].Split('.')[0].ToLower())
                        .Where(plugin => !BGSArchives.Contains(plugin))
                        .ToList();

                    List<string> suffixes = new(Tools.Suffixes);

                    directory = Path.Combine(StarfieldGamePath, "Data");

                    ba2Count = Directory.EnumerateFiles(directory, "*.ba2").Count();

                    esmCount = Directory.EnumerateFiles(directory, "*.esm").Count();

                    espCount = Directory.EnumerateFiles(directory, "*.esp")
                        .Select(file => Path.GetFileNameWithoutExtension(file)?.ToLower())
                        .Count(plugin => plugins.Contains(plugin));

                    mainCount = Directory.EnumerateFiles(directory, "* - main*.ba2")
                        .Select(file => Path.GetFileNameWithoutExtension(file)?.ToLower())
                        .Select(file => file.Replace(" - main", string.Empty)) // Remove "- main" suffix for matching
                        .Count(mod => plugins.Contains(mod));

                    i = Directory.EnumerateFiles(directory, "* - texture*.ba2").Select(file => Path.GetFileNameWithoutExtension(file)?.ToLower())
                    .Select(file => file.Replace(" - textures", string.Empty)) // Remove "- textures" suffix for matching
                    .Count(mod => plugins.Contains(mod));

                    StatText = $"Mods: Creations {CreationsPlugin.Count}, Other {dataGridView1.RowCount - CreationsPlugin.Count}, " +
                               $"Enabled: {EnabledCount}, esm: {esmCount}, Archives Total: {ba2Count}, Enabled - Main: {mainCount}, Textures: {i}";

                    if (espCount > 0)
                        StatText += $", esp files: {espCount}";

                    if (dataGridView1.RowCount - CreationsPlugin.Count < 0)
                    {
                        sbar4("Catalog/Plugins mismatch - Run game to solve");
#if DEBUG
                        MessageBox.Show("Catalog/Plugins mismatch - Run game to solve");
#endif
                    }
                }
                catch (Exception ex)
                {
                    sbar("Starfield path needs to be set for mod stats");
#if DEBUG
                    MessageBox.Show(ex.Message);
#endif
                }
            }
            else
            {
                sbar("Starfield path needs to be set for mod stats");
            }

            // Restore column visibility based on user settings.
            SetColumnVisibility(Properties.Settings.Default.CreationsID, toolStripMenuCreationsID, dataGridView1.Columns["CreationsID"]);
            SetColumnVisibility(Properties.Settings.Default.URL, uRLToolStripMenuItem, dataGridView1.Columns["URL"]);

            if (ActiveOnly)
            {
                sbar("Hiding inactive mods...");
                statusStrip1.Refresh();
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (!(bool)row.Cells["ModEnabled"].Value)
                        row.Visible = false;
                }
            }

            sbar(StatText);
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

            LastProfile ??= Properties.Settings.Default.LastProfile;
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
                sbar2("Backup done");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Backup failed");
            }
        }

        private void btnBackupPlugins_Click(object sender, EventArgs e)
        {
            BackupPlugins();
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
                sbar2("Restore done");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Restore failed");
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
            toolStripStatusStats.ForeColor = Color.Red;
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
                string cellText = cellValue?.ToString().ToLowerInvariant() ?? string.Empty;

                if (cellText.Contains(searchQuery))
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
            if (Tools.ConfirmAction("This will reset your current load order", "Enable all mods?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                EnableAll();
        }

        private void toolStripMenuDisableAll_Click(object sender, EventArgs e)
        {
            if (Tools.ConfirmAction("This will reset your current load order", "Disable all mods?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
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
            SaveFileDialog saveDialog = new()
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
            if (!File.Exists(ProfileName))
                return;

            if (log)
            {
#if DEBUG
                StackTrace stackTrace = new StackTrace();
                StackFrame frame = stackTrace.GetFrame(1); // Get the caller
                activityLog.WriteLog($"Switching profile to {ProfileName}, Called from {frame.GetMethod().Name}");
#else
                activityLog.WriteLog($"Switching profile to {ProfileName}");
#endif
            }

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
                sbar2("Error switching profile");
#if DEBUG
                MessageBox.Show(ex.Message);
#endif
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string profileFolder = Properties.Settings.Default.ProfileFolder
                                   ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            OpenFileDialog openPlugins = new()
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
                isModified = true;
            }

            if (log)
                activityLog.WriteLog($"Adding missing plugins - {addedFiles}");
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

            isModified = removedFiles > 0;

            if (log)
                activityLog.WriteLog($"Removing missing plugins - {removedFiles}");

            return removedFiles;
        }

        private int AddRemove()
        {
            int ReturnStatus = AddMissing() + RemoveMissing();
            if (ReturnStatus > 0)
                SavePlugins();

            return ReturnStatus;
        }

        private void toolStripMenuAutoClean_Click(object sender, EventArgs e)
        {
            sbar3($"Changes made: {AddRemove().ToString()}");
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
        }

        private static void SaveSettings()
        {
            Properties.Settings.Default.Save();
        }

        private void cmbProfile_SelectedIndexChanged(object sender, EventArgs e)
        {
            SwitchProfile(Path.Combine(Properties.Settings.Default.ProfileFolder, (string)cmbProfile.SelectedItem));
        }

        private void InstallMod(string InstallMod = "")
        {
            string extractPath = Path.Combine(Path.GetTempPath(), "hstTools");
            bool SFSEMod = false, looseFileMod = false;
            int filesInstalled = 0;

            if (!CheckGamePath()) // Bail out if game path not set
                return;

            // Clean the extract directory if it exists.
            if (Directory.Exists(extractPath))
            {
                try { Directory.Delete(extractPath, true); }
                catch { /* Optionally log or ignore cleanup errors */ }
            }

            // Obtain the mod file path either from the parameter or by showing a file dialog.
            string modFilePath = InstallMod;
#if DEBUG
            //MessageBox.Show($"Installing {InstallMod}");
#endif
            if (string.IsNullOrEmpty(modFilePath))
            {
                using (OpenFileDialog openMod = new OpenFileDialog
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
                MessageBox.Show(ex.Message);
                loadScreen.Close();
                return;
            }

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
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }

            // Install Loose files
            var looseFileDirs = new[] { "materials", "meshes", "interface", "textures", "geometries", "scripts", "sound", "naf" };

            // Define the target directory
            var targetDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"My Games\Starfield\Data");

            // Ensure the target directory exists
            Directory.CreateDirectory(targetDir);

            // Recursively search for each directory and copy its contents
            foreach (var dirName in looseFileDirs)
            {
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
            }
            if (SFSEMod)
            {
                sbar3("SFSE mod installed");
            }
            if (filesInstalled > 0)
            {
                AddMissing();
                SavePlugins();
                if (AutoSort && string.IsNullOrEmpty(InstallMod))
                    RunLOOT(true);

                sbar3($"Mod installed: {filesInstalled} files");
            }
            else
            {
                sbar3("Nothing installed");
            }

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
                                activityLog.WriteLog($"Installing {modFile}");
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

            if (chkProfile.Checked)
                GetProfiles();
        }

        private void toolStripMenuExportActive_Click(object sender, EventArgs e)
        {
            var exportDialog = new SaveFileDialog
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
                if (row.Cells["ModEnabled"].Value is bool enabled && enabled)
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
                Size dragSize = SystemInformation.DragSize;
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

            // Create a copy of selected rows to avoid collection-modification issues.
            var selectedRows = dataGridView1.SelectedRows.Cast<DataGridViewRow>().ToList();
            string dataDirectory = Path.Combine(StarfieldGamePath, "Data");

            foreach (var row in selectedRows)
            {
                // Get the mod name from the PluginName cell (before the first dot).
                string pluginName = row.Cells["PluginName"].Value?.ToString() ?? string.Empty;
                int dotIndex = pluginName.IndexOf('.');
                if (dotIndex < 0)
                    continue;

                string modName = pluginName.Substring(0, dotIndex);

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

                    SavePlugins();
                    sbar3($"Mod '{modName}' uninstalled.");
                    if (log)
                        activityLog.WriteLog($"Uninstall mod: {modName}");
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
            Properties.Settings.Default.GameVersion = GameVersion;
            SaveSettings();
            Form SS = new frmSplashScreen();

            sbar("Starting game...");
            if (GameVersion != MS)
            {
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
            AddRemove();
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
            if (isModified)
                SavePlugins();
            SaveSettings();
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
            RefreshDataGrid();

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
            string pathToFile = (Path.Combine(Tools.StarfieldAppData, "Plugins.txt"));
            Process.Start("explorer", pathToFile);
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
            Tools.OpenFolder(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"My Games\Starfield"));
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

            OpenFileDialog OpenEXE = new()
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
            string pathToFile = (Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"My Games\Starfield\StarfieldCustom.ini"));
            Process.Start("explorer", pathToFile);
        }

        private void editContentCatalogtxtToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string pathToFile = Tools.GetCatalogPath();
            Process.Start("explorer", pathToFile);
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
                sbar3(ChangeCount + " Change(s) made to Vortex created files");
            return ChangeCount;
        }

        private void undoVortexChangesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UndoVortexChanges(true);
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            int changes = AddRemove() + RemoveDuplicates();
            if (AutoSort && changes > 0)
                RunLOOT(true);

            sbar3($"Changes made: {changes}");
        }

        private void LooseFilesOnOff(bool enable)
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Starfield", "StarfieldCustom.ini");

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
                activityLog.WriteLog($"Removed {removedCount} duplicates from Plugins.txt");
            sbar4($"Duplicates removed: {removedCount}");
            return removedCount;
        }

        private void removeDuplicatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RemoveDuplicates();
        }

        private void ActiveOnlyToggle()
        {
            ActiveOnly = activeOnlyToolStripMenuItem.Checked = !activeOnlyToolStripMenuItem.Checked;
            Properties.Settings.Default.ActiveOnly = ActiveOnly;

            sbar4("Loading...");
            statusStrip1.Refresh();

            bool showAll = !ActiveOnly;

            dataGridView1.Rows.Cast<DataGridViewRow>()
                .ToList()
                .ForEach(row => row.Visible = showAll || (row.Cells["ModEnabled"].Value as bool? ?? false));

            sbar4(showAll ? "All mods shown" : "Active mods only");

            // Set button to bold for active
            btnActiveOnly.Font = new Font(btnActiveOnly.Font, ActiveOnly ? FontStyle.Bold : FontStyle.Regular);
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
        }

        private void darkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            darkToolStripMenuItem.Checked = !darkToolStripMenuItem.Checked;
            dataGridView1.EnableHeadersVisualStyles = false;
            System.Windows.Forms.Application.SetColorMode(SystemColorMode.Dark);
            Properties.Settings.Default.DarkMode = 1;
            lightToolStripMenuItem.Checked = false;
            systemToolStripMenuItem.Checked = false;
        }

        private void systemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            systemToolStripMenuItem.Checked = !systemToolStripMenuItem.Checked;
            System.Windows.Forms.Application.SetColorMode(SystemColorMode.System);
            Properties.Settings.Default.DarkMode = 2;
            lightToolStripMenuItem.Checked = false;
            darkToolStripMenuItem.Checked = false;
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
            ShowRecommendedColumns();
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

            SaveSettings();
            SetMenus();
        }

        private void enableAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Tools.ConfirmAction("Enable All settings?", "This will turn on a most of the Tools menu settings and reset ini settings", MessageBoxButtons.OKCancel,
                MessageBoxIcon.Exclamation) == DialogResult.OK)
            {
                ChangeSettings(true);
                ResetDefaults();
            }
            if (!ActiveOnly)
                ActiveOnlyToggle();
        }

        private void disableAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ChangeSettings(false);
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
        }

        /*private void SetAchievement(bool OnOff) // Experimental. Should probably remove
        {
            string jsonFilePath = Tools.GetCatalogPath(), json = File.ReadAllText(jsonFilePath);
            var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, Tools.Creation>>(json);

            data.Remove("ContentCatalog");

            foreach (var kvp in data)
            {
                int selectedIndex = dataGridView1.SelectedRows[0].Index;
                if (dataGridView1.Rows[selectedIndex].Cells["CreationsID"].Value.ToString() == kvp.Key.ToString())
                {
                    kvp.Value.AchievementSafe = OnOff;
                    dataGridView1.Rows[selectedIndex].Cells["Achievements"].Value = OnOff ? "Yes" : "";
                }
            }

            json = Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);

            // Hack the Bethesda header back in
            json = Tools.MakeHeader() + json[1..];

            File.WriteAllText(Tools.GetCatalogPath(), json); // Write updated catalog
            //RefreshDataGrid();
        }
        private void disableAchievementFlagToolStripMenuItem_Click(object sender, EventArgs e) // Experimental. Should probably remove
        {
            SetAchievement(false);
        }

        private void enableAchievementFlagToolStripMenuItem_Click(object sender, EventArgs e) // Experimental. Should probably remove
        {
            SetAchievement(true);
        }*/

        private void openAllActiveModWebPagesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int i;
            string url;

            if (Tools.ConfirmAction("Are you sure you want to open all mod web pages?", "This might take a while and a lot of memory",
                MessageBoxButtons.YesNo) == DialogResult.Yes)
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
                MessageBox.Show("Current archive will continue to be created", "Press F12 to stop operation");
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

                    // Check if archive already exists, bail out on user cancel
                    if (!File.Exists(zipPath))
                    {
                        sbar3($"Creating archive for {ModName}...");
                        statusStrip1.Refresh();
                        CreateZipFromFiles(files, zipPath); // Make zip
                        sbar3($"{ModName} archived");
                        statusStrip1.Refresh();
                        modsArchived++;
                    }
                    files.Clear();
                }
                sbar3(modsArchived + " Mod(s) archived");
            }
        }

        private int CheckArchives()
        {
            List<string> BGSArchives = Tools.BGSArchives();
            List<string> suffixes = new(Tools.Suffixes);
            List<string> archives = [];
            List<string> plugins = [];
            List<string> orphaned = [];
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
                List<string> modArchives = archives
                    .Except(BGSArchives) // Exclude BGS Archives
                    .Select(s =>
                        suffixes.Aggregate(
                            s.ToLower().Replace(".ba2", string.Empty), // Start by removing ".ba2"
                            (current, suffix) => current.Replace(suffix, string.Empty) // Remove all suffixes dynamically
                        )
                    ).ToList();

                // Build a list of archives to delete with full path
                orphaned = modArchives.Except(plugins).ToList(); // Strip out esm files to get orphaned archives
                suffixes.Add(""); // Add a blank suffix
                foreach (var item in orphaned)
                {
                    foreach (var suffix in suffixes)
                    {
                        tempstr = Path.Combine(StarfieldGamePath, "Data") + @"\" + Path.GetFileNameWithoutExtension(item) + suffix + ".ba2";
                        if (File.Exists(tempstr))
                            toDelete.Add(tempstr);
                    }
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
            ChangeSettings(false);
            File.Delete(Path.Combine(Tools.StarfieldAppData, "Plugins.txt"));
        }

        private void toolStripMenuAddToProfile_Click(object sender, EventArgs e)
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

            try
            {
                foreach (DataGridViewRow selectedRow in dataGridView1.SelectedRows)
                {
                    if (selectedRow.Cells["PluginName"].Value != null) // Ensure the cell value is not null
                    {
                        frmAddModToProfile addMod = new(profiles, selectedRow.Cells["PluginName"].Value.ToString());
                        addMod.ShowDialog(cmbProfile);
                    }
                    if (Tools.ConfirmAction("Run update/sort on all profiles", "Update All Profiles?",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        UpdateAllProfiles();
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                MessageBox.Show("Error: " + ex.Message);
#endif
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
        }

        private void frmLoadOrder_Load(object sender, EventArgs e)
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

            int minWidth = 1000; // Set your minimum width
            int minHeight = 500; // Set your minimum height

            if (this.Width < minWidth || this.Height < minHeight)
            {
                this.Size = new Size(minWidth, minHeight);
                this.Location = new Point(
        (Screen.PrimaryScreen.WorkingArea.Width - this.Width) / 2,
        (Screen.PrimaryScreen.WorkingArea.Height - this.Height) / 2
    );
            }

            progressBar1.Width = 400; // Set the width of the progress bar
            progressBar1.Height = 50; // Set the height of the progress bar
            progressBar1.Location = new Point((this.ClientSize.Width - progressBar1.Width) / 2, (this.ClientSize.Height - progressBar1.Height) / 2);

            if (Properties.Settings.Default.Log)
            {
                EnableLog();
            }
        }

        private void toolStripMenuResetWindow_Click(object sender, EventArgs e)
        {
            ResetWindowSize();
        }

        private void disableAllWarningToolStripMenuItem_Click(object sender, EventArgs e)
        {
            disableAllWarningToolStripMenuItem.Checked = !disableAllWarningToolStripMenuItem.Checked;
            Properties.Settings.Default.NoWarn = disableAllWarningToolStripMenuItem.Checked;
            NoWarn = disableAllWarningToolStripMenuItem.Checked;
        }

        private void toolStripMenuExportCSV_Click(object sender, EventArgs e)
        {
            int i, j, ExportedLines = 0;

            SaveFileDialog ExportActive = new()
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
                        for (i = 0; i < dataGridView1.Rows.Count; i++)
                        {
                            ExportedLines++;
                            for (j = 0; j < dataGridView1.Columns.Count; j++)
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
        }

        private void toolStripMenuDescription_Click(object sender, EventArgs e)
        {
            toolStripMenuDescription.Checked = !toolStripMenuDescription.Checked;
            dataGridView1.Columns["Description"].Visible = toolStripMenuDescription.Checked;
            Properties.Settings.Default.Description = toolStripMenuDescription.Checked;
        }

        private void toolStripMenuItemHideAll_Click(object sender, EventArgs e)
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
            var items = new[]
       {
    ("Group", toolStripMenuGroup),
    ("Version", toolStripMenuVersion),
    ("AuthorVersion", toolStripMenuAuthorVersion),
    ("Description", toolStripMenuDescription),
    ("Blocked", blockedToolStripMenuItem)
};

            foreach (var (columnName, menuItem) in items)
            {
                SetColumnVisibility(true, menuItem, dataGridView1.Columns[columnName]);
                Properties.Settings.Default[columnName] = true;
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
            string pathToFile = (Tools.LocalAppDataPath + "BlockedMods.txt");
            Process.Start("explorer", pathToFile);
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

        private void prepareForCreationsUpdateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!Properties.Settings.Default.CreationsUpdate) // Catalog Auto Restore off etc.
            {
                prepareForCreationsUpdateToolStripMenuItem.Checked = true;
                Properties.Settings.Default.CreationsUpdate = true;
                Properties.Settings.Default.AutoRestore = false;
                if (log)
                    activityLog.WriteLog("Creations Update started");
                if (Tools.ConfirmAction("1. Run the game and update Creations mods.\n2. Don't Load a Save Game\n3. Quit the game and run this app again\n\n" +
                    "To Cancel this option," +
                    " click this menu option again\n\nRun the game now?", "Steps to Update Creations Mods", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                    true) == DialogResult.Yes)
                    RunGame(); ;
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

        private void dataGridView1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void profileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Tools.OpenFolder(Properties.Settings.Default.ProfileFolder);
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

        private void starUIConfiguratorToolStripMenuItem_Click(object sender, EventArgs e)
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
                return 0;
            }
        }

        private void restoreStarfieldiniToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RestoreStarfieldINI();
        }

        private void resetEverythingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int actionCount;

            if (Tools.ConfirmAction("This will reset all settings and delete all loose files folders", "Are you sure?", MessageBoxButtons.YesNo,
                MessageBoxIcon.Exclamation, true) == DialogResult.No)
                return;
            actionCount = RestoreStarfieldINI();
            actionCount += DeleteLooseFileFolders();
            actionCount += ResetDefaults();
            actionCount += CheckArchives();
            sbar3(actionCount.ToString() + " Change(s) made");
        }

        public static void ResetPreferences()
        {
            string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appPreferencesPath = Path.Combine(localAppDataPath, "Starfield_Tools");

            if (Tools.ConfirmAction("Are you sure you want to reset user preferences?", "This will delete all user settings and preferences", MessageBoxButtons.YesNo,
                MessageBoxIcon.Exclamation, true) == DialogResult.No) // Override Nowarn
                return;

            if (Directory.Exists(appPreferencesPath))
            {
                Directory.Delete(appPreferencesPath, true); // true to delete subdirectories and files
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

        private void modStatsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            modStatsToolStripMenuItem.Checked = !modStatsToolStripMenuItem.Checked;
            Properties.Settings.Default.ModStats = modStatsToolStripMenuItem.Checked;
            if (modStatsToolStripMenuItem.Checked)
                RefreshDataGrid();
        }

        private void backupProfilesToolStripMenuItem_Click(object sender, EventArgs e)
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
            }
            sbar3("Profiles backed up to Backup folder");
        }

        private void restoreProfilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.ProfileFolder == "" || !Directory.Exists(Path.Combine(Properties.Settings.Default.ProfileFolder, "Backup")))
            {
                MessageBox.Show("No profile or backup folder set");
                return;
            }

            if (Tools.ConfirmAction("Restore Backup", "Restore Backup", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.Cancel)
            {
                sbar3("Restore cancelled");
                return;
            }

            if (Directory.Exists(Path.Combine(Properties.Settings.Default.ProfileFolder, "Backup")))
            {
                foreach (var item in Directory.EnumerateFiles(Path.Combine(Properties.Settings.Default.ProfileFolder, "Backup"), "*.txt", SearchOption.TopDirectoryOnly))
                {
                    string fileName = Path.GetFileName(item);
                    string destinationPath = Path.Combine(Properties.Settings.Default.ProfileFolder, fileName);
                    File.Copy(item, destinationPath, true);
                }
                sbar3("Profiles restored from Backup folder");
                RefreshDataGrid();
            }
            else
            {
                sbar3("No backup folder found");
            }
        }

        private void mnuBackupBlockedMods_Click(object sender, EventArgs e)
        {
            using FolderBrowserDialog folderBrowserDialog = new();
            folderBrowserDialog.Description = "Choose folder to use to backup BlockedMods.txt";
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
                sbar3("BlockedMods.txt backed up successfully.");
            }
        }

        private void mnuRestoreBlockedMods_Click(object sender, EventArgs e)
        {
            using FolderBrowserDialog folderBrowserDialog = new();
            folderBrowserDialog.Description = "Choose folder to restore BlockedMods.txt from";
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
                    sbar3("BlockedMods.txt restored successfully.");
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
            string activeProfile = cmbProfile.SelectedItem.ToString();
            bool activeStatus = ActiveOnly, profileChanges = Properties.Settings.Default.CompareProfiles;
            int changes = 0;

            Properties.Settings.Default.CompareProfiles = false;
            SaveSettings();

            if (ActiveOnly)
                ActiveOnlyToggle();

            if (cmbProfile.Items.Count == 0 || cmbProfile.SelectedItem == null)
            {
                MessageBox.Show("No valid profiles found");
                return;
            }

            foreach (var item in cmbProfile.Items)
            {
                SwitchProfile(Path.Combine(Properties.Settings.Default.ProfileFolder, item.ToString()));
                RefreshDataGrid();
                changes += AddRemove() + RemoveDuplicates();
                if (AutoSort)
                    RunLOOT(true);
            }
            SwitchProfile(Path.Combine(Properties.Settings.Default.ProfileFolder, activeProfile));
            RefreshDataGrid();
            if (activeStatus)
                ActiveOnlyToggle();

            Properties.Settings.Default.CompareProfiles = profileChanges;
            sbar3($"Changes made: {changes}");
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
                //sbar3(ex.Message);
#if DEBUG
                MessageBox.Show(ex.Message);
#endif
            }
        }

        private void updateAllProfilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateAllProfiles();
        }

        private void sFSEPluginsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string SFSEPlugins = Path.Combine(StarfieldGamePath, @"Data\SFSE\Plugins");
            if (Directory.Exists(SFSEPlugins))
                Tools.OpenFolder(SFSEPlugins);
            else
                MessageBox.Show("Unable to find SFSE Plugins Directory");
        }

        private void webPageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Tools.OpenUrl(Path.Combine("docs", "index.html"));
        }

        private void downloadsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string downloadsDirectory = Properties.Settings.Default.DownloadsDirectory;
            if (!string.IsNullOrEmpty(downloadsDirectory))
                Tools.OpenFolder(downloadsDirectory);
            else
                MessageBox.Show("It will be set after a mod has been installed.", "Downloads directory not set.", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void toggleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toggleToolStripMenuItem.Checked = Properties.Settings.Default.Log = log = !toggleToolStripMenuItem.Checked;
            if (activityLog == null)
                EnableLog();
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (activityLog == null)
                return;
            activityLog.DeleteLog();
            if (log)
                EnableLog();
        }

        private void ShowLog()
        {
            string pathToFile = Path.Combine(Tools.LocalAppDataPath, "Activity Log.txt");
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

        private void EnableLog()
        {
            activityLog = new Tools.ActivityLog(Path.Combine(Tools.LocalAppDataPath, "Activity Log.txt"));
            log = true;
        }

        private void btnLog_Click(object sender, EventArgs e)
        {
            ShowLog();
        }
    }
}