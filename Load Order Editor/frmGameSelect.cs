using hstCMM.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using static hstCMM.Shared.Tools;

namespace hstCMM.Load_Order_Editor
{
    public partial class frmGameSelect : Form
    {
        private readonly Tools tools = new();
        //private Tools.GameLibrary gl = new();

        public frmGameSelect()
        {
            InitializeComponent();
            

            int GameIndex = Properties.Settings.Default.Game;
            var gl = GameLibrary.GetById(GameIndex);
            for (int i = 0; i < Tools.GameLibrary.Games.Count ; i++)
            {
                if (Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", GameLibrary.GetById(i).DocFolder)))
                {
                    RadioButton rb = new RadioButton
                    {
                        Text = GameLibrary.GetById(i).GameName,
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
            for (int i = 0; i <GameLibrary.Games.Count; i++)
            {
                gamePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", GameLibrary.GetById(i).DocFolder);
  
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
                        frmLoadOrder.GameName = rb.Text;
                        if (Properties.Settings.Default.GameVersion == frmLoadOrder.Steam)
                        {
                            Properties.Settings.Default.GamePath = frmLoadOrder.GamePath = tools.GetSteamGamePath(frmLoadOrder.GameName);
                            //MessageBox.Show($"Game path set to: {frmLoadOrder.GamePath}");
                        }
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