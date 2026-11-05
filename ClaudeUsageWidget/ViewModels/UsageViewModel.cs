using System.Collections.ObjectModel;
using ClaudeUsageWidget.Services;

namespace ClaudeUsageWidget.ViewModels;

public sealed class UsageViewModel : ObservableObject
{
    private string _title = "Claude";
    private string _status = "loading…";
    private string? _breakdownTooltip;
    private bool _isStale;
    private bool _isCompact;

    private PollState _state = PollState.Loading;
    private DateTimeOffset? _fetchedAt;
    private DateTimeOffset? _nextAttemptAt;
    private bool _resetRefreshPending;

    /// <summary>Raised when a limit's reset time passes, so fresh numbers are fetched right away.</summary>
    public event Action? RefreshRequested;

    public ObservableCollection<LimitViewModel> Limits { get; } = new();

    public string Title { get => _title; private set => Set(ref _title, value); }
    public string Status { get => _status; private set => Set(ref _status, value); }
    public string? BreakdownTooltip { get => _breakdownTooltip; private set => Set(ref _breakdownTooltip, value); }

    public bool IsStale
    {
        get => _isStale;
        private set { if (Set(ref _isStale, value)) OnPropertyChanged(nameof(ShowStatus)); }
    }

    public bool IsCompact
    {
        get => _isCompact;
        set
        {
            if (!Set(ref _isCompact, value)) return;
            OnPropertyChanged(nameof(ShowDetails));
            OnPropertyChanged(nameof(ShowStatus));
        }
    }

    public bool ShowDetails => !IsCompact;

    /// <summary>The status line hides in compact mode unless something is wrong.</summary>
    public bool ShowStatus => !IsCompact || IsStale;

    public void Apply(PollResult result)
    {
        _state = result.State;
        if (result.Plan is not null) Title = $"Claude · {result.Plan}";

        if (result.Snapshot is { } snapshot)
        {
            Limits.Clear();
            foreach (var limit in snapshot.Limits) Limits.Add(new LimitViewModel(limit));
            _fetchedAt = snapshot.FetchedAt;
            _resetRefreshPending = false;

            var rows = snapshot.Breakdown.Where(r => r.Percent > 0).ToList();
            BreakdownTooltip = rows.Count == 0
                ? null
                : "Weekly usage by surface\n" + string.Join("\n", rows.Select(r => $"  {r.Name}: {r.Percent:0}%"));
        }

        _nextAttemptAt = result.NextAttemptIn is { } d ? DateTimeOffset.Now + d : null;
        IsStale = result.State != PollState.Ok;
        Tick();
    }

    public void Tick()
    {
        var now = DateTimeOffset.Now;
        foreach (var limit in Limits) limit.Tick(now);

        Status = _state switch
        {
            PollState.Loading => "loading…",
            PollState.Ok => $"updated {Ago(_fetchedAt, now)}",
            PollState.Expired => "Session expired — open Claude Code",
            PollState.NoCredentials => "Not signed in — log in to Claude Code",
            PollState.Stale => StaleText(now),
            _ => "",
        };

        // A window just reset and our data predates it: fetch the new numbers.
        if (_state == PollState.Ok && !_resetRefreshPending && _fetchedAt is { } fetched &&
            Limits.Any(l => l.ResetsAt is { } r && r <= now && fetched < r))
        {
            _resetRefreshPending = true;
            RefreshRequested?.Invoke();
        }
    }

    private string StaleText(DateTimeOffset now)
    {
        var text = "offline";
        if (_nextAttemptAt is { } next)
        {
            var left = next - now;
            text += left > TimeSpan.Zero ? $" · retry in {Short(left)}" : " · retrying…";
        }
        if (_fetchedAt is not null) text += $" · data {Ago(_fetchedAt, now)}";
        return text;
    }

    private static string Ago(DateTimeOffset? at, DateTimeOffset now)
    {
        if (at is not { } t) return "never";
        var span = now - t;
        if (span < TimeSpan.FromMinutes(1)) return "just now";
        if (span < TimeSpan.FromHours(1)) return $"{(int)span.TotalMinutes}m ago";
        return $"{(int)span.TotalHours}h ago";
    }

    private static string Short(TimeSpan span) =>
        span < TimeSpan.FromMinutes(1) ? $"{span.Seconds}s" : $"{(int)Math.Ceiling(span.TotalMinutes)}m";
}
