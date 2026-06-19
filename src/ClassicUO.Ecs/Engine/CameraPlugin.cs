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
        var initZoomFn = InitCamera;
        var applyBoundsFn = ApplyCameraBounds;
        var maximizeOnEnterFn = MaximizeOnEnterGameScreen;
        var watchDpiFn = WatchDpiChange;
        var borderlessFn = ApplyWindowBorderless;

        app
            .AddResource(new Camera(0.5f, 2.5f, 0.1f) { Bounds = new(0, 0, 800, 600) })

            .AddSystem(maximizeOnEnterFn)
            .OnEnter(GameState.GameScreen)
            .Build()

            // Initial zoom from the profile on world entry (wheel/ctrl zoom then
            // moves it from here — so this is OnEnter only, NOT per-frame).
            .AddSystem(initZoomFn)
            .OnEnter(GameState.GameScreen)
            .Build()

            // Viewport bounds recomputed EVERY frame so GameWindowFullSize /
            // GameWindowSize / GameWindowPosition changes apply live (legacy
            // resizes the WorldViewportGump on every window resize + on the
            // options toggle). PreUpdate so UpdateCamera's wheel-zoom hit-test
            // reads the fresh bounds the same frame.
            .AddSystem(applyBoundsFn)
            .InStage(Stage.PreUpdate)
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
            .Build()

            .AddSystem(watchDpiFn)
            .InStage(Stage.First)
            .Build()

            // Apply WindowBorderless on change (and once at boot). Not gated on
            // game state — borderless applies on the login screen too.
            .AddSystem(borderlessFn)
            .InStage(Stage.First)
            .Build()

            // ReduceFPSWhenInactive: this app drives its own loop (while(true)
            // app.Update()), so FNA's InactiveSleepTime never fires — throttle by
            // sleeping at frame end when the window is unfocused. ~5 FPS: enough
            // to keep input/focus responsive, big GPU/CPU saving in the tray.
            .AddSystem((Res<UoGame> game, Res<Profile> profile) =>
            {
                if (profile.Value.ReduceFPSWhenInactive && !game.Value.IsActive)
                    System.Threading.Thread.Sleep(200);
            })
            .InStage(Stage.Last)
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

    // Mirror main's GameScene.Load: world entry starts at the profile scale +
    // the profile viewport bounds. OnEnter only — wheel/ctrl zoom moves the zoom
    // and the border drag / resize handle (GameScreenPlugin) own camera.Bounds
    // from here on (so the viewport stays movable). GameWindowFullSize, if set,
    // is taken over live by ApplyCameraBounds next frame.
    private static void InitCamera(Res<Camera> camera, Res<Profile> profile)
    {
        camera.Value.Zoom = profile.Value.DefaultScale;
        if (!profile.Value.GameWindowFullSize)
            camera.Value.Bounds = new(
                profile.Value.GameWindowPosition.X,
                profile.Value.GameWindowPosition.Y,
                profile.Value.GameWindowSize.X,
                profile.Value.GameWindowSize.Y);
    }

    // Apply the borderless-window profile flag on change (Local sentinel: 0 =
    // not yet applied, so the first frame syncs the OS window to the profile).
    private static void ApplyWindowBorderless(Res<UoGame> game, Res<Profile> profile, Local<int> applied)
    {
        int want = profile.Value.WindowBorderless ? 1 : 2;
        if (applied.Value == want) return;
        game.Value.SetBorderless(profile.Value.WindowBorderless);
        applied.Value = want;
    }

    // GameWindowFullSize fills the whole backbuffer, live, regardless of maximize
    // state (legacy resizes the viewport to the full window on every resize). When
    // it's OFF the viewport is owned by the border drag / resize handle
    // (GameScreenPlugin mutates camera.Bounds), so this MUST NOT overwrite it each
    // frame — doing so snapped the window back and made it un-movable. It only
    // restores the profile bounds once, on the frame fullsize is turned off.
    private static void ApplyCameraBounds(
        Res<Camera> camera,
        Res<Profile> profile,
        Res<UoGame> game,
        Res<GraphicsDevice> device,
        Local<bool> wasFullSize
    )
    {
        if (profile.Value.GameWindowFullSize)
        {
            // Fill the whole backbuffer — the top bar floats over the world.
            var dpi = game.Value.DpiScale;
            if (dpi <= 0f) dpi = 1f;
            var pp = device.Value.PresentationParameters;
            var logicalW = (int)(pp.BackBufferWidth / dpi);
            var logicalH = (int)(pp.BackBufferHeight / dpi);
            camera.Value.Bounds = new(0, 0, logicalW, logicalH);
            wasFullSize.Value = true;
            return;
        }

        // Just left full-size → restore the manual viewport from the profile once.
        if (wasFullSize.Value)
        {
            camera.Value.Bounds = new(
                profile.Value.GameWindowPosition.X,
                profile.Value.GameWindowPosition.Y,
                profile.Value.GameWindowSize.X,
                profile.Value.GameWindowSize.Y);
            wasFullSize.Value = false;
        }
        // Otherwise leave camera.Bounds alone — the border drag / resize handle
        // own it, and clobbering it here makes the game window un-movable.
    }

    private static void UpdateCamera(
        Res<Time> time,
        Res<Camera> camera,
        Res<MouseContext> mouseCtx,
        Res<KeyboardContext> keyboardCtx,
        Res<Profile> profile,
        Local<float> lastDefaultScale
    )
    {
        // Live apply the "Default zoom" option: when the slider moves DefaultScale,
        // push it to the camera now (wheel/ctrl zoom changes camera.Zoom but NOT
        // DefaultScale, so this won't fight them — it only fires on a slider edit).
        if (lastDefaultScale.Value != profile.Value.DefaultScale)
        {
            camera.Value.Zoom = profile.Value.DefaultScale;
            lastDefaultScale.Value = profile.Value.DefaultScale;
        }

        // Position is UI-space (UiScale folded in at MouseContext). camera.Bounds
        // is fed into the GameWindow UI node, so the viewport renders at Bounds ×
        // UiScale — the world hit-tests (static/tile pick, hover highlight, peek,
        // via MouseToWorldPosition / IsMouseInsideBounds) all read this one feed.
        var mousePos = mouseCtx.Value.Position;
        var ctrl = keyboardCtx.Value.IsPressed(Keys.LeftControl) || keyboardCtx.Value.IsPressed(Keys.RightControl);

        // Legacy GameSceneInputHandler: wheel zoom only with ctrl held and the
        // profile opt-in; releasing ctrl optionally snaps back to DefaultScale.
        if (profile.Value.EnableMousewheelScaleZoom)
        {
            if (ctrl &&
                !mouseCtx.Value.WheelConsumed &&
                camera.Value.Bounds.Contains((int)mousePos.X, (int)mousePos.Y))
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
