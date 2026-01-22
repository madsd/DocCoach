namespace DocCoach.Web.Exceptions;

/// <summary>
/// Thrown when an external service (AI, storage) fails.
/// </summary>
public class ExternalServiceException : Exception
{
    public string ServiceName { get; }
    public string? Operation { get; }

    public ExternalServiceException(string serviceName, string message, string? operation = null, Exception? innerException = null)
        : base(message, innerException)
    {
        ServiceName = serviceName;
        Operation = operation;
    }

    public ExternalServiceException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
        ServiceName = "ExternalService";
    }
}
