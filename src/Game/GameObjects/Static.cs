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

using System.Collections.Generic;
using System.Runtime.CompilerServices;

using ClassicUO.Configuration;
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
        private static readonly Queue<Static> _pool = new Queue<Static>();
        static Static()
        {
            for (int i = 0; i < 1000; i++)
                _pool.Enqueue(new Static());
        }

        private Static()
        {

        }

        public Static(ushort graphic, ushort hue, int index)
        {
            Graphic = OriginalGraphic = graphic;
            Hue = hue;

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

        public static Static Create(ushort graphic, ushort hue, int index)
        {
            if (_pool.Count != 0)
            {
                var s = _pool.Dequeue();
                s.Graphic = s.OriginalGraphic = graphic;
                s.Hue = hue;
                s.IsDestroyed = false;
                s.AlphaHue = 0;
                s.FoliageIndex = 0;
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

        public string Name => ItemData.Name;

        public ushort OriginalGraphic { get; private set; }

        public bool IsVegetation;

        public ref readonly StaticTiles ItemData => ref TileDataLoader.Instance.StaticData[Graphic];

        public void SetGraphic(ushort g)
        {
            Graphic = g;
            SetTextureByGraphic(g);
        }

        public void RestoreOriginalGraphic()
        {
            Graphic = OriginalGraphic;
            SetTextureByGraphic(Graphic);
        }

        public override void UpdateGraphicBySeason()
        {
            SetGraphic(SeasonManager.GetSeasonGraphic(World.Season, OriginalGraphic));
            AllowedToDraw = !GameObjectHelper.IsNoDrawable(Graphic);
            SetTextureByGraphic(Graphic);
            IsVegetation = StaticFilters.IsVegetation(Graphic);
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

            int startX = ProfileManager.Current.GameWindowPosition.X + 6;
            int startY = ProfileManager.Current.GameWindowPosition.Y + 6;
            var scene = Client.Game.GetScene<GameScene>();
            float scale = scene?.Scale ?? 1;
            int x = RealScreenPosition.X;
            int y = RealScreenPosition.Y;

            x += 22;
            y += 44;

            if (Texture != null)
                y -= Texture is ArtTexture t ? (t.ImageRectangle.Height >> 1) : (Texture.Height >> 1);

            x = (int)(x / scale);
            y = (int)(y / scale);

            for (; last != null; last = last.ListLeft)
            {
                if (last.RenderedText != null && !last.RenderedText.IsDestroyed)
                {
                    if (offY == 0 && last.Time < Time.Ticks)
                        continue;

                    last.OffsetY = offY;
                    offY += last.RenderedText.Height;

                    last.RealScreenPosition.X = startX + (x - (last.RenderedText.Width >> 1));
                    last.RealScreenPosition.Y = startY + (y - offY);
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