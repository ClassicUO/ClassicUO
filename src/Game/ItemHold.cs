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
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game
{
    internal static class ItemHold
    {
        public static bool OnGround { get; private set; }
        public static ushort X { get; private set; }
        public static ushort Y { get; private set; }
        public static sbyte Z { get; private set; }
        public static uint Container { get; private set; }
        public static uint Serial { get; private set; }
        public static ushort Graphic { get; private set; }
        public static ushort DisplayedGraphic { get; private set; }
        public static ushort Hue { get; private set; }
        public static ushort Amount { get; private set; }
        public static bool IsStackable { get; private set; }
        public static bool IsPartialHue { get; private set; }
        public static bool IsWearable { get; private set; }
        public static bool HasAlpha { get; private set; }
        public static Layer Layer { get; private set; }
        public static Flags Flags { get; private set; }

        public static bool Enabled { get; set; }
        public static bool Dropped { get; set; }

        public static void Set(Item item, ushort amount, Point? offset = null)
        {
            Enabled = true;

            Serial = item.Serial;
            Graphic = item.Graphic;
            DisplayedGraphic = item.IsCoin && amount == 1 ? item.Graphic : item.DisplayedGraphic;
            X = item.X;
            Y = item.Y;
            Z = item.Z;
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
        }

        public static void Clear()
        {
            Serial = 0;
            X = 0xFFFF;
            Y = 0xFFFF;
            Z = 0;
            Container = 0;
            DisplayedGraphic = Graphic = 0xFFFF;
            Hue = 0xFFFF;
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