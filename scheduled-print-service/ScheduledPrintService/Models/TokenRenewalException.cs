namespace ScheduledPrintService.Models;

/// <summary>
/// Exception thrown when token renewal fails after a 401 Unauthorized response.
/// This signals that the service should stop and notify administrators.
/// </summary>
public class TokenRenewalException : Exception
{
    public TokenRenewalException() 
        : base("Failed to renew authentication token after receiving 401 Unauthorized")
    {
    }

    public TokenRenewalException(string message) 
        : base(message)
    {
    }

    public TokenRenewalException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
