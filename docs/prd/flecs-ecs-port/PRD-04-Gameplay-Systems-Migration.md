# PRD-04: Gameplay Systems Migration (Simulation Authority)

## 1. Objective

Move gameplay simulation authority from legacy entity methods/managers to ECS systems with behavior parity.

## 2. Scope

In scope:

- Movement/pathing integration
- Mobile and item lifecycle
- Combat/status updates
- Containers/equipment/loot logic
- Effects/weather/auxiliary managers

Out of scope:

- Renderer internals
- UI widget hierarchy internals

## 3. Subsystem Mapping

Legacy to ECS mapping:

- `World.Update` loop -> phase-ordered ECS system sets
- `Mobile.Update` + movement queue -> `MovementModule` systems/components
- `Item` container logic -> `InventoryModule` systems/relationships
- `EffectManager`, `Weather`, world text pathways -> `EffectsModule` + `UiBridge` outputs
- Manager classes in [`src/ClassicUO.Client/Game/Managers`](../../../src/ClassicUO.Client/Game/Managers) -> module-local systems + transitional service facades

## 4. Vertical Slice Plan

Slice 1: Entity lifecycle and identity

- Spawn/update/despawn mobile/item/effect entities
- Serial-to-entity index singleton
- `OnRemove` cleanup for dependent relationships

Slice 2: Movement and stepping

- Componentized step queue state
- Position integration and direction updates
- Range/view pruning behavior parity

Slice 3: Combat and vitals

- Hits/mana/stamina/warmode/notoriety components
- Ability update triggers formerly tied to item/layer mutations

Slice 4: Item/container/equipment

- `ChildOf` + relation pairs for containment/equipment
- open/close/container refresh signals to UI bridge

Slice 5: Effects/weather/text

- Effect entities + timed cleanup
- Weather state as singleton + effect tags
- world text emissions as ECS events to UI bridge

## 5. Component and Relation Proposals

Core:

- `SerialComponent`
- `EntityTypeTag` (`MobileTag`, `ItemTag`, `EffectTag`)
- `DestroyedTag`, `PendingRemovalTag`

Movement:

- `Position3`
- `Direction8`
- `StepQueueHandle`
- `VelocityFlags` / `MovementMode`

Combat:

- `Vitals`
- `CombatFlags`
- `Notoriety`
- `WarModeTag`

Inventory:

- `ContainerState`
- `LayerState`
- `AmountState`
- `Pair<ContainedBy, Entity>`
- `Pair<EquippedBy, Entity>`

Effects:

- `EffectType`
- `Lifetime`
- `EffectTarget`

## 6. System Ordering

Recommended intra-frame simulation order:

1. Entity lifecycle finalize
2. Movement pre-step validation
3. Movement step integration
4. Position-derived updates (distance, visibility candidates)
5. Combat/status reconciliation
6. Inventory/equipment reconciliation
7. Effects lifetime and cleanup
8. Signal generation for render/UI extraction

## 7. State Transition Rules

- Avoid in-system direct recursive operations.
- Use command/event components for chain reactions.
- Keep destructive operations deferred to end-of-phase cleanup systems.

Example:

- Container removal cascades produce `PendingDetach` commands first, then finalized in cleanup phase.

## 8. Manager Decomposition Plan

Managers to decompose first:

- `BoatMovingManager`
- `ContainerManager`
- `EffectManager`
- `TargetManager` (simulation-facing part)
- `PartyManager` (state core)

Managers to keep as facades longer:

- `UIManager` (UI ownership remains)
- `MacroManager` (until command remap complete)

## 9. Parity Requirements per Slice

Slice completion requires:

- Packet replay parity for slice-owned fields
- In-game scenario checklist pass
- No regression in related unit/integration tests
- Legacy path for slice disabled or bypassed in ECS mode

## 10. Testing Matrix

Core scenarios:

- Login -> world enter -> movement -> logout
- Ground item pick/drop/equip cycles
- Corpse open/loot interactions
- Warmode toggle and combat target updates
- House/multi interactions
- Effects lifecycle (spawn/update/remove)

Stress scenarios:

- Dense mobile crowds
- Item-rich containers
- Prolonged session with frequent packet bursts

## 11. Risks and Mitigations

Risk: relation-heavy inventory model causes query fragmentation
- Mitigation: benchmark archetype counts and relation patterns early

Risk: duplicate legacy+ECS logic diverges
- Mitigation: one-way authority switch per slice as soon as parity passes

Risk: hidden UI dependencies on manager internals
- Mitigation: publish explicit bridge events/components; avoid direct manager access in new code

## 12. Acceptance Criteria

- ECS owns simulation state for all slices in ECS mode.
- Legacy object methods (`Mobile.Update`, `Item.Update`, large parts of `World.Update`) are no longer authoritative in ECS mode.
- Gameplay behavior remains equivalent by parity suite and manual high-value flows.

