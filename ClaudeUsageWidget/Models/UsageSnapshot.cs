namespace ClaudeUsageWidget.Models;

/// <summary>One plan limit, e.g. the 5-hour session or the weekly cap.</summary>
public sealed record UsageLimit(
    string Kind,
    string Label,
    double Percent,
    string? Severity,
    DateTimeOffset? ResetsAt,
    bool IsActive);

/// <summary>Share of the weekly window consumed by a surface (Claude Code, chat, ...).</summary>
public sealed record UsageBreakdownRow(string Name, double Percent);

public sealed record UsageSnapshot(
    IReadOnlyList<UsageLimit> Limits,
    IReadOnlyList<UsageBreakdownRow> Breakdown,
    DateTimeOffset FetchedAt);

public sealed record Credentials(string AccessToken, DateTimeOffset? ExpiresAt, string? SubscriptionType)
{
    public bool IsExpired => ExpiresAt is { } exp && exp <= DateTimeOffset.UtcNow;
}
