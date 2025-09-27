using CMM.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace CMM.Load_Order_Editor
{
    public partial class frmAddModToProfile : Form
    {
        private readonly Tools tools = new();
        private string ModName;
        private frmLoadOrder.ActivityLog activityLog = frmLoadOrder.activityLog;
        private bool log = Properties.Settings.Default.Log;

        public frmAddModToProfile(List<string> items, string modName) // List of profiles and mod name
        {
            InitializeComponent();

            foreach (string item in items)
            {
                checkedListBox1.Items.Add(item);
            }
            this.Text = "Enable or Disable " + modName + " in Profile(s)"; // Change form title to name of mod being applied
            ModName = modName;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            List<string> fileContents = new();

            if (Directory.Exists(Properties.Settings.Default.ProfileFolder) && checkedListBox1.CheckedItems.Count > 0)
            {
                foreach (var item in checkedListBox1.CheckedItems)
                {
                    fileContents = File.ReadAllLines(Path.Combine(Properties.Settings.Default.ProfileFolder, item.ToString())).ToList();
                    fileContents.Remove("*" + ModName);
                    fileContents.Add(ModName); // Add the mod back without the * to indicate it is inactive
                    fileContents = fileContents.Distinct().ToList(); // Avoid adding a duplicate
                    File.WriteAllLines(Path.Combine(Properties.Settings.Default.ProfileFolder, item.ToString()), fileContents);
                    if (log)
                        activityLog.WriteLog("Removed " + ModName + " from " + item.ToString() + " profile.");
                }
                MessageBox.Show(checkedListBox1.CheckedItems.Count.ToString() + " profile(s) updated.", "Profiles Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
                return;

            this.Close();
        }

        private void btnSelectAll_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                checkedListBox1.SetItemChecked(i, true);
            }
        }

        private void btnSelectNone_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                checkedListBox1.SetItemChecked(i, false);
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            List<string> fileContents = new();
            string filePath;

            if (Directory.Exists(Properties.Settings.Default.ProfileFolder) && checkedListBox1.CheckedItems.Count > 0)
            {
                foreach (var item in checkedListBox1.CheckedItems)
                {
                    filePath = Path.Combine(Properties.Settings.Default.ProfileFolder, item.ToString());
                    fileContents = File.ReadAllLines(filePath).ToList();
                    fileContents.Remove(ModName);
                    fileContents.Remove("*" + ModName);
                    fileContents.Add("*" + ModName); // Add the mod back with the * to indicate it is active
                    fileContents = fileContents.Distinct().ToList(); // Avoid adding a duplicate
                    File.WriteAllLines(Path.Combine(Properties.Settings.Default.ProfileFolder, item.ToString()), fileContents);
                    if (log)
                        activityLog.WriteLog("Added " + ModName + " to " + item.ToString() + " profile.");
                }
                MessageBox.Show(checkedListBox1.CheckedItems.Count.ToString() + " profile(s) updated.", "Profiles Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
                return;

            this.Close();
        }
    }
}