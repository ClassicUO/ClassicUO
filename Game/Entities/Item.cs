using ClassicUO.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game.Entities
{
    public enum Layer : byte
    {
        Invalid = 0x00,
        RightHand = 0x01,
        LeftHand = 0x02,
        Shoes = 0x03,
        Pants = 0x04,
        Shirt = 0x05,
        Helm = 0x06,
        Gloves = 0x07,
        Ring = 0x08,
        Talisman = 0x09,
        Neck = 0x0A,
        Hair = 0x0B,
        Waist = 0x0C,
        InnerTorso = 0x0D,
        Bracelet = 0x0E,
        Face = 0x0F,
        FacialHair = 0x10,
        MiddleTorso = 0x11,
        Earrings = 0x12,
        Arms = 0x13,
        Cloak = 0x14,
        Backpack = 0x15,
        OuterTorso = 0x16,
        OuterLegs = 0x17,
        InnerLegs = 0x18,
        Mount = 0x19,
        ShopBuy = 0x1A,
        ShopResale = 0x1B,
        ShopSell = 0x1C,
        Bank = 0x1D
    }

    public class Item : Entity
    {
        private ushort _amount;
        private Serial _container;
        private Layer _layer;

        public Item(Serial serial) : base(serial)
        {
        }


        public event EventHandler OwnerChanged;


        public ushort Amount
        {
            get { return _amount; }
            set
            {
                if (_amount != value)
                {
                    _amount = value;
                    _delta |= Delta.Attributes;
                }
            }
        }

        public Serial Container
        {
            get { return _container; }
            set
            {
                if (_container != value)
                {
                    _container = value;
                    _delta |= Delta.Ownership;
                }
            }
        }

        public Layer Layer
        {
            get { return _layer; }
            set
            {
                if (_layer != value)
                {
                    _layer = value;
                    _delta |= Delta.Ownership;
                }
            }
        }


        public bool IsCorpse => MathHelper.InRange(Graphic, 0x0ECA, 0x0ED2) || Graphic == 0x2006;

        public override bool Exists => World.Contains(Serial);

        public bool OnGround => !Container.IsValid;

        public Serial RootContainer
        {
            get
            {
                Item item = this;
                while (item.Container.IsItem)
                    item = World.Items.Get(item.Container);
                return item.Container.IsMobile ? item.Container : item;
            }
        }

        protected override void OnProcessDelta(Delta d)
        {
            base.OnProcessDelta(d);
            if (d.HasFlag(Delta.Ownership))
                OwnerChanged.Raise(this);
        }


    }
}
