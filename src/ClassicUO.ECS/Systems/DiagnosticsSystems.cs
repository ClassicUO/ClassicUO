// SPDX-License-Identifier: BSD-2-Clause

using Flecs.NET.Core;

namespace ClassicUO.ECS.Systems
{
    /// <summary>
    /// PostFrame diagnostics collection system.
    /// Gathers per-frame entity counts and command stats into
    /// the FrameDiagnostics singleton for performance dashboards.
    /// </summary>
    public static class DiagnosticsSystems
    {
        public static void Register(World world)
        {
            RegisterCollectDiagnostics(world);
        }

        private static void RegisterCollectDiagnostics(World world)
        {
            world.System<FrameTiming, ParityCounters, NetDebugCounters, FrameDiagnostics>(
                    "PostFrame_CollectDiagnostics")
                .Kind(Phases.PostFrame)
                .TermAt(0).Singleton()
                .TermAt(1).Singleton()
                .TermAt(2).Singleton()
                .TermAt(3).Singleton()
                .Run((Iter it) =>
                {
                    while (it.Next())
                    {
                        var w = it.World();
                        ref readonly var timing = ref w.Get<FrameTiming>();
                        ref readonly var parity = ref w.Get<ParityCounters>();
                        ref readonly var netDbg = ref w.Get<NetDebugCounters>();

                        int effectCount = 0;
                        using var eq = w.QueryBuilder<SerialComponent>()
                            .With<EffectTag>()
                            .Without<PendingRemovalTag>()
                            .Build();
                        eq.Run((Iter eit) =>
                        {
                            while (eit.Next())
                                effectCount += eit.Count();
                        });

                        int totalEntities = parity.MobileCount + parity.ItemCount + effectCount;

                        ref var diag = ref w.GetMut<FrameDiagnostics>();
                        diag = new FrameDiagnostics(
                            EntityCount: totalEntities,
                            MobileCount: parity.MobileCount,
                            ItemCount: parity.ItemCount,
                            EffectCount: effectCount,
                            CommandsProcessed: netDbg.CommandsApplied,
                            FrameTimeMs: timing.DeltaSeconds * 1000f
                        );
                    }
                });
        }
    }
}
