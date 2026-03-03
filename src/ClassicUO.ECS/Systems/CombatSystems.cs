// SPDX-License-Identifier: BSD-2-Clause

using Flecs.NET.Core;

namespace ClassicUO.ECS.Systems
{
    /// <summary>
    /// Combat systems: attack target tracking, swing handling, damage display.
    ///
    /// UO combat is server-authoritative. The client:
    ///   - Tracks the current attack target (0xAA AttackCharacter)
    ///   - Receives swing notifications (0x2F Swing) for auto-turn
    ///   - Receives damage values (0x0B Damage) for overhead display
    ///   - Handles warmode toggle (already in NetApplySystems)
    /// </summary>
    public static class CombatSystems
    {
        /// <summary>Duration overhead damage numbers stay visible (ms).</summary>
        private const long DAMAGE_DISPLAY_DURATION_MS = 1500;

        public static void Register(World world)
        {
            RegisterApplyAttackTarget(world);
            RegisterApplySwing(world);
            RegisterApplyDamage(world);
            RegisterExpireOverheadDamage(world);
        }

        // ── Attack target (NetApply) ──────────────────────────────────

        private static void RegisterApplyAttackTarget(World world)
        {
            world.System<CmdAttackTarget, NetDebugCounters>("NetApply_AttackTarget")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .Each((Entity cmdEntity, ref CmdAttackTarget cmd, ref NetDebugCounters counters) =>
                {
                    // Find the player entity and set attack target.
                    Entity player = SerialRegistry.FindPlayer(cmdEntity.CsWorld());
                    if (player != 0)
                        player.Set(new AttackTarget(cmd.TargetSerial));

                    counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                });
        }

        // ── Swing notification (NetApply) ─────────────────────────────

        private static void RegisterApplySwing(World world)
        {
            world.System<CmdSwing, NetDebugCounters>("NetApply_Swing")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .Each((Entity cmdEntity, ref CmdSwing cmd, ref NetDebugCounters counters) =>
                {
                    // Find attacker entity.
                    Entity attacker = SerialRegistry.FindBySerial(cmd.AttackerSerial);
                    if (attacker == 0 || !attacker.IsAlive())
                    {
                        counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                        return;
                    }

                    // If attacker is the player and target matches LastAttack,
                    // auto-turn to face the defender.
                    if (attacker.Has<PlayerTag>() && attacker.Has<AttackTarget>())
                    {
                        ref readonly var at = ref attacker.Get<AttackTarget>();
                        if (at.TargetSerial == cmd.DefenderSerial)
                        {
                            Entity defender = SerialRegistry.FindBySerial(cmd.DefenderSerial);
                            if (defender != 0 && defender.IsAlive()
                                && attacker.Has<WorldPosition>() && defender.Has<WorldPosition>())
                            {
                                ref readonly var aPos = ref attacker.Get<WorldPosition>();
                                ref readonly var dPos = ref defender.Get<WorldPosition>();
                                byte dir = DirectionTo(aPos.X, aPos.Y, dPos.X, dPos.Y);
                                attacker.Set(new DirectionComponent(dir));
                            }
                        }
                    }

                    counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                });
        }

        // ── Damage (NetApply) ─────────────────────────────────────────

        private static void RegisterApplyDamage(World world)
        {
            world.System<CmdDamage, FrameTiming, NetDebugCounters>("NetApply_Damage")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .TermAt(2).Singleton()
                .Each((Entity cmdEntity, ref CmdDamage cmd, ref FrameTiming timing, ref NetDebugCounters counters) =>
                {
                    Entity target = SerialRegistry.FindBySerial(cmd.Serial);
                    if (target != 0 && target.IsAlive())
                        target.Set(new OverheadDamage(cmd.Amount, timing.Ticks));

                    counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                });
        }

        // ── Expire overhead damage (Simulation) ──────────────────────

        private static void RegisterExpireOverheadDamage(World world)
        {
            world.System<OverheadDamage, FrameTiming>("Sim_ExpireOverheadDamage")
                .Kind(Phases.Simulation)
                .TermAt(1).Singleton()
                .Each((Entity entity, ref OverheadDamage dmg, ref FrameTiming timing) =>
                {
                    if (timing.Ticks - dmg.StartTick > DAMAGE_DISPLAY_DURATION_MS)
                    {
                        entity.Remove<OverheadDamage>();
                    }
                });
        }

        // ── Helpers ───────────────────────────────────────────────────

        /// <summary>Compute UO direction (0-7) from one tile to another.</summary>
        private static byte DirectionTo(ushort fromX, ushort fromY, ushort toX, ushort toY)
        {
            int dx = toX - fromX;
            int dy = toY - fromY;

            // Normalize to -1/0/+1
            int sx = dx > 0 ? 1 : (dx < 0 ? -1 : 0);
            int sy = dy > 0 ? 1 : (dy < 0 ? -1 : 0);

            // UO direction mapping:
            // North=0, Right=1, East=2, Down=3, South=4, Left=5, West=6, Up=7
            return (sx, sy) switch
            {
                (0, -1)  => 0, // North
                (1, -1)  => 1, // Right (NE)
                (1, 0)   => 2, // East
                (1, 1)   => 3, // Down (SE)
                (0, 1)   => 4, // South
                (-1, 1)  => 5, // Left (SW)
                (-1, 0)  => 6, // West
                (-1, -1) => 7, // Up (NW)
                _        => 0
            };
        }
    }
}
