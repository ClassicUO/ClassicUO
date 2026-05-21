// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game
{
    internal sealed partial class World
    {
        private void SubscribeGumpsUI()
        {
            EventSink.MapDisplayed += OnMapDisplayed;
            EventSink.BookOpened += OnBookOpened;
            EventSink.BookDataReceived += OnBookDataReceived;
            EventSink.BulletinBoardDataReceived += OnBulletinBoardDataReceived;
            EventSink.PaperdollOpened += OnPaperdollOpened;
        }

        private void UnsubscribeGumpsUI()
        {
            EventSink.MapDisplayed -= OnMapDisplayed;
            EventSink.BookOpened -= OnBookOpened;
            EventSink.BookDataReceived -= OnBookDataReceived;
            EventSink.BulletinBoardDataReceived -= OnBulletinBoardDataReceived;
            EventSink.PaperdollOpened -= OnPaperdollOpened;
        }

        private void OnMapDisplayed(MapDisplayedArgs e)
        {
            MapGump gump = new MapGump(this, e.Serial, e.GumpId, e.Width, e.Height);
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

            Item it = Items.Get(e.Serial);

            if (it != null)
            {
                it.Opened = true;
            }
        }

        private void OnBookOpened(BookOpenedArgs e)
        {
            var p = new StackDataReader(e.Data);
            p.Seek(e.Offset);

            ModernBookGump bgump = UIManager.GetGump<ModernBookGump>(e.Serial);

            if (bgump == null || bgump.IsDisposed)
            {
                string title = e.OldPacket
                    ? p.ReadUTF8(60, true)
                    : p.ReadUTF8(p.ReadUInt16BE(), true);
                string author = e.OldPacket
                    ? p.ReadUTF8(30, true)
                    : p.ReadUTF8(p.ReadUInt16BE(), true);

                UIManager.Add(
                    new ModernBookGump(this, e.Serial, e.PageCount, title, author, e.Editable, e.OldPacket)
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
                bgump.SetTitle(
                    e.OldPacket ? p.ReadUTF8(60, true) : p.ReadUTF8(p.ReadUInt16BE(), true),
                    e.Editable
                );
                bgump.SetAuthor(
                    e.OldPacket ? p.ReadUTF8(30, true) : p.ReadUTF8(p.ReadUInt16BE(), true),
                    e.Editable
                );
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

            var p = new StackDataReader(e.Data);
            p.Seek(e.Offset);

            for (int i = 0; i < e.PageCount; i++)
            {
                int pageNum = p.ReadUInt16BE() - 1;
                gump.KnownPages.Add(pageNum);

                if (pageNum < gump.BookPageCount && pageNum >= 0)
                {
                    ushort lineCnt = p.ReadUInt16BE();

                    for (int line = 0; line < lineCnt; line++)
                    {
                        int index = pageNum * ModernBookGump.MAX_BOOK_LINES + line;

                        if (index < gump.BookLines.Length)
                        {
                            gump.BookLines[index] = ModernBookGump.IsNewBook
                                ? p.ReadUTF8(true)
                                : p.ReadASCII();
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

        private void OnBulletinBoardDataReceived(BulletinBoardDataReceivedArgs e)
        {
            var p = new StackDataReader(e.Data);
            p.Seek(e.Offset);

            switch (e.Action)
            {
                case 0: // open
                    {
                        Item item = Items.Get(e.Serial);

                        if (item != null)
                        {
                            BulletinBoardGump bulletinBoard = UIManager.GetGump<BulletinBoardGump>(e.Serial);
                            bulletinBoard?.Dispose();

                            int x = (Client.Game.ClientBounds.Width >> 1) - 245;
                            int y = (Client.Game.ClientBounds.Height >> 1) - 205;

                            bulletinBoard = new BulletinBoardGump(this, item, x, y, p.ReadUTF8(22, true));
                            UIManager.Add(bulletinBoard);

                            item.Opened = true;
                        }
                    }
                    break;

                case 1: // summary msg
                    {
                        BulletinBoardGump bulletinBoard = UIManager.GetGump<BulletinBoardGump>(e.Serial);

                        if (bulletinBoard != null)
                        {
                            uint serial = p.ReadUInt32BE();
                            uint parendID = p.ReadUInt32BE();

                            // poster
                            int len = p.ReadUInt8();
                            string text = (len <= 0 ? string.Empty : p.ReadUTF8(len, true)) + " - ";

                            // subject
                            len = p.ReadUInt8();
                            text += (len <= 0 ? string.Empty : p.ReadUTF8(len, true)) + " - ";

                            // datetime
                            len = p.ReadUInt8();
                            text += (len <= 0 ? string.Empty : p.ReadUTF8(len, true));

                            bulletinBoard.AddBulletinObject(serial, text);
                        }
                    }
                    break;

                case 2: // message
                    {
                        BulletinBoardGump bulletinBoard = UIManager.GetGump<BulletinBoardGump>(e.Serial);

                        if (bulletinBoard != null)
                        {
                            uint serial = p.ReadUInt32BE();

                            int len = p.ReadUInt8();
                            string poster = len > 0 ? p.ReadASCII(len) : string.Empty;

                            len = p.ReadUInt8();
                            string subject = len > 0 ? p.ReadUTF8(len, true) : string.Empty;

                            len = p.ReadUInt8();
                            string dataTime = len > 0 ? p.ReadASCII(len) : string.Empty;

                            p.Skip(4);

                            byte unk = p.ReadUInt8();

                            if (unk > 0)
                            {
                                p.Skip(unk * 4);
                            }

                            byte lines = p.ReadUInt8();

                            Span<char> span = stackalloc char[256];
                            ValueStringBuilder sb = new ValueStringBuilder(span);

                            for (int i = 0; i < lines; i++)
                            {
                                byte lineLen = p.ReadUInt8();

                                if (lineLen > 0)
                                {
                                    string putta = p.ReadUTF8(lineLen, true);
                                    sb.Append(putta);
                                    sb.Append('\n');
                                }
                            }

                            string msg = sb.ToString();
                            byte variant = (byte)(1 + (poster == Player.Name ? 1 : 0));

                            UIManager.Add(
                                new BulletinBoardItem(
                                    this,
                                    e.Serial,
                                    serial,
                                    poster,
                                    subject,
                                    dataTime,
                                    msg.TrimStart(),
                                    variant
                                )
                                {
                                    X = 40,
                                    Y = 40
                                }
                            );

                            sb.Dispose();
                        }
                    }
                    break;
            }
        }

        private void OnPaperdollOpened(PaperdollOpenedArgs e)
        {
            Mobile mobile = Mobiles.Get(e.Serial);

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
                    new PaperDollGump(this, mobile, (e.Flags & 0x02) != 0) { Location = location }
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
