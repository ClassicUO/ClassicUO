using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDL3;

namespace ClassicUO.Ecs;

internal sealed class UoGame : Microsoft.Xna.Framework.Game
{
    public UoGame(bool mouseVisible, bool allowWindowResizing, bool vSync)
    {
        GraphicManager = new GraphicsDeviceManager(this)
        {
            SynchronizeWithVerticalRetrace = vSync,
            PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8,
            // Match main's LoginScene.SetWindowSize(640, 480) default so
            // backbuffer dimensions don't float with the OS DPI. ECS has
            // no per-scene SetWindowSize call yet; without this the
            // backbuffer ends up at the window's PHYSICAL pixel size
            // (e.g. 800x480 at 125% DPI) instead of the logical UO size.
            PreferredBackBufferWidth = 640,
            PreferredBackBufferHeight = 480,
        };

        GraphicManager.PreparingDeviceSettings += (sender, e) =>
        {
            e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage =
                RenderTargetUsage.DiscardContents;
        };

        IsFixedTimeStep = false;
        IsMouseVisible = mouseVisible;
        Window.AllowUserResizing = allowWindowResizing;
    }

    public GraphicsDeviceManager GraphicManager { get; }
    public GameTime GameTime { get; private set; }

    protected override void Initialize()
    {
        base.Initialize();

        if (GraphicManager.GraphicsDevice.Adapter.IsProfileSupported(GraphicsProfile.HiDef))
        {
            GraphicManager.GraphicsProfile = GraphicsProfile.HiDef;
        }

        GraphicManager.ApplyChanges();
    }

    protected override void Update(GameTime gameTime)
    {
        GameTime = gameTime;
        // I don't want to update things here, but on ecs systems instead
    }

    protected override void Draw(GameTime gameTime)
    {
        // I don't want to render things here, but on ecs systems instead
    }

    // Match main's GameController.MaximizeWindow: SDL maximize first, then
    // sync the GraphicsDeviceManager backbuffer dims to the new client
    // bounds so RenderTarget / capture readback see the full screen, not
    // the 640x480 the LoginScene defaulted to.
    public void MaximizeWindow()
    {
        SDL.SDL_MaximizeWindow(Window.Handle);

        GraphicManager.PreferredBackBufferWidth = Window.ClientBounds.Width;
        GraphicManager.PreferredBackBufferHeight = Window.ClientBounds.Height;
        GraphicManager.ApplyChanges();
    }

    public bool IsWindowMaximized()
    {
        var flags = SDL.SDL_GetWindowFlags(Window.Handle);
        return (flags & SDL.SDL_WindowFlags.SDL_WINDOW_MAXIMIZED) != 0;
    }

    // User-configurable extra multiplier on top of the OS scale (parity with
    // main's GameController.ScreenScale). Settable; the DpiScale getter
    // recomputes off SDL_GetWindowDisplayScale * ScreenScale.
    public float ScreenScale { get; set; } = 1f;

    // Final UI scale = OS display scale * user multiplier. On Windows at
    // 125% scaling SDL_GetWindowDisplayScale returns 1.25; multiplying by
    // ScreenScale (default 1) gives 1.25 — matches main's DpiScale exactly
    // so ScaleWithDpi(640) yields the same 800 logical pixels on the same
    // monitor.
    public float DpiScale => SDL.SDL_GetWindowDisplayScale(Window.Handle) * ScreenScale;

    // Mirrors main's ScaleWithDpi: scale `value` by the ratio of the
    // current DpiScale to a previously-applied scale (default 1 meaning
    // "the value is in unscaled UO logical pixels"). Used when resizing
    // the window on display-scale-changed events.
    public int ScaleWithDpi(int value, float previousDpi = 1f)
    {
        return (int)System.Math.Round((value / previousDpi) * DpiScale);
    }

    // Mirrors main's GameController.SetWindowSize. PreferredBackBuffer*
    // are physical pixels; callers in LoginScreen pass DPI-scaled values
    // so the backbuffer matches the resized window.
    public void SetWindowSize(int width, int height)
    {
        GraphicManager.PreferredBackBufferWidth = width;
        GraphicManager.PreferredBackBufferHeight = height;
        GraphicManager.ApplyChanges();
    }
}
