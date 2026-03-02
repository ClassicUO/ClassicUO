// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.ECS.Systems;
using Flecs.NET.Core;

namespace ClassicUO.ECS.Modules
{
    /// <summary>
    /// Network module: command/event ingestion from packet handlers.
    /// Registers command component types and NetApply phase systems.
    /// </summary>
    public struct NetworkModule : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<NetworkModule>();

            // ── Command infrastructure ──────────────────────────────
            world.Component<NetworkCommand>();
            world.Component<SequenceIndex>();
            world.Component<NetDebugCounters>();

            // ── Phase A command types ───────────────────────────────
            world.Component<CmdCreateOrUpdateMobile>();
            world.Component<CmdCreateOrUpdateItem>();
            world.Component<CmdDeleteEntity>();
            world.Component<CmdConfirmWalk>();
            world.Component<CmdDenyWalk>();
            world.Component<CmdMovePlayer>();
            world.Component<CmdContainedItem>();
            world.Component<CmdClearContainer>();
            world.Component<CmdEquipItem>();
            world.Component<CmdOpenContainer>();

            // ── Phase B command types ───────────────────────────────
            world.Component<CmdUpdateVitals>();
            world.Component<CmdSetWarmode>();
            world.Component<CmdSetMap>();

            // Initialize debug counters singleton.
            world.Set(new NetDebugCounters(0, 0, 0));

            // ── Register NetApply + cleanup systems ─────────────────
            NetApplySystems.Register(world);
        }
    }
}
