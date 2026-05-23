// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Macros
{
    /// <summary>
    /// Runtime engine for the macro VM: holds the cursor into the currently
    /// running <see cref="MacroObject"/> chain, ticks per-frame, and dispatches
    /// every <see cref="MacroType"/> opcode (chat, walk, open / close gumps,
    /// cast spells, use skills, target self / last, bandage, etc.). Also
    /// owns the cross-frame waiting flags
    /// (<see cref="WaitForTargetTimer"/>, <see cref="WaitingBandageTarget"/>)
    /// shared with input + UI callers.
    /// </summary>
    internal interface IMacroExecutor
    {
        /// <summary>Time-stamp the macro engine is waiting until a target cursor appears. <c>0</c> when not waiting.</summary>
        long WaitForTargetTimer { get; set; }

        /// <summary>True between bandage double-click and the matching target reply (legacy bandage flow).</summary>
        bool WaitingBandageTarget { get; set; }

        /// <summary>Arm the engine: the next <see cref="Update"/> will start stepping through <paramref name="macro"/> and its successors.</summary>
        void SetMacroToExecute(MacroObject macro);

        /// <summary>Per-frame pump. Walks the linked <see cref="MacroObject"/> chain until it stalls (target wait, delay) or completes.</summary>
        void Update();
    }
}
