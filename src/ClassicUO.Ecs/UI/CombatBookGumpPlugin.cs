// CombatBookGump — ECS port of Game/UI/Gumps/CombatBookGump.cs.
//
// A paged book (bg 0x2B02, page corners 0x08BB/0x08BC), structurally the same
// as the spellbook: index pages list every weapon special-move name (click to
// jump to its detail page), and per-ability detail pages show the move icon,
// name and the weapons that grant it. The index pages also carry the player's
// current primary/secondary ability picker — double-click an icon to use the
// move, or drag it off to spawn a floating UseAbilityButton.
//
// The ability data layer (which moves the equipped weapon grants, and whether a
// move is queued) lives in PlayerAbilities, refreshed from the weapon graphic on
// every equipment change. Using a move toggles the queued (0x80) bit and sends
// the modern 0xD7 special-move packet (or pre-AOS stun/disarm). The icon tints
// hue 38 while queued, mirroring legacy.

using System;
using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.Game.Data;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using ClayColor = Clay.Color;
using GameLayer = ClassicUO.Game.Data.Layer;

namespace ClassicUO.Ecs;

// Player's current weapon special moves. The low 7 bits are the ability index
// (1-based, into AbilityData.Abilities); 0x80 = the move is queued. Mirrors
// legacy PlayerMobile.Abilities[0/1].
internal sealed class PlayerAbilities
{
    public byte Primary;
    public byte Secondary;
}

internal struct CombatBookWindow
{
    public int Page;        // 1-based active page (legacy ActivePage)
    public int PageCount;
    public int DictPages;   // leading index pages
    public int AbilityCount;
    public bool Dirty;
    public ulong Content, Left, Right;
}

internal struct CombatBookCorner { public ulong Window; public int Dir; }
internal struct CombatNameLabel; // hover recolour
// Picker icon on an index page, or a floating button — both show the current
// primary/secondary ability icon, tint while queued, use on double-click.
internal struct CombatAbilityIcon { public bool Primary; public bool Floating; }

internal readonly struct CombatBookGumpPlugin : IPlugin
{
    private const ushort Bg = 0x2B02;
    private const ushort LeftCorner = 0x08BB, RightCorner = 0x08BC;
    private const ushort TextHue = 0x0288, OverHue = 0x0033;
    private const ushort AbilityIconBase = 0x5200;
    private const ushort QueuedHue = 38;
    private const int NamesLeft = 9, NamesRight = 4, NamesPerPage = NamesLeft + NamesRight;

    public void Build(App app)
    {
        // Seed with the default special moves (legacy DefaultItemAbilities) so
        // the picker shows icons even before the first equipment change / when
        // unarmed; the equip observer overrides from the weapon.
        app.AddResource(new PlayerAbilities
        {
            Primary = (byte)AbilityData.DefaultItemAbilities.First,
            Secondary = (byte)AbilityData.DefaultItemAbilities.Second,
        });

        // Refresh primary/secondary from the equipped weapon graphic whenever the
        // player's equipment changes (legacy PlayerMobile.UpdateAbilities). Mark
        // any open book dirty so its picker icons rebuild.
        app.AddObserver((
            OnInsert<EquipmentSlots> trig,
            Res<UOFileManager> files,
            ResMut<PlayerAbilities> abil,
            Query<Data<Player>> playerQ,
            Query<Data<Graphic>> graphicQ,
            Query<Data<CombatBookWindow>> booksQ) =>
        {
            if (!playerQ.Contains(trig.EntityId)) return;

            var slots = trig.Component;
            ulong w = slots[GameLayer.OneHanded];
            if (w == 0) w = slots[GameLayer.TwoHanded];
            ushort wg = 0;
            if (w != 0 && graphicQ.TryGet(w, out var gRow)) { var (_, g) = gRow; wg = g.Ref.Value; }

            Resolve(wg, files.Value.TileData.StaticData, out var p, out var s);
            abil.Value.Primary = (byte)p;
            abil.Value.Secondary = (byte)s;

            foreach (var (_, b) in booksQ) b.Ref.Dirty = true;
        });

        // Server cleared the queued move (0xBF 0x21) — drop the 0x80 bit so the
        // picker / floating button un-tint (SyncQueuedHue picks it up next frame).
        app.AddObserver((
            On<PacketReceived<OnExtendedCommandPacket_0xBF>> trig,
            ResMut<PlayerAbilities> abil) =>
        {
            if (!trig.Event.Packet.ClearAbilities) return;
            abil.Value.Primary &= 0x7F;
            abil.Value.Secondary &= 0x7F;
        });

        var rebuildFn = RebuildContent;
        app.AddSystem(rebuildFn).InStage(Stage.Update)
            .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen).Build();

        var hueFn = SyncQueuedHue;
        app.AddSystem(hueFn).InStage(Stage.Update)
            .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen).Build();

        var hoverFn = NameHover;
        app.AddSystem(hoverFn).InStage(Stage.Update)
            .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen).Build();

        var navFn = Nav;
        app.AddSystem(navFn).InStage(Stage.PreUpdate).Build();

        var dragFn = IconDragSystem;
        app.AddSystem(dragFn).InStage(Stage.PreUpdate).Build();

        var despawnFn = Despawn;
        app.AddSystem(despawnFn).OnExit(GameState.GameScreen).Build();

#if AGENT_BUILD
        app.AddResource(new DebugCombatBookQueue());
        var drainFn = DrainDebug;
        app.AddSystem(drainFn).InStage(Stage.First).Build();
#endif
    }

    // Weapon graphic -> primary/secondary. Default Paralyzing/Disarm; weapon
    // graphic (or its AnimID neighbour) overrides. Mirrors UpdateAbilities.
    private static void Resolve(ushort wg, StaticTiles[] tileData, out Ability primary, out Ability secondary)
    {
        var def = AbilityData.DefaultItemAbilities;
        primary = def.First;
        secondary = def.Second;
        if (wg == 0) return;

        ushort animId = wg < tileData.Length ? tileData[wg].AnimID : (ushort)0;
        ushort animGraphic = 0;
        if (wg - 1 >= 0 && wg - 1 < tileData.Length && tileData[wg - 1].AnimID == animId)
            animGraphic = (ushort)(wg - 1);
        else if (wg + 1 < tileData.Length && tileData[wg + 1].AnimID == animId)
            animGraphic = (ushort)(wg + 1);

        if (AbilityData.GraphicToAbilitiesMap.TryGetValue(wg, out var ab)
            || AbilityData.GraphicToAbilitiesMap.TryGetValue(animGraphic, out ab))
        {
            primary = ab.First;
            secondary = ab.Second;
        }
    }

    // Open (or focus) the combat book. No server packet — legacy opens it from a
    // macro / hotkey; this is the shared entry point (also driven by the harness).
    public static void OpenOrFocus(
        Commands commands, GumpBuilder builder, AssetsServer assets, UiZCounter zCounter, GameContext gameCtx,
        Query<Data<CombatBookWindow>> existingQ)
    {
        foreach (var (ent, _) in existingQ)
        {
            commands.Entity(ent.Ref).Insert(new GlobalZIndex(zCounter.Bump()));
            return;
        }

        int abilityCount = 32, dictPages = 3;
        if (gameCtx.ClientVersion < ClientVersion.CV_7000)
        {
            if (gameCtx.ClientVersion < ClientVersion.CV_500A) abilityCount = 29;
            else { abilityCount = 13; dictPages = 1; }
        }

        var root = builder.SpawnUOGump(commands, Bg, Vector3.UnitZ, new Vector2(100, 100), zCounter);

        var content = commands.Spawn()
            .Insert(new Node { PositionType = PositionType.Absolute, Left = Val.Px(0), Top = Val.Px(0), Width = Val.Px(GumpW(assets, Bg)), Height = Val.Px(GumpH(assets, Bg)) });
        commands.AddChild(root.Id, content.Id);

        var left = builder.AddGump(commands, LeftCorner, Vector3.UnitZ, new Vector2(50, 8))
            .Insert<UiNoWindowDrag>().Insert(new CombatBookCorner { Window = root.Id, Dir = -1 });
        commands.AddChild(root.Id, left.Id);
        var right = builder.AddGump(commands, RightCorner, Vector3.UnitZ, new Vector2(321, 8))
            .Insert<UiNoWindowDrag>().Insert(new CombatBookCorner { Window = root.Id, Dir = 1 });
        commands.AddChild(root.Id, right.Id);

        commands.Entity(root.Id).Insert(new CombatBookWindow
        {
            Page = 1,
            DictPages = dictPages,
            AbilityCount = abilityCount,
            PageCount = dictPages + abilityCount,
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
        Res<AssetsServer> assets,
        Res<UOFileManager> files,
        Res<GumpBuilder> builder,
        Res<PlayerAbilities> abil,
        Query<Data<CombatBookWindow>> windowsQ,
        Query<Data<TinyEcs.Children>> childrenQ)
    {
        foreach (var (_, win) in windowsQ)
        {
            if (!win.Ref.Dirty) continue;
            win.Ref.Dirty = false;
            win.Ref.Page = Math.Clamp(win.Ref.Page, 1, Math.Max(1, win.Ref.PageCount));

            var content = win.Ref.Content;
            if (childrenQ.TryGet(content, out var contentRow))
            {
                var (_, kids) = contentRow;
                foreach (var cid in kids.Ref) commands.Entity(cid).Despawn();
            }

            int page = win.Ref.Page;
            int dictPages = win.Ref.DictPages;
            int abilityCount = win.Ref.AbilityCount;

            if (page <= dictPages)
                BuildIndexPage(commands, builder.Value, content, page, dictPages, abilityCount, abil.Value);
            else
                BuildDetailPage(commands, builder.Value, assets.Value, files.Value.TileData.StaticData, content, page - dictPages - 1);
        }
    }

    private static void BuildIndexPage(
        Commands commands, GumpBuilder builder, ulong content,
        int page, int dictPages, int abilityCount, PlayerAbilities abil)
    {
        int baseOffs = (page - 1) * NamesPerPage;

        // Left column: "Index" + up to 9 names.
        AddText(commands, content, "Index", 6, 96, 6);
        for (int k = 0; k < NamesLeft; k++)
        {
            int offs = baseOffs + k;
            if (offs >= abilityCount || offs >= AbilityData.Abilities.Length) break;
            AddNameLabel(commands, content, offs, dictPages, 52, 42 + k * 15);
        }

        // Right column: "Index" + up to 4 names, then the primary/secondary picker.
        AddText(commands, content, "Index", 6, 259, 6);
        for (int k = 0; k < NamesRight; k++)
        {
            int offs = baseOffs + NamesLeft + k;
            if (offs >= abilityCount || offs >= AbilityData.Abilities.Length) break;
            AddNameLabel(commands, content, offs, dictPages, 215, 42 + k * 15);
        }

        AddPicker(commands, builder, content, primary: true, abil.Primary, 215, 105, 265);
        AddPicker(commands, builder, content, primary: false, abil.Secondary, 215, 150, 265);
    }

    private static void AddPicker(
        Commands commands, GumpBuilder builder, ulong content,
        bool primary, byte ability, int iconX, int iconY, int labelX)
    {
        int idx = (ability & 0x7F);
        if (idx >= 1 && idx <= AbilityData.Abilities.Length)
        {
            var icon = builder.AddGump(commands, (ushort)(AbilityIconBase + (idx - 1)), Vector3.UnitZ, new Vector2(iconX, iconY))
                .Insert(Interaction.None).Insert<UiNoWindowDrag>().Insert<UiContainsByBounds>()
                .Insert(new CombatAbilityIcon { Primary = primary, Floating = false });
            icon.Observe((On<UiDoubleClick> _, Res<NetClient> net, ResMut<PlayerAbilities> a, Res<GameContext> ctx) =>
                Use(net.Value, a.Value, ctx.Value, primary));
            commands.AddChild(content, icon.Id);
        }

        // Wrapped at width 80 (legacy Label maxWidth 80) so "Primary Ability
        // Icon" breaks to two lines like the original instead of overflowing.
        AddWrapped(commands, content, primary ? "Primary Ability Icon" : "Secondary Ability Icon", 6, TextHue, labelX, iconY, 80);
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

    private static void BuildDetailPage(
        Commands commands, GumpBuilder builder, AssetsServer assets, StaticTiles[] tileData, ulong content, int abilityIndex)
    {
        if (abilityIndex < 0 || abilityIndex >= AbilityData.Abilities.Length) return;
        ref readonly var def = ref AbilityData.Abilities[abilityIndex];

        var icon = builder.AddGump(commands, (ushort)(AbilityIconBase + abilityIndex), Vector3.UnitZ, new Vector2(62, 40))
            .Insert(Interaction.None).Insert<UiNoWindowDrag>();
        commands.AddChild(content, icon.Id);

        // Ability name wraps at width 80 (legacy Label maxWidth 80, font 6) —
        // long names like "Whirlwind Attack" break to two lines.
        AddWrapped(commands, content, CapWords(def.Name), 6, TextHue, 110, 34, 80);

        var div = builder.AddGumpTiled(commands, 0x0835, Vector3.UnitZ, new Vector2(62, 88), new Vector2(128, 5));
        commands.AddChild(content, div.Id);

        // The weapons that grant this move — item names from tiledata, two
        // columns (legacy GetItemsList + StaticData[id].Name).
        var list = GetItemsList((byte)abilityIndex);
        int textX = 62, textY = 98;
        for (int j = 0; j < list.Length; j++)
        {
            if (j == 6) { textX = 215; textY = 34; }
            ushort id = list[j];
            if (id >= tileData.Length) continue;
            AddText(commands, content, CapWords(tileData[id].Name ?? string.Empty), 9, textX, textY);
            textY += 16;
        }
    }

    private static void AddNameLabel(Commands commands, ulong content, int offs, int dictPages, int x, int y)
    {
        ref readonly var def = ref AbilityData.Abilities[offs];
        int targetPage = dictPages + offs + 1; // jump to this ability's detail page
        var lbl = commands.Spawn()
            .Insert(new Node { PositionType = PositionType.Absolute, Left = Val.Px(x), Top = Val.Px(y), Width = Val.Px(150), Height = Val.Auto })
            .Insert(new Text(def.Name))
            .Insert(new TextFont { FontId = (ushort)(9 | UoFontRuntime.AsciiFlag), Size = 12 })
            .Insert(new TextColor(UoFontRuntime.AsciiHue(TextHue)))
            .Insert(new UiCustom { Data = new UOCustomRender { Kind = UOCustomKind.None, Hue = Vector3.UnitZ } })
            .Insert(Interaction.None).Insert<UiNoWindowDrag>().Insert<UiContainsByBounds>()
            .Insert<CombatNameLabel>();
        lbl.Observe((On<UiClick> _, Query<Data<CombatBookWindow>> wq) =>
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

    // Tint picker/floating icons hue 38 while their move is queued (0x80 bit).
    private static void SyncQueuedHue(
        Res<PlayerAbilities> abil,
        Query<Data<CombatAbilityIcon, UiCustom>> iconsQ)
    {
        foreach (var (_, ic, custom) in iconsQ)
        {
            bool queued = ((ic.Ref.Primary ? abil.Value.Primary : abil.Value.Secondary) & 0x80) != 0;
            custom.Ref.Render().Hue = queued
                ? ShaderHueTranslator.GetHueVector(QueuedHue, false, 1f, true)
                : Vector3.UnitZ;
        }
    }

    private static void NameHover(Query<Data<Interaction, TextColor>, Filter<With<CombatNameLabel>>> labelsQ)
    {
        var over = UoFontRuntime.AsciiHue(OverHue);
        var normal = UoFontRuntime.AsciiHue(TextHue);
        foreach (var (_, inter, color) in labelsQ)
            color.Ref.Value = inter.Ref == Interaction.Hovered || inter.Ref == Interaction.Pressed ? over : normal;
    }

    // Toggle the queued bit + send the special-move packet. Mirrors legacy
    // UsePrimaryAbility / UseSecondaryAbility + SendAbility.
    private static void Use(NetClient net, PlayerAbilities abil, GameContext ctx, bool primary)
    {
        ref byte ability = ref (primary ? ref abil.Primary : ref abil.Secondary);
        if ((ability & 0x80) == 0)
        {
            abil.Primary &= 0x7F;
            abil.Secondary &= 0x7F;
            SendAbility(net, ctx, (byte)(ability & 0x7F), primary);
        }
        else
        {
            SendAbility(net, ctx, 0, true);
        }
        ability ^= 0x80;
    }

    private static void SendAbility(NetClient net, GameContext ctx, byte idx, bool primary)
    {
        if ((ctx.LockedFeatures & LockedFeatureFlags.AOS) == 0)
        {
            if (primary) net.Send_StunRequest();
            else net.Send_DisarmRequest();
        }
        else
        {
            net.Send_UseCombatAbility(ctx.PlayerSerial, idx);
        }
    }

    private static void Nav(
        Res<MouseContext> mouse,
        Res<DragGate> gate,
        Local<bool> claimed,
        Query<Data<ComputedNode, CombatBookCorner>> cornersQ,
        Query<Data<CombatBookWindow>> windowsQ)
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
            if (windowsQ.TryGet(c.Ref.Window, out var windowRow))
            {
                var (_, w) = windowRow;
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

    private struct DragAnchor { public bool Active, Claimed, Spawned; public bool Primary; public byte Ability; public Vector2 Start; }

    // Drag a picker icon off the book to spawn a floating UseAbilityButton that
    // rides the cursor (legacy OnGumpicDragBegin* -> UseAbilityButtonGump). One
    // button per side; double-click it to use the move.
    private static void IconDragSystem(
        Res<MouseContext> mouse,
        Res<DragGate> gate,
        Res<ForcedWindowDrag> forced,
        Res<GumpBuilder> builder,
        Res<UiZCounter> z,
        Res<PlayerAbilities> abil,
        Commands commands,
        Local<DragAnchor> anchor,
        Query<Data<ComputedNode, CombatAbilityIcon>> iconsQ,
        Query<Data<CombatAbilityIcon>> existingQ)
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
                anchor.Value.Primary = ic.Ref.Primary;
                anchor.Value.Ability = (byte)((ic.Ref.Primary ? abil.Value.Primary : abil.Value.Secondary) & 0x7F);
                anchor.Value.Start = pos;
                gate.Value.Mode = ActiveDrag.UIWindow;
                break;
            }
        }

        if (anchor.Value.Active && !anchor.Value.Spawned
            && (mouse.Value.Position - anchor.Value.Start).Length() > 6f)
        {
            int idx = anchor.Value.Ability;
            if (idx < 1 || idx > AbilityData.Abilities.Length) { anchor.Value.Spawned = true; anchor.Value.Claimed = false; gate.Value.Mode = ActiveDrag.None; return; }

            bool primary = anchor.Value.Primary;
            foreach (var (ent, b) in existingQ)
                if (b.Ref.Floating && b.Ref.Primary == primary) commands.Entity(ent.Ref).Despawn();

            var pos = mouse.Value.Position;
            ushort iconGfx = (ushort)(AbilityIconBase + (idx - 1));
            var fb = builder.Value.SpawnUOGump(commands, iconGfx, Vector3.UnitZ, new Vector2(pos.X - 22, pos.Y - 22), z.Value)
                .Insert(new CombatAbilityIcon { Primary = primary, Floating = true });
            fb.Observe((On<UiDoubleClick> _, Res<NetClient> net, ResMut<PlayerAbilities> a, Res<GameContext> ctx) =>
                Use(net.Value, a.Value, ctx.Value, primary));

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
        Query<Data<CombatBookWindow>> windowsQ,
        Query<Data<CombatAbilityIcon>, Filter<With<UiMovable>>> floatingQ,
        Query<Data<TinyEcs.Children>> childrenQ)
    {
        foreach (var (ent, _) in windowsQ) DespawnSubtree(commands, ent.Ref, childrenQ);
        foreach (var (ent, _) in floatingQ) DespawnSubtree(commands, ent.Ref, childrenQ);
    }

    private static void DespawnSubtree(Commands commands, ulong e, Query<Data<TinyEcs.Children>> childrenQ)
    {
        if (childrenQ.TryGet(e, out var childrenRow))
        {
            var (_, kids) = childrenRow;
            foreach (var cid in kids.Ref) DespawnSubtree(commands, cid, childrenQ);
        }
        commands.Entity(e).Despawn();
    }

    private static string CapWords(string s) => StringHelper.CapitalizeAllWords(s);

    // Weapon item graphics that grant ability `index` (legacy GetItemsList).
    private static ushort[] GetItemsList(byte index) => index switch
    {
        0 => new ushort[] { 3908, 5048, 3935, 5119, 9927, 5181, 5040, 5121, 3939, 9932, 11554, 16497, 16502, 16494, 16491 },
        1 => new ushort[] { 3779, 5115, 3912, 3910, 5185, 9924, 5127, 5040, 3720, 5125, 11552, 16499, 16498 },
        2 => new ushort[] { 5048, 3912, 5183, 5179, 3933, 5113, 3722, 9930, 3920, 11556, 16487, 16500 },
        3 => new ushort[] { 5050, 3914, 3935, 3714, 5092, 5179, 5127, 5177, 9926, 4021, 10146, 11556, 11560, 5109, 16500, 16495 },
        4 => new ushort[] { 5111, 3718, 3781, 3908, 3573, 3714, 3933, 5125, 11558, 11560, 5109, 9934, 16493, 16494 },
        5 => new ushort[] { 3918, 3914, 9927, 3573, 5044, 3720, 9930, 5117, 16501, 16495 },
        6 => new ushort[] { 3718, 5187, 3916, 5046, 5119, 9931, 3722, 9929, 9933, 10148, 10153, 16488, 16493, 16496 },
        7 => new ushort[] { 5111, 3779, 3922, 9928, 5121, 9929, 11553, 16490, 16488 },
        8 => new ushort[] { 3910, 9925, 9931, 5181, 9926, 5123, 3920, 5042, 16499, 16502, 16496, 16491 },
        9 => new ushort[] { 5117, 9932, 9933, 16492 },
        10 => new ushort[] { 5050, 3918, 5046, 9924, 9925, 5113, 3569, 9928, 3939, 5042, 16497, 16498 },
        11 => new ushort[] { 3781, 5187, 5185, 5092, 5044, 3922, 5123, 4021, 11553, 16490 },
        12 => new ushort[] { 5115, 5183, 3916, 5177, 3569, 10157, 11559, 9934, 16501 },
        13 => new ushort[] { 10146 },
        14 => new ushort[] { 10148, 10150, 10151 },
        15 => new ushort[] { 10147, 10158, 10159, 11557 },
        16 => new ushort[] { 10151, 10157, 11561 },
        17 => new ushort[] { 10152 },
        18 or 20 => new ushort[] { 10155 },
        19 => new ushort[] { 10152, 10153, 10158, 11554 },
        21 => new ushort[] { 10149 },
        22 => new ushort[] { 10149, 10159 },
        23 => new ushort[] { 11555, 11558, 11559, 11561 },
        24 or 27 => new ushort[] { 11550 },
        25 => new ushort[] { 11551 },
        26 => new ushort[] { 11551, 11552 },
        28 => new ushort[] { 11557 },
        29 => new ushort[] { 16492 },
        30 => new ushort[] { 16487 },
        _ => Array.Empty<ushort>(),
    };

#if AGENT_BUILD
    private static void DrainDebug(
        Commands commands,
        ResMut<DebugCombatBookQueue> q,
        Res<GumpBuilder> builder,
        Res<AssetsServer> assets,
        Res<UiZCounter> zCounter,
        Res<GameContext> gameCtx,
        ResMut<PlayerAbilities> abil,
        Query<Data<CombatBookWindow>> existingQ)
    {
        if (!q.Value.OpenRequested) return;
        q.Value.OpenRequested = false;
        // Seed sample abilities so the picker shows icons without a real weapon.
        if (abil.Value.Primary == 0) abil.Value.Primary = (byte)Ability.ParalyzingBlow;
        if (abil.Value.Secondary == 0) abil.Value.Secondary = (byte)Ability.Disarm;
        OpenOrFocus(commands, builder.Value, assets.Value, zCounter.Value, gameCtx.Value, existingQ);
    }
#endif
}

#if AGENT_BUILD
internal sealed class DebugCombatBookQueue { public bool OpenRequested; }
#endif
