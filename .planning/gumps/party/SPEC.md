# Party Gump Spec

## Overview

Two client-side gumps backing the UO party system:

1. **PartyGump** ("Party Manifest") — a 450x480 fixed-background window listing up
   to 10 party member slots. Each slot has a **Tell** button, an optional **Kick**
   button (leader only), a member-name backdrop sprite, and the member's name. The
   footer carries action buttons: Send-message, Loot-type toggle, Leave/Disband,
   Add-new-member (leader only), and OK / Cancel. It is entirely client-driven: it
   never receives its own server packet, it reads `PartyManager` state. It opens by
   left-double-clicking the **party-manifest** profile pic on the player's paperdoll,
   or via the `PartyManifest` macro.

2. **PartyInviteGump** — a small translucent (alpha 0.8) prompt that appears when the
   server sends a party invite (general-info packet `0xBF` subcommand `0x07`). Shows
   "{name} has invited you to join a party." with **Accept** / **Decline** buttons.
   It is server-triggered.

Neither gump nor its supporting `PartyManager` state exists in `ClassicUO.Ecs` yet.
The 0xBF party subcommand (`0x06`) and invite subcommand (`0x07`) are NOT parsed in
the ECS `OnExtendedCommandPacket_0xBF` handler — this spec includes the
networking + state work needed before the gump can show real data.

## Source of truth

| File | Lines | Role |
|------|-------|------|
| `src/ClassicUO.Client/Game/UI/Gumps/PartyGump.cs` | 12-414 | Party Manifest window |
| `src/ClassicUO.Client/Game/UI/Gumps/PartyInviteGump.cs` | 11-84 | Invite prompt |
| `src/ClassicUO.Client/Game/Managers/PartyManager.cs` | 14-218 | Party state + 0x06/0x07 parse (`ParsePacket`) |
| `src/ClassicUO.Client/Network/PacketHandlers.cs` | 4192-4195 | `case 6: world.Party.ParsePacket` |
| `src/ClassicUO.Client/Network/OutgoingPackets.cs` | 2049-2208 | `Send_Party*` request senders |
| `src/ClassicUO.Client/Game/GameActions.cs` | 407-437 | `RequestParty*` wrappers |
| `src/ClassicUO.Client/Game/UI/Gumps/PaperdollGump.cs` | 361-373 | party-manifest pic double-click → open PartyGump |
| `src/ClassicUO.Client/Game/Managers/MacroManager.cs` | 599-610 | `PartyManifest` macro → open PartyGump |

Key methods:
- `PartyGump.BuildGump()` (lines 36-270) — the full control tree.
- `PartyGump.OnButtonClick(int)` (lines 272-402) — button semantics.
- `PartyGump.Buttons` enum (lines 404-414) — `OK=0, Cancel=1, SendMessage=2,
  LootType=3, Leave=4, Add=5, TellMember=6, KickMember=16` (TellMember+10).
- `PartyInviteGump` ctor (lines 13-83) — control tree + Accept/Decline `MouseUp`.
- `PartyManager.ParsePacket` (lines 32-191) — codes 1/2 (add/list), 3/4 (party
  message), 7 (invite). 10 member slots.

## Visual structure

### PartyGump (450 x 480)

OOP origin: `X = ClientBounds.Width/2 - 272`, `Y = ClientBounds.Height/2 - 240`
(MacroManager.cs:605-606 / PaperdollGump.cs:368-369). Coordinates below are
relative to the window root (the ResizePic at 0,0).

- **Background** — `ResizePic 0x0A28`, W=450, H=480 (nine-patch / scalable).
- **Header labels** (font 1 = ASCII-ish, hue `0x0386`):
  - "Tell"  — Label, x=40, y=30, font 1, hue 0x0386
  - "Kick"  — Label, x=80, y=30, font 1, hue 0x0386
  - "Party Manifest" — Label, x=153, y=20, font 2, hue 0x0386
- **10 member rows** (loop i=0..9, `yPtr` starts 48, +25 per row → 48,73,98,…,273):
  - **Tell button** — `Button(TellMember+i)` gumps `(normal 0x0FAB, pressed 0x0FAD,
    over 0x0FAC)`, x=40, y=yPtr+2, action=Activate.
  - **Kick button** *(only if `isLeader`)* — `Button(KickMember+i)` gumps
    `(0x0FB1, 0x0FB3, 0x0FB2)`, x=80, y=yPtr+2, action=Activate.
  - **Name backdrop** — `GumpPic 0x0475`, x=130, y=yPtr, hue 0.
  - **Member name** — Label, x=140, y=yPtr+1, font 2, hue 0x0386, maxwidth=250,
    align=CENTER. Text = `Party.Members[i].Name` or "" if empty.
- **Footer**:
  - **Send-message button** — `Button(SendMessage)` `(0x0FAB, 0x0FAD, 0x0FAC)`,
    x=70, y=307, Activate.
  - Label "Send the party a message" — x=110, y=307, font 2, hue 0x0386.
  - **Loot-type button** — `Button(LootType)`, x=70, y=334, Activate.
    - if `CanLoot`: gumps `(0x0FA2, 0x0FA2, 0x0FA2)` + label "Party can loot me".
    - else: gumps `(0x0FA9, 0x0FA9, 0x0FA9)` + label "Party CANNOT loot me".
    - label x=110, y=334, font 2, hue 0x0386.
  - **Leave button** — `Button(Leave)` `(0x0FAE, 0x0FB0, 0x0FAF)`, x=70, y=360,
    Activate.
    - label "Leave the party" (if `isMember`) OR "Disband the party" (else),
      x=110, y=360, font 2, hue 0x0386.
  - **Add button** *(only if `isLeader`)* — `Button(Add)` `(0x0FA8, 0x0FAA, 0x0FA9)`,
    x=70, y=385, Activate.
    - label "Add New Member", x=110, y=385, font 2, hue 0x0386.
  - **OK button** — `Button(OK)` `(normal 0x00F9, pressed 0x00F8, over 0x00F7)`,
    x=130, y=430, Activate.
  - **Cancel button** — `Button(Cancel)` `(0x00F3, 0x00F1, 0x00F2)`, x=236, y=430,
    Activate.

Leader/member flags (PartyGump.cs:74-75):
- `isLeader = Party.Leader == 0 || Party.Leader == Player`
- `isMember = Party.Leader != 0 && Party.Leader != Player`

### PartyInviteGump (translucent prompt)

`nameWidthAdjustment = (mobile==null || name.Length < 10) ? 0 : name.Length * 5`.
All X positions are screen-absolute (camera-centered, not a window root).

- **Background** — `AlphaBlendControl` (NOT a gump sprite): W=270+adj, H=80,
  X=`CameraBounds.Width/2 - 125`, Y=150, Alpha=0.8 (translucent black box).
- **Text** — Label "{name} has invited you to join a party." (unicode, hue 15),
  X=`CameraBounds.Width/2 - 115`, Y=165. Name falls back to "No Name" when empty.
- **Accept button** — `NiceButton` (text button), at X=`CamW/2 + 99 + adj`, Y=205,
  W=45, H=25, label "Accept", action=Activate.
- **Decline button** — `NiceButton`, X=`CamW/2 + 39 + adj`, Y=205, W=45, H=25,
  label "Decline", action=Activate.

## Assets

| Asset | ID | Used by |
|-------|----|---------|
| ResizePic bg | `0x0A28` | PartyGump window background (nine-patch) |
| Name backdrop GumpPic | `0x0475` | each member row name plate |
| Tell button | `0x0FAB` / `0x0FAD` / `0x0FAC` | normal/pressed/over |
| Kick button | `0x0FB1` / `0x0FB3` / `0x0FB2` | normal/pressed/over |
| Send-message button | `0x0FAB` / `0x0FAD` / `0x0FAC` | same triplet as Tell |
| Loot ON button | `0x0FA2` (all 3 states) | "can loot" state |
| Loot OFF button | `0x0FA9` (all 3 states) | "cannot loot" state |
| Leave button | `0x0FAE` / `0x0FB0` / `0x0FAF` | normal/pressed/over |
| Add-member button | `0x0FA8` / `0x0FAA` / `0x0FA9` | normal/pressed/over |
| OK button | `0x00F9` / `0x00F8` / `0x00F7` | normal/pressed/over |
| Cancel button | `0x00F3` / `0x00F1` / `0x00F2` | normal/pressed/over |
| Text hue | `0x0386` | every PartyGump label |
| Invite text hue | `15` | PartyInviteGump label (unicode) |
| Fonts | font 1 (Tell/Kick headers), font 2 (everything else in PartyGump) | |

String resources (from `ResGumps.resx`, mirror in `src/ClassicUO.Ecs/Resources/`):
`Tell`="Tell", `Kick`="Kick", `PartyManifest`="Party Manifest",
`SendThePartyAMessage`="Send the party a message", `PartyCanLootMe`="Party can loot me",
`PartyCannotLootMe`="Party CANNOT loot me", `LeaveTheParty`="Leave the party",
`DisbandTheParty`="Disband the party", `AddNewMember`="Add New Member",
`YouAreNotInAParty`="You are not in a party.",
`ThereIsNoOneInThatPartySlot`="There is no one in that party slot.",
`Accept`="Accept", `Decline`="Decline",
`P0HasInvitedYouToParty`="{0} has invited you to join a party.", `NoName`="No Name".

## Behaviors

| Behavior | OOP source | ECS mechanism |
|----------|-----------|---------------|
| Drag to move | `CanMove = true` | `UIMovable` on root (UOGumpBundle); WindowDragPlugin.Drag |
| Right-click closes | `CanCloseWithRightClick = true` | `UIMovable`; WindowDragPlugin.CloseOnRightClick (despawn subtree) |
| Topmost on click | gump z | root `GlobalZIndex` bumped via `UiZCounter` on latch |
| Click-capture to world | base Gump | falls out of `ClaimSelectedFromMovable` (UIMovable, no NetworkSerial) |
| Pixel-perfect hit-test | base | `UiHitTest.PixelHit`; bg is `GumpNinePatch` (solid-fill bbox — no new case needed) |
| Buttons fire on release | `ButtonAction.Activate` (mouse-up) | `On<UiClick>` observer per button |
| Tell button | sets system-chat text `"/{index+1} "` | observer → set chat input text (see Open Q) |
| Kick button | `Send_PartyRemoveRequest(member.Serial)` | observer → ECS `Send_PartyRemoveRequest` |
| Empty-slot Tell/Kick | print "There is no one in that party slot." | observer guards on member serial==0 |
| Send-message button | `/`→chat OR "You are not in a party." | observer; print when `Leader==0` |
| Loot-type button | toggles local `CanLoot` + `RequestUpdateContents()` | observer flips a `Res<PartyState>`/window field + rebuild |
| Leave button | `RequestPartyQuit(Player)` → `Send_PartyRemoveRequest(playerSerial)` | observer; print when not in party |
| Add button | `Send_PartyInviteRequest()` | observer (leader only) |
| OK button | if `CanLoot` changed: `Send_PartyChangeLootTypeRequest`; then close | observer → send + despawn root |
| Cancel button | close | observer → despawn root |
| Server-driven member updates | `PartyManager.ParsePacket` → `GetGump<PartyGump>()?.RequestUpdateContents()` | observer on `OnInsert<PartyState>` rebuilds open window subtree |
| Open (dclick paperdoll party pic) | PaperdollGump.cs:364-373 | wire the existing `partyPic.Observe(On<UiDoubleClick>)` in PaperdollPlugin.cs:413 to spawn PartyGump |
| Invite Accept | `RequestPartyAccept(Inviter)`; Leader=Inviter; Inviter=0; close | NiceButton-equiv `On<UiClick>` → `Send_PartyAccept` + update state + despawn |
| Invite Decline | `Send_PartyDecline(Inviter)`; Inviter=0; close | observer → `Send_PartyDecline` + despawn |

`RequestUpdateContents` in OOP = `Clear()` + `BuildGump()` (PartyGump.cs:30-34). The
ECS equivalent is the despawn-subtree-and-rebuild pattern used by
`PaperdollPlugin.RebuildOnEquip` — keep the root, despawn tagged children, rebuild.

The Loot-type toggle keeps a *local* (not yet sent) `CanLoot` value that is only
committed to the server when OK is pressed (PartyGump.cs:276-285). Store this as a
field on the `PartyGump` window component (e.g. `PendingCanLoot`), not on `PartyState`.

## Server packets

PartyGump has **no dedicated open packet** — it is opened client-side. Its *content*
is driven by the general-information packet:

- **Incoming `0xBF` subcommand `0x06`** (party) → `PartyManager.ParsePacket`:
  - code `1` = add members (list follows, no removed serial).
  - code `2` = full member list (4-byte removed serial, then `count` x 4-byte serials;
    first serial is the leader).
  - code `3`/`4` = party chat message (serial + unicode text) — routed to message
    manager, NOT the gump.
  - code `7` = invite: 4-byte inviter serial → opens **PartyInviteGump** (if profile
    setting `PartyInviteGump` enabled).
- **Outgoing `0xBF` subcommand `0x06`** (all `Send_Party*` use packet ID `0xBF`,
  sub `0x0006`, then a 1-byte action code), from `OutgoingPackets.cs`:
  - `Send_PartyInviteRequest` — action `1`, serial `0` (line 2049-2080).
  - `Send_PartyRemoveRequest(serial)` — action `2`, serial (line 2082-2113).
  - `Send_PartyChangeLootTypeRequest(bool)` — action `0x06`, 1 bool byte (2115-2146).
  - `Send_PartyAccept(serial)` — action `0x08`, serial (2148-2179).
  - `Send_PartyDecline(serial)` — action `0x09`, serial (2181-2208).

These `Send_Party*` methods do NOT yet exist in
`src/ClassicUO.Ecs/Network/OutgoingPackets.cs` — they must be ported (trivial copies,
same wire format). The ECS `OnExtendedCommandPacket_0xBF.Fill` (lines 108-251) handles
subcommands 1,2,4,8,0x0C,0x10,0x16,0x19,0x1B,0x1D,0x20,0x22,0x25,0x26,0x2A,0x2B —
it does NOT handle `0x06` (party) or `0x07`. Add a `case 6:` that parses the party
sub-protocol into typed data the new PartyPlugin consumes.

## ECS implementation plan

### Plugin
`src/ClassicUO.Ecs/Gameplay/PartyPlugin.cs` — `internal readonly struct PartyPlugin :
IPlugin`. Compose it in `src/ClassicUO.Ecs/Boot.cs` (`CuoPlugin.Build`) next to
`PaperdollPlugin`.

### Resources
- `PartyState` (register `app.AddResource(new PartyState())`) mirroring
  `PartyManager`: `uint Leader`, `uint Inviter`, `bool CanLoot`, and a fixed
  `(uint Serial, string Name)[10] Members`. This is the singleton party model the
  gump reads. Name resolution: look up the member serial in `NetworkEntitiesMap` →
  read the mobile's `Name` component (port `PartyMember.Name`'s "Not seeing" fallback).
  Marker-bump pattern: re-insert `PartyState` as a component on a dedicated party
  singleton entity (or fire a custom `PartyChangedEvent`) so the rebuild observer can
  react — see Observers.

### Components
- `PartyWindow { bool PendingCanLoot; }` — tag on the PartyGump root (UOGumpBundle).
  Carries the not-yet-committed loot toggle (OOP `PartyGump.CanLoot`).
- `PartyWindowChild { ulong WindowEntity; }` — tag on every rebuildable child
  (rows, footer labels/buttons) so the update observer despawns precisely these and
  leaves the root + bg intact. Mirrors `PaperdollBodyChild`.
- `PartyInviteWindow` — tag on the invite prompt root (for dedup + despawn).

### Bundle usage
- PartyGump root: `GumpBuilder.SpawnUOGump(commands, 0x0A28, Vector3.UnitZ, spawnPos,
  zCounter)` — but the background is a **scalable** ResizePic (W=450,H=480), so use
  `UOGumpBundle` with `Kind = UOCustomKind.GumpNinePatch` and explicit
  `Size = (450,480)` (the same path ServerGumpPlugin uses for resizepic, see
  ServerGumpPlugin.cs:268-277). `SpawnUOGump` resolves size from the native sprite,
  which is wrong for a nine-patch — spawn via `commands.SpawnBundle(new UOGumpBundle{
  ... Kind = GumpNinePatch, Size = new Vector2(450,480)})` directly, then
  `.Insert(new PartyWindow{...})`.
- Children: `GumpBuilder.AddButton` (button triplets), `AddGump` (0x0475 name plate),
  `AddLabel` (names + static labels). Each child gets
  `.Insert(new PartyWindowChild{WindowEntity = root.Id})` and `commands.AddChild`.

### Observers
- `On<UiDoubleClick>` on the paperdoll party pic — **already exists** at
  `PaperdollPlugin.cs:413` as a no-op log. Replace its body with a spawn-or-focus of
  PartyGump (dedup by querying `Query<Data<PartyWindow>>`; bump z if open). Keep the
  rule: no `World` access; use Commands + a `PartySpawnParams` composite.
- `OnInsert<PartyState>` (or `On<PartyChangedEvent>`) — rebuild the member-rows +
  footer subtree of every open PartyWindow. Same shape as
  `PaperdollPlugin.RebuildOnEquip`: query `PartyWindowChild` by `WindowEntity`,
  despawn, then rebuild from `PartyState`. Use `Commands` top-level so deferred ops
  auto-apply.
- Per-button `On<UiClick>` observers attached at build time (`btn.Observe(...)`)
  for OK / Cancel / Send / Loot / Leave / Add / Tell[i] / Kick[i] — exactly the
  paperdoll button pattern (PaperdollPlugin.cs:293-374). Capture only immutable
  values (member index, root id) per the closure rule; read live state from
  `Res<PartyState>` / the query inside the lambda.
- Party invite: `On<PacketReceived<OnExtendedCommandPacket_0xBF>>` filtered on
  `Command == 6` with party-code `7` (or a dedicated typed event from the 0x06 parse)
  → spawn PartyInviteGump. Accept/Decline buttons get `On<UiClick>` observers.

### Systems
- A name-refresh is event-driven (rebuild on `PartyState` change), so no per-frame
  polling system is needed. The loot-toggle is handled inside the Loot button's
  `On<UiClick>` (flip `PartyWindow.PendingCanLoot`, swap the button's `UOButton`
  triplet + the label text in place, OR re-insert `PartyState` to trigger a rebuild).

### Networking (prereq)
- Port `Send_PartyInviteRequest`, `Send_PartyRemoveRequest`,
  `Send_PartyChangeLootTypeRequest`, `Send_PartyAccept`, `Send_PartyDecline` into
  `src/ClassicUO.Ecs/Network/OutgoingPackets.cs` (verbatim wire format from the
  Client copies, lines 2049-2208).
- Extend `src/ClassicUO.Ecs/Network/IncomingPackets/OnExtendedCommandPacket_0xBF.cs`
  with `case 6:` (party list / add / message / invite). Surface typed fields
  (`PartyAddOrList`, `PartyInviterSerial`, party-message data) the way the existing
  subcommands do, then a registered handler/observer applies them to `PartyState`.
  This mirrors `PartyManager.ParsePacket` (PartyManager.cs:32-191).

### New ClayUO custom command / UiHitTest
- **None required.** The background is a nine-patch (`UOCustomKind.GumpNinePatch`,
  already rendered by ServerGumpPlugin's resizepic path and bbox-hit-tested by
  `UiHitTest` default case). Buttons/labels/gump-pics all use existing
  `GumpBuilder` helpers + existing `UOCustomKind.Gump`/`Art` render + hit-test paths.
  The invite prompt's translucent box maps to a `BackgroundColor` node (as
  ServerGumpPlugin does for `checkertrans`, ServerGumpPlugin.cs:584-593) — no UO
  sprite, no new command. Text buttons (NiceButton) for Accept/Decline can be a
  `BackgroundColor` box + `AddLabel` + `On<UiClick>`, since there is no ECS
  NiceButton yet (see Open Q).

### Font note
PartyGump uses UO fonts 1 and 2 with hue `0x0386`. `GumpBuilder.AddLabel` currently
emits a TTF `Text` node (FontId 0). Pixel-exact parity needs the UO font path
(`UoFontRenderer.Bake` baked-texture approach used by ServerGumpPlugin's
`SpawnWrappedText`, ServerGumpPlugin.cs:688-743) to honour font id + hue. For v1 a
plain `AddLabel` is acceptable but will not match font/hue precisely — flag in the
implementation PR (CLAUDE.md: type-check is not feature-correctness).

## How to trigger for capture

PartyGump (manifest):
1. Boot ECS client (`dotnet run --project src/ClassicUO.Bootstrap`), log into
   ModernUO (`127.0.0.1:2593`, `admin/admin`), enter the world.
2. Open the player paperdoll (top-bar paperdoll button / packet `0x88`).
3. Left-**double-click** the small **party-manifest** profile pic on the paperdoll
   (the second 0x07D2 pic, at root-relative ~(39,196) — `PaperdollPlugin.cs:411`,
   the one that currently logs "Party manifest clicked — no ECS PartyGump").
   - Required state: none for an empty manifest (works solo — `Leader==0` shows
     Disband + Add as leader). For populated rows, first form a party.
4. To show member rows + Kick buttons, form a party: target another mobile and party-
   invite, or use a second client. Members fill from the `0xBF` sub `0x06` list.

PartyInviteGump:
1. From a second logged-in character, send a party invite to the captured character
   (server pushes `0xBF` sub `0x06` code `7`). The prompt appears centered near
   Y=150. Requires profile setting `PartyInviteGump` enabled (default).

Prefer the deterministic harness loop (`tools/agent-desktop`: `up --persist` →
`rpc-shot`) over manual clicking — see `tools/agent-desktop/AGENTS.md`.

## Open questions

1. **System-chat integration for Tell / Send-message.** OOP writes `"/{index+1} "`
   or `"/"` into `UIManager.SystemChat.TextBoxControl`. The ECS branch has no
   confirmed SystemChat control wired for programmatic text injection — verify what
   exists before wiring these two buttons (may be a no-op log for v1, like the
   paperdoll Options/Skills buttons).
2. **NiceButton equivalent.** PartyInviteGump uses `NiceButton` (a text button with
   its own bg). ECS has no NiceButton; confirm whether to approximate with
   `BackgroundColor` box + label + `On<UiClick>`, or introduce a reusable text-button
   helper in `GumpBuilder`.
3. **Font/hue fidelity.** Confirm whether v1 must match UO font 1/2 + hue 0x0386
   exactly (requires the `UoFontRenderer.Bake` path) or can ship with `AddLabel`'s
   TTF default and refine later.
4. **PartyState ownership / change signal.** Decide the exact rebuild trigger:
   `OnInsert<PartyState>` on a singleton entity vs. a dedicated `PartyChangedEvent`.
   The 0xBF `0x06` parse must emit whichever the rebuild observer keys on.
5. **PartyInviteGump profile gate.** OOP only opens it when
   `ProfileManager.CurrentProfile.PartyInviteGump` is true. Confirm the ECS profile
   has this flag (or default to always-show for v1).
6. **Member-name source.** OOP `PartyMember.Name` reads `World.Mobiles.Get(serial)`
   with a "Not seeing" fallback. Confirm the ECS path: `NetworkEntitiesMap` →
   mobile `Name` component, and the fallback string resource.
