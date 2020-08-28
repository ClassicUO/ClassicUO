﻿#region license
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

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    internal sealed partial class Multi : GameObject
    {
        private ushort _originalGraphic;

        private static readonly QueuedPool<Multi> _pool = new QueuedPool<Multi>(Constants.PREDICTABLE_MULTIS, m =>
        {
            m.IsDestroyed = false;
            m.AlphaHue = 0;
            m.FoliageIndex = 0;
            m.IsFromTarget = false;
            m.MultiOffsetX = m.MultiOffsetY = m.MultiOffsetZ = 0;
            m.IsCustom = false;
            m.State = 0;
            m.Offset = Vector3.Zero;
        });



        public static Multi Create(ushort graphic)
        {
            Multi m = _pool.GetOne();
            m.Graphic = m._originalGraphic = graphic;
            m.UpdateGraphicBySeason();
            m.AllowedToDraw = !GameObjectHelper.IsNoDrawable(m.Graphic);

            if (m.ItemData.Height > 5)
                m._canBeTransparent = 1;
            else if (m.ItemData.IsRoof || m.ItemData.IsSurface && m.ItemData.IsBackground || m.ItemData.IsWall)
                m._canBeTransparent = 1;
            else if (m.ItemData.Height == 5 && m.ItemData.IsSurface && !m.ItemData.IsBackground)
                m._canBeTransparent = 1;
            else
                m._canBeTransparent = 0;

            return m;
        }


        public string Name => ItemData.Name;
        public int MultiOffsetX;
        public int MultiOffsetY;
        public int MultiOffsetZ;
        public CUSTOM_HOUSE_MULTI_OBJECT_FLAGS State = 0;
        public bool IsCustom;
        public bool IsVegetation;

        public ref StaticTiles ItemData => ref TileDataLoader.Instance.StaticData[Graphic];

        public override void UpdateGraphicBySeason()
        {
            Graphic = SeasonManager.GetSeasonGraphic(World.Season, _originalGraphic);
            IsVegetation = StaticFilters.IsVegetation(Graphic);
        }

        public override void UpdateTextCoordsV()
        {
            if (TextContainer == null)
                return;

            TextObject last = (TextObject) TextContainer.Items;

            while (last?.Next != null)
                last = (TextObject) last.Next;

            if (last == null)
                return;

            int offY = 0;

            int startX = ProfileManager.Current.GameWindowPosition.X + 6;
            int startY = ProfileManager.Current.GameWindowPosition.Y + 6;
            GameScene scene = Client.Game.GetScene<GameScene>();
            float scale = scene?.Scale ?? 1;
            int x = RealScreenPosition.X;
            int y = RealScreenPosition.Y;

            x += 22;
            y += 44;

            ArtTexture texture = ArtLoader.Instance.GetTexture(Graphic);

            if (texture != null)
                y -= (texture.ImageRectangle.Height >> 1);

            x = (int)(x / scale);
            y = (int)(y / scale);

            x += (int) Offset.X;
            y += (int) (Offset.Y - Offset.Z);

            for (; last != null; last = (TextObject) last.Previous)
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
            _pool.ReturnOne(this);
        }
    }
}