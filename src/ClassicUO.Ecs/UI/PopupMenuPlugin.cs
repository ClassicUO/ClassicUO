// PopupMenuGump — ECS port of Game/UI/Gumps/PopupMenuGump.cs. The server's
// 0xBF 0x14 "display popup menu" (parsed in OnExtendedCommandPacket_0xBF, stored
// in PopupMenuState) opens a context menu at the cursor: a 0x0A3C resizepic
// background at 0.75 alpha with one cliloc label per entry. Left-click an entry
// to pick it (Send_PopupMenuSelection); right-click or click away to dismiss.
//
// The request side mirrors legacy DelayedObjectClickManager: a clean single
// left-click on a world entity arms a delayed Send_RequestPopupMenu that fires
// once the double-click window elapses (a double-click cancels it — that's a
// use-object gesture, owned by UseObjectPlugin).

using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using ClayColor = Clay.Color;

namespace ClassicUO.Ecs;

// Singleton: the pending single-click request + the latest server menu to build.
internal sealed class PopupMenuState
{
    // Request side (arming a delayed Send_RequestPopupMenu).
    public bool ClickPending;
    public uint ClickSerial;
    public float ClickTimer;
    public Vector2 ClickPos;
    // Where the next popup window spawns — the cursor position at request time.
    public Vector2 RequestPos;

    // Display side (set by the packet handler, consumed by PopupMenuBuild).
    public bool HasPending;
    public uint PendingSerial;
    public OnExtendedCommandPacket_0xBF.PopupMenuItemData[] PendingItems = System.Array.Empty<OnExtendedCommandPacket_0xBF.PopupMenuItemData>();

    public void SetPending(uint serial, OnExtendedCommandPacket_0xBF.PopupMenuItemData[] items)
    {
        HasPending = true;
        PendingSerial = serial;
        PendingItems = items;
    }
}

// Window root marker (carries the entity serial the menu acts on).
internal struct PopupMenuRoot { public uint Serial; }
// Clickable entry row (carries the menu item index sent back on selection,
// and the child bar entity recoloured on hover).
internal struct PopupMenuRow { public uint Serial; public ushort Index; public ulong Bar; }

internal readonly struct PopupMenuPlugin : IPlugin
{
    private const ushort Background = 0x0A3C;   // resizepic
    private const ushort ArrowGump = 0x15E6;    // submenu/continuation arrow
    private const int Pad = 10;
    // Legacy HitBox hover bar: white at alpha 0.25 (≈64/255).
    private static readonly ClayColor Transparent = new(0, 0, 0, 0);
    private static readonly ClayColor Highlight = new(255, 255, 255, 64);

    public void Build(App app)
    {
        app.AddResource(new PopupMenuState());

        var trackFn = PopupMenuTrack;
        app.AddSystem(trackFn).InStage(Stage.Update)
            .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen).Build();

        var buildFn = PopupMenuBuild;
        app.AddSystem(buildFn).InStage(Stage.Update)
            .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen).Build();

        var hoverFn = PopupRowHover;
        app.AddSystem(hoverFn).InStage(Stage.Update)
            .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen).Build();

        // Runs before world input so it can eat the click that selects/closes.
        var inputFn = PopupMenuInput;
        app.AddSystem(inputFn).InStage(Stage.PreUpdate).Build();

        var despawnFn = PopupMenuDespawnAll;
        app.AddSystem(despawnFn).OnExit(GameState.GameScreen).Build();
    }

    // Arm / fire / cancel the delayed popup request.
    private static void PopupMenuTrack(
        Res<Time> time,
        Res<MouseContext> mouse,
        Res<SelectedEntity> selected,
        Res<NetClient> net,
        Res<GrabbedItem> grabbed,
        ResMut<TargetingState> targeting,
        Res<KeyboardContext> keyboard,
        Res<Profile> profile,
        Res<GameContext> gameCtx,
        ResMut<PopupMenuState> state,
        Query<Data<NetworkSerial>> serialQ,
        Query<Data<ComputedNode>, With<PopupMenuRoot>> popupQ,
        Query<Data<ComputedNode>, With<UiMovable>> movableQ)
    {
        // A double-click is a use-object gesture; it cancels a pending popup.
        if (mouse.Value.IsPressedDouble(MouseButtonType.Left))
            state.Value.ClickPending = false;

        if (state.Value.ClickPending && time.Value.Total > state.Value.ClickTimer)
        {
            state.Value.ClickPending = false;
            net.Value.Send_RequestPopupMenu(state.Value.ClickSerial);
            state.Value.RequestPos = state.Value.ClickPos;
        }

        if (!mouse.Value.IsReleased(MouseButtonType.Left)) return;
        // A target cursor resolves on the PRESS (TargetingPlugin, Stage.First) and
        // clears IsTargeting before this release-time gate runs, so IsTargeting is
        // already false here. Swallow the release that ended a targeting click so
        // it can't arm a context-menu request for the just-targeted object.
        if (targeting.Value.JustTargeted)
        {
            targeting.Value.JustTargeted = false;
            return;
        }
        if (targeting.Value.IsTargeting) return;
        if (grabbed.Value.IsActive || grabbed.Value.Serial != 0) return;

        // Only request a popup if the server advertised context-menu support
        // (CLF_CONTEXT_MENU). Legacy gates DelayedObjectClickManager.OpenPopupMenu
        // on ClientFeatures.PopupEnabled; without this we spam 0xBF requests at
        // servers that have no context menus.
        if ((gameCtx.Value.ClientFeatures & CharacterListFlags.CLF_CONTEXT_MENU) == 0) return;

        // Legacy GameActions.OpenPopupMenu: with HoldShiftForContext set, the
        // menu only opens while Shift is held.
        if (profile.Value.HoldShiftForContext
            && !keyboard.Value.IsPressed(Microsoft.Xna.Framework.Input.Keys.LeftShift)
            && !keyboard.Value.IsPressed(Microsoft.Xna.Framework.Input.Keys.RightShift)) return;

        // Don't open a second popup over an open one.
        foreach (var _ in popupQ) return;

        var target = selected.Value.Entity;
        if (!serialQ.TryGet(target, out var serialRow)) return;
        var (_, ser) = serialRow;
        if (ser.Ref.Value == 0) return;

        // Ignore clicks that landed on a gump window, not the world.
        var pos = mouse.Value.Position;
        foreach (var (_, bb) in movableQ)
            if (Contains(bb.Ref, pos)) return;

        state.Value.ClickPending = true;
        state.Value.ClickSerial = ser.Ref.Value;
        state.Value.ClickTimer = time.Value.Total + MouseContext.DoubleClickDelta;
        state.Value.ClickPos = pos;
    }

    // Build the menu window from the pending server data.
    private static void PopupMenuBuild(
        Commands commands,
        ResMut<PopupMenuState> state,
        Res<AssetsServer> assets,
        Res<UOFileManager> files,
        Res<GumpBuilder> builder,
        Res<UiZCounter> z,
        Query<Data<PopupMenuRoot>> existingQ)
    {
        if (!state.Value.HasPending) return;
        state.Value.HasPending = false;

        var items = state.Value.PendingItems;
        var serial = state.Value.PendingSerial;
        var pos = state.Value.RequestPos;

        // Only one popup at a time — drop any open one.
        foreach (var (ent, _) in existingQ)
            commands.Entity(ent.Ref).Despawn();

        // Layout pass mirrors legacy PopupMenuGump: stack labels from y=10, size
        // the background to the widest label (+20) and the total height. The
        // arrow flag short-circuits the size accumulation exactly as legacy does.
        var rows = new List<RowLayout>(items.Length);
        int offsetY = Pad;
        bool arrowAdded = false;
        int width = 0, height = 20;

        for (int i = 0; i < items.Length; i++)
        {
            ref readonly var it = ref items[i];
            string text = files.Value.Clilocs.GetString(it.Cliloc) ?? string.Empty;
            bool html = it.ReplacedHue != 0;
            uint htmlColor = html ? ((HuesHelper.Color16To32(it.ReplacedHue) << 8) | 0xFF) : 0u;

            var (lw, th) = UoFontRenderer.Measure(text, font: 1, 600, isHtml: html, htmlStartColor: htmlColor, htmlBg: false);
            // Legacy Label.Height for unicode == GetHeightUnicode + 4 (the
            // RenderedText margin, FontsLoader.GeneratePixelsUnicode). Match it so
            // the row pitch and overall window height equal PopupMenuGump's.
            int lh = th + 4;

            bool thisArrow = (it.Flags & 0x02) != 0 && !arrowAdded;

            rows.Add(new RowLayout
            {
                Text = text,
                Index = it.Index,
                Hue = it.Hue,
                Html = html,
                HtmlColor = htmlColor,
                Width = lw,
                Height = lh,
                Y = offsetY,
                Arrow = thisArrow,
            });

            if (thisArrow)
            {
                arrowAdded = true;
                height += 20;
            }

            offsetY += lh;

            if (!arrowAdded)
            {
                height += lh;
                if (width < lw) width = lw;
            }
        }

        width += 20;
        if (height <= 10 || width <= 20) return;

        int rowW = width - 20;

        var root = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(pos.X),
                Top = Val.Px(pos.Y),
                Width = Val.Px(width),
                Height = Val.Px(height),
            })
            .Insert(new UiCustom
            {
                Data = new UOCustomRender
                {
                    Kind = UOCustomKind.GumpNinePatch,
                    AssetId = Background,
                    // (hue, mode, alpha); 0.75 alpha matches legacy ResizePic.Alpha.
                    Hue = new Vector3(0f, 0f, 0.75f),
                }
            })
            .Insert(Interaction.None)
            .Insert(new GlobalZIndex(z.Value.Bump()))
            .Insert(new PopupMenuRoot { Serial = serial });

        ulong rootId = root.Id;

        foreach (var r in rows)
        {
            ushort textHue = r.Hue == 0xFFFF ? (ushort)0 : r.Hue;   // 0xFFFF sentinel → white
            int rowH = r.Height <= 0 ? 18 : r.Height;

            // Row carries the layout box; hover/selection are driven by bbox-vs-
            // cursor (PopupRowHover / PopupMenuInput), not Bevy pointer events
            // (which don't reach these rows). UiCustom None emits the element so
            // it gets a ComputedNode to hit-test against. Two children paint
            // bottom-up: the highlight bar (BackgroundColor — needs its OWN
            // element since Clay renders a UiCustom OR a background, not both),
            // then the label on top.
            var row = commands.Spawn()
                .Insert(new Node
                {
                    PositionType = PositionType.Absolute,
                    Left = Val.Px(Pad),
                    Top = Val.Px(r.Y),
                    Width = Val.Px(rowW),
                    Height = Val.Px(rowH),
                })
                .Insert(new UiCustom { Data = new UOCustomRender { Kind = UOCustomKind.None, Hue = Vector3.UnitZ } });
            commands.AddChild(rootId, row.Id);

            var bar = commands.Spawn()
                .Insert(new Node
                {
                    PositionType = PositionType.Absolute,
                    Left = Val.Px(0),
                    Top = Val.Px(0),
                    Width = Val.Px(rowW),
                    Height = Val.Px(rowH),
                })
                .Insert(new BackgroundColor(Transparent));
            commands.AddChild(row.Id, bar.Id);

            var label = commands.Spawn()
                .Insert(new Node
                {
                    PositionType = PositionType.Absolute,
                    Left = Val.Px(0),
                    Top = Val.Px(0),
                    Width = Val.Px(rowW),
                    Height = Val.Px(rowH),
                })
                .Insert(new UiCustom
                {
                    Data = new UOCustomRender
                    {
                        Kind = UOCustomKind.WrappedText,
                        Hue = Vector3.UnitZ,
                        Text = r.Text,
                        TextFont = 1,
                        TextHue = textHue,
                        WrapWidth = rowW,
                        IsHtml = r.Html,
                        HtmlStartColor = r.HtmlColor,
                        HtmlBg = false,
                    }
                });
            commands.AddChild(row.Id, label.Id);

            commands.Entity(row.Id).Insert(new PopupMenuRow { Serial = serial, Index = r.Index, Bar = bar.Id });

            if (r.Arrow)
            {
                var arrow = builder.Value.AddGump(commands, ArrowGump, Vector3.UnitZ, new Vector2(20, r.Y));
                commands.AddChild(rootId, arrow.Id);
            }
        }
    }

    // All popup mouse input. Bbox-vs-cursor against the raw mouse (not Bevy
    // UiClick — pointer events don't reach these rows reliably). Selection fires
    // on RELEASE, matching legacy PopupMenuGump.OnMouseUp:
    //   * left press on the menu → consume (defer to release); press outside → close,
    //   * left release on a row → send that entry's selection + close,
    //   * left release elsewhere over an open menu → close,
    //   * right press anywhere → close.
    // PreUpdate + consume so the press/release is eaten before world pickup/use,
    // and so PopupMenuTrack doesn't re-arm a popup on the same release.
    private static void PopupMenuInput(
        Res<MouseContext> mouse,
        Res<NetClient> net,
        Commands commands,
        Query<Data<ComputedNode>, With<PopupMenuRoot>> popupQ,
        Query<Data<ComputedNode, PopupMenuRow>> rowsQ)
    {
        bool leftDown = mouse.Value.IsPressedOnce(MouseButtonType.Left);
        bool leftUp = mouse.Value.IsReleased(MouseButtonType.Left);
        bool rightDown = mouse.Value.IsPressedOnce(MouseButtonType.Right);
        if (!leftDown && !leftUp && !rightDown) return;

        var pos = mouse.Value.Position;
        bool any = false, over = false;
        foreach (var (_, bb) in popupQ)
        {
            any = true;
            if (Contains(bb.Ref, pos)) over = true;
        }
        if (!any) return;

        if (rightDown)
        {
            CloseAll(commands, popupQ);
            mouse.Value.Consume(MouseButtonType.Right);
            return;
        }

        if (leftDown)
        {
            if (over)
                mouse.Value.Consume(MouseButtonType.Left);   // defer to release
            else
                CloseAll(commands, popupQ);        // pressed away → dismiss
            return;
        }

        // Left release: select the row under the cursor (if any), then close.
        foreach (var (_, bb, row) in rowsQ)
        {
            if (!Contains(bb.Ref, pos)) continue;
            net.Value.Send_PopupMenuSelection(row.Ref.Serial, row.Ref.Index);
            break;
        }
        CloseAll(commands, popupQ);
        mouse.Value.Consume(MouseButtonType.Left);
    }

    private static void CloseAll(
        Commands commands,
        Query<Data<ComputedNode>, With<PopupMenuRoot>> popupQ)
    {
        foreach (var (ent, _) in popupQ)
            commands.Entity(ent.Ref).Despawn();
    }

    // Translucent white bar under the hovered row (legacy HitBox MouseIsOver).
    // Bbox-vs-cursor (same idiom as WindowDrag / SpellbookNav) — Bevy
    // Interaction.Hovered doesn't read reliably on these rows.
    private static void PopupRowHover(
        Res<MouseContext> mouse,
        Query<Data<ComputedNode, PopupMenuRow>> rowsQ,
        Query<Data<BackgroundColor>> barsQ)
    {
        var pos = mouse.Value.Position;
        foreach (var (_, bb, row) in rowsQ)
        {
            if (!barsQ.TryGet(row.Ref.Bar, out var barRow)) continue;
            var (_, bar) = barRow;
            bar.Ref.Value = Contains(bb.Ref, pos) ? Highlight : Transparent;
        }
    }

    private static void PopupMenuDespawnAll(
        Commands commands,
        Query<Data<PopupMenuRoot>> popupQ)
    {
        foreach (var (ent, _) in popupQ)
            commands.Entity(ent.Ref).Despawn();
    }

    private static bool Contains(in ComputedNode bb, Vector2 p)
        => p.X >= bb.Position.X && p.Y >= bb.Position.Y
        && p.X < bb.Position.X + bb.Size.X && p.Y < bb.Position.Y + bb.Size.Y;

    private struct RowLayout
    {
        public string Text;
        public ushort Index;
        public ushort Hue;
        public bool Html;
        public uint HtmlColor;
        public int Width;
        public int Height;
        public int Y;
        public bool Arrow;
    }
}
