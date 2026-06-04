using System;
using System.Numerics;
using ClassicUO.Configuration;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using TinyEcs.Bevy.UI.Widgets;
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
        var applyDpiFn = ApplyLoginScreenDpiSize;
        var updateInputHueFn = UpdateInputHue;

        app
            .AddState(LoginInteraction.None)

            .AddSystem(applyDpiFn)
            .OnEnter(GameState.LoginScreen)
            .Build()

            .AddSystem(setupFn)
            .OnEnter(GameState.LoginScreen)
            .Build()

            .AddSystem((Res<NextState<LoginInteraction>> state) => state.Value.Set(LoginInteraction.None))
            .OnEnter(GameState.LoginScreen)
            .Build()

            .AddSystem(deleteMenuFn)
            .OnExit(GameState.LoginScreen)
            .Build()

            // Per-char editing of the focused field is handled globally by
            // GuiPlugin.EditFocusedTextField (the login fields opt in via the
            // EditableText marker SpawnTextField adds).

            // Mirror LoginGump.Update: focused textbox renders with hue 0x0021,
            // unfocused with hue 0 (white).
            .AddSystem(updateInputHueFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.LoginScreen)
            .Build()

            .AddPlugin<ServerSelectionPlugin>()
            .AddPlugin<CharacterSelectionPlugin>()
            .AddPlugin<LoginErrorScreenPlugin>();
    }

    // Per-frame hue toggle to match LoginGump.Update: the focused textbox
    // renders with hue 0x0021, all others with hue 0. TextColor carries the
    // packed ASCII hue (see UoFontRuntime.AsciiHue), not an RGB tint.
    private static void UpdateInputHue(
        Res<FocusedInput> focused,
        Query<Data<TextColor>, Filter<With<TextInput>, With<LoginScene>>> inputs)
    {
        var focusEnt = focused.Value.Entity;
        foreach (var (ent, color) in inputs)
        {
            var hue = ent.Ref == focusEnt ? (ushort)0x0021 : (ushort)0;
            color.Ref = new TextColor(UoFontRuntime.AsciiHue(hue));
        }
    }

    // Mirror main's LoginScene.Load: resize the window to ScaleWithDpi(640,
    // 480) so the login chest gump renders at the same logical UO size as
    // main on the same monitor. Also propagate the user's ScreenScale
    // setting onto UoGame so DpiScale = SDL_GetWindowDisplayScale * scale.
    private static void ApplyLoginScreenDpiSize(
        Res<UoGame> game,
        Res<Settings> settings)
    {
        game.Value.ScreenScale = settings.Value.ScreenScale <= 0f ? 1f : settings.Value.ScreenScale;
        var w = game.Value.ScaleWithDpi(640);
        var h = game.Value.ScaleWithDpi(480);
        game.Value.SetWindowSize(w, h);
    }

    private static void Setup(
        Commands commands,
        Res<GumpBuilder> gumpBuilder,
        Res<Settings> settings,
        ResMut<FocusedInput> focused)
    {
        // Root: full-screen, top-left anchored. Main's LoginGump places the
        // chest at (0,0); mirror that by NOT centering with Flex.
        var root = commands.Spawn()
            .Insert<LoginScene>()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Relative,
                FlexDirection = FlexDirection.Column,
                JustifyContent = JustifyContent.Start,
                AlignItems = AlignItems.Start,
                Width = Val.Percent(100),
                Height = Val.Percent(100),
            });

        // MainMenu: fixed 640x480 (the chest gump's natural size). All child
        // positions are absolute UO logical pixels measured from (0,0) —
        // same coord space as main's LoginGump. Explicit size so absolute
        // children get a non-zero parent box for hit-testing.
        var mainMenu = commands.Spawn()
            .Insert<LoginScene>()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Relative,
                FlexDirection = FlexDirection.Column,
                JustifyContent = JustifyContent.Start,
                AlignItems = AlignItems.Start,
                Width = Val.Px(640),
                Height = Val.Px(480),
            });

        root.AddChild(mainMenu);

        // Tiled wallpaper 0x0150 + UO flag 0x0151 — mirrors main's
        // LoginBackground (CV>=706400 branch).
        mainMenu.AddChild(gumpBuilder.Value.AddGumpTiled(
            commands, 0x0150, XnaVector3.UnitZ,
            new XnaVector2(0, 0), new XnaVector2(640, 480))
            .Insert<LoginScene>());
        mainMenu.AddChild(gumpBuilder.Value.AddGump(
            commands, 0x0151, XnaVector3.UnitZ, new XnaVector2(0, 4))
            .Insert<LoginScene>());

        // Chest gump on top of wallpaper.
        mainMenu.AddChild(gumpBuilder.Value.AddGump(
            commands,
            0x014E,
            XnaVector3.UnitZ,
            new XnaVector2(0, 0)
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

        // Username + password fields. The shared SpawnTextField builds the
        // focusable glyph + caret + click-to-focus row on the hollow 0x0BB8
        // frame (its interior is transparent, so UiContainsByBounds makes the
        // whole box focusable, matching legacy's solid StbTextBox). font 5 +
        // resting hue 0 mirror main's LoginGump StbTextBox(font:5, !unicode);
        // UpdateInputHue swaps the focused field to 0x0021 per frame.
        var loginFieldFont = new TextFont { FontId = (ushort)(5 | UoFontRuntime.AsciiFlag), Size = 20 };

        var usernameField = gumpBuilder.Value.AddGumpNinePatch(
            commands, 0x0BB8, XnaVector3.UnitZ,
            new XnaVector2(218, 283), new XnaVector2(210, 30))
            .Insert<LoginScene>();
        var usernameTextId = GuiPlugin.SpawnTextField(
            commands, usernameField, new XnaVector2(4, 4), loginFieldFont, 0,
            settings.Value.Username ?? string.Empty, masked: false,
            decorate: e => e.Insert<LoginScene>());
        commands.Entity(usernameTextId).Insert<UsernameInput>();
        mainMenu.AddChild(usernameField);

        // Parity with main's LoginGump: auto-focus username field on entry
        // when empty (LoginGump calls SetKeyboardFocus on the account box).
        if (string.IsNullOrEmpty(settings.Value.Username))
        {
            focused.Value.Entity = usernameTextId;
        }

        // Real password kept in MaskedText.Value; SyncMaskedText (GuiPlugin)
        // mirrors it into Text as mask chars before the renderer sees it.
        var decrypted = Crypter.Decrypt(settings.Value.Password ?? string.Empty) ?? string.Empty;

        var passwordField = gumpBuilder.Value.AddGumpNinePatch(
            commands, 0x0BB8, XnaVector3.UnitZ,
            new XnaVector2(218, 283 + 50), new XnaVector2(210, 30))
            .Insert<LoginScene>();
        var passwordTextId = GuiPlugin.SpawnTextField(
            commands, passwordField, new XnaVector2(4, 4), loginFieldFont, 0,
            decrypted, masked: true,
            decorate: e => e.Insert<LoginScene>());
        commands.Entity(passwordTextId).Insert<PasswordInput>();
        mainMenu.AddChild(passwordField);

        // Version text — matches main's LoginGump.ctor CV>=706400 branch:
        // two lines at (286, 453) and (286, 465), ASCII font 9, hue 0x0481.
        // FontId carries the AsciiFlag so the renderer uses the UO ASCII font
        // set; TextColor carries the packed hue (baked per pixel) — pixel-exact
        // vs the OOP client, not a flat RGB approximation.
        var versionColor = UoFontRuntime.AsciiHue(0x0481);
        var versionFont = new TextFont { FontId = (ushort)(9 | UoFontRuntime.AsciiFlag), Size = 10 };
        var uoVersion = commands.Spawn()
            .Insert<LoginScene>()
            .Insert(new Node
            {
                PositionType = PositionType.Absolute,
                Left = Val.Px(286),
                Top = Val.Px(453),
                Width = Val.Auto,
                Height = Val.Auto,
            })
            .Insert(new Text($"UO Version {settings.Value.ClientVersion}."))
            .Insert(versionFont)
            .Insert(new TextColor(versionColor));
        mainMenu.AddChild(uoVersion);

        var cuoVersion = commands.Spawn()
            .Insert<LoginScene>()
            .Insert(new Node
            {
                PositionType = PositionType.Absolute,
                Left = Val.Px(286),
                Top = Val.Px(465),
                Width = Val.Auto,
                Height = Val.Auto,
            })
            .Insert(new Text($"ClassicUO Version {ClassicUO.CUOEnviroment.Version}"))
            .Insert(versionFont)
            .Insert(new TextColor(versionColor));
        mainMenu.AddChild(cuoVersion);

        // Autologin / SaveAccount checkboxes — main's LoginGump (CV>=706400)
        // uses ASCII font 9, hue 0x0481, toggling 0x00D2 unchecked / 0x00D3
        // checked. AddCheckbox is the real widget: CheckboxPlugin flips
        // Checkbox.Checked on click and fires CheckboxChanged; UpdateUOCheckboxes
        // swaps the sprite. hue UnitZ — AddCheckbox's default Vector3.Zero has
        // alpha 0 (invisible box). The .Observe persists the new value into the
        // Settings singleton so it round-trips like main's SaveCheckboxStatus.
        var checkboxColor = UoFontRuntime.AsciiHue(0x0481);
        var checkboxFont = new TextFont { FontId = (ushort)(9 | UoFontRuntime.AsciiFlag), Size = 11 };

        var autologinCheck = gumpBuilder.Value.AddCheckbox(
            commands, settings.Value.AutoLogin, new XnaVector2(150, 417), hue: XnaVector3.UnitZ)
            .Insert<LoginScene>()
            .Observe((On<CheckboxChanged> trig, Res<Settings> s) => s.Value.AutoLogin = trig.Event.Checked);
        mainMenu.AddChild(autologinCheck);
        AddCheckboxLabel(commands, mainMenu, "Autologin", new XnaVector2(170, 419), checkboxFont, checkboxColor, autologinCheck.Id);

        var saveAccountCheck = gumpBuilder.Value.AddCheckbox(
            commands, settings.Value.SaveAccount, new XnaVector2(240, 417), hue: XnaVector3.UnitZ)
            .Insert<LoginScene>()
            .Observe((On<CheckboxChanged> trig, Res<Settings> s) => s.Value.SaveAccount = trig.Event.Checked);
        mainMenu.AddChild(saveAccountCheck);
        AddCheckboxLabel(commands, mainMenu, "Save Account", new XnaVector2(260, 419), checkboxFont, checkboxColor, saveAccountCheck.Id);
    }

    // Caption next to a checkbox that toggles it on click, so the checkbox +
    // its label read as one control. CheckboxLabel + GuiPlugin's global
    // observer do the toggling generically; the label just has to be hittable
    // (Interaction.None + UiContainsByBounds).
    private static void AddCheckboxLabel(Commands commands, EntityCommands parent, string text, XnaVector2 pos, TextFont font, ClayColor color, ulong checkboxId)
    {
        var label = commands.Spawn()
            .Insert<LoginScene>()
            .Insert(new Node
            {
                PositionType = PositionType.Absolute,
                Left = Val.Px(pos.X),
                Top = Val.Px(pos.Y),
                Width = Val.Auto,
                Height = Val.Auto,
            })
            .Insert(new Text(text))
            .Insert(font)
            .Insert(new TextColor(color))
            .Insert(Interaction.None)
            .Insert<UiContainsByBounds>()
            .Insert(new CheckboxLabel { Target = checkboxId });
        parent.AddChild(label);
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
