// Paperdoll packet handling + minimal paperdoll gump UI.
//
// On OnOpenPaperdollPacket_0x88 (0x88, server's reply to Send_DoubleClick
// on a mobile), spawn a draggable gump entity with the paperdoll
// background sprite (0x07D0 for the player, 0x07D1 for others) plus
// a Custom-rendered overlay per equipped item drawn from the mobile's
// EquipmentSlots, plus a stat-text overlay.
//
// Live updates:
//  * RefreshEquipmentOverlays runs whenever any EquipmentSlots component
//    is Changed. It tears down the previous equipment-child entities for
//    the matching paperdoll window and respawns them from the new slots.
//  * RefreshStatPanel runs every frame and rewrites the stat text's
//    Text.Value from the target mobile's PlayerData / Hits / Mana /
//    Stamina components. This is cheap (one string format) and keeps
//    the panel in sync with the engine's own per-frame Hits/Mana ticks.
//
// Equipment graphic resolution mirrors main's
// PaperDollInteractable.GetAnimID:
//     paperdollGump = MALE_GUMP_OFFSET + ItemData.AnimID
// Female bodies, equip conversions, tileart appearance lookup, and the
// quiver-fix layer order are still TODO — minimal port renders the male
// gump set, which is the default for most accounts.

using System.Text;
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

            // Initial equipment overlays. Subsequent equip/unequip
            // packets re-run this work via RefreshEquipmentOverlays.
            if (entitiesMap.Value.TryGet(commands, pd.Serial, out var targetEnt)
                && qSlots.Contains(targetEnt.Id))
            {
                (_, var slotsData) = qSlots.Get(targetEnt.Id);
                SpawnEquipmentOverlays(commands, window.Id, ref slotsData.Ref,
                    fileManager.Value.TileData, assets.Value, qItem);
            }

            // Title at the bottom of the panel.
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

            // Stat text panel. Populated immediately by RefreshStatPanel
            // on the next frame; spawn with an empty string so the entity
            // exists for the per-frame refresh to find via the marker.
            var stats = commands.Spawn()
                .Insert(new Node
                {
                    PositionType = PositionType.Absolute,
                    Left = Val.Px(8),
                    Top = Val.Px(285),
                    Width = Val.Auto,
                    Height = Val.Auto,
                })
                .Insert(new Text(string.Empty))
                .Insert(new TextFont { FontId = 0, Size = 12 })
                .Insert(new TextColor(ClayColor.White))
                .Insert(new PaperdollStatText { WindowEntity = window.Id });
            commands.AddChild(window.Id, stats.Id);
        }
    }

    // Re-render equipment children on any EquipmentSlots change. The
    // simplest approach: query open paperdolls whose target's slots are
    // marked Changed this frame, despawn every PaperdollEquipChild
    // attached to that window, then spawn fresh overlays.
    private static void RefreshEquipmentOverlays(
        Commands commands,
        Res<AssetsServer> assets,
        Res<UOFileManager> fileManager,
        Res<NetworkEntitiesMap> entitiesMap,
        Query<Data<EquipmentSlots>, Filter<Changed<EquipmentSlots>>> qChangedSlots,
        Query<Data<EquipmentSlots>> qSlots,
        Query<Data<PaperdollTarget>, With<IsPaperdoll>> qWindows,
        Query<Data<PaperdollEquipChild>> qEquipChildren,
        Query<Data<Graphic, Hue>> qItem)
    {
        // Build the set of mobile serials whose equipment changed.
        // qChangedSlots iterates entity IDs — translate to serials via
        // the open paperdoll's PaperdollTarget so we can match.
        foreach ((var winEnt, var target) in qWindows)
        {
            if (!entitiesMap.Value.TryGet(commands, target.Ref.Serial, out var targetEnt))
                continue;

            // Only refresh if THIS target's slots actually changed.
            if (!qChangedSlots.Contains(targetEnt.Id))
                continue;

            // Despawn the previous equipment children.
            foreach ((var childEnt, var info) in qEquipChildren)
            {
                if (info.Ref.WindowEntity != winEnt.Ref) continue;
                commands.Entity(childEnt.Ref).Despawn();
            }

            (_, var slotsData) = qSlots.Get(targetEnt.Id);
            SpawnEquipmentOverlays(commands, winEnt.Ref, ref slotsData.Ref,
                fileManager.Value.TileData, assets.Value, qItem);
        }
    }

    private static void SpawnEquipmentOverlays(
        Commands commands,
        ulong windowEntity,
        ref EquipmentSlots slots,
        TileDataLoader tileData,
        AssetsServer assets,
        Query<Data<Graphic, Hue>> qItem)
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
                .Insert(new PaperdollEquipChild { WindowEntity = windowEntity });

            commands.AddChild(windowEntity, equipEnt.Id);
        }
    }

    // Format the stat panel string from PlayerData + Hits/Mana/Stamina
    // and write it onto the per-paperdoll PaperdollStatText entity.
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
                  .Append("  DEX ").Append(d.Dex)
                  .Append("  INT ").Append(d.Int).Append('\n');
                sb.Append("Weight ").Append(d.Weight).Append('/').Append(d.WeightMax)
                  .Append("  Gold ").Append(d.Gold).Append('\n');
            }
            if (qHits.Contains(targetEnt.Id))
            {
                (_, var h) = qHits.Get(targetEnt.Id);
                sb.Append("HP ").Append(h.Ref.Value).Append('/').Append(h.Ref.MaxValue);
            }
            if (qMana.Contains(targetEnt.Id))
            {
                (_, var m) = qMana.Get(targetEnt.Id);
                sb.Append("  MP ").Append(m.Ref.Value).Append('/').Append(m.Ref.MaxValue);
            }
            if (qStam.Contains(targetEnt.Id))
            {
                (_, var s) = qStam.Get(targetEnt.Id);
                sb.Append("  ST ").Append(s.Ref.Value).Append('/').Append(s.Ref.MaxValue);
            }

            // Write the text into the matching PaperdollStatText entity.
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

// Marker on each equipment overlay child entity so RefreshEquipmentOverlays
// can find + despawn the previous overlays before respawning.
internal struct PaperdollEquipChild
{
    public ulong WindowEntity;
}

// Marker on the stat-text entity inside a paperdoll so RefreshStatPanel
// can locate it from the parent window.
internal struct PaperdollStatText
{
    public ulong WindowEntity;
}
