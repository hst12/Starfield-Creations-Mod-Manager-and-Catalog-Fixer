using hstCMM.Shared;
using System;
using System.Windows.Forms;
using static hstCMM.frmLoadOrder;

namespace hstCMM.Load_Order_Editor
{
    public partial class frmOptions : Form
    {
        private readonly Tools tools = new();

        public frmOptions()
        {
            InitializeComponent();
            var settings = Properties.Settings.Default;
            if (settings.GameVersion != frmLoadOrder.MS)
                txtGame.Text = settings.GamePath;
            else
                txtGame.Text = settings.GamePathMS;
            txtLOOT.Text = settings.LOOTPath;
            txtMO2.Text = settings.MO2Path;
            txtVortex.Text = settings.VortexPath;
            txtxEdit.Text = settings.xEditPath;
            lblCK.Text = Tools.GameLibrary.GetById(Properties.Settings.Default.Game).CKId;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            frmLoadOrder.returnStatus = 0;
            Close();
        }

        private void btnGame_Click(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.GameVersion != frmLoadOrder.MS)
                GamePath = tools.SetGamePath();
            else
                GamePath = tools.SetGamePathMS();
        }

        private void btnFont_Click(object sender, EventArgs e)
        {
            if (fontDialog1.ShowDialog() != DialogResult.Cancel)
            {
                this.Font = fontDialog1.Font;
                //frmLoadOrder.menuStrip1.Font = fontDialog1.Font;
            }
            this.CenterToScreen();
            Properties.Settings.Default.FontSize = this.Font;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            frmLoadOrder.returnStatus = 1;
            Close();
        }

        private void btnLOOT_Click(object sender, EventArgs e)
        {
            using (var openFileDialog1 = new OpenFileDialog
            {
                InitialDirectory = Properties.Settings.Default.LOOTPath,
                RestoreDirectory = true,
                Filter = "Executable Files|*.exe",
                Title = "Set the path to the LOOT executable",
                FileName = "LOOT.exe"
            })

                if (openFileDialog1.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(openFileDialog1.FileName))
                {
                    Properties.Settings.Default.LOOTPath = openFileDialog1.FileName;
                    txtLOOT.Text = openFileDialog1.FileName;
                    SaveSettings();
                    MessageBox.Show($"LOOT path set to {openFileDialog1.FileName}", "Restart the app for changes to take effect");
                }
        }

        private void SaveSettings()
        {
            Properties.Settings.Default.Save();
        }

        private void btnMO2_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Executable Files|*.exe";
            openFileDialog1.Title = "Set the path to the MO2 executable";
            openFileDialog1.FileName = "ModOrganizer.exe";
            DialogResult MO2Path = openFileDialog1.ShowDialog();
            if (MO2Path == DialogResult.OK && openFileDialog1.FileName != "")
            {
                Properties.Settings.Default.MO2Path = openFileDialog1.FileName;
                txtMO2.Text = openFileDialog1.FileName;
                //mO2ToolStripMenuItem.Visible = true;
            }
        }

        private void btnVortex_Click(object sender, EventArgs e)
        {
            if (!tools.DetectVortex())
            {
                openFileDialog1.Filter = "Executable Files|*.exe";
                openFileDialog1.Title = "Set the path to the Vortex executable";
                openFileDialog1.FileName = "Vortex.exe";
                DialogResult VortexPath = openFileDialog1.ShowDialog();
                if (VortexPath == DialogResult.OK && openFileDialog1.FileName != "")
                {
                    Properties.Settings.Default.VortexPath = openFileDialog1.FileName;
                    txtVortex.Text = openFileDialog1.FileName;
                    //vortexToolStripMenuItem.Visible = true;
                }
            }
            else
                txtVortex.Text = Properties.Settings.Default.VortexPath;
        }

        private void btnxEdit_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Executable Files|*.exe";
            openFileDialog1.Title = "Set the path to the xEdit executable";
            openFileDialog1.FileName = "SF1Edit.exe";
            DialogResult xEditPath = openFileDialog1.ShowDialog();
            if (xEditPath == DialogResult.OK && openFileDialog1.FileName != "")
            {
                Properties.Settings.Default.xEditPath = openFileDialog1.FileName;
                txtxEdit.Text = openFileDialog1.FileName;
                //xEditToolStripMenuItem.Visible = true;
            }
        }

        private void btnDetect_Click(object sender, EventArgs e)
        {
            frmLoadOrder.GamePath = tools.GetSteamGamePath(frmLoadOrder.GameName);
            if (!String.IsNullOrEmpty(GamePath))
                txtGame.Text = frmLoadOrder.GamePath;
            //MessageBox.Show($"Game Path set to {GamePath}", $"{frmLoadOrder.GameName} Detected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
                MessageBox.Show("Error detecting game path", "Game Path Not Found", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }
    }
}