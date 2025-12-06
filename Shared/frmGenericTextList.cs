using System.Collections.Generic;
using System.Windows.Forms;

namespace hstCMM.Common
{
    public partial class frmGenericTextList : Form
    {
        public frmGenericTextList(string windowTitle, List<string> textLines)
        {
            InitializeComponent();
            frmLoadOrder.returnStatus = 1;
            this.Text = windowTitle;
            foreach (var item in textLines)
                richTextBox1.Text += item + "\n";
        }

        private void btnOk_Click(object sender, System.EventArgs e)
        {
            this.Close();
        }

        private void btnClose_Click(object sender, System.EventArgs e)
        {
            frmLoadOrder.returnStatus = 0; // No choice made
            this.Close();
        }
    }
}