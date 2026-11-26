namespace DevPrism.Api.Mcp;

using System.Text.RegularExpressions;
using DevPrism.Api.Projects;
using DevPrism.Infrastructure.Persistence;

/// <summary>Approved destination policy. Browser source links are not MCP destinations.</summary>
internal sealed partial record McpProfile(Guid? ProjectId, string Provider, string? Endpoint, bool Enabled, Guid? Version)
{
    internal const string FigmaEndpoint = "https://mcp.figma.com/mcp";
    internal const string BoardsRoot = "https://mcp.dev.azure.com/";
    internal static IReadOnlyList<string> Providers { get; } = [SourceSelection.Figma, SourceSelection.Boards];

    internal string Origin => ProjectId is not null ? "project" : Version is not null ? "global" : "default";
    internal string? Organization => Provider == SourceSelection.Boards && Endpoint is not null ? Endpoint[BoardsRoot.Length..] : null;

    internal static McpProfile Resolve(string provider, McpProfileRecord? row)
    {
        if (row is null) return new(null, provider, provider == SourceSelection.Figma ? FigmaEndpoint : null, true, null);
        var allowed = TryEndpoint(provider, row.Endpoint, out var endpoint);
        return new(row.ProjectId, provider, endpoint, row.Enabled && allowed, row.Version);
    }

    internal static bool TryEndpoint(string provider, string? value, out string? endpoint)
    {
        endpoint = null;
        var text = value?.Trim();
        if (provider == SourceSelection.Figma)
        {
            if (string.IsNullOrEmpty(text) || text == FigmaEndpoint) { endpoint = FigmaEndpoint; return true; }
            return false;
        }
        if (provider != SourceSelection.Boards) return false;
        if (string.IsNullOrEmpty(text)) return true;
        if (!text.StartsWith(BoardsRoot, StringComparison.Ordinal) || !OrganizationPattern().IsMatch(text[BoardsRoot.Length..]))
            return false;
        endpoint = BoardsRoot + text[BoardsRoot.Length..].ToLowerInvariant();
        return true;
    }

    [GeneratedRegex(@"\A[a-zA-Z0-9][a-zA-Z0-9-]{0,49}\z", RegexOptions.NonBacktracking)]
    private static partial Regex OrganizationPattern();
}
