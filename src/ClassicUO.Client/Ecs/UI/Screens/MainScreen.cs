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
            var root = world.Entity()
                .Set(new UINode()
                {
                    Config = {
                        backgroundColor = new (48, 48, 48, 255),
                        layout = {
                            sizing = {
                                width = Clay_SizingAxis.Fit(0, 0),
                                height = Clay_SizingAxis.Fit(0, 0),
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



            root.AddChild(AddGump(world, assets, 0x014E, Vector3.UnitZ));
            root.AddChild(AddGump(world, assets, 0x05CA, Vector3.UnitZ, new(25, 240))
                .Set(UIInteractionState.None));

        }, Stages.Startup, ThreadingMode.Single);
    }

    private static EntityView AddGump(TinyEcs.World world, AssetsServer assets, ushort id, Vector3 hue, Vector2? position = null)
    {
        ref readonly var gumpInfo = ref assets.Gumps.GetGump(id);
        return world.Entity()
            .Set(new UINode()
            {
                Config = {
                    layout = {
                        sizing = {
                            width = Clay_SizingAxis.Fixed(gumpInfo.UV.Width),
                            height = Clay_SizingAxis.Fixed(gumpInfo.UV.Height),
                        }
                    },
                    floating = {
                        attachTo = position.HasValue ? Clay_FloatingAttachToElement.CLAY_ATTACH_TO_PARENT : Clay_FloatingAttachToElement.CLAY_ATTACH_TO_NONE,
                        offset = {
                            x = position?.X ?? 0,
                            y = position?.Y ?? 0
                        }
                    }
                },
                UOConfig = {
                    Type = ClayUOCommandType.Gump,
                    Id = id,
                    Hue = hue,
                }
            });
    }
}