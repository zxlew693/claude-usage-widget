using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using ClaudeUsageWidget.Models;

namespace ClaudeUsageWidget.Services;

public sealed class UnauthorizedException() : Exception("The Claude Code token was rejected.");

public sealed class RateLimitedException(TimeSpan? retryAfter) : Exception("Rate limited.")
{
    public TimeSpan? RetryAfter { get; } = retryAfter;
}

/// <summary>
/// Calls Anthropic's usage endpoint (the same one Claude Code's /usage uses).
/// Undocumented, so parsing is deliberately tolerant. The token is only ever sent here.
/// </summary>
public sealed class UsageClient : IDisposable
{
    private const string Endpoint = "https://api.anthropic.com/api/oauth/usage";

    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(20) };

    public UsageClient()
    {
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("claude-usage-widget/0.1");
        _http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        _http.DefaultRequestHeaders.Add("anthropic-beta", "oauth-2025-04-20");
    }

    public async Task<UsageSnapshot> FetchAsync(string accessToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, Endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _http.SendAsync(request, ct);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            throw new UnauthorizedException();

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            var ra = response.Headers.RetryAfter;
            throw new RateLimitedException(ra?.Delta ?? (ra?.Date - DateTimeOffset.UtcNow));
        }

        response.EnsureSuccessStatusCode();

        await using Stream stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        return UsageParser.Parse(doc.RootElement, DateTimeOffset.Now);
    }

    public void Dispose() => _http.Dispose();
}
