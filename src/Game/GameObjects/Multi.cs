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

using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.IO.Resources;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    internal sealed partial class Multi : GameObject
    {
        private static readonly QueuedPool<Multi> _pool = new QueuedPool<Multi>
        (
            Constants.PREDICTABLE_MULTIS, m =>
            {
                m.IsDestroyed = false;
                m.AlphaHue = 0;
                m.FoliageIndex = 0;
                m.IsHousePreview = false;
                m.MultiOffsetX = m.MultiOffsetY = m.MultiOffsetZ = 0;
                m.IsCustom = false;
                m.State = 0;
                m.Offset = Vector3.Zero;
            }
        );
        private ushort _originalGraphic;


        public string Name => ItemData.Name;

        public ref StaticTiles ItemData => ref TileDataLoader.Instance.StaticData[Graphic];
        public bool IsCustom;
        public bool IsVegetation;
        public int MultiOffsetX;
        public int MultiOffsetY;
        public int MultiOffsetZ;
        public CUSTOM_HOUSE_MULTI_OBJECT_FLAGS State = 0;


        public static Multi Create(ushort graphic)
        {
            Multi m = _pool.GetOne();
            m.Graphic = m._originalGraphic = graphic;
            m.UpdateGraphicBySeason();
            m.AllowedToDraw = !GameObjectHelper.IsNoDrawable(m.Graphic);

            if (m.ItemData.Height > 5)
            {
                m._canBeTransparent = 1;
            }
            else if (m.ItemData.IsRoof || m.ItemData.IsSurface && m.ItemData.IsBackground || m.ItemData.IsWall)
            {
                m._canBeTransparent = 1;
            }
            else if (m.ItemData.Height == 5 && m.ItemData.IsSurface && !m.ItemData.IsBackground)
            {
                m._canBeTransparent = 1;
            }
            else
            {
                m._canBeTransparent = 0;
            }

            return m;
        }

        public override void UpdateGraphicBySeason()
        {
            Graphic = SeasonManager.GetSeasonGraphic(World.Season, _originalGraphic);
            IsVegetation = StaticFilters.IsVegetation(Graphic);
        }

        public override void Destroy()
        {
            if (IsDestroyed)
            {
                return;
            }

            base.Destroy();
            _pool.ReturnOne(this);
        }
    }
}