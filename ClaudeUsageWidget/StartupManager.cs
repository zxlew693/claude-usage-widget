using Microsoft.Win32;
using Windows.ApplicationModel;

namespace ClaudeUsageWidget;

/// <summary>
/// Toggles "Start with Windows".
/// Plain exe: HKCU\...\Run (no admin rights needed).
/// MSIX/Store: the package's StartupTask, which users also see in Settings → Apps → Startup.
/// </summary>
public static class StartupManager
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "ClaudeUsageWidget";
    private const string TaskId = "UsageMeterStartup"; // must match AppxManifest.xml

    private static string Command => $"\"{Environment.ProcessPath}\"";

    public static async Task<bool> IsEnabledAsync()
    {
        if (PackageInfo.IsPackaged)
        {
            var task = await StartupTask.GetAsync(TaskId);
            return IsOn(task.State);
        }

        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue(ValueName) is string value &&
               string.Equals(value, Command, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Returns the resulting state, which can differ from the request (e.g. the user disabled it in Settings).</summary>
    public static async Task<bool> SetEnabledAsync(bool enabled)
    {
        if (PackageInfo.IsPackaged)
        {
            var task = await StartupTask.GetAsync(TaskId);
            if (enabled) return IsOn(await task.RequestEnableAsync());
            if (task.State == StartupTaskState.Enabled) task.Disable();
            return IsOn((await StartupTask.GetAsync(TaskId)).State);
        }

        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (enabled) key.SetValue(ValueName, Command);
        else key.DeleteValue(ValueName, throwOnMissingValue: false);
        return enabled;
    }

    private static bool IsOn(StartupTaskState state) =>
        state is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy;
}
