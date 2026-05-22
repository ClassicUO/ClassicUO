// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Utility;

namespace ClassicUO.Game.Combat
{
    internal sealed class DeathDisplayHandler : IEventListener
    {
        private readonly World _world;

        public DeathDisplayHandler(World world) => _world = world;

        public void Subscribe()
        {
            EventSink.MobileDeath += OnMobileDeath;
        }

        public void Unsubscribe()
        {
            EventSink.MobileDeath -= OnMobileDeath;
        }

        private void OnMobileDeath(MobileDeathArgs e)
        {
            if (!_world.InGame)
            {
                return;
            }

            uint serial = e.Serial;
            uint corpseSerial = e.CorpseSerial;
            bool running = e.IsRunning;

            Mobile owner = _world.Mobiles.Get(serial);

            if (owner == null || serial == _world.Player)
            {
                return;
            }

            serial |= 0x80000000;

            if (_world.Mobiles.Remove(owner.Serial))
            {
                for (LinkedObject i = owner.Items; i != null; i = i.Next)
                {
                    Item it = (Item)i;
                    it.Container = serial;
                }

                _world.Mobiles[serial] = owner;
                owner.Serial = serial;
            }

            if (SerialHelper.IsValid(corpseSerial))
            {
                _world.CorpseManager.Add(corpseSerial, serial, owner.Direction, running);
            }

            var animations = Client.Game.UO.Animations;
            var gfx = owner.Graphic;
            animations.ConvertBodyIfNeeded(ref gfx);
            var animGroup = animations.GetAnimType(gfx);
            var animFlags = animations.GetAnimFlags(gfx);
            byte group = Client.Game.UO.FileManager.Animations.GetDeathAction(
                gfx,
                animFlags,
                animGroup,
                running,
                true
            );
            owner.SetAnimation(group, 0, 5, 1);
            owner.AnimIndex = 0;

            if (ProfileManager.CurrentProfile.AutoOpenCorpses)
            {
                _world.Player.TryOpenCorpses();
            }
        }
    }
}
