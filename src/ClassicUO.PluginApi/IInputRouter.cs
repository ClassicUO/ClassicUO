// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.PluginApi;

/// <summary>
/// Subscribes to keyboard and mouse events that originate from the game
/// window. Modifier flags follow SDL_Keymod values.
/// </summary>
public interface IInputRouter
{
    /// <summary>
    /// Fires when a hotkey is pressed or released in the game window. Return
    /// <c>false</c> to block the default handler (for example, to suppress
    /// chat-mode toggling). If multiple plugins subscribe, the hotkey is
    /// suppressed if any handler returns <c>false</c>.
    /// </summary>
    event HotkeyHandler? Hotkey;

    /// <summary>
    /// Fires when a mouse button is pressed or the wheel scrolled. Mouse
    /// events are observation-only; they cannot be blocked.
    /// </summary>
    event MouseHandler? Mouse;
}

/// <summary>
/// Hotkey handler. <paramref name="key"/> is an SDL keycode;
/// <paramref name="modifiers"/> is an SDL_Keymod bitmask;
/// <paramref name="pressed"/> distinguishes keydown (true) from keyup (false).
/// Return <c>false</c> to suppress the client's default handling.
/// </summary>
public delegate bool HotkeyHandler(int key, int modifiers, bool pressed);

/// <summary>
/// Mouse handler. <paramref name="button"/> is the SDL mouse button index;
/// <paramref name="wheel"/> is the wheel delta (0 for non-wheel events).
/// </summary>
public delegate void MouseHandler(int button, int wheel);
