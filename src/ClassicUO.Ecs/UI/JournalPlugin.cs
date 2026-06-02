// Journal gump — ECS port of Game/UI/Gumps/JournalGump.cs, matched to the
// legacy client (the source of truth). Verified against harness screenshots of
// `cuo` running ModernUO.
//
// Layout mirrors JournalGump + ExpandableScroll:
//   * Scroll-shaped window: top cap (0x1F40) + vertically tiled body (0x1F42) +
//     bottom cap (0x1F43), title art (0x82A) centered on the cap, a minimize
//     gumppic (0x82D / 0x830) at the cap, a dark-mode checkbox top-right and
//     four filter checkboxes (System / Objects / Client / Guild) at the bottom.
//   * Entry list: a left time column ("HH:mm", muted) + the hued, wrapped
//     message text ("{Name}: {Text}"), newest at the bottom; wheel-scrolls.
//   * Dark mode tints the scroll bg with hue 903; minimize collapses to the
//     title gumppic; filters hide entries by their TextType.
//
// Capture: the TextOverheadEvent stream (independent EventReader cursor). The
// accepted set is broadened past the overhead-only types to include System /
// Guild / Alliance / Party so server broadcasts ("world is saving", "Help
// request aborted") land in the journal like legacy. TextType is classified
// from MessageType + serial since ECS doesn't carry legacy's TextType.
//
// Known gaps vs legacy: cliloc (0xC1) messages aren't surfaced as ECS events
// yet (the packet is stubbed) so cliloc-delivered lines don't appear; the flag
// scrollbar is a position indicator (wheel scrolls, flag-drag deferred — a
// draggable thumb competes with WindowDragPlugin for the press); resize-by-
// expander is wired but the timestamp uses the wall clock (see TimeProvider).

using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Assets;
using ClassicUO.Game.Data;
using ClassicUO.Input;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using TinyEcs.Bevy.UI.Widgets;
using ClayColor = Clay.Color;

namespace ClassicUO.Ecs;

// Legacy TextType (Game/Data/TextType.cs) — drives the filter checkboxes.
internal enum JournalTextType : byte { Client, System, Object, GuildAlly }

internal struct JournalEntry
{
    public string Text;
    public string Name;
    public ushort Hue;
    public JournalTextType TextType;
    public string TimeLabel; // "HH:mm" captured at append (legacy uses wall clock)
}

// Session-wide store. Accumulates regardless of whether the window is open
// (legacy JournalManager). Revision bumps on append so the open window rebuilds.
internal sealed class JournalStore
{
    public const int MaxEntries = 200;

    private readonly List<JournalEntry> _entries = new();
    public int Revision { get; private set; }
    public IReadOnlyList<JournalEntry> Entries => _entries;

    public void Append(in JournalEntry entry)
    {
        if (_entries.Count >= MaxEntries)
            _entries.RemoveAt(0);
        _entries.Add(entry);
        Revision++;
    }
}

// Marker + full UI state on the window root. Geometry (LayoutWindow) and content
// (RebuildContent) are derived from these fields; observers flip them.
internal struct JournalWindow
{
    public int SpecialHeight;   // scroll body height (legacy 274..800, default 300)
    public bool Minimized;
    public bool DarkMode;
    public bool ShowSystem, ShowObjects, ShowClient, ShowGuild;

    public int BuiltRevision;
    public int StyleEpoch;      // bumped on FILTER change only (forces row rebuild)
    public int BuiltStyleEpoch;

    public ulong ListEntity;    // scroll container (flex column of rows)
    public ulong FlagEntity;    // scroll-position flag sprite
    public ulong TopEntity, MidEntity, BottomEntity, TitleEntity, MinimizeEntity;
    public ulong DarkCheckEntity, ExpanderEntity;
    public float Width;
    public float ListWidth;
    public float ContentHeight; // total row height (for the flag ratio)
}

internal struct JournalRow { public JournalTextType Type; } // a list row, filtered by Type
internal struct JournalListTag;
internal struct JournalFlagTag;
internal struct JournalExpander { public ulong Window; } // drag-to-resize handle
// Filter-checkbox markers (one per filter) so the CheckboxChanged observer can
// route the new value into the right JournalWindow flag.
internal struct JournalFilterCheck { public JournalTextType Filter; public ulong Window; }
internal struct JournalDarkCheck { public ulong Window; }

internal readonly struct JournalPlugin : IPlugin
{
    private const ushort ScrollGraphic = 0x1F40; // +0 top, +2 body(tiled), +3 bottom
    private const ushort TitleGump = 0x82A;
    private const ushort MinimizeGump = 0x82D;
    private const ushort MinimizedGump = 0x0830;
    private const ushort FlagGump = 0x0828;
    private const ushort ExpanderNormal = 0x082E;
    private const ushort ExpanderPressed = 0x082F;
    private const ushort DarkModeHue = 903;

    private const int DiffY = 22;       // legacy DIFF_Y
    private const int ListTop = 40;     // just below the top cap (~37px) — minimal gap
    private const int ListLeft = 25;
    private const int DefaultHeight = 300;
    private const int MinHeight = 274;  // legacy c_ExpandableScrollHeight_Min
    private const int MaxHeight = 800;  // legacy c_ExpandableScrollHeight_Max

    public void Build(App app)
    {
        app.AddResource(new JournalStore());
        app.AddResource(new JournalTimeProvider());

        var captureFn = Capture;
        var captureSystemFn = CaptureSystem;
        var rebuildFn = RebuildContent;
        var layoutFn = LayoutWindow;
        var flagFn = PositionFlag;
        var despawnFn = Despawn;

        app.AddSystem(captureFn).InStage(Stage.Update)
            .RunIf((EventReader<TextOverheadEvent> texts) => texts.HasEvents).Build();
        app.AddSystem(captureSystemFn).InStage(Stage.Update)
            .RunIf((EventReader<SystemMessageEvent> msgs) => msgs.HasEvents).Build();
        app.AddSystem(rebuildFn).InStage(Stage.Update).Build();
        app.AddSystem(layoutFn).InStage(Stage.Update).Build();
        app.AddSystem(flagFn).InStage(Stage.PostUpdate).Build();
        app.AddSystem(despawnFn).OnExit(GameState.GameScreen).Build();

        // Drag-to-resize via the expander handle. PreUpdate (before window drag)
        // so a press on the handle claims the shared DragGate first.
        var resizeFn = ResizeDrag;
        app.AddSystem(resizeFn).InStage(Stage.PreUpdate).Build();

        // Drag the scroll flag to scroll. PreUpdate, claims the DragGate too.
        var flagDragFn = FlagDrag;
        app.AddSystem(flagDragFn).InStage(Stage.PreUpdate).Build();

        // Filter checkboxes -> window flags.
        app.AddObserver<On<CheckboxChanged>, Query<Data<JournalFilterCheck>>, Query<Data<JournalWindow>>>(
            (trig, checks, windows) =>
            {
                if (!checks.Contains(trig.EntityId)) return;
                var (_, fc) = checks.Get(trig.EntityId);
                if (!windows.Contains(fc.Ref.Window)) return;
                var (_, win) = windows.Get(fc.Ref.Window);
                switch (fc.Ref.Filter)
                {
                    case JournalTextType.System: win.Ref.ShowSystem = trig.Event.Checked; break;
                    case JournalTextType.Object: win.Ref.ShowObjects = trig.Event.Checked; break;
                    case JournalTextType.Client: win.Ref.ShowClient = trig.Event.Checked; break;
                    case JournalTextType.GuildAlly: win.Ref.ShowGuild = trig.Event.Checked; break;
                }
                win.Ref.StyleEpoch++;
            });

        // Dark-mode checkbox -> window flag.
        app.AddObserver<On<CheckboxChanged>, Query<Data<JournalDarkCheck>>, Query<Data<JournalWindow>>>(
            (trig, checks, windows) =>
            {
                if (!checks.Contains(trig.EntityId)) return;
                var (_, dc) = checks.Get(trig.EntityId);
                if (!windows.Contains(dc.Ref.Window)) return;
                var (_, win) = windows.Get(dc.Ref.Window);
                win.Ref.DarkMode = trig.Event.Checked; // LayoutWindow re-hues each frame; no rebuild
            });
    }

    // Broadened vs the overhead reader: keep System/Guild/Alliance/Party so
    // server broadcasts reach the journal. Classify TextType from type + serial.
    private static void Capture(
        Res<JournalTimeProvider> clock,
        EventReader<TextOverheadEvent> texts,
        ResMut<JournalStore> store)
    {
        foreach (var t in texts.Read())
        {
            JournalTextType type;
            switch (t.MessageType)
            {
                case MessageType.Guild:
                case MessageType.Alliance:
                    type = JournalTextType.GuildAlly; break;
                case MessageType.System:
                    type = JournalTextType.System; break;
                case MessageType.Regular:
                case MessageType.Emote:
                case MessageType.Label:
                case MessageType.Focus:
                case MessageType.Whisper:
                case MessageType.Yell:
                case MessageType.Spell:
                case MessageType.Party:
                case MessageType.Limit3Spell:
                    // System broadcasts arrive as Regular with serial 0/0xFFFFFFFF
                    // and name "System"; an entity serial means an object spoke.
                    type = (t.Serial == 0 || t.Serial == 0xFFFFFFFF)
                        ? JournalTextType.System : JournalTextType.Object;
                    break;
                default:
                    continue; // Command/Encoded etc. — not journalled
            }

            store.Value.Append(new JournalEntry
            {
                Text = t.Text,
                Name = t.Name,
                Hue = t.Hue,
                TextType = type,
                TimeLabel = clock.Value.NowLabel(),
            });
        }
    }

    // Server broadcasts (serial 0xFFFFFFFF / cliloc system notices) are routed
    // to SystemMessageEvent for the bottom-left log, not TextOverheadEvent — so
    // the Capture path above never sees them. Mirror them into the journal as
    // System lines (legacy's journal shows server system text under its System
    // filter). Independent EventReader cursor; the system log keeps its own.
    private static void CaptureSystem(
        Res<JournalTimeProvider> clock,
        EventReader<SystemMessageEvent> msgs,
        ResMut<JournalStore> store)
    {
        foreach (var m in msgs.Read())
        {
            store.Value.Append(new JournalEntry
            {
                Text = m.Text,
                Name = "System",
                Hue = m.Hue,
                TextType = JournalTextType.System,
                TimeLabel = clock.Value.NowLabel(),
            });
        }
    }

    // Open, or focus (bump z) if already open. Called from the top-bar button.
    public static void OpenOrFocus(
        Commands commands,
        GumpBuilder builder,
        AssetsServer assets,
        UiZCounter zCounter,
        Query<Data<JournalWindow>> existingQ)
    {
        foreach (var (ent, _) in existingQ)
        {
            commands.Entity(ent.Ref).Insert(new GlobalZIndex(zCounter.Bump()));
            return;
        }

        int topW = GumpW(assets, ScrollGraphic), topH = GumpH(assets, ScrollGraphic);
        int midW = GumpW(assets, (ushort)(ScrollGraphic + 2));
        int botW = GumpW(assets, (ushort)(ScrollGraphic + 3));
        int width = Math.Max(topW, Math.Max(midW, botW));
        var z = zCounter.Bump();
        var pos = new Vector2(300, 110); // clear of the paperdoll on the left

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

        // Scroll bg pieces (positioned by LayoutWindow). Caps centered (pieces
        // differ in width). Body (mid) spawned FIRST so the top/bottom caps paint
        // OVER it — the body overlaps up under the cap roll to close the seam.
        var mid = builder.AddGumpTiled(commands, (ushort)(ScrollGraphic + 2), Vector3.UnitZ, new Vector2((width - midW) / 2f, topH), new Vector2(midW, 1));
        commands.AddChild(rootId, mid.Id);
        var top = builder.AddGump(commands, ScrollGraphic, Vector3.UnitZ, new Vector2((width - topW) / 2f, 0));
        commands.AddChild(rootId, top.Id);
        var bottom = builder.AddGump(commands, (ushort)(ScrollGraphic + 3), Vector3.UnitZ, new Vector2((width - botW) / 2f, 0));
        commands.AddChild(rootId, bottom.Id);

        // Title art centered on the cap.
        var title = builder.AddGump(commands, TitleGump, Vector3.UnitZ,
            new Vector2((topW - GumpW(assets, TitleGump)) / 2f, Math.Max(0, (topH - GumpH(assets, TitleGump)) / 2f)));
        commands.AddChild(rootId, title.Id);

        // Minimize gumppic — a knob sitting ABOVE the top edge, centered, painted
        // on top of the cap (added after it). Left-click toggles minimized.
        int minW = GumpW(assets, MinimizeGump), minH = GumpH(assets, MinimizeGump);
        var minimize = builder.AddGump(commands, MinimizeGump, Vector3.UnitZ, new Vector2((width - minW) / 2f, -minH + 4))
            .Insert(Interaction.None);
        minimize.Observe((On<UiClick> _, Query<Data<JournalWindow>> wq) =>
        {
            if (!wq.Contains(rootId)) return;
            var (_, w) = wq.Get(rootId);
            w.Ref.Minimized = !w.Ref.Minimized; // LayoutWindow toggles display each frame; no rebuild
        });
        commands.AddChild(rootId, minimize.Id);

        // Entry list: flex column, wheel-scrolls. Text stops short of the flag so
        // the two don't overlap; the flag rides the parchment's right edge.
        int parchRight = (width + midW) / 2;
        int flagW = GumpW(assets, FlagGump);
        int flagX = parchRight - flagW + 8; // slight overhang past the parchment edge
        var listWidth = flagX - ListLeft - 8;
        var list = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                PositionType = PositionType.Absolute,
                Left = Val.Px(ListLeft), Top = Val.Px(ListTop),
                Width = Val.Px(listWidth), Height = Val.Px(DefaultHeight - ListTop - 30),
                Overflow = Overflow.Scroll,
            })
            .Insert(new ScrollPosition())
            .Insert<JournalListTag>();
        commands.AddChild(rootId, list.Id);

        // Scroll-position flag — rides the parchment's right edge, right of the
        // text. PositionFlag drives its Y.
        var flag = builder.AddGump(commands, FlagGump, Vector3.UnitZ, new Vector2(flagX, ListTop))
            .Insert<JournalFlagTag>();
        commands.AddChild(rootId, flag.Id);

        // Resize expander (bottom-center). UOButton swaps the pressed sprite;
        // ResizeDrag reads the press and grows/shrinks SpecialHeight.
        var expander = builder.AddButton(commands, (ExpanderNormal, ExpanderPressed, ExpanderNormal), Vector3.UnitZ, new Vector2(0, 0))
            .Insert(new JournalExpander { Window = rootId });
        commands.AddChild(rootId, expander.Id);

        // Dark-mode checkbox + label — on the top cap (header), right side.
        // hue UnitZ — AddCheckbox's default hue is Vector3.Zero whose Z(=alpha)=0
        // renders the sprite fully transparent (invisible boxes).
        int capY = Math.Max(2, (topH - 18) / 2);
        var darkCheck = builder.AddCheckbox(commands, false, new Vector2(width - 110, capY), hue: Vector3.UnitZ)
            .Insert(new JournalDarkCheck { Window = rootId });
        commands.AddChild(rootId, darkCheck.Id);
        var darkLabel = builder.AddLabel(commands, "Dark mode", new Vector2(width - 92, capY + 2));
        commands.Entity(darkLabel.Id).Insert(new TextFont { FontId = 1, Size = 12 });
        commands.AddChild(rootId, darkLabel.Id);

        // Filter checkboxes (legacy cx=43, dist=75). Default all on.
        var filters = new (string label, JournalTextType type)[]
        {
            ("System", JournalTextType.System),
            ("Objects", JournalTextType.Object),
            ("Client", JournalTextType.Client),
            ("Guild", JournalTextType.GuildAlly),
        };
        for (var i = 0; i < filters.Length; i++)
        {
            int fx = 43 + i * 75;
            var cb = builder.AddCheckbox(commands, true, new Vector2(fx, 0), hue: Vector3.UnitZ)
                .Insert(new JournalFilterCheck { Filter = filters[i].type, Window = rootId });
            commands.AddChild(rootId, cb.Id);
            var lbl = builder.AddLabel(commands, filters[i].label, new Vector2(fx + 20, 0));
            commands.Entity(lbl.Id).Insert(new TextFont { FontId = 1, Size = 12 }).Insert<JournalFilterLabel>();
            commands.AddChild(rootId, lbl.Id);
        }

        commands.Entity(rootId).Insert(new JournalWindow
        {
            SpecialHeight = DefaultHeight,
            ShowSystem = true, ShowObjects = true, ShowClient = true, ShowGuild = true,
            BuiltRevision = -1,
            StyleEpoch = 1, BuiltStyleEpoch = 0,
            ListEntity = list.Id, FlagEntity = flag.Id,
            TopEntity = top.Id, MidEntity = mid.Id, BottomEntity = bottom.Id,
            TitleEntity = title.Id, MinimizeEntity = minimize.Id,
            DarkCheckEntity = darkCheck.Id, ExpanderEntity = expander.Id,
            Width = width, ListWidth = listWidth,
        });
    }

    // Position bg pieces + list + flag + filter row from SpecialHeight/Minimized,
    // and apply dark-mode hue. Mirrors ExpandableScroll.Update.
    private static void LayoutWindow(
        Res<AssetsServer> assets,
        Query<Data<JournalWindow>> windowsQ,
        Query<Data<Node>> nodeQ,
        Query<Data<UiCustom>> customQ,
        Query<Data<JournalFilterCheck, Node>> filterChecksQ,
        Query<Data<Node>, Filter<With<JournalFilterLabel>>> filterLabelsQ)
    {
        foreach (var (rootEnt, win) in windowsQ)
        {
            int topH = GumpH(assets.Value, ScrollGraphic);
            int botH = GumpH(assets.Value, (ushort)(ScrollGraphic + 3));
            int expW = GumpW(assets.Value, ExpanderNormal), expH = GumpH(assets.Value, ExpanderNormal);
            int h = win.Ref.Minimized ? topH : win.Ref.SpecialHeight;
            // Body runs between the caps, overlapping up under the top cap to
            // close the seam (caps paint over it — spawn order).
            const int CapOverlap = 14;
            int botY = h - botH;
            int midY = topH - CapOverlap;
            int midH = Math.Max(1, botY - midY);

            var hue = win.Ref.DarkMode ? HueVector(DarkModeHue) : Vector3.UnitZ;

            // Only the height changes (minimize / resize). Top/Left are the
            // drag-controlled window position — never reset them here.
            SetNodeHeight(nodeQ, rootEnt.Ref, h);

            SetCustomHue(customQ, win.Ref.TopEntity, hue);
            SetNodeY(nodeQ, win.Ref.MidEntity, midY);
            SetNodeHeight(nodeQ, win.Ref.MidEntity, midH);
            SetCustomHue(customQ, win.Ref.MidEntity, hue);
            SetNodeY(nodeQ, win.Ref.BottomEntity, botY);
            SetCustomHue(customQ, win.Ref.BottomEntity, hue);

            // Hide everything but the title cap + minimize when minimized.
            var bodyDisplay = win.Ref.Minimized ? Display.None : Display.Flex;
            SetDisplay(nodeQ, win.Ref.MidEntity, bodyDisplay);
            SetDisplay(nodeQ, win.Ref.BottomEntity, bodyDisplay);
            SetDisplay(nodeQ, win.Ref.ListEntity, bodyDisplay);
            SetDisplay(nodeQ, win.Ref.FlagEntity, bodyDisplay);
            SetDisplay(nodeQ, win.Ref.DarkCheckEntity, bodyDisplay);

            // List + flag height track the body.
            SetNodeHeight(nodeQ, win.Ref.ListEntity, Math.Max(1, botY - ListTop - 4));
            SetNodeY(nodeQ, win.Ref.FlagEntity, ListTop);

            // Expander knob touches the bottom cap's end and hangs below it.
            SetNodeXY(nodeQ, win.Ref.ExpanderEntity, (win.Ref.Width - expW) / 2f, h - 2);
            SetDisplay(nodeQ, win.Ref.ExpanderEntity, bodyDisplay);

            // Filter checkboxes + labels sit on the bottom cap (above the expander).
            int filterY = botY + 4;
            foreach (var (fcEnt, _, node) in filterChecksQ)
            {
                node.Ref.Top = Val.Px(filterY);
                node.Ref.Display = bodyDisplay;
            }
            foreach (var (_, node) in filterLabelsQ)
            {
                node.Ref.Top = Val.Px(filterY + 2);
                node.Ref.Display = bodyDisplay;
            }
        }
    }

    // Rebuild the row list whenever the store or the filter/style state changes.
    private static void RebuildContent(
        Commands commands,
        Res<JournalStore> store,
        Query<Data<JournalWindow>> windowsQ,
        Query<Data<TinyEcs.Children>> childrenQ,
        Query<Data<Node>> nodeQ,
        Query<Data<ScrollPosition>> scrollQ)
    {
        foreach (var (_, win) in windowsQ)
        {
            bool dirty = win.Ref.BuiltRevision != store.Value.Revision
                      || win.Ref.BuiltStyleEpoch != win.Ref.StyleEpoch;
            if (!dirty) continue;
            win.Ref.BuiltRevision = store.Value.Revision;
            win.Ref.BuiltStyleEpoch = win.Ref.StyleEpoch;

            var list = win.Ref.ListEntity;

            // Auto-tail: if the view was already pinned to the bottom, keep it
            // pinned after the new content; if the user scrolled up, leave it.
            float listH = 0f;
            if (nodeQ.Contains(list)) { var (_, ln) = nodeQ.Get(list); listH = ln.Ref.Height.Value; }
            float oldOffset = 0f;
            if (scrollQ.Contains(list)) { var (_, sp) = scrollQ.Get(list); oldOffset = sp.Ref.OffsetY; }
            float oldMax = Math.Max(0f, win.Ref.ContentHeight - listH);
            bool wasAtBottom = oldMax <= 0f || oldOffset >= oldMax - 4f;

            // Clear existing rows.
            if (childrenQ.Contains(list))
            {
                var (_, kids) = childrenQ.Get(list);
                foreach (var cid in kids.Ref)
                    commands.Entity(cid).Despawn();
            }

            int textCol = (int)win.Ref.ListWidth - 44; // after the time column
            float contentH = 0f;
            foreach (var e in store.Value.Entries)
            {
                if (!Visible(win.Ref, e.TextType)) continue;

                var row = commands.Spawn()
                    .Insert(new Node { Display = Display.Flex, FlexDirection = FlexDirection.Row, Width = Val.Px(win.Ref.ListWidth), Height = Val.Auto })
                    .Insert(new JournalRow { Type = e.TextType });
                commands.AddChild(list, row.Id);

                // Time column (muted).
                var timeNode = commands.Spawn()
                    .Insert(new Node { Display = Display.Flex, Width = Val.Px(44), Height = Val.Auto })
                    .Insert(new Text(e.TimeLabel ?? string.Empty))
                    .Insert(new TextFont { FontId = 1, Size = 12 })
                    .Insert(new TextColor(new ClayColor(190, 190, 190, 255)));
                commands.AddChild(row.Id, timeNode.Id);

                // Message text: "{Name}: {Text}" (or bare text), wrapped + hued.
                var msg = string.IsNullOrEmpty(e.Name) ? (e.Text ?? string.Empty) : $"{e.Name}: {e.Text}";
                var (tw, th) = UoFontRenderer.Measure(msg, font: 1, textCol, isHtml: false, 0, htmlBg: false);
                var textNode = commands.Spawn()
                    .Insert(new Node { Display = Display.Flex, Width = Val.Px(textCol), Height = Val.Px(Math.Max(th, 1)) })
                    .Insert(new UiCustom
                    {
                        Data = new UOCustomRender
                        {
                            Kind = UOCustomKind.WrappedText,
                            Hue = Vector3.UnitZ,
                            Text = msg,
                            TextFont = 1,
                            TextHue = e.Hue,
                            WrapWidth = textCol,
                            IsHtml = false,
                        },
                    });
                commands.AddChild(row.Id, textNode.Id);
                contentH += Math.Max(th, 1);
            }
            win.Ref.ContentHeight = contentH;

            // Pin to bottom (newest) only if we were already there. Clay clamps
            // an over-large offset to the real max on the next layout.
            if (wasAtBottom && scrollQ.Contains(list))
            {
                var (_, sp) = scrollQ.Get(list);
                sp.Ref.OffsetY = contentH + 100000f;
            }
        }
    }

    private static bool Visible(in JournalWindow w, JournalTextType t) => t switch
    {
        JournalTextType.System => w.ShowSystem,
        JournalTextType.Object => w.ShowObjects,
        JournalTextType.Client => w.ShowClient,
        JournalTextType.GuildAlly => w.ShowGuild,
        _ => true,
    };

    // Slide the flag along the list track to reflect scroll position. Reads the
    // list's ScrollPosition.OffsetY (written back by LayoutSystem) against the
    // tracked content height — no Clay-internal access needed.
    private static void PositionFlag(
        Query<Data<JournalWindow>> windowsQ,
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
        }
    }

    private struct ResizeAnchor
    {
        public bool Active, Claimed;
        public ulong Window;
        public float MouseY0, Origin;
    }

    // Drag the expander handle to grow/shrink the window. Continuous-held +
    // press-once bbox latch, mirroring WindowDragPlugin/TopBar. Claims the shared
    // DragGate so window-move doesn't also fire.
    private static void ResizeDrag(
        Res<MouseContext> mouse,
        Res<DragGate> gate,
        Local<ResizeAnchor> anchor,
        Query<Data<ComputedNode>, Filter<With<JournalExpander>>> handleBbQ,
        Query<Data<JournalExpander>> handleQ,
        Query<Data<JournalWindow>> windowsQ)
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
            foreach (var (ent, bb) in handleBbQ)
            {
                if (pos.X < bb.Ref.Position.X || pos.Y < bb.Ref.Position.Y) continue;
                if (pos.X >= bb.Ref.Position.X + bb.Ref.Size.X || pos.Y >= bb.Ref.Position.Y + bb.Ref.Size.Y) continue;
                if (!handleQ.Contains(ent.Ref)) continue;
                var (_, exp) = handleQ.Get(ent.Ref);
                if (!windowsQ.Contains(exp.Ref.Window)) continue;
                var (_, win) = windowsQ.Get(exp.Ref.Window);
                gate.Value.Mode = ActiveDrag.UIWindow;
                anchor.Value.Claimed = true;
                anchor.Value.Active = true;
                anchor.Value.Window = exp.Ref.Window;
                anchor.Value.MouseY0 = pos.Y;
                anchor.Value.Origin = win.Ref.SpecialHeight;
                break;
            }
        }

        if (!anchor.Value.Active || !windowsQ.Contains(anchor.Value.Window)) return;
        var (_, w) = windowsQ.Get(anchor.Value.Window);
        // Screenshot/render space is ~1.25x the input space; the delta sign is
        // all that matters here — grow when dragging down.
        float newH = anchor.Value.Origin + (mouse.Value.Position.Y - anchor.Value.MouseY0);
        int clamped = (int)Math.Clamp(newH, MinHeight, MaxHeight);
        // Only the height changes; LayoutWindow repositions every frame. Do NOT
        // bump StyleEpoch — rebuilding the rows mid-drag flickers the content.
        w.Ref.SpecialHeight = clamped;
    }

    private struct FlagAnchor
    {
        public bool Active, Claimed;
        public ulong List;
        public float StartY, StartOffset, TrackPixels, MaxScroll;
    }

    // Drag the scroll flag to scroll the list (the flag is the draggable thumb).
    // PreUpdate + DragGate claim so window-move doesn't also fire.
    private static void FlagDrag(
        Res<MouseContext> mouse,
        Res<DragGate> gate,
        Res<AssetsServer> assets,
        Local<FlagAnchor> anchor,
        Query<Data<JournalWindow>> windowsQ,
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
                if (pos.X < bb.Ref.Position.X || pos.Y < bb.Ref.Position.Y) continue;
                if (pos.X >= bb.Ref.Position.X + bb.Ref.Size.X || pos.Y >= bb.Ref.Position.Y + bb.Ref.Size.Y) continue;

                float listH = 0f;
                if (nodeQ.Contains(win.Ref.ListEntity)) { var (_, ln) = nodeQ.Get(win.Ref.ListEntity); listH = ln.Ref.Height.Value; }
                float maxScroll = Math.Max(0f, win.Ref.ContentHeight - listH);
                if (maxScroll <= 0f) return; // nothing to scroll
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

    private static void Despawn(
        Commands commands,
        Query<Data<JournalWindow>> windowsQ,
        Query<Data<TinyEcs.Children>> childrenQ)
    {
        foreach (var (ent, _) in windowsQ)
            DespawnSubtree(commands, ent.Ref, childrenQ);
    }

    private static void DespawnSubtree(Commands commands, ulong entity, Query<Data<TinyEcs.Children>> childrenQ)
    {
        if (childrenQ.Contains(entity))
        {
            var (_, kids) = childrenQ.Get(entity);
            foreach (var cid in kids.Ref)
                DespawnSubtree(commands, cid, childrenQ);
        }
        commands.Entity(entity).Despawn();
    }

    // --- helpers ---------------------------------------------------------

    private static int GumpW(AssetsServer a, ushort id) { ref readonly var g = ref a.Gumps.GetGump(id); return g.UV.Width; }
    private static int GumpH(AssetsServer a, ushort id) { ref readonly var g = ref a.Gumps.GetGump(id); return g.UV.Height; }

    private static Vector3 HueVector(ushort hue) => hue == 0 ? Vector3.UnitZ : new Vector3(hue, 1f, 1f); // matches ServerGumpPlugin.ToShaderHue

    private static void SetNode(Query<Data<Node>> q, ulong id, float top, float height, float width)
    {
        if (!q.Contains(id)) return;
        var (_, n) = q.Get(id);
        n.Ref.Top = Val.Px(top); n.Ref.Height = Val.Px(height); n.Ref.Width = Val.Px(width);
    }
    private static void SetNodeY(Query<Data<Node>> q, ulong id, float y)
    { if (q.Contains(id)) { var (_, n) = q.Get(id); n.Ref.Top = Val.Px(y); } }
    private static void SetNodeXY(Query<Data<Node>> q, ulong id, float x, float y)
    { if (q.Contains(id)) { var (_, n) = q.Get(id); n.Ref.Left = Val.Px(x); n.Ref.Top = Val.Px(y); } }
    private static void SetNodeHeight(Query<Data<Node>> q, ulong id, float h)
    { if (q.Contains(id)) { var (_, n) = q.Get(id); n.Ref.Height = Val.Px(h); } }
    private static void SetDisplay(Query<Data<Node>> q, ulong id, Display d)
    { if (q.Contains(id)) { var (_, n) = q.Get(id); n.Ref.Display = d; } }
    private static void SetCustomHue(Query<Data<UiCustom>> q, ulong id, Vector3 hue)
    { if (q.Contains(id)) { var (_, c) = q.Get(id); var r = c.Ref.Render(); if (r != null) r.Hue = hue; } }
}

internal struct JournalFilterLabel;

// Wall-clock timestamp provider for the journal. Legacy JournalManager stamps
// each entry with DateTime.Now for display; this is the one sanctioned wall-clock
// touch (display text only, never gameplay/engine timing — see CLAUDE.md). Kept
// behind a resource so a deterministic harness can swap it for a fixed clock.
internal sealed class JournalTimeProvider
{
    public Func<string> Source = static () => DateTime.Now.ToString("HH:mm");
    public string NowLabel() => Source();
}
