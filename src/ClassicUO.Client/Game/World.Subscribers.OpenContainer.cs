// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Network;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game
{
    internal sealed partial class World
    {
        private void SubscribeOpenContainer()
        {
            EventSink.ContainerOpened += OnContainerOpened;
        }

        private void UnsubscribeOpenContainer()
        {
            EventSink.ContainerOpened -= OnContainerOpened;
        }

        private void OnContainerOpened(ContainerOpenedArgs e)
        {
            if (Player == null)
            {
                return;
            }

            uint serial = e.Serial;
            ushort graphic = e.Graphic;

            if (graphic == 0xFFFF)
            {
                Item spellBookItem = Items.Get(serial);

                if (spellBookItem == null)
                {
                    return;
                }

                UIManager.GetGump<SpellbookGump>(serial)?.Dispose();

                SpellbookGump spellbookGump = new SpellbookGump(this, spellBookItem);

                if (!UIManager.GetGumpCachePosition(spellBookItem, out Point location))
                {
                    location = new Point(64, 64);
                }

                spellbookGump.Location = location;
                UIManager.Add(spellbookGump);

                Client.Game.Audio.PlaySound(0x0055);
            }
            else if (graphic == 0x0030)
            {
                Mobile vendor = Mobiles.Get(serial);

                if (vendor == null)
                {
                    return;
                }

                UIManager.GetGump<ShopGump>(serial)?.Dispose();

                ShopGump gump = new ShopGump(this, serial, true, 150, 5);
                UIManager.Add(gump);

                for (Layer layer = Layer.ShopBuyRestock; layer < Layer.ShopBuy + 1; layer++)
                {
                    Item item = vendor.FindItemByLayer(layer);

                    LinkedObject first = item.Items;

                    if (first == null)
                    {
                        //Log.Warn("buy item not found");
                        continue;
                    }

                    bool reverse = item.Graphic != 0x2AF8; //hardcoded logic in original client that we must match

                    if (reverse)
                    {
                        while (first?.Next != null)
                        {
                            first = first.Next;
                        }
                    }

                    while (first != null)
                    {
                        Item it = (Item)first;

                        gump.AddItem(
                            it.Serial,
                            it.Graphic,
                            it.Hue,
                            it.Amount,
                            it.Price,
                            it.Name,
                            false
                        );

                        if (reverse)
                        {
                            first = first.Previous;
                        }
                        else
                        {
                            first = first.Next;
                        }
                    }
                }
            }
            else
            {
                Item item = Items.Get(serial);

                if (item != null)
                {
                    if (
                        item.IsCorpse
                        && (
                            ProfileManager.CurrentProfile.GridLootType == 1
                            || ProfileManager.CurrentProfile.GridLootType == 2
                        )
                    )
                    {
                        //UIManager.GetGump<GridLootGump>(serial)?.Dispose();
                        //UIManager.Add(new GridLootGump(serial));
                        PacketHandlers._requestedGridLoot = serial;

                        if (ProfileManager.CurrentProfile.GridLootType == 1)
                        {
                            return;
                        }
                    }

                    ContainerGump container = UIManager.GetGump<ContainerGump>(serial);
                    bool playsound = false;
                    int x,
                        y;

                    // TODO: check client version ?
                    if (
                        Client.Game.UO.Version >= Utility.ClientVersion.CV_706000
                        && ProfileManager.CurrentProfile != null
                        && ProfileManager.CurrentProfile.UseLargeContainerGumps
                    )
                    {
                        var gumps = Client.Game.UO.Gumps;

                        switch (graphic)
                        {
                            case 0x0048:
                                if (gumps.GetGump(0x06E8).Texture != null)
                                {
                                    graphic = 0x06E8;
                                }

                                break;

                            case 0x0049:
                                if (gumps.GetGump(0x9CDF).Texture != null)
                                {
                                    graphic = 0x9CDF;
                                }

                                break;

                            case 0x0051:
                                if (gumps.GetGump(0x06E7).Texture != null)
                                {
                                    graphic = 0x06E7;
                                }

                                break;

                            case 0x003E:
                                if (gumps.GetGump(0x06E9).Texture != null)
                                {
                                    graphic = 0x06E9;
                                }

                                break;

                            case 0x004D:
                                if (gumps.GetGump(0x06EA).Texture != null)
                                {
                                    graphic = 0x06EA;
                                }

                                break;

                            case 0x004E:
                                if (gumps.GetGump(0x06E6).Texture != null)
                                {
                                    graphic = 0x06E6;
                                }

                                break;

                            case 0x004F:
                                if (gumps.GetGump(0x06E5).Texture != null)
                                {
                                    graphic = 0x06E5;
                                }

                                break;

                            case 0x004A:
                                if (gumps.GetGump(0x9CDD).Texture != null)
                                {
                                    graphic = 0x9CDD;
                                }

                                break;

                            case 0x0044:
                                if (gumps.GetGump(0x9CE3).Texture != null)
                                {
                                    graphic = 0x9CE3;
                                }

                                break;
                        }
                    }

                    if (container != null)
                    {
                        x = container.ScreenCoordinateX;
                        y = container.ScreenCoordinateY;
                        container.Dispose();
                    }
                    else
                    {
                        ContainerManager.CalculateContainerPosition(serial, graphic);
                        x = ContainerManager.X;
                        y = ContainerManager.Y;
                        playsound = true;
                    }

                    UIManager.Add(
                        new ContainerGump(this, item, graphic, playsound)
                        {
                            X = x,
                            Y = y,
                            InvalidateContents = true
                        }
                    );

                    UIManager.RemovePosition(serial);
                }
                else
                {
                    Log.Error("[OpenContainer]: item not found");
                }
            }

            if (graphic != 0x0030)
            {
                Item it = Items.Get(serial);

                if (it != null)
                {
                    it.Opened = true;

                    if (!it.IsCorpse && graphic != 0xFFFF)
                    {
                        ClearContainerAndRemoveItems(it);
                    }
                }
            }
        }
    }
}
