using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

//Highly subject to change
//This is the main global functions and variables widely used across all apps.
namespace AASPGlobalLibrary
{
    public class Globals
    {
        //Gets Vault URL based on Vaultname
        public static string VaultBase(string? vaultname) { return "https://" + vaultname + ".vault.azure.net/"; }
        //Microsoft Graph URL that can be configured based on version, defaults to 1.0 for now
        public static string GraphBase(string version = "1.0") { return "https://graph.microsoft.com/" + version + "/"; }
        //Dynamics 365 global distrubtion site to gather initial information about environments
        public const string Dynamics365Distro = "https://globaldisco.crm.dynamics.com/api/discovery/v1.0/Instances";
        //Sign in users of a specific organization only. The <tenant> in the URL is the tenant ID of the Azure Active Directory (Azure AD) tenant (a GUID), or its tenant domain.
        public static string TenantLoginAuth(string TenantId)
        {
            return "https://login.microsoftonline.com/" + TenantId;
        }
        //Sign in users with work and school accounts or personal Microsoft accounts.
        public const string CommonLoginAuth = "https://login.microsoftonline.com/common/";
        //Sign in users with work and school accounts.
        public const string OrgLoginAuth = "https://login.microsoftonline.com/organizations/";
        //Sign in users with personal Microsoft accounts (MSA) only.
        public const string ConsumersLoginAuth = "https://login.microsoftonline.com/consumers/";
        //Native OAuth2 used for .net desktop app specific processing
        public const string NativeLoginAuth = "https://login.microsoftonline.com/common/oauth2/nativeclient";
        //Interactive way to auth via localhost redirect
        public static (string, string) LocalHostLoginAuth(string port = "")
        {
            if (port != "")
                return ("http://localhost:" + port, "https://localhost:" + port);
            else
                return ("http://localhost", "https://localhost");
        }
        //You don't need to add a redirect URI if you're building a Xamarin Android and iOS application
        //that doesn't support the broker redirect URI.
        //It's automatically set to msal{ClientId}://auth for Xamarin Android and iOS.
        public static string MSALMobileLoginAuth(string ClientId) { return "msal(" + ClientId + ")://auth"; }
        //WAM specific broker based, token cache will be required
        public static string WebBrokerMSAppxWebLoginAuth(string AppId) { return "ms-appx-web://Microsoft.AAD.BrokerPlugin/" + AppId; }

        #region MISC
        //creates a stream out of a string
        public static Stream GenerateStreamFromString(string s)
        {
            using var stream = new MemoryStream();
            using var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        //fixes problems with opening links
        public static void OpenLink(string url)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                ProcessStartInfo processInfo = new()
                {
                    FileName = url,
                    UseShellExecute = true
                };
                Process.Start(processInfo);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
        }

        //checks if a string only has numbers
        public static bool IsNumbersOnly(string str)
        {
            Match match = Regex.Match(str, @"\d+");
            return match.Success;
        }
        #endregion

        //might not need to have as global...
        /*public static DateTime ConvertShortDateAndShortTimeStringToDateTime(string shortdateANDshorttime)
        {
            _ = DateTime.TryParse(shortdateANDshorttime, out DateTime dt);
            return dt;
        }*/

        //used to increase max int size to the 100th power * 2, mainly for cosmos DB ID's
        //also converts to a light encryption
        #region Big Counter Controller
        public static string ConvertToBase64UriSafeString(byte[] bytesToProcess)
        {
            string base64String = Convert.ToBase64String(bytesToProcess);

            // Base 64 Encoding with URL and Filename Safe Alphabet  https://datatracker.ietf.org/doc/html/rfc4648#section-5
            // https://docs.microsoft.com/en-us/azure/cosmos-db/concepts-limits#per-item-limits, due to base64 conversion and encryption
            // the permissible size of the property will further reduce.
            return new StringBuilder(base64String, base64String.Length).Replace("/", "_").Replace("+", "-").ToString();
        }
        public static byte[] ConvertFromBase64UriSafeString(string? uriSafeBase64String)
        {
            StringBuilder fromUriSafeBase64String = new StringBuilder(uriSafeBase64String, uriSafeBase64String.Length).Replace("_", "/").Replace("-", "+");
            return Convert.FromBase64String(fromUriSafeBase64String.ToString());
        }

        //temporary until created and handled properly, requires 64-bit:
        //BigInteger smsCounter = BigInteger.Pow(long.MaxValue, 100);

        //32-bit compatible and the same result
        static readonly BigInteger CounterMax = BigInteger.Pow(int.MaxValue, 100 * 2);
        public static string? IncreaseBigInt(int amount, string? base64lightencryptedstring)
        {
            byte[] bytes = ConvertFromBase64UriSafeString(base64lightencryptedstring);
            BigInteger big = new(bytes);
            big += amount;
            if (big < CounterMax)
                return ConvertToBase64UriSafeString(big.ToByteArray());
            else
                return null;
        }
        #endregion

        //removes a lot of complex issues with JSON
        #region JSON Handling
        public static async Task<T> LoadJSON<T>(string path)
        {
            return JsonSerializer.Deserialize<T>(await LoadJSONAsBytes(path));
        }
        public static async Task<byte[]> LoadJSONAsBytes(string path)
        {
            try { return await File.ReadAllBytesAsync(path); }
            catch { return await OpenJSONFileAsync(); }
        }
        public static byte[]? OpenJSONFile()
        {
            OpenFileDialog openFileDialog = new()
            {
                InitialDirectory = Application.StartupPath,
                DefaultExt = "json",
                Filter = "json files (*.json)|*.json|All files (*.*)|*.*",
                Multiselect = false,
            };
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                return File.ReadAllBytes(openFileDialog.FileName);
            }
            else return null;
        }
        public static async Task<byte[]?> OpenJSONFileAsync()
        {
            OpenFileDialog openFileDialog = new()
            {
                InitialDirectory = Application.StartupPath,
                DefaultExt = "json",
                Filter = "json files (*.json)|*.json|All files (*.*)|*.*",
                Multiselect = false,
            };
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                return await File.ReadAllBytesAsync(openFileDialog.FileName);
            }
            else return null;
        }

        public class JSONRestErrorHandler
        {
            public Error? error { get; set; }

            public class Error
            {
                public string? code { get; set; }
                public string? message { get; set; }
                public Innererror? innerError { get; set; }
            }

            public class Innererror
            {
                public DateTime date { get; set; }
                public string? requestid { get; set; }
                public string? clientrequestid { get; set; }
            }

        }

        public static dynamic DynamicJsonDeserializer(string jsonstring)
        {
            JsonSerializerOptions options = new();
            options.Converters.Add(new DynamicJsonConverter());
            return JsonSerializer.Deserialize<dynamic>(jsonstring, options);
        }
        public static T DynamicBytesJsonDeserializer<T>(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            JsonSerializerOptions options = new();
            options.Converters.Add(new DynamicJsonConverter());
            return JsonSerializer.Deserialize<T>(bytes, options);
        }
        public static async Task<T> DynamicBytesJsonDeserializer<T>()
        {
            JsonSerializerOptions options = new();
            options.Converters.Add(new DynamicJsonConverter());
            return JsonSerializer.Deserialize<T>(await OpenJSONFileAsync(), options);
        }
        public static async Task<T> BytesJsonDeserializer<T>()
        {
            return JsonSerializer.Deserialize<T>(await OpenJSONFileAsync());
        }

        public static string FindDynamicDataverseValue(string jsonstring, string key, int i)
        {
            dynamic incomingjson = DynamicJsonDeserializer(jsonstring);

            foreach (KeyValuePair<string, object> o in incomingjson.value[i])
            {
                if (o.Key == key)
                {
                    return o.Value.ToString();
                }
            }
            return "";
        }
        //faster because dynamic should be defined in the app first.
        public static string FindDynamicDataverseValue(dynamic incomingjson, string key, int i)
        {
            foreach (KeyValuePair<string, object> o in incomingjson.value[i])
            {
                if (o.Key == key)
                {
                    return o.Value.ToString();
                }
            }
            return "";
        }
        public static string FindDynamicDataverseSpecificAccountValue(dynamic incomingjson, string key)
        {
            foreach (KeyValuePair<string, object> o in incomingjson)
            {
                if (o.Key == key)
                {
                    return o.Value.ToString();
                }
            }
            return "";
        }
        #endregion
    }
}