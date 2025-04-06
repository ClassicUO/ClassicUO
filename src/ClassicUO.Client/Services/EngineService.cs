using Microsoft.Xna.Framework;
using ClassicUO.Game;
using Microsoft.Xna.Framework.Graphics;
using System;
using ClassicUO.Game.Scenes;

namespace ClassicUO.Services
{
    internal class EngineService : IService
    {
        private readonly ClassicUO.GameController _game;

        public EngineService(ClassicUO.GameController game)
        {
            _game = game;
        }

        public bool IsActive => _game.IsActive;
        public bool IsMouseVisible
        {
            get => _game.IsMouseVisible;
            set => _game.IsMouseVisible = value;
        }

        public uint[] FrameDelay => _game.FrameDelay;

        public T? GetScene<T>() where T : Scene => _game.GetScene<T>();

        public GameWindow Window => _game.Window;

        public GraphicsDevice GraphicsDevice => _game.GraphicsDevice;

        public void SetWindowTitle(string title)
        {
            _game.SetWindowTitle(title);
        }

        public void SetWindowSize(int width, int height)
        {
            _game.SetWindowSize(width, height);
        }

        public void SetWindowBorderless(bool borderless)
        {
            _game.SetWindowBorderless(borderless);
        }

        public void MaximizeWindow()
        {
            _game.MaximizeWindow();
        }

        public bool IsWindowMaximized()
        {
            return _game.IsWindowMaximized();
        }

        public void RestoreWindow()
        {
            _game.RestoreWindow();
        }

        public GraphicsDeviceManager GraphicsManager => _game.GraphicManager;

        public void EnqueueAction(uint time, Action action)
        {
            _game.EnqueueAction(time, action);
        }

        public void Exit()
        {
            _game.Exit();
        }
    }
}