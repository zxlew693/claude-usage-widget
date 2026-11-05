using System.Windows.Media;
using ClaudeUsageWidget.Models;

namespace ClaudeUsageWidget.ViewModels;

public sealed class LimitViewModel : ObservableObject
{
    private static readonly Brush NormalBrush = Freeze("#5FB49C");
    private static readonly Brush WarningBrush = Freeze("#E5B567");
    private static readonly Brush CriticalBrush = Freeze("#EF6461");

    private string _resetText = "";

    public LimitViewModel(UsageLimit limit)
    {
        Label = limit.Label;
        Percent = Math.Clamp(limit.Percent, 0, 100);
        ResetsAt = limit.ResetsAt;
        BarBrush = BrushFor(limit.Severity, Percent);
        ResetTooltip = ResetsAt is { } r ? $"Resets {r.ToLocalTime():dddd d MMM, t}" : null;
    }

    public string Label { get; }
    public double Percent { get; }
    public string PercentText => $"{Percent:0}%";
    public Brush BarBrush { get; }
    public DateTimeOffset? ResetsAt { get; }
    public string? ResetTooltip { get; }

    public string ResetText
    {
        get => _resetText;
        private set => Set(ref _resetText, value);
    }

    public void Tick(DateTimeOffset now) => ResetText = FormatReset(ResetsAt, now);

    private static string FormatReset(DateTimeOffset? at, DateTimeOffset now)
    {
        if (at is not { } reset) return "";
        var left = reset - now;
        if (left <= TimeSpan.Zero) return "resetting…";
        if (left < TimeSpan.FromHours(1)) return $"resets in {left.Minutes}m {left.Seconds:00}s";
        if (left < TimeSpan.FromHours(24)) return $"resets in {(int)left.TotalHours}h {left.Minutes:00}m";
        var local = reset.ToLocalTime();
        return $"resets {local:ddd} {local:t}";
    }

    private static Brush BrushFor(string? severity, double percent) => severity?.ToLowerInvariant() switch
    {
        "critical" or "exceeded" or "blocked" => CriticalBrush,
        "warning" or "high" => WarningBrush,
        "normal" or "low" => percent >= 90 ? CriticalBrush : percent >= 75 ? WarningBrush : NormalBrush,
        _ => percent >= 90 ? CriticalBrush : percent >= 70 ? WarningBrush : NormalBrush,
    };

    private static Brush Freeze(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}
