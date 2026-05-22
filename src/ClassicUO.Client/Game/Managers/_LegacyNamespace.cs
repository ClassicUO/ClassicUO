// SPDX-License-Identifier: BSD-2-Clause

// Compatibility shim: the legacy `ClassicUO.Game.Managers` namespace still
// needs to be importable because hundreds of files have
// `using ClassicUO.Game.Managers;`. All concrete manager types have moved
// into their feature folders (Game/<Feature>/) and live in feature
// namespaces. The new namespaces are re-exported globally in
// Game/_GlobalUsings.cs, so callers see the same type set as before.
//
// This file exists solely so the `using ClassicUO.Game.Managers;` import
// resolves to a real namespace.

namespace ClassicUO.Game.Managers
{
    // C# does not surface an empty namespace across assembly boundaries.
    // A private marker type keeps the namespace visible to importing
    // assemblies (notably the test project's `using ClassicUO.Game.Managers;`
    // imports). Do not remove without removing those imports first.
    internal static class _LegacyNamespaceMarker
    {
    }
}
