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

using System;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using Microsoft.Xna.Framework;

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

        public QuestArrowGump(uint serial, int mx, int my) : base(serial, serial)
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

            GameScene scene = Client.Game.GetScene<GameScene>();

            if (IsDisposed || ProfileManager.Current == null || scene == null)
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

            int gox = World.Player.X - _mx;
            int goy = World.Player.Y - _my;


            int x = (ProfileManager.Current.GameWindowSize.X >> 1) - (gox - goy) * 22;
            int y = (ProfileManager.Current.GameWindowSize.Y >> 1) - (gox + goy) * 22;

            x -= (int) (World.Player.Offset.X);
            y -= (int) (((World.Player.Offset.Y - World.Player.Offset.Z)));
            y += (World.Player.Z << 2);


            switch (dir)
            {
                case Direction.North:
                    x -= _arrow.Width;
                    break;
                case Direction.South:
                    y -= _arrow.Height;
                    break;
                case Direction.East:
                    x -= _arrow.Width;
                    y -= _arrow.Height;
                    break;
                case Direction.West:
                    break;
                case Direction.Right:
                    x -= _arrow.Width;
                    y -= _arrow.Height / 2;
                    break;
                case Direction.Left:
                    x += _arrow.Width / 2;
                    y -= _arrow.Height / 2;
                    break;
                case Direction.Up:
                    x -= _arrow.Width / 2;
                    y += _arrow.Height / 2;
                    break;
                case Direction.Down:
                    x -= _arrow.Width / 2;
                    y -= _arrow.Height;
                    break;
            }


            Point p = new Point(x, y);
            p = Client.Game.Scene.Camera.WorldToScreen(p);

            x = p.X;
            y = p.Y;
         
            if (x < ProfileManager.Current.GameWindowPosition.X)
                x = ProfileManager.Current.GameWindowPosition.X;
            else if (x > ProfileManager.Current.GameWindowPosition.X + ProfileManager.Current.GameWindowSize.X - _arrow.Width)
                x = ProfileManager.Current.GameWindowPosition.X + ProfileManager.Current.GameWindowSize.X - _arrow.Width;


            if (y < ProfileManager.Current.GameWindowPosition.Y)
                y = ProfileManager.Current.GameWindowPosition.Y;
            else if (y > ProfileManager.Current.GameWindowPosition.Y + ProfileManager.Current.GameWindowSize.Y - _arrow.Height)
                y = ProfileManager.Current.GameWindowPosition.Y + ProfileManager.Current.GameWindowSize.Y - _arrow.Height;

            X = x;
            Y = y;

            if (_timer < Time.Ticks)
            {
                _timer = Time.Ticks + 1000;
                _needHue = !_needHue;
            }

            _arrow.Hue = (ushort) (_needHue ? 0 : 0x21);
        }


        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            bool leftClick = button == MouseButtonType.Left;
            bool rightClick = button == MouseButtonType.Right;

            if (leftClick || rightClick)
                GameActions.QuestArrow(rightClick);
        }

        public override bool Contains(int x, int y)
        {
            if (_arrow == null)
                return true;

            return _arrow.Contains(x, y);
        }
    }
}