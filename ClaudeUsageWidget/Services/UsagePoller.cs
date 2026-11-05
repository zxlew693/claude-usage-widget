using ClaudeUsageWidget.Models;

namespace ClaudeUsageWidget.Services;

public enum PollState { Loading, Ok, Stale, Expired, NoCredentials }

/// <param name="Snapshot">New data, or null when the poll failed (the UI keeps the last good data).</param>
/// <param name="NextAttemptIn">Set when retrying sooner/later than normal after a failure.</param>
public sealed record PollResult(PollState State, UsageSnapshot? Snapshot, string? Plan, TimeSpan? NextAttemptIn);

/// <summary>Polls every 3 minutes; backs off 1→2→4→8→15 min on errors; can be kicked for an immediate refresh.</summary>
public sealed class UsagePoller : IDisposable
{
    private static readonly TimeSpan NormalInterval = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan MinManualGap = TimeSpan.FromSeconds(10);

    private readonly CredentialReader _credentials;
    private readonly UsageClient _client;
    private readonly SemaphoreSlim _kick = new(0, 1);
    private readonly CancellationTokenSource _cts = new();
    private int _failures;
    private DateTimeOffset _lastAttempt = DateTimeOffset.MinValue;

    public event Action<PollResult>? Result;

    public UsagePoller(CredentialReader credentials, UsageClient client)
    {
        _credentials = credentials;
        _client = client;
    }

    public void Start() => _ = Task.Run(() => LoopAsync(_cts.Token));

    /// <summary>Thread-safe. Coalesces multiple requests into one poll.</summary>
    public void RefreshNow()
    {
        try { _kick.Release(); }
        catch (SemaphoreFullException) { /* already pending */ }
        catch (ObjectDisposedException) { }
    }

    private async Task LoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            // Avoid hammering the endpoint if refresh is spammed.
            var sinceLast = DateTimeOffset.Now - _lastAttempt;
            if (sinceLast < MinManualGap)
            {
                try { await Task.Delay(MinManualGap - sinceLast, ct); }
                catch (OperationCanceledException) { return; }
            }

            _lastAttempt = DateTimeOffset.Now;
            var delay = await PollOnceAsync(ct);

            try { await _kick.WaitAsync(delay, ct); }
            catch (OperationCanceledException) { return; }
        }
    }

    private async Task<TimeSpan> PollOnceAsync(CancellationToken ct)
    {
        Credentials? creds;
        try
        {
            creds = _credentials.Read();
        }
        catch (Exception)
        {
            return Fail(PollState.Stale, null, null);
        }

        if (creds is null)
        {
            Raise(PollState.NoCredentials, null, null, null);
            return NormalInterval; // the file watcher kicks us when the user signs in
        }

        var plan = FormatPlan(creds.SubscriptionType);

        if (creds.IsExpired)
        {
            Raise(PollState.Expired, null, plan, null);
            return NormalInterval;
        }

        try
        {
            var snapshot = await _client.FetchAsync(creds.AccessToken, ct);
            _failures = 0;
            Raise(PollState.Ok, snapshot, plan, null);
            return NormalInterval;
        }
        catch (UnauthorizedException)
        {
            Raise(PollState.Expired, null, plan, null);
            return NormalInterval;
        }
        catch (RateLimitedException ex)
        {
            return Fail(PollState.Stale, plan, ex.RetryAfter);
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            return Fail(PollState.Stale, plan, null);
        }
    }

    private TimeSpan Fail(PollState state, string? plan, TimeSpan? hint)
    {
        _failures++;
        var delay = TimeSpan.FromMinutes(Math.Pow(2, Math.Min(_failures - 1, 4)));
        if (delay > MaxBackoff) delay = MaxBackoff;
        if (hint is { } h && h > delay) delay = h < TimeSpan.FromHours(1) ? h : TimeSpan.FromHours(1);
        Raise(state, null, plan, delay);
        return delay;
    }

    private void Raise(PollState state, UsageSnapshot? snapshot, string? plan, TimeSpan? next) =>
        Result?.Invoke(new PollResult(state, snapshot, plan, next));

    private static string? FormatPlan(string? subscriptionType) =>
        string.IsNullOrWhiteSpace(subscriptionType)
            ? null
            : char.ToUpperInvariant(subscriptionType[0]) + subscriptionType[1..];

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        _kick.Dispose();
    }
}
