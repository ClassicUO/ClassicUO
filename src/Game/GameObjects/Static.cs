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

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    internal sealed partial class Static : GameObject
    {
        private StaticTiles? _itemData;

        private static readonly Queue<Static> _pool = new Queue<Static>();

        public Static(Graphic graphic, Hue hue, int index)
        {
            Graphic = OriginalGraphic = graphic;
            Hue = hue;
            Index = index;

            UpdateGraphicBySeason();

            if (ItemData.Height > 5)
                _canBeTransparent = 1;
            else if (ItemData.IsRoof || ItemData.IsSurface && ItemData.IsBackground || ItemData.IsWall)
                _canBeTransparent = 1;
            else if (ItemData.Height == 5 && ItemData.IsSurface && !ItemData.IsBackground)
                _canBeTransparent = 1;
            else
                _canBeTransparent = 0;
        }

        public static Static Create(Graphic graphic, Hue hue, int index)
        {
            if (_pool.Count != 0)
            {
                var s = _pool.Dequeue();
                s.Graphic = s.OriginalGraphic = graphic;
                s.Hue = hue;
                s.Index = index;
                s.IsDestroyed = false;
                s._itemData = null;
                s.AlphaHue = 0;
                s._oldGraphic = 0;
                s.CharacterIsBehindFoliage = false;
                s.UpdateGraphicBySeason();

                if (s.ItemData.Height > 5)
                    s._canBeTransparent = 1;
                else if (s.ItemData.IsRoof || s.ItemData.IsSurface && s.ItemData.IsBackground || s.ItemData.IsWall)
                    s._canBeTransparent = 1;
                else if (s.ItemData.Height == 5 && s.ItemData.IsSurface && !s.ItemData.IsBackground)
                    s._canBeTransparent = 1;
                else
                    s._canBeTransparent = 0;

                return s;
            }

            return new Static(graphic, hue, index);
        }

        public int Index { get; private set; }

        public string Name => ItemData.Name;

        public Graphic OriginalGraphic { get; private set; }

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

        public override void UpdateGraphicBySeason()
        {
            SetGraphic(Season.GetSeasonGraphic(World.Season, OriginalGraphic));
            AllowedToDraw = !GameObjectHelper.IsNoDrawable(Graphic);
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