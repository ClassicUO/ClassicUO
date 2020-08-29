﻿#region license
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

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Map;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Utility.Platforms;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game
{
    enum SCAN_TYPE_OBJECT
    {
        STO_HOSTILE = 0,
        STO_PARTY,
        STO_FOLLOWERS,
        STO_OBJECTS,
        STO_MOBILES
    }
    enum SCAN_MODE_OBJECT
    {
        SMO_NEXT = 0,
        SMO_PREV,
        SMO_NEAREST
    }

    internal static class World
    {
        private static readonly EffectManager _effectManager = new EffectManager();
        private static readonly List<uint> _toRemove = new List<uint>();
        private static uint _time_to_delete;

        public static Point RangeSize;

        public static ObjectPropertiesListManager OPL { get; } = new ObjectPropertiesListManager();

        public static CorpseManager CorpseManager { get; } = new CorpseManager();

        public static PartyManager Party { get; } = new PartyManager();

        public static HouseManager HouseManager { get; } = new HouseManager();

        public static EntityCollection<Item> Items { get; } = new EntityCollection<Item>();

        public static EntityCollection<Mobile> Mobiles { get; } = new EntityCollection<Mobile>();

        public static PlayerMobile Player;

        public static Map.Map Map { get; private set; }

        public static byte ClientViewRange { get; set; } = Constants.MAX_VIEW_RANGE;

        public static bool SkillsRequested { get; set; }

        public static Seasons Season { get; private set; } = Seasons.Summer;
        public static Seasons OldSeason { get; set; } = Seasons.Summer;

        public static int OldMusicIndex { get; set; }

        public static WorldTextManager WorldTextManager { get; } = new WorldTextManager();

        public static JournalManager Journal { get; } = new JournalManager();

        public static HouseCustomizationManager CustomHouseManager;

        public static WorldMapEntityManager WMapManager = new WorldMapEntityManager();

        public static ActiveSpellIconsManager ActiveSpellIcons = new ActiveSpellIconsManager();

        public static uint LastObject, ObjectToRemove;


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
                        if (MapIndex >= 0)
                            Map.Destroy();

                        ushort x = Player.X;
                        ushort y = Player.Y;
                        sbyte z = Player.Z;

                        Map = null;

                        if (value >= Constants.MAPS_COUNT)
                            value = 0;

                        Map = new Map.Map(value);
                        Map.Initialize();

                        Player.X = x;
                        Player.Y = y;
                        Player.Z = z;
                        Player.UpdateScreenPosition();
                        Player.AddToTile();

                        Player.ClearSteps();
                    }
                    else
                    {
                        Map = new Map.Map(value);
                        Map.Initialize();
                    }

                    UoAssist.SignalMapChanged(value);
                }
            }
        }

        public static bool InGame => Player != null && Map != null;

        public static IsometricLight Light { get; } = new IsometricLight
        {
            Overall = 0,
            Personal = 0,
            RealOverall = 0,
            RealPersonal = 0
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
                        for (GameObject obj = chunk.GetHeadObject(x, y); obj != null; obj = obj.TNext)
                        {
                            obj.UpdateGraphicBySeason();
                        }
                    }
                }
            }

            Client.Game.Scene.Audio.PlayMusic(music, true);
        }


        /*[MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool CheckToRemove(Entity obj, int distance)
        {
            if (Player == null || obj.Serial == Player.Serial)
                return false;

            return Math.Max(Math.Abs(obj.X - RangeSize.X), Math.Abs(obj.Y - RangeSize.Y)) > distance;
        }
        */

        public static void Update(double totalMS, double frameMS)
        {
            if (Player != null)
            {
                if (SerialHelper.IsValid(ObjectToRemove))
                {
                    Item rem = Items.Get(ObjectToRemove);
                    ObjectToRemove = 0;

                    if (rem != null)
                    {
                        Entity container = Get(rem.Container);

                        RemoveItem(rem, true);

                        if (rem.Layer == Layer.OneHanded || rem.Layer == Layer.TwoHanded)
                            Player.UpdateAbilities();
                    
                        if (container != null)
                        {
                            if (SerialHelper.IsMobile(container.Serial))
                            {
                                UIManager.GetGump<PaperDollGump>(container.Serial)?.RequestUpdateContents();
                            }
                            else if (SerialHelper.IsItem(container.Serial))
                            {
                                UIManager.GetGump<ContainerGump>(container.Serial)?.RequestUpdateContents();

                                if (container.Graphic == 0x2006)
                                {
                                    UIManager.GetGump<GridLootGump>(container)?.RequestUpdateContents();
                                }
                            }
                        }
                    }
                }

                bool do_delete = _time_to_delete < Time.Ticks;

                if (do_delete)
                {
                    _time_to_delete = Time.Ticks + 50;
                }

                foreach (Mobile mob in Mobiles)
                {
                    mob.Update(totalMS, frameMS);

                    if (do_delete && mob.Distance > ClientViewRange /*CheckToRemove(mob, ClientViewRange)*/)
                        RemoveMobile(mob);

                    if (mob.IsDestroyed)
                        _toRemove.Add(mob);
                    else
                    {
                        if (mob.NotorietyFlag == NotorietyFlag.Ally)
                        {
                            WMapManager.AddOrUpdate(
                                                    mob.Serial,
                                                    mob.X,
                                                    mob.Y,
                                                    Utility.MathHelper.PercetangeOf(mob.Hits, mob.HitsMax),
                                                    MapIndex,
                                                    true,
                                                    mob.Name);
                        }
                        else if (Party.Leader != 0 && Party.Contains(mob))
                        {
                            WMapManager.AddOrUpdate(
                                                    mob.Serial,
                                                    mob.X,
                                                    mob.Y,
                                                    Utility.MathHelper.PercetangeOf(mob.Hits, mob.HitsMax),
                                                    MapIndex,
                                                    false,
                                                    mob.Name);
                        }
                    }
                }

                if (_toRemove.Count != 0)
                {
                    for (int i = 0; i < _toRemove.Count; i++)
                        Mobiles.Remove(_toRemove[i]);

                    _toRemove.Clear();
                }

                foreach (Item item in Items)
                {
                    item.Update(totalMS, frameMS);

                    if (do_delete && item.OnGround && item.Distance > ClientViewRange /*CheckToRemove(item, ClientViewRange)*/)
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

                    _toRemove.Clear();
                }

                _effectManager.Update(totalMS, frameMS);

                WorldTextManager.Update(totalMS, frameMS);
                WMapManager.RemoveUnupdatedWEntity();
            }
        }

        public static bool Contains(uint serial)
        {
            if (SerialHelper.IsItem(serial))
                return Items.Contains(serial);

            return SerialHelper.IsMobile(serial) && Mobiles.Contains(serial);
        }

        public static Entity Get(uint serial)
        {
            Entity ent;

            if (SerialHelper.IsMobile(serial))
            {
                ent = Mobiles.Get(serial);

                if (ent == null)
                {
                    ent = Items.Get(serial);
                }
            }
            else
            {
                ent = Items.Get(serial);

                if (ent == null)
                {
                    ent = Mobiles.Get(serial);
                }
            }

            if (ent != null && ent.IsDestroyed)
                ent = null;

            return ent;
        }

        public static Item GetOrCreateItem(uint serial)
        {
            Item item = Items.Get(serial);

            if (item != null && item.IsDestroyed)
            {
                Items.Remove(serial);
                item = null;
            }

            if (item == null /*|| item.IsDestroyed*/)
            {
                item = Item.Create(serial);
                Items.Add(item);
            }

            return item;
        }

        public static Mobile GetOrCreateMobile(uint serial)
        {
            Mobile mob = Mobiles.Get(serial);

            if (mob != null && mob.IsDestroyed)
            {
                Mobiles.Remove(serial);
                mob = null;
            }

            if (mob == null /*|| mob.IsDestroyed*/)
            {
                mob = Mobile.Create(serial);
                Mobiles.Add(mob);
            }

            return mob;
        }

        public static void RemoveItemFromContainer(uint serial)
        {
            Item it = Items.Get(serial);

            if (it != null)
            {
                RemoveItemFromContainer(it);
            }
        }

        public static void RemoveItemFromContainer(Item obj)
        {
            uint containerSerial = obj.Container;

            if (SerialHelper.IsValid(containerSerial))
            {
                if (SerialHelper.IsMobile(containerSerial))
                {
                    UIManager.GetGump<PaperDollGump>(containerSerial)?.RequestUpdateContents();
                }
                else if (SerialHelper.IsItem(containerSerial))
                {
                    UIManager.GetGump<ContainerGump>(containerSerial)?.RequestUpdateContents();
                }

                Entity container = World.Get(containerSerial);

                if (container != null)
                {
                    container.Remove(obj);
                }

                obj.Container = 0xFFFF_FFFF;
            }

            obj.Next = null;
            obj.Previous = null;
            obj.RemoveFromTile();
        }

        public static bool RemoveItem(uint serial, bool forceRemove = false)
        {
            Item item = Items.Get(serial);

            if (item == null || item.IsDestroyed)
                return false;

            LinkedObject first = item.Items;
            RemoveItemFromContainer(item);

            while (first != null)
            {
                LinkedObject next = first.Next;

                RemoveItem(first as Item, forceRemove);

                first = next;
            }

            item.Destroy();

            if (forceRemove)
                Items.Remove(serial);

            return true;
        }

        public static bool RemoveMobile(uint serial, bool forceRemove = false)
        {
            Mobile mobile = Mobiles.Get(serial);

            if (mobile == null || mobile.IsDestroyed)
                return false;

            LinkedObject first = mobile.Items;

            while (first != null)
            {
                LinkedObject next = first.Next;

                RemoveItem(first as Item, forceRemove);

                first = next;
            }

            mobile.Destroy();

            if (forceRemove)
                Mobiles.Remove(serial);

            return true;
        }

        internal static void AddEffect(GameEffect effect)
        {
            _effectManager.Add(effect);
        }

        internal static void RemoveEffect(GameEffect effect)
        {
            _effectManager.RemoveEffect(effect);
        }

        public static void AddEffect(GraphicEffectType type, uint source, uint target,
                                     ushort graphic, ushort hue,
                                     ushort srcX, ushort srcY, sbyte srcZ,
                                     ushort targetX, ushort targetY, sbyte targetZ,
                                     byte speed, int duration, bool fixedDir, bool doesExplode, bool hasparticles, GraphicEffectBlendMode blendmode)
        {
            _effectManager.Add(type, source, target, graphic, hue, srcX, srcY, srcZ, targetX, targetY, targetZ, speed, duration, fixedDir, doesExplode, hasparticles, blendmode);
        }

        public static uint SearchObject(uint serial, SCAN_TYPE_OBJECT scanType, SCAN_MODE_OBJECT scanMode)
        {
            Entity first = null, selected = null;
            int distance = int.MaxValue;
            bool currentTargetFound = false;

            if (scanType == SCAN_TYPE_OBJECT.STO_OBJECTS)
            {
                if (scanMode == SCAN_MODE_OBJECT.SMO_NEAREST)
                {
                    foreach (Item item in Items)
                    {
                        if (item.IsMulti || item.IsDestroyed || !item.OnGround)
                            continue;

                        int dist = item.Distance;

                        if (dist < distance)
                            distance = dist;
                    }
                }

                foreach (Item item in Items)
                {
                    if (item.IsMulti || item.IsDestroyed || !item.OnGround)
                        continue;

                    if (!SerialHelper.IsValid(serial))
                        return item;

                    if (scanMode == SCAN_MODE_OBJECT.SMO_NEXT)
                    {
                        if (serial == item)
                        {
                            currentTargetFound = true;
                            continue;
                        }

                        if (first == null)
                            first = item;

                        if (currentTargetFound)
                        {
                            selected = item;
                            break;
                        }
                    }
                    else if (scanMode == SCAN_MODE_OBJECT.SMO_PREV)
                    {
                        if (!currentTargetFound && first != null)
                            selected = first;

                        if (serial == item)
                        {
                            currentTargetFound = true;
                            continue;
                        }

                        first = item;
                    }
                    else if (scanMode == SCAN_MODE_OBJECT.SMO_NEAREST)
                    {
                        if (item.Distance > distance)
                            continue;

                        if (serial == item.Serial)
                        {
                            currentTargetFound = true;
                            continue;
                        }

                        if (first == null)
                            first = item;

                        if (currentTargetFound)
                        {
                            selected = item;
                            break;
                        }
                    }
                    else
                        break;
                }
            }
            else
            {
                if (scanMode == SCAN_MODE_OBJECT.SMO_NEAREST)
                {
                    foreach (Mobile mobile in Mobiles)
                    {
                        if (mobile.IsDestroyed || mobile == Player)
                            continue;

                        if (scanType == SCAN_TYPE_OBJECT.STO_PARTY)
                        {
                            if (!Party.Contains(mobile))
                            {
                                continue;
                            }
                        }
                        else if (scanType == SCAN_TYPE_OBJECT.STO_FOLLOWERS)
                        {
                            if (!(mobile.IsRenamable &&
                                  mobile.NotorietyFlag != NotorietyFlag.Invulnerable &&
                                  mobile.NotorietyFlag != NotorietyFlag.Enemy))
                            {
                                continue;
                            }
                        }
                        else if (scanType == SCAN_TYPE_OBJECT.STO_HOSTILE)
                        {
                            if (mobile.NotorietyFlag == NotorietyFlag.Ally ||
                                mobile.NotorietyFlag == NotorietyFlag.Innocent ||
                                mobile.NotorietyFlag == NotorietyFlag.Invulnerable)
                            {
                                continue;
                            }
                        }

                        int dist = mobile.Distance;

                        if (dist < distance)
                            distance = dist;
                    }
                }

                foreach (Mobile mobile in Mobiles)
                {
                    if (mobile.IsDestroyed || mobile == Player)
                        continue;

                    if (scanMode == SCAN_MODE_OBJECT.SMO_NEXT)
                    {
                        if (scanType == SCAN_TYPE_OBJECT.STO_PARTY)
                        {
                            if (!Party.Contains(mobile))
                            {
                                continue;
                            }
                        }
                        else if (scanType == SCAN_TYPE_OBJECT.STO_FOLLOWERS)
                        {
                            if (!(mobile.IsRenamable &&
                                mobile.NotorietyFlag != NotorietyFlag.Invulnerable &&
                                mobile.NotorietyFlag != NotorietyFlag.Enemy))
                            {
                                continue;
                            }
                        }
                        else if (scanType == SCAN_TYPE_OBJECT.STO_HOSTILE)
                        {
                            if (mobile.NotorietyFlag == NotorietyFlag.Ally ||
                                mobile.NotorietyFlag == NotorietyFlag.Innocent ||
                                mobile.NotorietyFlag == NotorietyFlag.Invulnerable)
                            {
                                continue;
                            }
                        }

                        if (serial == mobile)
                        {
                            currentTargetFound = true;
                            continue;
                        }

                        if (first == null)
                            first = mobile;

                        if (currentTargetFound)
                        {
                            selected = mobile;
                            break;
                        }
                    }
                    else if (scanMode == SCAN_MODE_OBJECT.SMO_PREV)
                    {
                        if (scanType == SCAN_TYPE_OBJECT.STO_PARTY)
                        {
                            if (!Party.Contains(mobile))
                            {
                                continue;
                            }
                        }
                        else if (scanType == SCAN_TYPE_OBJECT.STO_FOLLOWERS)
                        {
                            if (!(mobile.IsRenamable &&
                                  mobile.NotorietyFlag != NotorietyFlag.Invulnerable &&
                                  mobile.NotorietyFlag != NotorietyFlag.Enemy))
                            {
                                continue;
                            }
                        }
                        else if (scanType == SCAN_TYPE_OBJECT.STO_HOSTILE)
                        {
                            if (mobile.NotorietyFlag == NotorietyFlag.Ally ||
                                mobile.NotorietyFlag == NotorietyFlag.Innocent ||
                                mobile.NotorietyFlag == NotorietyFlag.Invulnerable)
                            {
                                continue;
                            }
                        }

                        if (!currentTargetFound && first != null)
                            selected = first;

                        if (serial == mobile)
                        {
                            currentTargetFound = true;
                            continue;
                        }

                        first = mobile;
                    }
                    else if (scanMode == SCAN_MODE_OBJECT.SMO_NEAREST)
                    {
                        if (scanType == SCAN_TYPE_OBJECT.STO_PARTY)
                        {
                            if (!Party.Contains(mobile))
                            {
                                continue;
                            }
                        }
                        else if (scanType == SCAN_TYPE_OBJECT.STO_FOLLOWERS)
                        {
                            if (!(mobile.IsRenamable &&
                                  mobile.NotorietyFlag != NotorietyFlag.Invulnerable &&
                                  mobile.NotorietyFlag != NotorietyFlag.Enemy))
                            {
                                continue;
                            }
                        }
                        else if (scanType == SCAN_TYPE_OBJECT.STO_HOSTILE)
                        {
                            if (mobile.NotorietyFlag == NotorietyFlag.Ally ||
                                mobile.NotorietyFlag == NotorietyFlag.Innocent ||
                                mobile.NotorietyFlag == NotorietyFlag.Invulnerable)
                            {
                                continue;
                            }
                        }

                        if (mobile.Distance > distance)
                            continue;

                        if (serial == mobile.Serial)
                        {
                            currentTargetFound = true;
                            continue;
                        }

                        if (first == null)
                            first = mobile;

                        if (currentTargetFound)
                        {
                            selected = mobile;
                            break;
                        }
                    }
                    else
                        break;
                }
            }

            if (first != null && selected == null)
            {
                return first;
            }

            return selected?.Serial ?? 0;
        }

        public static void Clear()
        {
            foreach (Mobile mobile in Mobiles)
            {
                RemoveMobile(mobile);
            }

            foreach (Item item in Items)
            {
                RemoveItem(item);
            }

            ObjectToRemove = 0;
            LastObject = 0;
            Items.Clear();
            Mobiles.Clear();
            Player?.Destroy();
            Player = null;
            Map?.Destroy();
            Map = null;
            Light.Overall = Light.RealOverall = 0;
            Light.Personal = Light.RealPersonal = 0;
            ClientFeatures.SetFlags(0);
            ClientLockedFeatures.SetFlags(0);
            Party?.Clear();
            TargetManager.LastAttack = 0;
            MessageManager.PromptData = default;
            _effectManager.Clear();
            _toRemove.Clear();
            CorpseManager.Clear();
            OPL.Clear();
            WMapManager.Clear();
            HouseManager?.Clear();

            Season = Seasons.Summer;
            OldSeason = Seasons.Summer;

            Journal.Clear();
            WorldTextManager.Clear();
            ActiveSpellIcons.Clear();

            SkillsRequested = false;
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

                if (item.OnGround && item.IsMulti)
                {
                    HouseManager.Remove(item.Serial);
                }

                _toRemove.Add(item);
            }

            foreach (uint serial in _toRemove)
            {
                RemoveItem(serial, true);
            }

            _toRemove.Clear();

            foreach (Mobile mob in Mobiles)
            {
                if (noplayer && Player != null && !Player.IsDestroyed)
                {
                    if (mob == Player)
                        continue;
                }

                _toRemove.Add(mob);
            }

            foreach (uint serial in _toRemove)
            {
                RemoveMobile(serial, true);
            }

            _toRemove.Clear();
        }
    }
}