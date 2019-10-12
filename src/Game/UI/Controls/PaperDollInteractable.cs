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
using System.Linq;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.Game.UI.Controls
{
    internal class PaperDollInteractable : Control
    {
        private static readonly Layer[] _layerOrder =
        {
            Layer.Cloak, Layer.Shirt, Layer.Pants, Layer.Shoes, Layer.Legs, Layer.Arms, Layer.Torso, Layer.Tunic,
            Layer.Ring, Layer.Bracelet, Layer.Face, Layer.Gloves, Layer.Skirt, Layer.Robe, Layer.Waist, Layer.Necklace,
            Layer.Hair, Layer.Beard, Layer.Earrings, Layer.Helmet, Layer.OneHanded, Layer.TwoHanded, Layer.Talisman
        };
        private Item _fakeItem;
        private Mobile _mobile;
        private GumpPic _body, _unk;

        private readonly ItemGumpPaperdoll[] _pgumps = new ItemGumpPaperdoll[(int) Layer.Mount]; // _backpackGump;
        private readonly PaperDollGump _paperDollGump;

        public PaperDollInteractable(int x, int y, Mobile mobile, PaperDollGump paperDollGump)
        {
            X = x;
            Y = y;
            _paperDollGump = paperDollGump;
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
            if (_pgumps[(int) Layer.Backpack] != null) _pgumps[(int) Layer.Backpack].MouseDoubleClick -= OnDoubleclickBackpackGump;
            base.Dispose();
        }

        private void ItemsOnRemoved(object sender, CollectionChangedEventArgs<Serial> e)
        {
            foreach (Serial serial in e)
            {
                Item item = World.Items.Get(serial);

                if (item != null && item.Layer >= 0 && (int) item.Layer < _pgumps.Length)
                {
                    if (Mobile == World.Player && (item.Layer == Layer.OneHanded || item.Layer == Layer.TwoHanded))
                    {
                        World.Player.UpdateAbilities();
                    }

                    // this fix is necessary to clean paperdoll
                    if (Mobile.HasEquipment && item.Layer >= 0 && (int)item.Layer < Mobile.Equipment.Length)
                        Mobile.Equipment[(int)item.Layer] = null;

                    ref var gump = ref _pgumps[(int)item.Layer];
                    gump?.Dispose();
                    gump = null;
                }
            }

         
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
                        Item i = World.Items.Get(item);

                        if (i != null && i.Layer >= 0 && (int)i.Layer < _pgumps.Length)
                        {
                            if (Mobile == World.Player && (i.Layer == Layer.OneHanded || i.Layer == Layer.TwoHanded))
                            {
                                World.Player.UpdateAbilities();
                            }

                            ref var gump = ref _pgumps[(int) i.Layer];
                            gump?.Dispose();
                            gump = null;

                        }

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

            //Clear();

            // Add the base gump - the semi-naked paper doll.
            Graphic body = 0;

            bool isGM = false;

            if (Mobile.IsElfMale)
                body = 0x000E;
            else if (Mobile.IsElfFemale)
                body = 0x000F;
            else if (Mobile.Graphic == 0x029A || Mobile.Graphic == 0x02B6)
                body = 0x029A;
            else if (Mobile.Graphic == 0x029B || Mobile.Graphic == 0x02B7)
                body = 0x0299;
            else if (Mobile.IsMale)
                body = 0x000C;
            else 
                body = 0x000D;

            if (Mobile.Graphic == 0x03DB)
                isGM = true;

            if (isGM)
            {
                if (_body == null)
                {
                    Add(_body = new GumpPic(0, 0, body, 0x03EA)
                    {
                        AcceptMouseInput = true,
                        IsPartialHue = true
                    });
                    _body.Initialize();
                }
                else
                    _body.Graphic = body;

                if (_unk == null)
                {
                    Add(_unk = new GumpPic(0, 0, 0xC72B, 0)
                    {
                        AcceptMouseInput = true,
                        IsPartialHue = true
                    });
                    _unk.Initialize();
                }
                else
                    _unk.Graphic = 0xC72B;
            }
            else
            {
                if (_unk != null)
                {
                    _unk.Dispose();
                    _unk = null;
                }

                if (_body == null)
                {
                    Add(_body = new GumpPic(0, 0, body, _mobile.Hue)
                    {
                        AcceptMouseInput = true,
                        IsPartialHue = true
                    });
                    _body.Initialize();
                }
                else
                {
                    _body.Graphic = body;
                    _body.Hue = _mobile.Hue;
                }


                if (Mobile.HasEquipment)
                {
                    ItemGumpPaperdoll g = null;


                    bool invertTunicWithArms = false;
                    bool isQuiver = false;

                    var torso = Mobile.Equipment[(int) Layer.Torso];
                    var quiver = Mobile.Equipment[(int) Layer.Cloak];

                    if (torso == null && _fakeItem != null && _fakeItem.ItemData.Layer == (int)Layer.Torso)
                    {
                        torso = _fakeItem;
                    }

                    if (quiver == null && _fakeItem != null && _fakeItem.ItemData.Layer == (int)Layer.Cloak)
                    {
                        quiver = _fakeItem;
                    }

                    if (torso != null && (torso.Graphic == 0x13BF || torso.Graphic == 0x13C4)) // chainmail tunic
                    {
                        invertTunicWithArms = true;
                    }

                    if (quiver != null && (quiver.Graphic == 0x2FB7 // elven
                                           || quiver.Graphic == 0x2B02 // infinity
                                           ))
                    {
                        isQuiver = true;
                    }

                    for (int i = 0; i < _layerOrder.Length; i++)
                    {
                        Layer layerIndex = _layerOrder[i];

                        if (invertTunicWithArms)
                        {
                            if (layerIndex == Layer.Arms)
                                layerIndex = Layer.Torso;
                            else if (layerIndex == Layer.Torso)
                            {
                                layerIndex = Layer.Arms;
                                invertTunicWithArms = false;
                            }
                        }

                        if (isQuiver)
                        {
                            if (layerIndex == Layer.Cloak)
                            {
                                // skip
                                continue;
                            }

                            if (layerIndex == Layer.Torso) // insert here the quiver if needed
                            {
                                layerIndex = Layer.Cloak;
                                i--;
                                isQuiver = false;
                            }
                        }



                        Item item = _mobile.Equipment[(int) layerIndex];
                        bool isfake = false;
                        bool canPickUp = World.InGame && 
                                         !World.Player.IsDead && 
                                         (_mobile == World.Player || (_paperDollGump != null && _paperDollGump.CanLift)) &&
                                         layerIndex != Layer.Hair && 
                                         layerIndex != Layer.Beard;

                        ref var itemGump = ref _pgumps[(int)layerIndex];

                        if (_fakeItem != null && _fakeItem.ItemData.Layer == (int) layerIndex)
                        {
                            item = _fakeItem;
                            isfake = true;
                            canPickUp = false;
                        }
                        else if (item == null || item.IsDestroyed)
                        {
                            itemGump?.Dispose();
                            itemGump = null;
                            continue;
                        }

                        bool isNew = false;
                        if (itemGump != null)
                        {
                            itemGump.IsVisible = true;
                        }
                        else
                        {
                            Add(itemGump = new ItemGumpPaperdoll(0, 0, item, Mobile, isfake)
                            {
                                CanPickUp = canPickUp
                            });
                            itemGump.Initialize();
                            isNew = true;
                        }

                        if (Mobile.IsCovered(_mobile, (Layer) layerIndex))
                        {
                            itemGump.IsVisible = false;
                            continue;
                        }

                        g = _pgumps[(int) layerIndex];


                        switch ((Layer) layerIndex)
                        {
                            case Layer.Hair:
                            case Layer.Beard:
                                canPickUp = false;

                                break;

                            case Layer.Torso:

                                //if (item.Graphic == 0x13BF || item.Graphic == 0x13C4) // chainmail tunic
                                //{

                                //}

                                //g = _pgumps[(int) Layer.Arms];

                                //if (g != null && !g.IsDisposed)
                                //{
                                //    if (item.Graphic != 0x13BF && item.Graphic != 0x13C4 && //chainmail tunic
                                //        g.Item.Graphic != 0x1410 && g.Item.Graphic != 0x1417) //platemail arms
                                //        g = null;
                                //}

                                goto case Layer.Arms;

                            case Layer.Arms:
                                var robe = _mobile.Equipment[(int)Layer.Robe];

                                if (robe != null)
                                {
                                    itemGump.IsVisible = false;

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
                                        {
                                            itemGump.IsVisible = false;

                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        if (robe.Graphic <= 0x2687)
                                        {
                                            if (robe.Graphic < 0x2683)
                                                if (robe.Graphic < 0x204E || robe.Graphic > 0x204F)
                                                    break;

                                            itemGump.IsVisible = false;

                                            continue;
                                        }

                                        if (robe.Graphic == 0x2FB9 || robe.Graphic == 0x3173)
                                        {
                                            itemGump.IsVisible = false;

                                            continue;
                                        }
                                    }
                                }

                                break;
                        }


                        if (!isNew || isfake)
                        {
                            itemGump.Update(item, isfake);
                            itemGump.CanPickUp = canPickUp;
                        }


                        if (g != null)
                        {
                            Children.Remove(g);
                            Children.Add(g); //move to top
                            g = null;
                        }
                    }
                }
            }

            if (_mobile.HasEquipment)
            {
                Item backpack = _mobile.Equipment[(int) Layer.Backpack];

                if (backpack != null)
                {
                    ref var backpackGump = ref _pgumps[(int) Layer.Backpack];
                    if (backpackGump == null)
                    {
                        Add(backpackGump = new ItemGumpPaperdoll(0, 0, backpack, Mobile)
                        {
                            AcceptMouseInput = true,
                            CanPickUp = false
                        });
                        backpackGump.Initialize();
                        backpackGump.MouseDoubleClick -= OnDoubleclickBackpackGump;
                        backpackGump.MouseDoubleClick += OnDoubleclickBackpackGump;
                    }
                    else
                    {
                        backpackGump.Update(backpack);

                        Children.Remove(backpackGump);
                        Children.Add(backpackGump); //move to top
                    }

                }
            }
        }

        internal bool IsOverBackpack
        {
            get
            {
                if (_mobile != null && _mobile.HasEquipment)
                {
                    var gump = _pgumps[(int)Layer.Backpack];
                    if (gump != null && !gump.IsDisposed)
                        return gump.MouseIsOver;
                }
                return false;
            }
        }

        public bool Fix(int itemID)
        {
            if (itemID == 0x1410 || itemID == 0x1417) // platemail arms
                return true;

            if (itemID == 0x13BF || itemID == 0x13C4) // chainmail tunic
                return true;

            if (itemID == 0x1C08 || itemID == 0x1C09) // leather skirt
                return true;

            if (itemID == 0x1C00 || itemID == 0x1C01) // leather shorts
                return true;

            return false;
        }

        private void OnDoubleclickBackpackGump(object sender, EventArgs args)
        {
            if (_mobile != null && !_mobile.IsDestroyed && _mobile.HasEquipment)
            {
                Item backpack = _mobile.Equipment[(int) Layer.Backpack];

                ContainerGump backpackGump = Engine.UI.GetGump<ContainerGump>(backpack);

                if (backpackGump == null)
                    GameActions.DoubleClick(backpack);
                else
                {
                    backpackGump.SetInScreen();
                    backpackGump.BringOnTop();
                }
            }
        }
    }
}