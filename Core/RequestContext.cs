namespace Core;

public interface IRequestContext
{
    public string? Tenant { get; set; }
}

public class RequestContext : IRequestContext
{
    public string? Tenant { get; set; }
}