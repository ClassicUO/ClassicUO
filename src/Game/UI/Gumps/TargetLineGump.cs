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

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Gumps
{
    internal class TargetLineGump : Gump
    {
        private readonly GumpPic _background;
        private readonly GumpPicWithWidth _hp;

        public TargetLineGump(Mobile mobile) : base(mobile.Serial, 0)
        {
            CanMove = false;
            AcceptMouseInput = false;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = false;

            Add(_background = new GumpPic(0, 0, 0x1068, 0));
            Add(_hp = new GumpPicWithWidth(0, 0, 0x1069, 0, 1));

            Mobile = mobile;
        }

        public Mobile Mobile { get; }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (IsDisposed)
                return;

            if (Mobile == null || Mobile.IsDestroyed)
            {
                Dispose();
                Engine.UI.RemoveTargetLineGump(Mobile);

                return;
            }

            int per = Mobile.HitsMax;

            if (per > 0)
            {
                per = Mobile.Hits * 100 / per;

                if (per > 100)
                    per = 100;

                if (per < 1)
                    per = 0;
                else
                    per = 34 * per / 100;
            }

            _hp.Percent = per;

            if (Mobile.IsPoisoned)
            {
                if (_hp.Hue != 63)
                    _hp.Hue = 63;
            }
            else if (Mobile.IsYellowHits)
            {
                if (_hp.Hue != 53)
                    _hp.Hue = 53;
            }
            else if (_hp.Hue != 90)
                _hp.Hue = 90;


            _background.Hue = Notoriety.GetHue(Mobile.NotorietyFlag);
            ;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (Engine.Profile == null || Engine.Profile.Current == null || Mobile == null || Mobile.IsDestroyed)
                return false;

            float scale = Engine.SceneManager.GetScene<GameScene>().Scale;

            int gx = Engine.Profile.Current.GameWindowPosition.X;
            int gy = Engine.Profile.Current.GameWindowPosition.Y;
            int w = Engine.Profile.Current.GameWindowSize.X;
            int h = Engine.Profile.Current.GameWindowSize.Y;

            x = (int) ((Mobile.RealScreenPosition.X + Mobile.Offset.X - (Width >> 1) + 22) / scale);
            y = (int) ((Mobile.RealScreenPosition.Y + Mobile.Offset.Y - Mobile.Offset.Z + 22) / scale);

            x += gx + 6;
            y += gy;

            X = x;
            Y = y;

            if (x < gx || x + Width > gx + w)
                return false;

            if (y < gy || y + Height > gy + h)
                return false;


            return base.Draw(batcher, x, y);
        }
    }
}