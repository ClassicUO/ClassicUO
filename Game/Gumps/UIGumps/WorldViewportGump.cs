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
        private readonly ChatControl _chatControl;
        private readonly InputManager _inputManager;
        private readonly Settings _settings;
        private readonly WorldViewport _viewport;
        private bool _clicked;
        private Point _lastPosition = Point.Zero;
        private int _worldHeight;
        private int _worldWidth;

        public WorldViewportGump(GameScene scene) : base(0, 0)
        {
            _settings = Service.Get<Settings>();
            _inputManager = Service.Get<InputManager>();
            AcceptMouseInput = false;
            CanMove = true;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = false;
            ControlInfo.Layer = UILayer.Under;
            X = _settings.GameWindowX;
            Y = _settings.GameWindowY;
            _worldWidth = _settings.GameWindowWidth;
            _worldHeight = _settings.GameWindowHeight;
            _button = new Button(0, 0x837, 0x838, 0x838);
            _button.MouseDown += (sender, e) => { _clicked = true; };

            _button.MouseUp += (sender, e) =>
            {
                _clicked = false;
                _lastPosition = Point.Zero;
            };
            Width = _worldWidth + BORDER_WIDTH * 2;
            Height = _worldHeight + BORDER_HEIGHT * 2;
            _border = new GameBorder(0, 0, Width, Height, 4);
            _viewport = new WorldViewport(scene, BORDER_WIDTH, BORDER_HEIGHT, _worldWidth, _worldHeight);
            _chatControl = new ChatControl(BORDER_WIDTH, BORDER_HEIGHT, _worldWidth, _worldHeight);
            AddChildren(_border);
            AddChildren(_button);
            AddChildren(_viewport);
            AddChildren(_chatControl);
            Service.Register(_chatControl);
            Resize();
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (_clicked && Mouse.LDroppedOffset != _lastPosition && Mouse.LDroppedOffset != Point.Zero)
            {
                _settings.GameWindowWidth += Mouse.LDroppedOffset.X - _lastPosition.X;
                _settings.GameWindowHeight += Mouse.LDroppedOffset.Y - _lastPosition.Y;
                _lastPosition = Mouse.LDroppedOffset;

                if (_settings.GameWindowWidth < 350)
                    _settings.GameWindowWidth = 350;

                if (_settings.GameWindowHeight < 350)
                    _settings.GameWindowHeight = 350;
            }

            if (_worldWidth != _settings.GameWindowWidth || _worldHeight != _settings.GameWindowHeight)
            {
                _worldWidth = _settings.GameWindowWidth;
                _worldHeight = _settings.GameWindowHeight;
                Width = _worldWidth + BORDER_WIDTH * 2;
                Height = _worldHeight + BORDER_HEIGHT * 2;
                Resize();
            }

            base.Update(totalMS, frameMS);
        }

        protected override void OnMove()
        {
            Point position = Location;
            SpriteBatch3D sb = Service.Get<SpriteBatch3D>();

            if (position.X + Width - BORDER_WIDTH > sb.GraphicsDevice.Viewport.Width)
                position.X = sb.GraphicsDevice.Viewport.Width - (Width - BORDER_WIDTH);

            if (position.X < -BORDER_WIDTH)
                position.X = -BORDER_WIDTH;

            if (position.Y + Height - BORDER_HEIGHT > sb.GraphicsDevice.Viewport.Height)
                position.Y = sb.GraphicsDevice.Viewport.Height - (Height - BORDER_HEIGHT);

            if (position.Y < -BORDER_HEIGHT)
                position.Y = -BORDER_HEIGHT;
            Location = position;
            _settings.GameWindowX = position.X;
            _settings.GameWindowY = position.Y;
        }

        private void Resize()
        {
            _border.Width = Width;
            _border.Height = Height;
            _button.X = Width - 6;
            _button.Y = Height - 6;
            _viewport.Width = _worldWidth;
            _viewport.Height = _worldHeight;
            _chatControl.Width = _worldWidth;
            _chatControl.Height = _worldHeight;
            _chatControl.Resize();
        }
    }

    internal class GameBorder : GumpControl
    {
        private readonly SpriteTexture[] _borders = new SpriteTexture[2];
        private readonly int _borderSize;

        public GameBorder(int x, int y, int w, int h, int borderSize)
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;
            _borders[0] = IO.Resources.Gumps.GetGumpTexture(0x0A8C);
            _borders[1] = IO.Resources.Gumps.GetGumpTexture(0x0A8D);
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

        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null)
        {
            // sopra
            spriteBatch.Draw2DTiled(_borders[0], new Rectangle((int) position.X, (int) position.Y, Width, _borderSize), Vector3.Zero);
            // sotto
            spriteBatch.Draw2DTiled(_borders[0], new Rectangle((int) position.X, (int) position.Y + Height - _borderSize, Width, _borderSize), Vector3.Zero);
            //sx
            spriteBatch.Draw2DTiled(_borders[1], new Rectangle((int) position.X, (int) position.Y, _borderSize, Height), Vector3.Zero);
            //dx
            spriteBatch.Draw2DTiled(_borders[1], new Rectangle((int) position.X + Width - _borderSize, (int) position.Y + _borders[1].Width / 2, _borderSize, Height - _borderSize), Vector3.Zero);

            return base.Draw(spriteBatch, position, hue);
        }
    }
}