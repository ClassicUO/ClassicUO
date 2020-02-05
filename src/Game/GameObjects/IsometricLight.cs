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

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    internal sealed class IsometricLight
    {
        private float _height = -0.75f;
        private int _overall = 9, _realOveall = 9;
        private int _personal = 9, _realPersonal = 9;

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

        public float Height
        {
            get => _height;
            set
            {
                _height = value;
                Recalculate();
            }
        }

        public int RealPersonal
        {
            get => _realPersonal;
            set
            {
                _realPersonal = value;
                Recalculate();
            }
        }

        public int RealOverall
        {
            get => _realOveall;
            set
            {
                _realOveall = value;
                Recalculate();
            }
        }

        public float IsometricLevel { get; private set; }

        public Vector3 IsometricDirection { get; } = new Vector3(-1.0f, -1.0f, .5f);

        private void Recalculate()
        {
            int reverted = 32 - Overall; //if overall is 0, we have MAXIMUM light, if 30, we have the MINIMUM light, so 30 is the max, but we must have some remainder for visibility
            float current = Personal > reverted ? Personal : reverted;
            IsometricLevel = current * 0.03125f;
        }
    }
}