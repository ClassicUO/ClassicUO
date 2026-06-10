// Port of legacy Game/UI/Gumps/QuestArrowGump.cs.
//
// The server pushes 0xBA (quest pointer): display=true creates/moves an arrow
// pointing at a world coordinate, display=false removes it (post-7090 keys
// arrows by serial; pre-7090 has a single anonymous arrow, serial 0).
//
// The arrow is a screen-space UI sprite (gump 0x1194 + direction) that slides
// along the viewport edge toward the target, clamped inside camera bounds,
// blinking white/red once per second. Left or right click sends
// Send_ClickQuestArrow (0xBF subcmd 0x07) — the server decides what happens
// (right is the abandon-quest prompt). Not movable, not right-click-closable.

using System;
using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;

namespace ClassicUO.Ecs;

// One live quest arrow. Target is a world coordinate, not an entity.
internal struct QuestArrowUI
{
    public uint Serial;
    public ushort TargetX;
    public ushort TargetY;
    public ushort Graphic;    // current 0x1194+dir sprite
    public float NextBlink;   // Time.Total ms of the next hue flip
    public bool RedPhase;
}

internal sealed class QuestArrowState
{
    // serial -> arrow UI entity (pre-7090 uses serial 0).
    public readonly Dictionary<uint, ulong> Arrows = new();
    // Press latch for the click gesture (entity + button).
    public ulong PressedArrow;
    public MouseButtonType PressedButton;
    // Agent-harness injections (debug.questArrow) drained by a system that
    // has Commands — the RPC handler can't spawn entities itself.
    public readonly List<(uint Serial, bool Display, ushort X, ushort Y)> Pending = new();
}

internal readonly struct QuestArrowPlugin : IPlugin
{
    private const ushort ArrowGumpBase = 0x1194;
    private const ushort BlinkHue = 0x21;
    private const float BlinkMs = 1000f;

    public void Build(App app)
    {
        app.AddResource(new QuestArrowState());

        // 0xBA — both client-version flavors route into the same apply.
        app.AddObserver((
            On<PacketReceived<OnQuestPointerPacket_0xBA_Pre7090>> trig,
            Commands commands,
            Res<QuestArrowState> state,
            Query<Data<QuestArrowUI>> arrowsQ) =>
        {
            var p = trig.Event.Packet;
            Apply(commands, state.Value, arrowsQ, serial: 0, p.Display, p.X, p.Y);
        });

        app.AddObserver((
            On<PacketReceived<OnQuestPointerPacket_0xBA_Post7090>> trig,
            Commands commands,
            Res<QuestArrowState> state,
            Query<Data<QuestArrowUI>> arrowsQ) =>
        {
            var p = trig.Event.Packet;
            Apply(commands, state.Value, arrowsQ, p.Serial, p.Display, p.X, p.Y);
        });

        // Drain harness injections through the same Apply path the packets use.
        app.AddSystem((
            Commands commands,
            Res<QuestArrowState> state,
            Query<Data<QuestArrowUI>> arrowsQ) =>
        {
            foreach (var (serial, display, x, y) in state.Value.Pending)
                Apply(commands, state.Value, arrowsQ, serial, display, x, y);
            state.Value.Pending.Clear();
        })
            .InStage(Stage.Update)
            .RunIf((Res<QuestArrowState> st) => st.Value.Pending.Count > 0)
            .Build();

        var updateFn = UpdateArrows;
        var clickFn = HandleClick;
        var claimFn = ClaimSelectedFromArrows;
        var disposeFn = DisposeOnLogout;

        app
            // PostUpdate next to the nameplate anchor pass: player position is
            // settled, layout snapshots after.
            .AddSystem(updateFn)
                .InStage(Stage.PostUpdate)
                .RunIf((Res<QuestArrowState> st) => st.Value.Arrows.Count > 0)
                .Build()
            // PreUpdate: consume the press (right would walk the player,
            // left would latch a world pickup) before those systems run.
            .AddSystem(clickFn)
                .InStage(Stage.PreUpdate)
                .RunIf((Res<QuestArrowState> st) => st.Value.Arrows.Count > 0)
                .RunIf((Res<TargetingState> t) => !t.Value.IsTargeting)
                .Build()
            // Stage.Last claim: world pick/pickup/use bail when the cursor is
            // over an arrow (the arrow entity carries no NetworkSerial, so
            // every world-action gate rejects it — same trick the generic
            // window claim uses).
            .AddSystem(claimFn)
                .InStage(Stage.Last)
                .RunIf((Res<QuestArrowState> st) => st.Value.Arrows.Count > 0)
                .Build()
            .AddSystem(disposeFn)
                .OnExit(GameState.GameScreen)
                .Build();
    }

    private static void Apply(
        Commands commands,
        QuestArrowState state,
        Query<Data<QuestArrowUI>> arrowsQ,
        uint serial,
        bool display,
        ushort x,
        ushort y)
    {
        if (display)
        {
            if (state.Arrows.TryGetValue(serial, out var existing) && arrowsQ.TryGet(existing, out var row))
            {
                var (_, arrow) = row;
                arrow.Ref.TargetX = x;
                arrow.Ref.TargetY = y;
                return;
            }

            var ui = commands.Spawn()
                .Insert(new Node
                {
                    Display = Display.None,   // positioned next frame
                    PositionType = PositionType.Absolute,
                    Width = Val.Px(1),
                    Height = Val.Px(1),
                })
                .Insert(new UiCustom
                {
                    Data = new UOCustomRender
                    {
                        Kind = UOCustomKind.Gump,
                        AssetId = ArrowGumpBase,
                        Hue = Vector3.UnitZ,
                    }
                })
                .Insert(new QuestArrowUI { Serial = serial, TargetX = x, TargetY = y })
                .Insert(new GlobalZIndex(0));
            state.Arrows[serial] = ui.Id;
        }
        else if (state.Arrows.Remove(serial, out var entity))
        {
            commands.Entity(entity).Despawn();
        }
    }

    private static void UpdateArrows(
        Res<Time> time,
        Res<Camera> camera,
        Res<AssetsServer> assets,
        Single<Data<WorldPosition, ScreenPositionOffset>, With<Player>> playerQ,
        Query<Data<QuestArrowUI, Node, UiCustom>> arrowsQ)
    {
        var (_, playerPos, playerOffset) = playerQ.Get();
        var bounds = camera.Value.Bounds;

        foreach (var (_, arrow, node, custom) in arrowsQ)
        {
            // Direction from the player tile to the quest target, mapped to
            // the 8 arrow sprites (legacy 0x1194 + (dir + 1) % 8).
            var dir = (Direction)GameCursor.GetMouseDirection(
                playerPos.Ref.X, playerPos.Ref.Y, arrow.Ref.TargetX, arrow.Ref.TargetY, 0);
            var gumpId = (ushort)(ArrowGumpBase + ((int)dir + 1) % 8);

            ref readonly var info = ref assets.Value.Gumps.GetGump(gumpId);
            int w = info.UV.Width, h = info.UV.Height;

            // Legacy QuestArrowGump.Update position math, verbatim: project
            // the tile delta into the iso screen offset from the view center,
            // undo the player's walk offset, nudge per direction so the arrow
            // points from its tip, then clamp into the camera bounds.
            int gox = playerPos.Ref.X - arrow.Ref.TargetX;
            int goy = playerPos.Ref.Y - arrow.Ref.TargetY;

            int x = (bounds.Width >> 1) - (gox - goy) * 22;
            int y = (bounds.Height >> 1) - (gox + goy) * 22;

            x -= (int)playerOffset.Ref.Value.X;
            y -= (int)playerOffset.Ref.Value.Y;
            y += playerPos.Ref.Z << 2;

            switch (dir)
            {
                case Direction.North: x -= w; break;
                case Direction.South: y -= h; break;
                case Direction.East: x -= w; y -= h; break;
                case Direction.West: break;
                case Direction.Right: x -= w; y -= h / 2; break;
                case Direction.Left: x += w / 2; y -= h / 2; break;
                case Direction.Up: x -= w / 2; y += h / 2; break;
                case Direction.Down: x -= w / 2; y -= h; break;
            }

            var p = camera.Value.WorldToScreen(new Point(x, y));
            x = p.X + bounds.X;
            y = p.Y + bounds.Y;

            x = Math.Clamp(x, bounds.X, bounds.Right - w);
            y = Math.Clamp(y, bounds.Y, bounds.Bottom - h);

            // 1s white/red blink (legacy _needHue flip).
            if (time.Value.Total >= arrow.Ref.NextBlink)
            {
                arrow.Ref.NextBlink = time.Value.Total + BlinkMs;
                arrow.Ref.RedPhase = !arrow.Ref.RedPhase;
            }

            arrow.Ref.Graphic = gumpId;
            var render = custom.Ref.Render();
            render.AssetId = gumpId;
            render.Hue = arrow.Ref.RedPhase ? new Vector3(BlinkHue, 1f, 1f) : Vector3.UnitZ;

            node.Ref.Display = Display.Flex;
            node.Ref.Left = Val.Px(x);
            node.Ref.Top = Val.Px(y);
            node.Ref.Width = Val.Px(w);
            node.Ref.Height = Val.Px(h);
        }
    }

    // Resolve a UiPick hit to an arrow entity (arrows have no children).
    private static ulong ArrowHit(
        Vector2 pos,
        AssetsServer assets,
        Query<Data<ComputedNode, Node, UiCustom, BackgroundColor, Text>, Filter<Optional<UiCustom>, Optional<BackgroundColor>, Optional<Text>>> rendered,
        Query<Data<TinyEcs.Parent>> parents,
        Query<Data<QuestArrowUI>> arrowsQ)
    {
        var hit = UiPick.Topmost(pos, assets, rendered, parents);
        return hit.Found && arrowsQ.Contains(hit.Entity) ? hit.Entity : 0;
    }

    // Left OR right click on the arrow -> Send_ClickQuestArrow(right).
    // UiClick-style latch: press and release must land on the same arrow.
    private static void HandleClick(
        Res<MouseContext> mouse,
        Res<NetClient> net,
        Res<QuestArrowState> state,
        Res<AssetsServer> assets,
        Query<Data<ComputedNode, Node, UiCustom, BackgroundColor, Text>, Filter<Optional<UiCustom>, Optional<BackgroundColor>, Optional<Text>>> rendered,
        Query<Data<TinyEcs.Parent>> parents,
        Query<Data<QuestArrowUI>> arrowsQ)
    {
        var st = state.Value;

        foreach (var button in stackalloc[] { MouseButtonType.Left, MouseButtonType.Right })
        {
            if (mouse.Value.IsPressedOnce(button))
            {
                var target = ArrowHit(mouse.Value.Position, assets.Value, rendered, parents, arrowsQ);
                if (target != 0)
                {
                    st.PressedArrow = target;
                    st.PressedButton = button;
                    mouse.Value.Consume(button);
                }
                return;
            }
        }

        if (st.PressedArrow == 0)
            return;

        // Keep the held press consumed so movement/pickup never see it.
        if (mouse.Value.IsPressed(st.PressedButton))
        {
            mouse.Value.Consume(st.PressedButton);
            return;
        }

        if (mouse.Value.IsReleased(st.PressedButton))
        {
            var target = st.PressedArrow;
            var button = st.PressedButton;
            st.PressedArrow = 0;

            mouse.Value.Consume(button);

            if (ArrowHit(mouse.Value.Position, assets.Value, rendered, parents, arrowsQ) != target)
                return; // dragged off — cancel

            net.Value.Send_ClickQuestArrow(button == MouseButtonType.Right);
        }
    }

    private static void ClaimSelectedFromArrows(
        Res<MouseContext> mouse,
        Res<SelectedEntity> selected,
        Res<AssetsServer> assets,
        Query<Data<ComputedNode, Node, UiCustom, BackgroundColor, Text>, Filter<Optional<UiCustom>, Optional<BackgroundColor>, Optional<Text>>> rendered,
        Query<Data<TinyEcs.Parent>> parents,
        Query<Data<QuestArrowUI>> arrowsQ)
    {
        var target = ArrowHit(mouse.Value.Position, assets.Value, rendered, parents, arrowsQ);
        if (target == 0) return;
        selected.Value.Set(target, float.MaxValue, bypassViewport: true);
    }

    private static void DisposeOnLogout(
        Commands commands,
        Res<QuestArrowState> state)
    {
        foreach (var entity in state.Value.Arrows.Values)
            commands.Entity(entity).Despawn();
        state.Value.Arrows.Clear();
        state.Value.PressedArrow = 0;
    }
}
