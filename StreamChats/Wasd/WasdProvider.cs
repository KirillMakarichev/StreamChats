using H.Socket.IO;
using Newtonsoft.Json;
using Polly.Contrib.WaitAndRetry;
using StreamChats.Shared;

namespace StreamChats.Wasd;

public class WasdProvider : StreamingPlatformProviderBase
{
    public override string Platform => "Wasd";
    public string AccessToken => _accessToken;

    private readonly string _accessToken;
    private readonly SocketIoClient _socketClient = new();
    private readonly HttpClient _apiClient;
    private readonly string _channelName;
    private Thread _longPollHeartbeat;
    private Thread _longPollExponentialBackoff;
    private bool _isUsed;

    private WasdProvider(HttpClient apiClient, string channelName, string accessToken)
    {
        _apiClient = apiClient;
        _channelName = channelName;
        _accessToken = accessToken;
    }

    public static async Task<WasdProvider> InitializeAsync(string channelName, string? apiToken = null,
        string? accessToken = null)
    {
        var apiClient = new HttpClient();

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            return new WasdProvider(apiClient, channelName, accessToken);
        }

        apiClient.DefaultRequestHeaders.Add("Authorization", $"Token {(apiToken == null ? "" : apiToken)}");
        var tokenResult = await apiClient.PostAsync("https://wasd.tv/api/auth/chat-token", null);

        if (!tokenResult.IsSuccessStatusCode)
        {
            return null;
        }

        var tokenJson = await tokenResult.Content.ReadAsStringAsync();

        var resp = JsonConvert.DeserializeObject<Dictionary<string, string>>(tokenJson);
        if (resp == null)
            return null;

        var gotToken = resp.TryGetValue("result", out var token);
        return !gotToken ? null : new WasdProvider(apiClient, channelName, token);
    }

    protected override async Task SubscribeForUpdatesAsync()
    {
        if (_isUsed)
        {
            return;
        }

        _isUsed = true;

        await ConnectAsync();
        var user = await GetUserInfoAsync(_channelName);

        if (!user.IsSuccessfulCode)
            return;

        var joinRequest = new JoinRequest()
        {
            Jwt = _accessToken,
            ChannelId = user.Content.ChannelId,
            StreamId = user.Content.StreamId,
            ExcludeStickers = true
        };

        await _socketClient.Emit("join", joinRequest);

        _socketClient.Disconnected += (_, args) => OnError($"{args.Status}");

        _socketClient.ErrorReceived += (_, args) => OnError(args.Value);

        _socketClient.On("message", async s =>
        {
            var json = JsonConvert.DeserializeObject<Dictionary<string, object>>(s);
            var userId = (long)json["user_id"];

            OnUpdate(new MessagesArray(
                new List<Message>()
                {
                    new(Text: (string)json["message"],
                        CreatedAt: ((DateTime)json["date_time"]).ToUniversalTime(),
                        UserId: userId.ToString(),
                        UserName: (string)json["user_login"],
                        Mine: userId == user.Content.UserId)
                }
            ));
        });

        _longPollHeartbeat = new Thread(async () =>
        {
            while (true)
            {
                await _socketClient.SendEventAsync("heartbeat", "/");
                await Task.Delay(40000);
            }
        });

        _longPollExponentialBackoff = new Thread(async () =>
        {
            var backoffs = new Queue<TimeSpan>(Backoff.ExponentialBackoff(TimeSpan.FromSeconds(1), 3));

            while (true)
            {
                var peeked = backoffs.Dequeue();
                await Task.Delay((int)peeked.TotalMilliseconds);
                await ConnectAsync();

                var last = backoffs.Last();
                backoffs.Enqueue(last);
            }
        });

        _longPollHeartbeat.Start();
        _longPollExponentialBackoff.Start();
    }

    private async Task ConnectAsync()
    {
        await _socketClient.ConnectAsync(new Uri("wss://chat.wasd.tv"));
    }

    private async Task<ResponseWrapper<UserInfo>> GetUserInfoAsync(string userName)
    {
        var resp = await _apiClient.GetAsync($"https://wasd.tv/api/v2/broadcasts/public?channel_name={userName}");
        if (!resp.IsSuccessStatusCode)
        {
            return new ResponseWrapper<UserInfo>()
            {
                IsSuccessfulCode = false
            };
        }

        var userJson = await resp.Content.ReadAsStringAsync();

        var userInfo = JsonConvert.DeserializeObject<UserInfo>(userJson, new UserInfoConverter());

        return new ResponseWrapper<UserInfo>()
        {
            IsSuccessfulCode = userInfo != null,
            Content = userInfo
        };
    }

    public override void Dispose()
    {
        _apiClient?.Dispose();
        _socketClient?.Dispose();
        _longPollHeartbeat?.Interrupt();
        _longPollExponentialBackoff?.Interrupt();
    }
}