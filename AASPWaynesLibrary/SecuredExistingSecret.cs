//Pop this up if a secret is needed for anything
//Needs improvement, currently used for API secret to Key Vault
//Since Key Vault must be a string before being sent anyway, it is currently not a real SecuredString
namespace AASPWaynesLibrary
{
    public partial class SecuredExistingSecret : Form
    {
        public SecuredExistingSecret()
        {
            InitializeComponent();
        }

        public string GetSecuredString()
        {
            return maskedTextBox1.Text;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
