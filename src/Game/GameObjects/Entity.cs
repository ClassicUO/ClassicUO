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
using System.Collections.Concurrent;
using System.Collections.Generic;

using ClassicUO.Game.Data;
using ClassicUO.Interfaces;
using ClassicUO.Utility;

namespace ClassicUO.Game.GameObjects
{
    internal abstract class Entity : GameObject
    {
        private readonly ConcurrentDictionary<int, Property> _properties = new ConcurrentDictionary<int, Property>();
        protected Delta _delta;
        private Direction _direction;
        private Flags _flags;
        private Hue _hue;
        private string _name;
        private Item[] _equipment;

        protected Entity(Serial serial)
        {
            Serial = serial;
            Items = new EntityCollection<Item>();
        }

        protected long LastAnimationChangeTime { get; set; }

        public EntityCollection<Item> Items { get; }

        public Item[] Equipment => _equipment ?? (_equipment = new Item[(int) Layer.Bank + 1]);

        public Serial Serial { get; }

        public IReadOnlyList<Property> Properties => (IReadOnlyList<Property>) _properties.Values;

        public override Graphic Graphic
        {
            get => base.Graphic;
            set
            {
                if (base.Graphic != value)
                {
                    base.Graphic = value;
                    _delta |= Delta.Appearance;
                }
            }
        }

        public override Hue Hue
        {
            get => _hue;
            set
            {
                ushort fixedColor = (ushort) (value & 0x3FFF);

                if (fixedColor != 0)
                {
                    if (fixedColor >= 0x0BB8)
                        fixedColor = 1;
                    fixedColor |= (ushort) (value & 0xC000);
                }
                else
                    fixedColor = (ushort) (value & 0x8000);

                if (_hue != fixedColor)
                {
                    _hue = fixedColor;
                    _delta |= Delta.Appearance;
                }
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    _delta |= Delta.Appearance;
                }
            }
        }

        public bool IsHidden => (Flags & Flags.Hidden) != 0;

        //public override Position Position
        //{
        //    get => base.Position;
        //    set
        //    {
        //        if (base.Position != value)
        //        {
        //            base.Position = value;
        //            _delta |= Delta.Position;
        //        }
        //    }
        //}

        public Direction Direction
        {
            get => _direction;
            set
            {
                if (_direction != value)
                {
                    _direction = value;
                    _delta |= Delta.Position;
                }
            }
        }

        public Flags Flags
        {
            get => _flags;
            set
            {
                if (_flags != value)
                {
                    _flags = value;
                    _delta |= Delta.Attributes;
                }
            }
        }

        public virtual bool Exists => World.Contains(Serial);

        public uint PropertiesHash { get; set; }

        public event EventHandler AppearanceChanged, PositionChanged, AttributesChanged, PropertiesChanged;

        public void UpdateProperties(IEnumerable<Property> props)
        {
            _properties.Clear();
            int temp = 0;
            foreach (Property p in props) _properties.TryAdd(temp++, p);
            _delta |= Delta.Properties;
        }

        protected virtual void OnProcessDelta(Delta d)
        {
            if (d.HasFlag(Delta.Appearance)) AppearanceChanged.Raise(this);
            if (d.HasFlag(Delta.Position)) PositionChanged.Raise(this);
            if (d.HasFlag(Delta.Attributes)) AttributesChanged.Raise(this);
            if (d.HasFlag(Delta.Properties)) PropertiesChanged.Raise(this);
        }

        protected override void OnPositionChanged()
        {
            base.OnPositionChanged();

            _delta |= Delta.Position;
        }

        public void ProcessDelta()
        {
            Delta d = _delta;
            OnProcessDelta(d);
            Items.ProcessDelta();
            _delta = Delta.None;
        }

        public override void Dispose()
        {
            _properties.Clear();
            base.Dispose();
        }


        public static implicit operator Serial(Entity entity)
        {
            return entity.Serial;
        }

        public static implicit operator uint(Entity entity)
        {
            return entity.Serial;
        }

        public override int GetHashCode()
        {
            return Serial.GetHashCode();
        }

        public abstract void ProcessAnimation();

        public abstract Graphic GetGraphicForAnimation();

        [Flags]
        protected enum Delta
        {
            None = 0,
            Appearance = 1 << 0,
            Position = 1 << 1,
            Attributes = 1 << 2,
            Ownership = 1 << 3,
            Hits = 1 << 4,
            Mana = 1 << 5,
            Stamina = 1 << 6,
            Stats = 1 << 7,
            Skills = 1 << 8,
            Properties = 1 << 9,
            ItemsUpdate = 1 << 10
        }
    }
}