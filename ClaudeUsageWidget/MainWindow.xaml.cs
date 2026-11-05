using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using ClaudeUsageWidget.Services;
using ClaudeUsageWidget.ViewModels;
using Microsoft.Win32;

namespace ClaudeUsageWidget;

public partial class MainWindow : Window
{
    private readonly Settings _settings = Settings.Load();
    private readonly UsageViewModel _vm = new();
    private readonly CredentialReader _credentials = new();
    private readonly UsageClient _client = new();
    private readonly UsagePoller _poller;
    private readonly DispatcherTimer _ticker = new() { Interval = TimeSpan.FromSeconds(1) };

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;

        _poller = new UsagePoller(_credentials, _client);
        _poller.Result += r => Dispatcher.InvokeAsync(() => _vm.Apply(r));
        _vm.RefreshRequested += _poller.RefreshNow;
        _credentials.Changed += _poller.RefreshNow;
        _ticker.Tick += (_, _) => _vm.Tick();
        SystemEvents.PowerModeChanged += OnPowerModeChanged;

        ApplySettings();
        PlaceWindow();

        SourceInitialized += (_, _) => NativeMethods.HideFromAltTab(this);
        Loaded += (_, _) =>
        {
            _credentials.StartWatching();
            _poller.Start();
            _ticker.Start();
        };
    }

    private void ApplySettings()
    {
        Topmost = _settings.Topmost;
        Opacity = Math.Clamp(_settings.Opacity, 0.3, 1.0);
        _vm.IsCompact = _settings.Compact;
    }

    private void PlaceWindow()
    {
        var screen = new Rect(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);

        if (_settings.Left is { } left && _settings.Top is { } top && screen.Contains(new Point(left + 40, top + 20)))
        {
            Left = left;
            Top = top;
        }
        else
        {
            // Default: top-right corner of the primary monitor's work area.
            var work = SystemParameters.WorkArea;
            Left = work.Right - Width - 16;
            Top = work.Top + 16;
        }
    }

    private void SavePosition()
    {
        _settings.Left = Left;
        _settings.Top = Top;
        _settings.Save();
    }

    private void OnDragWindow(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed) return;
        DragMove();
        SavePosition();
    }

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume) _poller.RefreshNow();
    }

    // ---- Menu ----

    private void OnMenuButtonClick(object sender, RoutedEventArgs e)
    {
        var menu = Card.ContextMenu!;
        menu.PlacementTarget = MenuButton;
        menu.Placement = PlacementMode.Bottom;
        menu.IsOpen = true;
    }

    private void OnMenuOpened(object sender, RoutedEventArgs e)
    {
        TopmostItem.IsChecked = Topmost;
        CompactItem.IsChecked = _vm.IsCompact;
        StartupItem.IsChecked = StartupManager.IsEnabled;
        foreach (var item in OpacityMenu.Items.OfType<MenuItem>())
            item.IsChecked = Math.Abs(ParseTag(item) - Opacity) < 0.01;
    }

    private void OnRefreshClick(object sender, RoutedEventArgs e) => _poller.RefreshNow();

    private void OnTopmostClick(object sender, RoutedEventArgs e)
    {
        Topmost = _settings.Topmost = TopmostItem.IsChecked;
        _settings.Save();
    }

    private void OnCompactClick(object sender, RoutedEventArgs e)
    {
        _vm.IsCompact = _settings.Compact = CompactItem.IsChecked;
        _settings.Save();
    }

    private void OnOpacityClick(object sender, RoutedEventArgs e)
    {
        Opacity = _settings.Opacity = ParseTag((MenuItem)sender);
        _settings.Save();
    }

    private void OnStartupClick(object sender, RoutedEventArgs e)
    {
        try
        {
            StartupManager.SetEnabled(StartupItem.IsChecked);
        }
        catch
        {
            StartupItem.IsChecked = StartupManager.IsEnabled;
        }
    }

    private void OnExitClick(object sender, RoutedEventArgs e) => Close();

    private static double ParseTag(MenuItem item) =>
        double.Parse((string)item.Tag, CultureInfo.InvariantCulture);

    protected override void OnClosing(CancelEventArgs e)
    {
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        _ticker.Stop();
        SavePosition();
        _poller.Dispose();
        _credentials.Dispose();
        _client.Dispose();
        base.OnClosing(e);
    }
}
