// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.Events;
using ClassicUO.Network;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Events
{
    // Golden-byte tests for UI gump, chat, prompt, skill, quest, and waypoint
    // packet handlers in PacketHandlers. Each test feeds raw bytes through
    // PacketHandlers.AnalyzePacket and asserts the parsed args on the matching
    // EventSink event. Handlers gated on world.InGame (Player + Map) are listed
    // in comments and skipped because production-only seam edits are out of scope.
    public class UiChatSkillsPacketTests : IDisposable
    {
        private readonly World _world;

        public UiChatSkillsPacketTests()
        {
            _world = new World();
            EventSink.ClearAll();
        }

        public void Dispose()
        {
            EventSink.ClearAll();
        }

        private void Dispatch(byte[] data, int offset)
            => PacketHandlers.Handler.AnalyzePacket(_world, data, offset);

        // Returns ASCII bytes for `value` padded to `length` with NUL.
        private static byte[] Ascii(string value, int length)
        {
            byte[] dest = new byte[length];
            for (int i = 0; i < value.Length && i < length; i++)
            {
                dest[i] = (byte)value[i];
            }
            return dest;
        }

        // Writes a big-endian unicode null-terminated string into a List<byte>.
        private static void WriteUnicodeBE(List<byte> list, string value, bool nullTerminate = true)
        {
            foreach (char c in value)
            {
                list.Add((byte)(c >> 8));
                list.Add((byte)(c & 0xFF));
            }

            if (nullTerminate)
            {
                list.Add(0x00);
                list.Add(0x00);
            }
        }

        // Writes a little-endian unicode null-terminated string into a List<byte>.
        private static void WriteUnicodeLE(List<byte> list, string value, bool nullTerminate = true)
        {
            foreach (char c in value)
            {
                list.Add((byte)(c & 0xFF));
                list.Add((byte)(c >> 8));
            }

            if (nullTerminate)
            {
                list.Add(0x00);
                list.Add(0x00);
            }
        }

        // ---- 0x88 OpenPaperdoll (fixed 0x42, offset=1) ----

        [Fact]
        public void OpenPaperdoll_0x88_RaisesPaperdollOpened()
        {
            PaperdollOpenedArgs? captured = null;
            EventSink.PaperdollOpened += e => captured = e;

            byte[] packet = new byte[1 + 4 + 60 + 1];
            packet[0] = 0x88;
            packet[1] = 0x12; packet[2] = 0x34; packet[3] = 0x56; packet[4] = 0x78;
            byte[] name = Ascii("Sir Reginald", 60);
            Array.Copy(name, 0, packet, 5, 60);
            packet[65] = 0x07;

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x12345678u);
            captured.Value.Title.Should().Be("Sir Reginald");
            captured.Value.Flags.Should().Be(0x07);
        }

        // ---- 0xF5 DisplayMap (fixed 0x15, offset=1) ----
        // 0x90 also routes to DisplayMap but uses Client.Game.UO.Version when
        // not 0xF5, and Client.Game is null in tests — skip 0x90.

        [Fact]
        public void DisplayMap_0xF5_RaisesMapDisplayed_WithFacet()
        {
            MapDisplayedArgs? captured = null;
            EventSink.MapDisplayed += e => captured = e;

            byte[] packet =
            {
                0xF5,
                0x00, 0x00, 0x00, 0x42,        // serial
                0x00, 0x11,                    // gumpid
                0x00, 0x10,                    // startX
                0x00, 0x20,                    // startY
                0x00, 0x40,                    // endX
                0x00, 0x80,                    // endY
                0x01, 0x00,                    // width
                0x02, 0x00,                    // height
                0x00, 0x02                     // facet
            };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x42u);
            captured.Value.GumpId.Should().Be((ushort)0x0011);
            captured.Value.StartX.Should().Be((ushort)0x0010);
            captured.Value.StartY.Should().Be((ushort)0x0020);
            captured.Value.EndX.Should().Be((ushort)0x0040);
            captured.Value.EndY.Should().Be((ushort)0x0080);
            captured.Value.Width.Should().Be((ushort)0x0100);
            captured.Value.Height.Should().Be((ushort)0x0200);
            captured.Value.Facet.Should().Be((ushort)2);
        }

        // ---- 0x93 OpenBook (legacy, fixed 0x63, offset=1) ----

        [Fact]
        public void OpenBook_0x93_LegacyForm_RaisesBookOpened()
        {
            BookOpenedArgs? captured = null;
            EventSink.BookOpened += e => captured = e;

            byte[] packet = new byte[1 + 4 + 1 + 1 + 2 + 60 + 30];
            packet[0] = 0x93;
            packet[1] = 0x00; packet[2] = 0x00; packet[3] = 0x12; packet[4] = 0x34;
            packet[5] = 0x01;          // editable (legacy reads then overwrites via Skip path)
            packet[6] = 0x00;          // padding byte (Skip(1))
            packet[7] = 0x00; packet[8] = 0x05;  // pageCount
            byte[] title = Ascii("The Title", 60);
            Array.Copy(title, 0, packet, 9, 60);
            byte[] author = Ascii("Anon", 30);
            Array.Copy(author, 0, packet, 69, 30);

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x1234u);
            captured.Value.OldPacket.Should().BeTrue();
            captured.Value.Editable.Should().BeTrue();
            captured.Value.PageCount.Should().Be((ushort)5);
            captured.Value.Title.Should().Be("The Title");
            captured.Value.Author.Should().Be("Anon");
        }

        // ---- 0xD4 OpenBook (newer, variable, offset=3) ----

        [Fact]
        public void OpenBook_0xD4_NewerForm_RaisesBookOpened()
        {
            BookOpenedArgs? captured = null;
            EventSink.BookOpened += e => captured = e;

            // newer wire: serial(4) writeflag(1) editable(1) pages(2)
            // titleLen(2) title(utf8) authorLen(2) author(utf8)
            string title = "Hello";
            string author = "Bob";
            byte[] titleBytes = System.Text.Encoding.UTF8.GetBytes(title + "\0");
            byte[] authorBytes = System.Text.Encoding.UTF8.GetBytes(author + "\0");

            List<byte> bytes = new List<byte>();
            bytes.Add(0xD4);
            bytes.Add(0x00); bytes.Add(0x00); // length placeholder
            // payload
            bytes.AddRange(new byte[] { 0x00, 0x00, 0x10, 0x00 }); // serial=0x1000
            bytes.Add(0x00);                       // writeflag (ignored for newer)
            bytes.Add(0x01);                       // editable
            bytes.Add(0x00); bytes.Add(0x03);      // pages=3
            bytes.Add((byte)(titleBytes.Length >> 8));
            bytes.Add((byte)(titleBytes.Length & 0xFF));
            bytes.AddRange(titleBytes);
            bytes.Add((byte)(authorBytes.Length >> 8));
            bytes.Add((byte)(authorBytes.Length & 0xFF));
            bytes.AddRange(authorBytes);

            Dispatch(bytes.ToArray(), 3);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x1000u);
            captured.Value.OldPacket.Should().BeFalse();
            captured.Value.Editable.Should().BeTrue();
            captured.Value.PageCount.Should().Be((ushort)3);
            captured.Value.Title.Should().Be(title);
            captured.Value.Author.Should().Be(author);
        }

        // ---- 0xA5 OpenUrl (variable, offset=3) ----

        [Fact]
        public void OpenUrl_0xA5_RaisesOpenUrlRequested()
        {
            OpenUrlRequestedArgs? captured = null;
            EventSink.OpenUrlRequested += e => captured = e;

            string url = "https://example.com/x";
            byte[] urlBytes = Ascii(url, url.Length + 1); // +1 for NUL

            List<byte> bytes = new List<byte>();
            bytes.Add(0xA5);
            bytes.Add(0x00); bytes.Add(0x00);
            bytes.AddRange(urlBytes);

            Dispatch(bytes.ToArray(), 3);

            captured.Should().NotBeNull();
            captured.Value.Url.Should().Be(url);
        }

        // ---- 0xA6 TipWindow (variable, offset=3) ----

        [Fact]
        public void TipWindow_0xA6_RaisesTipWindowDisplayed()
        {
            TipWindowDisplayedArgs? captured = null;
            EventSink.TipWindowDisplayed += e => captured = e;

            string text = "Tip body\rline2";

            List<byte> bytes = new List<byte>();
            bytes.Add(0xA6);
            bytes.Add(0x00); bytes.Add(0x00);     // length placeholder
            bytes.Add(0x00);                       // flag (not 1, so handler proceeds)
            bytes.AddRange(new byte[] { 0x00, 0x00, 0x10, 0x20 }); // tipId
            bytes.Add(0x00); bytes.Add((byte)text.Length);
            bytes.AddRange(Ascii(text, text.Length));

            Dispatch(bytes.ToArray(), 3);

            captured.Should().NotBeNull();
            captured.Value.TipId.Should().Be(0x1020u);
            captured.Value.Flag.Should().Be((byte)0x00);
            captured.Value.Text.Should().Be("Tip body\nline2");
        }

        [Fact]
        public void TipWindow_0xA6_FlagOne_DoesNotRaise()
        {
            bool fired = false;
            EventSink.TipWindowDisplayed += _ => fired = true;

            List<byte> bytes = new List<byte>();
            bytes.Add(0xA6);
            bytes.Add(0x00); bytes.Add(0x00);
            bytes.Add(0x01);   // sentinel — handler returns early

            Dispatch(bytes.ToArray(), 3);

            fired.Should().BeFalse();
        }

        // ---- 0xB8 CharacterProfile (variable, offset=3) ----
        // Gated on !world.InGame → handler returns early. Skip.

        // ---- 0xB0 OpenGump (variable, offset=3) ----
        // Gated on world.Player == null → handler returns early. Skip.

        // ---- 0xDD OpenCompressedGump (variable, offset=3) ----
        // Handler raises CompressedGumpOpened *before* attempting zlib decompress
        // of the layout payload. ZLib.Decompress P/Invokes a native zlib that
        // may be unavailable on this CI host. We swallow any post-raise
        // exception — the contract under test is the event payload.

        [Fact]
        public void OpenCompressedGump_0xDD_RaisesCompressedGumpOpened()
        {
            CompressedGumpOpenedArgs? captured = null;
            EventSink.CompressedGumpOpened += e => captured = e;

            List<byte> bytes = new List<byte>();
            bytes.Add(0xDD);
            bytes.Add(0x00); bytes.Add(0x00);
            bytes.AddRange(BeUInt32(0x11111111));   // sender
            bytes.AddRange(BeUInt32(0x22222222));   // gumpID
            bytes.AddRange(BeUInt32(0x00000050));   // x = 80
            bytes.AddRange(BeUInt32(0x00000060));   // y = 96
            bytes.AddRange(BeUInt32(4));             // compressed len placeholder (-4 → 0)
            bytes.AddRange(BeUInt32(0));             // decompressed len = 0
            bytes.AddRange(BeUInt32(0));             // linesNum = 0

            try
            {
                Dispatch(bytes.ToArray(), 3);
            }
            catch
            {
                // Downstream ZLib/CreateGump may throw on this minimal payload —
                // the event under test was already raised before that point.
            }

            captured.Should().NotBeNull();
            captured.Value.Sender.Should().Be(0x11111111u);
            captured.Value.GumpId.Should().Be(0x22222222u);
            captured.Value.X.Should().Be(80);
            captured.Value.Y.Should().Be(96);
        }

        // ---- 0x1C Talk (variable, offset=3) ----

        [Fact]
        public void Talk_0x1C_RaisesChatMessage()
        {
            ChatMessageArgs? captured = null;
            EventSink.ChatMessage += e => captured = e;

            // Layout: serial(4) graphic(2) type(1) hue(2) font(2) name(30) [text...]
            List<byte> bytes = new List<byte>();
            bytes.Add(0x1C);
            bytes.Add(0x00); bytes.Add(0x00);
            bytes.AddRange(BeUInt32(0xAABBCCDD));      // serial
            bytes.Add(0x01); bytes.Add(0x02);          // graphic = 0x0102
            bytes.Add((byte)MessageType.Regular);      // type=0
            bytes.Add(0x00); bytes.Add(0x21);          // hue = 0x0021
            bytes.Add(0x00); bytes.Add(0x03);          // font = 3
            bytes.AddRange(Ascii("Lord Blackthorn", 30));
            bytes.AddRange(Ascii("Hail traveller!", 16));   // text, NUL-terminated

            Dispatch(bytes.ToArray(), 3);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0xAABBCCDDu);
            captured.Value.Graphic.Should().Be((ushort)0x0102);
            captured.Value.Type.Should().Be(MessageType.Regular);
            captured.Value.Hue.Should().Be((ushort)0x0021);
            captured.Value.Font.Should().Be((byte)3);
            captured.Value.Name.Should().Be("Lord Blackthorn");
            captured.Value.Text.Should().Be("Hail traveller!");
        }

        // ---- 0xAE UnicodeTalk (variable, offset=3) ----
        // Gated on !world.InGame → returns early in test World. Skip.

        // ---- 0xC1 / 0xCC DisplayCliloc ----
        // Gated on world.Player == null. Skip.

        // ---- 0x9A AsciiPrompt ----
        // Gated on !world.InGame. Skip.

        // ---- 0xC2 UnicodePrompt ----
        // Gated on !world.InGame. Skip.

        // ---- 0xB2 ChatMessage sub-events (variable, offset=3) ----

        [Fact]
        public void ChatMessage_0xB2_0x03EB_RaisesChatUsernameRequest()
        {
            bool fired = false;
            EventSink.ChatUsernameRequest += _ => fired = true;

            List<byte> bytes = new List<byte>();
            bytes.Add(0xB2);
            bytes.Add(0x00); bytes.Add(0x00);
            bytes.Add(0x03); bytes.Add(0xEB);

            Dispatch(bytes.ToArray(), 3);

            fired.Should().BeTrue();
        }

        [Fact]
        public void ChatMessage_0xB2_0x03EC_RaisesChatClosed()
        {
            bool fired = false;
            EventSink.ChatClosed += _ => fired = true;

            List<byte> bytes = new List<byte>();
            bytes.Add(0xB2);
            bytes.Add(0x00); bytes.Add(0x00);
            bytes.Add(0x03); bytes.Add(0xEC);

            Dispatch(bytes.ToArray(), 3);

            fired.Should().BeTrue();
        }

        [Fact]
        public void ChatMessage_0xB2_0x03E8_CreateConference_RaisesChatConferenceCreated()
        {
            ChatConferenceCreatedArgs? captured = null;
            EventSink.ChatConferenceCreated += e => captured = e;

            List<byte> bytes = new List<byte>();
            bytes.Add(0xB2);
            bytes.Add(0x00); bytes.Add(0x00);
            bytes.Add(0x03); bytes.Add(0xE8);
            bytes.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00 }); // skipped 4
            WriteUnicodeBE(bytes, "Lobby");
            bytes.Add(0x00); bytes.Add(0x31);                       // 0x31 → hasPassword=true

            Dispatch(bytes.ToArray(), 3);

            captured.Should().NotBeNull();
            captured.Value.ChannelName.Should().Be("Lobby");
            captured.Value.HasPassword.Should().BeTrue();
        }

        [Fact]
        public void ChatMessage_0xB2_0x03EE_AddUser_RaisesChatUserAdded()
        {
            ChatUserAddedArgs? captured = null;
            EventSink.ChatUserAdded += e => captured = e;

            List<byte> bytes = new List<byte>();
            bytes.Add(0xB2);
            bytes.Add(0x00); bytes.Add(0x00);
            bytes.Add(0x03); bytes.Add(0xEE);
            bytes.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00 });
            bytes.Add(0x00); bytes.Add(0x05);                       // userType=5
            WriteUnicodeBE(bytes, "Alice");

            Dispatch(bytes.ToArray(), 3);

            captured.Should().NotBeNull();
            captured.Value.UserType.Should().Be((ushort)5);
            captured.Value.Username.Should().Be("Alice");
        }

        [Fact]
        public void ChatMessage_0xB2_0x0025_RaisesChatTextReceived()
        {
            ChatTextReceivedArgs? captured = null;
            EventSink.ChatTextReceived += e => captured = e;

            List<byte> bytes = new List<byte>();
            bytes.Add(0xB2);
            bytes.Add(0x00); bytes.Add(0x00);
            bytes.Add(0x00); bytes.Add(0x25);                       // cmd
            bytes.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00 });  // skipped 4
            bytes.Add(0x00); bytes.Add(0x00);                       // msgType (unused)
            WriteUnicodeBE(bytes, "Bob");
            WriteUnicodeBE(bytes, "hi there");

            Dispatch(bytes.ToArray(), 3);

            captured.Should().NotBeNull();
            captured.Value.Cmd.Should().Be((ushort)0x0025);
            captured.Value.Username.Should().Be("Bob");
            captured.Value.Message.Should().Be("hi there");
        }

        [Fact]
        public void ChatMessage_0xB2_0x0010_RaisesChatSystemMessage()
        {
            ChatSystemMessageArgs? captured = null;
            EventSink.ChatSystemMessage += e => captured = e;

            List<byte> bytes = new List<byte>();
            bytes.Add(0xB2);
            bytes.Add(0x00); bytes.Add(0x00);
            bytes.Add(0x00); bytes.Add(0x10);                       // cmd in [1..0x24]
            bytes.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00 });
            WriteUnicodeBE(bytes, "system text");

            Dispatch(bytes.ToArray(), 3);

            captured.Should().NotBeNull();
            captured.Value.Cmd.Should().Be((ushort)0x0010);
            captured.Value.Text.Should().Be("system text");
        }

        // ---- 0x3A UpdateSkills ----
        // Gated on !world.InGame. Skip.

        // ---- 0xBA DisplayQuestArrow ----
        // Handler unconditionally dereferences Client.Game.UO.Version *before*
        // raising the event. Client.Game is null in tests, so this would NRE
        // before the event fires. Skip.

        // ---- 0xE5 DisplayWaypoint (variable, offset=3) ----

        [Fact]
        public void DisplayWaypoint_0xE5_RaisesWaypointDisplayed()
        {
            WaypointDisplayedArgs? captured = null;
            EventSink.WaypointDisplayed += e => captured = e;

            string name = "Inn";

            List<byte> bytes = new List<byte>();
            bytes.Add(0xE5);
            bytes.Add(0x00); bytes.Add(0x00);
            bytes.AddRange(BeUInt32(0xDEADBEEF));    // serial
            bytes.Add(0x01); bytes.Add(0x00);        // x = 0x0100
            bytes.Add(0x02); bytes.Add(0x00);        // y = 0x0200
            bytes.Add(0xF5);                          // z = -11 (sbyte)
            bytes.Add(0x01);                          // map = 1
            bytes.Add(0x00); bytes.Add(0x05);        // type = 5
            bytes.Add(0x00); bytes.Add(0x01);        // ignoreobject = true
            bytes.AddRange(BeUInt32(1077344));        // cliloc
            WriteUnicodeLE(bytes, name);

            Dispatch(bytes.ToArray(), 3);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0xDEADBEEFu);
            captured.Value.X.Should().Be((ushort)0x0100);
            captured.Value.Y.Should().Be((ushort)0x0200);
            captured.Value.Z.Should().Be((sbyte)-11);
            captured.Value.Map.Should().Be((byte)1);
            captured.Value.Type.Should().Be((ushort)5);
            captured.Value.IgnoreObject.Should().BeTrue();
            captured.Value.Cliloc.Should().Be(1077344u);
        }

        // ---- 0xE6 RemoveWaypoint (fixed 5, offset=1) ----

        [Fact]
        public void RemoveWaypoint_0xE6_RaisesWaypointRemoved()
        {
            WaypointRemovedArgs? captured = null;
            EventSink.WaypointRemoved += e => captured = e;

            byte[] packet = { 0xE6, 0x00, 0x00, 0x12, 0x34 };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x00001234u);
        }

        // ---- 0x7C OpenMenu ----
        // Gated on !world.InGame. Skip.

        // ---- 0x66 BookData ----
        // Gated on !world.InGame. Skip.

        // ---- 0xAB TextEntryDialog ----
        // Gated on !world.InGame. Skip.

        // ---- 0x71 BulletinBoardData ----
        // Gated on !world.InGame. Skip.

        // ---- 0x3B CloseVendorInterface ----
        // Gated on !world.InGame. Skip.

        // ---- 0x56 MapData ----
        // Gated on !world.InGame. Skip.

        // ---- helpers ----

        private static byte[] BeUInt32(uint v)
        {
            return new byte[]
            {
                (byte)(v >> 24),
                (byte)(v >> 16),
                (byte)(v >> 8),
                (byte)(v & 0xFF)
            };
        }

    }
}
