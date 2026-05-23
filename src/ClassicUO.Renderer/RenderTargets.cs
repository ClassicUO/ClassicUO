using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;


namespace ClassicUO.Renderer
{
    public class RenderTargets
    {
        private RenderTarget2D _uiRenderTarget;
        private RenderTarget2D _lightRenderTarget;
        private RenderTarget2D _worldRenderTarget;

        private Rectangle _gameWindowOnScreen;
        private Rectangle _gameWindowAfterDPI;
        private Rectangle _gameWorldSceneOnScreen;
        private Rectangle _gameWorldSceneAfterDPI;

        private Func<Vector3> _lightsHue;
        private Func<BlendState> _lightsBlendState;

        private Texture2D _background;
        private SamplerState _defaultSamplerState;

        public RenderTarget2D UiRenderTarget { get => _uiRenderTarget; }
        public RenderTarget2D LightRenderTarget { get => _lightRenderTarget; }
        public RenderTarget2D WorldRenderTarget { get => _worldRenderTarget; }

        public void SetLightsConfiguration(Func<BlendState> lightsBlendState, Func<Vector3> lightsHue)
        {
            _lightsBlendState = lightsBlendState;
            _lightsHue = lightsHue;
        }

        public void EnsureSizes(GraphicsDevice graphicsDevice, Rectangle gameWindowOnScreen, Rectangle gameWorldSceneAfterDPI, float dpiScale)
        {
            _gameWindowOnScreen = gameWindowOnScreen;
            _gameWindowAfterDPI = ScaleRectangle(gameWindowOnScreen, dpiScale);
            _gameWorldSceneOnScreen = ScaleRectangle(gameWorldSceneAfterDPI, 1/dpiScale);
            _gameWorldSceneAfterDPI = gameWorldSceneAfterDPI;

            EnsureSize(graphicsDevice, ref _uiRenderTarget, _gameWindowAfterDPI.Width, _gameWindowAfterDPI.Height);
            EnsureSize(graphicsDevice, ref _lightRenderTarget, _gameWorldSceneAfterDPI.Width, _gameWorldSceneAfterDPI.Height);
            EnsureSize(graphicsDevice, ref _worldRenderTarget, _gameWorldSceneAfterDPI.Width, _gameWorldSceneAfterDPI.Height);

            if (dpiScale == Math.Floor(dpiScale))
            {
                // Use PointClamp for integer DPI scaling to avoid blurriness
                _defaultSamplerState = SamplerState.PointClamp;
            }
            else
            {
                // Use LinearClamp for non-integer DPI scaling for smoother results
                _defaultSamplerState = SamplerState.LinearClamp;
            }
        }

        private static Rectangle ScaleRectangle(Rectangle gameWindowOnScreen, float dpiScale) => new(
                (int)(gameWindowOnScreen.X / dpiScale),
                (int)(gameWindowOnScreen.Y / dpiScale),
                (int)(gameWindowOnScreen.Width / dpiScale),
                (int)(gameWindowOnScreen.Height / dpiScale)
            );

        private static void EnsureSize(GraphicsDevice graphicsDevice, ref RenderTarget2D renderTarget, int width, int height)
        {
            if (width <= 0 || height <= 0)
                return;

            if (renderTarget == null || renderTarget.IsDisposed || renderTarget.Width != width || renderTarget.Height != height)
            {
                renderTarget?.Dispose();

                PresentationParameters pp = graphicsDevice.PresentationParameters;

                renderTarget = new RenderTarget2D(
                    graphicsDevice,
                    width,
                    height,
                    false,
                    pp.BackBufferFormat,
                    pp.DepthStencilFormat,
                    pp.MultiSampleCount,
                    pp.RenderTargetUsage
                    );
            }
        }

        public void InitializeBackground(Texture2D background)
        {
            _background = background ?? throw new ArgumentNullException(nameof(background));
        }

        public void Draw(UltimaBatcher2D batcher)
        {
            // draw world
            Vector3 fullAlphaNoColor = Vector3.UnitZ;

            batcher.Begin();
            batcher.GraphicsDevice.Clear(ClearOptions.Target, Color.Black, 0f, 0);

            var rect = new Rectangle(
                0,
                0,
                _gameWindowOnScreen.Width,
                _gameWindowOnScreen.Height
            );
            batcher.DrawTiled(
                _background,
                rect,
                _background.Bounds,
                new Vector3(0, 0, 0.1f),
                0f
            );

            batcher.SetSampler(_defaultSamplerState);

            batcher.Draw(
                WorldRenderTarget,
                _gameWorldSceneOnScreen,
                fullAlphaNoColor,
                0f
            );

            // draw lights
            batcher.SetBlendState(_lightsBlendState?.Invoke());

            batcher.Draw(
                LightRenderTarget,
                _gameWorldSceneOnScreen,
                _lightsHue?.Invoke() ?? Vector3.Up,
                0f
            );

            batcher.SetBlendState(null);

            // Draw UI at original window size (render target is DPI-scaled but destination is not)
            batcher.Draw(
                UiRenderTarget,
                _gameWindowOnScreen,
                fullAlphaNoColor,
                0f
            );
            
            // Reset sampler to default
            batcher.SetSampler(null);
            batcher.End();
        }
    }
}
