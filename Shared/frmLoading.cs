using System.Windows.Forms;

namespace hstCMM.Common
{
    public partial class frmLoading : Form
    {
        public frmLoading(string msgText)
        {
            InitializeComponent();
            this.CenterToScreen();
            txtMessage.Text = msgText;
        }

    }
}
