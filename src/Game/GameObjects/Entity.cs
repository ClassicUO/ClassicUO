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
using System.Runtime.CompilerServices;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using static ClassicUO.Network.NetClient;

namespace ClassicUO.Game.GameObjects
{
    enum HitsRequestStatus
    {
        None,
        Pending,
        Received
    }

    internal abstract class Entity : GameObject, IEquatable<Entity>
    {
        private static readonly RenderedText[] _hitsPercText = new RenderedText[101];
        private Direction _direction;


        protected Entity(uint serial)
        {
            Serial = serial;
        }

        public byte AnimIndex;
        public bool ExecuteAnimation = true;
        internal long LastAnimationChangeTime;
        public Flags Flags;
        public ushort Hits;
        public ushort HitsMax;
        public byte HitsPercentage;
        public bool IsClicked;
        public uint LastStepTime;
        public string Name;
        public uint Serial;
        public HitsRequestStatus HitsRequest;


        public bool IsHidden => (Flags & Flags.Hidden) != 0;

        public Direction Direction
        {
            get => _direction;
            set
            {
                if (_direction != value)
                {
                    _direction = value;
                    OnDirectionChanged();
                }
            }
        }

        public bool Exists => World.Contains(Serial);

        public RenderedText HitsTexture => _hitsPercText[HitsPercentage % _hitsPercText.Length];


        public bool Equals(Entity e)
        {
            return e != null && Serial == e.Serial;
        }

        public void FixHue(ushort hue)
        {
            ushort fixedColor = (ushort) (hue & 0x3FFF);

            if (fixedColor != 0)
            {
                if (fixedColor >= 0x0BB8)
                {
                    fixedColor = 1;
                }

                fixedColor |= (ushort) (hue & 0xC000);
            }
            else
            {
                fixedColor = (ushort) (hue & 0x8000);
            }

            Hue = fixedColor;
        }

        public void UpdateHits(byte perc)
        {
            if (perc != HitsPercentage)
            {
                HitsPercentage = perc;

                ref var rtext = ref _hitsPercText[perc % _hitsPercText.Length];

                if (rtext == null || rtext.IsDestroyed)
                {
                    ushort color = 0x0044;

                    if (perc < 30)
                    {
                        color = 0x0021;
                    }
                    else if (perc < 50)
                    {
                        color = 0x0030;
                    }
                    else if (perc < 80)
                    {
                        color = 0x0058;
                    }

                    rtext = RenderedText.Create($"[{perc}%]", color, 3, false);
                }
            }
        }

        public virtual void CheckGraphicChange(byte animIndex = 0)
        {
        }

        public override void Update()
        {
            base.Update();

            if (ObjectHandlesStatus == ObjectHandlesStatus.OPEN)
            {
                ObjectHandlesStatus = ObjectHandlesStatus.DISPLAYING;

                // TODO: Some servers may not want to receive this (causing original client to not send it),
                //but all servers tested (latest POL, old POL, ServUO, Outlands) do.
                if ( /*Client.Version > ClientVersion.CV_200 &&*/ SerialHelper.IsMobile(Serial))
                {
                    Socket.Send_NameRequest(Serial);
                }

                UIManager.Add(new NameOverheadGump(this));
            }


            if (HitsMax > 0)
            {
                var perc = MathHelper.PercetangeOf(Hits, HitsMax);
                perc = perc > 100 ? 100 : perc < 0 ? 0 : perc;

                UpdateHits((byte)perc);
            }
        }

        public override void Destroy()
        {
            base.Destroy();

            GameActions.SendCloseStatus(Serial, HitsRequest >= HitsRequestStatus.Pending);

            AnimIndex = 0;
            LastAnimationChangeTime = 0;
        }

        public Item FindItem(ushort graphic, ushort hue = 0xFFFF)
        {
            Item item = null;

            if (hue == 0xFFFF)
            {
                int minColor = 0xFFFF;

                for (LinkedObject i = Items; i != null; i = i.Next)
                {
                    Item it = (Item) i;

                    if (it.Graphic == graphic)
                    {
                        if (it.Hue < minColor)
                        {
                            item = it;
                            minColor = it.Hue;
                        }
                    }

                    if (SerialHelper.IsValid(it.Container))
                    {
                        Item found = it.FindItem(graphic, hue);

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
                for (LinkedObject i = Items; i != null; i = i.Next)
                {
                    Item it = (Item) i;

                    if (it.Graphic == graphic && it.Hue == hue)
                    {
                        item = it;
                    }

                    if (SerialHelper.IsValid(it.Container))
                    {
                        Item found = it.FindItem(graphic, hue);

                        if (found != null)
                        {
                            item = found;
                        }
                    }
                }
            }

            return item;
        }

        public Item GetItemByGraphic(ushort graphic, bool deepsearch = false)
        {
            for (LinkedObject i = Items; i != null; i = i.Next)
            {
                Item item = (Item) i;

                if (item.Graphic == graphic)
                {
                    return item;
                }

                if (deepsearch && !item.IsEmpty)
                {
                    for (LinkedObject ic = Items; ic != null; ic = ic.Next)
                    {
                        Item childItem = (Item) ic;

                        Item res = childItem.GetItemByGraphic(graphic, deepsearch);

                        if (res != null)
                        {
                            return res;
                        }
                    }
                }
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Item FindItemByLayer(Layer layer)
        {
            for (LinkedObject i = Items; i != null; i = i.Next)
            {
                Item it = (Item) i;

                if (!it.IsDestroyed && it.Layer == layer)
                {
                    return it;
                }
            }

            return null;
        }

        public static implicit operator uint(Entity entity)
        {
            return entity.Serial;
        }

        public static bool operator ==(Entity e, Entity s)
        {
            return Equals(e, s);
        }

        public static bool operator !=(Entity e, Entity s)
        {
            return !Equals(e, s);
        }

        public override bool Equals(object obj)
        {
            return obj is Entity ent && Equals(ent);
        }

        public override int GetHashCode()
        {
            return (int) Serial;
        }

        public abstract void ProcessAnimation(bool evalutate = false);

        public abstract ushort GetGraphicForAnimation();
    }
}