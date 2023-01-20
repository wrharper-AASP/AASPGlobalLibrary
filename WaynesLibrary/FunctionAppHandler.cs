using System.Net;
using System.Text;
using Azure.ResourceManager.AppService;

//Removes the problems with Function App Zip Deploying requiring you to use visual studio to publish updates.
namespace WaynesLibrary
{
    public static class FunctionAppHandler
    {
        static async Task DeployZip(PublishingUserResource user, Stream s, string deployUrl, string deploymentStatusUrl)
        {
            using HttpClient client = new();
            var byteArray = Encoding.ASCII.GetBytes(user.Data.PublishingUserName + ":" + user.Data.PublishingPassword);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            HttpContent fileContent = new StreamContent(s);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/zip");
            var r = client.PostAsync(deployUrl, fileContent).Result;

            int NumStatusChecks = 600;
            int numChecks = 0;
            while (r.StatusCode == HttpStatusCode.Accepted && numChecks < NumStatusChecks)
            {
                await Task.Delay(2000);
                r = client.GetAsync(deploymentStatusUrl).Result;
                numChecks++;
                Console.Write(", " + numChecks.ToString());
            }
        }
        public static async Task ZipDeploy(PublishingUserResource user, string path)
        {
            string deployUrl = user.Data.ScmUri + "/api/zipdeploy?isAsync=true";
            string deploymentStatusUrl = user.Data.ScmUri + "/api/deployments/latest";

            if (path.StartsWith("https://"))
            {
                using HttpClient client = new();
                //await client.DownloadFileAsync(url, pathwithfilename);
                using var s = await client.GetStreamAsync(new Uri(path));
                await DeployZip(user, s, deployUrl, deploymentStatusUrl);
            }
            else
            {
                using var s = File.OpenRead(path);
                await DeployZip(user, s, deployUrl, deploymentStatusUrl);
            }
        }
    }
}
