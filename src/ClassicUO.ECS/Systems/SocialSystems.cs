// SPDX-License-Identifier: BSD-2-Clause

using Flecs.NET.Core;

namespace ClassicUO.ECS.Systems
{
    /// <summary>
    /// Social systems: name updates, party management, overhead text lifecycle.
    /// </summary>
    public static class SocialSystems
    {
        private const ushort DEFAULT_TEXT_DURATION_MS = 3000;
        private const ushort MAX_TEXT_DURATION_MS = 10000;

        public static void Register(World world)
        {
            RegisterApplyUpdateName(world);
            RegisterApplyPartyAddMember(world);
            RegisterApplyPartyRemoveMember(world);
            RegisterApplyPartyDisband(world);
            RegisterApplySpeech(world);
            RegisterExpireOverheadText(world);
        }

        // ── Name update (NetApply) ──────────────────────────────────

        private static void RegisterApplyUpdateName(World world)
        {
            world.System<CmdUpdateName, NetDebugCounters>("NetApply_UpdateName")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .Each((Entity cmdEntity, ref CmdUpdateName cmd, ref NetDebugCounters counters) =>
                {
                    Entity target = SerialRegistry.FindBySerial(cmd.Serial);
                    if (target != 0 && target.IsAlive())
                    {
                        target.Set(new NameIndex(cmd.NameTableIndex));
                    }
                    counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                });
        }

        // ── Party: Add member (NetApply) ────────────────────────────

        private static void RegisterApplyPartyAddMember(World world)
        {
            world.System<CmdPartyAddMember, PartyState, NetDebugCounters>("NetApply_PartyAddMember")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .TermAt(2).Singleton()
                .Each((Entity cmdEntity, ref CmdPartyAddMember cmd, ref PartyState party, ref NetDebugCounters counters) =>
                {
                    Entity member = SerialRegistry.FindBySerial(cmd.Serial);
                    if (member != 0 && member.IsAlive())
                    {
                        if (!member.Has<PartyTag>())
                            member.Add<PartyTag>();
                    }

                    party.IsInParty = true;
                    party.MemberCount++;
                    counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                });
        }

        // ── Party: Remove member (NetApply) ─────────────────────────

        private static void RegisterApplyPartyRemoveMember(World world)
        {
            world.System<CmdPartyRemoveMember, PartyState, NetDebugCounters>("NetApply_PartyRemoveMember")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .TermAt(2).Singleton()
                .Each((Entity cmdEntity, ref CmdPartyRemoveMember cmd, ref PartyState party, ref NetDebugCounters counters) =>
                {
                    Entity member = SerialRegistry.FindBySerial(cmd.Serial);
                    if (member != 0 && member.IsAlive())
                    {
                        if (member.Has<PartyTag>())
                            member.Remove<PartyTag>();
                    }

                    if (party.MemberCount > 0)
                        party.MemberCount--;
                    if (party.MemberCount == 0)
                        party.IsInParty = false;

                    counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                });
        }

        // ── Party: Disband (NetApply) ───────────────────────────────

        private static void RegisterApplyPartyDisband(World world)
        {
            world.System("NetApply_PartyDisband")
                .Kind(Phases.NetApply)
                .With<CmdPartyDisband>()
                .With<NetworkCommand>()
                .Run((Iter it) =>
                {
                    while (it.Next())
                    {
                        var w = it.World();

                        // Remove PartyTag from all mobiles.
                        using var q = w.QueryBuilder()
                            .With<PartyTag>()
                            .With<MobileTag>()
                            .Build();

                        q.Run((Iter inner) =>
                        {
                            while (inner.Next())
                            {
                                for (int i = 0; i < inner.Count(); i++)
                                {
                                    inner.Entity(i).Remove<PartyTag>();
                                }
                            }
                        });

                        ref var party = ref w.GetMut<PartyState>();
                        party = new PartyState { LeaderSerial = 0, MemberCount = 0, IsInParty = false };

                        ref var counters = ref w.GetMut<NetDebugCounters>();
                        counters = counters with { CommandsApplied = counters.CommandsApplied + it.Count() };
                    }
                });
        }

        // ── Speech / Overhead text (NetApply) ───────────────────────

        private static void RegisterApplySpeech(World world)
        {
            world.System<CmdSpeech, NetDebugCounters, FrameTiming>("NetApply_Speech")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .TermAt(2).Singleton()
                .Each((Entity cmdEntity, ref CmdSpeech cmd, ref NetDebugCounters counters, ref FrameTiming ft) =>
                {
                    long ticks = (long)ft.Ticks;

                    // Duration based on text length (rough approximation).
                    ushort durationMs = DEFAULT_TEXT_DURATION_MS;
                    if (durationMs > MAX_TEXT_DURATION_MS)
                        durationMs = MAX_TEXT_DURATION_MS;

                    var w = cmdEntity.CsWorld();
                    var textEntity = w.Entity()
                        .Add<OverheadTextTag>()
                        .Set(new OverheadText
                        {
                            SourceSerial = cmd.Serial,
                            Hue = cmd.Hue,
                            Type = cmd.Type,
                            StartTick = ticks,
                            DurationMs = durationMs,
                            TextIndex = cmd.TextIndex
                        });

                    // Parent to source entity if it exists.
                    if (cmd.Serial != 0)
                    {
                        Entity source = SerialRegistry.FindBySerial(cmd.Serial);
                        if (source != 0 && source.IsAlive())
                            textEntity.ChildOf(source);
                    }

                    counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                });
        }

        // ── Expire overhead text (Simulation) ──────────────────────

        private static void RegisterExpireOverheadText(World world)
        {
            world.System<OverheadText, FrameTiming>("Sim_ExpireOverheadText")
                .Kind(Phases.Simulation)
                .With<OverheadTextTag>()
                .TermAt(1).Singleton()
                .Each((Entity entity, ref OverheadText text, ref FrameTiming ft) =>
                {
                    long expiryTick = text.StartTick + text.DurationMs;
                    if ((long)ft.Ticks >= expiryTick)
                    {
                        entity.Destruct();
                    }
                });
        }
    }
}
