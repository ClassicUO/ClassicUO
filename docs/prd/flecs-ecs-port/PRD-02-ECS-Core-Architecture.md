# PRD-02: ECS Core Architecture and Data Model

## 1. Objective

Define the target ECS architecture, component conventions, module boundaries, and execution pipeline for ClassicUO using Flecs.NET.

## 2. Scope

In scope:

- ECS world ownership and lifecycle
- Core component taxonomy
- Module boundaries
- Flecs pipeline and system order
- OO-to-ECS mapping for foundational domain objects

Out of scope:

- Packet ingestion details (PRD-03)
- Subsystem-specific gameplay logic (PRD-04 to PRD-06)

## 3. World Ownership

Introduce `EcsRuntimeHost` as the authoritative runtime state owner during migration mode.

Responsibilities:

- Create/dispose Flecs `World`
- Register modules, components, prefabs, observers, systems
- Execute pipeline each frame (`Progress`)
- Offer bridge APIs for legacy callers during transition

Lifecycle integration:

- `GameController` retains frame scheduling.
- `GameScene` delegates simulation authority to `EcsRuntimeHost` in ECS mode.

## 4. Component Conventions

Rules:

- Prefer `struct` components.
- Prefer unmanaged/blittable components when feasible.
- Do not store object references in components unless unavoidable.
- Store identifiers (`uint Serial`, entity IDs, indices, enum flags) instead.
- Separate frequently-updated from rarely-updated data to reduce churn.

Component classes:

1. Identity components
- `SerialComponent`
- `EntityKind` (`Mobile`, `Item`, `Effect`, `Static`, etc.)

2. Spatial/movement
- `WorldPosition`
- `WorldOffset`
- `DirectionComponent`
- `StepQueueState` (indexes into ECS buffers)

3. Visual/animation
- `GraphicComponent`
- `HueComponent`
- `AnimationState`
- `LightState`

4. Status/combat
- `Vitals` (`Hits`, `HitsMax`, `Mana`, `Stamina`)
- `FlagsComponent`
- `NotorietyComponent`

5. Item/inventory
- `ContainerLink` (relation-based primary model, data fallback)
- `LayerComponent`
- `AmountComponent`

6. Session/global
- singleton components for frame timing, map index, client features, profile toggles

## 5. Relationship Model

Use Flecs pairs/relationships as the default for graph semantics:

- `ChildOf`: containment (item in container, entity in hierarchy namespace)
- `IsA`: prefab inheritance for archetypes/templates
- Custom relations:
- `OwnedBy(Player)`
- `EquippedOn(Mobile, Layer)`
- `Targeting(Entity)`
- `Affects(Entity)` for effects/projectiles

Benefits:

- Eliminates many bidirectional object references.
- Enables query-driven traversal.

## 6. Module Boundaries

Proposed Flecs modules:

- `CoreModule`: identity, world bootstrap, singleton config
- `NetworkModule`: command/event ingestion application
- `MovementModule`: steps, pathing, position integration
- `CombatModule`: vitals, warmode, notoriety, attack state
- `InventoryModule`: containers, equipment, loot interactions
- `MapModule`: map/chunk scope and visibility metadata
- `EffectsModule`: weather/effects/audio triggers
- `RenderBridgeModule`: render-extract components and sorted views
- `UiBridgeModule`: UI query adapters and command translators
- `PluginBridgeModule`: plugin-facing compatibility layer

## 7. Execution Pipeline

Custom Flecs phases:

1. `PreInput`  
2. `Input`
3. `PreNet`
4. `NetApply`
5. `PreSim`
6. `Simulation`
7. `PostSim`
8. `RenderExtract`
9. `UiExtract`
10. `PostFrame`

Execution rules:

- Structural changes from packet/UI commands use defer/staging.
- Write-heavy systems run before read-mostly extraction systems.
- Observers only for lifecycle/event reactions, not main per-frame simulation loops.

## 8. Query Strategy

Query policy:

- Cached queries for hot paths (movement, render extraction, nearby entity sets)
- Uncached/adhoc for tools, debug, infrequent flows
- Use sorting/grouping where current code does manual ordering
- Use change detection for expensive recomputations (nameplate, minimap entries, derived visuals)

## 9. OO-to-ECS Foundational Mapping

From current classes:

- `World` -> world singletons + module state entities
- `Entity` base fields -> shared components (`Serial`, `Flags`, `NameRef`, etc.)
- `Mobile` -> mobile-tagged entities with movement/combat/status components
- `Item` -> item-tagged entities with container/equipment components
- `*Manager` classes -> system sets plus module service façades

During transition:

- Keep read-only mirrors where needed by legacy UI/plugin code.
- Remove mirrors after feature gate per subsystem.

## 10. Memory and Allocation Policy

- Avoid managed allocations in hot systems.
- Use Flecs tables/archetypes as primary storage.
- For variable-sized state (e.g., step queues, journal text), use indexed stores with ID components.
- Track allocations per frame in debug builds.

## 11. Migration Tasks

1. Build ECS bootstrap with no gameplay side effects.
2. Register core components, tags, and singleton state.
3. Introduce custom phases and pipeline.
4. Add compatibility façade for legacy callers.
5. Mirror basic entity lifecycle for parity instrumentation.

## 12. Acceptance Criteria

- ECS world initializes/tears down with scene lifecycle.
- Core modules and pipeline execute each frame in deterministic order.
- Entity mirror counts match legacy collections in parity mode.
- No gameplay behavior changes yet (foundation-only gate).

## 13. Risks and Mitigations

Risk: over-normalized component model increases query complexity
- Mitigation: component split decisions validated with profiling before wide rollout

Risk: observers misused for per-frame logic
- Mitigation: enforce design rule in code review; systems for frame loops, observers for events

Risk: transitional duplication causes drift
- Mitigation: parity assertions and periodic state diff checks

