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
            RegisterEffectLifetime(world);
            RegisterEffectSourceCleanup(world);
            RegisterEffectDistancePrune(world);
            RegisterEffectAnimation(world);
        }

        // ── Duration-based expiry (Simulation) ──────────────────────

        private static void RegisterEffectLifetime(World world)
        {
            world.System<EffectLifetime>("Sim_EffectLifetime")
                .Kind(Phases.Simulation)
                .With<EffectTag>()
                .Without<PendingRemovalTag>()
                .Each((Entity entity, ref EffectLifetime lifetime) =>
                {
                    if (lifetime.Duration <= 0)
                        return; // Infinite lifetime

                    ref readonly var ft = ref entity.CsWorld().Get<FrameTiming>();

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

                    var w = entity.CsWorld();
                    Entity sourceEntity = FindBySerial(w, source.SourceSerial);

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
            world.System<WorldPosition>("Sim_EffectDistancePrune")
                .Kind(Phases.Simulation)
                .With<EffectTag>()
                .Without<PendingRemovalTag>()
                .Each((Entity entity, ref WorldPosition pos) =>
                {
                    var w = entity.CsWorld();
                    ref readonly var vr = ref w.Get<ViewRange>();
                    byte viewRange = vr.Range;
                    var playerPos = GetPlayerPosition(w);

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
            world.System<EffectLifetime, EffectAnimPlayback, GraphicComponent>(
                    "Sim_EffectAnimation")
                .Kind(Phases.Simulation)
                .With<EffectTag>()
                .Without<PendingRemovalTag>()
                .Each((Entity entity,
                    ref EffectLifetime lifetime,
                    ref EffectAnimPlayback anim,
                    ref GraphicComponent graphic) =>
                {
                    if (anim.FrameCount == 0)
                    {
                        anim.AnimationGraphic = graphic.Graphic;
                        return;
                    }

                    ref readonly var ft2 = ref entity.CsWorld().Get<FrameTiming>();
                    long ticks = (long)ft2.Ticks;

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

        // ── Helpers ──────────────────────────────────────────────────

        private static Entity FindBySerial(World world, uint serial)
        {
            using var q = world.QueryBuilder<SerialComponent>().Build();
            Entity found = default;
            q.Each((Entity e, ref SerialComponent s) =>
            {
                if (s.Serial == serial)
                    found = e;
            });
            return found;
        }

        private static WorldPosition GetPlayerPosition(World world)
        {
            WorldPosition result = default;
            using var q = world.QueryBuilder<WorldPosition>()
                .With<PlayerTag>()
                .Build();

            q.Each((Entity _, ref WorldPosition pos) =>
            {
                result = pos;
            });
            return result;
        }
    }
}
