// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.Game.Containers.Vendor
{
    /// <summary>
    /// Listens for shop buy/sell lists and trade-window lifecycle events.
    /// Builds <see cref="ShopGump"/> and <see cref="TradingGump"/> instances
    /// and keeps them in sync with server-side state.
    /// </summary>
    internal sealed class VendorHandler : IEventListener
    {
        private readonly World _world;

        public VendorHandler(World world)
        {
            _world = world;
        }

        public void Subscribe()
        {
            EventSink.ShopBuyListReceived += OnShopBuyListReceived;
            EventSink.ShopSellListReceived += OnShopSellListReceived;
            EventSink.TradeWindowOpened += OnTradeWindowOpened;
            EventSink.TradeWindowClosed += OnTradeWindowClosed;
            EventSink.TradeWindowAcceptUpdated += OnTradeWindowAcceptUpdated;
            EventSink.TradeWindowCurrencyUpdated += OnTradeWindowCurrencyUpdated;
        }

        public void Unsubscribe()
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
            if (!_world.InGame)
            {
                return;
            }

            Mobile vendor = _world.Mobiles.Get(e.VendorSerial);

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
                gump = new ShopGump(_world, vendor, true, 150, 5);
                UIManager.Add(gump);
            }

            foreach (var entry in e.Entries)
            {
                Item it = _world.Items.Get(entry.ItemSerial);

                if (it == null)
                {
                    continue;
                }

                it.Price = entry.Price;

                if (_world.OPL.TryGetNameAndData(it.Serial, out string s, out _))
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
            if (!_world.InGame)
            {
                return;
            }

            Mobile vendor = _world.Mobiles.Get(e.VendorSerial);

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
            gump = new ShopGump(_world, vendor, false, 100, 0);

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
                    bool success = _world.OPL.TryGetNameAndData(entry.Serial, out name, out _);

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
            if (!_world.InGame)
            {
                return;
            }

            UIManager.Add(new TradingGump(_world, e.Serial, e.Name, e.Id1, e.Id2));
        }

        private void OnTradeWindowClosed(TradeWindowClosedArgs e)
        {
            if (!_world.InGame)
            {
                return;
            }

            UIManager.GetTradingGump(e.Serial)?.Dispose();
        }

        private void OnTradeWindowAcceptUpdated(TradeWindowAcceptUpdatedArgs e)
        {
            if (!_world.InGame)
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
            if (!_world.InGame)
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
