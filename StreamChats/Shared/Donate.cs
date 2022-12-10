namespace StreamChats.Shared;

public record Donate(long Id, string Message, string UserName, decimal AmountInUserCurrency, DateTime CreatedAt);