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

namespace ClassicUO.Game.GameObjects
{
    internal sealed partial class LightningEffect : GameEffect
    {
        public LightningEffect(ushort hue)
        {
            Graphic = 0x4E20;
            Hue = hue;
            IsEnabled = true;
            IntervalInMs = Constants.ITEM_EFFECT_ANIMATION_DELAY;
            AnimIndex = 0;
        }

        public LightningEffect(GameObject source, ushort hue) : this(hue)
        {
            SetSource(source);
        }

        public LightningEffect(int x, int y, int z, ushort hue) : this(hue)
        {
            SetSource(x, y, z);
        }

        public LightningEffect(uint src, int x, int y, int z, ushort hue) : this(hue)
        {
            Entity source = World.Get(src);

            if (SerialHelper.IsValid(src) && source != null)
                SetSource(source);
            else
                SetSource(x, y, z);
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (!IsDestroyed)
            {
                if (AnimIndex >= 10) //TODO: fix time
                    Destroy();
                else
                {
                    AnimationGraphic = (ushort) (Graphic + AnimIndex);

                    if (NextChangeFrameTime < totalMS)
                    {
                        AnimIndex++;
                        NextChangeFrameTime = (long) totalMS + IntervalInMs;
                    }

                    (int x, int y, int z) = GetSource();

                    if (X != x || Y != y || Z != z)
                    {
                        X = (ushort) x;
                        Y = (ushort) y;
                        Z = (sbyte) z;
                        UpdateScreenPosition();
                        AddToTile();
                    }
                }
            }
        }
    }
}