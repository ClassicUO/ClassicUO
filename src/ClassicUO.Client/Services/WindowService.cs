using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDL2;

namespace ClassicUO.Services
{
    internal class WindowService : IService
    {
        private readonly GameWindow _window;

        internal WindowService(GameWindow window)
        {
            _window = window;
        }

        public IntPtr Handle => _window.Handle;
        public bool AllowUserResizing
        {
            get => _window.AllowUserResizing;
            set => _window.AllowUserResizing = value;
        }
        internal Rectangle ClientBounds => _window.ClientBounds;
        internal GameWindow Window => _window;
    }
}