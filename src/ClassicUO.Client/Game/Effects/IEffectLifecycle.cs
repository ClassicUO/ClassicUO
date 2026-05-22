// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Effects
{
    /// <summary>
    /// Per-frame driver for the live <see cref="Entities.GameEffect"/> list
    /// owned by the host <see cref="EffectManager"/>. Single responsibility:
    /// iterate the list each tick, advance each effect, and cull anything
    /// past the client's view range. <see cref="Clear"/> destroys the whole
    /// list. No construction logic.
    /// </summary>
    internal interface IEffectLifecycle
    {
        void Update(EffectManager manager);
        void Clear(EffectManager manager);
    }
}
