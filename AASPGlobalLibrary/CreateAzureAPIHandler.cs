using Microsoft.Graph;

//Used to automatically create Azure API's.
//A heavily needed process to allow for a lot more automation in the future
//For those who need app registration
namespace AASPGlobalLibrary
{
    class APIPermissionIds
    {
        //A_ means application consent, D_ means delegated consent

        public const string GraphAppId = "00000003-0000-0000-c000-000000000000";
        //Allows users to sign-in to the app, and allows the app to read the profile of signed-in users.
        //It also allows the app to read basic company information of signed-in users.
        public const string D_GraphUserReadId = "e1fe6dd8-ba31-4d61-89e7-88639da4683d";
        //Allows the app to send mail as users in the organization.
        public const string D_GraphMailSendId = "e383f46e-2787-4529-855e-0e479a3ffac0";
        //Allows the app to send mail as any user without a signed-in user.
        public const string A_GraphMailSendId = "b633e1c5-b582-4048-a93e-9f11b44c7e96";
        //Allows the app to send mail as the signed-in user, including sending on-behalf of others.
        public const string D_GraphMailSharedSendId = "a367ab51-6b49-43bf-a716-a1fb06d2a174";

        public const string DynamicsAppId = "00000007-0000-0000-c000-000000000000";
        //Allows access to Dynamics 365 APIs as the signed-in user.
        public const string D_DynamicsUserImpersonationId = "78ce3f0f-a1ce-49c2-8cde-64b5c0896db4";

        public const string CosmosAppId = "a232010e-820c-4083-83bb-3ace5fc29d0b";
        //Allows delegated access to the Cosmos DB
        public const string D_CosmosUserImpersonationId = "8741c20d-e8c0-41ff-8adf-b7b9ba168197";
    }
    //CRITICAL ERRORS WILL OCCUR: web redirect has been disabled. it was creating connection errors to dataverse.
    public class CreateAzureAPIHandler
    {
        const string signInAudience = "AzureADandPersonalMicrosoftAccount";
        const string noSDKSupport = "NoLiveSdkSupport";

        public static async Task<string> AddSecretClientPasswordAsync(GraphServiceClient gs, string appObjectId, string secretDisplayName, bool infiniteloop = true)
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
                        PasswordCredential credential = new()
                        {
                            DisplayName = secretDisplayName
                        };
                        var end = await gs.Applications[appObjectId].AddPassword(credential).Request().PostAsync();

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
                            Console.Write(Environment.NewLine + "Waiting for app resource to add secret for object id: " + appObjectId);
                        else
                            Console.Write(Environment.NewLine + ex.Message);
                    }
                }
            }
            else
            {
                try
                {
                    PasswordCredential credential = new()
                    {
                        DisplayName = secretDisplayName
                    };
                    var end = await gs.Applications[appObjectId].AddPassword(credential).Request().PostAsync();

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

        public static async Task<Microsoft.Graph.Application> CreateAzureAPIAsync(GraphServiceClient gs, string displayName, bool iscosmos = false)
        {
            JSONAutoCreateDataverseAPI autoCreateDataverseAPI = new(displayName, iscosmos);
            var app = await gs.Applications.Request().AddAsync(new Microsoft.Graph.Application()
            {
                DisplayName = autoCreateDataverseAPI.displayName,
                SignInAudience = autoCreateDataverseAPI.signInaudience,
                Tags = autoCreateDataverseAPI.tags,
                PublicClient = new PublicClientApplication() { RedirectUris = autoCreateDataverseAPI.publicClient.redirectUris }, // autoCreateDataverseAPI.publicClient.,
                RequiredResourceAccess = autoCreateDataverseAPI.requiredResourceAccess
                //Web = autoCreateDataverseAPI.web,

            });
            await gs.ServicePrincipals.Request().AddAsync(new()
            {
                AppId = app.AppId,
                DisplayName = app.DisplayName,
                Tags = app.Tags,
                ServicePrincipalType = "Application",
            });
            await UpdateRedirectUrlsAsync(gs, app.Id, app.AppId);
            return app;
        }

        public static async Task<Microsoft.Graph.Application> CreateAzureAPIAsync(string displayName, bool iscosmos = false)
        {
            GraphServiceClient gs = GraphHandler.GetServiceClientWithoutAPI();
            JSONAutoCreateDataverseAPI autoCreateDataverseAPI = new(displayName, iscosmos);
            var app = await gs.Applications.Request().AddAsync(new Microsoft.Graph.Application()
            {
                DisplayName = autoCreateDataverseAPI.displayName,
                SignInAudience = autoCreateDataverseAPI.signInaudience,
                Tags = autoCreateDataverseAPI.tags,
                PublicClient = new PublicClientApplication() { RedirectUris = autoCreateDataverseAPI.publicClient.redirectUris }, // autoCreateDataverseAPI.publicClient.,
                RequiredResourceAccess = autoCreateDataverseAPI.requiredResourceAccess,
                //Web = autoCreateDataverseAPI.web,

            });
            await gs.ServicePrincipals.Request().AddAsync(new()
            {
                AppId = app.AppId,
                DisplayName = app.DisplayName,
                Tags = app.Tags,
                ServicePrincipalType = "Application",
            });
            await UpdateRedirectUrlsAsync(gs, app.Id, app.AppId);
            return app;
        }

        #region Binded JSONS
        //haven't made dynamic yet
        class JSONOAuth2PermissionGrants
        {
            public string? clientid { get; set; }
            public string? consentType { get; set; }
            //leave null if consent type = AllPrincipals instead of Principal
            public string? principalId { get; set; }
            public string? resourceId { get; set; }
            public string? scope { get; set; }
        }
        class JSONAutoCreateDataverseAPI
        {
            public string? displayName { get; set; }
            public string signInaudience = signInAudience;
            public string[] tags = new string[] { noSDKSupport };
            public Publicclient? publicClient { get; set; }
            public Requiredresourceaccess[]? requiredResourceAccess { get; set; }
            //public Web web { get; set; }

            public JSONAutoCreateDataverseAPI(string displayName, bool isCosmos)
            {
                this.displayName = displayName;

                publicClient = new Publicclient();

                //web = new Web();
                //web.implicitGrantSettings = new Implicitgrantsettings();
                //web.implicitGrantSettings.enableAccessTokenIssuance = false;
                //web.implicitGrantSettings.enableIdTokenIssuance = false;
                //web.redirectUris = new string[] { APIRedirectUrls.localhost };

                //type is the same for all at this time
                string type = "Scope";

                requiredResourceAccess = new Requiredresourceaccess[2];

                //Microsoft Graph
                requiredResourceAccess[0] = new()
                {
                    resourceAppId = APIPermissionIds.GraphAppId,
                    resourceAccess = new Resourceaccess[3]
                    {
                        new()
                        {
                            id = APIPermissionIds.D_GraphUserReadId,
                            type = type
                        },
                        new()
                        {
                            id = APIPermissionIds.D_GraphMailSendId,
                            type = type
                        },
                        new()
                        {
                            id = APIPermissionIds.A_GraphMailSendId,
                            type = "Role"
                        }
                    }
                };

                if (!isCosmos)
                {
                    //Dynamics CRM
                    requiredResourceAccess[1] = new()
                    {
                        resourceAppId = APIPermissionIds.DynamicsAppId,
                        resourceAccess = new Resourceaccess[1]
                        {
                            new()
                            {
                                id = APIPermissionIds.D_DynamicsUserImpersonationId,
                                type = type
                            }
                        }
                    };
                }
                else
                {
                    //Dynamics CRM
                    requiredResourceAccess[1] = new()
                    {
                        resourceAppId = APIPermissionIds.CosmosAppId,
                        resourceAccess = new Resourceaccess[1]
                        {
                            new()
                            {
                                id = APIPermissionIds.D_CosmosUserImpersonationId,
                                type = type
                            }
                        }
                    };
                }
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
                    string localhost1 = Globals.LocalHostLoginAuth(true, "65135");
                    string localhost2 = Globals.LocalHostLoginAuth(true);
                    string localhost3 = Globals.LocalHostLoginAuth(false, "65135");
                    string localhost4 = Globals.LocalHostLoginAuth(false);
                    redirectUris = new string[] { Globals.WebBrokerMSAppxWebLoginAuth(appid), Globals.NativeLoginAuth, localhost1, localhost2, localhost3, localhost4 };
                }
                public string[] redirectUris;
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
