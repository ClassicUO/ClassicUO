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
using ClassicUO.Game.Scenes;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
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
                m.IsFromTarget = false;

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
            _itemData = FileManager.TileData.StaticData[Graphic];
        }

        public override void UpdateTextCoordsV()
        {
            if (TextContainer == null)
                return;

            var last = TextContainer.Items;

            while (last?.ListRight != null)
                last = last.ListRight;

            if (last == null)
                return;

            int offY = 0;

            int startX = Engine.Profile.Current.GameWindowPosition.X + 6;
            int startY = Engine.Profile.Current.GameWindowPosition.Y + 6;
            var scene = Engine.SceneManager.GetScene<GameScene>();
            float scale = scene?.Scale ?? 1;
            int x = RealScreenPosition.X;
            int y = RealScreenPosition.Y;

            x += 22;
            y += 44;

            if (Texture != null)
                y -= Texture is ArtTexture t ? (t.ImageRectangle.Height >> 1) : (Texture.Height >> 1);

            for (; last != null; last = last.ListLeft)
            {
                if (last.RenderedText != null && !last.RenderedText.IsDestroyed)
                {
                    if (offY == 0 && last.Time < Engine.Ticks)
                        continue;


                    last.OffsetY = offY;
                    offY += last.RenderedText.Height;

                    last.RealScreenPosition.X = startX + (int)((x - (last.RenderedText.Width >> 1)) / scale);
                    last.RealScreenPosition.Y = startY + (int)((y - offY) / scale);
                }
            }

            FixTextCoordinatesInScreen();
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