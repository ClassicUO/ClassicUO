using ClassicUO.Game.WorldObjects.Views;
using ClassicUO.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game.WorldObjects
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
        private bool _isMulti;

        public Item(Serial serial) : base(serial)
        {
        }


        public event EventHandler OwnerChanged;



        public new ItemView ViewObject => (ItemView)base.ViewObject;

        protected override WorldRenderObject CreateView() => new ItemView(this);

        public AssetsLoader.StaticTiles ItemData => AssetsLoader.TileData.StaticData[Graphic];

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

        public bool IsCoin => Graphic >= 0x0EEA && Graphic <= 0x0EF2;

        public Graphic DisplayedGraphic
        {
            get
            {
                if (IsCoin)
                {
                    if (Amount > 5)
                        return (Graphic)(Graphic + 2);
                    if (Amount > 1)
                        return (Graphic)(Graphic + 1);
                }

                return Graphic;
            }
        }

        public bool IsMulti
        {
            get => _isMulti;
            set
            {
                if (_isMulti != value)
                {
                    _isMulti = value;

                    if (value)
                    {
                        if (Multi == null)
                        {
                            short minX = 0;
                            short minY = 0;
                            short maxX = 0;
                            short maxY = 0;

                            int count = AssetsLoader.Multi.GetCount(Graphic);
                            MultiComponent[] components = new MultiComponent[count];

                            for (int i = 0; i < count; i++)
                            {
                                AssetsLoader.MultiBlock pbm = AssetsLoader.Multi.GetMulti(i);

                                MultiComponent component = new MultiComponent(pbm.ID, (ushort)(Position.X + pbm.X), (ushort)(Position.Y + pbm.Y), (sbyte)(Position.Z + pbm.Z), pbm.Flags);

                                if (pbm.X < minX)
                                    minX = pbm.X;
                                if (pbm.X > maxX)
                                    maxX = pbm.X;
                                if (pbm.Y < minY)
                                    minY = pbm.Y;
                                if (pbm.Y > maxY)
                                    maxY = pbm.Y;
                            }

                            Multi = new Multi(this)
                            {
                                MinX = minX,
                                MaxX = maxX,
                                MinY = minY,
                                MaxY = maxY,
                                Components = components
                            };
                        }
                    }
                    else
                    {
                        Multi = null;
                    }
                }
            }
        }

        public Multi Multi { get; private set; }

        public bool IsCorpse => Utility.MathHelper.InRange(Graphic, 0x0ECA, 0x0ED2) || Graphic == 0x2006;

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
