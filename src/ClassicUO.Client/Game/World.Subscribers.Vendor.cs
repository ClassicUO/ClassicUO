// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Data;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;

namespace ClassicUO.Game
{
    internal sealed partial class World
    {
        private void SubscribeVendor()
        {
            EventSink.ShopBuyListReceived += OnShopBuyListReceived;
            EventSink.ShopSellListReceived += OnShopSellListReceived;
            EventSink.TradeWindow += OnTradeWindow;
        }

        private void UnsubscribeVendor()
        {
            EventSink.ShopBuyListReceived -= OnShopBuyListReceived;
            EventSink.ShopSellListReceived -= OnShopSellListReceived;
            EventSink.TradeWindow -= OnTradeWindow;
        }

        private void OnShopBuyListReceived(ShopBuyListReceivedArgs e)
        {
            if (!InGame)
            {
                return;
            }

            var p = new StackDataReader(e.Data);
            p.Seek(e.Offset);

            Item container = Items.Get(p.ReadUInt32BE());

            if (container == null)
            {
                return;
            }

            Mobile vendor = Mobiles.Get(container.Container);

            if (vendor == null)
            {
                return;
            }

            ShopGump gump = UIManager.GetGump<ShopGump>();

            if (gump != null && (gump.LocalSerial != vendor || !gump.IsBuyGump))
            {
                gump.Dispose();
                gump = null;
            }

            if (gump == null)
            {
                gump = new ShopGump(this, vendor, true, 150, 5);
                UIManager.Add(gump);
            }

            if (container.Layer == Layer.ShopBuyRestock || container.Layer == Layer.ShopBuy)
            {
                byte count = p.ReadUInt8();

                LinkedObject first = container.Items;

                if (first == null)
                {
                    return;
                }

                bool reverse = false;

                if (container.Graphic == 0x2AF8) //hardcoded logic in original client that we must match
                {
                    //sort the contents
                    first = container.SortContents<Item>((x, y) => x.X - y.X);
                }
                else
                {
                    //skip to last item and read in reverse later
                    reverse = true;

                    while (first?.Next != null)
                    {
                        first = first.Next;
                    }
                }

                for (int i = 0; i < count; i++)
                {
                    if (first == null)
                    {
                        break;
                    }

                    Item it = (Item)first;

                    it.Price = p.ReadUInt32BE();
                    byte nameLen = p.ReadUInt8();
                    string name = p.ReadASCII(nameLen);

                    if (this.OPL.TryGetNameAndData(it.Serial, out string s, out _))
                    {
                        it.Name = s;
                    }
                    else if (int.TryParse(name, out int cliloc))
                    {
                        it.Name = Client.Game.UO.FileManager.Clilocs.Translate(
                            cliloc,
                            $"\t{it.ItemData.Name}: \t{it.Amount}",
                            true
                        );
                    }
                    else if (string.IsNullOrEmpty(name))
                    {
                        it.Name = it.ItemData.Name;
                    }
                    else
                    {
                        it.Name = name;
                    }

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

        private void OnShopSellListReceived(ShopSellListReceivedArgs e)
        {
            if (!InGame)
            {
                return;
            }

            var p = new StackDataReader(e.Data);
            p.Seek(e.Offset);

            uint vendorSerial = p.ReadUInt32BE();
            Mobile vendor = Mobiles.Get(vendorSerial);

            if (vendor == null)
            {
                return;
            }

            ushort countItems = p.ReadUInt16BE();

            if (countItems <= 0)
            {
                return;
            }

            ShopGump gump = UIManager.GetGump<ShopGump>(vendor);
            gump?.Dispose();
            gump = new ShopGump(this, vendor, false, 100, 0);

            for (int i = 0; i < countItems; i++)
            {
                uint serial = p.ReadUInt32BE();
                ushort graphic = p.ReadUInt16BE();
                ushort hue = p.ReadUInt16BE();
                ushort amount = p.ReadUInt16BE();
                ushort price = p.ReadUInt16BE();
                string name = p.ReadASCII(p.ReadUInt16BE());
                bool fromcliloc = false;

                if (int.TryParse(name, out int clilocnum))
                {
                    name = Client.Game.UO.FileManager.Clilocs.GetString(clilocnum);
                    fromcliloc = true;
                }
                else if (string.IsNullOrEmpty(name))
                {
                    bool success = this.OPL.TryGetNameAndData(serial, out name, out _);

                    if (!success)
                    {
                        name = Client.Game.UO.FileManager.TileData.StaticData[graphic].Name;
                    }
                }

                //if (string.IsNullOrEmpty(item.Name))
                //    item.Name = name;

                gump.AddItem(serial, graphic, hue, amount, price, name, fromcliloc);
            }

            UIManager.Add(gump);
        }

        private void OnTradeWindow(TradeWindowArgs e)
        {
            if (!InGame)
            {
                return;
            }

            byte type = e.SubType;
            uint serial = e.Serial;

            if (type == 1)
            {
                UIManager.GetTradingGump(serial)?.Dispose();
                return;
            }

            var p = new StackDataReader(e.Data);
            p.Seek(e.Offset);

            // Re-read header (subtype + serial) to advance position to payload.
            p.ReadUInt8();
            p.ReadUInt32BE();

            if (type == 0)
            {
                uint id1 = p.ReadUInt32BE();
                uint id2 = p.ReadUInt32BE();

                // standard client doesn't allow the trading system if one of the traders is invisible (=not sent by server)
                if (this.Get(id1) == null || this.Get(id2) == null)
                {
                    return;
                }

                bool hasName = p.ReadBool();
                string name = string.Empty;

                if (hasName && p.Position < p.Length)
                {
                    name = p.ReadASCII();
                }

                UIManager.Add(new TradingGump(this, serial, name, id1, id2));
            }
            else if (type == 2)
            {
                uint id1 = p.ReadUInt32BE();
                uint id2 = p.ReadUInt32BE();

                TradingGump trading = UIManager.GetTradingGump(serial);

                if (trading != null)
                {
                    trading.ImAccepting = id1 != 0;
                    trading.HeIsAccepting = id2 != 0;

                    trading.RequestUpdateContents();
                }
            }
            else if (type == 3 || type == 4)
            {
                TradingGump trading = UIManager.GetTradingGump(serial);

                if (trading != null)
                {
                    if (type == 4)
                    {
                        trading.Gold = p.ReadUInt32BE();
                        trading.Platinum = p.ReadUInt32BE();
                    }
                    else
                    {
                        trading.HisGold = p.ReadUInt32BE();
                        trading.HisPlatinum = p.ReadUInt32BE();
                    }
                }
            }
        }
    }
}
