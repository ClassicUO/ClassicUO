using ClassicUO.Network;
using Clay_cs;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.UI.Clay;
using TinyEcs.UI.Bevy;
using TinyEcs.UI;

namespace ClassicUO.Ecs;

internal readonly struct LoginErrorScreenPlugin : IPlugin
{
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

    private static void Cleanup(Commands commands, Query<Data<ClayNode>, Filter<With<LoginErrorScene>, Without<Parent>>> query)
    {
        foreach ((var ent, _) in query)
        {
            commands.Entity(ent.Ref).Despawn();
        }
    }

    private static void LoginErrorInfoSetup(Commands commands, EventReader<LoginErrorsInfoEvent> reader)
    {
        var root = commands.Spawn()
            .Insert<LoginErrorScene>()
            .Insert(ClayNode.Configure()
                .WidthGrow()
                .HeightGrow()
                .Column()
                .Align(Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER, Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER)
                .Background(51, 51, 51, 255)
                .Build());

        var loginErrorLabel = commands.Spawn()
            .Insert<LoginErrorScene>()
            .Insert(ClayNode.Configure()
                .WidthPercent(0.5f)
                .HeightFit()
                .Column()
                .Align(Clay_LayoutAlignmentX.CLAY_ALIGN_X_LEFT, Clay_LayoutAlignmentY.CLAY_ALIGN_Y_TOP)
                .Padding(8)
                .Gap(4)
                .Background(76, 76, 76, 255)
                .Text("Error on login", 28, new Clay_Color(255, 255, 255, 255))
                .Build());

        var menu = commands.Spawn()
            .Insert<LoginErrorScene>()
            .Insert(ClayNode.Configure()
                .WidthPercent(0.5f)
                .HeightPercent(0.5f)
                .Column()
                .Align(Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER, Clay_LayoutAlignmentY.CLAY_ALIGN_Y_TOP)
                .Padding(8)
                .Gap(4)
                .Background(76, 76, 76, 255)
                .Build());

        foreach (var ev in reader.Read())
        {
            var serverEnt = commands.Spawn()
                .Insert<LoginErrorScene>()
                .Insert(ev.Error)
                .Insert(ClayNode.Configure()
                    .WidthPercent(0.8f)
                    .HeightFit()
                    .Column()
                    .Align(Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER, Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER)
                    .Padding(8)
                    .Gap(4)
                    .Background(153, 153, 153, 255)
                    .Text(ev.Error.ErrorMessage, 24, new Clay_Color(255, 255, 255, 255))
                    .Build());

            menu.AddChild(serverEnt);
        }

        var footerMenu = commands.Spawn()
            .Insert<LoginErrorScene>()
            .Insert(ClayNode.Configure()
                .WidthGrow()
                .HeightGrow()
                .Column()
                .Align(Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER, Clay_LayoutAlignmentY.CLAY_ALIGN_Y_BOTTOM)
                .Padding(8)
                .Gap(4)
                .Build());

        var okButtonEntity = commands.Spawn()
            .Insert<LoginErrorScene>()
            .Insert(ClayNode.Configure()
                .WidthPercent(0.4f)
                .HeightFit()
                .Column()
                .Align(Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER, Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER)
                .Padding(8)
                .Gap(4)
                .Background(153, 153, 153, 255)
                .Text("OK", 24, new Clay_Color(255, 255, 255, 255))
                .Build())
            .Insert(LoginButtons.Ok)
            .Observe<On<ClayPointerEvent>, Res<NetClient>, Res<NextState<GameState>>>(OkButtonHandler);

        footerMenu.AddChild(okButtonEntity);
        menu.AddChild(footerMenu);
        root.AddChild(loginErrorLabel);
        root.AddChild(menu);
    }

    private static void OkButtonHandler(
        On<ClayPointerEvent> trigger,
        Res<NetClient> network,
        Res<NextState<GameState>> state
    )
    {
        if (!trigger.Event.IsLeftButton || trigger.Event.EventType != ClayPointerEventType.Click)
            return;

        state.Value.Set(GameState.LoginScreen);
        network.Value.Disconnect();
    }

    private struct LoginErrorScene;
    private enum LoginButtons : byte
    {
        Ok
    }
}

internal struct LoginErrorsInfoEvent
{
    public LoginErrorInfo Error;
}

internal record struct LoginErrorInfo(
    string ErrorMessage
);
