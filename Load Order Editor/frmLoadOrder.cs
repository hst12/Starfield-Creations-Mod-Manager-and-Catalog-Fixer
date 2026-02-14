using hstCMM.Common;
using hstCMM.Load_Order_Editor;
using hstCMM.Shared;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Taskbar;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SevenZipExtractor;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using File = System.IO.File;

namespace hstCMM
{
    public partial class frmLoadOrder : Form
    {
        public const byte Steam = 0, MS = 1, Custom = 2, SFSE = 3;
        public static ActivityLog activityLog;
        public static string GamePath, GameName;
        public static bool NoWarn, log;
        public static int returnStatus;
        public int Game = Properties.Settings.Default.Game;
        private readonly List<Tools.GameInfo> gameInfo = new();
        private readonly Tools tools = new();
        private CancellationTokenSource cancellationTokenSource;
        private Rectangle dragBoxFromMouseDown;
        private Tools.Configuration Groups = new();
        private string LastProfile, tempstr;
        private List<string> pluginList;

        private bool Profiles = false, GridSorted = false, AutoUpdate = false, ActiveOnly = false, AutoSort = false, isModified = false,
            LooseFiles, GameExists, devMode = false;

        private int rowIndexFromMouseDown, rowIndexOfItemUnderMouseToDrop, GameVersion = Steam;

        public frmLoadOrder(string parameter)
        {
            InitializeComponent();

            this.KeyPreview = true; // Ensure the form captures key presses
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(KeyEvent); // Handle <enter> for search

            InitLogging(); // Logging

            foreach (var arg in Environment.GetCommandLineArgs()) // Handle some command line arguments
            {
                if (arg.Equals("-noauto", StringComparison.InvariantCultureIgnoreCase))
                {
                    ChangeSettings(false); // Disable auto settings
                    sbar3("Auto Settings Disabled");
                    activityLog.WriteLog("Auto Settings Disabled via command line");
                }

                if (arg.Equals("-reset", StringComparison.InvariantCultureIgnoreCase))
                    ResetPreferences();

                if (arg.Equals("-norestore", StringComparison.InvariantCultureIgnoreCase))
                {
                    Properties.Settings.Default.AutoRestore = false;
                    SaveSettings();
                    sbar3("Auto Restore Disabled");
                    activityLog.WriteLog("Auto Restore Disabled via command line");
                }
            }

            SetupGame();

            // Check catalog
            frmCatalogFixer catalogFixer = new();
            if (Properties.Settings.Default.AutoCheck && GameExists)
            {
                tempstr = catalogFixer.CatalogStatus;
                if (tempstr != null && catalogFixer.CatalogStatus.Contains("Error"))
                    catalogFixer.Show(); // Show catalog fixer if catalog broken
            }

            LooseFilesCheck(); // Check if loose files are enabled
            DetectApps(); // Detect other apps
            SetTheme(); // Light/Dark mode

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
                LogError(ex.Message);
                MessageBox.Show(ex.Message);
            }

            SetUpMenus();
            SetupColumns();

            // Do a 1-time backup of StarfieldCustom.ini if it doesn't exist
            tempstr = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), Path.Combine("My Games", GameName, "StarfieldCustom.ini"));
            if (!File.Exists(tempstr + ".bak") && File.Exists(tempstr))
            {
                sbar2("StarfieldCustom.ini backed up to StarfieldCustom.ini.bak");
                File.Copy(tempstr, tempstr + ".bak");
            }

            if (Properties.Settings.Default.LOOTEnabled) // Cache LOOT groups
                ReadLOOTGroups();

            pluginList = tools.GetPluginList(Game); // Cache plugins

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
            LastProfile ??= Properties.Settings.Default.LastProfile;

            foreach (var arg in Environment.GetCommandLineArgs()) // Handle other command line arguments
            {
                if (arg.StartsWith("-profile", StringComparison.InvariantCultureIgnoreCase))
                {
                    tempstr = Path.Combine(Properties.Settings.Default.ProfileFolder, Environment.GetCommandLineArgs()[2]);
                    //LastProfile = Environment.GetCommandLineArgs()[2];
                    SwitchProfile(tempstr);
                }
                if (arg.StartsWith("-install")) // For future use (maybe) install mod from Nexus web link
                {
                    string strippedCommandLine = Environment.GetCommandLineArgs()[2];

                    InstallMod(strippedCommandLine);
                }
                if (arg.Equals("-dev"))
                {
                    testToolStripMenuItem.Visible = true;
                    gameSelectToolStripMenuItem.Visible = true;
                    devMode = true;
                }
            }

            cmbProfile.Enabled = Profiles;
            if (Profiles)
                GetProfiles();
            else
                InitDataGrid();

            foreach (var arg in Environment.GetCommandLineArgs()) // Handle run command line argument
            {
                if (arg.Equals("-run", StringComparison.InvariantCultureIgnoreCase))
                {
                    RunGame();
                    this.WindowState = FormWindowState.Minimized;
                    Application.Exit();
                }
            }

            // Creations update
            bool BackupStatus = false;
            if (Properties.Settings.Default.CreationsUpdate)
            {
                cretionsUpdateToolStripMenuItem.Checked = false;
                Properties.Settings.Default.CreationsUpdate = false;
                SaveSettings();
                BackupStatus = catalogFixer.BackupCatalog();
                tempstr = BackupStatus ? "Catalog backed up" : "Catalog backup is up to date";
                Properties.Settings.Default.AutoRestore = true;
                MessageBox.Show(tempstr + "\nAuto Restore turned on\n\nYou can now play the game normally until the next time you want to update\n\n" +
                    "Remember to choose the Prepare for Creations Update option again before you update or add new mods", "Creations update complete");
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

                    //InitDataGrid();
                }
            }

            this.Text = tools.AppName() + " - " + GameName + " "; // Show selected game in title bar
        }

        private void LooseFilesCheck()
        {
            string LooseFilesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", GameName),
    filePath = Path.Combine(LooseFilesDir, "StarfieldCustom.ini");
            try
            {
                if (File.Exists(filePath))
                {
                    var StarfieldCustomINI = File.ReadAllLines(filePath);
                    foreach (var lines in StarfieldCustomINI)
                        if (lines.Contains("bInvalidateOlderFiles"))
                        {
                            Properties.Settings.Default.LooseFiles = true;
                            LooseFiles = true;
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private void InitLogging()
        {
            activityLog = new ActivityLog();
            activityLog.LogRichTextBox = rtbLog; // reference the RichTextBox in TableLayoutPanel
            if (Properties.Settings.Default.Log)
            {
                tempstr = Properties.Settings.Default.LogFileDirectory;
                if (tempstr == "")
                    tempstr = Tools.LocalAppDataPath;
                log = true;
                activityLog.LoadLog(Path.Combine(tempstr, "Activity Log.txt"));
                btnLog.Font = new System.Drawing.Font(btnLog.Font, log ? FontStyle.Bold : FontStyle.Regular);
                SetupLogRow();
            }
        }

        public static void SetupJumpList()
        {
            if (!TaskbarManager.IsPlatformSupported)
                return;

            try
            {
                JumpList jumpList = JumpList.CreateJumpList();
                jumpList.KnownCategoryToDisplay = JumpListKnownCategoryType.Recent;

                // Add custom tasks with command-line argument
                JumpListLink runGameTask = new JumpListLink(Application.ExecutablePath, "Run Game")
                {
                    Arguments = "-run",
                    IconReference = new IconReference(Application.ExecutablePath, 0),
                    WorkingDirectory = Path.GetDirectoryName(Application.ExecutablePath),
                    Title = "Run Game"
                };

                JumpListLink devModeTask = new JumpListLink(Application.ExecutablePath, "Dev Mode")
                {
                    Arguments = "-dev",
                    IconReference = new IconReference(Application.ExecutablePath, 0),
                    WorkingDirectory = Path.GetDirectoryName(Application.ExecutablePath),
                    Title = "Dev Mode"
                };

                JumpListLink disableSettings = new JumpListLink(Application.ExecutablePath, "Disable Settings")
                {
                    Arguments = "-noauto",
                    IconReference = new IconReference(Application.ExecutablePath, 0),
                    WorkingDirectory = Path.GetDirectoryName(Application.ExecutablePath),
                    Title = "Disable Settings"
                };

                JumpListLink disableCatalogRestore = new JumpListLink(Application.ExecutablePath, "Disable Catalog Restore")
                {
                    Arguments = "-norestore",
                    IconReference = new IconReference(Application.ExecutablePath, 0),
                    WorkingDirectory = Path.GetDirectoryName(Application.ExecutablePath),
                    Title = "Disable Catalog Restore"
                };

                /*JumpListLink DemoTask = new JumpListLink(Application.ExecutablePath, "Demo Profile")
                {
                    Arguments = "-profile Demo.txt",
                    IconReference = new IconReference(Application.ExecutablePath, 0),
                    WorkingDirectory = Path.GetDirectoryName(Application.ExecutablePath),
                    Title = "Demo Profile"
                };*/

                jumpList.AddUserTasks(runGameTask, devModeTask, disableSettings, disableCatalogRestore);
                jumpList.Refresh();
            }
            catch (Exception ex)
            {
                activityLog.WriteLog("Jump List setup failed: " + ex.Message);
            }
        }

        public void LogError(string message)
        {
            activityLog.WriteLog("ERROR: " + message);
            sbar(message);
        }

        public void ResetPreferences() // Reset user preferences
        {
            string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appPreferencesPath = Path.Combine(localAppDataPath, "hstCMM");

            if (Tools.ConfirmAction("Are you sure you want to reset user preferences?", "This will delete all user settings and preferences",
                MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, true) == DialogResult.No) // Override Nowarn
                return;

            if (Directory.Exists(appPreferencesPath))
            {
                Directory.Delete(appPreferencesPath, true); // true to delete subdirectories and files
                activityLog.WriteLog("User preferences reset successfully.");
                tools.RestartApp("User preferences reset successfully.");
            }
            else
            {
                MessageBox.Show("No preferences found to reset.");
            }
        }

        private static int CheckAndDeleteINI(string FileName)
        {
            string FolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", Tools.GameName);
            if (File.Exists(Path.Combine(FolderPath, FileName)))
            {
                File.Delete(Path.Combine(FolderPath, FileName));
                return 1;
            }
            else
                return 0;
        }

        private static bool CheckGameCustom()
        {
            return Tools.FileCompare(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                     "My Games", GameName, $"{GameName}Custom.ini"), Path.Combine(Tools.CommonFolder, $"{GameName}Custom.ini"));
        }

        private static void CreateZipFromFiles(List<string> files, string zipPath)
        {
            using (FileStream zipToOpen = new FileStream(zipPath, FileMode.Create))
            {
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
                {
                    foreach (string file in files)
                    {
                        ZipArchiveEntry entry = archive.CreateEntryFromFile(file, Path.GetFileName(file), CompressionLevel.Fastest);
                    }
                }
            }
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

        private static bool GameSwitchWarning()
        {
            return (Tools.ConfirmAction("Do you want to proceed?\nAn app restart may be required to enable certain features.",
                "Switching to a no mods profile is suggested before proceeding",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question, true) == DialogResult.Yes);
        }

        private static void SaveSettings()
        {
            Properties.Settings.Default.Save();
        }

        private static void SetColumnVisibility(bool condition, ToolStripMenuItem menuItem, DataGridViewColumn column)
        {
            menuItem.Checked = condition;
            column.Visible = condition;
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Tools.ShowAbout();
        }

        private void activateNewModsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            activateNewModsToolStripMenuItem.Checked = !activateNewModsToolStripMenuItem.Checked;
            Properties.Settings.Default.ActivateNew = activateNewModsToolStripMenuItem.Checked;
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
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                isEnabled = row.Cells["ModEnabled"].Value as bool? ?? false;
                row.Visible = showAll || isEnabled;
            }

            dataGridView1.ResumeLayout();
            sbar4(showAll ? "All mods shown" : "Active mods only");

            if (resizeToolStripMenuItem.Checked)
                ResizeForm();
            btnActiveOnly.Font = new System.Drawing.Font(btnActiveOnly.Font, ActiveOnly ? FontStyle.Bold : FontStyle.Regular);
        }

        private void activeOnlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ActiveOnlyToggle();
        }

        private int AddMissing()
        {
            int addedFiles = 0;
            if (!CheckGamePath() || string.IsNullOrEmpty(GamePath))
                return 0;

            string directory = Path.Combine(GamePath, "Data");
            List<string> pluginFiles = tools.GetPluginList(Game); // Add .esm files

            try
            {
                pluginFiles.AddRange(Directory.EnumerateFiles(directory, "*.esp", SearchOption.TopDirectoryOnly) // Add .esp files
                                              .Select(Path.GetFileName));
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
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
                activityLog.WriteLog($"Adding {file} to Plugins.txt");
            }

            if (addedFiles > 0)
            {
                isModified = true;
                SavePlugins(); // Save changes to Plugins.txt
            }

            sbar4($"Plugins added: {addedFiles}");
            activityLog.WriteLog($"Plugins added: {addedFiles}");
            return addedFiles;
        }

        private void allTheThingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            BackupPlugins();
            BackupBlockedMods(true); // Use Documents folder
            BackupContentCatalog(true);
            BackupLOOTUserlist(true);
            BackupProfiles();
            BackupAppSettings(true);
        }

        private void appAppDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Tools.OpenFolder(Tools.LocalAppDataPath);
        }

        private void ApplySettings(Dictionary<string, object?> imported)
        {
            foreach (var kvp in imported)
            {
                var key = kvp.Key;
                if (Properties.Settings.Default.Properties[key] is null) continue;

                try
                {
                    var targetProp = Properties.Settings.Default.Properties[key];
                    var targetType = targetProp.PropertyType;

                    if (kvp.Value is null)
                    {
                        Properties.Settings.Default[key] = null;
                        continue;
                    }

                    // System.Text.Json deserializes numbers as JsonElement by default when object; handle common cases
                    if (kvp.Value is System.Text.Json.JsonElement je)
                    {
                        object? converted = JsonElementToType(je, targetType);
                        Properties.Settings.Default[key] = converted;
                    }
                    else
                    {
                        // Try direct convert (covers strings, bools, etc.)
                        var converted = Convert.ChangeType(kvp.Value, targetType);
                        Properties.Settings.Default[key] = converted;
                    }
                }
                catch
                {
                    LogError($"Failed to import setting: {key}");
                    // Ignore single-property failures and continue
                }
            }
        }

        private void appSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            BackupAppSettings();
        }

        private void appSettingsToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            RestoreAppSettings();
        }

        private void archiveModToolStripMenuItem_Click_1(object sender, EventArgs e) // Make a zip of a mod and copy it to specified folder
        {
            List<string> files = new();
            if (!CheckGamePath()) // Abort if game path not set
                return;

            string directoryPath = Path.Combine(GamePath, "Data");

            using FolderBrowserDialog folderBrowserDialog = new();
            folderBrowserDialog.Description = "Choose folder to archive the mods to";
            folderBrowserDialog.InitialDirectory = Properties.Settings.Default.BackupDirectory;
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                string selectedFolderPath = folderBrowserDialog.SelectedPath;
                Properties.Settings.Default.BackupDirectory = selectedFolderPath;
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

                    foreach (var textureFile in Directory.EnumerateFiles(directoryPath, ModName + " - textures*.ba2"))
                    {
                        if (File.Exists(textureFile))
                            files.Add(textureFile);
                    }

                    foreach (var mainFile in Directory.EnumerateFiles(directoryPath, ModName + " - main*.ba2"))
                    {
                        if (File.Exists(mainFile))
                            files.Add(mainFile);
                    }

                    if (File.Exists(ModFile + " - voices_en.ba2"))
                        files.Add(ModFile + " - voices_en.ba2");

                    string zipPath = Path.Combine(selectedFolderPath, ModName) + ".zip"; // Choose path to Zip it

                    void makeArchive()
                    {
                        sbar3($"Creating archive for {ModName}...");
                        statusStrip1.Refresh();
                        CreateZipFromFiles(files, zipPath); // Make zip
                        if (log)
                        {
                            foreach (var file in files)
                                activityLog.WriteLog($"Archived {file}");
                        }
                        sbar3($"{ModName} archived");
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

        private void autoResetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!autoResetToolStripMenuItem.Checked)
            {
                DialogResult DialogResult =
                    MessageBox.Show("This will run every time the app is started - Are you sure?",
                    "This will reset settings made by other mod managers.",
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

        private void autoSortToolStripMenuItem_Click(object sender, EventArgs e)
        {
            autoSortToolStripMenuItem.Checked = !autoSortToolStripMenuItem.Checked;
            Properties.Settings.Default.AutoSort = autoSortToolStripMenuItem.Checked;
            AutoSort = Properties.Settings.Default.AutoSort;
        }

        private void autoUpdateModsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AutoUpdate = autoUpdateModsToolStripMenuItem.Checked = !AutoUpdate;
            Properties.Settings.Default.AutoUpdate = AutoUpdate;
        }

        private void BackupAppSettings(bool useDocuments = false)
        {
            string fileName;
            if (!useDocuments)
            {
                using var sfd = new System.Windows.Forms.SaveFileDialog
                {
                    Title = "Export application settings to JSON",
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = "json",
                    FileName = "hstCMMsettings.json",
                    AddExtension = true,
                    OverwritePrompt = true
                };

                if (sfd.ShowDialog(this) != DialogResult.OK) return;
                fileName = sfd.FileName;
            }
            else
                fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "hstCMMsettings.json");

            SaveSettings();
            try
            {
                var dict = GatherSettings();
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = System.Text.Json.JsonSerializer.Serialize(dict, options);
                File.WriteAllText(fileName, json);
                sbar("Settings exported successfully");
                activityLog.WriteLog($"Application settings exported to {fileName}");
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
                MessageBox.Show(this, "Export failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BackupBlockedMods(bool UseDocuments = false)
        {
            string blockedModsFilePath = Path.Combine(Tools.LocalAppDataPath, "BlockedMods.txt");
            BackupFile(blockedModsFilePath, UseDocuments);
        }

        private void BackupContentCatalog(bool useDocuments = false)
        {
            string selectedFolderPath = string.Empty, filePath = Path.Combine(Tools.GameAppData, "ContentCatalog.txt");
            BackupFile(filePath, useDocuments);
        }

        private void backupContentCatalogtxtToolStripMenuItem_Click(object sender, EventArgs e)
        {
            BackupContentCatalog();
        }

        private void BackupFile(string file, bool UseDocuments)
        {
            string destinationPath = string.Empty, selectedFolderPath = string.Empty;
            using FolderBrowserDialog folderBrowserDialog = new();
            folderBrowserDialog.Description = $"Choose folder to use to backup {Path.GetFileName(file)}";
            folderBrowserDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); // Set initial directory to Documents Directory
            if (!UseDocuments)
            {
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    selectedFolderPath = folderBrowserDialog.SelectedPath;
                    destinationPath = Path.Combine(selectedFolderPath, Path.GetFileName(file));
                    if (!File.Exists(file))
                    {
                        MessageBox.Show($"{file} not found", "Source file not found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        LogError($"Source {file} to be backed up not found");
                        return;
                    }
                }
            }
            else
                destinationPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), Path.GetFileName(file));
            try
            {
                if (destinationPath != string.Empty)
                {
                    File.Copy(file, destinationPath, true);
                    sbar($"{file} backed up successfully.");
                    activityLog.WriteLog($"Backup up {file} to {destinationPath}");
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
                MessageBox.Show($"An error occurred while backing up {file}: {ex.Message}", "Backup Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        private void BackupLOOTUserlist(bool UseDocuments = false)
        {
            if (Properties.Settings.Default.LOOTPath == "")
                return;
            string yamlPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                $"LOOT\\games\\{GameName}\\userlist.yaml");
            BackupFile(yamlPath, UseDocuments);
        }

        private void BackupPlugins()
        {
            string sourceFileName = Path.Combine(Tools.GameAppData, "Plugins.txt");
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
                activityLog.WriteLog($"Backup of {Path.GetFileName(sourceFileName)} done to {Path.GetFileName(destFileName)}");
            }
            catch (Exception ex)
            {
                LogError("Backup failed " + ex.Message);
                MessageBox.Show($"Error: {ex.Message}", "Backup failed");
            }
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
            activityLog.WriteLog("Starting profile backup");
            foreach (var item in Directory.EnumerateFiles(Properties.Settings.Default.ProfileFolder, "*.txt", SearchOption.TopDirectoryOnly))
            {
                string fileName = Path.GetFileName(item);
                string destinationPath = Path.Combine(Properties.Settings.Default.ProfileFolder, "Backup", fileName);
                File.Copy(item, destinationPath, true);
                activityLog.WriteLog($"Backed up {item} to backup folder {destinationPath}.");
            }
            sbar("Profiles backed up to Backup folder");
        }

        private void backupProfilesToolStripMenuItem_Click(object sender, EventArgs e) // Backup profiles to Backup folder in Profile folder
        {
            BackupProfiles();
        }

        private void blockedModsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            blockedModsToolStripMenuItem.Checked = !blockedModsToolStripMenuItem.Checked;
            Properties.Settings.Default.BlockedView = blockedModsToolStripMenuItem.Checked;
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

                currentRow.Cells["Blocked"].Value ??= false;

                // Toggle blocked status
                currentRow.Cells["Blocked"].Value = !(bool)(currentRow.Cells["Blocked"].Value);

                if ((bool)currentRow.Cells["Blocked"].Value) // Add mod to blocked list
                {
                    blockedMods.Add(currentRow.Cells["PluginName"].Value.ToString());
                    currentRow.Cells["ModEnabled"].Value = false;
                    sbar2(currentRow.Cells["PluginName"].Value.ToString() + " blocked");
                    activityLog.WriteLog($"Blocked {currentRow.Cells["PluginName"].Value.ToString()}");
                }
                else // Remove mod from blocked list
                {
                    blockedMods.Remove(currentRow.Cells["PluginName"].Value.ToString());
                    sbar2(currentRow.Cells["PluginName"].Value.ToString() + " unblocked");
                    activityLog.WriteLog($"Unblocked {currentRow.Cells["PluginName"].Value.ToString()}");
                }
            }

            // Write to BlockedMods.txt and update isModified flag. No blank lines.
            File.WriteAllLines(Path.Combine(Tools.LocalAppDataPath, "BlockedMods.txt"), blockedMods.Distinct().Where(s => !string.IsNullOrEmpty(s)));
            isModified = true;
            SavePlugins();
        }

        private void btnActiveOnly_Click(object sender, EventArgs e)
        {
            ActiveOnlyToggle();
        }

        private void btnBottom_Click(object sender, EventArgs e)
        {
            MoveBottom();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void btnCatalog_Click(object sender, EventArgs e)
        {
            CheckCatalog();
        }

        private void btnDown_Click(object sender, EventArgs e)
        {
            MoveDown();
        }

        private void btnLog_Click(object sender, EventArgs e)
        {
            string pathToFile = "";
            if (Properties.Settings.Default.SaveLog)
                pathToFile = Path.Combine(string.IsNullOrEmpty(Properties.Settings.Default.LogFileDirectory)
               ? Tools.LocalAppDataPath : Properties.Settings.Default.LogFileDirectory, "Activity Log.txt");
            if (!String.IsNullOrEmpty(pathToFile))
                activityLog.PersistLog(pathToFile);
            ShowLog();
        }

        private void btnLoot_Click(object sender, EventArgs e)
        {
            RunLOOT(true);
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnQuit_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Application.Exit();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            RefreshDataGrid();
            btnGroups.Font = new System.Drawing.Font(btnGroups.Font, FontStyle.Regular);
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            RunGame();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            SavePlugins();
            SaveSettings();
        }

        private void btnTop_Click(object sender, EventArgs e)
        {
            MoveTop();
        }

        private void btnUp_Click(object sender, EventArgs e)
        {
            MoveUp();
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            UpdatePlugins();
        }

        private void ChangeSettings(bool NewSetting)
        {
            var props = Properties.Settings.Default;
            props.ProfileOn = NewSetting;
            Profiles = NewSetting;
            chkProfile.Checked = NewSetting;
            props.AutoSort = NewSetting;
            AutoSort = NewSetting;
            AutoUpdate = props.AutoUpdate;
            props.AutoUpdate = NewSetting;
            props.AutoReset = NewSetting;
            props.AutoDelccc = NewSetting;
            props.CompareProfiles = NewSetting;
            props.ActivateNew = NewSetting;
            props.LOOTEnabled = NewSetting;
            props.ModStats = NewSetting;
            props.RowHighlight = NewSetting;

            SaveSettings();
            SetUpMenus();
        }

        private int CheckArchives()
        {
            List<string> archives = [];
            List<string> plugins = [];
            List<string> toDelete = [];

            if (GamePath == "")
                return 0;

            // Build a list of all plugins excluding base game files
            plugins = pluginList.Select(s => s[..^4].ToLower()).ToList();

            foreach (string file in Directory.EnumerateFiles(Path.Combine(GamePath, "Data"), "*.ba2", SearchOption.TopDirectoryOnly))
            // Build a list of all .ba 2archives
            {
                archives.Add(Path.GetFileName(file).ToLower());
            }

            foreach (string file in Directory.EnumerateFiles(Path.Combine(GamePath, "Data"), "*.bsa", SearchOption.TopDirectoryOnly))
            // Build a list of all .bsa archives
            {
                archives.Add(Path.GetFileName(file).ToLower());
            }

            try
            {
                List<string> modArchives = archives.Except(tools.BGSArchives()) // Exclude BGS Archives
                    .Select(s => s.ToLower().Replace(".ba2", string.Empty)) // Remove ".ba2" from archive names
                    .Select(s => s.ToLower().Replace(".bsa", string.Empty)) // Remove ".bsa" from archive names
                    .ToList();

                foreach (var archive in modArchives) // Check if archive is orphaned
                {
                    if (!plugins.Any(plugin => archive.StartsWith(plugin))) // If no plugin starts with the archive name
                        toDelete.Add(Path.Combine(GamePath, "Data", archive) + ".ba2");
                    if (!plugins.Any(plugin => archive.StartsWith(plugin))) // If no plugin starts with the archive name
                        toDelete.Add(Path.Combine(GamePath, "Data", archive) + ".bsa");
                }

                if (toDelete.Count > 0)
                {
                    activityLog.WriteLog($"Checked for orphaned archives - {toDelete.Count} found");
                    Form Orphaned = new frmOrphaned(toDelete);
                    Orphaned.ShowDialog();
                    return toDelete.Count;
                }
                else
                {
                    sbar3("No orphaned archives found");
                    activityLog.WriteLog("No orphaned archives found");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
                MessageBox.Show("Error: " + ex.Message);
                return 0;
            }
        }

        private void checkArchivesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CheckArchives();
        }

        private string CheckCatalog()
        {
            frmCatalogFixer StarfieldTools = new();
            activityLog.WriteLog("Starting Catalog Checker");
            StarfieldTools.Show();
            sbar4(StarfieldTools.CatalogStatus);
            return StarfieldTools.CatalogStatus;
        }

        private bool CheckGamePath() // Check if game path is set
        {
            if (String.IsNullOrEmpty(GamePath))
                GamePath = tools.SetGamePath(); // Prompt user to set game path if not set
            if (GamePath == "")
            {
                LogError($"Unable to continue without {GameName} game path");
                MessageBox.Show($"Unable to continue without {GameName} game path");

                if (Profiles)
                    ToggleProfiles();
                return false;
            }
            else
                return true;
        }

        private void CheckUnusedUserlistPlugins()
        {
            // Path to userlist.yaml
            string yamlPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                $"LOOT\\games\\{GameName}\\userlist.yaml"
            );

            if (!File.Exists(yamlPath))
            {
                MessageBox.Show("userlist.yaml not found.");
                return;
            }

            // Parse userlist.yaml
            var deserializer = new YamlDotNet.Serialization.DeserializerBuilder().Build();
            var yamlContent = File.ReadAllText(yamlPath);
            var lootConfig = deserializer.Deserialize<Tools.Configuration>(yamlContent);

            // Get plugin names from userlist.yaml
            var yamlPlugins = lootConfig.plugins?.Select(p => p.name).ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>();

            // Get current plugins from game data
            var gamePlugins = new HashSet<string>(pluginList, StringComparer.OrdinalIgnoreCase);

            // Find unused plugins (in userlist.yaml but not in game data)
            var unused = yamlPlugins.Except(gamePlugins).ToList();

            // Report results
            if (unused.Count > 0)
            {
                frmGenericTextList unusedForm = new frmGenericTextList(windowTitle: "Unused Plugins in userlist.yaml", textLines: unused);
                unusedForm.Show();
            }
            else
            {
                MessageBox.Show("All plugins in userlist.yaml are present in game data.", "Check Complete");
            }
        }

        private void chkProfile_CheckedChanged(object sender, EventArgs e)
        {
            Profiles = chkProfile.Checked;
            cmbProfile.Enabled = chkProfile.Checked;
        }

        private void cmbProfile_SelectedIndexChanged(object sender, EventArgs e)
        {
            SwitchProfile(Path.Combine(Properties.Settings.Default.ProfileFolder, (string)cmbProfile.SelectedItem));
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

        private void compareStarfieldCustominiToBackupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            sbar3(CheckGameCustom() ? "Same" : "Modified");
        }

        private void configurationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmCacheConfig frmCacheConfig = new frmCacheConfig();
            frmCacheConfig.Show();
        }

        private void ConvertLooseFiles(string esm = "")
        {
            activityLog.WriteLog("Converting loose files to archive(s)");
            returnStatus = 0;
            /*try
            {*/
            frmConvertLooseFiles frmCLF = new frmConvertLooseFiles(esm);
            frmCLF.StartPosition = FormStartPosition.CenterScreen;
            frmCLF.ShowDialog();
            /*}
            catch (Exception ex)
            {
                LogError(ex.Message);
                //MessageBox.Show($"Error converting loose files. {ex.Message}.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }*/

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

        private void creationKitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            const string UserRoot = "HKEY_CURRENT_USER";
            const string Subkey = @"Software\Valve\Steam";
            const string KeyName = UserRoot + "\\" + Subkey;

            string executable = GamePath;
            if (executable != null)
            {
                try
                {
                    SaveSettings();
                    string stringValue = (string)Registry.GetValue(KeyName, "SteamExe", ""); // Get Steam path from Registry
                    var processInfo = new ProcessStartInfo(stringValue, $"-applaunch {Tools.GameLibrary.GetById(Properties.Settings.Default.Game).CKId}");
                    var process = Process.Start(processInfo);
                    activityLog.WriteLog("Starting Creation Kit");
                    System.Windows.Forms.Application.Exit();
                }
                catch (Exception ex)
                {
                    LogError(ex.Message);
                    MessageBox.Show(ex.Message);
                }
            }
            else
            {
                MessageBox.Show($"{GameName} path not set");
            }
        }

        private void darkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            darkToolStripMenuItem.Checked = !darkToolStripMenuItem.Checked;
            dataGridView1.EnableHeadersVisualStyles = false;
            Properties.Settings.Default.DarkMode = 1;
            lightToolStripMenuItem.Checked = false;
            systemToolStripMenuItem.Checked = false;
            SetTheme();
            SetThemeMessage();
        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            EnableDisable();
        }

        private void dataGridView1_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            sbar(e.Exception.Message);
        }

        private void dataGridView1_DragDrop(object sender, DragEventArgs e)
        {
            int ModCounter = 0;

            if (e.Data.GetDataPresent(DataFormats.FileDrop)) // Install mod files on drag drop
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var item in files)
                {
                    ModCounter++;
                    if (!InstallMod(item))
                        break;
                    isModified = true;
                    //SavePlugins();
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
                activityLog.WriteLog($"Row moved: {rowToMove.Cells["PluginName"].Value}");
                SavePlugins();
            }
        }

        private void dataGridView1_DragEnter(object sender, DragEventArgs e) // Handle drag and drop of files into the DataGridView
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void dataGridView1_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
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
                    MessageBox.Show("F12 pressed, operation cancelled");
                    break;
            }
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
                    LogError(ex.Message);
                }
            }
        }

        private void dataGridView1_Sorted(object sender, EventArgs e)
        {
            sbar("Plugins sorted - saving changes disabled - Refresh to enable saving");
            toolStripStatusStats.ForeColor = System.Drawing.Color.Red;
            btnSave.Enabled = false;
            saveToolStripMenuItem.Enabled = false;
            GridSorted = true;
        }

        private bool Delccc(bool noErrorLog=false) // true to log delete failed 
        {
            try
            {
                string Starfieldccc = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", GameName, $"{GameName}.ccc");
                if (File.Exists(Starfieldccc))
                {
                    File.Delete(Starfieldccc);
                    sbar3($"{GameName}.ccc deleted");
                    activityLog.WriteLog($"GameName.ccc deleted");
                    return true;
                }
                else
                {
                    sbar3($"{GameName}.ccc not found");
                    if (noErrorLog)
                        activityLog.WriteLog($"{GameName}.ccc not found");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError("Error deleting .ccc file " + ex.Message);
                return false;
            }
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
                LogError(ex.Message);
                MessageBox.Show("Error deleting BlockedMods.txt: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void deleteContentCatalogtxtToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Tools.ConfirmAction("Are you sure you want to delete ContentCatalog.txt?",
                "This will delete ContentCatalog.txt", MessageBoxButtons.YesNo) == DialogResult.No)
                return;
            File.Delete(Path.Combine(Tools.GameAppData, "ContentCatalog.txt"));
        }

        private void DeleteLines()
        {
            foreach (DataGridViewRow row in dataGridView1.SelectedRows)
            {
                // Check if the row is not a new row
                if (!row.IsNewRow)
                {
                    activityLog.WriteLog($"Deleting {row.Cells["PluginName"].Value} from Plugins.txt");
                    dataGridView1.Rows.Remove(row);
                }
            }

            isModified = true;
        }

        private void deleteLooseFileFoldersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            sbar3($"Folders Deleted: {DeleteLooseFileFolders().ToString()}");
        }

        private void deleteStarfieldCustominiToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var GameCustomINIPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "My Games", GameName, $"{GameName}Custom.ini");
                if (File.Exists(GameCustomINIPath))
                {
                    File.Delete(GameCustomINIPath);
                    sbar3($"{GameName}Custom.ini deleted");
                }
                else
                    sbar3($"{GameName}Custom.ini not found");
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
                MessageBox.Show(ex.Message);
            }
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e) // Log Delete
        {
            if (activityLog is null)
                return;
            activityLog.DeleteLog();
            activityLog.Dispose();
            rtbLog.Clear();
            if (log)
                EnableLog();
        }

        private void DetectApps()
        {
            /*if (!File.Exists(Path.Combine(GamePath, "CreationKit.exe"))) // Hide option to launch CK if not found
                creationKitToolStripMenuItem.Visible = false;*/

            if (!File.Exists(Path.Combine(GamePath, "sfse_loader.exe")))
            {
                gameVersionSFSEToolStripMenuItem.Visible = false;
                sFSEPluginsEnableDisableToolStripMenuItem.Visible = false;
            }
            GameVersionDisplay();

            if (Properties.Settings.Default.MO2Path == "")
                mO2ToolStripMenuItem.Visible = false;

            if (Properties.Settings.Default.xEditPath == "")
                xEditToolStripMenuItem.Visible = false;

            if (string.IsNullOrEmpty(Properties.Settings.Default.LOOTPath) &&
                File.Exists(@"C:\Program Files\LOOT\LOOT.exe")) // Try to detect LOOT if installed in default location
                Properties.Settings.Default.LOOTPath = @"C:\Program Files\LOOT\LOOT.exe";

            // Hide menu items if LOOTPath is unset
            bool lootPathIsEmpty = string.IsNullOrEmpty(Properties.Settings.Default.LOOTPath);
            toolStripMenuLOOTToggle.Visible = !lootPathIsEmpty;
            autoSortToolStripMenuItem.Visible = !lootPathIsEmpty;
            toolStripMenuLoot.Visible = !lootPathIsEmpty;
            toolStripMenuLootSort.Visible = !lootPathIsEmpty;

            // Unhide Star UI Configurator menu if found
            if (!File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", GameName, "Data",
                "StarUI Configurator.bat")))
                starUIConfiguratorToolStripMenuItem.Visible = false;
        }

        private void directoriesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmOptions fd = new();
            fd.Show();
            if (returnStatus == 1)
            {
                SetUpMenus();
            }
        }

        private void DisableAll()
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
                row.Cells["ModEnabled"].Value = false;

            sbar2("All mods disabled");
            isModified = true;
            SavePlugins();
            activityLog.WriteLog("All mods disabled");
        }

        private void disableAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ActiveOnly)
                ActiveOnlyToggle();
            ChangeSettings(false);
            disableAllWarnings();
            activityLog.WriteLog("Disabling all settings");
            sbar5("");
        }

        private void disableAllWarnings()
        {
            disableAllWarningToolStripMenuItem.Checked = Properties.Settings.Default.NoWarn = NoWarn = false;
            activityLog.WriteLog("Disable all warnings set to " + NoWarn.ToString());
        }

        private void disableAllWarningToolStripMenuItem_Click(object sender, EventArgs e)
        {
            disableAllWarningToolStripMenuItem.Checked = !disableAllWarningToolStripMenuItem.Checked;
            Properties.Settings.Default.NoWarn = disableAllWarningToolStripMenuItem.Checked;
            NoWarn = disableAllWarningToolStripMenuItem.Checked;
            activityLog.WriteLog("Disable all warnings set to " + NoWarn.ToString());
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
            btnLog.Font = new System.Drawing.Font(btnLog.Font, log ? FontStyle.Bold : FontStyle.Regular);
        }

        private void displayAllSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<string> settingsList = new();

            settingsList.Add("Application Settings:");

            var settings = Properties.Settings.Default;
            var props = settings.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach (var prop in props)
            {
                Debug.WriteLine($"Property: {prop.Name}, Type: {prop.PropertyType}");
                try
                {
                    var value = prop.GetValue(settings, null);
                    settingsList.Add($"{prop.Name}: {value}");
                }
                catch
                {
                    settingsList.Add($"{prop.Name}: <error reading value>");
                }
            }

            frmGenericTextList das = new("Application Settings", settingsList);
            das.Show();
        }

        private void documentationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Tools.OpenUrl(@"Documentation\Index.htm");
        }

        private void downloadsToolStripMenuItem_Click(object sender, EventArgs e) // Open Downloads Directory
        {
            string downloadsDirectory = Properties.Settings.Default.DownloadsDirectory;
            if (!string.IsNullOrEmpty(downloadsDirectory))
                Tools.OpenFolder(downloadsDirectory);
            else
                MessageBox.Show("It will be set after a mod has been installed.", "Downloads directory not set.",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void editBlockedModstxtToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Tools.OpenFile(Path.Combine(Tools.LocalAppDataPath, "BlockedMods.txt"));
            MessageBox.Show("Click OK to refresh");
            isModified = true;
            RefreshDataGrid();
        }

        private void editContentCatalogtxtToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Tools.OpenFile(Tools.GetCatalogPath());
        }

        private void editLOOTUserlistyamlToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string LOOTPath = Properties.Settings.Default.LOOTPath;
            if (string.IsNullOrEmpty(LOOTPath)) // Check if LOOT path is set
                return;
            CheckUnusedUserlistPlugins();
            Tools.OpenFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Path.Combine("LOOT", "games", GameName, "Userlist.yaml")));
            MessageBox.Show("Click OK to refresh");
            ReadLOOTGroups();
            RefreshDataGrid();
        }

        private void editStarfieldCustominiToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Tools.OpenFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "My Games", GameName, $"{GameName}Custom.ini"));
        }

        private void enableAchievementSafeOnlyToolStripMenuItem_Click(object sender, EventArgs e) // Experimental. Should probably remove
        {
            if (Tools.ConfirmAction("Do you want to continue",
                "Warning - this will alter your current load order to achievement friendly mods only",
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
            activityLog.WriteLog("Achievement friendly mods enabled");
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
            activityLog.WriteLog("All mods enabled");
        }

        private void enableAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Tools.ConfirmAction("Enable All settings?", "This will turn on a most of the Tools menu settings and reset ini settings",
                MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation) == DialogResult.OK)
            {
                ChangeSettings(true);
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
            RefreshDataGrid();
        }

        private void EnableDisable()
        {
            if (GridSorted)
                return;

            if (Tools.BlockedMods().Contains((string)dataGridView1.CurrentRow.Cells["PluginName"].Value))
            {
                sbar("Mod is blocked");
                return;
            }
            isModified = true;
            foreach (var row in dataGridView1.SelectedRows)
            {
                DataGridViewRow currentRow = (DataGridViewRow)row;
                currentRow.Cells["ModEnabled"].Value = !(bool)(currentRow.Cells["ModEnabled"].Value);
            }

            activityLog.WriteLog($"Enable/Disable mod: {dataGridView1.CurrentRow.Cells["PluginName"].Value}," +
                $" {dataGridView1.CurrentRow.Cells["ModEnabled"].Value}");
            SavePlugins();
        }

        private void EnableLog()
        {
            tempstr = Properties.Settings.Default.LogFileDirectory;
            if (tempstr == "")
                tempstr = Tools.LocalAppDataPath;
            activityLog = new();
            log = true;
            activityLog.LogRichTextBox = rtbLog; // reference the RichTextBox in TableLayoutPanel
        }

        private void enableSplashScreenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableSplashScreenToolStripMenuItem.Checked = Properties.Settings.Default.LoadScreenEnabled = !enableSplashScreenToolStripMenuItem.Checked;
            SaveSettings();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveSettings();
            System.Windows.Forms.Application.Exit();
        }

        private void frmLoadOrder_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveWindowSettings();

            if (isModified)
                SavePlugins();
            SaveSettings();
            string pathToFile = string.Empty;
            if (log)
            {
                activityLog.WriteLog("Shutting down");
                if (Properties.Settings.Default.SaveLog)
                    pathToFile = Path.Combine(string.IsNullOrEmpty(Properties.Settings.Default.LogFileDirectory)
                   ? Tools.LocalAppDataPath : Properties.Settings.Default.LogFileDirectory, "Activity Log.txt");
                if (!String.IsNullOrEmpty(pathToFile))
                    activityLog.PersistLog(pathToFile);
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
                    LogError(ex.Message);
                }
            }
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

            int minWidth = 1840; // Set minimum width
            int minHeight = 800; // Set minimum height

            if (this.Width < minWidth || this.Height < minHeight)
            {
                this.Size = new System.Drawing.Size(minWidth, minHeight);
                this.Location = new Point
                (
                    (Screen.PrimaryScreen.WorkingArea.Width - this.Width) / 2,
                     (Screen.PrimaryScreen.WorkingArea.Height - this.Height) / 2
                );
            }

            progressBar1.Width = 400; // Set the width of the progress bar
            progressBar1.Height = 50; // Set the height of the progress bar
            progressBar1.Location = new Point((this.ClientSize.Width - progressBar1.Width) / 2, (this.ClientSize.Height - progressBar1.Height) / 2);
        }

        private void frmLoadOrder_Resize(object sender, EventArgs e)
        {
            progressBar1.Location = new Point((this.ClientSize.Width - progressBar1.Width) / 2, (this.ClientSize.Height - progressBar1.Height) / 2);
        }

        private void frmLoadOrder_Shown(object sender, EventArgs e)
        {
            SetupJumpList();
            if (Properties.Settings.Default.Resize)
                ResizeForm();
        }

        private void gameSelectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool profilesEnabled = Profiles;
            if (Profiles)
                ToggleProfiles();
            frmGameSelect gameSelectForm = new frmGameSelect();
            gameSelectForm.StartPosition = FormStartPosition.CenterScreen;
            returnStatus = 0;
            gameSelectForm.ShowDialog();
            if (returnStatus == 0)
            {
                MessageBox.Show("App will restart. Profiles and Catalog Auto-Restore Disabled", "Restart Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                if (GameVersion != MS)
                {
                    if (GamePath == "")
                        GamePath = tools.SetGamePath();
                }
                else
                {
                    if (Properties.Settings.Default.GamePathMS == "")
                        tools.SetGamePathMS();
                    GamePath = Properties.Settings.Default.GamePathMS;
                }
                Properties.Settings.Default.AutoRestore = false;
                SaveSettings();

                tools.RestartApp("Switching Game");
            }
            else
            {
                if (profilesEnabled)
                    ToggleProfiles();
            }
        }

        private void GameVersionDisplay()
        {
            string version = GameVersion switch
            {
                Steam => "Steam",
                MS => "MS",
                Custom => $"Custom - {Properties.Settings.Default.CustomEXE}",
                SFSE => "SFSE",
                _ => "Unknown game version"
            };

            sbar2(version);
        }

        private void gameVersionSFSEToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (GameVersion == MS)
                if (GameSwitchWarning())
                    return;

            if (File.Exists(Path.Combine(GamePath, "sfse_loader.exe")))
            {
                gameVersionSFSEToolStripMenuItem.Checked = true;
                toolStripMenuSteam.Checked = false;
                toolStripMenuMS.Checked = false;
                toolStripMenuCustom.Checked = false;
                GameVersion = SFSE;
                GamePath = Properties.Settings.Default.GamePath;
                UpdateGameVersion();
            }
            else
            {
                MessageBox.Show($"SFSE doesn't seem to be installed or {GameName} path not set", "Unable to switch to SFSE",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                GameVersion = Steam;
                toolStripMenuSteam.Checked = true;
                toolStripMenuMS.Checked = false;
                toolStripMenuCustom.Checked = false;
                gameVersionSFSEToolStripMenuItem.Checked = false;
            }
        }

        private Dictionary<string, object?> GatherSettings()
        {
            var result = new Dictionary<string, object?>();

            // Iterate properties defined in the Settings designer
            foreach (SettingsProperty prop in Properties.Settings.Default.Properties)
            {
                // Only export user-scoped writable settings
                if (prop.Attributes.Contains(typeof(UserScopedSettingAttribute)))
                {
                    var name = prop.Name;
                    try
                    {
                        var value = Properties.Settings.Default[name];
                        result[name] = value;
                    }
                    catch
                    {
                        // If a property fails to read, include a null placeholder
                        result[name] = null;
                    }
                }
            }

            return result;
        }

        private void generateBGSArchivestxtToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<string> plugins = tools.GetPluginList(Game).Select(p => Path.GetFileNameWithoutExtension(p)).ToList();
            List<string> ba2Archives = Directory.EnumerateFiles(Path.Combine(GamePath, "Data"), "*.ba2").Select(p => Path.GetFileNameWithoutExtension(p)).ToList();
            List<string> bsaArchives = Directory.EnumerateFiles(Path.Combine(GamePath, "Data"), "*.bsa").Select(p => Path.GetFileNameWithoutExtension(p)).ToList();
            List<string> bgsArchives = new List<string>();
            List<string> allArchives = ba2Archives.Concat(bsaArchives).ToList();
            string GameFolder = Tools.GameLibrary.GetById(Properties.Settings.Default.Game).ExcludeFile;

            foreach (string fileName in allArchives)
            {
                foreach (string plugin in plugins)
                {
                    if (fileName.StartsWith(plugin))
                        bgsArchives.Add(fileName);
                    //break; // Found a match, skip to next fileName
                }
            }
            bgsArchives = allArchives.Except(bgsArchives).ToList();
            List<string> modifiedLines;
            if (Game == 0)
                modifiedLines = new(bgsArchives.Select(line => line + ".ba2"));
            else
                modifiedLines = new(bgsArchives.Select(line => line + ".bsa"));

            System.Windows.Forms.SaveFileDialog saveDialog = new()
            {
                InitialDirectory = Tools.CommonFolder,
                Filter = "Txt File|*.txt",
                Title = "Create Game Archives.txt",
                //FileName = GameFolder + " Archives.txt";
                FileName = Tools.GameLibrary.GetById(Properties.Settings.Default.Game).ExcludeFile + " Archives.txt"
            };

            if (saveDialog.ShowDialog() != DialogResult.OK || string.IsNullOrEmpty(saveDialog.FileName))
                return;

            File.WriteAllLines(saveDialog.FileName, modifiedLines);
        }

        private void generateExcludeFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(GamePath) || !Directory.Exists(GamePath))
            {
                MessageBox.Show("Game path not set or invalid.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string[] patterns = { "*.esm", "*.esl", "*.esp" }; // Build a list of mod files
            List<string> excludefiles = new();
            foreach (var pattern in patterns)
            {
                var modFiles = Directory.EnumerateFiles(Path.Combine(GamePath, "Data"), pattern, SearchOption.TopDirectoryOnly);
                foreach (var modFile in modFiles)
                {
                    excludefiles.Add(Path.GetFileName(modFile));
                }
            }
            foreach (var row in dataGridView1.Rows) // Remove installed mods from the list
            {
                excludefiles.Remove(((DataGridViewRow)row).Cells["PluginName"].Value.ToString());
            }

            System.Windows.Forms.SaveFileDialog saveDialog = new()
            {
                InitialDirectory = Tools.CommonFolder,
                Filter = "Txt File|*.txt",
                Title = "Create Game Exclude File",
                FileName = Tools.CommonFolder + "\\" + (Tools.GameLibrary.GetById(Properties.Settings.Default.Game).ExcludeFile + " Exclude.txt")
            };

            if (saveDialog.ShowDialog() != DialogResult.OK || string.IsNullOrEmpty(saveDialog.FileName))
                return;

            using (StreamWriter writer = new StreamWriter(saveDialog.FileName))
            {
                foreach (var item in excludefiles)
                {
                    writer.WriteLine(Path.GetFileName(item));
                    Debug.WriteLine(Path.GetFileName(item));
                }
            }
        }

        private void generateTestPluginstxtToolStripMenuItem_Click(object sender, EventArgs e)
        {
            const int Count = 5000;
            const int MinLength = 8;
            const int MaxLength = 16;
            var rand = new Random();
            var set = new HashSet<string>();
            var chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            using var writer = new StreamWriter(Path.Combine(Properties.Settings.Default.ProfileFolder, "unique_plugins.txt"));

            while (set.Count < Count)
            {
                int len = rand.Next(MinLength, MaxLength + 1);
                var arr = new char[len];
                for (int i = 0; i < len; i++)
                    arr[i] = chars[rand.Next(chars.Length)];
                var entry = new string(arr) + ".esm";
                if (set.Add(entry))
                    writer.WriteLine(entry);
            }
            sbar("unique_plugins.txt created.");
        }

        private void GetProfiles()
        {
            string ProfileFolder;
            if (!Profiles)
                return;
            cmbProfile.Items.Clear();
            ProfileFolder = Properties.Settings.Default.ProfileFolder;
            ProfileFolder = string.IsNullOrEmpty(ProfileFolder)
        ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) : ProfileFolder;

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
                LogError(ex.Message);
            }
        }

        private void githubLatestReleaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Tools.OpenUrl("https://github.com/hst12/Starfield-Creations-Mod-Manager-and-Catalog-Fixer/releases/latest");
        }

        private void HideAllColumns()
        {
            ShowHideColumns(false);
        }

        private void HideLogRow()
        {
            rtbLog.Visible = false;
            rtbLog.Dock = DockStyle.None;
            rtbLog.Height = 0;
            tableLayoutPanel1.RowStyles[1].SizeType = SizeType.Percent; // Set datagrid display height
            tableLayoutPanel1.RowStyles[1].Height = 100f;
            tableLayoutPanel1.RowStyles[2].SizeType = SizeType.AutoSize; // Set log display height
            ResizeForm();
        }

        private void InitDataGrid()
        {
            int enabledCount = 0, IndexCount = 1, i, versionDelimiter, dotIndex,
                webskipchars = Tools.GameLibrary.GetById(Properties.Settings.Default.Game).WebSkipChars;

            long totalFileSize = 0;

            string loText = Path.Combine(Tools.GameAppData, "Plugins.txt"),
                   LOOTPath = Properties.Settings.Default.LOOTPath, pluginName, rawVersion;

            List<string> CreationsPlugin = [], CreationsTitle = [], CreationsFiles = [], CreationsVersion = new();
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
            bool rowHighlight = Properties.Settings.Default.RowHighlight;
            bool modEnabled;
            string json = "";
            System.Drawing.Color rowColour = System.Drawing.Color.Empty;

            var colorMode = Properties.Settings.Default.DarkMode switch
            {
                0 => SystemColorMode.Classic,
                1 => SystemColorMode.Dark,
                2 => SystemColorMode.System,
                _ => SystemColorMode.Classic // Default fallback
            };

            if (File.Exists(Tools.GetCatalogPath()))
                json = File.ReadAllText(Tools.GetCatalogPath()); // Read Catalog
            var bethFilesSet = new HashSet<string>(tools.BethFiles); // Read files to exclude
            string[] lines;
            if (File.Exists(loText))
                lines = File.ReadAllLines(loText); // Read Plugins.txt
            else
            {
                sbar("Plugins.txt not found");
                return;
            }

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
                        file.EndsWith(".esp", StringComparison.OrdinalIgnoreCase) ||
                        file.EndsWith(".esl", StringComparison.OrdinalIgnoreCase)));

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
                LogError(ex.Message);
            }

            // Pre-build a dictionary for quick lookup from plugin name (.esm and .esp) to index
            var creationLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (i = 0; i < CreationsPlugin.Count; i++)
            {
                dotIndex = CreationsPlugin[i].LastIndexOf('.');
                if (dotIndex > 0)
                {
                    string baseName = CreationsPlugin[i][..dotIndex];
                    creationLookup[baseName + ".esm"] = i;
                    creationLookup[baseName + ".esp"] = i;
                    creationLookup[baseName + ".esl"] = i;
                }
            }

            progressBar1.Maximum = lines.Length;
            progressBar1.Value = 0;
            progressBar1.Show();

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
                                    modVersion = start.AddSeconds(seconds).Date.ToString("yyyy-MM-dd");
                                else // Handle failed parsing
                                    modVersion = "Invalid version format";
                            }
                            else // Handle unexpected delimiter position
                                modVersion = "Delimiter out of bounds";
                        }
                        catch (Exception ex) // Log or handle unexpected exceptions
                        {
                            modVersion = $"Error: {ex.Message}";
                            LogError(ex.Message);
                        }
                    }
                    modFiles = CreationsFiles[idx];
                    aSafe = AchievementSafe[idx] ? "Yes" : "";
                    modTimeStamp = Tools.ConvertTime(TimeStamp[idx]).ToString();
                    modID = CreationsID[idx];
                    modFileSize = FileSize[idx] / 1024;
                    if (modEnabled)
                        totalFileSize += modFileSize;
                    url = $"https://creations.bethesda.net/en/{Tools.GameLibrary.GetById(Properties.Settings.Default.Game).
                        CreationsSite}/details/{(modID.Length > 3 ? modID[webskipchars..] : modID)}/" +
                        CreationsTitle[idx].Replace(" ", "_").Replace("[", "_").Replace("]", "_");
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
                    row.Cells[13].Value = true; // Blocked column
                }

                enabledCount += modEnabled ? 1 : 0;

                /* Disabled for now.Interferes with group filter
                // Special handling for Bethesda Game Studio mods.
                if (pluginName.StartsWith("sfbgs", StringComparison.OrdinalIgnoreCase))
                {
                    string currentGroupX = row.Cells[4].Value?.ToString() ?? "Bethesda Game Studios Creations"; //Group = column 4
                    row.Cells[4].Value = $"{currentGroupX} (Bethesda)";
                }*/

                // Update required cells.
                row.Cells[1].Value = modEnabled; // Enabled = column 1
                row.Cells[2].Value = pluginName; // PluginName = column 2
                row.Cells[10].Value = modID; // CreationsID = column 10
                row.Cells[12].Value = url; // URL = column 12

                // Update additional columns if visible
                var columnUpdates = new Dictionary<int, (bool isVisible, object value)>
                {
                    [0] = (isIndexVisible, IndexCount++),
                    [3] = (isDescriptionVisible, description),
                    [5] = (isVersionVisible, modVersion),
                    [6] = (isAuthorVersionVisible, authorVersion),
                    [7] = (isTimeStampVisible, modTimeStamp),
                    [8] = (isAchievementsVisible, aSafe),
                    [9] = (isFilesVisible, modFiles),
                    [11] = (isFileSizeVisible, modFileSize != 0 ? modFileSize : null)
                };

                foreach (var kvp in columnUpdates)
                {
                    if (kvp.Value.isVisible)
                        row.Cells[kvp.Key].Value = kvp.Value.value;
                }

                rowBuffer.Add(row);

                if (colorMode == SystemColorMode.Dark ||
                    (colorMode == SystemColorMode.System && System.Windows.Forms.Application.SystemColorMode == SystemColorMode.Dark))
                    rowColour = System.Drawing.Color.SlateGray;
                else
                    rowColour = System.Drawing.Color.AntiqueWhite;
                if (row.Cells[1].Value is bool enabled && !enabled && rowHighlight) // Highlight rows
                    row.DefaultCellStyle.BackColor = rowColour;
                /*else
                    row.DefaultCellStyle.BackColor = System.Drawing.Color.White;*/
            } // End of main loop

            dataGridView1.Rows.AddRange(rowBuffer.ToArray());

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

            progressBar1.Value = progressBar1.Maximum;
            progressBar1.Hide();

            dataGridView1.ResumeLayout(); // Resume layout
            dataGridView1.EndEdit();

            if (Properties.Settings.Default.Resize)
                ResizeForm();

            // -- Process mod stats if the game path is set --
            if (!string.IsNullOrEmpty(GamePath) && Properties.Settings.Default.ModStats)
                sbar(ShowModStats(CreationsPlugin, enabledCount, totalFileSize));
            else
                sbar("");
        }

        private bool InstallMod(string InstallMod = "") // false for cancel
        {
            string esmFile = "";
            string extractPath = Path.Combine(Path.GetTempPath(), "hstCMM");
            bool SFSEMod = false, looseFileMod = false;
            int filesInstalled = 0;

            if (!CheckGamePath()) // Bail out if game path not set
                return false;

            // Clean the extract directory if it exists.
            if (Directory.Exists(extractPath))
            {
                try { Directory.Delete(extractPath, true); }
                catch (Exception ex)
                {
                    LogError("Unable to clear extract directory " + ex.Message);
                }
            }

            // Obtain the mod file path either from the parameter or by showing a file dialog.
            string modFilePath = InstallMod;
            if (string.IsNullOrEmpty(modFilePath))
            {
                using (System.Windows.Forms.OpenFileDialog openMod = new System.Windows.Forms.OpenFileDialog
                {
                    InitialDirectory = Properties.Settings.Default.DownloadsDirectory,
                    Filter = "Archive Files (*.zip;*.7z;*.rar)|*.zip;*.7z;*.rar|All Files (*.*)|*.*",
                    Title = "Install Mod - Loose files partially supported"
                })
                {
                    if (openMod.ShowDialog() == DialogResult.OK)
                        modFilePath = openMod.FileName;
                }
            }

            if (string.IsNullOrEmpty(modFilePath))
                return false;

            string[] modFileTypes = { ".esp", ".esm", ".esl", ".ba2", ".bsa" }; // Install unzipped files directly

            if (modFileTypes.Contains(Path.GetExtension(modFilePath), StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    if (File.Exists(Path.Combine(GamePath, "Data", Path.GetFileName(modFilePath))))
                    {
                        if (!NoWarn)
                            if (Tools.ConfirmAction($"Overwrite existing file {Path.GetFileName(modFilePath)} in Data folder?",
                                "File exists", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                                return false;
                    }
                    File.Copy(modFilePath, Path.Combine(GamePath, "Data", Path.GetFileName(modFilePath)), true);
                    sbar2($"Copied {Path.GetFileName(modFilePath)} to Data folder");
                    activityLog.WriteLog($"Copied {modFilePath} to {Path.Combine(GamePath, "Data", Path.GetFileName(modFilePath))}");
                }
                catch (Exception ex)
                {
                    LogError(ex.Message);
                    MessageBox.Show(ex.Message);
                    return false;
                }
                if (modFilePath.Contains(".esm"))
                    UpdatePlugins();
                return true;
            }

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
                    activityLog.WriteLog($"Extracting: {modFilePath}");
                    try
                    {
                        archiveFile.Extract(extractPath, password: txtSearchBox.Text);
                    }
                    catch (Exception ex)
                    {
                        LogError("Extraction failed: " + ex.Message);
                        MessageBox.Show("Extraction failed: " + ex.Message);
                        loadScreen.Close();
                        return false;
                    }

                    if (Directory.Exists(Path.Combine(extractPath, "fomod")))
                    {
                        if (Tools.ConfirmAction("Attempt installation anyway?", "Fomod detected - mod will probably not install correctly",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question) != DialogResult.Yes)
                        {
                            if (Directory.Exists(extractPath))
                                Directory.Delete(extractPath, true);
                            loadScreen.Close();
                            return false;
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
                                    using ArchiveFile archiveFile2 = new ArchiveFile(file);
                                    sbar2($"Extracting embedded archive: {file}");
                                    statusStrip1.Refresh();
                                    archiveFile2.Extract(extractPath);
                                    activityLog.WriteLog($"Extracting embedded archive: {file}");
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
                LogError(ex.Message);
                MessageBox.Show(ex.Message);
                loadScreen.Close();
                return false;
            }

            if (Directory.EnumerateFiles(extractPath, "*.esm", SearchOption.AllDirectories).Any())
                esmFile = Directory.GetFiles(extractPath, "*.esm", SearchOption.AllDirectories).FirstOrDefault();
            if (Directory.EnumerateFiles(extractPath, "*.esp", SearchOption.AllDirectories).Any())
                esmFile = Directory.GetFiles(extractPath, "*.esp", SearchOption.AllDirectories).FirstOrDefault();
            if (Directory.EnumerateFiles(extractPath, "*.esl", SearchOption.AllDirectories).Any())
                esmFile = Directory.GetFiles(extractPath, "*.esl", SearchOption.AllDirectories).FirstOrDefault();

            // Move .esm and .ba2 files to the game's Data folder.
            try
            {
                filesInstalled += MoveExtractedFiles("*.esm", "esm");
                filesInstalled += MoveExtractedFiles("*.esp", "esp");
                filesInstalled += MoveExtractedFiles("*.ba2", "archive");
                filesInstalled += MoveExtractedFiles("*.bsa", "archive");
                filesInstalled += MoveExtractedFiles("*.esl", "esl");
            }
            catch (OperationCanceledException)
            {
                sbar("Mod installation cancelled by user");
                activityLog.WriteLog("Mod installation cancelled by user");
                return false;
            }

            // Install SFSE plugin if found.
            try
            {
                string[] sfseDirs = Directory.GetDirectories(extractPath, "SFSE", SearchOption.AllDirectories);
                if (sfseDirs.Length > 0)
                    SFSEMod = true;

                foreach (string dir in sfseDirs)
                {
                    tempstr = Path.Combine(GamePath, "Data", "SFSE");
                    CopyDirectory(dir, tempstr);
                    filesInstalled++;
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
                MessageBox.Show($"An error occurred: {ex.Message}");
            }

            // Install Loose files
            List<string> looseFileDirs = Tools.LooseFolderDirsOnly; // Get the list of loose file directories from the Tools class

            // Define the target directory
            var targetDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), Path.Combine("My Games", GameName, "Data"));

            // Ensure the target directory exists
            Directory.CreateDirectory(targetDir);

            // Recursively search for each directory and copy its contents
            foreach (var dirName in looseFileDirs)
            {
                var directoriesFound = Directory.GetDirectories(extractPath, dirName, SearchOption.AllDirectories);

                foreach (var sourceDir in directoriesFound)
                {
                    CopyDirectory(sourceDir, Path.Combine(targetDir, dirName));
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
                activityLog.WriteLog($"Directories installed (loose files): {filesInstalled}");
                if (Tools.ConfirmAction("Do you want to convert them", "Loose Files found", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes)
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
                sbar2("SFSE mod installed");
                activityLog.WriteLog("SFSE mod installed");
            }

            if (filesInstalled > 0)
            {
                UpdatePlugins();
                sbar2($"Files installed: {filesInstalled}");
            }

            activityLog.WriteLog($"Mod files installed: {filesInstalled}");

            return true;

            // Helper local function that moves extracted files with confirmation if a destination file exists.
            int MoveExtractedFiles(string searchPattern, string fileTypeLabel)
            {
                int count = 0;
                foreach (string modFile in Directory.EnumerateFiles(extractPath, searchPattern, SearchOption.AllDirectories))
                {
                    string fileName = Path.GetFileName(modFile);
                    string destinationPath = Path.Combine(GamePath, "Data", fileName);

                    if (File.Exists(destinationPath))
                    {
                        var dlg = Tools.ConfirmAction($"Overwrite {fileTypeLabel} {destinationPath}", "Replace mod?", MessageBoxButtons.YesNoCancel);
                        if (dlg == DialogResult.Yes)
                        {
                            File.Move(modFile, destinationPath, true);
                            activityLog.WriteLog($"Moving {modFile} to {destinationPath}");
                            count++;
                        }
                        if (dlg == DialogResult.Cancel)
                        {
                            loadScreen.Close();
                            throw new OperationCanceledException();
                        }
                        if (DialogResult == DialogResult.No)
                            break;
                        {
                            // If the user declines to overwrite, break out of this file loop
                            break;
                        }
                    }
                    else
                    {
                        File.Move(modFile, destinationPath, true);
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

        private object? JsonElementToType(System.Text.Json.JsonElement je, Type targetType)
        {
            try
            {
                if (targetType == typeof(string)) return je.GetString();
                if (targetType == typeof(bool)) return je.GetBoolean();
                if (targetType == typeof(int)) return je.GetInt32();
                if (targetType == typeof(long)) return je.GetInt64();
                if (targetType == typeof(double)) return je.GetDouble();
                if (targetType.IsEnum)
                {
                    var s = je.GetString();
                    if (s != null) return Enum.Parse(targetType, s);
                    return null;
                }

                // For complex types, try deserializing the element into the target type
                var raw = je.GetRawText();
                return System.Text.Json.JsonSerializer.Deserialize(raw, targetType);
            }
            catch
            {
                return null;
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
            if (e.Control && e.KeyCode == Keys.F) // Ctrl+F to search
                txtSearchBox.Focus(); // Focus the search box when Ctrl+F is pressed
        }

        private void lightToolStripMenuItem_Click(object sender, EventArgs e)
        {
            lightToolStripMenuItem.Checked = !lightToolStripMenuItem.Checked;
            Properties.Settings.Default.DarkMode = 0;
            darkToolStripMenuItem.Checked = false;
            systemToolStripMenuItem.Checked = false;
            SetTheme();
            SetThemeMessage();
        }

        private void logWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            logWindowToolStripMenuItem.Checked = !logWindowToolStripMenuItem.Checked;
            Properties.Settings.Default.LogWindow = logWindowToolStripMenuItem.Checked;

            if (logWindowToolStripMenuItem.Checked)
                SetupLogRow();
            else
                HideLogRow();
        }

        private void looseFilesDisabledToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LooseFiles = looseFilesDisabledToolStripMenuItem.Checked = !LooseFiles;
            LooseFilesOnOff(LooseFiles);
            Properties.Settings.Default.LooseFiles = LooseFiles;
        }

        private void LooseFilesMenuUpdate()
        {
            LooseFiles = looseFilesDisabledToolStripMenuItem.Checked = LooseFiles;
            Properties.Settings.Default.LooseFiles = LooseFiles;
        }

        private void LooseFilesOnOff(bool enable)
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "My Games", Tools.GameName, "StarfieldCustom.ini");

            if (Tools.FileCompare(filePath, Path.Combine(Tools.CommonFolder,
                "StarfieldCustom.ini")) && enable == false) // Return if loose files are already disabled
                return;

            if (enable)
            {
                var existingLines = File.Exists(filePath) ? File.ReadLines(filePath).Select(line => line.Trim()).ToHashSet() : new HashSet<string>();

                string[] linesToAppend = { "[Archive]", "bInvalidateOlderFiles=1" };

                if (linesToAppend.Any(line => !existingLines.Contains(line)))
                {
                    File.AppendAllLines(filePath, linesToAppend.Where(line => !existingLines.Contains(line)));
                    LooseFiles = true;
                    sbar("Loose Files Enabled");
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
                sbar("Loose Files Disabled");
                activityLog.WriteLog("Loose files disabled in StarfieldCustom.ini");
            }
        }

        private void lOOTUserlistToolStripMenuItem_Click(object sender, EventArgs e)
        {
            BackupLOOTUserlist();
        }

        private void mnuBackupBlockedMods_Click(object sender, EventArgs e) // Backup BlockedMods.txt to a user selected folder
        {
            BackupBlockedMods();
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
                        page.Size(PageSizes.A4.Landscape());
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
                                        //columns.ConstantColumn(width);
                                        columns.RelativeColumn(1);
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
                    activityLog.WriteLog($"BlockedMods.txt restored from {selectedFolderPath}");
                }
                catch (Exception ex)
                {
                    LogError(ex.Message);
                    MessageBox.Show($"An error occurred while restoring BlockedMods.txt: {ex.Message}", "Restore Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
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
                        if (frmLoadOrder.log)
                            activityLog.WriteLog("Starting Mod Organizer 2");
                        System.Windows.Forms.Application.Exit();
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex.Message);
                    MessageBox.Show(ex.Message);
                }
            }
            else
                MessageBox.Show("MO2 doesn't seem to be installed or path not configured.");
        }

        private void modBackupsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(Properties.Settings.Default.BackupDirectory))
                Tools.OpenFolder(Properties.Settings.Default.BackupDirectory);
            else
                MessageBox.Show("Backup directory will be set after backing up a mod", "Backup Directory Not Set",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void modContentsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string modName;

            if (dataGridView1.CurrentRow is null || dataGridView1.CurrentRow.IsNewRow)
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

        private void modStatsToolStripMenuItem_Click(object sender, EventArgs e) // Toggle Mod Stats visibility
        {
            modStatsToolStripMenuItem.Checked = !modStatsToolStripMenuItem.Checked;
            Properties.Settings.Default.ModStats = modStatsToolStripMenuItem.Checked;
            if (modStatsToolStripMenuItem.Checked)
                RefreshDataGrid();
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
            activityLog.WriteLog($"Moved {selectedRow.Cells["PluginName"].Value} to bottom of list");
            isModified = true;
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
            activityLog.WriteLog($"Moved {selectedRow.Cells["PluginName"].Value} down the list from {selectedRow.Index} to {selectedRow.Index + 1}");
            isModified = true;
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
            activityLog.WriteLog($"Moved {selectedRow.Cells["PluginName"].Value} to top of list");
            isModified = true;
        }

        private void moveUnusedModsOutOfDataDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<string> mods = new(), modsToMove = new();

            frmMoveMods moveModsForm = new();
            moveModsForm.ShowDialog();
            if (returnStatus == 0) // 0 = Cancel, 1 = Creations, 2 = Other, 3 = Both
            {
                sbar("Move Inactive Mods cancelled.");
                return;
            }

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if ((bool)row.Cells["ModEnabled"].Value == false) // Disabled only
                {
                    switch (returnStatus)
                    {
                        case 1: // Creations
                            if (!string.IsNullOrEmpty(row.Cells["CreationsID"].Value.ToString()))
                                modsToMove.Add(row.Cells["PluginName"].Value.ToString());
                            break;

                        case 2: // Other
                            if (string.IsNullOrEmpty(row.Cells["CreationsID"].Value.ToString()))
                                modsToMove.Add(row.Cells["PluginName"].Value.ToString());
                            break;

                        case 3: // Both
                            modsToMove.Add(row.Cells["PluginName"].Value.ToString());
                            break;

                        case 4: // Blocked mods
                            if (row.Cells["Blocked"].Value != null && (bool)row.Cells["Blocked"].Value == true)
                                modsToMove.Add(row.Cells["PluginName"].Value.ToString());
                            break;
                    }
                }
                else
                    mods.Add(row.Cells["PluginName"].Value.ToString());
            }

            if (modsToMove.Count == 0)
            {
                sbar("No inactive mods found to move.");
                MessageBox.Show("No inactive mods found to move.", "Move Inactive Mods", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            frmGenericTextList inactive = new("These mods will be moved", modsToMove); // Show list of mods to be moved
            inactive.ShowDialog();
            if (returnStatus == 0)
            {
                sbar("Move Inactive Mods cancelled.");
                return;
            }
            if (Tools.ConfirmAction("Move Inactive Mods\nExisting files will not be moved", $"Move {modsToMove.Count} inactive mods to a separate folder?",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            using FolderBrowserDialog folderBrowserDialog = new();
            folderBrowserDialog.Description = "Select destination folder for inactive mods";
            folderBrowserDialog.ShowDialog();
            if (string.IsNullOrEmpty(folderBrowserDialog.SelectedPath))
                return;
            string destDir = Path.Combine(folderBrowserDialog.SelectedPath, "Inactive Mods");
            foreach (var mod in modsToMove)
            {
                string[] patterns = { mod[..mod.LastIndexOf('.')] + ".*", mod[..mod.LastIndexOf('.')] + " - *.ba2" }; // Strip extension
                foreach (var pattern in patterns)
                {
                    var files = Directory.GetFiles(Path.Combine(GamePath, "Data"), pattern);
                    foreach (var file in files)
                    {
                        if (!Directory.Exists(destDir))
                            Directory.CreateDirectory(destDir);
                        string destPath = Path.Combine(destDir, Path.GetFileName(file));
                        try
                        {
                            sbar($"Moving {Path.GetFileName(file)}...");
                            statusStrip1.Refresh();
                            File.Move(file, destPath);
                            activityLog.WriteLog($"Moved inactive mod file {Path.GetFileName(file)} to Inactive Mods folder.");
                        }
                        catch (Exception ex)
                        {
                            LogError(ex.Message);
                            sbar($"Failed to move {Path.GetFileName(file)}:\n{ex.Message}");
                        }
                    }
                }
            }
            sbar($"Inactive mods moved to {destDir}");
            MessageBox.Show("Press the Update button to refresh the mod list or Cancel to leave the Plugins.txt file unchanged.");
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
            activityLog.WriteLog($"Moved {selectedRow.Cells["PluginName"].Value} up the list from {selectedRow.Index + 2} to {selectedRow.Index + 1}");
            isModified = true;
        }

        private void nexusTrackingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Tools.OpenUrl($"https://www.nexusmods.com/{GameName}/mods/trackingcentre?tab=tracked+content+updates");
        }

        private void nexusUpdatedModsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Tools.OpenUrl($"https://www.nexusmods.com/games/{GameName}/mods?sort=updatedAt");
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

        private void prepareForCreationsUpdateToolStripMenuItem_Click(object sender, EventArgs e) // Workaround for Creations update re-downloading mods
        {
            if (!Properties.Settings.Default.CreationsUpdate) // Catalog Auto Restore off etc.
            {
                cretionsUpdateToolStripMenuItem.Checked = true;
                Properties.Settings.Default.CreationsUpdate = true;
                Properties.Settings.Default.AutoRestore = false;
                Properties.Settings.Default.AutoCheck = true;

                if (Tools.ConfirmAction("1. Start the game and update Creations mods.\n2. Don't Load a Save Game\n3. Quit the game and run this app again\n\n" +
                    "To Cancel this option," +
                    " click this menu option again\n\nRun the game now?", "Steps to Update Creations Mods", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                    true) == DialogResult.Yes)
                {
                    activityLog.WriteLog("Creations Update started, running game now");
                    RunGame(); ;
                }
            }
            else
            {
                cretionsUpdateToolStripMenuItem.Checked = false; // Cancel Creations update
                Properties.Settings.Default.CreationsUpdate = false;
                Properties.Settings.Default.AutoRestore = true;
                MessageBox.Show("Catalog Auto Restore set to on", "Creations Update Cancelled");
                activityLog.WriteLog("Creations Update Cancelled");
            }
        }

        private void profileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Tools.OpenFolder(Properties.Settings.Default.ProfileFolder);
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
                MessageBox.Show("Error opening profile folder: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
                activityLog.WriteLog("Readfile path set to: " + openReadfile.FileName);
            }
        }

        private void ReadLOOTGroups() // Read LOOT Groups
        {
            string yamlPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                $"LOOT\\games\\{GameName}\\userlist.yaml");
            if (!File.Exists(yamlPath))
                return;
            try
            {
                //var deserializer = new DeserializerBuilder().Build();
                var deserializer = new DeserializerBuilder()
    .WithNamingConvention(UnderscoredNamingConvention.Instance)
    .IgnoreUnmatchedProperties()
    .Build();

                string yamlContent = File.ReadAllText(yamlPath);
                Groups = deserializer.Deserialize<Tools.Configuration>(yamlContent);
            }
            catch (Exception ex)
            {
                MessageBox.Show("LOOT userlist.yaml possibly corrupt\nPossible missing display field in required mods\nRun LOOT to edit metadata",
                    "Yaml decoding error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                LogError(ex.Message);
            }
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

        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshDataGrid();
        }

        private int RemoveDuplicates()
        {
            string loText = Path.Combine(Tools.GameAppData, "Plugins.txt");
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
            activityLog.WriteLog($"Duplicates Removed: {removedCount}");
            sbar4($"Duplicates removed: {removedCount}");
            return removedCount;
        }

        private void removeDuplicatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RemoveDuplicates();
        }

        private int RemoveMissing() // Remove missing .esm/.esp entries from Plugins.txt
        {
            int removedFiles = 0;

            if (!CheckGamePath() || string.IsNullOrEmpty(GamePath))
                return 0; // Can't proceed without game path

            string directory = Path.Combine(GamePath, "Data");
            List<string> pluginFiles = tools.GetPluginList(Game); // Get existing plugin files

            try
            {
                pluginFiles.AddRange(Directory.EnumerateFiles(directory, "*.esp", SearchOption.TopDirectoryOnly)
                                              .Select(Path.GetFileName));
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
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
            activityLog.WriteLog($"Plugins removed: {removedFiles}");

            return removedFiles;
        }

        private void removeMissingModsFromBlockedModstxtToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var blockedMods = Tools.BlockedMods();
            var missingMods = pluginList.Intersect(blockedMods);

            try
            {
                if (File.Exists(Path.Combine(Tools.LocalAppDataPath, "BlockedMods.txt")))
                {
                    File.WriteAllLines(Path.Combine(Tools.LocalAppDataPath, "BlockedMods.txt"), missingMods);
                    sbar3("Removed missing mods from BlockedMods.txt");
                    activityLog.WriteLog("Removed missing mods from BlockedMods.txt");
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
                MessageBox.Show("Error writing BlockedMods.txt: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void removeSFSEToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string[] files = Directory.GetFiles(GamePath, "sfse_*.dll", SearchOption.TopDirectoryOnly); // Delete the dll files as well
            foreach (var file in files)
            {
                if (File.Exists(file))
                    activityLog.WriteLog("Deleting " + tempstr);
                File.Delete(file);
            }

            tempstr = Path.Combine(GamePath, "sfse_loader.exe");
            if (File.Exists(tempstr))
            {
                activityLog.WriteLog("Deleting " + tempstr);
                File.Delete(tempstr);
            }
            else
                LogError($"{tempstr} not found");
        }

        private void renameModToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<string> files = new();
            if (!CheckGamePath()) // Abort if game path not set
                return;

            if (Tools.ConfirmAction("This may affect other mods.", "Rename mod - Use with caution", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation) != DialogResult.OK)
                return;

            string directoryPath = Path.Combine(GamePath, "Data");
            var row = dataGridView1.CurrentRow;
            string ModName = row.Cells["PluginName"].Value.ToString();
            ModName = ModName[..ModName.LastIndexOf('.')]; // Strip extension
            string ModFile = Path.Combine(directoryPath, ModName);

            // Collect existing mod-related files
            string[] fixedExtensions = { ".esp", ".esm", " - voices_en.ba2" };
            foreach (var ext in fixedExtensions)
            {
                string fullPath = ModFile + ext;
                if (File.Exists(fullPath))
                    files.Add(fullPath);
            }

            // Handle texture files like " - textures*.ba2"
            string pattern = ModName + " - textures*.ba2";

            string[] matchedFiles = Directory.GetFiles(directoryPath, Path.GetFileName(pattern));
            files.AddRange(matchedFiles);

            // Handle texture files like " - main*.ba2"
            pattern = ModName + " - main*.ba2";
            matchedFiles = Directory.GetFiles(directoryPath, Path.GetFileName(pattern));
            files.AddRange(matchedFiles);

            string userInput = Interaction.InputBox("New Name:", "Rename Mod", ModName);
            if (string.IsNullOrWhiteSpace(userInput))
                return;
            userInput = Path.GetFileNameWithoutExtension(userInput); // Remove any extension from user input

            // Rename each file
            foreach (var oldPath in files)
            {
                string extensionPart = oldPath.Substring(ModFile.Length); // Get suffix like ".esp" or " - textures01.ba2"
                string newPath = Path.Combine(directoryPath, userInput + extensionPart);

                try
                {
                    File.Move(oldPath, newPath);
                    activityLog.WriteLog($"Renamed: {Path.GetFileName(oldPath)} to {Path.GetFileName(newPath)}");
                }
                catch (Exception ex)
                {
                    LogError(ex.Message);
                    MessageBox.Show($"Failed to rename {oldPath} to {newPath}:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            SyncPlugins();
            sbar($"Mod {ModName} renamed to: {userInput}");
        }

        private void resetAppPreferencesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ResetPreferences();
        }

        private int ResetDefaults()
        {
            string LooseFilesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "My Games", GameName), // Check if loose files are enabled
        filePath = Path.Combine(LooseFilesDir, $"{GameName}Custom.ini");
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

                if (Delccc()) // Delete ccc
                    ChangeCount++;

                if (ResetGameCustomINI(false) == 1) // Apply recommended settings
                    ChangeCount++;

                sbar3(ChangeCount.ToString() + " Change(s) made to ini files");
            }
            sbar5("Auto Reset");
            return ChangeCount;
        }

        private void resetEverythingToolStripMenuItem_Click(object sender, EventArgs e) // Reset all game settings and delete loose file folders, preserves app settings.
        {
            int actionCount;

            if (Tools.ConfirmAction("This will reset all game settings and delete all loose files folders", "Are you sure?", MessageBoxButtons.YesNo,
                MessageBoxIcon.Exclamation, true) == DialogResult.No)
                return;
            activityLog.WriteLog("Starting reset everything.");
            //actionCount = RestoreStarfieldINI();
            actionCount = DeleteLooseFileFolders();
            actionCount += ResetDefaults();
            actionCount += CheckArchives();

            sbar3(actionCount.ToString() + " Change(s) made");
            activityLog.WriteLog("Reset everything: " + actionCount.ToString() + " Change(s) made");
        }

        private int ResetGameCustomINI(bool ConfirmOverwrite)  // true for confirmation
        {
            if (ConfirmOverwrite)
            {
                DialogResult DialogResult = MessageBox.Show($"This will overwrite your {GameName}Custom.ini to a recommended version", "Are you sure?",
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Stop);
                if (DialogResult != DialogResult.OK)
                    return 0;
            }

            try
            {
                if (!Tools.FileCompare(Path.Combine(Tools.CommonFolder, $"{GameName}Custom.ini"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "My Games", GameName, $"{GameName}Custom.ini"))) // Check if game Custom.ini needs resetting
                {
                    File.Copy(Path.Combine(Tools.CommonFolder, $"{GameName}Custom.ini"),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        "My Games", GameName, $"{GameName}Custom.ini"), true);
                    sbar3($"{GameName}Custom.ini restored");
                    activityLog.WriteLog($"{GameName} Custom.ini restored");
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, $"Error restoring {GameName}Custom.ini");
                LogError("Unable to reset INI " + ex.Message);
                return 0;
            }
        }

        private void resetLoadScreenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.LoadScreenFilename = "";
            Properties.Settings.Default.RandomLoadScreen = randomToolStripMenuItem.Checked = false;
            SaveSettings();
        }

        private void resetToVanillaStarfieldSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Tools.ConfirmAction("Reset ini settings?", "Reset to recommended settings", MessageBoxButtons.YesNo) == DialogResult.Yes)
                ResetDefaults();
        }

        private void ResetWindowSize()
        {
            Rectangle resolution = Screen.PrimaryScreen.Bounds; // Resize window to 85% of screen width
            double screenWidth = resolution.Width;
            double screenHeight = resolution.Height;
            this.Width = (int)(screenWidth * 0.85);
            this.Height = (int)(screenHeight * 0.85);
            this.StartPosition = FormStartPosition.CenterScreen;
            activityLog.WriteLog("Window size reset to default");
        }

        private void ResizeForm()
        {
            // Force autosize calculation
            dataGridView1.AutoResizeColumns();
            dataGridView1.AutoResizeRows();

            // Calculate total width of visible columns
            int totalWidth = 0;
            foreach (DataGridViewColumn col in dataGridView1.Columns)
            {
                if (col.Visible)
                    totalWidth += col.Width;
            }

            // Calculate total height of visible rows
            int totalHeight = dataGridView1.ColumnHeadersHeight;
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Visible)
                    totalHeight += row.Height;
            }
            totalHeight += dataGridView1.Margin.All * 30;

            // Add padding for borders, scrollbars, margins
            totalWidth += SystemInformation.VerticalScrollBarWidth + dataGridView1.RowHeadersWidth +
                SystemInformation.BorderSize.Width * 2 + dataGridView1.Margin.All * 2;

            // Desired size
            int desiredWidth = totalWidth + 50;
            if (desiredWidth < flowLayoutPanel1.Width + 200)
                desiredWidth = flowLayoutPanel1.Width + 200;
            int desiredHeight = totalHeight + menuStrip1.Height + rtbLog.Height + flowLayoutPanel1.Height +
                statusStrip1.Height + SystemInformation.CaptionHeight + SystemInformation.BorderSize.Height;

            // Clamp to screen working area
            Rectangle screenBounds = Screen.FromControl(this).WorkingArea;
            int finalWidth = Math.Min(desiredWidth, screenBounds.Width);
            int finalHeight = Math.Min(desiredHeight, screenBounds.Height);
            finalHeight = Math.Max(finalHeight, 600); // Minimum height

            // Apply size
            this.Size = new System.Drawing.Size(finalWidth, finalHeight);

            // Center form
            int x = (screenBounds.Width - this.Width) / 2 + screenBounds.Left;
            int y = (screenBounds.Height - this.Height) / 2 + screenBounds.Top;

            this.Location = new Point(x, y);
        }

        private void resizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            resizeToolStripMenuItem.Checked = Properties.Settings.Default.Resize = !resizeToolStripMenuItem.Checked;
            if (resizeToolStripMenuItem.Checked)
                ResizeForm();
        }

        private void RestoreAppSettings()
        {
            using var ofd = new System.Windows.Forms.OpenFileDialog
            {
                Title = "Import application settings from JSON",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = "json",
                Multiselect = false
            };

            if (ofd.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                var json = File.ReadAllText(ofd.FileName);
                Dictionary<string, object> dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(json);
                if (dict is null) throw new InvalidOperationException("Invalid JSON or empty file.");
                ApplySettings(dict);
                Properties.Settings.Default.Save();
                tools.RestartApp("App settings restored");
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
                MessageBox.Show(this, "Import failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void restoreContentCatalogtxtToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using FolderBrowserDialog folderBrowserDialog = new();
            folderBrowserDialog.Description = "Choose folder to restore ContentCatalog.txt from";
            folderBrowserDialog.InitialDirectory =
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); // Set initial directory to Documents Directory
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                string selectedFolderPath = folderBrowserDialog.SelectedPath;

                string backupFilePath = Path.Combine(selectedFolderPath, "ContentCatalog.txt");
                string destinationPath = Path.Combine(Tools.GameAppData, "ContentCatalog.txt");

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
                    activityLog.WriteLog($"ContentCatalog.txt restored from {selectedFolderPath}");
                }
                catch (Exception ex)
                {
                    LogError(ex.Message);
                    MessageBox.Show($"An error occurred while restoring ContentCatalog.txt: {ex.Message}", "Restore Failed",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void RestorePlugins()
        {
            string sourceFileName = Path.Combine(Tools.GameAppData, "Plugins.txt.bak");
            string destFileName = Path.Combine(Tools.GameAppData, "Plugins.txt");

            try
            {
                // Copy the file
                File.Copy(sourceFileName, destFileName, true); // overwrite
                InitDataGrid();

                toolStripStatusStats.ForeColor = DefaultForeColor;
                SavePlugins();
                sbar("Restore done");
                activityLog.WriteLog($"Restore of {Path.GetFileName(sourceFileName)} done to {Path.GetFileName(destFileName)}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Restore failed");
                LogError("Restore failed " + ex.Message);
            }
        }

        private void restoreProfilesToolStripMenuItem_Click(object sender, EventArgs e) // Restore profiles from Backup folder
        {
            if (Properties.Settings.Default.ProfileFolder == "" || !Directory.Exists(Path.Combine(Properties.Settings.Default.ProfileFolder, "Backup")))
            {
                MessageBox.Show("No profile or backup folder set");
                return;
            }

            if (Tools.ConfirmAction("Restore Profile Backup", "Restore Backup", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.Cancel)
            {
                sbar("Restore cancelled");
                return;
            }

            if (Directory.Exists(Path.Combine(Properties.Settings.Default.ProfileFolder, "Backup")))
            {
                foreach (var item in Directory.EnumerateFiles(Path.Combine(Properties.Settings.Default.ProfileFolder, "Backup"), "*.txt",
                    SearchOption.TopDirectoryOnly))
                {
                    string fileName = Path.GetFileName(item);
                    string destinationPath = Path.Combine(Properties.Settings.Default.ProfileFolder, fileName);
                    File.Copy(item, destinationPath, true);
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

        private int RestoreStarfieldINI()
        {
            string StarfieldiniPath = Path.Combine(GamePath, $"{GameName}.ini");

            if (!Tools.FileCompare(StarfieldiniPath, Path.Combine(Tools.CommonFolder, $"{GameName}.ini")))
            {
                try
                {
                    File.Copy(Path.Combine(Tools.CommonFolder, $"{GameName}.ini"), Path.Combine(GamePath, $"{GameName}.ini"), true); // Restore game.ini
                    sbar3($"{GameName}.ini restored");
                    activityLog.WriteLog($"{GameName}.ini restored to default settings");
                    return 1;
                }
                catch (Exception ex)
                {
                    LogError(ex.Message);
                    MessageBox.Show(ex.Message);
                    return 0;
                }
            }
            else
            {
                sbar3($"{GameName}.ini Matches Default");
                activityLog.WriteLog($"{GameName}.ini Matches Default");
                return 0;
            }
        }

        private void restoreStarfieldiniToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RestoreStarfieldINI();
        }

        private void rtbLog_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
                contextMenuLogRow.Show(Cursor.Position);
        }

        private void runBatchFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(Properties.Settings.Default.ReadFileBatchPath))
                Task.Run(() => Tools.OpenFile(Properties.Settings.Default.ReadFileBatchPath));
            //Tools.OpenFile(Properties.Settings.Default.ReadFileBatchPath);
            else
                MessageBox.Show("Batch file path not set. Please set it in the settings.", "Batch File Not Set", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void RunGame()
        {
            bool result;

            tempstr = Properties.Settings.Default.RunProgramPath;
            if (Properties.Settings.Default.RunProgram && !string.IsNullOrEmpty(tempstr))
            {
                if (File.Exists(tempstr))
                {
                    if (Process.GetProcessesByName(Path.GetFileNameWithoutExtension(tempstr)).Length == 0)
                        Process.Start(tempstr); // Start the configured program before running the game.
                }
                else
                {
                    if (Tools.ConfirmAction($"Run program path not found: {tempstr}", "Stop Game Launch?", MessageBoxButtons.YesNo,
                        MessageBoxIcon.Error) == DialogResult.Yes)
                        return;
                }
            }

            Properties.Settings.Default.GameVersion = GameVersion;
            SaveSettings();
            Form SS;
            /*if (devMode)
                SS = new frmSplashScreenVideo();
            else*/
            SS = new frmSplashScreen();

            sbar("Starting game...");
            if (GameVersion != MS && Properties.Settings.Default.LoadScreenEnabled)
                SS.Show();

            if (isModified)
                SavePlugins();
            activityLog.WriteLog($"Starting game: {GameName}");
            result = Tools.StartGame(GameVersion);
            activityLog.WriteLog($"Game started: {GameVersion}, Status: {result}");

            if (!result)
            {
                timer1.Stop();
                SS.Close();
            }
            else
            {
                if (Properties.Settings.Default.LoadScreenEnabled)
                    timer1.Start();
                else
                    this.Close();
            }
        }

        private void RunLOOT(bool LOOTMode) // True for autosort
        {
            string GameFolder = Tools.GameLibrary.GetById(Properties.Settings.Default.Game).AppData;
            bool profilesActive = Profiles;
            string lootPath = Properties.Settings.Default.LOOTPath;

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
                sbar("LOOT path is required to run LOOT");
                return;
            }

            ResetGroupsButton();
            lootPath = Properties.Settings.Default.LOOTPath;
            string cmdLine = (GameVersion != MS) ? $"--game=\"{GameFolder}\"" : $"--game=\"{GameFolder} (MS Store)\"";
            if (LOOTMode) cmdLine += " --auto-sort";

            // Temporarily disable profiles
            Profiles = cmbProfile.Enabled = chkProfile.Checked = false;

            // Start LOOT process and wait for it to close
            ProcessStartInfo startInfo = new()
            {
                FileName = lootPath,
                Arguments = cmdLine,
                WorkingDirectory = Path.GetDirectoryName(lootPath) ?? string.Empty
            };

            if (File.Exists(lootPath))
            {
                activityLog.WriteLog($"Starting LOOT with arguments: {cmdLine}");
                using (Process process = Process.Start(startInfo))
                {
                    process.WaitForExit();
                    ReadLOOTGroups();
                }

                if (Properties.Settings.Default.AutoDelccc)
                    Delccc();
                InitDataGrid();

                // Remove base game files if LOOT added them
                tools.BethFiles.ForEach(bethFile =>
                {
                    var rowToRemove = dataGridView1.Rows
                        .Cast<DataGridViewRow>()
                        .FirstOrDefault(row => row.Cells["PluginName"].Value as string == bethFile);

                    if (rowToRemove != null) dataGridView1.Rows.Remove(rowToRemove);
                });
            }

            // Re-enable profiles if previously active
            Profiles = profilesActive;
            isModified = true;
            SavePlugins();
            cmbProfile.Enabled = Profiles;
            chkProfile.Checked = Profiles;
        }

        private void runProgramToolStripMenuItem_Click(object sender, EventArgs e)
        {
            runProgramToolStripMenuItem.Checked = Properties.Settings.Default.RunProgram = !runProgramToolStripMenuItem.Checked;
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

        private void savedGameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string savesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", GameName, "Saves");
            if (Directory.Exists(savesPath))
                Tools.OpenFolder(savesPath);
            else
                MessageBox.Show("Save game directory not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    writer.WriteLine($"# This file is used by {GameName} to keep track of your downloaded content.");
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
                pluginList = tools.GetPluginList(Game);
            }
            catch (Exception ex)
            {
                FileInfo fileInfo = new FileInfo(PluginFileName);

                if (fileInfo.Exists)
                {
                    bool isReadOnly = fileInfo.IsReadOnly;
                    MessageBox.Show($"{PluginFileName} is read-only", "Unable to save plugins", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    LogError($"Error saving {Path.GetFileName(PluginFileName)}: {ex.Message}");
                }
            }

            sbar2($"{Path.GetFileName(PluginFileName)} saved");
            isModified = false;
            activityLog.WriteLog($"{Path.GetFileName(PluginFileName)} saved");
        }

        private void saveOnExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveOnExitToolStripMenuItem.Checked = Properties.Settings.Default.SaveLog = !saveOnExitToolStripMenuItem.Checked;
        }

        private void SavePlugins()
        {
            string pluginPath = Path.Combine(Tools.GameAppData, @"Plugins.txt");
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

        private void SaveProfileSettings(string fileName)
        {
            Properties.Settings.Default.ProfileFolder = Path.GetDirectoryName(fileName);
            SaveSettings();
            SwitchProfile(fileName);
            GetProfiles();
            isModified = false;
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SavePlugins();
        }

        private void SaveWindowSettings()
        {
            Properties.Settings.Default.WindowLocation = this.Location; // Save window pos and size
            Properties.Settings.Default.WindowSize = this.Size;
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
            toolStripStatus4.Text = StatusBarMessage;
        }

        private void sbar5(string StatusMessage)
        {
            toolStripStatusTime.Text = StatusMessage;
        }

        private void scriptLogsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tempstr = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", GameName, "Logs\\Script");
            if (!Directory.Exists(tempstr))
            {
                MessageBox.Show("Script logs directory not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Tools.OpenFolder(tempstr);
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

            if (ActiveOnly)
                ActiveOnlyToggle(); // Disable filter

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
                    // Set current cell
                    dataGridView1.CurrentCell = dataGridView1.Rows[rowIndex].Cells["PluginName"];
                    return;
                }
            }

            // Notify that the search query was not found.
            sbar2($"{txtSearchBox.Text} not found");
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

        private void SetTheme()
        {
            // Set the color mode based on the theme
            var colorMode = Properties.Settings.Default.DarkMode switch
            {
                0 => SystemColorMode.Classic,
                1 => SystemColorMode.Dark,
                2 => SystemColorMode.System,
                _ => SystemColorMode.Classic // Default fallback
            };

            try
            {
                Application.SetColorMode(colorMode);
            }
            catch (Exception ex)
            {
                LogError("SetTheme error: " + ex.Message);
            }

            // Update menu item selection
            var menuItems = new Dictionary<int, ToolStripMenuItem>
            {
                 { 0, lightToolStripMenuItem },
                 { 1, darkToolStripMenuItem },
                 { 2, systemToolStripMenuItem }
             };
            if (menuItems.TryGetValue(Properties.Settings.Default.DarkMode, out var menuItem))
                menuItem.Checked = true;

            // Apply UI changes for dark mode conditions
            if (colorMode == SystemColorMode.Dark ||
               (colorMode == SystemColorMode.System && System.Windows.Forms.Application.SystemColorMode == SystemColorMode.Dark))
            {
                dataGridView1.EnableHeadersVisualStyles = false;
                dataGridView1.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.Blue; // Background color of selected cells
                dataGridView1.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.White; // Text color of selected cells
                statusStrip1.BackColor = System.Drawing.Color.Black;
            }
            else
                dataGridView1.EnableHeadersVisualStyles = true;
        }

        private void SetThemeMessage()
        {
            if (Tools.ConfirmAction("Restart for all changes to be applied", "App restart recommended", MessageBoxButtons.YesNo,
                MessageBoxIcon.Information) == DialogResult.Yes)
                tools.RestartApp("Theme change");
        }

        private void SetupColumns()
        {
            var props = Properties.Settings.Default;
            SetColumnVisibility(props.TimeStamp, timeStampToolStripMenuItem, dataGridView1.Columns["TimeStamp"]);
            SetColumnVisibility(props.Achievements, toolStripMenuAchievements, dataGridView1.Columns["Achievements"]);
            SetColumnVisibility(props.CreationsID, toolStripMenuCreationsID, dataGridView1.Columns["CreationsID"]);
            SetColumnVisibility(props.Files, toolStripMenuFiles, dataGridView1.Columns["Files"]);
            SetColumnVisibility(props.Group, toolStripMenuGroup, dataGridView1.Columns["Group"]);
            SetColumnVisibility(props.Index, toolStripMenuIndex, dataGridView1.Columns["Index"]);
            SetColumnVisibility(props.FileSize, toolStripMenuFileSize, dataGridView1.Columns["FileSize"]);
            SetColumnVisibility(props.URL, uRLToolStripMenuItem, dataGridView1.Columns["URL"]);
            SetColumnVisibility(props.Version, toolStripMenuVersion, dataGridView1.Columns["Version"]);
            SetColumnVisibility(props.AuthorVersion, toolStripMenuAuthorVersion, dataGridView1.Columns["AuthorVersion"]);
            SetColumnVisibility(props.Description, toolStripMenuDescription, dataGridView1.Columns["Description"]);
            SetColumnVisibility(props.Blocked, blockedToolStripMenuItem, dataGridView1.Columns["Blocked"]);
        }

        private void SetupGame()
        {
            GameVersion = Properties.Settings.Default.GameVersion;
            Game = Properties.Settings.Default.Game;

            //GameName = gl.GameName(Properties.Settings.Default.Game);
            var x = Properties.Settings.Default.Game;
            GameName = Tools.GameLibrary.GetById(Properties.Settings.Default.Game).GameName;
            activityLog.WriteLog($"Game set to {GameName}");
            GameExists = Tools.CheckGame(); // Check if game appdata folder exists

            string PluginsPath = Path.Combine(Tools.GameAppData, "Plugins.txt");
            if (!File.Exists(PluginsPath) && GameExists)
            {
                MessageBox.Show(@"Missing Plugins.txt file

Click Ok to create a blank Plugins.txt file
Click File->Restore if you have a backup of your Plugins.txt file
Alternatively, run the game once to have it create a Plugins.txt file for you.
The game will delete your Plugins.txt file if it doesn't find any mods", "Plugins.txt not found");

                try
                {
                    File.WriteAllText(PluginsPath, $"# This file is used by {GameName} to keep track of your downloaded content.\n# Please do not modify this file.\n");
                }
                catch (Exception ex)
                {
                    LogError(ex.Message);
                }
            }

            // Do a 1-time backup of Plugins.txt if it doesn't exist
            try
            {
                if (!File.Exists(Path.Combine(Tools.GameAppData, "Plugins.txt.bak")) && File.Exists(PluginsPath))
                {
                    File.Copy(PluginsPath, Tools.GameAppData + @"\Plugins.txt.bak");
                    sbar2("Plugins.txt backed up to Plugins.txt.bak");
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }

            // Setup game version
            if (GameVersion != MS)
            {
                GamePath = Properties.Settings.Default.GamePath;
                if (GamePath == "")
                {
                    GamePath = tools.GetSteamGamePath(GameName); // Detect Steam path
                    if (GamePath == "")
                    {
                        GamePath = tools.SetGamePath();
                        Properties.Settings.Default.GamePath = GamePath;
                    }
                }
            }
            else
            {
                if (Properties.Settings.Default.GamePathMS == "")
                    tools.SetGamePathMS();
                GamePath = Properties.Settings.Default.GamePathMS;
            }
            if (!Tools.CheckGame())
                GamePath = "";

            if (Properties.Settings.Default.AutoDelccc)
            {
                toolStripMenuAutoDelccc.Checked = true;
                if (Delccc())
                    sbar("CCC deleted");
            }

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
        }

        private void SetupLogRow()
        {
            if (!Properties.Settings.Default.LogWindow)
                return;

            tableLayoutPanel1.RowStyles[1].SizeType = SizeType.Percent; // Set datagrid display height
            tableLayoutPanel1.RowStyles[1].Height = 90f;
            tableLayoutPanel1.RowStyles[2].SizeType = SizeType.Percent; // Set log display height
            tableLayoutPanel1.RowStyles[2].Height = 10f;
            rtbLog.Visible = true;
            rtbLog.Dock = DockStyle.Fill;
            ResizeForm();
            rtbLog.ScrollToCaret();
        }

        private void SetUpMenus()
        {
            menuStrip1.Font = Properties.Settings.Default.FontSize; // Set custom font size
            this.Font = Properties.Settings.Default.FontSize;

            var settings = Properties.Settings.Default;
            // Assign values
            toggleToolStripMenuItem.Checked = settings.Log;
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
            cretionsUpdateToolStripMenuItem.Checked = settings.CreationsUpdate;
            modStatsToolStripMenuItem.Checked = settings.ModStats;
            blockedToolStripMenuItem.Checked = settings.Blocked;
            blockedModsToolStripMenuItem.Checked = settings.BlockedView;
            if (Properties.Settings.Default.VortexPath != "")
                vortexToolStripMenuItem.Visible = true;
            runProgramToolStripMenuItem.Checked = settings.RunProgram;
            resizeToolStripMenuItem.Checked = settings.Resize;
            enableSplashScreenToolStripMenuItem.Checked = Properties.Settings.Default.LoadScreenEnabled;
            logWindowToolStripMenuItem.Checked = Properties.Settings.Default.LogWindow;
            saveOnExitToolStripMenuItem.Checked = Properties.Settings.Default.SaveLog;
            rowHighlightToolStripMenuItem.Checked = Properties.Settings.Default.RowHighlight;
            randomToolStripMenuItem.Checked = Properties.Settings.Default.RandomLoadScreen;
            sequenceToolStripMenuItem.Checked = Properties.Settings.Default.LoadScreenSequence;
        }

        private void sFSEPluginsToolStripMenuItem_Click(object sender, EventArgs e) // Open SFSE Plugins Directory
        {
            string SFSEPlugins = Path.Combine(GamePath, @"Data\SFSE\Plugins");
            if (Directory.Exists(SFSEPlugins))
                Tools.OpenFolder(SFSEPlugins);
            else
                MessageBox.Show("Unable to find SFSE Plugins Directory");
        }

        private void showAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowHideColumns(true);
        }

        private void ShowHideColumns(bool action)
        {
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
                    SetColumnVisibility(action, menuItem, dataGridView1.Columns[columnName]);
                    Properties.Settings.Default[columnName] = action;
                }
            }
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

        private string ShowModStats(List<string> CreationsPlugin, int enabledCount, long totalFileSize)
        {
            string loText = Path.Combine(Tools.GameAppData, "Plugins.txt"), StatText = "",
                GameFolder = Tools.GameLibrary.GetById(Properties.Settings.Default.Game).AppData; ;
            int ba2Count, esmCount, espCount, mainCount;
            try
            {
                // Cache file paths and load BGS archives
                var dataDirectory = Path.Combine(GamePath, "Data");
                var bgsArchives = File.ReadLines(Path.Combine(Tools.CommonFolder,
                    Tools.GameLibrary.GetById(Properties.Settings.Default.Game).ExcludeFile + " Archives.txt"))
                    .Where(line => line.Length > 4)
                    .Select(line => line[..^4].ToLowerInvariant())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                // Load enabled plugins
                var enabledPlugins = File.ReadLines(loText)
                    .Where(line => line.StartsWith('*') && line.Length > 1)
                    .Select(line => line[1..])
                    .Where(plugin => plugin.Contains('.'))
                    .Select(plugin => plugin.Split('.')[0].ToLowerInvariant())
                    .Where(plugin => !bgsArchives.Contains(plugin))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                // Process all BA2 files
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
                statusBuilder.Append($"Creations {CreationsPlugin.Count}, Other {Math.Abs(dataGridView1.RowCount - CreationsPlugin.Count)}, ");
                statusBuilder.Append($"Enabled: {enabledCount}, esm: {esmCount}, Archives: {ba2Count}, ");
                statusBuilder.Append($"Enabled - Main: {mainCount}, Textures: {textureCount}");
                if (dataGridView1.Columns["FileSize"].Visible)
                    statusBuilder.Append($", Total Size: {totalFileSize / 1048576:N1} GB"); // 1048576=1024 * 1024 for conversion to GB

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
                LogError($"Mod stats: {ex.Message}");
            }
            return StatText;
        }

        private void ShowRecommendedColumns()
        {
            HideAllColumns();
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

        private void ShowSplashScreen()
        {
            Form SS;
            /*if (devMode)
            {
                 SS = new frmSplashScreenVideo();
            }
            else*/
            SS = new frmSplashScreen();
            SS.Show();
        }

        private void showTimeToolStripMenuItem_Click_1(object sender, EventArgs e) // Display date and time
        {
            timer2.Enabled = !timer2.Enabled;
            showTimeToolStripMenuItem.Checked = timer2.Enabled;
            Properties.Settings.Default.Showtime = showTimeToolStripMenuItem.Checked;
            if (!timer2.Enabled)
                sbar5("");
        }

        private void starUIConfiguratorToolStripMenuItem_Click(object sender, EventArgs e) // Launch StarUI Configurator if installed
        {
            string workingDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", GameName, "Data");
            string StarUI = Path.Combine(workingDirectory, @"StarUI Configurator.bat");
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
                    activityLog.WriteLog("Starting StarUI Configurator");
                }
                catch (Exception ex)
                {
                    LogError(ex.Message);
                    MessageBox.Show(ex.Message);
                }
            }
            else
                MessageBox.Show("StarUI Configurator doesn't seem to be installed correctly.");
        }

        private void steamDBToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Tools.OpenUrl("https://steamdb.info/app/1716740/depots/");
        }

        private void SwitchProfile(string ProfileName)
        {
            activityLog.WriteLog($"Profile selected {ProfileName}");

            if (!File.Exists(ProfileName))
                return;

            if (Properties.Settings.Default.CompareProfiles)
            {
                var currentProfile = File.ReadAllLines(Path.Combine(Tools.GameAppData, "Plugins.txt")).ToList();
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
                File.Copy(ProfileName, Path.Combine(Tools.GameAppData, "Plugins.txt"), true);
                Properties.Settings.Default.LastProfile = ProfileName[(ProfileName.LastIndexOf('\\') + 1)..];
                SaveSettings();
                isModified = false;
                InitDataGrid();
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
                MessageBox.Show(ex.Message, "Error switching profile", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private int SyncPlugins()
        {
            // 1) Validate game path
            if (!CheckGamePath() || string.IsNullOrEmpty(GamePath))
                return 0;

            sbar3("Updating...");
            statusStrip1.Refresh();
            dataGridView1.SuspendLayout();

            // 2) Gather all on-disk plugin filenames
            var pluginFiles = tools.GetPluginList(Game);
            string dataDir = Path.Combine(GamePath, "Data");
            string[] patterns = { "*.esp", "*.esm", "*.esl" };
            foreach (var pattern in patterns)
            {
                try
                {
                    // Use parallel enumeration for large directories
                    var espFiles = Directory.EnumerateFiles(dataDir, pattern, SearchOption.TopDirectoryOnly)
                                           .AsParallel()
                                           .Select(Path.GetFileName)
                                           .ToList();
                    pluginFiles.AddRange(espFiles);
                }
                catch (Exception ex)
                {
                    LogError("Error reading plugins " + ex.Message);
                    MessageBox.Show(
                        $"Error reading plugin files: {ex.Message}",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return 0;
                }
            }

            // Pre-allocate with estimated capacity and use fastest comparer
            var onDisk = new HashSet<string>(pluginFiles.Count, StringComparer.OrdinalIgnoreCase);
            var bethFilesSet = new HashSet<string>(tools.BethFiles.Count(), StringComparer.OrdinalIgnoreCase);
            var inGrid = new HashSet<string>(dataGridView1.Rows.Count, StringComparer.OrdinalIgnoreCase);
            var seenInGrid = new HashSet<string>(dataGridView1.Rows.Count, StringComparer.OrdinalIgnoreCase);

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
            var pluginNameEnabled = dataGridView1.Columns["ModEnabled"].Index;

            // Process all rows
            for (int i = 0; i < rowCount; i++)
            {
                var row = rows[i];
                var cellValue = row.Cells[pluginNameIndex].Value;

                if (cellValue is null) continue;

                var pluginName = cellValue as string;
                if (string.IsNullOrEmpty(pluginName)) continue;

                // Duplicate check
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

            frmGenericTextList fgt;
            List<string> missingMods = new();
            // Removal using batch operations
            if (rowsToRemove.Count > 0)
            {
                if (log)
                {
                    foreach (var row in rowsToRemove)
                    {
                        activityLog.WriteLog($"Found missing mods {row.Cells[pluginNameIndex].Value} from Plugins.txt");
                        missingMods.Add(row.Cells[pluginNameIndex].Value.ToString());
                    }
                    fgt = new frmGenericTextList("Missing Mods", missingMods);
                    fgt.Show();
                }

                DialogResult missingMod = Tools.ConfirmAction("Choose Yes to proceed and remove the missing mods from Plugins.txt or No cancel",
                    "Missing mods found", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                /*if (missingMod== DialogResult.Cancel)
                {
                    sbar3("Update cancelled");
                    dataGridView1.ResumeLayout();
                    return (0);
                }*/
                if (missingMod == DialogResult.No)
                {
                    if (Tools.ConfirmAction("Copy mods from backup folder?", "Attempt to Restore Missing Mods",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        string modName;
                        using FolderBrowserDialog folderBrowserDialog = new();
                        folderBrowserDialog.Description = "Choose Backup Folder";
                        folderBrowserDialog.InitialDirectory = Properties.Settings.Default.BackupDirectory;
                        if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                        {
                            string selectedFolderPath = folderBrowserDialog.SelectedPath;

                            foreach (var mod in rowsToRemove)
                            {
                                if (bool.TryParse(mod.Cells["ModEnabled"].Value?.ToString(), out bool enabled) && enabled) // Enabled mods only
                                {
                                    modName = Path.GetFileNameWithoutExtension(mod.Cells[pluginNameIndex].Value.ToString());
                                    var modFiles = Directory.EnumerateFiles(selectedFolderPath, modName + "*", SearchOption.TopDirectoryOnly);

                                    foreach (var file in modFiles)
                                    {
                                        try
                                        {
                                            File.Copy(file, Path.Combine(dataDir, Path.GetFileName(file)), true);
                                            activityLog.WriteLog("Copying " + file + "to " + Path.Combine(dataDir, Path.GetFileName(file)));
                                        }
                                        catch (Exception ex)
                                        {
                                            LogError($"Error restoring {Path.GetFileName(file)}: {ex.Message}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                    sbar3("Update cancelled");
                    dataGridView1.ResumeLayout();
                    return (0);
                }

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

            // Batch write logs to avoid I/O overhead
            if (log && logEntries?.Count > 0)
            {
                activityLog.WriteLog(string.Join(Environment.NewLine, logEntries));
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

            // Batch add rows
            if (toAdd.Count > 0)
            {
                // Pre-allocate row capacity if supported
                var currentCapacity = rows.Count;

                foreach (var file in toAdd)
                {
                    int idx = rows.Add();
                    var row = rows[idx];

                    row.Cells[modEnabledIndex].Value =
                        (file.Length > 4 &&
                        (file.EndsWith(".esm", StringComparison.OrdinalIgnoreCase) ||
                        file.EndsWith(".esl", StringComparison.OrdinalIgnoreCase) ||
                        file.EndsWith(".esp", StringComparison.OrdinalIgnoreCase)))
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
                activityLog.WriteLog($"Plugins added: {added}, removed: {removed}, duplicates removed: {dupRemoved}");
                isModified = true;
                SavePlugins();
            }

            sbar3("Update complete");
            dataGridView1.ResumeLayout();
            return totalChanges;
        }

        private void systemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            systemToolStripMenuItem.Checked = !systemToolStripMenuItem.Checked;
            Properties.Settings.Default.DarkMode = 2;
            lightToolStripMenuItem.Checked = false;
            darkToolStripMenuItem.Checked = false;
            SetTheme();
            SetThemeMessage();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();
            this.Close();
        }

        private void timer2_Tick(object sender, EventArgs e) // Used for date / time display, ticks once per second.
        {
            DateTime now = DateTime.Now;
            sbar5(now.ToString("ddd, d MMM yyyy - hh:mm tt", CultureInfo.CurrentCulture.DateTimeFormat));
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

        private void ToggleProfiles()
        {
            toolStripMenuProfilesOn.Checked = !toolStripMenuProfilesOn.Checked;
            Properties.Settings.Default.ProfileOn = toolStripMenuProfilesOn.Checked;
            Profiles = toolStripMenuProfilesOn.Checked;
            chkProfile.Checked = toolStripMenuProfilesOn.Checked;

            if (Profiles) GetProfiles();
        }

        private void toggleToolStripMenuItem_Click(object sender, EventArgs e) // Log Toggle
        {
            toggleToolStripMenuItem.Checked = Properties.Settings.Default.Log = log = !toggleToolStripMenuItem.Checked;
            if (activityLog is null)
                EnableLog();
            btnLog.Font = new System.Drawing.Font(btnLog.Font, log ? FontStyle.Bold : FontStyle.Regular);
            SaveSettings();
        }

        private void toolStripClearLogRow_Click(object sender, EventArgs e)
        {
            rtbLog.Clear();
        }

        private void toolStripMenAddRemoveContext_Click(object sender, EventArgs e)
        {
            SyncPlugins();
        }

        private void toolStripMenuAchievements_Click(object sender, EventArgs e)
        {
            toolStripMenuAchievements.Checked = !toolStripMenuAchievements.Checked;
            dataGridView1.Columns["Achievements"].Visible = toolStripMenuAchievements.Checked;
            Properties.Settings.Default.Achievements = toolStripMenuAchievements.Checked;
        }

        private void toolStripMenuAddToProfile_Click(object sender, EventArgs e) // Add selected mods to a different profile
        {
            if (ActiveOnly && dataGridView1.SelectedRows.Count > 1)
            {
                MessageBox.Show("Please disable Active Only mode to use this feature", "Mods may be disabled or enabled unintentionally");
                return;
            }
            List<string> profiles = new();

            if (cmbProfile.Items.Count == 0 || cmbProfile.SelectedItem is null)
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
                    LogError(ex.Message);
                }

                var dlgResult = Tools.ConfirmAction("Run update/sort on all profiles", "Update All Profiles?",
                        MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (dlgResult == DialogResult.Yes)
                    UpdateAllProfiles();

                if (dlgResult == DialogResult.Cancel)
                    return;
            }
        }

        private void toolStripMenuAuthorVersion_Click(object sender, EventArgs e) // View author column
        {
            toolStripMenuAuthorVersion.Checked = !toolStripMenuAuthorVersion.Checked;
            dataGridView1.Columns["AuthorVersion"].Visible = toolStripMenuAuthorVersion.Checked;
            Properties.Settings.Default.AuthorVersion = toolStripMenuAuthorVersion.Checked;
        }

        private void toolStripMenuAutoClean_Click(object sender, EventArgs e)
        {
            sbar($"Changes made: {SyncPlugins().ToString()}");
        }

        private void toolStripMenuAutoDelccc_Click(object sender, EventArgs e)
        {
            toolStripMenuAutoDelccc.Checked = !toolStripMenuAutoDelccc.Checked;
            Properties.Settings.Default.AutoDelccc = toolStripMenuAutoDelccc.Checked;
        }

        private void toolStripMenuBackup_Click(object sender, EventArgs e)
        {
            BackupPlugins();
        }

        private void toolStripMenuBGSStarfield_Click(object sender, EventArgs e)
        {
            Tools.OpenUrl("https://discord.com/channels/784542837596225567/1083043812949110825");
        }

        private void toolStripMenuBGSX_Click(object sender, EventArgs e)
        {
            Tools.OpenUrl("https://x.com/StarfieldGame");
        }

        private void toolStripMenuBottom_Click(object sender, EventArgs e)
        {
            MoveBottom();
        }

        private void toolStripMenuCatalog_Click(object sender, EventArgs e)
        {
            CheckCatalog();
        }

        private void toolStripMenuCleanup_Click(object sender, EventArgs e)
        {
            RemoveMissing();
        }

        private void toolStripMenuCreations_Click(object sender, EventArgs e)
        {
            Tools.OpenUrl($"https://creations.bethesda.net/en/{GameName}/all?platforms=PC&sort=latest_uploaded");
        }

        private void toolStripMenuCreationsID_Click(object sender, EventArgs e)
        {
            toolStripMenuCreationsID.Checked = !toolStripMenuCreationsID.Checked;
            dataGridView1.Columns["CreationsID"].Visible = toolStripMenuCreationsID.Checked;
            Properties.Settings.Default.CreationsID = toolStripMenuCreationsID.Checked;
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

        private void toolStripMenuDelContext_Click(object sender, EventArgs e)
        {
            DeleteLines();
        }

        private void toolStripMenuDelete_Click(object sender, EventArgs e)
        {
            DeleteLines();
        }

        private void toolStripMenuDeleteCCC_Click(object sender, EventArgs e)
        {
            Delccc(true);
        }

        private void toolStripMenuDeleteLine_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.RemoveAt(dataGridView1.CurrentRow.Index);
        }

        private void toolStripMenuDescription_Click(object sender, EventArgs e)
        {
            toolStripMenuDescription.Checked = !toolStripMenuDescription.Checked;
            dataGridView1.Columns["Description"].Visible = toolStripMenuDescription.Checked;
            Properties.Settings.Default.Description = toolStripMenuDescription.Checked;
        }

        private void toolStripMenuDisableAll_Click(object sender, EventArgs e)
        {
            if (Tools.ConfirmAction("This will reset your current load order", "Disable all mods?", MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes || NoWarn)
                DisableAll();
        }

        private void toolStripMenuDown_Click(object sender, EventArgs e)
        {
            MoveDown();
        }

        private void toolStripMenuEditPlugins_Click(object sender, EventArgs e)
        {
            Tools.OpenFile(Path.Combine(Tools.GameAppData, "Plugins.txt"));
        }

        private void toolStripMenuEnableAll_Click(object sender, EventArgs e)
        {
            if (Tools.ConfirmAction("This will reset your current load order", "Enable all mods?", MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes || NoWarn)
                EnableAll();
        }

        private void toolStripMenuEnableDisable_Click(object sender, EventArgs e)
        {
            EnableDisable();
        }

        private void toolStripMenuExploreAppData_Click(object sender, EventArgs e)
        {
            Tools.OpenFolder(Tools.GameAppData);
        }

        private void toolStripMenuExploreCommon_Click(object sender, EventArgs e)
        {
            Tools.OpenFolder(Tools.CommonFolder);
            sbar3("Restart the application for any changes to take effect");
        }

        private void toolStripMenuExploreData_Click(object sender, EventArgs e)
        {
            Tools.OpenFolder(Path.Combine(GamePath, "Data"));
        }

        private void toolStripMenuExploreGameDocs_Click(object sender, EventArgs e)
        {
            Tools.OpenFolder(tools.GameDocuments);
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
                    LogError(ex.Message);
                    MessageBox.Show("Error while exporting data: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                if (ExportedLines == 0)
                {
                    sbar3("Nothing to export");
                    return;
                }
            }
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

            string header = $"# Exported active mod list from hst {GameName} Tools";
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

        private void toolStripMenuFiles_Click(object sender, EventArgs e)
        {
            toolStripMenuFiles.Checked = !toolStripMenuFiles.Checked;
            dataGridView1.Columns["Files"].Visible = toolStripMenuFiles.Checked;
            Properties.Settings.Default.Files = toolStripMenuFiles.Checked;
        }

        private void toolStripMenuFileSize_Click(object sender, EventArgs e)
        {
            toolStripMenuFileSize.Checked = !toolStripMenuFileSize.Checked;
            dataGridView1.Columns["FileSize"].Visible = toolStripMenuFileSize.Checked;
            Properties.Settings.Default.FileSize = toolStripMenuFileSize.Checked;
        }

        private void toolStripMenuGitHub_Click(object sender, EventArgs e)
        {
            Tools.OpenUrl("https://github.com/hst12/Starfield-Tools---ContentCatalog.txt-fixer");
        }

        private void toolStripMenuGroup_Click(object sender, EventArgs e)
        {
            toolStripMenuGroup.Checked = !toolStripMenuGroup.Checked;
            dataGridView1.Columns["Group"].Visible = toolStripMenuGroup.Checked;
            Properties.Settings.Default.Group = toolStripMenuGroup.Checked;
        }

        private void toolStripMenuIndex_Click(object sender, EventArgs e)
        {
            toolStripMenuIndex.Checked = !toolStripMenuIndex.Checked;
            dataGridView1.Columns["Index"].Visible = toolStripMenuIndex.Checked;
            Properties.Settings.Default.Index = toolStripMenuIndex.Checked;
        }

        private void toolStripMenuInstall_Click(object sender, EventArgs e)
        {
            InstallMod();
        }

        private void toolStripMenuInstallMod_Click(object sender, EventArgs e)
        {
            InstallMod();
        }

        private void toolStripMenuItemDeletePlugins_Click(object sender, EventArgs e)
        {
            if (Tools.ConfirmAction("Are you sure you want to delete Plugins.txt?", "This will delete Plugins.txt", MessageBoxButtons.YesNo) == DialogResult.No)
                return;
            File.Delete(Path.Combine(Tools.GameAppData, "Plugins.txt"));
        }

        private void toolStripMenuItemHideAll_Click(object sender, EventArgs e) // Hide all columns in the DataGridView except active status and plugin name
        {
            HideAllColumns();
        }

        private void toolStripMenuLoadingScreen_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp|All Files (*.*)|*.*\"";
            openFileDialog1.FileName = "";
            openFileDialog1.Title = "Choose a loadscreen image";
            DialogResult LoadScreen = openFileDialog1.ShowDialog();
            if (LoadScreen == DialogResult.OK)
            {
                if (openFileDialog1.FileName != "")
                {
                    Properties.Settings.Default.LoadScreenFilename = openFileDialog1.FileName;
                    Properties.Settings.Default.RandomLoadScreen = false;
                    randomToolStripMenuItem.Checked = false;
                    Properties.Settings.Default.LoadScreenSequence = false;
                    sequenceToolStripMenuItem.Checked = false;

                }
            }
        }

        private void toolStripMenuLoadScreenPreview_Click(object sender, EventArgs e)
        {
            ShowSplashScreen();
        }

        private void toolStripMenuLoot_Click(object sender, EventArgs e)
        {
            RunLOOT(true);
        }

        private void toolStripMenuLoot_Click_1(object sender, EventArgs e)
        {
            RunLOOT(false);
        }

        private void toolStripMenuLootAutoSort_Click(object sender, EventArgs e)
        {
            RunLOOT(true);
        }

        private void toolStripMenuLootPath_Click(object sender, EventArgs e)
        {
            SetLOOTPath();
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

        private void toolStripMenuNexus_Click(object sender, EventArgs e)
        {
            Tools.OpenUrl($"https://www.nexusmods.com/games/{GameName}/mods");
        }

        private void toolStripMenuProfilesOn_Click(object sender, EventArgs e)
        {
            ToggleProfiles();
        }

        private void toolStripMenuRefresh_Click(object sender, EventArgs e)
        {
            RefreshDataGrid();
        }

        private void toolStripMenuResetStarfieldCustom_Click(object sender, EventArgs e)
        {
            ResetGameCustomINI(true);
        }

        private void toolStripMenuResetWindow_Click(object sender, EventArgs e)
        {
            ResetWindowSize();
        }

        private void toolStripMenuRestore_Click(object sender, EventArgs e)
        {
            RestorePlugins();
        }

        private void toolStripMenuRun_Click(object sender, EventArgs e)
        {
            RunGame();
        }

        private void toolStripMenuRunCustom_Click(object sender, EventArgs e)
        {
            RunGame();
        }

        private void toolStripMenuRunMS_Click(object sender, EventArgs e)
        {
            RunGame();
        }

        private void toolStripMenuScanMods_Click(object sender, EventArgs e)
        {
            AddMissing();
        }

        private void toolStripMenuShortcuts_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("explorer.exe", Path.Combine(Tools.DocumentationFolder, "Shortcuts.txt"));
            }
            catch (Exception ex)
            {
                LogError("Error opening Shortcuts.txt " + ex.Message);
                MessageBox.Show(ex.Message, "Error opening Shortcuts.txt");
            }
        }

        private void toolStripMenuShowRecommended_Click(object sender, EventArgs e)
        {
            ShowRecommendedColumns();
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

        private void toolStripMenuTop_Click(object sender, EventArgs e)
        {
            MoveTop();
        }

        private void toolStripMenuUninstall_Click(object sender, EventArgs e)
        {
            UninstallMod();
        }

        private void toolStripMenuUninstallContext_Click(object sender, EventArgs e)
        {
            UninstallMod();
        }

        private void toolStripMenuUp_Click(object sender, EventArgs e)
        {
            MoveUp();
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

        private void txtSearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.F3)
                SearchMod();
        }

        private void uIToEditStarfieldCustominiToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmStarfieldCustomINI fci = new();
            fci.ShowDialog();
            string PluginsPath = Path.Combine(Tools.GameAppData, "Plugins.txt"),
        LooseFilesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", GameName), // Check if loose files are enabled
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
                LogError(ex.Message);
            }
            if (LooseFiles)
                sbar("Loose Files Enabled");
            else
                sbar("Loose Files Disabled");
            looseFilesDisabledToolStripMenuItem.Checked = LooseFiles;
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

            string FolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", Tools.GameName);
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
            ChangeCount += CheckAndDeleteINI($"{GameName}.ini");
            ChangeCount += CheckAndDeleteINI($"{GameName}.ini.baked");
            ChangeCount += CheckAndDeleteINI($"{GameName}Custom.ini.baked");
            ChangeCount += CheckAndDeleteINI($"{GameName}Prefs.ini.baked");
            ChangeCount += CheckAndDeleteINI($"{GameName}.ini.base");
            LooseFiles = false;
            LooseFilesOnOff(false);
            LooseFilesMenuUpdate();
            if (Delccc())
                ChangeCount++;
            if (ChangeCount > 0)
            {
                sbar3(ChangeCount + " Change(s) made to Vortex created files");
                activityLog.WriteLog($"{ChangeCount} Vortex changes undone");
            }
            return ChangeCount;
        }

        private void undoVortexChangesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UndoVortexChanges(true);
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
            string dataDirectory = Path.Combine(GamePath, "Data");

            var dlg = DialogResult.None;
            foreach (var row in selectedRows)
            {
                // Get the mod name from the PluginName cell (before the first dot).
                string pluginName = row.Cells["PluginName"].Value?.ToString() ?? string.Empty;
                int dotIndex = pluginName.LastIndexOf('.');
                if (dotIndex < 0)
                    continue;

                string modName = pluginName.Substring(0, dotIndex);

                activityLog.WriteLog($"Starting uninstall for mod: {pluginName}");
                dlg = Tools.ConfirmAction(
                        $"This will delete all files related to the '{pluginName}' mod",
                        $"Delete {pluginName} - Are you sure?",
                        MessageBoxButtons.YesNoCancel);
                if (dlg == DialogResult.Yes || NoWarn)
                {
                    isModified = true;
                    dataGridView1.Rows.Remove(row);

                    // Build the base file path.
                    string modBasePath = Path.Combine(dataDirectory, modName);

                    /*string[] patterns = { ".esp",".esl" }; // Delete .esp and .esl files
                    foreach (var pattern in patterns)
                    {
                        string modFile = modBasePath + pattern;
                        if (File.Exists(modFile))
                        {
                            File.Delete(modFile);
                            activityLog.WriteLog($"Deleted: {modFile}");
                            SavePlugins();
                            sbar3($"{modFile} uninstalled");
                            continue;
                        }
                    }*/
                    if (pluginName.EndsWith(".esp"))
                    {
                        string modFile = modBasePath + Path.GetExtension(pluginName);
                        if (File.Exists(modFile))
                        {
                            File.Delete(modFile);
                            activityLog.WriteLog($"Deleted: {modFile}");
                            SavePlugins();
                            sbar3($"{modFile} uninstalled");
                            return;
                        }
                    }
                    // Define the file extensions for the mod files to delete.
                    var extensions = new string[]
                    {
                ".esm",
                " - textures.ba2",
                " - Textures_xbox.ba2",
                " - main_xbox.ba2",
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
                            activityLog.WriteLog($"Deleted: {filePath}");
                        }
                    }

                    // match files like 'modname - textures.ba2', 'modname - textures01.ba2', 'modname - textures02.ba2', etc.
                    string directoryPath = GamePath + "\\Data\\";
                    string[] textureFiles = Directory.GetFiles(directoryPath, modName + "* - textures*.ba2");

                    foreach (string file in textureFiles)
                    {
                        File.Delete(file);
                        activityLog.WriteLog($"Deleted: {file}");
                    }

                    // match files like 'modname - main.ba2', 'modname - main01.ba2', 'modname - main02.ba2', etc.
                    string[] mainFiles = Directory.GetFiles(directoryPath, modName + "* - main*.ba2");

                    foreach (string file in mainFiles)
                    {
                        File.Delete(file);
                        activityLog.WriteLog($"Deleted: {file}");
                    }

                    SavePlugins();
                    sbar3($"Mod '{modName}' uninstalled.");
                }
                else
                {
                    sbar2($"Un-install of '{pluginName}' cancelled.");
                    activityLog.WriteLog($"Un-install of {pluginName} cancelled");
                    if (dlg == DialogResult.Cancel)
                        return;
                }
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

            if (profiles.Length == 0 || activeProfile is null)
            {
                MessageBox.Show("No valid profiles found");
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
            activityLog.WriteLog($"UpdateAllProfiles – total changes: {totalChanges}");
            progressBar1.Hide();
        }

        private void updateAllProfilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateAllProfiles();
        }

        private async void updateArchivedModsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await UpdateArchiveModsAsync();
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

        private async Task UpdateBackupAsync(CancellationToken token)
        {
            List<string> files = new();
            int modsArchived = 0;

            if (!CheckGamePath()) // Abort if game path not set
                return;

            string directoryPath = Path.Combine(GamePath, "Data");

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

                    foreach (var textureFile in Directory.EnumerateFiles(directoryPath, ModName + " - textures*.ba2"))
                    {
                        if (File.Exists(textureFile))
                            files.Add(textureFile);
                    }

                    foreach (var mainFile in Directory.EnumerateFiles(directoryPath, ModName + " - main*.ba2"))
                    {
                        if (File.Exists(mainFile))
                            files.Add(mainFile);
                    }

                    if (File.Exists(ModFile + " - voices_en.ba2"))
                        files.Add(ModFile + " - voices_en.ba2");

                    string zipPath = Path.Combine(selectedFolderPath, ModName) + ".zip"; // Choose path to Zip it

                    // Check if archive already exists, bail out on user cancel
                    if (!File.Exists(zipPath))
                    {
                        sbar($"Creating archive for {ModName}...");
                        statusStrip1.Refresh();
                        activityLog.WriteLog($"Creating archive for {ModName} at {zipPath}");
                        CreateZipFromFiles(files, zipPath); // Make zip
                        sbar($"{ModName} archived");
                        statusStrip1.Refresh();
                        modsArchived++;
                    }
                    files.Clear();
                }
                sbar(modsArchived + " Mod(s) archived");
                activityLog.WriteLog($"{modsArchived} mods archived to {selectedFolderPath}");
            }
        }

        private void UpdateGameVersion() // Display game version
        {
            Properties.Settings.Default.GameVersion = GameVersion;
            if (Properties.Settings.Default.GamePath == "")
            {
                GamePath = tools.SetGamePath();
                if (GameVersion != MS)
                {
                    Properties.Settings.Default.GamePath = GamePath;
                }
                else
                {
                    Properties.Settings.Default.GamePathMS = GamePath;
                }
            }

            if (Properties.Settings.Default.GamePathMS == "" && GameVersion == MS)
            {
                GamePath = tools.SetGamePathMS();
                Properties.Settings.Default.GamePathMS = GamePath;
                SaveSettings();
            }

            SaveSettings();
            if (GameVersion != MS)
                GamePath = Properties.Settings.Default.GamePath;
            else
            {
                GamePath = Properties.Settings.Default.GamePathMS;
                RefreshDataGrid();
            }
            activityLog.WriteLog($"Game version set to {GameVersion}");
            GameVersionDisplay();
        }

        private void UpdatePlugins()
        {
            int changes = SyncPlugins();
            if (AutoSort && changes > 0)
                RunLOOT(true);

            sbar($"Changes made: {changes}");
            activityLog.WriteLog($"Update check. Changes made: {changes}");
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

        private void videoLoadscreenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            /*frmSplashScreenVideo ssVideo = new();
            ssVideo.Show();*/
        }

        private void viewLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowLog();
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
                        activityLog.WriteLog("Starting Vortex");
                        System.Windows.Forms.Application.Exit();
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex.Message);
                    MessageBox.Show(ex.Message);
                }
            }
            else
                MessageBox.Show("Vortex doesn't seem to be installed.");
        }

        private void webPageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Tools.OpenUrl("https://www.nexusmods.com/starfield/mods/10432?tab=files");
        }

        private void xEditToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string xEditPath = Properties.Settings.Default.xEditPath;
            if (xEditPath != "")
            {
                try
                {
                    var result = Process.Start(xEditPath);
                    if (result != null)
                    {
                        SaveSettings();
                        activityLog.WriteLog("Starting xEdit");
                        System.Windows.Forms.Application.Exit();
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex.Message);
                    MessageBox.Show(ex.Message);
                }
            }
            else
                MessageBox.Show("xEdit doesn't seem to be installed or path not configured.");
        }

        public class ActivityLog : IDisposable
        {
            private readonly MemoryStream memoryStream;
            private readonly StreamWriter writer;

            public ActivityLog()
            {
                memoryStream = new MemoryStream();
                writer = new StreamWriter(memoryStream, Encoding.UTF8, 1024, true);
                WriteLog("Starting log in memory");
            }

            public System.Windows.Forms.RichTextBox LogRichTextBox { get; set; } // assign externally

            public void DeleteLog()
            {
                /*try
                {
                    memoryStream.SetLength(0);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting log: {ex.Message}");
                }*/
                try
                {
                    string logFilePath = Path.Combine(string.IsNullOrEmpty(Properties.Settings.Default.LogFileDirectory)
                        ? Tools.LocalAppDataPath : Properties.Settings.Default.LogFileDirectory, "Activity Log.txt");
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
                writer?.Dispose();
                memoryStream?.Dispose();
            }

            public void LoadLog(string filePath)
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        // Clear current memory stream
                        memoryStream.SetLength(0);

                        // Copy file contents into memory
                        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                        {
                            fileStream.CopyTo(memoryStream);
                        }

                        // Reset position for future reads/writes
                        memoryStream.Position = memoryStream.Length;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading log file: {ex.Message}");
                }
            }

            public void PersistLog(string filePath)
            {
                try
                {
                    writer.Flush();
                    memoryStream.Position = 0;

                    using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        memoryStream.CopyTo(fileStream);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error persisting log: {ex.Message}");
                }
            }

            public string ReadLog()
            {
                try
                {
                    writer.Flush();
                    memoryStream.Position = 0;
                    using (var reader = new StreamReader(memoryStream, Encoding.UTF8, true, 1024, true))
                    {
                        return reader.ReadToEnd();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error reading log: {ex.Message}");
                    return string.Empty;
                }
            }

            public void WriteLog(string message)
            {
                if (!log)
                    return;
                try
                {
                    // Update log row if enabled
                    if (Properties.Settings.Default.LogWindow && LogRichTextBox is not null)
                    {
                        LogRichTextBox.AppendText($"{DateTime.Now.ToString("dd MMM yyyy h:mm:ss tt")}: {message}\n");
                        LogRichTextBox.ScrollToCaret();
                    }

                    // Prepend new entry at the top (requires reordering)
                    string existing = ReadLog();
                    string newEntry = $"{DateTime.Now.ToString("dd MMM yyyy h:mm:ss tt")}: {message}\n{existing}";

                    // Reset memory stream and rewrite
                    memoryStream.SetLength(0);
                    writer.BaseStream.Position = 0;
                    writer.Write(newEntry);
                    writer.Flush();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error writing to log: {ex.Message}");
                }
            }
        }

        public class AppSettings
        {
            public bool EnableNotifications { get; set; }
            public int RefreshInterval { get; set; }
            public string Username { get; set; }
        }

        private void rowHighlightToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.RowHighlight = rowHighlightToolStripMenuItem.Checked = !rowHighlightToolStripMenuItem.Checked;
            RefreshDataGrid();
        }

        private void ResetGroupsButton()
        {
            btnGroups.Font = new System.Drawing.Font(btnGroups.Font, FontStyle.Regular);
        }

        private void btnGroups_Click(object sender, EventArgs e)
        {
            string groupName = "";
            List<string> groupList = new(), groupFilter = new();
            foreach (var itemn in Groups.groups)
                groupList.Add(itemn.name);
            groupList.Sort();
            using frmFilterGroups filterGroups = new(groupList);
            {
                if (filterGroups.ShowDialog() == DialogResult.OK)
                {
                    groupFilter = filterGroups.GetSelectedGroups();
                    foreach (DataGridViewRow row in dataGridView1.Rows)
                    {
                        groupName = row.Cells["Group"].Value as string ?? "";
                        if (groupFilter.Contains(groupName))
                            row.Visible = true;
                        else
                            row.Visible = false;
                    }
                    if (groupFilter.Count > 0)
                        btnGroups.Font = new System.Drawing.Font(btnGroups.Font, FontStyle.Bold);
                    else
                        ResetGroupsButton();
                }
            }
            if (groupList.Count == groupFilter.Count)
                ResetGroupsButton();
            else
                sbar("Group filter applied. Refresh to clear");
        }

        private void toolStripMenuDuplicateRename_Click(object sender, EventArgs e)
        {
            if (!CheckGamePath()) // Abort if game path not set
                return;

            List<string> files = new();
            if (!CheckGamePath()) // Abort if game path not set
                return;

            string directoryPath = Path.Combine(GamePath, "Data");
            var row = dataGridView1.CurrentRow;
            string ModName = row.Cells["PluginName"].Value.ToString();
            ModName = ModName[..ModName.LastIndexOf('.')]; // Strip extension
            string ModFile = Path.Combine(directoryPath, ModName);

            // Collect existing mod-related files
            string[] fixedExtensions = { ".esp", ".esm", " - voices_en.ba2" };
            foreach (var ext in fixedExtensions)
            {
                string fullPath = ModFile + ext;
                if (File.Exists(fullPath))
                    files.Add(fullPath);
            }

            // Handle texture files like " - textures*.ba2"
            string pattern = ModName + " - textures*.ba2";

            string[] matchedFiles = Directory.GetFiles(directoryPath, Path.GetFileName(pattern));
            files.AddRange(matchedFiles);

            // Handle texture files like " - main*.ba2"
            pattern = ModName + " - main*.ba2";
            matchedFiles = Directory.GetFiles(directoryPath, Path.GetFileName(pattern));
            files.AddRange(matchedFiles);

            string userInput = Interaction.InputBox("New Name:", "Duplicate Mod", ModName + " (2)");
            if (string.IsNullOrWhiteSpace(userInput) || userInput == ModName)
                return;
            userInput = Path.GetFileNameWithoutExtension(userInput); // Remove any extension from user input

            // Copy each file
            foreach (var oldPath in files)
            {
                string extensionPart = oldPath.Substring(ModFile.Length); // Get suffix like ".esp" or " - textures01.ba2"
                string newPath = Path.Combine(directoryPath, userInput + extensionPart);

                try
                {
                    File.Copy(oldPath, newPath);
                    activityLog.WriteLog($"Copied: {Path.GetFileName(oldPath)} to {Path.GetFileName(newPath)}");
                }
                catch (Exception ex)
                {
                    LogError(ex.Message);
                    MessageBox.Show($"Failed to copy {oldPath} to {newPath}:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            SyncPlugins();
            sbar($"Mod {ModName} copied to: {userInput}");
        }

        private void testToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            List<string> pluginSizes = Directory.GetFiles(Path.Combine(GamePath, "Data"))
                .Where(f => f.EndsWith(".esm", StringComparison.OrdinalIgnoreCase))
                .Select(f => new FileInfo(f))
                .Where(fi => fi.Length == 77)
                .Select(fi => $"{fi.Name} - {fi.Length} bytes")
                .ToList();

            frmGenericTextList displayList = new("Plugin Sizes", pluginSizes);
            displayList.Show();
        }

        private void sFSEPluginsEnableDisableToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmSFSEPlugins fsp = new();
            fsp.ShowDialog();
        }

        private void randomToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.RandomLoadScreen = randomToolStripMenuItem.Checked = !randomToolStripMenuItem.Checked;
            Properties.Settings.Default.LoadScreenSequence = false;
            sequenceToolStripMenuItem.Checked = false;
        }

        private void sequenceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.LoadScreenSequence = sequenceToolStripMenuItem.Checked = !sequenceToolStripMenuItem.Checked;
            Properties.Settings.Default.RandomLoadScreen = false;
            randomToolStripMenuItem.Checked = false;
        }

        private void blockedOnlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool isBlocked;

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                isBlocked = row.Cells["Blocked"].Value as bool? ?? false;
                row.Visible = isBlocked;
            }

            if (resizeToolStripMenuItem.Checked)
                ResizeForm();
        }
    }
}