// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Houses;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Houses
{
    /// <summary>
    /// Facade over the three Houses collaborators: keeps the existing public
    /// surface (<c>_world.HouseManager.X</c>) and owns the EventSink
    /// subscriptions. Storage, geometry and revision-request queue live in
    /// dedicated cohesive classes under <see cref="Game.Houses"/>.
    /// </summary>
    internal sealed class HouseManager : IEventListener
    {
        private readonly IHouseStore _store;
        private readonly IHouseGeometry _geometry;
        private readonly IHouseRevisionTracker _revisions;
        private readonly World _world;

        /// <summary>Production composition root. Defaults to concrete collaborators.</summary>
        public HouseManager(World world)
            : this(world, new HouseStore(), new HouseRevisionTracker())
        {
        }

        /// <summary>Test / extension seam — inject a fake store / tracker.</summary>
        internal HouseManager(World world, IHouseStore store, IHouseRevisionTracker revisions)
            : this(world, store, new HouseGeometry(world, store), revisions)
        {
        }

        /// <summary>Full DI seam — inject all three collaborators.</summary>
        internal HouseManager(World world, IHouseStore store, IHouseGeometry geometry, IHouseRevisionTracker revisions)
        {
            _world = world;
            _store = store;
            _geometry = geometry;
            _revisions = revisions;
        }

        public void Subscribe()
        {
            EventSink.CustomHouseReceived += OnCustomHouseReceived;
            EventSink.HouseRevisionState += OnHouseRevisionState;
        }

        public void Unsubscribe()
        {
            EventSink.CustomHouseReceived -= OnCustomHouseReceived;
            EventSink.HouseRevisionState -= OnHouseRevisionState;
        }

        // ---- Storage facade ----
        public IReadOnlyCollection<House> Houses => _store.Houses;
        public bool TryGetHouse(uint serial, out House house) => _store.TryGetHouse(serial, out house);
        public bool Exists(uint serial) => _store.Exists(serial);
        public void Add(uint serial, House house) => _store.Add(serial, house);
        public void Remove(uint serial) => _store.Remove(serial);
        public void RemoveMultiTargetHouse() => _store.RemoveMultiTargetHouse();
        public void Clear() => _store.Clear();

        // ---- Geometry facade ----
        public bool IsHouseInRange(uint serial, int distance) => _geometry.IsHouseInRange(serial, distance);
        public bool EntityIntoHouse(uint house, GameObject obj) => _geometry.EntityIntoHouse(house, obj);
        public bool TryToRemove(uint serial, int distance) => _geometry.TryToRemove(serial, distance);

        // ---- Revision queue facade ----
        public void DrainRevisionRequests() => _revisions.Drain();

        // ---- Event handlers ----
        private void OnCustomHouseReceived(CustomHouseReceivedArgs e)
        {
            if (e.Components == null) return;

            Item foundation = _world.Items.Get(e.Serial);
            if (foundation == null) return;
            if (!foundation.IsMulti || foundation.MultiInfo is not Rectangle multi) return;

            if (multi.X == 0 && multi.Y == 0 && multi.Height == 0 && multi.Width == 0)
            {
                Log.Warn("[CustomHouse (0xD8) - Invalid multi dimentions. Maybe missing some installation required files");
                return;
            }

            if (!_store.TryGetHouse(e.Serial, out House house))
            {
                house = new House(_world, foundation, e.Revision, true);
                _store.Add(e.Serial, house);
            }
            else
            {
                house.ClearComponents(true);
                house.Revision = e.Revision;
                house.IsCustom = true;
            }

            house.ClearCustomHouseComponents(0);
            bool movable = foundation.ItemData.IsMultiMovable;

            foreach (var comp in e.Components)
            {
                house.Add(
                    comp.Graphic,
                    0,
                    (ushort)(foundation.X + comp.OffsetX),
                    (ushort)(foundation.Y + comp.OffsetY),
                    (sbyte)(foundation.Z + comp.OffsetZ),
                    true,
                    movable
                );
            }

            if (_world.CustomHouseManager != null)
            {
                _world.CustomHouseManager.GenerateFloorPlace();
                UIManager.GetGump<HouseCustomizationGump>(house.Serial)?.Update();
            }

            UIManager.GetGump<MiniMapGump>()?.RequestUpdateContents();

            if (_geometry.EntityIntoHouse(e.Serial, _world.Player))
            {
                Client.Game.GetScene<GameScene>()?.UpdateMaxDrawZ(true);
            }

            _world.BoatMovingManager.ClearSteps(e.Serial);
        }

        private void OnHouseRevisionState(HouseRevisionStateArgs e)
        {
            if (_world.Items.Get(e.Serial) == null)
            {
                _store.Remove(e.Serial);
            }

            if (!_store.TryGetHouse(e.Serial, out House house) || !house.IsCustom || house.Revision != e.Revision)
            {
                _revisions.Enqueue(e.Serial);
                return;
            }

            house.Generate();
            _world.BoatMovingManager.ClearSteps(e.Serial);
            UIManager.GetGump<MiniMapGump>()?.RequestUpdateContents();

            if (_geometry.EntityIntoHouse(e.Serial, _world.Player))
            {
                Client.Game.GetScene<GameScene>()?.UpdateMaxDrawZ(true);
            }
        }
    }
}
