#region license
//  Copyright (C) 2019 ClassicUO Development Community on Github
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
using ClassicUO.Game.UI.Gumps;

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

        private ItemGumpPaperdoll _backpackGump;
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
        }
        
        public Mobile Mobile
        {
            get => _mobile;
            set
            {
                if (value != _mobile)
                {
                    _mobile = value;
                    UpdateEntity();
                }
            }
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (_mobile == null || _mobile.IsDestroyed)
                Dispose();
        }


        public override void Dispose()
        {
            Mobile.Items.Added -= ItemsOnAdded;
            Mobile.Items.Removed -= ItemsOnRemoved;
            if (_backpackGump != null) _backpackGump.MouseDoubleClick -= OnDoubleclickBackpackGump;
            base.Dispose();
        }

        private void ItemsOnRemoved(object sender, CollectionChangedEventArgs<Serial> e)
        {
            UpdateEntity();
        }

        private void ItemsOnAdded(object sender, CollectionChangedEventArgs<Serial> e)
        {
            if (_fakeItem != null)
            {
                foreach (Serial item in e)
                {
                    if (item == _fakeItem.Serial)
                    {
                        _fakeItem = null;
                        break;
                    }
                }
            }

            UpdateEntity();
        }

        public void Update()
        {
            UpdateEntity();
        }


        public void AddFakeDress(Item item)
        {
            if (item == null && _fakeItem != null)
            {
                _fakeItem = null;
                UpdateEntity();
            }
            else if (item != null && _mobile.Equipment[item.ItemData.Layer] == null)
            {
                _fakeItem = item;
                UpdateEntity();
            }
        }

        private void UpdateEntity()
        {
            if (Mobile == null || Mobile.IsDestroyed)
            {
                Dispose();
                return;
            }

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

            if (isGM)
            {
                Add(new GumpPic(0, 0, body, 0x03EA)
                {
                    AcceptMouseInput = true,
                    IsPaperdoll = true,
                    IsPartialHue = true
                });
                Add(new GumpPic(0, 0, 0xC72B, 0)
                {
                    AcceptMouseInput = true,
                    IsPaperdoll = true,
                    IsPartialHue = true
                });
            }
            else
            {
                Add(new GumpPic(0, 0, body, _mobile.Hue)
                {
                    AcceptMouseInput = true,
                    IsPaperdoll = true,
                    IsPartialHue = true
                });

                if (Mobile.HasEquipment)
                {
                    for (int i = 0; i < _layerOrder.Length; i++)
                    {
                        int layerIndex = (int) _layerOrder[i];
                        Item item = _mobile.Equipment[layerIndex];
                        bool isfake = false;
                        bool canPickUp = World.InGame;

                        if (_fakeItem != null && _fakeItem.ItemData.Layer == layerIndex)
                        {
                            item = _fakeItem;
                            isfake = true;
                            canPickUp = false;
                        }
                        else if (item == null || item.IsDestroyed /*|| MobileView.IsCovered(_mobile, (Layer)layerIndex)*/)
                            continue;

                        switch (_layerOrder[i])
                        {
                            case Layer.Beard:
                            case Layer.Hair:
                                canPickUp = false;

                                break;

                            case Layer.Arms:
                            case Layer.Torso:
                                var robe = _mobile.Equipment[(int) Layer.Robe];

                                if (robe != null)
                                {
                                    continue;
                                }

                                break;


                            case Layer.Helmet:
                                robe = _mobile.Equipment[(int) Layer.Robe];

                                if (robe != null)
                                {
                                    if (robe.Graphic > 0x3173)
                                    {
                                        if (robe.Graphic == 0x4B9D || robe.Graphic == 0x7816)
                                            continue;
                                    }
                                    else
                                    {
                                        if (robe.Graphic <= 0x2687)
                                        {
                                            if (robe.Graphic < 0x2683)
                                            {
                                                if (robe.Graphic < 0x204E || robe.Graphic > 0x204F)
                                                {
                                                    break;
                                                }
                                            }

                                            continue;
                                        }

                                        if (robe.Graphic == 0x2FB9 || robe.Graphic == 0x3173)
                                            continue;
                                    }
                                }

                                break;
                        }

                        Add(new ItemGumpPaperdoll(0, 0, item, Mobile, isfake)
                        {
                            SlotIndex = i, CanPickUp = canPickUp
                        });
                    }
                }
            }

            if (_mobile.HasEquipment)
            {
                Item backpack = _mobile.Equipment[(int) Layer.Backpack];

                if (backpack != null)
                {
                    Add(_backpackGump = new ItemGumpPaperdoll(0, 0, backpack, Mobile)
                    {
                        AcceptMouseInput = true,
                        CanPickUp = false,
                    });
                    _backpackGump.MouseDoubleClick += OnDoubleclickBackpackGump;
                }
            }
        }

        private void OnDoubleclickBackpackGump(object sender, EventArgs args)
        {
            if (_mobile != null && !_mobile.IsDestroyed && _mobile.HasEquipment)
            {
                Item backpack = _mobile.Equipment[(int) Layer.Backpack];

                ContainerGump backpackGump = Engine.UI.GetByLocalSerial<ContainerGump>(backpack);

                if (backpackGump == null)
                    GameActions.DoubleClick(backpack);
                else
                    backpackGump.BringOnTop();
            }
        }

    }
}