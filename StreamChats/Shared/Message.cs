namespace StreamChats.Shared;

public class Message
{
    public string UserId { get; set; }
    public string Text { get; set; }
    public string UserName { get; set; }
    public DateTime CreatedAt { get; set; }
}