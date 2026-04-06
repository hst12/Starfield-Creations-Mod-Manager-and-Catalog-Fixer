using hstCMM.Shared;
using System.IO;
using System.Windows.Forms;

namespace hstCMM
{
    public partial class frmAbout : Form
    {
        private readonly Tools tools = new();

        public frmAbout()
        {
            InitializeComponent();

            string Readme = File.ReadAllText(Path.Combine(Tools.DocumentationFolder, "Readme.txt"));
            this.Text = tools.AppName() + " - Version:" + File.ReadAllText(Path.Combine(Tools.CommonFolder, "App Version.txt"));
            richTextBox1.Text = Readme;
        }
    }
}