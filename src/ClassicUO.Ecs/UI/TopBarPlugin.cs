// Port of main's TopBarGump (src/Game/UI/Gumps/TopBarGump.cs): the always-on-top
// toolbar. Spawned on entering GameScreen, despawned on exit.
//
// Patterns mirror MiniMapPlugin: each button action is an entity-scoped
// On<UiClick> observer; captions are interactive entity-children so clicks on
// the text BUBBLE to the button's observer. Toggle buttons are UOButtons
// (normal/pressed/over swapped by GuiPlugin.UpdateUOButtonsState).
//
// Tree: root[IsTopBar] -> { fullGroup[TopBarFull] -> bg + minimizeBtn + N×
// (button -> caption); arrowBtn[TopBarArrow] (minimized state) }. Minimize hides
// fullGroup + shows the arrow; the arrow re-expands.
//
// Drag: press on the background (or the minimized arrow) and move — drags the
// root. Pressing a button activates it instead. Right-click resets to (0,0).

using System;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Input;
using ClassicUO.Network;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using ClayColor = Clay.Color;
using GameLayer = ClassicUO.Game.Data.Layer;

namespace ClassicUO.Ecs;

internal readonly struct TopBarPlugin : IPlugin
{
    private const int BarHeight = 27;
    private const int CaptionTop = 5; // resting caption Y; +1 when the button is pressed
    private static ClayColor s_capNormal = new(0, 0, 0, 255);
    private static ClayColor s_capHover = new(238, 238, 49, 255);

    public void Build(App app)
    {
        var syncFn = SyncPresence;
        var despawnFn = Despawn;
        var dragFn = Drag;
        var resetFn = RightClickReset;
        var pressFn = PressOffsetCaptions;

        app
            // Presence follows Profile.TopbarGumpIsDisabled live: spawns the
            // bar when enabled (incl. the first GameScreen frame), despawns it
            // when the options gump disables it.
            .AddSystem(syncFn)
                .InStage(Stage.Update)
                .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen)
                .Build()
            .AddSystem(despawnFn).OnExit(GameState.GameScreen).Build()
            // PreUpdate so the bar claims the shared DragGate BEFORE the
            // Stage.Update drags (GameScreen border-drag, WindowDragPlugin) —
            // the bar is on top, so a press on it must win over the game-window
            // border / windows beneath. Node moves apply at the same frame's
            // layout (UiLayoutStage runs after PreUpdate).
            .AddSystem(dragFn).InStage(Stage.PreUpdate).Build()
            .AddSystem(pressFn).InStage(Stage.Update).Build()
            .AddSystem(resetFn)
                .InStage(Stage.PreUpdate)
                .RunIf((Res<MouseContext> m) => m.Value.IsPressedOnce(MouseButtonType.Right))
                .Build();
    }

    // Mirrors TopBarGump.textTable: 0 = small (0x098B), 1 = large (0x098D).
    private static readonly (int sizeKind, string caption, Buttons id)[] s_buttons =
    {
        (0, "Map",        Buttons.Map),
        (1, "Paperdoll",  Buttons.Paperdoll),
        (1, "Inventory",  Buttons.Inventory),
        (1, "Journal",    Buttons.Journal),
        (0, "Chat",       Buttons.Chat),
        (0, "Help",       Buttons.Help),
        (1, "World Map",  Buttons.WorldMap),
        (0, "Info",       Buttons.Info),
        (0, "Debug",      Buttons.Debug),
        (1, "NetStats",   Buttons.NetStats),
        (1, "UO Store",   Buttons.UOStore),
        (1, "Global Chat", Buttons.GlobalChat),
    };

    private enum Buttons : int
    {
        Map, Paperdoll, Inventory, Journal, Chat, Help,
        WorldMap, Info, Debug, NetStats, UOStore, GlobalChat,
    }

    // Spawn / despawn the bar to match the profile flag. `spawnQueued` covers
    // the one-frame gap between queueing the deferred spawn and the root
    // becoming visible to the query (avoids a double spawn).
    private static void SyncPresence(
        Commands commands,
        Res<AssetsServer> assets,
        Res<GameContext> gameCtx,
        Res<ClassicUO.Configuration.Profile> profile,
        Local<bool> spawnQueued,
        Query<Data<Node>, Filter<With<IsTopBar>>> roots,
        Query<Data<TinyEcs.Children>> childrenQ)
    {
        bool exists = false;
        foreach (var _ in roots) { exists = true; break; }
        if (exists) spawnQueued.Value = false;

        if (profile.Value.TopbarGumpIsDisabled)
        {
            spawnQueued.Value = false;
            if (exists)
                Despawn(commands, roots, childrenQ);
            return;
        }

        if (exists || spawnQueued.Value) return;
        spawnQueued.Value = true;
        Spawn(commands, assets, gameCtx);
    }

    private static void Spawn(
        Commands commands,
        Res<AssetsServer> assets,
        Res<GameContext> gameCtx)
    {
        var smallInfo = assets.Value.Gumps.GetGump(0x098B);
        var smallWidth = smallInfo.UV.Width > 0 ? smallInfo.UV.Width : 50;
        var smallHeight = smallInfo.UV.Height > 0 ? smallInfo.UV.Height : 22;

        var largeInfo = assets.Value.Gumps.GetGump(0x098D);
        var largeWidth = largeInfo.UV.Width > 0 ? largeInfo.UV.Width : 100;
        var largeHeight = largeInfo.UV.Height > 0 ? largeInfo.UV.Height : 22;

        var hasUOStore = gameCtx.Value.ClientVersion >= ClassicUO.Utility.ClientVersion.CV_706400;
        var startX = 30;
        for (var i = 0; i < s_buttons.Length; i++)
        {
            if (!hasUOStore && (int)s_buttons[i].id >= (int)Buttons.UOStore) break;
            startX += (s_buttons[i].sizeKind == 1 ? largeWidth : smallWidth) + 1;
        }
        var totalWidth = startX + 1;

        var root = commands.Spawn()
            .Insert(new Node
            {
                PositionType = PositionType.Absolute,
                Left = Val.Px(0), Top = Val.Px(0),
                Width = Val.Px(totalWidth), Height = Val.Px(BarHeight),
            })
            // Sit one above the game window (z 0) — just over the world view,
            // below draggable gump windows (UiZCounter bumps from 1 upward).
            .Insert(new ZIndex(1))
            .Insert<IsTopBar>();

        var fullGroup = commands.Spawn()
            .Insert(new Node
            {
                PositionType = PositionType.Absolute,
                Left = Val.Px(0), Top = Val.Px(0),
                Width = Val.Px(totalWidth), Height = Val.Px(BarHeight),
            })
            .Insert<TopBarFull>();
        commands.AddChild(root.Id, fullGroup.Id);

        // Background ResizePic (0x13BE) — also the drag handle.
        var bg = commands.Spawn()
            .Insert(new Node
            {
                PositionType = PositionType.Absolute,
                Left = Val.Px(0), Top = Val.Px(0),
                Width = Val.Px(totalWidth), Height = Val.Px(BarHeight),
            })
            .Insert(new UiCustom { Data = new UOCustomRender { Kind = UOCustomKind.GumpNinePatch, AssetId = 0x13BE, Hue = Vector3.UnitZ } })
            .Insert(Interaction.None)
            .Insert<TopBarDragHandle>();
        commands.AddChild(fullGroup.Id, bg.Id);

        // Minimize handle (UOButton 0x15A4/5/6) — hides full bar, shows arrow.
        var minBtn = commands.Spawn()
            .Insert(new Node
            {
                PositionType = PositionType.Absolute,
                Left = Val.Px(5), Top = Val.Px(3),
                Width = Val.Px(15), Height = Val.Px(20),
            })
            .Insert(new UiCustom { Data = new UOCustomRender { Kind = UOCustomKind.Gump, AssetId = 0x15A4, Hue = Vector3.UnitZ } })
            .Insert(new UOButton { Normal = 0x15A4, Pressed = 0x15A6, Over = 0x15A5 })
            .Insert(new Button())
            .Insert(Interaction.None)
            .Insert<TopBarButton>();
        minBtn.Observe((On<UiClick> _,
                        Query<Data<Node>, Filter<With<TopBarFull>>> fullQ,
                        Query<Data<Node>, Filter<With<TopBarArrow>>> arrowQ) =>
            SetMinimized(fullQ, arrowQ, minimized: true));
        commands.AddChild(fullGroup.Id, minBtn.Id);

        // Caption hues: legacy RighClickableButton(normalHue:0, hoverHue:0x0036).
        // hue 0 renders black; hover swaps to hue 0x0036's colour (a yellow).
        // The caption (Text, no UiCustom) isn't hit-testable, so hover is driven
        // off the parent BUTTON's Interaction in PressOffsetCaptions.
        s_capNormal = new ClayColor(0, 0, 0, 255);
        uint hc = UoFontRuntime.Hues != null ? UoFontRuntime.Hues.GetPolygoneColor(0x1F, 0x36) : 0x00FFFFFFu;
        s_capHover = new ClayColor((int)(hc & 0xFF), (int)((hc >> 8) & 0xFF), (int)((hc >> 16) & 0xFF), 255);

        var x = 30;
        for (var i = 0; i < s_buttons.Length; i++)
        {
            var (sizeKind, caption, btnId) = s_buttons[i];
            if (!hasUOStore && (int)btnId >= (int)Buttons.UOStore) break;

            var w = sizeKind == 1 ? largeWidth : smallWidth;
            var h = sizeKind == 1 ? largeHeight : smallHeight;
            var gumpId = (ushort)(sizeKind == 1 ? 0x098D : 0x098B);

            var btn = commands.Spawn()
                .Insert(new Node
                {
                    PositionType = PositionType.Absolute,
                    Left = Val.Px(x), Top = Val.Px(1),
                    Width = Val.Px(w), Height = Val.Px(h),
                })
                .Insert(new UiCustom { Data = new UOCustomRender { Kind = UOCustomKind.Gump, AssetId = gumpId, Hue = Vector3.UnitZ } })
                .Insert(Interaction.None)
                .Insert<TopBarButton>();
            commands.AddChild(fullGroup.Id, btn.Id);

            // Caption: absolute (button-relative), an entity-child of the button
            // with Interaction so clicks on the text bubble to the button. The
            // Draw path is left-aligned, so center via the measured unicode width.
            // Absolute (not flex) because PressOffsetCaptions nudges Node.Top +1px
            // on press, and (matching upstream clay.h) only Absolute nodes honour
            // Left/Top — clay.h has no in-flow position offset.
            int textW = (int)(UoFontRuntime.Fonts?.GetWidthUnicode(1, caption) ?? w);
            int captionX = Math.Max(0, (w - textW) / 2);
            var label = commands.Spawn()
                .Insert(new Node
                {
                    PositionType = PositionType.Absolute,
                    Left = Val.Px(captionX), Top = Val.Px(CaptionTop),
                    Width = Val.Px(textW), Height = Val.Px(h),
                })
                .Insert(new Text(caption))
                // Legacy uses unicode font 1, hue 0 — which renders BLACK, not
                // white (the top-bar captions are dark on the marble button).
                .Insert(new TextFont { FontId = 1, Size = 12 })
                .Insert(new TextColor(new ClayColor(0, 0, 0, 255)))
                .Insert(Interaction.None)
                .Insert<TopBarCaption>();
            commands.AddChild(btn.Id, label.Id);

            WireButton(btn, btnId);

            x += w + 1;
        }

        // Minimized state: the expand arrow (UOButton 0x15A1/2/3). Hidden until
        // minimized; also a drag handle.
        ref readonly var arrowInfo = ref assets.Value.Gumps.GetGump(0x15A1);
        var arrowW = arrowInfo.UV.Width > 0 ? arrowInfo.UV.Width : 30;
        var arrowH = arrowInfo.UV.Height > 0 ? arrowInfo.UV.Height : BarHeight;
        var arrowBtn = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.None,
                PositionType = PositionType.Absolute,
                Left = Val.Px(0), Top = Val.Px(0),
                Width = Val.Px(arrowW), Height = Val.Px(arrowH),
            })
            .Insert(new UiCustom { Data = new UOCustomRender { Kind = UOCustomKind.Gump, AssetId = 0x15A1, Hue = Vector3.UnitZ } })
            .Insert(new UOButton { Normal = 0x15A1, Pressed = 0x15A3, Over = 0x15A2 })
            .Insert(new Button())
            .Insert(Interaction.None)
            .Insert<TopBarArrow>()
            .Insert<TopBarDragHandle>();
        arrowBtn.Observe((On<UiClick> _,
                          Query<Data<Node>, Filter<With<TopBarFull>>> fullQ,
                          Query<Data<Node>, Filter<With<TopBarArrow>>> arrowQ) =>
            SetMinimized(fullQ, arrowQ, minimized: false));
        commands.AddChild(root.Id, arrowBtn.Id);
    }

    private static void SetMinimized(
        Query<Data<Node>, Filter<With<TopBarFull>>> fullQ,
        Query<Data<Node>, Filter<With<TopBarArrow>>> arrowQ,
        bool minimized)
    {
        foreach (var (_, n) in fullQ) n.Ref.Display = minimized ? Display.None : Display.Flex;
        foreach (var (_, n) in arrowQ) n.Ref.Display = minimized ? Display.Flex : Display.None;
    }

    private struct DragAnchor
    {
        public bool Active;   // currently moving the bar (press began on bg/arrow)
        public bool Claimed;  // holds the shared DragGate (any press over the bar)
        public ulong Root;
        public Vector2 Mouse;
        public float OriginX, OriginY;
    }

    // Drag the whole bar by its background (or the minimized arrow). Pressing a
    // button activates it instead — buttons are skipped as latch targets. Mirrors
    // WindowDragPlugin's continuous-held + press-once bbox latch (Interaction is
    // stale on the press-once frame).
    private static void Drag(
        Res<MouseContext> mouse,
        Res<DragGate> gate,
        Res<AssetsServer> assets,
        Local<DragAnchor> anchor,
        Query<Data<ComputedNode, Node>, Filter<With<TopBarButton>>> buttonsQ,
        Query<Data<ComputedNode, Node>, Filter<With<TopBarDragHandle>>> handlesQ,
        Query<Data<ComputedNode, Node, UiCustom, BackgroundColor, Text>, Filter<Optional<UiCustom>, Optional<BackgroundColor>, Optional<Text>>> allRenderedQ,
        Query<Data<Node>, Filter<With<IsTopBar>>> rootQ)
    {
        bool held = mouse.Value.IsPressed(MouseButtonType.Left)
                 || mouse.Value.IsPressedOnce(MouseButtonType.Left);
        if (!held)
        {
            // Release the shared gate only if WE claimed it.
            if (anchor.Value.Claimed && gate.Value.Mode == ActiveDrag.UIWindow)
                gate.Value.Mode = ActiveDrag.None;
            anchor.Value.Active = false;
            anchor.Value.Claimed = false;
            anchor.Value.Root = 0;
            return;
        }

        if (mouse.Value.IsPressedOnce(MouseButtonType.Left))
        {
            // Another drag (window / pickup / resize) owns this gesture.
            if (gate.Value.Mode != ActiveDrag.None) return;

            var pos = mouse.Value.Position;

            // Over the bar at all? The bg (or minimized arrow) handle spans the
            // whole bar and sits under the buttons. Any press over it CLAIMS the
            // gesture so WindowDragPlugin / pickup don't grab a gump beneath the
            // bar (the bar is on top). Buttons still fire their UiClick.
            bool overBar = false;
            int barTop = int.MinValue;
            foreach (var (_, bb, node) in handlesQ)
            {
                if (node.Ref.Display == Display.None) continue;
                if (bb.Ref.PaintOrder > barTop) barTop = bb.Ref.PaintOrder;
                if (Inside(bb.Ref, pos)) overBar = true;
            }
            if (!overBar) return;
            // Buttons paint above the bg handle; fold them into the threshold so
            // the bar's own controls aren't mistaken for a gump stacked above.
            foreach (var (_, bb, node) in buttonsQ)
            {
                if (node.Ref.Display == Display.None) continue;
                if (bb.Ref.PaintOrder > barTop) barTop = bb.Ref.PaintOrder;
            }

            // A gump painted above the bar at this pixel owns the gesture — bail
            // WITHOUT claiming the gate so WindowDragPlugin (Stage.Update) can
            // latch the gump. Bbox-over-bar is not enough: gumps stack above the
            // bar (z=1, bumped up) and a press there must reach the gump.
            if (GumpAbove(assets.Value, pos, barTop, allRenderedQ)) return;

            gate.Value.Mode = ActiveDrag.UIWindow;
            anchor.Value.Claimed = true;

            // Only the bg/arrow area drags the bar; a press on a button just
            // claims the gate and lets the button's own UiClick run.
            bool overButton = false;
            foreach (var (_, bb, node) in buttonsQ)
            {
                if (node.Ref.Display == Display.None) continue;
                if (Inside(bb.Ref, pos)) { overButton = true; break; }
            }

            if (!overButton)
            {
                foreach (var (ent, node) in rootQ)
                {
                    anchor.Value.Active = true;
                    anchor.Value.Root = ent.Ref;
                    anchor.Value.Mouse = pos;
                    anchor.Value.OriginX = node.Ref.Left.Type == ValType.Px ? node.Ref.Left.Value : 0f;
                    anchor.Value.OriginY = node.Ref.Top.Type == ValType.Px ? node.Ref.Top.Value : 0f;
                    break;
                }
            }
        }

        if (!anchor.Value.Active || !rootQ.TryGet(anchor.Value.Root, out var rootRow))
        {
            anchor.Value.Active = false;
            return;
        }

        var delta = mouse.Value.Position - anchor.Value.Mouse;
        var (_, rootNode) = rootRow;
        rootNode.Ref.Left = Val.Px(anchor.Value.OriginX + delta.X);
        rootNode.Ref.Top = Val.Px(anchor.Value.OriginY + delta.Y);
    }

    // Legacy: right-click on the bar snaps it back to (0,0).
    private static void RightClickReset(
        Res<MouseContext> mouse,
        Res<AssetsServer> assets,
        Query<Data<ComputedNode, Node>, Filter<With<TopBarDragHandle>>> handlesQ,
        Query<Data<ComputedNode, Node>, Filter<With<TopBarButton>>> buttonsQ,
        Query<Data<ComputedNode, Node, UiCustom, BackgroundColor, Text>, Filter<Optional<UiCustom>, Optional<BackgroundColor>, Optional<Text>>> allRenderedQ,
        Query<Data<Node>, Filter<With<IsTopBar>>> rootQ)
    {
        var pos = mouse.Value.Position;
        bool over = false;
        int barTop = int.MinValue;
        foreach (var (_, bb, node) in handlesQ)
        {
            if (node.Ref.Display == Display.None) continue;
            if (bb.Ref.PaintOrder > barTop) barTop = bb.Ref.PaintOrder;
            if (Inside(bb.Ref, pos)) over = true;
        }
        if (!over) return;
        foreach (var (_, bb, node) in buttonsQ)
        {
            if (node.Ref.Display == Display.None) continue;
            if (bb.Ref.PaintOrder > barTop) barTop = bb.Ref.PaintOrder;
        }

        // Gump painted above the bar here owns the right-click (close-on-right
        // click) — don't consume it for the bar reset.
        if (GumpAbove(assets.Value, pos, barTop, allRenderedQ)) return;

        mouse.Value.Consume(MouseButtonType.Right);
        foreach (var (_, node) in rootQ)
        {
            node.Ref.Left = Val.Px(0);
            node.Ref.Top = Val.Px(0);
        }
    }

    private static bool Inside(in ComputedNode bb, Vector2 p)
        => p.X >= bb.Position.X && p.Y >= bb.Position.Y
        && p.X < bb.Position.X + bb.Size.X && p.Y < bb.Position.Y + bb.Size.Y;

    // True when ANY UO element is painted above the bar at `pos` — including a
    // gump window's CHILD sprites (container items, paperdoll body/equipment),
    // not just movable roots. Container/paperdoll roots have a transparent
    // interior, so checking only the root bg let an opaque item/body sitting
    // over the bar fall through and the bar swallowed the click. `barTopOrder`
    // is the bar's OWN topmost paint order, so the bar's buttons/bg don't count
    // as "above". Pixel-perfect: a transparent gump pixel still passes through.
    private static bool GumpAbove(
        AssetsServer assets,
        Vector2 pos,
        int barTopOrder,
        Query<Data<ComputedNode, Node, UiCustom, BackgroundColor, Text>, Filter<Optional<UiCustom>, Optional<BackgroundColor>, Optional<Text>>> allRenderedQ)
    {
        // Topmost element overall sits above the bar's own topmost element ->
        // a gump is stacked above the bar here. Same pick everything else uses.
        var hit = UiPick.Topmost(pos, assets, allRenderedQ);
        return hit.Found && hit.PaintOrder > barTopOrder;
    }

    private static void WireButton(EntityCommands btn, Buttons id)
    {
        switch (id)
        {
            case Buttons.Map:
                btn.Observe((On<UiClick> _,
                             Commands cmd,
                             Res<GumpBuilder> gb,
                             Res<UiZCounter> z,
                             Query<Data<MiniMapWindow>> existing) =>
                    MiniMapPlugin.OpenOrFocus(cmd, gb.Value, z.Value, existing));
                break;

            case Buttons.Paperdoll:
                // Mirror legacy GameActions.OpenPaperdoll: focus an already-open
                // paperdoll, else request it with the 0x80000000 high bit set.
                // A PLAIN dclick on the player serial is the default action
                // (attack in war mode) — the high bit forces the paperdoll open.
                btn.Observe((On<UiClick> _,
                             Commands cmd,
                             Res<NetClient> net,
                             Res<GameContext> ctx,
                             Res<UiZCounter> z,
                             Query<Data<PaperdollWindow>> existing) =>
                {
                    if (ctx.Value.PlayerSerial == 0) return;

                    foreach (var (ent, w) in existing)
                    {
                        if (w.Ref.Serial != ctx.Value.PlayerSerial) continue;
                        cmd.Entity(ent.Ref).Insert(new GlobalZIndex(z.Value.Bump()));
                        return;
                    }

                    net.Value.Send_DoubleClick(ctx.Value.PlayerSerial | 0x80000000);
                });
                break;

            case Buttons.Inventory:
                btn.Observe((On<UiClick> _,
                             Res<NetClient> net,
                             Query<Data<EquipmentSlots>, Filter<With<Player>>> playerQ,
                             Query<Data<NetworkSerial>> serialQ) =>
                {
                    foreach (var (_, slots) in playerQ)
                    {
                        var bp = slots.Ref[GameLayer.Backpack];
                        if (bp != 0 && serialQ.TryGet(bp, out var serialRow))
                        {
                            var (_, ns) = serialRow;
                            net.Value.Send_DoubleClick(ns.Ref.Value);
                        }
                        break;
                    }
                });
                break;

            case Buttons.Help:
                btn.Observe((On<UiClick> _, Res<NetClient> net) => net.Value.Send_HelpRequest());
                break;

            case Buttons.Journal:
                btn.Observe((On<UiClick> _,
                             Commands cmd,
                             Res<GumpBuilder> gb,
                             Res<AssetsServer> assets,
                             Res<UiZCounter> z,
                             Query<Data<JournalWindow>> existing) =>
                    JournalPlugin.OpenOrFocus(cmd, gb.Value, assets.Value, z.Value, existing));
                break;
            case Buttons.Chat:
                btn.Observe((On<UiClick> _) => Console.WriteLine("[TopBar] Chat — no ECS gump yet"));
                break;
            case Buttons.WorldMap:
                btn.Observe((On<UiClick> _,
                             Commands cmd,
                             ResMut<WorldMapState> state,
                             Res<Profile> profile,
                             Res<UiZCounter> z,
                             Query<Data<WorldMapWindow>> existing) =>
                    WorldMapGumpPlugin.OpenOrFocus(cmd, state.Value, profile.Value, z.Value, existing));
                break;
            case Buttons.Debug:
                // Repurposed as an Options/test window launcher (host-side gump
                // with buttons that drive client-local features — e.g. the dye
                // ColorPicker — that have no in-world trigger yet).
                btn.Observe((On<UiClick> _,
                             Commands cmd,
                             Res<UiZCounter> z,
                             Res<UiSurface> surf,
                             Res<OptionsUiState> st,
                             Query<Data<OptionsWindow>> existing) =>
                    OptionsGumpPlugin.OpenOrFocus(cmd, z.Value, surf.Value, st.Value, existing));
                break;
            case Buttons.NetStats:
                btn.Observe((On<UiClick> _) => Console.WriteLine("[TopBar] NetStats — no ECS gump yet"));
                break;
            case Buttons.UOStore:
                btn.Observe((On<UiClick> _) => Console.WriteLine("[TopBar] UO Store — no ECS path yet"));
                break;
            case Buttons.GlobalChat:
                btn.Observe((On<UiClick> _) => Console.WriteLine("[TopBar] Global Chat — not implemented (matches legacy)"));
                break;

            case Buttons.Info:
                break;
        }
    }

    // Drives caption press-feedback from the parent button's Interaction (the
    // caption itself isn't hit-testable — Text has no UiCustom — so it never
    // hovers; the button beneath it does). Replicates legacy Button.Draw:
    //   * pressed -> caption +1px down (yoffset = IsClicked ? 1 : 0)
    //   * hovered/pressed -> caption recoloured to hoverHue 0x0036, else hue 0.
    private static void PressOffsetCaptions(
        Query<Data<Interaction, Node, TextColor, TinyEcs.Parent>, Filter<With<TopBarCaption>>> captionsQ,
        Query<Data<Interaction>> interQ)
    {
        foreach (var (_, capInter, node, color, parent) in captionsQ)
        {
            // The caption captures the press when clicked over the text; the
            // parent button captures it when clicked on the bare button face.
            // Honour either.
            var btnInter = Interaction.None;
            if (interQ.TryGet(parent.Ref.Id, out var interRow))
            {
                var (_, pInter) = interRow;
                btnInter = pInter.Ref;
            }

            bool pressed = capInter.Ref == Interaction.Pressed || btnInter == Interaction.Pressed;
            bool over = pressed
                     || capInter.Ref == Interaction.Hovered || btnInter == Interaction.Hovered;
            node.Ref.Top = Val.Px(pressed ? CaptionTop + 1 : CaptionTop);
            color.Ref.Value = over ? s_capHover : s_capNormal;
        }
    }

    private static void Despawn(
        Commands commands,
        Query<Data<Node>, Filter<With<IsTopBar>>> roots,
        Query<Data<TinyEcs.Children>> childrenQ)
    {
        foreach (var (ent, _) in roots)
            DespawnSubtree(commands, ent.Ref, childrenQ);
    }

    private static void DespawnSubtree(Commands commands, ulong entity, Query<Data<TinyEcs.Children>> childrenQ)
    {
        if (childrenQ.TryGet(entity, out var childrenRow))
        {
            var (_, kids) = childrenRow;
            foreach (var cid in kids.Ref)
                DespawnSubtree(commands, cid, childrenQ);
        }
        commands.Entity(entity).Despawn();
    }
}

internal struct IsTopBar;
internal struct TopBarFull;       // wraps the expanded bar; hidden when minimized
internal struct TopBarArrow;      // minimized expand-arrow; hidden when expanded
internal struct TopBarButton;     // clickable buttons (skipped as drag latch targets)
internal struct TopBarDragHandle; // bg + arrow: press here to drag the bar
internal struct TopBarCaption;    // button caption; nudges down +1px while pressed
