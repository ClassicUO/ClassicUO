using System;
using ClassicUO.Assets;
using ClassicUO.Ecs.Modding.Host;
using ClassicUO.IO;
using ClassicUO.Network;
using ClassicUO.Utility;
using Clay_cs;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;

namespace ClassicUO.Ecs;

internal readonly struct ContainersPlugin : IPlugin
{
    public void Build(App app)
    {
        var registerOpenContainerFn = RegisterOpenContainer;
        app.AddSystem(Stage.Startup, registerOpenContainerFn);

        var registerUpdateContainerFn = RegisterUpdateContainer;
        app.AddSystem(Stage.Startup, registerUpdateContainerFn);

        var onOpenContainersFn = OnOpenContainers;
        var closeContainersTooFarFromPlayerFn = CloseContainersTooFarFromPlayer;

        app
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
                ent.Ref.Unset<UINode>();
                ent.Ref.Unset<UIMouseAction>();
                ent.Ref.Unset<UIMovable>();

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
                    .CreateUINode
                    (
                        new UINode()
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
                    )
                    .Insert(new UIMouseAction())
                    .Insert<UIMovable>();
            }
        }
    }

    private static void RegisterOpenContainer(
        Res<PacketsMap> packets,
        EventWriter<ContainerOpenedEvent> writer,
        EventWriter<HostMessage> hostMsgs
    )
    {
        // open container
        packets.Value[0x24] = buffer =>
        {
            var reader = new StackDataReader(buffer);

            var serial = reader.ReadUInt32BE();
            var graphic = reader.ReadUInt16BE();

            writer.Send(new(serial, graphic));
            hostMsgs.Send(new HostMessage.ContainerOpened(serial, graphic));
        };
    }

    private static void RegisterUpdateContainer(
        Commands commands,
        Res<PacketsMap> packets,
        Res<GameContext> gameCtx,
        Res<NetworkEntitiesMap> entitiesMap,
        Res<AssetsServer> assets,
        EventWriter<ContainerUpdateEvent> writer,
        EventWriter<HostMessage> hostMsgs
    )
    {
        // update container
        packets.Value[0x25] = buffer =>
        {
            var reader = new StackDataReader(buffer);

            var serial = reader.ReadUInt32BE();
            var graphic = reader.ReadUInt16BE();
            var graphicInc = reader.ReadInt8();
            var amount = Math.Max((ushort)1, reader.ReadUInt16BE());
            (var x, var y) = (reader.ReadUInt16BE(), reader.ReadUInt16BE());

            if (gameCtx.Value.ClientVersion >= ClientVersion.CV_6017)
                reader.Skip(1);

            var containerSerial = reader.ReadUInt32BE();
            var hue = reader.ReadUInt16BE();

            var ent = entitiesMap.Value.GetOrCreate(commands, serial);
            var parentEnt = entitiesMap.Value.GetOrCreate(commands, containerSerial)
                .Insert<IsContainer>();

            ent.Insert(new Graphic() { Value = (ushort)(graphic + graphicInc) })
                .Insert(new WorldPosition() { X = x, Y = y, Z = 0 })
                .Insert(new Hue() { Value = hue })
                .Insert(new Amount() { Value = amount })
                .Insert<ContainedInto>()
                ;

            parentEnt.AddChild(ent);

            hostMsgs.Send(new HostMessage.ContainerItemAdded
            (
                containerSerial,
                serial,
                (ushort)(graphic + graphicInc),
                amount,
                x,
                y,
                0,
                hue
            ));

            ref readonly var artInfo = ref assets.Value.Arts.GetArt((ushort)(graphic + graphicInc));

            ent.Insert<UIMovable>()
                .Insert(new UIMouseAction())
                .CreateUINode
                (
                    new UINode()
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
                                    x = x,
                                    y = y
                                }
                            }
                        },
                        UOConfig =
                        {
                            Type = ClayUOCommandType.Art,
                            Id = (ushort)(graphic + graphicInc),
                            Hue = new Vector3(hue, 1, 1),
                        }
                    }
                );
        };

        // update contained items
        packets.Value[0x3C] = buffer =>
        {
            var reader = new StackDataReader(buffer);

            var count = reader.ReadUInt16BE();

            for (var i = 0; i < count; ++i)
            {
                var serial = reader.ReadUInt32BE();
                var graphic = reader.ReadUInt16BE();
                var graphicInc = reader.ReadUInt8();
                var amount = Math.Max((ushort)1, reader.ReadUInt16BE());
                (var x, var y) = (reader.ReadUInt16BE(), reader.ReadUInt16BE());
                var gridIdx = gameCtx.Value.ClientVersion < ClientVersion.CV_6017 ?
                    0 : reader.ReadUInt8();
                var containerSerial = reader.ReadUInt32BE();
                var hue = reader.ReadUInt16BE();

                hostMsgs.Send(new HostMessage.ContainerItemAdded
                (
                    containerSerial,
                    serial,
                    (ushort)(graphic + graphicInc),
                    amount,
                    x,
                    y,
                    gridIdx,
                    hue
                ));

                var parentEnt = entitiesMap.Value.GetOrCreate(commands, containerSerial)
                    .Insert<IsContainer>();
                var ent = entitiesMap.Value.GetOrCreate(commands, serial);
                ent.Insert(new Graphic() { Value = (ushort)(graphic + graphicInc) })
                    .Insert(new Hue() { Value = hue })
                    .Insert(new WorldPosition() { X = x, Y = y, Z = 0 })
                    .Insert(new Amount() { Value = amount })
                    .Insert<ContainedInto>();
                parentEnt.AddChild(ent);

                ref readonly var artInfo = ref assets.Value.Arts.GetArt((ushort)(graphic + graphicInc));

                ent.Insert<UIMovable>()
                    .Insert(new UIMouseAction())
                    .CreateUINode
                    (
                        new UINode()
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
                                        x = x,
                                        y = y
                                    }
                                }
                            },
                            UOConfig =
                            {
                                Type = ClayUOCommandType.Art,
                                Id = (ushort)(graphic + graphicInc),
                                Hue = new Vector3(hue, 1, 1),
                            }
                        }
                    );
            }
        };
    }
}

internal record struct ContainerOpenedEvent(uint Serial, ushort Graphic);
internal record struct ContainerClosedEvent(uint Serial);
internal record struct ContainerUpdateEvent(uint Serial, uint ItemSerial);
