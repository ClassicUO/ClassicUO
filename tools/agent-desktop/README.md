# agent-desktop

Supervisor CLI for the ClassicUO desktop agent dev loop. Counterpart to
the web agent at `https://github.com/andreakarasho/classicuo-wasm` →
`web/apps/agent/`. Same verb shape, same JSON-RPC contract; different
transport (TCP on loopback to an in-process server inside the desktop
client, instead of Playwright + agent-browser against a Chromium tab).

## Status

**Scaffold only.** Every verb prints `{"status":"unimplemented"}`. See
`docs/agent-desktop/design.md` for the full design.

## Build

```bash
dotnet build tools/agent-desktop/AgentDesktop.csproj
```

## Layout

```
tools/agent-desktop/
├── AgentDesktop.csproj
├── Program.cs                    System.CommandLine root
└── Commands/
    ├── UpCommand.cs              up [--persist]
    ├── DownCommand.cs            down
    ├── SmokeCommand.cs           smoke [--attach]
    └── PingCommand.cs            ping
```

## Where the agent server will live

Inside `src/ClassicUO.Client/`, in a new `Agent/` directory gated on the
`AGENT_BUILD` MSBuild constant. The server hooks into the ECS scheduler
(`src/ClassicUO.Client/Ecs/CuoPlugin.cs`), not the legacy `GameController`
boot path that the `impl/ecs` branch has retired.

See `docs/agent-desktop/design.md` for the full picture.
