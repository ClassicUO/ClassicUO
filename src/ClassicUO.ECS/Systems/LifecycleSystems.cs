// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using Flecs.NET.Core;

namespace ClassicUO.ECS.Systems
{
    /// <summary>
    /// Entity lifecycle systems: distance-based pruning and deferred destruction.
    ///
    /// Mirrors legacy World.Update() behavior:
    ///   - Every 50ms, check entity distance from player using Chebyshev metric
    ///   - Entities beyond ClientViewRange get PendingRemovalTag
    ///   - PendingRemovalTag entities are destructed in PostSim cleanup
    ///   - Optional 1-frame delay via PendingRemovalDelay
    ///   - EntityRemovedEvent emitted for UI bridge consumption
    /// </summary>
    public static class LifecycleSystems
    {
        private const long PRUNE_INTERVAL_MS = 50;

        public static void Register(World world)
        {
            RegisterViewRangePrune(world);
            RegisterRemovalObserver(world);
            RegisterDelayedRemoval(world);
            RegisterDeferredDestroy(world);
        }

        // ── View-range pruning (PreSim) ──────────────────────────────

        private static void RegisterViewRangePrune(World world)
        {
            // Prune mobiles beyond view range (throttled to every 50ms).
            world.System<WorldPosition, SerialComponent>("PreSim_PruneMobiles")
                .Kind(Phases.PreSim)
                .With<MobileTag>()
                .Without<PlayerTag>()
                .Without<PendingRemovalTag>()
                .Run((Iter it) =>
                {
                    var w = it.World();
                    ref readonly var timing = ref w.Get<FrameTiming>();
                    ref readonly var pruneTimer = ref w.Get<PruneTimer>();

                    if (timing.Ticks < pruneTimer.NextPruneTick)
                    {
                        // Skip iteration entirely when not due for pruning.
                        while (it.Next()) { }
                        return;
                    }

                    ref var pt = ref w.GetMut<PruneTimer>();
                    pt = new PruneTimer(timing.Ticks + PRUNE_INTERVAL_MS);

                    byte viewRange = w.Get<ViewRange>().Range;
                    var playerPos = SerialRegistry.GetPlayerPosition(w);
                    if (playerPos.X == 0 && playerPos.Y == 0)
                    {
                        while (it.Next()) { }
                        return; // No player yet
                    }

                    while (it.Next())
                    {
                        var positions = it.Field<WorldPosition>(0);
                        for (int i = 0; i < it.Count(); i++)
                        {
                            ref readonly var pos = ref positions[i];
                            int dist = ChebyshevDistance(playerPos.X, playerPos.Y, pos.X, pos.Y);
                            if (dist > viewRange)
                            {
                                it.Entity(i).Add<PendingRemovalTag>();
                            }
                        }
                    }
                });

            // Prune ground items beyond view range (same throttle via PruneTimer).
            world.System<WorldPosition, SerialComponent>("PreSim_PruneItems")
                .Kind(Phases.PreSim)
                .With<ItemTag>()
                .With<OnGroundTag>()
                .Without<MultiTag>()
                .Without<PendingRemovalTag>()
                .Run((Iter it) =>
                {
                    var w = it.World();
                    ref readonly var timing = ref w.Get<FrameTiming>();
                    ref readonly var pruneTimer = ref w.Get<PruneTimer>();

                    if (timing.Ticks + PRUNE_INTERVAL_MS < pruneTimer.NextPruneTick)
                    {
                        while (it.Next()) { }
                        return;
                    }

                    byte viewRange = w.Get<ViewRange>().Range;
                    var playerPos = SerialRegistry.GetPlayerPosition(w);
                    if (playerPos.X == 0 && playerPos.Y == 0)
                    {
                        while (it.Next()) { }
                        return;
                    }

                    while (it.Next())
                    {
                        var positions = it.Field<WorldPosition>(0);
                        for (int i = 0; i < it.Count(); i++)
                        {
                            ref readonly var pos = ref positions[i];
                            int dist = ChebyshevDistance(playerPos.X, playerPos.Y, pos.X, pos.Y);
                            if (dist > viewRange)
                            {
                                it.Entity(i).Add<PendingRemovalTag>();
                            }
                        }
                    }
                });
        }

        // ── Removal observer (emit EntityRemovedEvent on PendingRemovalTag) ──

        private static void RegisterRemovalObserver(World world)
        {
            // When PendingRemovalTag is added, emit an EntityRemovedEvent
            // for UI bridge consumption and mark equipped items for recalc.
            world.Observer("OnAdd_PendingRemoval")
                .With<PendingRemovalTag>()
                .Event(Ecs.OnAdd)
                .Run((Iter it) =>
                {
                    while (it.Next())
                    {
                        var w = it.World();
                        for (int i = 0; i < it.Count(); i++)
                        {
                            var entity = it.Entity(i);
                            if (!entity.IsAlive())
                                continue;

                            // Emit removal event if entity has a serial.
                            if (entity.Has<SerialComponent>())
                            {
                                ref readonly var serial = ref entity.Get<SerialComponent>();
                                w.Entity()
                                    .Set(new EntityRemovedEvent(serial.Serial))
                                    .Add<PendingRemovalTag>();
                            }

                            // If an equipped item is being removed, flag parent for recalc.
                            if (entity.Has<ItemTag>() && entity.Has<ContainerLink>())
                            {
                                ref readonly var link = ref entity.Get<ContainerLink>();
                                Entity parent = SerialRegistry.FindBySerial(link.ContainerSerial);
                                if (parent != 0 && parent.IsAlive() && parent.Has<MobileTag>())
                                {
                                    if (!parent.Has<RecalcAbilitiesTag>())
                                        parent.Add<RecalcAbilitiesTag>();
                                }
                            }
                        }
                    }
                });
        }

        // ── Delayed removal (PostSim) ─────────────────────────────────

        private static void RegisterDelayedRemoval(World world)
        {
            // Entities with PendingRemovalDelay get their counter decremented.
            // When it reaches 0, the delay component is removed and they proceed
            // to normal deferred destroy.
            world.System<PendingRemovalDelay>("PostSim_DelayedRemoval")
                .Kind(Phases.PostSim)
                .With<PendingRemovalTag>()
                .Each((Entity entity, ref PendingRemovalDelay delay) =>
                {
                    if (delay.FramesRemaining > 1)
                    {
                        delay = delay with { FramesRemaining = (byte)(delay.FramesRemaining - 1) };
                    }
                    else
                    {
                        entity.Remove<PendingRemovalDelay>();
                    }
                });
        }

        // ── Deferred destruction (PostSim) ───────────────────────────

        private static void RegisterDeferredDestroy(World world)
        {
            // Destroy entities that have PendingRemovalTag but NOT PendingRemovalDelay.
            world.System("PostSim_DeferredDestroy")
                .Kind(Phases.PostSim)
                .With<PendingRemovalTag>()
                .Without<PendingRemovalDelay>()
                .Without<EntityRemovedEvent>()
                .Run((Iter it) =>
                {
                    while (it.Next())
                    {
                        for (int i = 0; i < it.Count(); i++)
                        {
                            var entity = it.Entity(i);
                            if (entity.IsAlive())
                                entity.Destruct();
                        }
                    }
                });
        }

        // ── Helpers ──────────────────────────────────────────────────

        /// <summary>Chebyshev (L-infinity) distance, matching legacy Distance property.</summary>
        private static int ChebyshevDistance(ushort x1, ushort y1, ushort x2, ushort y2)
        {
            return Math.Max(Math.Abs(x1 - x2), Math.Abs(y1 - y2));
        }
    }
}
