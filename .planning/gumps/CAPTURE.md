# Reference Capture — Status

Source of truth = **ClassicUO.Client** (legacy OOP, `cuo.agent.dll`), driven over JSON-RPC
against ModernUO (`127.0.0.1:2593`, `admin/admin`). Window pinned 800×600 @ (100,100) for
deterministic coords (see `settings.json`; backed up to `settings.json.capturebak`).

## Rig recipe (reproduce)

```bash
# 1. ModernUO up
cd C:/dev/ModernUO/Distribution && ./ModernUO.exe        # background; listens :2593

# 2. build + bring up legacy agent client
dotnet build src/ClassicUO.Client/ClassicUO.Client.csproj -p:AGENT_BUILD=true
dotnet build tools/agent-desktop/AgentDesktop.csproj
A="dotnet tools/agent-desktop/bin/Debug/net10.0/agent-desktop.dll"
$A up --persist --ready-timeout-ms 90000

# 3. login + in-world
$A script --file .planning/gumps/_login.json     # agent.login admin/admin + poll inWorld

# 4. drive + shoot
$A rpc-click --x <X> --y <Y>
$A rpc-shot  --out .planning/gumps/<slug>/reference.png
$A down                                            # teardown
```

Legacy build registers only: `agent.login`, `capture.*`, `input.*`, `lifecycle.*`, `world.*`.
**No `gump.tree`/`gump.dump`** — control bounds are not introspectable; capture is by
coordinate click + screenshot feedback loop.

## Solved coordinates (800×600, this NEW-LEGACY 7.0.115.0 build)

- **Paperdoll window origin ≈ (115, 67)**; opens on login. Button column local `X=185, Y=44+27·n`:
  Help n0 (~305,117) · **Options n1 (~305,144)** · LogOut n2 · Journal/Quests n3 (~305,198) ·
  **Skills n4 (~305,225)** · Guild n5.
- Right-click anywhere on a window closes it (shared contract).

## Captured (reference.png in each slug dir)

| Gump | status | notes |
|------|--------|-------|
| skills | ✅ captured | Opens via paperdoll Skills (305,225). Groups collapsed → mostly-empty scroll (0x1F40). Expand a group header for populated variant. |
| options | ✅ captured | Opens via paperdoll Options (305,144). Sparse render at this scale. |
| _refs/paperdoll_baseline.png | ✅ | Paperdoll itself (already ported in ECS) — baseline. |

## Not captured — blocked / deferred

| Gump | blocker |
|------|---------|
| minimap | Top-bar **Map** button coords not nailed (no `gump.tree`); blind clicks along y≈8 missed. Retry: enumerate top-bar button x-positions from `TopBarGump.cs` layout, or wire a temp open-RPC. |
| journal | Top-bar Journal stub; paperdoll Journal button (305,198) opens legacy JournalGump — retry capture. Needs prior chat text to show entries. |
| statusbar-healthbar | Status opens from health-bar double-click / paperdoll; self health bar needs spawn trigger. |
| buff | Needs a self-buff cast (0xDF) — requires castable spell + reagents. |
| counterbar | Opened from Options→Counters; no clean standalone trigger. |
| macro | Options→Macros tab. |
| party | Paperdoll party-manifest pic double-click; empty manifest solo. |
| spellbook | **Needs GM-spawned filled spellbook** in backpack (`[add spellbook`). Harness has no GM-command channel. |
| shop | **Needs vendor NPC** (`[add Provisioner`). |
| trading | **Needs 2nd logged-in player** + secure-trade init, or injected 0x6F type 0x00. |

To unblock Tier 2/3: add a ModernUO seed (AgentSeed.cs per AGENTS.md) that spawns a filled
spellbook + a vendor near the test char, or add a harness GM-command verb.

## Next step

Specs are the design contract (Client source = truth). Review `INDEX.md` + per-gump `SPEC.md`,
then implement gump-by-gump in CUO.Ecs (separate per-gump workflow), capturing the ECS render
with the **ECS** harness (`src/ClassicUO.Ecs/Agent/`) and diffing vs these references.
