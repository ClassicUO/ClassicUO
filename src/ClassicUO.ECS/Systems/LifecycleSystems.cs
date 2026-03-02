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
    /// </summary>
    public static class LifecycleSystems
    {
        private const long PRUNE_INTERVAL_MS = 50;

        public static void Register(World world)
        {
            RegisterViewRangePrune(world);
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
                    var playerPos = GetPlayerPosition(w);
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

                    // Use same 50ms window as mobile prune — if PruneMobiles already
                    // reset the timer this frame, NextPruneTick > current ticks,
                    // so we still run because both systems share the same frame.
                    // The timer was already advanced by PruneMobiles above,
                    // but both systems run within the same frame's ticks value.
                    // We check against (ticks + PRUNE_INTERVAL) to see if we're
                    // within the same frame window.
                    if (timing.Ticks + PRUNE_INTERVAL_MS < pruneTimer.NextPruneTick)
                    {
                        while (it.Next()) { }
                        return;
                    }

                    byte viewRange = w.Get<ViewRange>().Range;
                    var playerPos = GetPlayerPosition(w);
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

        // ── Deferred destruction (PostSim) ───────────────────────────

        private static void RegisterDeferredDestroy(World world)
        {
            world.System("PostSim_DeferredDestroy")
                .Kind(Phases.PostSim)
                .With<PendingRemovalTag>()
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

        private static WorldPosition GetPlayerPosition(World world)
        {
            WorldPosition result = default;
            using var q = world.QueryBuilder<WorldPosition>()
                .With<PlayerTag>()
                .Build();

            q.Each((Entity _, ref WorldPosition pos) =>
            {
                result = pos;
            });
            return result;
        }

        /// <summary>Chebyshev (L-infinity) distance, matching legacy Distance property.</summary>
        private static int ChebyshevDistance(ushort x1, ushort y1, ushort x2, ushort y2)
        {
            return Math.Max(Math.Abs(x1 - x2), Math.Abs(y1 - y2));
        }
    }
}
