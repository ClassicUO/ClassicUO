// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using ClassicUO.Game.GameObjects;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Houses.Customization
{
    /// <summary>
    /// Pure placement-rules logic for the customization mode: given the
    /// manager's current state (selected graphic, floor, bounds, component
    /// counts) decides what may be built or erased on the targeted tile and
    /// whether a candidate <see cref="Multi"/> is in a structurally valid
    /// place. No file I/O, no world mutation, no networking.
    /// </summary>
    internal interface IHouseValidator
    {
        /// <summary>Build the placement preview for the current selection and report whether placement is allowed.</summary>
        bool CanBuildHere(List<CustomBuildObject> list, out CUSTOM_HOUSE_BUILD_TYPE type);

        /// <summary>Decide whether the targeted object may be erased and classify it.</summary>
        bool CanEraseHere(GameObject place, out CUSTOM_HOUSE_BUILD_TYPE type);

        /// <summary>Locate a graphic across the seven category lists and return the matching gump state.</summary>
        (int, int) ExistsInList(ref CUSTOM_HOUSE_GUMP_STATE state, ushort graphic);

        /// <summary>Verify a candidate multi piece is in a structurally valid place on the given floor.</summary>
        bool ValidateItemPlace(Item foundationItem, Multi item, int minZ, int maxZ, List<Point> validatedFloors);

        /// <summary>Recursive helper: probe a multi enumeration for a wall / floor that satisfies the supplied flag mask.</summary>
        bool ValidatePlaceStructure(Item foundationItem, House house, IEnumerable<Multi> multi, int minZ, int maxZ, int flags);
    }
}
