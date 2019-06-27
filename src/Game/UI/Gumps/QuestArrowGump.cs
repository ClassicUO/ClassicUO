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

using System;

using ClassicUO.Game.Data;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;

namespace ClassicUO.Game.UI.Gumps
{
    internal class QuestArrowGump : Gump
    {
        private GumpPic _arrow;
        private Direction _direction;
        private int _mx;
        private int _my;
        private bool _needHue;
        private float _timer;

        public QuestArrowGump(Serial serial, int mx, int my) : base(serial, serial)
        {
            CanMove = false;
            CanCloseWithRightClick = false;

            AcceptMouseInput = true;

            SetRelativePosition(mx, my);
            WantUpdateSize = false;
        }

        public void SetRelativePosition(int x, int y)
        {
            _mx = x;
            _my = y;
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (!World.InGame) Dispose();

            var scene = Engine.SceneManager.GetScene<GameScene>();

            if (IsDisposed || Engine.Profile.Current == null || scene == null)
                return;

            Direction dir = (Direction) GameCursor.GetMouseDirection(World.Player.X, World.Player.Y, _mx, _my, 0);
            ushort gumpID = (ushort) (0x1194 + ((int) dir + 1) % 8);

            if (_direction != dir || _arrow == null)
            {
                _direction = dir;

                if (_arrow == null)
                    Add(_arrow = new GumpPic(0, 0, gumpID, 0));
                else
                    _arrow.Graphic = gumpID;

                Width = _arrow.Width;
                Height = _arrow.Height;
            }

            var scale = scene.Scale;


            int gox = _mx - World.Player.X;
            int goy = _my - World.Player.Y;


            int x = (Engine.Profile.Current.GameWindowPosition.X + (Engine.Profile.Current.GameWindowSize.X >> 1)) + 6 + ((gox - goy) * (int) (22 / scale)) - (int) ((_arrow.Width / 2f) / scale);
            int y = (Engine.Profile.Current.GameWindowPosition.Y + (Engine.Profile.Current.GameWindowSize.Y >> 1)) + 6 + ((gox + goy) * (int) (22 / scale)) + (int) ((_arrow.Height) / scale);

            x -= (int) (World.Player.Offset.X / scale);
            y -= (int) (((World.Player.Offset.Y - World.Player.Offset.Z) + (World.Player.Z >> 2)) / scale);

         
            if (x < Engine.Profile.Current.GameWindowPosition.X)
                x = Engine.Profile.Current.GameWindowPosition.X;
            else if (x > Engine.Profile.Current.GameWindowPosition.X + Engine.Profile.Current.GameWindowSize.X - _arrow.Width)
                x = Engine.Profile.Current.GameWindowPosition.X + Engine.Profile.Current.GameWindowSize.X - _arrow.Width;


            if (y < Engine.Profile.Current.GameWindowPosition.Y)
                y = Engine.Profile.Current.GameWindowPosition.Y;
            else if (y > Engine.Profile.Current.GameWindowPosition.Y + Engine.Profile.Current.GameWindowSize.Y - _arrow.Height)
                y = Engine.Profile.Current.GameWindowPosition.Y + Engine.Profile.Current.GameWindowSize.Y - _arrow.Height;

            X = x;
            Y = y;

            if (_timer < Engine.Ticks)
            {
                _timer = Engine.Ticks + 1000;
                _needHue = !_needHue;
            }

            _arrow.Hue = (Hue) (_needHue ? 0 : 0x21);
        }


        protected override void OnMouseUp(int x, int y, MouseButton button)
        {
            var leftClick = button == MouseButton.Left;
            var rightClick = button == MouseButton.Right;

            if (leftClick || rightClick)
                GameActions.QuestArrow(rightClick);
        }

        public override bool Contains(int x, int y)
        {
            if (_arrow == null)
                return true;

            return _arrow.Texture.Contains(x, y);
        }
    }
}