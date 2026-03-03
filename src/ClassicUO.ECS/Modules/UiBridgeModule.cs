// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.ECS.Systems;
using Flecs.NET.Core;

namespace ClassicUO.ECS.Modules
{
    /// <summary>
    /// UI bridge module: input command processing and targeting state.
    /// </summary>
    public struct UiBridgeModule : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<UiBridgeModule>();

            // ── Input command types ──────────────────────────────────
            world.Component<CmdRequestMove>();
            world.Component<CmdRequestAttack>();
            world.Component<CmdUseObject>();
            world.Component<CmdSingleClick>();
            world.Component<CmdPickUp>();
            world.Component<CmdDropItem>();
            world.Component<CmdEquipRequest>();
            world.Component<CmdToggleWarMode>();
            world.Component<CmdTargetEntity>();
            world.Component<CmdTargetPosition>();
            world.Component<CmdCancelTarget>();
            world.Component<CmdCastSpell>();
            world.Component<CmdUseSkill>();

            // ── Input systems ────────────────────────────────────────
            InputSystems.Register(world);
        }
    }
}
