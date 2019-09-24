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

using System.Collections.Generic;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Map;
using ClassicUO.Utility.Platforms;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game
{
    internal static class World
    {
        private static readonly EffectManager _effectManager = new EffectManager();
        private static readonly List<Serial> _toRemove = new List<Serial>();

        public static Point RangeSize;

        public static ObjectPropertiesListManager OPL { get; } = new ObjectPropertiesListManager();

        public static CorpseManager CorpseManager { get; } = new CorpseManager();

        public static PartyManager Party { get; } = new PartyManager();

        public static HouseManager HouseManager { get; } = new HouseManager();

        public static EntityCollection<Item> Items { get; } = new EntityCollection<Item>();

        public static EntityCollection<Mobile> Mobiles { get; } = new EntityCollection<Mobile>();

        public static PlayerMobile Player { get; set; }

        public static Map.Map Map { get; private set; }

        public static byte ClientViewRange { get; set; } = Constants.MAX_VIEW_RANGE;

        public static bool SkillsRequested { get; set; }

        public static Seasons Season { get; private set; } = Seasons.Summer;
        public static Seasons OldSeason { get; set; } = Seasons.Summer;

        public static int OldMusicIndex { get; set; }

        public static WorldTextManager WorldTextManager { get; } = new WorldTextManager();

        public static JournalManager Journal { get; } = new JournalManager();

        public static int MapIndex
        {
            get => Map?.Index ?? -1;
            set
            {
                if (MapIndex != value)
                {
                    InternalMapChangeClear(true);

                    if (value < 0 && Map != null)
                    {
                        Map.Destroy();
                        Map = null;

                        return;
                    }

                    if (Map != null)
                    {
                        if (MapIndex >= 0) Map.Destroy();

                        Position position = Player.Position;
                        Map = null;

                        Map = new Map.Map(value)
                        {
                            Center = new Point(position.X, position.Y)
                        };
                        Map.Initialize();

                        Player.Position = position;
                        Player.AddToTile();

                        Player.ClearSteps();
                        Player.ProcessDelta();
                    }
                    else
                    {
                        Map = new Map.Map(value);

                        if (Player != null)
                            Map.Center = new Point(Player.X, Player.Y);
                        Map.Initialize();
                    }

                    UoAssist.SignalMapChanged(value);
                }
            }
        }

        public static bool InGame => Player != null && Map != null;

        public static IsometricLight Light { get; } = new IsometricLight
        {
            Overall = 0, Personal = 0, RealOverall = 0, RealPersonal = 0
        };

        public static LockedFeatures ClientLockedFeatures { get; } = new LockedFeatures();

        public static ClientFeatures ClientFeatures { get; } = new ClientFeatures();

        public static string ServerName { get; set; }


        public static void ChangeSeason(Seasons season, int music)
        {
            Season = season;

            foreach (int i in Map.GetUsedChunks())
            {
                Chunk chunk = Map.Chunks[i];

                for (int x = 0; x < 8; x++)
                {
                    for (int y = 0; y < 8; y++)
                    {
                        Tile tile = chunk.Tiles[x, y];

                        for (GameObject obj = tile.FirstNode; obj != null; obj = obj.Right)
                        {
                            obj.UpdateGraphicBySeason();
                        }
                    }
                }
            }

            if (Engine.Profile.Current.EnableCombatMusic)
                Engine.SceneManager.CurrentScene.Audio.PlayMusic(music);
        }


        public static void Update(double totalMS, double frameMS)
        {
            if (Player != null)
            {
                foreach (Mobile mob in Mobiles)
                {
                    mob.Update(totalMS, frameMS);

                    if (mob.Distance > ClientViewRange)
                        RemoveMobile(mob);

                    if (mob.IsDestroyed)
                        _toRemove.Add(mob);
                }

                if (_toRemove.Count != 0)
                {
                    for (int i = 0; i < _toRemove.Count; i++)
                        Mobiles.Remove(_toRemove[i]);

                    Mobiles.ProcessDelta();
                    _toRemove.Clear();
                }

                foreach (Item item in Items)
                {
                    item.Update(totalMS, frameMS);

                    if (item.OnGround && item.Distance > ClientViewRange)
                    {
                        if (item.IsMulti)
                        {
                            if (HouseManager.TryToRemove(item, ClientViewRange))
                                RemoveItem(item);
                        }
                        else
                            RemoveItem(item);
                    }

                    if (item.IsDestroyed)
                        _toRemove.Add(item);
                }

                if (_toRemove.Count != 0)
                {
                    for (int i = 0; i < _toRemove.Count; i++)
                        Items.Remove(_toRemove[i]);

                    Items.ProcessDelta();
                    _toRemove.Clear();
                }

                _effectManager.Update(totalMS, frameMS);

                WorldTextManager.Update(totalMS, frameMS);
            }
        }

        public static bool Contains(Serial serial)
        {
            if (serial.IsItem) return Items.Contains(serial);

            return serial.IsMobile && Mobiles.Contains(serial);
        }

        public static Entity Get(Serial serial)
        {
            if (serial.IsItem) return Items.Get(serial);

            return serial.IsMobile ? Mobiles.Get(serial) : null;
        }

        public static Item GetOrCreateItem(Serial serial)
        {
            Item item = Items.Get(serial);

            if (item == null /*|| item.IsDestroyed*/)
            {
                //Items.Remove(serial);
                item = Item.Create(serial);
            }

            return item;
        }

        public static Mobile GetOrCreateMobile(Serial serial)
        {
            Mobile mob = Mobiles.Get(serial);

            if (mob == null /*|| mob.IsDestroyed*/)
            {
                //Mobiles.Remove(serial);
                mob = new Mobile(serial);
            }

            return mob;
        }

        public static bool RemoveItem(Serial serial, bool forceRemove = false)
        {
            Item item = Items.Get(serial);

            if (item == null)
                return false;

            if (item.Layer != Layer.Invalid)
            {
                Entity e = Get(item.RootContainer);

                if (e != null && e.HasEquipment)
                {
                    int index = (int) item.Layer;

                    if (index >= 0 && index < e.Equipment.Length)
                        e.Equipment[index] = null;
                }
            }

            foreach (Item i in item.Items)
                RemoveItem(i, forceRemove);


            item.Items.Clear();
            item.Destroy();

            if (forceRemove)
                Items.Remove(serial);

            return true;
        }

        public static bool RemoveMobile(Serial serial, bool forceRemove = false)
        {
            Mobile mobile = Mobiles.Get(serial);

            if (mobile == null)
                return false;

            foreach (Item i in mobile.Items)
                RemoveItem(i, forceRemove);

            mobile.Items.Clear();
            mobile.Destroy();

            if (forceRemove)
                Mobiles.Remove(serial);

            return true;
        }

        internal static void AddEffect(GameEffect effect)
        {
            _effectManager.Add(effect);
        }

        public static void AddEffect(GraphicEffectType type, Serial source, Serial target, Graphic graphic, Hue hue, Position srcPos, Position targPos, byte speed, int duration, bool fixedDir, bool doesExplode, bool hasparticles, GraphicEffectBlendMode blendmode)
        {
            _effectManager.Add(type, source, target, graphic, hue, srcPos, targPos, speed, duration, fixedDir, doesExplode, hasparticles, blendmode);
        }

        public static void Clear()
        {
            HouseManager.Clear();
            Items.Clear();
            Mobiles.Clear();
            Player.Destroy();
            Player = null;
            Map.Destroy();
            Map = null;
            Light.Overall = Light.RealOverall = 0;
            Light.Personal = Light.RealPersonal = 0;
            ClientFeatures.SetFlags(0);
            ClientLockedFeatures.SetFlags(0);
            HouseManager.Clear();
            Party.Clear();
            ServerName = string.Empty;
            TargetManager.LastAttack = 0;
            Chat.PromptData = default;
            _effectManager.Clear();
            _toRemove.Clear();
            CorpseManager.Clear();
            OPL.Clear();

            Season = Seasons.Summer;
            OldSeason = Seasons.Summer;

            Journal.Clear();
            WorldTextManager.Clear();
        }

        private static void InternalMapChangeClear(bool noplayer)
        {
            if (!noplayer)
            {
                Map.Destroy();
                Map = null;
                Player.Destroy();
                Player = null;
            }

            foreach (Item item in Items)
            {
                if (noplayer && Player != null && !Player.IsDestroyed)
                {
                    if (item.RootContainer == Player)
                        continue;
                }

                RemoveItem(item);
            }

            foreach (Mobile mob in Mobiles)
            {
                if (noplayer && Player != null && !Player.IsDestroyed)
                {
                    if (mob == Player)
                        continue;
                }

                RemoveMobile(mob);
            }
        }
    }
}