using Newtonsoft.Json;
using Starfield_Tools.Common;
using Starfield_Tools.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Starfield_Tools
{
    public partial class frmStarfieldTools : Form
    {
        public bool AutoCheck, AutoClean, AutoBackup, AutoRestore, ForceClean, Verbose, log;
        public string CatalogStatus;

        private readonly string StarfieldGamePath;
        private readonly Tools tools = new();
        private frmLoadOrder.ActivityLog activityLog = frmLoadOrder.activityLog;

        public frmStarfieldTools()
        {
            InitializeComponent();

            Tools.CheckGame();

            // Retrieve settings
            AutoCheck = Properties.Settings.Default.AutoCheck;
            AutoClean = Properties.Settings.Default.AutoClean;
            AutoBackup = Properties.Settings.Default.AutoBackup;
            StarfieldGamePath = Properties.Settings.Default.StarfieldGamePath;
            Verbose = Properties.Settings.Default.Verbose;
            chkVerbose.Checked = Properties.Settings.Default.Verbose;
            AutoRestore = Properties.Settings.Default.AutoRestore;
            ForceClean = Properties.Settings.Default.ForceClean;
            log = Properties.Settings.Default.Log;
            SetAutoCheckBoxes();

            richTextBox2.Text = "";

            if (AutoCheck) // Check catalog status if enabled
            {
                if (!CheckCatalog()) // If not okay, then...
                {
                    richTextBox2.Text += "\nCatalog issues(s) found\n";
                    if (AutoRestore) // Restore backup file if auto restore is on
                    {
                        if (RestoreCatalog())
                            toolStripStatusLabel1.Text = "Catalog backup restored";
                    }
                    else
                    if (AutoClean)
                        CleanCatalog();
                }
                else
                    toolStripStatusLabel1.Text = "Catalog ok";
            }
            else toolStripStatusLabel1.Text = "Ready";
            ScrollToEnd();

            if (AutoRestore)
            {
                RestoreCatalog();
                CatalogStatus = "Catalog restored";
            }

            if (AutoBackup)
                if (!CheckBackup()) // Backup if necessary
                    BackupCatalog();

            if (!File.Exists(Tools.GetCatalogPath() + ".bak")) // Backup catalog if backup doesn't exist
                BackupCatalog();

            DisplayCatalog();
        }

        public static string GetStarfieldAppData()
        {
            return (Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)) + @"\Starfield";
        }

        public bool BackupCatalog()
        {
            bool BackupStatus = false;

            if (!CheckCatalog())
            {
                richTextBox2.Text += "\nCatalog is corrupted. Backup not made.\n";
                if (AutoClean)
                    CleanCatalog();
                return false;
            }

            if (!CheckBackup())
            {
                string sourceFileName = Tools.GetCatalogPath();
                string destFileName = sourceFileName + ".bak";

                try
                {
                    File.Copy(sourceFileName, destFileName, true); // overwrite
                    richTextBox2.Text += "\nBackup done\n";
                    toolStripStatusLabel1.Text = "Backup done";
                    if (log)
                        activityLog.WriteLog("Catalog Backup done");
                    BackupStatus = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Catalog Backup failed");
                    if (log)
                        activityLog.WriteLog($"Error: {ex.Message} Backup failed");
                }
            }
            else
                BackupStatus = false;
            return BackupStatus;
        }

        public void SaveSettings()  // Save user settings
        {
            Settings.Default.AutoCheck = AutoCheck;
            Settings.Default.AutoClean = AutoClean;
            Settings.Default.AutoBackup = AutoBackup;
            Settings.Default.AutoRestore = AutoRestore;
            if (StarfieldGamePath != "")
                Settings.Default.StarfieldGamePath = StarfieldGamePath;
            Settings.Default.ForceClean = ForceClean;
            Settings.Default.Verbose = Verbose;
            Settings.Default.Save();
        }

        private void btnAchievemnts_Click(object sender, EventArgs e)
        {
            if (Tools.ConfirmAction("Do you want to continue?", "Experimental Feature - All achievement flags will be set. ", MessageBoxButtons.YesNo) == DialogResult.No)
            {
                toolStripStatusLabel1.Text = "Achievement flags not reset";
                return;
            }

            string jsonFilePath = Tools.GetCatalogPath(), json = File.ReadAllText(jsonFilePath);
            var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, Tools.Creation>>(json);

            foreach (var kvp in data)
            {
                kvp.Value.AchievementSafe = true;  // set Achievement flag
            }

            data.Remove("ContentCatalog"); // remove messed up content catalog section

            json = Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);

            // Hack the Bethesda header back in
            json = Tools.MakeHeader() + json[1..];

            File.WriteAllText(Tools.GetCatalogPath(), json); // Write updated catalog
            DisplayCatalog();
            toolStripStatusLabel1.Text = "Achievement flags set";
        }

        private void btnBackup_Click(object sender, EventArgs e)
        {
            BackupCatalog();
            ScrollToEnd();
            DisplayCatalog();
        }

        private void btnCheck_Click(object sender, EventArgs e)
        {
            CheckCatalog();
            ScrollToEnd();
            DisplayCatalog();
        }

        private void btnClearLog_Click(object sender, EventArgs e)
        {
            richTextBox2.Text = "";
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (Tools.ConfirmAction("Are you Sure?", "Delete ContentCatalog.txt", MessageBoxButtons.YesNo) == DialogResult.Yes)
                File.Delete(Tools.GetCatalogPath());
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            string pathToFile = Tools.GetCatalogPath();
            Process.Start("explorer", pathToFile);
        }

        private void btnQuit_Click(object sender, EventArgs e)
        {
            SaveSettings();
            this.Close();
        }

        private void btnResetAll_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Do you want to continue?", "All version numbers will be reset. This will force all Creations to re-download", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
            if (result != DialogResult.OK)
            {
                toolStripStatusLabel1.Text = "Version numbers not reset";
                return;
            }

            string jsonFilePath = Tools.GetCatalogPath();

            string json = File.ReadAllText(jsonFilePath);

            var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, Tools.Creation>>(json);

            foreach (var kvp in data)
            {
                kvp.Value.Version = "1704067200.0"; // set version to 1704067200.0
            }

            data.Remove("ContentCatalog"); // remove messed up content catalog section

            json = Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);

            // Hack the Bethesda header back in
            json = Tools.MakeHeader() + json[1..];

            File.WriteAllText(Tools.GetCatalogPath(), json); // Write updated catalog
            DisplayCatalog();
            toolStripStatusLabel1.Text = "Version numbers reset";
        }

        private void btnRestore_Click(object sender, EventArgs e)
        {
            RestoreCatalog();
            ScrollToEnd();
            DisplayCatalog();
        }

        private bool CheckBackup()
        {
            string fileName1 = Tools.GetCatalogPath();
            string fileName2 = fileName1 + ".bak";

            if (Tools.FileCompare(fileName1, fileName2))
            {
                richTextBox2.Text += "\nBackup is up to date.\n";
                ScrollToEnd();
                return true;
            }
            else
            {
                richTextBox2.Text += "\nBackup is out of date.\n";
                ScrollToEnd();
                return false;
            }
        }

        private bool CheckCatalog() // returns true if catalog is good
        {
            toolStripStatusLabel1.Text = "Checking...";
            richTextBox1.Clear();
            richTextBox2.Clear();
            richTextBox2.AppendText("Checking Catalog\n");

            int errorCount = 0;
            int warningCount = 0;
            string catalogPath = Tools.GetCatalogPath();

            try
            {
                if (string.IsNullOrEmpty(catalogPath))
                {
                    string message = "Start the game and enter the Creations menu or load a save to create a catalog file";
                    toolStripStatusLabel1.Text = message;
                    richTextBox2.Text = message;
                    return false;
                }

                string json = File.ReadAllText(catalogPath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    toolStripStatusLabel1.Text = "Catalog file is empty, nothing to check";
                    return false;
                }

                // Deserialize the JSON but remove the header
                var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, Tools.Creation>>(json);
                data?.Remove("ContentCatalog");

                foreach (var kvp in data)
                {
                    Tools.Creation creation = kvp.Value;

                    // Check each file for an ".esp" entry
                    if (creation.Files != null)
                    {
                        foreach (string file in creation.Files)
                        {
                            if (file.IndexOf(".esp", StringComparison.OrdinalIgnoreCase) > 0)
                            {
                                richTextBox2.AppendText($"\nWarning - esp file found in catalog file - {file}\n");
                                if (log)
                                    activityLog.WriteLog($"Warning - esp file found in catalog file - {file}");

                                warningCount++;
                            }
                        }
                    }

                    string versionStr = creation.Version;
                    int dotIndex = versionStr.IndexOf('.');
                    double versionCheck = dotIndex > 0 ? double.Parse(versionStr[..dotIndex]) : 0;

                    // If the version string does not match the expected header and Verbose logging is enabled, log details.
                    if (versionStr != Tools.CatalogVersion && Verbose)
                    {
                        string versionDetail = (dotIndex >= 0 && dotIndex < versionStr.Length - 1)
                            ? versionStr[(dotIndex + 1)..]
                            : "";
                        richTextBox2.AppendText($"{creation.Title}, date: {Tools.ConvertTime(versionCheck)} version: {versionDetail}\n");
                    }

                    // If the numeric part of the version is out of range (and not equal to 1), log an error.
                    if (versionCheck > creation.Timestamp && !versionCheck.Equals(1))
                    {
                        errorCount++;
                        richTextBox2.AppendText($"Out of range version number detected in {creation.Title}: {versionStr}, {Tools.ConvertTime(versionCheck)}\n");
                        if (log)
                            activityLog.WriteLog($"Out of range version number detected in {creation.Title}: {versionStr}, {Tools.ConvertTime(versionCheck)}");
                    }

                    // Check the entire version string for invalid characters (anything other than letters, digits, or '.')
                    foreach (char c in versionStr)
                    {
                        if (!char.IsLetterOrDigit(c) && c != '.' && c!='\\' && c!=' ')
                        {
                            errorCount++;
                            richTextBox2.AppendText($"Non numeric version number detected in {creation.Title} - {c}\n");
                            if (log)
                                activityLog.WriteLog($"Non numeric version number detected in {creation.Title} - {c}");
                            break;
                        }
                    }
                }

                // Reporting based on error and warning counts
                if (errorCount == 0)
                {
                    if (warningCount == 0)
                    {
                        toolStripStatusLabel1.Text = "Catalog OK";
                        CatalogStatus = toolStripStatusLabel1.Text;
                        richTextBox2.AppendText("\nCatalog OK\n");
                        if (log)
                            activityLog.WriteLog("Catalog OK");
                        return true;
                    }
                    else
                    {
                        toolStripStatusLabel1.Text = $"{warningCount} Warning(s) Press the Catalog button for details";
                        CatalogStatus = toolStripStatusLabel1.Text;
                        return true;
                    }
                }
                else
                {
                    toolStripStatusLabel1.Text = $"{errorCount} Error(s) found";
                    CatalogStatus = toolStripStatusLabel1.Text;
                    richTextBox2.AppendText($"{errorCount} Error(s) found\n");
                    if (log)
                        activityLog.WriteLog($"{errorCount} Error(s) found in catalog file");
                    return false;
                }
            }
            catch (Exception ex)
            {
                // If the catalog file is missing create a dummy one.
                if (!File.Exists(catalogPath))
                {
                    Tools.ConfirmAction("Missing ContentCatalog.txt", "A Blank ContentCatalog.txt file will be created", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    File.WriteAllText(catalogPath, Tools.MakeHeaderBlank());
                    toolStripStatusLabel1.Text = "Dummy ContentCatalog.txt created";
                    if (log)
                        activityLog.WriteLog("Dummy ContentCatalog.txt created");
                    return false;
                }
                else
                {
                    richTextBox2.AppendText("\n" + ex.Message);
                    toolStripStatusLabel1.Text = "Catalog corrupt. Use the Restore or Clean functions to repair";
                    if (log)
                        activityLog.WriteLog("Catalog corrupt. Use the Restore or Clean functions to repair");
                }
                return false;
            }
        }

        private void chkAutoBackup_CheckedChanged(object sender, EventArgs e)
        {
            AutoBackup = chkAutoBackup.Checked;
        }

        private void chkAutoCheck_CheckedChanged(object sender, EventArgs e)
        {
            AutoCheck = chkAutoCheck.Checked;
        }

        private void chkAutoClean_CheckedChanged(object sender, EventArgs e)
        {
            AutoClean = chkAutoClean.Checked;
        }

        private void chkAutoRestore_CheckedChanged(object sender, EventArgs e)
        {
            AutoRestore = chkAutoRestore.Checked;
            Properties.Settings.Default.AutoRestore = AutoRestore;
        }

        private void chkForceClean_CheckedChanged(object sender, EventArgs e)
        {
            ForceClean = chkForceClean.Checked;
        }

        private void chkVerbose_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.Verbose = chkVerbose.Checked;
            Verbose = chkVerbose.Checked;
        }

        private void CleanCatalog()
        {
            if (!File.Exists(Tools.GetCatalogPath()))
                File.WriteAllText(Tools.GetCatalogPath(), string.Empty); // Create dummy catalog
            else
            {
                long fileSize = new FileInfo(Tools.GetCatalogPath()).Length;

                if (fileSize > 0)
                {
                    FixCatalog();

                    if (AutoBackup)
                        BackupCatalog();
                    DisplayCatalog();
                }
            }
        }

        private void cmdClean_Click(object sender, EventArgs e)
        {
            if (!CheckCatalog() || ForceClean)
            {
                CleanCatalog();
                //toolStripStatusLabel1.Text = errorCount.ToString() + " Errors found";
            }
            else
            {
                richTextBox2.Text += "Cleaning not needed\n";
                ScrollToEnd();
                toolStripStatusLabel1.Text = "Catalog ok. Cleaning not needed.";
                DisplayCatalog();
            }
        }

        private void cmdDeleteStale_Click(object sender, EventArgs e)
        {
            RemoveDeleteddEntries();
            if (AutoBackup)
                if (!CheckBackup()) // Backup if necessary
                    BackupCatalog();
            DisplayCatalog();
            ScrollToEnd();
        }

        private void DisplayCatalog()
        {
            try
            {
                richTextBox1.Text = File.ReadAllText(Tools.GetCatalogPath());
            }
            catch (Exception ex)
            {
                toolStripStatusLabel1.Text = $"{ex.Message} Catalog not found";
            }
        }

        private void FixCatalog()
        {
            string jsonFilePath = Tools.GetCatalogPath();
            string json = File.ReadAllText(jsonFilePath); // Load catalog

            if (string.IsNullOrWhiteSpace(json))
            {
                toolStripStatusLabel1.Text = "Catalog file is empty, nothing to clean";
                return;
            }

            int errorCount = 0;
            int versionReplacementCount = 0;

            // Deserialize the catalog into a dictionary.
            var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, Tools.Creation>>(json);

            foreach (var kvp in data)
            {
                string versionStr = kvp.Value.Version;
                if (Verbose)
                    richTextBox2.AppendText($"Checking {kvp.Value.Title}, {versionStr}\n");

                // Check for any invalid characters in the version (allows letters, digits, or '.').
                bool fixVersion = versionStr.Any(ch => !char.IsLetterOrDigit(ch) && ch != '.');

                // If the version does not match the header, perform the timestamp check.
                if (versionStr != Tools.CatalogVersion)
                {
                    int dotIndex = versionStr.IndexOf('.');
                    if (dotIndex > 0)
                    {
                        double versionCheck = double.Parse(versionStr[..dotIndex]);
                        long timeStamp = kvp.Value.Timestamp;
                        if (versionCheck > timeStamp)
                        {
                            richTextBox2.AppendText($"Replacing version no for {kvp.Value.Title}\n");
                            kvp.Value.Version = "1704067200.0";
                            versionReplacementCount++;
                        }
                    }
                }

                // If invalid characters were found, correct the version string.
                if (fixVersion)
                {
                    richTextBox2.AppendText($"Invalid characters in {kvp.Value.Title}\n");
                    kvp.Value.Version = "1704067200.0"; // set version to default
                    errorCount++;
                }
            }

            // Remove any corrupted catalog header before re-serializing.
            data.Remove("ContentCatalog");

            // Serialize the catalog using Newtonsoft.Json for indented formatting.
            json = Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
            if (json == "{}")
            {
                toolStripStatusLabel1.Text = "Catalog is empty";
                return;
            }

            // Re-insert the header (skipping the first '{' character from the JSON string).
            json = Tools.MakeHeader() + json.Substring(1);
            File.WriteAllText(jsonFilePath, json);

            toolStripStatusLabel1.Text = $"{versionReplacementCount} Version replacements";
            ScrollToEnd();
        }

        private void frmStarfieldTools_Shown(object sender, EventArgs e)
        {
            this.Focus();
        }

        private void RemoveDeleteddEntries() // Remove left over entries from catalog
        {
            List<string> esmFiles = [];
            string jsonFilePath = Tools.GetCatalogPath();
            string json = File.ReadAllText(jsonFilePath); // Read catalog
            List<string> CreationsPlugin = []; // filename of .esm
            List<string> CreationsTitle = []; // Display title for .esm
            List<string> CreationsGUID = []; // Creations GUID
            int RemovalCount = 0;
            int index;
            bool unusedMods = false;
            richTextBox2.Text += "\nChecking for unused items in catalog...\n";
            if (log)
                activityLog.WriteLog("Checking for unused items in catalog.");

            string filePath = Path.Combine(GetStarfieldAppData(), "Plugins.txt");
            //string fileContent = File.ReadAllText(filePath); // Load Plugins.txt

            // Split the content into lines
            //List<string> lines = [.. fileContent.Split('\n')];
            List<string> lines = File.ReadLines(filePath)
                                     .Select(line => line.Trim())
                                     .ToList();

            foreach (var file in lines) // Process Plugins.txt to a list of .esm files
            {
                if (file != "")
                {
                    if (file[0] != '#') // skip the comments
                        if (file[0] == '*') // Make a list of .esm files
                            esmFiles.Add(file[1..]); // strip any *
                        else
                            esmFiles.Add(file);
                }
            }
            for (int i = 0; i < esmFiles.Count; i++)
            {
                esmFiles[i] = esmFiles[i].Trim();
            }

            dynamic json_Dictionary = JsonConvert.DeserializeObject<dynamic>(json);
            try
            {
                var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, Tools.Creation>>(json); // Process catalog
                data.Remove("ContentCatalog"); // Remove header

                foreach (var kvp in data)
                {
                    try
                    {
                        for (int i = 0; i < kvp.Value.Files.Length; i++)
                        {
                            if (kvp.Value.Files[i].ToLower().IndexOf(".esm") > 0 || kvp.Value.Files[i].ToLower().IndexOf(".esp") > 0) // Look for .esm or .esp files
                            {
                                CreationsPlugin.Add(kvp.Value.Files[i]);
                                CreationsGUID.Add(kvp.Key);
                                CreationsTitle.Add(kvp.Value.Title);
                                if (Verbose)
                                    richTextBox2.Text += kvp.Value.Title + "\n";
                            }
                            if (kvp.Value.Files[i].ToLower().IndexOf(".esp") > 0)
                            {
                                richTextBox2.Text += "\nWarning - esp file found in catalog file - " + kvp.Value.Files[i] + "\n";
                                if (log)
                                    activityLog.WriteLog($"Warning - esp file found in catalog file - {kvp.Value.Files[i]}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error: {ex.Message}");
                    }
                }

                List<string> missingStrings = CreationsPlugin.Except(esmFiles, StringComparer.OrdinalIgnoreCase).ToList();
                richTextBox1.Text = "";
                index = 0;

                if (missingStrings.Count > 0)
                {
                    for (index = 0; index < missingStrings.Count; index++)
                    {
                        for (int i = 0; i < CreationsGUID.Count; i++)
                        {
                            if (CreationsPlugin[i].ToLower() == missingStrings[index].ToLower())
                            {
                                richTextBox2.Text += "Removing " + CreationsGUID[i] + " " + CreationsTitle[i] + "\n";
                                if (log)
                                    activityLog.WriteLog($"Removing {CreationsGUID[i]} {CreationsTitle[i]}");
                                data.Remove(CreationsGUID[i]);
                                unusedMods = true;
                                RemovalCount++;
                            }
                        }
                    }
                }
                if (unusedMods)
                {
                    toolStripStatusLabel1.Text = RemovalCount.ToString() + " Unused mods removed from catalog";
                    json = Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);

                    // Hack the Bethesda header back in
                    json = Tools.MakeHeader() + json[1..];

                    File.WriteAllText(Tools.GetCatalogPath(), json);
                }
                else
                {
                    richTextBox2.Text += "\nNo unused mods found in catalog\n";
                    ScrollToEnd();
                    toolStripStatusLabel1.Text = "No unused mods found in catalog";
                    if (log)
                        activityLog.WriteLog("No unused mods found in catalog");
                }
            }
            catch (Exception ex)
            {
                toolStripStatusLabel1.Text = (ex.Message);
                json = Tools.MakeHeaderBlank();
                File.WriteAllText(Tools.GetCatalogPath(), json);
            }
        }

        private bool RestoreCatalog()
        {
            string destFileName = Tools.GetCatalogPath();
            string sourceFileName = destFileName + ".bak";

            if (Tools.FileCompare(destFileName, sourceFileName))
            {
                richTextBox2.Text += "\nBackup file is up to date.\n";
                toolStripStatusLabel1.Text = "Backup file is up to date.";
                return true;
            }

            try
            {
                // Copy the file
                File.Copy(sourceFileName, destFileName, true); // overwrite

                richTextBox2.Text += "\nRestore complete\n";
                toolStripStatusLabel1.Text = "Restore complete";
                if (log)
                    activityLog.WriteLog("Catalog Restore complete");
                return true;
            }
            catch (Exception ex)
            {
                richTextBox2.Text += "\nRestore failed.\n";
                toolStripStatusLabel1.Text = $"{ex.Message} Restore failed";
                if (log)
                    activityLog.WriteLog($"{ex.Message} Catalog Restore failed");
                return false;
            }
        }

        private void ScrollToEnd()
        {
            richTextBox2.SelectionStart = richTextBox2.Text.Length;
            richTextBox2.ScrollToCaret();
        }

        private void SetAutoCheckBoxes()
        {
            // Initialise Checkboxes
            chkAutoCheck.Checked = AutoCheck;
            chkAutoClean.Checked = AutoClean;
            chkAutoBackup.Checked = AutoBackup;
            chkAutoRestore.Checked = AutoRestore;
            chkForceClean.Checked = ForceClean;
        }

        private void frmStarfieldTools_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (log)
                activityLog.WriteLog("Catalog Checker Closing.");
        }
    }
}