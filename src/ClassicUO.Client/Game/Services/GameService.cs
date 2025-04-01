using Microsoft.Xna.Framework;
using ClassicUO.Game;

namespace ClassicUO.Game.Services
{
    internal class GameService : IService
    {
        private readonly ClassicUO.GameController _game;

        public GameService(ClassicUO.GameController game)
        {
            _game = game;
        }

        public bool IsActive => _game.IsActive;
        public bool IsMouseVisible
        {
            get => _game.IsMouseVisible;
            set => _game.IsMouseVisible = value;
        }

        public void SetWindowTitle(string title)
        {
            _game.Window.Title = title;
        }

        public void SetWindowSize(int width, int height)
        {
            _game.GraphicManager.PreferredBackBufferWidth = width;
            _game.GraphicManager.PreferredBackBufferHeight = height;
            _game.GraphicManager.ApplyChanges();
        }

        public bool IsWindowMaximized()
        {
            return _game.Window.ClientBounds.Width == _game.GraphicsDevice.DisplayMode.Width &&
                   _game.Window.ClientBounds.Height == _game.GraphicsDevice.DisplayMode.Height;
        }

        public void RestoreWindow()
        {
            // _game.Window.ClientBounds = new Rectangle(0, 0, 640, 480);
        }
    }
}