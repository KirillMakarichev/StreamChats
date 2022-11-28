using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace StreamChats.DonationAlerts;

internal class SocketClientInfoConverter : JsonConverter<SocketClientInfo>
{
    public override void WriteJson(JsonWriter writer, SocketClientInfo? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override SocketClientInfo? ReadJson(JsonReader reader, Type objectType, SocketClientInfo? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        return new SocketClientInfo
        {
            ClientId = (string)JObject.Load(reader)["result"]["client"]
        };
    }
}