using ClassicUO.Configuration;
using ClassicUO.Renderer;
using TinyEcs;

namespace ClassicUO.Ecs;

internal readonly struct CameraPlugin : IPlugin
{
    public void Build(Scheduler scheduler)
    {
        var updateCameraFn = UpdateCamera;

        scheduler.AddResource(new Camera(0.5f, 2.5f, 0.1f));

        scheduler.OnUpdate(updateCameraFn, ThreadingMode.Single)
                .RunIf((SchedulerState state) => state.InState(GameState.GameScreen));

    }

    private static void UpdateCamera(Time time, Res<Camera> camera, Res<MouseContext> mouseCtx, Res<Profile> profile)
    {
        var mousePos = mouseCtx.Value.Position;

        if (mouseCtx.Value.Wheel > 0)
            camera.Value.ZoomIn();
        else if (mouseCtx.Value.Wheel < 0)
            camera.Value.ZoomOut();

        camera.Value.Bounds = new(
            profile.Value.GameWindowPosition.X,
            profile.Value.GameWindowPosition.Y,
            profile.Value.GameWindowSize.X,
            profile.Value.GameWindowSize.Y
        );

        camera.Value.Update(true, time.Total, new((int)mousePos.X, (int)mousePos.Y));
    }
}