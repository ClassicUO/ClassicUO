using ClassicUO.Configuration;
using ClassicUO.Renderer;
using TinyEcs;

namespace ClassicUO.Ecs;

[TinyPlugin]
internal readonly partial struct CameraPlugin
{
    public void Build(Scheduler scheduler)
    {
        var setCameraBoundsFn = SetCameraBounds;

        scheduler.AddResource(new Camera(0.5f, 2.5f, 0.1f) { Bounds = new(0, 0, 800, 600) });
        scheduler.OnEnter(GameState.GameScreen, setCameraBoundsFn, ThreadingMode.Single);
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

    private static bool OnInGameState(SchedulerState state) => state.InState(GameState.GameScreen);

    [TinySystem(Stages.Update, ThreadingMode.Single)]
    [RunIf(nameof(OnInGameState))]
    private static void UpdateCamera(
        Time time,
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

        camera.Value.Update(true, time.Total, new((int)mousePos.X, (int)mousePos.Y));
    }
}
