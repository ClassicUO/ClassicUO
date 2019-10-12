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
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using ClassicUO.Game.Managers;
using ClassicUO.IO;
using ClassicUO.IO.Resources;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    internal sealed partial class Land : GameObject
    {
        private static readonly Queue<Land> _pool = new Queue<Land>();

        public Land(Graphic graphic)
        {
            Graphic = graphic;
            IsStretched = TileData.TexID == 0 && TileData.IsWet;

            AllowedToDraw = Graphic > 2;

            AlphaHue = 255;
        }

        public static Land Create(Graphic graphic)
        {
            if (_pool.Count != 0)
            {
                var l = _pool.Dequeue();
                l.Graphic = graphic;
                l.IsDestroyed = false;
                l._tileData = null;
                l.AlphaHue = 255;
                l.IsStretched = l.TileData.TexID == 0 && l.TileData.IsWet;
                l.AllowedToDraw = l.Graphic > 2;
                l.Normals = null;
                l.Rectangle = Rectangle.Empty;
                l.MinZ = l.AverageZ = 0;
                l.Texture = null;
                l.Bounds = Rectangle.Empty;
                return l;
            }
            return new Land(graphic);
        }

        public override void Destroy()
        {
            if (IsDestroyed)
                return;

            base.Destroy();
            _pool.Enqueue(this);
        }

        private LandTiles? _tileData;

        public Vector3[] Normals;

        public Rectangle Rectangle;

        public LandTiles TileData
        {
            [MethodImpl(256)]
            get
            {
                if (!_tileData.HasValue)
                    _tileData = FileManager.TileData.LandData[Graphic];

                return _tileData.Value;
            }
        }

        public sbyte MinZ { get; set; }

        public sbyte AverageZ { get; set; }

        public bool IsStretched { get; set; }
        

        public void UpdateZ(int zTop, int zRight, int zBottom, sbyte currentZ)
        {
            if (IsStretched)
            {
                int x = (currentZ << 2) + 1;
                int y = (zTop << 2);
                int w = (zRight << 2) - x;
                int h = (zBottom << 2) + 1 - y;

                Rectangle.X = x;
                Rectangle.Y = y;
                Rectangle.Width = w;
                Rectangle.Height = h;

                if (Math.Abs(currentZ - zRight) <= Math.Abs(zBottom - zTop))
                    AverageZ = (sbyte) ((currentZ + zRight) >> 1);
                else
                    AverageZ = (sbyte) ((zBottom + zTop) >> 1);

                MinZ = currentZ;

                if (zTop < MinZ)
                    MinZ = (sbyte) zTop;

                if (zRight < MinZ)
                    MinZ = (sbyte) zRight;

                if (zBottom < MinZ)
                    MinZ = (sbyte) zBottom;
            }
        }

        public int CalculateCurrentAverageZ(int direction)
        {
            int result = GetDirectionZ(((byte) (direction >> 1) + 1) & 3);

            if ((direction & 1) != 0)
                return result;

            return (result + GetDirectionZ(direction >> 1)) >> 1;
        }

        [MethodImpl(256)]
        private int GetDirectionZ(int direction)
        {
            switch (direction)
            {
                case 1: return Rectangle.Bottom >> 2;
                case 2: return Rectangle.Right >> 2;
                case 3: return Rectangle.Top >> 2;
                default: return Z;
            }
        }
    }
}