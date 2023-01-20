using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Serialization;

//Highly subject to change since this is extensions across all main objects
//Currently:
//Fixes System.Text.Json dynamic issues
//Extends HttpClient to handle downloading files easier
namespace AASPWaynesLibrary
{
    public static class HttpClientUtils
    {
        public static async Task DownloadFileAsync(this HttpClient client, string url, string FileName)
        {
            using var s = await client.GetStreamAsync(new Uri(url));
            using var fs = new FileStream(FileName, FileMode.CreateNew);
            await s.CopyToAsync(fs);
        }
        public static async Task DownloadFileAsync(string url, string FileName)
        {
            using var client = new HttpClient();
            using var s = await client.GetStreamAsync(new Uri(url));
            using var fs = new FileStream(FileName, FileMode.CreateNew);
            await s.CopyToAsync(fs);
        }
    }
#pragma warning disable CS8619
#pragma warning disable CS8601
    public class DynamicJsonConverter : JsonConverter<dynamic>
    {
        public override dynamic Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.True)
            {
                return true;
            }
            if (reader.TokenType == JsonTokenType.False)
            {
                return false;
            }
            if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetInt64(out long l))
                {
                    return l;
                }
                return reader.GetDouble();
            }
            if (reader.TokenType == JsonTokenType.String)
            {
                if (reader.TryGetDateTime(out DateTime datetime))
                {
                    return datetime;
                }
                return reader.GetString();
            }
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                using JsonDocument documentV = JsonDocument.ParseValue(ref reader);
                return ReadObject(documentV.RootElement);
            }
            // Use JsonElement as fallback.
            // Newtonsoft uses JArray or JObject.
            JsonDocument document = JsonDocument.ParseValue(ref reader);
            return document.RootElement.Clone();
        }
        private object ReadObject(JsonElement jsonElement)
        {
            IDictionary<string, object> expandoObject = new ExpandoObject();
            foreach (var obj in jsonElement.EnumerateObject())
            {
                var k = obj.Name;
                var value = ReadValue(obj.Value);
                expandoObject[k] = value;
            }
            return expandoObject;
        }
        private object? ReadValue(JsonElement jsonElement)
        {
            object? result;
            switch (jsonElement.ValueKind)
            {
                case JsonValueKind.Object:
                    result = ReadObject(jsonElement);
                    break;
                case JsonValueKind.Array:
                    result = ReadList(jsonElement);
                    break;
                case JsonValueKind.String:
                    //TODO: Missing Datetime&Bytes Convert
                    result = jsonElement.GetString();
                    break;
                case JsonValueKind.Number:
                    //TODO: more num type
                    result = 0;
                    if (jsonElement.TryGetInt64(out long l))
                    {
                        result = l;
                    }
                    break;
                case JsonValueKind.True:
                    result = true;
                    break;
                case JsonValueKind.False:
                    result = false;
                    break;
                case JsonValueKind.Undefined:
                case JsonValueKind.Null:
                    result = null;
                    break;
                default:
                    throw new();
            }
            return result;
        }
        private object? ReadList(JsonElement jsonElement)
        {
            IList<object?> list = new List<object?>();
            foreach (var item in jsonElement.EnumerateArray())
            {
                list.Add(ReadValue(item));
            }
            return list.Count == 0 ? null : list;
        }
        public override void Write(Utf8JsonWriter writer,
            object value,
            JsonSerializerOptions options)
        {
            // writer.WriteStringValue(value.ToString());
        }
    }
#pragma warning restore CS8619
#pragma warning restore CS8601
}
