namespace DataAccessor.Models;

public class AuditLogEntry
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string ActorName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string? TechnicalDetails { get; set; }
}
