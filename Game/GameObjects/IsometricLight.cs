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

using System;

using ClassicUO.IO.Resources;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    public sealed class IsometricLight
    {
        private float _direction = 4.12f;
        private float _height = -0.75f;
        private int _overall = 9;
        private int _personal = 9;

        public int Personal
        {
            get => _personal;
            set
            {
                _personal = value;
                Recalculate();
            }
        }

        public int Overall
        {
            get => _overall;
            set
            {
                _overall = value;
                Recalculate();
            }
        }

        public float Direction
        {
            get => _direction;
            set
            {
                _direction = value;
                Recalculate();
            }
        }

        public float Height
        {
            get => _height;
            set
            {
                _height = value;
                Recalculate();
            }
        }

        public float IsometricLevel { get; private set; }

        public Vector3 IsometricDirection { get; private set; }

        private void Recalculate()
        {           
            IsometricLevel = (32 - Overall + Personal) / 32.0f;
            //_direction = 1.2f;
            //IsometricDirection = Vector3.Normalize(new Vector3((float)Math.Cos(_direction), (float)Math.Sin(_direction), 1f));
            IsometricDirection = new Vector3(-1.0f, -1.0f, .5f);
        }
    }
}