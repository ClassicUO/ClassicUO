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

        private EcsRuntimeHost(World world)
        {
            _world = world;
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
            return host;
        }

        public void Dispose()
        {
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

            // 4. Register parity-tracking observers
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
            if (_serialToEntity.TryGetValue(serial, out var entity) && entity.IsAlive())
                return entity;

            entity = _world.Entity()
                .Set(new SerialComponent(serial));
            return entity;
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
    }
}
