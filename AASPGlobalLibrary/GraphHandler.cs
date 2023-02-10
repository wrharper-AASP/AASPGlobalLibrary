using Microsoft.Graph;
using System.Text.Json;

//Highly subject to change
//This has the primary handler for using Microsoft Graph API
//Not widely used yet, just handles Teams Channels
namespace AASPGlobalLibrary
{
    public class GraphHandler
    {
        //uses a provider to bypass needing an API
        public static GraphServiceClient GetServiceClientWithoutAPI()
        {
            DelegateAuthenticationProvider authProvider = new(authenticateRequestAsyncDelegate: async (request) =>
            {
                // Use Microsoft.Identity.Client to retrieve token
                //var result = auth;
                request.Headers.Authorization = HttpClientHandler.SetAuthorizationBearerHeader(await TokenHandler.GetDefaultGraphToken());
            });

            return new GraphServiceClient(authProvider);
        }

        #region Teams Channels GET & POST
        public static async Task<string> GETTeamsMessagesFromChannel(string token, string teamsId, string channelId)
        {
            var jsonstring = await HttpClientHandler.GetJsonStringAsync(token, Globals.GraphBase(), "teams/" + teamsId + "/channels/" + channelId + "/messages");
            return jsonstring;
        }
        public static async Task<string> GETTeamsChannelIdByName(string token, string teamsId, string name)
        {
            var jsonstring = await HttpClientHandler.GetJsonStringAsync(token, Globals.GraphBase(), "teams/" + teamsId + "/channels");
            try
            {
                var channels = JsonSerializer.Deserialize<JSONGetTeamsChannels>(jsonstring);
                for (int i = 0; i < channels.value.Length; i++)
                {
                    if (channels.value[i].displayName == name)
                        return channels.value[i].id;
                }
            }
            catch
            {
                var error = JsonSerializer.Deserialize<Globals.JSONRestErrorHandler>(jsonstring);
                return error.error.code + ": " + error.error.message;
            }
            return "";
        }
        public static async Task<string> POSTTeamsMessageToChannel(string token, string teamsId, string channelId, string message)
        {
            JSONSendTeamsMessageToChannel json = new() { body = new() { content = message } };
            string jsonstring = JsonSerializer.Serialize(json);
            return await HttpClientHandler.PostJsonStringBearerAsync(token, Globals.GraphBase(), "teams/" + teamsId + "/channels/" + channelId + "/messages", jsonstring);
        }
        #endregion

        #region Binded JSONS
        class JSONSendTeamsMessageToChannel
        {
            public Body? body { get; set; }

            public class Body
            {
                public string? content { get; set; }
            }
        }
        class JSONGetTeamsMessagesFromChannel
        {
            public string? odatacontext { get; set; }
            public int? odatacount { get; set; }
            public string? odatanextLink { get; set; }
            public Value[]? value { get; set; }

            public class Value
            {
                public string? id { get; set; }
                public string? replyToId { get; set; }
                public string? etag { get; set; }
                public string? messageType { get; set; }
                public DateTime createdDateTime { get; set; }
                public DateTime lastModifiedDateTime { get; set; }
                public string? lastEditedDateTime { get; set; }
                public string? deletedDateTime { get; set; }
                public string? subject { get; set; }
                public string? summary { get; set; }
                public string? chatId { get; set; }
                public string? importance { get; set; }
                public string? locale { get; set; }
                public string? webUrl { get; set; }
                public string? policyViolation { get; set; }
                public string? eventDetail { get; set; }
                public From? from { get; set; }
                public Body? body { get; set; }
                public Channelidentity? channelIdentity { get; set; }
                public string[]? attachments { get; set; }
                public string[]? mentions { get; set; }
                public string[]? reactions { get; set; }
            }

            public class From
            {
                public string? application { get; set; }
                public string? device { get; set; }
                public User? user { get; set; }
            }

            public class User
            {
                public string? id { get; set; }
                public string? displayName { get; set; }
                public string? userIdentityType { get; set; }
                public string? tenantId { get; set; }
            }

            public class Body
            {
                public string? contentType { get; set; }
                public string? content { get; set; }
            }

            public class Channelidentity
            {
                public string? teamId { get; set; }
                public string? channelId { get; set; }
            }

        }
        class JSONGetTeamsChannels
        {
            public string? odatacontext { get; set; }
            public int? odatacount { get; set; }
            public Value[]? value { get; set; }

            public class Value
            {
                public string? id { get; set; }
                public DateTime createdDateTime { get; set; }
                public string? displayName { get; set; }
                public string? description { get; set; }
                public string? isFavoriteByDefault { get; set; }
                public string? email { get; set; }
                public string? tenantId { get; set; }
                public string? webUrl { get; set; }
                public string? membershipType { get; set; }
            }

        }
        #endregion
    }
}