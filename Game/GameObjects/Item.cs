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

using System;
using ClassicUO.Game.Data;
using ClassicUO.Game.Views;
using ClassicUO.Interfaces;
using ClassicUO.IO.Resources;
using ClassicUO.Utility;

namespace ClassicUO.Game.GameObjects
{
    public enum Layer : byte
    {
        Invalid = 0x00,
        OneHanded = 0x01,
        TwoHanded = 0x02,
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

    public class Item : Entity, IDynamicItem
    {
        private ushort _amount;
        private Serial _container;
        private Graphic? _displayedGraphic;


        private bool _invokeUpdate;

        private bool _isMulti;

        private Layer _layer;


        private ulong _spellsBitFiled;

        public Item(Serial serial) : base(serial)
        {
            Items.Added += ItemsOnAddedAndDeleted;
            Items.Removed += ItemsOnAddedAndDeleted;
        }

        private GameEffect _effect;

        public GameEffect Effect
        {
            get => _effect;
            set
            {
                _effect?.Dispose();
                _effect = value;
            }
        }

        public ushort Amount
        {
            get => _amount;
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
            get => _container;
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
            get => _layer;
            set
            {
                if (_layer != value)
                {
                    _layer = value;
                    _delta |= Delta.Ownership;
                }
            }
        }


        public bool UsedLayer { get; set; }

        public bool IsCoin => Graphic >= 0x0EEA && Graphic <= 0x0EF2;

        public Item[] Equipment { get; } = new Item[(int) Layer.Bank + 1];

        public bool IsPickable => ItemData.Weight < 255;

        public Graphic DisplayedGraphic
        {
            get
            {
                if (_displayedGraphic.HasValue) return _displayedGraphic.Value;

                if (IsCoin)
                {
                    if (Amount > 5) return (Graphic) (Graphic + 2);

                    if (Amount > 1) return (Graphic) (Graphic + 1);
                }

                return Graphic;
            }
            set => _displayedGraphic = value;
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

                            int count = IO.Resources.Multi.GetCount(Graphic);
                            MultiComponent[] components = new MultiComponent[count];

                            for (int i = 0; i < count; i++)
                            {
                                MultiBlock pbm = IO.Resources.Multi.GetMulti(i);

                                MultiComponent component = new MultiComponent(pbm.ID, (ushort) (Position.X + pbm.X),
                                    (ushort) (Position.Y + pbm.Y), (sbyte) (Position.Z + pbm.Z), pbm.Flags);

                                if (pbm.X < minX) minX = pbm.X;

                                if (pbm.X > maxX) maxX = pbm.X;

                                if (pbm.Y < minY) minY = pbm.Y;

                                if (pbm.Y > maxY) maxY = pbm.Y;

                                components[i] = component;
                            }

                            Multi = new Multi(this)
                                {MinX = minX, MaxX = maxX, MinY = minY, MaxY = maxY, Components = components};

                            House house = World.GetOrCreateHouse(Serial);
                            house.GenerateOriginal(Multi);
                            World.AddOrUpdateHouse(house);
                        }
                    }
                    else
                        Multi = null;
                }
            }
        }

        public Multi Multi { get; private set; }

        public bool IsCorpse => /*MathHelper.InRange(Graphic, 0x0ECA, 0x0ED2) ||*/ Graphic == 0x2006;

        public bool IsSpellBook => Graphic == 0x0E38 || Graphic == 0x0EFA || Graphic == 0x2252 ||
                                   Graphic == 0x2253 || Graphic == 0x238C || Graphic == 0x23A0 ||
                                   Graphic == 0x2D50;

        public override bool Exists => World.Contains(Serial);

        public bool OnGround => !Container.IsValid;

        public Serial RootContainer
        {
            get
            {
                Item item = this;
                while (item.Container.IsItem) item = World.Items.Get(item.Container);

                return item.Container.IsMobile ? item.Container : item;
            }
        }

        public SpellBookType BookType { get; private set; } = SpellBookType.Unknown;

        public override Graphic Graphic
        {
            get => base.Graphic;
            set
            {
                Name = ItemData.Name;
                base.Graphic = value;
            }
        }

        public StaticTiles ItemData => TileData.StaticData[IsMulti ? Graphic + 0x4000 : Graphic];

        public bool IsAtWorld(int x, int y) => Position.X == x && Position.Y == y;

        private void ItemsOnAddedAndDeleted(object sender, EventArgs e)
        {
            _invokeUpdate = true;
        }


        public event EventHandler OwnerChanged;

        protected override View CreateView() => new ItemView(this);

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (_invokeUpdate)
            {
                _OnUpdated?.Invoke(this);
                _invokeUpdate = false;
            }

            if (IsCorpse)
                ProcessAnimation();
            else if (Effect != null)
            {
                if (Effect.IsDisposed)
                    Effect = null;
                else
                    Effect.Update(totalMS, frameMS);
            }
        }

        protected override void OnProcessDelta(Delta d)
        {
            base.OnProcessDelta(d);
            if (d.HasFlag(Delta.Ownership))
            {
                OwnerChanged.Raise(this);
            }
        }


        public Graphic GetMountAnimation()
        {
            Graphic graphic = Graphic;

            if (Layer == Layer.Mount)
            {
                switch (graphic)
                {
                    case 0x3E90:
                    {
                        graphic = 0x0114;
                        break;
                    }
                    case 0x3E91:
                    {
                        graphic = 0x0115;
                        break;
                    }
                    case 0x3E92:
                    {
                        graphic = 0x011C;
                        break;
                    }
                    case 0x3E94:
                    {
                        graphic = 0x00F3;
                        break;
                    }
                    case 0x3E95:
                    {
                        graphic = 0x00A9;
                        break;
                    }
                    case 0x3E97:
                    {
                        graphic = 0x00C3;
                        break;
                    }
                    case 0x3E98:
                    {
                        graphic = 0x00C2;
                        break;
                    }
                    case 0x3E9A:
                    {
                        graphic = 0x00C1;
                        break;
                    }
                    case 0x3E9B:
                    case 0x3E9D:
                    {
                        graphic = 0x00C0;
                        break;
                    }
                    case 0x3E9C:
                    {
                        graphic = 0x00BF;
                        break;
                    }
                    case 0x3E9E:
                    {
                        graphic = 0x00BE;
                        break;
                    }
                    case 0x3EA0:
                    {
                        graphic = 0x00E2;
                        break;
                    }
                    case 0x3EA1:
                    {
                        graphic = 0x00E4;
                        break;
                    }
                    case 0x3EA2:
                    {
                        graphic = 0x00CC;
                        break;
                    }
                    case 0x3EA3:
                    {
                        graphic = 0x00D2;
                        break;
                    }
                    case 0x3EA4:
                    {
                        graphic = 0x00DA;
                        break;
                    }
                    case 0x3EA5:
                    {
                        graphic = 0x00DB;
                        break;
                    }
                    case 0x3EA6:
                    {
                        graphic = 0x00DC;
                        break;
                    }
                    case 0x3EA7:
                    {
                        graphic = 0x0074;
                        break;
                    }
                    case 0x3EA8:
                    {
                        graphic = 0x0075;
                        break;
                    }
                    case 0x3EA9:
                    {
                        graphic = 0x0072;
                        break;
                    }
                    case 0x3EAA:
                    {
                        graphic = 0x0073;
                        break;
                    }
                    case 0x3EAB:
                    {
                        graphic = 0x00AA;
                        break;
                    }
                    case 0x3EAC:
                    {
                        graphic = 0x00AB;
                        break;
                    }
                    case 0x3EAD:
                    {
                        graphic = 0x0084;
                        break;
                    }
                    case 0x3EAF:
                    {
                        graphic = 0x0078;
                        break;
                    }
                    case 0x3EB0:
                    {
                        graphic = 0x0079;
                        break;
                    }
                    case 0x3EB1:
                    {
                        graphic = 0x0077;
                        break;
                    }
                    case 0x3EB2:
                    {
                        graphic = 0x0076;
                        break;
                    }
                    case 0x3EB3:
                    {
                        graphic = 0x0090;
                        break;
                    }
                    case 0x3EB4:
                    {
                        graphic = 0x007A;
                        break;
                    }
                    case 0x3EB5:
                    {
                        graphic = 0x00B1;
                        break;
                    }
                    case 0x3EB6:
                    {
                        graphic = 0x00B2;
                        break;
                    }
                    case 0x3EB7:
                    {
                        graphic = 0x00B3;
                        break;
                    }
                    case 0x3EB8:
                    {
                        graphic = 0x00BC;
                        break;
                    }
                    case 0x3EBA:
                    {
                        graphic = 0x00BB;
                        break;
                    }
                    case 0x3EBB:
                    {
                        graphic = 0x0319;
                        break;
                    }
                    case 0x3EBC:
                    {
                        graphic = 0x0317;
                        break;
                    }
                    case 0x3EBD:
                    {
                        graphic = 0x031A;
                        break;
                    }
                    case 0x3EBE:
                    {
                        graphic = 0x031F;
                        break;
                    }
                    case 0x3EC3:
                    {
                        graphic = 0x02D4;
                        break;
                    }
                    case 0x3EC5:
                    case 0x3F3A:
                    {
                        graphic = 0x00D5;
                        break;
                    }
                    case 0x3EC6:
                    {
                        graphic = 0x01B0;
                        break;
                    }
                    case 0x3EC7:
                    {
                        graphic = 0x04E6;
                        break;
                    }
                    case 0x3EC8:
                    {
                        graphic = 0x04E7;
                        break;
                    }
                    case 0x3EC9:
                    {
                        graphic = 0x042D;
                        break;
                    }
                    default:
                    {
                        graphic = 0x00C8;

                        break;
                    }
                }

                if (ItemData.AnimID != 0) graphic = ItemData.AnimID;
            }
            else if (IsCorpse) return Amount;

            return graphic;
        }

        public bool HasSpell(int circle, int index)
        {
            index = (3 - circle % 4 + circle / 4 * 4) * 8 + (index - 1);
            ulong flag = (ulong) 1 << index;
            return (_spellsBitFiled & flag) == flag;
        }

        public void FillSpellbook(SpellBookType type, ulong field)
        {
            if (!IsSpellBook)
                return;

            bool needUpdate = false;

            if (BookType != type)
            {
                BookType = type;
                needUpdate = true;
            }

            if (_spellsBitFiled != field)
            {
                _spellsBitFiled = field;
                needUpdate = true;
            }

            if (needUpdate)
                _OnUpdated?.Invoke(this);
        }


        public override void Dispose()
        {
            if (IsMulti && Multi != null)
            {
                Array.Clear(Multi.Components, 0, 0);
                Multi = null;
            }

            Effect?.Dispose();
            Effect = null;

            Items.Added -= ItemsOnAddedAndDeleted;
            Items.Removed -= ItemsOnAddedAndDeleted;

            base.Dispose();
        }

        public override void ProcessAnimation()
        {
            if (IsCorpse)
            {
                byte dir = (byte) Layer;

                if (_lastAnimationChangeTime < CoreGame.Ticks)
                {
                    sbyte frameIndex = (sbyte) (AnimIndex + 1);

                    Graphic id = GetMountAnimation();

                    bool mirror = false;

                    Animations.GetAnimDirection(ref dir, ref mirror);

                    if (id < Animations.MAX_ANIMATIONS_DATA_INDEX_COUNT)
                    {
                        int animGroup = Animations.GetDieGroupIndex(id, UsedLayer);

                        ref AnimationDirection direction =
                            ref Animations.DataIndex[id].Groups[animGroup].Direction[dir];

                        Animations.AnimID = id;
                        Animations.AnimGroup = (byte) animGroup;
                        Animations.Direction = dir;

                        if (direction.FrameCount == 0) Animations.LoadDirectionGroup(ref direction);

                        if (direction.Address != 0 || direction.IsUOP)
                        {
                            direction.LastAccessTime = CoreGame.Ticks;
                            int fc = direction.FrameCount;

                            if (frameIndex >= fc) frameIndex = (sbyte) (fc - 1);

                            AnimIndex = frameIndex;
                        }
                    }

                    _lastAnimationChangeTime = CoreGame.Ticks + (int) CHARACTER_ANIMATION_DELAY;
                }
            }
        }
    }
}