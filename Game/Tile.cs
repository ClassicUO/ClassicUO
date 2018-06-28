using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game
{
    public sealed class Tile
    {
        private readonly Dictionary<Serial, Entity> _entities;

        public Tile()
        {
            _entities = new Dictionary<Serial, Entity>();
        }

        public Position Location { get; set; }

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
