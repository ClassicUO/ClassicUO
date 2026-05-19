using System;
using System.Numerics;
using ClassicUO.Configuration;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using ClayColor = Clay.Color;
using XnaVector2 = Microsoft.Xna.Framework.Vector2;
using XnaVector3 = Microsoft.Xna.Framework.Vector3;

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

    private static void Setup(
        Commands commands,
        Res<GumpBuilder> gumpBuilder,
        Res<Settings> settings)
    {
        var bg = new ClayColor(51, 51, 51, 255);

        // Root: full-screen column, center-aligned.
        var root = commands.Spawn()
            .Insert<LoginScene>()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Relative,
                FlexDirection = FlexDirection.Column,
                JustifyContent = JustifyContent.Center,
                AlignItems = AlignItems.Center,
                Width = Val.Percent(100),
                Height = Val.Percent(100),
            })
            .Insert(new BackgroundColor(bg));

        // MainMenu: column, fits content.
        var mainMenu = commands.Spawn()
            .Insert<LoginScene>()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Relative,
                FlexDirection = FlexDirection.Column,
                JustifyContent = JustifyContent.Start,
                AlignItems = AlignItems.Start,
                Width = Val.Auto,
                Height = Val.Auto,
            })
            .Insert(new BackgroundColor(bg));

        root.AddChild(mainMenu);

        // Background gump.
        mainMenu.AddChild(gumpBuilder.Value.AddGump(
            commands,
            0x014E,
            XnaVector3.UnitZ
        ).Insert<LoginScene>());

        // Quit button.
        mainMenu.AddChild(gumpBuilder.Value.AddButton(
            commands,
            (0x05CA, 0x05C9, 0x05C8),
            XnaVector3.UnitZ,
            new XnaVector2(25, 240)
        )
        .Insert(ButtonAction.Quit)
        .Insert<LoginScene>()
        .Observe((On<UiClick> trigger) =>
        {
            // TODO: route to a real quit handler.
            Console.WriteLine("[LoginScreen] Quit clicked");
        }));

        // Credits button.
        mainMenu.AddChild(gumpBuilder.Value.AddButton(
            commands,
            (0x05D0, 0x05CF, 0x05CE),
            XnaVector3.UnitZ,
            new XnaVector2(530, 125)
        )
        .Insert(ButtonAction.Credits)
        .Insert<LoginScene>()
        .Observe((On<UiPointerDown> _) => Console.WriteLine("pressed credits"))
        .Observe((On<UiPointerUp> _)   => Console.WriteLine("released credits")));

        // Arrow login button.
        var arrowButton = gumpBuilder.Value.AddButton(
            commands,
            (0x5CD, 0x5CC, 0x5CB),
            XnaVector3.UnitZ,
            new XnaVector2(280, 365)
        )
        .Insert(ButtonAction.Login)
        .Insert<LoginScene>()
        .Observe((
            On<UiClick> trigger,
            Commands innerCommands,
            Res<Settings> innerSettings,
            ResMut<NextState<LoginInteraction>> state,
            Single<Data<Text>, Filter<With<UsernameInput>, With<LoginScene>, With<TextInput>>> queryUsername,
            Single<Data<MaskedText>, Filter<With<PasswordInput>, With<LoginScene>, With<TextInput>>> queryPassword
        ) =>
        {
            (_, var username) = queryUsername.Get();
            (_, var password) = queryPassword.Get();
            Login(innerCommands, innerSettings.Value, username.Ref.Value, password.Ref.Value ?? string.Empty);
            state.Value.Set(LoginInteraction.LoginRequested);
        });

        mainMenu.AddChild(arrowButton);

        // Username field background + text child.
        var usernameField = gumpBuilder.Value.AddGumpNinePatch(
            commands,
            0x0BB8,
            XnaVector3.UnitZ,
            new XnaVector2(218, 283),
            new XnaVector2(210, 30))
            .Insert<LoginScene>();

        // Static text inside username field. Carries the markers so the
        // FocusedInput-clearing system in GuiPlugin still sees the inputs.
        // TODO: wire real text editing (StbTextEdit integration) — for now we
        // render the saved username read-only.
        var usernameText = commands.Spawn()
            .Insert(new Node
            {
                PositionType = PositionType.Absolute,
                Left = Val.Px(4),
                Top = Val.Px(4),
                Width = Val.Auto,
                Height = Val.Auto,
            })
            .Insert(new Text(settings.Value.Username ?? string.Empty))
            .Insert(new TextFont { FontId = 0, Size = 20 })
            .Insert(new TextColor(new ClayColor(51, 51, 51, 255)))
            .Insert<TextInput>()
            .Insert<UsernameInput>()
            .Insert<LoginScene>();

        usernameField.AddChild(usernameText);
        mainMenu.AddChild(usernameField);

        // Password field background + text child (masked).
        var passwordField = gumpBuilder.Value.AddGumpNinePatch(
            commands,
            0x0BB8,
            XnaVector3.UnitZ,
            new XnaVector2(218, 283 + 50),
            new XnaVector2(210, 30))
            .Insert<LoginScene>();

        // Real password kept in MaskedText.Value; SyncMaskedText (GuiPlugin)
        // mirrors it into Text as mask chars before the renderer sees it.
        var decrypted = Crypter.Decrypt(settings.Value.Password ?? string.Empty) ?? string.Empty;

        var passwordText = commands.Spawn()
            .Insert(new Node
            {
                PositionType = PositionType.Absolute,
                Left = Val.Px(4),
                Top = Val.Px(4),
                Width = Val.Auto,
                Height = Val.Auto,
            })
            .Insert(new Text(string.Empty))
            .Insert(new MaskedText { Value = decrypted, MaskChar = '*' })
            .Insert(new TextFont { FontId = 0, Size = 20 })
            .Insert(new TextColor(ClayColor.White))
            .Insert<TextInput>()
            .Insert<PasswordInput>()
            .Insert<LoginScene>();

        passwordField.AddChild(passwordText);
        mainMenu.AddChild(passwordField);
    }

    private static void DeleteMenu(
        Commands commands,
        Query<Data<Node>, Filter<With<LoginScene>>> query)
    {
        foreach (var (ent, _) in query)
        {
            commands.Entity(ent.Ref).Despawn();
        }
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
    private struct PasswordInput;
}
