using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

//Removes complexity for calling REST
//Currently handles Dynamics 365, OpenAI, and all standard Bearer tokens
//All clients are initialized and disposed right after use

//example usage of custom header calls:
/*
//dictionary example (easiest to tell what is going on)
Dictionary<string, string> odataInfo = new()
{
    { "OData-MaxVersion", "4.0" },
    { "OData-Version", "4.0" }
};
//array example (fastest processing)
string[] odataNames = { "OData-MaxVersion", "OData-Version" };
string[] odataValues = { "4.0", "4.0" };
//LINQ list example (not recommended but added as a feature)
List<string> odataNames = new()
{
    "OData-MaxVersion",
    "OData-Version"
};
List<string> odataValues = new()
{
    "4.0",
    "4.0"
};
*/
namespace AASPGlobalLibrary
{
    public static class HttpClientHandler
    {
        public static AuthenticationHeaderValue SetAuthorizationBearerHeader(string token)
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
        public static async Task<string> DeleteJsonBearerCustomHeadersAsync(string token, string url, string[] headernames, string[] headervalues)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = SetAuthorizationBearerHeader(token);
            for (int i = 0; i < headernames.Length; i++)
            {
                client.DefaultRequestHeaders.Add(headernames[i], headervalues[i]);
            }
            client.DefaultRequestHeaders.Accept.Add(OnlyAllowJSON());
            var response = await client.DeleteAsync(url);
            return await response.Content.ReadAsStringAsync();
        }
        public static async Task<string> DeleteJsonBearerCustomHeadersAsync(string token, string url, List<string> headernames, List<string> headervalues)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = SetAuthorizationBearerHeader(token);
            for (int i = 0; i < headernames.Count; i++)
            {
                client.DefaultRequestHeaders.Add(headernames[i], headervalues[i]);
            }
            client.DefaultRequestHeaders.Accept.Add(OnlyAllowJSON());
            var response = await client.DeleteAsync(url);
            return await response.Content.ReadAsStringAsync();
        }
        public static async Task<string> DeleteJsonBearerCustomHeadersAsync(string token, string url, Dictionary<string, string> headers)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = SetAuthorizationBearerHeader(token);
            foreach (var header in headers)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
            client.DefaultRequestHeaders.Accept.Add(OnlyAllowJSON());
            var response = await client.DeleteAsync(url);
            return await response.Content.ReadAsStringAsync();
        }
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
            string[] odataNames = { "OData-MaxVersion", "OData-Version" };
            string[] odataValues = { "4.0", "4.0" };
            return await DeleteJsonBearerCustomHeadersAsync(token, url, odataNames, odataValues);
            /*using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = SetAuthorizationBearerHeader(token);
            client.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
            client.DefaultRequestHeaders.Add("OData-Version", "4.0");
            client.DefaultRequestHeaders.Accept.Add(OnlyAllowJSON());
            var response = await client.DeleteAsync(url);
            return await response.Content.ReadAsStringAsync();*/
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
        public static async Task<string> GetJsonBearerStringCustomHeadersAsync(string token, string baseUrl, string[] headernames, string[] headervalues, string requestUrl = "")
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = SetAuthorizationBearerHeader(token);
            client.BaseAddress = new Uri(baseUrl);
            for (int i = 0; i < headernames.Length; i++)
            {
                client.DefaultRequestHeaders.Add(headernames[i], headervalues[i]);
            }
            client.DefaultRequestHeaders.Accept.Add(OnlyAllowJSON());
            var response = await client.GetAsync(baseUrl + requestUrl);
            return await response.Content.ReadAsStringAsync();
        }
        public static async Task<T> GetJsonBearerStringCustomHeadersAsync<T>(string token, string baseUrl, string[] headernames, string[] headervalues, T json, string requestUrl = "")
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = SetAuthorizationBearerHeader(token);
            client.BaseAddress = new Uri(baseUrl);
            for (int i = 0; i < headernames.Length; i++)
            {
                client.DefaultRequestHeaders.Add(headernames[i], headervalues[i]);
            }
            client.DefaultRequestHeaders.Accept.Add(OnlyAllowJSON());
            var type = json.GetType();
            return (T)await client.GetFromJsonAsync(baseUrl + requestUrl, type);
        }
        public static async Task<string> GetJsonBearerStringCustomHeadersAsync(string token, string baseUrl, List<string> headernames, List<string> headervalues, string requestUrl = "")
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = SetAuthorizationBearerHeader(token);
            client.BaseAddress = new Uri(baseUrl);
            for (int i = 0; i < headernames.Count; i++)
            {
                client.DefaultRequestHeaders.Add(headernames[i], headervalues[i]);
            }
            client.DefaultRequestHeaders.Accept.Add(OnlyAllowJSON());
            var response = await client.GetAsync(baseUrl + requestUrl);
            return await response.Content.ReadAsStringAsync();
        }
        public static async Task<T> GetJsonBearerStringCustomHeadersAsync<T>(string token, string baseUrl, List<string> headernames, List<string> headervalues, T json, string requestUrl = "")
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = SetAuthorizationBearerHeader(token);
            client.BaseAddress = new Uri(baseUrl);
            for (int i = 0; i < headernames.Count; i++)
            {
                client.DefaultRequestHeaders.Add(headernames[i], headervalues[i]);
            }
            client.DefaultRequestHeaders.Accept.Add(OnlyAllowJSON());
            var type = json.GetType();
            return (T)await client.GetFromJsonAsync(baseUrl + requestUrl, type);
        }
        public static async Task<string> GetJsonBearerStringCustomHeadersAsync(string token, string baseUrl, Dictionary<string, string> headers, string requestUrl = "")
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = SetAuthorizationBearerHeader(token);
            client.BaseAddress = new Uri(baseUrl);
            foreach (var header in headers)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
            client.DefaultRequestHeaders.Accept.Add(OnlyAllowJSON());
            var response = await client.GetAsync(baseUrl + requestUrl);
            return await response.Content.ReadAsStringAsync();
        }
        public static async Task<T> GetJsonBearerStringCustomHeadersAsync<T>(string token, string baseUrl, Dictionary<string, string> headers, T json, string requestUrl = "")
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = SetAuthorizationBearerHeader(token);
            client.BaseAddress = new Uri(baseUrl);
            foreach (var header in headers)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
            client.DefaultRequestHeaders.Accept.Add(OnlyAllowJSON());
            var type = json.GetType();
            return (T)await client.GetFromJsonAsync(baseUrl + requestUrl, type);
        }
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
            string[] odataNames = { "OData-MaxVersion", "OData-Version" };
            string[] odataValues = { "4.0", "4.0" };
            return await GetJsonBearerStringCustomHeadersAsync(token, baseUrl, odataNames, odataValues, requestUrl);
            /*using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = SetAuthorizationBearerHeader(token);
            client.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
            client.DefaultRequestHeaders.Add("OData-Version", "4.0");
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Accept.Add(OnlyAllowJSON());
            var response = await client.GetAsync(baseUrl + requestUrl);
            return await response.Content.ReadAsStringAsync();*/
        }
        public static async Task<T> GetJsonAsync<T>(string token, string baseUrl, T json, string requestUrl = "")
        {
            string[] odataNames = { "OData-MaxVersion", "OData-Version" };
            string[] odataValues = { "4.0", "4.0" };
            return await GetJsonBearerStringCustomHeadersAsync(token, baseUrl, odataNames, odataValues, json, requestUrl);

            /*using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = SetAuthorizationBearerHeader(token);
            client.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
            client.DefaultRequestHeaders.Add("OData-Version", "4.0");
            client.BaseAddress = new Uri(baseUrl);
            var type = json.GetType();
            return (TValue)await client.GetFromJsonAsync(baseUrl + requestUrl, type);*/ //"api/data/v9.2/systemusers", type);
        }
        #endregion

        #region POST Handling
        public static async Task<string> PostJsonBearerCustomHeadersAsync(string token, string baseUrl, string requestUrl, string json, string[] headernames, string[] headervalues)
        {
            HttpContent httpContent = ConvertJsonStringToHttpContent(json);
            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = SetAuthorizationBearerHeader(token);
            for (int i = 0; i < headernames.Length; i++)
            {
                client.DefaultRequestHeaders.Add(headernames[i], headervalues[i]);
            }
            client.BaseAddress = new Uri(baseUrl);

            var response = await client.PostAsync(baseUrl + requestUrl, httpContent);
            return await response.Content.ReadAsStringAsync();
        }
        public static async Task<string> PostJsonBearerCustomHeadersAsync(string token, string baseUrl, string requestUrl, string json, List<string> headernames, List<string> headervalues)
        {
            HttpContent httpContent = ConvertJsonStringToHttpContent(json);
            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = SetAuthorizationBearerHeader(token);
            for (int i = 0; i < headernames.Count; i++)
            {
                client.DefaultRequestHeaders.Add(headernames[i], headervalues[i]);
            }
            client.BaseAddress = new Uri(baseUrl);

            var response = await client.PostAsync(baseUrl + requestUrl, httpContent);
            return await response.Content.ReadAsStringAsync();
        }
        public static async Task<string> PostJsonBearerCustomHeadersAsync(string token, string baseUrl, string requestUrl, string json, Dictionary<string, string> headers)
        {
            HttpContent httpContent = ConvertJsonStringToHttpContent(json);
            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = SetAuthorizationBearerHeader(token);
            foreach(var header in headers)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
            client.BaseAddress = new Uri(baseUrl);

            var response = await client.PostAsync(baseUrl + requestUrl, httpContent);
            return await response.Content.ReadAsStringAsync();
        }
        public static async Task<string> PostJsonStringBearerWithODataAsync(string token, string baseUrl, string requestUrl, string json)
        {
            HttpContent httpContent = ConvertJsonStringToHttpContent(json);
            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = SetAuthorizationBearerHeader(token);
            client.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
            client.DefaultRequestHeaders.Add("OData-Version", "4.0");
            client.BaseAddress = new Uri(baseUrl);

            var response = await client.PostAsync(baseUrl + requestUrl, httpContent);
            return await response.Content.ReadAsStringAsync();
            /*HttpContent httpContent = ConvertJsonStringToHttpContent(json);
            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = SetAuthorizationBearerHeader(token);
            client.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
            client.DefaultRequestHeaders.Add("OData-Version", "4.0");
            client.BaseAddress = new Uri(baseUrl);

            var response = await client.PostAsync(baseUrl + requestUrl, httpContent);
            return await response.Content.ReadAsStringAsync();*/
        }
        public static async Task<string> PostJsonStringBearerAsync(string token, string baseUrl, string requestUrl, string json)
        {
            HttpContent httpContent = ConvertJsonStringToHttpContent(json);
            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = SetAuthorizationBearerHeader(token);
            client.DefaultRequestHeaders.Accept.Add(OnlyAllowJSON());
            client.BaseAddress = new Uri(baseUrl);

            var response = await client.PostAsync(baseUrl + requestUrl, httpContent);
            return await response.Content.ReadAsStringAsync();
        }
        public static async Task<string> PostJsonBearerAsync<TValue>(string token, string baseUrl, string requestUrl, TValue json)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = SetAuthorizationBearerHeader(token);
            client.BaseAddress = new Uri(baseUrl);

            var response = await client.PostAsJsonAsync(baseUrl + requestUrl, json);
            return await response.Content.ReadAsStringAsync();
        }
        public static async Task<string> PostJsonObjectAsync<TValue>(TValue json, string url)
        {
            using HttpClient client = new();
            var response = await client.PostAsJsonAsync(url, json);
            return await response.Content.ReadAsStringAsync();
        }
        public static async Task<string> PostJsonOpenAIBearerWithOrgAsync<TValue>(string OrgId, string token, string baseUrl, string requestUrl, TValue json)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = SetAuthorizationBearerHeader(token);
            client.DefaultRequestHeaders.Add("OpenAI-Organization", OrgId);
            client.BaseAddress = new Uri(baseUrl);

            var response = await client.PostAsJsonAsync(baseUrl + requestUrl, json);
            return await response.Content.ReadAsStringAsync();
        }
        public static async Task<string> PostJsonOpenAIBearerWithOrgStringAsync(string OrgId, string token, string baseUrl, string requestUrl, string json)
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
