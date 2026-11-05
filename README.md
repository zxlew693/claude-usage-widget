# Claude Usage Widget

A small floating Windows desktop card that shows your Claude Pro/Max plan limits (5-hour session, weekly, per-model weekly) and their reset countdowns.

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

Then run `publish\ClaudeUsageWidget.exe`. Right-click the card and choose **Start with Windows** to launch it at login.

## Using it

- Drag the card to move it. The position is saved.
- Right-click the card (or click ⋯) for: Refresh, Always on top, Compact mode, Opacity, Start with Windows, Exit.
- Hover over a bar to see the exact reset time. Hover over the card to see the weekly breakdown by surface.

Settings are saved in `%APPDATA%\ClaudeUsageWidget\settings.json`.
