// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal sealed class GumpsUIHandler : IEventListener
    {
        private readonly World _world;

        public GumpsUIHandler(World world)
        {
            _world = world;
        }

        public void Subscribe()
        {
            EventSink.MapDisplayed += OnMapDisplayed;
            EventSink.BookOpened += OnBookOpened;
            EventSink.BookDataReceived += OnBookDataReceived;
            EventSink.BulletinBoardOpened += OnBulletinBoardOpened;
            EventSink.BulletinBoardSummary += OnBulletinBoardSummary;
            EventSink.BulletinBoardMessage += OnBulletinBoardMessage;
            EventSink.PaperdollOpened += OnPaperdollOpened;
        }

        public void Unsubscribe()
        {
            EventSink.MapDisplayed -= OnMapDisplayed;
            EventSink.BookOpened -= OnBookOpened;
            EventSink.BookDataReceived -= OnBookDataReceived;
            EventSink.BulletinBoardOpened -= OnBulletinBoardOpened;
            EventSink.BulletinBoardSummary -= OnBulletinBoardSummary;
            EventSink.BulletinBoardMessage -= OnBulletinBoardMessage;
            EventSink.PaperdollOpened -= OnPaperdollOpened;
        }

        private void OnMapDisplayed(MapDisplayedArgs e)
        {
            MapGump gump = new MapGump(_world, e.Serial, e.GumpId, e.Width, e.Height);
            SpriteInfo multiMapInfo;

            if (e.Facet.HasValue)
            {
                multiMapInfo = Client.Game.UO.MultiMaps.GetMap(e.Facet.Value, e.Width, e.Height, e.StartX, e.StartY, e.EndX, e.EndY);
            }
            else
            {
                multiMapInfo = Client.Game.UO.MultiMaps.GetMap(null, e.Width, e.Height, e.StartX, e.StartY, e.EndX, e.EndY);
            }

            if (multiMapInfo.Texture != null)
                gump.SetMapTexture(multiMapInfo.Texture);

            UIManager.Add(gump);

            Item it = _world.Items.Get(e.Serial);

            if (it != null)
            {
                it.Opened = true;
            }
        }

        private void OnBookOpened(BookOpenedArgs e)
        {
            ModernBookGump bgump = UIManager.GetGump<ModernBookGump>(e.Serial);

            if (bgump == null || bgump.IsDisposed)
            {
                UIManager.Add(
                    new ModernBookGump(_world, e.Serial, e.PageCount, e.Title, e.Author, e.Editable, e.OldPacket)
                    {
                        X = 100,
                        Y = 100
                    }
                );

                NetClient.Socket.Send_BookPageDataRequest(e.Serial, 1);
            }
            else
            {
                bgump.IsEditable = e.Editable;
                bgump.SetTitle(e.Title, e.Editable);
                bgump.SetAuthor(e.Author, e.Editable);
                bgump.UseNewHeader = !e.OldPacket;
                bgump.SetInScreen();
                bgump.BringOnTop();
            }
        }

        private void OnBookDataReceived(BookDataReceivedArgs e)
        {
            ModernBookGump gump = UIManager.GetGump<ModernBookGump>(e.Serial);

            if (gump == null || gump.IsDisposed)
            {
                return;
            }

            foreach (BookPage page in e.Pages)
            {
                int pageNum = page.PageNumber;
                gump.KnownPages.Add(pageNum);

                if (pageNum < gump.BookPageCount && pageNum >= 0)
                {
                    int lineCnt = page.Lines.Count;

                    for (int line = 0; line < lineCnt; line++)
                    {
                        int index = pageNum * ModernBookGump.MAX_BOOK_LINES + line;

                        if (index < gump.BookLines.Length)
                        {
                            gump.BookLines[index] = page.Lines[line];
                        }
                        else
                        {
                            Log.Error(
                                "BOOKGUMP: The server is sending a page number GREATER than the allowed number of pages in BOOK!"
                            );
                        }
                    }

                    if (lineCnt < ModernBookGump.MAX_BOOK_LINES)
                    {
                        for (int line = lineCnt; line < ModernBookGump.MAX_BOOK_LINES; line++)
                        {
                            gump.BookLines[pageNum * ModernBookGump.MAX_BOOK_LINES + line] =
                                string.Empty;
                        }
                    }
                }
                else
                {
                    Log.Error(
                        "BOOKGUMP: The server is sending a page number GREATER than the allowed number of pages in BOOK!"
                    );
                }
            }

            gump.ServerSetBookText();
        }

        private void OnBulletinBoardOpened(BulletinBoardOpenedArgs e)
        {
            Item item = _world.Items.Get(e.Serial);

            if (item == null)
            {
                return;
            }

            BulletinBoardGump bulletinBoard = UIManager.GetGump<BulletinBoardGump>(e.Serial);
            bulletinBoard?.Dispose();

            int x = (Client.Game.ClientBounds.Width >> 1) - 245;
            int y = (Client.Game.ClientBounds.Height >> 1) - 205;

            bulletinBoard = new BulletinBoardGump(_world, item, x, y, e.Name);
            UIManager.Add(bulletinBoard);

            item.Opened = true;
        }

        private void OnBulletinBoardSummary(BulletinBoardSummaryArgs e)
        {
            BulletinBoardGump bulletinBoard = UIManager.GetGump<BulletinBoardGump>(e.BoardSerial);

            if (bulletinBoard == null)
            {
                return;
            }

            string text = e.Poster + " - " + e.Subject + " - " + e.DateTime;
            bulletinBoard.AddBulletinObject(e.MessageSerial, text);
        }

        private void OnBulletinBoardMessage(BulletinBoardMessageArgs e)
        {
            BulletinBoardGump bulletinBoard = UIManager.GetGump<BulletinBoardGump>(e.BoardSerial);

            if (bulletinBoard == null)
            {
                return;
            }

            byte variant = (byte)(1 + (e.Poster == _world.Player.Name ? 1 : 0));

            UIManager.Add(
                new BulletinBoardItem(
                    _world,
                    e.BoardSerial,
                    e.MessageSerial,
                    e.Poster,
                    e.Subject,
                    e.DateTime,
                    e.Message.TrimStart(),
                    variant
                )
                {
                    X = 40,
                    Y = 40
                }
            );
        }

        private void OnPaperdollOpened(PaperdollOpenedArgs e)
        {
            Mobile mobile = _world.Mobiles.Get(e.Serial);

            if (mobile == null)
            {
                return;
            }

            mobile.Title = e.Title;

            PaperDollGump paperdoll = UIManager.GetGump<PaperDollGump>(mobile);

            if (paperdoll == null)
            {
                if (!UIManager.GetGumpCachePosition(mobile, out Point location))
                {
                    location = new Point(100, 100);
                }

                UIManager.Add(
                    new PaperDollGump(_world, mobile, (e.Flags & 0x02) != 0) { Location = location }
                );
            }
            else
            {
                bool old = paperdoll.CanLift;
                bool newLift = (e.Flags & 0x02) != 0;

                paperdoll.CanLift = newLift;
                paperdoll.UpdateTitle(e.Title);

                if (old != newLift)
                {
                    paperdoll.RequestUpdateContents();
                }

                paperdoll.SetInScreen();
                paperdoll.BringOnTop();
            }
        }
    }
}
