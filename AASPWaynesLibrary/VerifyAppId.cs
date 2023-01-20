//Used for a specific error that can happen during dataverse deployment
namespace AASPWaynesLibrary
{
    public partial class VerifyAppId : Form
    {
        readonly string message = "An API with a dynamics 365 connection is required to continue deployment.";
        public VerifyAppId()
        {
            InitializeComponent();
            this.button1.Click += (sender, e) =>
            {
                if (textBox1.Text != "")
                    this.Close();
                else
                    MessageBox.Show(message);
            };
        }

        public string GetTextInfo()
        {
            return this.textBox1.Text;
        }

        private void Form_Closing(object sender, FormClosingEventArgs e)
        {
            if (textBox1.Text == "")
            {
                e.Cancel = true;
                MessageBox.Show(message);
            }
        }
    }
}