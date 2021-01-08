#region license

// Copyright (C) 2020 project dust765
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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
using ClassicUO.Game.InteropServices.Runtime.Managers;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.InteropServices.Runtime.Autos
{
    internal class Defender
    {
        enum Potion : ushort
        {
            Cure = 0x0F07,
            Heal = 0x0F0C,
            Refresh = 0x0F0B
        }

        static void TryUsePotion(Potion p, out bool found)
        {
            found = false;
            var potion = World.Player.FindItemByGraphic((ushort)p);
            if (potion != null)
            {
                GameActions.DoubleClick(potion);
                found = true;
            }
        }

        public static bool IsEnabled { get; set; }

        public static void Toggle()
        {
            GameActions.Print(String.Format("Defender:{0}abled", (IsEnabled = !IsEnabled) == true ? "En" : "Dis"), 1154);
        }

        public static void Initialize()
        {
            CommandManager.Register("df", args => Toggle());
        }

        public static bool IsUsingAnimations { get; set; }

        private static long _timer;
        private static Random _rand = new Random(0);

        private static Mobile mob = null; // ## BEGIN - END ## //

        static Defender()
        {
            IsEnabled = false;
            IsUsingAnimations = true;
        }

        public static void Update(double totalMS)
        {
            if (!IsEnabled || World.Player.IsDead)
                return;

            if (_timer <= totalMS)
            {
                if (TargetManager.IsTargeting)
                {
                    if (mob == null)
                        return;

                    SpellAction spell = SpellManager.LastSpell;

                    if (spell == SpellAction.Unknown)
                        spell = (SpellAction) GameActions.LastSpellIndexCursor;

                    int h1 = 0;
                    int h2 = 0;

                    if (!mob.IsDead && spell == SpellAction.GreaterHeal && (mob.IsPoisoned && mob.Hits < (h1 = _rand.Next(65, 80)) || mob.Hits < (h2 = _rand.Next(40, 70))))
                    {
                        GameActions.Print($"{h2}");
                        TargetManager.Target(mob);
                        _timer += 250;
                    }
                }
            }

            if (mob != null)
                mob = null;
        }

        //// ## BEGIN - END ## //
        internal static Mobile Scan(uint triggerMobile)
        {
            if (triggerMobile == World.Player)
            {
                return World.Player;
            }

            bool inParty = World.Party.Leader != 0;
            if (inParty)
            {
                foreach (PartyMember pm in World.Party.Members)
                {
                    if (pm == null)
                        continue;

                    Mobile mob = World.Mobiles.Get(pm.Serial);
                    if (triggerMobile == mob)
                    {
                        return mob;
                    }
                }
            }

            return null;
        }

        public static void gfxTrigger(uint source, uint target, ushort graphic)
        {
            if (!IsEnabled || World.Player.IsDead)
                return;

            switch (graphic)
            {
                case 14013:
                case 14089:
                    _timer += 10;
                    mob = Scan(source);
                    break;

                case 14239://
                case 14027://     
                    _timer += 100;
                    mob = Scan(target);

                    break;
            }
        }
        //// ## BEGIN - END ## //
    }
}