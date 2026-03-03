// SPDX-License-Identifier: BSD-2-Clause

using Flecs.NET.Core;

namespace ClassicUO.ECS.Systems
{
    /// <summary>
    /// Buff/debuff and health bar flag systems.
    ///
    /// Health bar flags (0x16/0x17): SA-era poison/yellow bar per mobile.
    /// Buffs (0xDF): child entities of the buffed mobile with BuffEntry + BuffTag.
    /// </summary>
    public static class BuffSystems
    {
        public static void Register(World world)
        {
            RegisterApplyHealthBarUpdate(world);
            RegisterApplyAddBuff(world);
            RegisterApplyRemoveBuff(world);
            RegisterExpireBuffs(world);
        }

        // ── Health bar flags (NetApply) ─────────────────────────────

        private static void RegisterApplyHealthBarUpdate(World world)
        {
            world.System<CmdHealthBarUpdate, NetDebugCounters>("NetApply_HealthBarUpdate")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .Each((Entity cmdEntity, ref CmdHealthBarUpdate cmd, ref NetDebugCounters counters) =>
                {
                    Entity target = SerialRegistry.FindBySerial(cmd.Serial);
                    if (target == 0 || !target.IsAlive())
                    {
                        counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                        return;
                    }

                    var flags = target.Has<HealthBarFlags>()
                        ? target.Get<HealthBarFlags>()
                        : new HealthBarFlags(false, false);

                    if (cmd.Type == 1)
                    {
                        flags = flags with { Poisoned = cmd.Enabled };

                        // Sync PoisonedTag.
                        if (cmd.Enabled)
                        {
                            if (!target.Has<PoisonedTag>())
                                target.Add<PoisonedTag>();
                        }
                        else
                        {
                            if (target.Has<PoisonedTag>())
                                target.Remove<PoisonedTag>();
                        }
                    }
                    else if (cmd.Type == 2)
                    {
                        flags = flags with { YellowBar = cmd.Enabled };
                    }

                    target.Set(flags);
                    counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                });
        }

        // ── Add buff (NetApply) ─────────────────────────────────────

        private static void RegisterApplyAddBuff(World world)
        {
            world.System<CmdAddBuff, NetDebugCounters, FrameTiming>("NetApply_AddBuff")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .TermAt(2).Singleton()
                .Each((Entity cmdEntity, ref CmdAddBuff cmd, ref NetDebugCounters counters, ref FrameTiming ft) =>
                {
                    Entity target = SerialRegistry.FindBySerial(cmd.Serial);
                    if (target == 0 || !target.IsAlive())
                    {
                        counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                        return;
                    }

                    long ticks = (long)ft.Ticks;

                    // Check if buff with same IconId already exists as a child.
                    var w = cmdEntity.CsWorld();
                    ushort iconId = cmd.IconId;
                    Entity existing = default;

                    using var q = w.QueryBuilder<BuffEntry>()
                        .With<BuffTag>()
                        .Build();

                    q.Each((Entity buffEntity, ref BuffEntry entry) =>
                    {
                        if (entry.IconId == iconId && buffEntity.IsChildOf(target))
                            existing = buffEntity;
                    });

                    Entity buff = existing != 0 && existing.IsAlive() ? existing : w.Entity();

                    buff.Add<BuffTag>()
                        .ChildOf(target)
                        .Set(new BuffEntry
                        {
                            IconId = cmd.IconId,
                            Duration = cmd.Duration,
                            StartTick = ticks,
                            TitleCliloc = cmd.TitleCliloc,
                            DescriptionCliloc = cmd.DescriptionCliloc
                        });

                    counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                });
        }

        // ── Remove buff (NetApply) ──────────────────────────────────

        private static void RegisterApplyRemoveBuff(World world)
        {
            world.System<CmdRemoveBuff, NetDebugCounters>("NetApply_RemoveBuff")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .Each((Entity cmdEntity, ref CmdRemoveBuff cmd, ref NetDebugCounters counters) =>
                {
                    Entity target = SerialRegistry.FindBySerial(cmd.Serial);
                    if (target == 0 || !target.IsAlive())
                    {
                        counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                        return;
                    }

                    var w = cmdEntity.CsWorld();
                    ushort iconId = cmd.IconId;
                    Entity toRemove = default;

                    using var q = w.QueryBuilder<BuffEntry>()
                        .With<BuffTag>()
                        .Build();

                    q.Each((Entity buffEntity, ref BuffEntry entry) =>
                    {
                        if (entry.IconId == iconId && buffEntity.IsChildOf(target))
                            toRemove = buffEntity;
                    });

                    if (toRemove != 0 && toRemove.IsAlive())
                        toRemove.Destruct();

                    counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                });
        }

        // ── Expire buffs (Simulation) ───────────────────────────────

        private static void RegisterExpireBuffs(World world)
        {
            world.System<BuffEntry, FrameTiming>("Sim_ExpireBuffs")
                .Kind(Phases.Simulation)
                .With<BuffTag>()
                .TermAt(1).Singleton()
                .Each((Entity entity, ref BuffEntry entry, ref FrameTiming ft) =>
                {
                    if (entry.Duration == 0)
                        return; // Permanent buff

                    long expiryTick = entry.StartTick + entry.Duration * 1000;
                    if ((long)ft.Ticks >= expiryTick)
                    {
                        entity.Destruct();
                    }
                });
        }
    }
}
