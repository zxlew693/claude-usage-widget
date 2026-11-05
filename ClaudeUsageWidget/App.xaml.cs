using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace ClaudeUsageWidget;

public partial class App : Application
{
    private Mutex? _mutex;
    private bool _ownsMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Only one widget per user session.
        _mutex = new Mutex(initiallyOwned: true, @"Local\ClaudeUsageWidget.SingleInstance", out _ownsMutex);
        if (!_ownsMutex)
        {
            Shutdown();
            return;
        }

        DispatcherUnhandledException += OnUnhandledException;
        new MainWindow().Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_ownsMutex) _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }

    private static void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // Keep the widget alive; record the error (never contains the token) for debugging.
        try
        {
            Directory.CreateDirectory(Settings.Directory);
            File.AppendAllText(Path.Combine(Settings.Directory, "error.log"),
                $"[{DateTimeOffset.Now:u}] {e.Exception}{Environment.NewLine}");
        }
        catch { /* best effort */ }
        e.Handled = true;
    }
}
