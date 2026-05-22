// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using ClassicUO.Game.GameObjects;
using ClassicUO.Renderer;

namespace ClassicUO.Game.WorldText
{
    /// <summary>
    /// Owns the per-mobile floating-damage state. Pure state + ticking +
    /// drawing — no EventSink wiring. The owning facade
    /// (<see cref="Managers.WorldTextManager"/>) routes
    /// <see cref="Events.EventSink.DamageReceived"/> here.
    /// </summary>
    internal sealed class DamageOverlay : IDamageOverlay
    {
        private readonly World _world;
        private readonly Dictionary<uint, OverheadDamage> _damages = new Dictionary<uint, OverheadDamage>();
        private readonly List<Tuple<uint, uint>> _subst = new List<Tuple<uint, uint>>();
        private readonly List<uint> _toRemoveDamages = new List<uint>();

        public DamageOverlay(World world)
        {
            _world = world;
        }

        public void AddDamage(uint serial, int damage)
        {
            if (!_damages.TryGetValue(serial, out OverheadDamage dm) || dm == null)
            {
                dm = new OverheadDamage(_world, _world.Get(serial));
                _damages[serial] = dm;
            }

            dm.Add(damage);
        }

        public void Update()
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

            if (_toRemoveDamages.Count > 0)
            {
                foreach (uint s in _toRemoveDamages)
                {
                    _damages.Remove(s);
                }

                _toRemoveDamages.Clear();
            }
        }

        public void Draw(UltimaBatcher2D batcher, float layerDepth)
        {
            foreach (KeyValuePair<uint, OverheadDamage> overheadDamage in _damages)
            {
                Entity mob = _world.Get(overheadDamage.Key);

                if (mob == null || mob.IsDestroyed)
                {
                    uint ser = overheadDamage.Key | 0x8000_0000;

                    if (_world.CorpseManager.Exists(0, ser))
                    {
                        Item item = _world.CorpseManager.GetCorpseObject(ser);

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

                overheadDamage.Value.Draw(batcher, layerDepth);
            }
        }

        public void Clear()
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
        }
    }
}
