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

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.UI.Controls
{
    internal class PaperDollInteractable : Control
    {
        private static readonly Layer[] _layerOrder =
        {
            Layer.Cloak, Layer.Ring, Layer.Shirt, Layer.Arms, Layer.Pants, Layer.Shoes, Layer.Legs,
            Layer.Torso, Layer.Bracelet, Layer.Face, Layer.Gloves, Layer.Tunic, Layer.Skirt, Layer.Necklace,
            Layer.Hair, Layer.Robe, Layer.Earrings, Layer.Beard, Layer.Helmet, Layer.Waist, Layer.OneHanded, Layer.TwoHanded, Layer.Talisman
        };

        private GumpPicBackpack _backpackGump;
        private Item _fakeItem;
        private Mobile _mobile;

        public PaperDollInteractable(int x, int y, Mobile mobile)
        {
            X = x;
            Y = y;
            Mobile = mobile;
            AcceptMouseInput = false;
            mobile.Items.Added += ItemsOnAdded;
            mobile.Items.Removed += ItemsOnRemoved;
            mobile.Disposed += MobileOnDisposed;
        }

        public Mobile Mobile
        {
            get => _mobile;
            set
            {
                if (value != _mobile)
                {
                    _mobile = value;
                    OnEntityUpdated(_mobile);
                }
            }
        }

        public override void Dispose()
        {
            Mobile.Items.Added -= ItemsOnAdded;
            Mobile.Items.Removed -= ItemsOnRemoved;
            Mobile.Disposed -= MobileOnDisposed;
            if (_backpackGump != null) _backpackGump.MouseDoubleClick -= OnDoubleclickBackpackGump;
            base.Dispose();
        }

        private void ItemsOnRemoved(object sender, CollectionChangedEventArgs<Item> e)
        {
            OnEntityUpdated(Mobile);
        }

        private void ItemsOnAdded(object sender, CollectionChangedEventArgs<Item> e)
        {
            if (_fakeItem != null)
            {
                foreach (Item item in e)
                {
                    if (item.Serial == _fakeItem.Serial)
                    {
                        _fakeItem = null;
                        break;
                    }
                }
            }

            OnEntityUpdated(Mobile);
        }

        private void MobileOnDisposed(object sender, EventArgs e)
        {
            Dispose();
        }

        public void Update()
        {
            OnEntityUpdated(Mobile);
        }


        public void AddFakeDress(Item item)
        {
            if (item == null && _fakeItem != null)
            {
                _fakeItem = null;
                OnEntityUpdated(Mobile);
            }
            else if (item != null && _mobile.Equipment[item.ItemData.Layer] == null)
            {
                _fakeItem = item;
                OnEntityUpdated(Mobile);
            }
        }

        private void OnEntityUpdated(Entity entity)
        {
            Clear();

            // Add the base gump - the semi-naked paper doll.
            Graphic body = 0;

            bool isGM = false;

            if (Mobile.Graphic == 0x0191 || Mobile.Graphic == 0x0193)
            {
                body = 0x000D;
            }
            else if (Mobile.Graphic == 0x025D)
            {
                body = 0x000E;
            }
            else if (Mobile.Graphic == 0x025E)
            {
                body = 0x000F;
            }
            else if (Mobile.Graphic == 0x029A)
            {
                body = 0x029A;
            }
            else if (Mobile.Graphic == 0x029B)
            {
                body = 0x299;
            }
            else if (Mobile.Graphic == 0x03DB)
            {
                body = 0x000C;
                isGM = true;
            }
            else
            {
                body = 0x000C;
            }

            //if (_mobile == World.Player)
            //{
            //    switch (_mobile.Race)
            //    {
            //        default:
            //        case RaceType.HUMAN:
            //            body = (Graphic) (0xC + (_mobile.IsFemale ? 1 : 0));

            //            break;
            //        case RaceType.ELF:
            //            body = (Graphic) (0xE + (_mobile.IsFemale ? 1 : 0));

            //            break;
            //        case RaceType.GARGOYLE:
            //            body = (Graphic) (0x29A + (_mobile.IsFemale ? 1 : 0));

            //            break;
            //    }
            //}
            //else
            //    body = (Graphic) (12 + (_mobile.IsFemale ? 1 : 0));

            

            // Loop through the items on the mobile and create the gump pics.

            //GameScene gs = Engine.SceneManager.GetScene<GameScene>();

            if (isGM)
            {
                AddChildren(new GumpPic(0, 0, body, 0x03EA)
                {
                    AcceptMouseInput = true,
                    IsPaperdoll = true,
                    IsPartialHue = true
                });
                AddChildren(new GumpPic(0, 0, 0xC72B, 0)
                {
                    AcceptMouseInput = true,
                    IsPaperdoll = true,
                    IsPartialHue = true
                });
            }
            else
            {
                AddChildren(new GumpPic(0, 0, body, _mobile.Hue)
                {
                    AcceptMouseInput = true,
                    IsPaperdoll = true,
                    IsPartialHue = true
                });
                for (int i = 0; i < _layerOrder.Length; i++)
                {
                    int layerIndex = (int) _layerOrder[i];
                    Item item = _mobile.Equipment[layerIndex];
                    bool isfake = false;
                    bool canPickUp = true;

                    if (_fakeItem != null && _fakeItem.ItemData.Layer == layerIndex)
                    {
                        item = _fakeItem;
                        isfake = true;
                        canPickUp = false;
                    }
                    else if (item == null /*|| MobileView.IsCovered(_mobile, (Layer)layerIndex)*/)
                        continue;

                    switch (_layerOrder[i])
                    {
                        case Layer.Beard:
                        case Layer.Hair:
                            canPickUp = false;

                            break;
                    }

                    AddChildren(new ItemGumpPaperdoll(0, 0, item, Mobile, isfake)
                    {
                        SlotIndex = i, CanPickUp = canPickUp
                    });
                }
            }

            // If this object has a backpack, add it last.
            if (_mobile.Equipment[(int) Layer.Backpack] != null)
            {
                Item backpack = _mobile.Equipment[(int)Layer.Backpack];

                AddChildren(_backpackGump = new GumpPicBackpack(-7, 0, backpack)
                {
                    AcceptMouseInput = true
                });
                _backpackGump.MouseDoubleClick += OnDoubleclickBackpackGump;
            }
        }

        private void OnDoubleclickBackpackGump(object sender, EventArgs args)
        {
            Item backpack = _mobile.Equipment[(int)Layer.Backpack];
            GameActions.DoubleClick(backpack);
        }

    }
}