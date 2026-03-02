# PRD-00: ClassicUO Flecs ECS Program Charter

## 1. Summary

Port ClassicUO from the current object/manager-centric runtime (`World`, `Entity`, `Item`, `Mobile`, `*Manager`) to a Flecs.NET ECS runtime without gameplay regressions.

This is a full-program migration, not a partial optimization pass.

## 2. Background

Current architecture characteristics:

- Stateful domain object graph centered on [`src/ClassicUO.Client/Game/World.cs`](../../../src/ClassicUO.Client/Game/World.cs)
- Mutable entities with behavior in class methods (`Item`, `Mobile`, `Entity`)
- Per-domain manager classes in [`src/ClassicUO.Client/Game/Managers`](../../../src/ClassicUO.Client/Game/Managers)
- Network packets decoded in [`src/ClassicUO.Client/Network/PacketHandlers.cs`](../../../src/ClassicUO.Client/Network/PacketHandlers.cs), then directly mutate world objects
- Rendering and UI consume direct object references from world and manager state

Limitations to solve:

- Behavior logic spread across many mutable classes and managers
- Hard-to-measure data locality and update order coupling
- Expensive large-scale refactors due to reference-heavy object graph
- Limited deterministic replay and parity validation tooling

## 3. Goals

- Preserve all gameplay behavior and shipped features.
- Replace runtime game-state authority with Flecs ECS.
- Use Flecs.NET as source dependency (no NuGet packages).
- Prefer struct components and avoid object references inside components when possible.
- Exploit Flecs features broadly: systems, custom pipelines, queries, relationships, prefabs, observers, modules, timers, staging/defer.
- Keep build/publish support across current target platforms.

## 4. Non-Goals

- Rewriting FNA renderer from scratch.
- Replacing all UI/gumps in one step.
- Changing plugin protocol/ABI during first migration pass.
- Feature redesign (this is parity-first, not feature expansion).

## 5. Success Criteria

- 100% functional parity for login, movement, combat, item interaction, containers, map navigation, UI flows, macros, targeting, and party/chat behaviors.
- Packet replay parity: migrated runtime produces equivalent state transitions for agreed packet corpora.
- No stability regression in long sessions (memory growth, crashes, desync).
- Performance is neutral or improved in 95th percentile frame time and update cost.

## 6. Program Constraints

- No NuGet Flecs packages; integrate from `C:\dev\Flecs.NET` and repository-managed source dependency path.
- Existing branch may be dirty; migration work must be incremental and isolatable.
- A/B runtime toggle required until parity gate is met.

## 7. ECS Design Principles

1. Data-Oriented Components
- Components are plain structs, blittable where possible.
- Store IDs/handles (`uint serial`, flecs `Entity`) instead of object references.

2. System-Driven Behavior
- Business logic moves from entity/member methods and managers into systems.
- Systems are ordered explicitly via pipeline phases.

3. Relationship-Centric Modeling
- Use pairs (`ChildOf`, `IsA`, domain relations) for containment, ownership, attachments, and hierarchy.

4. Deferred Structural Changes
- Use staging/defer for safe in-frame structural mutations.

5. Observable State Transitions
- Use observers/hooks for spawn/despawn side effects and state replication glue.

6. Parity-First Migration
- Maintain side-by-side compatibility with current APIs until subsystem cutovers complete.

## 8. Flecs Feature Utilization Plan

- `World`, entities, tags/components: all runtime state
- Queries (cached/uncached, sorting/grouping/change tracking): update and render extraction
- Systems and custom pipeline phases: deterministic execution order
- Observers/monitors/custom events: packet-driven and lifecycle-driven reactions
- Relationships/pairs: container trees, equipment, ownership, party links, map hierarchy
- Prefabs and `IsA`: reusable templates for mobiles/items/effects
- Modules: subsystem boundaries (`Network`, `Movement`, `Combat`, `RenderingBridge`, etc.)
- Timers/rate filters: periodic mechanics and throttled systems
- Singleton components: global config/session state/frame context
- Reflection/serialize utilities: debug snapshots and parity tooling

## 9. Migration Waves

Wave 0: Foundation
- Add Flecs.NET source dependency and bootstrap ECS host.
- Introduce runtime toggle (`LegacyWorld` vs `EcsWorld`).

Wave 1: Identity + State Mirror
- Mirror legacy entities into ECS components (read-only parity mode).
- Validate entity counts, key fields, and lifecycle events.

Wave 2: Network Ingestion
- Convert packet handlers from direct mutation to ECS command/event ingestion.

Wave 3: Core Gameplay Systems
- Movement, stats, combat flags, item/container flow, effects.

Wave 4: Rendering Bridge
- Drive render extraction from ECS queries, keep renderer backend.

Wave 5: UI/Input/Plugin Bridge
- Move UI state readers to ECS-backed facades; preserve plugin behavior.

Wave 6: Legacy Decommission
- Remove duplicate legacy authority paths once parity passes.

## 10. Deliverables

- PRD set `PRD-01` to `PRD-07` in this folder
- ECS architecture and component schema
- Dependency and CI integration plan
- Parity and rollout plan with measurable gates

## 11. Risks

- Hidden behavior in side effects across managers and entity methods
- Packet ordering edge cases during ingestion redesign
- UI and plugin dependencies on mutable object references
- Build/publish native library handling for Flecs on all targets

All risk mitigations are defined in later PRDs.

