// SPDX-License-Identifier: BSD-2-Clause

using Flecs.NET.Core;

namespace ClassicUO.ECS.Systems
{
    /// <summary>
    /// Manager migration systems: delayed click, use item queue.
    /// These run in the Simulation phase.
    /// </summary>
    public static class ManagerSystems
    {
        private const long DELAYED_CLICK_MS = 500;
        private const long USE_ITEM_COOLDOWN_MS = 1000;

        public static void Register(World world)
        {
            RegisterDelayedClick(world);
            RegisterUseItemQueue(world);
        }

        // ── Delayed Click (Simulation) ──────────────────────────────────

        private static void RegisterDelayedClick(World world)
        {
            world.System<DelayedClickState, FrameTiming>("Sim_DelayedClick")
                .Kind(Phases.Simulation)
                .TermAt(0).Singleton()
                .TermAt(1).Singleton()
                .Run((Iter it) =>
                {
                    while (it.Next())
                    {
                        var w = it.World();
                        ref var dcs = ref w.GetMut<DelayedClickState>();
                        ref readonly var ft = ref w.Get<FrameTiming>();

                        if (!dcs.Pending)
                            return;

                        long elapsed = (long)ft.Ticks - dcs.ClickTime;
                        if (elapsed < DELAYED_CLICK_MS)
                            return;

                        // 500ms elapsed with no cancellation — emit single click.
                        uint serial = dcs.Serial;
                        dcs = new DelayedClickState(); // clear

                        w.Entity()
                            .Add<NetworkCommand>()
                            .Set(new CmdSingleClick { Serial = serial });
                    }
                });
        }

        // ── Use Item Queue (Simulation) ──────────────────────────────────

        private static void RegisterUseItemQueue(World world)
        {
            world.System<UseItemQueueState, FrameTiming>("Sim_UseItemQueue")
                .Kind(Phases.Simulation)
                .TermAt(0).Singleton()
                .TermAt(1).Singleton()
                .Run((Iter it) =>
                {
                    while (it.Next())
                    {
                        var w = it.World();
                        ref var uiq = ref w.GetMut<UseItemQueueState>();
                        ref readonly var ft = ref w.Get<FrameTiming>();

                        if (!uiq.HasPending)
                            return;

                        if ((long)ft.Ticks < uiq.NextUseTime)
                            return;

                        // Cooldown expired — emit use object command.
                        uint serial = uiq.Serial;
                        uiq = new UseItemQueueState(); // clear

                        w.Entity()
                            .Add<NetworkCommand>()
                            .Set(new CmdUseObject { Serial = serial });
                    }
                });
        }
    }
}
