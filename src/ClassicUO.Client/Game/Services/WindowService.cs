using System;
using Microsoft.Xna.Framework;
using SDL2;

namespace ClassicUO.Game.Services
{
    internal class WindowService : IService
    {
        private readonly Microsoft.Xna.Framework.GameWindow _window;

        public WindowService(Microsoft.Xna.Framework.GameWindow window)
        {
            _window = window;
        }

        public IntPtr Handle => _window.Handle;
        public bool AllowUserResizing
        {
            get => _window.AllowUserResizing;
            set => _window.AllowUserResizing = value;
        }
        public Rectangle ClientBounds => _window.ClientBounds;
    }
}