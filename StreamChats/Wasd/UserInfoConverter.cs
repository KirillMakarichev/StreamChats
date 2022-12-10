using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace StreamChats.Wasd;

internal class UserInfoConverter : JsonConverter<UserInfo>
{
    public override void WriteJson(JsonWriter writer, UserInfo? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override UserInfo? ReadJson(JsonReader reader, Type objectType, UserInfo? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        var jObject = JObject.Load(reader)["result"];

        var userInfo = new UserInfo();
        userInfo.ChannelId = jObject["channel"]["channel_id"].ToObject<long>();
        userInfo.UserId = jObject["channel"]["user_id"].ToObject<long>();

        userInfo.IsActive = jObject["channel"]["channel_is_live"].ToObject<bool>();

        if (userInfo.IsActive)
        {
            userInfo.StreamId = jObject["media_container"]["media_container_streams"].First["stream_id"]
                .ToObject<long>();
        }

        return userInfo;
    }
}