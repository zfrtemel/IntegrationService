namespace IntegrationService.Core.Exceptions;

public sealed class ProviderNotSupportedException : Exception
{
    public ProviderNotSupportedException(string message) : base(message)
    {
    }
}
