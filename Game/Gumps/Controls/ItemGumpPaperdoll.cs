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

using ClassicUO.Game.GameObjects;
using ClassicUO.Interfaces;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.Controls
{
    internal class ItemGumpPaperdoll : ItemGump, IMobilePaperdollOwner
    {
        private readonly ushort _gumpIndex;
        private readonly bool _isTransparent;

        public ItemGumpPaperdoll(int x, int y, Item item, Mobile owner, bool transparent = false) : base(item)
        {
            X = x;
            Y = y;
            Mobile = owner;
            HighlightOnMouseOver = false;
            _isTransparent = transparent;
            _gumpIndex = (ushort) (Item.ItemData.AnimID + (owner.IsFemale ? 60000 : 50000));

            if (Animations.EquipConversions.TryGetValue(Item.Graphic, out var dict))
            {
                if (dict.TryGetValue(Item.ItemData.AnimID, out EquipConvData data)) _gumpIndex = data.Gump;
            }

            Texture = IO.Resources.Gumps.GetGumpTexture(_gumpIndex);
            Width = Texture.Width;
            Height = Texture.Height;
        }

        public int SlotIndex { get; set; }

        public Mobile Mobile { get; set; }

        public override void Update(double totalMS, double frameMS)
        {
            if (Item.IsDisposed || IsDisposed)
                return;
            base.Update(totalMS, frameMS);
            Texture.Ticks = (long) totalMS;
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Point position, Vector3? hue = null)
        {
            if (Item.IsDisposed || IsDisposed)
                return false;

            return spriteBatch.Draw2D(Texture, position, ShaderHuesTraslator.GetHueVector(Item.Hue & 0x3FFF, TileData.IsPartialHue((long) Item.ItemData.Flags), _isTransparent ? .5f : 0, false));
        }

        protected override bool Contains(int x, int y)
        {
            return IO.Resources.Gumps.Contains(_gumpIndex, x, y);
        }
    }
}