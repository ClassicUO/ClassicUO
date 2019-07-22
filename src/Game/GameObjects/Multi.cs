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

using System.Collections.Generic;
using System.Runtime.CompilerServices;

using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Utility;

namespace ClassicUO.Game.GameObjects
{
    internal sealed partial class Multi : GameObject
    {
        private StaticTiles? _itemData;

        private ushort _originalGraphic;

        private static readonly Queue<Multi> _pool = new Queue<Multi>();

        public Multi(Graphic graphic)
        {
            Graphic = _originalGraphic = graphic;
            UpdateGraphicBySeason();
            _isFoliage = ItemData.IsFoliage;
            AllowedToDraw = !GameObjectHelper.IsNoDrawable(Graphic);

            if (ItemData.Height > 5)
                _canBeTransparent = 1;
            else if (ItemData.IsRoof || ItemData.IsSurface && ItemData.IsBackground || ItemData.IsWall)
                _canBeTransparent = 1;
            else if (ItemData.Height == 5 && ItemData.IsSurface && !ItemData.IsBackground)
                _canBeTransparent = 1;
            else
                _canBeTransparent = 0;
        }

        public static Multi Create(Graphic graphic)
        {
            if (_pool.Count != 0)
            {
                var m = _pool.Dequeue();

                m.Graphic = m._originalGraphic = graphic;
                m.IsDestroyed = false;
                m._itemData = null;
                m.UpdateGraphicBySeason();
                m._isFoliage = m.ItemData.IsFoliage;
                m.AllowedToDraw = !GameObjectHelper.IsNoDrawable(m.Graphic);
                m.AlphaHue = 0;

                if (m.ItemData.Height > 5)
                    m._canBeTransparent = 1;
                else if (m.ItemData.IsRoof || m.ItemData.IsSurface && m.ItemData.IsBackground || m.ItemData.IsWall)
                    m._canBeTransparent = 1;
                else if (m.ItemData.Height == 5 && m.ItemData.IsSurface && !m.ItemData.IsBackground)
                    m._canBeTransparent = 1;
                else
                    m._canBeTransparent = 0;

                m.MultiOffsetX = m.MultiOffsetY = m.MultiOffsetZ = 0;
                m.CharacterIsBehindFoliage = false;

                return m;
            }

            return new Multi(graphic);
        }

        public string Name => ItemData.Name;

        public int MultiOffsetX { get; set; }
        public int MultiOffsetY { get; set; }
        public int MultiOffsetZ { get; set; }

        public StaticTiles ItemData
        {
            [MethodImpl(256)]
            get
            {
                if (!_itemData.HasValue)
                    _itemData = FileManager.TileData.StaticData[Graphic];

                return _itemData.Value;
            }
        }

        public override void UpdateGraphicBySeason()
        {
            Graphic = Season.GetSeasonGraphic(World.Season, _originalGraphic);
        }

        protected override void InitializeTextContainer()
        {
            if (EntityTextContainerContainer == null)
                EntityTextContainerContainer = new EntityTextContainer(this, 1);
        }

        public override void Destroy()
        {
            if (IsDestroyed)
                return;
            base.Destroy();
            _pool.Enqueue(this);
        }
    }
}