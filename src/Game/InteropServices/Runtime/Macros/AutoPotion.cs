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
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using System;

namespace ClassicUO.Game.InteropServices.Runtime.Macros
{
    internal class AutoPotion
    {
        public enum Potions : ushort
        {
            Cure = 0x0F07,
            Heal = 0x0F0C,
            Refresh = 0x0F0B,
            Agility = 0x0F08,
            Strength = 0x0F09
        }
        public static void FindAndUsePotion(Potions p, out bool found)
        {
            var _backpack = World.Player.FindItemByLayer(Layer.Backpack);

            found = false;
            var potion = World.Player.FindItemByGraphic((ushort) p);
            if (potion != null)
            {
                GameActions.DoubleClickQueued(potion);
                GameActions.Print("AP: Using Potions.");
                found = true;
            }
        }

        public static Item _twohand;

        public static void UseAutoPotion()
        {
            GameScene gs = Client.Game.GetScene<GameScene>();

            if (World.Player != null)
            {
                var _backpack = World.Player.FindItemByLayer(Layer.Backpack);
                if (!World.Player.IsDead && !TargetManager.IsTargeting)
                {
                    if (_backpack == null)
                        return;

                    _twohand = World.Player.FindItemByLayer(Layer.TwoHanded);
                    if (_twohand != null)
                    {
                        GameActions.Print("AP: Disarming.");
                        GameActions.PickUp(_twohand.Serial, 0, 0, 1);
                        TimeSpan.FromMilliseconds(50);
                        GameActions.DropItem(ItemHold.Serial, 0xFFFF, 0xFFFF, 0, _backpack.Serial);
                        TimeSpan.FromMilliseconds(50);
                    }

                    if (World.Player.Hits < 85)
                    {
                        FindAndUsePotion(Potions.Heal, out bool found);
                        if (found == true)
                        {
                            TimeSpan.FromMilliseconds(50);
                        }
                        else
                        {
                            GameActions.Print("AP: No Heal Potions.");
                            return;
                        }
                    }

                    if (World.Player.IsParalyzed)
                    {
                        GameActions.Say($"[pouch");
                        TimeSpan.FromMilliseconds(50);
                    }

                    if (World.Player.IsPoisoned && World.Player.Hits < World.Player.HitsMax)
                    {
                        FindAndUsePotion(Potions.Cure, out bool found);

                        if (found == true)
                        {
                            TimeSpan.FromMilliseconds(50);
                        }
                        else
                        {
                            GameActions.Print("AP: No Cure Potions.");
                            return;
                        }
                    }

                    if (World.Player.Stamina < 23)
                    {
                        FindAndUsePotion(Potions.Refresh, out bool found);
                        if (found == true)
                        {
                            TimeSpan.FromMilliseconds(50);
                        }
                        else
                        {
                            GameActions.Print("AP: No Refresh Potions.");
                            return;
                        }
                    }

                    if (World.Player.Strength < 100)
                    {
                        FindAndUsePotion(Potions.Strength, out bool found);
                        if (found == true)
                        {
                            TimeSpan.FromMilliseconds(50);
                        }
                        else
                        {
                            GameActions.Print("AP: No Strength Potions.");
                            return;
                        }
                    }

                    if (World.Player.Dexterity > 89)
                    {
                        FindAndUsePotion(Potions.Agility, out bool found);
                        if (found == true)
                        {
                            TimeSpan.FromMilliseconds(50);
                        }
                        else
                        {
                            GameActions.Print("AP: No Agility Potions.");
                            return;
                        }
                    }

                    if (!World.Player.IsPoisoned && World.Player.Hits > 85 && World.Player.Stamina > 23 && World.Player.Strength > 100 && World.Player.Dexterity > 100 && !World.Player.IsParalyzed)
                    {
                        GameActions.Print("AP: Nothing to do.");
                        return;
                    }
                }
                else
                    return;
            }
        }
    }
}
