namespace StreamChats.Shared;

public class Donate
{
    public long Id { get; set; }
    public string Message { get; set; }
    public string UserName { get; set; }
    public decimal AmountInUserCurrency { get; set; }
    public DateTime CreatedAt { get; set; }
}