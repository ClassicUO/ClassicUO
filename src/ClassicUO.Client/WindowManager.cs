// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using Microsoft.Xna.Framework;
using static SDL3.SDL;

namespace ClassicUO
{
    /// <summary>
    /// Manages window size, position, DPI, borderless mode, and related settings.
    /// </summary>
    internal sealed class WindowManager
    {
        private readonly GameController _game;

        public WindowManager(GameController game)
        {
            _game = game;
            _game.Window.ClientSizeChanged += (_, _) => OnClientSizeChanged();
        }

        public void SetTitle(string title)
        {
            if (string.IsNullOrEmpty(title))
            {
#if DEV_BUILD
                _game.Window.Title = $"ClassicUO [dev] - {CUOEnviroment.Version}";
#else
                _game.Window.Title = $"ClassicUO - {CUOEnviroment.Version}";
#endif
            }
            else
            {
#if DEV_BUILD
                _game.Window.Title = $"{title} - ClassicUO [dev] - {CUOEnviroment.Version}";
#else
                _game.Window.Title = $"{title} - ClassicUO - {CUOEnviroment.Version}";
#endif
            }
        }

        public void SetSize(int width, int height)
        {
            _game.GraphicManager.PreferredBackBufferWidth = width;
            _game.GraphicManager.PreferredBackBufferHeight = height;
            _game.GraphicManager.ApplyChanges();
        }

        public void SetBorderless(bool borderless, IUIManager ui)
        {
            SDL_WindowFlags flags = (SDL_WindowFlags)SDL_GetWindowFlags(_game.Window.Handle);

            if ((flags & SDL_WindowFlags.SDL_WINDOW_BORDERLESS) != 0 && borderless)
                return;

            if ((flags & SDL_WindowFlags.SDL_WINDOW_BORDERLESS) == 0 && !borderless)
                return;

            SDL_SetWindowBordered(_game.Window.Handle, !borderless);

            unsafe
            {
                SDL_DisplayMode* displayMode = (SDL_DisplayMode*)SDL_GetCurrentDisplayMode(
                    SDL_GetDisplayForWindow(_game.Window.Handle)
                );

                int width = displayMode->w;
                int height = displayMode->h;

                if (borderless)
                {
                    SetSize(width, height);
                    SDL_GetDisplayUsableBounds(
                        SDL_GetDisplayForWindow(_game.Window.Handle),
                        out SDL_Rect rect
                    );
                    SDL_SetWindowPosition(_game.Window.Handle, rect.x, rect.y);
                }
                else
                {
                    SDL_GetWindowBordersSize(_game.Window.Handle, out int top, out _, out int bottom, out _);
                    SetSize(width, height - (top - bottom));
                    SetPositionBySettings();
                }

                WorldViewportGump viewport = ui.GetGump<WorldViewportGump>();

                if (viewport != null && _game.UO.World?.Profile?.CurrentProfile is { } borderlessProfile && borderlessProfile.GameWindowFullSize)
                {
                    viewport.ResizeGameWindow(new Point(width, height));
                    viewport.X = -5;
                    viewport.Y = -5;
                }
            }
        }

        public void Maximize()
        {
            SDL_MaximizeWindow(_game.Window.Handle);

            _game.GraphicManager.PreferredBackBufferWidth = _game.Window.ClientBounds.Width;
            _game.GraphicManager.PreferredBackBufferHeight = _game.Window.ClientBounds.Height;
            _game.GraphicManager.ApplyChanges();
        }

        public bool IsMaximized()
        {
            SDL_WindowFlags flags = (SDL_WindowFlags)SDL_GetWindowFlags(_game.Window.Handle);
            return (flags & SDL_WindowFlags.SDL_WINDOW_MAXIMIZED) != 0;
        }

        public void Restore()
        {
            SDL_RestoreWindow(_game.Window.Handle);
        }

        public void SetPositionBySettings()
        {
            var borderSizesRetrieved = SDL_GetWindowBordersSize(_game.Window.Handle, out int top, out int left, out _, out _);

            if (!borderSizesRetrieved)
            {
                top = 0;
                left = 0;
            }

            if (Settings.GlobalSettings.WindowPosition.HasValue)
            {
                int x = left + Settings.GlobalSettings.WindowPosition.Value.X;
                int y = top + Settings.GlobalSettings.WindowPosition.Value.Y;
                x = System.Math.Max(0, x);
                y = System.Math.Max(0, y);

                SDL_Point desiredStartPoint = new() { x = x, y = y };
                var displayId = SDL_GetDisplayForPoint(ref desiredStartPoint);
                if (displayId <= 0)
                {
                    SDL_SetWindowPosition(_game.Window.Handle, left, top);
                }

                var boundsRetrieved = SDL_GetDisplayUsableBounds(displayId, out SDL_Rect displayBounds);
                if (!boundsRetrieved)
                    return;

                if (x < displayBounds.x || x >= displayBounds.x + displayBounds.w)
                    x = left + displayBounds.x;

                if (y < displayBounds.y || y >= displayBounds.y + displayBounds.h)
                    y = top + displayBounds.y;

                SDL_SetWindowPosition(_game.Window.Handle, x, y);
            }
        }

        internal void OnClientSizeChanged()
        {
            int width = _game.Window.ClientBounds.Width;
            int height = _game.Window.ClientBounds.Height;
            OnClientSizeChanged(width, height);
        }

        internal void OnClientSizeChanged(int width, int height)
        {
            if (!IsMaximized() && _game.Window.AllowUserResizing)
            {
                if (_game.UO.World?.Profile?.CurrentProfile is { } resizeProfile)
                    resizeProfile.WindowClientBounds = new Point(width, height);
            }

            SetSize(width, height);

            WorldViewportGump viewport = _game.UI?.GetGump<WorldViewportGump>();

            if (viewport != null && _game.UO.World?.Profile?.CurrentProfile is { GameWindowFullSize: true })
            {
                viewport.ResizeGameWindow(new Point(width, height));
                viewport.X = -5;
                viewport.Y = -5;
            }
        }
    }
}
