namespace WaynesLibrary
{
    //Used so you can bring up a progress bar across any application.
    public partial class DynamicProgressBar : Form
    {
        public DynamicProgressBar()
        {
            InitializeComponent();
        }

        public void SetMax(int max)
        {
            progressBar1.Maximum = max;
        }

        public void UpdateProgress(int current, string customstart="")
        {
            label2.Text = customstart + current.ToString() + " / " + progressBar1.Maximum.ToString();
            progressBar1.Value = current;
            progressBar1.Update();
        }
    }
}
