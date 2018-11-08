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
using System.Linq;

using ClassicUO.Game.Data;
using ClassicUO.Interfaces;
using ClassicUO.Utility;

namespace ClassicUO.Game.GameObjects
{
    [Flags]
    public enum Flags : byte
    {
        Frozen = 0x01,
        Female = 0x02,
        Poisoned = 0x04,
        Flying = 0x04,
        YellowBar = 0x08,
        IgnoreMobiles = 0x10,
        Movable = 0x20,
        WarMode = 0x40,
        Hidden = 0x80
    }

    public abstract class Entity : GameObject, IDeferreable
    {
        protected const float CHARACTER_ANIMATION_DELAY = 80;
        private readonly ConcurrentDictionary<int, Property> _properties = new ConcurrentDictionary<int, Property>();
        protected Delta _delta;
        private Direction _direction;
        private Flags _flags;
        private Graphic _graphic;
        private Hue _hue;
        protected long _lastAnimationChangeTime;
        private string _name;
        protected Action<Entity> _OnDisposed;
        protected Action<Entity> _OnUpdated;

        protected Entity(Serial serial)
        {
            Serial = serial;
            Items = new EntityCollection<Item>();
            PositionChanged += OnPositionChanged;
            Items.Added += ItemsOnUpdated;
            Items.Removed += ItemsOnUpdated;
        }

        private void ItemsOnUpdated(object sender, CollectionChangedEventArgs<Item> e)
        {
            _OnUpdated?.Invoke(this);
        }

        public EntityCollection<Item> Items { get; }

        public Serial Serial { get; }

        public IReadOnlyList<Property> Properties => (IReadOnlyList<Property>) _properties.Values;

        public override Graphic Graphic
        {
            get => _graphic;
            set
            {
                if (_graphic != value)
                {
                    _graphic = value;
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

                if (fixedColor > 0)
                {
                    if (fixedColor >= 0x0BB8) fixedColor = 1;
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

        public override Position Position
        {
            get => base.Position;
            set
            {
                if (base.Position != value)
                {
                    base.Position = value;
                    _delta |= Delta.Position;
                }
            }
        }

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

        public DeferredEntity DeferredObject { get; set; }

        public void SetCallbacks(Action<Entity> onUpdate, Action<Entity> onDispose)
        {
            if (onUpdate != null)
                _OnUpdated += onUpdate;

            if (onDispose != null)
                _OnDisposed += onDispose;
        }

        public void ClearCallBacks(Action<Entity> onUpdate, Action<Entity> onDispose)
        {
            if (_OnUpdated == null && _OnDisposed == null)
                return;

            if (_OnUpdated.GetInvocationList().Contains(onUpdate))
                _OnUpdated -= onUpdate;

            if (_OnDisposed.GetInvocationList().Contains(onDispose))
                _OnDisposed -= onDispose;
        }

        public event EventHandler AppearanceChanged, PositionChanged, AttributesChanged, PropertiesChanged;

        public void UpdateProperties(IEnumerable<Property> props)
        {
            _properties.Clear();
            int temp = 0;
            foreach (Property p in props) _properties.TryAdd(temp++, p);
            _delta |= Delta.Properties;
        }

        public uint PropertiesHash { get; set; }

        protected virtual void OnProcessDelta(Delta d)
        {
            if (d.HasFlag(Delta.Appearance)) AppearanceChanged.Raise(this);
            if (d.HasFlag(Delta.Position)) PositionChanged.Raise(this);
            if (d.HasFlag(Delta.Attributes)) AttributesChanged.Raise(this);
            if (d.HasFlag(Delta.Properties)) PropertiesChanged.Raise(this);
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
            if (DeferredObject != null)
            {
                DeferredObject.Reset();
                DeferredObject = null;
            }

            _properties.Clear();
            _OnDisposed?.Invoke(this);
            Items.Added -= ItemsOnUpdated;
            Items.Removed -= ItemsOnUpdated;
            _OnUpdated = null;
            _OnDisposed = null;
            base.Dispose();
        }

        protected virtual void OnPositionChanged(object sender, EventArgs e)
        {
            
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

        public virtual void ProcessAnimation()
        {
        }

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