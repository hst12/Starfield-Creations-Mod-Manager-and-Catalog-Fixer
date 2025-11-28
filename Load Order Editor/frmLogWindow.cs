using System;
using System.Windows.Forms;

namespace hstCMM.Load_Order_Editor
{
    public partial class frmLogWindow : Form
    {
        public frmLogWindow()
        {
            InitializeComponent();
        }

        public void AppendLog(string message)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action<string>(AppendLog), message);
            }
            else
            {
                txtLog.AppendText(message);
                txtLog.ScrollToCaret();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            txtLog.Clear();
        }
    }
}