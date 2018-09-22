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
using Microsoft.Xna.Framework;

namespace ClassicUO.Input
{
    public class MousePicker<T> where T : class
    {
        private MouseOverItem<T> _overObject;
        private MouseOverItem<T> _overTile;

        public MousePicker() => PickOnly = PickerType.PickNothing;

        public PickerType PickOnly { get; set; }
        public Point Position { get; set; }

        public T MouseOverObject => _overObject.Equals(default) ? null : _overObject.Object;

        public Point MouseOverObjectPoint => _overObject.Equals(default) ? Point.Zero : _overObject.InTexturePoint;


        public void UpdateOverObjects(MouseOverList<T> list, Point position)
        {
            _overObject = list.GetItem(position);
        }
    }
}