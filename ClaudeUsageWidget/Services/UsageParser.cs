using System.Globalization;
using System.Text.Json;
using ClaudeUsageWidget.Models;

namespace ClaudeUsageWidget.Services;

/// <summary>
/// Parses the /api/oauth/usage response. Prefers the generic <c>limits[]</c> array so new
/// limit kinds show up automatically; falls back to the older five_hour / seven_day fields.
/// </summary>
public static class UsageParser
{
    public static UsageSnapshot Parse(JsonElement root, DateTimeOffset fetchedAt)
    {
        var limits = new List<UsageLimit>();

        if (root.TryGetProperty("limits", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var e in arr.EnumerateArray())
            {
                if (Json.Number(e, "percent") is not { } percent) continue;
                var kind = Json.String(e, "kind") ?? "limit";
                limits.Add(new UsageLimit(
                    kind,
                    LabelFor(kind, ScopeName(e)),
                    percent,
                    Json.String(e, "severity"),
                    Json.Date(e, "resets_at"),
                    Json.Bool(e, "is_active") ?? false));
            }
        }

        if (limits.Count == 0)
        {
            AddLegacy(root, "five_hour", "Session", limits);
            AddLegacy(root, "seven_day", "Weekly", limits);
            AddLegacy(root, "seven_day_opus", "Opus weekly", limits);
            AddLegacy(root, "seven_day_sonnet", "Sonnet weekly", limits);
        }

        var breakdown = new List<UsageBreakdownRow>();
        if (Json.Object(root, "seven_day_breakdown") is { } bd &&
            bd.TryGetProperty("rows", out var rows) && rows.ValueKind == JsonValueKind.Array)
        {
            foreach (var r in rows.EnumerateArray())
            {
                var name = Json.String(r, "display_name") ?? Json.String(r, "key");
                if (name is not null && Json.Number(r, "percent") is { } p)
                    breakdown.Add(new UsageBreakdownRow(name, p));
            }
        }

        return new UsageSnapshot(limits, breakdown, fetchedAt);
    }

    private static string? ScopeName(JsonElement limit)
    {
        if (Json.Object(limit, "scope") is not { } scope) return null;
        foreach (var key in new[] { "model", "surface" })
        {
            if (Json.Object(scope, key) is { } o &&
                (Json.String(o, "display_name") ?? Json.String(o, "id")) is { } name)
                return name;
            if (Json.String(scope, key) is { } plain) return plain;
        }
        return null;
    }

    private static string LabelFor(string kind, string? scope) => kind switch
    {
        "session" => "Session",
        "weekly_all" => "Weekly",
        "weekly_scoped" => scope is null ? "Weekly (scoped)" : $"{scope} weekly",
        _ => Humanize(kind) + (scope is null ? "" : $" · {scope}"),
    };

    private static string Humanize(string kind)
    {
        var text = kind.Replace('_', ' ').Trim();
        return text.Length == 0 ? "Limit" : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text);
    }

    private static void AddLegacy(JsonElement root, string field, string label, List<UsageLimit> into)
    {
        if (Json.Object(root, field) is { } o && Json.Number(o, "utilization") is { } pct)
            into.Add(new UsageLimit(field, label, pct, null, Json.Date(o, "resets_at"), false));
    }
}
