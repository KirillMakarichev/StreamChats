namespace StreamChats.Shared;

internal class ResponseWrapper<T>
{
    public bool IsSuccessfulCode { get; set; }
    public T Content { get; set; }
}