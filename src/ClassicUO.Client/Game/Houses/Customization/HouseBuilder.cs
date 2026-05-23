// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using System.Linq;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Network;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Houses.Customization
{
    /// <summary>
    /// Concrete <see cref="IHouseBuilder"/>. Owns all the mutable world-side
    /// behaviour of the customization mode: re-flagging every custom multi
    /// each tick (<see cref="GenerateFloorPlace"/>), translating a world
    /// click into add / erase packets (<see cref="OnTargetWorld"/>), and
    /// resetting the cursor (<see cref="SetTargetMulti"/>). Reads state and
    /// runs rule checks via the facade + validator; speaks to the server via
    /// <see cref="NetClient"/>.
    /// </summary>
    internal sealed class HouseBuilder : IHouseBuilder
    {
        private readonly HouseCustomizationManager _facade;
        private readonly IHouseValidator _validator;
        private readonly World _world;

        public HouseBuilder(HouseCustomizationManager facade, IHouseValidator validator, World world)
        {
            _facade = facade;
            _validator = validator;
            _world = world;
        }

        public void GenerateFloorPlace()
        {
            Item foundationItem = _world.Items.Get(_facade.Serial);

            if (foundationItem == null || !_world.HouseManager.TryGetHouse(_facade.Serial, out House house))
            {
                return;
            }

            house.ClearCustomHouseComponents(CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL);

            foreach (Multi item in house.Components)
            {
                if (!item.IsCustom)
                {
                    continue;
                }

                int currentFloor = -1;
                int floorZ = foundationItem.Z + 7;
                int itemZ = item.Z;

                bool ignore = false;

                for (int i = 0; i < 4; i++)
                {
                    int offset = 0 /*i != 0 ? 0 : 7*/;

                    if (itemZ >= floorZ - offset && itemZ < floorZ + 20)
                    {
                        currentFloor = i;

                        break;
                    }

                    floorZ += 20;
                }

                if (currentFloor == -1)
                {
                    ignore = true;
                    currentFloor = 0;
                    //continue;
                }

                (int floorCheck1, int floorCheck2) = _facade.DataLoader.SeekGraphic(_facade.Floors, item.Graphic);

                CUSTOM_HOUSE_MULTI_OBJECT_FLAGS state = item.State;

                if (floorCheck1 != -1 && floorCheck2 != -1)
                {
                    state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR;

                    if (_facade.FloorVisionState[currentFloor] == (int) CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_HIDE_FLOOR)
                    {
                        state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_IGNORE_IN_RENDER;
                    }
                    else if (_facade.FloorVisionState[currentFloor] == (int) CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_TRANSPARENT_FLOOR
                             || _facade.FloorVisionState[currentFloor] == (int) CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_TRANSLUCENT_FLOOR)
                    {
                        state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_TRANSPARENT;
                    }
                }
                else
                {
                    (int stairCheck1, int stairCheck2) = _facade.DataLoader.SeekGraphic(_facade.Stairs, item.Graphic);

                    if (stairCheck1 != -1 && stairCheck2 != -1)
                    {
                        state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_STAIR;
                    }
                    else
                    {
                        (int roofCheck1, int roofCheck2) = _facade.DataLoader.SeekGraphicWithCategory<CustomHouseRoof, CustomHouseRoofCategory>(_facade.Roofs, item.Graphic);

                        if (roofCheck1 != -1 && roofCheck2 != -1)
                        {
                            state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_ROOF;
                        }
                        else
                        {
                            (int fixtureCheck1, int fixtureCheck2) = _facade.DataLoader.SeekGraphic(_facade.Doors, item.Graphic);

                            if (fixtureCheck1 == -1 || fixtureCheck2 == -1)
                            {
                                (fixtureCheck1, fixtureCheck2) = _facade.DataLoader.SeekGraphic(_facade.Teleports, item.Graphic);

                                if (fixtureCheck1 != -1 && fixtureCheck2 != -1)
                                {
                                    state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR;
                                }
                            }
                            else
                            {
                                state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FIXTURE;
                            }
                        }
                    }

                    if (!ignore)
                    {
                        if (_facade.FloorVisionState[currentFloor] == (int) CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_HIDE_CONTENT)
                        {
                            state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_IGNORE_IN_RENDER;
                        }
                        else if (_facade.FloorVisionState[currentFloor] == (int) CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_TRANSPARENT_CONTENT)
                        {
                            state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_TRANSPARENT;
                        }
                    }
                }

                if (!ignore)
                {
                    if (_facade.FloorVisionState[currentFloor] == (int) CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_HIDE_ALL)
                    {
                        state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_IGNORE_IN_RENDER;
                    }
                }

                item.State = state;
            }

            int z = foundationItem.Z + 7;

            for (int x = _facade.StartPos.X + 1; x < _facade.EndPos.X; x++)
            {
                for (int y = _facade.StartPos.Y + 1; y < _facade.EndPos.Y; y++)
                {
                    IEnumerable<Multi> multi = house.Components.Where(s => s.X == x && s.Y == y);

                    if (multi == null)
                    {
                        continue;
                    }

                    Multi floorMulti = null;
                    Multi floorCustomMulti = null;

                    foreach (Multi item in multi)
                    {
                        if (item.Z != z || (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) == 0)
                        {
                            continue;
                        }

                        if (item.IsCustom)
                        {
                            floorCustomMulti = item;
                        }
                        else
                        {
                            floorMulti = item;
                        }
                    }

                    if (floorMulti != null && floorCustomMulti == null)
                    {
                        Multi mo = house.Add
                        (
                            floorMulti.Graphic,
                            0,
                            (ushort) (foundationItem.X + (x - foundationItem.X)),
                            (ushort) (foundationItem.Y + (y - foundationItem.Y)),
                            (sbyte) z,
                            true,
                            false
                        );

                        mo.AlphaHue = 0xFF;

                        CUSTOM_HOUSE_MULTI_OBJECT_FLAGS state = CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL;

                        if (_facade.FloorVisionState[0] == (int) CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_HIDE_FLOOR)
                        {
                            state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_IGNORE_IN_RENDER;
                        }
                        else if (_facade.FloorVisionState[0] == (int) CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_TRANSPARENT_FLOOR
                                 || _facade.FloorVisionState[0] == (int) CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_TRANSLUCENT_FLOOR)
                        {
                            state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_TRANSPARENT;
                        }

                        mo.State = state;
                    }
                }
            }

            var validatedFloors = new List<Point>();
            for (int i = 0; i < _facade.FloorCount; i++)
            {
                int minZ = foundationItem.Z + 7 + i * 20;
                int maxZ = minZ + 20;

                for (int j = 0; j < 2; j++)
                {
                    validatedFloors.Clear();

                    for (int x = _facade.Bounds.X; x < _facade.EndPos.X + 1; x++)
                    {
                        for (int y = _facade.Bounds.Y; y < _facade.EndPos.Y + 1; y++)
                        {
                            IEnumerable<Multi> multi = house.GetMultiAt(x, y);

                            if (multi == null)
                            {
                                continue;
                            }

                            foreach (Multi item in multi)
                            {
                                if (!item.IsCustom)
                                {
                                    continue;
                                }

                                if (j == 0)
                                {
                                    if (i == 0 && item.Z < minZ)
                                    {
                                        item.State = item.State | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE;

                                        continue;
                                    }

                                    if ((item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) == 0)
                                    {
                                        continue;
                                    }

                                    if (i == 0 && item.Z >= minZ && item.Z < maxZ)
                                    {
                                        item.State = item.State | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE;

                                        continue;
                                    }
                                }

                                if ((item.State & (CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL)) == 0 &&
                                    item.Z >= minZ && item.Z < maxZ)
                                {
                                    if (!_validator.ValidateItemPlace
                                        (
                                            foundationItem,
                                            item,
                                            minZ,
                                            maxZ,
                                            validatedFloors
                                        ))
                                    {
                                        item.State = item.State | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE;
                                    }
                                    else
                                    {
                                        item.State = item.State | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE;
                                    }
                                }
                            }
                        }
                    }

                    if (i != 0 && j == 0)
                    {
                        foreach (Point point in validatedFloors)
                        {
                            IEnumerable<Multi> multi = house.GetMultiAt(point.X, point.Y);

                            if (multi == null)
                            {
                                continue;
                            }

                            foreach (Multi item in multi)
                            {
                                if (item.IsCustom && (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) != 0 && item.Z >= minZ && item.Z < maxZ)
                                {
                                    item.State = item.State & ~CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE;
                                }
                            }
                        }

                        for (int x = _facade.Bounds.X; x < _facade.EndPos.X + 1; x++)
                        {
                            int minY = 0, maxY = 0;

                            for (int y = _facade.Bounds.Y; y < _facade.EndPos.Y + 1; y++)
                            {
                                IEnumerable<Multi> multi = house.GetMultiAt(x, y);

                                if (multi == null)
                                {
                                    continue;
                                }

                                foreach (Multi item in multi)
                                {
                                    if (item.IsCustom && (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) != 0 &&
                                        (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE) != 0 &&
                                        (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE) == 0 && item.Z >= minZ && item.Z < maxZ)
                                    {
                                        minY = y;

                                        break;
                                    }
                                }

                                if (minY != 0)
                                {
                                    break;
                                }
                            }

                            for (int y = _facade.EndPos.Y; y >= _facade.Bounds.Y; y--)
                            {
                                IEnumerable<Multi> multi = house.GetMultiAt(x, y);

                                if (multi == null)
                                {
                                    continue;
                                }

                                foreach (Multi item in multi)
                                {
                                    if (item.IsCustom && (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) != 0 &&
                                        (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE) != 0 &&
                                        (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE) == 0 && item.Z >= minZ && item.Z < maxZ)
                                    {
                                        maxY = y;

                                        break;
                                    }
                                }

                                if (maxY != 0)
                                {
                                    break;
                                }
                            }

                            for (int y = minY; y < maxY; y++)
                            {
                                IEnumerable<Multi> multi = house.GetMultiAt(x, y);

                                if (multi == null)
                                {
                                    continue;
                                }

                                foreach (Multi item in multi)
                                {
                                    if (item.IsCustom && (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) != 0 &&
                                        (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE) != 0 && item.Z >= minZ && item.Z < maxZ)
                                    {
                                        item.State = item.State & ~CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE;
                                    }
                                }
                            }
                        }

                        for (int y = _facade.Bounds.Y; y < _facade.EndPos.Y + 1; y++)
                        {
                            int minX = 0;
                            int maxX = 0;

                            for (int x = _facade.Bounds.X; x < _facade.EndPos.X + 1; x++)
                            {
                                IEnumerable<Multi> multi = house.GetMultiAt(x, y);

                                if (multi == null)
                                {
                                    continue;
                                }

                                foreach (Multi item in multi)
                                {
                                    if (item.IsCustom && (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) != 0 &&
                                        (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE) != 0 &&
                                        (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE) == 0 && item.Z >= minZ && item.Z < maxZ)
                                    {
                                        minX = x;

                                        break;
                                    }
                                }

                                if (minX != 0)
                                {
                                    break;
                                }
                            }

                            for (int x = _facade.EndPos.X; x >= _facade.Bounds.X; x--)
                            {
                                IEnumerable<Multi> multi = house.GetMultiAt(x, y);

                                if (multi == null)
                                {
                                    continue;
                                }

                                foreach (Multi item in multi)
                                {
                                    if (item.IsCustom && (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) != 0 &&
                                        (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE) != 0 &&
                                        (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE) == 0 && item.Z >= minZ && item.Z < maxZ)
                                    {
                                        maxX = x;

                                        break;
                                    }
                                }

                                if (maxX != 0)
                                {
                                    break;
                                }
                            }

                            for (int x = minX; x < maxX; x++)
                            {
                                IEnumerable<Multi> multi = house.GetMultiAt(x, y);

                                if (multi == null)
                                {
                                    continue;
                                }

                                foreach (Multi item in multi)
                                {
                                    if (item.IsCustom && (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) != 0 &&
                                        (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE) != 0 && item.Z >= minZ && item.Z < maxZ)
                                    {
                                        item.State = item.State & ~CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE;
                                    }
                                }
                            }
                        }
                    }
                }

                // After both validation passes, flood-fill propagate correctness
                // from walls with direct support to connected same-floor walls.
                // This fixes processing-order dependency in same-floor propagation.
                if (i > 0)
                {
                    var propagationQueue = new Queue<Multi>();

                    const CUSTOM_HOUSE_MULTI_OBJECT_FLAGS excludeMask =
                        CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR |
                        CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_STAIR |
                        CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_ROOF |
                        CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FIXTURE |
                        CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL;

                    // Seed: all wall-type items on this floor that are validated and correct.
                    for (int x = _facade.Bounds.X; x < _facade.EndPos.X + 1; x++)
                    {
                        for (int y = _facade.Bounds.Y; y < _facade.EndPos.Y + 1; y++)
                        {
                            foreach (Multi item in house.GetMultiAt(x, y))
                            {
                                if (item.IsCustom &&
                                    item.Z >= minZ && item.Z < maxZ &&
                                    (item.State & excludeMask) == 0 &&
                                    (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE) != 0 &&
                                    (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE) == 0)
                                {
                                    propagationQueue.Enqueue(item);
                                }
                            }
                        }
                    }

                    int[] pdx = { -1, 1, 0, 0 };
                    int[] pdy = { 0, 0, -1, 1 };

                    while (propagationQueue.Count > 0)
                    {
                        Multi seed = propagationQueue.Dequeue();

                        for (int d = 0; d < 4; d++)
                        {
                            foreach (Multi neighbor in house.GetMultiAt(seed.X + pdx[d], seed.Y + pdy[d]))
                            {
                                if (neighbor.IsCustom &&
                                    neighbor.Z >= minZ && neighbor.Z < maxZ &&
                                    (neighbor.State & excludeMask) == 0 &&
                                    (neighbor.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE) != 0 &&
                                    (neighbor.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE) != 0)
                                {
                                    neighbor.State &= ~CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE;
                                    propagationQueue.Enqueue(neighbor);
                                }
                            }
                        }
                    }
                }
            }

            z = foundationItem.Z + 7 + 20;

            ushort color = 0x0051;

            for (int i = 1; i < _facade.CurrentFloor; i++)
            {
                for (int x = _facade.Bounds.X; x < _facade.EndPos.X; x++)
                {
                    for (int y = _facade.Bounds.Y; y < _facade.EndPos.Y; y++)
                    {
                        var mo = house.Add
                        (
                            0x0496,
                            (ushort)(x == _facade.Bounds.X || y == _facade.Bounds.Y ? 0x34 : color),
                            (ushort)(foundationItem.X + (x - foundationItem.X)),
                            (ushort)(foundationItem.Y + (y - foundationItem.Y)),
                            (sbyte) z,
                            true,
                            false
                        );

                        mo.AlphaHue = 0xFF;
                        mo.State = CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_TRANSPARENT;
                        mo.AddToTile();
                    }
                }

                color += 5;
                z += 20;
            }
        }

        public void OnTargetWorld(GameObject place)
        {
            if (place == null /*&& place is Multi m*/)
            {
                return;
            }

            if (!_facade.Bounds.Contains(place.X, place.Y))
                return;

            // apply a minor offset for roof tiles
            int zOffset = -3;

            HouseCustomizationGump gump = UIManager.GetGump<HouseCustomizationGump>(_facade.Serial);

            if (_facade.CurrentFloor == 1)
            {
                zOffset = -7;
            }

            if (_facade.SeekTile)
            {
                if (place is Multi)
                {
                    SeekGraphic(place.Graphic);
                }
            }
            else if (place.Z >= _world.Player.Z + zOffset && place.Z < _world.Player.Z + 20)
            {
                Item foundationItem = _world.Items.Get(_facade.Serial);

                if (foundationItem == null || !_world.HouseManager.TryGetHouse(_facade.Serial, out House house))
                {
                    return;
                }

                if (_facade.Erasing)
                {
                    if (!(place is Multi))
                    {
                        return;
                    }

                    if (_validator.CanEraseHere(place, out CUSTOM_HOUSE_BUILD_TYPE type))
                    {
                        IEnumerable<Multi> multi = house.GetMultiAt(place.X, place.Y);

                        if (multi == null || !multi.Any())
                        {
                            return;
                        }

                        int z = 7 + (_facade.CurrentFloor - 1) * 20;

                        if (type == CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR || type == CUSTOM_HOUSE_BUILD_TYPE.CHBT_ROOF)
                        {
                            z = place.Z - (foundationItem.Z + z) + z;
                        }

                        if (type == CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR)
                        {
                            int floorBase = foundationItem.Z;
                            int stairFloorBase = floorBase;

                            for (int f = 0; f < _facade.FloorCount; f++)
                            {
                                int fz = floorBase + 7 + f * 20;

                                if (place.Z >= fz && place.Z < fz + 20)
                                {
                                    stairFloorBase = fz;
                                    break;
                                }
                            }

                            if (place.Z < floorBase + 7)
                                stairFloorBase = floorBase;

                            // Collect stair pieces sharing same X (N/S) or same Y (E/W) with clicked piece
                            var sameXPieces = new List<Multi>();
                            var sameYPieces = new List<Multi>();

                            foreach (Multi comp in house.Components)
                            {
                                if (comp.IsDestroyed || !comp.IsCustom)
                                    continue;

                                if ((comp.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_STAIR) == 0)
                                    continue;

                                if (comp.Z < stairFloorBase || comp.Z >= stairFloorBase + 20)
                                    continue;

                                if (comp.X == place.X)
                                    sameXPieces.Add(comp);

                                if (comp.Y == place.Y)
                                    sameYPieces.Add(comp);
                            }

                            // Determine orientation by piece count, then find exact 4-tile group
                            var stairPieces = new List<Multi>();

                            if (sameXPieces.Count >= sameYPieces.Count && sameXPieces.Count > 0)
                            {
                                // N/S orientation - find best 4-consecutive-Y window containing place.Y
                                int bestCount = 0;
                                int bestStart = place.Y;

                                for (int startY = place.Y - 3; startY <= place.Y; startY++)
                                {
                                    int count = 0;

                                    foreach (var p in sameXPieces)
                                    {
                                        if (p.Y >= startY && p.Y <= startY + 3)
                                            count++;
                                    }

                                    if (count > bestCount)
                                    {
                                        bestCount = count;
                                        bestStart = startY;
                                    }
                                }

                                foreach (var p in sameXPieces)
                                {
                                    if (p.Y >= bestStart && p.Y <= bestStart + 3)
                                        stairPieces.Add(p);
                                }
                            }
                            else if (sameYPieces.Count > 0)
                            {
                                // E/W orientation - find best 4-consecutive-X window containing place.X
                                int bestCount = 0;
                                int bestStart = place.X;

                                for (int startX = place.X - 3; startX <= place.X; startX++)
                                {
                                    int count = 0;

                                    foreach (var p in sameYPieces)
                                    {
                                        if (p.X >= startX && p.X <= startX + 3)
                                            count++;
                                    }

                                    if (count > bestCount)
                                    {
                                        bestCount = count;
                                        bestStart = startX;
                                    }
                                }

                                foreach (var p in sameYPieces)
                                {
                                    if (p.X >= bestStart && p.X <= bestStart + 3)
                                        stairPieces.Add(p);
                                }
                            }

                            // Combined staircases have pieces at multiple Z levels (0/5/10/15 offsets).
                            // Single stairs are all at one Z. Only group-delete for combined staircases.
                            bool isCombined = false;

                            if (stairPieces.Count > 1)
                            {
                                int firstZ = stairPieces[0].Z;

                                for (int i = 1; i < stairPieces.Count; i++)
                                {
                                    if (stairPieces[i].Z != firstZ)
                                    {
                                        isCombined = true;
                                        break;
                                    }
                                }
                            }

                            if (isCombined)
                            {
                                foreach (Multi piece in stairPieces)
                                {
                                    int pz = piece.Z - (foundationItem.Z + (7 + (_facade.CurrentFloor - 1) * 20)) + (7 + (_facade.CurrentFloor - 1) * 20);

                                    NetClient.Socket.Send_CustomHouseDeleteItem(_world, piece.Graphic, piece.X - foundationItem.X, piece.Y - foundationItem.Y, pz);
                                    piece.Destroy();
                                }
                            }
                            else
                            {
                                NetClient.Socket.Send_CustomHouseDeleteItem(_world, place.Graphic, place.X - foundationItem.X, place.Y - foundationItem.Y, z);
                                place.Destroy();
                            }
                        }
                        else if (type == CUSTOM_HOUSE_BUILD_TYPE.CHBT_ROOF)
                        {
                            NetClient.Socket.Send_CustomHouseDeleteRoof(_world, place.Graphic, place.X - foundationItem.X, place.Y - foundationItem.Y, z);
                            place.Destroy();
                        }
                        else
                        {
                            NetClient.Socket.Send_CustomHouseDeleteItem(_world, place.Graphic, place.X - foundationItem.X, place.Y - foundationItem.Y, z);
                            place.Destroy();
                        }
                    }
                }
                else if (_facade.SelectedGraphic != 0)
                {
                    var list = new List<CustomBuildObject>();

                    if (_validator.CanBuildHere(list, out CUSTOM_HOUSE_BUILD_TYPE type) && list.Count > 0)
                    {
                        //if (type != CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR && !(place is Multi))
                        //    return;

                        int placeX = place.X;
                        int placeY = place.Y;

                        if (type == CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR && _facade.CombinedStair)
                        {
                            if (gump.Page >= 0 && gump.Page < _facade.Stairs.Count)
                            {
                                CustomHouseStair stair = _facade.Stairs[gump.Page];

                                ushort graphic = 0;

                                if (_facade.SelectedGraphic == stair.North)
                                {
                                    graphic = (ushort) stair.MultiNorth;
                                }
                                else if (_facade.SelectedGraphic == stair.East)
                                {
                                    graphic = (ushort) stair.MultiEast;
                                }
                                else if (_facade.SelectedGraphic == stair.South)
                                {
                                    graphic = (ushort) stair.MultiSouth;
                                }
                                else if (_facade.SelectedGraphic == stair.West)
                                {
                                    graphic = (ushort) stair.MultiWest;
                                }

                                if (graphic != 0)
                                {
                                    NetClient.Socket.Send_CustomHouseAddStair(_world, graphic, placeX - foundationItem.X, placeY - foundationItem.Y);
                                }
                            }
                        }
                        else
                        {
                            CustomBuildObject item = list[0];

                            int x = placeX - foundationItem.X + item.X;
                            int y = placeY - foundationItem.Y + item.Y;
                            IEnumerable<Multi> multi = house.GetMultiAt(placeX + item.X, placeY + item.Y);

                            if (multi.Any() || type == CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR)
                            {
                                if (!_facade.CombinedStair)
                                {
                                    int minZ = foundationItem.Z + 7 + (_facade.CurrentFloor - 1) * 20;
                                    int maxZ = minZ + 20;

                                    if (_facade.CurrentFloor == 1)
                                    {
                                        minZ -= 7;
                                    }

                                    foreach (Multi multiObject in multi)
                                    {
                                        int testMinZ = minZ;

                                        if ((multiObject.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_ROOF) != 0)
                                        {
                                            testMinZ -= 3;
                                        }

                                        if (multiObject.Z < testMinZ || multiObject.Z >= maxZ || !multiObject.IsCustom || (multiObject.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL) != 0 /*|| (multiObject.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_DONT_REMOVE) != 0*/
                                           )
                                        {
                                            continue;
                                        }

                                        if (type == CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR)
                                        {
                                            if ((multiObject.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_STAIR) != 0)
                                            {
                                                multiObject.Destroy();
                                            }
                                        }
                                        else if (type == CUSTOM_HOUSE_BUILD_TYPE.CHBT_ROOF)
                                        {
                                            if ((multiObject.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_ROOF) != 0)
                                            {
                                                multiObject.Destroy();
                                            }
                                        }
                                        else if (type == CUSTOM_HOUSE_BUILD_TYPE.CHBT_FLOOR)
                                        {
                                            if ((multiObject.State & (CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FIXTURE)) != 0)
                                            {
                                                multiObject.Destroy();
                                            }
                                        }
                                        else
                                        {
                                            if ((multiObject.State & (CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_STAIR | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_ROOF | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_DONT_REMOVE)) == 0)
                                            {
                                                multiObject.Destroy();
                                            }
                                        }
                                    }

                                    // todo: remove foundation if no components
                                }

                                if (type == CUSTOM_HOUSE_BUILD_TYPE.CHBT_ROOF)
                                {
                                    NetClient.Socket.Send_CustomHouseAddRoof(_world, item.Graphic, x, y, item.Z);
                                }
                                else
                                {
                                    NetClient.Socket.Send_CustomHouseAddItem(_world, item.Graphic, x, y);
                                }
                            }
                        }

                        int xx = placeX - foundationItem.X;
                        int yy = placeY - foundationItem.Y;
                        int z = foundationItem.Z + 7 + (_facade.CurrentFloor - 1) * 20;

                        if (type == CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR && !_facade.CombinedStair)
                        {
                            z = foundationItem.Z;
                        }

                        foreach (CustomBuildObject item in list)
                        {
                            house.Add
                            (
                                item.Graphic,
                                0,
                                (ushort) (foundationItem.X + xx + item.X),
                                (ushort) (foundationItem.Y + yy + item.Y),
                                (sbyte) (z + item.Z),
                                true,
                                false
                            );
                        }
                    }
                }

                GenerateFloorPlace();
                gump.Update();
            }
        }

        public void SetTargetMulti()
        {
            _world.TargetManager.SetTargetingMulti
            (
                0,
                0,
                0,
                0,
                0,
                0
            );

            _facade.Erasing = false;
            _facade.SeekTile = false;
            _facade.SelectedGraphic = 0;
            _facade.CombinedStair = false;
        }

        private void SeekGraphic(ushort graphic)
        {
            CUSTOM_HOUSE_GUMP_STATE state = 0;
            (int res1, int res2) = _validator.ExistsInList(ref state, graphic);

            if (res1 != -1 && res2 != -1)
            {
                _facade.State = state;
                HouseCustomizationGump gump = UIManager.GetGump<HouseCustomizationGump>(_facade.Serial);

                if (_facade.State == CUSTOM_HOUSE_GUMP_STATE.CHGS_WALL || _facade.State == CUSTOM_HOUSE_GUMP_STATE.CHGS_ROOF || _facade.State == CUSTOM_HOUSE_GUMP_STATE.CHGS_MISC)
                {
                    _facade.Category = res1;
                    gump.Page = res2;
                }
                else
                {
                    _facade.Category = -1;
                    gump.Page = res1;
                }

                gump.UpdateMaxPage();
                SetTargetMulti();
                _facade.SelectedGraphic = graphic;
                gump.Update();
            }
        }
    }
}
