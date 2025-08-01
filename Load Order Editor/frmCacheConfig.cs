﻿using Starfield_Tools.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Starfield_Tools.Load_Order_Editor
{
    public partial class frmCacheConfig : Form
    {
        private List<string> cacheOptions = new();
        private readonly Tools tools = new();

        public frmCacheConfig()
        {
            InitializeComponent();

            var readFileOther = File.ReadAllLines(Path.Combine(Tools.CommonFolder, "ReadFile.txt")).Where(x => !x.StartsWith("rem ")).ToList();

            var plugins = File.ReadAllLines(Path.Combine(Tools.StarfieldAppData, "Plugins.txt")) // Only active plugins
                .Where(p => p[0] == '*')
                .Select(p => p.Substring(1).Replace(".esm", "", StringComparison.OrdinalIgnoreCase)) // Strip * and .esm
                .ToList();

            List<string> pluginFiles = plugins.Select(p => "Data\\" + p + "*").ToList(); // Get esm and associated files*/
            foreach (string pluginFile in pluginFiles)
                cacheOptions.Add(pluginFile);
            foreach (string item in readFileOther)
                cacheOptions.Add(item);
            UpdateCacheSize();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            UpdateCacheSize();

            string readfilePath = Properties.Settings.Default.ReadfilePath;

            if (string.IsNullOrEmpty(readfilePath))
            {
                MessageBox.Show("Please set the readfile.exe path in Settings first.", "Readfile Path Not Set", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            System.Windows.Forms.SaveFileDialog saveDialog = new()
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Filter = "Cmd File|*.cmd",
                Title = "Generate Readfile commands",
                FileName = "Starfield Readfile.cmd"
            };

            if (saveDialog.ShowDialog() != DialogResult.OK || string.IsNullOrEmpty(saveDialog.FileName))
            {
                // sbar("Readfile generation cancelled");
                return;
            }

            List<string> selectedOptions = new();
            if (chkWwise.Checked)
                selectedOptions.Add("Data\\starfield - wwise*");
            if (chkMesh.Checked)
                selectedOptions.Add("Data\\starfield - mesh*");
            if (chkLOD.Checked)
                selectedOptions.Add("Data\\starfield - lod*");
            if (chkTextures.Checked)
                selectedOptions.Add("Data\\starfield - textures*");

            using (StreamWriter writer = new(saveDialog.FileName))
            {
                foreach (var item in cacheOptions.Concat(selectedOptions))
                    writer.WriteLine($"{readfilePath} \"{Path.Combine(frmLoadOrder.StarfieldGamePath, item)}\" /h /b /o");
            }
            if (Tools.ConfirmAction("Readfile generation complete", "Run batch file?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                Tools.OpenFile(saveDialog.FileName);
        }

        private void UpdateCacheSize()
        {
            List<string> selectedOptions = new();
            if (chkWwise.Checked)
                selectedOptions.Add("Data\\starfield - wwise*");
            if (chkMesh.Checked)
                selectedOptions.Add("Data\\starfield - mesh*");
            if (chkLOD.Checked)
                selectedOptions.Add("Data\\starfield - lod*");
            if (chkTextures.Checked)
                selectedOptions.Add("Data\\starfield - textures*");

            long cacheSize = 0;

            foreach (string item in cacheOptions.Concat(selectedOptions))
                cacheSize += GetCacheSize(frmLoadOrder.StarfieldGamePath, item);
            txtCacheSize.Text = $"{cacheSize / 1024 / 1024 / 1024} GB";
        }

        private long GetCacheSize(string baseDirectory, string pattern)
        {
            long totalSize = 0;

            try
            {
                // Get directory path and file pattern
                string directory = Path.GetDirectoryName(Path.Combine(baseDirectory, pattern)) ?? baseDirectory;
                string searchPattern = Path.GetFileName(pattern);

                // Ensure directory exists before attempting enumeration
                if (Directory.Exists(directory))
                {
                    // Enumerate all files matching the pattern
                    var files = Directory.EnumerateFiles(directory, searchPattern, SearchOption.TopDirectoryOnly);

                    foreach (string file in files)
                    {
                        var fileInfo = new FileInfo(file);
                        totalSize += fileInfo.Length;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing pattern '{pattern}': {ex.Message}");
                // Optionally log or handle the error
            }

            return totalSize;
        }

        private void btnCalc_Click(object sender, EventArgs e)
        {
            UpdateCacheSize();
        }
    }
}