using ClassicUO.Game.Scenes;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Services
{
    internal class SceneService : IService
    {
        private readonly Scene _scene;

        public SceneService(Scene scene)
        {
            _scene = scene;
        }

        public Camera Camera => _scene.Camera;
        public Rectangle Bounds => _scene.Camera.Bounds;
    }
}