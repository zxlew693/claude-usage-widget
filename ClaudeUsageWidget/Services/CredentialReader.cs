using System.IO;
using System.Text.Json;
using ClaudeUsageWidget.Models;

namespace ClaudeUsageWidget.Services;

/// <summary>
/// Reads the OAuth token Claude Code stores in ~/.claude/.credentials.json.
/// Read-only by design: the widget never refreshes the token itself, because rotating the
/// refresh token would sign Claude Code out. When Claude Code writes a new token, the
/// file watcher raises <see cref="Changed"/>.
/// </summary>
public sealed class CredentialReader : IDisposable
{
    private FileSystemWatcher? _watcher;
    private readonly Timer _debounce;

    public event Action? Changed;

    public string FilePath { get; }

    public CredentialReader()
    {
        var dir = Environment.GetEnvironmentVariable("CLAUDE_CONFIG_DIR");
        if (string.IsNullOrWhiteSpace(dir))
            dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude");
        FilePath = Path.Combine(dir, ".credentials.json");
        _debounce = new Timer(_ => Changed?.Invoke(), null, Timeout.Infinite, Timeout.Infinite);
    }

    /// <summary>Returns null when the user isn't signed in. Throws IOException if the file stays unreadable.</summary>
    public Credentials? Read()
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                if (!File.Exists(FilePath)) return null;

                using var stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);
                using var doc = JsonDocument.Parse(stream);

                if (!doc.RootElement.TryGetProperty("claudeAiOauth", out var oauth) ||
                    oauth.ValueKind != JsonValueKind.Object)
                    return null;

                var token = Json.String(oauth, "accessToken");
                if (string.IsNullOrEmpty(token)) return null;

                DateTimeOffset? expiresAt = null;
                if (oauth.TryGetProperty("expiresAt", out var exp) &&
                    exp.ValueKind == JsonValueKind.Number && exp.TryGetInt64(out var ms))
                    expiresAt = DateTimeOffset.FromUnixTimeMilliseconds(ms);

                return new Credentials(token, expiresAt, Json.String(oauth, "subscriptionType"));
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
            {
                // Probably caught Claude Code mid-write; try again shortly.
                Thread.Sleep(200);
            }
        }
        throw new IOException("Could not read the Claude Code credentials file.");
    }

    public void StartWatching()
    {
        var dir = Path.GetDirectoryName(FilePath);
        if (dir is null || !Directory.Exists(dir)) return;

        _watcher = new FileSystemWatcher(dir, Path.GetFileName(FilePath))
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
        };
        _watcher.Changed += OnFileEvent;
        _watcher.Created += OnFileEvent;
        _watcher.Renamed += OnFileEvent;
        _watcher.EnableRaisingEvents = true;
    }

    private void OnFileEvent(object sender, FileSystemEventArgs e) =>
        _debounce.Change(TimeSpan.FromMilliseconds(750), Timeout.InfiniteTimeSpan);

    public void Dispose()
    {
        _watcher?.Dispose();
        _debounce.Dispose();
    }
}
