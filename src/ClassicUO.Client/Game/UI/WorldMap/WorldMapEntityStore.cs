// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;

namespace ClassicUO.Game.UI.WorldMap
{
    /// <summary>
    /// In-memory per-serial dictionary of tracked world-map entities.
    /// Pure data store: no time, no network, no event subs.
    /// </summary>
    internal sealed class WorldMapEntityStore : IWorldMapEntityStore
    {
        public Dictionary<uint, WMapEntity> Entities { get; } = new Dictionary<uint, WMapEntity>();

        public WMapEntity AddOrUpdate(uint serial, int x, int y, int hp, int map, bool isguild, string name)
        {
            if (!Entities.TryGetValue(serial, out WMapEntity entity) || entity == null)
            {
                entity = new WMapEntity(serial)
                {
                    X = x,
                    Y = y,
                    HP = hp,
                    Map = map,
                    LastUpdate = Time.Ticks + 1000,
                    IsGuild = isguild,
                    Name = name
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

                if (string.IsNullOrEmpty(entity.Name) && !string.IsNullOrEmpty(name))
                {
                    entity.Name = name;
                }
            }

            return entity;
        }

        public bool TryGet(uint serial, out WMapEntity entity) => Entities.TryGetValue(serial, out entity);

        public void Remove(uint serial)
        {
            if (Entities.ContainsKey(serial))
            {
                Entities.Remove(serial);
            }
        }

        public void Clear() => Entities.Clear();
    }
}
