using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

namespace Starfield_Tools.Load_Order_Editor
{
    public partial class frmDisplaySettings : Form
    {
        public frmDisplaySettings()
        {
            InitializeComponent();
            DisplayAllSettings();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // Add this method to enumerate and display all settings
        private void DisplayAllSettings()
        {
            // Find the rich text box control (assumed to be named richTextBox1)
            //var rtb = this.Controls["richTextBox1"] as RichTextBox;
            var rtb = richTextBox1;
            if (rtb == null)
                return;

            rtb.Clear();
            rtb.AppendText("Application Settings:\n\n");

            var settings = Properties.Settings.Default;
            var props = settings.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach (var prop in props)
            {
                Debug.WriteLine($"Property: {prop.Name}, Type: {prop.PropertyType}");
                try
                {
                    var value = prop.GetValue(settings, null);
                    rtb.AppendText($"{prop.Name}: {value}\n");
                }
                catch
                {
                    rtb.AppendText($"{prop.Name}: <error reading value>\n");
                }
            }
        }

        // Call this method when you want to display settings, e.g. from a button click
        private void btnDisplaySettings_Click(object sender, EventArgs e)
        {
            DisplayAllSettings();
        }
    }
}
