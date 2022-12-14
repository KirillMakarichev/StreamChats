namespace StreamChats.Shared;

public interface IStreamingPlatformProvider : IDisposable
{
    public event Func<UpdateEvent<IUpdate>, Task>? OnUpdateAsync;
    public event Func<StreamingException, Task>? OnErrorAsync;
    public string Platform { get; }
    public Task SubscribeForMessagesAsync();
}