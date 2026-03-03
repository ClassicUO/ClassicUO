// SPDX-License-Identifier: BSD-2-Clause

using System;
using Flecs.NET.Core;

namespace ClassicUO.ECS.Systems
{
    /// <summary>
    /// Movement systems: step queue processing and position integration.
    ///
    /// Mirrors legacy Mobile.ProcessSteps() behavior:
    ///   - Dequeue steps from StepBuffer based on elapsed time
    ///   - Interpolate WorldOffset for smooth visual movement
    ///   - Commit position when step delay is reached
    ///   - Handle direction-only changes immediately
    ///
    /// Speed constants (from MovementSpeed.cs):
    ///   Walk=400ms, Run=200ms, MountWalk=200ms, MountRun=100ms
    ///   AnimDelay=80ms, pixels: diagonal=44, cardinal=22
    /// </summary>
    public static class MovementSystems
    {
        // Movement speed constants matching legacy MovementSpeed.cs
        private const int STEP_DELAY_WALK = 400;
        private const int STEP_DELAY_RUN = 200;
        private const int STEP_DELAY_MOUNT_WALK = 200;
        private const int STEP_DELAY_MOUNT_RUN = 100;
        private const int CHARACTER_ANIMATION_DELAY = 80;

        // Pixel offsets per tile (isometric)
        private const float PIXELS_DIAGONAL = 44.0f;
        private const float PIXELS_CARDINAL = 22.0f;

        public static void Register(World world)
        {
            RegisterProcessSteps(world);
            RegisterApplyConfirmWalk(world);
            RegisterApplyDenyWalk(world);
            RegisterApplyMovePlayer(world);
        }

        // ── Step processing (Simulation phase) ──────────────────────

        private static void RegisterProcessSteps(World world)
        {
            world.System<StepBuffer, MovementTiming, WorldPosition, WorldOffset, DirectionComponent, FrameTiming>(
                    "Sim_ProcessSteps")
                .Kind(Phases.Simulation)
                .With<MobileTag>()
                .TermAt(5).Singleton()
                .Each((Entity entity,
                    ref StepBuffer buffer,
                    ref MovementTiming timing,
                    ref WorldPosition pos,
                    ref WorldOffset offset,
                    ref DirectionComponent dir,
                    ref FrameTiming frameTiming) =>
                {
                    if (buffer.IsEmpty)
                        return;

                    long ticks = (long)frameTiming.Ticks;

                    ProcessStepQueue(entity, ref buffer, ref timing, ref pos, ref offset, ref dir, ticks);
                });
        }

        private static void ProcessStepQueue(
            Entity entity,
            ref StepBuffer buffer,
            ref MovementTiming timing,
            ref WorldPosition pos,
            ref WorldOffset offset,
            ref DirectionComponent dir,
            long ticks)
        {
            if (buffer.IsEmpty)
                return;

            MobileStep step = buffer.Front();

            int delay = (int)(ticks - timing.LastStepTime);

            bool mounted = entity.Has<MountedTag>() || entity.Has<FlyingTag>();
            bool run = step.Run != 0;

            // Adaptive mounted detection for other mobiles (network coalescing).
            if (!mounted && !entity.Has<PlayerTag>() && buffer.Count > 1 && delay > 0)
            {
                int mountThreshold = run ? STEP_DELAY_MOUNT_RUN : STEP_DELAY_MOUNT_WALK;
                mounted = delay <= mountThreshold;
            }

            int maxDelay = TimeToCompleteMovement(run, mounted);

            // Check if this is a direction-only change (same position)
            bool directionOnly = (pos.X == (ushort)step.X && pos.Y == (ushort)step.Y && pos.Z == step.Z);

            bool removeStep = delay >= maxDelay || directionOnly;

            if (!removeStep)
            {
                // Interpolate offset for smooth movement
                if (pos.X != (ushort)step.X || pos.Y != (ushort)step.Y)
                {
                    float steps = maxDelay / (float)CHARACTER_ANIMATION_DELAY;
                    float x = delay / (float)CHARACTER_ANIMATION_DELAY;
                    float y = x;

                    // Z interpolation
                    float offsetZ = (step.Z - pos.Z) * x * (4.0f / steps);

                    // XY pixel offset based on direction
                    GetPixelOffset(step.Direction, ref x, ref y, steps);

                    offset = new WorldOffset(x, y, offsetZ);
                }
            }
            else
            {
                // Commit the step: update entity position
                pos = new WorldPosition((ushort)step.X, (ushort)step.Y, step.Z);
                dir = new DirectionComponent(step.Direction);
                offset = new WorldOffset(0, 0, 0);

                if (run)
                {
                    if (!entity.Has<RunningTag>())
                        entity.Add<RunningTag>();
                }
                else
                {
                    if (entity.Has<RunningTag>())
                        entity.Remove<RunningTag>();
                }

                buffer.Dequeue();
                timing = new MovementTiming(ticks);

                // If it was a direction-only change, immediately process next step
                if (directionOnly && !buffer.IsEmpty)
                {
                    ProcessStepQueue(entity, ref buffer, ref timing, ref pos, ref offset, ref dir, ticks);
                }
            }
        }

        // ── NetApply: Confirm walk → enqueue step ────────────────────

        private static void RegisterApplyConfirmWalk(World world)
        {
            world.System<CmdConfirmWalk>("NetApply_ConfirmWalk")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .Each((Entity cmdEntity, ref CmdConfirmWalk cmd) =>
                {
                    var w = cmdEntity.CsWorld();
                    // ConfirmWalk applies to the local player.
                    Entity player = SerialRegistry.FindPlayer(w);
                    if (player == 0 || !player.IsAlive())
                        return;

                    if (player.Has<NotorietyComponent>())
                        player.Set(new NotorietyComponent(cmd.Notoriety));
                });
        }

        // ── NetApply: Deny walk → reset position ─────────────────────

        private static void RegisterApplyDenyWalk(World world)
        {
            world.System<CmdDenyWalk>("NetApply_DenyWalk")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .Each((Entity cmdEntity, ref CmdDenyWalk cmd) =>
                {
                    var w = cmdEntity.CsWorld();
                    Entity player = SerialRegistry.FindPlayer(w);
                    if (player == 0 || !player.IsAlive())
                        return;

                    // Reset position and clear step queue
                    player.Set(new WorldPosition(cmd.X, cmd.Y, cmd.Z));
                    player.Set(new DirectionComponent(cmd.Direction));
                    player.Set(new WorldOffset(0, 0, 0));

                    if (player.Has<StepBuffer>())
                    {
                        var buf = player.Get<StepBuffer>(); // copy needed: mutate + set back
                        buf.Clear();
                        player.Set(buf);
                    }
                });
        }

        // ── NetApply: Server-initiated move ──────────────────────────

        private static void RegisterApplyMovePlayer(World world)
        {
            world.System<CmdMovePlayer>("NetApply_MovePlayer")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .Each((Entity cmdEntity, ref CmdMovePlayer cmd) =>
                {
                    var w = cmdEntity.CsWorld();
                    Entity player = SerialRegistry.FindPlayer(w);
                    if (player == 0 || !player.IsAlive())
                        return;

                    player.Set(new DirectionComponent(cmd.Direction));

                    if (cmd.Running)
                    {
                        if (!player.Has<RunningTag>())
                            player.Add<RunningTag>();
                    }
                    else
                    {
                        if (player.Has<RunningTag>())
                            player.Remove<RunningTag>();
                    }
                });
        }

        // ── Helpers ──────────────────────────────────────────────────

        private static int TimeToCompleteMovement(bool run, bool mounted)
        {
            if (mounted)
                return run ? STEP_DELAY_MOUNT_RUN : STEP_DELAY_MOUNT_WALK;
            return run ? STEP_DELAY_RUN : STEP_DELAY_WALK;
        }

        /// <summary>
        /// Calculate pixel offset for smooth movement interpolation.
        /// Matches legacy MovementSpeed.GetPixelOffset().
        /// Isometric grid: diagonal tiles = 44px, cardinal tiles = 22px.
        /// </summary>
        private static void GetPixelOffset(byte direction, ref float x, ref float y, float framesPerTile)
        {
            float stepDiag = PIXELS_DIAGONAL / framesPerTile;
            float stepCard = PIXELS_CARDINAL / framesPerTile;

            switch (direction & 7)
            {
                case 0: // North: -22x, -22y
                    x *= -stepCard;
                    y *= -stepCard;
                    break;
                case 1: // Right (NE): +44x, 0y
                    x *= stepDiag;
                    y = 0;
                    break;
                case 2: // East: +22x, +22y
                    x *= stepCard;
                    y *= stepCard;
                    break;
                case 3: // Down (SE): 0x, +44y
                    x = 0;
                    y *= stepDiag;
                    break;
                case 4: // South: +22x, +22y → actually -x, +y mirrored
                    x *= stepCard;
                    y *= stepCard;
                    break;
                case 5: // Left (SW): -44x, 0y
                    x *= -stepDiag;
                    y = 0;
                    break;
                case 6: // West: -22x, -22y
                    x *= -stepCard;
                    y *= -stepCard;
                    break;
                case 7: // Up (NW): 0x, -44y
                    x = 0;
                    y *= -stepDiag;
                    break;
            }
        }

    }
}
