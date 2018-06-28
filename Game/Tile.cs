using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game
{
    public sealed class Tile
    {
        private Position _location;
        private readonly Dictionary<Serial, Entity> _entities;

        public Tile()
        {
            _entities = new Dictionary<Serial, Entity>();
        }

        public ushort X
        {
            get => _location.X;
            set => _location.X = value;
        }

        public ushort Y
        {
            get => _location.Y;
            set => _location.Y = value;
        }

        public Position Location { get => _location; set => _location = value; }

        public IReadOnlyDictionary<Serial, Entity> Entities => _entities;

        public void AddEntity(in Entity entity)
        {
            _entities.Add(entity.Serial, entity);
        }

        public void RemoveEntity(in Entity entity)
        {
            _entities.Remove(entity);
        }

        public void Clear()
        {
            _entities.Clear();
        }
    }
}
