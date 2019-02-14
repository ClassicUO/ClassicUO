﻿#region license
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
using System;
using System.Runtime.CompilerServices;

using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Interfaces;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.GameObjects
{
    internal partial class Item : Entity
    {
        private ushort _amount;
        private Serial _container;
        private Graphic? _displayedGraphic;
        private bool _isMulti;
        private Layer _layer;
        private uint _price;
        private ulong _spellsBitFiled;

        public Item(Serial serial) : base(serial)
        {
        }

        public Item FindItem(ushort graphic, ushort hue = 0xFFFF)
        {
            Item item = null;

            if (hue == 0xFFFF)
            {
                var minColor = 0xFFFF;
                foreach (Item i in Items)
                {
                    if (i.Graphic == graphic)
                    {
                        if (i.Hue < minColor)
                        {
                            item = i;
                            minColor = i.Hue;
                        }
                    }
                    if (i.Container.IsValid)
                    {
                        Item found = i.FindItem(graphic, hue);
                        if (found != null && found.Hue < minColor)
                        {
                            item = found;
                            minColor = found.Hue;
                        }
                    }
                }
            }
            else
            {
                foreach (Item i in Items)
                {
                    if (i.Graphic == graphic && i.Hue == hue)
                        item = i;

                    if (i.Container.IsValid)
                    {
                        Item found = i.FindItem(graphic, hue);
                        if (found != null)
                            item = found;
                    }
                }
            }

            return item;
        }

        public uint Price
        {
            get => _price;
            set
            {
                if (_price != value)
                {
                    _price = value;
                    _delta |= Delta.Attributes;
                }
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

        public bool IsPickable => ItemData.Weight < 255;

        public Graphic DisplayedGraphic
        {
            get
            {
                if (_displayedGraphic.HasValue)
                    return _displayedGraphic.Value;

                if (IsCoin)
                {
                    if (Amount > 5) return (Graphic) (Graphic + 2);
                    if (Amount > 1) return (Graphic) (Graphic + 1);
                }
                else if (IsMulti)
                    return MultiGraphic;

                return Graphic;
            }
            set => _displayedGraphic = value;
        }

        public bool IsLocked => (Flags & Flags.Movable) == 0 && ItemData.Weight > 90;

        public Graphic MultiGraphic { get; private set; }

        public bool IsMulti
        {
            get => _isMulti;
            set
            {
                //if (_isMulti != value)
                {
                    _isMulti = value;


                    if (value)
                    {
                        //if (MultiDistanceBonus == 0 || MultiInfo == null)
                        {
                            short minX = 0;
                            short minY = 0;
                            short maxX = 0;
                            short maxY = 0;

                            int count = FileManager.Multi.GetCount(Graphic, out bool uopValid);

                            if (!World.HouseManager.TryGetHouse(Serial, out House house))
                            {
                                house = new House(Serial, 0, false);
                                World.HouseManager.Add(Serial, house);
                            }
                            else
                            {
                                house.ClearComponents();
                            }

                            for (int i = 0; i < count; i++)
                            {
                                FileManager.Multi.GetMultiData(i, Graphic, uopValid, out ushort graphic, out short x, out short y, out short z, out bool add);

                                if (x < minX) minX = x;
                                if (x > maxX) maxX = x;
                                if (y < minY) minY = y;
                                if (y > maxY) maxY = y;

                                if (add)
                                {
                                    house.Components.Add(new Multi(graphic)
                                    {
                                        Position = new Position((ushort) (X + x), (ushort) (Y + y), (sbyte) (Z + z)),
                                        MultiOffset = new Position((ushort)x, (ushort)y , (sbyte) z),
                                        AlphaHue = 0xFF
                                    });
                                }
                                else if (i == 0)
                                {
                                    MultiGraphic = graphic;
                                }
                            }

                            FileManager.Multi.ReleaseLastMultiDataRead();

                            MultiInfo = new MultiInfo((short) X, (short) Y)
                            {
                                MinX = minX,
                                MaxX = maxX,
                                MinY = minY,
                                MaxY = maxY
                            };

                            MultiDistanceBonus = Math.Max(Math.Max(Math.Abs(minX), maxX), Math.Max(Math.Abs(minY), maxY));

                            house.Generate();

                            Engine.UI.GetByLocalSerial<MiniMapGump>()?.ForceUpdate();
                        }
                    }
                    else
                    {
                        MultiDistanceBonus = 0;
                        MultiInfo = null;
                    }
                }

                AllowedToDraw = MultiGraphic != 0;
            }
        }

        public MultiInfo MultiInfo { get; private set; }

        public int MultiDistanceBonus { get; private set; }

        public bool IsCorpse => /*MathHelper.InRange(Graphic, 0x0ECA, 0x0ED2) ||*/ Graphic == 0x2006;

        public bool IsSpellBook => Graphic == 0x0E38 || Graphic == 0x0EFA || Graphic == 0x2252 || Graphic == 0x2253 || Graphic == 0x238C || Graphic == 0x23A0 || Graphic == 0x2D50 || Graphic == 0x2D9D; // mysticism

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

        public SpellBookType BookType { get; private set; } = SpellBookType.Unknown;


        public override Graphic Graphic
        {
            get => base.Graphic;
            set
            {
                if (base.Graphic != value)
                {
                    base.Graphic = value;
                    _itemData = FileManager.TileData.StaticData[value];
                    //Name = ItemData.Name;

                    CheckGraphicChange();
                }
            }
        }

        private void CheckGraphicChange()
        {
            if (!IsCorpse)
            {
                if (IsMulti)
                    AllowedToDraw = MultiGraphic != 0;
                else
                    AllowedToDraw = Graphic >= 2 && DisplayedGraphic >= 2 && !GameObjectHelper.IsNoDrawable(Graphic);
            }
            else
            {
                if ((Direction & Direction.Running) != 0)
                {
                    UsedLayer = true;
                    Direction &= (Direction)0x7F;
                }
                else
                    UsedLayer = false;

                Layer = (Layer)Direction;
                AllowedToDraw = true;
            }
        }

        private StaticTiles? _itemData;

        public StaticTiles ItemData
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (!_itemData.HasValue)
                    _itemData = FileManager.TileData.StaticData[IsMulti ? Graphic + 0x4000 : Graphic];
                return _itemData.Value;
            }
        }

        public event EventHandler OwnerChanged;

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (IsCorpse)
                ProcessAnimation();
        }

        protected override void OnProcessDelta(Delta d)
        {
            base.OnProcessDelta(d);
            if (d.HasFlag(Delta.Ownership)) OwnerChanged.Raise(this);
        }

        public override Graphic GetGraphicForAnimation()
        {
            Graphic graphic = Graphic;

            if (Layer == Layer.Mount)
            {
                switch (graphic)
                {
                    case 0x3E90: // 16016 Reptalon
                        {
                            graphic = 0x0114;
                            break;
                        }
                    case 0x3E91: // 16017
                        {
                            graphic = 0x0115;
                            break;
                        }
                    case 0x3E92: // 16018
                        {
                            graphic = 0x011C;
                            break;
                        }
                    case 0x3E94: // 16020
                        {
                            graphic = 0x00F3;
                            break;
                        }
                    case 0x3E95: // 16021
                        {
                            graphic = 0x00A9;
                            break;
                        }
                    case 0x3E97: // 16023 Ethereal Giant Beetle
                        {
                            graphic = 0x00C3;
                            break;
                        }
                    case 0x3E98: // 16024 Ethereal Swamp Dragon
                        {
                            graphic = 0x00C2;
                            break;
                        }
                    case 0x3E9A: // 16026 Ethereal Ridgeback
                        {
                            graphic = 0x00C1;
                            break;
                        }
                    case 0x3E9B: // 16027
                    case 0x3E9D: // 16029 Ethereal Unicorn
                        {
                            graphic = 0x00C0;
                            break;
                        }
                    case 0x3E9C: // 16028 Ethereal Kirin
                        {
                            graphic = 0x00BF;
                            break;
                        }
                    case 0x3E9E: // 16030
                        {
                            graphic = 0x00BE;
                            break;
                        }
                    case 0x3EA0: // 16032 light grey/horse3
                        {
                            graphic = 0x00E2;
                            break;
                        }
                    case 0x3EA1: // 16033 greybrown/horse4
                        {
                            graphic = 0x00E4;
                            break;
                        }
                    case 0x3EA2: // 16034 dark brown/horse
                        {
                            graphic = 0x00CC;
                            break;
                        }
                    case 0x3EA3: // 16035 desert ostard
                        {
                            graphic = 0x00D2;
                            break;
                        }
                    case 0x3EA4: // 16036 frenzied ostard (=zostrich)
                        {
                            graphic = 0x00DA;
                            break;
                        }
                    case 0x3EA5: // 16037 forest ostard
                        {
                            graphic = 0x00DB;
                            break;
                        }
                    case 0x3EA6: // 16038 Llama
                        {
                            graphic = 0x00DC;
                            break;
                        }
                    case 0x3EA7: // 16039 Nightmare / Vortex
                        {
                            graphic = 0x0074;
                            break;
                        }
                    case 0x3EA8: // 16040 Silver Steed
                        {
                            graphic = 0x0075;
                            break;
                        }
                    case 0x3EA9: // 16041 Nightmare
                        {
                            graphic = 0x0072;
                            break;
                        }
                    case 0x3EAA: // 16042 Ethereal Horse
                        {
                            graphic = 0x0073;
                            break;
                        }
                    case 0x3EAB: // 16043 Ethereal Llama
                        {
                            graphic = 0x00AA;
                            break;
                        }
                    case 0x3EAC: // 16044 Ethereal Ostard
                        {
                            graphic = 0x00AB;
                            break;
                        }
                    case 0x3EAD: // 16045 Kirin
                        {
                            graphic = 0x0084;
                            break;
                        }
                    case 0x3EAF: // 16047 War Horse (Blood Red)
                        {
                            graphic = 0x0078;
                            break;
                        }
                    case 0x3EB0: // 16048 War Horse (Light Green)
                        {
                            graphic = 0x0079;
                            break;
                        }
                    case 0x3EB1: // 16049 War Horse (Light Blue)
                        {
                            graphic = 0x0077;
                            break;
                        }
                    case 0x3EB2: // 16050 War Horse (Purple)
                        {
                            graphic = 0x0076;
                            break;
                        }
                    case 0x3EB3: // 16051 Sea Horse (Medium Blue)
                        {
                            graphic = 0x0090;
                            break;
                        }
                    case 0x3EB4: // 16052 Unicorn
                        {
                            graphic = 0x007A;
                            break;
                        }
                    case 0x3EB5: // 16053 Nightmare
                        {
                            graphic = 0x00B1;
                            break;
                        }
                    case 0x3EB6: // 16054 Nightmare 4
                        {
                            graphic = 0x00B2;
                            break;
                        }
                    case 0x3EB7: // 16055 Dark Steed
                        {
                            graphic = 0x00B3;
                            break;
                        }
                    case 0x3EB8: // 16056 Ridgeback
                        {
                            graphic = 0x00BC;
                            break;
                        }
                    case 0x3EBA: // 16058 Ridgeback, Savage
                        {
                            graphic = 0x00BB;
                            break;
                        }
                    case 0x3EBB: // 16059 Skeletal Mount
                        {
                            graphic = 0x0319;
                            break;
                        }
                    case 0x3EBC: // 16060 Beetle
                        {
                            graphic = 0x0317;
                            break;
                        }
                    case 0x3EBD: // 16061 SwampDragon
                        {
                            graphic = 0x031A;
                            break;
                        }
                    case 0x3EBE: // 16062 Armored Swamp Dragon
                        {
                            graphic = 0x031F;
                            break;
                        }
                    case 0x3EC3: //16067 Beetle
                        {
                            graphic = 0x02D4;
                            break;
                        }
                    case 0x3EC5: // 16069
                    case 0x3F3A: // 16186 snow bear ???
                        {
                            graphic = 0x00D5;
                            break;
                        }
                    case 0x3EC6: // 16070 Boura
                        {
                            graphic = 0x01B0;
                            break;
                        }
                    case 0x3EC7: // 16071 Tiger
                        {
                            graphic = 0x04E6;
                            break;
                        }
                    case 0x3EC8: // 16072 Tiger
                        {
                            graphic = 0x04E7;
                            break;
                        }
                    case 0x3EC9: // 16073
                        {
                            graphic = 0x042D;
                            break;
                        }
                    default: //lightbrown/horse2
                        {
                            graphic = 0x00C8;

                            break;
                        }
                }

                /* TODO: maybe this code is wrong. The animID in this case doesn't work like expected.
                
                if (ItemData.AnimID != 0)
                    graphic = ItemData.AnimID;
                */
            }
            else if (IsCorpse)
                return Amount;

            return graphic;
        }

        public bool HasSpell(int circle, int index)
        {
            index = (3 - circle % 4 + (circle >> 2) * 4) * 8 + (index - 1);
            ulong flag = (ulong) 1 << index;

            return (_spellsBitFiled & flag) == flag;
        }

        public bool FillSpellbook(SpellBookType type, ulong field)
        {
            if (!IsSpellBook)
                return false;
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
            return needUpdate;
        }


        public override void ProcessAnimation()
        {
            if (IsCorpse)
            {
                byte dir = (byte) Layer;

                if (LastAnimationChangeTime < Engine.Ticks)
                {
                    sbyte frameIndex = (sbyte) (AnimIndex + 1);
                    Graphic id = GetGraphicForAnimation();
                    bool mirror = false;
                    FileManager.Animations.GetAnimDirection(ref dir, ref mirror);

                    if (id < Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT && dir < 5)
                    {
                        int animGroup = FileManager.Animations.GetDieGroupIndex(id, UsedLayer);
                        ref AnimationDirection direction = ref FileManager.Animations.DataIndex[id].Groups[animGroup].Direction[dir];
                        FileManager.Animations.AnimID = id;
                        FileManager.Animations.AnimGroup = (byte) animGroup;
                        FileManager.Animations.Direction = dir;
                        if ((direction.FrameCount == 0 || direction.FramesHashes == null)) FileManager.Animations.LoadDirectionGroup(ref direction);

                        if (direction.Address != 0 || direction.IsUOP)
                        {
                            direction.LastAccessTime = Engine.Ticks;
                            int fc = direction.FrameCount;
                            if (frameIndex >= fc) frameIndex = (sbyte) (fc - 1);
                            AnimIndex = frameIndex;
                        }
                    }

                    LastAnimationChangeTime = Engine.Ticks + Constants.CHARACTER_ANIMATION_DELAY;
                }
            }
        }
    }
}