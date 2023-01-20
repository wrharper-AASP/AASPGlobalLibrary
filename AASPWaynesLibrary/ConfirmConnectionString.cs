namespace AASPWaynesLibrary
{
    //This was more for dev overrides if the need arises to alter dataverse connection strings during deployment.
    //Currently not needed and is optional
    public partial class ConfirmConnectionString : Form
    {
        readonly string message = "An API with a dynamics 365 connection is required to continue deployment.";
        public ConfirmConnectionString()
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
