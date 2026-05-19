// Container packet handling + container window UI.
//
// Migrated from the old Clay_cs / TinyEcs.UI.Clay path onto TinyEcs.Bevy.UI:
//   * `ClayNode`/`UINode`              -> `Node`            (Bevy.UI)
//   * `ClayInteraction` / mouse action -> `Interaction`     (Bevy.UI)
//   * `UOClayUiBundle.UONodeData`      -> `UOCustomRender`  (GuiPlugin.cs)
//
// Right-click close: Bevy.UI's interaction system only wires the LEFT mouse
// button. There is no `UiPointerUp` for right-click. We work around this with
// a small `Stage.Update` system that walks `MouseContext` directly, picks the
// topmost UIMovable container under the cursor (using its `ComputedNode`
// bounding box), and tears down the UI components when right-button is pressed.

using System;
using ClassicUO.Assets;
using ClassicUO.Ecs.Modding.Host;
using ClassicUO.Game.Data;
using ClassicUO.Network;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;

namespace ClassicUO.Ecs;

internal readonly struct ContainersPlugin : IPlugin
{
    public void Build(App app)
    {
        var processPacketsFn = ProcessContainerPackets;
        var onOpenContainersFn = OnOpenContainers;
        var closeContainersTooFarFromPlayerFn = CloseContainersTooFarFromPlayer;
        var closeContainersOnRightClickFn = CloseContainersOnRightClick;

        app
            .AddSystem(processPacketsFn)
            .InStage(Stage.Update)
            .RunIf((EventReader<IPacket> reader) => reader.HasEvents)
            .Build()

            .AddSystem(onOpenContainersFn)
            .InStage(Stage.Update)
            .RunIf((EventReader<ContainerOpenedEvent> reader) => reader.HasEvents)
            .Build()

            .AddSystem(closeContainersTooFarFromPlayerFn)
            .InStage(Stage.Update)
            .RunIf((Query<Data<WorldPosition>, With<Player>> queryPlayer) => queryPlayer.Count() > 0)
            .Build()

            .AddSystem(closeContainersOnRightClickFn)
            .InStage(Stage.Update)
            .RunIf((Res<MouseContext> mouseCtx) => mouseCtx.Value.IsPressedOnce(Input.MouseButtonType.Right))
            .Build();
    }

    private static void CloseContainersTooFarFromPlayer(
        Commands commands,
        Query<Data<WorldPosition, NetworkSerial>,
             Filter<With<IsContainer>, With<Node>, With<UIMovable>>> query,
        Single<Data<WorldPosition>, With<Player>> queryPlayer,
        EventWriter<HostMessage> hostMsgs)
    {
        const int MAX_CONTAINER_DIST = 5;
        (_, var playerPos) = queryPlayer.Get();

        foreach ((var ent, var pos, var serial) in query)
        {
            if (Math.Abs(playerPos.Ref.X - pos.Ref.X) >= MAX_CONTAINER_DIST ||
                Math.Abs(playerPos.Ref.Y - pos.Ref.Y) >= MAX_CONTAINER_DIST)
            {
                TearDownContainerUI(commands, ent.Ref);
                hostMsgs.Send(new HostMessage.ContainerClosed(serial.Ref.Value));
            }
        }
    }

    // Bevy.UI doesn't fire right-button pointer events. Manually pick the
    // topmost UIMovable container under the cursor each frame the right
    // button is pressed and tear it down.
    private static void CloseContainersOnRightClick(
        Commands commands,
        Res<MouseContext> mouseCtx,
        Query<Data<ComputedNode, NetworkSerial>,
             Filter<With<UIMovable>, With<IsContainer>>> query,
        EventWriter<HostMessage> hostMsgs)
    {
        var pos = mouseCtx.Value.Position;
        ulong topId = 0;
        uint topClayId = 0;
        uint topSerial = 0;

        // Higher Clay ids are emitted later in the render command stream and
        // therefore draw on top. Pick that one.
        foreach ((var ent, var computed, var serial) in query)
        {
            var bb = computed.Ref;
            if (pos.X < bb.Position.X || pos.Y < bb.Position.Y) continue;
            if (pos.X >= bb.Position.X + bb.Size.X) continue;
            if (pos.Y >= bb.Position.Y + bb.Size.Y) continue;

            if (bb.ClayId >= topClayId)
            {
                topClayId = bb.ClayId;
                topId = ent.Ref;
                topSerial = serial.Ref.Value;
            }
        }

        if (topId == 0)
            return;

        TearDownContainerUI(commands, topId);
        hostMsgs.Send(new HostMessage.ContainerClosed(topSerial));
    }

    private static void TearDownContainerUI(Commands commands, ulong entityId)
    {
        commands.Entity(entityId)
            .Remove<Node>()
            .Remove<UOCustomRender>()
            .Remove<UIMovable>()
            .Remove<Interaction>()
            .Remove<FloatingWindowState>();
    }

    private static void OnOpenContainers(
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        Res<AssetsServer> assets,
        EventReader<ContainerOpenedEvent> reader)
    {
        foreach (var ev in reader.Read())
        {
            if (ev.Graphic == 0xFFFF || ev.Graphic == 0x0030)
                continue;

            ref readonly var gumpInfo = ref assets.Value.Gumps.GetGump(ev.Graphic);
            var width = gumpInfo.UV.Width;
            var height = gumpInfo.UV.Height;

            entitiesMap.Value.GetOrCreate(commands, ev.Serial)
                .Insert(new Node
                {
                    Display = Display.Flex,
                    PositionType = PositionType.Absolute,
                    Left = Val.Px(0),
                    Top = Val.Px(0),
                    Width = Val.Px(width),
                    Height = Val.Px(height),
                })
                .Insert(new UOCustomRender
                {
                    Kind = UOCustomKind.Gump,
                    AssetId = ev.Graphic,
                    Hue = Vector3.UnitZ,
                })
                .Insert(Interaction.None)
                .Insert(new FloatingWindowState
                {
                    InitialX = 0,
                    InitialY = 0,
                    InitialWidth = width,
                    InitialHeight = height,
                })
                .Insert<UIMovable>()
                .Insert<IsContainer>();
        }
    }

    private static void ProcessContainerPackets(
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        Res<AssetsServer> assets,
        EventWriter<ContainerUpdateEvent> writer,
        EventWriter<ContainerOpenedEvent> openedWriter,
        EventWriter<HostMessage> hostMsgs,
        EventReader<IPacket> packets)
    {
        foreach (var packet in packets.Read())
        {
            switch (packet)
            {
                case OnOpenContainerPacket_0x24 open:
                    openedWriter.Send(new(open.Serial, open.Graphic));
                    hostMsgs.Send(new HostMessage.ContainerOpened(open.Serial, open.Graphic));
                    break;

                case OnUpdateContainerPacket_0x25_Pre6017 updatePre:
                    HandleUpdateContainer(updatePre, commands, entitiesMap, assets, writer, hostMsgs);
                    break;

                case OnUpdateContainerPacket_0x25_Post6017 updatePost:
                    HandleUpdateContainer(updatePost, commands, entitiesMap, assets, writer, hostMsgs);
                    break;

                case OnUpdateContainerItemsPacket_0x3C_Pre6017 updateItemsPre:
                    HandleUpdateContainerItems(updateItemsPre, commands, entitiesMap, assets, writer, hostMsgs);
                    break;

                case OnUpdateContainerItemsPacket_0x3C_Post6017 updateItemsPost:
                    HandleUpdateContainerItems(updateItemsPost, commands, entitiesMap, assets, writer, hostMsgs);
                    break;
            }
        }
    }

    private static void HandleUpdateContainer(
        IUpdateContainerPacket packet,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        Res<AssetsServer> assets,
        EventWriter<ContainerUpdateEvent> writer,
        EventWriter<HostMessage> hostMsgs)
    {
        var finalGraphic = (ushort)(packet.Graphic + packet.GraphicIncrement);
        var amount = packet.Amount == 0 ? (ushort)1 : packet.Amount;
        var gridIdx = packet.HasGridIndex ? packet.GridIndex : (byte)0;

        var ent = entitiesMap.Value.GetOrCreate(commands, packet.Serial);
        var parentEnt = entitiesMap.Value.GetOrCreate(commands, packet.ContainerSerial)
            .Insert<IsContainer>();

        ent.Insert(new Graphic() { Value = finalGraphic })
            .Insert(new WorldPosition() { X = packet.X, Y = packet.Y, Z = 0 })
            .Insert(new Hue() { Value = packet.Hue })
            .Insert(new Amount() { Value = amount })
            .Insert<ContainedInto>();

        parentEnt.AddChild(ent);
        writer.Send(new ContainerUpdateEvent(packet.ContainerSerial, packet.Serial));

        hostMsgs.Send(new HostMessage.ContainerItemAdded(
            packet.ContainerSerial,
            packet.Serial,
            finalGraphic,
            amount,
            packet.X,
            packet.Y,
            gridIdx,
            packet.Hue));

        SpawnContainerItemUI(ent, assets.Value, finalGraphic, packet.X, packet.Y, packet.Hue);
    }

    private static void HandleUpdateContainerItems(
        IUpdateContainerItemsPacket packet,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        Res<AssetsServer> assets,
        EventWriter<ContainerUpdateEvent> writer,
        EventWriter<HostMessage> hostMsgs)
    {
        foreach (var item in packet.Items)
        {
            hostMsgs.Send(new HostMessage.ContainerItemAdded(
                item.ContainerSerial,
                item.Serial,
                (ushort)(item.Graphic + item.GraphicInc),
                item.Amount,
                item.X,
                item.Y,
                item.GridIndex,
                item.Hue));

            var parentEnt = entitiesMap.Value.GetOrCreate(commands, item.ContainerSerial)
                .Insert<IsContainer>();
            var ent = entitiesMap.Value.GetOrCreate(commands, item.Serial);

            var finalGraphic = (ushort)(item.Graphic + item.GraphicInc);
            ent.Insert(new Graphic() { Value = finalGraphic })
                .Insert(new Hue() { Value = item.Hue })
                .Insert(new WorldPosition() { X = item.X, Y = item.Y, Z = 0 })
                .Insert(new Amount() { Value = item.Amount })
                .Insert<ContainedInto>();
            parentEnt.AddChild(ent);

            writer.Send(new ContainerUpdateEvent(item.ContainerSerial, item.Serial));

            SpawnContainerItemUI(ent, assets.Value, finalGraphic, item.X, item.Y, item.Hue);
        }
    }

    private static void SpawnContainerItemUI(
        EntityCommands ent,
        AssetsServer assets,
        ushort graphic,
        ushort x,
        ushort y,
        ushort hue)
    {
        ref readonly var artInfo = ref assets.Arts.GetArt(graphic);
        var width = artInfo.UV.Width;
        var height = artInfo.UV.Height;

        ent.Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(x),
                Top = Val.Px(y),
                Width = Val.Px(width),
                Height = Val.Px(height),
            })
            .Insert(new UOCustomRender
            {
                Kind = UOCustomKind.Art,
                AssetId = graphic,
                Hue = new Vector3(hue, 1, 1),
            })
            .Insert(Interaction.None)
            .Insert(new FloatingWindowState
            {
                InitialX = x,
                InitialY = y,
                InitialWidth = width,
                InitialHeight = height,
            })
            .Insert<UIMovable>();
    }
}

// Snapshot of a movable container window's initial placement & size. The
// drag/resize system (not migrated yet) reads this when it first attaches.
internal struct FloatingWindowState
{
    public float InitialX;
    public float InitialY;
    public float InitialWidth;
    public float InitialHeight;
}

internal record struct ContainerOpenedEvent(uint Serial, ushort Graphic);
internal record struct ContainerClosedEvent(uint Serial);
internal record struct ContainerUpdateEvent(uint Serial, uint ItemSerial);
