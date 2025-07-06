using System;
using ClassicUO.Configuration;
using ClassicUO.Input;
using ClassicUO.Utility;
using Clay_cs;
using Microsoft.Xna.Framework;
using TinyEcs;

namespace ClassicUO.Ecs;


internal readonly struct LoginScreenPlugin : IPlugin
{
    public void Build(Scheduler scheduler)
    {
        var setupFn = Setup;
        var buttonsHandlerFn = ButtonsHandler;
        var deleteMenuFn = DeleteMenu;

        scheduler.AddState<LoginInteraction>();

        scheduler.OnUpdate(buttonsHandlerFn)
            .RunIf((SchedulerState state) => state.InState(GameState.LoginScreen))
            .RunIf((SchedulerState state) => state.InState(LoginInteraction.None));
        scheduler.OnEnter(GameState.LoginScreen, setupFn);
        scheduler.OnEnter(GameState.LoginScreen, (State<LoginInteraction> state) => state.Set(LoginInteraction.None));
        scheduler.OnExit(GameState.LoginScreen, deleteMenuFn);
    }

    private static void Setup(TinyEcs.World world, Res<GumpBuilder> gumpBuilder, Res<ClayUOCommandBuffer> clay, Res<AssetsServer> assets, Res<Settings> settings)
    {
        var root = world.Entity()
            .Add<LoginScene>()
            .Set(new UINode()
            {
                Config = {
                    backgroundColor = new (0.2f, 0.2f, 0.2f, 1),
                    layout = {
                        sizing = {
                            width = Clay_SizingAxis.Grow(),
                            height = Clay_SizingAxis.Grow(),
                        },
                        childAlignment = {
                            x = Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER,
                            y = Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER,
                        },
                        layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                    }
                }
            });

        var mainMenu = world.Entity()
            .Add<LoginScene>()
            .Set(new UINode()
            {
                Config = {
                    backgroundColor = new (0.2f, 0.2f, 0.2f, 1),
                    layout = {
                        sizing = {
                            width = Clay_SizingAxis.Fit(0, 0),
                            height = Clay_SizingAxis.Fit(0, 0),
                        },
                        layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                    }
                }
            });

        // background
        mainMenu.AddChild(gumpBuilder.Value.AddGump(
            0x014E,
            Vector3.UnitZ
        ).Add<LoginScene>());

        // quit button
        mainMenu.AddChild(gumpBuilder.Value.AddButton(
            (0x05CA, 0x05C9, 0x05C8),
            Vector3.UnitZ,
            new(25, 240)
        ).Set(ButtonAction.Quit).Add<LoginScene>());

        // credit button
        mainMenu.AddChild(gumpBuilder.Value.AddButton(
            (0x05D0, 0x05CF, 0x5CE),
            Vector3.UnitZ,
            new(530, 125)
        ).Set(ButtonAction.Credits).Add<LoginScene>());

        // arrow button
        mainMenu.AddChild(gumpBuilder.Value.AddButton(
            (0x5CD, 0x5CC, 0x5CB),
            Vector3.UnitZ,
            new(280, 365)
        ).Set(ButtonAction.Login).Add<LoginScene>());

        // username background
        mainMenu.AddChild(gumpBuilder.Value.AddGumpNinePatch(
            0x0BB8,
            Vector3.UnitZ,
            new(218, 283),
            new(210, 30))
            .Set(new Text()
            {
                Value = settings.Value.Username,
                TextConfig = {
                    fontId = 0,
                    fontSize = 24,
                    textColor = new (0.2f, 0.2f, 0.2f, 1),
                },
            })
            .Add<TextInput>()
            .Add<LoginScene>()
            .Add<UsernameInput>()
            .Set(new UIMouseAction()));

        // password background
        mainMenu.AddChild(gumpBuilder.Value.AddGumpNinePatch(
            0x0BB8,
            Vector3.UnitZ,
            new(218, 283 + 50),
            new(210, 30))
            .Set(new Text()
            {
                Value = Crypter.Decrypt(settings.Value.Password),
                ReplaceChar = '*',
                TextConfig = {
                    fontId = 0,
                    fontSize = 24,
                    textColor = new (1, 1, 1, 1),
                },
            })
            .Add<TextInput>()
            .Add<LoginScene>()
            .Add<PasswordInput>()
            .Set(new UIMouseAction()));

        root.AddChild(mainMenu);
    }

    private static void ButtonsHandler(
        Query<Data<UIMouseAction, ButtonAction>, Changed<UIMouseAction>> query,
        Res<Settings> settings,
        State<LoginInteraction> state,
        EventWriter<OnLoginRequest> writer,
        Single<Data<Text>, Filter<With<UsernameInput>, With<LoginScene>, With<TextInput>>> queryUsername,
        Single<Data<Text>, Filter<With<PasswordInput>, With<LoginScene>, With<TextInput>>> queryPassword
    )
    {
        foreach ((var interaction, var action) in query)
        {
            if (interaction.Ref is not { State: UIInteractionState.Released, Button: MouseButtonType.Left })
            {
                continue;
            }

            Action fn = action.Ref switch
            {
                ButtonAction.Quit => () => Console.WriteLine("quit"),
                ButtonAction.Credits => () => Console.WriteLine("credits"),
                ButtonAction.Login => () =>
                {
                    (_, var username) = queryUsername.Get();
                    (_, var password) = queryPassword.Get();
                    Login(writer, settings, username.Ref.Value, password.Ref.Value);
                    state.Set(LoginInteraction.LoginRequested);
                }
                ,
                _ => null
            };

            fn?.Invoke();
        }
    }

    private static void DeleteMenu(World world, Query<Data<UINode>, Filter<Without<Parent>, With<LoginScene>>> query)
    {
        Console.WriteLine("[LoginScreen] cleanup start");
        foreach ((var ent, _) in query)
            world.Delete(ent.Ref);
        Console.WriteLine("[LoginScreen] cleanup done");
    }

    private static void Login(EventWriter<OnLoginRequest> writer, Settings settings, string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            Console.WriteLine("username or password is empty");
            return;
        }

        settings.Username = username;
        settings.Password = Crypter.Encrypt(password);

        Console.WriteLine("doing login");

        writer.Enqueue(new()
        {
            Username = settings.Username,
            Password = settings.Password,
            Address = settings.IP,
            Port = settings.Port,
        });
    }

    private enum ButtonAction : byte
    {
        Quit = 0,
        Credits = 1,
        Login = 2,
    }

    private enum LoginInteraction : byte
    {
        None,
        LoginRequested
    }

    private struct LoginScene;
    private struct UsernameInput;
    private struct PasswordInput;
}
