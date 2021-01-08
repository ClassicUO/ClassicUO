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
using ClassicUO.Configuration;
using ClassicUO.Game.InteropServices.Runtime.Macros;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using System;

namespace ClassicUO.Game.InteropServices.Runtime.Autos
{
    internal class AutoMimic
    {
        public static bool IsEnabled { get; set; }

        public static void Toggle()
        {
            GameActions.Print(String.Format("Mimic :{0}abled", (IsEnabled = !IsEnabled) == true ? "En" : "Dis"), 70);
        }

        static AutoMimic()
        {
            IsEnabled = false;
        }

        public static void Initialize()
        {
            // Create command to mimic target
            CommandManager.Register("mimic", args => Toggle());
        }

        public static void SyncByClilocString(uint target, string text)
        {
            //ENABLED CHECK
            if (!IsEnabled)
                return;

            //IDENTIFY WHAT SPELL HE CASTED

            if (SpellDefinition.WordToTargettype.TryGetValue(text, out SpellDefinition spell))
            {
                if (spell.TargetType == TargetType.Beneficial)
                {
                    DoBeneficial();
                }
                else if (spell.TargetType == TargetType.Harmful)
                {
                    DoHarmful(target, spell);
                }
                else
                {
                    //DO NOTHING
                }
            }
            else
            {
                //THIS IS INCASE RAZOR OR ANOTHER ASSISTANT REWRITES THE STRIN

                foreach (var key in SpellDefinition.WordToTargettype.Keys)
                {
                    if (text.Contains(key)) //SPELL FOUND
                    {
                        if (SpellDefinition.WordToTargettype[key].TargetType == TargetType.Beneficial)
                        {
                            DoBeneficial();
                        }
                        else if (SpellDefinition.WordToTargettype[key].TargetType == TargetType.Harmful)
                        {
                            DoHarmful(target, SpellDefinition.WordToTargettype[key]);
                        }
                        else
                        {
                            //DO NOTHING
                        }
                        break; //GET OUT OF LOOP
                    }   
                }
            }
        }

        private static void DoHarmful(uint target, SpellDefinition spell)
        {
            Mobile mobile = World.Mobiles.Get(target);

            if (mobile != null && mobile.Serial == ProfileManager.CurrentProfile.Mimic_PlayerSerial)
            {
                //DO IT
                GameActions.Print($"Syncing by ClilocString", 88);

                //CHECK IF WE HAVE A TARGETCURSOR UP
                if (TargetManager.IsTargeting)
                {
                    // Maybe check to see if its an explosion beforehand?
                    // Drop whatever we have for now on lasttarget..
                    GameActions.Print($"Syncing by ClilocString dropping spell on lasttarget", 88);
                    TargetManager.Target(TargetManager.LastTargetInfo.Serial);
                    TimeSpan.FromMilliseconds(250);

                }
                GameActions.Print($"Syncing by ClilocString casting {spell.Name}", 88);
                GameActions.CastSpell((int) spell.ID);
            }
        }
        private static void DoBeneficial()
        {
            if (World.Party.Contains(World.Player))
            {
                AutoDefender.DefendParty();
            }
            else
            {
                AutoDefender.DefendSelf();
            }
        }
    }
}

