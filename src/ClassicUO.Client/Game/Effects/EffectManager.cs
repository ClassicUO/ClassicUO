// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Data;
using ClassicUO.Game.Entities;

namespace ClassicUO.Game.Effects
{
    /// <summary>
    /// Facade over the two Effects collaborators. Keeps the existing public
    /// surface (<c>_world.EffectManager.CreateEffect / Update / Clear</c>)
    /// and the <see cref="LinkedObject"/> inheritance so that
    /// <see cref="GameEffect"/> can still call
    /// <c>_manager.PushToBack(effect)</c> and tests can poke
    /// <see cref="LinkedObject.Items"/>. Spawning and per-frame lifecycle
    /// live in dedicated cohesive classes under
    /// <see cref="Game.Effects"/>.
    /// </summary>
    internal sealed class EffectManager : LinkedObject
    {
        private readonly IEffectSpawner _spawner;
        private readonly IEffectLifecycle _lifecycle;

        /// <summary>Production composition root. Defaults to concrete collaborators.</summary>
        public EffectManager(World world)
            : this(new EffectSpawner(world), new EffectLifecycle(world))
        {
        }

        /// <summary>Full DI seam — inject both collaborators.</summary>
        internal EffectManager(IEffectSpawner spawner, IEffectLifecycle lifecycle)
        {
            _spawner = spawner;
            _lifecycle = lifecycle;
        }

        public void Update() => _lifecycle.Update(this);

        public new void Clear() => _lifecycle.Clear(this);

        public void CreateEffect
        (
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
            GameEffect effect = _spawner.Create
            (
                this,
                type,
                source,
                target,
                graphic,
                hue,
                srcX,
                srcY,
                srcZ,
                targetX,
                targetY,
                targetZ,
                speed,
                duration,
                fixedDir,
                doesExplode,
                hasparticles,
                blendmode
            );

            if (effect != null)
            {
                PushToBack(effect);
            }
        }
    }
}
