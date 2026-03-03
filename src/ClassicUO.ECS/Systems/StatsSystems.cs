// SPDX-License-Identifier: BSD-2-Clause

using Flecs.NET.Core;

namespace ClassicUO.ECS.Systems
{
    /// <summary>
    /// NetApply systems for stats, skills, extended stats, and OPL.
    /// </summary>
    public static class StatsSystems
    {
        public static void Register(World world)
        {
            RegisterApplyCharacterStatus(world);
            RegisterApplyUpdateSkill(world);
            RegisterApplyExtendedStats(world);
            RegisterApplyOplRevision(world);
        }

        // ── Character Status (0x11) ──────────────────────────────────

        private static void RegisterApplyCharacterStatus(World world)
        {
            world.System<CmdCharacterStatus, NetDebugCounters>("NetApply_CharacterStatus")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .Each((Entity cmdEntity, ref CmdCharacterStatus cmd, ref NetDebugCounters counters) =>
                {
                    Entity target = SerialRegistry.FindBySerial(cmd.Serial);
                    if (target == 0 || !target.IsAlive())
                    {
                        counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                        return;
                    }

                    // Update Vitals on all mobiles.
                    target.Set(new Vitals(
                        cmd.Hits, cmd.HitsMax,
                        cmd.Mana, cmd.ManaMax,
                        cmd.Stamina, cmd.StaminaMax));

                    // Extended stats (player only, StatusType >= 3).
                    if (cmd.StatusType >= 3 && target.Has<PlayerTag>())
                    {
                        target.Set(new PlayerStats(
                            cmd.Str, cmd.Dex, cmd.Int,
                            cmd.Gold, cmd.Weight, cmd.WeightMax,
                            cmd.PhysResist, cmd.FireResist, cmd.ColdResist,
                            cmd.PoisonResist, cmd.EnergyResist,
                            cmd.Luck, cmd.DamageMin, cmd.DamageMax,
                            cmd.TithingPoints, cmd.StatsCap,
                            cmd.Followers, cmd.FollowersMax,
                            cmd.MaxPhysResist, cmd.MaxFireResist, cmd.MaxColdResist,
                            cmd.MaxPoisonResist, cmd.MaxEnergyResist,
                            cmd.DefenseChanceInc, cmd.MaxDefenseChanceInc,
                            cmd.HitChanceInc, cmd.SwingSpeedInc, cmd.DamageInc,
                            cmd.LowerReagentCost, cmd.SpellDamageInc,
                            cmd.FasterCastRecovery, cmd.FasterCasting, cmd.LowerManaCost));

                        if (cmd.Race != 0)
                            target.Set(new RaceComponent(cmd.Race));
                    }

                    counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                });
        }

        // ── Skill Update (0x3A) ──────────────────────────────────────

        private static void RegisterApplyUpdateSkill(World world)
        {
            world.System<CmdUpdateSkill, NetDebugCounters>("NetApply_UpdateSkill")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .Each((Entity cmdEntity, ref CmdUpdateSkill cmd, ref NetDebugCounters counters) =>
                {
                    // Skills apply to the player entity.
                    Entity player = SerialRegistry.FindPlayer(cmdEntity.CsWorld());
                    if (player == 0)
                    {
                        counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                        return;
                    }

                    // Use local copy: Set on non-iterated entity is deferred,
                    // so Get after Set would read stale/missing data.
                    var skills = player.Has<SkillsComponent>()
                        ? player.Get<SkillsComponent>()
                        : new SkillsComponent();
                    if (cmd.SkillId < 60)
                    {
                        skills.Skills[cmd.SkillId] = new EcsSkillEntry
                        {
                            Value = cmd.Value,
                            Base = cmd.Base,
                            Cap = cmd.Cap,
                            Lock = cmd.Lock
                        };
                        if (cmd.SkillId >= skills.SkillCount)
                            skills.SkillCount = (byte)(cmd.SkillId + 1);
                    }
                    player.Set(skills);

                    counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                });
        }

        // ── Extended Stats (0xBF sub-0x19) ────────────────────────────

        private static void RegisterApplyExtendedStats(World world)
        {
            world.System<CmdExtendedStats, NetDebugCounters>("NetApply_ExtendedStats")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .Each((Entity cmdEntity, ref CmdExtendedStats cmd, ref NetDebugCounters counters) =>
                {
                    switch (cmd.SubType)
                    {
                        case 2: // StatLock
                        {
                            Entity player = SerialRegistry.FindPlayer(cmdEntity.CsWorld());
                            if (player != 0)
                            {
                                StatLocks current = player.Has<StatLocks>()
                                    ? player.Get<StatLocks>() : default;

                                player.Set(cmd.StatIndex switch
                                {
                                    0 => current with { StrLock = cmd.LockValue },
                                    1 => current with { DexLock = cmd.LockValue },
                                    2 => current with { IntLock = cmd.LockValue },
                                    _ => current
                                });
                            }
                            break;
                        }

                        case 4: // BondedStatus (bonded pet death)
                        {
                            Entity target = SerialRegistry.FindBySerial(cmd.Serial);
                            if (target != 0 && target.IsAlive())
                            {
                                if (cmd.BondedDead)
                                {
                                    if (!target.Has<DeadTag>())
                                        target.Add<DeadTag>();
                                }
                                else
                                {
                                    if (target.Has<DeadTag>())
                                        target.Remove<DeadTag>();
                                }
                            }
                            break;
                        }
                    }

                    counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                });
        }

        // ── OPL Revision (0xD6) ─────────────────────────────────────

        private static void RegisterApplyOplRevision(World world)
        {
            world.System<CmdOplRevision, NetDebugCounters>("NetApply_OplRevision")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .Each((Entity cmdEntity, ref CmdOplRevision cmd, ref NetDebugCounters counters) =>
                {
                    Entity target = SerialRegistry.FindBySerial(cmd.Serial);
                    if (target != 0 && target.IsAlive())
                    {
                        ObjectProperties current = target.Has<ObjectProperties>()
                            ? target.Get<ObjectProperties>() : default;

                        bool changed = current.Revision != cmd.Revision;
                        target.Set(new ObjectProperties(cmd.Revision, changed));
                    }

                    counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                });
        }
    }
}
