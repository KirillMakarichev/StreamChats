using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StreamChats.DonationAlerts;
using StreamChats.Shared;
using StreamChats.Wasd;
using StreamChats.Youtube;

var clientSecrets = JObject.Parse(File.ReadAllText("app_secrets.json"));

var wasd = await WasdProvider.InitializeAsync(channelName: "hardplay");

var token = (string)clientSecrets["donationAlerts"]["token"];
using var da = await DonationAlertsProvider.InitializeAsync(token);

var youtubeOptions = clientSecrets["youtube"].ToString();

using var youtube =
    await YoutubeProvider.InitializeFromJsonAsync(youtubeOptions, 10);

using var streamingProvider = new StreamingProvider(
    new List<IStreamingPlatformProvider>()
    {
        da,
        youtube,
        wasd,
    });

streamingProvider.SubscribeForException(async exception =>
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(JsonConvert.SerializeObject(exception));
    Console.ResetColor();

    Console.WriteLine(exception.PlatformProvider);
});

await streamingProvider.SubscribeForMessagesAsync(async @event =>
{
    Console.WriteLine(@event.PlatformIdentity);
    switch (@event.Body.EventType)
    {
        case "Messages":
            if (@event.Body is not MessagesArray messages)
            {
                return;
            }

            foreach (var message in messages.Messages)
            {
                Console.WriteLine($"{message.UserName}: {message.Text}");
            }

            break;

        case "Donate":
            if (@event.Body is not Donate donate)
            {
                return;
            }

            Console.WriteLine($"{donate.UserName}: {donate.AmountInUserCurrency}");
            break;

        default:
            return;
    }

    Console.WriteLine();
});

await Task.Delay(-1);