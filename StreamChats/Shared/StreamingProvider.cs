namespace StreamChats.Shared;

public class StreamingProvider : IDisposable
{
    private readonly List<IStreamingPlatformProvider> _providers;
    
    public StreamingProvider(List<IStreamingPlatformProvider> providers)
    {
        if (providers == null && !providers.Any())
        {
            throw new ArgumentNullException(nameof(providers));
        }

        _providers = providers.Where(x => x != null).ToList();
    }

    public async Task SubscribeForMessagesAsync(Func<UpdateEvent<IUpdate>, Task> handler)
    {
        var tasks = new List<Task>();
        foreach (var provider in _providers)
        {
            provider.OnUpdateAsync += handler;
            
            tasks.Add(provider.SubscribeForMessagesAsync());
        }

        await Task.WhenAll(tasks);
    }

    public void Dispose()
    {
        foreach (var provider in _providers)
        {
            provider.Dispose();
        }
    }
}