namespace DevPrism.Api.Mcp;

using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text.Json;

/// <summary>One OAuth client per workstation user. Only status is exposed by the API.</summary>
internal sealed class FigmaCredentials
{
    private const int MaxFileBytes = 16 * 1024;
    private readonly string? path;
    private readonly bool externallyManaged;
    private Snapshot snapshot = new(null, false);

    public FigmaCredentials(IConfiguration configuration)
        : this(configuration, Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)) { }

    internal FigmaCredentials(IConfiguration configuration, string localApplicationData)
    {
        var id = configuration["Mcp:Figma:ClientId"];
        var secret = configuration["Mcp:Figma:ClientSecret"];
        externallyManaged = !string.IsNullOrEmpty(id) || !string.IsNullOrEmpty(secret);
        if (externallyManaged)
        {
            if (Valid(id) && Valid(secret)) snapshot = new(new Pair(id!, secret!), false);
            return;
        }
        if (!OperatingSystem.IsWindows() || !Path.IsPathFullyQualified(localApplicationData)) return;
        path = Path.Combine(localApplicationData, "DevPrismAi", "figma-mcp-client.dpapi");
        snapshot = Load(path);
    }

    internal Pair? Current => Volatile.Read(ref snapshot).Credential;
    internal bool CanEdit => path is not null && !externallyManaged;

    internal Status Describe()
    {
        var current = Volatile.Read(ref snapshot);
        return new(current.Credential is not null, CanEdit,
            externallyManaged ? "External" : current.Failed ? "Unreadable" : current.Credential is not null ? "Local" : "None");
    }

    internal static bool Valid(string? value) => value is { Length: > 0 and <= 512 }
        && !value.Any(ch => char.IsControl(ch) || char.IsWhiteSpace(ch));

    internal void Save(Pair credential)
    {
        if (!OperatingSystem.IsWindows() || !CanEdit || path is null) throw new InvalidOperationException();
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new[] { credential.ClientId, credential.Secret });
        try
        {
            var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    file.Write(protectedBytes);
                    file.Flush(flushToDisk: true);
                }
                File.Move(temporary, path, overwrite: true);
            }
            finally { File.Delete(temporary); }
        }
        finally { CryptographicOperations.ZeroMemory(bytes); }
        Volatile.Write(ref snapshot, new(credential, false));
    }

    internal void Remove()
    {
        if (!CanEdit || path is null) throw new InvalidOperationException();
        if (Volatile.Read(ref snapshot) is { Credential: null, Failed: false }) return;
        File.Delete(path);
        Volatile.Write(ref snapshot, new(null, false));
    }

    [SupportedOSPlatform("windows")]
    private static Snapshot Load(string file)
    {
        try
        {
            using var input = File.OpenRead(file);
            if (input.Length is 0 or > MaxFileBytes) return new(null, true);
            var ciphertext = new byte[(int)input.Length];
            input.ReadExactly(ciphertext);
            var bytes = ProtectedData.Unprotect(ciphertext, null, DataProtectionScope.CurrentUser);
            try
            {
                return JsonSerializer.Deserialize<string[]>(bytes) is [var id, var secret] && Valid(id) && Valid(secret)
                    ? new(new Pair(id, secret), false) : new(null, true);
            }
            finally { CryptographicOperations.ZeroMemory(bytes); }
        }
        catch (Exception error) when (error is FileNotFoundException or DirectoryNotFoundException) { return new(null, false); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or CryptographicException or JsonException)
        { return new(null, true); }
    }

    private sealed record Snapshot(Pair? Credential, bool Failed);
    internal sealed record Status(bool Configured, bool CanEdit, string Source);

    internal sealed class Pair(string clientId, string secret)
    {
        internal string ClientId { get; } = clientId;
        internal string Secret { get; } = secret;
    }
}
