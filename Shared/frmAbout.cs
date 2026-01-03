using hstCMM.Shared;
using System.IO;
using System.Reflection;
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
            /*string AboutText = tools.AppName() (+ " " + System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription +
                " - Version:" + File.ReadAllText(Path.Combine(Tools.CommonFolder, "App Version.txt")) + "\n\n" + Readme;*/
            this.Text = tools.AppName()+ " - Version:" + File.ReadAllText(Path.Combine(Tools.CommonFolder, "App Version.txt"));
            richTextBox1.Text = Readme;
        }
    }
}