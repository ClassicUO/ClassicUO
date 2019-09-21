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

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Input;

namespace ClassicUO.Game.UI.Controls
{
    internal class GumpPicContainer : GumpPic
    {
        public GumpPicContainer(int x, int y, Graphic graphic, Hue hue, Item item) : base(x, y, graphic, hue)
        {
            Item = item;
        }

        public Item Item { get; }

        protected override void OnMouseUp(int x, int y, MouseButton button)
        {
            if (button != MouseButton.Left)
                return;

            GameScene gs = Engine.SceneManager.GetScene<GameScene>();

            if (!gs.IsHoldingItem || !gs.IsMouseOverUI)
                return;

            if (Item.Layer == Layer.Backpack || !Item.OnGround || Item.Distance < Constants.DRAG_ITEMS_DISTANCE)
            {
                SelectedObject.Object = Item;
                gs.DropHeldItemToContainer(Item, x, y);
            }
            else
                gs.Audio.PlaySound(0x0051);
        }

        public override bool Contains(int x, int y)
        {
            float scale = Engine.UI.ContainerScale;

            x = (int)(x / scale);
            y = (int)(y / scale);

            return base.Contains(x, y);
        }
    }
}