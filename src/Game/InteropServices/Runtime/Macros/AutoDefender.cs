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
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using System;

namespace ClassicUO.Game.InteropServices.Runtime.Macros
{
    internal class AutoDefender
    {
        public static Item _backpack = World.Player.FindItemByLayer(Layer.Backpack);
        public enum Potions : ushort
        {
            Cure = 0x0F07,
            Heal = 0x0F0C,
            Refresh = 0x0F0B,
            Agility = 0x0F08,
            Strength = 0x0F09
        }

        public static void DefendParty()
        {
            //
            var healpotion = World.Player.FindItemByGraphic(0x0F0C);
            //
            if (!World.Player.IsDead && World.Player.Exists && World.Player != null && ProfileManager.CurrentProfile != null)
            {
                foreach (Mobile mobile in World.Mobiles)
                {
                    if (World.Mobiles.Get(mobile.Serial).Distance < 12 && mobile.IsHuman && ProfileManager.CurrentProfile != null)
                    {
                        if (mobile.Name.Length == 0 || mobile.Name == null && ProfileManager.CurrentProfile != null)
                        {
                            TimeSpan.FromMilliseconds(125);
                            return;
                        }
                        else
                            foreach (Mobile mobile1 in World.Mobiles)
                            {
                                if (mobile.NotorietyFlag == NotorietyFlag.Ally || World.Party.Contains(mobile.Serial) && mobile != World.Player && ProfileManager.CurrentProfile != null)
                                {
                                    if (!mobile.IsDead && mobile != null && SerialHelper.IsMobile(mobile.Serial) && ProfileManager.CurrentProfile != null)
                                    {
                                        if (SerialHelper.IsValid(mobile.Serial) && ProfileManager.CurrentProfile != null)
                                        {
                                            if (mobile.Hits < 64 && mobile.Distance < 12 && ProfileManager.CurrentProfile != null)
                                            {
                                                if (TargetManager.IsTargeting)
                                                {
                                                    if (SerialHelper.IsValid(mobile.Serial) && mobile.Distance < 12 && ProfileManager.CurrentProfile != null)
                                                    {
                                                        TargetManager.Target(mobile.Serial);

                                                        TimeSpan.FromMilliseconds(125);
                                                        break;
                                                    }
                                                    else
                                                        break;
                                                }
                                                else if (!TargetManager.IsTargeting)
                                                {
                                                    GameActions.CastSpell(29);

                                                    TimeSpan.FromMilliseconds(375);

                                                    if (!TargetManager.IsTargeting)
                                                    {
                                                        TimeSpan.FromMilliseconds(125);
                                                    }
                                                    else
                                                        continue;
                                                    
                                                    if (TargetManager.IsTargeting && SerialHelper.IsValid(mobile.Serial) && mobile.Distance < 12 && ProfileManager.CurrentProfile != null)
                                                    {

                                                        TargetManager.Target(mobile.Serial);

                                                        TimeSpan.FromMilliseconds(125);
                                                        break;
                                                    }
                                                    else
                                                        break;
                                                }
                                            }
                                            else if (World.Player.Hits < 64 && ProfileManager.CurrentProfile != null)
                                            {
                                                if (TargetManager.IsTargeting)
                                                {
                                                    if (!World.Player.IsDead && World.Player.Hits < 64 && ProfileManager.CurrentProfile != null)
                                                    {

                                                        TargetManager.Target(World.Player);

                                                        TimeSpan.FromMilliseconds(125);
                                                        if (healpotion != null && ProfileManager.CurrentProfile != null)
                                                        {
                                                            GameActions.DoubleClick(healpotion);
                                                            GameActions.Print("AD: Heal Potion used");
                                                        }
                                                        else
                                                            GameActions.Print("AD: No Heal Potions.");

                                                        TimeSpan.FromMilliseconds(125);
                                                        break;
                                                    }
                                                    else
                                                        break;
                                                }
                                                else if (!TargetManager.IsTargeting)
                                                {
                                                    GameActions.CastSpell(29);

                                                    TimeSpan.FromMilliseconds(375);

                                                    if (!TargetManager.IsTargeting)
                                                    {
                                                        TimeSpan.FromMilliseconds(125);
                                                    }
                                                    else
                                                        continue;
                                                    
                                                    if (TargetManager.IsTargeting && !World.Player.IsDead && World.Player.Hits < 64 && ProfileManager.CurrentProfile != null)
                                                    {

                                                        TargetManager.Target(World.Player);

                                                        TimeSpan.FromMilliseconds(125);
                                                        if (healpotion != null && ProfileManager.CurrentProfile != null)
                                                        {
                                                            GameActions.DoubleClickQueued(healpotion);
                                                            GameActions.Print("AD: Heal Potion used");
                                                        }
                                                        else
                                                            GameActions.Print("AD: No Heal Potions.");

                                                        TimeSpan.FromMilliseconds(125);
                                                        break;
                                                    }
                                                    else
                                                        break;
                                                }
                                            }
                                            else
                                            {
                                                GameActions.Print("AD: Nothing to do.");
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        continue;
                    }
                    else
                        GameActions.Print("AD: Nobody near.");
                }
            }
        }

        public static void DefendSelf()
        {
            //
            var healpotion = World.Player.FindItemByGraphic(0x0F0C);
            //
            if (!World.Player.IsDead && World.Player.Exists && World.Player != null && ProfileManager.CurrentProfile != null)
            {
                if (World.Player.Hits < 64 && ProfileManager.CurrentProfile != null)
                {
                    if (TargetManager.IsTargeting)
                    {
                        if (!World.Player.IsDead && World.Player.Hits < 64 && ProfileManager.CurrentProfile != null)
                        {

                            TargetManager.Target(World.Player);

                            TimeSpan.FromMilliseconds(125);
                            if (healpotion != null && ProfileManager.CurrentProfile != null)
                            {
                                GameActions.DoubleClick(healpotion);
                                GameActions.Print("AD: Heal Potion used");
                            }
                            else
                                GameActions.Print("AD: No Heal Potions.");


                            TimeSpan.FromMilliseconds(125);
                            return;
                        }
                        else
                            return;
                    }
                    else if (!TargetManager.IsTargeting)
                    {
                        GameActions.CastSpell(29);

                        TimeSpan.FromMilliseconds(375);

                        if (!TargetManager.IsTargeting)
                        {
                            TimeSpan.FromMilliseconds(125);
                        }
                        else
                            return;

                        if (TargetManager.IsTargeting && !World.Player.IsDead && World.Player.Hits < 64 && ProfileManager.CurrentProfile != null)
                        {

                            TargetManager.Target(World.Player);

                            TimeSpan.FromMilliseconds(125);
                            if (healpotion != null)
                            {
                                GameActions.DoubleClickQueued(healpotion);
                                GameActions.Print("AD: Heal Potion used");
                            }
                            else
                                GameActions.Print("AD: No Heal Potions.");

                            TimeSpan.FromMilliseconds(125);
                            return;
                        }
                        else
                            return;
                    }
                }
                else
                {
                    GameActions.Print("AD: Nothing to do.");
                    return;
                }
            }
        }
    }
}

