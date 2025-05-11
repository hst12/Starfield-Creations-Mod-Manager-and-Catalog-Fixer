using Starfield_Tools.Common;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using static Starfield_Tools.Common.Tools;

namespace Starfield_Tools.Load_Order_Editor
{
    public partial class frmConvertLooseFiles : Form
    {
        private string esm;

        public frmConvertLooseFiles(string esmFile="")
        {
            InitializeComponent();
            frmLoadOrder.returnStatus = 0;
            txtEsm.Text = esmFile = esm = esmFile;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(esm))
                if (txtEsm.Text != string.Empty)
                {
                    esm = txtEsm.Text;

                    if (!File.Exists(Path.Combine(frmLoadOrder.StarfieldGamePath, "Data", esm) + ".esm"))
                    {
                        File.Copy(Path.Combine(Tools.CommonFolder, "dummy.esm"), Path.Combine(frmLoadOrder.StarfieldGamePath, "Data", esm + ".esm"));
                    }
                }
                else
                    return;

            string archive2Path = Path.Combine(frmLoadOrder.StarfieldGamePath, "Tools", "Archive2", "Archive2.exe");
            string cmdLine = @"textures -create=""" + Path.Combine(frmLoadOrder.StarfieldGamePath, "Data", Path.GetFileNameWithoutExtension(esm)) + " - textures.ba2" + @""""
                + " -format=DDS -excludefile=" + "\""
                + Path.Combine(Tools.CommonFolder, "exclude.txt" + "\"");
            string workingDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"My Games\Starfield\Data");

            /*Debug.WriteLine($"Archive 2 path: {archive2Path}");
            Debug.WriteLine($"cmdLine: {cmdLine}");
            Debug.WriteLine($"workingDirectory: {workingDirectory}");*/

            // Create texture archive
            if (!File.Exists(Path.Combine(frmLoadOrder.StarfieldGamePath, "Data", Path.GetFileNameWithoutExtension(esm) + " - textures.ba2")))
            {
                MakeArchive(archive2Path, cmdLine, workingDirectory);
            }
            else
            {
                MessageBox.Show("Skipping texture archive creation.", "Textures archive already exists.", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }


            // Create main archive
            cmdLine = @"interface,geometries,materials,meshes,scripts -create="""
                    + Path.Combine(frmLoadOrder.StarfieldGamePath, "Data", Path.GetFileNameWithoutExtension(esm) + " - main.ba2") + @""""
                    + " -format=General";
            Debug.WriteLine($"cmdLine: {cmdLine}");
            if (!File.Exists(Path.Combine(frmLoadOrder.StarfieldGamePath, "Data", Path.GetFileNameWithoutExtension(esm) + " - main.ba2")))
                MakeArchive(archive2Path, cmdLine, workingDirectory);
            else
            {
                MessageBox.Show("Skipping main archive creation.", "Main archive already exists.", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            frmLoadOrder.returnStatus = 1;
            this.Close();
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
            }
        }

        private void btnEsmSelect_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Esm files (*.esm)|*.esm|All files (*.*)|*.*",
                Title = "Select the esm file to convert loose files to",
                InitialDirectory = Path.Combine(frmLoadOrder.StarfieldGamePath, "Data")
            };

            //Debug.WriteLine(openFileDialog.InitialDirectory);
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
    }
}