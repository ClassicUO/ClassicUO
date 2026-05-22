// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Data;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.Game
{
    internal sealed partial class World
    {
        private void SubscribeVendor()
        {
            EventSink.ShopBuyListReceived += OnShopBuyListReceived;
            EventSink.ShopSellListReceived += OnShopSellListReceived;
            EventSink.TradeWindowOpened += OnTradeWindowOpened;
            EventSink.TradeWindowClosed += OnTradeWindowClosed;
            EventSink.TradeWindowAcceptUpdated += OnTradeWindowAcceptUpdated;
            EventSink.TradeWindowCurrencyUpdated += OnTradeWindowCurrencyUpdated;
        }

        private void UnsubscribeVendor()
        {
            EventSink.ShopBuyListReceived -= OnShopBuyListReceived;
            EventSink.ShopSellListReceived -= OnShopSellListReceived;
            EventSink.TradeWindowOpened -= OnTradeWindowOpened;
            EventSink.TradeWindowClosed -= OnTradeWindowClosed;
            EventSink.TradeWindowAcceptUpdated -= OnTradeWindowAcceptUpdated;
            EventSink.TradeWindowCurrencyUpdated -= OnTradeWindowCurrencyUpdated;
        }

        private void OnShopBuyListReceived(ShopBuyListReceivedArgs e)
        {
            if (!InGame)
            {
                return;
            }

            Mobile vendor = Mobiles.Get(e.VendorSerial);

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

            foreach (var entry in e.Entries)
            {
                Item it = Items.Get(entry.ItemSerial);

                if (it == null)
                {
                    continue;
                }

                it.Price = entry.Price;

                if (this.OPL.TryGetNameAndData(it.Serial, out string s, out _))
                {
                    it.Name = s;
                }
                else if (int.TryParse(entry.Name, out int cliloc))
                {
                    it.Name = Client.Game.UO.FileManager.Clilocs.Translate(
                        cliloc,
                        $"\t{it.ItemData.Name}: \t{it.Amount}",
                        true
                    );
                }
                else if (string.IsNullOrEmpty(entry.Name))
                {
                    it.Name = it.ItemData.Name;
                }
                else
                {
                    it.Name = entry.Name;
                }
            }
        }

        private void OnShopSellListReceived(ShopSellListReceivedArgs e)
        {
            if (!InGame)
            {
                return;
            }

            Mobile vendor = Mobiles.Get(e.VendorSerial);

            if (vendor == null)
            {
                return;
            }

            if (e.Entries.Count <= 0)
            {
                return;
            }

            ShopGump gump = UIManager.GetGump<ShopGump>(vendor);
            gump?.Dispose();
            gump = new ShopGump(this, vendor, false, 100, 0);

            foreach (var entry in e.Entries)
            {
                string name = entry.Name;
                bool fromcliloc = false;

                if (int.TryParse(name, out int clilocnum))
                {
                    name = Client.Game.UO.FileManager.Clilocs.GetString(clilocnum);
                    fromcliloc = true;
                }
                else if (string.IsNullOrEmpty(name))
                {
                    bool success = this.OPL.TryGetNameAndData(entry.Serial, out name, out _);

                    if (!success)
                    {
                        name = Client.Game.UO.FileManager.TileData.StaticData[entry.Graphic].Name;
                    }
                }

                gump.AddItem(entry.Serial, entry.Graphic, entry.Hue, entry.Amount, entry.Price, name, fromcliloc);
            }

            UIManager.Add(gump);
        }

        private void OnTradeWindowOpened(TradeWindowOpenArgs e)
        {
            if (!InGame)
            {
                return;
            }

            UIManager.Add(new TradingGump(this, e.Serial, e.Name, e.Id1, e.Id2));
        }

        private void OnTradeWindowClosed(TradeWindowClosedArgs e)
        {
            if (!InGame)
            {
                return;
            }

            UIManager.GetTradingGump(e.Serial)?.Dispose();
        }

        private void OnTradeWindowAcceptUpdated(TradeWindowAcceptUpdatedArgs e)
        {
            if (!InGame)
            {
                return;
            }

            TradingGump trading = UIManager.GetTradingGump(e.Serial);

            if (trading != null)
            {
                trading.ImAccepting = e.ImAccepting;
                trading.HeIsAccepting = e.HeIsAccepting;

                trading.RequestUpdateContents();
            }
        }

        private void OnTradeWindowCurrencyUpdated(TradeWindowCurrencyUpdatedArgs e)
        {
            if (!InGame)
            {
                return;
            }

            TradingGump trading = UIManager.GetTradingGump(e.Serial);

            if (trading == null)
            {
                return;
            }

            if (e.IsMine)
            {
                trading.Gold = e.Gold;
                trading.Platinum = e.Platinum;
            }
            else
            {
                trading.HisGold = e.Gold;
                trading.HisPlatinum = e.Platinum;
            }
        }
    }
}
