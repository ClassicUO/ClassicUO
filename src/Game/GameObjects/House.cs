﻿#region license

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

using System;
using System.Collections.Generic;

using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Utility.Collections;

namespace ClassicUO.Game.GameObjects
{
    internal sealed class House : IEquatable<Serial>
    {
        public House(Serial serial, uint revision, bool isCustom)
        {
            Serial = serial;
            Revision = revision;
            IsCustom = isCustom;
        }

        public Serial Serial { get; }
        public uint Revision;
        public List<Multi> Components { get; } = new List<Multi>();
        public bool IsCustom;

        public Multi GetMultiAt(int x, int y)
        {
            foreach (Multi component in Components)
            {
                if (component.X == x && component.Y == y)
                    return component;
            }

            return null;
        }

        public Multi Add(ushort graphic, ushort hue, int x, int y, sbyte z, bool iscustom)
        {
            Item item = World.Items.Get(Serial);

            Multi m = Multi.Create(graphic);
            m.Hue = hue;
            m.Position = new Position((ushort) (item.X + x), (ushort) (item.Y +  y), z);
            m.IsCustom = iscustom;
            m.AddToTile();

            //if (iscustom)
            Components.Add(m);

            return m;
        }

        public void ClearCustomHouseComponents(CUSTOM_HOUSE_MULTI_OBJECT_FLAGS state)
        {
            Item item = World.Items.Get(Serial);

            if (item != null)
            {
                int checkZ = item.Z + 7;

                for (int i = 0; i < Components.Count; i++)
                {
                    var component = Components[i];

                    component.State = component.State & ~(CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_TRANSPARENT |
                                                          CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_IGNORE_IN_RENDER |
                                                          CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE |
                                                          CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE);


                    if (component.IsCustom)
                    {
                        if (component.Z <= item.Z)
                        {
                            if ((component.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_STAIR) == 0)
                                component.State |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_DONT_REMOVE;
                        }

                        if (((state == 0) || (component.State & state) != 0))
                        {
                            component.Destroy();
                            Components.RemoveAt(i--);
                        }
                    }
                    else if (component.Z <= checkZ)
                    {
                        component.State = component.State | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_IGNORE_IN_RENDER;
                    }
                }
            }
        }

        public bool Equals(Serial other)
        {
            return Serial == other;
        }

        public void Fill(RawList<CustomBuildObject> list)
        {
            Item item = World.Items.Get(Serial);

            ClearCustomHouseComponents(0);

            for (int i = 0; i < list.Count; i++)
            {
                ref var b = ref list[i];
                Add(b.Graphic, 0, b.X, b.Y, (sbyte) (item.Z + b.Z), true);
            }
        }

        public void Generate(bool recalculate = false)
        {
            Item item = World.Items.Get(Serial);
            //ClearCustomHouseComponents(0);

            foreach (Multi s in Components)
            {
                if (item != null)
                {
                    if (recalculate)
                        s.Position = new Position((ushort) (item.X + s.MultiOffsetX), (ushort) (item.Y + s.MultiOffsetY), (sbyte) (item.Z + s.MultiOffsetZ));
                    s.Hue = item.Hue;
                    //s.State = CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE;
                    //s.IsCustom = IsCustom;
                }

                s.AddToTile();
            }

            World.CustomHouseManager?.GenerateFloorPlace();
        }

        public void ClearComponents(bool removeCustomOnly = false)
        {
            Item item = World.Items.Get(Serial);

            if (item != null && !item.IsDestroyed)
                item.WantUpdateMulti = true;

            for (int i = 0; i < Components.Count; i++)
            {
                var s = Components[i];

                if (!s.IsCustom && removeCustomOnly)
                    continue;
                s.Destroy();
                Components.RemoveAt(i--);
            }

            //Components.Clear();
        }
    }
}