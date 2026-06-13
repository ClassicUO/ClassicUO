// Classic-style healthbar gump — ECS port of HealthBarGump (the non-custom,
// gump-graphic variant) from Game/UI/Gumps/HealthBarGump.cs.
//
// Opened by pressing on a mobile (or the player) in the world and dragging
// past MIN_PICKUP_DRAG_DISTANCE_PIXELS — the legacy gesture (a plain click is
// a look / double-click is a use, neither opens a bar). The window spawns
// centered on the press position. One window per serial (re-drag focuses).
// Drag / right-click-close / topmost-on-click / world click-capture all fall
// out of the shared UOGump + UiMovable infra (WindowDragPlugin) — this plugin
// does NOT reimplement any of them.
//
// Layout matches the legacy non-party branch:
//   * Player: bg 0x0803 (0x0807 in war mode), three fill bars (HP/Mana/Stam)
//     over red backing lines at x=34, y=12/25/38. No name text.
//   * Other mobile: bg 0x0804 (tinted by notoriety), one HP fill bar at
//     (34,38) over a red backing line, plus a name label at (16,14).
//
// Fill bars are GumpTiled sprites whose width is the pixel-percentage of the
// stat (CalculatePercents), exactly like legacy GumpPicWithWidth.

using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Network;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;

namespace ClassicUO.Ecs;

// Marker on the healthbar window root (the bg gump). Carries the tracked
// serial so refresh can resolve the mobile + dedupe duplicate opens.
internal struct HealthBarWindow
{
    public uint Serial;
    public bool IsPlayer;
    // Tracks the in-range<->out-of-range edge so Refresh can fire the status
    // re-subscribe / close packets once per transition (not every frame).
    public bool OutOfRange;
    // True while a status (HP) request is outstanding — set when we send one,
    // cleared once HP actually arrives (HitsMax>0). Guards against flooding the
    // server with repeat requests if the entity churns in/out before the reply
    // lands (legacy HitsRequestStatus Pending/Received).
    public bool HpRequested;
    // Last in-range HP reading was 0 — when the entity then vanishes, the
    // disappearance was a death, which is what CloseHealthBarType == 2 closes
    // on (legacy checks CorpseManager.Exists; ECS has no corpse manager, so
    // the hits-at-zero edge is the death signal).
    public bool WasDead;
    // Which layout is currently built (party 3-bar vs normal). The rebuild
    // system flips the layout when this diverges from PartyState.Contains.
    public bool InParty;
}

// Carried by each fill bar (HP=0, Mana=1, Stam=2). Refresh resolves the
// mobile by Serial, computes the pixel width from the matching stat, and
// (for HP) swaps the bar graphic on poison / yellow-hits.
internal struct HealthBarFill
{
    public uint Serial;
    public byte Kind;
    public int BarWidth;
    // Party bars tint the HP bar on poison/yellow (hue 63/353) instead of
    // swapping the graphic, and keep the LINE_BLUE_PARTY sprite.
    public bool Party;
}

// Carried by each red backing line. Out-of-range (the tracked mobile left the
// world) tints it gray (legacy _hpLineRed.Hue) while the fill bars hide.
internal struct HealthBarBackLine
{
    public uint Serial;
}

// Carried by the mobile name label.
internal struct HealthBarNameText
{
    public uint Serial;
}

internal readonly struct HealthBarPlugin : IPlugin
{
    private const ushort BACKGROUND_NORMAL = 0x0803;
    private const ushort BACKGROUND_WAR = 0x0807;
    private const ushort BACKGROUND_MOBILE = 0x0804;
    private const ushort LINE_RED = 0x0805;
    private const ushort LINE_BLUE = 0x0806;
    private const ushort LINE_POISONED = 0x0808;
    private const ushort LINE_YELLOWHITS = 0x0809;

    // Party-mode bar graphics (legacy LINE_RED_PARTY / LINE_BLUE_PARTY) and the
    // two heal-spell buttons (Greater Heal 0x0938 / Cure 0x0939, shared hover
    // 0x093A). Party bars fill 96px at 100%.
    private const ushort LINE_RED_PARTY = 0x0028;
    private const ushort LINE_BLUE_PARTY = 0x0029;
    private const ushort HEAL1_NORMAL = 0x0938;
    private const ushort HEAL2_NORMAL = 0x0939;
    private const ushort HEAL_OVER = 0x093A;
    private const int BAR_WIDTH_PARTY = 96;
    // Greater Heal / Cure spell indices (legacy ButtonParty heal handlers).
    private const int SPELL_GREATER_HEAL = 29;
    private const int SPELL_CURE = 11;

    // Non-party bar fill spans 109px at 100% (legacy barW for the single-mobile
    // / player path). The name label hue is the legacy status text hue.
    private const int BAR_WIDTH = 109;
    private const ushort NameHue = 0x0386;

    // Profile default notoriety hues (Configuration/Profile.cs).
    internal static ushort NotorietyHue(NotorietyFlag flag) => flag switch
    {
        NotorietyFlag.Innocent => 0x005A,
        NotorietyFlag.Ally => 0x0044,
        NotorietyFlag.Criminal => 0x03B2,
        NotorietyFlag.Gray => 0x03B2,
        NotorietyFlag.Enemy => 0x0031,
        NotorietyFlag.Murderer => 0x0023,
        NotorietyFlag.Invulnerable => 0x0034,
        _ => 0,
    };

    public void Build(App app)
    {
        var openFn = OpenOnDrag;
        var interactFn = WindowInteractions;
        var refreshFn = Refresh;
        var despawnFn = Despawn;
        var rebuildFn = RebuildOnPartyChange;

        app.AddSystem(openFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
            .Build();

        app.AddSystem(interactFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
            .Build();

        // Rebuild a bar's layout when its party membership flips. Declared before
        // Refresh so (declaration order preserved) the new bars fill same frame.
        app.AddSystem(rebuildFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
            .Build();

        app.AddSystem(refreshFn).InStage(Stage.Update).Build();
        app.AddSystem(despawnFn).OnExit(GameState.GameScreen).Build();
    }

    private struct ClickLatch
    {
        public ulong Target;
        public Vector2 PressPos;
    }

    // Double-click a healthbar / single-click the status bar — resolved via
    // UiPick over the raw mouse (NOT Bevy pointer events), matching the
    // drag/right-click-close gestures. Double-click the player bar toggles to
    // the status gump (and vice-versa); double-click another mobile's bar
    // sends a use/attack request. Both honour StatusGumpBarMutuallyExclusive.
    private static void WindowInteractions(
        Commands commands,
        Res<MouseContext> mouse,
        Res<AssetsServer> assets,
        Res<GumpBuilder> builder,
        Res<GameContext> gameCtx,
        Res<UiZCounter> zCounter,
        Res<NetClient> net,
        Res<PartyState> party,
        Res<ClassicUO.Configuration.Profile> profile,
        Local<ClickLatch> latch,
        HbInteractParams p)
    {
        var pos = mouse.Value.Position;

        // Double-click: toggle the player bar <-> status gump, or use a mobile.
        if (mouse.Value.IsPressedDouble(Input.MouseButtonType.Left))
        {
            latch.Value = default;
            var win = p.TopmostMovable(pos, assets.Value);
            if (win == 0 || !p.Hb.TryGet(win, out var winRow)) return;

            var (_, w) = winRow;
            if (w.Ref.IsPlayer)
            {
                StatusBarPlugin.OpenOrFocus(commands, builder.Value, assets.Value, gameCtx.Value, profile.Value, zCounter.Value, p.Status);
                commands.Entity(win).Despawn();
            }
            else
            {
                net.Value.Send_DoubleClick(w.Ref.Serial);
            }
            return;
        }

        // Press: latch the window under the cursor for a click-release test.
        // A press on a button (buff / stat-lock) doesn't latch — the button
        // owns that click, exactly like legacy where the gump's OnMouseUp only
        // fires off the button controls.
        if (mouse.Value.IsPressedOnce(Input.MouseButtonType.Left))
        {
            var hit = UiPick.Topmost(pos, assets.Value, p.Rendered);
            if (p.Buttons.Contains(hit.Entity) || p.LockButtons.Contains(hit.Entity))
            {
                latch.Value = default;
                return;
            }
            latch.Value.Target = UiPick.MovableRoot(hit.Entity, p.Movables, p.Parents);
            latch.Value.PressPos = pos;
            return;
        }

        // Release: a click (no drag) on the status bar minimizes it into the
        // player healthbar at the bar's position (legacy StatusGumpBase.OnMouseUp).
        if (mouse.Value.IsReleased(Input.MouseButtonType.Left))
        {
            var target = latch.Value.Target;
            var pressPos = latch.Value.PressPos;
            latch.Value = default;
            if (target == 0 || !p.Status.Contains(target)) return;

            var off = pos - pressPos;
            if (System.Math.Abs(off.X) >= Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS ||
                System.Math.Abs(off.Y) >= Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS)
                return; // dragged — that's a move, not a click

            if (p.TopmostMovable(pos, assets.Value) != target)
                return; // released off the window

            Vector2 spawn = new(20, 150);
            if (p.Nodes.TryGet(target, out var nodeRow))
            {
                var (_, n) = nodeRow;
                spawn = new Vector2(n.Ref.Left.Value, n.Ref.Top.Value);
            }
            OpenForSerial(commands, builder.Value, assets.Value, gameCtx.Value, zCounter.Value,
                net.Value, party.Value, gameCtx.Value.PlayerSerial, spawn, center: false, p.Hb);
            commands.Entity(target).Despawn();
        }
    }

    // Per-gesture state for the drag-to-open trigger. Mirrors legacy: a
    // left-press on a mobile that then drags past MIN_PICKUP_DRAG_DISTANCE_PIXELS
    // pulls out its healthbar (GameSceneInputHandler's drag path). A plain click
    // does NOT open one — that's a single-click look / double-click use.
    private struct HbDragState
    {
        public uint Serial;
        public Vector2 PressPos;
        public bool Opened;
    }

    // current*100/max capped at 100, then scaled to maxValue pixels — verbatim
    // from BaseHealthBarGump.CalculatePercents.
    internal static int CalculatePercents(int max, int current, int maxValue)
    {
        if (max > 0)
        {
            max = current * 100 / max;
            if (max > 100) max = 100;
            if (max > 1) max = maxValue * max / 100;
        }
        return max;
    }

    private static Vector3 ToShaderHue(ushort hue)
        => hue == 0 ? Vector3.UnitZ : new Vector3(hue, 1f, 1f);

    // HP fill sprite + tint for a mobile's poison / yellow-bar / party state.
    // Party bars keep one sprite (LINE_BLUE_PARTY) and tint it lime/orange
    // (legacy _bars[0].Hue 63/353); non-party bars swap the sprite and stay
    // untinted. Pure, so the parity is unit-tested.
    internal static (ushort AssetId, Vector3 Hue) ResolveHpFillSprite(bool party, Flags flags)
    {
        if (party)
        {
            var hue = (flags & Flags.Poisoned) != 0 ? ToShaderHue(63)
                    : (flags & Flags.YellowBar) != 0 ? ToShaderHue(353)
                    : Vector3.UnitZ;
            return (LINE_BLUE_PARTY, hue);
        }

        var asset = (flags & Flags.Poisoned) != 0 ? LINE_POISONED
                  : (flags & Flags.YellowBar) != 0 ? LINE_YELLOWHITS
                  : LINE_BLUE;
        return (asset, Vector3.UnitZ);
    }

    private static void OpenOnDrag(
        Commands commands,
        Res<MouseContext> mouse,
        Res<SelectedEntity> selected,
        Res<GameContext> gameCtx,
        Res<GumpBuilder> builder,
        Res<AssetsServer> assets,
        Res<UiZCounter> zCounter,
        Res<NetClient> net,
        Res<ForcedWindowDrag> forcedDrag,
        Res<PartyState> party,
        Local<HbDragState> drag,
        Query<Data<NetworkSerial>> serialQ,
        Query<Data<HealthBarWindow>> existingQ)
    {
        var m = mouse.Value;

        if (m.IsReleased(Input.MouseButtonType.Left))
        {
            drag.Value = default;
            return;
        }

        // Press: latch the mobile under the cursor + the press position. The
        // healthbar isn't opened yet — only a subsequent drag pulls it out.
        if (m.IsPressedOnce(Input.MouseButtonType.Left))
        {
            drag.Value = default;
            var target = selected.Value.Entity;
            if (serialQ.TryGet(target, out var serialRow))
            {
                var (_, ns) = serialRow;
                if (SerialHelper.IsMobile(ns.Ref.Value))
                {
                    drag.Value.Serial = ns.Ref.Value;
                    drag.Value.PressPos = m.Position;
                }
            }
            return;
        }

        if (drag.Value.Serial == 0 || drag.Value.Opened || !m.IsPressed(Input.MouseButtonType.Left))
            return;

        var off = m.Position - drag.Value.PressPos;
        if (System.Math.Abs(off.X) < Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS &&
            System.Math.Abs(off.Y) < Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS)
            return;

        drag.Value.Opened = true;
        // Legacy spawns the window centered on the original press (LClickPosition)
        // and attaches it to the cursor (AttemptDragControl). Hand the new root
        // to ForcedWindowDrag so WindowDragPlugin latches it onto the still-held
        // mouse next frame (the spawn is deferred).
        var newRoot = OpenForSerial(commands, builder.Value, assets.Value, gameCtx.Value, zCounter.Value,
            net.Value, party.Value, drag.Value.Serial, drag.Value.PressPos, center: true, existingQ);
        if (newRoot != 0)
            forcedDrag.Value.Owner = newRoot;
    }

    // Open (or focus) the healthbar for a serial. `anchor` is the press point
    // (center: true → centered, the drag-out gesture) or a window top-left
    // (center: false → e.g. the status bar minimizing into the player bar).
    // Returns the new window root entity (0 if an existing one was focused) so
    // the drag-out path can hand it to ForcedWindowDrag.
    public static ulong OpenForSerial(
        Commands commands,
        GumpBuilder builder,
        AssetsServer assets,
        GameContext gameCtx,
        UiZCounter zCounter,
        NetClient net,
        PartyState party,
        uint serial,
        Vector2 anchor,
        bool center,
        Query<Data<HealthBarWindow>> existingQ)
    {
        // Already open for this serial -> focus (bump z) instead of duplicating.
        foreach (var (ent, w) in existingQ)
        {
            if (w.Ref.Serial == serial)
            {
                commands.Entity(ent.Ref).Insert(new GlobalZIndex(zCounter.Bump()));
                return 0;
            }
        }

        net.Send_StatusRequest(serial);

        bool isPlayer = serial == gameCtx.PlayerSerial;
        bool inParty = party.Contains(serial);
        ushort bgId = isPlayer ? BACKGROUND_NORMAL : BACKGROUND_MOBILE;
        // Party bars are 115x55 with an invisible background (legacy bg Alpha=0);
        // normal bars are sized to the bg sprite.
        ref readonly var bgInfo = ref assets.Gumps.GetGump(bgId);
        Vector2 size = inParty ? new Vector2(115, 55) : new Vector2(bgInfo.UV.Width, bgInfo.UV.Height);
        var topLeft = center
            ? new Vector2(anchor.X - size.X / 2f, anchor.Y - size.Y / 2f)
            : anchor;

        return BuildWindow(commands, builder, assets, gameCtx, zCounter, topLeft, serial, bgId, isPlayer, inParty);
    }

    private static ulong BuildWindow(
        Commands commands,
        GumpBuilder builder,
        AssetsServer assets,
        GameContext gameCtx,
        UiZCounter zCounter,
        Vector2 spawnPos,
        uint serial,
        ushort bgId,
        bool isPlayer,
        bool inParty)
    {
        // Double-click handling lives in WindowInteractions (UiPick over the raw
        // mouse), NOT a UiDoubleClick observer — Bevy pointer events route to the
        // topmost INTERACTIVE element, which other centered interactive surfaces
        // can steal over the bar cutouts. The drag/close gestures use UiPick for
        // the same reason (see the UO Gump contract).
        var root = builder.SpawnUOGump(commands, bgId, Vector3.UnitZ, spawnPos, zCounter)
            .Insert(new HealthBarWindow { Serial = serial, IsPlayer = isPlayer, InParty = inParty })
            .Insert<UiContainsByBounds>();
        var rootId = root.Id;

        ConfigureRoot(commands, assets, rootId, bgId, spawnPos, inParty);
        Populate(commands, builder, assets, gameCtx, rootId, serial, isPlayer, inParty);
        return rootId;
    }

    // Set the root's box size + background render for the chosen layout. Party
    // mode hides the background (legacy bg Alpha=0) and fixes the box at 115x55;
    // normal mode keeps the bg sprite. Re-inserting Node/UiCustom replaces them,
    // so this also re-shapes a window during a layout rebuild.
    private static void ConfigureRoot(
        Commands commands,
        AssetsServer assets,
        ulong rootId,
        ushort bgId,
        Vector2 spawnPos,
        bool inParty)
    {
        ref readonly var bgInfo = ref assets.Gumps.GetGump(bgId);
        Vector2 size = inParty ? new Vector2(115, 55) : new Vector2(bgInfo.UV.Width, bgInfo.UV.Height);

        commands.Entity(rootId).Insert(new Node
        {
            Display = Display.Flex,
            PositionType = PositionType.Absolute,
            Left = Val.Px(spawnPos.X), Top = Val.Px(spawnPos.Y),
            Width = Val.Px(size.X), Height = Val.Px(size.Y),
        });

        commands.Entity(rootId).Insert(new UiCustom
        {
            Data = new UOCustomRender
            {
                // None = nothing drawn but still hit-testable (UiContainsByBounds).
                Kind = inParty ? UOCustomKind.None : UOCustomKind.Gump,
                AssetId = bgId,
                Hue = Vector3.UnitZ,
            }
        });
    }

    // Build the bars / name / heal buttons for the chosen layout under rootId.
    private static void Populate(
        Commands commands,
        GumpBuilder builder,
        AssetsServer assets,
        GameContext gameCtx,
        ulong rootId,
        uint serial,
        bool isPlayer,
        bool inParty)
    {
        if (inParty)
        {
            int barHeight = assets.Gumps.GetGump(LINE_BLUE_PARTY).UV.Height;

            // All party members show three bars (HP/Mana/Stam) at x=18.
            AddBar(commands, builder, rootId, serial, 0, 18, 20, barHeight, LINE_RED_PARTY, LINE_BLUE_PARTY, BAR_WIDTH_PARTY, true);
            AddBar(commands, builder, rootId, serial, 1, 18, 33, barHeight, LINE_RED_PARTY, LINE_BLUE_PARTY, BAR_WIDTH_PARTY, true);
            AddBar(commands, builder, rootId, serial, 2, 18, 45, barHeight, LINE_RED_PARTY, LINE_BLUE_PARTY, BAR_WIDTH_PARTY, true);

            // Greater Heal / Cure buttons down the left edge.
            // Legacy Button(id, normal, pressed, over): Heal1 (0x0938,0x093A,0x0938).
            var heal1 = builder.AddButton(commands, (HEAL1_NORMAL, HEAL_OVER, HEAL1_NORMAL), Vector3.UnitZ, new Vector2(0, 20));
            var ver = gameCtx.ClientVersion;
            heal1.Observe((On<UiClick> _, Res<NetClient> net) => net.Value.Send_CastSpell(SPELL_GREATER_HEAL, ver));
            commands.AddChild(rootId, heal1.Id);

            var heal2 = builder.AddButton(commands, (HEAL2_NORMAL, HEAL_OVER, HEAL2_NORMAL), Vector3.UnitZ, new Vector2(0, 33));
            heal2.Observe((On<UiClick> _, Res<NetClient> net) => net.Value.Send_CastSpell(SPELL_CURE, ver));
            commands.AddChild(rootId, heal2.Id);

            // Name label (legacy font 3, Fixed). Player shows the static "Self"
            // and is NOT live-refreshed (legacy keeps it). Width 120 (player) /
            // 109 (other) per legacy.
            ushort partyFont = (ushort)(3 | UoFontRuntime.AsciiFlag);
            var partyColor = UoFontRuntime.AsciiHue(isPlayer ? (ushort)0x005A : NameHue);
            var nameCmd = commands.Spawn()
                .Insert(new Node
                {
                    PositionType = PositionType.Absolute,
                    Left = Val.Px(0), Top = Val.Px(-2),
                    Width = Val.Px(isPlayer ? 120 : 109), Height = Val.Px(50),
                })
                .Insert(new Text(isPlayer ? "Self" : string.Empty))
                .Insert(new TextFont { FontId = partyFont, Size = 12 })
                .Insert(new TextColor(partyColor));
            if (!isPlayer)
                nameCmd.Insert(new HealthBarNameText { Serial = serial });
            commands.AddChild(rootId, nameCmd.Id);
            return;
        }

        int normalBarH = assets.Gumps.GetGump(LINE_BLUE).UV.Height;

        if (isPlayer)
        {
            AddBar(commands, builder, rootId, serial, 0, 34, 12, normalBarH, LINE_RED, LINE_BLUE, BAR_WIDTH, false);
            AddBar(commands, builder, rootId, serial, 1, 34, 25, normalBarH, LINE_RED, LINE_BLUE, BAR_WIDTH, false);
            AddBar(commands, builder, rootId, serial, 2, 34, 38, normalBarH, LINE_RED, LINE_BLUE, BAR_WIDTH, false);
        }
        else
        {
            AddBar(commands, builder, rootId, serial, 0, 34, 38, normalBarH, LINE_RED, LINE_BLUE, BAR_WIDTH, false);

            var color = UoFontRuntime.AsciiHue(NameHue);
            ushort fontId = (ushort)(1 | UoFontRuntime.AsciiFlag);
            var name = commands.Spawn()
                .Insert(new Node
                {
                    PositionType = PositionType.Absolute,
                    Left = Val.Px(16), Top = Val.Px(14),
                    Width = Val.Px(120), Height = Val.Px(15),
                })
                .Insert(new Text(string.Empty))
                .Insert(new TextFont { FontId = fontId, Size = 12 })
                .Insert(new TextColor(color))
                .Insert(new HealthBarNameText { Serial = serial });
            commands.AddChild(rootId, name.Id);
        }
    }

    // Rebuild a window's children when its party membership flips (join -> party
    // 3-bar layout, leave -> normal). Mirrors legacy BaseHealthBarGump.
    // UpdateContents (Clear + BuildGump) keyed on World.Party.Contains.
    private static void RebuildOnPartyChange(
        Commands commands,
        Res<PartyState> party,
        Res<GumpBuilder> builder,
        Res<AssetsServer> assets,
        Res<GameContext> gameCtx,
        Query<Data<HealthBarWindow, Node>> windowsQ,
        Query<Data<TinyEcs.Children>> childrenQ)
    {
        foreach (var (ent, win, node) in windowsQ)
        {
            bool desired = party.Value.Contains(win.Ref.Serial);
            if (desired == win.Ref.InParty)
                continue;

            ulong rootId = ent.Ref;

            // Drop the current children (bars / name / heal buttons).
            if (childrenQ.TryGet(rootId, out var kidsRow))
            {
                var (_, kids) = kidsRow;
                foreach (var cid in kids.Ref)
                    commands.Entity(cid).Despawn();
            }

            ushort bgId = win.Ref.IsPlayer ? BACKGROUND_NORMAL : BACKGROUND_MOBILE;
            var spawnPos = new Vector2(node.Ref.Left.Value, node.Ref.Top.Value);

            ConfigureRoot(commands, assets.Value, rootId, bgId, spawnPos, desired);
            Populate(commands, builder.Value, assets.Value, gameCtx.Value, rootId, win.Ref.Serial, win.Ref.IsPlayer, desired);
            win.Ref.InParty = desired;
        }
    }

    // Red backing line + the dynamic blue fill bar over it, at (x,y).
    private static void AddBar(
        Commands commands,
        GumpBuilder builder,
        ulong rootId,
        uint serial,
        byte kind,
        int x,
        int y,
        int barHeight,
        ushort redId,
        ushort blueId,
        int barWidth,
        bool party)
    {
        var red = builder.AddGump(commands, redId, Vector3.UnitZ, new Vector2(x, y))
            .Insert(new HealthBarBackLine { Serial = serial });
        commands.AddChild(rootId, red.Id);

        // Width starts at 0 (empty) — refresh sets it from the stat the moment
        // the status packet lands.
        var fill = builder.AddGumpTiled(commands, blueId, Vector3.UnitZ, new Vector2(x, y), new Vector2(0, barHeight))
            .Insert(new HealthBarFill { Serial = serial, Kind = kind, BarWidth = barWidth, Party = party });
        commands.AddChild(rootId, fill.Id);
    }

    private static void Refresh(
        Commands commands,
        Res<NetworkEntitiesMap> entities,
        Res<NetClient> net,
        Res<ClassicUO.Configuration.Profile> profile,
        Query<Data<Hits>> hitsQ,
        Query<Data<Mana>> manaQ,
        Query<Data<Stamina>> stamQ,
        Query<Data<ServerFlags>> flagsQ,
        Query<Data<Notoriety>> notorietyQ,
        Query<Data<EntityName>> nameQ,
        Query<Data<HealthBarFill, Node, UiCustom>> fillsQ,
        Query<Data<HealthBarBackLine, UiCustom>> backLinesQ,
        Query<Data<HealthBarWindow, UiCustom>> rootsQ,
        Query<Data<HealthBarNameText, Text>> nameLabelsQ)
    {
        // Fill bar widths + HP poison/yellow graphic.
        foreach (var (_, fill, node, custom) in fillsQ)
        {
            if (!entities.Value.TryGet(fill.Ref.Serial, out var ent))
            {
                node.Ref.Width = Val.Px(0);
                continue;
            }

            int px = fill.Ref.Kind switch
            {
                0 => hitsQ.Contains(ent) ? Percent(hitsQ, ent, fill.Ref.BarWidth) : 0,
                1 => manaQ.Contains(ent) ? PercentMana(manaQ, ent, fill.Ref.BarWidth) : 0,
                _ => stamQ.Contains(ent) ? PercentStam(stamQ, ent, fill.Ref.BarWidth) : 0,
            };
            node.Ref.Width = Val.Px(px);

            if (fill.Ref.Kind == 0)
            {
                var r = custom.Ref.Render();
                if (r != null)
                {
                    var flags = flagsQ.Contains(ent) ? Get(flagsQ, ent).Value : Flags.None;
                    (r.AssetId, r.Hue) = ResolveHpFillSprite(fill.Ref.Party, flags);
                }
            }
        }

        // Red backing lines: gray out (hue 0x0386) when the mobile is out of
        // range, plain red otherwise — legacy _hpLineRed.Hue toggle.
        foreach (var (_, line, custom) in backLinesQ)
        {
            var r = custom.Ref.Render();
            if (r == null) continue;
            bool outOfRange = !entities.Value.TryGet(line.Ref.Serial, out _);
            r.Hue = outOfRange ? ToShaderHue(0x0386) : Vector3.UnitZ;
        }

        // Background: war-mode graphic (player) + notoriety tint (mobiles);
        // out-of-range mobiles drop the tint (legacy _background.Hue = 0).
        // Also drive the status re-subscribe / close packets on the range edge:
        // going out -> Send_CloseStatusBarGump (server stops hp updates), coming
        // back -> Send_StatusRequest if hp is stale (HitsMax==0) so the bar
        // refills instead of showing 0 (legacy RequestMobileStatus on re-entry).
        foreach (var (ent, win, custom) in rootsQ)
        {
            var r = custom.Ref.Render();
            if (r == null) continue;

            bool inRange = entities.Value.TryGet(win.Ref.Serial, out var ment);
            bool hpKnown = inRange && hitsQ.Contains(ment) && Get(hitsQ, ment).MaxValue > 0;

            // HP arrived -> request satisfied; allow a future re-request.
            if (hpKnown)
            {
                win.Ref.HpRequested = false;
                win.Ref.WasDead = Get(hitsQ, ment).Value == 0;
            }

            if (inRange == win.Ref.OutOfRange) // edge: state flipped
            {
                if (!inRange)
                {
                    // CloseHealthBarType: 1 = close when the mobile leaves
                    // range, 2 = close only when it died (legacy HealthBarGump
                    // entity-null path). Party bars stay (legacy keeps them);
                    // the player's own bar never auto-closes.
                    net.Value.Send_CloseStatusBarGump(win.Ref.Serial);
                    if (!win.Ref.IsPlayer && !win.Ref.InParty
                        && (profile.Value.CloseHealthBarType == 1
                            || (profile.Value.CloseHealthBarType == 2 && win.Ref.WasDead)))
                    {
                        commands.Entity(ent.Ref).Despawn();
                        continue;
                    }
                }
                else if (!hpKnown && !win.Ref.HpRequested)
                {
                    // Came back with stale HP and no request in flight: ask once.
                    net.Value.Send_StatusRequest(win.Ref.Serial);
                    win.Ref.HpRequested = true;
                }
                win.Ref.OutOfRange = !inRange;
            }

            // Party bars have no background sprite (render kind None) — leave it.
            if (win.Ref.InParty)
                continue;

            if (win.Ref.IsPlayer)
            {
                bool war = inRange
                    && flagsQ.Contains(ment)
                    && (Get(flagsQ, ment).Value & Flags.WarMode) != 0;
                r.AssetId = war ? BACKGROUND_WAR : BACKGROUND_NORMAL;
            }
            else
            {
                ushort hue = 0;
                if (inRange && notorietyQ.Contains(ment))
                {
                    var flag = Get(notorietyQ, ment).Value;
                    hue = flag == NotorietyFlag.Criminal || flag == NotorietyFlag.Gray ? (ushort)0 : NotorietyHue(flag);
                }
                r.Hue = ToShaderHue(hue);
            }
        }

        // Mobile name text.
        foreach (var (_, tag, text) in nameLabelsQ)
        {
            if (entities.Value.TryGet(tag.Ref.Serial, out var ent) && nameQ.Contains(ent))
                text.Ref.Value = Get(nameQ, ent).Value ?? string.Empty;
        }
    }

    private static int Percent(Query<Data<Hits>> q, ulong ent, int barW)
    {
        var h = Get(q, ent);
        return CalculatePercents(h.MaxValue, h.Value, barW);
    }

    private static int PercentMana(Query<Data<Mana>> q, ulong ent, int barW)
    {
        var m = Get(q, ent);
        return CalculatePercents(m.MaxValue, m.Value, barW);
    }

    private static int PercentStam(Query<Data<Stamina>> q, ulong ent, int barW)
    {
        var s = Get(q, ent);
        return CalculatePercents(s.MaxValue, s.Value, barW);
    }

    private static T Get<T>(Query<Data<T>> q, ulong ent) where T : struct
    {
        var (_, c) = q.Get(ent);
        return c.Ref;
    }

    private static void Despawn(
        Commands commands,
        Query<Data<HealthBarWindow>> windowsQ)
    {
        foreach (var (ent, _) in windowsQ)
            commands.Entity(ent.Ref).Despawn();
    }
}

// Queries for WindowInteractions, bundled so the system stays under the
// system-parameter arity limit. UiPick over the raw mouse needs the rendered
// element set + movable roots + parent chain; the rest resolve windows,
// button exclusions, and the status-bar position / despawn.
internal sealed class HbInteractParams : CompositeSystemParam
{
    public readonly Query<Data<ComputedNode, Node, UiCustom, BackgroundColor, Text>, Filter<Optional<UiCustom>, Optional<BackgroundColor>, Optional<Text>>> Rendered;
    public readonly Query<Data<Node, GlobalZIndex>, Filter<With<UiMovable>>> Movables;
    public readonly Query<Data<TinyEcs.Parent>> Parents;
    public readonly Query<Data<HealthBarWindow>> Hb;
    public readonly Query<Data<StatusBarWindow>> Status;
    public readonly Query<Data<Node>> Nodes;
    public readonly Query<Data<UOButton>> Buttons;
    public readonly Query<Data<StatLockButton>> LockButtons;

    public HbInteractParams()
    {
        Rendered    = Add(new Query<Data<ComputedNode, Node, UiCustom, BackgroundColor, Text>, Filter<Optional<UiCustom>, Optional<BackgroundColor>, Optional<Text>>>());
        Movables    = Add(new Query<Data<Node, GlobalZIndex>, Filter<With<UiMovable>>>());
        Parents     = Add(new Query<Data<TinyEcs.Parent>>());
        Hb          = Add(new Query<Data<HealthBarWindow>>());
        Status      = Add(new Query<Data<StatusBarWindow>>());
        Nodes       = Add(new Query<Data<Node>>());
        Buttons     = Add(new Query<Data<UOButton>>());
        LockButtons = Add(new Query<Data<StatLockButton>>());
    }

    // Window root under the cursor (0 if none). Pixel-perfect topmost element
    // resolved up to its UiMovable root — the answer the press/release/dclick
    // gestures share. Deliberately the no-parents Topmost overload (no overflow
    // clip / ContainsByBounds), matching what these handlers have always used.
    public ulong TopmostMovable(Vector2 pos, AssetsServer assets)
        => UiPick.MovableRoot(UiPick.Topmost(pos, assets, Rendered).Entity, Movables, Parents);
}
