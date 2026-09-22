using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace hstCMM.Load_Order_Editor
{
    public partial class frmKeymap : Form
    {
        public frmKeymap()
        {
            InitializeComponent();

            // 1. Get the working area of the screen where the form is currently located.
            // WorkingArea excludes the Windows taskbar, which prevents the form from going behind it.
            var workingArea = Screen.FromControl(this).WorkingArea;

            // 2. Calculate 75% of the screen's width and height
            int newWidth = (int)(workingArea.Width * 0.75);
            int newHeight = (int)(workingArea.Height * 0.75);

            // 3. Apply the new size to the form
            this.Size = new System.Drawing.Size(newWidth, newHeight);

            // Center the form on the screen after resizing
            this.Location = new System.Drawing.Point(
                workingArea.Left + (workingArea.Width - this.Width) / 2,
                workingArea.Top + (workingArea.Height - this.Height) / 2
            );
        }
    }
}