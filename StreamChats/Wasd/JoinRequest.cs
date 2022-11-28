using Newtonsoft.Json;

namespace StreamChats.Wasd;

internal class JoinRequest
{
    [JsonProperty("streamId")]
    public long StreamId { get; set; }
    [JsonProperty("channelId")]
    public long ChannelId { get; set; }
    [JsonProperty("jwt")]
    public string Jwt { get; set; }
    [JsonProperty("excludeStickers")]
    public bool ExcludeStickers { get; set; }
}