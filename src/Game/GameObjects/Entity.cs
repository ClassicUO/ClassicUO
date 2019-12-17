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
using System.Collections.Concurrent;
using System.Collections.Generic;

using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Utility;

namespace ClassicUO.Game.GameObjects
{
    internal abstract class Entity : GameObject, IEquatable<Entity>
    {
        private Direction _direction;
        private Item[] _equipment;

        protected Entity(uint serial)
        {
            Serial = serial;
            Items = new EntityCollection<Item>();
        }




        protected long LastAnimationChangeTime;

        public EntityCollection<Item> Items { get; protected set; }

        public bool HasEquipment => _equipment != null;

        public Item[] Equipment
        {
            get => _equipment ?? (_equipment = new Item[(int) Layer.Bank + 0x11]);
            set => _equipment = value;
        }

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

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (UseObjectHandles && !ObjectHandlesOpened)
            {
                //NameOverheadGump gump = UIManager.GetByLocalSerial<NameOverheadGump>(Serial);

                //if (gump == null)
                {
                    UIManager.Add(new NameOverheadGump(this));
                    ObjectHandlesOpened = true;
                }
            }
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

                    if (SerialHelper.IsValid(i.Container))
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

                    if (SerialHelper.IsValid(i.Container))
                    {
                        Item found = i.FindItem(graphic, hue);

                        if (found != null)
                            item = found;
                    }
                }
            }

            return item;
        }

        public Item FindItemByLayer(Layer layer)
        {
            foreach (Item i in Items)
            {
                if (i.Layer == layer)
                    return i;
            }

            return null;
        }

        public void ProcessDelta()
        {
            Items.ProcessDelta();
        }

        public override void Destroy()
        {
            _equipment = null;
            base.Destroy();
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
            return Serial == e.Serial;
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