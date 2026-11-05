using System.Runtime.InteropServices;
using System.Text;

namespace ClaudeUsageWidget;

/// <summary>Tells whether the app runs from an MSIX package (Microsoft Store) or as a plain exe.</summary>
internal static class PackageInfo
{
    public const string DisplayName = "Usage Meter for Claude";

    private const int APPMODEL_ERROR_NO_PACKAGE = 15700;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, StringBuilder? packageFullName);

    public static bool IsPackaged { get; } = Detect();

    private static bool Detect()
    {
        var length = 0;
        return GetCurrentPackageFullName(ref length, null) != APPMODEL_ERROR_NO_PACKAGE;
    }
}
