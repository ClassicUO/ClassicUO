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
using ClassicUO.Game.Scenes;
using System;

namespace ClassicUO.Game.InteropServices.Runtime.Managers
{
    class EquipManager
    {        
        public static UseItemQueue _useItemQueue = new UseItemQueue();
        internal enum Weapons : ushort
        {
            Katana = 0x13FF,
            Hally = 0x143E,
            ExecutionerAxe = 0xf45,
            HeavyXbow = 0x13FD,
            Bow = 0x13B2,
            Spear = 0x0F62,
            ShortSpear = 0x1403,
            Kryss = 0x1401,
            QuarterStaff = 0x0E89,
            WarHammer = 0x1439,
            WarMace = 0x1407,
            Mace = 0x0F5C,
            Maul = 0x143B,
            WarAxe = 0x13B0,
            HammerPick = 0x143D,
            Club = 0x13B4,
            GnarledStaff = 0x13F8,
            CrossBow = 0x0F50,
            Axe = 0x0F49,
            TwoHandedAxe = 0x1443,
            DoubleAxe = 0xF4B,
            LargeBattleAxe = 0x13FB,
            BattleAxe = 0x0F47,
            Cutlass = 0x1441,
            VikingSword = 0x13B9,
            BroadSword = 0x0F5E,
            LongSword = 0xF61,
            Scimitar = 0x13B6,
            Bardiche = 0x0F4D,
            WarFork = 0x1405,
            KiteShield = 0x1B75,
            BronzeShield = 0x1B72,
            HeaterShield = 0x1B77,
            ChaosShield = 0x1BC3,
            OrderShield = 0x1BC5
        }
        /*internal enum Shields : ushort
        {
            KiteShield = 0x1B75, BronzeShield = 0x1B72, HeaterShield = 0x1B77, ChaosShield = 0x1BC3, OrderShield = 0x1BC5
        }*/
        public enum Leather : ushort
        {
            Leggings = 0x13D2,
            Chest = 0x13D3,
            Arms = 0x13C5,
            Gloves = 0x13CE,
            Necklace = 0x13C7,
            Helmet = 0x1DBA
        }
        public static void LocateLeather(Leather l, out bool found)
        {
            var _backpack = World.Player.FindItemByLayer(Layer.Backpack);
            var leather = World.Player.FindItemByGraphic((ushort) l);
            found = false;

            if (_backpack == null)
                found = false;
            if (leather != null)
            {
                GameActions.DoubleClickQueued(leather);
                found = true;
            }
        }
        //public static Item _backpack = World.Player.Equipment[(int) Layer.Backpack];
        public static Item EM_FindUsedLayer()
        {
            Item layeredItem = World.Player.FindItemByLayer(Layer.OneHanded);
            if (layeredItem != null && World.Player.FindItemByLayer(Layer.TwoHanded) == null)
                return layeredItem;

            layeredItem = World.Player.FindItemByLayer(Layer.TwoHanded);
            if (layeredItem != null && World.Player.FindItemByLayer(Layer.OneHanded) == null)
                return layeredItem;

            // - For Shield, You can maintain keeping your weapon equipped and just toggle your shield with this##//
            Item layeredItem1 = World.Player.FindItemByLayer(Layer.OneHanded);
            layeredItem = World.Player.FindItemByLayer(Layer.TwoHanded);
            if (layeredItem != null)// && World.Player.Equipment[(int) Layer.TwoHanded] == World.Player.Equipment.GetType().IsEquivalentTo((Type) Shields))
                return layeredItem1;
            return null;
        }

        public static void EM_UnEquip()
        {

            GameScene gs2 = Client.Game.GetScene<GameScene>();
            var _backpack = World.Player.FindItemByLayer(Layer.Backpack);

            if (_backpack == null)
                return;

            if (ItemHold.Enabled)
                return;

            Item unequippedItem = EM_FindUsedLayer();
            if (unequippedItem != null)
            {
                GameActions.PickUp(unequippedItem.Serial, 0, 0, 1);
                if (ItemHold.Enabled && !ItemHold.Dropped)
                {
                    int x = 0;
                    int y = 0;
                    TimeSpan.FromMilliseconds(50);

                    GameActions.DropItem(ItemHold.Serial, 0xFFFF, 0xFFFF, 0, _backpack.Serial);

                    TimeSpan.FromMilliseconds(50);
                }
            }
        }
        public static void EquipWeapon(Weapons EquipWep)
        {
            GameScene gs1 = Client.Game.GetScene<GameScene>();
            var _backpack = World.Player.FindItemByLayer(Layer.Backpack);
            var Weapon = World.Player.FindItemByGraphic((ushort) EquipWep);

            Item equippedItem = EM_FindUsedLayer();

            if (ItemHold.Enabled)
                return;
            if (_backpack == null)
                return;

            if (World.Player != null && !World.Player.IsDead)
            {
                if (EM_FindUsedLayer() != null)
                {
                    EM_UnEquip();
                    TimeSpan.FromMilliseconds(50);
                }
                else if (Weapon != null && equippedItem == null)
                {
                    GameActions.PickUp(Weapon.Serial, 0, 0, 1);
                    TimeSpan.FromMilliseconds(50);

                    if (ItemHold.Enabled && !ItemHold.Dropped)
                        GameActions.Equip();
                    else
                        return;

                    TimeSpan.FromMilliseconds(50);
                }
            }
        }

        public static void EquipLeather()
        {
            var _backpack = World.Player.FindItemByLayer(Layer.Backpack);

            if (ItemHold.Enabled)
                return;
            if (_backpack == null)
                return;

            if (World.Player != null && !World.Player.IsDead)
            {
                if (World.Player.FindItemByLayer(Layer.Pants) == null)
                {
                    LocateLeather(Leather.Leggings, out bool found);
                    if (found == true)
                    {
                        TimeSpan.FromMilliseconds(50);
                    }
                    else
                        return;
                }
                else return;

                if (World.Player.FindItemByLayer(Layer.Torso) == null)
                {
                    LocateLeather(Leather.Chest, out bool found);
                    if (found == true)
                    {
                        TimeSpan.FromMilliseconds(50);
                    }
                    else
                        return;
                }
                else return;

                if (World.Player.FindItemByLayer(Layer.Arms) == null)
                {
                    LocateLeather(Leather.Arms, out bool found);
                    if (found == true)
                    {
                        TimeSpan.FromMilliseconds(50);
                    }
                    else
                        return;
                }
                else return;

                if (World.Player.FindItemByLayer(Layer.Gloves) == null)
                {
                    LocateLeather(Leather.Gloves, out bool found);
                    if (found == true)
                    {
                        TimeSpan.FromMilliseconds(50);
                    }
                    else
                        return;
                }
                else return;

                if (World.Player.FindItemByLayer(Layer.Helmet) == null)
                {
                    LocateLeather(Leather.Helmet, out bool found);
                    if (found == true)
                    {
                        TimeSpan.FromMilliseconds(50);
                    }
                    else
                        return;
                }
                else return;

                if (World.Player.FindItemByLayer(Layer.Necklace) == null)
                {
                    LocateLeather(Leather.Necklace, out bool found);
                    if (found == true)
                    {
                        TimeSpan.FromMilliseconds(50);
                    }
                    else
                        return;
                }
                else return;
            }
        }

        public static Item EM_FindCustomWeapon()
        {
            Item customWeapon = World.Player.FindItemByLayer(Layer.OneHanded);
            if (customWeapon != null)
            {
                return customWeapon;
            }

            customWeapon = World.Player.FindItemByLayer(Layer.TwoHanded);
            if (customWeapon != null)
            {
                return customWeapon;
            }

            return null;
        }
        public static void EquipCustom()
        {
            GameScene gs11 = Client.Game.GetScene<GameScene>();
            var _backpack = World.Player.FindItemByLayer(Layer.Backpack);
            Item customweapon = World.Items.Get(ProfileManager.CurrentProfile.CustomSerial);

            if (World.Player != null && !World.Player.IsDead)
            {
                if (ItemHold.Enabled)
                    return;
                if (_backpack == null)
                    return;
                if (customweapon == null)
                    return;

                if (customweapon.Serial == 0|| ProfileManager.CurrentProfile.CustomSerial == 0)
                {
                    if (World.Player.FindItemByLayer(Layer.OneHanded) != null)
                    {
                        customweapon.Serial = World.Player.FindItemByLayer(Layer.OneHanded).Serial;
                        GameActions.Print("Custom Item not defined, Setting current equipped Item", 33, MessageType.System);
                    }
                    else if (World.Player.FindItemByLayer(Layer.OneHanded) != null)
                    {
                        customweapon.Serial = World.Player.FindItemByLayer(Layer.TwoHanded).Serial;
                        GameActions.Print("Custom Item not defined, Setting current equipped Item", 33, MessageType.System);
                    }
                    else
                    {
                        TargetManager.SetTargeting(CursorTarget.SetCustomSerial, 0, TargetType.Neutral);
                        GameActions.Print("Custom Item not defined, Using SetCustomSerial Hotkey.", 33, MessageType.System);
                    }
                }

                if (EM_FindCustomWeapon() != null)
                {
                    EM_UnEquip();
                    TimeSpan.FromMilliseconds(50);
                }
                else if (customweapon != null && customweapon.Serial != 0)
                {
                    customweapon.Serial = ProfileManager.CurrentProfile.CustomSerial;
                    GameActions.PickUp(customweapon.Serial, 0, 0, 1);
                    TimeSpan.FromMilliseconds(50);

                    if (ItemHold.Enabled && !ItemHold.Dropped)
                        GameActions.Equip();
                    else
                        return;

                    if (ItemHold.Dropped)
                        ItemHold.Clear();
                    else
                        return;
                }
            }

        }
    }
}
