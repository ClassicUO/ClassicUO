// Paperdoll packet handling + minimal paperdoll gump UI.
//
// On OnOpenPaperdollPacket_0x88 (0x88, server's reply to Send_DoubleClick
// on a mobile), spawn a draggable gump entity with the paperdoll
// background sprite (0x07D0 for the player, 0x07D1 for others) and the
// mobile's title rendered on top.
//
// Equipment slots, virtue button, profile button, party manifest, and
// the full set of legacy interactions are NOT ported yet — this is the
// minimum that proves a double-click on a mobile produces a visible
// paperdoll on impl/ecs the same way it does on main.

using ClassicUO.Assets;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using ClayColor = Clay.Color;

namespace ClassicUO.Ecs;

internal readonly struct PaperdollPlugin : IPlugin
{
    private static readonly object CustomMarker = new();

    public void Build(App app)
    {
        var processFn = ProcessPaperdollPackets;

        app
            .AddSystem(processFn)
            .InStage(Stage.Update)
            .RunIf((EventReader<IPacket> reader) => reader.HasEvents)
            .Build();
    }

    private static void ProcessPaperdollPackets(
        Commands commands,
        Res<AssetsServer> assets,
        Res<GameContext> gameCtx,
        EventReader<IPacket> packets)
    {
        foreach (var packet in packets.Read())
        {
            if (packet is not OnOpenPaperdollPacket_0x88 pd) continue;

            // 0x07D0 for the local player, 0x07D1 for everyone else
            // (matches PaperDollGump.cs line 66 on the legacy branch).
            var graphic = (ushort)(pd.Serial == gameCtx.Value.PlayerSerial ? 0x07D0 : 0x07D1);
            ref readonly var gumpInfo = ref assets.Value.Gumps.GetGump(graphic);
            var width = gumpInfo.UV.Width;
            var height = gumpInfo.UV.Height;

            var window = commands.Spawn()
                .Insert(new Node
                {
                    Display = Display.Flex,
                    PositionType = PositionType.Absolute,
                    Left = Val.Px(100),
                    Top = Val.Px(100),
                    Width = Val.Px(width),
                    Height = Val.Px(height),
                })
                // UiCustom emits a Clay Custom render command at layout
                // time; without it the renderer never asks UOCustomRender
                // to draw the sprite. ContainersPlugin gets away without
                // it because container entities pick up the marker
                // elsewhere — fresh-spawned windows must add it directly.
                .Insert(new UiCustom { Data = CustomMarker })
                .Insert(new UOCustomRender
                {
                    Kind = UOCustomKind.Gump,
                    AssetId = graphic,
                    Hue = Vector3.UnitZ,
                })
                .Insert(Interaction.None)
                .Insert(new FloatingWindowState
                {
                    InitialX = 100,
                    InitialY = 100,
                    InitialWidth = width,
                    InitialHeight = height,
                })
                .Insert<UIMovable>()
                .Insert<IsPaperdoll>();

            // Title text on the paperdoll. The legacy gump puts it near
            // the top of the panel; same Y offset works here.
            var title = commands.Spawn()
                .Insert(new Node
                {
                    PositionType = PositionType.Absolute,
                    Left = Val.Px(40),
                    Top = Val.Px(262),
                    Width = Val.Auto,
                    Height = Val.Auto,
                })
                .Insert(new Text(pd.Title ?? string.Empty))
                .Insert(new TextFont { FontId = 0, Size = 14 })
                .Insert(new TextColor(ClayColor.White));

            commands.AddChild(window.Id, title.Id);
        }
    }
}

// Tag component for paperdoll windows so future systems (drag, close,
// stat updates) can query them.
internal struct IsPaperdoll;
