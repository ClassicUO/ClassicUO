// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.ECS.Modules;
using ClassicUO.ECS.Systems;
using Flecs.NET.Core;
using System;
using System.Collections.Generic;

namespace ClassicUO.ECS
{
    /// <summary>
    /// Authoritative ECS runtime state owner during migration.
    /// Creates the Flecs world, registers all modules/phases/pipeline,
    /// and provides bridge APIs for legacy callers.
    /// </summary>
    public sealed class EcsRuntimeHost : IDisposable
    {
        private readonly World _world;

        // Serial → ECS entity lookup for legacy bridge callers.
        private readonly Dictionary<uint, Entity> _serialToEntity = new();

        // Monotonic command sequence counter for deterministic ordering.
        private int _commandSequence;

        // String table for name/text data referenced by ECS components.
        // Commands reference strings by index to avoid managed types in unmanaged structs.
        private readonly List<string> _stringTable = new();
        private readonly Dictionary<uint, int> _nameBySerial = new();

        // Cached queries for hot-path iteration (avoids per-frame QueryBuilder allocations).
        private Query<SerialComponent, WorldPosition> _visibleMobilesQuery;
        private Query<SerialComponent, WorldPosition> _visibleItemsQuery;
        private Query<SerialComponent, WorldPosition> _allMobilesQuery;
        private Query<SerialComponent, WorldPosition> _allItemsQuery;

        private EcsRuntimeHost(World world)
        {
            _world = world;
            SerialRegistry.Initialize(_serialToEntity);
        }

        /// <summary>The underlying Flecs world. Exposed for advanced queries.</summary>
        public ref World World => ref System.Runtime.CompilerServices.Unsafe.AsRef(in _world);

        /// <summary>
        /// Create and fully bootstrap the ECS runtime.
        /// Registers phases, modules, and parity-tracking observers.
        /// </summary>
        public static EcsRuntimeHost Create()
        {
            World world = World.Create();

            var host = new EcsRuntimeHost(world);
            host.Bootstrap();
            host.BuildCachedQueries();
            return host;
        }

        public void Dispose()
        {
            _visibleMobilesQuery.Dispose();
            _visibleItemsQuery.Dispose();
            _allMobilesQuery.Dispose();
            _allItemsQuery.Dispose();
            SerialRegistry.Clear();
            _serialToEntity.Clear();
            _world.Dispose();
        }

        /// <summary>Advance the ECS pipeline by one frame.</summary>
        public bool Progress(float deltaTime)
        {
            return _world.Progress(deltaTime);
        }

        // ── Bootstrap ───────────────────────────────────────────────────

        private void Bootstrap()
        {
            // 1. Register custom pipeline phases (must be before modules that use them)
            Phases.Register(_world);

            // 2. Import modules in dependency order
            _world.Import<CoreModule>();
            _world.Import<NetworkModule>();
            _world.Import<MovementModule>();
            _world.Import<CombatModule>();
            _world.Import<InventoryModule>();
            _world.Import<MapModule>();
            _world.Import<EffectsModule>();
            _world.Import<RenderBridgeModule>();
            _world.Import<UiBridgeModule>();
            _world.Import<PluginBridgeModule>();

            // 3. Register cross-cutting lifecycle systems (distance pruning, deferred destroy)
            LifecycleSystems.Register(_world);

            // 4. Register manager migration systems (delayed click, use item queue)
            Systems.ManagerSystems.Register(_world);

            // 5. Register diagnostics (PostFrame)
            Systems.DiagnosticsSystems.Register(_world);

            // 6. Register parity-tracking observers
            RegisterParityObservers();
        }

        private void RegisterParityObservers()
        {
            // Track serial → entity mapping on add/remove of SerialComponent.
            // This lets legacy code look up ECS entities by UO serial.
            _world.Observer<SerialComponent>("SerialIndex_OnSet")
                .Event(Ecs.OnSet)
                .Each((Entity entity, ref SerialComponent serial) =>
                {
                    _serialToEntity[serial.Serial] = entity;
                });

            _world.Observer<SerialComponent>("SerialIndex_OnRemove")
                .Event(Ecs.OnRemove)
                .Each((Entity entity, ref SerialComponent serial) =>
                {
                    _serialToEntity.Remove(serial.Serial);
                });

            // Track mobile/item counts for parity validation.
            // NOTE: Zero-sized tag structs cannot be used as generic type params
            // for Observer<T>. Use non-generic observer + .With<Tag>() + .Run() instead.
            _world.Observer("ParityCount_MobileAdd")
                .With<MobileTag>()
                .Event(Ecs.OnAdd)
                .Run((Iter it) =>
                {
                    while (it.Next())
                    {
                        ref var c = ref _world.GetMut<ParityCounters>();
                        c = c with { MobileCount = c.MobileCount + it.Count() };
                    }
                });

            _world.Observer("ParityCount_MobileRemove")
                .With<MobileTag>()
                .Event(Ecs.OnRemove)
                .Run((Iter it) =>
                {
                    while (it.Next())
                    {
                        ref var c = ref _world.GetMut<ParityCounters>();
                        c = c with { MobileCount = c.MobileCount - it.Count() };
                    }
                });

            _world.Observer("ParityCount_ItemAdd")
                .With<ItemTag>()
                .Event(Ecs.OnAdd)
                .Run((Iter it) =>
                {
                    while (it.Next())
                    {
                        ref var c = ref _world.GetMut<ParityCounters>();
                        c = c with { ItemCount = c.ItemCount + it.Count() };
                    }
                });

            _world.Observer("ParityCount_ItemRemove")
                .With<ItemTag>()
                .Event(Ecs.OnRemove)
                .Run((Iter it) =>
                {
                    while (it.Next())
                    {
                        ref var c = ref _world.GetMut<ParityCounters>();
                        c = c with { ItemCount = c.ItemCount - it.Count() };
                    }
                });
        }

        // ── Cached Queries ──────────────────────────────────────────────

        private void BuildCachedQueries()
        {
            _visibleMobilesQuery = _world.QueryBuilder<SerialComponent, WorldPosition>()
                .With<MobileTag>()
                .With<VisibleTag>()
                .Without<PendingRemovalTag>()
                .Build();

            _visibleItemsQuery = _world.QueryBuilder<SerialComponent, WorldPosition>()
                .With<ItemTag>()
                .With<VisibleTag>()
                .Without<PendingRemovalTag>()
                .Build();

            _allMobilesQuery = _world.QueryBuilder<SerialComponent, WorldPosition>()
                .With<MobileTag>()
                .Without<PendingRemovalTag>()
                .Build();

            _allItemsQuery = _world.QueryBuilder<SerialComponent, WorldPosition>()
                .With<ItemTag>()
                .Without<PendingRemovalTag>()
                .Build();
        }

        /// <summary>
        /// Iterate visible mobile entities using a cached query (hot path).
        /// More efficient than ForEachMobile for render-loop use.
        /// </summary>
        public void ForEachVisibleMobile(Action<MobileSnapshot> callback)
        {
            _visibleMobilesQuery.Each((Entity entity, ref SerialComponent serial, ref WorldPosition pos) =>
            {
                callback(GetMobileSnapshot(serial.Serial));
            });
        }

        /// <summary>
        /// Iterate visible item entities using a cached query (hot path).
        /// </summary>
        public void ForEachVisibleItem(Action<ItemSnapshot> callback)
        {
            _visibleItemsQuery.Each((Entity entity, ref SerialComponent serial, ref WorldPosition pos) =>
            {
                callback(GetItemSnapshot(serial.Serial));
            });
        }

        // ── Legacy Bridge APIs ──────────────────────────────────────────

        /// <summary>
        /// Look up the ECS entity for a given UO serial.
        /// Returns default (0) entity if not found.
        /// </summary>
        public Entity GetEntityBySerial(uint serial)
        {
            return _serialToEntity.TryGetValue(serial, out var entity) ? entity : default;
        }

        /// <summary>Check whether an entity with this serial exists in the ECS world.</summary>
        public bool ContainsSerial(uint serial)
        {
            return _serialToEntity.ContainsKey(serial);
        }

        /// <summary>Current parity counters for validation against legacy World.</summary>
        public ParityCounters GetParityCounters()
        {
            return _world.Get<ParityCounters>();
        }

        /// <summary>
        /// Update the per-frame timing singleton. Called from GameController
        /// before Progress() each frame.
        /// </summary>
        public void SetFrameTiming(uint ticks, float deltaSeconds)
        {
            ref var ft = ref _world.GetMut<FrameTiming>();
            ft = new FrameTiming(ticks, deltaSeconds);
        }

        // ── Command Emission ────────────────────────────────────────────

        /// <summary>
        /// Enqueue a network command as a transient entity.
        /// Called from packet handlers before Progress(). Commands are consumed
        /// during NetApply phase and destroyed during PostFrame.
        /// </summary>
        public void EnqueueCommand<T>(T command) where T : unmanaged
        {
            _world.Entity()
                .Add<NetworkCommand>()
                .Set(new SequenceIndex(_commandSequence++))
                .Set(command);

            ref var c = ref _world.GetMut<NetDebugCounters>();
            c = c with { CommandsEnqueued = c.CommandsEnqueued + 1 };
        }

        /// <summary>
        /// Reset per-frame command sequence counter. Called at the start
        /// of each frame before packet parsing.
        /// </summary>
        public void ResetCommandSequence()
        {
            _commandSequence = 0;
            ref var ndc = ref _world.GetMut<NetDebugCounters>();
            ndc = new NetDebugCounters(0, 0, 0);
        }

        /// <summary>
        /// Get or create the ECS entity for a given UO serial.
        /// Used by NetApply systems to resolve command targets.
        /// </summary>
        internal Entity GetOrCreateEntity(uint serial)
        {
            return SerialRegistry.FindOrCreate(_world, serial);
        }

        /// <summary>Current network debug counters for diagnostics.</summary>
        public NetDebugCounters GetNetDebugCounters()
        {
            return _world.Get<NetDebugCounters>();
        }

        // ── Render Bridge APIs ───────────────────────────────────────────

        /// <summary>
        /// Update the viewport state singleton. Called from the scene/renderer
        /// each frame before Progress() to define the visible area.
        /// </summary>
        public void SetViewport(
            int minTileX, int minTileY, int maxTileX, int maxTileY,
            int minPixelX, int minPixelY, int maxPixelX, int maxPixelY,
            int cameraOffsetX, int cameraOffsetY)
        {
            ref var vs = ref _world.GetMut<ViewportState>();
            vs = new ViewportState(
                minTileX, minTileY, maxTileX, maxTileY,
                minPixelX, minPixelY, maxPixelX, maxPixelY,
                cameraOffsetX, cameraOffsetY);
        }

        /// <summary>
        /// Update global lighting state. Called when light levels change
        /// (server packet, time-of-day, personal light).
        /// </summary>
        public void SetLighting(int overall, int personal, int realOverall, int realPersonal)
        {
            int reverted = 32 - overall;
            float current = personal > reverted ? personal : reverted;
            float isometricLevel = current * 0.03125f; // Scale 0-32 → 0.0-1.0
            ref var ls = ref _world.GetMut<LightingState>();
            ls = new LightingState(overall, personal, realOverall, realPersonal, isometricLevel);
        }

        /// <summary>Current lighting state for renderer queries.</summary>
        public LightingState GetLightingState()
        {
            return _world.Get<LightingState>();
        }

        /// <summary>Current viewport state.</summary>
        public ViewportState GetViewportState()
        {
            return _world.Get<ViewportState>();
        }

        // ── Render Data Bridge APIs ──────────────────────────────────────

        /// <summary>
        /// Get render-ready data for a mobile by serial.
        /// Bundles all ECS components that MobileView.Draw() needs.
        /// Returns EcsMobileRenderData.Empty if not found.
        /// </summary>
        public EcsMobileRenderData GetMobileRenderData(uint serial)
        {
            if (!_serialToEntity.TryGetValue(serial, out var entity) || !entity.IsAlive())
                return EcsMobileRenderData.Empty;
            if (!entity.Has<MobileTag>())
                return EcsMobileRenderData.Empty;

            ushort graphic = entity.Has<GraphicComponent>() ? entity.Get<GraphicComponent>().Graphic : (ushort)0;
            ushort hue = entity.Has<HueComponent>() ? entity.Get<HueComponent>().Hue : (ushort)0;
            byte dir = entity.Has<DirectionComponent>() ? entity.Get<DirectionComponent>().Direction : (byte)0;
            byte notoriety = entity.Has<NotorietyComponent>() ? entity.Get<NotorietyComponent>().Notoriety : (byte)0;

            byte alphaHue = 0xFF;
            byte animGroup = 0, frameIndex = 0, animDir = 0;
            ushort frameCount = 0;
            if (entity.Has<RenderSprite>())
                alphaHue = entity.Get<RenderSprite>().AlphaHue;
            if (entity.Has<RenderAnimationFrame>())
            {
                ref readonly var anim = ref entity.Get<RenderAnimationFrame>();
                animGroup = anim.AnimGroup;
                frameIndex = anim.FrameIndex;
                animDir = anim.Direction;
                frameCount = anim.FrameCount;
            }

            int screenX = 0, screenY = 0;
            if (entity.Has<ScreenPosition>())
            {
                ref readonly var sp = ref entity.Get<ScreenPosition>();
                screenX = sp.X;
                screenY = sp.Y;
            }

            float offsetX = 0, offsetY = 0, offsetZ = 0;
            if (entity.Has<WorldOffset>())
            {
                ref readonly var wo = ref entity.Get<WorldOffset>();
                offsetX = wo.OffsetX;
                offsetY = wo.OffsetY;
                offsetZ = wo.OffsetZ;
            }

            short priorityZ = 0;
            float depthZ = 0;
            if (entity.Has<RenderLayerKey>())
            {
                ref readonly var lk = ref entity.Get<RenderLayerKey>();
                priorityZ = lk.PriorityZ;
                depthZ = lk.DepthZ;
            }

            ushort hits = 0, hitsMax = 0;
            if (entity.Has<Vitals>())
            {
                ref readonly var v = ref entity.Get<Vitals>();
                hits = v.Hits;
                hitsMax = v.HitsMax;
            }

            // Mount: look for child entity with LayerComponent(Layer.Mount = 25)
            ushort mountGraphic = 0;
            bool isMounted = entity.Has<MountedTag>();
            entity.Children((Entity child) =>
            {
                if (child.Has<LayerComponent>() && child.Get<LayerComponent>().Layer == 25)
                {
                    mountGraphic = child.Has<GraphicComponent>()
                        ? child.Get<GraphicComponent>().Graphic : (ushort)0;
                    isMounted = mountGraphic != 0 && mountGraphic != 0xFFFF;
                }
            });

            return new EcsMobileRenderData(
                graphic, hue, alphaHue, dir, notoriety,
                animGroup, frameIndex, animDir, frameCount,
                screenX, screenY,
                offsetX, offsetY, offsetZ,
                priorityZ, depthZ,
                hits, hitsMax,
                mountGraphic,
                entity.Has<HiddenTag>(),
                entity.Has<FemaleTag>(),
                entity.Has<IsHumanTag>(),
                entity.Has<IsGargoyleTag>(),
                entity.Has<PoisonedTag>(),
                entity.Has<FlyingTag>(),
                entity.Has<FrozenTag>(),
                entity.Has<YellowHitsTag>(),
                entity.Has<WarModeTag>(),
                entity.Has<DeadTag>(),
                entity.Has<PlayerTag>(),
                isMounted
            );
        }

        /// <summary>
        /// Get equipment graphic and hue for a specific layer on a mobile.
        /// Returns (0, 0) if no equipment found on that layer.
        /// Used by MobileView.Draw() to resolve equipment without legacy Item lookups.
        /// </summary>
        public (ushort Graphic, ushort Hue) GetEquipmentOnLayer(uint serial, byte layer)
        {
            if (!_serialToEntity.TryGetValue(serial, out var entity) || !entity.IsAlive())
                return (0, 0);

            ushort graphic = 0, hue = 0;
            entity.Children((Entity child) =>
            {
                if (child.Has<LayerComponent>() && child.Get<LayerComponent>().Layer == layer)
                {
                    graphic = child.Has<GraphicComponent>() ? child.Get<GraphicComponent>().Graphic : (ushort)0;
                    hue = child.Has<HueComponent>() ? child.Get<HueComponent>().Hue : (ushort)0;
                }
            });
            return (graphic, hue);
        }

        /// <summary>
        /// Iterate all equipment children of a mobile, invoking callback with (layer, graphic, hue).
        /// Used by MobileView.Draw() to render all equipment layers from ECS.
        /// </summary>
        public void ForEachEquipment(uint serial, Action<byte, ushort, ushort> callback)
        {
            if (!_serialToEntity.TryGetValue(serial, out var entity) || !entity.IsAlive())
                return;

            entity.Children((Entity child) =>
            {
                if (!child.Has<LayerComponent>() || !child.Has<GraphicComponent>())
                    return;

                byte layer = child.Get<LayerComponent>().Layer;
                ushort graphic = child.Get<GraphicComponent>().Graphic;
                ushort hue = child.Has<HueComponent>() ? child.Get<HueComponent>().Hue : (ushort)0;
                callback(layer, graphic, hue);
            });
        }

        /// <summary>
        /// Get render-ready data for an item by serial.
        /// Bundles all ECS components that ItemView.Draw() needs.
        /// Returns EcsItemRenderData.Empty if not found.
        /// </summary>
        public EcsItemRenderData GetItemRenderData(uint serial)
        {
            if (!_serialToEntity.TryGetValue(serial, out var entity) || !entity.IsAlive())
                return EcsItemRenderData.Empty;
            if (!entity.Has<ItemTag>())
                return EcsItemRenderData.Empty;

            ushort graphic = entity.Has<GraphicComponent>() ? entity.Get<GraphicComponent>().Graphic : (ushort)0;
            ushort hue = entity.Has<HueComponent>() ? entity.Get<HueComponent>().Hue : (ushort)0;
            ushort amount = entity.Has<AmountComponent>() ? entity.Get<AmountComponent>().Amount : (ushort)0;

            byte alphaHue = 0xFF;
            if (entity.Has<RenderSprite>())
                alphaHue = entity.Get<RenderSprite>().AlphaHue;

            int screenX = 0, screenY = 0;
            if (entity.Has<ScreenPosition>())
            {
                ref readonly var sp = ref entity.Get<ScreenPosition>();
                screenX = sp.X;
                screenY = sp.Y;
            }

            float offsetX = 0, offsetY = 0, offsetZ = 0;
            if (entity.Has<WorldOffset>())
            {
                ref readonly var wo = ref entity.Get<WorldOffset>();
                offsetX = wo.OffsetX;
                offsetY = wo.OffsetY;
                offsetZ = wo.OffsetZ;
            }

            short priorityZ = 0;
            float depthZ = 0;
            if (entity.Has<RenderLayerKey>())
            {
                ref readonly var lk = ref entity.Get<RenderLayerKey>();
                priorityZ = lk.PriorityZ;
                depthZ = lk.DepthZ;
            }

            byte frameIndex = 0;
            bool isAnimated = false;
            if (entity.Has<ItemAnimationState>())
            {
                ref readonly var ia = ref entity.Get<ItemAnimationState>();
                frameIndex = ia.FrameIndex;
                isAnimated = ia.IsAnimated;
            }

            return new EcsItemRenderData(
                graphic, hue, alphaHue, amount,
                screenX, screenY,
                offsetX, offsetY, offsetZ,
                priorityZ, depthZ,
                frameIndex, isAnimated,
                entity.Has<HiddenTag>(),
                entity.Has<CorpseTag>(),
                entity.Has<MultiTag>()
            );
        }

        /// <summary>
        /// Get render-ready data for an effect by serial.
        /// Returns EcsEffectRenderData.Empty if not found.
        /// </summary>
        public EcsEffectRenderData GetEffectRenderData(uint serial)
        {
            if (!_serialToEntity.TryGetValue(serial, out var entity) || !entity.IsAlive())
                return EcsEffectRenderData.Empty;
            if (!entity.Has<EffectTag>())
                return EcsEffectRenderData.Empty;

            ushort graphic = entity.Has<GraphicComponent>() ? entity.Get<GraphicComponent>().Graphic : (ushort)0;
            ushort hue = entity.Has<HueComponent>() ? entity.Get<HueComponent>().Hue : (ushort)0;

            ushort animGraphic = graphic;
            if (entity.Has<EffectAnimPlayback>())
                animGraphic = entity.Get<EffectAnimPlayback>().AnimationGraphic;

            float offsetX = 0, offsetY = 0, offsetZ = 0;
            if (entity.Has<WorldOffset>())
            {
                ref readonly var wo = ref entity.Get<WorldOffset>();
                offsetX = wo.OffsetX;
                offsetY = wo.OffsetY;
                offsetZ = wo.OffsetZ;
            }

            return new EcsEffectRenderData(graphic, animGraphic, hue, offsetX, offsetY, offsetZ);
        }

        // ── UI Query Bridge APIs ─────────────────────────────────────────

        /// <summary>
        /// Get a read-only snapshot of a mobile by serial.
        /// Returns MobileSnapshot.Empty if not found or not a mobile.
        /// </summary>
        public MobileSnapshot GetMobileSnapshot(uint serial)
        {
            if (!_serialToEntity.TryGetValue(serial, out var entity) || !entity.IsAlive())
                return MobileSnapshot.Empty;
            if (!entity.Has<MobileTag>())
                return MobileSnapshot.Empty;

            ref readonly var pos = ref entity.Get<WorldPosition>();
            ushort graphic = entity.Has<GraphicComponent>() ? entity.Get<GraphicComponent>().Graphic : (ushort)0;
            ushort hue = entity.Has<HueComponent>() ? entity.Get<HueComponent>().Hue : (ushort)0;
            byte dir = entity.Has<DirectionComponent>() ? entity.Get<DirectionComponent>().Direction : (byte)0;
            byte notoriety = entity.Has<NotorietyComponent>() ? entity.Get<NotorietyComponent>().Notoriety : (byte)0;
            uint flags = entity.Has<FlagsComponent>() ? entity.Get<FlagsComponent>().Flags : 0;

            Vitals vitals = entity.Has<Vitals>() ? entity.Get<Vitals>() : default;

            return new MobileSnapshot(
                serial, pos.X, pos.Y, pos.Z,
                graphic, hue, dir, notoriety, flags,
                vitals.Hits, vitals.HitsMax,
                vitals.Mana, vitals.ManaMax,
                vitals.Stamina, vitals.StaminaMax,
                entity.Has<PlayerTag>(),
                entity.Has<WarModeTag>(),
                entity.Has<DeadTag>(),
                entity.Has<HiddenTag>());
        }

        /// <summary>
        /// Get a read-only snapshot of an item by serial.
        /// Returns ItemSnapshot.Empty if not found or not an item.
        /// </summary>
        public ItemSnapshot GetItemSnapshot(uint serial)
        {
            if (!_serialToEntity.TryGetValue(serial, out var entity) || !entity.IsAlive())
                return ItemSnapshot.Empty;
            if (!entity.Has<ItemTag>())
                return ItemSnapshot.Empty;

            ref readonly var pos = ref entity.Get<WorldPosition>();
            ushort graphic = entity.Has<GraphicComponent>() ? entity.Get<GraphicComponent>().Graphic : (ushort)0;
            ushort hue = entity.Has<HueComponent>() ? entity.Get<HueComponent>().Hue : (ushort)0;
            ushort amount = entity.Has<AmountComponent>() ? entity.Get<AmountComponent>().Amount : (ushort)0;
            uint flags = entity.Has<FlagsComponent>() ? entity.Get<FlagsComponent>().Flags : 0;
            uint container = entity.Has<ContainerLink>() ? entity.Get<ContainerLink>().ContainerSerial : 0;
            byte layer = entity.Has<LayerComponent>() ? entity.Get<LayerComponent>().Layer : (byte)0;

            return new ItemSnapshot(
                serial, pos.X, pos.Y, pos.Z,
                graphic, hue, amount, flags,
                container, layer,
                entity.Has<OnGroundTag>(),
                entity.Has<MultiTag>(),
                entity.Has<CorpseTag>(),
                (flags & 0x01) != 0);
        }

        /// <summary>
        /// Get the local player's snapshot. Returns MobileSnapshot.Empty if no player.
        /// </summary>
        public MobileSnapshot GetPlayerSnapshot()
        {
            Entity player = default;
            using var q = _world.QueryBuilder<SerialComponent>()
                .With<PlayerTag>()
                .Build();

            q.Each((Entity e, ref SerialComponent s) =>
            {
                player = e;
            });

            if (player == 0 || !player.IsAlive())
                return MobileSnapshot.Empty;

            return GetMobileSnapshot(player.Get<SerialComponent>().Serial);
        }

        /// <summary>
        /// Iterate all mobile entities, invoking a callback with each snapshot.
        /// Uses cached query for optimal performance.
        /// </summary>
        public void ForEachMobile(Action<MobileSnapshot> callback)
        {
            _allMobilesQuery.Each((Entity entity, ref SerialComponent serial, ref WorldPosition pos) =>
            {
                callback(GetMobileSnapshot(serial.Serial));
            });
        }

        /// <summary>
        /// Iterate all item entities, invoking a callback with each snapshot.
        /// Uses cached query for optimal performance.
        /// </summary>
        public void ForEachItem(Action<ItemSnapshot> callback)
        {
            _allItemsQuery.Each((Entity entity, ref SerialComponent serial, ref WorldPosition pos) =>
            {
                callback(GetItemSnapshot(serial.Serial));
            });
        }

        // ── Input Command Bridge APIs ────────────────────────────────────

        /// <summary>Request a player movement step.</summary>
        public void RequestMove(byte direction, bool run)
        {
            EnqueueCommand(new CmdRequestMove { Direction = direction, Run = run });
        }

        /// <summary>Request an attack on a target.</summary>
        public void RequestAttack(uint targetSerial)
        {
            EnqueueCommand(new CmdRequestAttack { TargetSerial = targetSerial });
        }

        /// <summary>Request warmode toggle.</summary>
        public void RequestToggleWarMode(bool warMode)
        {
            EnqueueCommand(new CmdToggleWarMode { WarMode = warMode });
        }

        /// <summary>Request double-click / use on an object.</summary>
        public void RequestUseObject(uint serial)
        {
            EnqueueCommand(new CmdUseObject { Serial = serial });
        }

        /// <summary>Request to pick up an item.</summary>
        public void RequestPickUp(uint serial, ushort amount)
        {
            EnqueueCommand(new CmdPickUp { Serial = serial, Amount = amount });
        }

        /// <summary>Request to drop a held item.</summary>
        public void RequestDropItem(uint serial, ushort x, ushort y, sbyte z, uint containerSerial)
        {
            EnqueueCommand(new CmdDropItem
            {
                Serial = serial, X = x, Y = y, Z = z,
                ContainerSerial = containerSerial
            });
        }

        /// <summary>Set target to an entity.</summary>
        public void RequestTargetEntity(uint serial)
        {
            EnqueueCommand(new CmdTargetEntity { Serial = serial });
        }

        /// <summary>Set target to a ground position.</summary>
        public void RequestTargetPosition(ushort x, ushort y, sbyte z, ushort graphic)
        {
            EnqueueCommand(new CmdTargetPosition { X = x, Y = y, Z = z, Graphic = graphic });
        }

        /// <summary>Cancel current targeting.</summary>
        public void RequestCancelTarget()
        {
            EnqueueCommand(new CmdCancelTarget());
        }

        /// <summary>Request spell cast by index.</summary>
        public void RequestCastSpell(int spellIndex)
        {
            EnqueueCommand(new CmdCastSpell { SpellIndex = spellIndex });
        }

        /// <summary>Request skill use by index.</summary>
        public void RequestUseSkill(int skillIndex)
        {
            EnqueueCommand(new CmdUseSkill { SkillIndex = skillIndex });
        }

        // ── Targeting State Bridge APIs ──────────────────────────────────

        /// <summary>Set the targeting cursor state (from server targeting request).</summary>
        public void SetTargetingState(byte cursorTarget, byte targetType, uint cursorID, ushort multiGraphic = 0)
        {
            ref var ts = ref _world.GetMut<TargetingState>();
            ts = new TargetingState
            {
                CursorTarget = cursorTarget,
                TargetType = targetType,
                CursorID = cursorID,
                MultiGraphic = multiGraphic,
                IsTargeting = cursorTarget != 0
            };
        }

        /// <summary>Current targeting state.</summary>
        public TargetingState GetTargetingState()
        {
            return _world.Get<TargetingState>();
        }

        /// <summary>Last target info for re-targeting.</summary>
        public LastTargetInfo GetLastTargetInfo()
        {
            return _world.Get<LastTargetInfo>();
        }

        // ── Plugin Bridge APIs ───────────────────────────────────────────

        /// <summary>Get player position for plugin queries.</summary>
        public bool GetPlayerPosition(out int x, out int y, out int z)
        {
            var snap = GetPlayerSnapshot();
            if (!snap.Exists)
            {
                x = y = z = 0;
                return false;
            }
            x = snap.X;
            y = snap.Y;
            z = snap.Z;
            return true;
        }

        /// <summary>Get the player's current attack target serial. 0 = none.</summary>
        public uint GetAttackTarget()
        {
            Entity player = default;
            using var q = _world.QueryBuilder<AttackTarget>()
                .With<PlayerTag>()
                .Build();

            q.Each((Entity e, ref AttackTarget at) =>
            {
                player = e;
            });

            if (player == 0 || !player.IsAlive())
                return 0;

            return player.Get<AttackTarget>().TargetSerial;
        }

        /// <summary>Get the player's serial. 0 if no player entity.</summary>
        public uint GetPlayerSerial()
        {
            var snap = GetPlayerSnapshot();
            return snap.Exists ? snap.Serial : 0;
        }

        /// <summary>Chebyshev distance from player to a given serial. -1 if either entity missing.</summary>
        public int DistanceToPlayer(uint serial)
        {
            if (!_serialToEntity.TryGetValue(serial, out var entity) || !entity.IsAlive())
                return -1;
            var snap = GetPlayerSnapshot();
            if (!snap.Exists) return -1;
            ref readonly var pos = ref entity.Get<WorldPosition>();
            int dx = Math.Abs(pos.X - snap.X);
            int dy = Math.Abs(pos.Y - snap.Y);
            return Math.Max(dx, dy);
        }

        /// <summary>Check if a serial is a mobile in ECS.</summary>
        public bool IsMobile(uint serial)
        {
            return _serialToEntity.TryGetValue(serial, out var e) && e.IsAlive() && e.Has<MobileTag>();
        }

        /// <summary>Check if a serial is an item in ECS.</summary>
        public bool IsItem(uint serial)
        {
            return _serialToEntity.TryGetValue(serial, out var e) && e.IsAlive() && e.Has<ItemTag>();
        }

        /// <summary>Check if a mobile has IsHumanTag.</summary>
        public bool IsHumanMobile(uint serial)
        {
            return _serialToEntity.TryGetValue(serial, out var e) && e.IsAlive() && e.Has<IsHumanTag>();
        }

        /// <summary>Get the serial of an item equipped at a given layer on a mobile.</summary>
        public uint GetEquippedItemSerial(uint mobileSerial, byte layer)
        {
            if (!_serialToEntity.TryGetValue(mobileSerial, out var mobile) || !mobile.IsAlive())
                return 0;

            uint result = 0;
            using var q = _world.QueryBuilder<SerialComponent, LayerComponent, ContainerLink>()
                .With<ItemTag>()
                .Build();

            q.Each((ref SerialComponent s, ref LayerComponent lc, ref ContainerLink cl) =>
            {
                if (cl.ContainerSerial == mobileSerial && lc.Layer == layer)
                    result = s.Serial;
            });

            return result;
        }

        /// <summary>Total mobile count.</summary>
        public int MobileCount => _world.Get<ParityCounters>().MobileCount;

        /// <summary>Total item count.</summary>
        public int ItemCount => _world.Get<ParityCounters>().ItemCount;

        // ── String Table APIs ────────────────────────────────────────────

        /// <summary>
        /// Register a string in the string table and return its index.
        /// Used by packet handlers to pass text data through unmanaged commands.
        /// </summary>
        public int RegisterString(string text)
        {
            int index = _stringTable.Count;
            _stringTable.Add(text ?? string.Empty);
            return index;
        }

        /// <summary>Get a string from the string table by index.</summary>
        public string GetString(int index)
        {
            return index >= 0 && index < _stringTable.Count ? _stringTable[index] : string.Empty;
        }

        /// <summary>
        /// Set entity name in the name-by-serial lookup.
        /// Used by CmdUpdateName and speech handlers.
        /// </summary>
        public void SetEntityName(uint serial, string name)
        {
            int index = RegisterString(name);
            _nameBySerial[serial] = index;
        }

        /// <summary>Get entity name by serial. Returns empty if not found.</summary>
        public string GetEntityName(uint serial)
        {
            return _nameBySerial.TryGetValue(serial, out int index) ? GetString(index) : string.Empty;
        }

        /// <summary>Get party state singleton.</summary>
        public PartyState GetPartyState()
        {
            return _world.Get<PartyState>();
        }

        // ── Buff Query APIs ──────────────────────────────────────────────

        /// <summary>
        /// Get active buffs for an entity by serial. Returns list of buff entries.
        /// </summary>
        public List<BuffEntry> GetBuffs(uint serial)
        {
            var result = new List<BuffEntry>();
            if (!_serialToEntity.TryGetValue(serial, out var entity) || !entity.IsAlive())
                return result;

            using var q = _world.QueryBuilder<BuffEntry>()
                .With<BuffTag>()
                .Build();

            q.Each((Entity buffEntity, ref BuffEntry entry) =>
            {
                if (buffEntity.IsChildOf(entity))
                    result.Add(entry);
            });

            return result;
        }

        /// <summary>Get current item hold state.</summary>
        public ItemHoldState GetItemHoldState()
        {
            return _world.Get<ItemHoldState>();
        }

        // ── Render/UI Query APIs ──────────────────────────────────────

        /// <summary>Get the buff bar snapshot for the player.</summary>
        public BuffBarSnapshot GetBuffBarSnapshot()
        {
            return _world.Get<BuffBarSnapshot>();
        }

        /// <summary>Get the status gump snapshot for the player.</summary>
        public StatusSnapshot GetStatusSnapshot()
        {
            return _world.Get<StatusSnapshot>();
        }

        /// <summary>Get weather render data.</summary>
        public WeatherRenderData GetWeatherRenderData()
        {
            return _world.Get<WeatherRenderData>();
        }

        /// <summary>Get the player's sub-tile render offset. Returns false if no player.</summary>
        public bool GetPlayerOffset(out float x, out float y, out float z)
        {
            Entity player = SerialRegistry.FindPlayer(_world);
            if (player == 0 || !player.IsAlive() || !player.Has<WorldOffset>())
            {
                x = y = z = 0;
                return false;
            }
            ref readonly var wo = ref player.Get<WorldOffset>();
            x = wo.OffsetX;
            y = wo.OffsetY;
            z = wo.OffsetZ;
            return true;
        }

        /// <summary>Get the player's race byte. Returns 0 if no player.</summary>
        public byte GetPlayerRace()
        {
            Entity player = SerialRegistry.FindPlayer(_world);
            if (player == 0 || !player.IsAlive() || !player.Has<RaceComponent>())
                return 0;
            return player.Get<RaceComponent>().Race;
        }

        /// <summary>Set current combat abilities from legacy UpdateAbilities().</summary>
        public void SetPlayerAbilities(ushort primary, ushort secondary)
        {
            ref var state = ref _world.GetMut<AbilitiesState>();
            state = new AbilitiesState(primary, secondary);
        }

        /// <summary>Get current combat abilities. Returns (Primary, Secondary) ushorts.</summary>
        public AbilitiesState GetPlayerAbilities()
        {
            return _world.Has<AbilitiesState>() ? _world.Get<AbilitiesState>() : default;
        }

        /// <summary>Get vitals for any entity by serial. Returns default if not found.</summary>
        public Vitals GetEntityVitals(uint serial)
        {
            if (!_serialToEntity.TryGetValue(serial, out var entity) || !entity.IsAlive())
                return default;
            return entity.Has<Vitals>() ? entity.Get<Vitals>() : default;
        }

        /// <summary>Get notoriety byte for any entity. Returns 0 if not found.</summary>
        public byte GetNotoriety(uint serial)
        {
            if (!_serialToEntity.TryGetValue(serial, out var entity) || !entity.IsAlive())
                return 0;
            return entity.Has<NotorietyComponent>() ? entity.Get<NotorietyComponent>().Notoriety : (byte)0;
        }

        /// <summary>Check if entity exists and is alive in ECS.</summary>
        public bool IsEntityAlive(uint serial)
        {
            return _serialToEntity.TryGetValue(serial, out var entity) && entity.IsAlive();
        }

        /// <summary>Get player skills component. Returns default if no player.</summary>
        public SkillsComponent GetPlayerSkills()
        {
            Entity player = SerialRegistry.FindPlayer(_world);
            if (player == 0 || !player.IsAlive() || !player.Has<SkillsComponent>())
                return default;
            return player.Get<SkillsComponent>();
        }

        /// <summary>Get skills for any entity. Returns default if not found.</summary>
        public SkillsComponent GetSkillsForEntity(uint serial)
        {
            if (!_serialToEntity.TryGetValue(serial, out var entity) || !entity.IsAlive())
                return default;
            return entity.Has<SkillsComponent>() ? entity.Get<SkillsComponent>() : default;
        }

        /// <summary>Enumerate children of a container entity, returning item snapshots with container position.</summary>
        public void ForEachContainerItem(uint containerSerial, Action<uint, ushort, ushort, ushort, ushort, ushort> callback)
        {
            if (!_serialToEntity.TryGetValue(containerSerial, out var entity) || !entity.IsAlive())
                return;

            entity.Children((Entity child) =>
            {
                if (!child.Has<SerialComponent>() || !child.Has<GraphicComponent>())
                    return;
                uint serial = child.Get<SerialComponent>().Serial;
                ushort graphic = child.Get<GraphicComponent>().Graphic;
                ushort hue = child.Has<HueComponent>() ? child.Get<HueComponent>().Hue : (ushort)0;
                ushort amount = child.Has<AmountComponent>() ? child.Get<AmountComponent>().Amount : (ushort)0;
                ushort cx = 0, cy = 0;
                if (child.Has<ContainerPosition>())
                {
                    var cp = child.Get<ContainerPosition>();
                    cx = cp.X;
                    cy = cp.Y;
                }
                callback(serial, graphic, hue, amount, cx, cy);
            });
        }

        /// <summary>Set mouse-over entity. Clears previous mouse-over.</summary>
        public void SetMouseOver(uint serial)
        {
            // Clear previous mouse-over
            using var q = _world.QueryBuilder()
                .With<MouseOverTag>()
                .Build();

            q.Run((Iter it) =>
            {
                while (it.Next())
                {
                    for (int i = 0; i < it.Count(); i++)
                        it.Entity(i).Remove<MouseOverTag>();
                }
            });

            // Set new mouse-over
            if (serial != 0 && _serialToEntity.TryGetValue(serial, out var entity) && entity.IsAlive())
            {
                entity.Add<MouseOverTag>();
            }
        }

        /// <summary>Set selected entity.</summary>
        public void SetSelected(uint serial)
        {
            // Clear previous selection
            using var q = _world.QueryBuilder()
                .With<SelectedTag>()
                .Build();

            q.Run((Iter it) =>
            {
                while (it.Next())
                {
                    for (int i = 0; i < it.Count(); i++)
                        it.Entity(i).Remove<SelectedTag>();
                }
            });

            if (serial != 0 && _serialToEntity.TryGetValue(serial, out var entity) && entity.IsAlive())
            {
                entity.Add<SelectedTag>();
            }
        }

        // ── Manager Migration Bridge APIs ──────────────────────────────

        /// <summary>Get delayed click state.</summary>
        public DelayedClickState GetDelayedClickState()
        {
            return _world.Get<DelayedClickState>();
        }

        /// <summary>Register a delayed single-click. System will emit after 500ms.</summary>
        public void RegisterDelayedClick(uint serial, long currentTick)
        {
            ref var dcs = ref _world.GetMut<DelayedClickState>();
            dcs = new DelayedClickState { Serial = serial, ClickTime = currentTick, Pending = true };
        }

        /// <summary>Cancel pending delayed click (e.g., on double-click).</summary>
        public void CancelDelayedClick()
        {
            ref var dcs = ref _world.GetMut<DelayedClickState>();
            dcs = new DelayedClickState();
        }

        /// <summary>Queue an item use with cooldown.</summary>
        public void QueueUseItem(uint serial, long nextUseTime)
        {
            ref var q = ref _world.GetMut<UseItemQueueState>();
            q = new UseItemQueueState { Serial = serial, NextUseTime = nextUseTime, HasPending = true };
        }

        /// <summary>Get use item queue state.</summary>
        public UseItemQueueState GetUseItemQueueState()
        {
            return _world.Get<UseItemQueueState>();
        }

        /// <summary>Get macro bridge state.</summary>
        public MacroState GetMacroState()
        {
            return _world.Get<MacroState>();
        }

        /// <summary>Set macro bridge state.</summary>
        public void SetMacroState(bool isPlaying, bool isRecording, string macroName = null)
        {
            int nameIndex = macroName != null ? RegisterString(macroName) : -1;
            ref var ms = ref _world.GetMut<MacroState>();
            ms = new MacroState { IsPlaying = isPlaying, IsRecording = isRecording, MacroNameIndex = nameIndex };
        }

        /// <summary>Get aura state.</summary>
        public AuraState GetAuraState()
        {
            return _world.Get<AuraState>();
        }

        /// <summary>Set aura state.</summary>
        public void SetAuraState(bool enabled, ushort hue)
        {
            ref var a = ref _world.GetMut<AuraState>();
            a = new AuraState(enabled, hue);
        }

        /// <summary>Get multi-placement state.</summary>
        public MultiPlacement GetMultiPlacement()
        {
            return _world.Get<MultiPlacement>();
        }

        /// <summary>Set multi-placement state for targeting preview.</summary>
        public void SetMultiPlacement(ushort graphic, ushort hue, ushort x, ushort y, sbyte z, bool isValid)
        {
            ref var mp = ref _world.GetMut<MultiPlacement>();
            mp = new MultiPlacement
            {
                Graphic = graphic, Hue = hue,
                X = x, Y = y, Z = z,
                IsValid = isValid
            };
        }

        // ── Diagnostics / Parity / Replay APIs ──────────────────────────

        /// <summary>Current frame diagnostics.</summary>
        public FrameDiagnostics GetFrameDiagnostics()
        {
            return _world.Get<FrameDiagnostics>();
        }

        /// <summary>Current subsystem flags.</summary>
        public SubsystemFlags GetSubsystemFlags()
        {
            return _world.Get<SubsystemFlags>();
        }

        /// <summary>Set subsystem flags for granular rollout control.</summary>
        public void SetSubsystemFlags(SubsystemFlags flags)
        {
            ref var f = ref _world.GetMut<SubsystemFlags>();
            f = flags;
        }

        /// <summary>Current cutover flags.</summary>
        public CutoverFlags GetCutoverFlags()
        {
            return _world.Get<CutoverFlags>();
        }

        /// <summary>Set cutover flags for phased migration control.</summary>
        public void SetCutoverFlags(CutoverFlags flags)
        {
            ref var f = ref _world.GetMut<CutoverFlags>();
            f = flags;
        }

        /// <summary>
        /// Enable the Flecs REST API for development/debug entity inspection.
        /// Opens an HTTP server (default port 27750) for the Flecs Explorer.
        /// Navigate to https://flecs.dev/explorer to inspect.
        /// Only call in development builds.
        /// </summary>
        public void EnableRestApi()
        {
            _world.Import<Ecs.Stats>();
            _world.Set(default(Flecs.NET.Bindings.flecs.EcsRest));
        }

        /// <summary>Create a parity validator bound to this host.</summary>
        public ParityValidator CreateParityValidator()
        {
            return new ParityValidator(this);
        }

        /// <summary>Replay capture instance. Null if not recording.</summary>
        public ReplayCapture ActiveCapture { get; private set; }

        /// <summary>Start recording commands for replay.</summary>
        public void StartReplayCapture()
        {
            var capture = new ReplayCapture();
            ref readonly var timing = ref _world.Get<FrameTiming>();
            capture.StartCapture(timing.Ticks);
            ActiveCapture = capture;
        }

        /// <summary>Stop recording and return the capture.</summary>
        public ReplayCapture StopReplayCapture()
        {
            var capture = ActiveCapture;
            if (capture != null)
                capture.StopCapture();
            ActiveCapture = null;
            return capture;
        }

        // ── World.Update() Retirement (PRD-22) ─────────────────────────

        /// <summary>
        /// Get the current world-map update list (filled by Sim_WorldMapTracking each frame).
        /// </summary>
        public WorldMapUpdateList GetWorldMapUpdateList()
        {
            return _world.Has<WorldMapUpdateList>() ? _world.Get<WorldMapUpdateList>() : default;
        }

        /// <summary>
        /// Get the current map index singleton for WorldMap flush.
        /// </summary>
        public int GetCurrentMapIndex()
        {
            return _world.Has<MapIndex>() ? _world.Get<MapIndex>().Index : 0;
        }
    }
}
