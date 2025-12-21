using System;
using ClassicUO.Configuration;
using ClassicUO.Input;
using ClassicUO.Utility;
using Clay_cs;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.UI.Clay;
using TinyEcs.UI.Bevy;
using TinyEcs.UI;

namespace ClassicUO.Ecs;


internal readonly struct LoginScreenPlugin : IPlugin
{
    public void Build(App app)
    {
        var setupFn = Setup;
        var deleteMenuFn = DeleteMenu;

        app
            .AddState(LoginInteraction.None)

            .AddSystem(setupFn)
            .OnEnter(GameState.LoginScreen)
            .Build()

            .AddSystem((Res<NextState<LoginInteraction>> state) => state.Value.Set(LoginInteraction.None))
            .OnEnter(GameState.LoginScreen)
            .Build()

            .AddSystem(deleteMenuFn)
            .OnExit(GameState.LoginScreen)
            .Build()

            .AddPlugin<ServerSelectionPlugin>()
            .AddPlugin<CharacterSelectionPlugin>()
            .AddPlugin<LoginErrorScreenPlugin>();
    }

    private static void Setup(Commands commands, Res<GumpBuilder> gumpBuilder, Res<ClayUOCommandBuffer> clay, Res<AssetsServer> assets, Res<Settings> settings)
    {
        var root = commands.Spawn()
            .Insert<LoginScene>()
            .Insert(ClayNode.Configure()
                .WidthGrow()
                .HeightGrow()
                .Column()
                .Align(Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER, Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER)
                .Background(51, 51, 51, 255)
                .Build());

        var mainMenu = commands.Spawn()
            .Insert<LoginScene>()
            .Insert(ClayNode.Configure()
                .WidthFit()
                .HeightFit()
                .Column()
                .Background(51, 51, 51, 255)
                .Build());

        // background
        mainMenu.AddChild(gumpBuilder.Value.AddGump(
            commands,
            0x014E,
            Vector3.UnitZ
        ).Insert<LoginScene>());

        // quit button
        mainMenu.AddChild(gumpBuilder.Value.AddButton(
            commands,
            (0x05CA, 0x05C9, 0x05C8),
            Vector3.UnitZ,
            new(25, 240)
        ).Insert(ButtonAction.Quit).Insert<LoginScene>());

        // credit button
        mainMenu.AddChild(gumpBuilder.Value.AddButton(
            commands,
            (0x05D0, 0x05CF, 0x5CE),
            Vector3.UnitZ,
            new(530, 125)
        ).Insert(ButtonAction.Credits).Insert<LoginScene>()
        .Observe<On<ClayPointerEvent>>((On<ClayPointerEvent> trigger) =>
        {
            if (trigger.Event.EventType == ClayPointerEventType.Pressed)
                Console.WriteLine("pressed button");
            else if (trigger.Event.EventType == ClayPointerEventType.Released)
                Console.WriteLine("released button");
        }));

        // arrow button
        var arrowButton = gumpBuilder.Value.AddButton(
            commands,
            (0x5CD, 0x5CC, 0x5CB),
            Vector3.UnitZ,
            new(280, 365)
        )
        .Insert(ButtonAction.Login).Insert<LoginScene>()
        .Observe((
            On<ClayPointerEvent> trigger,
            Commands commands,
            Res<Settings> settings,
            ResMut<NextState<LoginInteraction>> state
        ) =>
        {
            if (trigger.Event is { EventType: ClayPointerEventType.Click, IsLeftButton: true })
            {
                Console.WriteLine("login button clicked");
                // TODO: Get username and password from text input components once implemented
                Login(commands, settings.Value, settings.Value.Username, Crypter.Decrypt(settings.Value.Password));
                state.Value.Set(LoginInteraction.LoginRequested);
            }
        });

        mainMenu.AddChild(arrowButton);

        var usernameBackground = gumpBuilder.Value.AddGumpNinePatch(
            commands,
            0x0BB8,
            Vector3.UnitZ,
            new(218, 283),
            new(210, 30))
            .Insert<LoginScene>();

        var usernameField = commands.Spawn()
            .Insert<UsernameInput>()
            .Insert<TextInput>()
            .Insert(ClayNode.Configure()
            .WidthFit()
            .HeightFit()
            .Text(settings.Value.Username, 20)
            .Build());

        usernameBackground.AddChild(usernameField);
        mainMenu.AddChild(usernameBackground);

        var passwordBackground = gumpBuilder.Value.AddGumpNinePatch(
            commands,
            0x0BB8,
            Vector3.UnitZ,
            new(218, 283 + 50),
            new(210, 30))
            .Insert<LoginScene>()
            ;

        var psw = Crypter.Decrypt(settings.Value.Password);
        var masked = new string('*', psw.Length);
        var passwordField = commands.Spawn()
            .Insert(new PasswordInput() { Masked = masked, Real = psw })
            .Insert<TextInput>()
            .Insert(ClayNode.Configure()
            .WidthFit()
            .HeightFit()
            .Text(masked, 20)
            .Build());

        passwordBackground.AddChild(passwordField);
        mainMenu.AddChild(passwordBackground);

        root.AddChild(mainMenu);
    }

    private static void DeleteMenu(Commands commands, Query<Data<ClayNode>, Filter<Without<Parent>, With<LoginScene>>> query)
    {
        Console.WriteLine("[LoginScreen] cleanup start");
        foreach ((var ent, _) in query)
            commands.Entity(ent.Ref).Despawn();
        Console.WriteLine("[LoginScreen] cleanup done");
    }

    private static void Login(Commands commands, Settings settings, string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            Console.WriteLine("username or password is empty");
            return;
        }

        settings.Username = username;
        settings.Password = Crypter.Encrypt(password);

        Console.WriteLine("doing login");

        commands.EmitTrigger(new OnLoginRequest
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
    private struct PasswordInput
    {
        public string Real;
        public string Masked;
    }
}
