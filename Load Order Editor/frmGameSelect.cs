using hstCMM.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace hstCMM.Load_Order_Editor
{
    public partial class frmGameSelect : Form
    {
        private readonly Tools tools = new();

        public frmGameSelect()
        {
            InitializeComponent();
            switch (Properties.Settings.Default.Game)
            {
                case 0:
                    radStarfield.Checked = true;
                    break;

                case 1:
                    radFallout5.Checked = true;
                    break;

                case 2:
                    radES6.Checked = true;
                    break;

                default:
                    radStarfield.Checked = true;
                    break;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnSelect_Click(object sender, EventArgs e)
        {
            string[] GameNames = { "Starfield", "Fallout 5", "ES6" };
            List<string> GamePaths = new();
            for (int i = 0; i < GameNames.Length; i++)
            {
                GamePaths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", GamePaths[i]));
            }

            if (radStarfield.Checked)
            {
                Properties.Settings.Default.Game = 0; // Starfield
            }
            if (radFallout5.Checked)
            {
                Properties.Settings.Default.Game = 1; // Fallout 5
            }
            if (radES6.Checked)
            {
                Properties.Settings.Default.Game = 2; //ES6
            }

            // Serialize GamePaths to JSON and save to file
            string json = JsonSerializer.Serialize(GamePaths, new JsonSerializerOptions { WriteIndented = true });
            string filePath = Path.Combine(Tools.LocalAppDataPath, "GamePaths.json");
            File.WriteAllText(filePath, json);

            this.Close();
        }
    }
}