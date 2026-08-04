using hstCMM.Shared;
using System;
using System.IO;
using System.Windows.Forms;


namespace hstCMM
{
    public partial class frmProfiles : Form
    {
        private string profileDir = "";
        string[] profiles;
        private readonly Tools tools = new();

        public frmProfiles()
        {
            InitializeComponent();
            SetupForm();
        }

        private void SetupForm()
        {
            if (string.IsNullOrEmpty(Properties.Settings.Default.ProfileFolder))
                return;

            profileDir = Path.Combine(txtProfileDirectory.Text = Properties.Settings.Default.ProfileFolder, frmLoadOrder.GameName);
            profiles = Directory.GetFiles(profileDir);
            foreach (var file in profiles)
            {
                checkedListBox1.Items.Add(Path.GetFileName(file));
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            frmLoadOrder.returnStatus = 0;
            this.Close();
        }

        private void btnSetProfileDirectory_Click(object sender, EventArgs e)
        {
            using FolderBrowserDialog profileDirDialog = new()
            {
                InitialDirectory = Properties.Settings.Default.ProfileFolder,
                Description = "Choose profile directory"
            };
            profileDirDialog.ShowDialog();
            if (!string.IsNullOrEmpty(profileDirDialog.SelectedPath))
                Properties.Settings.Default.ProfileFolder = profileDirDialog.SelectedPath;
        }

        private void btnOpenDir_Click(object sender, EventArgs e)
        {
            Tools.OpenDirectory(profileDir);
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            frmLoadOrder.returnStatus = 1;
            Properties.Settings.Default.Save();
            this.Close();
        }

        private void btnDuplicate_Click(object sender, EventArgs e)
        {
            if (checkedListBox1.SelectedItems.Count == 0) // Do nothing if no selection
                return;

            string newProfile = Microsoft.VisualBasic.Interaction.InputBox("Profile:", "Enter New Profile Name", "New");
            if (string.IsNullOrEmpty(newProfile))
                return;
            if (!newProfile.EndsWith(".txt")) // Append .txt if necessary
                newProfile += ".txt";

            try
            {
                File.Copy(profiles[checkedListBox1.SelectedIndex], Path.Combine(profileDir, newProfile));
                frmLoadOrder.activityLog.WriteLog(profiles[checkedListBox1.SelectedIndex] + " " + profileDir + " " + newProfile);
                checkedListBox1.Items.Clear();
                SetupForm();
            }
            catch (Exception ex)
            {
                frmLoadOrder.activityLog.WriteLog(ex.Message);
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (checkedListBox1.SelectedItems.Count == 0)
                return;

            try
            {
                foreach (int item in checkedListBox1.CheckedIndices)
                {
                    File.Delete(profiles[item]);

                }
                checkedListBox1.Items.Clear();
                SetupForm();
            }
            catch (Exception ex)
            {
                frmLoadOrder.activityLog.WriteLog(ex.Message);
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (!File.Exists(Path.Combine(Tools.GameAppData, "Plugins.txt")))
            {
                MessageBox.Show("Plugins.txt not found");
                return;
            }

            string newProfile = Microsoft.VisualBasic.Interaction.InputBox("Profile:", "Enter New Profile Name", "New");
            if (string.IsNullOrEmpty(newProfile))
                return;
            if (!newProfile.EndsWith(".txt")) // Append .txt if necessary
                newProfile += ".txt";
            try
            {
                frmLoadOrder.activityLog.WriteLog($"Will create from: {Path.Combine(Tools.GameAppData,"Plugins.txt")}");
                frmLoadOrder.activityLog.WriteLog($"Will copy to: {Path.Combine(profileDir, newProfile)}");
                File.Copy(Path.Combine(Tools.GameAppData, "Plugins.txt"), Path.Combine(profileDir, newProfile));
                checkedListBox1.Items.Clear();
                SetupForm();
            }
            catch (Exception ex)
            {
                frmLoadOrder.activityLog.WriteLog(ex.Message);
            }
        }
    }
}