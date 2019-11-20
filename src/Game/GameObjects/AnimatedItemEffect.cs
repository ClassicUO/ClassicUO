﻿#region license

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

namespace ClassicUO.Game.GameObjects
{
    internal sealed partial class AnimatedItemEffect : GameEffect
    {
        public AnimatedItemEffect(Graphic graphic, Hue hue, int duration, int speed)
        {
            Graphic = graphic;
            Hue = hue;
            Duration = duration > 0 ? Time.Ticks + duration : -1;
            Speed = speed;
            Load();
        }

        public AnimatedItemEffect(GameObject source, Graphic graphic, Hue hue, int duration, int speed) : this(graphic, hue, duration, speed)
        {
            SetSource(source);
        }

        public AnimatedItemEffect(Serial source, Graphic graphic, Hue hue, int duration, int speed) : this(source, 0, 0, 0, graphic, hue, duration, speed)
        {
        }

        public AnimatedItemEffect(int sourceX, int sourceY, int sourceZ, Graphic graphic, Hue hue, int duration, int speed) : this(graphic, hue, duration, speed)
        {
            SetSource(sourceX, sourceY, sourceZ);
        }

        public AnimatedItemEffect(Serial sourceSerial, int sourceX, int sourceY, int sourceZ, Graphic graphic, Hue hue, int duration, int speed) : this(graphic, hue, duration, speed)
        {
            Entity source = World.Get(sourceSerial);

            if (source != null && sourceSerial.IsValid)
                SetSource(source);
            else
                SetSource(sourceX, sourceY, sourceZ);
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (!IsDestroyed)
            {
                (int x, int y, int z) = GetSource();

                if (Source != null) Offset = Source.Offset;

                if (X != x || Y != y || Z != z)
                {
                    Position = new Position((ushort) x, (ushort) y, (sbyte) z);
                    AddToTile();
                }
            }
        }
    }
}