<img src="assets/logo.svg" width="96" alt="Claude Usage Widget logo">

# Claude Usage Widget

A small floating Windows desktop card that shows your Claude Pro/Max plan limits (5-hour session, weekly, per-model weekly) and their reset countdowns.

## Download

1. Download `claude-usage-widget.exe` from the [latest release](https://github.com/zxlew693/claude-usage-widget/releases/latest). There's nothing to install, and you don't need .NET.
2. Run it. Because the exe isn't code-signed, Windows SmartScreen may show **"Windows protected your PC"** the first time. Click **More info → Run anyway**.
3. You need to have logged in to [Claude Code](https://claude.com/claude-code) on this PC, because the widget uses Claude Code's sign-in.

Each release also includes a `.sha256` file. To check that the download is intact: `Get-FileHash claude-usage-widget.exe`.

## How it works

- It reads the OAuth token that Claude Code stores in `%USERPROFILE%\.claude\.credentials.json` (or in `CLAUDE_CONFIG_DIR` if that is set). It only reads this file and never refreshes the token, because refreshing would log Claude Code out.
- Every 3 minutes it calls `GET https://api.anthropic.com/api/oauth/usage`, the endpoint behind Claude Code's `/usage`. On errors it waits longer between retries (up to 15 minutes). It also refreshes when the PC wakes from sleep, when Claude Code writes a new token, and when a limit resets.
- **This endpoint is undocumented**, so it may change without notice. The parser reads the generic `limits[]` array and tolerates missing fields.

## Run

```powershell
dotnet run --project ClaudeUsageWidget
```

## Build a single exe

```powershell
dotnet publish ClaudeUsageWidget -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish
```

Then run `publish\claude-usage-widget.exe`.

## Releasing

Push a version tag. GitHub Actions (`.github/workflows/release.yml`) then builds the exe and publishes it as a release:

```powershell
git tag v0.2.0
git push origin v0.2.0
``` Right-click the card and choose **Start with Windows** to launch it at login.

## Using it

- Drag the card to move it. The position is saved.
- Right-click the card (or click ⋯) for: Refresh, Always on top, Compact mode, Opacity, Start with Windows, Add desktop & Start menu shortcuts, Exit.
- **Add desktop & Start menu shortcuts** creates shortcuts named "Claude Usage Widget" that point at the exe's current location. Move the exe to its permanent folder before you use this.
- Hover over a bar to see the exact reset time. Hover over the card to see the weekly breakdown by surface.

Settings are saved in `%APPDATA%\ClaudeUsageWidget\settings.json`.
