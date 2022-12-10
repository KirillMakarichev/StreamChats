using Google.Apis.YouTube.v3;

namespace StreamChats.Youtube;

internal class YoutubeServices : IDisposable
{
    private readonly YouTubeService _youTubeService;

    public YoutubeServices(YouTubeService youTubeService)
    {
        _youTubeService = youTubeService;

        LiveBroadcastsResource = new LiveBroadcastsResource(_youTubeService);
        LiveChatMessagesResource = new LiveChatMessagesResource(_youTubeService);
        VideosResource = new VideosResource(_youTubeService);
    }

    public LiveBroadcastsResource LiveBroadcastsResource { get; }
    public LiveChatMessagesResource LiveChatMessagesResource { get; }
    public VideosResource VideosResource { get; }

    public void Dispose()
    {
        _youTubeService.Dispose();
    }
}