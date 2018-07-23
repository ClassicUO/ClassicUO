using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Utility;

namespace ClassicUO.Game.WorldObjects
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

    public abstract class Entity : WorldObject
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

        private Position _position;

        protected Entity(in Serial serial) : base(World.Map)
        {
            Serial = serial;
            Items = new EntityCollection<Item>();

            PositionChanged += OnPositionChanged;
        }

        public EntityCollection<Item> Items { get; }
        public Serial Serial { get; }
        public IEnumerable<Property> Properties => _properties.Select(s => s.Value);

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
                var fixedColor = (ushort) (value & 0x3FFF);

                if (fixedColor > 0)
                {
                    if (fixedColor >= 0x0BB8)
                        fixedColor = 1;
                    fixedColor |= (ushort) (value & 0xC000);
                }
                else
                {
                    fixedColor = (ushort) (value & 0x8000);
                }


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
            get => _position;
            set
            {
                if (_position != value)
                {
                    _position = value;
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
        public int Distance => DistanceTo(World.Player);
        public event EventHandler AppearanceChanged, PositionChanged, AttributesChanged, PropertiesChanged;

        public void UpdateProperties(IEnumerable<Property> props)
        {
            _properties.Clear();
            var temp = 0;
            foreach (var p in props)
                _properties.TryAdd(temp++, p);
            _delta |= Delta.Properties;
        }

        protected virtual void OnProcessDelta(Delta d)
        {
            if (d.HasFlag(Delta.Appearance))
                AppearanceChanged.Raise(this);

            if (d.HasFlag(Delta.Position))
                PositionChanged.Raise(this);

            if (d.HasFlag(Delta.Attributes))
                AttributesChanged.Raise(this);

            if (d.HasFlag(Delta.Properties))
                PropertiesChanged.Raise(this);
        }

        public void ProcessDelta()
        {
            var d = _delta;
            OnProcessDelta(d);
            Items.ProcessDelta();
            _delta = Delta.None;
        }

        protected virtual void OnPositionChanged(object sender, EventArgs e)
        {
            Tile = World.Map.GetTile((short) Position.X, (short) Position.Y);
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

        public int DistanceTo(Entity entity)
        {
            return Position.DistanceTo(entity.Position);
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
            Properties = 1 << 9
        }
    }
}