using Starfield_Tools.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Starfield_Tools.Load_Order_Editor
{
    public partial class frmDeleteLooseFiles : Form
    {
        private frmLoadOrder.ActivityLog activityLog = frmLoadOrder.activityLog;
        private bool log = Properties.Settings.Default.Log;

        public frmDeleteLooseFiles()
        {
            InitializeComponent();

            frmLoadOrder.returnStatus = 0;
            List<string> DeletedFiles = new();

            if (string.IsNullOrEmpty(frmLoadOrder.StarfieldGamePath))
            {
                MessageBox.Show("Game path not set");
                return;
            }

            // Delete these folders
            List<string> foldersToDelete = new(Tools.LooseFolders);

            foreach (var item in foldersToDelete)
            {
                if (CheckFolder(item))
                {
                    DeletedFiles.Add(item);
                    checkedListBox1.Items.Add(item);
                }
            }
            if (DeletedFiles.Count == 0)
            {
                if (log)
                    activityLog.WriteLog("No files found to delete");
                this.Close();
            }
        }

        private static bool CheckFolder(string folderPath)
        {
            bool gameFolder = false, documentsFolder = false;
            string gameFolderPath = Path.Combine(frmLoadOrder.StarfieldGamePath, "Data", folderPath);
            string documentsFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"My Games\Starfield\Data", folderPath);

            // Check if the folder exists in the game directory
            try
            {
                if (Directory.Exists(gameFolderPath))
                    gameFolder = true;
            }
            catch
            {
                gameFolder = false;
            }

            try
            {
                if (Directory.Exists(documentsFolderPath))
                    documentsFolder = true;
            }
            catch (Exception ex)
            {
                documentsFolder = false;
            }
            return documentsFolder || gameFolder;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnSelectAll_Click(object sender, EventArgs e)
        {
            foreach (int i in Enumerable.Range(0, checkedListBox1.Items.Count))
            {
                checkedListBox1.SetItemChecked(i, true);
            }
        }

        private void btnSelectNone_Click(object sender, EventArgs e)
        {
            foreach (int i in Enumerable.Range(0, checkedListBox1.Items.Count))
            {
                checkedListBox1.SetItemChecked(i, false);
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            string gameFolderPath = Path.Combine(frmLoadOrder.StarfieldGamePath, "Data");
            string documentsFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"My Games\Starfield\Data");

            if (checkedListBox1.CheckedItems.Count == 0)
            {
                MessageBox.Show("No folders selected to delete.");
                return;
            }

            if (Tools.ConfirmAction("Are you sure you want to delete loose files folders including their contents?", "Warning, this will delete any loose file mods",
    MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, true) == DialogResult.No) // Always show warning
                return;

            foreach (var item in checkedListBox1.CheckedItems)
            {
                if (Directory.Exists(Path.Combine(gameFolderPath, item.ToString())))
                {
                    Directory.Delete(Path.Combine(gameFolderPath, item.ToString()), true); // Delete recursive
                    if (log)
                        activityLog.WriteLog($"Deleted folder: {Path.Combine(gameFolderPath, item.ToString())}");
                    frmLoadOrder.returnStatus++;
                }
                if (Directory.Exists(Path.Combine(documentsFolderPath, item.ToString())))
                {
                    Directory.Delete(Path.Combine(documentsFolderPath, item.ToString()), true); // Delete recursive
                    if (log)
                        activityLog.WriteLog($"Deleted folder: {Path.Combine(documentsFolderPath, item.ToString())}");
                    frmLoadOrder.returnStatus++;
                }
            }

            this.Close();
        }
    }
}