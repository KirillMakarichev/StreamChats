using System.Text;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using StreamChats.Shared;

namespace StreamChats.Youtube;

public class YoutubeProvider : IStreamingPlatformProvider
{
    public event Func<UpdateEvent, Task> OnUpdateAsync;
    public Platform Platform => Platform.Youtube;
    private Thread _longPollThread;
    private readonly YoutubeServices _youtubeServices;
    private readonly VideoData _videoData;

    private YoutubeProvider(YoutubeServices youtubeServices, LiveBroadcastListResponse streamData)
    {
        _youtubeServices = youtubeServices;

        var streamDataItem = streamData.Items[0];
        _videoData = new VideoData(
            streamDataItem.Snippet.LiveChatId,
            streamDataItem.Snippet.ChannelId,
            streamDataItem.Id
        );
    }

    public static async Task<YoutubeProvider> InitializeFromFileAsync(string fileCredentialsPath)
    {
        await using var stream = new FileStream(fileCredentialsPath, FileMode.Open, FileAccess.Read);

        return await SetCredentialAsync(stream);
    }

    public static async Task<YoutubeProvider> InitializeFromJsonAsync(string credentialsJson)
    {
        await using var stream = new MemoryStream(Encoding.Default.GetBytes(credentialsJson));

        return await SetCredentialAsync(stream);
    }

    private static async Task<YoutubeProvider> SetCredentialAsync(Stream stream)
    {
        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            (await GoogleClientSecrets.FromStreamAsync(stream)).Secrets,
            new[] { YouTubeService.Scope.YoutubeReadonly, YouTubeService.Scope.YoutubeForceSsl },
            "user",
            CancellationToken.None,
            new FileDataStore(typeof(YoutubeProvider).FullName)
        );

        //await GoogleWebAuthorizationBroker.ReauthorizeAsync(credential, CancellationToken.None);

        var youTubeService = new YouTubeService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = typeof(YoutubeProvider).FullName,
        });

        var services = new YoutubeServices(youTubeService);

        var req = services.LiveBroadcastsResource.List(new Repeatable<string>(new[] { "snippet" }));
        req.Mine = true;

        var streamData = await req.ExecuteAsync();

        return new YoutubeProvider(services, streamData);
    }

    public async Task SubscribeForMessagesAsync()
    {
        var query = _youtubeServices.LiveChatMessagesResource.List(_videoData.StreamId,
            new Repeatable<string>(new[] { "snippet", "authorDetails" }));

        _longPollThread = new Thread(async () =>
        {
            while (true)
            {
                var resp = await query.ExecuteAsync();
                query.PageToken = resp.NextPageToken;

                if (resp.Items.Any())
                {
                    OnUpdateAsync?.Invoke(new UpdateEvent()
                    {
                        EventType = EventType.Message,
                        PlatformIdentity = Platform,
                        Messages = resp.Items.Select(x =>
                        {
                            var snippetAuthorChannelId = x.Snippet.AuthorChannelId;

                            return new Message(
                                Text: x.Snippet.DisplayMessage,
                                CreatedAt: DateTime.Parse(x.Snippet.PublishedAtRaw),
                                UserId: snippetAuthorChannelId,
                                UserName: x.AuthorDetails.DisplayName,
                                Mine: _videoData.ChannelId == snippetAuthorChannelId
                            );
                        }).ToList()
                    });
                }

                await Task.Delay(5000);
            }
        });

        _longPollThread.Start();
    }

    public async Task SendMessageAsync(string messageText)
    {
        if (string.IsNullOrWhiteSpace(messageText))
        {
            return;
        }

        var req = _youtubeServices.LiveChatMessagesResource.Insert(new LiveChatMessage()
        {
            Snippet = new LiveChatMessageSnippet()
            {
                Type = "textMessageEvent",
                LiveChatId = _videoData.StreamId,
                TextMessageDetails = new LiveChatTextMessageDetails()
                {
                    MessageText = messageText
                }
            }
        }, new Repeatable<string>(new[] { "snippet" }));

        await req.ExecuteAsync();
    }

    public async Task<StreamStatistic> GetStatisticAsync()
    {
        var req = _youtubeServices.VideosResource.List(new Repeatable<string>(new[]
            { "statistics", "liveStreamingDetails" }));

        req.Id = new Repeatable<string>(new[] { _videoData.VideoId });

        var videoData = await req.ExecuteAsync();

        var videoDataItem = videoData.Items[0];
        return new StreamStatistic(
            ConcurrentViewersCount: videoDataItem.LiveStreamingDetails.ConcurrentViewers ?? 0,
            ViewsCount: videoDataItem.Statistics.ViewCount ?? 0,
            LikesCount: videoDataItem.Statistics.LikeCount ?? 0
        );
    }

    public void Dispose()
    {
        _youtubeServices.Dispose();
        _longPollThread.Interrupt();
    }
}