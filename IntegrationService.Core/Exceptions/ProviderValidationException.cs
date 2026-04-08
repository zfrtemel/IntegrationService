namespace IntegrationService.Core.Exceptions;

public sealed class ProviderValidationException : Exception
{
    public ProviderValidationException(string message) : base(message)
    {
    }
}
