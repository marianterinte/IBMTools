namespace DevPrism.Infrastructure.Persistence;

/// <summary>Non-secret MCP configuration. A null project uses the workstation-wide default.</summary>
public sealed class McpProfileRecord
{
    public Guid Id { get; init; }
    public Guid? ProjectId { get; init; }
    public required string Provider { get; init; }
    public string? Endpoint { get; set; }
    public bool Enabled { get; set; }
    public Guid Version { get; set; }
}
