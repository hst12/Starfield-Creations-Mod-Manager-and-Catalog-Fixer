using hstCMM.Shared;
using Microsoft.VisualBasic.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using static hstCMM.frmLoadOrder;
using static hstCMM.Shared.Tools;

namespace hstCMM.Load_Order_Editor
{
    public partial class frmDirectories : Form
    {
        private readonly Tools tools = new();

        public frmDirectories()
        {
            InitializeComponent();
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
                    //vortexToolStripMenuItem.Visible = true;
                }
            }
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
                //xEditToolStripMenuItem.Visible = true;
            }
        }
    }
}