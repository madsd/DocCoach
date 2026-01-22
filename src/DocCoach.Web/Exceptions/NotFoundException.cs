namespace DocCoach.Web.Exceptions;

/// <summary>
/// Thrown when a requested entity is not found.
/// </summary>
public class NotFoundException : Exception
{
    public string EntityType { get; }
    public string? EntityId { get; }

    public NotFoundException(string entityType, string? entityId = null)
        : base($"{entityType} not found{(entityId != null ? $": {entityId}" : "")}")
    {
        EntityType = entityType;
        EntityId = entityId;
    }

    public NotFoundException(string message) : base(message)
    {
        EntityType = "Entity";
    }
}
