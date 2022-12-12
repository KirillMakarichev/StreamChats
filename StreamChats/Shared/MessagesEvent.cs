namespace StreamChats.Shared;

public class UpdateEvent<T> 
where T: IUpdate
{
    public T Body { get; set; }
    public string PlatformIdentity { get; set; }
}