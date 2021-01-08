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

namespace ClassicUO.Game.GameObjects
{
    internal sealed partial class Static : GameObject
    {
        private static readonly QueuedPool<Static> _pool = new QueuedPool<Static>
        (
            Constants.PREDICTABLE_STATICS, s =>
            {
                s.IsDestroyed = false;
                s.AlphaHue = 0;
                s.FoliageIndex = 0;
            }
        );

        public string Name => ItemData.Name;

        public ushort OriginalGraphic { get; private set; }

        // ## BEGIN - END ## //
        public ushort OriginalHue { get; private set; }
        // ## BEGIN - END ## //

        public ref StaticTiles ItemData => ref TileDataLoader.Instance.StaticData[Graphic];

        public bool IsVegetation;


        public static Static Create(ushort graphic, ushort hue, int index)
        {
            Static s = _pool.GetOne();
            s.Graphic = s.OriginalGraphic = graphic;
            // ## BEGIN - END ## //  ORIG
            //s.Hue = hue;
            // ## BEGIN - END ## // 
            s.Hue = s.OriginalHue = hue;
            // ## BEGIN - END ## //
            s.UpdateGraphicBySeason();

            if (s.ItemData.Height > 5)
            {
                s._canBeTransparent = 1;
            }
            else if (s.ItemData.IsRoof || s.ItemData.IsSurface && s.ItemData.IsBackground || s.ItemData.IsWall)
            {
                s._canBeTransparent = 1;
            }
            else if (s.ItemData.Height == 5 && s.ItemData.IsSurface && !s.ItemData.IsBackground)
            {
                s._canBeTransparent = 1;
            }
            else
            {
                s._canBeTransparent = 0;
            }

            return s;
        }

        public void SetGraphic(ushort g)
        {
            Graphic = g;
        }

        public void RestoreOriginalGraphic()
        {
            Graphic = OriginalGraphic;
        }

        public override void UpdateGraphicBySeason()
        {
            SetGraphic(SeasonManager.GetSeasonGraphic(World.Season, OriginalGraphic));
            AllowedToDraw = !GameObjectHelper.IsNoDrawable(Graphic);
            IsVegetation = StaticFilters.IsVegetation(Graphic);
        }

        // ## BEGIN - END ## //
        public void RestoreOriginalHue()
        {
            Hue = OriginalHue;
        }
        // ## BEGIN - END ## //

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