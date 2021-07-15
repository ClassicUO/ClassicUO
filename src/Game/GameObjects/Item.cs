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

using System;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using ClassicUO.Utility.Platforms;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    internal partial class Item : Entity
    {
        private static readonly QueuedPool<Item> _pool = new QueuedPool<Item>
        (
            Constants.PREDICTABLE_CHUNKS * 3,
            i =>
            {
                i.IsDestroyed = false;
                i.Graphic = 0;
                i.Amount = 0;
                i.Container = 0xFFFF_FFFF;
                i._isMulti = false;
                i.Layer = 0;
                i.Price = 0;
                i.UsedLayer = false;
                i._displayedGraphic = null;
                i.X = 0;
                i.Y = 0;
                i.Z = 0;

                i.LightID = 0;
                i.MultiDistanceBonus = 0;
                i.Flags = 0;
                i.WantUpdateMulti = true;
                i.MultiInfo = null;
                i.MultiGraphic = 0;

                i.AlphaHue = 0;
                i.Name = null;
                i.Direction = 0;
                i.AnimIndex = 0;
                i.Hits = 0;
                i.HitsMax = 0;
                i.LastStepTime = 0;
                i.LastAnimationChangeTime = 0;

                i.Clear();

                i.IsClicked = false;
                i.IsDamageable = false;
                i.Offset = Vector3.Zero;

                i.Opened = false;
                i.TextContainer?.Clear();
                i.IsFlipped = false;
                i.FrameInfo = Rectangle.Empty;
                i.UseObjectHandles = false;
                i.ClosedObjectHandles = false;
                i.ObjectHandlesOpened = false;
                i.AlphaHue = 0;
                i.AllowedToDraw = true;

                i.HitsRequest = HitsRequestStatus.None;
            }
        );

        private ushort? _displayedGraphic;
        private bool _isMulti;


        public Item() : base(0)
        {
        }

        public bool IsCoin => Graphic == 0x0EEA || Graphic == 0x0EED || Graphic == 0x0EF0;

        public ushort DisplayedGraphic
        {
            get
            {
                if (_displayedGraphic.HasValue)
                {
                    return _displayedGraphic.Value;
                }

                if (IsCoin)
                {
                    if (Amount > 5)
                    {
                        return (ushort) (Graphic + 2);
                    }

                    if (Amount > 1)
                    {
                        return (ushort) (Graphic + 1);
                    }
                }
                else if (IsMulti)
                {
                    return MultiGraphic;
                }

                return Graphic;
            }
            set => _displayedGraphic = value;
        }

        public bool IsLocked => (Flags & Flags.Movable) == 0 && ItemData.Weight > 90;

        public ushort MultiGraphic { get; private set; }

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

        public int MultiDistanceBonus { get; private set; }

        public bool IsCorpse => /*MathHelper.InRange(Graphic, 0x0ECA, 0x0ED2) ||*/ Graphic == 0x2006;

        public bool OnGround => !SerialHelper.IsValid(Container);

        public uint RootContainer
        {
            get
            {
                Item item = this;

                while (SerialHelper.IsItem(item.Container))
                {
                    item = World.Items.Get(item.Container);

                    if (item == null)
                    {
                        return 0;
                    }
                }

                return SerialHelper.IsMobile(item.Container) ? item.Container : item;
            }
        }

        public ref StaticTiles ItemData => ref TileDataLoader.Instance.StaticData[IsMulti ? MultiGraphic : Graphic];

        public bool IsLootable =>
            ItemData.Layer != (int) Layer.Hair && ItemData.Layer != (int) Layer.Beard && ItemData.Layer != (int) Layer.Face && Graphic != 0;

        public ushort Amount;
        public uint Container = 0xFFFF_FFFF;

        public bool IsDamageable;
        public Layer Layer;
        public byte LightID;

        public Rectangle? MultiInfo;
        public bool Opened;

        public uint Price;
        public bool UsedLayer;
        public bool WantUpdateMulti = true;


        public static Item Create(uint serial)
        {
            Item i = _pool.GetOne();
            i.Serial = serial;

            return i;
        }

        public override void Destroy()
        {
            if (IsDestroyed)
            {
                return;
            }

            if (Opened)
            {
                UIManager.GetGump<ContainerGump>(Serial)?.Dispose();

                UIManager.GetGump<SpellbookGump>(Serial)?.Dispose();

                UIManager.GetGump<MapGump>(Serial)?.Dispose();

                if (IsCorpse)
                {
                    UIManager.GetGump<GridLootGump>(Serial)?.Dispose();
                }

                UIManager.GetGump<BulletinBoardGump>(Serial)?.Dispose();

                UIManager.GetGump<SplitMenuGump>(Serial)?.Dispose();

                Opened = false;
            }

            base.Destroy();

            _pool.ReturnOne(this);
        }

        private unsafe void LoadMulti()
        {
            WantUpdateMulti = false;

            short minX = 0;
            short minY = 0;
            short maxX = 0;
            short maxY = 0;


            if (!World.HouseManager.TryGetHouse(Serial, out House house))
            {
                house = new House(Serial, 0, false);
                World.HouseManager.Add(Serial, house);
            }
            else
            {
                house.ClearComponents();
            }


            ref UOFileIndex entry = ref MultiLoader.Instance.GetValidRefEntry(Graphic);
            MultiLoader.Instance.File.SetData(entry.Address, entry.FileSize);
            bool movable = false;

            if (MultiLoader.Instance.IsUOP)
            {
                if (entry.Length > 0 && entry.DecompressedLength > 0)
                {
                    MultiLoader.Instance.File.Seek(entry.Offset);

                    byte[] buffer = null;
                    Span<byte> span = entry.DecompressedLength <= 1024 ? stackalloc byte[entry.DecompressedLength] : (buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(entry.DecompressedLength));

                    try
                    {
                        fixed (byte* dataPtr = span)
                        {
                            ZLib.Decompress
                            (
                                MultiLoader.Instance.File.PositionAddress,
                                entry.Length,
                                0,
                                (IntPtr)dataPtr,
                                entry.DecompressedLength
                            );

                            StackDataReader reader = new StackDataReader(span.Slice(0, entry.DecompressedLength));
                            reader.Skip(4);

                            int count = reader.ReadInt32LE();

                            int sizeOf = sizeof(MultiBlockNew);

                            for (int i = 0; i < count; i++)
                            {
                                MultiBlockNew* block = (MultiBlockNew*)(reader.PositionAddress + i * sizeOf);

                                if (block->Unknown != 0)
                                {
                                    reader.Skip((int)(block->Unknown * 4));
                                }

                                if (block->X < minX)
                                {
                                    minX = block->X;
                                }

                                if (block->X > maxX)
                                {
                                    maxX = block->X;
                                }

                                if (block->Y < minY)
                                {
                                    minY = block->Y;
                                }

                                if (block->Y > maxY)
                                {
                                    maxY = block->Y;
                                }

                                if (block->Flags == 0 || block->Flags == 0x100)
                                {
                                    Multi m = Multi.Create(block->ID);
                                    m.X = (ushort)(X + block->X);
                                    m.Y = (ushort)(Y + block->Y);
                                    m.Z = (sbyte)(Z + block->Z);
                                    m.UpdateScreenPosition();
                                    m.MultiOffsetX = block->X;
                                    m.MultiOffsetY = block->Y;
                                    m.MultiOffsetZ = block->Z;
                                    m.Hue = Hue;
                                    m.AlphaHue = 255;
                                    m.IsCustom = false;
                                    m.State = CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_DONT_REMOVE;
                                    m.IsMovable = ItemData.IsMultiMovable;
                                    m.AddToTile();
                                    house.Components.Add(m);

                                    if (m.ItemData.IsMultiMovable)
                                    {
                                        movable = true;
                                    }
                                }
                                else if (i == 0)
                                {
                                    MultiGraphic = block->ID;
                                }
                            }

                            reader.Release();
                        }
                    }
                    finally
                    {
                        if (buffer != null)
                        {
                            System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
                        }
                    }
                }
                else
                {
                    Log.Warn($"[MultiCollection.uop] invalid entry (0x{Graphic:X4})");
                }
            }
            else
            {
                int count = entry.Length / MultiLoader.Instance.Offset;
                MultiLoader.Instance.File.Seek(entry.Offset);

                for (int i = 0; i < count; i++)
                {
                    MultiBlock* block = (MultiBlock*) (MultiLoader.Instance.File.PositionAddress + i * MultiLoader.Instance.Offset);

                    if (block->X < minX)
                    {
                        minX = block->X;
                    }

                    if (block->X > maxX)
                    {
                        maxX = block->X;
                    }

                    if (block->Y < minY)
                    {
                        minY = block->Y;
                    }

                    if (block->Y > maxY)
                    {
                        maxY = block->Y;
                    }

                    if (block->Flags != 0)
                    {
                        Multi m = Multi.Create(block->ID);
                        m.X = (ushort) (X + block->X);
                        m.Y = (ushort) (Y + block->Y);
                        m.Z = (sbyte) (Z + block->Z);
                        m.UpdateScreenPosition();
                        m.MultiOffsetX = block->X;
                        m.MultiOffsetY = block->Y;
                        m.MultiOffsetZ = block->Z;
                        m.Hue = Hue;
                        m.AlphaHue = 255;
                        m.IsCustom = false;
                        m.State = CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_DONT_REMOVE;
                        m.IsMovable = ItemData.IsMultiMovable;
                        m.AddToTile();
                        house.Components.Add(m);

                        if (m.ItemData.IsMultiMovable)
                        {
                            movable = true;
                        }
                    }
                    else if (i == 0)
                    {
                        MultiGraphic = block->ID;
                    }
                }
            }

            MultiInfo = new Rectangle
            {
                X = minX,
                Y = minY,
                Width = maxX,
                Height = maxY
            };

            // hack to make baots movable.
            // Mast is not the main center in bigger boats, so if we got a movable multi --> makes all multi movable
            if (movable)
            {
                foreach (Multi m in house.Components)
                {
                    m.IsMovable = movable;
                }
            }

            MultiDistanceBonus = Math.Max(Math.Max(Math.Abs(minX), maxX), Math.Max(Math.Abs(minY), maxY));

            house.Bounds = MultiInfo.Value;

            UIManager.GetGump<MiniMapGump>()?.RequestUpdateContents();

            if (World.HouseManager.EntityIntoHouse(Serial, World.Player))
            {
                Client.Game.GetScene<GameScene>()?.UpdateMaxDrawZ(true);
            }

            BoatMovingManager.ClearSteps(Serial);
        }

        public override void CheckGraphicChange(sbyte animIndex = 0)
        {
            if (!IsMulti)
            {
                if (!IsCorpse)
                {
                    AllowedToDraw = !GameObjectHelper.IsNoDrawable(Graphic);
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
                    {
                        UsedLayer = false;
                    }

                    Layer = (Layer) Direction;
                    AllowedToDraw = true;
                }
            }
            else if (WantUpdateMulti)
            {
                UoAssist.SignalAddMulti((ushort) (Graphic | 0x4000), X, Y);

                if (MultiDistanceBonus == 0 || World.HouseManager.IsHouseInRange(Serial, World.ClientViewRange))
                {
                    LoadMulti();
                    AllowedToDraw = MultiGraphic > 2;
                }
            }
        }

        public override void Update(double totalTime, double frameTime)
        {
            if (IsDestroyed)
            {
                return;
            }

            base.Update(totalTime, frameTime);

            ProcessAnimation(out _);
        }

        public override ushort GetGraphicForAnimation()
        {
            ushort graphic = Graphic;

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
                        return 0x00C0;
                    }

                    case 0x3E9C: // 16028 Ethereal Kirin

                    {
                        graphic = 0x00BF;

                        return graphic;
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

                    case 0x3ECE: // serpentine dragon
                    {
                        graphic = 0x059A;

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

                    case 0x3ECA: // tarantula

                    {
                        graphic = 0x0579;

                        break;
                    }

                    case 0x3ECC:
                    {
                        graphic = 0x0582;

                        break;
                    }

                    case 0x3ED1: // CoconutCrab
                    {
                        graphic = 0x05E6;

                        break;
                    }

                    case 0x3ECB: // Lasher
                    {
                        graphic = 0x057F;

                        break;
                    }

                    case 0x3ED0: //SkeletalCat
                    {
                        graphic = 0x05A1;

                        break;
                    }

                    case 0x3ED2: // war boar
                    {
                        graphic = 0x05F6;

                        break;
                    }

                    case 0x3ECD: //Palomino
                    {
                        graphic = 0x0580;

                        break;
                    }

                    case 0x3ECF: //Eowmu
                    {
                        graphic = 0x05A0;

                        break;
                    }

                    case 0x3ED3: // capybara
                    {
                        graphic = 0x05F7;

                        break;
                    }
                }

                if (ItemData.AnimID != 0)
                {
                    graphic = ItemData.AnimID;
                }

                //else
                //    graphic = 0xFFFF;
            }
            else if (IsCorpse)
            {
                return Amount;
            }

            return graphic;
        }

        public override void UpdateTextCoordsV()
        {
            if (TextContainer == null)
            {
                return;
            }

            TextObject last = (TextObject) TextContainer.Items;

            while (last?.Next != null)
            {
                last = (TextObject) last.Next;
            }

            if (last == null)
            {
                return;
            }

            int offY = 0;

            if (OnGround)
            {
                Point p = RealScreenPosition;

                ArtTexture texture = ArtLoader.Instance.GetTexture(Graphic);

                if (texture != null)
                {
                    p.Y -= texture.ImageRectangle.Height >> 1;
                }

                p.X += (int) Offset.X + 22;
                p.Y += (int) (Offset.Y - Offset.Z) + 22;

                p = Client.Game.Scene.Camera.WorldToScreen(p);

                for (; last != null; last = (TextObject) last.Previous)
                {
                    if (last.RenderedText != null && !last.RenderedText.IsDestroyed)
                    {
                        if (offY == 0 && last.Time < Time.Ticks)
                        {
                            continue;
                        }

                        last.OffsetY = offY;
                        offY += last.RenderedText.Height;

                        last.RealScreenPosition.X = p.X - (last.RenderedText.Width >> 1);
                        last.RealScreenPosition.Y = p.Y - offY;
                    }
                }

                FixTextCoordinatesInScreen();
            }
            else
            {
                for (; last != null; last = (TextObject) last.Previous)
                {
                    if (last.RenderedText != null && !last.RenderedText.IsDestroyed)
                    {
                        if (offY == 0 && last.Time < Time.Ticks)
                        {
                            continue;
                        }

                        last.OffsetY = offY;
                        offY += last.RenderedText.Height;

                        last.RealScreenPosition.X = last.X - (last.RenderedText.Width >> 1);
                        last.RealScreenPosition.Y = last.Y - offY;
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

                if (LastAnimationChangeTime < Time.Ticks)
                {
                    sbyte frameIndex = (sbyte) (AnimIndex + 1);
                    ushort id = GetGraphicForAnimation();

                    //FileManager.Animations.GetCorpseAnimationGroup(ref graphic, ref animGroup, ref newHue);

                    //ushort corpseGraphic = FileManager.Animations.DataIndex[id].CorpseGraphic;

                    //if (corpseGraphic != id && corpseGraphic != 0) 
                    //    id = corpseGraphic;

                    bool mirror = false;
                    AnimationsLoader.Instance.GetAnimDirection(ref dir, ref mirror);

                    if (id < Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT && dir < 5)
                    {
                        byte animGroup = AnimationsLoader.Instance.GetDieGroupIndex(id, UsedLayer);

                        ushort hue = 0;

                        AnimationDirection direction = AnimationsLoader.Instance.GetCorpseAnimationGroup(ref id, ref animGroup, ref hue).Direction[dir];

                        if (direction.FrameCount == 0 || direction.Frames == null)
                        {
                            AnimationsLoader.Instance.LoadAnimationFrames(id, animGroup, dir, ref direction);
                        }

                        if (direction.Address != 0 && direction.Size != 0 || direction.IsUOP)
                        {
                            direction.LastAccessTime = Time.Ticks;
                            int fc = direction.FrameCount;

                            if (frameIndex >= fc)
                            {
                                frameIndex = (sbyte) (fc - 1);
                            }

                            AnimIndex = frameIndex;
                        }
                    }

                    LastAnimationChangeTime = Time.Ticks + Constants.CHARACTER_ANIMATION_DELAY;
                }
            }
            /*else if (evalutate && SerialHelper.IsMobile(Container) && Layer != Layer.Invalid)
            {
                ushort id = ItemData.AnimID;

                if (id == 0)
                {
                    return;
                }

                Mobile parent = World.Mobiles.Get(Container);

                if (parent != null)
                {
                    byte animGroup = Mobile.GetGroupForAnimation(parent, id, true);

                    bool mirror = false;
                    AnimationsLoader.Instance.GetAnimDirection(ref dir, ref mirror);
                    int currentDelay = Constants.CHARACTER_ANIMATION_DELAY;

                    if (id < Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT && dir < 5)
                    {
                        ushort hue = 0;

                        sbyte frameIndex = AnimIndex;

                        if (parent.AnimationFromServer && !parent.AnimationForwardDirection)
                        {
                            frameIndex--;
                        }
                        else
                        {
                            frameIndex++;
                        }

                        AnimationDirection direction = AnimationsLoader.Instance.GetBodyAnimationGroup(ref id, ref animGroup, ref hue, true).Direction[dir];

                        if (direction != null && (direction.FrameCount == 0 || direction.Frames == null))
                        {
                            AnimationsLoader.Instance.LoadAnimationFrames(id, animGroup, dir, ref direction);
                        }

                        if (direction != null && direction.FrameCount != 0)
                        {
                            direction.LastAccessTime = Time.Ticks;
                            int fc = direction.FrameCount;

                            if (parent.AnimationFromServer)
                            {
                                currentDelay += currentDelay * (parent.AnimationInterval + 1);

                                if (parent.AnimationFrameCount == 0)
                                {
                                    parent.AnimationFrameCount = (byte)fc;
                                }
                                else
                                {
                                    fc = parent.AnimationFrameCount;
                                }

                                if (parent.AnimationForwardDirection)
                                {
                                    if (frameIndex >= fc)
                                    {
                                        frameIndex = 0;

                                        if (parent.AnimationRepeat)
                                        {
                                            byte repCount = parent.AnimationRepeatMode;

                                            if (repCount == 2)
                                            {
                                                repCount--;
                                                parent.AnimationRepeatMode = repCount;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (frameIndex < 0)
                                    {
                                        if (fc == 0)
                                        {
                                            frameIndex = 0;
                                        }
                                        else
                                        {
                                            frameIndex = (sbyte)(fc - 1);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (frameIndex >= fc)
                                {
                                    frameIndex = 0;
                                }
                            }

                            AnimIndex = frameIndex;
                        }
                    }

                    //LastAnimationChangeTime = Time.Ticks + currentDelay;
                }
            }
            */
        }
    }
}