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

using System.Collections.Generic;

namespace ClassicUO.Game.Managers
{
    class WMapEntity
    {
        public WMapEntity(uint serial)
        {
            Serial = serial;
        } 

        public readonly uint Serial;
        public int X, Y, HP, Map;
        public uint LastUpdate;
        public bool IsGuild;
        public bool IsParty;
        public string Name;

    }

    class WorldMapEntityManager
    {
        public readonly Dictionary<uint, WMapEntity> Entities = new Dictionary<uint, WMapEntity>();

        private readonly List<WMapEntity> _toRemove = new List<WMapEntity>();

        private uint _lastUpdate;
 
        public void AddOrUpdate(uint serial, int x, int y, int hp, int map, string name, int isguild, int isparty)
        {
            if (!Entities.TryGetValue(serial, out var entity) || entity == null)
            {
                
                entity = new WMapEntity(serial) // create new entity
                {
                    X = x,
                    Y = y,
                    HP = hp,
                    Map = map,
                    Name = name,
                    LastUpdate = Time.Ticks,
                    IsGuild = bool.Equals(isguild, 1),
                    IsParty = bool.Equals(isparty, 1),

                };

                Entities[serial] = entity;
            }
            else //Update existing entity > Leave existing if values are -1 or null
            {
                entity.X = x;
                entity.Y = y;
                entity.LastUpdate = Time.Ticks;

                if (hp != -1)
                {
                    entity.HP = hp;
                }

                if (map != -1)
                {
                    entity.Map = map;
                }

                if (name != null)
                {
                    entity.Name = name;
                }

                if (isguild != -1)
                {
                    entity.IsGuild = (bool.Equals(isguild, 1));
                }

                if (isparty != -1)
                {
                    entity.IsParty = (bool.Equals(isparty, 1));
                }

                //entity.Map = map;                
                //entity.Name = name;
                //entity.IsGuild = isguild;
                //entity.IsParty = isparty;
            }    
        }

        
        public void RemoveUnupdatedWEntity()
        {
            if (_lastUpdate > Time.Ticks)
                return;

            _lastUpdate = Time.Ticks + 150;  // removal update scan time

            long ticks = Time.Ticks - 1000;  // time to remove unupdated entries

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

        public WMapEntity GetEntity(uint serial)
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
