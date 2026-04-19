// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;
using ClassicUO.Resources;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Network
{
    internal sealed partial class PacketHandlers
    {
        internal static void RegisterMiscHandlers(PacketHandlers h)
        {
            h.Add(0x3A, UpdateSkills);
            h.Add(0x66, BookData);
            h.Add(0x93, OpenBook);
            h.Add(0xD4, OpenBook);
        }

        private static void UpdateSkills(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            byte type = p.ReadUInt8();
            bool haveCap = type != 0u && type <= 0x03 || type == 0xDF;
            bool isSingleUpdate = type == 0xFF || type == 0xDF;

            if (type == 0xFE)
            {
                int count = p.ReadUInt16BE();

                Client.Game.UO.FileManager.Skills.Skills.Clear();
                Client.Game.UO.FileManager.Skills.SortedSkills.Clear();

                for (int i = 0; i < count; i++)
                {
                    bool haveButton = p.ReadBool();
                    int nameLength = p.ReadUInt8();

                    Client.Game.UO.FileManager.Skills.Skills.Add(
                        new SkillEntry(i, p.ReadASCII(nameLength), haveButton)
                    );
                }

                Client.Game.UO.FileManager.Skills.SortedSkills.AddRange(Client.Game.UO.FileManager.Skills.Skills);

                Client.Game.UO.FileManager.Skills.SortedSkills.Sort(
                    (a, b) => string.Compare(a.Name, b.Name, StringComparison.InvariantCulture)
                );
            }
            else
            {
                StandardSkillsGump standard = null;
                SkillGumpAdvanced advanced = null;

                if (ProfileManager.CurrentProfile.StandardSkillsGump)
                {
                    standard = UIManager.GetGump<StandardSkillsGump>();
                }
                else
                {
                    advanced = UIManager.GetGump<SkillGumpAdvanced>();
                }

                if (!isSingleUpdate && (type == 1 || type == 3 || world.SkillsRequested))
                {
                    world.SkillsRequested = false;

                    // TODO: make a base class for this gump
                    if (ProfileManager.CurrentProfile.StandardSkillsGump)
                    {
                        if (standard == null)
                        {
                            UIManager.Add(standard = new StandardSkillsGump(world) { X = 100, Y = 100 });
                        }
                    }
                    else
                    {
                        if (advanced == null)
                        {
                            UIManager.Add(advanced = new SkillGumpAdvanced(world) { X = 100, Y = 100 });
                        }
                    }
                }

                while (p.Position < p.Length)
                {
                    ushort id = p.ReadUInt16BE();

                    if (p.Position >= p.Length)
                    {
                        break;
                    }

                    if (id == 0 && type == 0)
                    {
                        break;
                    }

                    if (type == 0 || type == 0x02)
                    {
                        id--;
                    }

                    ushort realVal = p.ReadUInt16BE();
                    ushort baseVal = p.ReadUInt16BE();
                    Lock locked = (Lock)p.ReadUInt8();
                    ushort cap = 1000;

                    if (haveCap)
                    {
                        cap = p.ReadUInt16BE();
                    }

                    if (id < world.Player.Skills.Length)
                    {
                        Skill skill = world.Player.Skills[id];

                        if (skill != null)
                        {
                            if (isSingleUpdate)
                            {
                                float change = realVal / 10.0f - skill.Value;

                                if (
                                    change != 0.0f
                                    && !float.IsNaN(change)
                                    && ProfileManager.CurrentProfile != null
                                    && ProfileManager.CurrentProfile.ShowSkillsChangedMessage
                                    && Math.Abs(change * 10)
                                        >= ProfileManager.CurrentProfile.ShowSkillsChangedDeltaValue
                                )
                                {
                                    GameActions.Print(
                                        world,
                                        string.Format(
                                            ResGeneral.YourSkillIn0Has1By2ItIsNow3,
                                            skill.Name,
                                            change < 0
                                                ? ResGeneral.Decreased
                                                : ResGeneral.Increased,
                                            Math.Abs(change),
                                            skill.Value + change
                                        ),
                                        0x58,
                                        MessageType.System,
                                        3,
                                        false
                                    );
                                }
                            }

                            skill.BaseFixed = baseVal;
                            skill.ValueFixed = realVal;
                            skill.CapFixed = cap;
                            skill.Lock = locked;

                            standard?.Update(id);
                            advanced?.ForceUpdate();
                        }
                    }

                    if (isSingleUpdate)
                    {
                        break;
                    }
                }
            }
        }

        private static void BookData(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            ushort pageCnt = p.ReadUInt16BE();

            ModernBookGump gump = UIManager.GetGump<ModernBookGump>(serial);

            if (gump == null || gump.IsDisposed)
            {
                return;
            }

            for (int i = 0; i < pageCnt; i++)
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

        private static void OpenBook(World world, ref StackDataReader p)
        {
            uint serial = p.ReadUInt32BE();
            bool oldpacket = p[0] == 0x93;
            bool editable = p.ReadBool();

            if (!oldpacket)
            {
                editable = p.ReadBool();
            }
            else
            {
                p.Skip(1);
            }

            ModernBookGump bgump = UIManager.GetGump<ModernBookGump>(serial);

            if (bgump == null || bgump.IsDisposed)
            {
                ushort page_count = p.ReadUInt16BE();
                string title = oldpacket
                    ? p.ReadUTF8(60, true)
                    : p.ReadUTF8(p.ReadUInt16BE(), true);
                string author = oldpacket
                    ? p.ReadUTF8(30, true)
                    : p.ReadUTF8(p.ReadUInt16BE(), true);

                UIManager.Add(
                    new ModernBookGump(world, serial, page_count, title, author, editable, oldpacket)
                    {
                        X = 100,
                        Y = 100
                    }
                );

                NetClient.Socket.Send_BookPageDataRequest(serial, 1);
            }
            else
            {
                p.Skip(2);
                bgump.IsEditable = editable;
                bgump.SetTile(
                    oldpacket ? p.ReadUTF8(60, true) : p.ReadUTF8(p.ReadUInt16BE(), true),
                    editable
                );
                bgump.SetAuthor(
                    oldpacket ? p.ReadUTF8(30, true) : p.ReadUTF8(p.ReadUInt16BE(), true),
                    editable
                );
                bgump.UseNewHeader = !oldpacket;
                bgump.SetInScreen();
                bgump.BringOnTop();
            }
        }
    }
}
