using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game
{
    class ItemHold
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

        public void Set(Item item)
        {
            Enabled = true;

            Serial = item.Serial;
            Graphic = item.Graphic;
            DisplayedGraphic = item.DisplayedGraphic;
            Position = item.Position;
            OnGround = item.OnGround;
            Container = item.Container;
            Hue = item.Hue;
            Amount = item.Amount;
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
