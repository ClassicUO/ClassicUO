// SPDX-License-Identifier: BSD-2-Clause

using Flecs.NET.Core;

namespace ClassicUO.ECS.Systems
{
    /// <summary>
    /// Input phase systems that process player-initiated commands.
    ///
    /// Player input commands are enqueued via EcsRuntimeHost bridge APIs
    /// (e.g., RequestMove, RequestAttack) as transient command entities,
    /// then consumed here during the Input phase.
    ///
    /// These systems translate player intent into state changes or
    /// outbound packet requests. They run before NetApply so that
    /// input-driven state is ready before server responses arrive.
    /// </summary>
    public static class InputSystems
    {
        public static void Register(World world)
        {
            RegisterRequestMove(world);
            RegisterRequestAttack(world);
            RegisterToggleWarMode(world);
            RegisterTargetEntity(world);
            RegisterTargetPosition(world);
            RegisterCancelTarget(world);
            RegisterPickUp(world);
            RegisterDropItem(world);
        }

        // ── Move request (Input) ──────────────────────────────────────

        private static void RegisterRequestMove(World world)
        {
            world.System<CmdRequestMove, FrameTiming>("Input_RequestMove")
                .Kind(Phases.Input)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .Each((Entity cmdEntity, ref CmdRequestMove cmd, ref FrameTiming ft) =>
                {
                    Entity player = SerialRegistry.FindPlayer(cmdEntity.CsWorld());
                    if (player == 0)
                        return;

                    // Enqueue a step into the player's step buffer.
                    // Use local copy: Set on non-iterated entity is deferred,
                    // so Get after Set would read stale/missing data.
                    var buf = player.Has<StepBuffer>()
                        ? player.Get<StepBuffer>()
                        : new StepBuffer();
                    if (buf.IsFull)
                        return;

                    ref readonly var pos = ref player.Get<WorldPosition>();

                    // Calculate destination from direction
                    int dx = 0, dy = 0;
                    GetDirectionOffset(cmd.Direction, ref dx, ref dy);

                    var step = new MobileStep
                    {
                        X = pos.X + dx,
                        Y = pos.Y + dy,
                        Z = pos.Z,
                        Direction = cmd.Direction,
                        Run = (byte)(cmd.Run ? 1 : 0)
                    };

                    buf.Enqueue(step);
                    player.Set(buf);

                    if (!player.Has<MovementTiming>())
                    {
                        player.Set(new MovementTiming((long)ft.Ticks));
                    }
                });
        }

        // ── Attack request (Input) ────────────────────────────────────

        private static void RegisterRequestAttack(World world)
        {
            world.System<CmdRequestAttack>("Input_RequestAttack")
                .Kind(Phases.Input)
                .With<NetworkCommand>()
                .Each((Entity cmdEntity, ref CmdRequestAttack cmd) =>
                {
                    Entity player = SerialRegistry.FindPlayer(cmdEntity.CsWorld());
                    if (player == 0)
                        return;

                    player.Set(new AttackTarget(cmd.TargetSerial));

                    // Ensure player is in warmode when attacking.
                    if (!player.Has<WarModeTag>())
                        player.Add<WarModeTag>();
                });
        }

        // ── Toggle warmode (Input) ────────────────────────────────────

        private static void RegisterToggleWarMode(World world)
        {
            world.System<CmdToggleWarMode>("Input_ToggleWarMode")
                .Kind(Phases.Input)
                .With<NetworkCommand>()
                .Each((Entity cmdEntity, ref CmdToggleWarMode cmd) =>
                {
                    Entity player = SerialRegistry.FindPlayer(cmdEntity.CsWorld());
                    if (player == 0)
                        return;

                    if (cmd.WarMode)
                    {
                        if (!player.Has<WarModeTag>())
                            player.Add<WarModeTag>();
                    }
                    else
                    {
                        if (player.Has<WarModeTag>())
                            player.Remove<WarModeTag>();
                    }
                });
        }

        // ── Target entity (Input) ─────────────────────────────────────

        private static void RegisterTargetEntity(World world)
        {
            world.System<CmdTargetEntity, TargetingState, LastTargetInfo>("Input_TargetEntity")
                .Kind(Phases.Input)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .TermAt(2).Singleton()
                .Each((Entity cmdEntity, ref CmdTargetEntity cmd, ref TargetingState ts, ref LastTargetInfo lti) =>
                {
                    // Clear targeting cursor.
                    ts = new TargetingState();

                    // Update last target info.
                    Entity target = SerialRegistry.FindBySerial(cmd.Serial);
                    if (target != 0 && target.IsAlive() && target.Has<WorldPosition>())
                    {
                        ref readonly var pos = ref target.Get<WorldPosition>();
                        ushort graphic = target.Has<GraphicComponent>()
                            ? target.Get<GraphicComponent>().Graphic : (ushort)0;
                        lti = new LastTargetInfo(cmd.Serial, graphic, pos.X, pos.Y, pos.Z, true);
                    }
                });
        }

        // ── Target position (Input) ───────────────────────────────────

        private static void RegisterTargetPosition(World world)
        {
            world.System<CmdTargetPosition, TargetingState, LastTargetInfo>("Input_TargetPosition")
                .Kind(Phases.Input)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .TermAt(2).Singleton()
                .Each((Entity cmdEntity, ref CmdTargetPosition cmd, ref TargetingState ts, ref LastTargetInfo lti) =>
                {
                    ts = new TargetingState(); // Clear targeting cursor
                    lti = new LastTargetInfo(0, cmd.Graphic, cmd.X, cmd.Y, cmd.Z, false);
                });
        }

        // ── Cancel target (Input) ─────────────────────────────────────

        private static void RegisterCancelTarget(World world)
        {
            world.System("Input_CancelTarget")
                .Kind(Phases.Input)
                .With<CmdCancelTarget>()
                .With<NetworkCommand>()
                .Run((Iter it) =>
                {
                    while (it.Next())
                    {
                        ref var ts = ref it.World().GetMut<TargetingState>();
                        ts = new TargetingState { TargetType = 3 }; // CursorTarget=None, TargetType=Cancel
                    }
                });
        }

        // ── Pick up item (Input) ────────────────────────────────────

        private static void RegisterPickUp(World world)
        {
            world.System<CmdPickUp, ItemHoldState>("Input_PickUp")
                .Kind(Phases.Input)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .Each((Entity cmdEntity, ref CmdPickUp cmd, ref ItemHoldState hold) =>
                {
                    Entity item = SerialRegistry.FindBySerial(cmd.Serial);
                    ushort graphic = 0;
                    ushort hue = 0;
                    if (item != 0 && item.IsAlive())
                    {
                        if (item.Has<GraphicComponent>())
                            graphic = item.Get<GraphicComponent>().Graphic;
                        if (item.Has<HueComponent>())
                            hue = item.Get<HueComponent>().Hue;
                    }

                    hold = new ItemHoldState
                    {
                        Serial = cmd.Serial,
                        Graphic = graphic,
                        Hue = hue,
                        Amount = cmd.Amount,
                        Enabled = true,
                        Dropped = false,
                        DropContainer = 0xFFFFFFFF
                    };
                });
        }

        // ── Drop item (Input) ───────────────────────────────────────

        private static void RegisterDropItem(World world)
        {
            world.System<CmdDropItem, ItemHoldState>("Input_DropItem")
                .Kind(Phases.Input)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .Each((Entity cmdEntity, ref CmdDropItem cmd, ref ItemHoldState hold) =>
                {
                    hold.Dropped = true;
                    hold.DropX = cmd.X;
                    hold.DropY = cmd.Y;
                    hold.DropZ = cmd.Z;
                    hold.DropContainer = cmd.ContainerSerial;
                });
        }

        // ── Helpers ───────────────────────────────────────────────────

        private static void GetDirectionOffset(byte direction, ref int dx, ref int dy)
        {
            switch (direction & 7)
            {
                case 0: dx = 0; dy = -1; break;  // North
                case 1: dx = 1; dy = -1; break;  // Right (NE)
                case 2: dx = 1; dy = 0;  break;  // East
                case 3: dx = 1; dy = 1;  break;  // Down (SE)
                case 4: dx = 0; dy = 1;  break;  // South
                case 5: dx = -1; dy = 1; break;  // Left (SW)
                case 6: dx = -1; dy = 0; break;  // West
                case 7: dx = -1; dy = -1; break; // Up (NW)
            }
        }
    }
}
