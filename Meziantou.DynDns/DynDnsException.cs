namespace Meziantou.DynDns;

public sealed class DynDnsException : Exception
{
    public DynDnsException()
    {
    }

    public DynDnsException(string message) : base(message)
    {
    }

    public DynDnsException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
