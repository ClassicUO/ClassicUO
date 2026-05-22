// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Data;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.Game
{
    internal sealed partial class World
    {
        private void SubscribeExtendedStats()
        {
            EventSink.ExtendedStatsBonded += OnExtendedStatsBonded;
            EventSink.ExtendedStatsLocks += OnExtendedStatsLocks;
            EventSink.ExtendedStatsAnimation += OnExtendedStatsAnimation;
            EventSink.SpellbookContent += OnSpellbookContent;
        }

        private void UnsubscribeExtendedStats()
        {
            EventSink.ExtendedStatsBonded -= OnExtendedStatsBonded;
            EventSink.ExtendedStatsLocks -= OnExtendedStatsLocks;
            EventSink.ExtendedStatsAnimation -= OnExtendedStatsAnimation;
            EventSink.SpellbookContent -= OnSpellbookContent;
        }

        private void OnExtendedStatsBonded(ExtendedStatsBondedArgs e)
        {
            Mobile bonded = Mobiles.Get(e.Serial);
            if (bonded == null) return;

            bonded.IsDead = e.IsDead;
        }

        private void OnExtendedStatsLocks(ExtendedStatsLocksArgs e)
        {
            if (Player == null) return;
            if (e.Serial != Player.Serial) return;

            Player.StrLock = (Lock)e.StrLock;
            Player.DexLock = (Lock)e.DexLock;
            Player.IntLock = (Lock)e.IntLock;

            StatusGumpBase.GetStatusGump()?.RequestUpdateContents();
        }

        private void OnExtendedStatsAnimation(ExtendedStatsAnimationArgs e)
        {
            Mobile mobile = Mobiles.Get(e.Serial);
            if (mobile == null) return;

            mobile.SetAnimation(
                Mobile.GetReplacedObjectAnimation(mobile.Graphic, e.Animation)
            );
            mobile.ExecuteAnimation = false;
            mobile.AnimIndex = (byte)e.Frame;
        }

        private void OnSpellbookContent(SpellbookContentArgs e)
        {
            Item spellbook = GetOrCreateItem(e.Serial);
            spellbook.Graphic = e.SpellbookGraphic;
            spellbook.Clear();

            var ids = e.SpellIds;
            int count = ids?.Count ?? 0;

            for (int i = 0; i < count; i++)
            {
                ushort cc = (ushort)ids[i];
                Item spellItem = Item.Create(this, cc);
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
