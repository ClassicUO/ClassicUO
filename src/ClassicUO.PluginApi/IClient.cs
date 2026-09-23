// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.PluginApi;

/// <summary>
/// Client-level services that aren't tied to a player or packet stream:
/// window title, localization, and similar.
/// </summary>
public interface IClient
{
    /// <summary>Replaces the game window's title. Empty strings restore the default.</summary>
    void SetWindowTitle(string title);

    /// <summary>
    /// Resolves a cliloc id to a localized string in the user's language.
    /// Returns <c>null</c> if the id is not present in the loaded clilocs.
    /// </summary>
    /// <param name="id">Cliloc identifier.</param>
    /// <param name="args">Tab-separated arguments for templated entries; empty for entries with no parameters.</param>
    /// <param name="capitalize">When <c>true</c>, capitalizes the first letter of the resolved string.</param>
    string? GetCliloc(int id, string args = "", bool capitalize = false);
}
