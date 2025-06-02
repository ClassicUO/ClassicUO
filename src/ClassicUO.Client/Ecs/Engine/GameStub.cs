using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Ecs;

internal sealed class UoGame : Microsoft.Xna.Framework.Game
{
    public UoGame(bool mouseVisible, bool allowWindowResizing, bool vSync)
    {
        GraphicManager = new GraphicsDeviceManager(this)
        {
            SynchronizeWithVerticalRetrace = vSync,
            PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8
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
}
