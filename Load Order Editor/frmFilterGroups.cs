using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace hstCMM.Load_Order_Editor
{
    public partial class frmFilterGroups : Form
    {
        public frmFilterGroups(List<string> groupList)
        {
            InitializeComponent();
            foreach (string group in groupList)
            {
                clbGroups.Items.Add(group, false);
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            GetSelectedGroups();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnSelectAll_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < clbGroups.Items.Count; i++)
            {
                clbGroups.SetItemChecked(i, true);
            }
        }

        private void btnNone_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < clbGroups.Items.Count; i++)
            {
                clbGroups.SetItemChecked(i, false);
            }
        }

        public List<string> GetSelectedGroups()
        {
            List<string> selectedGroups = new List<string>();
            foreach (var item in clbGroups.CheckedItems)
            {
                selectedGroups.Add(item.ToString());
            }
            return selectedGroups;
        }
    }
}
