// SPDX-License-Identifier: BSD-2-Clause

using Flecs.NET.Core;

namespace ClassicUO.ECS.Systems
{
    /// <summary>
    /// NetApply systems that update ECS singletons from server packets.
    /// Each system reads a command and writes to the corresponding singleton
    /// via .TermAt(N).Singleton().
    /// </summary>
    public static class SingletonUpdateSystems
    {
        public static void Register(World world)
        {
            RegisterSetViewRange(world);
            RegisterSetLockedFeatures(world);
            RegisterSetSpeedMode(world);
            RegisterSetPersonalLight(world);
            RegisterSetOverallLight(world);
        }

        private static void RegisterSetViewRange(World world)
        {
            world.System<CmdSetViewRange, ViewRange, NetDebugCounters>("NetApply_SetViewRange")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .TermAt(2).Singleton()
                .Each((Entity cmdEntity, ref CmdSetViewRange cmd, ref ViewRange vr, ref NetDebugCounters counters) =>
                {
                    vr = new ViewRange(cmd.Range);
                    counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                });
        }

        private static void RegisterSetLockedFeatures(World world)
        {
            world.System<CmdSetLockedFeatures, EcsLockedFeatureFlags, NetDebugCounters>("NetApply_SetLockedFeatures")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .TermAt(2).Singleton()
                .Each((Entity cmdEntity, ref CmdSetLockedFeatures cmd, ref EcsLockedFeatureFlags flags, ref NetDebugCounters counters) =>
                {
                    flags = new EcsLockedFeatureFlags(cmd.Flags);
                    counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                });
        }

        private static void RegisterSetSpeedMode(World world)
        {
            world.System<CmdSetSpeedMode, NetDebugCounters>("NetApply_SetSpeedMode")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .Each((Entity cmdEntity, ref CmdSetSpeedMode cmd, ref NetDebugCounters counters) =>
                {
                    // Set SpeedMode on the player entity.
                    Entity player = SerialRegistry.FindPlayer(cmdEntity.CsWorld());
                    if (player != 0)
                        player.Set(new SpeedModeComponent(cmd.Mode));

                    counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                });
        }

        private static void RegisterSetPersonalLight(World world)
        {
            world.System<CmdSetPersonalLight, LightingState, NetDebugCounters>("NetApply_SetPersonalLight")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .TermAt(2).Singleton()
                .Each((Entity cmdEntity, ref CmdSetPersonalLight cmd, ref LightingState ls, ref NetDebugCounters counters) =>
                {
                    ls = ls with { Personal = cmd.Level, RealPersonal = cmd.Level };
                    counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                });
        }

        private static void RegisterSetOverallLight(World world)
        {
            world.System<CmdSetOverallLight, LightingState, NetDebugCounters>("NetApply_SetOverallLight")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .TermAt(2).Singleton()
                .Each((Entity cmdEntity, ref CmdSetOverallLight cmd, ref LightingState ls, ref NetDebugCounters counters) =>
                {
                    ls = ls with { Overall = cmd.Level, RealOverall = cmd.Level };
                    counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                });
        }

    }
}
