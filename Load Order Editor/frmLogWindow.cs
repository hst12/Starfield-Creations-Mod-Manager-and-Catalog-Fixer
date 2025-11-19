using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Controls;
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
    }
}
