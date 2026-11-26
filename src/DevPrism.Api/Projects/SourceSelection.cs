namespace DevPrism.Api.Projects;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;

/// <summary>Turns supported browser links into local selections, never into MCP endpoints.</summary>
internal sealed partial record SourceSelection(string Provider, string ScopeKey, string SourceUrl)
{
    internal const string Boards = "AzureDevOpsBoards";
    internal const string Figma = "Figma";
    internal const string Prefix = "selection-v1:";
    internal const int MaxUrlLength = 2048;

    internal static bool TryParse(string? provider, string? input, [NotNullWhen(true)] out SourceSelection? selection)
    {
        selection = null;
        var text = input?.Trim();
        if (text is not { Length: > 0 and <= MaxUrlLength }
            || text.Any(char.IsControl) || text.Contains('\\')
            || !Uri.TryCreate(text, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps || !uri.IsDefaultPort || uri.UserInfo.Length != 0)
            return false;

        var path = provider switch
        {
            Boards => BoardsPath(uri),
            Figma => FigmaPath(uri),
            _ => null,
        };
        if (path is null || provider is null || Prefix.Length + path.Length > 400) return false;
        selection = new SourceSelection(provider, Prefix + path, Origin(provider) + path);
        return true;
    }

    internal static bool TryRead(string provider, string scopeKey, [NotNullWhen(true)] out SourceSelection? selection)
    {
        selection = null;
        return scopeKey.StartsWith(Prefix, StringComparison.Ordinal)
            && TryParse(provider, Origin(provider) + scopeKey[Prefix.Length..], out selection)
            && selection.ScopeKey == scopeKey;
    }

    private static string Origin(string provider) => provider == Boards ? "https://dev.azure.com" : "https://www.figma.com";

    private static string? BoardsPath(Uri uri)
    {
        var segments = uri.AbsolutePath.TrimEnd('/').Split('/')[1..];
        string organization;
        if (uri.Host == "dev.azure.com" && segments.Length >= 2)
        {
            organization = segments[0];
            segments = segments[1..];
        }
        else if (uri.Host.EndsWith(".visualstudio.com", StringComparison.Ordinal))
        {
            organization = uri.Host[..^".visualstudio.com".Length];
            if (segments.Length > 1 && segments[0].Equals("DefaultCollection", StringComparison.OrdinalIgnoreCase))
                segments = segments[1..];
        }
        else return null;

        if (!OrganizationPattern().IsMatch(organization) || segments.Length is not (1 or 2 or 4)) return null;
        var project = Uri.UnescapeDataString(segments[0]);
        if (!ProjectPattern().IsMatch(project) || project != project.Trim()) return null;
        var scope = $"/{organization.ToLowerInvariant()}/{Uri.EscapeDataString(project)}";
        if (segments.Length == 1 || (segments.Length == 2 && segments[1].Equals("_boards", StringComparison.OrdinalIgnoreCase)))
            return scope;
        if (segments.Length != 4 || !segments[1].Equals("_workitems", StringComparison.OrdinalIgnoreCase)
            || !segments[2].Equals("edit", StringComparison.OrdinalIgnoreCase)
            || !int.TryParse(segments[3], NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id <= 0)
            return null;
        return $"{scope}/_workitems/edit/{id.ToString(CultureInfo.InvariantCulture)}";
    }

    private static string? FigmaPath(Uri uri)
    {
        if (uri.Host is not ("figma.com" or "www.figma.com")) return null;
        var segments = uri.AbsolutePath.TrimEnd('/').Split('/');
        if (segments.Length is not (3 or 4) || segments[1] is not ("design" or "file")
            || !FileKeyPattern().IsMatch(segments[2])) return null;

        var path = $"/design/{segments[2]}";
        var query = QueryHelpers.ParseQuery(uri.Query);
        if (!query.TryGetValue("node-id", out var nodes)) return path;
        if (nodes.Count != 1 || nodes[0] is not string node || !NodePattern().IsMatch(node)) return null;
        return $"{path}?node-id={node.Replace(':', '-')}";
    }

    [GeneratedRegex(@"\A[a-zA-Z0-9][a-zA-Z0-9-]{0,49}\z", RegexOptions.NonBacktracking)]
    private static partial Regex OrganizationPattern();

    [GeneratedRegex(@"\A[\p{L}\p{Nd}][\p{L}\p{M}\p{Nd} _.-]{0,63}\z", RegexOptions.NonBacktracking)]
    private static partial Regex ProjectPattern();

    [GeneratedRegex(@"\A[a-zA-Z0-9]{10,128}\z", RegexOptions.NonBacktracking)]
    private static partial Regex FileKeyPattern();

    [GeneratedRegex(@"\A[0-9]{1,12}[-:][0-9]{1,12}\z", RegexOptions.NonBacktracking)]
    private static partial Regex NodePattern();
}
