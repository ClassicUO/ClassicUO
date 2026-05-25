# agent-desktop dev loop

Supervisor + CLI that brings up ModernUO and an `AGENT_BUILD` flavor of the
ClassicUO desktop client, then drives it over JSON-RPC on loopback. TCP to
an in-process server inside the desktop client.

Source under `Commands/` (one file per verb), `Services/` (process / pid
helpers), `RpcClient.cs`, `Program.cs`.

## TL;DR (the loop)

```bash
# one-time
dotnet build tools/agent-desktop/AgentDesktop.csproj
dotnet build src/ClassicUO.Client/ClassicUO.Client.csproj -p:AGENT_BUILD=true

# bring up rig (foreground; Ctrl-C tears down unless --persist)
dotnet tools/agent-desktop/bin/Debug/net10.0/agent-desktop.dll up --persist

# drive
dotnet tools/agent-desktop/bin/Debug/net10.0/agent-desktop.dll rpc-shot --out shot.png
dotnet tools/agent-desktop/bin/Debug/net10.0/agent-desktop.dll rpc-click --x 320 --y 295
dotnet tools/agent-desktop/bin/Debug/net10.0/agent-desktop.dll rpc-type --text admin
dotnet tools/agent-desktop/bin/Debug/net10.0/agent-desktop.dll script --file flow.json

# teardown
dotnet tools/agent-desktop/bin/Debug/net10.0/agent-desktop.dll down
```

`up` writes `<LocalAppData>/ClassicUO/agent/port.json` on success; every other
verb auto-reads that file if `--port` is omitted.

## ModernUO

Supervisor expects ModernUO already running on `127.0.0.1:2593`. Boot
manually from a published ModernUO checkout:

```bash
cd <modernuo-distribution-dir> && ./ModernUO.exe   # or ./ModernUO on linux/mac
```

Default test account: `admin` / `admin`. Server name advertised in the
server-select gump: `ModernUO`.

Clean rebuild of the server: `./publish.sh release` from the ModernUO repo root.

### Seed (not shipped yet, but planned)

Future `tools/agent-desktop/seed/` should hold an `AgentSeed.cs` hook
(copied into `Projects/UOContent/Misc/` before publish) + a baseline
`modernuo.json` (server config) + `expansion.json`. Pattern:

- `AgentSeed.cs` gated on `AGENT_SEED=1`; on `EventSink.ServerStarted`
  ensure account `admin` exists, create one character on slot 0, save world,
  `Core.Kill(false)` so the server self-exits.
- A `setup` verb clones ModernUO at a pinned SHA, copies the seed in, runs
  `publish.sh`, then boots once with `AGENT_SEED=1` to bake the account.

Until that lands, operator must have created `admin/admin` through the
first-boot wizard.

## Client settings

Client reads `settings.json` from the **current working directory**
(`Environment.CurrentDirectory`). `agent-desktop up` cds into the repo root
before spawning, so the repo-level `settings.json` is what gets loaded.

Pin these in repo-level `settings.json`:

```json
"window_position": { "X": 100, "Y": 100 },
"window_size":     { "X": 800, "Y": 600 },
"is_win_maximized": false,
"ip":   "127.0.0.1",
"port": 2593,
"ultimaonlinedirectory": "<path-to-uo-classic-install>",
"clientversion": "7.0.115.0"
```

Fixed windowed size keeps inputs deterministic across machines.

## Commands

One file per verb in `Commands/<Verb>.cs`, wired in `Program.cs`. Run
`--help` on each for current flags.

| Verb | Source | Notes |
|---|---|---|
| `up [--persist] [--ready-timeout-ms N]` | `Commands/UpCommand.cs` | spawns `bin/agent/net10.0/cuo.agent.dll`, polls `port.json`, probes `lifecycle.ping`. `--persist` writes pids and returns; foreground blocks until SIGINT |
| `down` | `Commands/DownCommand.cs` | reads pids, kills the rig |
| `ping` | `Commands/PingCommand.cs` | round-trips `lifecycle.ping` |
| `smoke [--attach]` | `Commands/SmokeCommand.cs` | smoke scenario; `--attach` reuses a persisted rig |
| `script --file <path.json>` | `Commands/ScriptCommand.cs` | runs a JSON array of steps over one connection; much faster than chaining one-shot CLI calls |
| `rpc-mouse-move --x --y` | `Commands/RpcInputCommands.cs` | `input.mouseMove` |
| `rpc-click --x --y [--button]` | `Commands/RpcInputCommands.cs` | `input.mouseClick` |
| `rpc-double-click --x --y` | `Commands/RpcInputCommands.cs` | `input.mouseDoubleClick` — see pitfall |
| `rpc-mouse-hold` / `rpc-mouse-release` | `Commands/RpcInputCommands.cs` | press / release without auto-release |
| `rpc-input-clear` | `Commands/RpcInputCommands.cs` | resets synth state |
| `rpc-type --text <s>` | `Commands/RpcInputCommands.cs` | `input.type` |
| `rpc-shot --out <path>` | `Commands/RpcInputCommands.cs` | `capture.shot`; FNA `GetBackBufferData` + StbImageWrite. Path absolute or relative to client CWD |
| `rpc-double-click-serial --serial <hex\|dec\|'player'>` | `Commands/RpcInputCommands.cs` | `input.doubleClickSerial`; only valid in-world |

### Script step shapes

```jsonc
{ "verb": "<rpc.verb>", "params": { ... }, "queueWait": N }   // RPC; sleep queueWait*frameMs after
{ "sleepMs": N }                                              // wall sleep
{ "poll": { "verb": "lifecycle.inWorld", "field": "inWorld",
            "expect": true, "timeoutMs": 15000, "intervalMs": 250 } }
{ "log": "marker" }                                           // echo to stdout
```

Each step prints a JSON line to stdout. Final line:
`{"status":"script-ok","steps":N}`.

## RPC verbs (server side)

Registered in `src/ClassicUO.Client/Agent/Handlers/*Handlers.Registration.cs`.
Full constants in `src/ClassicUO.Agent.Contracts/RpcVerbs.cs`. Highlights:

- `lifecycle.ping` / `lifecycle.inWorld` / `lifecycle.ready` / `lifecycle.shutdown`
- `world.dumpState` / `world.getPlayer`
- `input.mouseMove` / `input.mouseClick` / `input.mouseDoubleClick` /
  `input.mouseHold` / `input.mouseRelease` / `input.clear`
- `input.type` — text input via the focused control
- `input.doubleClickSerial` — skip pixel hit-test; `serial` accepts hex
  (`0x40000001`), decimal, or `"player"`
- `agent.login` — auto-pick server 0 + character 0
- `gump.tree` / `gump.dump` / `gump.close`
- `capture.shot` — deferred; reply arrives after the next render tick

## Pitfalls

- **`input.mouseDoubleClick` too fast for `InteractionSystem`.** Queues 4
  synthetic frames back-to-back; the press/release edge detector misses
  the second `UiClick`. For double-click use two `input.mouseClick` calls
  ~200 ms apart.
- **`input.type` on legacy SDL2 handler.** If the committed handler
  P/Invokes SDL2 but FNA is on SDL3, events go to a queue nothing reads.
  Fix: bypass SDL and call `UIManager.KeyboardFocusControl.InvokeTextInput`
  + `Scene.OnTextInput` directly. See
  `src/ClassicUO.Client/Agent/Handlers/InputHandlers.cs`.
- **Stale `port.json`.** `up` deletes it before spawning; standalone
  `dotnet bin/agent/net10.0/cuo.agent.dll` spawns leave it behind. Delete
  manually before re-spawning if the rig won't come back up.

## Build flavor switch

All agent code gated on `AGENT_BUILD` (defined by
`ClassicUO.Agent.Settings.props` when `-p:AGENT_BUILD=true`). Prod build
strips `src/ClassicUO.Client/Agent/`, outputs the standard client
assembly in `bin/Debug/` or `bin/Release/` (`cuo.dll` for `dotnet run`
builds; `cuo.exe` on Windows / `cuo` on Linux/macOS for AOT/self-contained
publish). Agent flavor outputs to `bin/agent/net10.0/cuo.agent.dll`.

Rebuild after C# edits:
```bash
dotnet build src/ClassicUO.Client/ClassicUO.Client.csproj -p:AGENT_BUILD=true
```

## Service diagnostics

- Port file: `<LocalAppData>/ClassicUO/agent/port.json` (`{"port":N,"pid":N}`).
  `<LocalAppData>` = `Environment.SpecialFolder.LocalApplicationData` (Windows
  `%LOCALAPPDATA%`, Linux `~/.local/share`, macOS `~/Library/Application Support`).
- Pid file (after `up --persist`): `tools/agent-desktop/.runtime/pids.json`
  (gitignored via `tools/agent-desktop/.gitignore`). Created by `up --persist`,
  consumed and removed by `down`. Schema: `{"client":{"pid":N,"port":N}}`.
- `.runtime/` is the only on-disk state the CLI writes inside the repo;
  no client stdout/stderr is logged to disk. `ClientProcess` keeps a 64-line
  in-memory ring buffer of each stream for inclusion in spawn-failure errors.
  If you need a full client log, redirect at the shell level when calling `up`.
- Stuck rig:
  - Windows: `tasklist //FI "IMAGENAME eq dotnet.exe"` then `taskkill //PID <pid> //F`
  - Linux/macOS: `pgrep -f cuo.agent.dll` then `kill <pid>` (or `kill -9` if needed)

## Useful references

- Agent server core: `src/ClassicUO.Client/Agent/AgentServer.cs`
- Per-frame plugin (drains synthetic mouse/text, services capture):
  `src/ClassicUO.Client/Agent/AgentServerPlugin.cs`
- Dispatcher route table: `src/ClassicUO.Client/Agent/AgentDispatcher.cs`
- Handlers: `src/ClassicUO.Client/Agent/Handlers/`
- Verb constants: `src/ClassicUO.Agent.Contracts/RpcVerbs.cs`
- Synthetic mouse bridge: `src/ClassicUO.Client/Ecs/Engine/Inputs/MouseContext.cs` (AGENT_BUILD branch)
- Build flavor toggle: `ClassicUO.Agent.Settings.props`
- Design doc: `docs/agent-desktop/design.md`

## Security boundary

Every agent surface lives under `#if AGENT_BUILD`. Default build
(`dotnet build src/ClassicUO.Client`) compiles it out entirely — prod
artefact contains no `AgentServer`, no `RpcVerbAttribute`, no synthetic
mouse hooks. Do not move automation logic out of the `AGENT_BUILD` gate.
