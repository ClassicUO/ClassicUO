using ClassicUO.Assets;
using ClassicUO.Ecs.Modding.Host;
using ClassicUO.Game.Data;
using ClassicUO.IO;
using ClassicUO.Network;
using ClassicUO.Utility;
using Clay_cs;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using TinyEcs;
using TinyEcs.Bevy;

namespace ClassicUO.Ecs;

internal readonly struct ContainersPlugin : IPlugin
{
    public void Build(App app)
    {
        var onOpenContainersFn = OnOpenContainers;
        var closeContainersTooFarFromPlayerFn = CloseContainersTooFarFromPlayer;
        var processPacketsFn = ProcessContainerPackets;

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
            .Build();
    }


    private static void CloseContainersTooFarFromPlayer(
        Commands commands,
        Query<Data<WorldPosition, NetworkSerial>,
             Filter<With<IsContainer>, With<UINode>, With<UIMouseAction>, With<UIMovable>>> query,
        Single<Data<WorldPosition>, With<Player>> queryPlayer,
        EventWriter<HostMessage> hostMsgs
    )
    {
        const int MAX_CONTAINER_DIST = 5;
        (_, var playerPos) = queryPlayer.Get();

        foreach ((var ent, var pos, var serial) in query)
        {
            if (Math.Abs(playerPos.Ref.X - pos.Ref.X) >= MAX_CONTAINER_DIST ||
                Math.Abs(playerPos.Ref.Y - pos.Ref.Y) >= MAX_CONTAINER_DIST)
            {
                commands.Entity(ent.Ref)
                    .Remove<UINode>()
                    .Remove<UIMouseAction>()
                    .Remove<UIMovable>();

                hostMsgs.Send(new HostMessage.ContainerClosed(serial.Ref.Value));
            }
        }
    }

    private static void OnOpenContainers(
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        Res<AssetsServer> assets,
        EventReader<ContainerOpenedEvent> reader
    )
    {
        foreach (var ev in reader.Read())
        {
            if (ev.Graphic == 0xFFFF)
            {

            }
            else if (ev.Graphic == 0x0030)
            {

            }
            else
            {
                ref readonly var gumpInfo = ref assets.Value.Gumps.GetGump(ev.Graphic);
                var ent = entitiesMap.Value.GetOrCreate(commands, ev.Serial)
                    .InsertBundle(new UINodeBundle()
                    {
                        Node = new UINode()
                        {
                            Config =
                            {
                                layout =
                                {
                                    sizing =
                                    {
                                        width = Clay_SizingAxis.Fixed(gumpInfo.UV.Width),
                                        height = Clay_SizingAxis.Fixed(gumpInfo.UV.Height),
                                    },
                                    layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM
                                },
                                floating = {
                                    clipTo = Clay_FloatingClipToElement.CLAY_CLIP_TO_ATTACHED_PARENT,
                                    attachTo = Clay_FloatingAttachToElement.CLAY_ATTACH_TO_PARENT,
                                    offset = {
                                        x = 0,
                                        y = 0
                                    }
                                }
                            },
                            UOConfig = {
                                Type = ClayUOCommandType.Gump,
                                Id = ev.Graphic,
                                Hue = Vector3.UnitZ,
                            }
                        }
                    })
                    .Insert<UIMovable>()
                    .Observe(static (On<OnReleased> trigger, Commands commands) =>
                    {
                        if (trigger.Event.Button != Input.MouseButtonType.Right)
                            return;

                        commands.Entity(trigger.EntityId)
                            .Remove<UINode>()
                            .Remove<UIMouseAction>()
                            .Remove<UIMovable>();

                        // hostMsgs.Send(new HostMessage.ContainerClosed(serial.Ref.Value));
                    })
                    .Observe(static (OnDespawn trigger) =>
                    {

                    });
            }
        }
    }

    private static void ProcessContainerPackets(
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        Res<AssetsServer> assets,
        EventWriter<ContainerUpdateEvent> writer,
        EventWriter<ContainerOpenedEvent> openedWriter,
        EventWriter<HostMessage> hostMsgs,
        EventReader<IPacket> packets
    )
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
        EventWriter<HostMessage> hostMsgs
    )
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
            packet.Hue
        ));

        ref readonly var artInfo = ref assets.Value.Arts.GetArt(finalGraphic);

        ent.Insert<UIMovable>()
            .Insert(new UIMouseAction())
            .InsertBundle(new UINodeBundle()
            {

                Node = new UINode
                {
                    Config =
                    {
                        layout =
                        {
                            sizing =
                            {
                                width = Clay_SizingAxis.Fixed(artInfo.UV.Width),
                                height = Clay_SizingAxis.Fixed(artInfo.UV.Height),
                            },
                            layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM
                        },
                        floating =
                        {
                            clipTo = Clay_FloatingClipToElement.CLAY_CLIP_TO_ATTACHED_PARENT,
                            attachTo = Clay_FloatingAttachToElement.CLAY_ATTACH_TO_PARENT,
                            offset =
                            {
                                x = packet.X,
                                y = packet.Y
                            }
                        }
                    },
                    UOConfig =
                    {
                        Type = ClayUOCommandType.Art,
                        Id = finalGraphic,
                        Hue = new Vector3(packet.Hue, 1, 1),
                    }
                }
            });
    }

    private static void HandleUpdateContainerItems(
        IUpdateContainerItemsPacket packet,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        Res<AssetsServer> assets,
        EventWriter<ContainerUpdateEvent> writer,
        EventWriter<HostMessage> hostMsgs
    )
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
               item.Hue
           ));

            var parentEnt = entitiesMap.Value.GetOrCreate(commands, item.ContainerSerial)
                .Insert<IsContainer>();
            var ent = entitiesMap.Value.GetOrCreate(commands, item.Serial);

            ent.Insert(new Graphic() { Value = (ushort)(item.Graphic + item.GraphicInc) })
                .Insert(new Hue() { Value = item.Hue })
                .Insert(new WorldPosition() { X = item.X, Y = item.Y, Z = 0 })
                .Insert(new Amount() { Value = item.Amount })
                .Insert<ContainedInto>();
            parentEnt.AddChild(ent);

            writer.Send(new ContainerUpdateEvent(item.ContainerSerial, item.Serial));

            ref readonly var artInfo = ref assets.Value.Arts.GetArt((ushort)(item.Graphic + item.GraphicInc));

            ent.Insert<UIMovable>()
                .Insert(new UIMouseAction())
                .InsertBundle(new UINodeBundle()
                {
                    Node = new UINode
                    {
                        Config =
                        {
                            layout =
                            {
                                sizing =
                                {
                                    width = Clay_SizingAxis.Fixed(artInfo.UV.Width),
                                    height = Clay_SizingAxis.Fixed(artInfo.UV.Height),
                                },
                                layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM
                            },
                            floating =
                            {
                                clipTo = Clay_FloatingClipToElement.CLAY_CLIP_TO_ATTACHED_PARENT,
                                attachTo = Clay_FloatingAttachToElement.CLAY_ATTACH_TO_PARENT,
                                offset =
                                {
                                    x = item.X,
                                    y = item.Y
                                }
                            }
                        },
                        UOConfig =
                        {
                            Type = ClayUOCommandType.Art,
                            Id = (ushort)(item.Graphic + item.GraphicInc),
                            Hue = new Vector3(item.Hue, 1, 1),
                        }
                    }
                });
        }
    }
}

internal record struct ContainerOpenedEvent(uint Serial, ushort Graphic);
internal record struct ContainerClosedEvent(uint Serial);
internal record struct ContainerUpdateEvent(uint Serial, uint ItemSerial);
