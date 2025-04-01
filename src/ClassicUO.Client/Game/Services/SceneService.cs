using ClassicUO.Game.Scenes;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Services
{
    internal class SceneService : IService
    {
        private readonly GameController _game;

        public SceneService(GameController game)
        {
            _game = game;
        }

        public Camera Camera => _game.Scene.Camera;
        public Rectangle Bounds => _game.Scene.Camera.Bounds;
        public Scene? CurrentScene => _game.Scene;

        public T? GetScene<T>() where T : Scene
        {
            return _game.GetScene<T>();
        }

        public void SetScene(Scene scene)
        {
            _game.SetScene(scene);
        }
    }
}