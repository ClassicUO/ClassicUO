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

            // ── Enter World command ────────────────────────────────
            world.Component<CmdEnterWorld>();

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

            // ── Animation command types ──────────────────────────────
            world.Component<CmdCharacterAnimation>();
            world.Component<CmdNewCharacterAnimation>();

            // ── Stats / Skills command types ─────────────────────────
            world.Component<CmdCharacterStatus>();
            world.Component<CmdUpdateSkill>();
            world.Component<CmdExtendedStats>();
            world.Component<CmdOplRevision>();

            // ── Death / Corpse command types ─────────────────────────
            world.Component<CmdDisplayDeath>();
            world.Component<CmdDeathScreen>();
            world.Component<CmdCorpseEquipment>();

            // ── Phase B command types ───────────────────────────────
            world.Component<CmdUpdateVitals>();
            world.Component<CmdSetWarmode>();
            world.Component<CmdSetMap>();
            world.Component<CmdChangeSeason>();
            world.Component<CmdAttackTarget>();
            world.Component<CmdSwing>();
            world.Component<CmdDamage>();

            // ── World / environment command types ────────────────────────
            world.Component<CmdBoatMoving>();
            world.Component<CmdBoatEntityUpdate>();
            world.Component<CmdPathfind>();
            world.Component<CmdSetWeather>();
            world.Component<CmdPlaySound>();
            world.Component<CmdPlayMusic>();
            world.Component<CmdHouseRevision>();

            // ── Name / social command types ──────────────────────────────
            world.Component<CmdUpdateName>();
            world.Component<CmdPartyAddMember>();
            world.Component<CmdPartyRemoveMember>();
            world.Component<CmdPartyDisband>();
            world.Component<CmdSpeech>();

            // ── Health bar / buff command types ──────────────────────────
            world.Component<CmdHealthBarUpdate>();
            world.Component<CmdAddBuff>();
            world.Component<CmdRemoveBuff>();
            world.Component<CmdMobileStatus>();

            // ── Effect spawn command types ──────────────────────────────
            world.Component<CmdSpawnEffect>();
            world.Component<CmdDragEffect>();

            // ── Item hold command types ────────────────────────────────
            world.Component<CmdDenyMoveItem>();
            world.Component<CmdEndDragging>();
            world.Component<CmdDropItemAccepted>();

            // ── Singleton-update command types ────────────────────────
            world.Component<CmdSetViewRange>();
            world.Component<CmdSetLockedFeatures>();
            world.Component<CmdSetSpeedMode>();
            world.Component<CmdSetPersonalLight>();
            world.Component<CmdSetOverallLight>();

            // Initialize debug counters singleton.
            world.Set(new NetDebugCounters(0, 0, 0));

            // ── Register NetApply + cleanup systems ─────────────────
            NetApplySystems.Register(world);
            SingletonUpdateSystems.Register(world);
            StatsSystems.Register(world);
            AnimationSystems.Register(world);
            BuffSystems.Register(world);
            SocialSystems.Register(world);
            WorldSystems.Register(world);
        }
    }
}
