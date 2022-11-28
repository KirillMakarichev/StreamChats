using H.WebSockets;
using Newtonsoft.Json;
using StreamChats.Shared;

namespace StreamChats.DonationAlerts;

public class DonationAlertsProvider : IStreamingPlatformProvider
{
    public event Func<UpdateEvent, Task>? OnUpdateAsync;

    public Platform Platform => Platform.DonationAlerts;

    private readonly HttpClient _httpClient;
    private readonly WebSocketClient _webSocketClient;
    private Thread _longPollThread;

    private readonly int _clientId = Random.Shared.Next(); 
    
    private DonationAlertsProvider(HttpClient httpClient, WebSocketClient webSocketClient)
    {
        _httpClient = httpClient;
        _webSocketClient = webSocketClient;
    }


    /// <summary>
    /// "https://www.donationalerts.com/oauth/authorize?client_id={ID приложения}&redirect_uri=http://127.0.0.1/login&response_type=token&scope=oauth-donation-subscribe oauth-user-show"
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public static async Task<DonationAlertsProvider> InitializeAsync(string token)
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        var webSocketClient = new WebSocketClient();

        return new DonationAlertsProvider(httpClient, webSocketClient);
    }

    public async Task SubscribeForMessagesAsync()
    {
        var connectionInfo = await StepFirstAsync();

        if (!connectionInfo.IsSuccessfulCode)
        {
            return;
        }

        await _webSocketClient.ConnectAsync(new Uri("wss://centrifugo.donationalerts.com/connection/websocket"));

        var clientInfo = await StepSecondAsync(connectionInfo.Content);

        if (!clientInfo.IsSuccessfulCode)
        {
            return;
        }

        var channelInfo = await StepThirdAsync(connectionInfo.Content, clientInfo.Content);

        if (!clientInfo.IsSuccessfulCode)
        {
            return;
        }

        await StepFourthAsync(channelInfo.Content);

        _longPollThread = new Thread(async () =>
        {
            while (_webSocketClient.IsConnected)
            {
                var message = await _webSocketClient.WaitTextAsync();
                
                var donate = JsonConvert.DeserializeObject<Donate>(message.Value, converters: new DonateConverter());
                
                OnUpdateAsync?.Invoke(new UpdateEvent()
                {
                    Donate = donate,
                    EventType = EventType.Donate,
                    PlatformIdentity = Platform.DonationAlerts
                });
            }
        });

        _longPollThread.Start();
    }

    private async Task StepFourthAsync(ChannelInfo channelInfo)
    {
        var message =
            $"{{\"params\":{{\"channel\":\"{channelInfo.Channel}\",\"token\":\"{channelInfo.Token}\"}},\"method\":1,\"id\":{_clientId + 1}}}";
        await _webSocketClient.SendTextAsync(
            message);

        await _webSocketClient.WaitTextAsync();
        await _webSocketClient.WaitTextAsync();
    }

    private async Task<ResponseWrapper<ChannelInfo>> StepThirdAsync(ConnectionInfo connectionInfo,
        SocketClientInfo clientInfo)
    {
        var createChannelResp = await _httpClient.PostAsync(
            "https://www.donationalerts.com/api/v1/centrifuge/subscribe",
            new StringContent(
                $"{{\"channels\":[\"$alerts:donation_{connectionInfo.UserId}\"], \"client\":\"{clientInfo.ClientId}\"}}"));

        if (!createChannelResp.IsSuccessStatusCode)
        {
            return new ResponseWrapper<ChannelInfo>()
            {
                IsSuccessfulCode = false
            };
        }

        var responseContent = await createChannelResp.Content.ReadAsStringAsync();
        var channelInfo =
            JsonConvert.DeserializeObject<ChannelInfo>(responseContent, converters: new ChannelInfoConverter());

        return new ResponseWrapper<ChannelInfo>()
        {
            IsSuccessfulCode = channelInfo != null,
            Content = channelInfo
        };
        ;
    }

    private async Task<ResponseWrapper<ConnectionInfo>> StepFirstAsync()
    {
        var resp = await _httpClient.GetAsync("https://www.donationalerts.com/api/v1/user/oauth");
        if (!resp.IsSuccessStatusCode)
        {
            return new ResponseWrapper<ConnectionInfo>()
            {
                IsSuccessfulCode = false
            };
        }

        var connectionInfoJson = (await resp
            .Content
            .ReadAsStringAsync());

        var connectionInfo =
            JsonConvert.DeserializeObject<ConnectionInfo>(connectionInfoJson,
                converters: new ConnectionInfoConverter());

        return new ResponseWrapper<ConnectionInfo>()
        {
            IsSuccessfulCode = connectionInfo != null,
            Content = connectionInfo
        };
    }

    private async Task<ResponseWrapper<SocketClientInfo>> StepSecondAsync(ConnectionInfo connectionInfo)
    {
        await _webSocketClient.SendTextAsync(
            $"{{\"params\":{{\"token\":\"{connectionInfo.ConnectionToken}\"}},\"id\":{_clientId}}}");

        var resp = (await _webSocketClient.WaitTextAsync()).Value;

        var clientInfo =
            JsonConvert.DeserializeObject<SocketClientInfo>(resp, converters: new SocketClientInfoConverter());

        return new ResponseWrapper<SocketClientInfo>()
        {
            IsSuccessfulCode = clientInfo != null,
            Content = clientInfo
        };
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _webSocketClient.Dispose();
        _longPollThread.Interrupt();
    }
}