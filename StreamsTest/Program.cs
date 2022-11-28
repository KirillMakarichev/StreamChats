using Newtonsoft.Json.Linq;
using StreamChats.DonationAlerts;
using StreamChats.Shared;
using StreamChats.Wasd;
using StreamChats.Youtube;

var clientSecrets = JObject.Parse(File.ReadAllText("app_secrets.json"));

var wasds = (await Task.WhenAll(Enumerable.Range(0, 5)
    .Select(x => WasdProvider.InitializeAsync(channelName: "xpamyjl9")))).ToList();

var token = (string)clientSecrets["donationAlerts"]["token"];
using var da = await DonationAlertsProvider.InitializeAsync(token);

var youtubeOptions = clientSecrets["youtube"].ToString();

using var youtube =
    await YoutubeProvider.InitializeFromJsonAsync(youtubeOptions);

using var streamingProvider = new StreamingProvider(
    new List<IStreamingPlatformProvider>(wasds)
    {
        da,
        youtube,
    });

await streamingProvider.SubscribeForMessagesAsync(async @event =>
{
    Console.WriteLine(@event.PlatformIdentity);
    switch (@event.EventType)
    {
        case EventType.Message:
            foreach (var message in @event.Messages)
            {
                Console.WriteLine($"{message.UserName}: {message.Text}");
            }

            break;
        case EventType.Donate:

            Console.WriteLine($"{@event.Donate.UserName}: {@event.Donate.AmountInUserCurrency}");

            break;
        default:
            throw new ArgumentOutOfRangeException();
    }

    Console.WriteLine();
});

await Task.Delay(-1);