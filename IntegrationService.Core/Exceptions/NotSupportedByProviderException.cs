namespace IntegrationService.Core.Exceptions;

public sealed class NotSupportedByProviderException : Exception
{
    public NotSupportedByProviderException(string message) : base(message)
    {
    }
}
