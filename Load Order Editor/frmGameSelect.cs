using System;
using System.Windows.Forms;

namespace CMM.Load_Order_Editor
{
    public partial class frmGameSelect : Form
    {
        public frmGameSelect()
        {
            InitializeComponent();
            switch (Properties.Settings.Default.Game)
            {
                case 0:
                    radStarfield.Checked = true;
                    break;

                case 1:
                    radFallout5.Checked = true;
                    break;

                case 2:
                    radES6.Checked = true;
                    break;

                default:
                    radStarfield.Checked = true;
                    break;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnSelect_Click(object sender, EventArgs e)
        {
            if (radStarfield.Checked)
                Properties.Settings.Default.Game = 0; // Starfield
            if (radFallout5.Checked)
                Properties.Settings.Default.Game = 1; // Fallout 5
            if (radES6.Checked)
                Properties.Settings.Default.Game = 2; //ES6
            this.Close();
        }
    }
}