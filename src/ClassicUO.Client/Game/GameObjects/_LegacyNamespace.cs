// SPDX-License-Identifier: BSD-2-Clause

// Compatibility shim for the legacy `ClassicUO.Game.GameObjects` namespace.
// Concrete entity types (Entity, GameObject, Mobile, Item, etc.) moved
// into Game/Entities/ and live in `ClassicUO.Game.Entities`. The new
// namespace is re-exported globally in Game/_GlobalUsings.cs so callers
// don't need to update their `using` directives in one pass.
//
// C# does not surface an empty namespace across assembly boundaries; the
// private marker type keeps `using ClassicUO.Game.GameObjects;` resolving.

namespace ClassicUO.Game.GameObjects
{
    internal static class _LegacyNamespaceMarker
    {
    }
}

namespace ClassicUO.Game.GameObjects.Views
{
    internal static class _LegacyNamespaceMarker
    {
    }
}
