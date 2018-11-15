#region license

//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//  This project is an alternative client for the game Ultima Online.
//  The goal of this is to develop a lightweight client considering 
//  new technologies.
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

using System;

using ClassicUO.Game.Gumps.Controls;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.UIGumps
{
    internal class StatusGumpOutlands : StatusGumpBase
    {
        public StatusGumpOutlands() : base()
        {
            Point pos = Point.Zero;

            AddChildren(new GumpPic(0, 0, 0x2A6C, 0));
            AddChildren(new GumpPic(34, 12, 0x0805, 0));    // Health bar
            AddChildren(new GumpPic(34, 25, 0x0805, 0));    // Mana bar
            AddChildren(new GumpPic(34, 38, 0x0805, 0));    // Stamina bar

            Graphic gumpIdHp = 0x0806;

            if (_mobile.IsPoisoned)
            {
                gumpIdHp = 0x0808;
            }
            else if (_mobile.IsYellowHits)
            {
                gumpIdHp = 0x0809;
            }

            FillStatusBar(34, 12, _mobile.Hits, _mobile.HitsMax, gumpIdHp);
            FillStatusBar(34, 25, _mobile.Mana, _mobile.ManaMax, 0x0806);
            FillStatusBar(34, 38, _mobile.Stamina, _mobile.StaminaMax, 0x0806);

            // Name
            // [0] Locks
            // [1] Str/stam/mana
            // [2] Hits / max hits
            // [2] Stam / max stam
            // [2] Mana / max mana
            // [3] Followers / max followers
            // [3] Armor
            // [3] Weight / max weight
            // [4] Hunger satisfaction minutes remaining
            // [4] Murder count
            // [4] Min damange - max damage
            // [4] Gold
            // [5] Criminal timer seconds remaining
            // [5] Murder count decay hours remaining
            // [5] PvP cooldown seconds remaining
            // [5] Bandage timer seconds remaining
            // [5] Minimize hitbox
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (_refreshTime < totalMS)
            {
                _refreshTime = totalMS + 250;

                // TODO: Update labels (from World.Player)
            }

            base.Update(totalMS, frameMS);
        }

        private void FillStatusBar(int x, int y, int current, int max, Graphic gumpId)
        {
            // TODO: max 109?
            int percent = (current * 100) / max;
            percent = (int)Math.Ceiling(100.0);

            if (percent > 0)
            {
                AddChildren(new GumpPicTiled(x, y, percent, 10, gumpId));
            }
        }

        private enum MobileStats
        {
            Name,
            Strength,
            Dexterity,
            Intelligence,
            HealthCurrent,
            HealthMax,
            StaminaCurrent,
            StaminaMax,
            ManaCurrent,
            ManaMax,
            WeightMax,
            Followers,
            WeightCurrent,
            LowerReagentCost,
            SpellDamageInc,
            FasterCasting,
            FasterCastRecovery,
            StatCap,
            HitChanceInc,
            DefenseChanceInc,
            LowerManaCost,
            DamageChanceInc,
            SwingSpeedInc,
            Luck,
            Gold,
            AR,
            RF,
            RC,
            RP,
            RE,
            Damage,
            Sex,
            Max
        }
    }
}
