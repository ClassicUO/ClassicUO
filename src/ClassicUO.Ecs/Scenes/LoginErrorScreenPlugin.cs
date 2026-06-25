// ECS port of the legacy login error popup (Game/UI/Gumps/Login/LoadingGump.cs
// in the LoginButtons.OK / PopUpMessage state). A ResizePic 0x0A28 panel at the
// fixed login coords with the rejection message centered inside (ASCII font 2,
// hue 0x0386, wrapped at 326px) and an OK button. OK returns to the login screen
// and disconnects, mirroring LoginScene.StepBack.
using System;
using ClassicUO.Network;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;

namespace ClassicUO.Ecs;

internal readonly struct LoginErrorScreenPlugin : IPlugin
{
    // LoadingGump geometry (login logical 640x480 coord space).
    private const ushort Background = 0x0A28;
    private static readonly Vector2 BoxPos = new(142, 134);
    private static readonly Vector2 BoxSize = new(366, 212);
    private static readonly Vector2 LabelPos = new(20, 44);   // box-relative (162,178)
    private const int LabelWidth = 326;
    private static readonly Vector2 OkPos = new(164, 170);     // box-relative (306,304)
    private const ushort OkN = 0x0481, OkP = 0x0483, OkO = 0x0482;
    private const ushort TextHue = 0x0386;
    private const byte Font = 2;

    public void Build(App app)
    {
        var cleanupFn = Cleanup;
        var loginErrorSetupFn = LoginErrorInfoSetup;

        app
            .AddSystem(cleanupFn)
            .OnExit(GameState.LoginError)
            .Build()

            .AddSystem(loginErrorSetupFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state, EventReader<LoginErrorsInfoEvent> reader)
                       => reader.HasEvents && state.Value.Current == GameState.LoginError)
            .Build();
    }

    private static void Cleanup(
        Commands commands,
        Query<Data<Node>, Filter<With<LoginErrorScene>>> query)
        => LoginSceneHelpers.DespawnAll<LoginErrorScene>(commands, query);

    private static void LoginErrorInfoSetup(
        Commands commands,
        Res<NetClient> network,
        Res<GumpBuilder> builder,
        Res<UiSurface> surface,
        ResMut<NextState<GameState>> nextState,
        EventReader<LoginErrorsInfoEvent> reader)
    {
        string message = "No Text";
        foreach (var ev in reader.Read())
            message = ev.Error.ErrorMessage;

        // Login wallpaper — legacy LoginBackground persists for the whole
        // LoginScene (added in Load, only disposed on Unload), so the popup
        // renders over it, not black. CV>=706400 branch: tiled 0x0150 + UO flag
        // 0x0151. Sized absolutely from the surface (a parentless Percent-sized
        // root collapses to 0).
        var size = surface.Value.LogicalSize;
        var backdrop = commands.Spawn()
            .Insert<LoginErrorScene>()
            .Insert(new Node
            {
                PositionType = PositionType.Absolute,
                Left = Val.Px(0), Top = Val.Px(0),
                Width = Val.Px(size.X), Height = Val.Px(size.Y),
            });
        var backdropId = backdrop.Id;

        LoginSceneHelpers.AddWallpaper<LoginErrorScene>(
            commands, builder.Value, backdrop, new Vector2(size.X, size.Y));

        // ResizePic panel.
        var box = commands.Spawn()
            .Insert<LoginErrorScene>()
            .Insert(new Node
            {
                PositionType = PositionType.Absolute,
                Left = Val.Px(BoxPos.X), Top = Val.Px(BoxPos.Y),
                Width = Val.Px(BoxSize.X), Height = Val.Px(BoxSize.Y),
            })
            .Insert(Interaction.None);
        var boxId = box.Id;
        commands.AddChild(backdropId, boxId);

        var bg = builder.Value.AddGumpNinePatch(commands, Background, Vector3.UnitZ, new Vector2(0, 0), BoxSize)
            .Insert<LoginErrorScene>();
        commands.AddChild(boxId, bg.Id);

        // Centered, wrapped ASCII message (Clay can't wrap the UO atlas, so wrap
        // at spawn via a WrappedText custom sized to the measured height).
        var (_, msgH) = UoFontRenderer.Measure(message, Font, LabelWidth, isHtml: false, 0, htmlBg: false, ascii: true);
        var label = commands.Spawn()
            .Insert<LoginErrorScene>()
            .Insert(new Node
            {
                PositionType = PositionType.Absolute,
                Left = Val.Px(LabelPos.X), Top = Val.Px(LabelPos.Y),
                Width = Val.Px(LabelWidth), Height = Val.Px(Math.Max(msgH, 18)),
            })
            .Insert(new UiCustom
            {
                Data = new UOCustomRender
                {
                    Kind = UOCustomKind.WrappedText,
                    Hue = Vector3.UnitZ,
                    Text = message,
                    TextFont = Font,
                    TextHue = TextHue,
                    WrapWidth = LabelWidth,
                    IsHtml = false,
                    TextAscii = true,
                    TextCenter = true,
                },
            });
        commands.AddChild(boxId, label.Id);

        // OK — back to the login screen + disconnect (LoginScene.StepBack).
        var okButton = builder.Value.AddButton(commands, (OkN, OkP, OkO), Vector3.UnitZ, OkPos)
            .Insert<LoginErrorScene>()
            .Observe((On<UiClick> _, ResMut<NextState<GameState>> state, Res<NetClient> net) =>
            {
                state.Value.Set(GameState.LoginScreen);
                net.Value.Disconnect();
            });
        commands.AddChild(boxId, okButton.Id);
    }

}

// Scene root marker — promoted to namespace-level internal so the modding
// registry (same assembly) can key a WIT scene path to it.
// cuo:modding contract type — do not merge/rename (queried by WIT path).
internal struct LoginErrorScene;

// cuo:modding contract type — do not merge/rename (queried by WIT path).
internal struct LoginErrorsInfoEvent
{
    public LoginErrorInfo Error;
}

internal record struct LoginErrorInfo(
    string ErrorMessage
);
