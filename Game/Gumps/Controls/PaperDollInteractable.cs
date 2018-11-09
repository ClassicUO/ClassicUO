#region license

//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
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

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.UIGumps;
using ClassicUO.Game.Views;
using ClassicUO.Interfaces;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Gumps.Controls
{
    internal class PaperDollInteractable : Gump, IMobilePaperdollOwner
    {
        private static readonly PaperDollEquipSlots[] _layerOrder =
        {
            PaperDollEquipSlots.Legging, PaperDollEquipSlots.Footwear, PaperDollEquipSlots.Shirt, PaperDollEquipSlots.Sleeves, PaperDollEquipSlots.Ring, PaperDollEquipSlots.Bracelet, PaperDollEquipSlots.Gloves,   PaperDollEquipSlots.Neck, PaperDollEquipSlots.Chest, PaperDollEquipSlots.Hair, PaperDollEquipSlots.FacialHair, PaperDollEquipSlots.Head, PaperDollEquipSlots.Sash, PaperDollEquipSlots.Earring, PaperDollEquipSlots.Back, PaperDollEquipSlots.Skirt, PaperDollEquipSlots.Robe, PaperDollEquipSlots.LeftHand, PaperDollEquipSlots.RightHand , PaperDollEquipSlots.Belt, PaperDollEquipSlots.Talisman
        };

        private bool _isElf;
        private Mobile _mobile;
        private GumpPicBackpack _backpackGump;
        private Item _fakeItem;

        public PaperDollInteractable(int x, int y, Mobile mobile) : base(0, 0)
        {
            X = x;
            Y = y;
            Mobile = mobile;
            AcceptMouseInput = false;
        }

        public Mobile Mobile
        {
            get => _mobile;

            set
            {
                if (value != _mobile)
                {
                    _mobile?.ClearCallBacks(OnEntityUpdated, OnEntityDisposed);   
                    _mobile = value;
                    OnEntityUpdated(_mobile);
                    _mobile.SetCallbacks(OnEntityUpdated, OnEntityDisposed);                 
                }
            }
        }

        public override void Dispose()
        {
            _mobile.ClearCallBacks(OnEntityUpdated, OnEntityDisposed);
            if (_backpackGump != null) _backpackGump.MouseDoubleClick -= OnDoubleclickBackpackGump;
            base.Dispose();
        }


        public void Update()
        {
            OnEntityUpdated(_mobile);
        }

        public void AddFakeDress(Item item)
        {
            if (item == null && _fakeItem != null)
            {
                _fakeItem.Dispose();
                _fakeItem = null;
            }
            else if (_mobile != null && item != null && _mobile.Equipment[item.ItemData.Layer] == null)
            {
                _fakeItem = item;
            }
        }



        private void OnEntityUpdated(Entity entity)
        {
            Clear();

            // Add the base gump - the semi-naked paper doll.

            Graphic body = 0;

            if (_mobile == World.Player)
            {
                switch (_mobile.Race)
                {
                    case RaceType.HUMAN:
                        body = (Graphic) (0xC + (_mobile.IsFemale ? 1 : 0));

                        break;
                    case RaceType.ELF:
                        body = (Graphic) (0xE + (_mobile.IsFemale ? 1 : 0));

                        break;
                    case RaceType.GARGOYLE:
                        body = (Graphic) (0x29A + (_mobile.IsFemale ? 1 : 0));

                        break;
                }
            }
            else
                body = (Graphic) (12 + (_mobile.IsFemale ? 1 : 0));
 
            AddChildren(new GumpPic(0, 0, body, _mobile.Hue)
            {
                AcceptMouseInput = true,
                IsPaperdoll = true
            });
            
            

            // Loop through the items on the mobile and create the gump pics.
            for (int i = 0; i < _layerOrder.Length; i++)
            {
                int layerIndex = (int) _layerOrder[i];
                Item item = _mobile.Equipment[layerIndex];

                bool isfake = false;
                bool canPickUp = true;

                if (_fakeItem != null && _fakeItem.ItemData.Layer == layerIndex)
                {
                    isfake = true;
                    canPickUp = false;                   
                }
                else if (item == null || MobileView.IsCovered(_mobile, (Layer)layerIndex))
                    continue;

                switch (_layerOrder[i])
                {
                    case PaperDollEquipSlots.FacialHair:
                    case PaperDollEquipSlots.Hair:
                        canPickUp = false;

                        break;
                }

                AddChildren(new ItemGumplingPaperdoll(0, 0, isfake ? _fakeItem : item, Mobile, isfake)
                {
                    SlotIndex = i,
                    IsFemale = _mobile.IsFemale,
                    CanPickUp = canPickUp
                });

            }

            // If this object has a backpack, add it last.
            if (_mobile.Equipment[(int) PaperDollEquipSlots.Backpack] != null)
            {
                Item backpack = _mobile.Equipment[(int) PaperDollEquipSlots.Backpack];
                AddChildren(_backpackGump = new GumpPicBackpack(-7, 0, backpack)
                {
                    AcceptMouseInput = true
                });
                _backpackGump.MouseDoubleClick += OnDoubleclickBackpackGump;
            }
        }

        private void OnEntityDisposed(Entity entity)
        {
            Dispose();
        }

        private void OnDoubleclickBackpackGump(object sender, EventArgs args)
        {
            Item backpack = _mobile.Equipment[(int) PaperDollEquipSlots.Backpack];
            GameActions.DoubleClick(backpack);
        }

        private enum PaperDollEquipSlots
        {
            Body = 0,
            RightHand = 1,
            LeftHand = 2,
            Footwear = 3,
            Legging = 4,
            Shirt = 5,
            Head = 6,
            Gloves = 7,
            Ring = 8,
            Talisman = 9,
            Neck = 10,
            Hair = 11,
            Belt = 12,
            Chest = 13,
            Bracelet = 14,
            Unused = 15,
            FacialHair = 16,
            Sash = 17,
            Earring = 18,
            Sleeves = 19,
            Back = 20,
            Backpack = 21,
            Robe = 22,
            Skirt = 23
        }
    }
}