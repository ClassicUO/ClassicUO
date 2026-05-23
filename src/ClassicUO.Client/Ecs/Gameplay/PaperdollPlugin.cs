// Paperdoll packet handling + minimal paperdoll gump UI.
//
// On OnOpenPaperdollPacket_0x88 (0x88, server's reply to Send_DoubleClick
// on a mobile), spawn a draggable gump entity with the paperdoll
// background sprite (0x07D0 for the player, 0x07D1 for others), the
// mobile's title rendered on top, and one Custom-rendered overlay per
// equipped item drawn from the mobile's EquipmentSlots.
//
// Equipment graphic resolution mirrors main's
// PaperDollInteractable.GetAnimID:
//     paperdollGump = MALE_GUMP_OFFSET + ItemData.AnimID
// Female bodies, equip conversions, tileart appearance lookup, and the
// quiver-fix layer order are still TODO — minimal port renders the male
// gump set, which is the default for most accounts.

using System;
using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using ClayColor = Clay.Color;

namespace ClassicUO.Ecs;

internal readonly struct PaperdollPlugin : IPlugin
{
    private static readonly object CustomMarker = new();

    // Same render order as main's PaperDollInteractable._layerOrder.
    // Later items overdraw earlier items, so jewelry / hair / helmets
    // need to come after body/clothing layers.
    private static readonly Layer[] s_layerOrder =
    {
        Layer.Cloak,
        Layer.Shirt,
        Layer.Pants,
        Layer.Shoes,
        Layer.Legs,
        Layer.Arms,
        Layer.Torso,
        Layer.Tunic,
        Layer.Ring,
        Layer.Bracelet,
        Layer.Face,
        Layer.Gloves,
        Layer.Skirt,
        Layer.Robe,
        Layer.Waist,
        Layer.Necklace,
        Layer.Hair,
        Layer.Beard,
        Layer.Earrings,
        Layer.Helmet,
        Layer.OneHanded,
        Layer.TwoHanded,
        Layer.Talisman,
    };

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
        Res<UOFileManager> fileManager,
        Res<GameContext> gameCtx,
        Res<NetworkEntitiesMap> entitiesMap,
        Query<Data<EquipmentSlots>> qSlots,
        Query<Data<Graphic, Hue>> qItem,
        EventReader<IPacket> packets)
    {
        foreach (var packet in packets.Read())
        {
            if (packet is not OnOpenPaperdollPacket_0x88 pd) continue;

            var isPlayer = pd.Serial == gameCtx.Value.PlayerSerial;
            var bgGraphic = (ushort)(isPlayer ? 0x07D0 : 0x07D1);
            ref readonly var bgInfo = ref assets.Value.Gumps.GetGump(bgGraphic);
            var width = bgInfo.UV.Width;
            var height = bgInfo.UV.Height;

            // Paperdoll window. PositionType=Absolute keeps it out of the
            // surrounding flex layout. ZIndex puts it above the world.
            // UiCustom is the marker the layout pass needs so the
            // renderer asks UOCustomRender to draw the sprite.
            var window = commands.Spawn()
                .Insert(new Node
                {
                    PositionType = PositionType.Absolute,
                    Left = Val.Px(20),
                    Top = Val.Px(20),
                    Width = Val.Px(width),
                    Height = Val.Px(height),
                })
                .Insert(new ZIndex(100))
                .Insert(new UiCustom { Data = CustomMarker })
                .Insert(new UOCustomRender
                {
                    Kind = UOCustomKind.Gump,
                    AssetId = bgGraphic,
                    Hue = Vector3.UnitZ,
                })
                .Insert(Interaction.None)
                .Insert(new FloatingWindowState
                {
                    InitialX = 20,
                    InitialY = 20,
                    InitialWidth = width,
                    InitialHeight = height,
                })
                .Insert<UIMovable>()
                .Insert<IsPaperdoll>()
                .Insert(new PaperdollTarget { Serial = pd.Serial });

            // Equipment overlays. Each visible layer becomes a Clay
            // Custom child anchored at (8, 19) — the same offset main's
            // PaperDollInteractable uses — sized to the full paperdoll
            // panel so the sprite renders at its native pixel size.
            if (entitiesMap.Value.TryGet(commands, pd.Serial, out var targetEnt)
                && qSlots.Contains(targetEnt.Id))
            {
                (_, var slotsData) = qSlots.Get(targetEnt.Id);
                ref var slots = ref slotsData.Ref;
                var tileData = fileManager.Value.TileData;
                foreach (var layer in s_layerOrder)
                {
                    var itemId = slots[layer];
                    if (itemId == 0) continue;
                    if (!qItem.Contains(itemId)) continue;

                    (_, var gfx, var hue) = qItem.Get(itemId);
                    var animId = tileData.StaticData[gfx.Ref.Value].AnimID;
                    if (animId == 0) continue;

                    var equipGump = (ushort)(Constants.MALE_GUMP_OFFSET + animId);
                    ref readonly var equipInfo = ref assets.Value.Gumps.GetGump(equipGump);
                    if (equipInfo.UV.Width == 0) continue;

                    // Item hue: 0 means use the gump's native colors (no
                    // override). Vector3(hue, 1, 1) tells the renderer
                    // which palette entry to apply; Vector3.UnitZ = (0,
                    // 0, 1) is the "no tint" signal matched by the
                    // existing login gump rendering path.
                    var hueValue = hue.Ref.Value;
                    var hueVec = hueValue != 0
                        ? new Vector3(hueValue, 1, 1)
                        : Vector3.UnitZ;
                    var equipEnt = commands.Spawn()
                        .Insert(new Node
                        {
                            PositionType = PositionType.Absolute,
                            Left = Val.Px(8),
                            Top = Val.Px(19),
                            Width = Val.Px(equipInfo.UV.Width),
                            Height = Val.Px(equipInfo.UV.Height),
                        })
                        .Insert(new ZIndex(101))
                        .Insert(new UiCustom { Data = CustomMarker })
                        .Insert(new UOCustomRender
                        {
                            Kind = UOCustomKind.Gump,
                            AssetId = equipGump,
                            Hue = hueVec,
                        });

                    commands.AddChild(window.Id, equipEnt.Id);
                }
            }

            // Title text near the bottom of the paperdoll panel.
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

// Records which mobile this paperdoll window represents so stat-update
// and equip/unequip packets can target the right window.
internal struct PaperdollTarget
{
    public uint Serial;
}
