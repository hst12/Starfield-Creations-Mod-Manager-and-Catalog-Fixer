using hstCMM.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace hstCMM.Load_Order_Editor
{
    public partial class frmSFSEPlugins : Form
    {
        private readonly Tools tools = new();
        private readonly string GamePath = frmLoadOrder.GamePath;
        
        public frmSFSEPlugins()
        {
            InitializeComponent();
           
            List<string> SFSEPluginList;
            string[] pluginPattern = { "*.dll", "*.disabled" };
            string pluginName;
            string SFSEPluginPath = Path.Combine(GamePath, @"Data\SFSE\Plugins");

            if (!Directory.Exists(SFSEPluginPath))
            {
                MessageBox.Show("Unable to find SFSE Plugins Directory");
                return;
            }

            foreach (string pattern in pluginPattern)
            {
                SFSEPluginList = Directory.GetFiles(SFSEPluginPath, pattern, SearchOption.TopDirectoryOnly).ToList();
                foreach (string plugin in SFSEPluginList)
                {
                    pluginName = Path.GetFileName(plugin);
                    if (pluginName.EndsWith(".dll"))
                        chkSFSEPlugins.Items.Add(Path.GetFileNameWithoutExtension(pluginName), true);
                    else
                        chkSFSEPlugins.Items.Add(pluginName.Split('.')[0]); // Get name before 1st dot
                }
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnSelectAll_Click(object sender, EventArgs e)
        {
            foreach (int i in Enumerable.Range(0, chkSFSEPlugins.Items.Count))
            {
                chkSFSEPlugins.SetItemChecked(i, true);
            }
        }

        private void btnSelectNone_Click(object sender, EventArgs e)
        {
            foreach (int i in Enumerable.Range(0, chkSFSEPlugins.Items.Count))
            {
                chkSFSEPlugins.SetItemChecked(i, false);
            }
        }

        private void btnOpenFolder_Click(object sender, EventArgs e)
        {
            Tools.OpenFolder(Path.Combine(GamePath, @"Data\SFSE\Plugins"));
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            string SFSEPluginPath = Path.Combine(GamePath, @"Data\SFSE\Plugins");
            foreach (string plugin in Directory.GetFiles(SFSEPluginPath, "*.*", SearchOption.TopDirectoryOnly))
            {
                string pluginName = Path.GetFileName(plugin);
                string baseName = pluginName.Split('.')[0]; // Get name before 1st dot
                if (chkSFSEPlugins.CheckedItems.Contains(baseName))
                {
                    // Enable plugin
                    if (pluginName.EndsWith(".disabled"))
                    {
                        string newName = Path.Combine(SFSEPluginPath, baseName + ".dll");
                        File.Move(plugin, newName);
                    }
                }
                else
                {
                    // Disable plugin
                    if (pluginName.EndsWith(".dll"))
                    {
                        string newName = Path.Combine(SFSEPluginPath, baseName + ".dll.disabled");
                        File.Move(plugin, newName);
                    }
                }
            }
            Close();
        }
    }
}