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

using System.Collections.Generic;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game
{
    internal class ItemHold
    {
        public bool OnGround { get; private set; }
        public Position Position { get; private set; }
        public Serial Container { get; private set; }
        public Serial Serial { get; private set; }
        public Graphic Graphic { get; private set; }
        public Graphic DisplayedGraphic { get; private set; }
        public Hue Hue { get; private set; }
        public ushort Amount { get; private set; }
        public bool IsStackable { get; private set; }
        public bool IsPartialHue { get; private set; }
        public bool IsWearable { get; private set; }
        public bool HasAlpha { get; private set; }
        public Layer Layer { get; private set; }
        public Flags Flags { get; private set; }

        public bool Enabled { get; set; }
        public bool Dropped { get; set; }

        public void Set(Item item, ushort amount)
        {
            Enabled = true;

            Serial = item.Serial;
            Graphic = item.Graphic;
            DisplayedGraphic = item.IsCoin && amount == 1 ? item.Graphic : item.DisplayedGraphic;
            Position = item.Position;
            OnGround = item.OnGround;
            Container = item.Container;
            Hue = item.Hue;
            Amount = amount;
            IsStackable = item.ItemData.IsStackable;
            IsPartialHue = item.ItemData.IsPartialHue;
            HasAlpha = item.ItemData.IsTranslucent;
            IsWearable = item.ItemData.IsWearable;
            Layer = item.Layer;
            Flags = item.Flags;

            Engine.UI.GameCursor.SetDraggedItem(this);
        }

        public void Clear()
        {
            Serial = Serial.INVALID;
            Position = Position.INVALID;
            Container = Serial.INVALID;
            DisplayedGraphic = Graphic = Graphic.INVALID;
            Hue = Hue.INVALID;
            OnGround = false;
            Amount = 0;
            IsWearable = IsStackable = IsPartialHue = HasAlpha = false;
            Layer = Layer.Invalid;
            Flags = Flags.None;

            Dropped = false;
            Enabled = false;
        }
    }
}