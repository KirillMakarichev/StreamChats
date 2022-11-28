using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace StreamChats.DonationAlerts;

internal class ConnectionInfoConverter : JsonConverter<ConnectionInfo>
{
    public override void WriteJson(JsonWriter writer, ConnectionInfo? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override ConnectionInfo? ReadJson(JsonReader reader, Type objectType, ConnectionInfo? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        var jObject = JObject.Load(reader)["data"];

        var connectionInfo = new ConnectionInfo()
        {
            UserId = (long)jObject["id"],
            ConnectionToken = (string)jObject["socket_connection_token"]
        };

        return connectionInfo;
    }
}