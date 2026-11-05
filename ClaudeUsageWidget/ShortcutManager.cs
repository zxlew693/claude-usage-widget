using System.IO;

namespace ClaudeUsageWidget;

/// <summary>
/// Creates "Claude Usage Widget" shortcuts on the desktop and in the Start menu, so the app
/// shows a friendly name there while the exe itself stays claude-usage-widget.exe.
/// </summary>
public static class ShortcutManager
{
    private const string DisplayName = "Claude Usage Widget";

    private static string DesktopPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), DisplayName + ".lnk");

    private static string StartMenuPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), DisplayName + ".lnk");

    public static void CreateAll()
    {
        Create(DesktopPath);
        Create(StartMenuPath);
    }

    private static void Create(string linkPath)
    {
        var exe = Environment.ProcessPath ?? throw new InvalidOperationException("Unknown executable path.");
        var shellType = Type.GetTypeFromProgID("WScript.Shell") ?? throw new InvalidOperationException("WScript.Shell unavailable.");

        dynamic shell = Activator.CreateInstance(shellType)!;
        try
        {
            dynamic link = shell.CreateShortcut(linkPath);
            link.TargetPath = exe;
            link.WorkingDirectory = Path.GetDirectoryName(exe);
            link.IconLocation = exe + ",0";
            link.Description = "Shows your Claude plan usage";
            link.Save();
        }
        finally
        {
            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);
        }
    }
}
