using Starfield_Tools.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Starfield_Tools.Load_Order_Editor
{
    public partial class frmOrphaned : Form
    {
        readonly Tools tools = new();
        Tools.ActivityLog activityLog = new(Path.Combine(Tools.LocalAppDataPath, "Activity Log.txt"));
        bool log= Properties.Settings.Default.Log;
        public frmOrphaned(List<string> orphaned)
        {
            InitializeComponent();
            Tools.ActivityLog activityLog = new(Path.Combine(Tools.LocalAppDataPath,"Activity Log.txt"));
            long fileSize = 0;

            foreach (var item in orphaned)
            {
                checkedListBox1.Items.Add(Path.GetFileName(item));
                FileInfo fileInfo = new FileInfo(item);
                fileSize += fileInfo.Length;
            }
            toolStripStatusLabel1.Text = "Total Size: " + (fileSize / (1024 * 1024)).ToString() + " Mbytes";
        }

        private void btnCancel_Click(object sender, EventArgs e)
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

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (checkedListBox1.CheckedItems.Count == 0)
                return;

            if (Tools.ConfirmAction("Are you sure you want to delete the selected files?", "Last Chance", MessageBoxButtons.YesNo) == DialogResult.No)
            {
                return;
            }
            try
            {
                foreach (var item in checkedListBox1.CheckedItems)
                {
                    File.Delete(Path.Combine(frmLoadOrder.StarfieldGamePath, "Data", item.ToString()));
                    if (log)
                        activityLog.WriteLog($"Deleted orphaned archive {item} from Data folder.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error deleting file: " + ex.Message);
                this.Close();
                return;
            }

            MessageBox.Show("Files deleted successfully.");
            this.Close();
        }
    }
}
