// SPDX-License-Identifier: BSD-2-Clause

using System;
using Flecs.NET.Core;

namespace ClassicUO.ECS.Systems
{
    /// <summary>
    /// Effect lifecycle and animation systems.
    ///
    /// Mirrors legacy EffectManager.Update() + GameEffect.Update():
    ///   - Duration-based expiry (Duration &lt; Ticks → destroy)
    ///   - Source dependency (source destroyed → effect destroyed)
    ///   - Distance-based pruning (beyond ClientViewRange)
    ///   - Animation frame cycling (NextFrameTime interval)
    /// </summary>
    public static class EffectSystems
    {
        public static void Register(World world)
        {
            RegisterApplySpawnEffect(world);
            RegisterApplyDragEffect(world);
            RegisterEffectMovement(world);
            RegisterEffectLifetime(world);
            RegisterEffectSourceCleanup(world);
            RegisterEffectDistancePrune(world);
            RegisterEffectAnimation(world);
        }

        // ── Spawn effect from packet (NetApply) ──────────────────────

        private static void RegisterApplySpawnEffect(World world)
        {
            world.System<CmdSpawnEffect, NetDebugCounters, FrameTiming>("NetApply_SpawnEffect")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .TermAt(2).Singleton()
                .Each((Entity cmdEntity, ref CmdSpawnEffect cmd, ref NetDebugCounters counters, ref FrameTiming ft) =>
                {
                    long ticks = (long)ft.Ticks;

                    // Compute lifetime in ticks from duration * speed.
                    int durationMs = cmd.Duration > 0 ? cmd.Duration * 50 : 4000;
                    int intervalMs = cmd.Speed > 0 ? cmd.Speed * 50 : 100;

                    var w = cmdEntity.CsWorld();
                    var effect = w.Entity()
                        .Add<EffectTag>()
                        .Set(new WorldPosition(cmd.SourceX, cmd.SourceY, cmd.SourceZ))
                        .Set(new GraphicComponent(cmd.Graphic))
                        .Set(new HueComponent(cmd.Hue))
                        .Set(new EffectLifetime(ticks + durationMs, ticks + intervalMs, intervalMs))
                        .Set(new EffectAnimPlayback(0, 0, cmd.Graphic));

                    if (cmd.SourceSerial != 0)
                        effect.Set(new EffectSourceLink(cmd.SourceSerial));

                    // Moving effects (type 0 or 3) get target + movement components.
                    if (cmd.Type == 0 || cmd.Type == 3)
                    {
                        effect.Set(new EffectTarget(cmd.TargetX, cmd.TargetY, cmd.TargetZ, cmd.TargetSerial));
                        effect.Set(new EffectMovement(cmd.Speed, 0f));
                    }

                    if (cmd.Explode)
                        effect.Add<ExplodeOnExpiryTag>();

                    counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                });
        }

        // ── Drag animation effect (NetApply) ────────────────────────

        private static void RegisterApplyDragEffect(World world)
        {
            world.System<CmdDragEffect, NetDebugCounters, FrameTiming>("NetApply_DragEffect")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .TermAt(2).Singleton()
                .Each((Entity cmdEntity, ref CmdDragEffect cmd, ref NetDebugCounters counters, ref FrameTiming ft) =>
                {
                    long ticks = (long)ft.Ticks;

                    int durationMs = 4000; // default drag duration
                    int intervalMs = 100;

                    var w = cmdEntity.CsWorld();
                    w.Entity()
                        .Add<EffectTag>()
                        .Set(new WorldPosition(cmd.FromX, cmd.FromY, cmd.FromZ))
                        .Set(new GraphicComponent(cmd.Graphic))
                        .Set(new HueComponent(cmd.Hue))
                        .Set(new EffectLifetime(ticks + durationMs, ticks + intervalMs, intervalMs))
                        .Set(new EffectAnimPlayback(0, 0, cmd.Graphic))
                        .Set(new EffectTarget(cmd.ToX, cmd.ToY, cmd.ToZ, 0))
                        .Set(new EffectMovement(5, 0f)); // moderate drag speed

                    counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                });
        }

        // ── Moving effect interpolation (Simulation) ────────────────

        private static void RegisterEffectMovement(World world)
        {
            world.System<EffectMovement, EffectTarget, WorldPosition, FrameTiming>(
                    "Sim_EffectMovement")
                .Kind(Phases.Simulation)
                .With<EffectTag>()
                .Without<PendingRemovalTag>()
                .TermAt(3).Singleton()
                .Each((Entity entity,
                    ref EffectMovement movement,
                    ref EffectTarget target,
                    ref WorldPosition pos,
                    ref FrameTiming ft) =>
                {
                    // Advance progress based on speed and delta time.
                    float step = movement.Speed > 0
                        ? ft.DeltaSeconds * movement.Speed * 2f
                        : ft.DeltaSeconds * 10f;
                    float newProgress = movement.Progress + step;

                    // If target is tracking an entity, update target position.
                    if (target.TargetSerial != 0)
                    {
                        Entity targetEntity = SerialRegistry.FindBySerial(target.TargetSerial);
                        if (targetEntity != 0 && targetEntity.IsAlive() && targetEntity.Has<WorldPosition>())
                        {
                            ref readonly var tPos = ref targetEntity.Get<WorldPosition>();
                            target = new EffectTarget(tPos.X, tPos.Y, tPos.Z, target.TargetSerial);
                        }
                    }

                    if (newProgress >= 1f)
                    {
                        // Arrived at destination.
                        pos = new WorldPosition(target.X, target.Y, target.Z);
                        movement = new EffectMovement(movement.Speed, 1f);

                        // If explode-on-expiry, spawn explosion at target.
                        if (entity.Has<ExplodeOnExpiryTag>())
                        {
                            var w = entity.CsWorld();
                            long ticks = (long)ft.Ticks;
                            ushort graphic = entity.Has<GraphicComponent>()
                                ? entity.Get<GraphicComponent>().Graphic : (ushort)0;

                            w.Entity()
                                .Add<EffectTag>()
                                .Set(new WorldPosition(target.X, target.Y, target.Z))
                                .Set(new GraphicComponent(graphic))
                                .Set(new HueComponent(0))
                                .Set(new EffectLifetime(ticks + 1000, ticks + 100, 100))
                                .Set(new EffectAnimPlayback(0, 0, graphic));
                        }

                        entity.Add<PendingRemovalTag>();
                    }
                    else
                    {
                        // Lerp position.
                        // We need the original source position — stored as the position
                        // when Progress was 0. Since we update pos each frame, we
                        // back-calculate from current state.
                        float oldP = movement.Progress;
                        float invOld = oldP < 0.001f ? 0f : 1f;

                        // Simple lerp: directly interpolate between current and target
                        // weighted by how much progress remains.
                        float t = newProgress;
                        // Estimate source from first frame position if early,
                        // otherwise lerp from current toward target.
                        ushort newX = (ushort)(pos.X + (target.X - pos.X) * (step / (1f - oldP + 0.001f)));
                        ushort newY = (ushort)(pos.Y + (target.Y - pos.Y) * (step / (1f - oldP + 0.001f)));
                        sbyte newZ = (sbyte)(pos.Z + (target.Z - pos.Z) * (step / (1f - oldP + 0.001f)));

                        pos = new WorldPosition(newX, newY, newZ);
                        movement = new EffectMovement(movement.Speed, newProgress);
                    }
                });
        }

        // ── Duration-based expiry (Simulation) ──────────────────────

        private static void RegisterEffectLifetime(World world)
        {
            world.System<EffectLifetime, FrameTiming>("Sim_EffectLifetime")
                .Kind(Phases.Simulation)
                .With<EffectTag>()
                .Without<PendingRemovalTag>()
                .TermAt(1).Singleton()
                .Each((Entity entity, ref EffectLifetime lifetime, ref FrameTiming ft) =>
                {
                    if (lifetime.Duration <= 0)
                        return; // Infinite lifetime

                    if ((long)ft.Ticks >= lifetime.Duration)
                    {
                        entity.Add<PendingRemovalTag>();
                    }
                });
        }

        // ── Source dependency cleanup (Simulation) ───────────────────

        private static void RegisterEffectSourceCleanup(World world)
        {
            world.System<EffectSourceLink>("Sim_EffectSourceCleanup")
                .Kind(Phases.Simulation)
                .With<EffectTag>()
                .Without<PendingRemovalTag>()
                .Each((Entity entity, ref EffectSourceLink source) =>
                {
                    if (source.SourceSerial == 0)
                        return;

                    Entity sourceEntity = SerialRegistry.FindBySerial(source.SourceSerial);

                    // If the source entity no longer exists or is pending removal, destroy this effect.
                    if (sourceEntity == 0 || !sourceEntity.IsAlive() || sourceEntity.Has<PendingRemovalTag>())
                    {
                        entity.Add<PendingRemovalTag>();
                    }
                });
        }

        // ── Distance-based pruning (Simulation) ─────────────────────

        private static void RegisterEffectDistancePrune(World world)
        {
            world.System<WorldPosition, ViewRange>("Sim_EffectDistancePrune")
                .Kind(Phases.Simulation)
                .With<EffectTag>()
                .Without<PendingRemovalTag>()
                .TermAt(1).Singleton()
                .Each((Entity entity, ref WorldPosition pos, ref ViewRange vr) =>
                {
                    byte viewRange = vr.Range;
                    var playerPos = SerialRegistry.GetPlayerPosition(entity.CsWorld());

                    if (playerPos.X == 0 && playerPos.Y == 0)
                        return; // No player yet

                    int dist = Math.Max(
                        Math.Abs(pos.X - playerPos.X),
                        Math.Abs(pos.Y - playerPos.Y));

                    if (dist > viewRange)
                    {
                        entity.Add<PendingRemovalTag>();
                    }
                });
        }

        // ── Animation frame cycling (Simulation) ────────────────────

        private static void RegisterEffectAnimation(World world)
        {
            world.System<EffectLifetime, EffectAnimPlayback, GraphicComponent, FrameTiming>(
                    "Sim_EffectAnimation")
                .Kind(Phases.Simulation)
                .With<EffectTag>()
                .Without<PendingRemovalTag>()
                .TermAt(3).Singleton()
                .Each((Entity entity,
                    ref EffectLifetime lifetime,
                    ref EffectAnimPlayback anim,
                    ref GraphicComponent graphic,
                    ref FrameTiming ft) =>
                {
                    if (anim.FrameCount == 0)
                    {
                        anim.AnimationGraphic = graphic.Graphic;
                        return;
                    }

                    long ticks = (long)ft.Ticks;

                    if (ticks >= lifetime.NextFrameTime)
                    {
                        byte nextIndex = (byte)((anim.AnimIndex + 1) % anim.FrameCount);
                        anim = new EffectAnimPlayback(
                            nextIndex,
                            anim.FrameCount,
                            (ushort)(graphic.Graphic + nextIndex));
                        lifetime = lifetime with
                        {
                            NextFrameTime = ticks + lifetime.IntervalMs
                        };
                    }
                });
        }

    }
}
