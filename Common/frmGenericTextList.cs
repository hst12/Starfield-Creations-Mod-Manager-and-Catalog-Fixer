using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Starfield_Tools.Common
{
    public partial class frmGenericTextList : Form
    {
        public frmGenericTextList(List<string> files)
        {
            InitializeComponent();

            foreach (var item in files)
            {
                richTextBox1.Text += item + Environment.NewLine;
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
