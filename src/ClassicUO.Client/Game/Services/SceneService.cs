using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Services
{
    internal class SceneService : IService
    {
        private readonly GameController _game;

        internal SceneService(GameController game)
        {
            _game = game;
        }

        internal Camera Camera => _game.Scene.Camera;
        internal Rectangle Bounds => _game.Scene.Camera.Bounds;
        internal Scene? Scene => _game.Scene;

        internal T? GetScene<T>() where T : Scene
        {
            return _game.Scene as T;
        }

        public void SetScene(Scene scene)
        {
            _game.SetScene(scene);
        }
    }
}