using ClassicUO.Configuration;
using ClassicUO.Renderer;
using TinyEcs;
using TinyEcs.Bevy;

namespace ClassicUO.Ecs;

internal readonly struct CameraPlugin : IPlugin
{
    public void Build(App app)
    {
        var updateCameraFn = UpdateCamera;
        var setCameraBoundsFn = SetCameraBounds;

        app
            .AddResource(new Camera(0.5f, 2.5f, 0.1f) { Bounds = new(0, 0, 800, 600) })

            .AddSystem(setCameraBoundsFn)
            .OnEnter(GameState.GameScreen)
            .Build()

            .AddSystem(updateCameraFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
            .Build();

    }

    private static void SetCameraBounds(
        Res<Camera> camera,
        Res<Profile> profile
    )
    {
        camera.Value.Bounds = new(
            profile.Value.GameWindowPosition.X,
            profile.Value.GameWindowPosition.Y,
            profile.Value.GameWindowSize.X,
            profile.Value.GameWindowSize.Y
        );
    }

    private static void UpdateCamera(
        Res<Time> time,
        Res<Camera> camera,
        Res<MouseContext> mouseCtx,
        Res<Profile> profile
    )
    {
        var mousePos = mouseCtx.Value.Position;

        if (camera.Value.Bounds.Contains((int)mouseCtx.Value.Position.X, (int)mouseCtx.Value.Position.Y))
        {
            if (mouseCtx.Value.Wheel > 0)
                camera.Value.ZoomIn();
            else if (mouseCtx.Value.Wheel < 0)
                camera.Value.ZoomOut();
        }

        camera.Value.Update(true, time.Value.Total, new((int)mousePos.X, (int)mousePos.Y));
    }
}
