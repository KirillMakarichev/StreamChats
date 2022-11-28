using System.Text;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;
using Google.Apis.Services;
using Google.Apis.Util;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using StreamChats.Shared;

namespace StreamChats.Youtube;

public class YoutubeProvider : IStreamingPlatformProvider
{
    public event Func<UpdateEvent, Task> OnUpdateAsync;
    public Platform Platform => Platform.Youtube;

    private readonly YouTubeService _youTubeService;
    private readonly LiveBroadcastsResource _liveBroadcastsResource;
    private Thread _longPollThread;

    private YoutubeProvider(IConfigurableHttpClientInitializer credential)
    {
        _youTubeService = new YouTubeService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = typeof(YoutubeProvider).FullName,
        });

        _liveBroadcastsResource = new LiveBroadcastsResource(_youTubeService);
    }

    public static async Task<YoutubeProvider> InitializeFromFileAsync(string fileCredentialsPath)
    {
        await using var stream = new FileStream(fileCredentialsPath, FileMode.Open, FileAccess.Read);

        return await SetCredential(stream);
    }
    
    public static async Task<YoutubeProvider> InitializeFromJsonAsync(string credentialsJson)
    {
        await using var stream = new MemoryStream(Encoding.Default.GetBytes(credentialsJson));

        return await SetCredential(stream);
    }

    private static async Task<YoutubeProvider> SetCredential(Stream stream)
    {
        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            GoogleClientSecrets.Load(stream).Secrets,
            new[] { YouTubeService.Scope.YoutubeReadonly },
            "user",
            CancellationToken.None,
            new FileDataStore(typeof(YoutubeProvider).FullName)
        );

        return new YoutubeProvider(credential);
    }

    public async Task SubscribeForMessagesAsync()
    {
        var req = _liveBroadcastsResource.List(new Repeatable<string>(new[] { "snippet" }));
        req.Mine = true;

        var data = await req.ExecuteAsync();

        var query = new LiveChatMessagesResource.ListRequest(_youTubeService, data.Items[0].Snippet.LiveChatId,
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
                        Messages = resp.Items.Select(x => new Message()
                        {
                            Text = x.Snippet.DisplayMessage,
                            CreatedAt = DateTime.Parse(x.Snippet.PublishedAtRaw),
                            UserId = x.Snippet.AuthorChannelId,
                            UserName = x.AuthorDetails.DisplayName
                        }).ToList()
                    });
                }

                await Task.Delay(5000);
            }
        });

        _longPollThread.Start();
    }

    public void Dispose()
    {
        _youTubeService.Dispose();
        _longPollThread.Interrupt();
    }
}