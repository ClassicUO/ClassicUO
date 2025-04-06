using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Services
{
    internal class GraphicsService : IService
    {
        private readonly GraphicsDevice _graphicsDevice;

        public GraphicsService(GraphicsDevice graphicsDevice)
        {
            _graphicsDevice = graphicsDevice;
        }

        public GraphicsDevice GraphicsDevice => _graphicsDevice;
    }
}