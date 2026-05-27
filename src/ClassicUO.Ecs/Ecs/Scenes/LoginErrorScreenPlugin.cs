using ClassicUO.Network;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using ClayColor = Clay.Color;

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

    private static void Cleanup(
        Commands commands,
        Query<Data<Node>, Filter<With<LoginErrorScene>>> query)
    {
        foreach ((var ent, _) in query)
        {
            commands.Entity(ent.Ref).Despawn();
        }
    }

    private static void LoginErrorInfoSetup(
        Commands commands,
        Res<NetClient> network,
        ResMut<NextState<GameState>> nextState,
        EventReader<LoginErrorsInfoEvent> reader)
    {
        // Root: full-screen vertical column, centered.
        var root = commands.Spawn()
            .Insert<LoginErrorScene>()
            .Insert(new Node
            {
                Width = Val.Percent(100f),
                Height = Val.Percent(100f),
                FlexDirection = FlexDirection.Column,
                JustifyContent = JustifyContent.Center,
                AlignItems = AlignItems.Center,
                Padding = UiRect.All(8),
                Gap = Val.Px(4),
            })
            .Insert(new BackgroundColor(new ClayColor(51, 51, 51, 255)));
        var rootId = root.Id;

        // Title label.
        var header = commands.Spawn()
            .Insert<LoginErrorScene>()
            .Insert(new Node
            {
                Width = Val.Percent(50f),
                Height = Val.Auto,
                FlexDirection = FlexDirection.Column,
                JustifyContent = JustifyContent.Start,
                AlignItems = AlignItems.Start,
                Padding = UiRect.All(8),
                Gap = Val.Px(4),
            })
            .Insert(new BackgroundColor(new ClayColor(76, 76, 76, 255)))
            .Insert(BorderRadius.All(8))
            .Insert(new Text("Error on login"))
            .Insert(new TextFont { FontId = 0, Size = 28 })
            .Insert(new TextColor(ClayColor.White));
        commands.AddChild(rootId, header.Id);

        // Menu container.
        var menu = commands.Spawn()
            .Insert<LoginErrorScene>()
            .Insert(new Node
            {
                Width = Val.Percent(50f),
                Height = Val.Percent(50f),
                FlexDirection = FlexDirection.Column,
                JustifyContent = JustifyContent.Start,
                AlignItems = AlignItems.Center,
                Padding = UiRect.All(8),
                Gap = Val.Px(4),
            })
            .Insert(new BackgroundColor(new ClayColor(76, 76, 76, 255)))
            .Insert(BorderRadius.All(8));
        commands.AddChild(rootId, menu.Id);
        var menuId = menu.Id;

        foreach (var ev in reader.Read())
        {
            var errorEntry = commands.Spawn()
                .Insert<LoginErrorScene>()
                .Insert(ev.Error)
                .Insert(new Node
                {
                    Width = Val.Auto,
                    Height = Val.Auto,
                    FlexDirection = FlexDirection.Column,
                    JustifyContent = JustifyContent.Center,
                    AlignItems = AlignItems.Center,
                    Padding = UiRect.All(8),
                    Gap = Val.Px(4),
                })
                .Insert(new BackgroundColor(new ClayColor(153, 153, 153, 255)))
                .Insert(BorderRadius.All(8))
                .Insert(new Text(ev.Error.ErrorMessage))
                .Insert(new TextFont { FontId = 0, Size = 24 })
                .Insert(new TextColor(ClayColor.White));
            commands.AddChild(menuId, errorEntry.Id);
        }

        // Footer row, anchored at the bottom-end.
        var footer = commands.Spawn()
            .Insert<LoginErrorScene>()
            .Insert(new Node
            {
                Width = Val.Percent(100f),
                Height = Val.Percent(100f),
                FlexDirection = FlexDirection.Column,
                JustifyContent = JustifyContent.End,
                AlignItems = AlignItems.Center,
                Padding = UiRect.All(8),
                Gap = Val.Px(4),
            });
        commands.AddChild(menuId, footer.Id);
        var footerId = footer.Id;

        // OK button.
        var okButton = commands.Spawn()
            .Insert<LoginErrorScene>()
            .Insert(LoginButtons.Ok)
            .Insert(new Node
            {
                Width = Val.Percent(40f),
                Height = Val.Auto,
                FlexDirection = FlexDirection.Column,
                JustifyContent = JustifyContent.Center,
                AlignItems = AlignItems.Center,
                Padding = UiRect.All(8),
                Gap = Val.Px(4),
            })
            .Insert(new BackgroundColor(new ClayColor(153, 153, 153, 255)))
            .Insert(BorderRadius.All(8))
            .Insert(new Text("OK"))
            .Insert(new TextFont { FontId = 0, Size = 24 })
            .Insert(new TextColor(ClayColor.White))
            .Insert(Interaction.None)
            .Insert(new FocusPolicy { Block = true })
            .Observe<On<UiClick>>(_ =>
            {
                nextState.Value.Set(GameState.LoginScreen);
                network.Value.Disconnect();
            });
        commands.AddChild(footerId, okButton.Id);
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
