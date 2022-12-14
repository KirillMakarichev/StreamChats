namespace StreamChats.Shared;

public class StreamingException
{
    public string Message { get; set; }
    public string Platform { get; set; }
    public IStreamingPlatformProvider PlatformProvider { get; set; }
}