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

using System.Runtime.CompilerServices;

using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Utility;

namespace ClassicUO.Game.GameObjects
{
    internal sealed partial class Static : GameObject
    {
        private StaticTiles? _itemData;

        public int Index { get; private set; }

        public string Name => ItemData.Name;

        public Graphic OriginalGraphic { get; private set; }

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

        public Static SetGraphic_New(ushort graphic, int index)
        {
            RemoveFromTile();
            Position = Position.INVALID;
            CurrentRenderIndex = 0;
            PriorityZ = 0;
            Hue = 0;
            Graphic = OriginalGraphic = graphic;
            _itemData = FileManager.TileData.StaticData[Graphic];
            Index = index;

            AllowedToDraw = !GameObjectHelper.IsNoDrawable(Graphic);
            AlphaHue = 0;

            if (ItemData.Height > 5)
                _canBeTransparent = 1;
            else if (ItemData.IsRoof || ItemData.IsSurface && ItemData.IsBackground || ItemData.IsWall)
                _canBeTransparent = 1;
            else if (ItemData.Height == 5 && ItemData.IsSurface && !ItemData.IsBackground)
                _canBeTransparent = 1;
            else
                _canBeTransparent = 0;

            Texture = null;

            return this;
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