using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.GameObjects;
using ClassicUO.Renderer;

namespace ClassicUO.Game.Managers
{
    class WMapEntity
    {
        public WMapEntity(Serial serial)
        {
            Serial = serial;

            var mob = World.Mobiles.Get(serial);

            if (mob != null)
                GetName();
        } 

        public readonly Serial Serial;
        public int X, Y, HP, Map;
        public uint LastUpdate;
        public bool IsGuild;
        public string Name;

        public string GetName()
        {
            Entity e = World.Get(Serial);

            if (e != null && !e.IsDestroyed && !string.IsNullOrEmpty(e.Name) && Name != e.Name)
            {
                Name = e.Name;
            }

            return string.IsNullOrEmpty(Name) ? "<out of range>" : Name;
        }
    }

    class WorldMapEntityManager
    {
        public readonly Dictionary<Serial, WMapEntity> Entities = new Dictionary<Serial, WMapEntity>();

        private readonly List<WMapEntity> _toRemove = new List<WMapEntity>();

        private uint _lastUpdate;
 
        public void AddOrUpdate(Serial serial, int x, int y, int hp, int map, bool isguild)
        {
            if (!Entities.TryGetValue(serial, out var entity) || entity == null)
            {
                entity = new WMapEntity(serial)
                {
                    X = x, Y = y, HP = hp, Map = map,
                    LastUpdate = Time.Ticks + 1000,
                    IsGuild = isguild
                };

                Entities[serial] = entity;
            }
            else
            {
                entity.X = x;
                entity.Y = y;
                entity.HP = hp;
                entity.Map = map;
                entity.IsGuild = isguild;
                entity.LastUpdate = Time.Ticks + 1000;
            }
        }

        public void Remove(Serial serial)
        {
            if (Entities.ContainsKey(serial))
            {
                Entities.Remove(serial);
            }
        }

        public void RemoveUnupdatedWEntity()
        {
            if (_lastUpdate < Time.Ticks)
                return;

            _lastUpdate = Time.Ticks + 1000;

            long ticks = Time.Ticks - 1000;

            foreach (WMapEntity entity in Entities.Values)
            {
                if (entity.LastUpdate < ticks)
                    _toRemove.Add(entity);
            }

            if (_toRemove.Count != 0)
            {
                foreach (WMapEntity entity in _toRemove)
                {
                    Entities.Remove(entity.Serial);
                }

                _toRemove.Clear();
            }
        }

        public WMapEntity GetEntity(Serial serial)
        {
            Entities.TryGetValue(serial, out var entity);

            return entity;
        }

        public void Clear()
        {
            Entities.Clear();
        }
    }
}
