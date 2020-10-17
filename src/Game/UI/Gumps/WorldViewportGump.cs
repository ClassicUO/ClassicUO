#region license

// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

using ClassicUO.Configuration;
using ClassicUO.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class WorldViewportGump : Gump
    {
        private const int BORDER_WIDTH = 5;
        private readonly BorderControl _borderControl;
        private readonly Button _button;
        private bool _clicked;
        private Point _lastSize, _savedSize;
        private readonly GameScene _scene;
        private readonly SystemChatControl _systemChatControl;
        private int _worldHeight;
        private int _worldWidth;

        public WorldViewportGump(GameScene scene) : base(0, 0)
        {
            _scene = scene;
            AcceptMouseInput = false;
            CanMove = !ProfileManager.CurrentProfile.GameWindowLock;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = false;
            LayerOrder = UILayer.Under;
            X = ProfileManager.CurrentProfile.GameWindowPosition.X;
            Y = ProfileManager.CurrentProfile.GameWindowPosition.Y;
            _worldWidth = ProfileManager.CurrentProfile.GameWindowSize.X;
            _worldHeight = ProfileManager.CurrentProfile.GameWindowSize.Y;
            _savedSize = _lastSize = ProfileManager.CurrentProfile.GameWindowSize;

            _button = new Button(0, 0x837, 0x838, 0x838);

            _button.MouseDown += (sender, e) =>
            {
                if (!ProfileManager.CurrentProfile.GameWindowLock)
                {
                    _clicked = true;
                }
            };

            _button.MouseUp += (sender, e) =>
            {
                if (!ProfileManager.CurrentProfile.GameWindowLock)
                {
                    Point n = ResizeGameWindow(_lastSize);

                    UIManager.GetGump<OptionsGump>()?.UpdateVideo();

                    if (Client.Version >= ClientVersion.CV_200)
                    {
                        NetClient.Socket.Send(new PGameWindowSize((uint) n.X, (uint) n.Y));
                    }

                    _clicked = false;
                }
            };

            _button.SetTooltip(ResGumps.ResizeGameWindow);
            Width = _worldWidth + BORDER_WIDTH * 2;
            Height = _worldHeight + BORDER_WIDTH * 2;
            _borderControl = new BorderControl(0, 0, Width, Height, 4);

            _borderControl.DragEnd += (sender, e) => { UIManager.GetGump<OptionsGump>()?.UpdateVideo(); };

            UIManager.SystemChat = _systemChatControl = new SystemChatControl
                (BORDER_WIDTH, BORDER_WIDTH, _worldWidth, _worldHeight);

            Add(_borderControl);
            Add(_button);
            Add(_systemChatControl);
            Resize();
        }


        public override void Update(double totalTime, double frameTime)
        {
            base.Update(totalTime, frameTime);

            if (IsDisposed)
            {
                return;
            }

            if (Mouse.IsDragging)
            {
                Point offset = Mouse.LDragOffset;

                _lastSize = _savedSize;

                if (_clicked && offset != Point.Zero)
                {
                    int w = _lastSize.X + offset.X;
                    int h = _lastSize.Y + offset.Y;

                    if (w < 640)
                    {
                        w = 640;
                    }

                    if (h < 480)
                    {
                        h = 480;
                    }

                    if (w > Client.Game.Window.ClientBounds.Width - BORDER_WIDTH)
                    {
                        w = Client.Game.Window.ClientBounds.Width - BORDER_WIDTH;
                    }

                    if (h > Client.Game.Window.ClientBounds.Height - BORDER_WIDTH)
                    {
                        h = Client.Game.Window.ClientBounds.Height - BORDER_WIDTH;
                    }

                    _lastSize.X = w;
                    _lastSize.Y = h;
                }

                if (_worldWidth != _lastSize.X || _worldHeight != _lastSize.Y)
                {
                    _worldWidth = _lastSize.X;
                    _worldHeight = _lastSize.Y;
                    Width = _worldWidth + BORDER_WIDTH * 2;
                    Height = _worldHeight + BORDER_WIDTH * 2;
                    ProfileManager.CurrentProfile.GameWindowSize = _lastSize;
                    Resize();
                }
            }
        }

        protected override void OnDragEnd(int x, int y)
        {
            base.OnDragEnd(x, y);

            Point position = Location;

            if (position.X + Width - BORDER_WIDTH > Client.Game.Window.ClientBounds.Width)
            {
                position.X = Client.Game.Window.ClientBounds.Width - (Width - BORDER_WIDTH);
            }

            if (position.X < -BORDER_WIDTH)
            {
                position.X = -BORDER_WIDTH;
            }

            if (position.Y + Height - BORDER_WIDTH > Client.Game.Window.ClientBounds.Height)
            {
                position.Y = Client.Game.Window.ClientBounds.Height - (Height - BORDER_WIDTH);
            }

            if (position.Y < -BORDER_WIDTH)
            {
                position.Y = -BORDER_WIDTH;
            }

            Location = position;

            ProfileManager.CurrentProfile.GameWindowPosition = position;

            UIManager.GetGump<OptionsGump>()?.UpdateVideo();

            UpdateGameWindowPos();
        }

        protected override void OnMove(int x, int y)
        {
            base.OnMove(x, y);

            ProfileManager.CurrentProfile.GameWindowPosition = new Point(ScreenCoordinateX, ScreenCoordinateY);

            UpdateGameWindowPos();
        }

        private void UpdateGameWindowPos()
        {
            if (_scene != null)
            {
                _scene.UpdateDrawPosition = true;
            }
        }


        private void Resize()
        {
            _borderControl.Width = Width;
            _borderControl.Height = Height;
            _button.X = Width - (_button.Width >> 1);
            _button.Y = Height - (_button.Height >> 1);
            _worldWidth = Width - BORDER_WIDTH * 2;
            _worldHeight = Height - BORDER_WIDTH * 2;
            _systemChatControl.Width = _worldWidth;
            _systemChatControl.Height = _worldHeight;
            _systemChatControl.Resize();
            WantUpdateSize = true;

            UpdateGameWindowPos();
        }

        public Point ResizeGameWindow(Point newSize)
        {
            if (newSize.X < 640)
            {
                newSize.X = 640;
            }

            if (newSize.Y < 480)
            {
                newSize.Y = 480;
            }

            //Resize();
            _lastSize = _savedSize = ProfileManager.CurrentProfile.GameWindowSize = newSize;

            if (_worldWidth != _lastSize.X || _worldHeight != _lastSize.Y)
            {
                _worldWidth = _lastSize.X;
                _worldHeight = _lastSize.Y;
                Width = _worldWidth + BORDER_WIDTH * 2;
                Height = _worldHeight + BORDER_WIDTH * 2;
                ProfileManager.CurrentProfile.GameWindowSize = _lastSize;
                Resize();
            }

            return newSize;
        }

        public override bool Contains(int x, int y)
        {
            if (x >= BORDER_WIDTH && x < Width - BORDER_WIDTH * 2 && y >= BORDER_WIDTH && y < Height -
                BORDER_WIDTH * 2 - (_systemChatControl?.TextBoxControl != null && _systemChatControl.IsActive ?
                    _systemChatControl.TextBoxControl.Height :
                    0))
            {
                return false;
            }

            return base.Contains(x, y);
        }
    }

    internal class BorderControl : Control
    {
        private readonly UOTexture[] _borders = new UOTexture[2];

        public BorderControl(int x, int y, int w, int h, int borderSize)
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;
            _borders[0] = GumpsLoader.Instance.GetTexture(0x0A8C);
            _borders[1] = GumpsLoader.Instance.GetTexture(0x0A8D);
            BorderSize = borderSize;
            CanMove = true;
            AcceptMouseInput = true;
        }

        public ushort Hue { get; set; }
        public int BorderSize { get; }

        public override void Update(double totalTime, double frameTime)
        {
            base.Update(totalTime, frameTime);

            foreach (UOTexture t in _borders)
            {
                t.Ticks = (long) totalTime;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            ResetHueVector();

            if (Hue != 0)
            {
                HueVector.X = Hue;
                HueVector.Y = 1;
            }

            // sopra
            batcher.Draw2DTiled(_borders[0], x, y, Width, BorderSize, ref HueVector);
            // sotto
            batcher.Draw2DTiled(_borders[0], x, y + Height - BorderSize, Width, BorderSize, ref HueVector);
            //sx
            batcher.Draw2DTiled(_borders[1], x, y, BorderSize, Height, ref HueVector);

            //dx
            batcher.Draw2DTiled
            (
                _borders[1], x + Width - BorderSize, y + (_borders[1].Width >> 1), BorderSize, Height - BorderSize,
                ref HueVector
            );

            return base.Draw(batcher, x, y);
        }
    }
}