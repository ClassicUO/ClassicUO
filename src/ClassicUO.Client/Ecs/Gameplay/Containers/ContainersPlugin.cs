using System;
using ClassicUO.Assets;
using ClassicUO.IO;
using ClassicUO.Network;
using ClassicUO.Utility;
using Clay_cs;
using Microsoft.Xna.Framework;
using TinyEcs;

namespace ClassicUO.Ecs;

internal readonly struct ContainersPlugin : IPlugin
{
    public void Build(Scheduler scheduler)
    {
        scheduler.AddEvent<ContainerOpenedEvent>();
        scheduler.AddEvent<ContainerClosedEvent>();
        scheduler.AddEvent<ContainerUpdateEvent>();

        var registerOpenContainerFn = RegisterOpenContainer;
        scheduler.OnStartup(registerOpenContainerFn);

        var registerUpdateContainerFn = RegisterUpdateContainer;
        scheduler.OnStartup(registerUpdateContainerFn);

        var onOpenContainersFn = OnOpenContainers;
        scheduler.OnUpdate(onOpenContainersFn)
            .RunIf((EventReader<ContainerOpenedEvent> reader) => !reader.IsEmpty);
    }


    private static void OnOpenContainers(
        World world,
        Res<NetworkEntitiesMap> entitiesMap,
        Res<AssetsServer> assets,
        EventReader<ContainerOpenedEvent> reader
    )
    {
        foreach (var ev in reader)
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
                var ent = entitiesMap.Value.GetOrCreate(world, ev.Serial);
                ent.Set
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
                .Set(UIInteractionState.None)
                .Add<UIMovable>()
                ;
            }
        }
    }

    private static void RegisterOpenContainer(
        Res<PacketsMap> packets,
        EventWriter<ContainerOpenedEvent> writer
    )
    {
        // open container
        packets.Value[0x24] = buffer =>
        {
            var reader = new StackDataReader(buffer);

            var serial = reader.ReadUInt32BE();
            var graphic = reader.ReadUInt16BE();

            writer.Enqueue(new(serial, graphic));
        };
    }

    private static void RegisterUpdateContainer(
        Res<PacketsMap> packets,
        Res<GameContext> gameCtx,
        Res<NetworkEntitiesMap> entitiesMap,
        Res<AssetsServer> assets,
        World world,
        EventWriter<ContainerUpdateEvent> writer
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

            var ent = entitiesMap.Value.GetOrCreate(world, serial);
            var parentEnt = entitiesMap.Value.GetOrCreate(world, containerSerial);

            ent.Set(new Graphic() { Value = (ushort)(graphic + graphicInc) })
                .Set(new WorldPosition() { X = x, Y = y, Z = 0 })
                .Set(new Hue() { Value = hue })
                .Set(new Amount() { Value = amount })
                .Add<ContainedInto>()
                ;

            parentEnt.AddChild(ent);


            ref readonly var artInfo = ref assets.Value.Arts.GetArt((ushort)(graphic + graphicInc));

            ent.Add<UIMovable>().Set(UIInteractionState.None).Set
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

                var parentEnt = entitiesMap.Value.GetOrCreate(world, containerSerial);
                var ent = entitiesMap.Value.GetOrCreate(world, serial);
                ent.Set(new Graphic() { Value = (ushort)(graphic + graphicInc) })
                    .Set(new Hue() { Value = hue })
                    .Set(new WorldPosition() { X = x, Y = y, Z = 0 })
                    .Set(new Amount() { Value = amount })
                    .Add<ContainedInto>();
                parentEnt.AddChild(ent);

                ref readonly var artInfo = ref assets.Value.Arts.GetArt((ushort)(graphic + graphicInc));

                ent.Add<UIMovable>().Set(UIInteractionState.None).Set
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
