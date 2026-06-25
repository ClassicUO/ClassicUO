// Bevy.UI port of legacy Game/UI/Gumps/MenuGump.cs (the old packet-0x7C menus).
//
// 0x7C carries two shapes, distinguished by the first entry's leading u16
// (legacy peeks it as `menuid`):
//   * != 0  -> ICON menu (MenuGump): bg gump 0x0910, a horizontally scrollable
//     strip of item-art icons; double-click an icon to reply with its index.
//   * == 0  -> GRAY menu (GrayMenuGump): a resizepic with a radio-button list
//     and OK / Cancel buttons; reply with the selected radio index.
//
// Reply (0x7D) is sent on selection (Send_MenuResponse / Send_GrayMenuResponse)
// and a cancel (index 0) is sent on right-click close. The OnRemove observer
// only sends the cancel when the window was NOT already answered — a flag set
// on the root before a selection despawn prevents a double reply.

using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.Network;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using TinyEcs.Bevy.UI.Widgets;
using ClayColor = Clay.Color;

namespace ClassicUO.Ecs;

// Window root marker. ServerMenuId is the 0x7C gump id echoed back in the reply.
// cuo:modding contract type — do not merge/rename (queried by WIT path).
internal struct MenuGumpWindow
{
    public uint Serial;
    public ushort ServerMenuId;
    public bool IsGray;
    // Set true right before a selection/cancel despawn so the OnRemove cancel
    // path doesn't fire a second (cancel) reply on top of the real answer.
    public bool Responded;
}

internal struct MenuIconItem
{
    public uint Serial;
    public ushort ServerMenuId;
    public int Index;
    public ushort Graphic;
    public ushort Hue;
}

// One radio row in a gray menu. Index is the 1-based reply index.
internal struct MenuRadio
{
    public ulong Root;
    public ushort Index;
}

// Horizontal-scroll strip of a gray... no: the icon menu strip. Bound to the
// HSlider so the slider drives ScrollPosition.OffsetX.
internal struct MenuIconStrip { public ulong Root; }

internal readonly struct MenuGumpPlugin : IPlugin
{
    private const ushort IconBg = 0x0910;
    private const ushort GrayBg = 0x13EC;
    private const ushort RadioOff = 0x138A, RadioOn = 0x138B;
    private const ushort TitleHue = 0x0386;
    private const int StripX = 40, StripY = 42, StripW = 217, StripH = 49;

    public void Build(App app)
    {
        app.AddObserver((
            On<PacketReceived<OnOpenMenuPacket_0x7C>> trig,
            Commands commands,
            Res<GumpBuilder> builder,
            Res<AssetsServer> assets,
            Res<UiZCounter> zCounter) =>
        {
            var p = trig.Event.Packet;
            // Legacy discriminator: the first entry's leading u16 (graphic for
            // an icon menu, 0 for a gray menu).
            bool icon = p.EntryCount > 0 && p.Entries[0].MenuId != 0;
            if (icon)
                SpawnIconMenu(commands, builder.Value, assets.Value, zCounter.Value, p);
            else
                SpawnGrayMenu(commands, builder.Value, assets.Value, zCounter.Value, p);
        });

        // Radio exclusivity: selecting one clears the rest of its group; a
        // re-click can't deselect (legacy radio always keeps one chosen).
        app.AddObserver((
            On<CheckboxChanged> trig,
            Query<Data<MenuRadio, Checkbox>> radiosQ) =>
        {
            if (!radiosQ.TryGet(trig.EntityId, out var radioRow)) return;
            var (_, clicked, clickedCb) = radioRow;
            clickedCb.Ref.Checked = true;
            var root = clicked.Ref.Root;
            foreach (var (ent, r, cb) in radiosQ)
            {
                if (r.Ref.Root != root) continue;
                if (ent.Ref != trig.EntityId)
                    cb.Ref.Checked = false;
            }
        });

        // Icon double-click -> reply with the item index.
        app.AddObserver((
            On<UiDoubleClick> trig,
            Commands commands,
            Res<NetClient> net,
            Query<Data<MenuIconItem>> itemQ,
            Query<Data<MenuGumpWindow>> winQ) =>
        {
            if (!itemQ.TryGet(trig.EntityId, out var itemRow)) return;
            var (_, it) = itemRow;
            net.Value.Send_MenuResponse(it.Ref.Serial, it.Ref.ServerMenuId, it.Ref.Index, it.Ref.Graphic, it.Ref.Hue);
            CloseAnswered(commands, it.Ref.Serial, winQ);
        });

        // Right-click close (WindowDragPlugin despawns the UiMovable root) -> the
        // cancel reply, unless a selection already answered.
        app.AddObserver((
            OnRemove<MenuGumpWindow> trig,
            Res<NetClient> net) =>
        {
            if (trig.Component.Responded) return;
            if (trig.Component.IsGray)
                net.Value.Send_GrayMenuResponse(trig.Component.Serial, trig.Component.ServerMenuId, 0);
            else
                net.Value.Send_MenuResponse(trig.Component.Serial, trig.Component.ServerMenuId, 0, 0, 0);
        });

        // Drive the icon strip's horizontal scroll from the slider.
        // (Registered per-slider via Observe in SpawnIconMenu.)

        var despawnFn = DisposeOnLogout;
        app.AddSystem(despawnFn).OnExit(GameState.GameScreen).Build();

#if AGENT_BUILD
        app.AddResource(new DebugMenuQueue());
        var drainFn = DrainDebugMenu;
        app.AddSystem(drainFn).InStage(Stage.First).Build();
#endif
    }

    private static void SpawnIconMenu(
        Commands commands, GumpBuilder builder, AssetsServer assets, UiZCounter zCounter,
        OnOpenMenuPacket_0x7C p)
    {
        var root = builder.SpawnUOGump(commands, IconBg, Vector3.UnitZ, new Vector2(100, 100), zCounter)
            .Insert(new MenuGumpWindow { Serial = p.Serial, ServerMenuId = p.MenuId, IsGray = false });
        var rootId = root.Id;

        AddLabel(commands, rootId, p.Name, new Vector2(39, 18), 200, TitleHue);

        // Scrollable strip: a flex row of icons clipped to 217x49; OffsetX driven
        // by the slider below. Dark fill mirrors the legacy ColorBox backdrop.
        var strip = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(StripX), Top = Val.Px(StripY),
                Width = Val.Px(StripW), Height = Val.Px(StripH),
                FlexDirection = FlexDirection.Row,
                AlignItems = AlignItems.Center,
                Overflow = Overflow.Scroll,
            })
            .Insert(new BackgroundColor(new ClayColor(0, 0, 0, 200)))
            .Insert(new ScrollPosition())
            .Insert(new MenuIconStrip { Root = rootId })
            .Insert<UiNoWindowDrag>();
        var stripId = strip.Id;
        commands.AddChild(rootId, stripId);

        int total = 0;
        for (int i = 0; i < p.Entries.Count; i++)
        {
            var e = p.Entries[i];
            ushort graphic = e.MenuId; // first u16 = item graphic in an icon menu
            ref readonly var art = ref assets.Arts.GetArt(graphic);
            if (art.UV.Width == 0 || art.UV.Height == 0)
                continue;

            var icon = builder.AddArt(commands, graphic, ShaderHueTranslator.GetHueVector(e.Hue, false, 1f, false))
                .Insert(Interaction.None)
                .Insert<UiNoWindowDrag>()
                .Insert(new MenuIconItem
                {
                    Serial = p.Serial,
                    ServerMenuId = p.MenuId,
                    Index = i + 1,
                    Graphic = graphic,
                    Hue = e.Hue,
                });
            commands.AddChild(stripId, icon.Id);
            total += art.UV.Width;
        }

        int maxScroll = total - StripW;
        if (maxScroll < 0) maxScroll = 0;

        // Slider under the strip (legacy HSliderBar at y = strip bottom + 12).
        var slider = builder.AddHSlider(commands, 0, maxScroll, 0,
            new Vector2(StripX, StripY + StripH + 12), StripW, thumbGump: GumpBuilder.SliderKnob);
        slider.Observe((On<SliderChanged> t,
                        Query<Data<ScrollPosition>> scrollQ) =>
        {
            if (!scrollQ.TryGet(stripId, out var scrollRow)) return;
            var (_, sp) = scrollRow;
            sp.Ref.OffsetX = t.Event.Value;
        });
        commands.AddChild(rootId, slider.Id);
    }

    private static void SpawnGrayMenu(
        Commands commands, GumpBuilder builder, AssetsServer assets, UiZCounter zCounter,
        OnOpenMenuPacket_0x7C p)
    {
        const int width = 400;
        const int titleY = 16, firstRowY = 40, rowStep = 22;

        int rows = p.Entries.Count;
        int buttonsY = firstRowY + rows * rowStep + 10;
        int height = buttonsY + 40;

        var rootCmd = commands.Spawn().Insert(new MenuGumpWindow
        {
            Serial = p.Serial, ServerMenuId = p.MenuId, IsGray = true
        });
        var rootId = rootCmd.Id;

        var bg = builder.AddGumpNinePatch(commands, GrayBg, Vector3.UnitZ, new Vector2(0, 0), new Vector2(width, height));
        commands.AddChild(rootId, bg.Id);

        AddLabel(commands, rootId, p.Name, new Vector2(20, titleY), 370, TitleHue);

        for (int i = 0; i < rows; i++)
        {
            int y = firstRowY + i * rowStep;
            var radio = builder.AddCheckbox(commands, false, new Vector2(50, y), off: RadioOff, on: RadioOn, hue: Vector3.UnitZ)
                .Insert<UiNoWindowDrag>()
                .Insert(new MenuRadio { Root = rootId, Index = (ushort)(i + 1) });
            commands.AddChild(rootId, radio.Id);

            AddLabel(commands, rootId, p.Entries[i].Response, new Vector2(75, y), 320, TitleHue);
        }

        // Cancel (0x1450) at x70, Continue (0x13B2) at x200 — legacy button order.
        var serial = p.Serial; var menuId = p.MenuId;
        var cancel = builder.AddButton(commands, (0x1450, 0x1451, 0x1450), Vector3.UnitZ, new Vector2(70, buttonsY))
            .Insert<UiNoWindowDrag>();
        cancel.Observe((On<UiClick> _, Commands cmd, Res<NetClient> net,
                        Query<Data<MenuGumpWindow>> winQ) =>
        {
            net.Value.Send_GrayMenuResponse(serial, menuId, 0);
            CloseAnswered(cmd, serial, winQ);
        });
        commands.AddChild(rootId, cancel.Id);

        var cont = builder.AddButton(commands, (0x13B2, 0x13B3, 0x13B2), Vector3.UnitZ, new Vector2(200, buttonsY))
            .Insert<UiNoWindowDrag>();
        cont.Observe((On<UiClick> _, Commands cmd, Res<NetClient> net,
                      Query<Data<MenuRadio, Checkbox>> radiosQ,
                      Query<Data<MenuGumpWindow>> winQ) =>
        {
            ushort chosen = 0;
            foreach (var (_, r, cb) in radiosQ)
                if (r.Ref.Root == rootId && cb.Ref.Checked) { chosen = r.Ref.Index; break; }
            if (chosen == 0) return; // legacy: nothing selected -> no-op
            net.Value.Send_GrayMenuResponse(serial, menuId, chosen);
            CloseAnswered(cmd, serial, winQ);
        });
        commands.AddChild(rootId, cont.Id);

        commands.Entity(rootId)
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(200), Top = Val.Px(100),
                Width = Val.Px(width), Height = Val.Px(height),
            })
            .Insert<UiMovable>()
            .Insert(new GlobalZIndex(zCounter.Bump()));
    }

    // Mark the window answered (so OnRemove skips the cancel reply) and despawn
    // its subtree.
    private static void CloseAnswered(
        Commands commands, uint serial,
        Query<Data<MenuGumpWindow>> winQ)
    {
        foreach (var (ent, win) in winQ)
        {
            if (win.Ref.Serial != serial) continue;
            win.Ref.Responded = true; // in-place; visible to OnRemove this frame
            commands.Entity(ent.Ref).Despawn();
            break;
        }
    }

    private static void AddLabel(Commands commands, ulong parent, string text, Vector2 pos, int width, ushort hue)
    {
        var ent = commands.Spawn()
            .Insert(new Node
            {
                PositionType = PositionType.Absolute,
                Left = Val.Px(pos.X), Top = Val.Px(pos.Y),
                Width = Val.Px(width), Height = Val.Px(20),
            })
            .Insert(new UiCustom
            {
                Data = new UOCustomRender
                {
                    Kind = UOCustomKind.WrappedText,
                    Hue = Vector3.UnitZ,
                    Text = text ?? string.Empty,
                    TextFont = 1,
                    TextHue = hue,
                    WrapWidth = width,
                    IsHtml = false,
                    TextAscii = true,
                }
            });
        commands.AddChild(parent, ent.Id);
    }

    private static void DisposeOnLogout(
        Commands commands,
        Query<Data<MenuGumpWindow>> winQ)
    {
        // Mark answered so the OnRemove cancel reply doesn't fire during teardown
        // (the socket is already disconnecting).
        foreach (var (ent, win) in winQ)
        {
            win.Ref.Responded = true;
            commands.Entity(ent.Ref).Despawn();
        }
    }

#if AGENT_BUILD
    private static void DrainDebugMenu(Commands commands, ResMut<DebugMenuQueue> q)
    {
        foreach (var pkt in q.Value.Pending)
            commands.EmitTrigger(new PacketReceived<OnOpenMenuPacket_0x7C> { Packet = pkt });
        q.Value.Pending.Clear();
    }
#endif
}

#if AGENT_BUILD
internal sealed class DebugMenuQueue
{
    public readonly List<OnOpenMenuPacket_0x7C> Pending = new();
}
#endif
