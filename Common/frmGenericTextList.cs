using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
