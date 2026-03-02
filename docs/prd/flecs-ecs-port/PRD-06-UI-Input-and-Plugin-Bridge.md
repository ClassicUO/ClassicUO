# PRD-06: UI, Input, and Plugin Compatibility Bridge

## 1. Objective

Migrate game-state access for UI/input/plugin flows to ECS-backed interfaces while preserving existing behavior and plugin compatibility.

## 2. Scope

In scope:

- UI data access bridge for gumps and controls
- Input command translation into ECS
- Plugin compatibility façade over ECS authority
- Transitional coexistence strategy

Out of scope:

- Full rewrite of UI framework/widgets
- New plugin API versioning (deferred)

## 3. Current State

- UI/gumps frequently pull from mutable `World`, `Items`, `Mobiles`, and managers.
- Input handlers invoke direct game actions that mutate legacy state.
- Plugin host expects world-backed behavior.

## 4. Target Architecture

Bridge pattern:

- ECS owns authoritative state.
- Legacy UI and plugin callers read/write via adapter services.
- Adapters translate calls into ECS queries and command events.

Adapter groups:

- `UiQueryAdapter` (read models)
- `UiCommandAdapter` (actions)
- `PluginBridgeAdapter` (compat APIs)

## 5. UI Migration Strategy

Stage A: Read-only adaptation

- Replace direct world dictionary lookups with adapter reads for selected gumps.

Stage B: Command adaptation

- Route `GameActions` style mutations through ECS command events.

Stage C: Legacy dependency removal

- Drop direct world object assumptions in migrated UI paths.

Priority UI targets:

- World viewport and selection flows
- Container/paperdoll/status/targeting gumps
- World map gump entity feeds

## 6. Input-to-ECS Command Flow

Input events become typed commands:

- Move/walk requests
- Targeting actions
- Item drag/drop/equip actions
- War mode and ability toggles

Rules:

- Input systems emit commands in early frame phases.
- Simulation/network systems consume commands in ordered phases.

## 7. Plugin Compatibility

Constraint:

- Existing plugin behavior must remain stable during migration.

Plan:

- Maintain plugin-facing facade that maps legacy concepts to ECS queries.
- Preserve existing command/event invocation points.
- Instrument compatibility calls to detect unsupported paths early.

## 8. Data Model Guidance for UI

- UI read models are snapshots/DTO structs with IDs and primitive fields.
- No direct component mutation from UI code.
- Use command events for write actions.

## 9. Migration Tasks

1. Build adapter interfaces and baseline implementations.
2. Migrate `GameActions` paths to emit ECS commands.
3. Migrate high-traffic gumps to adapter reads.
4. Implement plugin facade backed by ECS.
5. Remove direct legacy state access from migrated flows.

## 10. Acceptance Criteria

- Migrated UI flows behave identically in ECS mode.
- Input commands execute with same gameplay results.
- Existing plugins tested in compatibility suite continue to function for covered APIs.
- No direct world mutation from UI input path in migrated areas.

## 11. Risks and Mitigations

Risk: plugin assumptions on object lifetimes/references
- Mitigation: stable ID-based facade with compatibility shims and deprecation telemetry

Risk: UI regressions from stale snapshots
- Mitigation: frame-consistent extraction and explicit invalidation triggers

Risk: command latency perceptions
- Mitigation: preserve same-frame application order where legacy behavior expects immediate effects

