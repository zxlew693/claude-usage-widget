# Security

Usage Meter for Claude reads a sensitive credential: the OAuth access token that Claude Code saves when you sign in. This page explains how the app handles that token, and how to report a problem.

## How the token is handled

- **It is only read.**
  - The app reads `%USERPROFILE%\.claude\.credentials.json`, or the same file in `CLAUDE_CONFIG_DIR`.
  - It never writes to this file or refreshes the token. A refresh would replace the token Claude Code is using and sign Claude Code out.
  - When Claude Code saves a new token, the app picks it up by watching the file.
- **It is sent to one place.**
  - It goes in the `Authorization` header of HTTPS requests to `https://api.anthropic.com/api/oauth/usage`, and nowhere else. The address is fixed in the code (`Services/UsageClient.cs`) and can't be changed by settings.
  - Certificate checks use Windows' normal TLS validation and are never turned off.
- **It is kept in memory only.** It is not written to the app's settings or its error log, and it is never shown on screen.
- **The app has no other network access.** It has no update checks, no telemetry and no third-party services.
- **It runs with your normal user rights.** It needs no administrator access. The Store version declares `runFullTrust` because it needs that to read the Claude Code folder.

Anyone who can read your user profile can also read Claude Code's token file. This app doesn't add or remove that exposure.

## Verifying a download

- Releases are built by GitHub Actions (`.github/workflows/release.yml`) from the tagged commit, not on a developer's PC.
- Each release includes `claude-usage-widget.exe.sha256`. Compare it with the hash of your download:

  ```powershell
  Get-FileHash claude-usage-widget.exe -Algorithm SHA256
  ```

- The exe from GitHub isn't code-signed yet, which is why SmartScreen warns about it. The Microsoft Store version is signed by Microsoft.
- Download the app only from this repository's Releases page or from the Microsoft Store.

## Supported versions

Only the latest release gets security fixes.

## Reporting a vulnerability

**Please don't report security problems in public issues.**

To report privately:

1. Open the repository's **Security** tab.
2. Click **Report a vulnerability**.

Please include:

- what you found, and its impact;
- steps to reproduce it;
- the app version (right-click the exe → Properties → Details) and your Windows version.

You should get a reply within 7 days. Once a fix is released, reporters are credited unless they ask not to be.

Problems in Claude Code itself, or in Anthropic's API, should go to Anthropic: <https://www.anthropic.com/responsible-disclosure-policy>.
