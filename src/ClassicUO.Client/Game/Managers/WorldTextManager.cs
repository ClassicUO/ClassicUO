// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using ClassicUO.Game.GameObjects;
using ClassicUO.Renderer;

namespace ClassicUO.Game.Managers
{
    internal class WorldTextManager : TextRenderer
    {
        private readonly Dictionary<uint, OverheadDamage> _damages = new Dictionary<uint, OverheadDamage>();
        private readonly List<Tuple<uint, uint>> _subst = new List<Tuple<uint, uint>>();
        private readonly List<uint> _toRemoveDamages = new List<uint>();

        public WorldTextManager(World world) : base(world) { }

        public override void Update()
        {
            base.Update();


            UpdateDamageOverhead();

            if (_toRemoveDamages.Count > 0)
            {
                foreach (uint s in _toRemoveDamages)
                {
                    _damages.Remove(s);
                }

                _toRemoveDamages.Clear();
            }
        }


        public override void Draw(UltimaBatcher2D batcher, int startX, int startY, bool isGump = false)
        {
            base.Draw
            (
                batcher,
                startX,
                startY,
                isGump
            );

            foreach (KeyValuePair<uint, OverheadDamage> overheadDamage in _damages)
            {
                Entity mob = World.Get(overheadDamage.Key);

                if (mob == null || mob.IsDestroyed)
                {
                    uint ser = overheadDamage.Key | 0x8000_0000;

                    if (World.CorpseManager.Exists(0, ser))
                    {
                        Item item = World.CorpseManager.GetCorpseObject(ser);

                        if (item != null && !ReferenceEquals(item, overheadDamage.Value.Parent))
                        {
                            _subst.Add(Tuple.Create(overheadDamage.Key, item.Serial));
                            overheadDamage.Value.SetParent(item);
                        }
                    }
                    else
                    {
                        continue;
                    }
                }

                overheadDamage.Value.Draw(batcher);
            }
        }

        private void UpdateDamageOverhead()
        {
            if (_subst.Count != 0)
            {
                foreach (Tuple<uint, uint> tuple in _subst)
                {
                    if (_damages.TryGetValue(tuple.Item1, out OverheadDamage dmg))
                    {
                        _damages.Remove(tuple.Item1);
                        _damages[tuple.Item2] = dmg;
                    }
                }

                _subst.Clear();
            }

            foreach (KeyValuePair<uint, OverheadDamage> overheadDamage in _damages)
            {
                overheadDamage.Value.Update();

                if (overheadDamage.Value.IsEmpty)
                {
                    _toRemoveDamages.Add(overheadDamage.Key);
                }
            }
        }


        internal void AddDamage(uint obj, int dmg)
        {
            if (!_damages.TryGetValue(obj, out OverheadDamage dm) || dm == null)
            {
                dm = new OverheadDamage(World, World.Get(obj));
                _damages[obj] = dm;
            }

            dm.Add(dmg);
        }

        public override void Clear()
        {
            if (_toRemoveDamages.Count > 0)
            {
                foreach (uint s in _toRemoveDamages)
                {
                    _damages.Remove(s);
                }

                _toRemoveDamages.Clear();
            }

            _subst.Clear();

            //_staticToUpdate.Clear();

            base.Clear();
        }
    }
}