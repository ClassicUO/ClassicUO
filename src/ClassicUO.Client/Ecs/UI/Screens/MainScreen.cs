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
                .Add<MainScene>()
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
            ).Add<MainScene>());

            // quit button
            root.AddChild(AddButton(
                world,
                assets,
                (0x05CA, 0x05C9, 0x05C8),
                Vector3.UnitZ,
                new(25, 240)
            ).Set(ButtonAction.Quit).Add<MainScene>());

            // credit button
            root.AddChild(AddButton(
                world,
                assets,
                (0x05D0, 0x05CF, 0x5CE),
                Vector3.UnitZ,
                new(530, 125)
            ).Set(ButtonAction.Credits).Add<MainScene>());

            // arrow button
            root.AddChild(AddButton(
                world,
                assets,
                (0x5CD, 0x5CC, 0x5CB),
                Vector3.UnitZ,
                new(280, 365)
            ).Set(ButtonAction.Login).Add<MainScene>());

            // username background
            root.AddChild(AddGumpNinePatch(
                world,
                assets,
                0x0BB8,
                Vector3.UnitZ,
                new(218, 283),
                new(210, 30)
            ).Add<TextInput>()
                .Add<MainScene>()
                .Add<UsernameInput>()
                .Set(UIInteractionState.None));

            // password background
            root.AddChild(AddGumpNinePatch(
                world,
                assets,
                0x0BB8,
                Vector3.UnitZ,
                new(218, 283 + 50),
                new(210, 30)
            ).Add<TextInput>()
                .Add<MainScene>()
                .Add<PasswordInput>()
                .Set(UIInteractionState.None));

        }, Stages.Startup, ThreadingMode.Single);


        scheduler.AddSystem((Query<Data<UIInteractionState, ButtonAction>> query,
            Single<Data<UINode>, Filter<With<UsernameInput>, With<MainScene>>> queryUsername,
            Single<Data<UINode>, Filter<With<PasswordInput>, With<MainScene>>> queryPassword) =>
        {
            foreach ((var interaction, var action) in query)
            {
                if (interaction.Ref == UIInteractionState.Released)
                {
                    Action fn = action.Ref switch
                    {
                        ButtonAction.Quit => () => Console.WriteLine("quit"),
                        ButtonAction.Credits => () => Console.WriteLine("credits"),
                        ButtonAction.Login => () =>
                        {
                            (_, var username) = queryUsername.Get();
                            (_, var password) = queryPassword.Get();
                            Login(username.Ref.Text, password.Ref.Text);
                        }
                        ,
                        _ => null
                    };

                    fn?.Invoke();
                }
            }
        }, Stages.Update, ThreadingMode.Single);
    }

    private static void Login(string username, string password)
    {
        Console.WriteLine("login --> username: {0} -  password: {1}", username, password);
    }

    enum ButtonAction : byte
    {
        Quit = 0,
        Credits = 1,
        Login = 2,
    }

    struct MainScene;
    struct UsernameInput;
    struct PasswordInput;

    private static EntityView AddLabel(TinyEcs.World world, string text, Vector2? position = null, Vector2? size = null)
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
                    layout = {
                        sizing = {
                            width = Clay_SizingAxis.Fixed(size.HasValue ? size.Value.X : 0),
                            height = Clay_SizingAxis.Fixed(size.HasValue ? size.Value.Y : 0),
                        }
                    },
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