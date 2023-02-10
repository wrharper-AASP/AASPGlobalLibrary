using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;

//Removes a lot of the complexity for Azure Authentication and automatically handles tokens
namespace AASPGlobalLibrary
{
    public class TokenHandler
    {
        public static TokenCredential? tokenCredential;
        public static TokenCredential? managedTokenCredential;

        #region Client Handling
        class UsePublicClientTokenHandler
        {
            DateTime lasttime = DateTime.Now;
            string lasttoken = "";
            IPublicClientApplication? publicapp;

            internal async Task<string> GetToken(string tenantId, string clientId, string[] scopes, bool UseHttps)
            {
                if (DateTime.Now <= lasttime)
                {
                    publicapp ??= PublicClientApplicationBuilder.Create(clientId)
                            .WithTenantId(tenantId)
                            .WithRedirectUri(Globals.LocalHostLoginAuth(UseHttps))
                            .Build();

                    var accounts = await publicapp.GetAccountsAsync();
                    AuthenticationResult result;
                    try
                    {
                        result = await publicapp.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                                    .ExecuteAsync();
                    }
                    catch (MsalUiRequiredException)
                    {
                        result = await publicapp.AcquireTokenInteractive(scopes)
                                    .ExecuteAsync();
                    }
                    lasttoken = result.AccessToken;
                    lasttime = result.ExpiresOn.DateTime;
                }
                return lasttoken;
            }
        }
        static readonly UsePublicClientTokenHandler UsePublicClientToken = new();
        public static async Task<string> GetPublicClientAccessToken(string clientId, string[] scopes, string tenantId, bool UseHttps=false)
        {
            return await UsePublicClientToken.GetToken(tenantId, clientId, scopes, UseHttps);
        }

        class UseConfidentialClientTokenHandler
        {
            DateTime lasttime = DateTime.Now;
            string lasttoken = "";
            IConfidentialClientApplication? app;

            internal async Task<string> GetToken(string tenantId, string clientId, string[] scopes, string secret)
            {

                if (DateTime.Now <= lasttime)
                {
                    if (app == null)
                    {
                        try
                        {
                            var storageProperties =
                             new StorageCreationPropertiesBuilder("TokenCache", Environment.CurrentDirectory + "/TokenCache")
                                .Build();

                            app = ConfidentialClientApplicationBuilder.Create(clientId)
                                            .WithClientSecret(secret)
                                            .WithTenantId(tenantId)
                                            .WithRedirectUri(Globals.LocalHostLoginAuth(true))
                                            .WithLegacyCacheCompatibility(false)
                                            .Build();

                            MsalCacheHelper cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
                            cacheHelper.RegisterCache(app.UserTokenCache);
                        }
                        catch (Exception ex)
                        {
                            Console.Write(Environment.NewLine + "MSAL Failed, reverting to legacy ADAL: " + ex.Message);

                            app = ConfidentialClientApplicationBuilder.Create(clientId)
                                .WithClientSecret(secret)
                                .WithTenantId(tenantId)
                                .WithRedirectUri(Globals.LocalHostLoginAuth(true))
                                .WithLegacyCacheCompatibility(true)
                                .Build();
                        }
                    }

                    AuthenticationResult result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
                    lasttoken = result.AccessToken;
                    lasttime = result.ExpiresOn.DateTime;
                }
                return lasttoken;
            }
        }
        static readonly UseConfidentialClientTokenHandler UseConfidentialClientToken = new();
        public static async Task<string> GetConfidentialClientAccessToken(string clientId, string secret, string[] scopes, string tenantId)
        {
            return await UseConfidentialClientToken.GetToken(tenantId, clientId, scopes, secret);
        }

        public static ArmClient CreateArmClient()
        {
            tokenCredential ??= new InteractiveBrowserCredential();
            return new ArmClient(tokenCredential);
        }
        //must have environmental variable with "vaultname" defined
        public static SecretClient GetFunctionAppKeyVaultClient(string vaultname = "")
        {
            if (vaultname == "")
            {
                managedTokenCredential ??= new ManagedIdentityCredential();
                return new SecretClient(new Uri(Globals.VaultBase(Environment.GetEnvironmentVariable("vaultname"))), managedTokenCredential);
            }
            else
            {
                tokenCredential ??= new InteractiveBrowserCredential();
                return new SecretClient(new Uri(vaultname), tokenCredential);
            }
        }
        #endregion

        #region JWT
        public static class JwtGetUsersInfo
        {
            static JwtSecurityToken ParseJwt(string token)
            {
                if (string.IsNullOrEmpty(token))
                {
                    throw new Exception("SSO token is null or empty.");
                }

                JwtSecurityTokenHandler jwtSecurityTokenHandler = new();
                try
                {
                    if (jwtSecurityTokenHandler.ReadToken(token) is not JwtSecurityToken jwtSecurityToken || string.IsNullOrEmpty(jwtSecurityToken.Payload["exp"].ToString()))
                    {
                        throw new Exception("Decoded token is null or exp claim does not exists.");
                    }
                    /*JwtSecurityToken jwtSecurityToken = jwtSecurityTokenHandler.ReadToken(token) as JwtSecurityToken;
                    if (jwtSecurityToken == null || string.IsNullOrEmpty(jwtSecurityToken.Payload["exp"].ToString()))
                    {
                        throw new Exception("Decoded token is null or exp claim does not exists.");
                    }*/

                    return jwtSecurityToken;
                }
                catch (ArgumentException ex)
                {
                    throw new Exception("Parse jwt token failed with error: " + ex.Message, ex.InnerException);
                }
            }

            public static async Task<string> GetUsersEmail(TokenCredential tokenCredential)
            {
                JwtSecurityToken jwtSecurity = ParseJwt(await GetKeyVaultImpersonationToken(tokenCredential));
                string text = jwtSecurity.Payload["ver"].ToString();
                string name = "";

                if (text == "2.0")
                {
                    name = jwtSecurity.Payload["preferred_username"].ToString();
                }
                else if (text == "1.0")
                {
                    name = jwtSecurity.Payload["upn"].ToString();
                }
                return name;
            }
            public static async Task<string> GetUsersEmail()
            {
                tokenCredential ??= new InteractiveBrowserCredential();
                JwtSecurityToken jwtSecurity = ParseJwt(await GetKeyVaultImpersonationToken());
                string text = jwtSecurity.Payload["ver"].ToString();
                string name = "";

                if (text == "2.0")
                {
                    name = jwtSecurity.Payload["preferred_username"].ToString();
                }
                else if (text == "1.0")
                {
                    name = jwtSecurity.Payload["upn"].ToString();
                }
                return name;
            }

            public static async Task<string> GetUsersID()
            {
                tokenCredential ??= new InteractiveBrowserCredential();
                JwtSecurityToken jwtSecurity = ParseJwt(await GetKeyVaultImpersonationToken());
                string id = jwtSecurity.Payload["oid"].ToString();
                return id;
            }
            public static async Task<string> GetUsersID(TokenCredential tokenCredential)
            {
                JwtSecurityToken jwtSecurity = ParseJwt(await GetKeyVaultImpersonationToken(tokenCredential));
                string id = jwtSecurity.Payload["oid"].ToString();
                return id;
            }
        }
        #endregion

        #region Token Handling
        class UseUserDelegatedTokenHandler
        {
            DateTime lasttime = DateTime.Now;
            string lasttoken = "";
            public async Task<string> GetToken(TokenCredential _tokenCredential, string[] scopes)
            {
                if (DateTime.Now <= lasttime)
                {
                    AccessToken _Local = await _tokenCredential.GetTokenAsync(new TokenRequestContext(scopes), new CancellationToken());
                    lasttime = _Local.ExpiresOn.DateTime;
                    lasttoken = _Local.Token;
                }
                return lasttoken;
            }
        }
        class UseOAuthDirectTokenHandler
        {
            DateTime lasttime = DateTime.Now;
            string lasttoken = "";

            public async Task<string> GetToken(string tenantid, string clientid, string clientsecret, string scope)
            {
                if (DateTime.Now <= lasttime)
                {
                    using HttpClient client = new();
                    var data = new[]
                    {
                            new KeyValuePair<string, string>("grant_type", "client_credentials"),
                            new KeyValuePair<string, string>("client_id", clientid),
                            new KeyValuePair<string, string>("scope", scope),
                            new KeyValuePair<string, string>("client_secret", clientsecret),
                        };
                    var formurl = new FormUrlEncodedContent(data);
                    HttpResponseMessage response = await client.PostAsync("https://login.microsoftonline.com/" + tenantid + "/oauth2/v2.0/token", formurl);
                    JSONOAuthToken oauth = JsonSerializer.Deserialize<JSONOAuthToken>(await response.Content.ReadAsStringAsync());
                    lasttime = DateTime.Parse(oauth.expires_in.ToString());
                    if (oauth.access_token != null)
                        lasttoken = oauth.access_token;
                }
                return lasttoken;
            }
        }
        static readonly UseUserDelegatedTokenHandler UseUserDelegatedToken = new();
        static readonly UseOAuthDirectTokenHandler UseOAuthDirectToken = new();

        class JSONOAuthToken
        {
            public string? token_type { get; set; }
            public int expires_in { get; set; }
            public int ext_expires_in { get; set; }
            public string? access_token { get; set; }
        }
        public static async Task<string> GetOAuthDirectToken(string tenantid, string clientid, string clientsecret, string scope)
        {
            return await UseOAuthDirectToken.GetToken(tenantid, clientid, clientsecret, scope);
        }

        public static async Task<string> GetCustomToken(TokenCredential tokenC, string[] scope)
        {
            return await UseUserDelegatedToken.GetToken(tokenC, scope);
        }
        public static async Task<string> GetDefaultGraphToken(TokenCredential tokenC)
        {
            return await UseUserDelegatedToken.GetToken(tokenC, new[] { "https://graph.microsoft.com/.default" });
        }
        public static async Task<string> GetKeyVaultImpersonationToken(TokenCredential tokenC)
        {
            return await UseUserDelegatedToken.GetToken(tokenC, new[] { "https://vault.azure.net/user_impersonation" });
        }
        public static async Task<string> GetDynamicsImpersonationToken(TokenCredential tokenC, string environmentName)
        {
            return await UseUserDelegatedToken.GetToken(tokenC, new[] { environmentName + "/user_impersonation" });
        }
        public static async Task<string> GetGlobalDynamicsImpersonationToken(TokenCredential tokenC)
        {
            return await UseUserDelegatedToken.GetToken(tokenC, new[] { "https://globaldisco.crm.dynamics.com/user_impersonation" });
        }

        public static async Task<string> GetCustomToken(string[] scope)
        {
            tokenCredential ??= new InteractiveBrowserCredential();
            return await UseUserDelegatedToken.GetToken(tokenCredential, scope);
        }
        public static async Task<string> GetCustomManagedToken(string[] scope)
        {
            managedTokenCredential ??= new ManagedIdentityCredential();
            return await UseUserDelegatedToken.GetToken(managedTokenCredential, scope);
        }
        public static async Task<string> GetDefaultGraphToken()
        {
            tokenCredential ??= new InteractiveBrowserCredential();
            return await UseUserDelegatedToken.GetToken(tokenCredential, new[] { "https://graph.microsoft.com/.default" });
        }
        public static async Task<string> GetKeyVaultImpersonationToken()
        {
            tokenCredential ??= new InteractiveBrowserCredential();
            return await UseUserDelegatedToken.GetToken(tokenCredential, new[] { "https://vault.azure.net/user_impersonation" });
        }
        public static async Task<string> GetDynamicsImpersonationToken(string environmentName)
        {
            tokenCredential ??= new InteractiveBrowserCredential();
            return await UseUserDelegatedToken.GetToken(tokenCredential, new[] { environmentName + "/user_impersonation" });
        }
        public static async Task<string> GetGlobalDynamicsImpersonationToken()
        {
            tokenCredential ??= new InteractiveBrowserCredential();
            return await UseUserDelegatedToken.GetToken(tokenCredential, new[] { "https://globaldisco.crm.dynamics.com/user_impersonation" });
        }
        #endregion

        /*public static async Task<string> GetFacebookAPIToken(SecretClient sc)
        {
            return (await sc.GetSecretAsync("")).Value.Value;
        }*/
    }
}
