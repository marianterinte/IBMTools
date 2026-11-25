// Spec: Documentation/dev-prism-ai-specs/12_SECURITY_AND_OPERATIONS.md (local pairing).
namespace DevPrism.Api.Auth;

using System.Security.Cryptography;
using System.Text;

/// <summary>Owns the single, short-lived local operator session and one-use pairing grant.</summary>
internal sealed class LocalOperatorSession(IConfiguration configuration, TimeProvider clock)
{
    internal const string CookieName = "__Host-DevPrismSession";
    internal const string Scheme = "LocalOperator";
    internal static readonly Guid OperatorId = Guid.Parse("9d95cc2f-af03-4494-9ac4-8ad034aa2b36");
    private static readonly TimeSpan SessionDuration = TimeSpan.FromMinutes(30);
    private readonly object gate = new();
    private readonly DateTimeOffset pairingExpiresAt = clock.GetUtcNow().AddMinutes(15);
    private byte[]? pairingHash = HashPairingCode(configuration["DevPrism:PairingCode"]);
    private byte[]? sessionHash;
    private DateTimeOffset sessionExpiresAt;

    /// <summary>Reports whether a valid bootstrap grant is still available.</summary>
    public bool CanPair
    {
        get
        {
            lock (gate) return pairingHash is not null && clock.GetUtcNow() < pairingExpiresAt;
        }
    }

    /// <summary>Consumes the supplied grant and creates a new opaque browser session.</summary>
    public bool TryPair(string? code, out string token, out DateTimeOffset expiresAt)
    {
        token = string.Empty;
        expiresAt = default;
        var suppliedHash = HashPairingCode(code);
        lock (gate)
        {
            if (pairingHash is null || suppliedHash is null || clock.GetUtcNow() >= pairingExpiresAt
                || !CryptographicOperations.FixedTimeEquals(pairingHash, suppliedHash)) return false;

            CryptographicOperations.ZeroMemory(pairingHash);
            pairingHash = null;
            token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            sessionHash = SHA256.HashData(Encoding.ASCII.GetBytes(token));
            sessionExpiresAt = expiresAt = clock.GetUtcNow().Add(SessionDuration);
            return true;
        }
    }

    /// <summary>Authenticates only the current unexpired opaque session.</summary>
    public bool IsAuthenticated(string? token)
    {
        lock (gate) return clock.GetUtcNow() < sessionExpiresAt && MatchesSession(token);
    }

    /// <summary>Revokes the exact active session so replaying its cookie does not restore access.</summary>
    public void Revoke(string? token)
    {
        lock (gate)
        {
            if (!MatchesSession(token)) return;
            CryptographicOperations.ZeroMemory(sessionHash!);
            sessionHash = null;
            sessionExpiresAt = default;
        }
    }

    /// <summary>Compares the bounded opaque identifier with its server-side digest under the gate.</summary>
    private bool MatchesSession(string? token) => sessionHash is not null && token?.Length == 64
        && CryptographicOperations.FixedTimeEquals(sessionHash, SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    /// <summary>Rejects malformed bootstrap codes before comparing or hashing.</summary>
    private static byte[]? HashPairingCode(string? code) => code?.Length == 64 && code.All(Uri.IsHexDigit)
        ? SHA256.HashData(Encoding.ASCII.GetBytes(code))
        : null;
}
