using System;
using Clay_cs;
using Microsoft.Xna.Framework;
using TinyEcs;

namespace ClassicUO.Ecs;

internal readonly struct MainScreenPlugin : IPlugin
{
    public unsafe void Build(Scheduler scheduler)
    {
        scheduler.AddSystem((TinyEcs.World world, Res<ClayUOCommandBuffer> clay, Res<AssetsServer> assets) =>
        {
            ref readonly var gumpInfo = ref assets.Value.Gumps.GetGump(0x123);

            var root = world.Entity()
                .Set(new UINode()
                {
                    Config = {
                        backgroundColor = new (48, 48, 48, 255),
                        layout = {
                            sizing = {
                                width = Clay_SizingAxis.Percent(0.3f),
                                height = Clay_SizingAxis.Percent(0.3f),
                            },
                            // childAlignment = {
                            //     x = Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER,
                            //     y = Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER
                            // },
                            layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                            childGap = 8
                        },
                        scroll = {
                            vertical = true
                        }
                    }
                });

            var child = world.Entity()
                .Set(new UINode()
                {
                    // Config = {
                    //     custom = {
                    //         customData = (void*) clay.Value.AddGumpCommand(0x123, default, Vector3.UnitZ),
                    //     }
                    // }
                    Config = {
                        layout = {
                            sizing = {
                                width = Clay_SizingAxis.Fixed(gumpInfo.UV.Width),
                                height = Clay_SizingAxis.Fixed(gumpInfo.UV.Height),
                            }
                        }
                    },
                    UOConfig = {
                        Type = ClayUOCommandType.Gump,
                        Id = 0x123,
                        Position = default,
                        Hue = Vector3.UnitZ,
                    }
                });

            root.AddChild(child);

        }, Stages.Startup, ThreadingMode.Single);
    }
}