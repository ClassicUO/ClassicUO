#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2018 ClassicUO Development Team)
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

using System;
using ClassicUO.Configuration;
using ClassicUO.Game.Gumps.UIGumps;
using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.Controls.InGame
{
    public class WorldViewportGump : Gump
    {
        private int _worldWidth;
        private int _worldHeight;
        private WorldViewport _viewport;
        private ChatControl _chatControl;
        private GameBorder _border;
        private readonly GameScene _scene;
        private Settings _settings;
        private Button _button;
        private InputManager _inputManager;
        private bool _clicked;
        private Point _lastPosition = Point.Zero;

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
            
            _button.MouseDown += (sender, e) =>
            {
                _clicked = true;
            };
            _button.MouseUp += (sender, e) =>
            {
                _clicked = false;
                _lastPosition = Point.Zero;
            };


            _border = new GameBorder(0,0, _worldWidth + 8, _worldHeight + 12);

            _viewport = new WorldViewport(scene, 4, 6, _worldWidth, _worldHeight);
            _chatControl = new ChatControl(4, 6, _worldWidth, _worldHeight);

            AddChildren(_border);
            AddChildren(_button);
            AddChildren(_viewport);
            AddChildren(_chatControl);

            Service.Register(_chatControl);

            _scene = scene;

            OnResize();
        }


        public override void Update(double totalMS, double frameMS)
        {
            if (_clicked && _inputManager.Offset != _lastPosition && _inputManager.Offset != Point.Zero)
            {
                _settings.GameWindowWidth += _inputManager.Offset.X -_lastPosition.X;
                _settings.GameWindowHeight += _inputManager.Offset.Y - _lastPosition.Y;

                _lastPosition = _inputManager.Offset;

                if (_settings.GameWindowWidth < 640)
                    _settings.GameWindowWidth = 640;

                if (_settings.GameWindowHeight < 500)
                    _settings.GameWindowHeight = 500;
            }

            if (_worldWidth != _settings.GameWindowWidth || _worldHeight != _settings.GameWindowHeight)
            {
                _worldWidth = _settings.GameWindowWidth;
                _worldHeight = _settings.GameWindowHeight;

                OnResize();
            }

            base.Update(totalMS, frameMS);
        }


        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null) =>
            base.Draw(spriteBatch, position, hue);

        protected override void OnMove()
        {
            Point position = Location;

            SpriteBatch3D sb = Service.Get<SpriteBatch3D>();

            if (position.X + Width - 4 > sb.GraphicsDevice.Viewport.Width)
                position.X = sb.GraphicsDevice.Viewport.Width - (Width - 4);
            if (position.X < -4)
                position.X = -4;

            if (position.Y + Height - 6 > sb.GraphicsDevice.Viewport.Height)
                position.Y = sb.GraphicsDevice.Viewport.Height - (Height - 6);
            if (position.Y < -6)
                position.Y = -6;

            Location = position;

            _settings.GameWindowX = position.X;
            _settings.GameWindowY = position.Y;        
        }

        protected override void OnResize()
        {
            Width = _worldWidth + 8;
            Height = _worldHeight + 12;

            _border.Width = Width;
            _border.Height = Height;


            _button.X = Width - 6;
            _button.Y = Height - 8;


            _viewport.Width = _worldWidth;
            _viewport.Height = _worldHeight;


            _chatControl.X = 4;
            _chatControl.Y = 6;
            _chatControl.Width = _worldWidth;
            _chatControl.Height = _worldHeight - 2;
        }


    }

    class GameBorder : GumpControl
    {
        private readonly SpriteTexture[] _borders = new SpriteTexture[2];

        public GameBorder(int x, int y, int w, int h) : base()
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;

            _borders[0] = IO.Resources.Gumps.GetGumpTexture(0x0A8C);
            _borders[1] = IO.Resources.Gumps.GetGumpTexture(0x0A8D);

          

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
            spriteBatch.Draw2DTiled(_borders[0], new Rectangle((int)position.X, (int)position.Y + _borders[0].Height / 2, Width - 2, _borders[0].Height), Vector3.Zero);
            // sotto
            spriteBatch.Draw2DTiled(_borders[0], new Rectangle((int)position.X, (int)position.Y + Height - _borders[0].Height * 2 + 1, Width + 1, _borders[0].Height), Vector3.Zero);
            //sx
            spriteBatch.Draw2DTiled(_borders[1], new Rectangle((int)position.X - _borders[1].Width / 2 + 1, (int)position.Y + _borders[0].Height / 2, _borders[1].Width, Height - _borders[0].Height * 2), Vector3.Zero);
            //dx
            spriteBatch.Draw2DTiled(_borders[1], new Rectangle((int)position.X + Width - _borders[1].Width + 1, (int)position.Y + 2, _borders[1].Width, Height - _borders[0].Height * 2), Vector3.Zero);

            return base.Draw(spriteBatch, position, hue);
        } 
    }
}