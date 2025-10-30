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
        private Tools.GameLibrary gl = new();

        public frmGameSelect()
        {
            InitializeComponent();

            int GameIndex = Properties.Settings.Default.Game;
            string gamePath;
            for (int i = 0; i < gl.Games.Count; i++)
            {
                gamePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", gl.GameName(i));
                if (Directory.Exists(gamePath)) // Only show games that are installed
                {
                    RadioButton rb = new RadioButton
                    {
                        Text = gl.GameName(i),
                        Name = $"radioButton{i}",
                        AutoSize = true,
                        Tag = i,
                        Checked = GameIndex == i
                    };
                    flowLayoutPanel1.Controls.Add(rb);
                }
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            frmLoadOrder.returnStatus = 1; // Signal cancel
            this.Close();
        }

        private void btnSelect_Click(object sender, EventArgs e)
        {
            List<string> GamePaths = new();
            string gamePath;
            for (int i = 0; i < gl.Games.Count; i++)
            {
                gamePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", gl.GameName(i));
                if (Directory.Exists(gamePath))
                    GamePaths.Add(gamePath);
            }
            if (GamePaths.Count > 0)
            {
                RadioButton selectedRadio = null;

                foreach (Control control in flowLayoutPanel1.Controls)
                {
                    if (control is RadioButton rb && rb.Checked)
                    {
                        selectedRadio = rb;
                        Properties.Settings.Default.Game = (int)rb.Tag;
                        Properties.Settings.Default.Save();
                        break;
                    }
                }
                // Serialize GamePaths to JSON and save to file
                string json = JsonSerializer.Serialize(GamePaths, new JsonSerializerOptions { WriteIndented = true });
                string filePath = Path.Combine(Tools.LocalAppDataPath, "GamePaths.json");
                File.WriteAllText(filePath, json);
            }
            else
            {
                MessageBox.Show("Selected game not found");
                return;
            }
            this.Close();
        }
    }
}