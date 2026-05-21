// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Network;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Managers
{
    internal sealed class HouseManager
    {
        private readonly Dictionary<uint, House> _houses = new Dictionary<uint, House>();
        private readonly World _world;

        public HouseManager(World world)
        {
            _world = world;
            EventSink.CustomHouseReceived += OnCustomHouseReceived;
            EventSink.HouseRevisionState += OnHouseRevisionState;
        }

        public void Unsubscribe()
        {
            EventSink.CustomHouseReceived -= OnCustomHouseReceived;
            EventSink.HouseRevisionState -= OnHouseRevisionState;
        }

        public IReadOnlyCollection<House> Houses => _houses.Values;

        public void Add(uint serial, House revision)
        {
            _houses[serial] = revision;
        }

        public bool TryGetHouse(uint serial, out House house)
        {
            return _houses.TryGetValue(serial, out house);
        }

        public bool TryToRemove(uint serial, int distance)
        {
            if (!IsHouseInRange(serial, distance))
            {
                if (_houses.TryGetValue(serial, out House house))
                {
                    house.ClearComponents();
                    _houses.Remove(serial);
                }


                return true;
            }

            return false;
        }

        public bool IsHouseInRange(uint serial, int distance)
        {
            if (TryGetHouse(serial, out _))
            {
                int currX = _world.RangeSize.X;
                int currY = _world.RangeSize.Y;

                //if (World.Player.IsMoving)
                //{
                //    Mobile.Step step = World.Player.Steps.Back();

                //    currX = step.X;
                //    currY = step.Y;
                //}
                //else
                //{
                //    currX = World.Player.X;
                //    currY = World.Player.Y;
                //}

                Item found = _world.Items.Get(serial);

                if (found == null)
                {
                    return true;
                }

                distance += found.MultiDistanceBonus;

                return Math.Abs(found.X - currX) <= distance && Math.Abs(found.Y - currY) <= distance;
            }

            return false;
        }

        public bool EntityIntoHouse(uint house, GameObject obj)
        {
            if (obj != null && TryGetHouse(house, out _))
            {
                Item found = _world.Items.Get(house);

                if (found == null || !found.MultiInfo.HasValue)
                {
                    return true;
                }

                int minX = found.X + found.MultiInfo.Value.X;
                int maxX = found.X + found.MultiInfo.Value.Width;
                int minY = found.Y + found.MultiInfo.Value.Y;
                int maxY = found.Y + found.MultiInfo.Value.Height;

                return obj.X >= minX && obj.X <= maxX && obj.Y >= minY && obj.Y <= maxY;
            }

            return false;
        }

        public void Remove(uint serial)
        {
            if (TryGetHouse(serial, out House house))
            {
                house.ClearComponents();
                _houses.Remove(serial);
            }
        }

        public void RemoveMultiTargetHouse()
        {
            if (_houses.TryGetValue(0, out House house))
            {
                house.ClearComponents();
                _houses.Remove(0);
            }
        }

        public bool Exists(uint serial)
        {
            return _houses.ContainsKey(serial);
        }

        public void Clear()
        {
            foreach (KeyValuePair<uint, House> house in _houses)
            {
                house.Value.ClearComponents();
            }

            _houses.Clear();
        }

        private void OnCustomHouseReceived(CustomHouseReceivedArgs e)
        {
            uint serial = e.Serial;
            uint revision = e.Revision;
            IReadOnlyList<CustomHouseComponent> components = e.Components;

            if (components == null)
            {
                return;
            }

            Item foundation = _world.Items.Get(serial);

            if (foundation == null)
            {
                return;
            }

            Rectangle? multi = foundation.MultiInfo;

            if (!foundation.IsMulti || multi == null)
            {
                return;
            }

            short minX = (short)multi.Value.X;
            short minY = (short)multi.Value.Y;
            short maxY = (short)multi.Value.Height;

            if (minX == 0 && minY == 0 && maxY == 0 && multi.Value.Width == 0)
            {
                Log.Warn(
                    "[CustomHouse (0xD8) - Invalid multi dimentions. Maybe missing some installation required files"
                );

                return;
            }

            if (!TryGetHouse(foundation, out House house))
            {
                house = new House(_world, foundation, revision, true);
                Add(foundation, house);
            }
            else
            {
                house.ClearComponents(true);
                house.Revision = revision;
                house.IsCustom = true;
            }

            house.ClearCustomHouseComponents(0);

            bool ismovable = foundation.ItemData.IsMultiMovable;

            for (int i = 0; i < components.Count; i++)
            {
                CustomHouseComponent comp = components[i];

                house.Add(
                    comp.Graphic,
                    0,
                    (ushort)(foundation.X + comp.OffsetX),
                    (ushort)(foundation.Y + comp.OffsetY),
                    (sbyte)(foundation.Z + comp.OffsetZ),
                    true,
                    ismovable
                );
            }

            if (_world.CustomHouseManager != null)
            {
                _world.CustomHouseManager.GenerateFloorPlace();

                UIManager.GetGump<HouseCustomizationGump>(house.Serial)?.Update();
            }

            UIManager.GetGump<MiniMapGump>()?.RequestUpdateContents();

            if (EntityIntoHouse(serial, _world.Player))
            {
                Client.Game.GetScene<GameScene>()?.UpdateMaxDrawZ(true);
            }

            _world.BoatMovingManager.ClearSteps(serial);
        }

        private void OnHouseRevisionState(HouseRevisionStateArgs e)
        {
            uint serial = e.Serial;
            uint revision = e.Revision;

            Item multi = _world.Items.Get(serial);

            if (multi == null)
            {
                Remove(serial);
            }

            if (
                !TryGetHouse(serial, out House house)
                || !house.IsCustom
                || house.Revision != revision
            )
            {
                PacketHandlers.Handler._customHouseRequests.Add(serial);
            }
            else
            {
                house.Generate();
                _world.BoatMovingManager.ClearSteps(serial);

                UIManager.GetGump<MiniMapGump>()?.RequestUpdateContents();

                if (EntityIntoHouse(serial, _world.Player))
                {
                    Client.Game.GetScene<GameScene>()?.UpdateMaxDrawZ(true);
                }
            }
        }
    }
}
