using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace hstCMM.Load_Order_Editor
{
    public partial class frmMoveMods : Form
    {
        public frmMoveMods()
        {
            InitializeComponent();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            if (radCreations.Checked)
                frmLoadOrder.returnStatus = 1; // Move to Creations
            if (radOther.Checked)
                frmLoadOrder.returnStatus = 2; // Move to Other Mods
            if (radBoth.Checked)
                frmLoadOrder.returnStatus = 3; // Move to Both
            if (radBlocked.Checked)
                frmLoadOrder.returnStatus = 4; // Move Blocked Mods
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            frmLoadOrder.returnStatus = 0; // No choice made
            this.Close();
        }
    }
}
