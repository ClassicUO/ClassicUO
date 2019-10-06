#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
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

using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Network;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class WorldViewportGump : Gump
    {
        private const int BORDER_WIDTH = 5;
        private const int BORDER_HEIGHT = 5;
        private readonly GameBorder _border;
        private readonly Button _button;
        private readonly WorldViewport _viewport;
        private bool _clicked;
        private Point _lastSize, _savedSize;
        private SystemChatControl _systemChatControl;
        private int _worldHeight;
        private int _worldWidth;

        public WorldViewportGump(GameScene scene) : base(0, 0)
        {
            AcceptMouseInput = false;
            CanMove = !Engine.Profile.Current.GameWindowLock;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = false;
            ControlInfo.Layer = UILayer.Under;
            X = Engine.Profile.Current.GameWindowPosition.X;
            Y = Engine.Profile.Current.GameWindowPosition.Y;
            _worldWidth = Engine.Profile.Current.GameWindowSize.X;
            _worldHeight = Engine.Profile.Current.GameWindowSize.Y;
            _button = new Button(0, 0x837, 0x838, 0x838);

            _button.MouseDown += (sender, e) =>
            {
                if (!Engine.Profile.Current.GameWindowLock)
                    _clicked = true;
            };

            _button.MouseUp += (sender, e) =>
            {
                if (!Engine.Profile.Current.GameWindowLock)
                {
                    Point n = ResizeWindow(_lastSize);

                    OptionsGump options = Engine.UI.GetGump<OptionsGump>();
                    options?.UpdateVideo();

                    if (FileManager.ClientVersion >= ClientVersions.CV_200)
                        NetClient.Socket.Send(new PGameWindowSize((uint) n.X, (uint) n.Y));

                    _clicked = false;
                }
            };

            _button.SetTooltip("Resize game window");
            Width = _worldWidth + BORDER_WIDTH * 2;
            Height = _worldHeight + BORDER_HEIGHT * 2;
            _border = new GameBorder(0, 0, Width, Height, 4);
            _border.DragEnd += (sender, e) => 
            {
                OptionsGump options = Engine.UI.GetGump<OptionsGump>();
                options?.UpdateVideo();
            };
            _viewport = new WorldViewport(scene, BORDER_WIDTH, BORDER_HEIGHT, _worldWidth, _worldHeight);

            Engine.UI.SystemChat = _systemChatControl = new SystemChatControl(BORDER_WIDTH, BORDER_HEIGHT, _worldWidth, _worldHeight);

            Add(_border);
            Add(_button);
            Add(_viewport);
            Add(_systemChatControl);
            Resize();

            _savedSize = _lastSize = Engine.Profile.Current.GameWindowSize;
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (IsDisposed)
                return;

            if (Mouse.IsDragging)
            {
                Point offset = Mouse.LDroppedOffset;

                _lastSize = _savedSize;

                if (_clicked && offset != Point.Zero)
                {
                    int w = _lastSize.X + offset.X;
                    int h = _lastSize.Y + offset.Y;

                    if (w < 640)
                        w = 640;

                    if (h < 480)
                        h = 480;

                    if (w > Engine.WindowWidth - BORDER_WIDTH)
                        w = Engine.WindowWidth - BORDER_WIDTH;

                    if (h > Engine.WindowHeight - BORDER_HEIGHT)
                        h = Engine.WindowHeight - BORDER_HEIGHT;

                    _lastSize.X = w;
                    _lastSize.Y = h;
                }

                if (_worldWidth != _lastSize.X || _worldHeight != _lastSize.Y)
                {
                    _worldWidth = _lastSize.X;
                    _worldHeight = _lastSize.Y;
                    Width = _worldWidth + BORDER_WIDTH * 2;
                    Height = _worldHeight + BORDER_HEIGHT * 2;
                    Engine.Profile.Current.GameWindowSize = _lastSize;
                    Resize();
                }
            }
           

            base.Update(totalMS, frameMS);
        }

        protected override void OnMove()
        {
            Point position = Location;

            if (position.X + Width - BORDER_WIDTH > Engine.Batcher.GraphicsDevice.Viewport.Width)
                position.X = Engine.Batcher.GraphicsDevice.Viewport.Width - (Width - BORDER_WIDTH);

            if (position.X < -BORDER_WIDTH)
                position.X = -BORDER_WIDTH;

            if (position.Y + Height - BORDER_HEIGHT > Engine.Batcher.GraphicsDevice.Viewport.Height)
                position.Y = Engine.Batcher.GraphicsDevice.Viewport.Height - (Height - BORDER_HEIGHT);

            if (position.Y < -BORDER_HEIGHT)
                position.Y = -BORDER_HEIGHT;

            Location = position;

            Engine.Profile.Current.GameWindowPosition = position;

            var scene = Engine.SceneManager.GetScene<GameScene>();
            if (scene != null)
                scene.UpdateDrawPosition = true;
        }


        private void Resize()
        {
            _border.Width = Width;
            _border.Height = Height;
            _button.X = Width - (_button.Width >> 1);
            _button.Y = Height - (_button.Height >> 1);
            _worldWidth = Width - BORDER_WIDTH * 2;
            _worldHeight = Height - BORDER_WIDTH * 2;
            _viewport.Width = _worldWidth;
            _viewport.Height = _worldHeight;
            _systemChatControl.Width = _worldWidth;
            _systemChatControl.Height = _worldHeight;
            _systemChatControl.Resize();
            WantUpdateSize = true;
        }

        public Point ResizeWindow(Point newSize)
        {
            if (newSize.X < 640)
                newSize.X = 640;

            if (newSize.Y < 480)
                newSize.Y = 480;

            //Resize();
            _lastSize = _savedSize = Engine.Profile.Current.GameWindowSize = newSize;
            if (_worldWidth != _lastSize.X || _worldHeight != _lastSize.Y)
            {
                _worldWidth = _lastSize.X;
                _worldHeight = _lastSize.Y;
                Width = _worldWidth + BORDER_WIDTH * 2;
                Height = _worldHeight + BORDER_HEIGHT * 2;
                Engine.Profile.Current.GameWindowSize = _lastSize;
                Resize();

                Engine.SceneManager.GetScene<GameScene>().UpdateDrawPosition = true;
            }
            return newSize;
        }

        public void ReloadChatControl(SystemChatControl chat)
        {
            _systemChatControl.Dispose();
            Engine.UI.SystemChat = _systemChatControl = chat;
            Add(_systemChatControl);
            Resize();
        }
    }

    internal class GameBorder : Control
    {
        private readonly UOTexture[] _borders = new UOTexture[2];
        private readonly int _borderSize;

        public GameBorder(int x, int y, int w, int h, int borderSize)
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;
            _borders[0] = FileManager.Gumps.GetTexture(0x0A8C);
            _borders[1] = FileManager.Gumps.GetTexture(0x0A8D);
            _borderSize = borderSize;
            CanMove = true;
            AcceptMouseInput = true;
        }

        public Hue Hue { get; set; }

        public override void Update(double totalMS, double frameMS)
        {
            foreach (UOTexture t in _borders)
                t.Ticks = (long) totalMS;

            base.Update(totalMS, frameMS);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            ResetHueVector();

            if (Hue != 0)
            {
                _hueVector.X = Hue;
                _hueVector.Y = 1;
            }

            // sopra
            batcher.Draw2DTiled(_borders[0], x, y, Width, _borderSize, ref _hueVector);
            // sotto
            batcher.Draw2DTiled(_borders[0], x, y + Height - _borderSize, Width, _borderSize, ref _hueVector);
            //sx
            batcher.Draw2DTiled(_borders[1], x, y, _borderSize, Height, ref _hueVector);
            //dx
            batcher.Draw2DTiled(_borders[1], x + Width - _borderSize, y + (_borders[1].Width >> 1), _borderSize, Height - _borderSize, ref _hueVector);

            return base.Draw(batcher, x, y);
        }
    }
}