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
using ClassicUO.Game.GameObjects;
using ClassicUO.IO.Resources;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game
{
    internal static class ItemHold
    {
        private static bool _enabled;

        public static Point MouseOffset;

        public static bool IsFixedPosition;
        public static bool IgnoreFixedPosition;
        public static int FixedX, FixedY;

        public static bool OnGround { get; private set; }
        public static ushort X { get; private set; }
        public static ushort Y { get; private set; }
        public static sbyte Z { get; private set; }
        public static uint Container { get; private set; }
        public static uint Serial { get; private set; }
        public static ushort Graphic { get; private set; }
        public static ushort DisplayedGraphic { get; private set; }
        public static bool IsGumpTexture { get; set; }
        public static ushort Hue { get; private set; }
        public static ushort Amount { get; private set; }
        public static ushort TotalAmount { get; private set; }
        public static bool IsStackable { get; private set; }
        public static bool IsPartialHue { get; private set; }
        public static bool IsWearable { get; private set; }
        public static bool HasAlpha { get; private set; }
        public static Layer Layer { get; private set; }
        public static Flags Flags { get; private set; }

        public static bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;

                if (!value)
                {
                    IsFixedPosition = false;
                    FixedX = 0;
                    FixedY = 0;
                    IgnoreFixedPosition = false;
                }
            }
        }

        public static bool Dropped { get; set; }
        public static bool UpdatedInWorld { get; set; }
        public static ref StaticTiles ItemData => ref TileDataLoader.Instance.StaticData[Graphic];

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
            TotalAmount = item.Amount;
            IsStackable = item.ItemData.IsStackable;
            IsPartialHue = item.ItemData.IsPartialHue;
            HasAlpha = item.ItemData.IsTranslucent;
            IsWearable = item.ItemData.IsWearable;
            Layer = item.Layer;
            Flags = item.Flags;
            MouseOffset = offset ?? Point.Zero;
            IsFixedPosition = false;
            FixedX = 0;
            FixedY = 0;
            IgnoreFixedPosition = false;
            IsGumpTexture = false;
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
            MouseOffset = Point.Zero;

            Dropped = false;
            Enabled = false;
            UpdatedInWorld = false;
            IsFixedPosition = false;
            FixedX = 0;
            FixedY = 0;
            IgnoreFixedPosition = false;
            IsGumpTexture = false;
        }
    }
}