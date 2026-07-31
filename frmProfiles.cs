using System;
using System.IO;
using System.Windows.Forms;

namespace hstCMM
{
    public partial class frmProfiles : Form
    {
        private string profileDir = "";

        public frmProfiles()
        {
            InitializeComponent();
            SetupForm();
        }

        private void SetupForm()
        {
            if (string.IsNullOrEmpty(Properties.Settings.Default.ProfileFolder))
                return;

            profileDir = Path.Combine(txtProfileDirectory.Text = Properties.Settings.Default.ProfileFolder,frmLoadOrder.GameName);
            var profiles = Directory.GetFiles(profileDir);
            foreach (var file in profiles)
            {
                checkedListBox1.Items.Add(Path.GetFileName(file));
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnSetProfileDirectory_Click(object sender, EventArgs e)
        {
            using FolderBrowserDialog profileDirDialog = new()
            {
                InitialDirectory = profileDir,
                Description = "Choose profile directory"
            };
            profileDirDialog.ShowDialog();
            if (!string.IsNullOrEmpty(profileDirDialog.SelectedPath))
            {
                Properties.Settings.Default.ProfileFolder = profileDirDialog.SelectedPath;
                Properties.Settings.Default.Save();
            }
        }
    }
}