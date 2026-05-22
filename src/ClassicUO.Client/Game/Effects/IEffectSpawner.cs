// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Data;
using ClassicUO.Game.Entities;

namespace ClassicUO.Game.Effects
{
    /// <summary>
    /// Factory for <see cref="GameEffect"/> subclasses. Pure construction:
    /// given the wire-level effect descriptor returns the concrete
    /// <see cref="FixedEffect"/> / <see cref="MovingEffect"/> /
    /// <see cref="DragEffect"/> / <see cref="LightningEffect"/> instance,
    /// or <c>null</c> when the effect is unhandled or has an invalid graphic.
    /// No list management, no events.
    /// </summary>
    internal interface IEffectSpawner
    {
        GameEffect Create
        (
            EffectManager manager,
            GraphicEffectType type,
            uint source,
            uint target,
            ushort graphic,
            ushort hue,
            ushort srcX,
            ushort srcY,
            sbyte srcZ,
            ushort targetX,
            ushort targetY,
            sbyte targetZ,
            byte speed,
            int duration,
            bool fixedDir,
            bool doesExplode,
            bool hasparticles,
            GraphicEffectBlendMode blendmode
        );
    }
}
