# Security Policy

CopyPasta is a local Windows snippet manager. This document describes what the application does, what it does not do, and how to report vulnerabilities.

## Supported versions

| Version | Supported |
|---------|-----------|
| 0.3.x   | Yes |
| 0.2.x and earlier | No |

## What CopyPasta does

- Stores code snippets as plaintext JSON under `%LOCALAPPDATA%\CopyPasta`
- Lets the user search, tag, favorite, export, and import snippets
- Copies a snippet to the clipboard only when the user asks

## What CopyPasta does not do

- Does not require administrator privileges
- Does not install a Windows service or scheduled task
- Does not create startup or registry persistence
- Does not execute stored snippets, PowerShell, CMD, or other processes
- Does not load plugins or arbitrary assemblies
- Does not require network connectivity
- Does not contain telemetry or analytics
- Does not check for updates
- Does not encrypt snippet storage
- Does not manage credentials

## Local storage

CopyPasta stores its local database beneath the current user's `%LOCALAPPDATA%\CopyPasta` directory.

Typical files:

- `snippets.json` — active snippet database (plaintext JSON)
- `snippets.json.bak` — previous valid database after a successful save
- `settings.json` — local preferences, including optional clipboard auto-clear

If `%LOCALAPPDATA%\CopyPasta\snippets.json` is not present and a legacy database exists under `%APPDATA%\CopyPasta`, CopyPasta copies `snippets.json`, `snippets.json.bak`, and `settings.json` into LocalAppData. Existing LocalAppData data is never overwritten. Roaming originals are left in place. If that copy fails, CopyPasta continues using the roaming folder.

CopyPasta does not write to `Program Files`, Windows system directories, or HKLM.

A corrupt or oversized local database is not overwritten. The About dialog and load warning include the file path so the data can be recovered or restored from `snippets.json.bak`.

## Clipboard behavior

Clipboard access happens only when the user copies a snippet (toolbar button, `Ctrl+C`, or Enter). CopyPasta does not monitor clipboard history or transmit clipboard data.

Optional auto-clear (off by default) removes the clipboard contents after a delay **only if** the clipboard still contains the exact text CopyPasta copied.

## Network behavior

CopyPasta itself performs no network synchronization or transmission. There are no application-controlled HTTP clients, update checks, or cloud sync. CopyPasta does not require network connectivity for normal operation.

## Privilege requirements

The application requests `asInvoker` and is intended to run as a standard user. UAC elevation is not required and should not be requested.

## Import / export

Exports are plaintext JSON chosen by the user. Imports are treated as untrusted input:

- Deserialization is limited to CopyPasta snippet models
- `TypeNameHandling` is disabled
- File size, snippet count, and field lengths are bounded
- Invalid files are rejected without modifying the live database

Imported snippet text is never executed, interpreted, compiled, or launched.

The same size, snippet-count, and field-length bounds apply when loading the local `snippets.json` database. If the file is oversized, contains too many snippets, is not valid JSON, or contains any invalid snippet, CopyPasta does not load a partial list and does not overwrite the file.

## Secret-storage warning

CopyPasta is **not** a password manager and is **not** intended to store secrets. Do not store:

- Passwords
- API keys
- OAuth or session tokens
- Service-account credentials
- Client secrets
- Private keys
- BitLocker recovery keys
- Connection strings containing credentials
- Certificates containing private keys

Snippet files and exports are plaintext.

## Reporting a vulnerability

Please report security issues privately by opening a security advisory on the GitHub repository or contacting the maintainer through the GitHub profile for [wvunjo/CopyPasta](https://github.com/wvunjo/CopyPasta). Do not open a public issue for unreleased vulnerabilities.

Include the affected version, a description of the issue, and steps to reproduce.

## Verifying a release

Until Authenticode signing is available, verify downloaded artifacts with the published SHA-256 hashes:

```powershell
Get-FileHash .\CopyPastaNative.exe -Algorithm SHA256
```

Do not treat a self-signed certificate as public trust.
