// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Entities;

namespace ClassicUO.Game.Effects
{
    /// <summary>
    /// Concrete <see cref="IEffectLifecycle"/>. Walks the linked list of
    /// <see cref="GameEffect"/> nodes attached to the host
    /// <see cref="EffectManager"/>, calls <see cref="GameEffect.Update"/>
    /// each tick, and destroys any effect that strayed past
    /// <see cref="World.ClientViewRange"/>. <see cref="Clear"/> destroys
    /// every live effect and detaches the list head.
    /// </summary>
    internal sealed class EffectLifecycle : IEffectLifecycle
    {
        private readonly World _world;

        public EffectLifecycle(World world)
        {
            _world = world;
        }

        public void Update(EffectManager manager)
        {
            for (GameEffect f = (GameEffect) manager.Items; f != null;)
            {
                GameEffect next = (GameEffect) f.Next;

                f.Update();

                if (!f.IsDestroyed && f.Distance > _world.ClientViewRange)
                {
                    f.Destroy();
                }

                f = next;
            }
        }

        public void Clear(EffectManager manager)
        {
            GameEffect first = (GameEffect) manager.Items;

            while (first != null)
            {
                LinkedObject n = first.Next;

                first.Destroy();

                first = (GameEffect) n;
            }

            manager.Items = null;
        }
    }
}
