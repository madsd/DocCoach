namespace DocCoach.Web.Exceptions;

/// <summary>
/// Thrown when input validation fails.
/// </summary>
public class ValidationException : Exception
{
    public string Field { get; }
    public object? Value { get; }

    public ValidationException(string field, string message, object? value = null)
        : base(message)
    {
        Field = field;
        Value = value;
    }

    public ValidationException(string message) : base(message)
    {
        Field = string.Empty;
    }
}
