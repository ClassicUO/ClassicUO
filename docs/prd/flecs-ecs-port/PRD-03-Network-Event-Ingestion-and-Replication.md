# PRD-03: Network Event Ingestion and State Replication

## 1. Objective

Replace direct world mutation in packet handlers with an ECS event/command ingestion pipeline while preserving packet-order behavior.

## 2. Scope

In scope:

- Packet decode to command/event translation
- ECS command buffering and application
- Ordering guarantees and reconciliation rules
- Initial packet replay validation harness

Out of scope:

- Rendering/UI migration details (PRD-05, PRD-06)

## 3. Current State

- Packet handlers currently decode and mutate world objects directly in [`src/ClassicUO.Client/Network/PacketHandlers.cs`](../../../src/ClassicUO.Client/Network/PacketHandlers.cs).
- `GameController.Update` parses packets before scene/world updates.

## 4. Target Architecture

New flow:

1. Packet bytes -> decode structs (`PacketDecoded<T>`)
2. Decoder emits ECS commands/events (`SpawnMobile`, `UpdateItem`, `SetWarmode`, etc.)
3. `NetApply` phase systems consume command buffers and mutate ECS state
4. Observers fire for lifecycle side effects (spawn/despawn, changed stats, etc.)

Core rule:

- Packet decoding remains imperative.
- State mutation authority moves to ECS systems.

## 5. Command/Event Model

Command characteristics:

- Value-type payload structs
- No object references
- Include packet tick/frame sequence for diagnostics

Examples:

- `CmdCreateOrUpdateMobile`
- `CmdCreateOrUpdateItem`
- `CmdDeleteEntity`
- `CmdUpdateVitals`
- `CmdSetMap`
- `CmdContainerOpenState`
- `CmdPlayEffect`

Storage options:

- ECS transient entities tagged as `NetworkCommand`
- Or ring-buffer singleton + indexed command data components

Initial recommendation:

- Transient command entities per frame, destroyed after `NetApply`.

## 6. Ordering and Determinism

Ordering constraints:

- Preserve packet order within same network frame.
- Preserve handler semantics for known dependent packets.
- Ensure command application completes before simulation phase.

Implementation:

- Sequence index component on command entities.
- NetApply query sorted by sequence index.

## 7. Deferred Mutations

Use Flecs defer/staging in `NetApply`:

- Avoid unsafe structural changes while iterating.
- Batch add/remove/set operations.

Observers:

- Use `OnAdd` for initialization side effects
- Use `OnRemove` for cleanup side effects
- Use `OnSet` for derived state updates when beneficial

## 8. Packet Group Migration Plan

Phase A (high-frequency core):

- Movement/player update packets (`0x20`, `0x21`, `0x22`, `0x77`, `0x78`, `0x97`)
- Item/container lifecycle (`0x1A`, `0x24`, `0x25`, `0x3C`, `0x2E`, `0x1D`)

Phase B (combat/status):

- Hits/mana/stamina/status/warmode (`0x11`, `0x16`, `0x17`, `0xA1`, `0xA2`, `0xA3`, `0x72`)

Phase C (effects/social/ui-driven):

- Effects, sound, chat, gump-related packets and party/guild state

## 9. Error Handling and Diagnostics

- Command decode failures log packet ID + offset + bytes snapshot (already available via packet logger integration).
- Add ECS debug counters singletons:
- Commands enqueued per packet ID
- Commands applied
- Failed/ignored commands

Add replay trace mode:

- Deterministic record of packet stream + applied command sequence IDs.

## 10. Migration Tasks

1. Introduce packet-to-command translation layer.
2. Add command entity schema and NetApply systems.
3. Migrate Phase A packet groups.
4. Validate with packet replay.
5. Migrate Phase B/C packet groups.
6. Remove direct world mutation paths per packet group.

## 11. Acceptance Criteria

- No direct world-state mutation remains in migrated packet handlers.
- Packet replay parity for migrated packet groups reaches target diff threshold (zero critical diffs, approved noncritical diffs only).
- NetApply phase executes before simulation each frame.
- Crash/desync rate does not increase in soak testing.

## 12. Risks and Mitigations

Risk: hidden coupling between packet handlers and manager side effects
- Mitigation: observers for side effects plus parity trace assertions

Risk: sequence/order mismatch
- Mitigation: explicit sequence indices and deterministic sorted application

Risk: temporary dual-path divergence
- Mitigation: gate packet groups one-by-one, disable legacy path per group after parity pass

