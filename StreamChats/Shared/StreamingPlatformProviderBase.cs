namespace StreamChats.Shared;

public abstract class StreamingPlatformProviderBase : IStreamingPlatformProvider
{
    public event Func<UpdateEvent<IUpdate>, Task>? OnUpdateAsync;
    public event Func<StreamingException, Task>? OnErrorAsync;
    public abstract string Platform { get; }

    public async Task SubscribeForMessagesAsync()
    {
        try
        {
            await SubscribeForUpdatesAsync();
        }
        catch (Exception e)
        {
            OnError(e);
        }
    }

    protected abstract Task SubscribeForUpdatesAsync();

    protected void OnError(Exception exception, bool dispose = true) => OnError(exception.Message, dispose: dispose);

    protected void OnError(string exception, bool dispose = true)
    {
        OnErrorAsync?.Invoke(new StreamingException()
        {
            Message = exception,
            Platform = Platform,
            PlatformProvider = this
        });

        if (dispose)
        {
            Dispose();
        }
    }

    protected void OnUpdate(IUpdate update)
    {
        OnUpdateAsync?.Invoke(new UpdateEvent<IUpdate>()
        {
            PlatformIdentity = Platform,
            Body = update
        });
    }

    public virtual void Dispose()
    {
    }
}