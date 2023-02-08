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
        static IConfidentialClientApplication? app;
        static IPublicClientApplication? publicapp;

        #region Client Handling
        public static async Task<string> GetPublicClientAccessToken(string clientId, string[] scopes, string tenantId, bool UseHttps=false)
        {
            (string redirect2, string redirect) = Globals.LocalHostLoginAuth();
            if (!UseHttps)
            {
                publicapp ??= PublicClientApplicationBuilder.Create(clientId)
                        .WithTenantId(tenantId)
                        .WithRedirectUri(redirect2)
                        .Build();
            }
            else
            {
                publicapp ??= PublicClientApplicationBuilder.Create(clientId)
                        .WithTenantId(tenantId)
                        .WithRedirectUri(redirect)
                        .Build();
            }

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
            return result.AccessToken;
        }
        public static async Task<string> GetConfidentialClientAccessToken(string clientId, string secret, string[] scopes, string tenantId)
        {
            if (app == null)
            {
                try
                {
                    (_, string redirect) = Globals.LocalHostLoginAuth();
                    var storageProperties =
                     new StorageCreationPropertiesBuilder("TokenCache", Environment.CurrentDirectory + "/TokenCache")
                        .Build();

                    app = ConfidentialClientApplicationBuilder.Create(clientId)
                        .WithClientSecret(secret)
                        .WithTenantId(tenantId)
                        .WithRedirectUri(redirect)
                        .WithLegacyCacheCompatibility(false)
                        .Build();

                    var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
                    cacheHelper.RegisterCache(app.UserTokenCache);
                }
                catch(Exception ex)
                {
                    Console.Write(Environment.NewLine + "MSAL Failed, reverting to legacy ADAL: " + ex.Message);

                    (_, string redirect) = Globals.LocalHostLoginAuth();
                    app = ConfidentialClientApplicationBuilder.Create(clientId)
                        .WithClientSecret(secret)
                        .WithTenantId(tenantId)
                        .WithRedirectUri(redirect)
                        .WithLegacyCacheCompatibility(true)
                        .Build();
                }
            }

            string result = (await app.AcquireTokenForClient(scopes).ExecuteAsync()).AccessToken;

            return result; 
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
        class JSONOAuthToken
        {
            public string? token_type { get; set; }
            public int expires_in { get; set; }
            public int ext_expires_in { get; set; }
            public string? access_token { get; set; }
        }
        public static async Task<string> GetOAuthDirectToken(string tenantid, string clientid, string clientsecret, string scope)
        {
            using (HttpClient client = new())
            {
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
                return oauth.access_token;
            }
        }

        public static async Task<string> GetCustomToken(TokenCredential tokenC, string[] scope)
        {
            return (await tokenC.GetTokenAsync(new TokenRequestContext(scope), new CancellationToken())).Token;
        }
        public static async Task<string> GetDefaultGraphToken(TokenCredential tokenC)
        {
            return (await tokenC.GetTokenAsync(new TokenRequestContext(new string[] { "https://graph.microsoft.com/.default" }), new CancellationToken())).Token;
        }
        public static async Task<string> GetKeyVaultImpersonationToken(TokenCredential tokenC)
        {
            return (await tokenC.GetTokenAsync(new TokenRequestContext(new string[] { "https://vault.azure.net/user_impersonation" }), new CancellationToken())).Token;
        }
        public static async Task<string> GetDynamicsImpersonationToken(TokenCredential tokenC, string environmentName)
        {
            return (await tokenC.GetTokenAsync(new TokenRequestContext(new string[] { environmentName + "/user_impersonation" }), new CancellationToken())).Token;
        }
        public static async Task<string> GetGlobalDynamicsImpersonationToken(TokenCredential tokenC)
        {
            return (await tokenC.GetTokenAsync(new TokenRequestContext(new string[] { "https://globaldisco.crm.dynamics.com/user_impersonation" }), new CancellationToken())).Token;
        }

        public static async Task<string> GetCustomToken(string[] scope)
        {
            tokenCredential ??= new InteractiveBrowserCredential();
            return (await tokenCredential.GetTokenAsync(new TokenRequestContext(scope), new CancellationToken())).Token;
        }
        public static async Task<string> GetCustomManagedToken(string[] scope)
        {
            managedTokenCredential ??= new ManagedIdentityCredential();
            return (await managedTokenCredential.GetTokenAsync(new TokenRequestContext(scope), new CancellationToken())).Token;
        }
        public static async Task<string> GetDefaultGraphToken()
        {
            tokenCredential ??= new InteractiveBrowserCredential();
            return (await tokenCredential.GetTokenAsync(new TokenRequestContext(new string[] { "https://graph.microsoft.com/.default" }), new CancellationToken())).Token;
        }
        public static async Task<string> GetKeyVaultImpersonationToken()
        {
            tokenCredential ??= new InteractiveBrowserCredential();
            return (await tokenCredential.GetTokenAsync(new TokenRequestContext(new string[] { "https://vault.azure.net/user_impersonation" }), new CancellationToken())).Token;
        }
        public static async Task<string> GetDynamicsImpersonationToken(string environmentName)
        {
            tokenCredential ??= new InteractiveBrowserCredential();
            return (await tokenCredential.GetTokenAsync(new TokenRequestContext(new string[] { environmentName + "/user_impersonation" }), new CancellationToken())).Token;
        }
        public static async Task<string> GetGlobalDynamicsImpersonationToken()
        {
            tokenCredential ??= new InteractiveBrowserCredential();
            return (await tokenCredential.GetTokenAsync(new TokenRequestContext(new string[] { "https://globaldisco.crm.dynamics.com/user_impersonation" }), new CancellationToken())).Token;
        }
        #endregion

        /*public static async Task<string> GetFacebookAPIToken(SecretClient sc)
        {
            return (await sc.GetSecretAsync("")).Value.Value;
        }*/
    }
}
