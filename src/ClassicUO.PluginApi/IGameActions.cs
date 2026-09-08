// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.PluginApi;

/// <summary>
/// Player-driven game actions that synthesize the same packets the client
/// sends for user input. Per-method threading rules are documented below.
/// </summary>
public interface IGameActions
{
    /// <summary>
    /// Casts a spell by spellbook index. Auto-marshals to the game thread
    /// when called from elsewhere.
    /// </summary>
    void CastSpell(int spellIndex);

    /// <summary>
    /// Requests a single tile of movement in the given direction (0..7).
    /// Returns <c>false</c> if the player can't move right now (paralyzed,
    /// blocked, etc.). Must be called on the game thread; throws otherwise.
    /// Use <see cref="IPluginContext.Game"/>.<c>Post</c> to marshal.
    /// </summary>
    bool RequestMove(int direction, bool run);

    /// <summary>
    /// Reads the player's tile coordinates. Returns <c>false</c> if the
    /// player object is not initialized (pre-login or mid-disconnect).
    /// </summary>
    bool TryGetPlayerPosition(out int x, out int y, out int z);
}
