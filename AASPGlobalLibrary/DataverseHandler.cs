using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;

/*
this will be important when constraining lengths to DB's for perfect locked sizes if the need arises.
These notes can be ignored if non-devs are looking at this:
The maximum length for an AAD username (without domain) is 64 characters.
The maximum length for an AAD custom domain is 48 characters.
For a non-custom (*.onmicrosoft.com) domain, the string length limit is 27 characters.
As ".onmicrosoft.com" is 16 characters, this adds up to a 43-character limit in total, slightly less than the custom domain limit.
So overall, a username in the UPN format (username@domain) has a total string length limit of 113 characters

These figures can be found on the official Microsoft documentation here:
https://learn.microsoft.com/en-us/azure/active-directory/authentication/concept-sspr-policy#userprincipalname-policies-that-apply-to-all-user-accounts
*/

//The current way dataverse is being handled.
//Highly subject to change due to uniqueness with API and handling information.
//Make sure to call Init first to select the json file for metadata standard DB info
//SetBaseURL & Init should be called when you use SelectDynamicsEnvironment.cs (required)
//Can be initialized with or without async
namespace AASPGlobalLibrary
{
#pragma warning disable CS8604
    //the connection string redirect URL cannot have web enabled in the Azure API or it will fail.
    //create the DB and then update the redirect URL for the app to work properly.
    //doing it this way will also not require a system user, just need a standard user with high enough credentials.
    public class DataverseHandler
    {
        internal JSONInternalDbInfo? DbInfo { get; set; }
        //try to avoid needing this
        //public string GetDBPrefix()
        //{
        //return DbInfo.StartingPrefix;
        //}
        string baseUrl = "";
        public void Init(string environment)
        {
            baseUrl = "https://" + environment + ".crm.dynamics.com/";
            DbInfo = JsonSerializer.Deserialize<JSONInternalDbInfo>(Globals.OpenJSONFile());
        }
        public async Task InitAsync(string environment)
        {
            baseUrl = "https://" + environment + ".crm.dynamics.com/";
            DbInfo = JsonSerializer.Deserialize<JSONInternalDbInfo>(await Globals.OpenJSONFileAsync());
        }
        public async Task InitAsync(string environment, string path)
        {
            baseUrl = "https://" + environment + ".crm.dynamics.com/";
            DbInfo = JsonSerializer.Deserialize<JSONInternalDbInfo>(await File.ReadAllBytesAsync(path));
        }
        public void SetCustomPrefix(string environment, string customPrefix, string api = "api/data/v9.2/")
        {
            baseUrl = "https://" + environment + ".crm.dynamics.com/";
            DbInfo ??= new();
            DbInfo.api = api;
            DbInfo.StartingPrefix = customPrefix;
        }

        #region Get Account Info
        public async Task<string> GetAccountsDBJSON(string smsPhoneNumber, string emailAccountColumnName, string whatsappid, string secretName, string keyvaultname)
        {
            string select = "?$select=" + DbInfo.StartingPrefix + smsPhoneNumber + "," + DbInfo.StartingPrefix + emailAccountColumnName + "," + DbInfo.StartingPrefix + whatsappid;
            string query = DbInfo.StartingPrefix + await VaultHandler.GetSecretInteractive(keyvaultname, secretName) + select;
            var token = await TokenHandler.GetDynamicsImpersonationToken(baseUrl);
            var results = await HttpClientHandler.GetJsonStringOdataAsync(token, baseUrl, DbInfo.api + query);

            return results;
        }
        public async Task<string> GetAccountDBJSON(string smsPhoneNumber, string emailAccountColumnName, string whatsappid, string secretName, string keyvaultname, string Email)
        {
            string select = "?$select=" + DbInfo.StartingPrefix + smsPhoneNumber + "," + DbInfo.StartingPrefix + emailAccountColumnName + "," + DbInfo.StartingPrefix + whatsappid;
            string filter = "&$filter=" + DbInfo.StartingPrefix + emailAccountColumnName + "%20eq%20%27" + Email + "%27";
            string query = DbInfo.StartingPrefix + await VaultHandler.GetSecretInteractive(keyvaultname, secretName) + select + filter;
            var token = await TokenHandler.GetDynamicsImpersonationToken(baseUrl);
            var results = await HttpClientHandler.GetJsonStringOdataAsync(token, baseUrl, DbInfo.api + query);

            return results;
        }
        #endregion

        #region Create Account
        public async Task CreateAccountDB(string secretId, string smsPhoneNumber, string emailAccountColumnName, string whatsappid, string secretName, string internalkeyvaultname, string assignedto, string phonenumber, string phonenumberid)
        {
            string database = DbInfo.StartingPrefix + (await VaultHandler.GetSecretInteractive(internalkeyvaultname, secretName)).ToLower();
            string filter = "?$filter=" + DbInfo.StartingPrefix + emailAccountColumnName + "%20eq%20%27" + assignedto + "%27";
            var token = await TokenHandler.GetDynamicsImpersonationToken(baseUrl);
            string jsonstring = await HttpClientHandler.GetJsonStringOdataAsync(token, baseUrl + DbInfo.api + database + filter);
            dynamic getjsonid = Globals.DynamicJsonDeserializer(jsonstring);
            try
            {
                //dynamic json does not detect value correctly even though value equals [] which should translate to count 0.
                //due to this, the try catch fixes the issue.
                if (getjsonid.value.Count == 0) { }
                else Console.Write(Environment.NewLine + "Account name already exists, stopping to prevent duplicate.");
            }
            catch
            {
                var accountsdb = (await VaultHandler.GetSecretInteractive(internalkeyvaultname, secretName)).ToLower();
                ServiceClient service = await CreateStandardAuthServiceClient(secretId, internalkeyvaultname, assignedto);
                var entity = new Entity(string.Concat(DbInfo.StartingPrefix, accountsdb.AsSpan(0, accountsdb.Length - 2)));
                //var entity = new Entity(string.Concat(DbInfo.StartingPrefix, accountsdb));
                entity[DbInfo.StartingPrefix + emailAccountColumnName] = assignedto;
                entity[DbInfo.StartingPrefix + smsPhoneNumber] = phonenumber;
                entity[DbInfo.StartingPrefix + whatsappid] = phonenumberid;
                _ = await service.CreateAsync(entity);
            }
        }
        public async Task CreateAccountDBSecret(string secretId, string secretSecret, string smsPhoneNumber, string emailAccountColumnName, string whatsappid, string secretName, string internalkeyvaultname, string environment, string assignedto, string phonenumber, string phonenumberid)
        {
            string database = DbInfo.StartingPrefix + (await VaultHandler.GetSecretInteractive(internalkeyvaultname, secretName)).ToLower();
            string filter = "?$filter=" + DbInfo.StartingPrefix + emailAccountColumnName + "%20eq%20%27" + assignedto + "%27";
            var token = await TokenHandler.GetDynamicsImpersonationToken(baseUrl);
            string jsonstring = await HttpClientHandler.GetJsonStringOdataAsync(token, baseUrl + DbInfo.api + database + filter);
            dynamic getjsonid = Globals.DynamicJsonDeserializer(jsonstring);
            try
            {
                //dynamic json does not detect value correctly even though value equals [] which should translate to count 0.
                //due to this, the try catch fixes the issue.
                if (getjsonid.value.Count == 0) { }
                else Console.Write(Environment.NewLine + "Account name already exists, stopping to prevent duplicate.");
            }
            catch
            {
                var accountsdb = (await VaultHandler.GetSecretInteractive(internalkeyvaultname, secretName)).ToLower();
                ServiceClient service = await CreateSecretAuthServiceClient(secretId, secretSecret, internalkeyvaultname, environment);
                var entity = new Entity(string.Concat(DbInfo.StartingPrefix, accountsdb.AsSpan(0, accountsdb.Length - 2)));
                //var entity = new Entity(string.Concat(DbInfo.StartingPrefix, accountsdb));
                entity[DbInfo.StartingPrefix + emailAccountColumnName] = assignedto;
                entity[DbInfo.StartingPrefix + smsPhoneNumber] = phonenumber;
                entity[DbInfo.StartingPrefix + whatsappid] = phonenumberid;
                _ = await service.CreateAsync(entity);
            }
        }
        #endregion

        #region Update Users Account
        public async Task<string> PatchAccountDB(string accountDBIDColumnName, string whatsappid, string smsPhoneNumber, string emailAccountColumnName, string secretName, string keyvaultname, string email, dynamic json, string[] crosscompare)
        {
            string database = DbInfo.StartingPrefix + (await VaultHandler.GetSecretInteractive(keyvaultname, secretName)).ToLower();
            //string select = string.Concat("?$select=", database.AsSpan(0, database.Length - 2), "id");
            //string filter = "&$filter=" + DbInfo.StartingPrefix + emailAccountColumnName + "%20eq%20%27" + email + "%27";
            string filter = "?$filter=" + DbInfo.StartingPrefix + emailAccountColumnName + "%20eq%20%27" + email + "%27";
            var token = await TokenHandler.GetDynamicsImpersonationToken(baseUrl);
            //string jsonstring = await HttpClientHandler.GetJsonStringOdataAsync(token, baseUrl + DbInfo.api + database + select + filter);
            string jsonstring = await HttpClientHandler.GetJsonStringOdataAsync(token, baseUrl + DbInfo.api + database + filter);
            try
            {
                dynamic getjsonid = Globals.DynamicJsonDeserializer(jsonstring);
                if (getjsonid.value.Count > 1)
                {
                    Console.Write(Environment.NewLine + "Duplicates of this account have been found.");
                    string id = "";
                    for (int i = 0; i < getjsonid.value.Count; i++)
                    {
                        if (Globals.FindDynamicDataverseValue(getjsonid, DbInfo.StartingPrefix + whatsappid, i) == crosscompare[0]
                            && Globals.FindDynamicDataverseValue(getjsonid, DbInfo.StartingPrefix + smsPhoneNumber, i) == crosscompare[1])
                        {
                            id = Globals.FindDynamicDataverseValue(getjsonid, DbInfo.StartingPrefix + accountDBIDColumnName, i);
                            break;
                        }
                    }
                    if (id != "")
                    {
                        string specificaccount = database + "(" + id + ")";
                        return await HttpClientHandler.PatchJsonStringOdataAsync(token, baseUrl + DbInfo.api + specificaccount, JsonSerializer.Serialize(json));
                    }
                    else
                    {
                        string error = "";
                        for (int i = 0; i < getjsonid.value.Count; i++)
                        {
                            error += Globals.FindDynamicDataverseValue(getjsonid, DbInfo.StartingPrefix + whatsappid, i) + " : " + crosscompare[0] + Environment.NewLine;
                            error += Globals.FindDynamicDataverseValue(getjsonid, DbInfo.StartingPrefix + smsPhoneNumber, i) + " : " + crosscompare[1] + Environment.NewLine;
                        }
                        return error;
                    }
                }
                else
                {
                    var id = Globals.FindDynamicDataverseValue(getjsonid, DbInfo.StartingPrefix + accountDBIDColumnName, 0);
                    //Console.Write(id);
                    string specificaccount = database + "(" + id + ")";
                    return await HttpClientHandler.PatchJsonStringOdataAsync(token, baseUrl + DbInfo.api + specificaccount, JsonSerializer.Serialize(json));
                }
            }
            catch
            {
                return jsonstring;
            }
        }
        public async Task<List<string>> PatchSMSDB(NativeWindow nativeWindow, string secretName, string keyvaultname, string email, string fromColumnName, string toColumnName, string emailNonAccountColumnName, dynamic json)
        {
            string currentSMSDatabase = DbInfo.StartingPrefix + (await VaultHandler.GetSecretInteractive(keyvaultname, secretName)).ToLower();
            string select = string.Concat("?$select=", currentSMSDatabase.AsSpan(0, currentSMSDatabase.Length - 2), "id");
            string filter = "&$filter=" + DbInfo.StartingPrefix + emailNonAccountColumnName + "%20eq%20%27" + email + "%27";
            var token = await TokenHandler.GetDynamicsImpersonationToken(baseUrl);
            string jsonstring = await HttpClientHandler.GetJsonStringOdataAsync(token, baseUrl + DbInfo.api + currentSMSDatabase + select + filter);
            dynamic? currentSMSJson = Globals.DynamicJsonDeserializer(jsonstring);
            DynamicProgressBar dpb = new();
            try
            {
                dpb.SetMax(currentSMSJson.value.Count);
                //return currentSMSJson.value.Count;
            }
            catch
            {
                dpb.SetMax(0);
            }//return 0; }
            dpb.UpdateProgress(0, "SMS DB: ");
            dpb.Show(nativeWindow);

            var temp = new List<string>();
            string valuetoget = string.Concat(currentSMSDatabase.AsSpan(0, currentSMSDatabase.Length - 2), "id");
            //jsonstring = jsonstring.Replace(valuetoget, "lkhfasdhoiuyqwebrfuifytoid");
            try
            {
                for (int i = 0; i < currentSMSJson.value.Count; i++)
                {
                    dpb.UpdateProgress(i, "SMS DB: ");
                    //keeps changing json if assigned directly?
                    //interal bug exists by doing tempp = json.
                    //each variable must be assigned one by one with a completely new object
                    Dictionary<string, object> tempp = new()
                    {
                        { DbInfo.StartingPrefix + emailNonAccountColumnName, Globals.FindDynamicDataverseSpecificAccountValue(json, DbInfo.StartingPrefix + emailNonAccountColumnName) },
                        { DbInfo.StartingPrefix + toColumnName, Globals.FindDynamicDataverseSpecificAccountValue(json, DbInfo.StartingPrefix + "Number") },
                        { DbInfo.StartingPrefix + fromColumnName, Globals.FindDynamicDataverseSpecificAccountValue(json, DbInfo.StartingPrefix + "Number") }
                    };
                    var id = Globals.FindDynamicDataverseValue(currentSMSJson, valuetoget, i); // dynamicjson.value[i].lkhfasdhoiuyqwebrfuifytoid;
                    string specificaccount = currentSMSDatabase + "(" + id + ")";
                    string response = await HttpClientHandler.GetJsonStringOdataAsync(token, baseUrl + DbInfo.api + specificaccount);
                    //dynamic dynamicjson2 = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(response);
                    dynamic dynamicjson2 = Globals.DynamicJsonDeserializer(response);
                    string to = Globals.FindDynamicDataverseSpecificAccountValue(dynamicjson2, DbInfo.StartingPrefix + toColumnName);
                    if (to.Contains('+'))
                        tempp[DbInfo.StartingPrefix + toColumnName] = to;
                    string from = Globals.FindDynamicDataverseSpecificAccountValue(dynamicjson2, DbInfo.StartingPrefix + fromColumnName);
                    if (from.Contains('+'))
                        tempp[DbInfo.StartingPrefix + fromColumnName] = from;
                    temp.Add(await HttpClientHandler.PatchJsonStringOdataAsync(token, baseUrl + DbInfo.api + specificaccount, JsonSerializer.Serialize(tempp)));
                }
                dpb.Close();
                return temp;
            }
            catch (Exception ex)
            {
                dpb.Close();
                temp.Add(ex.Message);
                return temp;
            }
        }
        public async Task<List<string>> PatchWhatsAppDB(NativeWindow nativeWindow, string fromColumnName, string toColumnName, string emailNonAccountColumnName, string secretName, string keyvaultname, string email, dynamic json)
        {
            string database = DbInfo.StartingPrefix + (await VaultHandler.GetSecretInteractive(keyvaultname, secretName)).ToLower();
            string select = string.Concat("?$select=", database.AsSpan(0, database.Length - 2), "id");
            string filter = "&$filter=" + DbInfo.StartingPrefix + emailNonAccountColumnName + "%20eq%20%27" + email + "%27";
            var token = await TokenHandler.GetDynamicsImpersonationToken(baseUrl);
            string jsonstring = await HttpClientHandler.GetJsonStringOdataAsync(token, baseUrl + DbInfo.api + database + select + filter);
            dynamic dynamicjson = Globals.DynamicJsonDeserializer(jsonstring);
            DynamicProgressBar dpb = new();
            try
            {
                dpb.SetMax(dynamicjson.value.Count);
                //return currentSMSJson.value.Count;
            }
            catch
            {
                dpb.SetMax(0);
            }//return 0; }
            dpb.UpdateProgress(0, "WhatsApp DB: ");
            dpb.Show(nativeWindow);

            var temp = new List<string>();
            //jsonstring = jsonstring.Replace(valuetoget, "lkhfasdhoiuyqwebrfuifytoid");
            try
            {
                //dynamic dynamicjson = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(jsonstring);
                for (int i = 0; i < dynamicjson.value.Count; i++)
                {
                    dpb.UpdateProgress(i, "WhatsApp DB: ");
                    //keeps changing json if assigned directly?
                    //interal bug exists by doing tempp = json.
                    //each variable must be assigned one by one with a completely new object
                    Dictionary<string, object> tempp = new()
                    {
                        { DbInfo.StartingPrefix + emailNonAccountColumnName, Globals.FindDynamicDataverseSpecificAccountValue(json, DbInfo.StartingPrefix + emailNonAccountColumnName) },
                        { DbInfo.StartingPrefix + toColumnName, Globals.FindDynamicDataverseSpecificAccountValue(json, DbInfo.StartingPrefix + "Number") },
                        { DbInfo.StartingPrefix + fromColumnName, Globals.FindDynamicDataverseSpecificAccountValue(json, DbInfo.StartingPrefix + "Number") }
                    };

                    string valuetoget = string.Concat(database.AsSpan(0, database.Length - 2), "id");
                    var id = Globals.FindDynamicDataverseValue(dynamicjson, valuetoget, i); //dynamicjson.value[i].lkhfasdhoiuyqwebrfuifytoid;
                    string specificaccount = database + "(" + id + ")";
                    string response = await HttpClientHandler.GetJsonStringOdataAsync(token, baseUrl + DbInfo.api + specificaccount);
                    //dynamic dynamicjson2 = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(response);
                    dynamic dynamicjson2 = Globals.DynamicJsonDeserializer(response);
                    string to = Globals.FindDynamicDataverseSpecificAccountValue(dynamicjson2, DbInfo.StartingPrefix + toColumnName);
                    if (to.Contains('+'))
                        tempp[DbInfo.StartingPrefix + toColumnName] = to;
                    string from = Globals.FindDynamicDataverseSpecificAccountValue(dynamicjson2, DbInfo.StartingPrefix + fromColumnName);
                    if (from.Contains('+'))
                        tempp[DbInfo.StartingPrefix + fromColumnName] = from;
                    temp.Add(await HttpClientHandler.PatchJsonStringOdataAsync(token, baseUrl + DbInfo.api + specificaccount, JsonSerializer.Serialize(tempp)));
                }
                dpb.Close();
                return temp;
            }
            catch (Exception ex)
            {
                dpb.Close();
                temp.Add(ex.Message);
                return temp;
            }
        }
        #endregion

        #region Delete Account
        public async Task<string> DeleteAccountDB(string accountDBIDColumnName, string whatsappid, string smsPhoneNumber, string emailAccountColumnName, string secretName, string keyvaultname, string email, string[] crosscompare)
        {
            string database = DbInfo.StartingPrefix + (await VaultHandler.GetSecretInteractive(keyvaultname, secretName)).ToLower();
            //string select = "?$select=" + DbInfo.StartingPrefix + phoneNumberIDColumnName;
            //string filter = "&$filter=" + DbInfo.StartingPrefix + emailAccountColumnName + "%20eq%20%27" + email + "%27";
            string filter = "?$filter=" + DbInfo.StartingPrefix + emailAccountColumnName + "%20eq%20%27" + email + "%27";
            var token = await TokenHandler.GetDynamicsImpersonationToken(baseUrl);
            //Console.Write(baseUrl + api + database + select + filter);
            //string jsonstring = await HttpClientHandler.GetJsonStringOdataAsync(token, baseUrl + DbInfo.api + database + select + filter);
            string jsonstring = await HttpClientHandler.GetJsonStringOdataAsync(token, baseUrl + DbInfo.api + database + filter);

            try
            {
                dynamic getjsonid = Globals.DynamicJsonDeserializer(jsonstring);
                if (getjsonid.value.Count > 1)
                {
                    Console.Write(Environment.NewLine + "Duplicates of this account have been found.");
                    string id = "";
                    for (int i = 0; i < getjsonid.value.Count; i++)
                    {
                        if (Globals.FindDynamicDataverseValue(getjsonid, DbInfo.StartingPrefix + whatsappid, i) == crosscompare[0]
                            && Globals.FindDynamicDataverseValue(getjsonid, DbInfo.StartingPrefix + smsPhoneNumber, i) == crosscompare[1])
                        {
                            id = Globals.FindDynamicDataverseValue(getjsonid, DbInfo.StartingPrefix + accountDBIDColumnName, i);
                            break;
                        }
                    }
                    if (id != "")
                    {
                        string specificaccount = database + "(" + id + ")";
                        return await HttpClientHandler.DeleteOdataAsync(token, baseUrl + DbInfo.api + specificaccount);
                    }
                    else
                    {
                        string error = "";
                        for (int i = 0; i < getjsonid.value.Count; i++)
                        {
                            error += Globals.FindDynamicDataverseValue(getjsonid, DbInfo.StartingPrefix + whatsappid, i) + " : " + crosscompare[0] + Environment.NewLine;
                            error += Globals.FindDynamicDataverseValue(getjsonid, DbInfo.StartingPrefix + smsPhoneNumber, i) + " : " + crosscompare[1] + Environment.NewLine;
                        }
                        return error;
                    }
                }
                else
                {
                    var id = Globals.FindDynamicDataverseValue(getjsonid, DbInfo.StartingPrefix + accountDBIDColumnName, 0);

                    string specificaccount = database + "(" + id + ")";
                    return await HttpClientHandler.DeleteOdataAsync(token, baseUrl + DbInfo.api + specificaccount);
                }
            }
            catch
            {
                return jsonstring;
            }
        }
        #endregion

        #region Handles System Account Creation
        public class JSONBusinessUnits
            {
                public string? odatacontext { get; set; }
                public Value[]? value { get; set; }

                public class Value
                {
                    public string? odataetag { get; set; }
                    public string? name { get; set; }
                    public string? businessunitid { get; set; }
                }

            }

        async Task<string> GetBusinessID(string baseUrl)
        {
            HttpClient client = new()
            {
                // See https://learn.microsoft.com/powerapps/developer/data-platform/webapi/compose-http-requests-handle-errors#web-api-url-and-versions
                BaseAddress = new Uri(baseUrl + DbInfo.api),
                Timeout = new TimeSpan(0, 2, 0)    // Standard two minute timeout on web service calls.
            };

            // Default headers for each Web API call.
            // See https://learn.microsoft.com/powerapps/developer/data-platform/webapi/compose-http-requests-handle-errors#http-headers
            HttpRequestHeaders headers = client.DefaultRequestHeaders;
            headers.Authorization = new AuthenticationHeaderValue("Bearer", await TokenHandler.GetDynamicsImpersonationToken(baseUrl));
            headers.Add("OData-MaxVersion", "4.0");
            headers.Add("OData-Version", "4.0");
            headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.GetAsync("businessunits");
            var content = await response.Content.ReadAsStringAsync();
            //Console.Write(content);
            JSONBusinessUnits? businessUnits = Newtonsoft.Json.JsonConvert.DeserializeObject<JSONBusinessUnits>(content);
            string? businessId = "";
            if (businessUnits != null)
                if (businessUnits.value != null)
                {
                    var temp = baseUrl[8..].Split(".")[0];
                    for (int i = 0; i < businessUnits.value.Length; i++)
                    {
                        if (businessUnits.value[i].name == temp)
                        {
                            businessId = businessUnits.value[i].businessunitid;
                            break;
                        }
                    }
                }
            if (businessId != null)
                return businessId;
            else
                return "";
        }

        public async Task<HttpResponseMessage> CreateSystemUser(string appRegistrationClientId, int accessMode = 4)
        {
            string businessID = await GetBusinessID(baseUrl);

            using HttpClient client = new();
            // See https://learn.microsoft.com/powerapps/developer/data-platform/webapi/compose-http-requests-handle-errors#web-api-url-and-versions
            client.BaseAddress = new Uri(baseUrl + DbInfo.api);

            // Default headers for each Web API call.
            // See https://learn.microsoft.com/powerapps/developer/data-platform/webapi/compose-http-requests-handle-errors#http-headers
            HttpRequestHeaders headers = client.DefaultRequestHeaders;
            headers.Authorization = new AuthenticationHeaderValue("Bearer", await TokenHandler.GetDynamicsImpersonationToken(baseUrl));
            headers.Add("OData-MaxVersion", "4.0");
            headers.Add("OData-Version", "4.0");
            headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            // Invoke the Web API 'WhoAmI' unbound function.
            // See https://learn.microsoft.com/powerapps/developer/data-platform/webapi/compose-http-requests-handle-errors
            // See https://learn.microsoft.com/powerapps/developer/data-platform/webapi/use-web-api-functions#unbound-functions
            //HttpResponseMessage response = await client.GetAsync(filter);
            Newtonsoft.Json.Linq.JObject newAppUser = new()
                { 

                /* Access Modes
                0	Read-Write
                1	Administrative
                2	Read
                3	Support User
                4	Non-interactive
                5	Delegated Admin */
                    "accessmode", accessMode,
                    "businessunitid@odata.bind", "/businessunits(" + businessID + ")",
                    "applicationid", appRegistrationClientId,
                    "defaultodbfoldername", "Dynamics365"
                };
            //Console.Write(newAppUser.ToString());
            var response = await client.PostAsJsonAsync(baseUrl + DbInfo.api + "systemusers", newAppUser.ToString());

            return response;
        }

        public async Task<string> CreateSystemAccount(VerifyAppId apiWindow, string appClientId, string dynamicsOrgId)
        {
            string possiblenewappid = appClientId;
        tryagain:
            {
                try
                {
                    await Task.Delay(10000);

                    var response = await CreateSystemUser(appClientId);

                    if (!response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        if (content.Contains("0x8004f510"))
                        {
                            Console.Write(Environment.NewLine + "Cannot find appID " + appClientId);
                            apiWindow.textBox1.Text = appClientId;
                            _ = apiWindow.ShowDialog();
                            possiblenewappid = apiWindow.GetTextInfo();
                        }
                        else if (content.Contains("A record with matching key values already exists."))
                        {
                            //silent skip because this will be called after trying again most likely.
                            //Console.Write(Environment.NewLine + "Skipping: System account already exists for " + appClientId);
                            //finished = true;
                        }
                        else
                            Console.Write(Environment.NewLine + content);
                    }
                    else
                    {
                        Console.Write(Environment.NewLine + "Dataverse System Account Created");
                        await PostSystemAdminPermissionsToSystemUser(await TokenHandler.GetDynamicsImpersonationToken(baseUrl), appClientId, dynamicsOrgId);
                    }
                }
                catch (Exception e) { Console.Write(Environment.NewLine + "Failed auto dataverse deployment: " + e.Message); }
            }

            try
            {
                await Task.Delay(10000);

                var response = await CreateSystemUser(appClientId);

                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (content.Contains("0x8004f510"))
                        goto tryagain;
                    else if (content.Contains("A record with matching key values already exists."))
                    {
                        //silent skip because this will be called after trying again most likely.
                        //Console.Write(Environment.NewLine + "Skipping: System account already exists for " + appClientId);
                        //finished = true;
                    }
                    else
                        Console.Write(Environment.NewLine + content);
                }
                else
                {
                    Console.Write(Environment.NewLine + "Dataverse System Account Created");
                    await PostSystemAdminPermissionsToSystemUser(await TokenHandler.GetDynamicsImpersonationToken(baseUrl), appClientId, dynamicsOrgId);
                }
            }
            catch (Exception e) { Console.Write(Environment.NewLine + "Failed auto dataverse deployment: " + e.Message); }
            return possiblenewappid;
        }
        async Task PostSystemAdminPermissionsToSystemUser(string token, string appId, string selectedOrgId)
        {
            Console.Write(Environment.NewLine + "Assigning System Administrator permission automatically.");
            var getSystemUsers = await HttpClientHandler.GetJsonAsync(token, baseUrl, new JSONGetSystemUsers(), DbInfo.api + "systemusers");
            string systemuserId = "";
            for (int i = 0; i < getSystemUsers.value.Length; i++)
            {
                if (getSystemUsers.value[i].applicationid == appId)
                {
                    systemuserId = getSystemUsers.value[i].systemuserid;
                    break;
                }
            }

            if (systemuserId == "")
            {
                Console.Write(Environment.NewLine + "Unable to find SystemId by AppId: " + appId + Environment.NewLine + "Manual permission is required to continue.");
                System.Diagnostics.Process.Start("explorer", "https://admin.powerplatform.microsoft.com/environments/" + selectedOrgId + "/appusers");
                //return false;
            }
            else
            {
                var systemUserURL = "/api/data/v9.2/systemusers(" + systemuserId + ")";
                string odataid = "\"@odata.id\": \"" + baseUrl + systemUserURL + "\"";
                string json = "{" + odataid + "}";
                json = JsonSerializer.Serialize(json);

                string fieldsecurityprofileid = "";

                //JSONGetSecurityFields jsonSecurityFields = await HttpClientHandler.GetJsonAsync(token, baseUrl, "/api/data/v9.2/fieldsecurityprofiles", new JSONGetSecurityFields());
                var securitystring = await HttpClientHandler.GetJsonStringAsync(token, baseUrl, "/api/data/v9.2/fieldsecurityprofiles");
                Console.Write(securitystring);
                JSONGetSecurityFields jsonSecurityFields = JsonSerializer.Deserialize<JSONGetSecurityFields>(securitystring);
                for (int i = 0; i < jsonSecurityFields.value.Length; i++)
                {
                    if (jsonSecurityFields.value[i].name == "System Administrator")
                    {
                        fieldsecurityprofileid = jsonSecurityFields.value[i].fieldsecurityprofileid;
                        break;
                    }
                }

                string requestUrl = "/api/data/v9.2/fieldsecurityprofiles(" + fieldsecurityprofileid + ")/systemuserprofiles_association/$ref";

                var response = await HttpClientHandler.PostJsonStringBearerWithODataAsync(token, baseUrl, requestUrl, json);
                Console.Write(response);
                Console.Write(Environment.NewLine + "System Administrator permission assigned.");
                //return true;
            }
        }
        #endregion

        #region Create Databases
        static async Task RunMutiplePushes(ServiceClient service, CreateAttributeRequest[] createAttributeRequests)
        {
            for (int i = 0; i < createAttributeRequests.Length; i++)
            {
                try
                {
                    await service.ExecuteAsync(createAttributeRequests[i]);
                    Console.Write(Environment.NewLine + "The " + createAttributeRequests[i].Attribute.LogicalName + " column has been created.");
                }
                catch (Exception e)
                {
                    //if (e.Message.Contains("Error: An attribute with the specified name "))
                        //Console.Write(Environment.NewLine + createAttributeRequests[i].Attribute.SchemaName + " already exists, skipping");
                    //else
                    Console.Write(Environment.NewLine + "Error: " + e.Message);
                }
            }
        }
        public async Task CreateDataverseDatabases(ServiceClient service, string[] databases)
        {
            await CreateEntity(service, databases[0], "Accounts Database", "id", "Not used", 1);
            await CreateAccountsDefaultColumns(service, databases[0]);
            await CreateEntity(service, databases[1], "SMS Database", "id", "Not used", 1);
            await CreateSMSDefaultColumns(service, databases[1]);
            await CreateEntity(service, databases[2], "WhatsApp Database", "id", "Not used", 1);
            await CreateWhatsAppDefaultColumns(service, databases[2]);
            await CreateEntity(service, databases[3], "Phone Numbers Database", "id", "Not used", 1);
            await CreatePhoneNumberDefaultColumns(service, databases[3]);
            await CreateEntity(service, databases[4], "Phone Number Ids Database", "id", "Not used", 1);
            await CreatePhoneNumberIDDefaultColumns(service, databases[4]);

            Console.Write(Environment.NewLine + "Dataverse Deployment Finished");
        }
        CreateAttributeRequest CreateAttributeMetaData(string name, string dbname, int length)
        {
            return new CreateAttributeRequest
            {
                //must be lowercase or errors will occur
                EntityName = DbInfo.StartingPrefix + dbname.ToLower() + "es",
                Attribute = new StringAttributeMetadata
                {
                    LogicalName = name,
                    //must be lowercase or errors will occur
                    SchemaName = DbInfo.StartingPrefix + name.ToLower() + "es",
                    RequiredLevel = new AttributeRequiredLevelManagedProperty(AttributeRequiredLevel.None),
                    MaxLength = length,
                    FormatName = StringFormatName.Text,
                    DisplayName = new Microsoft.Xrm.Sdk.Label(name, 1033)
                }
            };
        }
        async Task CreateWhatsAppDefaultColumns(ServiceClient service, string whatsAppDBName)
        {
            CreateAttributeRequest[] createAttributeRequests = new CreateAttributeRequest[5];

            createAttributeRequests[0] = CreateAttributeMetaData(DbInfo.metadataEmailNonAccount, whatsAppDBName, 36);
            createAttributeRequests[1] = CreateAttributeMetaData(DbInfo.metadataFrom, whatsAppDBName, 20);
            createAttributeRequests[2] = CreateAttributeMetaData(DbInfo.metadataMessage, whatsAppDBName, 4000);
            createAttributeRequests[3] = CreateAttributeMetaData(DbInfo.metadataTo, whatsAppDBName, 20);
            createAttributeRequests[4] = CreateAttributeMetaData(DbInfo.metadataTimestamp, whatsAppDBName, 100);

            await RunMutiplePushes(service, createAttributeRequests);
        }
        async Task CreateSMSDefaultColumns(ServiceClient service, string smsDBName)
        {
            CreateAttributeRequest[] createAttributeRequests = new CreateAttributeRequest[5];

            createAttributeRequests[0] = CreateAttributeMetaData(DbInfo.metadataEmailNonAccount, smsDBName, 36);
            createAttributeRequests[1] = CreateAttributeMetaData(DbInfo.metadataFrom, smsDBName, 20);
            createAttributeRequests[2] = CreateAttributeMetaData(DbInfo.metadataMessage, smsDBName, 4000);
            createAttributeRequests[3] = CreateAttributeMetaData(DbInfo.metadataTo, smsDBName, 20);
            createAttributeRequests[4] = CreateAttributeMetaData(DbInfo.metadataTimestamp, smsDBName, 100);

            await RunMutiplePushes(service, createAttributeRequests);
        }
        async Task CreatePhoneNumberDefaultColumns(ServiceClient service, string phoneDBName)
        {
            CreateAttributeRequest[] createAttributeRequests = new CreateAttributeRequest[3];

            createAttributeRequests[0] = CreateAttributeMetaData(DbInfo.metadataPhoneNumber, phoneDBName, 36);
            createAttributeRequests[1] = CreateAttributeMetaData(DbInfo.metadataPicPath, phoneDBName, 200);
            createAttributeRequests[2] = CreateAttributeMetaData(DbInfo.metadataDisplayName, phoneDBName, 100);

            await RunMutiplePushes(service, createAttributeRequests);
        }
        async Task CreatePhoneNumberIDDefaultColumns(ServiceClient service, string phoneIDDBName)
        {
            CreateAttributeRequest[] createAttributeRequests = new CreateAttributeRequest[3];

            createAttributeRequests[0] = CreateAttributeMetaData(DbInfo.metadataPhoneNumber, phoneIDDBName, 36);
            createAttributeRequests[1] = CreateAttributeMetaData(DbInfo.metadataPicPath, phoneIDDBName, 200);
            createAttributeRequests[2] = CreateAttributeMetaData(DbInfo.metadataDisplayName, phoneIDDBName, 100);

            await RunMutiplePushes(service, createAttributeRequests);
        }
        async Task CreateAccountsDefaultColumns(ServiceClient service, string accountsDBName)
        {
            CreateAttributeRequest[] createAttributeRequests = new CreateAttributeRequest[3];

            createAttributeRequests[0] = CreateAttributeMetaData(DbInfo.metadataPhoneNumber, accountsDBName, 100);
            createAttributeRequests[1] = CreateAttributeMetaData(DbInfo.metadataPhoneNumberID, accountsDBName, 100);
            createAttributeRequests[2] = CreateAttributeMetaData(DbInfo.metadataEmailAccount, accountsDBName, 100);

            await RunMutiplePushes(service, createAttributeRequests);
        }
        async Task CreateEntity(ServiceClient service, string dbName, string dbDescription, string primaryKeyDisplayName, string primaryKeyDescription, int lengthOfPrimaryKey)
        {
            CreateEntityRequest createrequest = new()
            {

                //Define the entity
                Entity = new EntityMetadata
                {
                    SchemaName = DbInfo.StartingPrefix + dbName.ToLower() + "es",
                    DisplayName = new Microsoft.Xrm.Sdk.Label(dbName, 1033),
                    DisplayCollectionName = new Microsoft.Xrm.Sdk.Label(dbName, 1033),
                    Description = new Microsoft.Xrm.Sdk.Label(dbDescription, 1033),
                    OwnershipType = OwnershipTypes.UserOwned,
                    IsActivity = false,

                },

                // Define the primary attribute for the entity
                PrimaryAttribute = new StringAttributeMetadata
                {
                    //must be lowercase or errors will occur
                    SchemaName = DbInfo.StartingPrefix + dbName.ToLower() + "es",
                    //SchemaName = prefix + primaryKeyName + "es",
                    RequiredLevel = new AttributeRequiredLevelManagedProperty(AttributeRequiredLevel.None),
                    MaxLength = lengthOfPrimaryKey,
                    FormatName = StringFormatName.Text,
                    DisplayName = new Microsoft.Xrm.Sdk.Label(primaryKeyDisplayName, 1033),
                    Description = new Microsoft.Xrm.Sdk.Label(primaryKeyDescription, 1033)
                }
            };

            try
            {
                await service.ExecuteAsync(createrequest);
                Console.Write(Environment.NewLine + "The " + dbName + " database has been created.");
            }
            catch (Exception e)
            {
                if (e.Message.Contains("and object type code -1."))
                    Console.Write(Environment.NewLine + dbName + " already exists, skipping");
                else
                    Console.Write(Environment.NewLine + "Error: " + e.Message);
            }
        }
        #endregion

        #region Create Service Clients
        async Task<ServiceClient> CreateStandardAuthServiceClient(string idSecretName, string keyvaultname, string Email)
        {
            var connectionString = @"AuthType='OAuth'; Username='" +
                Email +
                "'; Password='passcode'; Url='" + baseUrl + "'; AppId='" +
                await VaultHandler.GetSecretInteractive(keyvaultname, idSecretName) +
                "'; RedirectUri='http://localhost:65135'; LoginPrompt='Auto'";

            return new ServiceClient(connectionString);
        }
        static async Task<ServiceClient> CreateSecretAuthServiceClient(string idSecretName, string secretSecretName, string keyvaultname, string environment)
        {
            //cannot get this to work currently...
            var connectionString = @"AuthType='ClientSecret'; Url='https://" + environment +
                ".crm.dynamics.com'; ClientId='" + await VaultHandler.GetSecretInteractive(keyvaultname, idSecretName) +
                "'; ClientSecret='" + await VaultHandler.GetSecretInteractive(keyvaultname, secretSecretName) +
                "'; RedirectUri='http://localhost:65135'";
            return new ServiceClient(connectionString);
        }
        #endregion

        #region Binded JSONS
        internal class JSONInternalDbInfo
        {
            public string? StartingPrefix { get; set; }
            public string? api { get; set; }
            public string? metadataFrom { get; set; }
            public string? metadataMessage { get; set; }
            public string? metadataTo { get; set; }
            public string? metadataTimestamp { get; set; }
            public string? metadataPicPath { get; set; }
            public string? metadataEmailNonAccount { get; set; }
            public string? metadataPhoneNumber { get; set; }
            public string? metadataPhoneNumberID { get; set; }
            public string? metadataEmailAccount { get; set; }
            public string? metadataDisplayName { get; set; }
        }
        class JSONGetSystemUsers
        {
            public string? odatacontext { get; set; }
            public Value[]? value { get; set; }

            public class Value
            {
                public string? systemuserid { get; set; }
                public string? applicationid { get; set; }
            }

        }
        class JSONGetSecurityFields
        {
            public string? odatacontext { get; set; }
            public Value[]? value { get; set; }

            public class Value
            {
                public string? odataetag { get; set; }
                public DateTime modifiedon { get; set; }
                public string? solutionid { get; set; }
                public DateTime createdon { get; set; }
                public DateTime overwritetime { get; set; }
                public int versionnumber { get; set; }
                public string? name { get; set; }
                public string? fieldsecurityprofileid { get; set; }
                public int componentstate { get; set; }
                public string? description { get; set; }
                public string? _organizationid_value { get; set; }
                public bool ismanaged { get; set; }
                public string? fieldsecurityprofileidunique { get; set; }
                public string? _createdby_value { get; set; }
                public string? _createdonbehalfby_value { get; set; }
                public string? _modifiedby_value { get; set; }
                public string? _modifiedonbehalfby_value { get; set; }
            }
        }
        class JSONGetFieldPermissions
        {
            public string? odatacontext { get; set; }
            public Value[]? value { get; set; }

            public class Value
            {
                public string? odataetag { get; set; }
                public DateTime overwritetime { get; set; }
                public string? _organizationid_value { get; set; }
                public string? solutionid { get; set; }
                public int canread { get; set; }
                public bool ismanaged { get; set; }
                public string? _fieldsecurityprofileid_value { get; set; }
                public string? fieldpermissionid { get; set; }
                public string? attributelogicalname { get; set; }
                public int componentstate { get; set; }
                public int canupdate { get; set; }
                public string? fieldpermissionidunique { get; set; }
                public string? entityname { get; set; }
                public int versionnumber { get; set; }
                public int cancreate { get; set; }
            }
        }
        #endregion
    }
#pragma warning restore CS8604
}
