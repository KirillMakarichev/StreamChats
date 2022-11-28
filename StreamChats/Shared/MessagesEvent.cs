namespace StreamChats.Shared;

public class UpdateEvent
{
    public List<Message> Messages { get; set; }
    
    public Donate Donate { get; set; }
    
    public EventType EventType { get; set; }
    public Platform PlatformIdentity { get; set; }
}