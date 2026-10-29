# Claude Token Manager

A small Windows tray app for keeping a library of auth tokens and switching which one Claude Code uses. It works with any token Claude Code accepts in `ANTHROPIC_AUTH_TOKEN`, such as keys for a LiteLLM proxy or another gateway.

Switching a profile rewrites `env.ANTHROPIC_AUTH_TOKEN` in `~/.claude/settings.json` and leaves everything else in the file alone. The previous version is saved as `settings.json.bak`. Running Claude Code sessions have to be restarted to pick up the new token.

## Storage

- Tokens: Windows Credential Manager, as generic credentials named `ClaudeTokenManager:<id>`.
- Profile names and notes: `%APPDATA%\ClaudeTokenManager\profiles.json`. This file holds only a SHA-256 hash and a short hint of each token, never the token itself.

## Install

Requires the .NET 10 SDK.

```powershell
.\install.ps1                    # build, copy to %LOCALAPPDATA%\Programs, add a Start Menu shortcut
.\install.ps1 -StartWithWindows  # same, and launch at sign-in
```

The app shows up under Settings > Apps > Installed apps. No admin rights are needed. Run `install.ps1` again to update; profiles and tokens are kept.

## Uninstall

Use Uninstall in Installed apps, or run `uninstall.ps1`. It asks whether to also delete your saved profiles and the tokens in Credential Manager. Pass `-RemoveData` or `-KeepData` to skip the question.

## Use

- Left-click the tray icon to open the manager. Right-click it to switch profiles; the active one is checked.
- If settings.json already has a token that isn't in your library, the manager offers to save it as a profile.
- To rotate a key, edit the profile and paste the new token. If that profile is active, settings.json is updated right away.
