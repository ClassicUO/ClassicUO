using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDL3;
using System;
using System.IO;

namespace ClassicUO.Game.UI.Gumps
{
    internal class WindowBorderFrameGump : Gump
    {
        private const int BORDER_THICKNESS = 2;
        private const int SIDE_BORDER_THICKNESS = 2;
        private const int RESIZE_GRIP_SIZE = 8;
        private const int TOP_RESIZE_THICKNESS = 3;

        private static readonly Color ColorBorder = new Color(65, 65, 75, 255);
        private static readonly Color ColorBottomAccent = new Color(125, 125, 145, 255);
        private static readonly Color ColorBorderHover = new Color(95, 115, 170, 255);
        private static readonly Color ColorBottomAccentHover = new Color(165, 190, 255, 255);
        private static readonly Vector3 HueNone = ShaderHueTranslator.GetHueVector(0);

        private static Texture2D _texBorder;
        private static bool _texBorderLoaded;
        private static IntPtr _cursorEW;
        private static IntPtr _cursorNS;
        private static IntPtr _cursorNWSE;
        private static IntPtr _cursorNESW;
        private static bool _systemCursorsLoaded;

        [System.Flags]
        private enum ResizeEdge
        {
            None = 0,
            Left = 1,
            Right = 2,
            Bottom = 4,
            Top = 8
        }

        private bool _isResizing;
        private bool _isUsingSystemResizeCursor;
        private IntPtr _activeSystemCursor;
        private ResizeEdge _hoverEdge;
        private ResizeEdge _resizeEdge;
        private Point _resizeStartGlobalMouse;
        private Point _resizeStartWindowPos;
        private Point _resizeStartSize;

        private static void EnsureBorderTexture()
        {
            if (_texBorderLoaded)
                return;
            _texBorderLoaded = true;
            string path = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Client", "windowframe.png");
            if (File.Exists(path))
                _texBorder = PNGLoader.Instance.GetImageTexture(path);
        }

        private static void EnsureSystemCursors()
        {
            if (_systemCursorsLoaded)
                return;

            _systemCursorsLoaded = true;
            _cursorEW = SDL.SDL_CreateSystemCursor(SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_EW_RESIZE);
            _cursorNS = SDL.SDL_CreateSystemCursor(SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_NS_RESIZE);
            _cursorNWSE = SDL.SDL_CreateSystemCursor(SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_NWSE_RESIZE);
            _cursorNESW = SDL.SDL_CreateSystemCursor(SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_NESW_RESIZE);
        }

        public WindowBorderFrameGump() : base(0, 0)
        {
            X = 0;
            Y = 0;
            Width = 800;
            Height = 600;
            CanMove = false;
            CanCloseWithRightClick = false;
            AcceptMouseInput = true;
            LayerOrder = UILayer.Over;
        }

        public static void UpdateVisibility()
        {
            if (ProfileManager.CurrentProfile == null)
                return;

            bool shouldShow = ProfileManager.CurrentProfile.UsesCustomWindowTitleBar()
                              && ProfileManager.CurrentProfile.EnableWindowBorderFrame;

            WindowBorderFrameGump existing = UIManager.GetGump<WindowBorderFrameGump>();

            if (shouldShow && existing == null)
                UIManager.Add(new WindowBorderFrameGump());
            else if (!shouldShow && existing != null)
                existing.Dispose();
        }

        public override void Update()
        {
            base.Update();

            int w = Client.Game.Window.ClientBounds.Width;
            int h = Client.Game.Window.ClientBounds.Height;

            if (w > 0 && Width != w)
                Width = w;

            if (h > 0 && Height != h)
                Height = h;

            ResizeEdge hoverEdge = ResizeEdge.None;

            if (_isResizing)
            {
                hoverEdge = _resizeEdge;
            }
            else
            {
                Point mouse = Mouse.Position;

                if (mouse.X >= 0 && mouse.X < Width && mouse.Y >= 0 && mouse.Y < Height)
                    hoverEdge = GetResizeEdge(mouse.X, mouse.Y);
            }

            _hoverEdge = hoverEdge;

            UpdateResizeCursor(hoverEdge);

            if (_isResizing)
            {
                if (!Mouse.LButtonPressed)
                {
                    _isResizing = false;
                    _resizeEdge = ResizeEdge.None;
                    return;
                }

                Point curGlobalMouse = Client.Game.GetGlobalMousePosition();
                int dx = curGlobalMouse.X - _resizeStartGlobalMouse.X;
                int dy = curGlobalMouse.Y - _resizeStartGlobalMouse.Y;

                int minW = Constants.MIN_GAME_WINDOW_WIDTH;
                int minH = Constants.MIN_GAME_WINDOW_HEIGHT;

                int newX = _resizeStartWindowPos.X;
                int newY = _resizeStartWindowPos.Y;
                int newW = _resizeStartSize.X;
                int newH = _resizeStartSize.Y;

                if ((_resizeEdge & ResizeEdge.Right) != 0)
                {
                    newW = _resizeStartSize.X + dx;
                }

                if ((_resizeEdge & ResizeEdge.Left) != 0)
                {
                    newW = _resizeStartSize.X - dx;
                    newX = _resizeStartWindowPos.X + dx;
                }

                if ((_resizeEdge & ResizeEdge.Bottom) != 0)
                {
                    newH = _resizeStartSize.Y + dy;
                }

                if ((_resizeEdge & ResizeEdge.Top) != 0)
                {
                    newH = _resizeStartSize.Y - dy;
                    newY = _resizeStartWindowPos.Y + dy;
                }

                if (newW < minW)
                {
                    if ((_resizeEdge & ResizeEdge.Left) != 0)
                        newX = _resizeStartWindowPos.X + (_resizeStartSize.X - minW);

                    newW = minW;
                }

                if (newH < minH)
                {
                    if ((_resizeEdge & ResizeEdge.Top) != 0)
                        newY = _resizeStartWindowPos.Y + (_resizeStartSize.Y - minH);

                    newH = minH;
                }

                Client.Game.SetWindowPosition(newX, newY);
                Client.Game.SetWindowSize(newW, newH);
            }
        }

        private void UpdateResizeCursor(ResizeEdge edge)
        {
            if (edge != ResizeEdge.None)
                EnsureSystemCursors();

            IntPtr cursor = IntPtr.Zero;

            if ((edge & ResizeEdge.Left) != 0 && (edge & ResizeEdge.Top) != 0)
                cursor = _cursorNWSE;
            else if ((edge & ResizeEdge.Right) != 0 && (edge & ResizeEdge.Bottom) != 0)
                cursor = _cursorNWSE;
            else if ((edge & ResizeEdge.Right) != 0 && (edge & ResizeEdge.Top) != 0)
                cursor = _cursorNESW;
            else if ((edge & ResizeEdge.Left) != 0 && (edge & ResizeEdge.Bottom) != 0)
                cursor = _cursorNESW;
            else if ((edge & ResizeEdge.Left) != 0 || (edge & ResizeEdge.Right) != 0)
                cursor = _cursorEW;
            else if ((edge & ResizeEdge.Top) != 0 || (edge & ResizeEdge.Bottom) != 0)
                cursor = _cursorNS;

            if (cursor != IntPtr.Zero)
            {
                if (!_isUsingSystemResizeCursor || _activeSystemCursor != cursor)
                {
                    SDL.SDL_SetCursor(cursor);
                    _activeSystemCursor = cursor;
                }

                _isUsingSystemResizeCursor = true;
            }
            else if (_isUsingSystemResizeCursor)
            {
                _isUsingSystemResizeCursor = false;
                _activeSystemCursor = IntPtr.Zero;
                Client.Game.GameCursor?.ForceUpdateSDLCursor();
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
                return false;

            EnsureBorderTexture();

            bool hoverLeft = (_hoverEdge & ResizeEdge.Left) != 0;
            bool hoverRight = (_hoverEdge & ResizeEdge.Right) != 0;
            bool hoverBottom = (_hoverEdge & ResizeEdge.Bottom) != 0;

            Color bottomColor = hoverBottom ? ColorBorderHover : ColorBorder;
            Color bottomAccentColor = hoverBottom ? ColorBottomAccentHover : ColorBottomAccent;
            Color leftColor = hoverLeft ? ColorBorderHover : ColorBorder;
            Color rightColor = hoverRight ? ColorBorderHover : ColorBorder;

            if (_texBorder != null && !_texBorder.IsDisposed)
            {
                // Bottom
                batcher.Draw(_texBorder, new Rectangle(x, y + Height - BORDER_THICKNESS, Width, BORDER_THICKNESS), null, HueNone);
                // Left
                batcher.Draw(_texBorder, new Rectangle(x, y, SIDE_BORDER_THICKNESS, Height), null, HueNone);
                // Right
                batcher.Draw(_texBorder, new Rectangle(x + Width - SIDE_BORDER_THICKNESS, y, SIDE_BORDER_THICKNESS, Height), null, HueNone);
            }
            else
            {
                // Bottom
                batcher.Draw(
                    SolidColorTextureCache.GetTexture(bottomColor),
                    new Rectangle(x, y + Height - BORDER_THICKNESS, Width, BORDER_THICKNESS),
                    HueNone,
                    0f);

                // Left
                batcher.Draw(
                    SolidColorTextureCache.GetTexture(leftColor),
                    new Rectangle(x, y, SIDE_BORDER_THICKNESS, Height),
                    HueNone,
                    0f);

                // Right
                batcher.Draw(
                    SolidColorTextureCache.GetTexture(rightColor),
                    new Rectangle(x + Width - SIDE_BORDER_THICKNESS, y, SIDE_BORDER_THICKNESS, Height),
                    HueNone,
                    0f);
            }

            return true;
        }

        protected override void OnMouseDown(int x, int y, MouseButtonType button)
        {
            if (button != MouseButtonType.Left)
            {
                base.OnMouseDown(x, y, button);
                return;
            }

            _resizeEdge = GetResizeEdge(x, y);

            if (_resizeEdge != ResizeEdge.None)
            {
                _isResizing = true;
                _resizeStartGlobalMouse = Client.Game.GetGlobalMousePosition();
                _resizeStartWindowPos = Client.Game.GetWindowPosition();
                _resizeStartSize = new Point(Client.Game.Window.ClientBounds.Width, Client.Game.Window.ClientBounds.Height);
            }

            base.OnMouseDown(x, y, button);
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                _isResizing = false;
                _resizeEdge = ResizeEdge.None;
            }

            base.OnMouseUp(x, y, button);
        }

        private ResizeEdge GetResizeEdge(int x, int y)
        {
            ResizeEdge edge = ResizeEdge.None;

            if (x <= RESIZE_GRIP_SIZE)
                edge |= ResizeEdge.Left;
            else if (x >= Width - RESIZE_GRIP_SIZE)
                edge |= ResizeEdge.Right;

            if (y <= TOP_RESIZE_THICKNESS)
                edge |= ResizeEdge.Top;
            else if (y >= Height - RESIZE_GRIP_SIZE)
                edge |= ResizeEdge.Bottom;

            return edge;
        }

        public override bool Contains(int x, int y)
        {
            if (_isResizing)
                return true;

            return GetResizeEdge(x, y) != ResizeEdge.None;
        }

        public override void Dispose()
        {
            if (_isUsingSystemResizeCursor)
            {
                _isUsingSystemResizeCursor = false;
                _activeSystemCursor = IntPtr.Zero;
                Client.Game.GameCursor?.ForceUpdateSDLCursor();
            }

            base.Dispose();
        }
    }
}
