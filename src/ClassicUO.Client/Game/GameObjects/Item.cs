#region license

// Copyright (c) 2024, andreakarasho
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
using System.Collections.Generic;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using ClassicUO.Utility.Platforms;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    internal partial class Item : Entity
    {
        //private static readonly QueuedPool<Item> _pool = new QueuedPool<Item>(
        //    Constants.PREDICTABLE_CHUNKS * 3,
        //    i =>
        //    {
        //        i.IsDestroyed = false;
        //        i.Graphic = 0;
        //        i.Amount = 0;
        //        i.Container = 0xFFFF_FFFF;
        //        i._isMulti = false;
        //        i.Layer = 0;
        //        i.Price = 0;
        //        i.UsedLayer = false;
        //        i._displayedGraphic = null;
        //        i.X = 0;
        //        i.Y = 0;
        //        i.Z = 0;

        //        i.LightID = 0;
        //        i.MultiDistanceBonus = 0;
        //        i.Flags = 0;
        //        i.WantUpdateMulti = true;
        //        i.MultiInfo = null;
        //        i.MultiGraphic = 0;

        //        i.AlphaHue = 0;
        //        i.Name = null;
        //        i.Direction = 0;
        //        i.AnimIndex = 0;
        //        i.Hits = 0;
        //        i.HitsMax = 0;
        //        i.LastStepTime = 0;
        //        i.LastAnimationChangeTime = 0;

        //        i.Clear();

        //        i.IsClicked = false;
        //        i.IsDamageable = false;
        //        i.Offset = Vector3.Zero;
        //        i.HitsPercentage = 0;
        //        i.Opened = false;
        //        i.TextContainer?.Clear();
        //        i.IsFlipped = false;
        //        i.FrameInfo = Rectangle.Empty;
        //        i.ObjectHandlesStatus = ObjectHandlesStatus.NONE;
        //        i.AlphaHue = 0;
        //        i.AllowedToDraw = true;
        //        i.ExecuteAnimation = true;
        //        i.HitsRequest = HitsRequestStatus.None;
        //    }
        //);

        private ushort? _displayedGraphic;
        private bool _isMulti;

        public Item(World world) : base(world, 0) { }

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
                        return (ushort)(Graphic + 2);
                    }

                    if (Amount > 1)
                    {
                        return (ushort)(Graphic + 1);
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

        public bool IsCorpse => /*MathHelper.InRange(Graphic, 0x0ECA, 0x0ED2) ||*/
            Graphic == 0x2006;

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

        public ref StaticTiles ItemData =>
            ref TileDataLoader.Instance.StaticData[IsMulti ? MultiGraphic : Graphic];

        public bool IsLootable =>
            ItemData.Layer != (int)Layer.Hair
            && ItemData.Layer != (int)Layer.Beard
            && ItemData.Layer != (int)Layer.Face
            && Graphic != 0;

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

        public static Item Create(World world, uint serial)
        {
            Item i = new Item(world); // _pool.GetOne();
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

            //_pool.ReturnOne(this);
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
                house = new House(World, Serial, 0, false);
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
                    Span<byte> span =
                        entry.DecompressedLength <= 1024
                            ? stackalloc byte[entry.DecompressedLength]
                            : (
                                buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(
                                    entry.DecompressedLength
                                )
                            );

                    try
                    {
                        fixed (byte* dataPtr = span)
                        {
                            ZLib.Decompress(
                                MultiLoader.Instance.File.PositionAddress,
                                entry.Length,
                                0,
                                (IntPtr)dataPtr,
                                entry.DecompressedLength
                            );

                            StackDataReader reader = new StackDataReader(
                                span.Slice(0, entry.DecompressedLength)
                            );
                            reader.Skip(4);

                            int count = reader.ReadInt32LE();

                            int sizeOf = sizeof(MultiBlockNew);

                            for (int i = 0; i < count; i++)
                            {
                                MultiBlockNew* block = (MultiBlockNew*)(
                                    reader.PositionAddress + i * sizeOf
                                );

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
                                    Multi m = Multi.Create(World, block->ID);
                                    m.MultiOffsetX = block->X;
                                    m.MultiOffsetY = block->Y;
                                    m.MultiOffsetZ = block->Z;
                                    m.Hue = Hue;
                                    m.AlphaHue = 255;
                                    m.IsCustom = false;
                                    m.State = CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_DONT_REMOVE;
                                    m.IsMovable = ItemData.IsMultiMovable;

                                    m.SetInWorldTile(
                                        (ushort)(X + block->X),
                                        (ushort)(Y + block->Y),
                                        (sbyte)(Z + block->Z)
                                    );

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
                    MultiBlock* block = (MultiBlock*)(
                        MultiLoader.Instance.File.PositionAddress + i * MultiLoader.Instance.Offset
                    );

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
                        Multi m = Multi.Create(World, block->ID);
                        m.MultiOffsetX = block->X;
                        m.MultiOffsetY = block->Y;
                        m.MultiOffsetZ = block->Z;
                        m.Hue = Hue;
                        m.AlphaHue = 255;
                        m.IsCustom = false;
                        m.State = CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_DONT_REMOVE;
                        m.IsMovable = ItemData.IsMultiMovable;

                        m.SetInWorldTile(
                            (ushort)(X + block->X),
                            (ushort)(Y + block->Y),
                            (sbyte)(Z + block->Z)
                        );

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

            MultiDistanceBonus = Math.Max(
                Math.Max(Math.Abs(minX), maxX),
                Math.Max(Math.Abs(minY), maxY)
            );

            house.Bounds = MultiInfo.Value;

            UIManager.GetGump<MiniMapGump>()?.RequestUpdateContents();

            if (World.HouseManager.EntityIntoHouse(Serial, World.Player))
            {
                Client.Game.GetScene<GameScene>()?.UpdateMaxDrawZ(true);
            }

            World.BoatMovingManager.ClearSteps(Serial);
        }

        public override void CheckGraphicChange(byte animIndex = 0)
        {
            if (!IsMulti)
            {
                if (!IsCorpse)
                {
                    AllowedToDraw = CanBeDrawn(World, Graphic);
                }
                else
                {
                    AnimIndex = 99;

                    if ((Direction & Direction.Running) != 0)
                    {
                        UsedLayer = true;
                        Direction &= (Direction)0x7F;
                    }
                    else
                    {
                        UsedLayer = false;
                    }

                    Layer = (Layer)Direction;
                    AllowedToDraw = true;
                }
            }
            else if (WantUpdateMulti)
            {
                World.UoAssist.SignalAddMulti((ushort)(Graphic | 0x4000), X, Y);

                if (
                    MultiDistanceBonus == 0
                    || World.HouseManager.IsHouseInRange(Serial, World.ClientViewRange)
                )
                {
                    LoadMulti();
                    AllowedToDraw = MultiGraphic > 2;
                }
            }
        }

        public override void Update()
        {
            if (IsDestroyed)
            {
                return;
            }

            base.Update();

            ProcessAnimation();
        }

        private static readonly Dictionary<ushort, ushort> _mounts = new Dictionary<
            ushort,
            ushort
        >()
        {
            { 0x3E90, 0x0114 }, // 16016 Reptalon
            { 0x3E91, 0x0115 }, // 16017
            { 0x3E92, 0x011C }, // 16018
            { 0x3E94, 0x00F3 }, // 16020
            { 0x3E95, 0x00A9 }, // 16021
            { 0x3E97, 0x00C3 }, // 16023 Ethereal Giant Beetle
            { 0x3E98, 0x00C2 }, // 16024 Ethereal Swamp Dragon
            { 0x3E9A, 0x00C1 }, // 16026 Ethereal Ridgeback
            { 0x3E9B, 0x00C0 }, // 16027
            { 0x3E9D, 0x00C0 }, // 16029 Ethereal Unicorn
            { 0x3E9C, 0x00BF }, // 16028 Ethereal Kirin
            { 0x3E9E, 0x00BE }, // 16030
            { 0x3EA0, 0x00E2 }, // 16032 light grey/horse3
            { 0x3EA1, 0x00E4 }, // 16033 greybrown/horse4
            { 0x3EA2, 0x00CC }, // 16034 dark brown/horse
            { 0x3EA3, 0x00D2 }, // 16035 desert ostard
            { 0x3EA4, 0x00DA }, // 16036 frenzied ostard (=zostrich)
            { 0x3EA5, 0x00DB }, // 16037 forest ostard
            { 0x3EA6, 0x00DC }, // 16038 Llama
            { 0x3EA7, 0x0074 }, // 16039 Nightmare / Vortex
            { 0x3EA8, 0x0075 }, // 16040 Silver Steed
            { 0x3EA9, 0x0072 }, // 16041 Nightmare
            { 0x3EAA, 0x0073 }, // 16042 Ethereal Horse
            { 0x3EAB, 0x00AA }, // 16043 Ethereal Llama
            { 0x3EAC, 0x00AB }, // 16044 Ethereal Ostard
            { 0x3EAD, 0x0084 }, // 16045 Kirin
            { 0x3EAF, 0x0078 }, // 16047 War Horse (Blood Red)
            { 0x3EB0, 0x0079 }, // 16048 War Horse (Light Green)
            { 0x3EB1, 0x0077 }, // 16049 War Horse (Light Blue)
            { 0x3EB2, 0x0076 }, // 16050 War Horse (Purple)
            { 0x3EB3, 0x0090 }, // 16051 Sea Horse (Medium Blue)
            { 0x3EB4, 0x007A }, // 16052 Unicorn
            { 0x3EB5, 0x00B1 }, // 16053 Nightmare
            { 0x3EB6, 0x00B2 }, // 16054 Nightmare 4
            { 0x3EB7, 0x00B3 }, // 16055 Dark Steed
            { 0x3EB8, 0x00BC }, // 16056 Ridgeback
            { 0x3EBA, 0x00BB }, // 16058 Ridgeback, Savage
            { 0x3EBB, 0x0319 }, // 16059 Skeletal Mount
            { 0x3EBC, 0x0317 }, // 16060 Beetle
            { 0x3EBD, 0x031A }, // 16061 SwampDragon
            { 0x3EBE, 0x031F }, // 16062 Armored Swamp Dragon
            { 0x3EC3, 0x02D4 }, // 16067 Beetle
            { 0x3ECE, 0x059A }, // serpentine dragon
            { 0x3EC5, 0x00D5 }, // 16069
            { 0x3F3A, 0x00D5 }, // 16186 snow bear ???
            { 0x3EC6, 0x01B0 }, // 16070 Boura
            { 0x3EC7, 0x04E6 }, // 16071 Tiger
            { 0x3EC8, 0x04E7 }, // 16072 Tiger
            { 0x3EC9, 0x042D }, // 16073
            { 0x3ECA, 0x0579 }, // tarantula
            { 0x3ECC, 0x0582 }, // 16016
            { 0x3ED1, 0x05E6 }, // CoconutCrab
            { 0x3ECB, 0x057F }, // Lasher
            { 0x3ED0, 0x05A1 }, // SkeletalCat
            { 0x3ED2, 0x05F6 }, // war boar
            { 0x3ECD, 0x0580 }, // Palomino
            { 0x3ECF, 0x05A0 }, // Eowmu
            { 0x3ED3, 0x05F7 }, // capybara
            { 0x3ED4, 0x060A },
            { 0x3ED5, 0x060B }, // a wolf
            { 0x3ED6, 0x060C }, // an orange dog 2?
            { 0x3ED7, 0x060D },
            { 0x3ED8, 0x060F }, // a black dog?
            { 0x3ED9, 0x0610 }, // a dobberman?
            { 0x3EDA, 0x0590 } // Frostmites Beetles
        };

        public override ushort GetGraphicForAnimation()
        {
            var graphic = Graphic;

            if (Layer == Layer.Mount)
            {
                // ethereal unicorn
                if (graphic == 0x3E9B || graphic == 0x3E9D)
                {
                    return 0x00C0;
                }

                // ethereal kirin
                if (graphic == 0x3E9C)
                {
                    return 0x00BF;
                }

                if (_mounts.TryGetValue(graphic, out var newGraphic))
                {
                    graphic = newGraphic;
                }

                if (ItemData.AnimID != 0)
                {
                    graphic = ItemData.AnimID;
                }
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

            TextObject last = (TextObject)TextContainer.Items;

            while (last?.Next != null)
            {
                last = (TextObject)last.Next;
            }

            if (last == null)
            {
                return;
            }

            int offY = 0;

            if (OnGround)
            {
                Point p = RealScreenPosition;

                var bounds = Client.Game.UO.Arts.GetRealArtBounds(Graphic);
                p.Y -= bounds.Height >> 1;

                p.X += (int)Offset.X + 22;
                p.Y += (int)(Offset.Y - Offset.Z) + 22;

                p = Client.Game.Scene.Camera.WorldToScreen(p);

                for (; last != null; last = (TextObject)last.Previous)
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
                for (; last != null; last = (TextObject)last.Previous)
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

        public override void ProcessAnimation(bool evalutate = false)
        {
            if (IsCorpse)
            {
                var dir = (byte)Layer;

                if (LastAnimationChangeTime < Time.Ticks)
                {
                    byte frameIndex = (byte)(AnimIndex + (ExecuteAnimation ? 1 : 0));
                    ushort id = GetGraphicForAnimation();

                    bool mirror = false;
                    AnimationsLoader.Instance.GetAnimDirection(ref dir, ref mirror);

                    if (id < Client.Game.UO.Animations.MaxAnimationCount && dir < 5)
                    {
                        Client.Game.UO.Animations.ConvertBodyIfNeeded(ref id);
                        var animGroup = Client.Game.UO.Animations.GetAnimType(id);
                        var animFlags = Client.Game.UO.Animations.GetAnimFlags(id);
                        byte action = AnimationsLoader.Instance.GetDeathAction(
                            id,
                            animFlags,
                            animGroup,
                            UsedLayer
                        );
                        var frames = Client.Game.UO.Animations.GetAnimationFrames(
                            id,
                            action,
                            dir,
                            out _,
                            out _,
                            isCorpse: true
                        );

                        if (frames.Length > 0)
                        {
                            // when the animation is done, stop to animate the corpse
                            if (frameIndex >= frames.Length)
                            {
                                frameIndex = (byte)(frames.Length - 1);
                            }

                            AnimIndex = (byte)(frameIndex % frames.Length);
                        }
                    }

                    LastAnimationChangeTime = Time.Ticks + Constants.CHARACTER_ANIMATION_DELAY;
                }
            }
        }
    }
}
