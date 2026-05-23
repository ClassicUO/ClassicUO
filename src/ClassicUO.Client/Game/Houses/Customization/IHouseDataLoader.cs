// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using ClassicUO.Game.Data;

namespace ClassicUO.Game.Houses.Customization
{
    /// <summary>
    /// Loads the customization-mode definition files (<c>walls.txt</c>,
    /// <c>floors.txt</c>, <c>doors.txt</c>, <c>misc.txt</c>, <c>stairs.txt</c>,
    /// <c>teleprts.txt</c>, <c>roof.txt</c>, <c>suppinfo.txt</c>) into the
    /// per-category lists owned by <see cref="HouseCustomizationManager"/>.
    /// Also exposes pure lookups over those lists used by the validator and
    /// the builder. One responsibility: file ingestion + graphic search over
    /// the static category tables.
    /// </summary>
    internal interface IHouseDataLoader
    {
        /// <summary>Read every customization definition file into the supplied lists.</summary>
        void Load(
            List<CustomHouseWallCategory> walls,
            List<CustomHouseFloor> floors,
            List<CustomHouseDoor> doors,
            List<CustomHouseMiscCategory> miscs,
            List<CustomHouseStair> stairs,
            List<CustomHouseTeleport> teleports,
            List<CustomHouseRoofCategory> roofs,
            List<CustomHousePlaceInfo> objectsInfo);

        /// <summary>Search a flat list of <see cref="CustomHouseObject"/> for a graphic.</summary>
        (int, int) SeekGraphic<T>(List<T> list, ushort graphic) where T : CustomHouseObject;

        /// <summary>Search a categorised list of <see cref="CustomHouseObject"/> for a graphic.</summary>
        (int, int) SeekGraphicWithCategory<T, U>(List<U> list, ushort graphic)
            where T : CustomHouseObject
            where U : CustomHouseObjectCategory<T>;
    }
}
