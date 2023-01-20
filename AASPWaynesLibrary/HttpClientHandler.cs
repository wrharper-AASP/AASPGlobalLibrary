using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

//Removes complexity for calling REST
//Currently handles Dynamics 365, OpenAI, and all standard Bearer tokens
//All clients are initialized and disposed right after use
namespace AASPWaynesLibrary
{
    public static class HttpClientHandler
    {
        static AuthenticationHeaderValue SetAuthorizationBearerHeader(string token)
        {
            return new AuthenticationHeaderValue("Bearer", token);
        }
        static MediaTypeWithQualityHeaderValue OnlyAllowJSON() { return new MediaTypeWithQualityHeaderValue("application/json"); }
        static StringContent ConvertJsonStringToHttpContent(string json)
        {
            json = Newtonsoft.Json.Linq.JToken.Parse(json).ToString();
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        #region DELETE Handling
        public static async Task<string> DeleteAsync(string token, string url)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = SetAuthorizationBearerHeader(token);
            client.DefaultRequestHeaders.Accept.Add(OnlyAllowJSON());
            var response = await client.DeleteAsync(url);
            return await response.Content.ReadAsStringAsync();
        }
        public static async Task<string> DeleteOdataAsync(string token, string url)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = SetAuthorizationBearerHeader(token);
            client.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
            client.DefaultRequestHeaders.Add("OData-Version", "4.0");
            client.DefaultRequestHeaders.Accept.Add(OnlyAllowJSON());
            var response = await client.DeleteAsync(url);
            return await response.Content.ReadAsStringAsync();
        }
        #endregion

        #region PATCH Handling
        public static async Task<string> PatchJsonStringOdataAsync(string token, string url, string json)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = SetAuthorizationBearerHeader(token);
            client.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
            client.DefaultRequestHeaders.Add("OData-Version", "4.0");
            client.DefaultRequestHeaders.Accept.Add(OnlyAllowJSON());
            HttpContent content = ConvertJsonStringToHttpContent(json);
            var response = await client.PatchAsync(url, content);
            return await response.Content.ReadAsStringAsync();
        }
        #endregion

        #region GET Handling
        public static async Task<string> GetJsonStringAsync(string token, string baseUrl, string requestUrl="")
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = SetAuthorizationBearerHeader(token);
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Accept.Add(OnlyAllowJSON());
            var response = await client.GetAsync(baseUrl + requestUrl);
            return await response.Content.ReadAsStringAsync();
        }
        public static async Task<string> GetJsonStringOdataAsync(string token, string baseUrl, string requestUrl = "")
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = SetAuthorizationBearerHeader(token);
            client.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
            client.DefaultRequestHeaders.Add("OData-Version", "4.0");
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Accept.Add(OnlyAllowJSON());
            var response = await client.GetAsync(baseUrl + requestUrl);
            return await response.Content.ReadAsStringAsync();
        }
        public static async Task<TValue> GetJsonAsync<TValue>(string token, string baseUrl, string requestUrl, TValue json)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = SetAuthorizationBearerHeader(token);
            client.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
            client.DefaultRequestHeaders.Add("OData-Version", "4.0");
            client.BaseAddress = new Uri(baseUrl);
            var type = json.GetType();
            return (TValue)await client.GetFromJsonAsync(baseUrl + requestUrl, type); //"api/data/v9.2/systemusers", type);
        }
        #endregion

        #region POST Handling
        public static async Task<string> PostJsonStringBearerWithOData(string token, string baseUrl, string requestUrl, string json)
        {
            HttpContent httpContent = ConvertJsonStringToHttpContent(json);
            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = SetAuthorizationBearerHeader(token);
            client.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
            client.DefaultRequestHeaders.Add("OData-Version", "4.0");
            client.BaseAddress = new Uri(baseUrl);

            var response = await client.PostAsync(baseUrl + requestUrl, httpContent);
            return await response.Content.ReadAsStringAsync();
        }
        public static async Task<string> PostJsonStringBearer(string token, string baseUrl, string requestUrl, string json)
        {
            HttpContent httpContent = ConvertJsonStringToHttpContent(json);
            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = SetAuthorizationBearerHeader(token);
            client.DefaultRequestHeaders.Accept.Add(OnlyAllowJSON());
            client.BaseAddress = new Uri(baseUrl);

            var response = await client.PostAsync(baseUrl + requestUrl, httpContent);
            return await response.Content.ReadAsStringAsync();
        }
        public static async Task<string> PostJsonBearer<TValue>(string token, string baseUrl, string requestUrl, TValue json)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = SetAuthorizationBearerHeader(token);
            client.BaseAddress = new Uri(baseUrl);

            var response = await client.PostAsJsonAsync(baseUrl + requestUrl, json);
            return await response.Content.ReadAsStringAsync();
        }
        public static async Task<string> PostJsonObject<TValue>(TValue json, string url)
        {
            using HttpClient client = new();
            var response = await client.PostAsJsonAsync(url, json);
            return await response.Content.ReadAsStringAsync();
        }
        public static async Task<string> PostJsonOpenAIBearerWithOrg<TValue>(string OrgId, string token, string baseUrl, string requestUrl, TValue json)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = SetAuthorizationBearerHeader(token);
            client.DefaultRequestHeaders.Add("OpenAI-Organization", OrgId);
            client.BaseAddress = new Uri(baseUrl);

            var response = await client.PostAsJsonAsync(baseUrl + requestUrl, json);
            return await response.Content.ReadAsStringAsync();
        }
        public static async Task<string> PostJsonOpenAIBearerWithOrgString(string OrgId, string token, string baseUrl, string requestUrl, string json)
        {
            HttpContent httpContent = ConvertJsonStringToHttpContent(json);
            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = SetAuthorizationBearerHeader(token);
            client.DefaultRequestHeaders.Add("OpenAI-Organization", OrgId);
            client.BaseAddress = new Uri(baseUrl);

            var response = await client.PostAsync(baseUrl + requestUrl, httpContent);
            return await response.Content.ReadAsStringAsync();
        }
        #endregion
    }
}
