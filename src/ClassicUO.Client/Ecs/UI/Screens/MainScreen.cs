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

            // background
            root.AddChild(AddGump(
                world,
                assets,
                0x014E,
                Vector3.UnitZ
            ));

            // quit button
            root.AddChild(AddButton(
                world,
                assets,
                (0x05CA, 0x05C9, 0x05C8),
                Vector3.UnitZ,
                new(25, 240)
            ).Set(ButtonAction.Quit));

            // credit button
            root.AddChild(AddButton(
                world,
                assets,
                (0x05D0, 0x05CF, 0x5CE),
                Vector3.UnitZ,
                new(530, 125)
            ).Set(ButtonAction.Credits));

            // arrow button
            root.AddChild(AddButton(
                world,
                assets,
                (0x5CD, 0x5CC, 0x5CB),
                Vector3.UnitZ,
                new(280, 365)
            ).Set(ButtonAction.Arrow));

            // username background
            root.AddChild(AddGumpNinePatch(
                world,
                assets,
                0x0BB8,
                Vector3.UnitZ,
                new(218, 283),
                new(210, 30)
            ));

            // password background
            root.AddChild(AddGumpNinePatch(
                world,
                assets,
                0x0BB8,
                Vector3.UnitZ,
                new(218, 283 + 50),
                new(210, 30)
            ));

            root.AddChild(AddLabel(world, "HELLO!!", new(218, 283)));

        }, Stages.Startup, ThreadingMode.Single);


        scheduler.AddSystem((Query<Data<UIInteractionState, ButtonAction>> query) =>
        {
            foreach ((var interaction, var action) in query)
            {
                if (interaction.Ref == UIInteractionState.Released)
                {
                    Action fn = action.Ref switch
                    {
                        ButtonAction.Quit => () => Console.WriteLine("quit"),
                        ButtonAction.Credits => () => Console.WriteLine("credits"),
                        ButtonAction.Arrow => () => Console.WriteLine("arrow"),
                        _ => null
                    };

                    fn?.Invoke();
                }
            }
        }, Stages.Update, ThreadingMode.Single);
    }

    enum ButtonAction : byte
    {
        Quit = 0,
        Credits = 1,
        Arrow = 2,
    }


    private static EntityView AddLabel(TinyEcs.World world, string text, Vector2? position = null)
    {
        return world.Entity()
            .Set(new UINode()
            {
                Text = text,
                TextConfig = {
                    fontId = 0,
                    fontSize = 12,
                    textColor = new (255, 255, 255, 255),
                },
                Config = {
                    floating = {
                        attachTo = position.HasValue ? Clay_FloatingAttachToElement.CLAY_ATTACH_TO_PARENT : Clay_FloatingAttachToElement.CLAY_ATTACH_TO_NONE,
                        offset = {
                            x = position?.X ?? 0,
                            y = position?.Y ?? 0
                        }
                    }
                }
            });
    }

    private static EntityView AddButton(TinyEcs.World world, AssetsServer assets, (ushort normal, ushort pressed, ushort over) ids, Vector3 hue, Vector2? position = null)
    {
        return AddGump(world, assets, ids.normal, hue, position)
            .Set(UIInteractionState.None)
            .Set(new UOButton() { Normal = ids.normal, Pressed = ids.pressed, Over = ids.over });
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

    private static EntityView AddGumpNinePatch(TinyEcs.World world, AssetsServer assets, ushort id, Vector3 hue, Vector2? position = null, Vector2? size = null)
    {
        ref readonly var gumpInfo = ref assets.Gumps.GetGump(id);
        return world.Entity()
            .Set(new UINode()
            {
                Config = {
                    layout = {
                        sizing = {
                            width = Clay_SizingAxis.Fixed(size.HasValue ? size.Value.X : gumpInfo.UV.Width),
                            height = Clay_SizingAxis.Fixed(size.HasValue ? size.Value.Y : gumpInfo.UV.Height),
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
                    Type = ClayUOCommandType.GumpNinePatch,
                    Id = id,
                    Hue = hue,
                }
            });
    }
}