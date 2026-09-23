// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.PluginApi;

/// <summary>
/// Host-provided service container handed to a plugin via
/// <see cref="IPlugin.OnInitialize"/>. Each plugin receives its own context;
/// events on one context are not visible to another. Service properties are
/// non-null and stable for the context's lifetime; lifecycle events fire on
/// the game thread unless noted.
/// </summary>
public interface IPluginContext
{
    /// <summary>Absolute path to the user's UO data directory (where mul/uop files live).</summary>
    string UODataPath { get; }

    /// <summary>
    /// Absolute path to a per-plugin directory the plugin owns for its own
    /// configuration, cache, and logs. Created by the host on first access.
    /// </summary>
    string PluginDataPath { get; }

    /// <summary>UO client version reported to the server (4 bytes packed into a uint).</summary>
    uint ClientVersion { get; }

    /// <summary>Server-to-client and client-to-server packet hooks plus injection.</summary>
    IPacketPipeline Packets { get; }

    /// <summary>Hotkey and mouse routing.</summary>
    IInputRouter Input { get; }

    /// <summary>Player movement, spell casting, position queries.</summary>
    IGameActions Actions { get; }

    /// <summary>Window title, cliloc translation, and other client-level services.</summary>
    IClient Client { get; }

    /// <summary>Marshals work onto the game thread.</summary>
    IDispatcher Game { get; }

    /// <summary>Raised after a successful login. Fired on the game thread.</summary>
    event Action? Connected;

    /// <summary>Raised after disconnect. Fired on the game thread.</summary>
    event Action? Disconnected;

    /// <summary>Raised when the game window gains focus.</summary>
    event Action? FocusGained;

    /// <summary>Raised when the game window loses focus.</summary>
    event Action? FocusLost;

    /// <summary>
    /// Raised whenever the player's tile coordinates change. The handler
    /// receives <c>(x, y, z)</c>: x and y are map coordinates, z is altitude.
    /// </summary>
    event Action<int, int, int>? PlayerPositionChanged;

    /// <summary>Raised once per game frame before render.</summary>
    event Action? Tick;

    /// <summary>Raised once during a clean client shutdown, before <see cref="IPlugin.OnShutdown"/>.</summary>
    event Action? Closing;
}
