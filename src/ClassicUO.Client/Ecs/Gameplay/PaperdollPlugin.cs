// Paperdoll packet handling + minimal paperdoll gump UI.
//
// On OnOpenPaperdollPacket_0x88 (0x88, server's reply to Send_DoubleClick
// on a mobile), spawn a draggable gump entity with the paperdoll
// background sprite (0x07D0 for the player, 0x07D1 for others), the
// virtue button, the profile button, equipment overlays drawn from the
// mobile's EquipmentSlots, title text, and a stat panel.
//
// Live updates:
//  * RefreshEquipmentOverlays runs whenever any EquipmentSlots component
//    is Changed. It tears down the previous equipment-child entities for
//    the matching paperdoll window and respawns them from the new slots.
//  * RefreshStatPanel runs every frame and rewrites the stat text's
//    Text.Value from the target mobile's PlayerData / Hits / Mana /
//    Stamina components.
//
// Interactions:
//  * Virtue button (0x0071 at (80, 4)) — UiPointerDown sends a ReplyGump
//    on the virtue request id, matching main's PaperDollGump.
//  * Profile button (0x07D2 at (25, 196)) — UiPointerDown sends a
//    Send_ProfileRequest for the paperdoll target.
//  * Equipment overlay child — UiPointerDown sends a Send_PickUpRequest
//    for that item serial (left-click to grab).
//
// Equipment graphic resolution mirrors main's
// PaperDollInteractable.GetAnimID:
//     paperdollGump = MALE_GUMP_OFFSET + ItemData.AnimID

using System;
using System.Text;
using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Network;
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
        var refreshEquipFn = RefreshEquipmentOverlays;
        var refreshStatsFn = RefreshStatPanel;

        app
            .AddSystem(processFn)
            .InStage(Stage.Update)
            .RunIf((EventReader<IPacket> reader) => reader.HasEvents)
            .Build()

            .AddSystem(refreshEquipFn)
            .InStage(Stage.Update)
            .RunIf((Query<Data<EquipmentSlots>, Filter<Changed<EquipmentSlots>>> changed,
                    Query<Empty, With<IsPaperdoll>> open) => changed.Count() > 0 && open.Count() > 0)
            .Build()

            .AddSystem(refreshStatsFn)
            .InStage(Stage.Update)
            .RunIf((Query<Empty, With<IsPaperdoll>> open) => open.Count() > 0)
            .Build();
    }

    private static void ProcessPaperdollPackets(
        Commands commands,
        Res<AssetsServer> assets,
        Res<UOFileManager> fileManager,
        Res<GameContext> gameCtx,
        Res<NetClient> network,
        Res<NetworkEntitiesMap> entitiesMap,
        Query<Data<EquipmentSlots>> qSlots,
        Query<Data<Graphic, Hue>> qItem,
        Query<Data<NetworkSerial>> qSerial,
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

            var targetSerial = pd.Serial;
            var windowId = window.Id;

            // Initial equipment overlays.
            if (entitiesMap.Value.TryGet(commands, pd.Serial, out var targetEnt)
                && qSlots.Contains(targetEnt.Id))
            {
                (_, var slotsData) = qSlots.Get(targetEnt.Id);
                SpawnEquipmentOverlays(commands, windowId, ref slotsData.Ref,
                    fileManager.Value.TileData, assets.Value, qItem, qSerial);
            }

            // Virtue menu button at (80, 4) — matches main's
            // PaperDollGump line 259.
            if (isPlayer)
            {
                SpawnButton(commands, windowId, 0x0071, 80, 4)
                    .Observe((On<UiPointerDown> _, Res<NetClient> net) =>
                    {
                        // Same args as PaperDollGump.VirtueMenu_MouseDoubleClick.
                        net.Value.Send_GumpResponse(
                            targetSerial, 0x000001CD, 0x00000001,
                            new uint[] { targetSerial },
                            Array.Empty<Tuple<ushort, string>>());
                    });
            }

            // Profile button at (25, 196).
            SpawnButton(commands, windowId, 0x07D2, 25, 196)
                .Observe((On<UiPointerDown> _, Res<NetClient> net) =>
                {
                    net.Value.Send_ProfileRequest(targetSerial);
                });

            // Title at the bottom of the panel.
            var title = commands.Spawn()
                .Insert(new Node
                {
                    PositionType = PositionType.Absolute,
                    Left = Val.Px(39),
                    Top = Val.Px(262),
                    Width = Val.Auto,
                    Height = Val.Auto,
                })
                .Insert(new Text(pd.Title ?? string.Empty))
                .Insert(new TextFont { FontId = 0, Size = 14 })
                .Insert(new TextColor(ClayColor.White));
            commands.AddChild(windowId, title.Id);

            // Stat text panel below the title. The 0x07D0 panel has
            // visible "info" lines roughly at y=240-290 below the body;
            // shift up so the text reads inside the rendered panel.
            var stats = commands.Spawn()
                .Insert(new Node
                {
                    PositionType = PositionType.Absolute,
                    Left = Val.Px(40),
                    Top = Val.Px(220),
                    Width = Val.Auto,
                    Height = Val.Auto,
                })
                .Insert(new Text(string.Empty))
                .Insert(new TextFont { FontId = 0, Size = 11 })
                .Insert(new TextColor(ClayColor.White))
                .Insert(new ZIndex(102))
                .Insert(new PaperdollStatText { WindowEntity = windowId });
            commands.AddChild(windowId, stats.Id);
        }
    }

    // Helper to spawn a gump-pic button as a paperdoll child. Returns
    // the EntityCommands so callers can chain an Observe<UiPointerDown>.
    private static EntityCommands SpawnButton(
        Commands commands,
        ulong windowEntity,
        ushort gumpId,
        int x,
        int y)
    {
        var ent = commands.Spawn()
            .Insert(new Node
            {
                PositionType = PositionType.Absolute,
                Left = Val.Px(x),
                Top = Val.Px(y),
                Width = Val.Px(16),
                Height = Val.Px(16),
            })
            .Insert(new ZIndex(102))
            .Insert(new UiCustom { Data = CustomMarker })
            .Insert(new UOCustomRender
            {
                Kind = UOCustomKind.Gump,
                AssetId = gumpId,
                Hue = Vector3.UnitZ,
            })
            .Insert(Interaction.None);
        commands.AddChild(windowEntity, ent.Id);
        return ent;
    }

    // Re-render equipment children on EquipmentSlots changes.
    private static void RefreshEquipmentOverlays(
        Commands commands,
        Res<AssetsServer> assets,
        Res<UOFileManager> fileManager,
        Res<NetworkEntitiesMap> entitiesMap,
        Query<Data<EquipmentSlots>, Filter<Changed<EquipmentSlots>>> qChangedSlots,
        Query<Data<EquipmentSlots>> qSlots,
        Query<Data<PaperdollTarget>, With<IsPaperdoll>> qWindows,
        Query<Data<PaperdollEquipChild>> qEquipChildren,
        Query<Data<Graphic, Hue>> qItem,
        Query<Data<NetworkSerial>> qSerial)
    {
        foreach ((var winEnt, var target) in qWindows)
        {
            if (!entitiesMap.Value.TryGet(commands, target.Ref.Serial, out var targetEnt))
                continue;
            if (!qChangedSlots.Contains(targetEnt.Id))
                continue;

            foreach ((var childEnt, var info) in qEquipChildren)
            {
                if (info.Ref.WindowEntity != winEnt.Ref) continue;
                commands.Entity(childEnt.Ref).Despawn();
            }

            (_, var slotsData) = qSlots.Get(targetEnt.Id);
            SpawnEquipmentOverlays(commands, winEnt.Ref, ref slotsData.Ref,
                fileManager.Value.TileData, assets.Value, qItem, qSerial);
        }
    }

    private static void SpawnEquipmentOverlays(
        Commands commands,
        ulong windowEntity,
        ref EquipmentSlots slots,
        TileDataLoader tileData,
        AssetsServer assets,
        Query<Data<Graphic, Hue>> qItem,
        Query<Data<NetworkSerial>> qSerial)
    {
        foreach (var layer in s_layerOrder)
        {
            var itemId = slots[layer];
            if (itemId == 0) continue;
            if (!qItem.Contains(itemId)) continue;

            (_, var gfx, var hue) = qItem.Get(itemId);
            var animId = tileData.StaticData[gfx.Ref.Value].AnimID;
            if (animId == 0) continue;

            var equipGump = (ushort)(Constants.MALE_GUMP_OFFSET + animId);
            ref readonly var equipInfo = ref assets.Gumps.GetGump(equipGump);
            if (equipInfo.UV.Width == 0) continue;

            var hueValue = hue.Ref.Value;
            var hueVec = hueValue != 0
                ? new Vector3(hueValue, 1, 1)
                : Vector3.UnitZ;

            // Resolve item NetworkSerial so the click handler can fire
            // Send_PickUpRequest without re-querying components.
            uint itemSerial = 0;
            if (qSerial.Contains(itemId))
            {
                (_, var serialData) = qSerial.Get(itemId);
                itemSerial = serialData.Ref.Value;
            }

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
                })
                .Insert(Interaction.None)
                .Insert(new PaperdollEquipChild
                {
                    WindowEntity = windowEntity,
                    ItemSerial = itemSerial,
                });

            // Left-click on equipped item → grab it. Mirrors main's
            // ItemGumpFixed pickup behavior on the paperdoll.
            equipEnt.Observe((On<UiPointerDown> _, Res<NetClient> net) =>
            {
                if (itemSerial != 0)
                    net.Value.Send_PickUpRequest(itemSerial, 1);
            });

            commands.AddChild(windowEntity, equipEnt.Id);
        }
    }

    // Per-frame stat panel refresh.
    private static void RefreshStatPanel(
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        Query<Data<PaperdollTarget>, With<IsPaperdoll>> qWindows,
        Query<Data<Text, PaperdollStatText>> qStatText,
        Query<Data<PlayerData>> qPlayerData,
        Query<Data<Hits>> qHits,
        Query<Data<Mana>> qMana,
        Query<Data<Stamina>> qStam)
    {
        foreach ((var winEnt, var target) in qWindows)
        {
            if (!entitiesMap.Value.TryGet(commands, target.Ref.Serial, out var targetEnt))
                continue;

            var sb = new StringBuilder();
            if (qPlayerData.Contains(targetEnt.Id))
            {
                (_, var data) = qPlayerData.Get(targetEnt.Id);
                ref var d = ref data.Ref;
                sb.Append("STR ").Append(d.Str)
                  .Append(" DEX ").Append(d.Dex)
                  .Append(" INT ").Append(d.Int).Append('\n');
                if (d.WeightMax > 0)
                {
                    sb.Append("Wt ").Append(d.Weight).Append('/').Append(d.WeightMax).Append('\n');
                }
                if (d.Gold > 0)
                {
                    sb.Append("Gold ").Append(d.Gold).Append('\n');
                }
            }
            if (qHits.Contains(targetEnt.Id))
            {
                (_, var h) = qHits.Get(targetEnt.Id);
                sb.Append("HP ").Append(h.Ref.Value).Append('/').Append(h.Ref.MaxValue);
            }
            if (qMana.Contains(targetEnt.Id))
            {
                (_, var m) = qMana.Get(targetEnt.Id);
                sb.Append(" MP ").Append(m.Ref.Value).Append('/').Append(m.Ref.MaxValue);
            }
            if (qStam.Contains(targetEnt.Id))
            {
                (_, var s) = qStam.Get(targetEnt.Id);
                sb.Append(" ST ").Append(s.Ref.Value).Append('/').Append(s.Ref.MaxValue);
            }

            var newText = sb.ToString();
            foreach ((var entText, var t, var marker) in qStatText)
            {
                if (marker.Ref.WindowEntity != winEnt.Ref) continue;
                if (t.Ref.Value != newText)
                    t.Ref.Value = newText;
                break;
            }
        }
    }
}

internal struct IsPaperdoll;

internal struct PaperdollTarget
{
    public uint Serial;
}

internal struct PaperdollEquipChild
{
    public ulong WindowEntity;
    public uint ItemSerial;
}

internal struct PaperdollStatText
{
    public ulong WindowEntity;
}
