using System;
using ClassicUO.Configuration;
using ClassicUO.Utility;
using Clay_cs;
using Microsoft.Xna.Framework;
using TinyEcs;

namespace ClassicUO.Ecs;


internal readonly struct LoginScreenPlugin : IPlugin
{
    public unsafe void Build(Scheduler scheduler)
    {
        var setupFn = Setup;
        var buttonsHandlerFn = ButtonsHandler;
        var deleteMenuFn = DeleteMenu;

        scheduler.AddState<LoginInteraction>();

        scheduler.OnUpdate(buttonsHandlerFn, ThreadingMode.Single)
            .RunIf((SchedulerState state) => state.InState(GameState.LoginScreen))
            .RunIf((SchedulerState state) => state.InState(LoginInteraction.None));
        scheduler.OnEnter(GameState.LoginScreen, setupFn, ThreadingMode.Single);
        scheduler.OnEnter(GameState.LoginScreen, (State<LoginInteraction> state) => state.Set(LoginInteraction.None), ThreadingMode.Single);
        scheduler.OnExit(GameState.LoginScreen, deleteMenuFn, ThreadingMode.Single);
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
                        layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                    }
                }
            });

        // background
        root.AddChild(gumpBuilder.Value.AddGump(
            0x014E,
            Vector3.UnitZ,
            new(0, 0)
        ).Add<LoginScene>());

        // quit button
        root.AddChild(gumpBuilder.Value.AddButton(
            (0x05CA, 0x05C9, 0x05C8),
            Vector3.UnitZ,
            new(25, 240)
        ).Set(ButtonAction.Quit).Add<LoginScene>());

        // credit button
        root.AddChild(gumpBuilder.Value.AddButton(
            (0x05D0, 0x05CF, 0x5CE),
            Vector3.UnitZ,
            new(530, 125)
        ).Set(ButtonAction.Credits).Add<LoginScene>());

        // arrow button
        root.AddChild(gumpBuilder.Value.AddButton(
            (0x5CD, 0x5CC, 0x5CB),
            Vector3.UnitZ,
            new(280, 365)
        ).Set(ButtonAction.Login).Add<LoginScene>());

        // username background
        root.AddChild(gumpBuilder.Value.AddGumpNinePatch(
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
            .Set(UIInteractionState.None));

        // password background
        root.AddChild(gumpBuilder.Value.AddGumpNinePatch(
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
            .Set(UIInteractionState.None));
    }

    private static void ButtonsHandler(
        Query<Data<UIInteractionState, ButtonAction>, Changed<UIInteractionState>> query,
        Res<Settings> settings,
        State<LoginInteraction> state,
        EventWriter<OnLoginRequest> writer,
        Single<Data<Text>, Filter<With<UsernameInput>, With<LoginScene>, With<TextInput>>> queryUsername,
        Single<Data<Text>, Filter<With<PasswordInput>, With<LoginScene>, With<TextInput>>> queryPassword
    )
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
                        Login(writer, settings, username.Ref.Value, password.Ref.Value);
                        state.Set(LoginInteraction.LoginRequested);
                    }
                    ,
                    _ => null
                };

                fn?.Invoke();
            }
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
