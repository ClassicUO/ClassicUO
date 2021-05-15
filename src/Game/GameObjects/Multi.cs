#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

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
            Constants.PREDICTABLE_MULTIS,
            m =>
            {
                m.IsDestroyed = false;
                m.AlphaHue = 0;
                m.FoliageIndex = 0;
                m.IsHousePreview = false;
                m.MultiOffsetX = m.MultiOffsetY = m.MultiOffsetZ = 0;
                m.IsCustom = false;
                m.State = 0;
                m.IsMovable = false;
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
        public bool IsMovable;
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