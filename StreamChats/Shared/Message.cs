namespace StreamChats.Shared;

public record Message(string UserId, string Text, string UserName, DateTime CreatedAt, bool Mine);