namespace DevPrism.Api.Mcp;

using DevPrism.Api.Projects;
using Microsoft.Extensions.Caching.Memory;

/// <summary>Per-process authorization and observations. Never stores credentials or status in PostgreSQL.</summary>
internal sealed class McpSessionState(TimeProvider clock) : IDisposable
{
    private readonly MemoryCache cache = new(new MemoryCacheOptions { SizeLimit = 128 });

    // Serialize probe/auth/disconnect operations on the single-operator workstation.
    internal SemaphoreSlim Operation { get; } = new(1, 1);

    internal Observation Status(McpProfile profile)
    {
        if (!profile.Enabled) return new("Disabled", null);
        if (profile.Endpoint is null) return new("EndpointRequired", null);
        var state = Find(profile);
        return state?.Token is { } token && token.ExpiresAt <= clock.GetUtcNow()
            ? new("SignInRequired", state.Observation.CheckedAt)
            : state?.Observation ?? new("NotChecked", null);
    }

    internal AccessToken? Token(McpProfile profile) => Find(profile)?.Token is { } token
        && token.ExpiresAt > clock.GetUtcNow() ? token : null;

    internal void Set(McpProfile profile, string state, AccessToken? token = null) =>
        cache.Set(Key(profile), new Entry(profile, token, new(state, clock.GetUtcNow())),
            new MemoryCacheEntryOptions().SetSize(1).SetAbsoluteExpiration(TimeSpan.FromHours(12)));

    internal void Forget(Guid? projectId, string provider) => cache.Remove((projectId, provider));
    internal void PutAttempt(AuthAttempt attempt) => cache.Set(typeof(AuthAttempt), attempt,
        new MemoryCacheEntryOptions().SetSize(1).SetAbsoluteExpiration(TimeSpan.FromMinutes(5)));
    internal AuthAttempt? Attempt() => cache.Get<AuthAttempt>(typeof(AuthAttempt));
    internal void ForgetAttempt() => cache.Remove(typeof(AuthAttempt));

    // Called under Operation so no authorized Figma request can finish after clearing its entry.
    internal void ForgetFigmaAuthorization()
    {
        ForgetAttempt();
        foreach (var key in cache.Keys.OfType<(Guid? ProjectId, string Provider)>())
            if (key.Provider == SourceSelection.Figma) cache.Remove(key);
    }

    private Entry? Find(McpProfile profile) => cache.Get<Entry>(Key(profile)) is { } entry && entry.Profile == profile ? entry : null;
    private static (Guid?, string) Key(McpProfile profile) => (profile.ProjectId, profile.Provider);
    private sealed record Entry(McpProfile Profile, AccessToken? Token, Observation Observation);

    internal sealed record Observation(string State, DateTimeOffset? CheckedAt);
    internal sealed class AccessToken(string value, DateTimeOffset expiresAt)
    {
        internal string Value { get; } = value;
        internal DateTimeOffset ExpiresAt { get; } = expiresAt;
    }

    internal sealed class AuthAttempt(Guid? routeProjectId, McpProfile profile, string state, string verifier)
    {
        internal Guid? RouteProjectId { get; } = routeProjectId;
        internal McpProfile Profile { get; } = profile;
        internal string State { get; } = state;
        internal string Verifier { get; } = verifier;
    }

    public void Dispose() { cache.Dispose(); Operation.Dispose(); }
}
