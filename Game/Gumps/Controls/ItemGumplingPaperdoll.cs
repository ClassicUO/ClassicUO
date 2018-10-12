﻿#region license

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

using ClassicUO.Game.GameObjects;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps
{
    internal class ItemGumplingPaperdoll : ItemGumpling
    {
        private ushort _gumpIndex;
        private Hue _hueOverride;

        public ItemGumplingPaperdoll(int x, int y, Item item) : base(item)
        {
            X = x;
            Y = y;
            HighlightOnMouseOver = false;
        }

        public int SlotIndex { get; set; }
        public bool IsFemale { get; set; }


        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null)
        {
            if (Texture == null || Texture.IsDisposed)
            {
                _gumpIndex = (ushort) (Item.ItemData.AnimID + (IsFemale ? 60000 : 50000));
                Texture = IO.Resources.Gumps.GetGumpTexture(_gumpIndex);
                Width = Texture.Width;
                Height = Texture.Height;
            }

            spriteBatch.Draw2D(Texture, position, RenderExtentions.GetHueVector(Item.Hue));

            return base.Draw(spriteBatch, position, hue);
        }

        protected override bool Contains(int x, int y)
            => IO.Resources.Gumps.Contains(_gumpIndex, x, y);
    }
}