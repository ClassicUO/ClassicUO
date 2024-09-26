#region license

// Copyright (c) 2024, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Game.Managers;
using ClassicUO.Utility.Collections;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    public sealed class House : IEquatable<uint>
    {
        private readonly World _world;

        public House(World world, uint serial, uint revision, bool isCustom)
        {
            _world = world;
            Serial = serial;
            Revision = revision;
            IsCustom = isCustom;
        }

        public uint Serial { get; }
        public List<Multi> Components { get; } = new List<Multi>();

        public Rectangle Bounds;

        public bool Equals(uint other)
        {
            return Serial == other;
        }

        public bool IsCustom;
        public uint Revision;

        public IEnumerable<Multi> GetMultiAt(int x, int y)
        {
            return Components.Where(s => !s.IsDestroyed && s.X == x && s.Y == y);
        }

        public Multi Add
        (
            ushort graphic,
            ushort hue,
            ushort x,
            ushort y,
            sbyte z,
            bool iscustom,
            bool ismovable
        )
        {
            Multi m = Multi.Create(_world, graphic);
            m.Hue = hue;
            m.IsCustom = iscustom;
            m.IsMovable = ismovable;

            m.SetInWorldTile(x, y, z);

            Components.Add(m);

            return m;
        }

        public void ClearCustomHouseComponents(CUSTOM_HOUSE_MULTI_OBJECT_FLAGS state)
        {
            Item item = _world.Items.Get(Serial);

            if (item != null)
            {
                int checkZ = item.Z + 7;

                for (int i = 0; i < Components.Count; i++)
                {
                    Multi component = Components[i];

                    component.State = component.State & ~(CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_TRANSPARENT | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_IGNORE_IN_RENDER | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE);


                    if (component.IsCustom)
                    {
                        if (component.Z <= item.Z)
                        {
                            if ((component.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_STAIR) == 0)
                            {
                                component.State |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_DONT_REMOVE;
                            }
                        }

                        if (state == 0 || (component.State & state) != 0)
                        {
                            component.Destroy();
                        }
                    }
                    else if (component.Z <= checkZ)
                    {
                        component.State = component.State | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_IGNORE_IN_RENDER;
                    }

                    if (component.IsDestroyed)
                    {
                        Components.RemoveAt(i--);
                    }
                }
            }
        }

        public void Generate(bool recalculate = false, bool pushtotile = true, bool removePreview = false)
        {
            Item item = _world.Items.Get(Serial);
            //ClearCustomHouseComponents(0);

            foreach (Multi s in Components)
            {
                if (item != null)
                {
                    if (recalculate)
                    {
                        s.SetInWorldTile((ushort)(item.X + s.MultiOffsetX), (ushort)(item.Y + s.MultiOffsetY), (sbyte)(item.Z + s.MultiOffsetZ));
                        s.Offset = Vector3.Zero;
                    }


                    if (removePreview)
                    {
                        s.State &= ~CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_PREVIEW;
                    }

                    s.Hue = item.Hue;
                    //s.State = CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE;
                    //s.IsCustom = IsCustom;
                }

                if (!pushtotile)
                {
                    s.RemoveFromTile();
                }
            }

            _world.CustomHouseManager?.GenerateFloorPlace();
        }

        public void ClearComponents(bool removeCustomOnly = false)
        {
            Item item = _world.Items.Get(Serial);

            if (item != null && !item.IsDestroyed)
            {
                item.WantUpdateMulti = true;
            }

            for (int i = 0; i < Components.Count; i++)
            {
                Multi s = Components[i];

                if (!s.IsCustom && removeCustomOnly)
                {
                    continue;
                }

                s.Destroy();
                Components.RemoveAt(i--);
            }

            //Components.Clear();
        }
    }
}