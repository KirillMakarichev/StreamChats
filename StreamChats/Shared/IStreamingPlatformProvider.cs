using StreamChats.DonationAlerts;

namespace StreamChats.Shared;

public interface IStreamingPlatformProvider : IDisposable
{
    public event Func<UpdateEvent, Task> OnUpdateAsync;
    public Platform Platform { get; }
    public Task SubscribeForMessagesAsync();
}