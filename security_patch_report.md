# ClassicUO Security Patch Report

Date: 2026-03-15
Source findings: `security_best_practices_report.md`

## CRIT-001 - UltimaLive path traversal

Problem
- Server-provided shard names were converted into filesystem paths with insufficient validation.

Patch
- Added strict shard-name validation (`A-Z`, `a-z`, `0-9`, `.`, `_`, `-`, max 64 chars).
- Rejected rooted paths and `.` / `..` path segments.
- Moved storage under fixed base: `<Local/CommonApplicationData>/ClassicUO/UltimaLive/<shard>`.
- Enforced canonical `StartsWith(basePath)` boundary check.

Files
- `src/ClassicUO.Client/Game/UltimaLive.cs`

Why this fixes it
- Untrusted shard names can no longer escape the storage root or traverse to arbitrary locations.

## HIGH-002 - Plugin/DLL loading hardening gaps

Problem
- Global `PATH` mutation and permissive load behavior increased side-loading risk.

Patch
- Removed process-wide `PATH` plugin-folder mutation.
- Added plugin root canonicalization and traversal rejection in plugin creation.
- Hardened Windows loader to use restricted DLL search flags (`LOAD_LIBRARY_SEARCH_DEFAULT_DIRS | LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR`) with compatibility fallback.

Files
- `src/ClassicUO.Client/Main.cs`
- `src/ClassicUO.Client/Network/Plugin.cs`
- `src/ClassicUO.Utility/Platforms/Native.cs`

Why this fixes it
- Plugin binaries are constrained to the intended plugin directory and native dependency resolution uses safer search rules.

## HIGH-003 - Weak reversible password storage

Problem
- Password persistence used reversible XOR obfuscation.

Patch
- Reworked `Crypter`:
  - Windows: DPAPI (`ProtectedData`) with `dpapi:` prefix.
  - Non-Windows: volatile in-memory encoding (`volatile:`) and no persisted secret.
  - Legacy decrypt compatibility retained for existing stored values.
- Added save-time password sanitation to avoid persisting non-secure values.

Files
- `src/ClassicUO.Utility/Crypter.cs`
- `src/ClassicUO.Client/Configuration/Settings.cs`
- `src/ClassicUO.Utility/ClassicUO.Utility.csproj` (added `System.Security.Cryptography.ProtectedData`)

Why this fixes it
- Stored secrets are OS-protected on Windows; insecure reversible-at-rest values are no longer written when secure storage is unavailable.

## MED-004 - WebSocket resource leak

Problem
- WebSocket wrapper owned disposable resources but did not dispose them.

Patch
- Implemented idempotent cleanup in `Disconnect`/`Dispose`:
  - cancel token
  - close/abort websocket as needed
  - await/settle receive task
  - dispose websocket, raw socket, token source
- Added null guards and receive-task tracking.

Files
- `src/ClassicUO.Client/Network/Socket/WebSocketWrapper.cs`

Why this fixes it
- Reconnect and teardown paths now release network and cancellation resources reliably.

## MED-005 - Connect crash on malformed IP input

Problem
- `Substring(0, 2)` on short IP strings could throw.

Patch
- Replaced substring check with safe prefix check: `StartsWith("ws", StringComparison.OrdinalIgnoreCase)`.

Files
- `src/ClassicUO.Client/Network/NetClient.cs`

Why this fixes it
- Empty/short/malformed input no longer causes substring exceptions.

## MED-006 - Marker parsing crashers

Problem
- Marker loaders used direct indexing and `int.Parse` without strict field validation.

Patch
- Added defensive parsing with `TryParse` and schema checks.
- Hardened XML/UOAM/CSV loaders to skip malformed rows instead of throwing.
- Added robust `TryParseMarker` helper used by CSV/user marker loading.
- Fixed empty-line behavior in CSV loader (`continue` instead of early `return`).

Files
- `src/ClassicUO.Client/Game/UI/Gumps/WorldMapGump.cs`

Why this fixes it
- Malformed files are now tolerated and ignored safely, preventing parser-driven crashes.

## LOW-007 - Plugin packet length trust

Problem
- Plugin callbacks could return invalid packet lengths used directly for copy/slice operations.

Patch
- Added centralized packet-length clamping/validation after each plugin callback.
- Added null-buffer handling and safe copy bounds for recv/send pipelines.

Files
- `src/ClassicUO.Client/Network/Plugin.cs`

Why this fixes it
- Invalid lengths no longer trigger out-of-range operations or unstable packet flow.

## LOW-008 - Unmanaged allocation leak in plugin host init

Problem
- `NativeMemory.AllocZeroed` allocation in plugin host init was not freed.

Patch
- Wrapped host init buffer usage in `try/finally` and released with `NativeMemory.Free`.

Files
- `src/ClassicUO.Client/PluginHost.cs`

Why this fixes it
- Native memory allocated for binding bootstrap is now released deterministically.

## Validation

Command
- `E:\dotnet 10\dotnet.exe test ClassicUO/ClassicUO.sln -c Release --nologo`

Result
- Passed: 162
- Failed: 0
- Skipped: 0

## End Result

- All report findings (CRIT-001 through LOW-008) have code-level remediation patches in this branch.
- Patch set compiles and test suite passes.

## Compatibility Validation Update (Latest Repo Snapshot)

Date checked: 2026-03-15

Scope requested:
- Browser
- Windows x64
- Linux x64
- macOS x64

What was run:
- `E:\dotnet 10\dotnet.exe build ClassicUO/ClassicUO.sln -c Release --nologo`
- `E:\dotnet 10\dotnet.exe test ClassicUO/ClassicUO.sln -c Release --nologo`
- `E:\dotnet 10\dotnet.exe publish src/ClassicUO.Client/ClassicUO.Client.csproj -c Release -r win-x64   -o dist-check/win-x64   /p:IS_DEV_BUILD=true /p:NativeLib=Shared /p:OutputType=Library /p:PublishAot=false`
- `E:\dotnet 10\dotnet.exe publish src/ClassicUO.Client/ClassicUO.Client.csproj -c Release -r linux-x64 -o dist-check/linux-x64 /p:IS_DEV_BUILD=true /p:NativeLib=Shared /p:OutputType=Library /p:PublishAot=false`
- `E:\dotnet 10\dotnet.exe publish src/ClassicUO.Client/ClassicUO.Client.csproj -c Release -r osx-x64   -o dist-check/osx-x64   /p:IS_DEV_BUILD=true /p:NativeLib=Shared /p:OutputType=Library /p:PublishAot=false`
- `E:\dotnet 10\dotnet.exe publish src/ClassicUO.Client/ClassicUO.Client.csproj -c Release -r browser-wasm -o dist-check/browser-wasm /p:IS_DEV_BUILD=true /p:NativeLib=Shared /p:OutputType=Library /p:PublishAot=false`

Results:
- `Build`: PASS
- `Tests`: PASS (`162/162`)
- `Windows x64 publish`: PASS
- `Linux x64 publish`: PASS
- `macOS x64 publish`: PASS
- `Browser (browser-wasm) publish`: FAIL on environment prerequisite (`NETSDK1147`, missing workloads `wasm-tools` and `wasm-tools-net8`)

Notes:
- Upstream CI/release workflows in this repo currently target `win-x64`, `linux-x64`, and `osx-x64` (not Browser) via `.github/workflows/deploy.yml`.
- Based on this check, the patch set does not require code changes for Windows/Linux/macOS x64 builds.
- Browser build requires workload/tooling setup before it can be validated further.
