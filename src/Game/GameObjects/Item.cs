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

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Platforms;

namespace ClassicUO.Game.GameObjects
{
    internal partial class Item : Entity
    {
        private AnimDataFrame2 _animDataFrame;
        private int _animSpeed;
        private Graphic? _displayedGraphic;
        private bool _isMulti;


        private StaticTiles? _itemData;

        private ulong _spellsBitFiled;


        private static readonly Queue<Item> _pool = new Queue<Item>();

        public Item(Serial serial) : base(serial)
        {
        }


        public static Item Create(Serial serial)
        {
            //if (_pool.Count != 0)
            //{
            //    var i = _pool.Dequeue();
            //    i.IsDestroyed = false;
            //    i.Graphic = 0;
            //    i.Serial = serial;
            //    i._amount = 0;
            //    i._animDataFrame = default;
            //    i._animSpeed = 0;
            //    i._container = 0;
            //    i._isMulti = false;
            //    i._layer = 0;
            //    i._price = 0;
            //    i._spellsBitFiled = 0;
            //    i.UsedLayer = false;
            //    i._originalGraphic = 0;
            //    i._displayedGraphic = null;

            //    i.LightID = 0;
            //    i.MultiDistanceBonus = 0;
            //    i.BookType = SpellBookType.Unknown;
            //    i.Flags = 0;
            //    i.WantUpdateMulti = true;
            //    i._force = false;
            //    i.MultiInfo = null;
            //    i.MultiGraphic = 0;
            //    i.CharacterIsBehindFoliage = false;

            //    i.AlphaHue = 0;
            //    i.Name = null;
            //    i.Direction = 0;
            //    i.Equipment = null;
            //    i.LastAnimationChangeTime = 0;
            //    i.Items = new EntityCollection<Item>();
            //    i.IsClicked = false;
            //    i.Properties.Clear();
            //    i._delta = 0;
            //    i.PropertiesHash = 0;

            //    i._itemData = null;
                

            //    return i;
            //}

            return new Item(serial);
        }

        public override void Destroy()
        {
            if (IsDestroyed)
                return;

            base.Destroy();
            //_pool.Enqueue(this);
        }

        public uint Price { get; set; }

        public ushort Amount { get; set; }

        public Serial Container { get; set; }

        public Layer Layer { get; set; }

        public bool UsedLayer { get; set; }

        public bool IsCoin => Graphic >= 0x0EEA && Graphic <= 0x0EF2;

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
                _isMulti = value;

                if (!value)
                {
                    MultiDistanceBonus = 0;
                    MultiInfo = null;
                }
            }
        }

        public bool IsDamageable { get; set; }

        public byte LightID { get; set; }

        public bool WantUpdateMulti { get; set; } = true;

        public MultiInfo MultiInfo { get; private set; }

        public int MultiDistanceBonus { get; private set; }

        public bool IsCorpse => /*MathHelper.InRange(Graphic, 0x0ECA, 0x0ED2) ||*/ Graphic == 0x2006;

        public bool IsSpellBook => Graphic == 0x0E38 || Graphic == 0x0EFA || Graphic == 0x2252 || Graphic == 0x2253 || Graphic == 0x238C || Graphic == 0x23A0 || Graphic == 0x2D50 || Graphic == 0x2D9D; // mysticism

        public bool OnGround => !Container.IsValid;

        public Serial RootContainer
        {
            get
            {
                Item item = this;

                while (item.Container.IsItem)
                {
                    item = World.Items.Get(item.Container);

                    if (item == null)
                        return Serial.INVALID;
                }

                return item.Container.IsMobile ? item.Container : item;
            }
        }

        public SpellBookType BookType { get; private set; } = SpellBookType.Unknown;


        public bool CharacterIsBehindFoliage { get; set; }

        public StaticTiles ItemData
        {
            [MethodImpl(256)]
            get
            {
                if (!_itemData.HasValue)
                    _itemData = FileManager.TileData.StaticData[IsMulti ? MultiGraphic : Graphic];

                return _itemData.Value;
            }
        }


        private void LoadMulti()
        {
            WantUpdateMulti = false;

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
                house.ClearComponents();

            for (int i = 0; i < count; i++)
            {
                FileManager.Multi.GetMultiData(i, Graphic, uopValid, out ushort graphic, out short x, out short y, out short z, out bool add);

                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;

                if (add)
                {
                    Multi m = Multi.Create(graphic);
                    m.Position = new Position((ushort) (X + x), (ushort) (Y + y), (sbyte) (Z + z));
                    m.MultiOffsetX = x;
                    m.MultiOffsetY = y;
                    m.MultiOffsetZ = z;
                    m.Hue = Hue;
                    m.AlphaHue = 255;

                    house.Components.Add(m);
                }
                else if (i == 0)
                {
                    MultiGraphic = graphic;
                    _itemData = null;
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

            Engine.UI.GetGump<MiniMapGump>()?.ForceUpdate();

            if (World.HouseManager.EntityIntoHouse(Serial, World.Player))
                Engine.SceneManager.GetScene<GameScene>()?.UpdateMaxDrawZ(true);
        }


        public void CheckGraphicChange(sbyte animIndex = 0)
        {
            _itemData = FileManager.TileData.StaticData[IsMulti ? Graphic + 0x4000 : Graphic];

            if (!IsMulti)
            {
                if (!IsCorpse)
                {
                    AllowedToDraw = !GameObjectHelper.IsNoDrawable(Graphic);

                    if (OnGround && ItemData.IsAnimated)
                    {
                        _animDataFrame = FileManager.AnimData.CalculateCurrentGraphic(Graphic);

                        AnimIndex = animIndex;
                        _animSpeed = _animDataFrame.FrameInterval != 0 ? _animDataFrame.FrameInterval * Constants.ITEM_EFFECT_ANIMATION_DELAY : Constants.ITEM_EFFECT_ANIMATION_DELAY;
                        LastAnimationChangeTime = Engine.Ticks;
                    }
                    else
                        _animDataFrame = default;

                    _originalGraphic = DisplayedGraphic;
                    _force = true;
                }
                else
                {
                    AnimIndex = 99;

                    if ((Direction & Direction.Running) != 0)
                    {
                        UsedLayer = true;
                        Direction &= (Direction) 0x7F;
                    }
                    else
                        UsedLayer = false;

                    Layer = (Layer) Direction;
                    AllowedToDraw = true;
                }
            }
            else if (WantUpdateMulti)
            {
                UoAssist.SignalAddMulti((ushort) (Graphic | 0x4000), Position);

                if (MultiDistanceBonus == 0 || World.HouseManager.IsHouseInRange(Serial, World.ClientViewRange))
                {
                    LoadMulti();
                    AllowedToDraw = MultiGraphic > 2;
                    _originalGraphic = MultiGraphic;
                    _force = true;
                }
            }
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (IsDestroyed)
                return;

            base.Update(totalMS, frameMS);

            ProcessAnimation(out _);
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

                    default:

                    {
                        if (ItemData.AnimID != 0)
                            graphic = ItemData.AnimID;
                        else
                            graphic = 0xFFFF;

                        break;
                    }
                }
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

        public override void UpdateTextCoordsV()
        {
            if (TextContainer == null)
                return;

            var last = TextContainer.Items;

            while (last?.ListRight != null)
                last = last.ListRight;

            if (last == null)
                return;

            int offY = 0;

            int startX = Engine.Profile.Current.GameWindowPosition.X + 6;
            int startY = Engine.Profile.Current.GameWindowPosition.Y + 6;

            int x = RealScreenPosition.X;
            int y = RealScreenPosition.Y;


            if (OnGround)
            {
                var scene = Engine.SceneManager.GetScene<GameScene>();
                float scale = scene?.Scale ?? 1;

                if (Texture != null)
                    y -= Texture is ArtTexture t ? (t.ImageRectangle.Height >> 1) : (Texture.Height >> 1);
                x += 22;
                for (; last != null; last = last.ListLeft)
                {
                    if (last.RenderedText != null && !last.RenderedText.IsDestroyed)
                    {
                        if (offY == 0 && last.Time < Engine.Ticks)
                            continue;


                        last.OffsetY = offY;
                        offY += last.RenderedText.Height;

                        last.RealScreenPosition.X = startX + (int)((x - (last.RenderedText.Width >> 1)) / scale);
                        last.RealScreenPosition.Y = startY + (int)((y - offY) / scale);
                    }
                }

                FixTextCoordinatesInScreen();
            }
            else
            {
                for (; last != null; last = last.ListLeft)
                {
                    if (last.RenderedText != null && !last.RenderedText.IsDestroyed)
                    {
                        if (offY == 0 && last.Time < Engine.Ticks)
                            continue;

                        x = last.X - startX;
                        y = last.Y - startY;

                        last.OffsetY = offY;
                        offY += last.RenderedText.Height;

                        last.RealScreenPosition.X = startX + ((x - (last.RenderedText.Width >> 1)));
                        last.RealScreenPosition.Y = startY + ((y - offY));
                    }
                }
            }
        }


        public override void ProcessAnimation(out byte dir, bool evalutate = false)
        {
            dir = 0;

            if (IsCorpse)
            {
                dir = (byte) Layer;

                if (LastAnimationChangeTime < Engine.Ticks)
                {
                    sbyte frameIndex = (sbyte) (AnimIndex + 1);
                    ushort id = GetGraphicForAnimation();

                    //FileManager.Animations.GetCorpseAnimationGroup(ref graphic, ref animGroup, ref newHue);

                    //ushort corpseGraphic = FileManager.Animations.DataIndex[id].CorpseGraphic;

                    //if (corpseGraphic != id && corpseGraphic != 0) 
                    //    id = corpseGraphic;

                    bool mirror = false;
                    FileManager.Animations.GetAnimDirection(ref dir, ref mirror);

                    if (id < Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT && dir < 5)
                    {
                        byte animGroup = FileManager.Animations.GetDieGroupIndex(id, UsedLayer);

                        ushort hue = 0;
                        ref var direction = ref FileManager.Animations.GetCorpseAnimationGroup(ref id, ref animGroup, ref hue).Direction[dir];
                        FileManager.Animations.AnimID = id;
                        FileManager.Animations.AnimGroup = animGroup;
                        FileManager.Animations.Direction = dir;

                        if (direction.FrameCount == 0 || direction.Frames == null)
                            FileManager.Animations.LoadDirectionGroup(ref direction);

                        if (direction.Address != 0 && direction.Size != 0 || direction.IsUOP)
                        {
                            direction.LastAccessTime = Engine.Ticks;
                            int fc = direction.FrameCount;

                            if (frameIndex >= fc)
                                frameIndex = (sbyte) (fc - 1);
                            AnimIndex = frameIndex;
                        }
                    }

                    LastAnimationChangeTime = Engine.Ticks + Constants.CHARACTER_ANIMATION_DELAY;
                }
            }
            else if (OnGround && _animDataFrame.FrameCount != 0 && LastAnimationChangeTime < Engine.Ticks)
            {

                unsafe
                {
                    _originalGraphic = (Graphic) (DisplayedGraphic + _animDataFrame.FrameData[AnimIndex++]);
                }

                if (AnimIndex >= _animDataFrame.FrameCount)
                    AnimIndex = 0;

                _force = _originalGraphic == DisplayedGraphic;

                LastAnimationChangeTime = Engine.Ticks + _animSpeed;
            }
        }
    }
}