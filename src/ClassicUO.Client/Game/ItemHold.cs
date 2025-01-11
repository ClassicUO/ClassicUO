// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Assets;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game
{
    sealed class ItemHold
    {
        private bool _enabled;

        public Point MouseOffset;

        public bool IsFixedPosition;
        public bool IgnoreFixedPosition;
        public int FixedX, FixedY;

        public bool OnGround { get; private set; }
        public ushort X { get; private set; }
        public ushort Y { get; private set; }
        public sbyte Z { get; private set; }
        public uint Container { get; private set; }
        public uint Serial { get; private set; }
        public ushort Graphic { get; private set; }
        public ushort DisplayedGraphic { get; private set; }
        public bool IsGumpTexture { get; set; }
        public ushort Hue { get; private set; }
        public ushort Amount { get; private set; }
        public ushort TotalAmount { get; private set; }
        public bool IsStackable { get; private set; }
        public bool IsPartialHue { get; private set; }
        public bool IsWearable { get; private set; }
        public bool HasAlpha { get; private set; }
        public Layer Layer { get; private set; }
        public Flags Flags { get; private set; }

        public bool Enabled
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

        public bool Dropped { get; set; }
        public bool UpdatedInWorld { get; set; }
        public ref StaticTiles ItemData => ref Client.Game.UO.FileManager.TileData.StaticData[Graphic];

        public void Set(Item item, ushort amount, Point? offset = null)
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

        public void Clear()
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