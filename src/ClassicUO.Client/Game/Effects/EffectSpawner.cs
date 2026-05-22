// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Data;
using ClassicUO.Game.Entities;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Effects
{
    /// <summary>
    /// Concrete <see cref="IEffectSpawner"/>. Maps a
    /// <see cref="GraphicEffectType"/> to the right <see cref="GameEffect"/>
    /// subclass with the parameters from the effect packet. Single
    /// responsibility: construction. Adding the spawned effect to the live
    /// list is the caller's / lifecycle's job.
    /// </summary>
    internal sealed class EffectSpawner : IEffectSpawner
    {
        private readonly World _world;

        public EffectSpawner(World world)
        {
            _world = world;
        }

        public GameEffect Create
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
        )
        {
            if (hasparticles)
            {
                Log.Warn("Unhandled particles in an effects packet.");
            }

            if (hue != 0)
            {
                hue++;
            }

            duration *= Constants.ITEM_EFFECT_ANIMATION_DELAY;

            switch (type)
            {
                case GraphicEffectType.Moving:
                    if (graphic <= 0)
                    {
                        return null;
                    }

                    // TODO: speed == 0 means run at standard frameInterval got from anim.mul?
                    if (speed == 0)
                    {
                        speed++;
                    }

                    return new MovingEffect
                    (
                        _world,
                        manager,
                        source,
                        target,
                        srcX,
                        srcY,
                        srcZ,
                        targetX,
                        targetY,
                        targetZ,
                        graphic,
                        hue,
                        fixedDir,
                        duration,
                        speed
                    )
                    {
                        Blend = blendmode,
                        CanCreateExplosionEffect = doesExplode
                    };

                case GraphicEffectType.DragEffect:
                    if (graphic <= 0)
                    {
                        return null;
                    }

                    if (speed == 0)
                    {
                        speed++;
                    }

                    return new DragEffect
                    (
                        _world,
                        manager,
                        source,
                        target,
                        srcX,
                        srcY,
                        srcZ,
                        targetX,
                        targetY,
                        targetZ,
                        graphic,
                        hue,
                        duration,
                        speed
                    )
                    {
                        Blend = blendmode,
                        CanCreateExplosionEffect = doesExplode
                    };

                case GraphicEffectType.Lightning:
                    return new LightningEffect
                    (
                        _world,
                        manager,
                        source,
                        srcX,
                        srcY,
                        srcZ,
                        hue
                    );

                case GraphicEffectType.FixedXYZ:
                    if (graphic <= 0)
                    {
                        return null;
                    }

                    return new FixedEffect
                    (
                        _world,
                        manager,
                        srcX,
                        srcY,
                        srcZ,
                        graphic,
                        hue,
                        duration,
                        0 //speed [use 50ms]
                    )
                    {
                        Blend = blendmode
                    };

                case GraphicEffectType.FixedFrom:
                    if (graphic <= 0)
                    {
                        return null;
                    }

                    return new FixedEffect
                    (
                        _world,
                        manager,
                        source,
                        srcX,
                        srcY,
                        srcZ,
                        graphic,
                        hue,
                        duration,
                        0 //speed [use 50ms]
                    )
                    {
                        Blend = blendmode
                    };

                case GraphicEffectType.ScreenFade:
                    Log.Warn("Unhandled 'Screen Fade' effect.");
                    return null;

                default:
                    Log.Warn("Unhandled effect.");
                    return null;
            }
        }
    }
}
