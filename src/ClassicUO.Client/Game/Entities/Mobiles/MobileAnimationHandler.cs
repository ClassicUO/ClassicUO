// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.Entities.Mobiles
{
    /// <summary>
    /// Listens for legacy and "new" character animation packets and drives
    /// <see cref="Mobile.SetAnimation"/> on the matching mobile. Both events
    /// share the same lookup/guard pattern so they live together.
    /// </summary>
    internal sealed class MobileAnimationHandler : IEventListener
    {
        private readonly World _world;

        public MobileAnimationHandler(World world) => _world = world;

        public void Subscribe()
        {
            EventSink.CharacterAnimation += OnCharacterAnimation;
            EventSink.NewCharacterAnimation += OnNewCharacterAnimation;
        }

        public void Unsubscribe()
        {
            EventSink.CharacterAnimation -= OnCharacterAnimation;
            EventSink.NewCharacterAnimation -= OnNewCharacterAnimation;
        }

        private void OnCharacterAnimation(CharacterAnimationArgs e)
        {
            Mobile mobile = _world.Mobiles.Get(e.Serial);
            if (mobile == null) return;

            mobile.SetAnimation(
                Mobile.GetReplacedObjectAnimation(mobile.Graphic, e.Action),
                e.Delay,
                (byte)e.FrameCount,
                (byte)e.RepeatCount,
                e.Repeat,
                e.Forward,
                true
            );
        }

        private void OnNewCharacterAnimation(NewCharacterAnimationArgs e)
        {
            Mobile mobile = _world.Mobiles.Get(e.Serial);
            if (mobile == null) return;

            byte group = Mobile.GetObjectNewAnimation(mobile, e.Type, e.Action, e.Mode);
            mobile.SetAnimation(
                group,
                repeatCount: 1,
                repeat: (e.Type == 1 || e.Type == 2) && mobile.Graphic == 0x0015,
                forward: true,
                fromServer: true
            );
        }
    }
}
