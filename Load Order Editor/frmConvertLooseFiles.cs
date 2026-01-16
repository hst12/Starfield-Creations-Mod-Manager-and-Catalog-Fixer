using hstCMM.Shared;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace hstCMM.Load_Order_Editor
{
    public partial class frmConvertLooseFiles : Form
    {
        private string esm;
        private frmLoadOrder.ActivityLog activityLog = frmLoadOrder.activityLog;

        private bool log = Properties.Settings.Default.Log;

        public frmConvertLooseFiles(string esmFile = "")
        {
            InitializeComponent();
            frmLoadOrder.returnStatus = 0;
            txtEsm.Text = esmFile = esm = esmFile;
            if (!string.IsNullOrEmpty(txtEsm.Text))
                lblRequired.Visible = false;
            this.AcceptButton = btnStart;
            if (!File.Exists(Path.Combine(frmLoadOrder.GamePath, @"Tools\Archive2", "Archive2.exe"))) // Check if Archive2.exe exists
            {
                MessageBox.Show("Install the Creation Kit.", "Archive2.exe not found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }

            if (!string.IsNullOrEmpty(esm))
            {
                ProcessArchives();
                //this.Close();
            }
        }

        private void ProcessArchives()
        {
            string cmdLine;
            DialogResult dlg;

            if (string.IsNullOrEmpty(esm))
                if (txtEsm.Text != string.Empty)
                {
                    if (txtEsm.Text.EndsWith(".esm"))
                        txtEsm.Text = txtEsm.Text.Replace(".esm", "");
                    esm = txtEsm.Text;

                    if (!File.Exists(Path.Combine(frmLoadOrder.GamePath, "Data", esm) + ".esm"))
                    {
                        File.Copy(Path.Combine(Tools.CommonFolder, "dummy.esm"), Path.Combine(frmLoadOrder.GamePath, "Data", esm + ".esm"));
                    }
                }
                else
                    return;

            string archive2Path = Path.Combine(frmLoadOrder.GamePath, "Tools", "Archive2", "Archive2.exe");
            string workingDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"My Games\Starfield\Data");

            // Create texture archive
            /*cmdLine = @"textures -create=""" + Path.Combine(frmLoadOrder.GamePath, "Data", Path.GetFileNameWithoutExtension(esm)) + " - textures.ba2" + @""""
           + " -format=DDS -maxSizeMB=1024 -excludefile=" + "\""
           + Path.Combine(Tools.CommonFolder, "exclude.txt" + "\"");*/
            cmdLine = @"textures -create=""" + Path.Combine(frmLoadOrder.GamePath, "Data",
                Path.GetFileNameWithoutExtension(esm)) + " - textures.ba2" + @""""
                + " -format=DDS -excludefile=" + "\"" + Path.Combine(Tools.CommonFolder, "exclude.txt" + "\"");

            if (File.Exists(Path.Combine(frmLoadOrder.GamePath, "Data", Path.GetFileNameWithoutExtension(esm) + " - textures.ba2")))
            {
                if (Tools.ConfirmAction("Overwrite Archive", "Texture archive already exists. Do you want to overwrite it?",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    activityLog.WriteLog($"Creating texture archive\n{archive2Path} {cmdLine}");
                    MakeArchive(archive2Path, cmdLine, workingDirectory);
                    frmLoadOrder.returnStatus++;
                }
                else
                {
                    if (log)
                        activityLog.WriteLog("Skipping texture archive creation.");
                }
            }
            else
            {
                activityLog.WriteLog($"Creating texture archive\n{archive2Path} {cmdLine}");
                MakeArchive(archive2Path, cmdLine, workingDirectory);
                frmLoadOrder.returnStatus++;
            }

            // Create main archive
            cmdLine = @"interface,geometries,materials,meshes,scripts -create="""
                        + Path.Combine(frmLoadOrder.GamePath, "Data", Path.GetFileNameWithoutExtension(esm) + " - main.ba2") + @""""
                        + " -format=General";

            if (File.Exists(Path.Combine(frmLoadOrder.GamePath, "Data", Path.GetFileNameWithoutExtension(esm) + " - main.ba2")))
            {
                if (Tools.ConfirmAction("Overwrite Archive", "Main archive already exists. Do you want to overwrite it?",
                     MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    if (log)
                        activityLog.WriteLog($"Creating main archive\n{archive2Path} {cmdLine}");
                    MakeArchive(archive2Path, cmdLine, workingDirectory);
                    frmLoadOrder.returnStatus++;
                }
                else
                {
                    if (log)
                        activityLog.WriteLog("Skipping main archive creation.");
                }
            }
            else
            {
                if (log)
                    activityLog.WriteLog($"Creating main archive\n{archive2Path} {cmdLine}");
                MakeArchive(archive2Path, cmdLine, workingDirectory);
                frmLoadOrder.returnStatus++;
            }

            // Create sound archive
            if (Directory.Exists(Path.Combine(workingDirectory, "sound")))
            {
                cmdLine = @"sound -create="""
                        + Path.Combine(frmLoadOrder.GamePath, "Data", Path.GetFileNameWithoutExtension(esm) + " - main.ba2") + @""""
                        + " -format=General -compression=None";

                if (File.Exists(Path.Combine(frmLoadOrder.GamePath, "Data", Path.GetFileNameWithoutExtension(esm) + " - main.ba2")))
                {
                    if (Tools.ConfirmAction("Sound archive exists", "Overwrite sound archive", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        if (log)
                            activityLog.WriteLog($"Creating sound archive\n{archive2Path} {cmdLine}");
                        MakeArchive(archive2Path, cmdLine, workingDirectory);
                        frmLoadOrder.returnStatus++;
                    }
                    else
                    {
                        if (log)
                            activityLog.WriteLog("Skipping sound archive creation.");
                    }
                }
                else
                {
                    if (log)
                        activityLog.WriteLog($"Creating sound archive\n{archive2Path} {cmdLine}");
                    MakeArchive(archive2Path, cmdLine, workingDirectory);
                    frmLoadOrder.returnStatus++;
                }
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(esm) || !string.IsNullOrEmpty(txtEsm.Text))
            {
                ProcessArchives();
                this.Close();
            }
        }

        private void MakeArchive(string archive2Path, string cmdLine, string workingDirectory)
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = archive2Path,
                Arguments = cmdLine,
                WorkingDirectory = workingDirectory
            };

            using (Process process = Process.Start(startInfo))
            {
                process.WaitForExit();
                //process.Start();
            }
        }

        private void btnEsmSelect_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                InitialDirectory = Path.GetFullPath(Path.Combine(frmLoadOrder.GamePath, "Data")),
                AutoUpgradeEnabled = true,
                Filter = "Esm files (*.esm)|*.esm|All files (*.*)|*.*",
                Title = "Select the esm file to convert loose files to",
            };

#if DEBUG
            Debug.WriteLine(openFileDialog.InitialDirectory);
#endif
            openFileDialog.ShowDialog(this);
            if (openFileDialog.FileName != string.Empty)
            {
                esm = Path.GetFileName(openFileDialog.FileName);
                btnEsmSelect.Text = "Selected " + esm;
                txtEsm.Text = "";
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void frmConvertLooseFiles_Shown(object sender, EventArgs e)
        {
            txtEsm.Focus();
        }
    }
}