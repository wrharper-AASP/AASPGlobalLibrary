namespace AASPWaynesLibrary
{
    //mainly used for auto deployment process because an API will be required one way or another
    public partial class APIRequiredWindow : Form
    {
        readonly string message = "An API with a dynamics 365 connection is required to continue deployment." + Environment.NewLine + "Press OK if you want the API to be automatically created instead.";
        public APIRequiredWindow()
        {
            InitializeComponent();
            this.button1.Click += (sender, e) =>
            {
                if (autoAppAccountCB.Checked)
                {
                    var results = MessageBox.Show("Make sure one does not already exist." + Environment.NewLine + "Press OK to continue.", "System Account Creation", MessageBoxButtons.OKCancel);
                    if (results == DialogResult.OK)
                    {
                        CloseViaButton();
                    }
                }
                else
                {
                    CloseViaButton();
                }
            };
        }

        void CloseViaButton()
        {
            if (appIdTB.Text != "" && objectTB.Text != "")
                this.Close();
            else
            {
                var results = MessageBox.Show(message, "Empty Fields Detected", MessageBoxButtons.OKCancel);
                if (results == DialogResult.OK)
                {
                    appIdTB.Text = "";
                    objectTB.Text = "";
                    autoAppAccountCB.Checked = true;
                    this.Close();
                }
            }
        }

        void CloseViaFormClosing(FormClosingEventArgs e)
        {
            if (appIdTB.Text == "" || objectTB.Text == "")
            {
                var results = MessageBox.Show(message, "Empty Fields Detected", MessageBoxButtons.OKCancel);
                if (results == DialogResult.Cancel)
                    e.Cancel = true;
                else
                {
                    appIdTB.Text = "";
                    objectTB.Text = "";
                    autoAppAccountCB.Checked = true;
                }
            }
        }

        public (string, string, bool) GetResponsePackage()
        {
            return (this.appIdTB.Text, this.objectTB.Text, this.autoAppAccountCB.Checked);
        }

        private void Form_Closing(object sender, FormClosingEventArgs e)
        {
            if (autoAppAccountCB.Checked)
            {
                var results = MessageBox.Show("Make sure a Application User does not already exist." + Environment.NewLine + "Press OK to continue.", "System Account Creation", MessageBoxButtons.OKCancel);
                if (results == DialogResult.OK)
                {
                    CloseViaFormClosing(e);
                }
                else
                    e.Cancel = true;
            }
            else
            {
                CloseViaFormClosing(e);
            }
        }

        private void Link_Clicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Globals.OpenLink(((LinkLabel)sender).Text);
        }
    }
}
