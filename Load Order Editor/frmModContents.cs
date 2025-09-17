using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Starfield_Tools.Load_Order_Editor
{
    public partial class frmModContents : Form
    {
        private List<string> files = new();

        public frmModContents(string modName)
        {
            InitializeComponent();

            richTextBox1.Text = $"Plugin Name: {modName}:\n\n";
            string modBaseName = modName[..modName.LastIndexOf('.')]; // Get current mod name

            string directoryPath = Path.Combine(frmLoadOrder.GamePath , "Data");
            string ModFile = Path.Combine(directoryPath, modBaseName); // Add esp, esm, and archives to files list

            if (File.Exists(ModFile + ".esp"))
                files.Add(ModFile + ".esp");

            if (File.Exists(ModFile + ".esm"))
                files.Add(ModFile + ".esm");

            // match files like 'modname - textures.ba2', 'modname - textures01.ba2', 'modname - textures02.ba2', etc.
            string[] textureFiles = Directory.GetFiles(directoryPath, modName[..^4] + "* - textures*.ba2");

                btnShowArchives.Enabled = false;

            foreach (string file in textureFiles)
                files.Add(file);

            if (File.Exists(ModFile + " - main.ba2"))
                files.Add(ModFile + " - main.ba2");

            if (File.Exists(ModFile + " - voices_en.ba2"))
                files.Add(ModFile + " - voices_en.ba2");

            // Disable the Archive Contents button if no file ends with ".ba2"
            btnShowArchives.Enabled = files.Any(f => f.EndsWith(".ba2", StringComparison.OrdinalIgnoreCase));

            richTextBox1.Text += "Mod files are:\n";
            foreach (var item in files)
            {
                FileInfo fileInfo = new FileInfo(item);
                long fileSize = fileInfo.Length / 1024;
                richTextBox1.Text += $"{Path.GetFileName(item)}, Size: {fileSize} Kb\n";
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnShowArchives_Click(object sender, EventArgs e)
        {
            string archive2Path = Path.Combine(frmLoadOrder.GamePath, "Tools", "Archive2", "Archive2.exe");

            if (!File.Exists(archive2Path)) // Check if Archive2.exe exists
            {
                MessageBox.Show("Install the Creation Kit.", "Archive2.exe not found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            foreach (var file in files.Where(f => f.EndsWith(".ba2")))
            {
                string cmdLine = "\"" + Path.Combine(frmLoadOrder.GamePath, "Data", file) + "\"";
                System.Diagnostics.Process.Start(archive2Path, cmdLine);
            }
        }
    }
}