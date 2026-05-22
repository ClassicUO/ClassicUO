// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Network;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Events
{
    // Positive parse-and-emit tests for UI gump, chat, prompt, vendor, map,
    // and bulletin-board packet handlers whose first action is a
    // world.InGame / world.Player gate. The companion fixture in
    // UiChatSkillsPacketTests skipped these handlers because the gate was
    // unsatisfiable from tests; new World seams (SetPlayerForTests +
    // SetMapForTests, both backed by null-safe ctors) now unblock them.
    //
    // EventSink.ClearAll() is called *after* the World ctor wires up its
    // own subscribers — that way the test's subscriber is the only listener
    // and we don't drive the live UI / Net stack while asserting payloads.
    [Collection("EventSinkSerial")]
    public class UiChatSkillsPacketInGameTests : IDisposable
    {
        private readonly World _world;

        public UiChatSkillsPacketInGameTests()
        {
            _world = new World();
            _world.SetPlayerForTests(new PlayerMobile(_world, 0x00000001u));
            _world.SetMapForTests(new ClassicUO.Game.Map.Map(_world, 0));
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

        private static byte[] BeUInt32(uint v) => new byte[]
        {
            (byte)(v >> 24),
            (byte)(v >> 16),
            (byte)(v >> 8),
            (byte)(v & 0xFF)
        };

        private static byte[] BeUInt16(ushort v) => new byte[]
        {
            (byte)(v >> 8),
            (byte)(v & 0xFF)
        };

        // ---- 0x7C OpenMenu ----
        // Layout: serial(4) id(2) nameLen(1) name(ascii) count(1) [menuid(2)...]
        // After the event raise the handler reads menuid + builds gumps that
        // dereference Client.Game (NRE in tests). We wrap dispatch in try/catch
        // because the contract under test is the event payload.

        [Fact]
        public void OpenMenu_0x7C_RaisesContextMenuOpened()
        {
            ContextMenuOpenedArgs? captured = null;
            EventSink.ContextMenuOpened += e => captured = e;

            string name = "Choose";

            var bytes = new List<byte>();
            bytes.Add(0x7C);
            bytes.Add(0x00); bytes.Add(0x00);                  // length placeholder
            bytes.AddRange(BeUInt32(0x12345678));               // serial
            bytes.AddRange(BeUInt16(0x00AB));                   // menu id
            bytes.Add((byte)name.Length);                       // nameLen
            bytes.AddRange(Ascii(name, name.Length));           // name
            bytes.Add(0x00);                                    // count = 0
            bytes.AddRange(BeUInt16(0x0000));                   // menuid sentinel (== 0 -> Gray path)
            // GrayMenuGump construction NREs on Client.Game.ClientBounds.
            // The event was already raised.

            try { Dispatch(bytes.ToArray(), 3); }
            catch { /* downstream gump construction NREs after raise */ }

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x12345678u);
            captured.Value.MenuId.Should().Be((ushort)0x00AB);
            captured.Value.Name.Should().Be(name);
        }

        // ---- 0x66 BookData ----
        // Handler reads Client.Game.UO.Version *before* the event raise to
        // decide UTF8 vs ASCII page-line decoding. Client.Game is null in
        // tests, so this NREs before BookDataReceived can fire. SKIPPED.

        // ---- 0x71 BulletinBoardData (3 sub-actions) ----

        [Fact]
        public void BulletinBoardData_0x71_Open_RaisesBulletinBoardOpened()
        {
            BulletinBoardOpenedArgs? captured = null;
            EventSink.BulletinBoardOpened += e => captured = e;

            string boardName = "Town Board";

            var bytes = new List<byte>();
            bytes.Add(0x71);
            bytes.Add(0x00); bytes.Add(0x00);                  // length placeholder
            bytes.Add(0x00);                                    // action 0 = open
            bytes.AddRange(BeUInt32(0x1000_2000u));             // serial
            // ReadUTF8(22, true): 22 fixed bytes, NUL-terminated within block
            byte[] name22 = new byte[22];
            for (int i = 0; i < boardName.Length; i++) name22[i] = (byte)boardName[i];
            bytes.AddRange(name22);

            Dispatch(bytes.ToArray(), 3);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x1000_2000u);
            captured.Value.Name.Should().Be(boardName);
        }

        [Fact]
        public void BulletinBoardData_0x71_Summary_RaisesBulletinBoardSummary()
        {
            BulletinBoardSummaryArgs? captured = null;
            EventSink.BulletinBoardSummary += e => captured = e;

            string poster = "Author";
            string subject = "Reward";
            string when = "Day 1";

            var bytes = new List<byte>();
            bytes.Add(0x71);
            bytes.Add(0x00); bytes.Add(0x00);
            bytes.Add(0x01);                                    // action 1 = summary
            bytes.AddRange(BeUInt32(0xAAAA_AAAAu));             // board serial
            bytes.AddRange(BeUInt32(0xBBBB_BBBBu));             // message serial
            bytes.AddRange(BeUInt32(0xCCCC_CCCCu));             // parent serial
            bytes.Add((byte)(poster.Length + 1));               // len incl NUL
            bytes.AddRange(Ascii(poster, poster.Length));
            bytes.Add(0x00);
            bytes.Add((byte)(subject.Length + 1));
            bytes.AddRange(Ascii(subject, subject.Length));
            bytes.Add(0x00);
            bytes.Add((byte)(when.Length + 1));
            bytes.AddRange(Ascii(when, when.Length));
            bytes.Add(0x00);

            Dispatch(bytes.ToArray(), 3);

            captured.Should().NotBeNull();
            captured.Value.BoardSerial.Should().Be(0xAAAA_AAAAu);
            captured.Value.MessageSerial.Should().Be(0xBBBB_BBBBu);
            captured.Value.ParentSerial.Should().Be(0xCCCC_CCCCu);
            captured.Value.Poster.Should().Be(poster);
            captured.Value.Subject.Should().Be(subject);
            captured.Value.DateTime.Should().Be(when);
        }

        [Fact]
        public void BulletinBoardData_0x71_Message_RaisesBulletinBoardMessage()
        {
            BulletinBoardMessageArgs? captured = null;
            EventSink.BulletinBoardMessage += e => captured = e;

            string poster = "Author";
            string subject = "Reward";
            string when = "Day 1";
            string line1 = "Line one";
            string line2 = "Line two";

            var bytes = new List<byte>();
            bytes.Add(0x71);
            bytes.Add(0x00); bytes.Add(0x00);
            bytes.Add(0x02);                                    // action 2 = message
            bytes.AddRange(BeUInt32(0xDEAD_BEEFu));             // board serial
            bytes.AddRange(BeUInt32(0xCAFE_BABEu));             // message serial
            bytes.Add((byte)poster.Length);                     // ASCII (no NUL)
            bytes.AddRange(Ascii(poster, poster.Length));
            bytes.Add((byte)(subject.Length + 1));              // UTF8 + NUL
            bytes.AddRange(Ascii(subject, subject.Length));
            bytes.Add(0x00);
            bytes.Add((byte)when.Length);                       // ASCII (no NUL)
            bytes.AddRange(Ascii(when, when.Length));
            bytes.AddRange(new byte[] { 0, 0, 0, 0 });           // p.Skip(4)
            bytes.Add(0x00);                                    // unk = 0
            bytes.Add(0x02);                                    // line count
            bytes.Add((byte)(line1.Length + 1));
            bytes.AddRange(Ascii(line1, line1.Length)); bytes.Add(0x00);
            bytes.Add((byte)(line2.Length + 1));
            bytes.AddRange(Ascii(line2, line2.Length)); bytes.Add(0x00);

            Dispatch(bytes.ToArray(), 3);

            captured.Should().NotBeNull();
            captured.Value.BoardSerial.Should().Be(0xDEAD_BEEFu);
            captured.Value.MessageSerial.Should().Be(0xCAFE_BABEu);
            captured.Value.Poster.Should().Be(poster);
            captured.Value.Subject.Should().Be(subject);
            captured.Value.DateTime.Should().Be(when);
            captured.Value.Message.Should().Contain(line1).And.Contain(line2);
        }

        // ---- 0xAB TextEntryDialog ----

        [Fact]
        public void TextEntryDialog_0xAB_RaisesTextEntryDialogOpened()
        {
            TextEntryDialogArgs? captured = null;
            EventSink.TextEntryDialogOpened += e => captured = e;

            string text = "default";
            string desc = "describe me";

            var bytes = new List<byte>();
            bytes.Add(0xAB);
            bytes.Add(0x00); bytes.Add(0x00);
            bytes.AddRange(BeUInt32(0x1111_2222u));             // serial
            bytes.Add(0x05);                                    // parentID
            bytes.Add(0x09);                                    // buttonID
            bytes.AddRange(BeUInt16((ushort)text.Length));
            bytes.AddRange(Ascii(text, text.Length));
            bytes.Add(0x01);                                    // haveCancel
            bytes.Add(0x00);                                    // variant
            bytes.AddRange(BeUInt32(64u));                       // maxLength
            bytes.AddRange(BeUInt16((ushort)desc.Length));
            bytes.AddRange(Ascii(desc, desc.Length));

            Dispatch(bytes.ToArray(), 3);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x1111_2222u);
            captured.Value.ParentId.Should().Be((byte)0x05);
            captured.Value.ButtonId.Should().Be((byte)0x09);
            captured.Value.MaxLength.Should().Be(64u);
            captured.Value.Text.Should().Be(text);
            captured.Value.Description.Should().Be(desc);
        }

        // ---- 0xB8 CharacterProfile ----

        [Fact]
        public void CharacterProfile_0xB8_RaisesCharacterProfileOpened()
        {
            CharacterProfileOpenedArgs? captured = null;
            EventSink.CharacterProfileOpened += e => captured = e;

            string header = "Sir Reginald";
            string footer = "Footer note";
            string body = "Body content";

            var bytes = new List<byte>();
            bytes.Add(0xB8);
            bytes.Add(0x00); bytes.Add(0x00);
            bytes.AddRange(BeUInt32(0x9999_8888u));
            bytes.AddRange(Ascii(header, header.Length));
            bytes.Add(0x00);                                    // ASCII NUL
            WriteUnicodeBE(bytes, footer);
            WriteUnicodeBE(bytes, body);

            Dispatch(bytes.ToArray(), 3);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x9999_8888u);
            captured.Value.Header.Should().Be(header);
            captured.Value.Footer.Should().Be(footer);
            captured.Value.Body.Should().Be(body);
        }

        // ---- 0xB0 OpenGump ----

        [Fact]
        public void OpenGump_0xB0_RaisesGumpOpened()
        {
            GumpOpenedArgs? captured = null;
            EventSink.GumpOpened += e => captured = e;

            var bytes = new List<byte>();
            bytes.Add(0xB0);
            bytes.Add(0x00); bytes.Add(0x00);
            bytes.AddRange(BeUInt32(0x0000_00AAu));             // sender
            bytes.AddRange(BeUInt32(0x0000_00BBu));             // gump id
            bytes.AddRange(BeUInt32(0x0000_0040u));             // x = 64
            bytes.AddRange(BeUInt32(0x0000_0080u));             // y = 128
            bytes.AddRange(BeUInt16(0x0000));                   // cmd len = 0 -> empty layout
            bytes.AddRange(BeUInt16(0x0000));                   // textLinesCount = 0

            Dispatch(bytes.ToArray(), 3);

            captured.Should().NotBeNull();
            captured.Value.Sender.Should().Be(0x0000_00AAu);
            captured.Value.GumpId.Should().Be(0x0000_00BBu);
            captured.Value.X.Should().Be(64);
            captured.Value.Y.Should().Be(128);
        }

        // ---- 0x3B CloseVendorInterface ----

        [Fact]
        public void CloseVendorInterface_0x3B_RaisesVendorWindowClosed()
        {
            VendorWindowClosedArgs? captured = null;
            EventSink.VendorWindowClosed += e => captured = e;

            byte[] packet =
            {
                0x3B,
                0x00, 0x00, 0x12, 0x34,                          // serial
            };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.VendorSerial.Should().Be(0x00001234u);
        }

        // ---- 0x56 MapData ----

        [Fact]
        public void MapData_0x56_Add_RaisesMapDataReceived()
        {
            MapDataReceivedArgs? captured = null;
            EventSink.MapDataReceived += e => captured = e;

            byte[] packet =
            {
                0x56,
                0xDE, 0xAD, 0xBE, 0xEF,                          // serial
                0x01,                                            // action = Add
                0x00,                                            // skipped padding
                0x00, 0x40,                                      // pinX = 64
                0x00, 0x20,                                      // pinY = 32
            };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0xDEADBEEFu);
            captured.Value.Action.Should().Be((byte)0x01);
            captured.Value.PinX.Should().Be((ushort)64);
            captured.Value.PinY.Should().Be((ushort)32);
            captured.Value.PlotState.Should().Be((byte)0);
        }

        [Fact]
        public void MapData_0x56_EditResponse_RaisesMapDataReceived_WithPlotState()
        {
            MapDataReceivedArgs? captured = null;
            EventSink.MapDataReceived += e => captured = e;

            byte[] packet =
            {
                0x56,
                0x00, 0x00, 0x00, 0x55,                          // serial
                0x07,                                            // action = EditResponse
                0x03,                                            // plotState
            };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x55u);
            captured.Value.Action.Should().Be((byte)0x07);
            captured.Value.PlotState.Should().Be((byte)0x03);
            captured.Value.PinX.Should().Be((ushort)0);
            captured.Value.PinY.Should().Be((ushort)0);
        }

        // ---- 0xAE UnicodeTalk ----
        // Layout: serial(4) graphic(2) type(1) hue(2) font(2) lang(4) name(ascii NUL)
        // Then handler does `if (p.Length > 48) { p.Seek(48); text = p.ReadUnicodeBE(); }`.

        [Fact]
        public void UnicodeTalk_0xAE_RaisesUnicodeChatMessage()
        {
            UnicodeChatMessageArgs? captured = null;
            EventSink.UnicodeChatMessage += e => captured = e;

            string text = "Hail!";

            var bytes = new List<byte>();
            bytes.Add(0xAE);
            bytes.Add(0x00); bytes.Add(0x00);                  // length placeholder
            bytes.AddRange(BeUInt32(0x0000_1234u));             // serial
            bytes.AddRange(BeUInt16(0x0011));                   // graphic
            bytes.Add((byte)MessageType.Regular);               // type = 0
            bytes.AddRange(BeUInt16(0x0042));                   // hue
            bytes.AddRange(BeUInt16(0x0003));                   // font
            bytes.AddRange(Ascii("ENU", 4));                    // lang (NUL-padded to 4)
            // Pad up to absolute position 48. Already wrote:
            //   3 (id+len) + 4 + 2 + 1 + 2 + 2 + 4 = 18 bytes
            // So name region runs from pos 18 to pos 48 (30 bytes).
            byte[] name30 = Ascii("Lord Test", 30);
            bytes.AddRange(name30);
            // Now we're at pos 48 — start of UnicodeBE text.
            WriteUnicodeBE(bytes, text);

            Dispatch(bytes.ToArray(), 3);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x0000_1234u);
            captured.Value.Graphic.Should().Be((ushort)0x0011);
            captured.Value.Type.Should().Be(MessageType.Regular);
            captured.Value.Hue.Should().Be((ushort)0x0042);
            captured.Value.Font.Should().Be((byte)0x03);
            captured.Value.Lang.Should().Be("ENU");
            captured.Value.Name.Should().Be("Lord Test");
            captured.Value.Text.Should().Be(text);
        }

        // ---- 0xC1 DisplayCliloc (no affix) ----

        [Fact]
        public void DisplayCliloc_0xC1_RaisesClilocMessage_NoAffix()
        {
            ClilocMessageArgs? captured = null;
            EventSink.ClilocMessage += e => captured = e;

            // Layout: serial(4) graphic(2) type(1) hue(2) font(2) cliloc(4) name(ascii 30) [args unicodeLE remainder]
            var bytes = new List<byte>();
            bytes.Add(0xC1);
            bytes.Add(0x00); bytes.Add(0x00);
            bytes.AddRange(BeUInt32(0x0000_2222u));             // serial
            bytes.AddRange(BeUInt16(0x00AA));                   // graphic
            bytes.Add((byte)MessageType.System);                // type = 1
            bytes.AddRange(BeUInt16(0x0030));                   // hue
            bytes.AddRange(BeUInt16(0x0001));                   // font
            bytes.AddRange(BeUInt32(1042971u));                 // cliloc
            bytes.AddRange(Ascii("Test", 30));                  // name (30 chars)
            WriteUnicodeLE(bytes, "tab\targ", nullTerminate: false);

            Dispatch(bytes.ToArray(), 3);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x0000_2222u);
            captured.Value.Graphic.Should().Be((ushort)0x00AA);
            captured.Value.Type.Should().Be(MessageType.System);
            captured.Value.Hue.Should().Be((ushort)0x0030);
            captured.Value.Font.Should().Be((byte)0x01);
            captured.Value.Cliloc.Should().Be(1042971u);
            captured.Value.Name.Should().Be("Test");
            captured.Value.Arguments.Should().Be("tab\targ");
            captured.Value.Affix.Should().BeEmpty();
            captured.Value.AffixFlags.Should().Be((byte)0);
        }

        // ---- 0xCC DisplayCliloc (with affix) ----

        [Fact]
        public void DisplayCliloc_0xCC_RaisesClilocMessage_WithAffix()
        {
            ClilocMessageArgs? captured = null;
            EventSink.ClilocMessage += e => captured = e;

            // Layout: serial(4) graphic(2) type(1) hue(2) font(2) cliloc(4) flags(1) name(ascii 30) affix(ascii NUL) [args unicodeBE remainder]
            string affix = "AFX";
            string args = "argy";

            var bytes = new List<byte>();
            bytes.Add(0xCC);
            bytes.Add(0x00); bytes.Add(0x00);
            bytes.AddRange(BeUInt32(0x0000_3333u));             // serial
            bytes.AddRange(BeUInt16(0x00BB));                   // graphic
            bytes.Add((byte)MessageType.Label);                 // type = 6
            bytes.AddRange(BeUInt16(0x0044));                   // hue
            bytes.AddRange(BeUInt16(0x0005));                   // font
            bytes.AddRange(BeUInt32(0x000F4240));               // cliloc = 1000000
            bytes.Add(0x01);                                    // affix flags = Prepend
            bytes.AddRange(Ascii("Lyra", 30));
            bytes.AddRange(Ascii(affix, affix.Length));         // ascii NUL-terminated
            bytes.Add(0x00);
            WriteUnicodeBE(bytes, args, nullTerminate: false);

            Dispatch(bytes.ToArray(), 3);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x0000_3333u);
            captured.Value.Cliloc.Should().Be(0x000F4240u);
            captured.Value.Name.Should().Be("Lyra");
            captured.Value.Affix.Should().Be(affix);
            captured.Value.Arguments.Should().Be(args);
            captured.Value.AffixFlags.Should().Be((byte)0x01);
        }

        // ---- 0x9A ASCIIPrompt ----

        [Fact]
        public void ASCIIPrompt_0x9A_RaisesAsciiPrompt()
        {
            AsciiPromptArgs? captured = null;
            EventSink.AsciiPrompt += e => captured = e;

            var bytes = new List<byte>();
            bytes.Add(0x9A);
            bytes.Add(0x00); bytes.Add(0x00);
            bytes.AddRange(new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF }); // prompt id

            Dispatch(bytes.ToArray(), 3);

            captured.Should().NotBeNull();
            captured.Value.PromptId.Should().Be(0x0123456789ABCDEFul);
        }

        // ---- 0xC2 UnicodePrompt ----

        [Fact]
        public void UnicodePrompt_0xC2_RaisesUnicodePrompt()
        {
            UnicodePromptArgs? captured = null;
            EventSink.UnicodePrompt += e => captured = e;

            var bytes = new List<byte>();
            bytes.Add(0xC2);
            bytes.Add(0x00); bytes.Add(0x00);
            bytes.AddRange(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0xCA, 0xFE, 0xBA, 0xBE });

            Dispatch(bytes.ToArray(), 3);

            captured.Should().NotBeNull();
            captured.Value.PromptId.Should().Be(0xDEADBEEFCAFEBABEul);
        }

        // ---- 0x3A UpdateSkills ----
        // Already covered by UpdateSkillsTests (no gate; emit-only). Not duplicated.

        // ---- 0x90 DisplayMap ----
        // Handler reaches `else if (Client.Game.UO.Version >= Utility.ClientVersion.CV_308Z)`
        // *before* the event raise for any packet whose first byte is not 0xF5.
        // Client.Game is null in tests -> NRE before event. SKIPPED.

        // ---- 0xBA DisplayQuestArrow ----
        // Handler reads `Client.Game.UO.Version` to decide whether to read the
        // serial field *before* the event raise. Client.Game is null in tests
        // -> NRE before event. SKIPPED.
    }
}
