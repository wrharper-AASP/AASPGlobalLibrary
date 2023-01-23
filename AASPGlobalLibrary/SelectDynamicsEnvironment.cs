//using System.Net.Http.Headers;
//using System.Net.Http.Json;

//Used for any apps that need to log into Dynamics 365
//Allows you to select any environment and everything can be used based on the selected environment
namespace AASPGlobalLibrary
{
    public partial class SelectDynamicsEnvironment : Form
    {
        class JSONGetDataverseEnvironments
        {
            public string? odatacontext { get; set; }
            public Value[]? value { get; set; }

            public class Value
            {
                public string? Id { get; set; }
                public string? UniqueName { get; set; }
                public string? UrlName { get; set; }
                public string? FriendlyName { get; set; }
                public int? State { get; set; }
                public string? Version { get; set; }
                public string? Url { get; set; }
                public string? ApiUrl { get; set; }
                public DateTime? LastUpdated { get; set; }
                public string? SchemaType { get; set; }
            }

        }
        JSONGetDataverseEnvironments info = new();

        public SelectDynamicsEnvironment(DataverseHandler dataverseHandler)
        {
            InitializeComponent();
            comboBox1.Enabled = false;
            button1.Enabled = false;
            button1.Click += (sender, e) =>
            {
#pragma warning disable CS8604
                dataverseHandler.Init(info.value[comboBox1.SelectedIndex].UrlName);
#pragma warning restore CS8604
                Close();
            };
        }

        //used to keep prefix internal
        public string InitializeWithPrefixReturn(DataverseHandler dataverseHandler)
        {
            ShowDialog();
            return dataverseHandler.DbInfo.StartingPrefix;
        }

        private async void Form_Load(object sender, EventArgs e)
        {
            info = await HttpClientHandler.GetJsonAsync(await TokenHandler.GetGlobalDynamicsImpersonationToken(), Globals.Dynamics365Distro, info);
            /*var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await TokenHandler.GetGlobalDynamicsImpersonationToken());
            HttpRequestMessage request = new(new("GET"), Globals.Dynamics365Distro);
            var response = await httpClient.SendAsync(request);*/

//#pragma warning disable CS8601 // Converting null literal or possible null value to non-nullable type.
            //info = await response.Content.ReadFromJsonAsync<JSONGetDataverseEnvironments>();
//#pragma warning restore CS8601 // Converting null literal or possible null value to non-nullable type.

            if (info.value.Length > 0)
            {
                for (int i = 0; i < info.value.Length; i++)
                {
                    comboBox1.Items.Add(info.value[i].FriendlyName);
                }
                comboBox1.SelectedIndex = 0;
            }
        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBox1.Enabled = true;
            button1.Enabled = true;
        }
    }
}
