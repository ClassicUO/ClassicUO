// SPDX-License-Identifier: BSD-2-Clause

using Flecs.NET.Core;

namespace ClassicUO.ECS.Systems
{
    /// <summary>
    /// Map systems: map index changes, season updates, ground entity cleanup.
    ///
    /// When the server sends a map change:
    ///   1. Update MapIndex singleton
    ///   2. Destroy all ground entities (mobiles, items, effects) except player inventory
    ///   3. Season is reset to default (server sends new season separately)
    ///
    /// Chunk loading/unloading is handled by the legacy map system via bridge —
    /// ECS tracks entity lifecycle only.
    /// </summary>
    public static class MapSystems
    {
        public static void Register(World world)
        {
            RegisterApplySetMap(world);
            RegisterApplyChangeSeason(world);
        }

        // ── Map change (NetApply) ─────────────────────────────────────

        private static void RegisterApplySetMap(World world)
        {
            world.System<CmdSetMap, MapIndex, NetDebugCounters>("NetApply_SetMap")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .TermAt(2).Singleton()
                .Each((Entity cmdEntity, ref CmdSetMap cmd, ref MapIndex mapIdx, ref NetDebugCounters counters) =>
                {
                    byte oldMap = mapIdx.Index;
                    mapIdx = new MapIndex(cmd.MapIndex);

                    if (oldMap != cmd.MapIndex)
                    {
                        // Destroy all ground entities on map change.
                        // Player entity and their inventory are preserved.
                        DestroyGroundEntities(cmdEntity.CsWorld());
                    }

                    counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                });
        }

        // ── Season change (NetApply) ──────────────────────────────────

        private static void RegisterApplyChangeSeason(World world)
        {
            world.System<CmdChangeSeason, SeasonState, NetDebugCounters>("NetApply_ChangeSeason")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .TermAt(2).Singleton()
                .Each((Entity cmdEntity, ref CmdChangeSeason cmd, ref SeasonState season, ref NetDebugCounters counters) =>
                {
                    season = new SeasonState(cmd.Season);

                    counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                });
        }

        // ── Helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Destroy all ground-level entities (mobiles not player, items on ground, effects).
        /// Mirrors legacy InternalMapChangeClear() behavior.
        /// </summary>
        private static void DestroyGroundEntities(World world)
        {
            var toDestroy = new System.Collections.Generic.List<Entity>();

            // Destroy non-player mobiles.
            using var mq = world.QueryBuilder<SerialComponent>()
                .With<MobileTag>()
                .Without<PlayerTag>()
                .Without<PendingRemovalTag>()
                .Build();

            mq.Each((Entity e, ref SerialComponent _) =>
            {
                toDestroy.Add(e);
            });

            // Destroy ground items (not in containers).
            using var iq = world.QueryBuilder<SerialComponent>()
                .With<ItemTag>()
                .With<OnGroundTag>()
                .Without<PendingRemovalTag>()
                .Build();

            iq.Each((Entity e, ref SerialComponent _) =>
            {
                toDestroy.Add(e);
            });

            // Destroy effects.
            using var eq = world.QueryBuilder<SerialComponent>()
                .With<EffectTag>()
                .Without<PendingRemovalTag>()
                .Build();

            eq.Each((Entity e, ref SerialComponent _) =>
            {
                toDestroy.Add(e);
            });

            // Apply deferred destruction.
            foreach (var e in toDestroy)
            {
                if (e.IsAlive() && !e.Has<PendingRemovalTag>())
                    e.Add<PendingRemovalTag>();
            }
        }
    }
}
