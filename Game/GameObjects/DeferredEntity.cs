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
using ClassicUO.Game.Map;
using ClassicUO.Game.Views;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    public class DeferredEntity : GameObject
    {
        public GameObject Entity { get; set; }

        public Vector3 AtPosition { get; set; }

        public sbyte Z { get; set; }

        public Tile AssociatedTile { get; set; }

        //private View GetBaseView(GameObject entity)
        //{
        //    if (entity is Mobile)
        //        return (MobileView) entity.View;
        //    if (entity is AnimatedItemEffect)
        //        return (AnimatedEffectView) entity.View;
        //    if (entity is )
        //    if (entity is Item item && item.IsCorpse)
        //        return (ItemView) entity.View;
        //    return null;
        //}

        public void Reset()
        {
            AssociatedTile.RemoveGameObject(this);
            DisposeView();
            Entity = null;
            AtPosition = Vector3.Zero;
            Position = Position.Invalid;
            Z = sbyte.MinValue;
        }

        protected override View CreateView()
        {
            return new DeferredView(this, Entity.View, AtPosition);
        }

        public override void Dispose()
        {
            Reset();
            base.Dispose();
        }

        public override string ToString()
        {
            return $"{base.ToString()} | deferred";
        }
    }
}