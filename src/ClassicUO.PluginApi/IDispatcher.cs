// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.PluginApi;

/// <summary>
/// Marshals work onto the game (UI) thread. Most plugin services that
/// touch world state require the game thread; use this when handling input
/// or timer callbacks that don't already run on it.
/// </summary>
public interface IDispatcher
{
    /// <summary>
    /// <c>true</c> if the calling thread is the game thread. Use this to
    /// avoid an unnecessary <see cref="Post"/> when already on it.
    /// </summary>
    bool IsGameThread { get; }

    /// <summary>
    /// Queues <paramref name="action"/> to run on the game thread before the
    /// next tick. Returns immediately. Exceptions thrown by the action are
    /// reported to the host's logger and do not crash the game.
    /// </summary>
    void Post(Action action);

    /// <summary>
    /// Queues <paramref name="action"/> to run on the game thread and
    /// returns a task that completes after the action runs (or faults if
    /// the action throws).
    /// </summary>
    Task PostAsync(Action action);
}
