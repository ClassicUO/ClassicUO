using System;
using ClassicUO.Configuration;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TinyEcs;
using TinyEcs.Bevy;

namespace ClassicUO.Ecs;

internal readonly struct CameraPlugin : IPlugin
{
    public void Build(App app)
    {
        var updateCameraFn = UpdateCamera;
        var setCameraBoundsFn = SetCameraBounds;
        var maximizeOnEnterFn = MaximizeOnEnterGameScreen;
        var watchDpiFn = WatchDpiChange;

        app
            .AddResource(new Camera(0.5f, 2.5f, 0.1f) { Bounds = new(0, 0, 800, 600) })

            .AddSystem(maximizeOnEnterFn)
            .OnEnter(GameState.GameScreen)
            .Build()

            .AddSystem(setCameraBoundsFn)
            .OnEnter(GameState.GameScreen)
            .Build()

            .AddSystem(watchDpiFn)
            .InStage(Stage.First)
            .Build()

            .AddSystem(updateCameraFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
            .Build()

            // Mirror main's GameScene.Unload: persist the session zoom as the
            // new default only when the profile opted in.
            .AddSystem((Res<Camera> camera, ResMut<Profile> profile) =>
            {
                if (profile.Value.SaveScaleAfterClose)
                    profile.Value.DefaultScale = camera.Value.Zoom;
            })
            .OnExit(GameState.GameScreen)
            .Build();

    }

    // Poll-based equivalent of main's SDL_EVENT_WINDOW_DISPLAY_SCALE_CHANGED
    // / DISPLAY_CHANGED handler. ECS doesn't install an SDL_SetEventFilter
    // (FNA pumps SDL inside Tick), so we detect DPI changes by comparing
    // the current scale to the last-applied value each frame. When it
    // moves, rescale the window so the LOGICAL backbuffer dim stays the
    // same on the new monitor — matches main's WindowOnClientSizeChanged
    // call with previousDpi=_displayScale.
    private static void WatchDpiChange(
        Res<UoGame> game,
        Local<float> lastDpi,
        Res<GraphicsDevice> device,
        Res<Settings> settings)
    {
        var current = game.Value.DpiScale;
        if (current <= 0f) current = 1f;

        if (lastDpi.Value == 0f)
        {
            lastDpi.Value = current;
            return;
        }

        if (System.Math.Abs(lastDpi.Value - current) < 0.001f)
            return;

        // Re-apply the current logical size scaled by the new DPI. We
        // can't read the prior logical size cheaply from FNA so derive
        // it from the current physical backbuffer divided by the OLD
        // DPI, then multiply by the NEW DPI for the resize.
        var pp = device.Value.PresentationParameters;
        var logicalW = (int)(pp.BackBufferWidth / lastDpi.Value);
        var logicalH = (int)(pp.BackBufferHeight / lastDpi.Value);
        game.Value.SetWindowSize((int)(logicalW * current), (int)(logicalH * current));
        lastDpi.Value = current;
    }

    // Mirror main's GameScene.Load: when settings say the window should be
    // maximized, maximize it on world entry. Without this the backbuffer
    // stays at LoginScreen's 640x480 default and every in-world capture
    // is letterboxed compared to main.
    private static void MaximizeOnEnterGameScreen(
        Res<UoGame> game,
        Res<Settings> settings
    )
    {
        if (settings.Value.IsWindowMaximized)
            game.Value.MaximizeWindow();
    }

    // Camera bounds in LOGICAL pixels (after DpiScale). Position from
    // profile.GameWindowPosition; size from profile.GameWindowSize unless
    // GameWindowFullSize is on AND the window is maximized, in which case
    // the viewport fills the backbuffer minus the TopBarGump reservation.
    // Mirrors main's WorldViewportGump sizing path.
    private static void SetCameraBounds(
        Res<Camera> camera,
        Res<Profile> profile,
        Res<Settings> settings,
        Res<UoGame> game,
        Res<GraphicsDevice> device
    )
    {
        // Mirror main's GameScene.Load: world entry starts at the profile scale.
        camera.Value.Zoom = profile.Value.DefaultScale;

        if (settings.Value.IsWindowMaximized && profile.Value.GameWindowFullSize)
        {
            const int TopBarHeight = 27;
            var dpi = game.Value.DpiScale;
            if (dpi <= 0f) dpi = 1f;
            var pp = device.Value.PresentationParameters;
            var logicalW = (int)(pp.BackBufferWidth / dpi);
            var logicalH = (int)(pp.BackBufferHeight / dpi);
            camera.Value.Bounds = new(0, TopBarHeight, logicalW, logicalH - TopBarHeight);
            return;
        }

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
        Res<KeyboardContext> keyboardCtx,
        Res<Profile> profile
    )
    {
        var mousePos = mouseCtx.Value.Position;
        var ctrl = keyboardCtx.Value.IsPressed(Keys.LeftControl) || keyboardCtx.Value.IsPressed(Keys.RightControl);

        // Legacy GameSceneInputHandler: wheel zoom only with ctrl held and the
        // profile opt-in; releasing ctrl optionally snaps back to DefaultScale.
        if (profile.Value.EnableMousewheelScaleZoom)
        {
            if (ctrl &&
                !mouseCtx.Value.WheelConsumed &&
                camera.Value.Bounds.Contains((int)mouseCtx.Value.Position.X, (int)mouseCtx.Value.Position.Y))
            {
                if (mouseCtx.Value.Wheel > 0)
                    camera.Value.ZoomIn();
                else if (mouseCtx.Value.Wheel < 0)
                    camera.Value.ZoomOut();
            }
            else if (!ctrl && profile.Value.RestoreScaleAfterUnpressCtrl)
            {
                camera.Value.Zoom = profile.Value.DefaultScale;
            }
        }

        camera.Value.Update(true, time.Value.Total, new((int)mousePos.X, (int)mousePos.Y));
    }
}
