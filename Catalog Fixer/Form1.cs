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

        public bool AutoCheck, AutoClean, AutoBackup, AutoRestore, ForceClean, Verbose;
        public string CatalogStatus;


        readonly Tools tools = new();
        private readonly string StarfieldGamePath;
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
            SetAutoCheckBoxes();

            bool cmdLineRunSteam = false;
            bool cmdLineRunMS = false;
            bool cmdlineRunSFSE = false;
            richTextBox2.Text = "";

            /*foreach (var arg in Environment.GetCommandLineArgs())
            {
                if (String.Equals(arg, "-runSteam", StringComparison.OrdinalIgnoreCase))
                    cmdLineRunSteam = true;
                if (String.Equals(arg, "-runMS", StringComparison.OrdinalIgnoreCase))
                    cmdLineRunMS = true;
                if (String.Equals(arg, "-runSFSE", StringComparison.OrdinalIgnoreCase))
                    cmdlineRunSFSE = true;
                if (String.Equals(arg, "-noauto", StringComparison.OrdinalIgnoreCase))
                {

                    AutoCheck = false;
                    AutoClean = false;
                    AutoBackup = false;
                    AutoRestore = false;
                    ForceClean = false;
                    SaveSettings();
                    SetAutoCheckBoxes();
                }

                if (String.Equals(arg, "-auto", StringComparison.OrdinalIgnoreCase)) // Set recommended settings
                {
                    AutoCheck = true;
                    AutoClean = true;
                    AutoBackup = true;
                    AutoRestore = true;
                    ForceClean = false;
                    SaveSettings();
                    SetAutoCheckBoxes();
                }
            }*/

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

            if (AutoRestore && !Tools.FileCompare(Tools.GetCatalogPath(), Tools.GetCatalogPath() + ".bak"))
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

            // Run  Command line params
            if (cmdLineRunSteam)
            {
                SaveSettings();
                Tools.StartStarfieldSteam();
                if (Application.MessageLoop)
                    Application.Exit();
                else
                    Environment.Exit(1);
            }

            if (cmdLineRunMS)
            {
                SaveSettings();
                Tools.StartStarfieldMS();
                if (Application.MessageLoop)
                    Application.Exit();
                else
                    Environment.Exit(1);
            }

            if (cmdlineRunSFSE)
            {
                SaveSettings();
                Tools.StartStarfieldSFSE();
                if (Application.MessageLoop)
                    Application.Exit();
                else
                    Environment.Exit(1);
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

        private void DisplayCatalog()
        {
#pragma warning disable CS0168 // Variable is declared but never used
            try
            {
                richTextBox1.Text = File.ReadAllText(Tools.GetCatalogPath());
            }
            catch (Exception ex)
            {
#if DEBUG
                MessageBox.Show(ex.Message);
#endif
                toolStripStatusLabel1.Text = "Catalog not found";
            }
#pragma warning restore CS0168 // Variable is declared but never used
        }

        private bool CheckCatalog() // returns true if catalog good
        {
            toolStripStatusLabel1.Text = "Checking...";
            richTextBox1.Text = "";
            int ErrorCount = 0;
            int WarningCount = 0;
            richTextBox2.Text += "Checking Catalog\n";
            double VersionCheck;
            double TimeStamp;

            try
            {
                string jsonFilePath = Tools.GetCatalogPath();
                if (jsonFilePath == null)
                {
                    toolStripStatusLabel1.Text = "Start the game and enter the Creations menu or load a save to create a catalog file";
                    richTextBox2.Text = "Start the game and enter the Creations menu or load a save to create a catalog file";
                    return false;
                }
                string json = File.ReadAllText(jsonFilePath);
                if (json == "")
                {
                    toolStripStatusLabel1.Text = "Catalog file is empty, nothing to check";
                    return false;
                }
                string TestString = "";

                var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, Tools.Creation>>(json);
                data.Remove("ContentCatalog"); // remove header

                foreach (var kvp in data)
                {
                    for (int i = 0; i < kvp.Value.Files.Length - 0; i++)
                    {
                        if (kvp.Value.Files[i].IndexOf(".esp") > 0)
                        {
                            richTextBox2.Text += "\nWarning - esp file found in catalog file - " + kvp.Value.Files[i] + "\n";
                            WarningCount++;
                        }
                    }

                    TestString = kvp.Value.Version;
                    VersionCheck = double.Parse((kvp.Value.Version[..kvp.Value.Version.IndexOf('.')]));
                    if (TestString != Tools.CatalogVersion) // Skip catalog header, pull version info apart into date and actual version number
                        if (Verbose)
                            richTextBox2.Text += kvp.Value.Title + ", date: " + Tools.ConvertTime(VersionCheck) + " version: " + TestString[(TestString.IndexOf('.') + 1)..] + "\n";

                    TimeStamp = kvp.Value.Timestamp;
                    if (VersionCheck > kvp.Value.Timestamp && VersionCheck != 1)
                    {
                        ErrorCount++;
                        richTextBox2.Text += "Out of range version number detected in " + kvp.Value.Title + ": " + TestString + ", " + Tools.ConvertTime(VersionCheck) + "\n";
                    }
                    for (int i = 0; i < TestString.Length; i++)
                    {
                        if (!char.IsLetterOrDigit(TestString[i])) // Check for numbers or . in Version
                        {
                            if (TestString[i] != '.')
                            {
                                ErrorCount++;
                                richTextBox2.Text += "Non numeric version number detected in " + kvp.Value.Title + "\n";
                                break;
                            }
                        }
                    }
                }

                if (ErrorCount == 0)
                {
                    if (WarningCount == 0)
                    {
                        toolStripStatusLabel1.Text = "Catalog OK";
                        CatalogStatus = toolStripStatusLabel1.Text;
                        richTextBox2.Text += "\nCatalog OK\n";
                        return true;
                    }
                    else
                    {
                        toolStripStatusLabel1.Text = WarningCount + " Warning(s) Press the Catalog button for details";
                        CatalogStatus = toolStripStatusLabel1.Text;
                        return true;
                    }
                }
                else
                {
                    toolStripStatusLabel1.Text = ErrorCount.ToString() + " Error(s) found";
                    CatalogStatus = toolStripStatusLabel1.Text;
                    richTextBox2.Text += ErrorCount.ToString() + " Error(s) found\n";
                    return false;
                }
            }

            catch (Exception ex)
            {
#if DEBUG
                MessageBox.Show(ex.Message);
#endif
                if (!File.Exists(Tools.GetCatalogPath()))
                {
                    DialogResult result = MessageBox.Show("Missing ContentCatalog.txt", "Do you want to create a blank ContentCatalog.txt file?", MessageBoxButtons.OKCancel);
                    if (result == DialogResult.OK)
                    {

                        var CatalogHeader = Tools.MakeHeaderBlank();
                        File.WriteAllText(Tools.GetCatalogPath(), CatalogHeader);
                        toolStripStatusLabel1.Text = "Dummy ContentCatalog.txt created";
                        return false;
                    }
                }
                else
                {
                    richTextBox2.Text += "\n" + (ex.Message);
                    toolStripStatusLabel1.Text = "Catalog corrupt. Use the Restore or Clean functions to repair";
                }
                return false;
            }
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
                    NewFix();

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

        private void btnQuit_Click(object sender, EventArgs e)
        {
            SaveSettings();
            this.Close();
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            string pathToFile = Tools.GetCatalogPath();
            Process.Start("explorer", pathToFile);
        }

        private void btnCheck_Click(object sender, EventArgs e)
        {
            CheckCatalog();
            ScrollToEnd();
            DisplayCatalog();
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
                    BackupStatus = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Backup failed");
                }
            }
            else
                BackupStatus = false;
            return BackupStatus;
        }

        private void chkAutoCheck_CheckedChanged(object sender, EventArgs e)
        {
            AutoCheck = chkAutoCheck.Checked;
        }

        private void chkAutoClean_CheckedChanged(object sender, EventArgs e)
        {
            AutoClean = chkAutoClean.Checked;
        }

        private void chkAutoBackup_CheckedChanged(object sender, EventArgs e)
        {
            AutoBackup = chkAutoBackup.Checked;
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

        private void btnBackup_Click(object sender, EventArgs e)
        {
            BackupCatalog();
            ScrollToEnd();
            DisplayCatalog();
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

        private bool RestoreCatalog()
        {
            string destFileName = Tools.GetCatalogPath();
            string sourceFileName = destFileName + ".bak";

#pragma warning disable CS0168 // Variable is declared but never used
            try
            {
                // Copy the file
                File.Copy(sourceFileName, destFileName, true); // overwrite

                richTextBox2.Text += "\nRestore complete\n";
                toolStripStatusLabel1.Text = "Restore complete";
                return true;
            }
            catch (Exception ex)
            {
#if DEBUG
                MessageBox.Show(ex.Message);
#endif
                richTextBox2.Text += "\nRestore failed.\n";
                toolStripStatusLabel1.Text = "Restore failed";
                return false;
            }
#pragma warning restore CS0168 // Variable is declared but never used
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

        private void btnClearLog_Click(object sender, EventArgs e)
        {
            richTextBox2.Text = "";
        }

        public static string GetStarfieldAppData()
        {
            return (Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)) + @"\Starfield";
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

            string filePath = GetStarfieldAppData() + "\\Plugins.txt";
            string fileContent = File.ReadAllText(filePath); // Load Plugins.txt
            // Split the content into lines if necessary
            List<string> lines = [.. fileContent.Split('\n')];

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
                data.Remove("ContentCatalog");

                foreach (var kvp in data)
                {
                    try
                    {
                        for (int i = 0; i < kvp.Value.Files.Length - 0; i++)
                        {
                            if (kvp.Value.Files[i].IndexOf(".esm") > 0 || kvp.Value.Files[i].IndexOf(".esp") > 0) // Look for .esm or .esp files
                            {
                                CreationsPlugin.Add(kvp.Value.Files[i]);
                                CreationsGUID.Add(kvp.Key);
                                CreationsTitle.Add(kvp.Value.Title);
                                if (Verbose)
                                    richTextBox2.Text += kvp.Value.Title + "\n";
                            }
                            if (kvp.Value.Files[i].IndexOf(".esp") > 0)
                                richTextBox2.Text += "\nWarning - esp file found in catalog file - " + kvp.Value.Files[i] + "\n";
                        }

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error: {ex.Message}");
                    }
                }

                List<string> missingStrings = CreationsPlugin.Except(esmFiles).ToList();
                richTextBox1.Text = "";
                index = 0;

                if (missingStrings.Count > 0)
                {
                    for (index = 0; index < missingStrings.Count; index++)
                    {
                        for (int i = 0; i < CreationsGUID.Count; i++)
                        {
                            if (CreationsPlugin[i] == missingStrings[index])
                            {
                                richTextBox2.Text += "Removing " + CreationsGUID[i] + " " + CreationsTitle[i] + "\n";
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
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                MessageBox.Show($"Error: {ex.Message}");
#endif
                toolStripStatusLabel1.Text = (ex.Message);
                json = Tools.MakeHeaderBlank();
                File.WriteAllText(Tools.GetCatalogPath(), json);
            }
        }

        private void NewFix()
        {
            string jsonFilePath = Tools.GetCatalogPath();

            string json = File.ReadAllText(jsonFilePath); // Load catalog
            if (json == "")
            {
                toolStripStatusLabel1.Text = "Catalog file is empty, nothing to clean";
                return;
            }
            string TestString;
            bool FixVersion;
            int errorCount = 0, VersionReplacementCount = 0;
            double VersionCheck;
            long TimeStamp;

            var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, Tools.Creation>>(json);

            foreach (var kvp in data)
            {
                TestString = kvp.Value.Version;
                FixVersion = false;
                if (Verbose)
                    richTextBox2.Text += "Checking " + kvp.Value.Title + ", " + TestString + "\n";

                for (int i = 0; i < TestString.Length; i++)
                {

                    if (!char.IsLetterOrDigit(TestString[i])) // Check for numbers or . in Version
                    {
                        if (TestString[i] != '.')
                        {
                            FixVersion = true;
                            break;
                        }
                    }
                }

                if (TestString != Tools.CatalogVersion) // Skip the catalog header then check for valid timestamps
                {
                    VersionCheck = double.Parse((kvp.Value.Version[..kvp.Value.Version.IndexOf('.')]));
                    TimeStamp = kvp.Value.Timestamp;
                    if (VersionCheck > kvp.Value.Timestamp)
                    {
                        richTextBox2.Text += "Replacing version no for " + kvp.Value.Title + "\n";
                        kvp.Value.Version = "1704067200.0";
                        VersionReplacementCount++;
                    }
                }
                if (FixVersion) // Replace version numbers if they contain garbage characters.
                {
                    richTextBox2.Text += "Invalid characters in " + kvp.Value.Title + "\n";
                    kvp.Value.Version = "1704067200.0"; // set version to 1704067200.0
                    errorCount++;
                }
            }
            data.Remove("ContentCatalog"); // remove content catalog section in case it's corrupted

            json = Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
            if (json == "{}")
            {
                toolStripStatusLabel1.Text = "Catalog is empty";
                return;
            }
            // Insert the Bethesda header back in. This will probably break if the version no. is updated from 1.1
            json = Tools.MakeHeader() + json[1..]; // Remove a { char

            File.WriteAllText(jsonFilePath, json);
            toolStripStatusLabel1.Text = VersionReplacementCount.ToString() + " Version replacements";
            ScrollToEnd();
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

        private void chkVerbose_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.Verbose = chkVerbose.Checked;
            Verbose = chkVerbose.Checked;
        }

        private void frmStarfieldTools_Shown(object sender, EventArgs e)
        {
            this.Focus();
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

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (Tools.ConfirmAction("Are you Sure?", "Delete ContentCatalog.txt", MessageBoxButtons.YesNo) == DialogResult.Yes)
                File.Delete(Tools.GetCatalogPath());
        }
    }
}
