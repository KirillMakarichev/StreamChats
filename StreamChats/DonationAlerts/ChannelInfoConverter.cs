using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace StreamChats.DonationAlerts;

internal class ChannelInfoConverter : JsonConverter<ChannelInfo>
{
    public override void WriteJson(JsonWriter writer, ChannelInfo? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override ChannelInfo? ReadJson(JsonReader reader, Type objectType, ChannelInfo? existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        var jObject = JObject.Load(reader)["channels"].First;
        return new ChannelInfo()
        {
            Channel = (string)jObject["channel"],
            Token = (string)jObject["token"]
        };
    }
}