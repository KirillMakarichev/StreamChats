namespace StreamChats.Shared;

public record MessagesArray(List<Message> Messages) : IUpdate
{
    public string EventType => "Messages";
}