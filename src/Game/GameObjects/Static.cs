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

using ClassicUO.Interfaces;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Utility;

namespace ClassicUO.Game.GameObjects
{
    internal partial class Static : GameObject
    {
        private StaticTiles? _itemData;

        public Static(Graphic graphic, Hue hue, int index)
        {
            Graphic = OriginalGraphic = graphic;
            Hue = hue;
            Index = index;

            _isFoliage = ItemData.IsFoliage;
            _isPartialHue = ItemData.IsPartialHue;
            _isTransparent = ItemData.IsTranslucent;

            AllowedToDraw = !GameObjectHelper.IsNoDrawable(Graphic);

            if (_isTransparent)
                _alpha = 0.5f;

            if (ItemData.Height > 5)
                _canBeTransparent = 1;
            else if (ItemData.IsRoof || (ItemData.IsSurface && ItemData.IsBackground) || ItemData.IsWall)
                _canBeTransparent = 1;
            else if (ItemData.Height == 5 && ItemData.IsSurface && !ItemData.IsBackground)
                _canBeTransparent = 1;
            else
                _canBeTransparent = 0;
        }

        public int Index { get; }

        public string Name => ItemData.Name;

        public Graphic OriginalGraphic { get; }

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

        public void SetGraphic(Graphic g)
        {
            Graphic = g;
            _itemData = FileManager.TileData.StaticData[Graphic];
        }

        public void RestoreOriginalGraphic()
        {
            Graphic = OriginalGraphic;
            _itemData = FileManager.TileData.StaticData[Graphic];
        }
    }
}