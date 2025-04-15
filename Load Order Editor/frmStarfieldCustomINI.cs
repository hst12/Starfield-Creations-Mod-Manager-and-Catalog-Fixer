using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace Starfield_Tools.Load_Order_Editor
{
    public partial class frmStarfieldCustomINI : Form
    {
        public frmStarfieldCustomINI()
        {
            InitializeComponent();
            string filePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\My Games\\Starfield\\StarfieldCustom.ini";
            if (File.Exists(filePath))
            {
                var fileContent = File.ReadAllLines(filePath);

                //Set checkboxes
                foreach (var lines in fileContent)
                {
                    if (lines.Contains("sIntroSequence"))
                        chkSkipIntro.Checked = true;
                    if (lines.Contains("bInvalidateOlderFiles"))
                        chkLooseFiles.Checked = true;
                    if (lines.Contains("bEnableMessageOfTheDay"))
                        chkMOTD.Checked = true;
                    if (lines.Contains("uMainMenuDelayBeforeAllowSkip"))
                        chkMainMenuDelay.Checked = true;
                    if (lines.Contains("bForcePhotoModeLoadScreen"))
                        chkUserPhotos.Checked = true;
                    if (lines.Contains("bEnableLogging"))
                        chkPapyrusLogging.Checked = true;
                }
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            string filePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\My Games\\Starfield\\StarfieldCustom.ini";

            List<string> INILines = new();
            INILines.Add(@"[General]");

            if (chkSkipIntro.Checked)
                INILines.Add("sIntroSequence=");

            if (chkMOTD.Checked)
                INILines.Add("bEnableMessageOfTheDay=0");

            if (chkMainMenuDelay.Checked)
                INILines.Add("uMainMenuDelayBeforeAllowSkip=0");

            if (chkUserPhotos.Checked)
                INILines.Add(@"
[Interface]
bForcePhotoModeLoadScreen=1");

            if (chkLooseFiles.Checked)
            {
                INILines.Add(@"
[Archive]
bInvalidateOlderFiles=");
            }

            if (chkPapyrusLogging.Checked)
            {
                INILines.Add(@"
[Papyrus]
bEnableLogging=1
bLoadDebugInformation=1
bEnableTrace=1");
            }

            File.WriteAllLines(filePath, INILines);
            Properties.Settings.Default.LooseFiles = chkLooseFiles.Checked;
            Properties.Settings.Default.Save();
            this.Close();
        }

        private void btnSuggested_Click(object sender, EventArgs e)
        {
            chkLooseFiles.Checked = false;
            chkMOTD.Checked = true;
            chkUserPhotos.Checked = true;
            chkMainMenuDelay.Checked = true;
            chkSkipIntro.Checked = true;
            chkPapyrusLogging.Checked = false;
        }
    }
}
