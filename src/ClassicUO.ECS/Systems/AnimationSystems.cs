// SPDX-License-Identifier: BSD-2-Clause

using Flecs.NET.Core;

namespace ClassicUO.ECS.Systems
{
    /// <summary>
    /// Animation systems: server-driven animation application,
    /// per-frame animation stepping, and idle animation triggering.
    /// </summary>
    public static class AnimationSystems
    {
        private const int DEFAULT_ANIM_INTERVAL_MS = 100;

        public static void Register(World world)
        {
            RegisterApplyCharacterAnimation(world);
            RegisterApplyNewCharacterAnimation(world);
            RegisterProcessMobileAnimation(world);
            RegisterProcessItemAnimation(world);
        }

        // ── Server animation (0x6E) ─────────────────────────────────

        private static void RegisterApplyCharacterAnimation(World world)
        {
            world.System<CmdCharacterAnimation, NetDebugCounters, FrameTiming>("NetApply_CharacterAnimation")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .TermAt(2).Singleton()
                .Each((Entity cmdEntity, ref CmdCharacterAnimation cmd, ref NetDebugCounters counters, ref FrameTiming ft) =>
                {
                    Entity target = SerialRegistry.FindBySerial(cmd.Serial);
                    if (target == 0 || !target.IsAlive())
                    {
                        counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                        return;
                    }

                    // If a server animation is already playing, only override if this is also from server.
                    if (target.Has<AnimationState>())
                    {
                        ref readonly var current = ref target.Get<AnimationState>();
                        if (current.FromServer)
                        {
                            // New server anim overrides old server anim.
                        }
                    }

                    int intervalMs = cmd.Delay > 0 ? cmd.Delay * 25 : DEFAULT_ANIM_INTERVAL_MS;

                    target.Set(new AnimationState
                    {
                        Group = (byte)cmd.Action,
                        FrameIndex = 0,
                        FrameCount = cmd.FrameCount,
                        Interval = (byte)(intervalMs / 25),
                        RepeatMode = cmd.RepeatCount,
                        Repeat = cmd.Repeat,
                        Forward = cmd.Forward,
                        FromServer = true,
                        LastChangeTime = (long)ft.Ticks
                    });

                    counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                });
        }

        // ── New character animation (0xE2) ──────────────────────────

        private static void RegisterApplyNewCharacterAnimation(World world)
        {
            world.System<CmdNewCharacterAnimation, NetDebugCounters, FrameTiming>("NetApply_NewCharacterAnimation")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .TermAt(2).Singleton()
                .Each((Entity cmdEntity, ref CmdNewCharacterAnimation cmd, ref NetDebugCounters counters, ref FrameTiming ft) =>
                {
                    Entity target = SerialRegistry.FindBySerial(cmd.Serial);
                    if (target == 0 || !target.IsAlive())
                    {
                        counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                        return;
                    }

                    // The actual animation group mapping is done in legacy code.
                    // We store the raw packet data; the render bridge resolves the group.
                    bool repeat = (cmd.AnimationType == 1 || cmd.AnimationType == 2);

                    target.Set(new AnimationState
                    {
                        Group = (byte)cmd.Action,
                        FrameIndex = 0,
                        FrameCount = 0, // resolved by render bridge from animation tables
                        Interval = 4,   // default 100ms
                        RepeatMode = 1,
                        Repeat = repeat,
                        Forward = true,
                        FromServer = true,
                        LastChangeTime = (long)ft.Ticks
                    });

                    counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                });
        }

        // ── Process mobile animation (Simulation) ───────────────────

        private static void RegisterProcessMobileAnimation(World world)
        {
            world.System<AnimationState, FrameTiming>("Sim_ProcessMobileAnimation")
                .Kind(Phases.Simulation)
                .With<MobileTag>()
                .Without<PendingRemovalTag>()
                .TermAt(1).Singleton()
                .Each((Entity entity, ref AnimationState anim, ref FrameTiming ft) =>
                {
                    if (anim.FrameCount == 0)
                        return; // no frames to animate

                    long ticks = (long)ft.Ticks;
                    long intervalMs = anim.Interval * 25;
                    if (intervalMs <= 0) intervalMs = DEFAULT_ANIM_INTERVAL_MS;

                    if (ticks < anim.LastChangeTime + intervalMs)
                        return;

                    // Advance frame
                    if (anim.Forward)
                    {
                        if (anim.FrameIndex + 1 < anim.FrameCount)
                        {
                            anim = anim with
                            {
                                FrameIndex = (byte)(anim.FrameIndex + 1),
                                LastChangeTime = ticks
                            };
                        }
                        else
                        {
                            // Last frame reached
                            if (anim.Repeat || anim.RepeatMode > 1)
                            {
                                ushort newRepeat = anim.RepeatMode > 1
                                    ? (ushort)(anim.RepeatMode - 1) : anim.RepeatMode;
                                anim = anim with
                                {
                                    FrameIndex = 0,
                                    RepeatMode = newRepeat,
                                    LastChangeTime = ticks
                                };
                            }
                            else
                            {
                                // Animation complete — clear it.
                                entity.Remove<AnimationState>();
                            }
                        }
                    }
                    else
                    {
                        if (anim.FrameIndex > 0)
                        {
                            anim = anim with
                            {
                                FrameIndex = (byte)(anim.FrameIndex - 1),
                                LastChangeTime = ticks
                            };
                        }
                        else
                        {
                            if (anim.Repeat || anim.RepeatMode > 1)
                            {
                                ushort newRepeat = anim.RepeatMode > 1
                                    ? (ushort)(anim.RepeatMode - 1) : anim.RepeatMode;
                                anim = anim with
                                {
                                    FrameIndex = (byte)(anim.FrameCount - 1),
                                    RepeatMode = newRepeat,
                                    LastChangeTime = ticks
                                };
                            }
                            else
                            {
                                entity.Remove<AnimationState>();
                            }
                        }
                    }
                });
        }

        // ── Process item animation (Simulation) ─────────────────────

        private static void RegisterProcessItemAnimation(World world)
        {
            world.System<ItemAnimationState, GraphicComponent, FrameTiming>(
                    "Sim_ProcessItemAnimation")
                .Kind(Phases.Simulation)
                .With<ItemTag>()
                .Without<PendingRemovalTag>()
                .TermAt(2).Singleton()
                .Each((Entity entity,
                    ref ItemAnimationState anim,
                    ref GraphicComponent graphic,
                    ref FrameTiming ft) =>
                {
                    if (!anim.IsAnimated || anim.FrameCount == 0)
                        return;

                    long ticks = (long)ft.Ticks;
                    if (ticks < anim.NextFrameTime)
                        return;

                    byte nextFrame = (byte)((anim.FrameIndex + 1) % anim.FrameCount);
                    ushort baseGraphic = (ushort)(graphic.Graphic - anim.FrameIndex);

                    anim = anim with
                    {
                        FrameIndex = nextFrame,
                        NextFrameTime = ticks + anim.IntervalMs
                    };

                    graphic = new GraphicComponent((ushort)(baseGraphic + nextFrame));
                });
        }
    }
}
