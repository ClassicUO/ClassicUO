// SPDX-License-Identifier: BSD-2-Clause

using Flecs.NET.Core;

namespace ClassicUO.ECS.Systems
{
    /// <summary>
    /// World-level systems: boat movement, weather, audio events, house revision.
    /// </summary>
    public static class WorldSystems
    {
        public static void Register(World world)
        {
            RegisterApplyBoatMoving(world);
            RegisterApplyBoatEntityUpdate(world);
            RegisterApplySetWeather(world);
            RegisterApplyHouseRevision(world);
            RegisterSimWorldMapTracking(world);
        }

        // ── Boat moving (NetApply) ──────────────────────────────────

        private static void RegisterApplyBoatMoving(World world)
        {
            world.System<CmdBoatMoving, NetDebugCounters>("NetApply_BoatMoving")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .Each((Entity cmdEntity, ref CmdBoatMoving cmd, ref NetDebugCounters counters) =>
                {
                    Entity boat = SerialRegistry.FindBySerial(cmd.BoatSerial);
                    if (boat != 0 && boat.IsAlive())
                    {
                        boat.Set(new WorldPosition(cmd.X, cmd.Y, (sbyte)cmd.Z));
                        boat.Set(new BoatState
                        {
                            Speed = cmd.Speed,
                            Direction = cmd.FacingDirection,
                            MovingDirection = cmd.MovingDirection,
                            IsMoving = cmd.Speed > 0
                        });

                        if (!boat.Has<MultiTag>())
                            boat.Add<MultiTag>();
                    }

                    counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                });
        }

        // ── Boat entity position update (NetApply) ──────────────────

        private static void RegisterApplyBoatEntityUpdate(World world)
        {
            world.System<CmdBoatEntityUpdate, NetDebugCounters>("NetApply_BoatEntityUpdate")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .Each((Entity cmdEntity, ref CmdBoatEntityUpdate cmd, ref NetDebugCounters counters) =>
                {
                    Entity target = SerialRegistry.FindBySerial(cmd.Serial);
                    if (target != 0 && target.IsAlive())
                    {
                        target.Set(new WorldPosition(cmd.X, cmd.Y, (sbyte)cmd.Z));
                    }

                    counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                });
        }

        // ── Weather (NetApply) ──────────────────────────────────────

        private static void RegisterApplySetWeather(World world)
        {
            world.System<CmdSetWeather, WeatherState, NetDebugCounters>("NetApply_SetWeather")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .TermAt(2).Singleton()
                .Each((Entity cmdEntity, ref CmdSetWeather cmd, ref WeatherState weather, ref NetDebugCounters counters) =>
                {
                    weather = new WeatherState(cmd.Type, cmd.Count, cmd.Temperature);
                    counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                });
        }

        // ── House revision (NetApply) ───────────────────────────────

        private static void RegisterApplyHouseRevision(World world)
        {
            world.System<CmdHouseRevision, NetDebugCounters>("NetApply_HouseRevision")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .Each((Entity cmdEntity, ref CmdHouseRevision cmd, ref NetDebugCounters counters) =>
                {
                    Entity house = SerialRegistry.FindBySerial(cmd.Serial);
                    if (house != 0 && house.IsAlive())
                    {
                        house.Set(new ObjectProperties(cmd.Revision, false));

                        if (!house.Has<MultiTag>())
                            house.Add<MultiTag>();
                    }

                    counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                });
        }

        // ── WorldMap entity tracking ──────────────────────────────────

        private static void RegisterSimWorldMapTracking(World world)
        {
            world.System<SerialComponent, WorldPosition, NotorietyComponent, Vitals>(
                    "Sim_WorldMapTracking")
                .Kind(Phases.Simulation)
                .With<MobileTag>()
                .Without<PlayerTag>()
                .Without<PendingRemovalTag>()
                .Run((Iter it) =>
                {
                    var w = it.World();
                    ref var list = ref w.GetMut<WorldMapUpdateList>();
                    list.Count = 0; // clear previous frame's entries

                    while (it.Next())
                    {
                        var serials = it.Field<SerialComponent>(0);
                        var positions = it.Field<WorldPosition>(1);
                        var notorieties = it.Field<NotorietyComponent>(2);
                        var vitals = it.Field<Vitals>(3);

                        for (int i = 0; i < it.Count(); i++)
                        {
                            byte noto = notorieties[i].Notoriety;
                            bool isAlly = noto == (byte)5; // NotorietyFlag.Ally
                            bool isParty = it.Entity(i).Has<PartyTag>();

                            if (!isAlly && !isParty)
                                continue;

                            int hpPct = vitals[i].HitsMax > 0
                                ? vitals[i].Hits * 100 / vitals[i].HitsMax
                                : 0;

                            int nameIdx = it.Entity(i).Has<NameIndex>()
                                ? it.Entity(i).Get<NameIndex>().Index
                                : -1;

                            list.Add(new WorldMapUpdateEntry
                            {
                                Serial = serials[i].Serial,
                                X = positions[i].X,
                                Y = positions[i].Y,
                                HpPercent = hpPct,
                                IsGuild = isAlly,
                                NameIndex = nameIdx,
                            });
                        }
                    }
                });
        }
    }
}
