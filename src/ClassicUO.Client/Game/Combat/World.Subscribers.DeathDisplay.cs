// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Utility;

namespace ClassicUO.Game
{
    internal sealed partial class World
    {
        private void SubscribeDeathDisplay()
        {
            EventSink.MobileDeath += OnMobileDeath;
        }

        private void UnsubscribeDeathDisplay()
        {
            EventSink.MobileDeath -= OnMobileDeath;
        }

        private void OnMobileDeath(MobileDeathArgs e)
        {
            if (!InGame)
            {
                return;
            }

            uint serial = e.Serial;
            uint corpseSerial = e.CorpseSerial;
            bool running = e.IsRunning;

            Mobile owner = Mobiles.Get(serial);

            if (owner == null || serial == Player)
            {
                return;
            }

            serial |= 0x80000000;

            if (Mobiles.Remove(owner.Serial))
            {
                for (LinkedObject i = owner.Items; i != null; i = i.Next)
                {
                    Item it = (Item)i;
                    it.Container = serial;
                }

                Mobiles[serial] = owner;
                owner.Serial = serial;
            }

            if (SerialHelper.IsValid(corpseSerial))
            {
                CorpseManager.Add(corpseSerial, serial, owner.Direction, running);
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
                Player.TryOpenCorpses();
            }
        }
    }
}
