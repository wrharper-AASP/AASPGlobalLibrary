using Azure.Core;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using System.Net;
using System.Numerics;
using Azure.Identity;

//If containers are not locally defined this can get expensive both on cost and performance for the app.
//It is recommended to locally define a container
//Make sure to call Init or InitAsync first to select a json file for DbInfo
//Can be initialized with or without async
namespace AASPGlobalLibrary
{
#pragma warning disable CS8604
    public class CosmosDBHandler
    {
        CosmosClient? cosmosClient;

        public JSONInternalDbInfo? DbInfo { get; set; }
        PartitionKey smsParitionKey = new();
        PartitionKey whatsappParitionKey = new();
        PartitionKey accountsParitionKey = new();
        PartitionKey countersParitionKey = new();
        public void SetParitionKeys()
        {
            smsParitionKey = new PartitionKey(DbInfo.smsIDName);
            whatsappParitionKey = new PartitionKey(DbInfo.whatsappIDName);
            accountsParitionKey = new PartitionKey(DbInfo.accountsIDName);
            countersParitionKey = new PartitionKey(DbInfo.countersIDName);
        }
        //create layered messages? not possible in dataverse or many other SQL servers but it is in cosmosdb??
        //example: if from is same, add messages to same from group or to group
        //idea concept
        //public class From
        //{
        //public string[]? Messages { get; set; }
        //}

        public void Init()
        {
            DbInfo = System.Text.Json.JsonSerializer.Deserialize<JSONInternalDbInfo>(Globals.OpenJSONFile());
        }
        public async Task InitAsync()
        {
            DbInfo = System.Text.Json.JsonSerializer.Deserialize<JSONInternalDbInfo>(await Globals.OpenJSONFileAsync());
        }

        #region Cosmos Client Handling
        //keys rotate up to a few seconds to a few days, unreliable connection
        public async Task CreateCosmosClient(string vaultname, string secret, string secret2)
        {
            cosmosClient = new CosmosClient("AccountEndpoint=https://" + (await VaultHandler.GetSecretInteractive(vaultname, secret)) + ".documents.azure.com:443/;AccountKey=" + (await VaultHandler.GetSecretInteractive(vaultname, secret2)) + ";");
            //cosmosClient = new CosmosClient(endpoint, primarykey);//, new CosmosClientOptions() { ApplicationName = "WaynesCosmosDBHandler" });
        }
        //rbac
        public void CreateCosmosClient(string? endpoint, TokenCredential token)
        {
            cosmosClient = new CosmosClient(endpoint, token);//, new CosmosClientOptions() { ApplicationName = "WaynesCosmosDBHandler" });
        }
        //for using the library remotely with admin permissions
        public void CreateCosmosClientInteractive(TokenCredential tokenC, string? endpoint)
        {
            CreateCosmosClient(endpoint, tokenC);
        }
        public void CreateCosmosClientManaged(TokenCredential tokenC, string? endpoint)
        {
            CreateCosmosClient(endpoint, tokenC);
        }
        public void CreateCosmosClientInteractive(string? endpoint)
        {
            TokenHandler.tokenCredential ??= new InteractiveBrowserCredential();
            CreateCosmosClient(endpoint, TokenHandler.tokenCredential);
        }
        public void CreateCosmosClientManaged(string? endpoint)
        {
            TokenHandler.managedTokenCredential ??= new ManagedIdentityCredential();
            CreateCosmosClient(endpoint, TokenHandler.managedTokenCredential);
        }
        #endregion

        #region Creates or Gets the database, containers, and items
        async Task<Database> CreateOrGetDatabaseAsync(string databaseName)
        {
            return await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName);
        }

        static async Task<Container> CreateOrGetContainerAsync(Database database, string containerName, string? partitionkeypath)
        {
            return await database.CreateContainerIfNotExistsAsync(containerName, partitionkeypath);
        }
        async Task<Container> CreateOrGetContainerAsync(string partitionkeypath, string dbname, string containername)
        {
            return await CreateOrGetContainerAsync(await CreateOrGetDatabaseAsync(dbname), containername, partitionkeypath);
        }
        async Task<Container> CreateOrGetContainerAsync(string? partitionkeypath, string containername)
        {
            return await CreateOrGetContainerAsync(await CreateOrGetDatabaseAsync("SMSAndWhatsApp"), containername, partitionkeypath);
        }

        static async Task CreateOrGetItemInContainerAsync<T>(T jsonT, Container container)
        {
            dynamic json = jsonT;
            try
            {
                _ = (await container.ReadItemAsync<T>(json.Id, new PartitionKey(json.PartitionKey)));
                Console.Write(Environment.NewLine + "Item already exists in database");
                // Read the item to see if it exists.  
                //return itemResponse;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _ = await container.CreateItemAsync(json, new PartitionKey(json.PartitionKey));
                Console.Write(Environment.NewLine + "Created item in database");
                // Create an item in the container representing the Andersen family. Note we provide the value of the partition key for this item, which is "Andersen"
                //return itemResponse;

                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
            }
        }
        async Task CreateOrGetItemInContainerAsync<T>(T jsonT, string containername, string? originalkey)
        {
            dynamic json = jsonT;

            var container = await CreateOrGetContainerAsync(originalkey, containername);

            try
            {
                _ = (await container.ReadItemAsync<T>(json.Id, new PartitionKey(json.PartitionKey)));
                Console.Write(Environment.NewLine + "Item already exists in database");
                // Read the item to see if it exists.  
                //return itemResponse;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _ = await container.CreateItemAsync(json, new PartitionKey(json.PartitionKey));
                Console.Write(Environment.NewLine + "Created item in database");
                // Create an item in the container representing the Andersen family. Note we provide the value of the partition key for this item, which is "Andersen"
                //return itemResponse;

                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
            }
        }
        #endregion

        //Many options created to get data without needing to manually do queries
        //Manual query is an option as well, just in case.
        #region Query Handling
        //examples
        //select: "c.itemnamehere like id"
        //from: "Containername c" or just use "c"
        //wherequery: "c.PartitionKey = 'partitionkeytolookup'"
        static async Task<List<T>> QueryItemsAsync<T>(Container container, string fromcontainer, string wherequery, string select = "*")
        {
            var sqlQueryText = "SELECT " + select + " FROM " + fromcontainer + " WHERE " + wherequery;

            Console.Write(Environment.NewLine + "Running query: " + sqlQueryText);

            QueryDefinition queryDefinition = new(sqlQueryText);
            FeedIterator<T> queryResultSetIterator = container.GetItemQueryIterator<T>(queryDefinition);

            List<T> typelist = new();

            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<T> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (T type in currentResultSet)
                {
                    typelist.Add(type);
                    Console.Write(Environment.NewLine + "Read " + type);
                }
            }
            return typelist;
        }
        static async Task<List<T>> QueryItemsAsync<T>(Container container, string wherequery)
        {
            var sqlQueryText = "SELECT * FROM c WHERE " + wherequery;

            //Console.Write(Environment.NewLine + "Running query: " + sqlQueryText);

            QueryDefinition queryDefinition = new(sqlQueryText);
            FeedIterator<T> queryResultSetIterator = container.GetItemQueryIterator<T>(queryDefinition);

            List<T> typelist = new();

            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<T> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (T type in currentResultSet)
                {
                    typelist.Add(type);
                    Console.Write(Environment.NewLine + "Read " + type);
                }
            }
            return typelist;
        }
        static async Task<List<T>> QueryItemsAsync<T>(Container container, string wherequery, string select)
        {
            var sqlQueryText = "SELECT " + select + " FROM c WHERE " + wherequery;

            Console.Write(Environment.NewLine + "Running query: " + sqlQueryText);

            QueryDefinition queryDefinition = new(sqlQueryText);
            FeedIterator<T> queryResultSetIterator = container.GetItemQueryIterator<T>(queryDefinition);

            List<T> typelist = new();

            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<T> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (T type in currentResultSet)
                {
                    typelist.Add(type);
                    Console.Write(Environment.NewLine + "Read " + type);
                }
            }
            return typelist;
        }

        async Task<List<T>> QueryItemsAsync<T>(string partition, string containername, string fromcontainer, string wherequery, string select = "*")
        {
            var container = await CreateOrGetContainerAsync(partition, containername);
            var sqlQueryText = "SELECT " + select + " FROM " + fromcontainer + " WHERE " + wherequery;

            Console.Write(Environment.NewLine + "Running query: " + sqlQueryText);

            QueryDefinition queryDefinition = new(sqlQueryText);
            FeedIterator<T> queryResultSetIterator = container.GetItemQueryIterator<T>(queryDefinition);

            List<T> typelist = new();

            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<T> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (T type in currentResultSet)
                {
                    typelist.Add(type);
                    Console.Write(Environment.NewLine + "Read " + type);
                }
            }
            return typelist;
        }
        async Task<List<T>> QueryItemsAsync<T>(string partition, string containername, string wherequery)
        {
            var container = await CreateOrGetContainerAsync(partition, containername);
            var sqlQueryText = "SELECT * FROM c WHERE " + wherequery;

            Console.Write(Environment.NewLine + "Running query: " + sqlQueryText);

            QueryDefinition queryDefinition = new(sqlQueryText);
            FeedIterator<T> queryResultSetIterator = container.GetItemQueryIterator<T>(queryDefinition);

            List<T> typelist = new();

            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<T> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (T type in currentResultSet)
                {
                    typelist.Add(type);
                    Console.Write(Environment.NewLine + "Found: " + Environment.NewLine + type);
                }
            }
            return typelist;
        }
        async Task<List<T>> QueryItemsAsync<T>(string partition, string containername, string wherequery, string select)
        {
            var container = await CreateOrGetContainerAsync(partition, containername);
            var sqlQueryText = "SELECT " + select + " FROM c WHERE " + wherequery;

            Console.Write(Environment.NewLine + "Running query: " + sqlQueryText);

            QueryDefinition queryDefinition = new(sqlQueryText);
            FeedIterator<T> queryResultSetIterator = container.GetItemQueryIterator<T>(queryDefinition);

            List<T> typelist = new();

            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<T> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (T type in currentResultSet)
                {
                    typelist.Add(type);
                    Console.Write(Environment.NewLine + "Read " + type);
                }
            }
            return typelist;
        }

        static async Task<List<JSONAccountsInfo>> QueryAccountItems(Container container, string assignedTo)
        {
            List<JSONAccountsInfo> typelist = await QueryItemsAsync<JSONAccountsInfo>(container, "c.AssignedTo = '" + assignedTo + "'");
            return typelist;
        }
        async Task<List<JSONAccountsInfo>> QueryAccountItems(string containername, string assignedTo)
        {
            var container = await CreateOrGetContainerAsync(DbInfo.accountsIDName, containername);
            List<JSONAccountsInfo> typelist = await QueryItemsAsync<JSONAccountsInfo>(container, "c.AssignedTo = '" + assignedTo + "'");
            return typelist;
        }
        public static async Task<List<dynamic>> QuerySMSOrWhatsAppItemsAsync(Container container, string assigneduser)
        {
            var sqlQueryText = "SELECT c.AssignedUser,c.To,c.Fromm,c.Message,c.PicturePath,c.Timestamp FROM c WHERE c.AssignedUser = \"" + assigneduser + "\"";
            sqlQueryText = sqlQueryText.Replace("\\", "");

            //log.LogInformation(Environment.NewLine + "Running query: " + sqlQueryText);

            QueryDefinition queryDefinition = new(sqlQueryText);
            FeedIterator<dynamic> queryResultSetIterator = container.GetItemQueryIterator<dynamic>(queryDefinition);

            List<dynamic> typelist = new();

            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<dynamic> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (dynamic type in currentResultSet)
                {
                    typelist.Add(type);
                    string s = type.ToString();
                    Console.Write(Environment.NewLine + "Read " + s);
                }
            }
            return typelist;
        }
        #endregion

        //For Example: When changing an accounts phone number, you will want to update that phone number across all container items.
        //Can be expensive as the database grows.
        #region Update Items
        public async Task UpdateSMSItem(Container container, string idname)
        {
            ItemResponse<JSONSMSInfo> itemResponse = await container.ReadItemAsync<JSONSMSInfo>(idname, smsParitionKey);
            JSONSMSInfo itemBody = itemResponse.Resource;

            // replace the item with the updated content
            _ = await container.ReplaceItemAsync(itemBody, itemBody.Id, smsParitionKey);
            Console.Write(Environment.NewLine + "Body is now: " + itemResponse.Resource);
            //return itemResponse;
        }
        public async Task UpdateWhatsAppItem(Container container, string idname)
        {
            ItemResponse<JSONWhatsAppInfo> itemResponse = await container.ReadItemAsync<JSONWhatsAppInfo>(idname, whatsappParitionKey);
            dynamic itemBody = itemResponse.Resource;

            // replace the item with the updated content
            _ = await container.ReplaceItemAsync<JSONWhatsAppInfo>(itemBody, itemBody.Id, whatsappParitionKey);
            Console.Write(Environment.NewLine + "Body is now: " + itemResponse.Resource);
            //return itemResponse;
        }

        public async Task UpdateSMSItem(string idname)
        {
            var container = await CreateOrGetContainerAsync(DbInfo.smsIDName, DbInfo.smsContainerName);
            ItemResponse<JSONSMSInfo> itemResponse = await container.ReadItemAsync<JSONSMSInfo>(idname, smsParitionKey);
            JSONSMSInfo itemBody = itemResponse.Resource;

            // replace the item with the updated content
            _ = await container.ReplaceItemAsync(itemBody, itemBody.Id, smsParitionKey);
            Console.Write(Environment.NewLine + "Body is now: " + itemResponse.Resource);
            //return itemResponse;
        }
        public async Task UpdateWhatsAppItem(string idname)
        {
            var container = await CreateOrGetContainerAsync(DbInfo.whatsappIDName, "WhatsApp");
            ItemResponse<JSONWhatsAppInfo> itemResponse = await container.ReadItemAsync<JSONWhatsAppInfo>(idname, whatsappParitionKey);
            dynamic itemBody = itemResponse.Resource;

            // replace the item with the updated content
            _ = await container.ReplaceItemAsync<JSONWhatsAppInfo>(itemBody, itemBody.Id, whatsappParitionKey);
            Console.Write(Environment.NewLine + "Body is now: " + itemResponse.Resource);
            //return itemResponse;
        }
        #endregion

        //Intended to delete inactive users/accounts only
        #region Delete Item
        public static async Task<ItemResponse<T>> DeleteItemAsync<T>(Container container, PartitionKey partition, string idname)
        {
            ItemResponse<T> itemResponse = await container.DeleteItemAsync<T>(idname, partition);
            Console.Write(Environment.NewLine + "Deleted [" + partition.ToString()[1..] + "," + idname + "]");
            return itemResponse;
        }
        public async Task<ItemResponse<T>> DeleteItemAsync<T>(string containername, PartitionKey partition, string idname)
        {
            var container = await CreateOrGetContainerAsync(partition.ToString(), containername);
            ItemResponse<T> itemResponse = await container.DeleteItemAsync<T>(idname, partition);
            Console.Write(Environment.NewLine + "Deleted [" + partition.ToString()[1..] + "," + idname + "]");
            return itemResponse;
        }
        #endregion

        //handles light encrypted ids and allows max parsing of integer max value * 2 by the power of 100
        #region Unique Counter Handling
        async Task<string?> CreateOrGetCurrentIdLightEncryptedBase64InternalAutoIncrement(string CounterId)
        {
            var container = await CreateOrGetContainerAsync(DbInfo.countersIDName, DbInfo.countersContainerName);
            JSONCounters counters = new()
            {
                Id = CounterId,
                PartitionKey = CounterId
            };

            try
            {
                ItemResponse<JSONCounters>  itemResponse = await container.ReadItemAsync<JSONCounters>(counters.Id, new PartitionKey(counters.PartitionKey));
                counters.Counter = itemResponse.Resource.Counter;
            }
            catch
            {
                BigInteger big = 0;
                counters.Counter = Globals.ConvertToBase64UriSafeString(big.ToByteArray());
                _ = await container.CreateItemAsync(counters, new PartitionKey(counters.PartitionKey));
            }

            counters.Counter = Globals.IncreaseBigInt(1, counters.Counter);

            // replace the item with the updated content
            _ = await container.ReplaceItemAsync(counters, counters.Id, new PartitionKey(counters.PartitionKey));
            return counters.Counter;
        }

        async Task<string?> IncreaseSMSIdCounter()
        {
            return await CreateOrGetCurrentIdLightEncryptedBase64InternalAutoIncrement("SMSCounter");
        }
        async Task<string?> IncreaseWhatsAppIdCounter()
        {
            return await CreateOrGetCurrentIdLightEncryptedBase64InternalAutoIncrement("WhatsAppCounter");
        }
        #endregion

        //Main area where Items are added to Cosmos
        #region Cosmos Adding Items
        public async Task AddSMSItem(Container container, string AssignedUser, string From, string To, string Message, string Timestamp, string PicturesPath = "")
        {
            if (AssignedUser == "")
                Console.Write("Assigned user cannot be empty.");
            else if (From == "")
                Console.Write("Fromcannot be empty.");
            else if (To == "")
                Console.Write("To cannot be empty.");
            else if (Message == "")
                Console.Write("Message cannot be empty.");
            else if (Timestamp == "")
                Console.Write("Timestamp cannot be empty.");
            else
            {
                JSONSMSInfo jsonInfo = new()
                {
                    PartitionKey = AssignedUser.Split("@")[0],
                    AssignedUser = AssignedUser,
                    Fromm = From,
                    To = To,
                    Message = Message,
                    Timestamp = Timestamp,
                    PicturePath = PicturesPath
                };
                jsonInfo.Id = jsonInfo.PartitionKey + "." + await IncreaseSMSIdCounter();
                await CreateOrGetItemInContainerAsync(jsonInfo, container);
            }
        }
        public async Task AddWhatsAppItem(Container container, string AssignedUser, string From, string To, string Message, string Timestamp, string PicturesPath = "")
        {
            if (AssignedUser == "")
                Console.Write("Assigned user cannot be empty.");
            else if (From == "")
                Console.Write("Fromcannot be empty.");
            else if (To == "")
                Console.Write("To cannot be empty.");
            else if (Message == "")
                Console.Write("Message cannot be empty.");
            else if (Timestamp == "")
                Console.Write("Timestamp cannot be empty.");
            else
            {
                JSONWhatsAppInfo jsonInfo = new()
                {
                    PartitionKey = AssignedUser.Split("@")[0],
                    AssignedUser = AssignedUser,
                    Fromm = From,
                    To = To,
                    Message = Message,
                    Timestamp = Timestamp,
                    PicturePath = PicturesPath
                };
                jsonInfo.Id = jsonInfo.PartitionKey + "." + await IncreaseWhatsAppIdCounter();

                await CreateOrGetItemInContainerAsync(jsonInfo, container);
            }
        }
        public static async Task AddOrUpdateAccountItem(Container container, string AssignedTo, string PhoneNumber, string PhoneNumberID)
        {
            if (AssignedTo == "")
                Console.Write("Assigned user cannot be empty.");
            else if (PhoneNumber == "")
                Console.Write("Phone Number cannot be empty.");
            else if (PhoneNumberID == "")
                Console.Write("Phone Number ID cannot be empty.");
            else
            {
                JSONAccountsInfo jsonInfo = new()
                {
                    PartitionKey = AssignedTo.Split("@")[0],
                    AssignedTo = AssignedTo,
                    PhoneNumber = PhoneNumber,
                    PhoneNumberID = PhoneNumberID
                };
                jsonInfo.Id = jsonInfo.PartitionKey;// + "." + IncreaseAccountsIdCounter();
                var list = await QueryAccountItems(container, AssignedTo);
                if (list.Count > 0)
                {
                    if (list[0].PhoneNumber != PhoneNumber || list[0].PhoneNumberID != PhoneNumberID)
                    {
                        ItemResponse<JSONAccountsInfo> itemResponse = await container.ReplaceItemAsync(jsonInfo, list[0].Id, new PartitionKey(list[0].PartitionKey));
                        Console.Write(Environment.NewLine + "New Body: " + Environment.NewLine + itemResponse.Resource);
                    }
                    else
                        Console.Write(Environment.NewLine + "No need to update because the following is already the same:" +
                            Environment.NewLine + "Account Name = " + AssignedTo +
                            Environment.NewLine + "Phone Number = " + PhoneNumber +
                            Environment.NewLine + "Phone Number ID = " + PhoneNumberID);
                }
                else
                    await CreateOrGetItemInContainerAsync(jsonInfo, container);
            }
        }

        public async Task AddSMSItem(string AssignedUser, string From, string To, string Message, string Timestamp, string PicturesPath = "")
        {
            await AddSMSItem(await CreateOrGetContainerAsync(DbInfo.smsIDName, DbInfo.smsContainerName), AssignedUser, From, To, Message, Timestamp, PicturesPath);
        }
        public async Task AddWhatsAppItem(string AssignedUser, string From, string To, string Message, string Timestamp, string PicturesPath = "")
        {
            await AddWhatsAppItem(await CreateOrGetContainerAsync(DbInfo.whatsappIDName, DbInfo.whatsappContainerName), AssignedUser, From, To, Message, Timestamp, PicturesPath);
        }
        public async Task AddOrUpdateAccountItem(string AssignedTo, string PhoneNumber, string PhoneNumberID)
        {
            await AddOrUpdateAccountItem(await CreateOrGetContainerAsync(DbInfo.accountsIDName, DbInfo.accountsContainerName), AssignedTo, PhoneNumber, PhoneNumberID);
        }
        #endregion

        //not possible on serverless which is recommended since it auto scales...
        #region Cosmos Scale Handling Ideas
        public static async Task ScaleContainerAsync(Container container, int newThroughputAdded = 100)
        {
            // Read the current throughput
            try
            {
                int? throughput = await container.ReadThroughputAsync();
                if (throughput.HasValue)
                {
                    Console.Write(Environment.NewLine + "Current provisioned throughput : " + throughput.Value);
                    int newThroughput = throughput.Value + newThroughputAdded;
                    // Update throughput
                    await container.ReplaceThroughputAsync(newThroughput);
                    Console.Write(Environment.NewLine + "New provisioned throughput : " + newThroughput);
                }
            }
            catch (CosmosException cosmosException) when (cosmosException.StatusCode == HttpStatusCode.BadRequest)
            {
                Console.Write(Environment.NewLine + "Cannot read container throughput.");
                Console.Write(Environment.NewLine + cosmosException.ResponseBody);
            }
        }
        public async Task ScaleContainerAsync(string partition, string containername, int newThroughputAdded = 100)
        {
            var container = await CreateOrGetContainerAsync(partition, containername);
            // Read the current throughput
            try
            {
                int? throughput = await container.ReadThroughputAsync();
                if (throughput.HasValue)
                {
                    Console.Write(Environment.NewLine + "Current provisioned throughput : " + throughput.Value);
                    int newThroughput = throughput.Value + newThroughputAdded;
                    // Update throughput
                    await container.ReplaceThroughputAsync(newThroughput);
                    Console.Write(Environment.NewLine + "New provisioned throughput : " + newThroughput);
                }
            }
            catch (CosmosException cosmosException) when (cosmosException.StatusCode == HttpStatusCode.BadRequest)
            {
                Console.Write(Environment.NewLine + "Cannot read container throughput.");
                Console.Write(Environment.NewLine + cosmosException.ResponseBody);
            }
        }
        #endregion

        //deleting databases is not programmed on purpose, will change if the need arises

        //partitionkey is needed due to a 10gb limitation with automatic local default partitionkey
        //id format standards is good to do partitionkey + counter
        #region Binded JSONS
        public class JSONInternalDbInfo
        {
            //shorter = faster and big difference on cost due to byte size
            public string? smsIDName { get; set; }
            public string? whatsappIDName { get; set; }
            public string? accountsIDName { get; set; }
            public string? countersIDName { get; set; }

            public string? smsContainerName { get; set; }
            public string? whatsappContainerName { get; set; }
            public string? accountsContainerName { get; set; }
            public string? countersContainerName { get; set; }
        }
        class JSONSMSInfo
        {
            [JsonProperty(PropertyName = "id")]
            public string? Id { get; set; }
            [JsonProperty(PropertyName = "sid")]
            public string? PartitionKey { get; set; }
            public string? AssignedUser { get; set; }
            //public From? From { get; set; }
            public string? Fromm { get; set; }
            public string? Message { get; set; }
            public string? PicturePath { get; set; }
            public string? To { get; set; }
            public string? Timestamp { get; set; }

            public override string ToString()
            {
                return JsonConvert.SerializeObject(this);
            }
        }
        class JSONWhatsAppInfo
        {
            [JsonProperty(PropertyName = "id")]
            public string? Id { get; set; }
            [JsonProperty(PropertyName = "wid")]
            public string? PartitionKey { get; set; }
            public string? AssignedUser { get; set; }
            public string? Fromm { get; set; }
            public string? Message { get; set; }
            public string? PicturePath { get; set; }
            public string? To { get; set; }
            public string? Timestamp { get; set; }

            public override string ToString()
            {
                return JsonConvert.SerializeObject(this);
            }
        }
        class JSONAccountsInfo
        {
            [JsonProperty(PropertyName = "id")]
            public string? Id { get; set; }
            [JsonProperty(PropertyName = "aid")]
            public string? PartitionKey { get; set; }
            public string? PhoneNumber { get; set; }
            public string? AssignedTo { get; set; }
            public string? PhoneNumberID { get; set; }

            public override string ToString()
            {
                return JsonConvert.SerializeObject(this);
            }
        }
        class JSONCounters
        {
            [JsonProperty(PropertyName = "id")]
            public string? Id { get; set; }
            [JsonProperty(PropertyName = "cid")]
            public string? PartitionKey { get; set; }
            public string? Counter { get; set; }
        }
        #endregion
    }
#pragma warning restore CS8604
}
