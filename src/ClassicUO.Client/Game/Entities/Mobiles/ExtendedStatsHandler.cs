// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Data;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.Game.Entities.Mobiles
{
    internal sealed class ExtendedStatsHandler : IEventListener
    {
        private readonly World _world;

        public ExtendedStatsHandler(World world)
        {
            _world = world;
        }

        public void Subscribe()
        {
            EventSink.ExtendedStatsBonded += OnExtendedStatsBonded;
            EventSink.ExtendedStatsLocks += OnExtendedStatsLocks;
            EventSink.ExtendedStatsAnimation += OnExtendedStatsAnimation;
            EventSink.SpellbookContent += OnSpellbookContent;
        }

        public void Unsubscribe()
        {
            EventSink.ExtendedStatsBonded -= OnExtendedStatsBonded;
            EventSink.ExtendedStatsLocks -= OnExtendedStatsLocks;
            EventSink.ExtendedStatsAnimation -= OnExtendedStatsAnimation;
            EventSink.SpellbookContent -= OnSpellbookContent;
        }

        private void OnExtendedStatsBonded(ExtendedStatsBondedArgs e)
        {
            Mobile bonded = _world.Mobiles.Get(e.Serial);
            if (bonded == null) return;

            bonded.IsDead = e.IsDead;
        }

        private void OnExtendedStatsLocks(ExtendedStatsLocksArgs e)
        {
            if (_world.Player == null) return;
            if (e.Serial != _world.Player.Serial) return;

            _world.Player.StrLock = (Lock)e.StrLock;
            _world.Player.DexLock = (Lock)e.DexLock;
            _world.Player.IntLock = (Lock)e.IntLock;

            StatusGumpBase.GetStatusGump()?.RequestUpdateContents();
        }

        private void OnExtendedStatsAnimation(ExtendedStatsAnimationArgs e)
        {
            Mobile mobile = _world.Mobiles.Get(e.Serial);
            if (mobile == null) return;

            mobile.SetAnimation(
                Mobile.GetReplacedObjectAnimation(mobile.Graphic, e.Animation)
            );
            mobile.ExecuteAnimation = false;
            mobile.AnimIndex = (byte)e.Frame;
        }

        private void OnSpellbookContent(SpellbookContentArgs e)
        {
            Item spellbook = _world.GetOrCreateItem(e.Serial);
            spellbook.Graphic = e.SpellbookGraphic;
            spellbook.Clear();

            var ids = e.SpellIds;
            int count = ids?.Count ?? 0;

            for (int i = 0; i < count; i++)
            {
                ushort cc = (ushort)ids[i];
                Item spellItem = Item.Create(_world, cc);
                spellItem.Serial = cc;
                spellItem.Graphic = 0x1F2E;
                spellItem.Amount = cc;
                spellItem.Container = spellbook;
                spellbook.PushToBack(spellItem);
            }

            UIManager.GetGump<SpellbookGump>(spellbook)?.RequestUpdateContents();
        }
    }
}
