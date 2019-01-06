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

using System.Runtime.CompilerServices;

using ClassicUO.Game.Data;
using ClassicUO.Game.Views;
using ClassicUO.IO;
using ClassicUO.IO.Resources;

namespace ClassicUO.Game.GameObjects
{
    internal sealed class Multi : GameObject
    {
        public Multi(Graphic graphic)
        {
            Graphic = graphic;
        }

        public string Name => ItemData.Name;

        private StaticTiles? _itemData;

        public StaticTiles ItemData
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (!_itemData.HasValue)
                    _itemData = FileManager.TileData.StaticData[Graphic];
                return _itemData.Value;
            }
        }

        protected override View CreateView() => new MultiView(this);
    }
}