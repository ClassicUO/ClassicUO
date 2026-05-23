# Desktop Agent Dev Loop — Design

**Status:** Draft (not committed). Counterpart to the web agent loop in
`classicuo-wasm/web/apps/agent/` (see its `AGENTS.md`).

## Goal

Reproduce the behaviour of the web agent dev loop for a native Windows
desktop build of ClassicUO. An LLM or human agent edits C# game code,
rebuilds, restarts the client, and verifies the change against the running
game via JSON-RPC — without any web stack, browser, or WASM toolchain
involvement.

## Decisions

| Decision | Choice |
|---|---|
| Repo location | This (upstream) repo. CLI under `tools/agent-desktop/`. Agent server under `src/ClassicUO.Client/Agent/` (new directory, gated). The wasm fork stays untouched. |
| CLI language | .NET 9 console app, System.CommandLine. |
| Target platform | Windows only (FNA + Windows.Graphics.Capture). |
| Observation model | In-process automation surface (TCP/JSON-RPC) + `Windows.Graphics.Capture` for screenshots / `.mp4` recordings. No OS-level input injection. |
| Server lifecycle | `agent-desktop up` spawns ModernUO, then spawns the desktop client (agent flavor), then waits for `lifecycle.ready`. |
| Edit→sync policy | Always restart. No .NET Hot Reload. |
| AGENT_BUILD gate | New `-p:AGENT_BUILD=true` MSBuild flag. Defines the `AGENT_BUILD` constant. Produces a separate assembly (`cuo.agent`) in `bin/agent/`. Prod builds (`bin/Release/cuo`) never contain agent symbols. |

## Architecture

```
                  ┌─────────────────────────────────────────┐
                  │  tools/agent-desktop/                   │
                  │  .NET 9 console app                     │
                  │  System.CommandLine                     │
                  └────────────────┬────────────────────────┘
                                   │ JSON-RPC 2.0 over TCP
                                   │ (127.0.0.1:<port>, newline-framed)
                                   ▼
  ┌──────────────────┐   ┌─────────────────────────────────────┐
  │  ModernUO        │◀──│  cuo.agent.exe  (Windows process)   │
  │  (server)        │   │  built with -p:AGENT_BUILD=true     │
  │  spawned by      │   │  AgentServer wired into the         │
  │  `agent-desktop  │   │  TinyEcs Scheduler as a plugin      │
  │   up`            │   │  Windows.Graphics.Capture sidecar   │
  └──────────────────┘   └─────────────────────────────────────┘
```

## Key context: the impl/ecs refactor

This branch is mid-refactor to a TinyEcs-based architecture. `Main.cs`
no longer calls `Client.Run(pluginHost);` — that's commented out. Instead:

```csharp
using var ecs = new TinyEcs.World();
var scheduler = new Scheduler(ecs);
scheduler.AddPlugin<Ecs.CuoPlugin>();
scheduler.Run(() => false);
```

The agent server therefore plugs into the **scheduler**, not the legacy
`GameController` lifecycle. The TinyEcs plugin pattern observed in
`Ecs/CuoPlugin.cs`, `Ecs/Network/NetworkPlugin.cs`, etc. is:

```csharp
internal readonly struct AgentServerPlugin : IPlugin
{
    public void Build(Scheduler scheduler)
    {
        scheduler.AddResource(new AgentServerState());
        scheduler.AddSystem(StartAcceptLoop, Stages.Startup);    // spin up background thread
        scheduler.AddSystem(DrainInbox);                          // per-frame, reads RPC requests + dispatches
        scheduler.AddSystem(FlushOutbox, threadingType: ThreadingMode.Single);
    }
}
```

The split between thread-of-execution-of-the-TCP-listener and
thread-of-execution-of-the-ECS-handlers is the critical design point:

1. **Background thread** (a `Task` started in `Stages.Startup`) owns the
   `TcpListener`, reads JSON-RPC frames off the socket, deserialises
   them, and pushes `RpcRequest` records into a `Channel<RpcRequest>`
   inbox. Never touches engine state.
2. **Per-frame ECS system** (`DrainInbox`) reads from the inbox each
   frame. For each verb it routes to a handler — handlers are themselves
   ECS-aware: they take `TinyEcs.World`, `Res<GameContext>`,
   `Res<Settings>`, etc. parameters and produce an `RpcResponse` which
   goes into a `Channel<RpcResponse>` outbox.
3. **Background thread** drains the outbox and writes responses + events
   back to the socket.

This split means every handler runs on the main game thread, so reading
`World.Player`, the gump tree, etc. is safe. The background thread only
ever touches the two channels.

Event push (`journalEntry`, `equipmentChanged`, etc.) works the same way:
an event-emitting system in the engine writes an `RpcEvent` to the
outbox; the background thread serialises and sends.

**No remaining unknowns on this front** — the pattern is straightforward
once the plugin shape is clear.

## CLI verb map

Same shape as the web `./agent`, minus the browser/HMR pieces.

| Verb | Notes |
|---|---|
| `setup` | Build ModernUO + agent-flavor client + seed shard + write `agent.local.json`. |
| `up [--persist]` | Spawn ModernUO → spawn client → wait for `lifecycle.ready`. |
| `down` | Idempotent teardown from `.runtime/pids.json`. |
| `build [--config Debug\|Release]` | `dotnet build src/ClassicUO.Client -p:AGENT_BUILD=true`. Witness-mtime staleness check, same convention as the web `wasm build`. |
| `restart-client` | Kill + relaunch the client only; ModernUO stays up. |
| `smoke [--attach]` | Login + walk 5 tiles east + shot. |
| `attach <script.cs>` | Roslyn-scripting host (`Microsoft.CodeAnalysis.CSharp.Scripting`). |
| `run <stmt>` | One-liner against the Roslyn host. |
| `scenario <name>` | Scenario module under `tools/agent-desktop/Scenarios/`. |
| `loop <scenario>` | Single-iteration JSON output. |
| `gump-tree` / `expect-gump` | YAML snapshot of open gumps; the ARIA / expect-aria analogue. |
| `shot <GumpName\|--rect x,y,w,h\|--window>` | PNG screenshot. |
| `record-start <label>` / `record-stop` | `.mp4` screencast (Media Foundation H.264). |
| `journal tail [--filter regex]` | Stream journal events. |
| `walk <dx> <dy>` | High-level walk. |
| `target <serial>` / `gump close <serial>` | Direct game actions. |
| `dump-state [--out path]` | One-shot `world.dumpState`. |
| `ping` | Smallest sanity check. |

Dropped from the web set: `ab *` (no browser), `hmr *` (no HMR),
`react *` (no React).

## Transport

JSON-RPC 2.0, newline-framed, loopback-only, single concurrent connection.

Request:
```json
{"jsonrpc":"2.0","id":42,"method":"world.dumpState","params":{}}
```

Server-pushed event (no `id`):
```json
{"jsonrpc":"2.0","method":"event","params":{"name":"journalEntry","data":{...}}}
```

Port advertised at `%LOCALAPPDATA%\ClassicUO\agent\port.json` so the CLI
can find the server without parsing stdout.

DTO field naming matches `classicuo-wasm/web/packages/ai/src/world.ts`
(camelCase). Seed scripts and assertion shapes transfer between the two
loops.

## Build flavor

New `ClassicUO.Agent.Settings.props` at the repo root (peer of any existing
`*.Settings.props`), imported by `src/ClassicUO.Client/ClassicUO.Client.csproj`:

```xml
<PropertyGroup Condition="'$(AGENT_BUILD)' == 'true'">
  <DefineConstants>$(DefineConstants);AGENT_BUILD</DefineConstants>
  <AssemblyName>cuo.agent</AssemblyName>
  <OutputPath>$(ProjectDir)..\..\bin\agent\</OutputPath>
</PropertyGroup>
```

CI grep rule on the prod publish output: fails if the assembly contains
`ClassicUO.Agent.AgentServerPlugin` or `RpcVerbAttribute`.

## Repo layout

```
src/
├── ClassicUO.Client/
│   ├── ClassicUO.Client.csproj                  (modified: import agent props)
│   └── Agent/                                   (new, #if AGENT_BUILD)
│       ├── AgentServerPlugin.cs                 (ECS plugin entry)
│       ├── AgentServer.cs                       (TcpListener accept loop)
│       ├── AgentDispatcher.cs                   (verb routing, JSON-RPC framing)
│       ├── Handlers/
│       │   ├── Lifecycle.cs
│       │   ├── World.cs
│       │   ├── Input.cs
│       │   ├── Login.cs
│       │   ├── Events.cs
│       │   ├── Gump.cs
│       │   └── Capture.cs
│       └── EngineHooks.cs                       (scheduler/system wiring)
└── ClassicUO.Agent.Contracts/                   (new shared project)
    ├── ClassicUO.Agent.Contracts.csproj
    ├── Dto/{Player,WorldState,Mobile,Gump,Journal}Dto.cs
    └── RpcVerbs.cs

tools/agent-desktop/                             (CLI, already scaffolded)
├── AgentDesktop.csproj
├── Program.cs
└── Commands/
    ├── UpCommand.cs
    ├── DownCommand.cs
    ├── SmokeCommand.cs
    └── PingCommand.cs

docs/agent-desktop/
└── design.md                                    (this file)

ClassicUO.Agent.Settings.props                   (new, repo root)
```

## Observation: screen capture

All capture happens inside the client process via:
- `Windows.Graphics.Capture` for frame acquisition (works on Windows 10
  1903+; FNA HWND obtained from inside the client).
- `Windows.Graphics.Imaging.BitmapEncoder` for PNG shots.
- `MediaFoundation.SinkWriter` (H.264/AAC) for `.mp4` recordings — note
  this differs from the web loop's `.webm`; users wanting webm can run
  ffmpeg afterward. Not shipped.

Single in-flight recording, mutex-guarded. Encoder runs on a dedicated
worker thread; game thread is never blocked.

## Lifecycle & errors

- **Startup ordering:** ModernUO → wait for port 2593 → cuo.agent.exe →
  RPC `lifecycle.ready` (10 s default).
- **Readiness signal:** emitted after asset loading + first frame.
- **PIDs persistence:** `tools/agent-desktop/.runtime/pids.json`.
- **Single-rig invariant:** lockfile at `.runtime/rig.lock`.
- **Crash recovery:** if RPC fails and the client process is dead, the
  CLI emits `{ error: "client_crashed", lastLog: ".logs/client.log tail" }`
  and exits 1. No auto-restart in v1.
- **Logs:** per-service rotated logs under `.logs/`.

## Out of scope

- Linux / macOS.
- .NET Hot Reload.
- Multi-client agent control.
- HTTP / WebSocket transport.
- Cloud sync of artefacts.
- `.webm` output.
- A scripting sandbox (`attach <script.cs>` runs full-trust Roslyn).

## Open questions

1. ~~ECS plugin shape.~~ **Resolved.** Pattern documented above.
2. **World-state access on the ECS branch.** The verb `world.dumpState`
   needs to enumerate the player, nearby mobiles, journal lines, open
   gumps. On the ECS branch these are presumably ECS entities + resources.
   Mapping from DTO field names (matching the web schema) to ECS queries
   needs an inventory pass before the first useful handler can be written.
3. **Settings / asset paths.** Upstream reads UO assets from a directory
   configured in `settings.json`. The `agent-desktop setup` step needs to
   either point at a known dev installation or vendor a minimal asset
   set for the agent flow.
4. **CI runner availability.** The end-to-end smoke needs a Windows
   runner. Upstream's CI configuration is not yet inspected for this.
5. ~~Login flow on impl/ecs.~~ **Investigated.** Confirmed shape:
   - `struct OnLoginRequest { Username, Password, Address, Port }` is
     declared in `Ecs/Network/NetworkPlugin.cs:17`.
   - `NetworkPlugin.Build` registers it as an event (`scheduler.AddEvent<OnLoginRequest>()`)
     and runs `HandleLoginRequests` (a `ThreadingMode.Single` system) when
     the reader is non-empty. The handler calls `NetClient.Connect` and
     `Send_Seed` → `Send_Login`.
   - `LoginScreenPlugin.Login` is the GUI-side caller that emits the
     event after writing username/password to `Settings`.

   **agent.login design:** the dispatcher's pure-handler signature can't
   reach `EventWriter<OnLoginRequest>`. Two options:

   (a) **Intent channel pattern.** Add a `Channel<LoginIntent>` to
       `AgentServerState`. The agent.login handler writes to it and
       returns `{dispatched: true}`. A separate per-frame system in
       `AgentServerPlugin` takes `EventWriter<OnLoginRequest>` +
       `Res<Settings>`, drains the channel, encrypts password, and
       emits the event. Handlers stay as pure functions.

   (b) **ECS-aware dispatcher.** Make `DrainInbox` take ECS parameters
       (`TinyEcs.World`, `Res<>`s, `EventWriter<>`s) and pass an
       `AgentRpcContext` struct to each handler. More natural for any
       verb that needs ECS, but couples every new verb to the
       dispatcher's signature.

   **Recommendation:** (b). Handlers will need read access to the ECS
   world for `world.dumpState`, `gump.tree`, etc. anyway; building the
   context once in `DrainInbox` is cleaner than threading channels per
   verb. Login becomes just one of many ECS-aware handlers.

6. **DrainInbox ECS context plumbing (new, follow-up to #5).** When
   implementing the first write-side verb (likely `agent.login`),
   refactor `AgentDispatcher.DrainInbox` from a pure function into a
   TinyEcs system. Pass an `AgentRpcContext { World, Settings, GameCtx,
   EventWriters... }` to handlers. Update `LifecycleHandlers.Ping` (and
   any future read-side verbs) to accept the context.
