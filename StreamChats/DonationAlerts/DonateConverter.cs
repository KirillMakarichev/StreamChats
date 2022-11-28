using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StreamChats.Shared;

namespace StreamChats.DonationAlerts;

internal class DonateConverter : JsonConverter<Donate>
{
    public override void WriteJson(JsonWriter writer, Donate? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override Donate? ReadJson(JsonReader reader, Type objectType, Donate? existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        var jObject = JObject.Load(reader)["result"]["data"]["data"];

        var donate = new Donate()
        {
            Id = (long)jObject["id"],
            UserName = (string)jObject["username"],
            CreatedAt = (DateTime)jObject["created_at"],
            Message = (string)jObject["message"],
            AmountInUserCurrency = (decimal)jObject["amount_in_user_currency"]
        };
        return donate;
    }
}