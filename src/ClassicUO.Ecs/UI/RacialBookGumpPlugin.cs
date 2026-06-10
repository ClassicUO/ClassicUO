// RacialBookGump — ECS port of Game/UI/Gumps/RacialAbilitiesBookGump.cs.
//
// A small paged book (bg 0x2B29, page corners 0x08BB/0x08BC) listing the
// player's racial abilities. Two leading index pages name the abilities (click
// a name to jump to its detail page); each detail-page spread shows up to two
// ability icons, names and a "Passive" tag plus a divider. The ability set,
// icon graphic base and names are fixed per race (Human / Elf / Gargoyle).
//
// Every racial ability is passive except the Gargoyle's "Flying" — double-click
// its icon (or a dragged-off floating button) sends the gargoyle-flight toggle
// (0xBF subcommand 0x32). Race is read from PlayerData (status packet 0x11,
// Type >= 5); 0 is treated as Human.
//
// Page layout mirrors the legacy gump's quirks exactly so navigation matches:
// index names all live on page 1, "Index" headers sit on pages 1..DictPages,
// and ability i is on detail page DictPages + (i / 2) (left column for even i,
// right for odd). PageCount = DictPages + (AbilityCount >> 1) — the legacy
// formula, which can leave a trailing blank page.

using System;
using ClassicUO.Assets;
using ClassicUO.Game.Data;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;

namespace ClassicUO.Ecs;

internal struct RacialBookWindow
{
    public int Page;        // 1-based active page (legacy ActivePage)
    public int PageCount;
    public int DictPages;   // leading index pages
    public int AbilityCount;
    public RaceType Race;
    public bool Dirty;
    public ulong Content, Left, Right;
}

internal struct RacialBookCorner { public ulong Window; public int Dir; }
internal struct RacialNameLabel; // hover recolour
// Gargoyle "Flying" icon — the only interactive racial ability. Lives on a
// detail page (Floating=false) or as a dragged-off floating button (true);
// double-click toggles flight.
internal struct RacialFlyingIcon { public bool Floating; }

internal readonly struct RacialBookGumpPlugin : IPlugin
{
    private const ushort Bg = 0x2B29;
    private const ushort LeftCorner = 0x08BB, RightCorner = 0x08BC;
    private const ushort TextHue = 0x0288, OverHue = 0x0033;
    private const ushort Divider = 0x0835;
    private const int DictPages = 2;
    private const int AbilityOnPage = 3; // index names per column
    private const ushort GargoyleFlyingGraphic = 0x5DDA;

    private static readonly string[] HumanNames =
        { "Strong Back", "Tough", "Workhorse", "Jack of All Trades" };
    private static readonly string[] ElfNames =
        { "Night Sight", "Infused with Magic", "Knowledge of Nature", "Difficult to Track", "Perception", "Wisdom" };
    private static readonly string[] GargoyleNames =
        { "Flying", "Berserk", "Master Artisan", "Deadly Aim", "Mystic Insight" };

    public void Build(App app)
    {
        var rebuildFn = RebuildContent;
        app.AddSystem(rebuildFn).InStage(Stage.Update)
            .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen).Build();

        var hoverFn = NameHover;
        app.AddSystem(hoverFn).InStage(Stage.Update)
            .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen).Build();

        var navFn = Nav;
        app.AddSystem(navFn).InStage(Stage.PreUpdate).Build();

        var dragFn = FlyingDragSystem;
        app.AddSystem(dragFn).InStage(Stage.PreUpdate).Build();

        var despawnFn = Despawn;
        app.AddSystem(despawnFn).OnExit(GameState.GameScreen).Build();

#if AGENT_BUILD
        app.AddResource(new DebugRacialBookQueue());
        var drainFn = DrainDebug;
        app.AddSystem(drainFn).InStage(Stage.First).Build();
#endif
    }

    private static int AbilityCountFor(RaceType race) => race switch
    {
        RaceType.ELF => 6,
        RaceType.GARGOYLE => 5,
        _ => 4, // Human
    };

    private static ushort IconBaseFor(RaceType race) => race switch
    {
        RaceType.ELF => 0x5DD4,
        RaceType.GARGOYLE => 0x5DDA,
        _ => 0x5DD0, // Human
    };

    private static string[] NamesFor(RaceType race) => race switch
    {
        RaceType.ELF => ElfNames,
        RaceType.GARGOYLE => GargoyleNames,
        _ => HumanNames,
    };

    private static bool IsPassive(RaceType race, int i) => !(race == RaceType.GARGOYLE && i == 0);

    // Open (or focus) the racial-abilities book. No server packet — legacy opens
    // it from the paperdoll racial-book button (also driven by the harness).
    public static void OpenOrFocus(
        Commands commands, GumpBuilder builder, AssetsServer assets, UiZCounter zCounter, RaceType race,
        Query<Data<RacialBookWindow>> existingQ)
    {
        foreach (var (ent, _) in existingQ)
        {
            commands.Entity(ent.Ref).Insert(new GlobalZIndex(zCounter.Bump()));
            return;
        }

        if (race == 0) race = RaceType.HUMAN;
        int abilityCount = AbilityCountFor(race);

        var root = builder.SpawnUOGump(commands, Bg, Vector3.UnitZ, new Vector2(100, 100), zCounter);

        var content = commands.Spawn()
            .Insert(new Node { PositionType = PositionType.Absolute, Left = Val.Px(0), Top = Val.Px(0), Width = Val.Px(GumpW(assets, Bg)), Height = Val.Px(GumpH(assets, Bg)) });
        commands.AddChild(root.Id, content.Id);

        var left = builder.AddGump(commands, LeftCorner, Vector3.UnitZ, new Vector2(50, 8))
            .Insert<UiNoWindowDrag>().Insert(new RacialBookCorner { Window = root.Id, Dir = -1 });
        commands.AddChild(root.Id, left.Id);
        var right = builder.AddGump(commands, RightCorner, Vector3.UnitZ, new Vector2(321, 8))
            .Insert<UiNoWindowDrag>().Insert(new RacialBookCorner { Window = root.Id, Dir = 1 });
        commands.AddChild(root.Id, right.Id);

        commands.Entity(root.Id).Insert(new RacialBookWindow
        {
            Page = 1,
            DictPages = DictPages,
            AbilityCount = abilityCount,
            Race = race,
            PageCount = DictPages + (abilityCount >> 1),
            Dirty = true,
            Content = content.Id,
            Left = left.Id,
            Right = right.Id,
        });
    }

    private static int GumpW(AssetsServer a, ushort id) { ref readonly var g = ref a.Gumps.GetGump(id); return g.UV.Width; }
    private static int GumpH(AssetsServer a, ushort id) { ref readonly var g = ref a.Gumps.GetGump(id); return g.UV.Height; }

    private static void RebuildContent(
        Commands commands,
        Res<GumpBuilder> builder,
        Res<UOFileManager> files,
        ResMut<ObjectPropertyLists> opl,
        Query<Data<RacialBookWindow>> windowsQ,
        Query<Data<TinyEcs.Children>> childrenQ)
    {
        foreach (var (_, win) in windowsQ)
        {
            if (!win.Ref.Dirty) continue;
            win.Ref.Dirty = false;
            win.Ref.Page = Math.Clamp(win.Ref.Page, 1, Math.Max(1, win.Ref.PageCount));

            var content = win.Ref.Content;
            if (childrenQ.Contains(content))
            {
                var (_, kids) = childrenQ.Get(content);
                foreach (var cid in kids.Ref) commands.Entity(cid).Despawn();
            }

            BuildPage(commands, builder.Value, files.Value, opl.Value, in win.Ref);
        }
    }

    // Synthetic OPL serial for a racial ability icon — not a real object, just a
    // stable key per icon graphic in the 0xF2000000 range (above item/mobile
    // serials and the spellbook's 0xF0000000 range).
    private static uint TooltipSerial(ushort graphic) => 0xF200_0000u | graphic;

    // Tooltip cliloc per icon graphic — legacy RacialAbilityButton.SetTooltip /
    // in-book SetTooltip both use 1112198 + (graphic - 0x5DD0).
    private static int TooltipCliloc(ushort graphic) => 1112198 + (graphic - 0x5DD0);

    // Seed an OPL entry for `graphic` so the shared tooltip path renders the
    // ability's cliloc description on hover, wrapped at `maxWidth` (legacy
    // SetTooltip maxWidth — 150 for in-book icons, 200 for the dragged button),
    // and return the synthetic serial to stamp on the icon's render.
    private static uint SeedTooltip(UOFileManager files, ObjectPropertyLists opl, ushort graphic, int maxWidth)
    {
        uint serial = TooltipSerial(graphic);
        string name = files.Clilocs.GetString(TooltipCliloc(graphic));
        opl.Add(serial, 1, name ?? string.Empty, string.Empty, maxWidth);
        return serial;
    }

    private static void BuildPage(Commands commands, GumpBuilder builder, UOFileManager files, ObjectPropertyLists opl, in RacialBookWindow win)
    {
        int page = win.Page;
        var race = win.Race;
        int abilityCount = win.AbilityCount;
        var names = NamesFor(race);
        ushort iconBase = IconBaseFor(race);
        ulong content = win.Content;

        // Index headers on the leading pages (legacy adds them to every dict
        // page regardless of whether names land there).
        if (page <= win.DictPages)
        {
            AddText(commands, content, "Index", 6, 106, 10);
            AddText(commands, content, "Index", 6, 269, 10);
        }

        // All ability names sit on page 1 (two columns of up to 3).
        if (page == 1)
        {
            for (int offs = 0; offs < abilityCount && offs < names.Length; offs++)
            {
                int col = offs / AbilityOnPage;       // 0 = left, 1 = right
                int row = offs % AbilityOnPage;
                int x = col == 0 ? 62 : 225;
                int y = 52 + row * 15;
                int targetPage = win.DictPages + (offs / 2); // this ability's detail page
                AddNameLabel(commands, content, names[offs], targetPage, win.PageCount, x, y);
            }
        }

        // Detail spread: ability 2*(page-2) on the left, +1 on the right.
        if (page >= 2)
        {
            int leftI = 2 * (page - win.DictPages);
            AddDetail(commands, builder, files, opl, content, race, names, iconBase, leftI, abilityCount, left: true);
            AddDetail(commands, builder, files, opl, content, race, names, iconBase, leftI + 1, abilityCount, left: false);
        }
    }

    private static void AddDetail(
        Commands commands, GumpBuilder builder, UOFileManager files, ObjectPropertyLists opl, ulong content,
        RaceType race, string[] names, ushort iconBase, int i, int abilityCount, bool left)
    {
        if (i < 0 || i >= abilityCount || i >= names.Length) return;

        int iconX = left ? 62 : 225;
        int iconTextX = left ? 112 : 275;
        bool passive = IsPassive(race, i);
        ushort graphic = (ushort)(iconBase + i);

        // Hover tooltip on the icon (legacy pic.SetTooltip) via a pre-seeded OPL
        // entry stamped onto the icon's render — same path as the spellbook.
        uint tip = SeedTooltip(files, opl, graphic, 150);
        var icon = builder.AddGump(commands, graphic, Vector3.UnitZ, new Vector2(iconX, 40))
            .Insert(new UiCustom { Data = new UOCustomRender { Kind = UOCustomKind.Gump, AssetId = graphic, Hue = Vector3.UnitZ, TooltipSerial = tip } })
            .Insert(Interaction.None).Insert<UiNoWindowDrag>().Insert<UiContainsByBounds>();
        if (!passive)
        {
            icon.Insert(new RacialFlyingIcon { Floating = false });
            icon.Observe((On<UiDoubleClick> _, Res<NetClient> net) => net.Value.Send_ToggleGargoyleFlying());
        }
        commands.AddChild(content, icon.Id);

        // Name wraps at width 100 (legacy Label maxWidth 100, font 6).
        AddWrapped(commands, content, names[i], 6, TextHue, iconTextX, 34, 100);

        if (passive)
            AddText(commands, content, "Passive", 6, iconTextX, 64);

        var div = builder.AddGumpTiled(commands, Divider, Vector3.UnitZ, new Vector2(iconX, 88), new Vector2(120, 4));
        commands.AddChild(content, div.Id);
    }

    private static void AddNameLabel(
        Commands commands, ulong content, string name, int targetPage, int pageCount, int x, int y)
    {
        var lbl = commands.Spawn()
            .Insert(new Node { PositionType = PositionType.Absolute, Left = Val.Px(x), Top = Val.Px(y), Width = Val.Px(150), Height = Val.Auto })
            .Insert(new Text(name))
            .Insert(new TextFont { FontId = (ushort)(9 | UoFontRuntime.AsciiFlag), Size = 12 })
            .Insert(new TextColor(UoFontRuntime.AsciiHue(TextHue)))
            .Insert(new UiCustom { Data = new UOCustomRender { Kind = UOCustomKind.None, Hue = Vector3.UnitZ } })
            .Insert(Interaction.None).Insert<UiNoWindowDrag>().Insert<UiContainsByBounds>()
            .Insert<RacialNameLabel>();
        lbl.Observe((On<UiClick> _, Query<Data<RacialBookWindow>> wq) =>
        {
            foreach (var (_, w) in wq)
                if (w.Ref.Page != targetPage && targetPage <= w.Ref.PageCount) { w.Ref.Page = targetPage; w.Ref.Dirty = true; }
        });
        commands.AddChild(content, lbl.Id);
    }

    private static void AddText(Commands commands, ulong parent, string s, int font, int x, int y)
    {
        var t = commands.Spawn()
            .Insert(new Node { PositionType = PositionType.Absolute, Left = Val.Px(x), Top = Val.Px(y), Width = Val.Auto, Height = Val.Auto })
            .Insert(new Text(s))
            .Insert(new TextFont { FontId = (ushort)(font | UoFontRuntime.AsciiFlag), Size = 12 })
            .Insert(new TextColor(UoFontRuntime.AsciiHue(TextHue)));
        commands.AddChild(parent, t.Id);
    }

    private static void AddWrapped(Commands commands, ulong parent, string text, int font, ushort hue, int x, int y, int w)
    {
        var (_, h) = UoFontRenderer.Measure(text, (byte)font, w, isHtml: false, 0, htmlBg: false, ascii: true);
        var ent = commands.Spawn()
            .Insert(new Node { PositionType = PositionType.Absolute, Left = Val.Px(x), Top = Val.Px(y), Width = Val.Px(w), Height = Val.Px(Math.Max(h, 12)) })
            .Insert(new UiCustom
            {
                Data = new UOCustomRender
                {
                    Kind = UOCustomKind.WrappedText,
                    Hue = Vector3.UnitZ,
                    Text = text,
                    TextFont = (byte)font,
                    TextHue = hue,
                    WrapWidth = w,
                    IsHtml = false,
                    TextAscii = true,
                }
            });
        commands.AddChild(parent, ent.Id);
    }

    private static void NameHover(Query<Data<Interaction, TextColor>, Filter<With<RacialNameLabel>>> labelsQ)
    {
        var over = UoFontRuntime.AsciiHue(OverHue);
        var normal = UoFontRuntime.AsciiHue(TextHue);
        foreach (var (_, inter, color) in labelsQ)
            color.Ref.Value = inter.Ref == Interaction.Hovered || inter.Ref == Interaction.Pressed ? over : normal;
    }

    private static void Nav(
        Res<MouseContext> mouse,
        Res<DragGate> gate,
        Local<bool> claimed,
        Query<Data<ComputedNode, RacialBookCorner>> cornersQ,
        Query<Data<RacialBookWindow>> windowsQ)
    {
        bool held = mouse.Value.IsPressed(MouseButtonType.Left) || mouse.Value.IsPressedOnce(MouseButtonType.Left);
        if (!held)
        {
            if (claimed.Value && gate.Value.Mode == ActiveDrag.UIWindow) gate.Value.Mode = ActiveDrag.None;
            claimed.Value = false;
            return;
        }
        if (!mouse.Value.IsPressedOnce(MouseButtonType.Left)) return;
        if (gate.Value.Mode != ActiveDrag.None) return;

        var pos = mouse.Value.Position;
        foreach (var (_, bb, c) in cornersQ)
        {
            if (!Contains(bb.Ref, pos)) continue;
            if (windowsQ.Contains(c.Ref.Window))
            {
                var (_, w) = windowsQ.Get(c.Ref.Window);
                if (mouse.Value.IsPressedDouble(MouseButtonType.Left))
                    w.Ref.Page = c.Ref.Dir < 0 ? 1 : w.Ref.PageCount;
                else
                {
                    int np = w.Ref.Page + c.Ref.Dir;
                    if (np >= 1 && np <= w.Ref.PageCount) w.Ref.Page = np;
                }
                w.Ref.Dirty = true;
            }
            gate.Value.Mode = ActiveDrag.UIWindow;
            claimed.Value = true;
            break;
        }
    }

    private struct DragAnchor { public bool Active, Claimed, Spawned; public Vector2 Start; }

    // Drag the Gargoyle flying icon off the book to spawn a floating button that
    // rides the cursor; double-click it to toggle flight (legacy RacialAbility
    // Button). One button at a time.
    private static void FlyingDragSystem(
        Res<MouseContext> mouse,
        Res<DragGate> gate,
        Res<ForcedWindowDrag> forced,
        Res<GumpBuilder> builder,
        Res<UiZCounter> z,
        Res<UOFileManager> files,
        ResMut<ObjectPropertyLists> opl,
        Commands commands,
        Local<DragAnchor> anchor,
        Query<Data<ComputedNode, RacialFlyingIcon>> iconsQ,
        Query<Data<RacialFlyingIcon>> existingQ)
    {
        bool held = mouse.Value.IsPressed(MouseButtonType.Left) || mouse.Value.IsPressedOnce(MouseButtonType.Left);
        if (!held)
        {
            if (anchor.Value.Claimed && gate.Value.Mode == ActiveDrag.UIWindow) gate.Value.Mode = ActiveDrag.None;
            anchor.Value = default;
            return;
        }

        if (mouse.Value.IsPressedOnce(MouseButtonType.Left))
        {
            if (gate.Value.Mode != ActiveDrag.None) return;
            var pos = mouse.Value.Position;
            foreach (var (_, bb, ic) in iconsQ)
            {
                if (ic.Ref.Floating || !Contains(bb.Ref, pos)) continue;
                anchor.Value.Active = true;
                anchor.Value.Claimed = true;
                anchor.Value.Start = pos;
                gate.Value.Mode = ActiveDrag.UIWindow;
                break;
            }
        }

        if (anchor.Value.Active && !anchor.Value.Spawned
            && (mouse.Value.Position - anchor.Value.Start).Length() > 6f)
        {
            foreach (var (ent, b) in existingQ)
                if (b.Ref.Floating) commands.Entity(ent.Ref).Despawn();

            var pos = mouse.Value.Position;
            uint tip = SeedTooltip(files.Value, opl.Value, GargoyleFlyingGraphic, 200);
            var fb = builder.Value.SpawnUOGump(commands, GargoyleFlyingGraphic, Vector3.UnitZ, new Vector2(pos.X - 22, pos.Y - 22), z.Value)
                .Insert(new UiCustom { Data = new UOCustomRender { Kind = UOCustomKind.Gump, AssetId = GargoyleFlyingGraphic, Hue = Vector3.UnitZ, TooltipSerial = tip } })
                .Insert(new RacialFlyingIcon { Floating = true });
            fb.Observe((On<UiDoubleClick> _, Res<NetClient> net) => net.Value.Send_ToggleGargoyleFlying());

            forced.Value.Owner = fb.Id;
            gate.Value.Mode = ActiveDrag.None;
            anchor.Value.Claimed = false;
            anchor.Value.Spawned = true;
        }
    }

    private static bool Contains(in ComputedNode bb, Vector2 p)
        => p.X >= bb.Position.X && p.Y >= bb.Position.Y
        && p.X < bb.Position.X + bb.Size.X && p.Y < bb.Position.Y + bb.Size.Y;

    private static void Despawn(
        Commands commands,
        Query<Data<RacialBookWindow>> windowsQ,
        Query<Data<RacialFlyingIcon>, Filter<With<UiMovable>>> floatingQ,
        Query<Data<TinyEcs.Children>> childrenQ)
    {
        foreach (var (ent, _) in windowsQ) DespawnSubtree(commands, ent.Ref, childrenQ);
        foreach (var (ent, _) in floatingQ) DespawnSubtree(commands, ent.Ref, childrenQ);
    }

    private static void DespawnSubtree(Commands commands, ulong e, Query<Data<TinyEcs.Children>> childrenQ)
    {
        if (childrenQ.Contains(e))
        {
            var (_, kids) = childrenQ.Get(e);
            foreach (var cid in kids.Ref) DespawnSubtree(commands, cid, childrenQ);
        }
        commands.Entity(e).Despawn();
    }

#if AGENT_BUILD
    private static void DrainDebug(
        Commands commands,
        ResMut<DebugRacialBookQueue> q,
        Res<GumpBuilder> builder,
        Res<AssetsServer> assets,
        Res<UiZCounter> zCounter,
        Query<Data<RacialBookWindow>> existingQ)
    {
        if (!q.Value.OpenRequested) return;
        q.Value.OpenRequested = false;
        OpenOrFocus(commands, builder.Value, assets.Value, zCounter.Value, q.Value.Race, existingQ);
    }
#endif
}

#if AGENT_BUILD
internal sealed class DebugRacialBookQueue { public bool OpenRequested; public RaceType Race = RaceType.GARGOYLE; }
#endif
