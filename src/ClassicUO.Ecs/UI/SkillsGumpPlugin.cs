// StandardSkillsGump — ECS port of Game/UI/Gumps/StandardSkillsGump.cs. Opened
// from the paperdoll Skills button. Renders the ExpandableScroll (0x1F40 family,
// SKILLS title 0x0834) holding collapsible skill GROUPS; each group expands to
// its skill rows (use button, name, value, lock-state button).
//
// Chrome (scroll caps / body / minimize / resize / title) mirrors JournalPlugin
// exactly — both wrap the same ExpandableScroll. Content (groups + skills) is a
// flex column rebuilt on a dirty flag when a group is collapsed/expanded or a
// value/checkbox changes. Skill names come from the shared SkillsLoader; values
// from Res<PlayerSkills> (the 0x3A packet).

using System;
using ClassicUO.Assets;
using ClassicUO.Game.Data;
using ClassicUO.Input;
using ClassicUO.Network;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using TinyEcs.Bevy.UI.Widgets;
using ClayColor = Clay.Color;

namespace ClassicUO.Ecs;

internal struct SkillsWindow
{
    public int SpecialHeight;
    public bool Minimized;
    public bool ShowReal;   // checkbox: display base value
    public bool ShowCaps;   // checkbox: display cap value
    public bool Dirty;      // rebuild the group/skill rows
    public int Width;
    public float ListWidth;
    public float ContentHeight; // total stacked row height (for the scroll flag)
    public ulong ListEntity, TopEntity, MidEntity, BottomEntity;
    public ulong TitleEntity, MinimizeEntity, ExpanderEntity;
    public ulong SumLabelEntity, FlagEntity;
}

// Row marker carrying which group it toggles (collapse arrow click).
internal struct SkillGroupToggle { public int GroupIndex; }
internal struct SkillsResizeHandle { public ulong Window; }

internal readonly struct SkillsGumpPlugin : IPlugin
{
    private const ushort ScrollGraphic = 0x1F40; // +0 top, +2 body(tiled), +3 bottom
    private const ushort TitleGump = 0x0834;      // SKILLS banner
    private const ushort MinimizeGump = 0x082D;
    private const ushort ExpanderNormal = 0x082E;
    private const ushort ExpanderPressed = 0x082F;
    private const ushort GroupCollapsed = 0x0827; // arrow (group minimized)
    private const ushort GroupExpanded = 0x0826;  // arrow (group maximized)
    private const ushort DottedLine = 0x0835;      // GumpPicTiled fill after name
    private const ushort UseBtnN = 0x0837, UseBtnO = 0x0838;
    private const ushort LockUp = 0x0984, LockDown = 0x0986, LockLocked = 0x082C;
    private const ushort CheckOff = 0x0938, CheckOn = 0x0939;
    private const ushort NewGroupBtn = 0x083A;
    private const ushort FlagGump = 0x0828;   // scroll-position flag (Journal-style)
    private const ushort SkillHue = 0x0288;

    private const int DiffY = 22;
    private const int ListTop = 40;
    private const int ListLeft = 24;
    private const int DefaultHeight = 360;
    private const int MinHeight = 222;
    private const int MaxHeight = 800;
    private const int RowH = 17;

    public void Build(App app)
    {
        var layoutFn = LayoutWindow;
        app.AddSystem(layoutFn).InStage(Stage.Update)
            .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen).Build();

        var rebuildFn = RebuildContent;
        app.AddSystem(rebuildFn).InStage(Stage.Update)
            .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen).Build();

        var sumFn = RefreshSum;
        app.AddSystem(sumFn).InStage(Stage.Update)
            .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen).Build();

        // PreUpdate + DragGate claim so the resize press doesn't also latch a
        // window move (WindowDragPlugin.Drag bails when the gate is taken).
        var resizeFn = ResizeDrag;
        app.AddSystem(resizeFn).InStage(Stage.PreUpdate).Build();

        var resetFn = ResetClick;
        app.AddSystem(resetFn).InStage(Stage.PreUpdate).Build();

        // Scroll flag: slide it to reflect scroll (Update), drag it to scroll
        // (PreUpdate + DragGate claim).
        var flagPosFn = PositionFlag;
        app.AddSystem(flagPosFn).InStage(Stage.Update)
            .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen).Build();
        var flagDragFn = FlagDrag;
        app.AddSystem(flagDragFn).InStage(Stage.PreUpdate).Build();

        // Type into a focused group-name field.
        var nameEditFn = NameEditType;
        app.AddSystem(nameEditFn).InStage(Stage.Update)
            .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen).Build();

        // Background highlight on the group name currently being edited.
        var editHlFn = EditHighlight;
        app.AddSystem(editHlFn).InStage(Stage.Update)
            .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen).Build();

        // Drag a skill onto another group to move it (PreUpdate + gate claim).
        var skillDragFn = SkillDrag;
        app.AddSystem(skillDragFn).InStage(Stage.PreUpdate).Build();

        var despawnFn = Despawn;
        app.AddSystem(despawnFn).OnExit(GameState.GameScreen).Build();

        // ShowReal / ShowCaps checkboxes -> window flags. Legacy: the two are
        // mutually exclusive (checking one unchecks the other).
        app.AddObserver<On<CheckboxChanged>, Query<Data<SkillsCheck>>, Query<Data<SkillsWindow>>>(
            (trig, checks, windows) =>
            {
                if (!checks.Contains(trig.EntityId)) return;
                var (_, c) = checks.Get(trig.EntityId);
                if (!windows.Contains(c.Ref.Window)) return;
                var (_, win) = windows.Get(c.Ref.Window);
                if (c.Ref.Cap) { win.Ref.ShowCaps = trig.Event.Checked; if (trig.Event.Checked) win.Ref.ShowReal = false; }
                else { win.Ref.ShowReal = trig.Event.Checked; if (trig.Event.Checked) win.Ref.ShowCaps = false; }
                win.Ref.Dirty = true;
            });
    }

    private static int GumpW(AssetsServer a, ushort id) { ref readonly var g = ref a.Gumps.GetGump(id); return g.UV.Width; }
    private static int GumpH(AssetsServer a, ushort id) { ref readonly var g = ref a.Gumps.GetGump(id); return g.UV.Height; }

    public static void OpenOrFocus(
        Commands commands,
        GumpBuilder builder,
        AssetsServer assets,
        UiZCounter zCounter,
        PlayerSkills skills,
        Query<Data<SkillsWindow>> existingQ)
    {
        foreach (var (ent, _) in existingQ)
        {
            commands.Entity(ent.Ref).Insert(new GlobalZIndex(zCounter.Bump()));
            return;
        }

        if (skills.Groups.Count == 0)
            SkillsGroupDefaults.Build(skills.Groups, assets.Skills.SkillsCount);

        Build(commands, builder, assets, zCounter);
    }

    private static void Build(Commands commands, GumpBuilder builder, AssetsServer assets, UiZCounter zCounter)
    {
        int topW = GumpW(assets, ScrollGraphic), topH = GumpH(assets, ScrollGraphic);
        int midW = GumpW(assets, (ushort)(ScrollGraphic + 2));
        int botW = GumpW(assets, (ushort)(ScrollGraphic + 3));
        int width = Math.Max(topW, Math.Max(midW, botW));
        var z = zCounter.Bump();
        var pos = new Vector2(120, 100);

        var root = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(pos.X), Top = Val.Px(pos.Y),
                Width = Val.Px(width), Height = Val.Px(DefaultHeight),
            })
            .Insert(new UiCustom { Data = new UOCustomRender { Kind = UOCustomKind.None, Hue = Vector3.UnitZ } })
            .Insert(Interaction.None)
            .Insert<UOGump>()
            .Insert<UIMovable>()
            .Insert(new GlobalZIndex(z));
        var rootId = root.Id;

        // Scroll bg pieces (body first so caps paint over the seam).
        var mid = builder.AddGumpTiled(commands, (ushort)(ScrollGraphic + 2), Vector3.UnitZ, new Vector2((width - midW) / 2f, topH), new Vector2(midW, 1));
        commands.AddChild(rootId, mid.Id);
        var top = builder.AddGump(commands, ScrollGraphic, Vector3.UnitZ, new Vector2((width - topW) / 2f, 0));
        commands.AddChild(rootId, top.Id);
        var bottom = builder.AddGump(commands, (ushort)(ScrollGraphic + 3), Vector3.UnitZ, new Vector2((width - botW) / 2f, 0));
        commands.AddChild(rootId, bottom.Id);

        var title = builder.AddGump(commands, TitleGump, Vector3.UnitZ,
            new Vector2((topW - GumpW(assets, TitleGump)) / 2f, Math.Max(0, (topH - GumpH(assets, TitleGump)) / 2f)));
        commands.AddChild(rootId, title.Id);

        int minW = GumpW(assets, MinimizeGump), minH = GumpH(assets, MinimizeGump);
        var minimize = builder.AddGump(commands, MinimizeGump, Vector3.UnitZ, new Vector2((width - minW) / 2f, -minH + 4))
            .Insert(Interaction.None);
        minimize.Observe((On<UiClick> _, Query<Data<SkillsWindow>> wq) =>
        {
            if (!wq.Contains(rootId)) return;
            var (_, w) = wq.Get(rootId);
            w.Ref.Minimized = !w.Ref.Minimized;
        });
        commands.AddChild(rootId, minimize.Id);

        // "Reset groups" — text button on the top cap. Restores default layout.
        // "Reset groups" — a text label (no sprite). Bare Text / None-kind
        // customs don't emit UiClick, so a PreUpdate bbox poll (ResetClick)
        // drives it off ComputedNode instead.
        var reset = builder.AddLabel(commands, "Reset groups", new Vector2(ListLeft + 1, 7))
            .Insert(new TextFont { FontId = 1, Size = 12 })
            .Insert(new TextColor(new ClayColor(40, 40, 180, 255)))
            // None-kind custom makes it a laid-out, ComputedNode-bearing element
            // so ResetClick's bbox poll can read its screen box (a bare Text node
            // isn't laid out for hit-testing).
            .Insert(new UiCustom { Data = new UOCustomRender { Kind = UOCustomKind.None, Hue = Vector3.UnitZ } })
            .Insert(new Node { PositionType = PositionType.Absolute, Left = Val.Px(ListLeft + 1), Top = Val.Px(7), Width = Val.Px(80), Height = Val.Px(16) })
            .Insert(new SkillsResetButton { Window = rootId })
            .Insert<UINoWindowDrag>();
        commands.AddChild(rootId, reset.Id);

        // Content panel — a scrollable flex column (wheel + scrollbar). Rows flow
        // so the scroll range is computed from their stacked height. Interactive
        // controls inside are sprite buttons tagged UINoWindowDrag so their clicks
        // survive both the scroll container and the window-drag gesture.
        int parchRight = (width + midW) / 2;
        int flagW = GumpW(assets, FlagGump);
        int flagX = parchRight - flagW + 8;
        // Leave the flag a clear lane on the right (content stops short of it),
        // mirroring JournalPlugin so the flag isn't painted over by the rows.
        var listWidth = flagX - ListLeft - 6;
        var list = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                PositionType = PositionType.Absolute,
                Left = Val.Px(ListLeft), Top = Val.Px(ListTop),
                Width = Val.Px(listWidth), Height = Val.Px(DefaultHeight - ListTop - 70),
                Overflow = Overflow.Scroll,
            })
            .Insert(new ScrollPosition());
        commands.AddChild(rootId, list.Id);

        // Scroll-position flag — rides the parchment's right edge (overhangs the
        // content, no dedicated column), exactly like JournalPlugin. PositionFlag
        // slides it; FlagDrag scrolls the list when it's dragged.
        var flag = builder.AddGump(commands, FlagGump, Vector3.UnitZ, new Vector2(flagX, ListTop))
            .Insert(Interaction.None)
            .Insert<UINoWindowDrag>();
        commands.AddChild(rootId, flag.Id);

        // Resize expander knob (bottom-center).
        var expander = builder.AddButton(commands, (ExpanderNormal, ExpanderPressed, ExpanderNormal), Vector3.UnitZ, new Vector2(0, 0))
            .Insert(new SkillsResizeHandle { Window = rootId })
            .Insert<UINoWindowDrag>();
        commands.AddChild(rootId, expander.Id);

        // Bottom comment line + baked-text comment decoration + total label.
        var bottomLine = builder.AddGump(commands, 0x082B, Vector3.UnitZ, new Vector2(50, 0));
        commands.AddChild(rootId, bottomLine.Id);
        var bottomComment = builder.AddGump(commands, 0x0836, Vector3.UnitZ, new Vector2(25, 0));
        commands.AddChild(rootId, bottomComment.Id);

        var sumLabel = builder.AddLabel(commands, "0.0", new Vector2(60, 0))
            .Insert(new TextFont { FontId = 3, Size = 12 })
            .Insert(new TextColor(new ClayColor(0, 0, 0, 255)));
        commands.AddChild(rootId, sumLabel.Id);

        // New Group button.
        var newGroup = builder.AddButton(commands, (NewGroupBtn, NewGroupBtn, NewGroupBtn), Vector3.UnitZ, new Vector2(60, 0))
            .Insert<UINoWindowDrag>()
            .Insert<UiContainsByBounds>();
        newGroup.Observe((On<UiClick> _, Res<PlayerSkills> sk, Query<Data<SkillsWindow>> wq) =>
        {
            sk.Value.Groups.Add(new SkillsGroup("New Group"));
            if (wq.Contains(rootId)) { var (_, w) = wq.Get(rootId); w.Ref.Dirty = true; }
        });
        commands.AddChild(rootId, newGroup.Id);

        // ShowReal / ShowCaps checkboxes + labels.
        var realCb = builder.AddCheckbox(commands, false, new Vector2(150, 0), CheckOff, CheckOn, Vector3.UnitZ)
            .Insert(new SkillsCheck { Window = rootId, Cap = false })
            .Insert<UINoWindowDrag>();
        commands.AddChild(rootId, realCb.Id);
        var realLbl = builder.AddLabel(commands, "Show real", new Vector2(170, 0))
            .Insert(new TextFont { FontId = 1, Size = 12 }).Insert<SkillsRealLabel>();
        commands.AddChild(rootId, realLbl.Id);

        var capCb = builder.AddCheckbox(commands, false, new Vector2(150, 0), CheckOff, CheckOn, Vector3.UnitZ)
            .Insert(new SkillsCheck { Window = rootId, Cap = true })
            .Insert<UINoWindowDrag>();
        commands.AddChild(rootId, capCb.Id);
        var capLbl = builder.AddLabel(commands, "Show caps", new Vector2(170, 0))
            .Insert(new TextFont { FontId = 1, Size = 12 }).Insert<SkillsCapLabel>();
        commands.AddChild(rootId, capLbl.Id);

        commands.Entity(rootId).Insert(new SkillsWindow
        {
            SpecialHeight = DefaultHeight,
            Dirty = true,
            Width = width, ListWidth = listWidth,
            ListEntity = list.Id, TopEntity = top.Id, MidEntity = mid.Id, BottomEntity = bottom.Id,
            TitleEntity = title.Id, MinimizeEntity = minimize.Id, ExpanderEntity = expander.Id,
            SumLabelEntity = sumLabel.Id, FlagEntity = flag.Id,
        });

        // Stash the chrome label/checkbox/button entities for LayoutWindow to
        // reposition each frame (their Y depends on SpecialHeight).
        commands.Entity(rootId)
            .Insert(new SkillsChrome
            {
                BottomLine = bottomLine.Id, BottomComment = bottomComment.Id, NewGroup = newGroup.Id,
                RealCheck = realCb.Id, RealLabel = realLbl.Id,
                CapCheck = capCb.Id, CapLabel = capLbl.Id,
            });
    }

    private static void LayoutWindow(
        Res<AssetsServer> assets,
        Query<Data<SkillsWindow, SkillsChrome>> windowsQ,
        Query<Data<Node>> nodeQ)
    {
        foreach (var (rootEnt, win, chrome) in windowsQ)
        {
            int topH = GumpH(assets.Value, ScrollGraphic);
            int botH = GumpH(assets.Value, (ushort)(ScrollGraphic + 3));
            int expW = GumpW(assets.Value, ExpanderNormal);
            int h = win.Ref.Minimized ? topH : win.Ref.SpecialHeight;
            const int CapOverlap = 14;
            int botY = h - botH;
            int midY = topH - CapOverlap;
            int midH = Math.Max(1, botY - midY);

            SetH(nodeQ, rootEnt.Ref, h);
            SetY(nodeQ, win.Ref.MidEntity, midY);
            SetH(nodeQ, win.Ref.MidEntity, midH);
            SetY(nodeQ, win.Ref.BottomEntity, botY);

            var bodyDisplay = win.Ref.Minimized ? Display.None : Display.Flex;
            SetDisplay(nodeQ, win.Ref.MidEntity, bodyDisplay);
            SetDisplay(nodeQ, win.Ref.BottomEntity, bodyDisplay);
            SetDisplay(nodeQ, win.Ref.ListEntity, bodyDisplay);
            SetDisplay(nodeQ, win.Ref.FlagEntity, bodyDisplay);

            // Footer band, offsets from SpecialHeight matching legacy
            // StandardSkillsGump.Update: bottomLine H-98, comment H-85,
            // sum H-83, newGroup H-52, checks around newGroup.
            // Sum label rides just right of the bottom-comment decoration,
            // matching legacy (_skillsLabelSum at comment.X + comment.Width + 5,
            // comment.Y - 5).
            int commentW = GumpW(assets.Value, 0x0836);
            // Center each "Show ..." label vertically on its checkbox sprite.
            int checkH = GumpH(assets.Value, CheckOff);
            const int LabelTextH = 14;
            int labelDY = Math.Max(0, (checkH - LabelTextH) / 2);
            int realCheckY = h - 58, capCheckY = h - 45;
            SetXY(nodeQ, chrome.Ref.BottomLine, 50, h - 98);
            SetXY(nodeQ, chrome.Ref.BottomComment, 25, h - 85);
            SetXY(nodeQ, win.Ref.SumLabelEntity, 25 + commentW + 5, h - 90);
            SetXY(nodeQ, chrome.Ref.NewGroup, 60, h - 52);
            SetXY(nodeQ, chrome.Ref.RealCheck, 150, realCheckY);
            SetXY(nodeQ, chrome.Ref.RealLabel, 170, realCheckY + labelDY);
            SetXY(nodeQ, chrome.Ref.CapCheck, 150, capCheckY);
            SetXY(nodeQ, chrome.Ref.CapLabel, 170, capCheckY + labelDY);
            SetDisplay(nodeQ, chrome.Ref.BottomLine, bodyDisplay);
            SetDisplay(nodeQ, chrome.Ref.BottomComment, bodyDisplay);
            SetDisplay(nodeQ, chrome.Ref.NewGroup, bodyDisplay);
            SetDisplay(nodeQ, chrome.Ref.RealCheck, bodyDisplay);
            SetDisplay(nodeQ, chrome.Ref.RealLabel, bodyDisplay);
            SetDisplay(nodeQ, chrome.Ref.CapCheck, bodyDisplay);
            SetDisplay(nodeQ, chrome.Ref.CapLabel, bodyDisplay);

            // List height runs from ListTop to just above the footer band.
            SetH(nodeQ, win.Ref.ListEntity, Math.Max(1, (h - 100) - ListTop));

            SetXY(nodeQ, win.Ref.ExpanderEntity, (win.Ref.Width - expW) / 2f, h - 2);
            SetDisplay(nodeQ, win.Ref.ExpanderEntity, bodyDisplay);
        }
    }

    // Rebuild group + skill rows when Dirty. Each group: collapse arrow + name +
    // dotted-line fill; expanded groups follow with their skill rows.
    private static void RebuildContent(
        Commands commands,
        Res<PlayerSkills> skills,
        Res<AssetsServer> assets,
        Res<GumpBuilder> builder,
        Query<Data<SkillsWindow>> windowsQ,
        Query<Data<TinyEcs.Children>> childrenQ)
    {
        foreach (var (rootEnt, win) in windowsQ)
        {
            if (!win.Ref.Dirty) continue;
            win.Ref.Dirty = false;

            ulong rootId = rootEnt.Ref;
            var list = win.Ref.ListEntity;
            if (childrenQ.Contains(list))
            {
                var (_, kids) = childrenQ.Get(list);
                foreach (var cid in kids.Ref)
                    commands.Entity(cid).Despawn();
            }

            int listW = (int)win.Ref.ListWidth;
            bool showReal = win.Ref.ShowReal, showCaps = win.Ref.ShowCaps;
            int rows = 0;

            // Rows FLOW in the scroll column; their controls are FLOWING flex
            // children too (NOT PositionType.Absolute). Absolute children become
            // Clay floating elements that escape the scroll's ClipContent and
            // render outside the gump — flowing columns get clipped correctly.
            int arrowW = GumpW(assets.Value, GroupCollapsed);
            int useW = GumpW(assets.Value, UseBtnN);
            int lockW = GumpW(assets.Value, LockUp);
            int dotH = GumpH(assets.Value, DottedLine);
            const int ValW = 48;
            int nameColW = Math.Max(20, listW - useW - ValW - lockW - 10);

            for (int gi = 0; gi < skills.Value.Groups.Count; gi++)
            {
                var group = skills.Value.Groups[gi];
                int captured = gi;

                var header = commands.Spawn()
                    .Insert(new Node { Display = Display.Flex, FlexDirection = FlexDirection.Row, AlignItems = AlignItems.Center, Gap = Val.Px(2), Width = Val.Px(listW), Height = Val.Px(RowH) })
                    .Insert(new SkillGroupToggle { GroupIndex = gi });
                commands.AddChild(list, header.Id);
                rows++;

                // Collapse arrow — a real-sprite button (None-kind hit areas don't
                // receive UiClick; a sprite button does). Click toggles the group.
                ushort arrowId = group.IsMaximized ? GroupExpanded : GroupCollapsed;
                var arrow = builder.Value.AddButton(commands, (arrowId, arrowId, arrowId), Vector3.UnitZ)
                    .Insert<UINoWindowDrag>()
                    // Whole-bbox hit (legacy ContainsByBounds) — the arrow sprite
                    // has transparent corners; a click anywhere on it must toggle.
                    .Insert<UiContainsByBounds>();
                arrow.Observe((On<UiClick> _, Res<PlayerSkills> sk, Query<Data<SkillsWindow>> wq) =>
                {
                    if (captured < sk.Value.Groups.Count)
                        sk.Value.Groups[captured].IsMaximized = !sk.Value.Groups[captured].IsMaximized;
                    if (wq.Contains(rootId)) { var (_, w) = wq.Get(rootId); w.Ref.Dirty = true; }
                });
                commands.AddChild(header.Id, arrow.Id);

                int nameW = UoFontRuntime.Fonts != null ? UoFontRuntime.Fonts.GetWidthASCII(6, group.Name) : group.Name.Length * 7;
                // Editable group name: None-kind custom + UiContainsByBounds make
                // the text bbox clickable; a click focuses it (NameEditType drains
                // typing into group.Name). Extra width gives room to type.
                var name = commands.Spawn()
                    .Insert(new Node { Width = Val.Px(nameW + 24), Height = Val.Auto })
                    .Insert(new Text(group.Name))
                    .Insert(new TextFont { FontId = (ushort)(6 | UoFontRuntime.AsciiFlag), Size = 12 })
                    .Insert(new TextColor(UoFontRuntime.AsciiHue(SkillHue)))
                    .Insert(new UiCustom { Data = new UOCustomRender { Kind = UOCustomKind.None, Hue = Vector3.UnitZ } })
                    .Insert(Interaction.None)
                    .Insert<UiContainsByBounds>()
                    .Insert<UINoWindowDrag>()
                    .Insert(new SkillNameEdit { Window = rootId, GroupIndex = gi });
                ulong nameId = name.Id;
                name.Observe((On<UiClick> _, ResMut<FocusedInput> focused) => focused.Value.Entity = nameId);
                commands.AddChild(header.Id, name.Id);

                // Dotted-line fill (0x0835) flows after the name to the row's right
                // edge. Flowing (no PositionType.Absolute) so the scroll clips it.
                int dotsW = listW - arrowW - (nameW + 4) - 12;
                if (dotsW > 4)
                {
                    var dots = commands.Spawn()
                        .Insert(new Node { Width = Val.Px(dotsW), Height = Val.Px(dotH) })
                        .Insert(new UiCustom { Data = new UOCustomRender { Kind = UOCustomKind.GumpTiled, AssetId = DottedLine, Hue = Vector3.UnitZ } });
                    commands.AddChild(header.Id, dots.Id);
                }

                if (!group.IsMaximized) continue;

                // Skill rows (flow under the header). Columns: gutter (use button
                // or spacer) | name | value | lock — all flowing for clipping.
                foreach (var sid in group.Skills)
                {
                    if (sid >= assets.Value.Skills.SkillsCount) continue;
                    var entry = assets.Value.Skills.Skills[sid];
                    ref var val = ref skills.Value.Values[sid];

                    var srow = commands.Spawn()
                        .Insert(new Node { Display = Display.Flex, FlexDirection = FlexDirection.Row, AlignItems = AlignItems.Center, Gap = Val.Px(2), Width = Val.Px(listW), Height = Val.Px(RowH) });
                    commands.AddChild(list, srow.Id);
                    rows++;

                    if (entry.HasAction)
                    {
                        int useId = sid;
                        var use = builder.Value.AddButton(commands, (UseBtnN, UseBtnO, UseBtnO), Vector3.UnitZ)
                            .Insert<UINoWindowDrag>();
                        use.Observe((On<UiClick> _, Res<NetClient> net) => net.Value.Send_UseSkill(useId));
                        commands.AddChild(srow.Id, use.Id);
                    }
                    else
                    {
                        // Empty gutter keeps names aligned with buttoned rows.
                        var spacer = commands.Spawn().Insert(new Node { Width = Val.Px(useW), Height = Val.Px(RowH) });
                        commands.AddChild(srow.Id, spacer.Id);
                    }

                    var nameLbl = commands.Spawn()
                        .Insert(new Node { Width = Val.Px(nameColW), Height = Val.Auto })
                        .Insert(new Text(entry.Name))
                        .Insert(new TextFont { FontId = (ushort)(9 | UoFontRuntime.AsciiFlag), Size = 12 })
                        .Insert(new TextColor(UoFontRuntime.AsciiHue(SkillHue)))
                        // None-kind custom so the name gets a ComputedNode (a bare
                        // Text node isn't laid out for hit-testing / bbox queries).
                        .Insert(new UiCustom { Data = new UOCustomRender { Kind = UOCustomKind.None, Hue = Vector3.UnitZ } })
                        // Drag handle: press the skill name and release over another
                        // group to move the skill there (SkillDrag).
                        .Insert<UINoWindowDrag>()
                        .Insert(new SkillRowDrag { Window = rootId, GroupIndex = gi, SkillId = sid });
                    commands.AddChild(srow.Id, nameLbl.Id);

                    float display = (showReal ? val.Base : showCaps ? val.Cap : val.Real) / 10f;
                    var valLbl = commands.Spawn()
                        .Insert(new Node { Width = Val.Px(ValW), Height = Val.Auto })
                        .Insert(new Text($"{display:F1}"))
                        .Insert(new TextFont { FontId = (ushort)(9 | UoFontRuntime.AsciiFlag), Size = 12 })
                        .Insert(new TextColor(UoFontRuntime.AsciiHue(SkillHue)));
                    commands.AddChild(srow.Id, valLbl.Id);

                    ushort lockId = val.Lock switch { Lock.Down => LockDown, Lock.Locked => LockLocked, _ => LockUp };
                    int lsid = sid;
                    var lockBtn = builder.Value.AddButton(commands, (lockId, lockId, lockId), Vector3.UnitZ)
                        .Insert<UINoWindowDrag>();
                    lockBtn.Observe((On<UiClick> _, Res<NetClient> net, Res<PlayerSkills> sk, Query<Data<SkillsWindow>> wq) =>
                    {
                        ref var sv = ref sk.Value.Values[lsid];
                        byte ns = (byte)sv.Lock;
                        ns = ns < 2 ? (byte)(ns + 1) : (byte)0;
                        sv.Lock = (Lock)ns;
                        net.Value.Send_SkillStatusChangeRequest((ushort)lsid, ns);
                        if (wq.Contains(rootId)) { var (_, w) = wq.Get(rootId); w.Ref.Dirty = true; }
                    });
                    commands.AddChild(srow.Id, lockBtn.Id);
                }
            }

            win.Ref.ContentHeight = rows * RowH;
        }
    }

    // Drain typing into the focused group-name field. Enter/escape commits
    // (clears focus + rebuilds to resize the dotted line); backspace deletes.
    // ChatPlugin yields its CharInputEvent reader while a name field is focused.
    private static void NameEditType(
        EventReader<CharInputEvent> reader,
        Res<KeyboardContext> keyboard,
        ResMut<FocusedInput> focused,
        Res<PlayerSkills> skills,
        Query<Data<Text, SkillNameEdit>> editsQ,
        Query<Data<SkillsWindow>> windowsQ)
    {
        var ent = focused.Value.Entity;
        if (ent == 0 || !editsQ.Contains(ent)) return;
        var (_, text, edit) = editsQ.Get(ent);
        int gi = edit.Ref.GroupIndex;
        var groups = skills.Value.Groups;
        if (gi >= groups.Count) return;
        var group = groups[gi];

        // Delete key removes the focused group (its skills fold into the first
        // group); the first group can't be deleted. Mirrors legacy
        // SkillsGroupControl.OnKeyUp / SkillsGroupManager.Remove.
        if (keyboard.Value.IsPressedOnce(Keys.Delete) && gi > 0)
        {
            foreach (var sid in group.Skills) groups[0].Add(sid);
            groups.RemoveAt(gi);
            focused.Value.Entity = 0;
            if (windowsQ.Contains(edit.Ref.Window)) { var (_, w) = windowsQ.Get(edit.Ref.Window); w.Ref.Dirty = true; }
            return;
        }

        bool commit = false;
        foreach (var ev in reader.Read())
        {
            char c = ev.Value;
            if (c == '\r' || c == '\n') { commit = true; continue; }
            if (c == '\t') continue;
            if (c == '\b') { if (group.Name.Length > 0) group.Name = group.Name[..^1]; continue; }
            group.Name += c;
        }
        text.Ref.Value = group.Name;

        if (commit)
        {
            focused.Value.Entity = 0;
            if (windowsQ.Contains(edit.Ref.Window)) { var (_, w) = windowsQ.Get(edit.Ref.Window); w.Ref.Dirty = true; }
        }
    }

    // Give the focused group-name field a dirty-white background so it reads as
    // "editing"; remove it when focus leaves. Tracks the last focused entity so
    // the structural insert/remove only fires on change.
    private static void EditHighlight(
        Commands commands,
        Res<FocusedInput> focused,
        Local<ulong> last,
        Query<Data<SkillNameEdit>> editsQ)
    {
        var cur = focused.Value.Entity;
        if (cur == last.Value) return;
        if (last.Value != 0 && editsQ.Contains(last.Value))
            commands.Entity(last.Value).Remove<BackgroundColor>();
        if (cur != 0 && editsQ.Contains(cur))
            commands.Entity(cur).Insert(new BackgroundColor(s_editHighlight));
        last.Value = cur;
    }

    private struct SkillDragAnchor { public bool Active, Claimed; public ulong Window; public int SrcGroup, SkillId; public ulong Preview; public int LastTarget; }

    // Dirty-white drop-target highlight + the group-name edit background.
    private static readonly ClayColor s_dropHighlight = new(235, 230, 215, 90);
    private static readonly ClayColor s_editHighlight = new(235, 230, 215, 120);

    private static int DropTarget(Vector2 pos, Query<Data<ComputedNode, SkillNameEdit>> groupNameQ, Query<Data<ComputedNode, SkillRowDrag>> dragQ)
    {
        foreach (var (_, bb, g) in groupNameQ)
            if (Contains(bb.Ref, pos)) return g.Ref.GroupIndex;
        foreach (var (_, bb, d) in dragQ)
            if (Contains(bb.Ref, pos)) return d.Ref.GroupIndex;
        return -1;
    }

    // Drag a skill row onto another group to move it. Latch on press over a skill
    // name; a cursor preview (the skill name) follows the mouse and the group
    // under it is highlighted; on release the target group receives the skill
    // (mirrors legacy SkillsGroupControl.OnMouseOver regrouping).
    private static void SkillDrag(
        Res<MouseContext> mouse,
        Res<DragGate> gate,
        Res<AssetsServer> assets,
        Commands commands,
        Local<SkillDragAnchor> anchor,
        Res<PlayerSkills> skills,
        Query<Data<ComputedNode, SkillRowDrag>> dragQ,
        Query<Data<ComputedNode, SkillNameEdit>> groupNameQ,
        Query<Data<SkillGroupToggle>> headerEntsQ,
        Query<Data<Node>> nodeQ,
        Query<Data<SkillsWindow>> windowsQ)
    {
        bool held = mouse.Value.IsPressed(MouseButtonType.Left) || mouse.Value.IsPressedOnce(MouseButtonType.Left);

        if (!held)
        {
            if (anchor.Value.Active)
            {
                int tgt = DropTarget(mouse.Value.Position, groupNameQ, dragQ);
                int src = anchor.Value.SrcGroup;
                var groups = skills.Value.Groups;
                if (tgt >= 0 && tgt != src && src < groups.Count && tgt < groups.Count)
                {
                    groups[src].Skills.Remove((byte)anchor.Value.SkillId);
                    groups[tgt].Add((byte)anchor.Value.SkillId);
                    if (windowsQ.Contains(anchor.Value.Window))
                    {
                        var (_, w) = windowsQ.Get(anchor.Value.Window);
                        w.Ref.Dirty = true;
                    }
                }

                // Tear down preview + clear all drop highlights.
                if (anchor.Value.Preview != 0) commands.Entity(anchor.Value.Preview).Despawn();
                foreach (var (ent, _) in headerEntsQ) commands.Entity(ent.Ref).Remove<BackgroundColor>();
            }
            if (anchor.Value.Claimed && gate.Value.Mode == ActiveDrag.UIWindow)
                gate.Value.Mode = ActiveDrag.None;
            anchor.Value = default;
            return;
        }

        if (mouse.Value.IsPressedOnce(MouseButtonType.Left) && !anchor.Value.Active)
        {
            if (gate.Value.Mode != ActiveDrag.None) return;
            var pos = mouse.Value.Position;
            foreach (var (_, bb, d) in dragQ)
            {
                if (!Contains(bb.Ref, pos)) continue;
                anchor.Value.Active = true;
                anchor.Value.Claimed = true;
                anchor.Value.Window = d.Ref.Window;
                anchor.Value.SrcGroup = d.Ref.GroupIndex;
                anchor.Value.SkillId = d.Ref.SkillId;
                anchor.Value.LastTarget = -2;
                gate.Value.Mode = ActiveDrag.UIWindow;

                // Spawn the cursor preview (the dragged skill's name).
                string nm = d.Ref.SkillId < assets.Value.Skills.SkillsCount ? assets.Value.Skills.Skills[d.Ref.SkillId].Name : "?";
                var preview = commands.Spawn()
                    .Insert(new Node { PositionType = PositionType.Absolute, Left = Val.Px(pos.X + 12), Top = Val.Px(pos.Y + 2), Width = Val.Auto, Height = Val.Auto, Padding = UiRect.All(2) })
                    .Insert(new Text(nm))
                    .Insert(new TextFont { FontId = (ushort)(9 | UoFontRuntime.AsciiFlag), Size = 12 })
                    .Insert(new TextColor(UoFontRuntime.AsciiHue(SkillHue)))
                    .Insert(new BackgroundColor(s_editHighlight))
                    .Insert(new GlobalZIndex(30000));
                anchor.Value.Preview = preview.Id;
                break;
            }
        }

        if (!anchor.Value.Active) return;

        // Preview follows the cursor.
        if (anchor.Value.Preview != 0 && nodeQ.Contains(anchor.Value.Preview))
        {
            var (_, pn) = nodeQ.Get(anchor.Value.Preview);
            pn.Ref.Left = Val.Px(mouse.Value.Position.X + 12);
            pn.Ref.Top = Val.Px(mouse.Value.Position.Y + 2);
        }

        // Highlight the group under the cursor (only when it changes, to avoid
        // per-frame structural churn). Source group doesn't highlight.
        int target = DropTarget(mouse.Value.Position, groupNameQ, dragQ);
        if (target == anchor.Value.SrcGroup) target = -1;
        if (target != anchor.Value.LastTarget)
        {
            anchor.Value.LastTarget = target;
            foreach (var (ent, h) in headerEntsQ)
            {
                if (h.Ref.GroupIndex == target)
                    commands.Entity(ent.Ref).Insert(new BackgroundColor(s_dropHighlight));
                else
                    commands.Entity(ent.Ref).Remove<BackgroundColor>();
            }
        }
    }

    private static void RefreshSum(
        Res<PlayerSkills> skills,
        Query<Data<SkillsWindow>> windowsQ,
        Query<Data<Text>> textQ)
    {
        foreach (var (_, win) in windowsQ)
        {
            if (!textQ.Contains(win.Ref.SumLabelEntity)) continue;
            var (_, t) = textQ.Get(win.Ref.SumLabelEntity);
            t.Ref.Value = $"{skills.Value.SumValues(win.Ref.ShowReal):F1}";
        }
    }

    // Drag the resize knob to grow/shrink SpecialHeight. Bbox latch on press-once
    // + DragGate claim (mirrors JournalPlugin.ResizeDrag) so window-move doesn't
    // also fire.
    private static void ResizeDrag(
        Res<MouseContext> mouse,
        Res<DragGate> gate,
        Local<ResizeAnchor> anchor,
        Query<Data<ComputedNode, SkillsResizeHandle>> handleQ,
        Query<Data<SkillsWindow>> windowsQ)
    {
        bool held = mouse.Value.IsPressed(MouseButtonType.Left) || mouse.Value.IsPressedOnce(MouseButtonType.Left);
        if (!held)
        {
            if (anchor.Value.Claimed && gate.Value.Mode == ActiveDrag.UIWindow)
                gate.Value.Mode = ActiveDrag.None;
            anchor.Value = default;
            return;
        }

        if (mouse.Value.IsPressedOnce(MouseButtonType.Left))
        {
            if (gate.Value.Mode != ActiveDrag.None) return;
            var pos = mouse.Value.Position;
            foreach (var (_, bb, handle) in handleQ)
            {
                if (pos.X < bb.Ref.Position.X || pos.Y < bb.Ref.Position.Y) continue;
                if (pos.X >= bb.Ref.Position.X + bb.Ref.Size.X || pos.Y >= bb.Ref.Position.Y + bb.Ref.Size.Y) continue;
                if (!windowsQ.Contains(handle.Ref.Window)) continue;
                var (_, w) = windowsQ.Get(handle.Ref.Window);
                gate.Value.Mode = ActiveDrag.UIWindow;
                anchor.Value.Claimed = true;
                anchor.Value.Active = true;
                anchor.Value.Window = handle.Ref.Window;
                anchor.Value.StartY = pos.Y;
                anchor.Value.StartH = w.Ref.SpecialHeight;
                break;
            }
        }

        if (!anchor.Value.Active || !windowsQ.Contains(anchor.Value.Window)) return;
        var (_, win) = windowsQ.Get(anchor.Value.Window);
        int nh = (int)(anchor.Value.StartH + (mouse.Value.Position.Y - anchor.Value.StartY));
        win.Ref.SpecialHeight = Math.Clamp(nh, MinHeight, MaxHeight);
    }

    private struct ResizeAnchor { public bool Active, Claimed; public ulong Window; public float StartY; public int StartH; }

    // "Reset groups" click via bbox poll (the label emits no UiClick). Press over
    // the label claims the gate; release still over it rebuilds the default
    // groups. Mirrors the resize/flag press-latch pattern.
    private static void ResetClick(
        Res<MouseContext> mouse,
        Res<DragGate> gate,
        Res<AssetsServer> assets,
        Local<ResetState> st,
        Query<Data<ComputedNode, SkillsResetButton>> resetQ,
        Res<PlayerSkills> skills,
        Query<Data<SkillsWindow>> windowsQ)
    {
        if (mouse.Value.IsPressedOnce(MouseButtonType.Left))
        {
            if (gate.Value.Mode != ActiveDrag.None) return;
            var pos = mouse.Value.Position;
            foreach (var (_, bb, r) in resetQ)
            {
                if (!Contains(bb.Ref, pos)) continue;
                st.Value.Armed = true;
                st.Value.Window = r.Ref.Window;
                gate.Value.Mode = ActiveDrag.UIWindow;
                break;
            }
            return;
        }

        if (mouse.Value.IsReleased(MouseButtonType.Left) && st.Value.Armed)
        {
            if (gate.Value.Mode == ActiveDrag.UIWindow) gate.Value.Mode = ActiveDrag.None;
            var win = st.Value.Window;
            st.Value = default;
            var pos = mouse.Value.Position;
            foreach (var (_, bb, r) in resetQ)
            {
                if (r.Ref.Window != win || !Contains(bb.Ref, pos)) continue;
                SkillsGroupDefaults.Build(skills.Value.Groups, assets.Value.Skills.SkillsCount);
                if (windowsQ.Contains(win)) { var (_, w) = windowsQ.Get(win); w.Ref.Dirty = true; }
                break;
            }
        }
    }

    private struct ResetState { public bool Armed; public ulong Window; }

    private static bool Contains(in ComputedNode bb, Vector2 p)
        => p.X >= bb.Position.X && p.Y >= bb.Position.Y
        && p.X < bb.Position.X + bb.Size.X && p.Y < bb.Position.Y + bb.Size.Y;

    // Slide the flag along the list track to reflect scroll position (mirrors
    // JournalPlugin.PositionFlag).
    private static void PositionFlag(
        Query<Data<SkillsWindow>> windowsQ,
        Query<Data<Node>> nodeQ,
        Query<Data<ScrollPosition>> scrollQ)
    {
        foreach (var (_, win) in windowsQ)
        {
            if (win.Ref.Minimized) continue;
            if (!nodeQ.Contains(win.Ref.FlagEntity) || !nodeQ.Contains(win.Ref.ListEntity)) continue;
            var (_, listNode) = nodeQ.Get(win.Ref.ListEntity);
            float listH = listNode.Ref.Height.Value;
            float maxScroll = Math.Max(0f, win.Ref.ContentHeight - listH);
            float offset = 0f;
            if (scrollQ.Contains(win.Ref.ListEntity))
            {
                var (_, sp) = scrollQ.Get(win.Ref.ListEntity);
                offset = Math.Abs(sp.Ref.OffsetY);
            }
            float ratio = maxScroll > 0f ? Math.Clamp(offset / maxScroll, 0f, 1f) : 0f;
            var (_, flag) = nodeQ.Get(win.Ref.FlagEntity);
            flag.Ref.Top = Val.Px(ListTop + ratio * Math.Max(0f, listH - 16f));
            flag.Ref.Display = win.Ref.Minimized ? Display.None : Display.Flex;
        }
    }

    private struct FlagAnchor { public bool Active, Claimed; public ulong List; public float StartY, StartOffset, TrackPixels, MaxScroll; }

    // Drag the flag to scroll the list (mirrors JournalPlugin.FlagDrag). PreUpdate
    // + DragGate claim so window-move doesn't also fire.
    private static void FlagDrag(
        Res<MouseContext> mouse,
        Res<DragGate> gate,
        Res<AssetsServer> assets,
        Local<FlagAnchor> anchor,
        Query<Data<SkillsWindow>> windowsQ,
        Query<Data<ComputedNode>> compQ,
        Query<Data<Node>> nodeQ,
        Query<Data<ScrollPosition>> scrollQ)
    {
        bool held = mouse.Value.IsPressed(MouseButtonType.Left) || mouse.Value.IsPressedOnce(MouseButtonType.Left);
        if (!held)
        {
            if (anchor.Value.Claimed && gate.Value.Mode == ActiveDrag.UIWindow)
                gate.Value.Mode = ActiveDrag.None;
            anchor.Value = default;
            return;
        }

        if (mouse.Value.IsPressedOnce(MouseButtonType.Left))
        {
            if (gate.Value.Mode != ActiveDrag.None) return;
            var pos = mouse.Value.Position;
            int flagH = GumpH(assets.Value, FlagGump);
            foreach (var (_, win) in windowsQ)
            {
                if (win.Ref.Minimized || !compQ.Contains(win.Ref.FlagEntity)) continue;
                var (_, bb) = compQ.Get(win.Ref.FlagEntity);
                if (!Contains(bb.Ref, pos)) continue;

                float listH = 0f;
                if (nodeQ.Contains(win.Ref.ListEntity)) { var (_, ln) = nodeQ.Get(win.Ref.ListEntity); listH = ln.Ref.Height.Value; }
                float maxScroll = Math.Max(0f, win.Ref.ContentHeight - listH);
                if (maxScroll <= 0f) return;
                float startOffset = 0f;
                if (scrollQ.Contains(win.Ref.ListEntity)) { var (_, sp) = scrollQ.Get(win.Ref.ListEntity); startOffset = sp.Ref.OffsetY; }

                gate.Value.Mode = ActiveDrag.UIWindow;
                anchor.Value.Claimed = true;
                anchor.Value.Active = true;
                anchor.Value.List = win.Ref.ListEntity;
                anchor.Value.StartY = pos.Y;
                anchor.Value.StartOffset = startOffset;
                anchor.Value.MaxScroll = maxScroll;
                anchor.Value.TrackPixels = Math.Max(1f, listH - flagH);
                break;
            }
        }

        if (!anchor.Value.Active || !scrollQ.Contains(anchor.Value.List)) return;
        float delta = mouse.Value.Position.Y - anchor.Value.StartY;
        float newOffset = Math.Clamp(
            anchor.Value.StartOffset + delta / anchor.Value.TrackPixels * anchor.Value.MaxScroll,
            0f, anchor.Value.MaxScroll);
        var (_, scroll) = scrollQ.Get(anchor.Value.List);
        scroll.Ref.OffsetY = newOffset;
    }

    private static void SetH(Query<Data<Node>> q, ulong e, float h) { if (q.Contains(e)) { var (_, n) = q.Get(e); n.Ref.Height = Val.Px(h); } }
    private static void SetY(Query<Data<Node>> q, ulong e, float y) { if (q.Contains(e)) { var (_, n) = q.Get(e); n.Ref.Top = Val.Px(y); } }
    private static void SetXY(Query<Data<Node>> q, ulong e, float x, float y) { if (q.Contains(e)) { var (_, n) = q.Get(e); n.Ref.Left = Val.Px(x); n.Ref.Top = Val.Px(y); } }
    private static void SetDisplay(Query<Data<Node>> q, ulong e, Display d) { if (q.Contains(e)) { var (_, n) = q.Get(e); n.Ref.Display = d; } }

    private static void Despawn(
        Commands commands,
        Query<Data<SkillsWindow>> windowsQ,
        Query<Data<TinyEcs.Children>> childrenQ)
    {
        foreach (var (ent, _) in windowsQ)
        {
            ulong root = ent.Ref;
            if (childrenQ.Contains(root))
            {
                var (_, kids) = childrenQ.Get(root);
                foreach (var cid in kids.Ref)
                    DespawnSubtree(commands, cid, childrenQ);
            }
            commands.Entity(root).Despawn();
        }
    }

    private static void DespawnSubtree(Commands commands, ulong e, Query<Data<TinyEcs.Children>> childrenQ)
    {
        if (childrenQ.Contains(e))
        {
            var (_, kids) = childrenQ.Get(e);
            foreach (var cid in kids.Ref)
                DespawnSubtree(commands, cid, childrenQ);
        }
        commands.Entity(e).Despawn();
    }
}

internal struct SkillsChrome
{
    public ulong BottomLine, BottomComment, NewGroup, RealCheck, RealLabel, CapCheck, CapLabel;
}
internal struct SkillsCheck { public ulong Window; public bool Cap; }
internal struct SkillsResetButton { public ulong Window; }
internal struct SkillNameEdit { public ulong Window; public int GroupIndex; }
internal struct SkillRowDrag { public ulong Window; public int GroupIndex; public int SkillId; }
internal struct SkillsRealLabel;
internal struct SkillsCapLabel;
