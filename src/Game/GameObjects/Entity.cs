#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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
using System.Diagnostics;

using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Utility.Logging;
using static ClassicUO.Network.NetClient;

namespace ClassicUO.Game.GameObjects
{
    internal abstract class Entity : GameObject, IEquatable<Entity>
    {
        private Direction _direction;

        protected Entity(uint serial)
        {
            Serial = serial;
        }


        public uint LastStepTime;
        protected long LastAnimationChangeTime;
        public uint Serial;
        public bool IsClicked;
        public ushort Hits;
        public ushort HitsMax;
        public string Name;

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

        public Flags Flags;

        public bool Exists => World.Contains(Serial);


        public void FixHue(ushort hue)
        {
            ushort fixedColor = (ushort) (hue & 0x3FFF);

            if (fixedColor != 0)
            {
                if (fixedColor >= 0x0BB8)
                    fixedColor = 1;
                fixedColor |= (ushort) (hue & 0xC000);
            }
            else
                fixedColor = (ushort) (hue & 0x8000);

            Hue = fixedColor;
        }


        public byte HitsPercentage;
        public RenderedText HitsTexture;

        public void UpdateHits(byte perc)
        {
            if (perc != HitsPercentage || (HitsTexture == null || HitsTexture.IsDestroyed))
            {
                HitsPercentage = perc;

                ushort color = 0x0044;

                if (perc < 30)
                    color = 0x0021;
                else if (perc < 50)
                    color = 0x0030;
                else if (perc < 80)
                    color = 0x0058;

                HitsTexture?.Destroy();
                HitsTexture = RenderedText.Create($"[{perc}%]", color, 3, false);
            }
        }

        public virtual void CheckGraphicChange(sbyte animIndex = 0)
        {
            
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (UseObjectHandles && !ObjectHandlesOpened)
            {
                // TODO: Some servers may not want to receive this (causing original client to not send it),
                //but all servers tested (latest POL, old POL, ServUO, Outlands) do.
                if (SerialHelper.IsMobile(Serial))
                {
                    Socket.Send(new PNameRequest(Serial));
                }

                UIManager.Add(new NameOverheadGump(this));

                ObjectHandlesOpened = true;
            }



            if (HitsMax > 0)
            {
                int hits_max = HitsMax;

                hits_max = Hits * 100 / hits_max;

                if (hits_max > 100)
                    hits_max = 100;
                else if (hits_max < 1)
                    hits_max = 0;

                UpdateHits((byte) hits_max);
            }
        }

        public override void Destroy()
        {
            base.Destroy();

            HitsTexture?.Destroy();
            HitsTexture = null;
        }

        public Item FindItem(ushort graphic, ushort hue = 0xFFFF)
        {
            Item item = null;

            if (hue == 0xFFFF)
            {
                var minColor = 0xFFFF;

                for (var i = Items; i != null; i = i.Next)
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
                for (var i = Items; i != null; i = i.Next)
                {
                    Item it = (Item) i;

                    if (it.Graphic == graphic && it.Hue == hue)
                        item = it;

                    if (SerialHelper.IsValid(it.Container))
                    {
                        Item found = it.FindItem(graphic, hue);

                        if (found != null)
                            item = found;
                    }
                }
            }

            return item;
        }

        public Item GetItemByGraphic(ushort graphic, bool deepsearch = false)
        {
            for (var i = Items; i != null; i = i.Next)
            {
                Item item = (Item) i;

                if (item.Graphic == graphic)
                    return item;

                if (deepsearch && !item.IsEmpty)
                {
                    for (var ic = Items; ic != null; ic = ic.Next)
                    {
                        Item childItem = (Item) ic;

                        Item res = childItem.GetItemByGraphic(graphic, deepsearch);

                        if (res != null)
                            return res;
                    }
                }
            }

            return null;
        }

        public Item FindItemByLayer(Layer layer)
        {
            for (var i = Items; i != null; i = i.Next)
            {
                Item it = (Item) i;

                if (it.Layer == layer)
                    return it;
            }

            return null;
        }

        //public new void Clear()
        //{
        //    if (!IsEmpty)
        //    {
        //        var obj = Items;

        //        while (obj != null)
        //        {
        //            var next = obj.Next;
        //            Item it = (Item) obj;

        //            it.Container = 0;
        //            World.Items.Remove(it);

        //            Remove(obj);

        //            obj = next;
        //        }
        //    }
        //}

        public void ClearUnequipped()
        {
            if (!IsEmpty)
            {
                LinkedObject new_first = null;
                var obj = Items;

                while (obj != null)
                {
                    var next = obj.Next;

                    Item it = (Item) obj;

                    if (it.Layer != 0)
                    {
                        if (new_first == null)
                        {
                            new_first = obj;
                        }
                    }
                    else
                    {
                        it.Container = 0;
                        World.Items.Remove(it);
                        it.Destroy();
                        Remove(obj);
                    }
                    
                    obj = next;
                }


                Items = new_first;

            }
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

        public bool Equals(Entity e)
        {
            return e != null && Serial == e.Serial;
        }

        public override bool Equals(object obj)
        {
            return obj is Entity ent && Equals(ent);
        }

        public override int GetHashCode()
        {
            return (int) Serial;
        }

        public abstract void ProcessAnimation(out byte dir, bool evalutate = false);

        public abstract ushort GetGraphicForAnimation();
    }
}