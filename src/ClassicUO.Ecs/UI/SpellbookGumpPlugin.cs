// SpellbookGump — ECS port of Game/UI/Gumps/SpellbookGump.cs (Magery for now).
// The server opens it with 0x24 graphic 0xFFFF (→ ContainerOpenedEvent) and
// sends the spell bitmask via 0xBF 0x1B (→ SpellbookStore). Renders the book
// background (0x08AC) with the present spells laid out two pages at a time —
// icon (0x08C0 + id-1) + name + power words — flipped with the page corners.
// Clicking a spell casts it (Send_CastSpell).

using System;
using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Input;
using ClassicUO.Network;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using ClayColor = Clay.Color;

namespace ClassicUO.Ecs;

internal struct SpellbookWindow
{
    public uint Serial;
    public SpellBookType School;
    public int Page;          // current spread (0-based); each spread = 2 pages
    public int PageCount;
    public int BuiltRevision;
    public bool Dirty;
    public ulong ContentEntity; // child holding the per-page spell entries
    public ulong LeftCorner, RightCorner;
}

internal struct SpellEntryClick { public int SpellId; }
internal struct SpellLabel;
internal struct SpellbookCorner { public ulong Window; public int Dir; }
internal struct SpellIconDrag { public int CastId; public ushort Icon; public int Cliloc; public string Name; }
internal struct SpellCastButton { public int CastId; }

internal readonly struct SpellbookGumpPlugin : IPlugin
{
    private const ushort LeftCorner = 0x08BB;
    private const ushort RightCorner = 0x08BC;
    private const ushort SpellHue = 0x0288;
    private const int SpellsPerPage = 8;      // per page side
    private const int SpellsPerSpread = SpellsPerPage * 2;

    public void Build(App app)
    {
        var openFn = OpenFromContainer;
        app.AddSystem(openFn).InStage(Stage.Update)
            .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen).Build();

        var rebuildFn = RebuildContent;
        app.AddSystem(rebuildFn).InStage(Stage.Update)
            .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen).Build();

        var hoverFn = SpellLabelHover;
        app.AddSystem(hoverFn).InStage(Stage.Update)
            .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen).Build();

        // Page-corner flip via bbox poll (PreUpdate + DragGate claim) so the
        // transparent corner sprites don't lose the click to a window drag.
        var navFn = SpellbookNav;
        app.AddSystem(navFn).InStage(Stage.PreUpdate).Build();

        var iconDragFn = SpellIconDragSystem;
        app.AddSystem(iconDragFn).InStage(Stage.PreUpdate).Build();

        var despawnFn = Despawn;
        app.AddSystem(despawnFn).OnExit(GameState.GameScreen).Build();
    }

    private static int GumpW(AssetsServer a, ushort id) { ref readonly var g = ref a.Gumps.GetGump(id); return g.UV.Width; }
    private static int GumpH(AssetsServer a, ushort id) { ref readonly var g = ref a.Gumps.GetGump(id); return g.UV.Height; }

    // Open (or focus) a spellbook when the server pushes a 0xFFFF "container".
    private static void OpenFromContainer(
        Commands commands,
        Res<GumpBuilder> builder,
        Res<AssetsServer> assets,
        Res<UiZCounter> zCounter,
        Res<SpellbookStore> store,
        EventReader<ContainerOpenedEvent> reader,
        Query<Data<SpellbookWindow>> existingQ)
    {
        foreach (var ev in reader.Read())
        {
            if (ev.Graphic != 0xFFFF) continue;

            bool focused = false;
            foreach (var (ent, w) in existingQ)
            {
                if (w.Ref.Serial != ev.Serial) continue;
                commands.Entity(ent.Ref).Insert(new GlobalZIndex(zCounter.Value.Bump()));
                focused = true;
                break;
            }
            if (focused) continue;

            // School comes from the content packet (resolved on item graphic).
            // Real servers push 0xBF 0x1B before the 0x24 open, so the store
            // entry exists here; default to Magery if it somehow doesn't.
            var schoolType = store.Value.BySerial.TryGetValue(ev.Serial, out var data)
                ? data.School : SpellBookType.Magery;
            Build(commands, builder.Value, assets.Value, zCounter.Value, ev.Serial, schoolType);
        }
    }

    private static void Build(Commands commands, GumpBuilder builder, AssetsServer assets, UiZCounter zCounter, uint serial, SpellBookType schoolType)
    {
        var school = SpellSchools.Get(schoolType);
        var pos = new Vector2(100, 100);
        var root = builder.SpawnUOGump(commands, school.BookGraphic, Vector3.UnitZ, pos, zCounter);
        int bookW = GumpW(assets, school.BookGraphic);

        // Content container FIRST so the controls added after it paint/hit-test
        // on top — corners added before it never received clicks.
        var content = commands.Spawn()
            .Insert(new Node { PositionType = PositionType.Absolute, Left = Val.Px(0), Top = Val.Px(0), Width = Val.Px(bookW), Height = Val.Px(GumpH(assets, school.BookGraphic)) });
        commands.AddChild(root.Id, content.Id);

        // Page corners (flip). The curled-page sprites are mostly transparent, so
        // WindowDrag's pixel-perfect UiPick misses them and would steal the click
        // for a window drag — SpellbookNav drives them via a bbox poll instead
        // (claims the DragGate in PreUpdate). Right corner inset on the page.
        var left = builder.AddGump(commands, LeftCorner, Vector3.UnitZ, new Vector2(50, 8))
            .Insert<UiNoWindowDrag>().Insert(new SpellbookCorner { Window = root.Id, Dir = -1 });
        commands.AddChild(root.Id, left.Id);

        var right = builder.AddGump(commands, RightCorner, Vector3.UnitZ, new Vector2(321, 8))
            .Insert<UiNoWindowDrag>().Insert(new SpellbookCorner { Window = root.Id, Dir = 1 });
        commands.AddChild(root.Id, right.Id);

        // Circle buttons (0x08B1..0x08B8) along the bottom — jump to the index
        // spread holding that circle. Magery only; mirrors legacy CreateBook.
        if (school.HasCircles)
        {
            ReadOnlySpan<int> bx = stackalloc int[] { 58, 93, 130, 164, 227, 260, 297, 332 };
            for (int b = 0; b < 8; b++)
            {
                ushort g = (ushort)(0x08B1 + b);
                int spread = b / 2;
                var cb = builder.AddGump(commands, g, Vector3.UnitZ, new Vector2(bx[b], 175))
                    .Insert(Interaction.None).Insert<UiNoWindowDrag>().Insert<UiContainsByBounds>();
                cb.Observe((On<UiClick> _, Query<Data<SpellbookWindow>> wq) =>
                {
                    foreach (var (_, w) in wq)
                        if (w.Ref.Serial == serial && w.Ref.Page != spread) { w.Ref.Page = spread; w.Ref.Dirty = true; }
                });
                commands.AddChild(root.Id, cb.Id);
            }
        }

        commands.Entity(root.Id).Insert(new SpellbookWindow
        {
            Serial = serial,
            School = schoolType,
            Page = 0,
            BuiltRevision = -1,
            Dirty = true,
            ContentEntity = content.Id,
            LeftCorner = left.Id,
            RightCorner = right.Id,
        });
    }

    // Rebuild the visible spread when the page changed, the store updated, or on
    // first build. Lays present spells two pages at a time.
    private static void RebuildContent(
        Commands commands,
        Res<SpellbookStore> store,
        Res<AssetsServer> assets,
        Res<GameContext> gameCtx,
        Res<GumpBuilder> builder,
        Query<Data<SpellbookWindow>> windowsQ,
        Query<Data<TinyEcs.Children>> childrenQ)
    {
        foreach (var (_, win) in windowsQ)
        {
            bool dirty = win.Ref.Dirty || win.Ref.BuiltRevision != store.Value.Revision;
            if (!dirty) continue;
            win.Ref.Dirty = false;
            win.Ref.BuiltRevision = store.Value.Revision;

            var school = SpellSchools.Get(win.Ref.School);
            int maxSpells = school.MaxSpells;

            ulong bits = 0;
            if (store.Value.BySerial.TryGetValue(win.Ref.Serial, out var data)) bits = data.Bitfields;

            // Ordered list of present spells + id -> present-index map (for the
            // index "click name → open that spell's page" jump).
            Span<int> present = stackalloc int[maxSpells];
            Span<int> presentIdx = stackalloc int[maxSpells + 1];
            presentIdx.Fill(-1);
            int count = 0;
            for (int id = 1; id <= maxSpells; id++)
                if ((bits & (1UL << (id - 1))) != 0) { presentIdx[id] = count; present[count++] = id; }

            // Index pages cover the whole spell list, spellsPerPage per side.
            // pagesToFill / spellsOnPage mirror legacy GetBookInfo.
            int dictPages = (int)Math.Ceiling(maxSpells / 8.0);
            if (dictPages % 2 != 0) dictPages++;
            int indexSpreads = dictPages >> 1;
            int spellsPerPage = Math.Min(maxSpells >> 1, 8);

            int infoSpreads = (count + 1) / 2;       // 2 spells per spread
            win.Ref.PageCount = indexSpreads + infoSpreads;
            win.Ref.Page = Math.Clamp(win.Ref.Page, 0, Math.Max(0, win.Ref.PageCount - 1));

            var content = win.Ref.ContentEntity;
            uint serial = win.Ref.Serial;
            if (childrenQ.TryGet(content, out var contentRow))
            {
                var (_, kids) = contentRow;
                foreach (var cid in kids.Ref) commands.Entity(cid).Despawn();
            }

            if (win.Ref.Page < indexSpreads)
            {
                // INDEX layout: per side, "INDEX" title (+ circle name for
                // Magery) then the present spells of that id-range. Single-click
                // a name opens its spell page; double-click casts.
                for (int j = 0; j < 2; j++)
                {
                    int sideBase = (win.Ref.Page * 2 + j) * spellsPerPage; // 0-based id index

                    int indexX = j == 0 ? 106 : 269;
                    int dataX = j == 0 ? 62 : 225;
                    AddText(commands, content, "INDEX", 6, indexX, 10);
                    if (school.HasCircles)
                    {
                        int circle = win.Ref.Page * 2 + j;
                        if (circle < school.CircleNames.Length)
                            AddText(commands, content, school.CircleNames[circle], 6, dataX, 30);
                    }

                    int y = 0;
                    for (int k = 0; k < spellsPerPage; k++)
                    {
                        int id = sideBase + k + 1;
                        if (id > maxSpells || presentIdx[id] < 0) continue;

                        int castId = school.CastId(id);
                        int targetPage = indexSpreads + presentIdx[id] / 2;
                        var name = SpawnSpellLabel(commands, school.Name(id), 9, dataX, 52 + y, 130);
                        name.Observe((On<UiClick> _, Query<Data<SpellbookWindow>> wq) =>
                        {
                            foreach (var (_, w) in wq)
                                if (w.Ref.Serial == serial && w.Ref.Page != targetPage) { w.Ref.Page = targetPage; w.Ref.Dirty = true; }
                        });
                        name.Observe((On<UiDoubleClick> _, Res<NetClient> net, Res<GameContext> ctx) =>
                            net.Value.Send_CastSpell(castId, ctx.Value.ClientVersion));
                        commands.AddChild(content, name.Id);
                        y += 15;
                    }
                }
            }
            else
            {
                // SPELL pages: two spells per spread, each with its gump icon,
                // header, name + power words, optional reagents and a mana/skill
                // requirement line. Double-click the icon or name to cast.
                int baseP = (win.Ref.Page - indexSpreads) * 2;
                for (int side = 0; side < 2; side++)
                {
                    int p = baseP + side;
                    if (p >= count) continue;
                    int id = present[p];
                    int iconX = side == 0 ? 62 : 225;
                    int topTextX = side == 0 ? 87 : 224;
                    int iconTextX = side == 0 ? 112 : 275;
                    int castId = school.CastId(id);
                    ushort iconGfx = school.Icon(id);

                    var icon = builder.Value.AddGump(commands, iconGfx, Vector3.UnitZ, new Vector2(iconX, 40))
                        .Insert(Interaction.None).Insert<UiNoWindowDrag>().Insert<UiContainsByBounds>()
                        .Insert(new SpellIconDrag { CastId = castId, Icon = iconGfx, Cliloc = SpellTooltipCliloc(castId), Name = school.Name(id) });
                    icon.Observe((On<UiDoubleClick> _, Res<NetClient> net, Res<GameContext> ctx) =>
                        net.Value.Send_CastSpell(castId, ctx.Value.ClientVersion));
                    commands.AddChild(content, icon.Id);

                    EntityCommands nm;
                    if (school.HasCircles)
                    {
                        AddText(commands, content, school.CircleNames[school.Circle(id)], 6, topTextX, 10);
                        nm = SpawnSpellLabel(commands, school.Name(id), 6, iconTextX, 34, 90);
                        AddText(commands, content, school.PowerWord(id), 8, iconTextX, 60);
                    }
                    else
                    {
                        nm = SpawnSpellLabel(commands, school.Name(id), 6, topTextX, 6, 130);
                        if (school.PowerWord(id).Length != 0)
                            AddText(commands, content, school.PowerWord(id), 8, iconTextX, 34);
                    }
                    nm.Observe((On<UiDoubleClick> _, Res<NetClient> net, Res<GameContext> ctx) =>
                        net.Value.Send_CastSpell(castId, ctx.Value.ClientVersion));
                    commands.AddChild(content, nm.Id);

                    if (school.ShowReagents)
                    {
                        var reagents = school.Reagent(id);
                        if (reagents.Length != 0)
                        {
                            // Dotted divider, "Reagents" header, then the names stacked.
                            var div = builder.Value.AddGumpTiled(commands, 0x0835, Vector3.UnitZ, new Vector2(iconX, 88), new Vector2(120, 5));
                            commands.AddChild(content, div.Id);
                            AddText(commands, content, "Reagents", 6, iconX, 92);
                            var regs = reagents.Split(", ", StringSplitOptions.RemoveEmptyEntries);
                            for (int r = 0; r < regs.Length; r++)
                                AddText(commands, content, regs[r], 9, iconX, 110 + r * 14);
                        }
                    }

                    if (school.ShowRequires)
                    {
                        // Mana cost / min skill (+ upkeep for tithing schools).
                        int reqY = 162;
                        int tithing = school.TithingCost[id - 1];
                        if (tithing > 0)
                        {
                            AddText(commands, content, $"Upkeep Cost: {tithing}", 6, iconX, 148);
                        }
                        AddText(commands, content, $"Mana cost: {school.ManaCost[id - 1]}", 6, iconX, reqY);
                        AddText(commands, content, $"Min. Skill: {school.MinSkill[id - 1]}", 6, iconX, reqY + 14);
                    }
                }
            }
        }
    }

    // A clickable, hover-recoloured spell label (None-kind custom for a hit box;
    // SpellLabel drives the over-colour via SpellLabelHover).
    private static EntityCommands SpawnSpellLabel(Commands commands, string text, int font, int x, int y, int w)
        => commands.Spawn()
            .Insert(new Node { PositionType = PositionType.Absolute, Left = Val.Px(x), Top = Val.Px(y), Width = Val.Px(w), Height = Val.Auto })
            .Insert(new Text(text))
            .Insert(new TextFont { FontId = (ushort)(font | UoFontRuntime.AsciiFlag), Size = 12 })
            .Insert(new TextColor(UoFontRuntime.AsciiHue(SpellHue)))
            .Insert(new UiCustom { Data = new UOCustomRender { Kind = UOCustomKind.None, Hue = Vector3.UnitZ } })
            .Insert(Interaction.None).Insert<UiNoWindowDrag>().Insert<UiContainsByBounds>()
            .Insert<SpellLabel>();

    private static void AddText(Commands commands, ulong parent, string s, int font, int x, int y)
    {
        var t = commands.Spawn()
            .Insert(new Node { PositionType = PositionType.Absolute, Left = Val.Px(x), Top = Val.Px(y), Width = Val.Auto, Height = Val.Auto })
            .Insert(new Text(s))
            .Insert(new TextFont { FontId = (ushort)(font | UoFontRuntime.AsciiFlag), Size = 12 })
            .Insert(new TextColor(UoFontRuntime.AsciiHue(SpellHue)));
        commands.AddChild(parent, t.Id);
    }

    // Flip pages by clicking a corner. Bbox poll + DragGate claim (PreUpdate)
    // so the mostly-transparent corner sprite isn't stolen by WindowDrag.
    private static void SpellbookNav(
        Res<MouseContext> mouse,
        Res<DragGate> gate,
        Local<bool> claimed,
        Query<Data<ComputedNode, SpellbookCorner>> cornersQ,
        Query<Data<SpellbookWindow>> windowsQ)
    {
        bool held = mouse.Value.IsPressed(MouseButtonType.Left) || mouse.Value.IsPressedOnce(MouseButtonType.Left);
        if (!held)
        {
            if (claimed.Value && gate.Value.Mode == ActiveDrag.UIWindow) gate.Value.Mode = ActiveDrag.None;
            claimed.Value = false;
            return;
        }

        if (mouse.Value.IsPressedOnce(MouseButtonType.Left))
        {
            if (gate.Value.Mode != ActiveDrag.None) return;
            var pos = mouse.Value.Position;
            foreach (var (_, bb, c) in cornersQ)
            {
                if (!Contains(bb.Ref, pos)) continue;
                if (windowsQ.TryGet(c.Ref.Window, out var windowRow))
                {
                    var (_, w) = windowRow;
                    if (mouse.Value.IsPressedDouble(MouseButtonType.Left))
                        // Double-click a corner jumps to the first / last page.
                        w.Ref.Page = c.Ref.Dir < 0 ? 0 : w.Ref.PageCount - 1;
                    else
                    {
                        int np = w.Ref.Page + c.Ref.Dir;
                        if (np >= 0 && np < w.Ref.PageCount) w.Ref.Page = np;
                    }
                    w.Ref.Dirty = true;
                }
                gate.Value.Mode = ActiveDrag.UIWindow;
                claimed.Value = true;
                break;
            }
        }
    }

    private static bool Contains(in ComputedNode bb, Vector2 p)
        => p.X >= bb.Position.X && p.Y >= bb.Position.Y
        && p.X < bb.Position.X + bb.Size.X && p.Y < bb.Position.Y + bb.Size.Y;

    private struct IconDragAnchor { public bool Active, Claimed, Spawned; public int CastId; public ushort Icon; public int Cliloc; public string Name; public Vector2 Start; }

    // Drag a spell icon off the page to make a standalone "cast button" (same
    // page icon) that rides the cursor immediately and drops where released —
    // double-click it to cast. Mirrors legacy OnIconDragBegin -> UseSpellButtonGump
    // + AttemptDragControl. Press latches + claims the DragGate (so the book
    // doesn't drag); once past the threshold the button is spawned and handed to
    // WindowDrag via ForcedWindowDrag so it follows the cursor.
    private static void SpellIconDragSystem(
        Res<MouseContext> mouse,
        Res<DragGate> gate,
        Res<ForcedWindowDrag> forced,
        Res<GumpBuilder> builder,
        Res<UiZCounter> z,
        Res<UOFileManager> fileManager,
        ResMut<ObjectPropertyLists> opl,
        Commands commands,
        Local<IconDragAnchor> anchor,
        Query<Data<ComputedNode, SpellIconDrag>> iconsQ,
        Query<Data<SpellCastButton>> existingButtonsQ)
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
                if (!Contains(bb.Ref, pos)) continue;
                anchor.Value.Active = true;
                anchor.Value.Claimed = true;
                anchor.Value.CastId = ic.Ref.CastId;
                anchor.Value.Icon = ic.Ref.Icon;
                anchor.Value.Cliloc = ic.Ref.Cliloc;
                anchor.Value.Name = ic.Ref.Name;
                anchor.Value.Start = pos;
                gate.Value.Mode = ActiveDrag.UIWindow;
                break;
            }
        }

        // Past the threshold: spawn the cast button now and hand it to WindowDrag
        // (ForcedWindowDrag) so it rides the cursor until release.
        if (anchor.Value.Active && !anchor.Value.Spawned
            && (mouse.Value.Position - anchor.Value.Start).Length() > 6f)
        {
            var pos = mouse.Value.Position;
            int castId = anchor.Value.CastId;
            ushort iconGfx = anchor.Value.Icon;

            // Only one floating button per spell — drop the old one first
            // (legacy GetSpellFloatingButton(id)?.Dispose()).
            foreach (var (ent, b) in existingButtonsQ)
                if (b.Ref.CastId == castId) commands.Entity(ent.Ref).Despawn();

            // Hover tooltip on the dragged button (legacy UseSpellButtonGump.
            // SetTooltip): pre-seed an OPL entry under a synthetic serial keyed by
            // cast id (spell-name cliloc, falling back to the table name when the
            // dataset lacks it) and stamp that serial on the button's render so the
            // shared tooltip path picks it up — no network request, no real serial.
            uint tipSerial = SpellTooltipSerial(castId);
            string tip = anchor.Value.Cliloc != 0 ? fileManager.Value.Clilocs.GetString(anchor.Value.Cliloc) : null;
            if (string.IsNullOrEmpty(tip)) tip = anchor.Value.Name;
            opl.Value.Add(tipSerial, 1, tip ?? string.Empty, string.Empty);

            var fb = builder.Value.SpawnUOGump(commands, iconGfx, Vector3.UnitZ, new Vector2(pos.X - 22, pos.Y - 22), z.Value)
                .Insert(new UiCustom { Data = new UOCustomRender { Kind = UOCustomKind.Gump, AssetId = iconGfx, Hue = Vector3.UnitZ, TooltipSerial = tipSerial } })
                .Insert(new SpellCastButton { CastId = castId });
            fb.Observe((On<UiDoubleClick> _, Res<NetClient> net, Res<GameContext> ctx) =>
                net.Value.Send_CastSpell(castId, ctx.Value.ClientVersion));

            forced.Value.Owner = fb.Id;          // WindowDrag latches it onto the cursor
            gate.Value.Mode = ActiveDrag.None;   // release ours so the forced drag can take over
            anchor.Value.Claimed = false;
            anchor.Value.Spawned = true;
        }
    }

    // Synthetic OPL serial for a dragged spell button — not a real object, just a
    // stable key per cast id in the 0xF0000000 range (above item/mobile serials).
    private static uint SpellTooltipSerial(int castId) => 0xF000_0000u | (uint)castId;

    // Spell-name cliloc per cast id, mirroring legacy UseSpellButtonGump.
    // GetSpellTooltip (cast id == legacy spell.ID across all schools).
    private static int SpellTooltipCliloc(int id)
    {
        if (id >= 1 && id <= 64) return 3002011 + (id - 1);       // Magery
        if (id >= 101 && id <= 117) return 1060509 + (id - 101);  // Necromancy
        if (id >= 201 && id <= 210) return 1060585 + (id - 201);  // Chivalry
        if (id >= 401 && id <= 406) return 1060595 + (id - 401);  // Bushido
        if (id >= 501 && id <= 508) return 1060610 + (id - 501);  // Ninjitsu
        if (id >= 601 && id <= 616) return 1071026 + (id - 601);  // Spellweaving
        if (id >= 678 && id <= 693) return 1031678 + (id - 678);  // Mysticism
        if (id >= 701 && id <= 745)
            return id <= 706 ? 1115612 + (id - 701) : 1155896 + (id - 707); // Mastery
        return 0;
    }

    // Recolour spell labels to hue 0x33 while hovered (legacy HoveredLabel
    // overHue), back to 0x0288 otherwise.
    private static void SpellLabelHover(
        Query<Data<Interaction, TextColor>, Filter<With<SpellLabel>>> labelsQ)
    {
        var over = UoFontRuntime.AsciiHue(0x0033);
        var normal = UoFontRuntime.AsciiHue(SpellHue);
        foreach (var (_, inter, color) in labelsQ)
            color.Ref.Value = inter.Ref == Interaction.Hovered || inter.Ref == Interaction.Pressed ? over : normal;
    }

    private static void Despawn(
        Commands commands,
        Query<Data<SpellbookWindow>> windowsQ,
        Query<Data<SpellCastButton>> castButtonsQ)
    {
        foreach (var (ent, _) in windowsQ)
            commands.Entity(ent.Ref).Despawn();
        foreach (var (ent, _) in castButtonsQ)
            commands.Entity(ent.Ref).Despawn();
    }
}
