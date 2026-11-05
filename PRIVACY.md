# Privacy Policy — Usage Meter for Claude

_Last updated: 5 November 2026_

Usage Meter for Claude ("the app") is an open-source Windows desktop widget that shows your Claude plan usage limits. It is an independent project and is not affiliated with or endorsed by Anthropic.

**In short:** the app has no servers, no accounts, no analytics and no ads. It only contacts Anthropic, and only to fetch your own usage numbers.

## What the app reads on your PC

- **Claude Code's sign-in file:** `%USERPROFILE%\.claude\.credentials.json`, or the folder set in `CLAUDE_CONFIG_DIR`. The app reads two values from it:
  - your access token, which it uses to request your usage;
  - your plan type (for example "Pro" or "Max"), which it shows in the card's title.

  The app never changes, refreshes or copies this file.

## What the app sends, and to whom

- Every 3 minutes, and whenever you click Refresh, the app sends one HTTPS request to Anthropic at `https://api.anthropic.com/api/oauth/usage`. The request contains your access token, which Anthropic needs to identify your account, and a generic client name (`claude-usage-widget`).
- Anthropic's reply is your current usage percentages and reset times. The app shows them and keeps them in memory only.
- Anthropic handles that request under its own privacy policy: <https://www.anthropic.com/legal/privacy>.

The app sends no data to the developer or to anyone else.

## What the app stores

In `%APPDATA%\ClaudeUsageWidget\` the app stores:

- `settings.json`: the window position and your display choices (always on top, compact mode, opacity).
- `error.log`: written only if the app crashes. It holds the error message and technical details, and never your token or your usage data.

In the Microsoft Store version, Windows keeps these files inside the app's own package folder.

If you turn on **Start with Windows**, the app adds a startup entry: a value in your user registry for the downloaded exe, or the app's startup task for the Store version.

Usage data is not saved to disk.

## What the app does not do

- It collects no analytics, telemetry or crash reports.
- It has no advertising or tracking.
- It never sells or shares data.
- It doesn't collect your name, email or other personal information.
- It has no account with the developer.

## Removing your data

1. Uninstall the app.
   - Store version: Settings → Apps.
   - Downloaded exe: turn off **Start with Windows** first, then delete the exe.
2. Delete `%APPDATA%\ClaudeUsageWidget\`.

Your Claude Code sign-in is unaffected.

## Children

The app is not directed at children, and it collects no personal information from anyone.

## Changes

Any change to this policy will be published in this file, with a new "Last updated" date. You can see every past version in the repository history.

## Contact

Open an issue at <https://github.com/zxlew693/claude-usage-widget/issues>.
