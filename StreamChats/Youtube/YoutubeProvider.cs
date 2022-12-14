using System.Text;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using StreamChats.Shared;

namespace StreamChats.Youtube;

public class YoutubeProvider : StreamingPlatformProviderBase
{
    public override string Platform => "Youtube";
    private Thread _longPollThread;
    private readonly YoutubeServices _youtubeServices;
    private readonly ulong _updateDelay;
    private readonly VideoData _videoData;
    private const ulong UpdateDelayDefault = 5000;

    private YoutubeProvider(YoutubeServices youtubeServices, LiveBroadcastListResponse streamData, ulong updateDelay)
    {
        _youtubeServices = youtubeServices;
        _updateDelay = updateDelay == 0 ? UpdateDelayDefault : updateDelay;

        var streamDataItem = streamData.Items[0];
        _videoData = new VideoData(
            streamDataItem.Snippet.LiveChatId,
            streamDataItem.Snippet.ChannelId,
            streamDataItem.Id
        );
    }

    public static async Task<YoutubeProvider> InitializeFromFileAsync(string fileCredentialsPath,
        ulong updateDelay = UpdateDelayDefault)
    {
        await using var stream = new FileStream(fileCredentialsPath, FileMode.Open, FileAccess.Read);

        return await SetCredentialAsync(stream, updateDelay);
    }

    public static async Task<YoutubeProvider> InitializeFromJsonAsync(string credentialsJson,
        ulong updateDelay = UpdateDelayDefault)
    {
        await using var stream = new MemoryStream(Encoding.Default.GetBytes(credentialsJson));

        return await SetCredentialAsync(stream, updateDelay);
    }

    private static async Task<YoutubeProvider> SetCredentialAsync(Stream stream, ulong updateDelay = UpdateDelayDefault)
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

        LiveBroadcastListResponse? streamData = null;

        try
        {
            streamData = await req.ExecuteAsync();
        }
        catch (Exception e)
        {
            return null;
        }

        return new YoutubeProvider(services, streamData, updateDelay);
    }

    protected override async Task SubscribeForUpdatesAsync()
    {
        var query = _youtubeServices.LiveChatMessagesResource.List(_videoData.StreamId,
            new Repeatable<string>(new[] { "snippet", "authorDetails" }));

        _longPollThread = new Thread(async () =>
        {
            while (true)
            {
                LiveChatMessageListResponse? resp = null;
                try
                {
                    resp = await query.ExecuteAsync();
                }
                catch (Exception e)
                {
                    OnError(e);
                    return;
                }

                query.PageToken = resp.NextPageToken;

                if (resp.Items.Any())
                {
                    OnUpdate(new MessagesArray(resp.Items.Select(x =>
                    {
                        var snippetAuthorChannelId = x.Snippet.AuthorChannelId;

                        return new Message(
                            Text: x.Snippet.DisplayMessage,
                            CreatedAt: DateTime.Parse(x.Snippet.PublishedAtRaw).ToUniversalTime(),
                            UserId: snippetAuthorChannelId,
                            UserName: x.AuthorDetails.DisplayName,
                            Mine: _videoData.ChannelId == snippetAuthorChannelId
                        );
                    }).ToList()));
                }

                await Task.Delay((int)_updateDelay);
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
        
        try
        {
            await req.ExecuteAsync();
        }
        catch (Exception e)
        {
            OnError(e, false);
        }
    }

    public async Task<StreamStatistic> GetStatisticAsync()
    {
        var req = _youtubeServices.VideosResource.List(new Repeatable<string>(new[]
            { "statistics", "liveStreamingDetails" }));

        req.Id = new Repeatable<string>(new[] { _videoData.VideoId });

        VideoListResponse? videoData;
        
        try
        {
            videoData = await req.ExecuteAsync();
        }
        catch (Exception e)
        {
            OnError(e, false);
            return null;
        }

        var videoDataItem = videoData.Items[0];
        return new StreamStatistic(
            ConcurrentViewersCount: videoDataItem.LiveStreamingDetails.ConcurrentViewers ?? 0,
            ViewsCount: videoDataItem.Statistics.ViewCount ?? 0,
            LikesCount: videoDataItem.Statistics.LikeCount ?? 0
        );
    }

    public override void Dispose()
    {
        _youtubeServices?.Dispose();
        _longPollThread?.Interrupt();
    }
}