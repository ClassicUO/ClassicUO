#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
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
using ClassicUO.Configuration;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.UIGumps
{
    public class WorldViewportGump : Gump
    {
        private const int BORDER_WIDTH = 5;
        private const int BORDER_HEIGHT = 5;
        private readonly GameBorder _border;
        private readonly Button _button;
        private readonly SystemChatControl _systemChatControl;
        private readonly WorldViewport _viewport;
        private bool _clicked;
        private Point _lastPosition = Point.Zero;
        private int _worldHeight;
        private int _worldWidth;

        public WorldViewportGump(GameScene scene) : base(0, 0)
        {
            AcceptMouseInput = false;
            CanMove = true;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = false;
            ControlInfo.Layer = UILayer.Under;
            X = Engine.Profile.Current.GameWindowPosition.X;
            Y = Engine.Profile.Current.GameWindowPosition.Y;
            _worldWidth = Engine.Profile.Current.GameWindowSize.X;
            _worldHeight = Engine.Profile.Current.GameWindowSize.Y;
            _button = new Button(0, 0x837, 0x838, 0x838);
            _button.MouseDown += (sender, e) => { _clicked = true; };

            _button.MouseUp += (sender, e) =>
            {
                _clicked = false;
                _lastPosition = Point.Zero;
            };
            _button.SetTooltip("Resize game window");
            Width = _worldWidth + BORDER_WIDTH * 2;
            Height = _worldHeight + BORDER_HEIGHT * 2;
            _border = new GameBorder(0, 0, Width, Height, 4);
            _viewport = new WorldViewport(scene, BORDER_WIDTH, BORDER_HEIGHT, _worldWidth, _worldHeight);
            _systemChatControl = new SystemChatControl(BORDER_WIDTH, BORDER_HEIGHT, _worldWidth, _worldHeight);
            AddChildren(_border);
            AddChildren(_button);
            AddChildren(_viewport);
            AddChildren(_systemChatControl);
            Resize();
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (IsDisposed)
                return;

            if (_clicked && Mouse.LDroppedOffset != _lastPosition && Mouse.LDroppedOffset != Point.Zero)
            {
                Engine.Profile.Current.GameWindowSize = new Point(Engine.Profile.Current.GameWindowSize.X + Mouse.LDroppedOffset.X - _lastPosition.X, Engine.Profile.Current.GameWindowSize.Y + Mouse.LDroppedOffset.Y - _lastPosition.Y);

                _lastPosition = Mouse.LDroppedOffset;

                int w = Engine.Profile.Current.GameWindowSize.X;
                int h = Engine.Profile.Current.GameWindowSize.Y;

                if (w < 640)
                    w = 640;

                if (h < 480)
                    h = 480;

                Engine.Profile.Current.GameWindowSize = new Point(w, h);
            }

            if (_worldWidth != Engine.Profile.Current.GameWindowSize.X || _worldHeight != Engine.Profile.Current.GameWindowSize.Y)
            {
                _worldWidth = Engine.Profile.Current.GameWindowSize.X;
                _worldHeight = Engine.Profile.Current.GameWindowSize.Y;
                Width = _worldWidth + BORDER_WIDTH * 2;
                Height = _worldHeight + BORDER_HEIGHT * 2;
                Resize();
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
        }

        private void Resize()
        {
            _border.Width = Width;
            _border.Height = Height;
            _button.X = Width - _button.Width / 2;
            _button.Y = Height - _button.Height / 2;
            _viewport.Width = _worldWidth;
            _viewport.Height = _worldHeight;
            _systemChatControl.Width = _worldWidth;
            _systemChatControl.Height = _worldHeight;
            _systemChatControl.Resize();
            WantUpdateSize = true;
        }
    }

    internal class GameBorder : Control
    {
        private readonly SpriteTexture[] _borders = new SpriteTexture[2];
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

        public override void Update(double totalMS, double frameMS)
        {
            for (int i = 0; i < _borders.Length; i++)
                _borders[i].Ticks = (long) totalMS;
            base.Update(totalMS, frameMS);
        }

        public override bool Draw(Batcher2D batcher, Point position, Vector3? hue = null)
        {
            // sopra
            batcher.Draw2DTiled(_borders[0], new Rectangle(position.X, position.Y, Width, _borderSize), Vector3.Zero);
            // sotto
            batcher.Draw2DTiled(_borders[0], new Rectangle(position.X, position.Y + Height - _borderSize, Width, _borderSize), Vector3.Zero);
            //sx
            batcher.Draw2DTiled(_borders[1], new Rectangle(position.X, position.Y, _borderSize, Height), Vector3.Zero);
            //dx
            batcher.Draw2DTiled(_borders[1], new Rectangle(position.X + Width - _borderSize, position.Y + (_borders[1].Width >> 1), _borderSize, Height - _borderSize), Vector3.Zero);

            return base.Draw(batcher, position, hue);
        }
    }
}