// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;

namespace ClassicUO.Game.Macros
{
    /// <summary>
    /// Persists the macro list held by a <see cref="MacroManager"/>: reads and
    /// writes <c>macros.xml</c> in the active profile directory, materialises
    /// the built-in defaults when the file is missing, and aggregates the
    /// linked list into a flat <see cref="List{T}"/> for consumers.
    /// One responsibility: macro XML I/O around the host's
    /// <see cref="LinkedObject"/> list.
    /// </summary>
    internal interface IMacroStore
    {
        /// <summary>Load <c>macros.xml</c> into <paramref name="host"/>. Clears the host first; writes defaults when the file does not yet exist.</summary>
        void Load(MacroManager host);

        /// <summary>Serialise every macro currently linked to <paramref name="host"/> into <c>macros.xml</c>.</summary>
        void Save(MacroManager host);

        /// <summary>Return every <see cref="Macro"/> linked to <paramref name="host"/>, walking from the head of the list.</summary>
        List<Macro> GetAllMacros(MacroManager host);
    }
}
