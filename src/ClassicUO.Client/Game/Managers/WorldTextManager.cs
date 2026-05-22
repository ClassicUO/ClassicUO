// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.WorldText;
using ClassicUO.Renderer;

namespace ClassicUO.Game.Managers
{
    /// <summary>
    /// Facade over the world-overhead text renderer and the floating
    /// damage overlay. Keeps <see cref="TextRenderer"/> inheritance so the
    /// existing public surface (<c>World.WorldTextManager.X</c>, base
    /// virtuals) is preserved. Damage state lives behind
    /// <see cref="IDamageOverlay"/>; this class only routes the
    /// <see cref="EventSink.DamageReceived"/> handler into it.
    /// </summary>
    internal class WorldTextManager : TextRenderer, IEventListener
    {
        private readonly IDamageOverlay _damageOverlay;

        /// <summary>Production composition root.</summary>
        public WorldTextManager(World world)
            : this(world, new DamageOverlay(world))
        {
        }

        /// <summary>Test / extension seam — inject a fake overlay.</summary>
        internal WorldTextManager(World world, IDamageOverlay damageOverlay)
            : base(world)
        {
            _damageOverlay = damageOverlay;
        }

        public void Subscribe()
        {
            EventSink.DamageReceived += OnDamageReceived;
        }

        public void Unsubscribe()
        {
            EventSink.DamageReceived -= OnDamageReceived;
        }

        private void OnDamageReceived(DamageReceivedArgs e)
        {
            Entity entity = World.Get(e.Serial);
            if (entity == null || e.Damage == 0) return;

            _damageOverlay.AddDamage(entity.Serial, e.Damage);
        }

        public override void Update()
        {
            base.Update();

            _damageOverlay.Update();
        }

        public override void Draw(UltimaBatcher2D batcher, int startX, int startY, float layerDepth, bool isGump = false)
        {
            base.Draw
            (
                batcher,
                startX,
                startY,
                layerDepth,
                isGump
            );

            _damageOverlay.Draw(batcher, layerDepth);
        }

        internal void AddDamage(uint obj, int dmg) => _damageOverlay.AddDamage(obj, dmg);

        public override void Clear()
        {
            _damageOverlay.Clear();
            base.Clear();
        }
    }
}
