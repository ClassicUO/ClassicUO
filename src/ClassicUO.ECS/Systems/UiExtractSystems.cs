// SPDX-License-Identifier: BSD-2-Clause

using Flecs.NET.Core;

namespace ClassicUO.ECS.Systems
{
    /// <summary>
    /// UiExtract phase systems that populate UI snapshot singletons
    /// from ECS state. These run after RenderExtract and provide
    /// read-only data for UI gumps.
    /// </summary>
    public static class UiExtractSystems
    {
        public static void Register(World world)
        {
            RegisterBuffBar(world);
            RegisterStatusGump(world);
        }

        // ── Buff bar snapshot (UiExtract) ────────────────────────────

        private static void RegisterBuffBar(World world)
        {
            world.System<BuffBarSnapshot>("UiExtract_BuffBar")
                .Kind(Phases.UiExtract)
                .TermAt(0).Singleton()
                .Run((Iter it) =>
                {
                    while (it.Next())
                    {
                        var w = it.World();

                        // Find the player entity.
                        Entity player = SerialRegistry.FindPlayer(w);
                        if (player == 0 || !player.IsAlive())
                        {
                            ref var empty = ref w.GetMut<BuffBarSnapshot>();
                            empty.Count = 0;
                            return;
                        }

                        // Build snapshot in a local to allow capture in nested lambda.
                        var local = new BuffBarSnapshot();

                        // Query buff children of the player.
                        using var q = w.QueryBuilder<BuffEntry>()
                            .With<BuffTag>()
                            .Build();

                        byte count = 0;
                        q.Each((Entity buffEntity, ref BuffEntry entry) =>
                        {
                            if (count >= 32)
                                return;
                            if (!buffEntity.IsChildOf(player))
                                return;

                            local.Icons[count] = entry;
                            count++;
                        });

                        local.Count = count;
                        ref var snapshot = ref w.GetMut<BuffBarSnapshot>();
                        snapshot = local;
                    }
                });
        }

        // ── Status gump snapshot (UiExtract) ─────────────────────────

        private static void RegisterStatusGump(World world)
        {
            world.System<StatusSnapshot>("UiExtract_StatusGump")
                .Kind(Phases.UiExtract)
                .TermAt(0).Singleton()
                .Run((Iter it) =>
                {
                    while (it.Next())
                    {
                        var w = it.World();
                        ref var snap = ref w.GetMut<StatusSnapshot>();
                        snap.IsValid = false;

                        Entity player = SerialRegistry.FindPlayer(w);
                        if (player == 0 || !player.IsAlive())
                            return;

                        if (!player.Has<SerialComponent>())
                            return;

                        snap.Serial = player.Get<SerialComponent>().Serial;
                        snap.Vitals = player.Has<Vitals>() ? player.Get<Vitals>() : default;
                        snap.Stats = player.Has<PlayerStats>() ? player.Get<PlayerStats>() : default;
                        snap.Locks = player.Has<StatLocks>() ? player.Get<StatLocks>() : default;
                        snap.Race = player.Has<RaceComponent>() ? player.Get<RaceComponent>().Race : (byte)0;
                        snap.IsFemale = player.Has<FemaleTag>();
                        snap.IsValid = true;
                    }
                });
        }
    }
}
