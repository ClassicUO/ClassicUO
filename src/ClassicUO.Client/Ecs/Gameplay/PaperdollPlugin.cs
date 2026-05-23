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
        var closeFn = ClosePaperdollOnRightClick;

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
            .Build()

            .AddSystem(closeFn)
            .InStage(Stage.Update)
            .RunIf((Res<MouseContext> m) => m.Value.IsPressedOnce(Input.MouseButtonType.Right))
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
        Query<Data<PlayerData>> qPlayerData,
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

            // Drop-on-paperdoll → equip. When a GrabbedItem is in flight
            // and the user clicks on the paperdoll panel, send an
            // EquipRequest for that item onto the target mobile. The
            // layer is taken from the held item's TileData; if zero we
            // skip — the server will reject malformed equips anyway, no
            // need to spam packets for non-wearables.
            window.Observe((On<UiPointerDown> _,
                            Res<GrabbedItem> grabbed,
                            Res<NetClient> net,
                            Res<UOFileManager> fm) =>
            {
                if (grabbed.Value.Serial == 0) return;
                var staticData = fm.Value.TileData.StaticData[grabbed.Value.Graphic];
                var layer = (Layer)staticData.Layer;
                if (layer == Layer.Invalid) return;
                net.Value.Send_EquipRequest(grabbed.Value.Serial, layer, targetSerial);
                grabbed.Value.Clear();
            });

            // Initial equipment overlays. Resolve IsFemale from
            // PlayerData (available only on the local player on
            // impl/ecs); fall back to male for other mobiles.
            var isFemale = false;
            if (entitiesMap.Value.TryGet(commands, pd.Serial, out var targetEnt))
            {
                if (qPlayerData.Contains(targetEnt.Id))
                {
                    (_, var data) = qPlayerData.Get(targetEnt.Id);
                    isFemale = data.Ref.IsFemale;
                }
                if (qSlots.Contains(targetEnt.Id))
                {
                    (_, var slotsData) = qSlots.Get(targetEnt.Id);
                    SpawnEquipmentOverlays(commands, windowId, ref slotsData.Ref,
                        fileManager.Value.TileData, assets.Value, qItem, qSerial, isFemale);
                    SpawnJewelrySlots(commands, windowId, ref slotsData.Ref,
                        assets.Value, qItem, qSerial);
                }
                else
                {
                    // Slots aren't populated yet (no items equipped, or
                    // the EquipItem packets haven't arrived for this
                    // mobile). Spawn empty frames so the UI looks right;
                    // RefreshEquipmentOverlays will fill the item arts
                    // when EquipmentSlots first lands.
                    var empty = default(EquipmentSlots);
                    SpawnJewelrySlots(commands, windowId, ref empty,
                        assets.Value, qItem, qSerial);
                }
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

            // Party manifest button at (25+SCROLLS_STEP, 196). Main
            // restricts this to clients with party-manifest support
            // (post-CV_500A); we always spawn it on impl/ecs since the
            // ECS scenes don't currently surface client-version flags
            // here. Click handler is a stub — no PartyGump on impl/ecs
            // yet, so we log a TODO line; wiring it to the future party
            // window only requires changing the body of the observer.
            if (isPlayer)
            {
                SpawnButton(commands, windowId, 0x07D2, 25 + 14, 196)
                    .Observe((On<UiPointerDown> _) =>
                    {
                        Console.WriteLine("[paperdoll] party manifest click — PartyGump not ported");
                    });
            }

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

    // Re-render equipment children on EquipmentSlots changes. Same
    // teardown-and-respawn applies to the left-side jewelry slots since
    // their item-art children depend on the current EquipmentSlots
    // contents.
    private static void RefreshEquipmentOverlays(
        Commands commands,
        Res<AssetsServer> assets,
        Res<UOFileManager> fileManager,
        Res<NetworkEntitiesMap> entitiesMap,
        Query<Data<EquipmentSlots>, Filter<Changed<EquipmentSlots>>> qChangedSlots,
        Query<Data<EquipmentSlots>> qSlots,
        Query<Data<PaperdollTarget>, With<IsPaperdoll>> qWindows,
        Query<Data<PaperdollEquipChild>> qEquipChildren,
        Query<Data<PaperdollJewelrySlot>> qJewelrySlots,
        Query<Data<Graphic, Hue>> qItem,
        Query<Data<NetworkSerial>> qSerial,
        Query<Data<PlayerData>> qPlayerData)
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
            foreach ((var childEnt, var info) in qJewelrySlots)
            {
                if (info.Ref.WindowEntity != winEnt.Ref) continue;
                commands.Entity(childEnt.Ref).Despawn();
            }

            var isFemale = false;
            if (qPlayerData.Contains(targetEnt.Id))
            {
                (_, var data) = qPlayerData.Get(targetEnt.Id);
                isFemale = data.Ref.IsFemale;
            }

            (_, var slotsData) = qSlots.Get(targetEnt.Id);
            SpawnEquipmentOverlays(commands, winEnt.Ref, ref slotsData.Ref,
                fileManager.Value.TileData, assets.Value, qItem, qSerial, isFemale);
            SpawnJewelrySlots(commands, winEnt.Ref, ref slotsData.Ref,
                assets.Value, qItem, qSerial);
        }
    }

    private static void SpawnEquipmentOverlays(
        Commands commands,
        ulong windowEntity,
        ref EquipmentSlots slots,
        TileDataLoader tileData,
        AssetsServer assets,
        Query<Data<Graphic, Hue>> qItem,
        Query<Data<NetworkSerial>> qSerial,
        bool isFemale)
    {
        foreach (var layer in s_layerOrder)
        {
            var itemId = slots[layer];
            if (itemId == 0) continue;
            if (!qItem.Contains(itemId)) continue;

            (_, var gfx, var hue) = qItem.Get(itemId);
            var animId = tileData.StaticData[gfx.Ref.Value].AnimID;
            if (animId == 0) continue;

            // Female-body gumps live in a parallel range starting at
            // FEMALE_GUMP_OFFSET. Mirrors main's GetAnimID. If the
            // chosen offset has no texture, fall back to the opposite
            // sex offset — many items only ship one variant.
            var primaryOffset = isFemale
                ? Constants.FEMALE_GUMP_OFFSET
                : Constants.MALE_GUMP_OFFSET;
            var fallbackOffset = isFemale
                ? Constants.MALE_GUMP_OFFSET
                : Constants.FEMALE_GUMP_OFFSET;

            var equipGump = (ushort)(primaryOffset + animId);
            var equipW = assets.Gumps.GetGump(equipGump).UV.Width;
            var equipH = assets.Gumps.GetGump(equipGump).UV.Height;
            if (equipW == 0)
            {
                equipGump = (ushort)(fallbackOffset + animId);
                equipW = assets.Gumps.GetGump(equipGump).UV.Width;
                equipH = assets.Gumps.GetGump(equipGump).UV.Height;
                if (equipW == 0) continue;
            }

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
                    Width = Val.Px(equipW),
                    Height = Val.Px(equipH),
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

    // Equipment slot frames around the paperdoll body. Mirrors main's
    // post-`show more paperdoll slots` (Dec 2025) layout: 9 slots in a
    // left column at X=2 and 6 slots in a right column at X=162, both
    // starting Y=70, step 21. Frame is gump 0x2344 (the legacy code
    // also tiles 0x243A behind it; we skip the tile background — it
    // shows through to the paperdoll panel's own color which already
    // looks correct). Each slot renders the equipped item's *art*
    // graphic centered when filled.
    private static readonly (int X, Layer Layer)[] s_slotPositions =
    {
        // Left column.
        (2, Layer.Helmet),
        (2, Layer.Earrings),
        (2, Layer.Necklace),
        (2, Layer.Ring),
        (2, Layer.Bracelet),
        (2, Layer.Tunic),
        (2, Layer.OneHanded),
        (2, Layer.TwoHanded),
        (2, Layer.Talisman),
        // Right column starts at (162, 70).
        (162, Layer.Robe),
        (162, Layer.Gloves),
        (162, Layer.Pants),
        (162, Layer.Arms),
        (162, Layer.Cloak),
        (162, Layer.Shoes),
    };

    private const int JewelrySlotY0 = 70;
    private const int JewelrySlotStep = 21;
    private const int JewelrySlotW = 19;
    private const int JewelrySlotH = 20;
    private const ushort JewelrySlotFrameGump = 0x2344;

    private static void SpawnJewelrySlots(
        Commands commands,
        ulong windowEntity,
        ref EquipmentSlots slots,
        AssetsServer assets,
        Query<Data<Graphic, Hue>> qItem,
        Query<Data<NetworkSerial>> qSerial)
    {
        // Y index resets at the start of each column. Track the X
        // currently being walked so the right column begins fresh.
        var leftIdx = 0;
        var rightIdx = 0;
        for (var i = 0; i < s_slotPositions.Length; i++)
        {
            var (slotX, layer) = s_slotPositions[i];
            int idx;
            if (slotX <= 2) { idx = leftIdx++; }
            else            { idx = rightIdx++; }
            var slotY = JewelrySlotY0 + idx * JewelrySlotStep;

            uint itemSerial = 0;
            ushort itemGraphic = 0;
            ushort itemHue = 0;
            var itemId = slots[layer];
            if (itemId != 0 && qItem.Contains(itemId))
            {
                (_, var gfx, var hue) = qItem.Get(itemId);
                itemGraphic = gfx.Ref.Value;
                itemHue = hue.Ref.Value;
                if (qSerial.Contains(itemId))
                {
                    (_, var serialData) = qSerial.Get(itemId);
                    itemSerial = serialData.Ref.Value;
                }
            }

            var slotEnt = commands.Spawn()
                .Insert(new Node
                {
                    PositionType = PositionType.Absolute,
                    Left = Val.Px(slotX),
                    Top = Val.Px(slotY),
                    Width = Val.Px(JewelrySlotW),
                    Height = Val.Px(JewelrySlotH),
                })
                .Insert(new ZIndex(102))
                .Insert(new UiCustom { Data = CustomMarker })
                .Insert(new UOCustomRender
                {
                    Kind = UOCustomKind.Gump,
                    AssetId = JewelrySlotFrameGump,
                    Hue = Vector3.UnitZ,
                })
                .Insert(Interaction.None)
                .Insert(new PaperdollJewelrySlot
                {
                    WindowEntity = windowEntity,
                    Layer = layer,
                    ItemSerial = itemSerial,
                });

            // Empty slot: only the frame. Click does nothing useful
            // (server rejects pickup on serial 0).
            if (itemSerial != 0 && itemGraphic != 0)
            {
                var capturedSerial = itemSerial;
                slotEnt.Observe((On<UiPointerDown> _, Res<NetClient> net) =>
                {
                    net.Value.Send_PickUpRequest(capturedSerial, 1);
                });

                // Item art icon inside the slot. Centered at (0, 0) of
                // the 19x20 frame and scaled to 18x18 — same as the
                // legacy ItemGumpFixed nested control.
                var artEnt = commands.Spawn()
                    .Insert(new Node
                    {
                        PositionType = PositionType.Absolute,
                        Left = Val.Px(0),
                        Top = Val.Px(0),
                        Width = Val.Px(18),
                        Height = Val.Px(18),
                    })
                    .Insert(new ZIndex(103))
                    .Insert(new UiCustom { Data = CustomMarker })
                    .Insert(new UOCustomRender
                    {
                        Kind = UOCustomKind.Art,
                        AssetId = itemGraphic,
                        Hue = itemHue != 0
                            ? new Vector3(itemHue, 1, 1)
                            : Vector3.UnitZ,
                    });
                commands.AddChild(slotEnt.Id, artEnt.Id);
            }

            commands.AddChild(windowEntity, slotEnt.Id);
        }
    }

    // Right-click on a paperdoll tears it down. Mirrors
    // ContainersPlugin.CloseContainersOnRightClick: pick the topmost
    // paperdoll under the cursor (highest ClayId draws latest, so it's
    // on top) and remove its UI components plus the child overlay /
    // stat / button entities. We Remove<Node> instead of Despawn on the
    // window so PaperdollTarget stays around if any other system wants
    // to remember the window existed — matches the container pattern.
    private static void ClosePaperdollOnRightClick(
        Commands commands,
        Res<MouseContext> mouseCtx,
        Query<Data<ComputedNode>, Filter<With<IsPaperdoll>>> query,
        Query<Data<PaperdollEquipChild>> qEquip,
        Query<Data<PaperdollStatText>> qStats,
        Query<Data<PaperdollJewelrySlot>> qJewelry)
    {
        var pos = mouseCtx.Value.Position;
        ulong topId = 0;
        uint topClayId = 0;

        foreach ((var ent, var computed) in query)
        {
            var bb = computed.Ref;
            if (pos.X < bb.Position.X || pos.Y < bb.Position.Y) continue;
            if (pos.X >= bb.Position.X + bb.Size.X) continue;
            if (pos.Y >= bb.Position.Y + bb.Size.Y) continue;
            if (bb.ClayId >= topClayId)
            {
                topClayId = bb.ClayId;
                topId = ent.Ref;
            }
        }

        if (topId == 0) return;

        // Tear down the children before the window so the renderer
        // doesn't try to layout free-floating equipment for one frame.
        foreach ((var childEnt, var info) in qEquip)
        {
            if (info.Ref.WindowEntity == topId)
                commands.Entity(childEnt.Ref).Despawn();
        }
        foreach ((var childEnt, var info) in qStats)
        {
            if (info.Ref.WindowEntity == topId)
                commands.Entity(childEnt.Ref).Despawn();
        }
        foreach ((var childEnt, var info) in qJewelry)
        {
            if (info.Ref.WindowEntity == topId)
                commands.Entity(childEnt.Ref).Despawn();
        }

        commands.Entity(topId).Despawn();
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

// Marker on each of the 6 left-side jewelry slot frames. Layer is
// stored so a future right-click context menu can show layer-specific
// actions without re-querying.
internal struct PaperdollJewelrySlot
{
    public ulong WindowEntity;
    public Layer Layer;
    public uint ItemSerial;
}
