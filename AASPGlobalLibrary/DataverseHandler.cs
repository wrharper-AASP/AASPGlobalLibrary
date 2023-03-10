using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Azure.Core;
using System.Xml.Linq;
using static Azure.Core.HttpHeader;

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

        #region Get Account or Profile Info
        public async Task<string> GetAllDBInfoNoFilterJSON(string[] selectionColumnNames, string dbEndingPrefixSecretName, string keyvaultname)
        {
            string select = "?$select=";
            for (int i = 0; i < selectionColumnNames.Length; i++)
            {
                if (i != selectionColumnNames.Length - 1)
                    select += DbInfo.StartingPrefix + selectionColumnNames[i] + ",";
                else
                    select += DbInfo.StartingPrefix + selectionColumnNames[i];
            }
            string query = DbInfo.StartingPrefix + await VaultHandler.GetSecretInteractive(keyvaultname, dbEndingPrefixSecretName) + select;
            string token = await TokenHandler.GetDynamicsImpersonationToken(baseUrl);
            string fullquery = baseUrl + DbInfo.api + query;
            var results = await HttpClientHandler.GetJsonStringOdataAsync(token, fullquery);

            return results;
        }
        public async Task<string> GetAllDBInfoNoFilterJSON(List<string> selectionColumnNames, string dbEndingPrefixSecretName, string keyvaultname)
        {
            string select = "?$select=";
            for (int i = 0; i < selectionColumnNames.Count; i++)
            {
                if (i != selectionColumnNames.Count - 1)
                    select += DbInfo.StartingPrefix + selectionColumnNames[i] + ",";
                else
                    select += DbInfo.StartingPrefix + selectionColumnNames[i];
            }
            string query = DbInfo.StartingPrefix + await VaultHandler.GetSecretInteractive(keyvaultname, dbEndingPrefixSecretName) + select;
            string token = await TokenHandler.GetDynamicsImpersonationToken(baseUrl);
            string fullquery = baseUrl + DbInfo.api + query;
            var results = await HttpClientHandler.GetJsonStringOdataAsync(token, fullquery);

            return results;
        }
        public async Task<string> GetAllDBInfoFilteredJSON(string[] selectionColumnNames, int filteredColumnName, string filteredEqualTo, string dbEndingPrefixSecretName, string keyvaultname)
        {
            string select = "?$select=";
            for (int i = 0; i < selectionColumnNames.Length; i++)
            {
                if (i != selectionColumnNames.Length - 1)
                    select += DbInfo.StartingPrefix + selectionColumnNames[i] + ",";
                else
                    select += DbInfo.StartingPrefix + selectionColumnNames[i];
            }
            string filter = "&$filter=" + DbInfo.StartingPrefix + selectionColumnNames[filteredColumnName] + "%20eq%20%27" + filteredEqualTo + "%27";
            string query = DbInfo.StartingPrefix + await VaultHandler.GetSecretInteractive(keyvaultname, dbEndingPrefixSecretName) + select + filter;
            string token = await TokenHandler.GetDynamicsImpersonationToken(baseUrl);
            string fullquery = baseUrl + DbInfo.api + query;
            var results = await HttpClientHandler.GetJsonStringOdataAsync(token, fullquery);

            return results;
        }
        public async Task<string> GetAllDBInfoFilteredJSON(List<string> selectionColumnNames, int filteredColumnName, string filteredEqualTo, string dbEndingPrefixSecretName, string keyvaultname)
        {
            string select = "?$select=";
            for (int i = 0; i < selectionColumnNames.Count; i++)
            {
                if (i != selectionColumnNames.Count - 1)
                    select += DbInfo.StartingPrefix + selectionColumnNames[i] + ",";
                else
                    select += DbInfo.StartingPrefix + selectionColumnNames[i];
            }
            string filter = "&$filter=" + DbInfo.StartingPrefix + selectionColumnNames[filteredColumnName] + "%20eq%20%27" + filteredEqualTo + "%27";
            string query = DbInfo.StartingPrefix + await VaultHandler.GetSecretInteractive(keyvaultname, dbEndingPrefixSecretName) + select + filter;
            string token = await TokenHandler.GetDynamicsImpersonationToken(baseUrl);
            string fullquery = baseUrl + DbInfo.api + query;
            var results = await HttpClientHandler.GetJsonStringOdataAsync(token, fullquery);

            return results;
        }

        public async Task<string> GetAllDBInfoNoFilterJSON(string[] selectionColumnNames, string dbEndingPrefix)
        {
            string select = "?$select=";
            for (int i = 0; i < selectionColumnNames.Length; i++)
            {
                if (i != selectionColumnNames.Length - 1)
                    select += DbInfo.StartingPrefix + selectionColumnNames[i] + ",";
                else
                    select += DbInfo.StartingPrefix + selectionColumnNames[i];
            }
            string query = DbInfo.StartingPrefix + dbEndingPrefix + select;
            string token = await TokenHandler.GetDynamicsImpersonationToken(baseUrl);
            string fullquery = baseUrl + DbInfo.api + query;
            var results = await HttpClientHandler.GetJsonStringOdataAsync(token, fullquery);

            return results;
        }
        public async Task<string> GetAllDBInfoNoFilterJSON(List<string> selectionColumnNames, string dbEndingPrefix)
        {
            string select = "?$select=";
            for (int i = 0; i < selectionColumnNames.Count; i++)
            {
                if (i != selectionColumnNames.Count - 1)
                    select += DbInfo.StartingPrefix + selectionColumnNames[i] + ",";
                else
                    select += DbInfo.StartingPrefix + selectionColumnNames[i];
            }
            string query = DbInfo.StartingPrefix + dbEndingPrefix + select;
            string token = await TokenHandler.GetDynamicsImpersonationToken(baseUrl);
            string fullquery = baseUrl + DbInfo.api + query;
            var results = await HttpClientHandler.GetJsonStringOdataAsync(token, fullquery);

            return results;
        }
        public async Task<string> GetAllDBInfoFilteredJSON(string[] selectionColumnNames, int filteredColumnName, string filteredEqualTo, string dbEndingPrefix)
        {
            string select = "?$select=";
            for (int i = 0; i < selectionColumnNames.Length; i++)
            {
                if (i != selectionColumnNames.Length - 1)
                    select += DbInfo.StartingPrefix + selectionColumnNames[i] + ",";
                else
                    select += DbInfo.StartingPrefix + selectionColumnNames[i];
            }
            string filter = "&$filter=" + DbInfo.StartingPrefix + selectionColumnNames[filteredColumnName] + "%20eq%20%27" + filteredEqualTo + "%27";
            string query = DbInfo.StartingPrefix + dbEndingPrefix + select + filter;
            string token = await TokenHandler.GetDynamicsImpersonationToken(baseUrl);
            string fullquery = baseUrl + DbInfo.api + query;
            var results = await HttpClientHandler.GetJsonStringOdataAsync(token, fullquery);

            return results;
        }
        public async Task<string> GetAllDBInfoFilteredJSON(List<string> selectionColumnNames, int filteredColumnName, string filteredEqualTo, string dbEndingPrefix)
        {
            string select = "?$select=";
            for (int i = 0; i < selectionColumnNames.Count; i++)
            {
                if (i != selectionColumnNames.Count - 1)
                    select += DbInfo.StartingPrefix + selectionColumnNames[i] + ",";
                else
                    select += DbInfo.StartingPrefix + selectionColumnNames[i];
            }
            string filter = "&$filter=" + DbInfo.StartingPrefix + selectionColumnNames[filteredColumnName] + "%20eq%20%27" + filteredEqualTo + "%27";
            string query = DbInfo.StartingPrefix + dbEndingPrefix + select + filter;
            string token = await TokenHandler.GetDynamicsImpersonationToken(baseUrl);
            string fullquery = baseUrl + DbInfo.api + query;
            var results = await HttpClientHandler.GetJsonStringOdataAsync(token, fullquery);

            return results;
        }
        #endregion

        #region Create Account
        public async Task CreateAccountDB(string secretId, string smsPhoneNumber, string emailAccountColumnName, string whatsappid, string secretName, string internalkeyvaultname, string assignedto, string phonenumber, string phonenumberid)
        {
            await CreateAccountDB((await VaultHandler.GetSecretInteractive(internalkeyvaultname, secretId)).ToLower(), (await VaultHandler.GetSecretInteractive(internalkeyvaultname, secretName)).ToLower(), smsPhoneNumber, emailAccountColumnName, whatsappid, assignedto, phonenumber, phonenumberid);
        }
        public async Task CreateAccountDB(TokenCredential tokenCredential, string secretId, string smsPhoneNumber, string emailAccountColumnName, string whatsappid, string secretName, string internalkeyvaultname, string assignedto, string phonenumber, string phonenumberid)
        {
            await CreateAccountDB(tokenCredential, (await VaultHandler.GetSecretInteractive(internalkeyvaultname, secretId)).ToLower(), (await VaultHandler.GetSecretInteractive(internalkeyvaultname, secretName)).ToLower(), smsPhoneNumber, emailAccountColumnName, whatsappid, assignedto, phonenumber, phonenumberid);
        }
        public async Task CreateAccountDBSecret(string secretId, string secretSecret, string smsPhoneNumber, string emailAccountColumnName, string whatsappid, string secretName, string internalkeyvaultname, string environment, string assignedto, string phonenumber, string phonenumberid)
        {
            await CreateAccountDBSecret(await VaultHandler.GetSecretInteractive(internalkeyvaultname, secretId), await VaultHandler.GetSecretInteractive(internalkeyvaultname, secretSecret), (await VaultHandler.GetSecretInteractive(internalkeyvaultname, secretName)).ToLower(), smsPhoneNumber, emailAccountColumnName, whatsappid, environment, assignedto, phonenumber, phonenumberid);
        }

        public async Task CreateAccountDB(string appId, string dbEndingPrefix, string smsPhoneNumber, string filterEndingPrefix, string whatsappid, string assignedto, string phonenumber, string phonenumberid)
        {
            string database = DbInfo.StartingPrefix + dbEndingPrefix;
            string filter = "?$filter=" + DbInfo.StartingPrefix + filterEndingPrefix + "%20eq%20%27" + assignedto + "%27";
            string query = baseUrl + DbInfo.api + database + filter;
            Console.Write(Environment.NewLine + query);
            var token = await TokenHandler.GetDynamicsImpersonationToken(baseUrl);
            string jsonstring = await HttpClientHandler.GetJsonStringOdataAsync(token, query);
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
                ServiceClient service = await CreateStandardAuthServiceClient(appId);
                var entity = new Entity(string.Concat(DbInfo.StartingPrefix, dbEndingPrefix.AsSpan(0, dbEndingPrefix.Length - 2)));
                //var entity = new Entity(string.Concat(DbInfo.StartingPrefix, accountsdb));
                entity[DbInfo.StartingPrefix + filterEndingPrefix] = assignedto;
                entity[DbInfo.StartingPrefix + smsPhoneNumber] = phonenumber;
                entity[DbInfo.StartingPrefix + whatsappid] = phonenumberid;
                _ = await service.CreateAsync(entity);
            }
        }
        public async Task CreateAccountDB(TokenCredential tokenCredential, string appId, string dbEndingPrefix, string smsPhoneNumber, string filterEndingPrefix, string whatsappid, string assignedto, string phonenumber, string phonenumberid)
        {
            string database = DbInfo.StartingPrefix + dbEndingPrefix;
            string filter = "?$filter=" + DbInfo.StartingPrefix + filterEndingPrefix + "%20eq%20%27" + assignedto + "%27";
            string query = baseUrl + DbInfo.api + database + filter;
            Console.Write(Environment.NewLine + query);
            var token = await TokenHandler.GetDynamicsImpersonationToken(baseUrl);
            string jsonstring = await HttpClientHandler.GetJsonStringOdataAsync(token, query);
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
                ServiceClient service = await CreateStandardAuthServiceClient(tokenCredential, appId);
                var entity = new Entity(string.Concat(DbInfo.StartingPrefix, dbEndingPrefix.AsSpan(0, dbEndingPrefix.Length - 2)));
                //var entity = new Entity(string.Concat(DbInfo.StartingPrefix, accountsdb));
                entity[DbInfo.StartingPrefix + filterEndingPrefix] = assignedto;
                entity[DbInfo.StartingPrefix + smsPhoneNumber] = phonenumber;
                entity[DbInfo.StartingPrefix + whatsappid] = phonenumberid;
                _ = await service.CreateAsync(entity);
            }
        }
        public async Task CreateAccountDBSecret(string clientId, string clientSecret, string dbEndingPrefix, string smsPhoneNumber, string filterEndingPrefix, string whatsappid, string environment, string assignedto, string phonenumber, string phonenumberid)
        {
            string database = DbInfo.StartingPrefix + dbEndingPrefix;
            string filter = "?$filter=" + DbInfo.StartingPrefix + filterEndingPrefix + "%20eq%20%27" + assignedto + "%27";
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
                ServiceClient service = CreateSecretAuthServiceClient(clientId, clientSecret, environment);
                var entity = new Entity(string.Concat(DbInfo.StartingPrefix, dbEndingPrefix.AsSpan(0, dbEndingPrefix.Length - 2)));
                //var entity = new Entity(string.Concat(DbInfo.StartingPrefix, accountsdb));
                entity[DbInfo.StartingPrefix + filterEndingPrefix] = assignedto;
                entity[DbInfo.StartingPrefix + smsPhoneNumber] = phonenumber;
                entity[DbInfo.StartingPrefix + whatsappid] = phonenumberid;
                _ = await service.CreateAsync(entity);
            }
        }
        #endregion

        #region Update Users Account
        public async Task<string> PatchAccountDB(string whatsappid, string smsPhoneNumber, string emailAccountColumnName, string secretName, string keyvaultname, string email, dynamic json, string[] crosscompare)
        {
            return await PatchAccountDB((await VaultHandler.GetSecretInteractive(keyvaultname, secretName)).ToLower(), whatsappid, smsPhoneNumber, emailAccountColumnName, email, json, crosscompare);
        }
        public async Task<List<string>> PatchSMSDB(NativeWindow nativeWindow, string secretName, string keyvaultname, string email, string fromColumnName, string toColumnName, string emailNonAccountColumnName, dynamic json)
        {
            return await PatchSMSDB(nativeWindow, (await VaultHandler.GetSecretInteractive(keyvaultname, secretName)).ToLower(), email, fromColumnName, toColumnName, emailNonAccountColumnName, json);
        }
        public async Task<List<string>> PatchWhatsAppDB(NativeWindow nativeWindow, string fromColumnName, string toColumnName, string emailNonAccountColumnName, string secretName, string keyvaultname, string email, dynamic json)
        {
            return await PatchWhatsAppDB(nativeWindow, (await VaultHandler.GetSecretInteractive(keyvaultname, secretName)).ToLower(), email, fromColumnName, toColumnName, emailNonAccountColumnName, json);
        }

        public async Task<string> PatchAccountDB(string dbEndingPrefix, string whatsappid, string smsPhoneNumber, string emailAccountColumnName, string email, dynamic json, string[] crosscompare)
        {
            string database = DbInfo.StartingPrefix + dbEndingPrefix;
            //string select = string.Concat("?$select=", database.AsSpan(0, database.Length - 2), "id");
            //string filter = "&$filter=" + DbInfo.StartingPrefix + emailAccountColumnName + "%20eq%20%27" + email + "%27";
            string filter = "?$filter=" + DbInfo.StartingPrefix + emailAccountColumnName + "%20eq%20%27" + email + "%27";
            var token = await TokenHandler.GetDynamicsImpersonationToken(baseUrl);
            //string jsonstring = await HttpClientHandler.GetJsonStringOdataAsync(token, baseUrl + DbInfo.api + database + select + filter);
            string jsonstring = await HttpClientHandler.GetJsonStringOdataAsync(token, baseUrl + DbInfo.api + database + filter);
            try
            {
                string accountDBIDColumnName = string.Concat(dbEndingPrefix.AsSpan(0, dbEndingPrefix.Length - 2), "id");
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
        public async Task<string> PatchAccountDB(string dbEndingPrefix, string filterEndingPrefix, string filterEqualTo, dynamic json)
        {
            string database = DbInfo.StartingPrefix + dbEndingPrefix;
            //string select = string.Concat("?$select=", database.AsSpan(0, database.Length - 2), "id");
            //string filter = "&$filter=" + DbInfo.StartingPrefix + emailAccountColumnName + "%20eq%20%27" + email + "%27";
            string filter = "?$filter=" + DbInfo.StartingPrefix + filterEndingPrefix + "%20eq%20%27" + filterEqualTo + "%27";
            var token = await TokenHandler.GetDynamicsImpersonationToken(baseUrl);
            //string jsonstring = await HttpClientHandler.GetJsonStringOdataAsync(token, baseUrl + DbInfo.api + database + select + filter);
            string query = baseUrl + DbInfo.api + database + filter;
            string jsonstring = await HttpClientHandler.GetJsonStringOdataAsync(token, query);
            try
            {
                string accountDBIDColumnName = string.Concat(dbEndingPrefix.AsSpan(0, dbEndingPrefix.Length - 2), "id");
                Console.WriteLine(accountDBIDColumnName);
                dynamic getjsonid = Globals.DynamicJsonDeserializer(jsonstring);
                if (getjsonid.value.Count > 1)
                    Console.Write(Environment.NewLine + "Duplicates of this phone number have been found.");

                var id = Globals.FindDynamicDataverseValue(getjsonid, DbInfo.StartingPrefix + accountDBIDColumnName, 0);
                string specificaccount = database + "(" + id + ")";
                return await HttpClientHandler.PatchJsonStringOdataAsync(token, baseUrl + DbInfo.api + specificaccount, JsonSerializer.Serialize(json));
            }
            catch
            {
                return jsonstring;
            }
        }
        public async Task<List<string>> PatchSMSDB(NativeWindow nativeWindow, string dbEndingPrefix, string email, string fromColumnName, string toColumnName, string emailNonAccountColumnName, dynamic json)
        {
            string currentSMSDatabase = DbInfo.StartingPrefix + dbEndingPrefix;
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
        public async Task<List<string>> PatchWhatsAppDB(NativeWindow nativeWindow, string dbEndingPrefix, string fromColumnName, string toColumnName, string emailNonAccountColumnName, string email, dynamic json)
        {
            string database = DbInfo.StartingPrefix + dbEndingPrefix;
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
        public async Task<string> DeleteAccountDB(string whatsappid, string smsPhoneNumber, string emailAccountColumnName, string secretName, string keyvaultname, string email, string[] crosscompare)
        {
            return await DeleteAccountDB((await VaultHandler.GetSecretInteractive(keyvaultname, secretName)).ToLower(), whatsappid, smsPhoneNumber, emailAccountColumnName, email, crosscompare);
        }

        public async Task<string> DeleteAccountDB(string dbEndingPrefix, string whatsappid, string smsPhoneNumber, string emailAccountColumnName, string email, string[] crosscompare)
        {
            string database = DbInfo.StartingPrefix + dbEndingPrefix;
            //string select = "?$select=" + DbInfo.StartingPrefix + phoneNumberIDColumnName;
            //string filter = "&$filter=" + DbInfo.StartingPrefix + emailAccountColumnName + "%20eq%20%27" + email + "%27";
            string filter = "?$filter=" + DbInfo.StartingPrefix + emailAccountColumnName + "%20eq%20%27" + email + "%27";
            var token = await TokenHandler.GetDynamicsImpersonationToken(baseUrl);
            //Console.Write(baseUrl + api + database + select + filter);
            //string jsonstring = await HttpClientHandler.GetJsonStringOdataAsync(token, baseUrl + DbInfo.api + database + select + filter);
            string jsonstring = await HttpClientHandler.GetJsonStringOdataAsync(token, baseUrl + DbInfo.api + database + filter);

            try
            {
                string accountDBIDColumnName = string.Concat(dbEndingPrefix.AsSpan(0, dbEndingPrefix.Length - 2), "id");
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

        class JSONPOSTSystemUser
        {
            public int accessmode { get; set; }
            public string? applicationid { get; set; }
            public string? defaultodbfoldername { get; set; }
            public string? businessunitid { get; set; }
        }
        /* Access Modes
        0	Read-Write
        1	Administrative
        2	Read
        3	Support User
        4	Non-interactive - Default
        5	Delegated Admin */
        public async Task<HttpResponseMessage> CreateSystemUser(string appRegistrationClientId, int accessMode = 4)
        {
            string businessID = await GetBusinessID(baseUrl);
            JSONPOSTSystemUser systemUser = new()
            {
                accessmode = accessMode,
                applicationid = appRegistrationClientId,
                defaultodbfoldername = "Dynamics365",
                businessunitid = "/businessunits(" + businessID + ")"
            };

            string json = JsonSerializer.Serialize(systemUser);
            json = json.Replace("businessunitid", "businessunitid@odata.bind");

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
            /*Newtonsoft.Json.Linq.JObject newAppUser = new()
                { 
                    "accessmode", accessMode,
                    "businessunitid@odata.bind", "/businessunits(" + businessID + ")",
                    "applicationid", appRegistrationClientId,
                    "defaultodbfoldername", "Dynamics365"
                };*/
            //Console.Write(newAppUser.ToString());
            var response = await client.PostAsJsonAsync(baseUrl + DbInfo.api + "systemusers", json);

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
                /*
                legacy? - no longer works for some reason.

                var systemUserURL = "api/data/v9.2/systemusers(" + systemuserId + ")";
                string odataid = "\"@odata.id\": \"" + baseUrl + systemUserURL + "\"";
                string json = "{" + odataid + "}";
                Newtonsoft.Json.JsonConvert.SerializeObject(json, Newtonsoft.Json.Formatting.Indented);
                string fieldsecurityprofileid = "";
                var securitystring = await HttpClientHandler.GetJsonStringAsync(token, baseUrl, "api/data/v9.2/fieldsecurityprofiles");
                JSONGetSecurityFields jsonSecurityFields = JsonSerializer.Deserialize<JSONGetSecurityFields>(securitystring);
                for (int i = 0; i < jsonSecurityFields.value.Length; i++)
                {
                    if (jsonSecurityFields.value[i].name == "System Administrator")
                    {
                        fieldsecurityprofileid = jsonSecurityFields.value[i].fieldsecurityprofileid;
                        break;
                    }
                }
                string requestUrl = "api/data/v9.2/fieldsecurityprofiles(" + fieldsecurityprofileid + ")/systemuserprofiles_association/$ref";
                */

                string requestUrl = "api/data/v9.2/systemusers(" + systemuserId + ")/systemuserroles_association/$ref";

                JSONGetRoles jsonRoles = await HttpClientHandler.GetJsonAsync(token, baseUrl, new JSONGetRoles(), "/api/data/v9.2/roles");
                string roleId = "";
                for (int i = 0; i < jsonRoles.value.Length; i++)
                {
                    if (jsonRoles.value[i].name == "System Administrator")
                    {
                        roleId = jsonRoles.value[i].roleid;
                        break;
                    }
                }
                string json = "{" + "\"@odata.id\":\"" + baseUrl + "api/data/v9.2/roles(" + roleId + ")\"}";
                json = json.Replace("\\", "");
                _ = await HttpClientHandler.PostJsonStringBearerWithODataAsync(token, baseUrl, requestUrl, json);
                Console.Write(Environment.NewLine + "System Administrator permission assigned.");
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
            CreateAttributeRequest[] createAttributeRequests = new CreateAttributeRequest[7];

            createAttributeRequests[0] = CreateAttributeMetaData(DbInfo.metadataEmailNonAccount, whatsAppDBName, 36);
            createAttributeRequests[1] = CreateAttributeMetaData(DbInfo.metadataFrom, whatsAppDBName, 20);
            createAttributeRequests[2] = CreateAttributeMetaData(DbInfo.metadataMessage, whatsAppDBName, 4000);
            createAttributeRequests[3] = CreateAttributeMetaData(DbInfo.metadataTo, whatsAppDBName, 20);
            createAttributeRequests[4] = CreateAttributeMetaData(DbInfo.metadataTimestamp, whatsAppDBName, 100);
            createAttributeRequests[5] = CreateAttributeMetaData(DbInfo.metadataMediaIdentifier, whatsAppDBName, 30);
            createAttributeRequests[6] = new CreateAttributeRequest
            {
                //must be lowercase or errors will occur
                EntityName = DbInfo.StartingPrefix + whatsAppDBName.ToLower() + "es",
                Attribute = new BigIntAttributeMetadata
                {
                    LogicalName = DbInfo.metadataCounterName,
                    //must be lowercase or errors will occur
                    SchemaName = DbInfo.StartingPrefix + DbInfo.metadataCounterName.ToLower() + "es",
                    RequiredLevel = new AttributeRequiredLevelManagedProperty(AttributeRequiredLevel.None),
                    DisplayName = new Microsoft.Xrm.Sdk.Label(DbInfo.metadataCounterName, 1033)
                }
            };

            await RunMutiplePushes(service, createAttributeRequests);
        }
        async Task CreateSMSDefaultColumns(ServiceClient service, string smsDBName)
        {
            CreateAttributeRequest[] createAttributeRequests = new CreateAttributeRequest[7];

            createAttributeRequests[0] = CreateAttributeMetaData(DbInfo.metadataEmailNonAccount, smsDBName, 36);
            createAttributeRequests[1] = CreateAttributeMetaData(DbInfo.metadataFrom, smsDBName, 20);
            createAttributeRequests[2] = CreateAttributeMetaData(DbInfo.metadataMessage, smsDBName, 4000);
            createAttributeRequests[3] = CreateAttributeMetaData(DbInfo.metadataTo, smsDBName, 20);
            createAttributeRequests[4] = CreateAttributeMetaData(DbInfo.metadataTimestamp, smsDBName, 100);
            createAttributeRequests[5] = CreateAttributeMetaData(DbInfo.metadataMediaIdentifier, smsDBName, 100);
            createAttributeRequests[6] = new CreateAttributeRequest
            {
                //must be lowercase or errors will occur
                EntityName = DbInfo.StartingPrefix + smsDBName.ToLower() + "es",
                Attribute = new BigIntAttributeMetadata
                {
                    LogicalName = DbInfo.metadataCounterName,
                    //must be lowercase or errors will occur
                    SchemaName = DbInfo.StartingPrefix + DbInfo.metadataCounterName.ToLower() + "es",
                    RequiredLevel = new AttributeRequiredLevelManagedProperty(AttributeRequiredLevel.None),
                    DisplayName = new Microsoft.Xrm.Sdk.Label(DbInfo.metadataCounterName, 1033)
                }
            };

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

            createAttributeRequests[0] = CreateAttributeMetaData(DbInfo.metadataPhoneNumberID, phoneIDDBName, 36);
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
        async Task<ServiceClient> CreateStandardAuthServiceClient(string appId)
        {
            var connectionString = @"AuthType='OAuth'; Username='" +
                await TokenHandler.JwtGetUsersInfo.GetUsersEmail() +
                "'; Password='passcode'; Url='" + baseUrl + "'; AppId='" +
                appId +
                "'; RedirectUri='http://localhost:65135'; LoginPrompt='Auto'";

            return new ServiceClient(connectionString);
        }
        async Task<ServiceClient> CreateStandardAuthServiceClient(TokenCredential tokenCredential, string appId)
        {
            var connectionString = @"AuthType='OAuth'; Username='" +
                await TokenHandler.JwtGetUsersInfo.GetUsersEmail(tokenCredential) +
                "'; Password='passcode'; Url='" + baseUrl + "'; AppId='" +
                appId +
                "'; RedirectUri='http://localhost:65135'; LoginPrompt='Auto'";

            return new ServiceClient(connectionString);
        }
        async Task<ServiceClient> CreateStandardAuthServiceClient(string idSecretName, string keyvaultname)
        {
            var connectionString = @"AuthType='OAuth'; Username='" +
                await TokenHandler.JwtGetUsersInfo.GetUsersEmail() +
                "'; Password='passcode'; Url='" + baseUrl + "'; AppId='" +
                await VaultHandler.GetSecretInteractive(keyvaultname, idSecretName) +
                "'; RedirectUri='http://localhost:65135'; LoginPrompt='Auto'";

            return new ServiceClient(connectionString);
        }
        async Task<ServiceClient> CreateStandardAuthServiceClient(TokenCredential tokenCredential, string idSecretName, string keyvaultname)
        {
            var connectionString = @"AuthType='OAuth'; Username='" +
                await TokenHandler.JwtGetUsersInfo.GetUsersEmail(tokenCredential) +
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
        static ServiceClient CreateSecretAuthServiceClient(string clientId, string clientSecret, string environment)
        {
            //cannot get this to work currently...
            var connectionString = @"AuthType='ClientSecret'; Url='https://" + environment +
                ".crm.dynamics.com'; ClientId='" + clientId +
                "'; ClientSecret='" + clientSecret +
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
            public string? metadataMediaIdentifier { get; set; }
            public string? metadataCounterName { get; set; }
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
        class JSONGetRoles
        {
            public string? odatacontext { get; set; }
            public Value[]? value { get; set; }

            public class Value
            {
                public string? odataetag { get; set; }
                public string? overwritetime { get; set; }
                public string? organizationid { get; set; }
                public int? isinherited { get; set; }
                public string? solutionid { get; set; }
                public string? roleidunique { get; set; }
                public string? _createdby_value { get; set; }
                public string? roleid { get; set; }
                public int? componentstate { get; set; }
                public string? modifiedon { get; set; }
                public string? _modifiedby_value { get; set; }
                public string? _parentrootroleid_value { get; set; }
                public bool? ismanaged { get; set; }
                public string? createdon { get; set; }
                public int? versionnumber { get; set; }
                public string? _businessunitid_value { get; set; }
                public string? name { get; set; }
                public string? _parentroleid_value { get; set; }
                public string? overriddencreatedon { get; set; }
                public string? importsequencenumber { get; set; }
                public string? _modifiedonbehalfby_value { get; set; }
                public string? _roletemplateid_value { get; set; }
                public string? _createdonbehalfby_value { get; set; }
                public Iscustomizable? iscustomizable { get; set; }
                public Canbedeleted? canbedeleted { get; set; }
            }

            public class Iscustomizable
            {
                public bool? Value { get; set; }
                public bool? CanBeChanged { get; set; }
                public string? ManagedPropertyLogicalName { get; set; }
            }

            public class Canbedeleted
            {
                public bool? Value { get; set; }
                public bool? CanBeChanged { get; set; }
                public string? ManagedPropertyLogicalName { get; set; }
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
