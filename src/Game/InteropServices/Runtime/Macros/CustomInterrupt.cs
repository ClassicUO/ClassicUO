#region license

// Copyright (C) 2020 project dust765
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
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using System;

namespace ClassicUO.Game.InteropServices.Runtime.Macros
{
    internal class CustomInterrupt
    {
        public static Item _backpack = World.Player.FindItemByLayer(Layer.Backpack);
        public static void Interrupt()
        {
            if (_backpack == null)
                return;

            GameScene gs = Client.Game.GetScene<GameScene>();

            Item item = FindUsedLayer();

            if (item != null)
            {
                GameActions.PickUp(item.Serial, 0, 0, 1);
                TimeSpan.FromMilliseconds(50);
                GameActions.Equip();
            }
        }

        public static Item FindUsedLayer()
        {
            Item layeredItem = World.Player.FindItemByLayer(Layer.Shirt);
            if (layeredItem != null)
                return layeredItem;

            layeredItem = World.Player.FindItemByLayer(Layer.Shoes);
            if (layeredItem != null)
                return layeredItem;

            layeredItem = World.Player.FindItemByLayer(Layer.Pants);
            if (layeredItem != null)
                return layeredItem;

            layeredItem = World.Player.FindItemByLayer(Layer.Helmet);
            if (layeredItem != null)
                return layeredItem;

            layeredItem = World.Player.FindItemByLayer(Layer.Gloves);
            if (layeredItem != null)
                return layeredItem;

            layeredItem = World.Player.FindItemByLayer(Layer.Ring);
            if (layeredItem != null)
                return layeredItem;

            layeredItem = World.Player.FindItemByLayer(Layer.Necklace);
            if (layeredItem != null)
                return layeredItem;

            layeredItem = World.Player.FindItemByLayer(Layer.Waist);
            if (layeredItem != null)
                return layeredItem;

            layeredItem = World.Player.FindItemByLayer(Layer.Torso);
            if (layeredItem != null)
                return layeredItem;

            layeredItem = World.Player.FindItemByLayer(Layer.Bracelet);
            if (layeredItem != null)
                return layeredItem;

            layeredItem = World.Player.FindItemByLayer(Layer.Tunic);
            if (layeredItem != null)
                return layeredItem;

            layeredItem = World.Player.FindItemByLayer(Layer.Earrings);
            if (layeredItem != null)
                return layeredItem;

            layeredItem = World.Player.FindItemByLayer(Layer.Arms);
            if (layeredItem != null)
                return layeredItem;

            layeredItem = World.Player.FindItemByLayer(Layer.Cloak);
            if (layeredItem != null)
                return layeredItem;

            layeredItem = World.Player.FindItemByLayer(Layer.Robe);
            if (layeredItem != null)
                return layeredItem;

            layeredItem = World.Player.FindItemByLayer(Layer.Skirt);
            if (layeredItem != null)
                return layeredItem;

            layeredItem = World.Player.FindItemByLayer(Layer.Legs);
            if (layeredItem != null)
                return layeredItem;

            layeredItem = World.Player.FindItemByLayer(Layer.OneHanded);
            if (layeredItem != null)
                return layeredItem;

            layeredItem = World.Player.FindItemByLayer(Layer.TwoHanded);
            if (layeredItem != null)
                return layeredItem;

            return null;
        }
    }
}
