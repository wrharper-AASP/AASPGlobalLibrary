using Microsoft.Graph;

//Used to automatically create Azure API's.
//A heavily needed process to allow for a lot more automation in the future
//For those who need app registration
namespace AASPGlobalLibrary
{
    //CRITICAL ERROS WILL OCCUR: web redirect has been disabled. it was creating connection errors to dataverse.
    public class CreateAzureAPIHandler
    {
        const string signInAudience = "AzureADandPersonalMicrosoftAccount";
        const string noSDKSupport = "NoLiveSdkSupport";

        const string graphAppId = "00000003-0000-0000-c000-000000000000";
        const string graphUserReadId = "e1fe6dd8-ba31-4d61-89e7-88639da4683d";
        const string graphMailSendId = "e383f46e-2787-4529-855e-0e479a3ffac0";

        const string dynamicsAppId = "00000007-0000-0000-c000-000000000000";
        const string dynamicsUserImpersonationId = "78ce3f0f-a1ce-49c2-8cde-64b5c0896db4";

        public static async Task<string> AddSecretClientPasswordAsync(GraphServiceClient gs, string appObjectId, string appid, string secretDisplayName, bool infiniteloop = true)
        {
            JSONGetClientSecretResponseDataverseAPI content = new()
            {
                secretText = ""
            };

            if (infiniteloop)
            {
                while (infiniteloop)
                {
                    try
                    {
                        var publicClient = new JSONAddClientInfoDataverseAPI.Publicclient(appid);
                        var end = await gs.Applications[appObjectId].AddPassword(JSONAddClientInfoDataverseAPI.Publicclient.PasswordCredentials(secretDisplayName)).Request().PostAsync();

                        content.secretText = end.SecretText;

                        //TODO: data needed for all apps that use it.
                        //content.secretText

                        infiniteloop = false;
                    }
                    catch (Exception ex)
                    {
                        //reduce the spam to azure servers and graph api
                        await Task.Delay(5000);
                        if (ex.Message.StartsWith("Code: Request_ResourceNotFound"))
                            Console.Write(Environment.NewLine + "Waiting for app resource to add secret for: " + appid);
                        else
                            Console.Write(Environment.NewLine + ex.Message);
                    }
                }
            }
            else
            {
                try
                {
                    var publicClient = new JSONAddClientInfoDataverseAPI.Publicclient(appid);
                    var end = await gs.Applications[appObjectId].AddPassword(JSONAddClientInfoDataverseAPI.Publicclient.PasswordCredentials(secretDisplayName)).Request().PostAsync();

                    content.secretText = end.SecretText;

                    infiniteloop = false;
                }
                catch (Exception ex)
                {
                    //reduce the spam to azure servers and graph api
                    if (ex.Message.StartsWith("Code: Request_ResourceNotFound"))
                        Console.Write(Environment.NewLine + "Infinite loop is off, failed adding password to App Registration Id: " + appObjectId);
                    else
                        Console.Write(Environment.NewLine + ex.Message);
                }
            }
            return content.secretText;
        }

        public static async Task UpdateRedirectUrlsAsync(GraphServiceClient gs, string appObjectId, string appId, bool infiniteloop = true)
        {
            JSONAddClientInfoDataverseAPI dataneededaftercreation = new();

            if (infiniteloop)
            {
                while (infiniteloop)
                {
                    try
                    {
                        dataneededaftercreation.publicClient = new JSONAddClientInfoDataverseAPI.Publicclient(appId);
                        var end = await gs.Applications[appObjectId].Request().UpdateAsync(new Microsoft.Graph.Application()
                        {
                            PublicClient = new PublicClientApplication()
                            {
                                RedirectUris = dataneededaftercreation.publicClient.redirectUris
                            }
                        });

                        infiniteloop = false;
                    }
                    catch (Exception ex)
                    {
                        //reduce the spam to azure servers and graph api
                        await Task.Delay(5000);
                        if (ex.Message.StartsWith("Code: Request_ResourceNotFound"))
                            Console.Write(Environment.NewLine + "Waiting for valid objectid: " + appObjectId);
                        else
                            Console.Write(Environment.NewLine + ex.Message);
                    }
                }
            }
            else
            {
                try
                {
                    dataneededaftercreation.publicClient = new JSONAddClientInfoDataverseAPI.Publicclient(appObjectId);
                    var end = await gs.Applications[appObjectId].Request().UpdateAsync(new Microsoft.Graph.Application()
                    {
                        PublicClient = new PublicClientApplication()
                        {
                            RedirectUris = dataneededaftercreation.publicClient.redirectUris
                        }
                    });

                    infiniteloop = false;
                }
                catch (Exception ex)
                {
                    //reduce the spam to azure servers and graph api
                    if (ex.Message.StartsWith("Code: Request_ResourceNotFound"))
                        Console.Write(Environment.NewLine + "Waiting for app resource to finish adding data for: " + appObjectId);
                    else
                        Console.Write(Environment.NewLine + ex.Message);
                }
            }
        }

        public static async Task<Microsoft.Graph.Application> CreateAzureAPIAsync(GraphServiceClient gs, string displayName)
        {
            JSONAutoCreateDataverseAPI autoCreateDataverseAPI = new(displayName);
            var app = await gs.Applications.Request().AddAsync(new Microsoft.Graph.Application()
            {
                DisplayName = autoCreateDataverseAPI.displayName,
                SignInAudience = autoCreateDataverseAPI.signInaudience,
                Tags = autoCreateDataverseAPI.tags,
                PublicClient = new PublicClientApplication() { RedirectUris = autoCreateDataverseAPI.publicClient.redirectUris }, // autoCreateDataverseAPI.publicClient.,
                RequiredResourceAccess = autoCreateDataverseAPI.requiredResourceAccess,
                //Web = autoCreateDataverseAPI.web,

            });
            return app;
        }

        #region Binded JSONS
        //haven't made dynamic yet
        class JSONAutoCreateDataverseAPI
        {
            public string? displayName { get; set; }
            public string signInaudience = signInAudience;
            public string[] tags = new string[] { noSDKSupport };
            public Publicclient? publicClient { get; set; }
            public Requiredresourceaccess[]? requiredResourceAccess { get; set; }
            //public Web web { get; set; }

            public JSONAutoCreateDataverseAPI(string displayName)
            {
                this.displayName = displayName;

                publicClient = new Publicclient();

                //web = new Web();
                //web.implicitGrantSettings = new Implicitgrantsettings();
                //web.implicitGrantSettings.enableAccessTokenIssuance = false;
                //web.implicitGrantSettings.enableIdTokenIssuance = false;
                //web.redirectUris = new string[] { APIRedirectUrls.localhost };

                requiredResourceAccess = new Requiredresourceaccess[2];

                //type is the same for all at this time
                string type = "Scope";

                //Microsoft Graph
                requiredResourceAccess[0] = new()
                {
                    resourceAppId = graphAppId,
                    resourceAccess = new Resourceaccess[2]
                    {
                        new()
                        {
                            id = graphUserReadId,
                            type = type
                        },
                        new()
                        {
                            id = graphMailSendId,
                            type = type
                        }
                    }
                };

                //Dynamics CRM
                requiredResourceAccess[1] = new()
                {
                    resourceAppId = dynamicsAppId,
                    resourceAccess = new Resourceaccess[1]
                    {
                        new()
                        {
                            id = dynamicsUserImpersonationId,
                            type = type
                        }
                    }
                };
            }

            public class Publicclient
            {
                public string[] redirectUris = new string[] { Globals.NativeLoginAuth };
            }

            public class Requiredresourceaccess : RequiredResourceAccess
            {
                public string? resourceAppId { get; set; }
                public Resourceaccess[]? resourceAccess { get; set; }
            }

            public class Resourceaccess
            {
                public string? id { get; set; }
                public string? type { get; set; }
            }

            public class Web : WebApplication
            {
                public string[]? redirectUris { get; set; }
                public Implicitgrantsettings? implicitGrantSettings { get; set; }
            }

            public class Implicitgrantsettings
            {
                public bool enableAccessTokenIssuance { get; set; }
                public bool enableIdTokenIssuance { get; set; }
            }
        }
        class JSONAddClientInfoDataverseAPI
        {
            public Publicclient? publicClient { get; set; }

            public class Publicclient
            {
                public Publicclient(string appid)
                {
                    (string localhost1, string localhost2) = Globals.LocalHostLoginAuth("65135");
                    redirectUris = new string[] { Globals.WebBrokerMSAppxWebLoginAuth(appid), Globals.NativeLoginAuth, localhost1, localhost2 };
                }
                public string[] redirectUris;

                public static PasswordCredential PasswordCredentials(string displayName)
                {
                    return new PasswordCredential()
                    {
                        DisplayName = displayName
                    };
                }
            }
        }
        class JSONGetClientSecretResponseDataverseAPI
        {
            public string? secretText { get; set; }
        }
        //for reference of all possibilities
        /*class JSONGetClientSecretResponseDataverseAPI
        {
            public string? customKeyIdentifier { get; set; }
            public DateTime endDateTime { get; set; }
            public string? keyId { get; set; }
            public DateTime startDateTime { get; set; }
            public string? secretText { get; set; }
            public string? hint { get; set; }
            public string? displayName { get; set; }
        }*/
        #endregion
    }
}
