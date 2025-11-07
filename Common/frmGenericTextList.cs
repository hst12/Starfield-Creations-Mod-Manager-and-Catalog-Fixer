using System.Collections.Generic;
using System.Windows.Forms;

namespace hstCMM.Common
{
    public partial class frmGenericTextList : Form
    {
        public frmGenericTextList(string windowTitle, List<string> textLines)
        {
            InitializeComponent();
            this.Text = windowTitle;
            foreach (var item in textLines)
                richTextBox1.Text += item + "\n";
        }

        private void btnOk_Click(object sender, System.EventArgs e)
        {
            this.Close();
        }
    }
}