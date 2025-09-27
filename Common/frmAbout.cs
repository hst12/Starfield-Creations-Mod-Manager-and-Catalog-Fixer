using CMM.Common;
using System.IO;
using System.Windows.Forms;

namespace CMM
{
    public partial class frmAbout : Form
    {
        readonly Tools tools = new();
        public frmAbout()
        {
            InitializeComponent();
            string Readme = File.ReadAllText(Path.Combine(Tools.DocumentationFolder , "Readme.txt"));
            string AboutText = Application.ProductName + " " + File.ReadAllText(Path.Combine(Tools.CommonFolder , "App Version.txt")) + "\n\n" + Readme;
            richTextBox1.Text = AboutText;
        }
    }
}
