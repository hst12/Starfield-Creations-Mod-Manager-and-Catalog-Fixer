using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Starfield_Tools.Load_Order_Editor
{
    public partial class frmProfileCompare : Form
    {
        public frmProfileCompare(List<string> Difference)
        {
            InitializeComponent();
            foreach (string str in Difference)
            {
                richTextBox1.AppendText(str + Environment.NewLine);
            }
        }
    }
}
