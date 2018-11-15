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

using ClassicUO.Game.Gumps.Controls;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.UIGumps
{
    internal class StatusGumpClassic : StatusGumpBase
    {
        public StatusGumpClassic() : base()
        {
            Point p = Point.Zero;
            _labels = new Label[(int)MobileStats.NumStats];

            AddChildren(new GumpPic(0, 0, 0x0802, 0));
            p.X = 244;
            p.Y = 112;

            if (p.X == 0)
            {
                p.X = 243;
                p.Y = 150;
            }

            Label text;

            if (!string.IsNullOrEmpty(World.Player.Name))
            {
                text = new Label(World.Player.Name, false, 0x0386, font: 1)
                {
                    X = 86,
                    Y = 42
                };
                _labels[(int)MobileStats.Name] = text;
                AddChildren(text);
            }

            text = new Label(World.Player.Strength.ToString(), false, 0x0386, font: 1)
            {
                X = 86,
                Y = 61
            };
            _labels[(int)MobileStats.Strength] = text;
            AddChildren(text);

            text = new Label(World.Player.Dexterity.ToString(), false, 0x0386, font: 1)
            {
                X = 86,
                Y = 73
            };
            _labels[(int)MobileStats.Dexterity] = text;
            AddChildren(text);

            text = new Label(World.Player.Intelligence.ToString(), false, 0x0386, font: 1)
            {
                X = 86,
                Y = 85
            };
            _labels[(int)MobileStats.Intelligence] = text;
            AddChildren(text);

            text = new Label(World.Player.IsFemale ? "F" : "M", false, 0x0386, font: 1)
            {
                X = 86,
                Y = 97
            };
            _labels[(int)MobileStats.Sex] = text;
            AddChildren(text);

            text = new Label(World.Player.ResistPhysical.ToString(), false, 0x0386, font: 1)
            {
                X = 86,
                Y = 109
            };
            _labels[(int)MobileStats.AR] = text;
            AddChildren(text);

            text = new Label($"{World.Player.Hits}/{World.Player.HitsMax}", false, 0x0386, font: 1)
            {
                X = 171,
                Y = 61
            };
            _labels[(int)MobileStats.HealthCurrent] = text;
            AddChildren(text);

            text = new Label($"{World.Player.Mana}/{World.Player.ManaMax}", false, 0x0386, font: 1)
            {
                X = 171,
                Y = 73
            };
            _labels[(int)MobileStats.ManaCurrent] = text;
            AddChildren(text);

            text = new Label($"{World.Player.Stamina}/{World.Player.StaminaMax}", false, 0x0386, font: 1)
            {
                X = 171,
                Y = 85
            };
            _labels[(int)MobileStats.StaminaCurrent] = text;
            AddChildren(text);

            text = new Label(World.Player.Gold.ToString(), false, 0x0386, font: 1)
            {
                X = 171,
                Y = 97
            };
            _labels[(int)MobileStats.Gold] = text;
            AddChildren(text);

            text = new Label(World.Player.Weight.ToString(), false, 0x0386, font: 1)
            {
                X = 171,
                Y = 109
            };
            _labels[(int)MobileStats.WeightCurrent] = text;
            AddChildren(text);

            _point = p;
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (_refreshTime < totalMS)
            {
                _refreshTime = totalMS + 250;

                _labels[(int)MobileStats.Name].Text = World.Player.Name;
                _labels[(int)MobileStats.Strength].Text = World.Player.Strength.ToString();
                _labels[(int)MobileStats.Dexterity].Text = World.Player.Dexterity.ToString();
                _labels[(int)MobileStats.Intelligence].Text = World.Player.Intelligence.ToString();
                _labels[(int)MobileStats.Sex].Text = World.Player.IsFemale ? "F" : "M";
                _labels[(int)MobileStats.AR].Text = World.Player.ResistPhysical.ToString();
                _labels[(int)MobileStats.HealthCurrent].Text = $"{World.Player.Hits}/{World.Player.HitsMax}";
                _labels[(int)MobileStats.ManaCurrent].Text = $"{World.Player.Mana}/{World.Player.ManaMax}";
                _labels[(int)MobileStats.StaminaCurrent].Text = $"{World.Player.Stamina}/{World.Player.StaminaMax}";
                _labels[(int)MobileStats.Gold].Text = World.Player.Gold.ToString();
                _labels[(int)MobileStats.WeightCurrent].Text = World.Player.Weight.ToString();
            }

            base.Update(totalMS, frameMS);
        }

        private enum MobileStats
        {
            Name,
            Strength,
            Dexterity,
            Intelligence,
            HealthCurrent,
            StaminaCurrent,
            ManaCurrent,
            WeightCurrent,
            Gold,
            AR,
            Sex,
            NumStats
        }
    }
}
