// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.Events;
using ClassicUO.Network;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Events
{
    // Golden-byte tests for PacketHandlers. Each test builds a raw packet,
    // dispatches it through PacketHandlers.AnalyzePacket, and asserts that
    // the matching EventSink event fires with the parsed args.
    //
    // Tests use a real World instance so that the same handler path runs as
    // in production. EventSink.ClearAll() runs after World construction to
    // remove the manager subscriptions World wires up — leaves us with only
    // the test subscriber listening.
    public class PacketHandlersTests : IDisposable
    {
        private readonly World _world;

        public PacketHandlersTests()
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

        // ---- 0x73 Ping ----

        [Fact]
        public void Ping_0x73_RaisesPingReceived()
        {
            PingReceivedArgs? captured = null;
            EventSink.PingReceived += e => captured = e;

            Dispatch(new byte[] { 0x73, 0x42 }, 1);

            captured.Should().NotBeNull();
            captured.Value.Sequence.Should().Be(0x42);
        }

        // ---- 0x6D PlayMusic ----

        [Fact]
        public void PlayMusic_0x6D_Short_RaisesMusicPlay()
        {
            // Short form: 6D <cmd> <index>. Production passes data length=3.
            // AnalyzePacket reads data[0] as packet id, seeks past offset=1.
            // PlayMusic checks p.Length==3, which is the StackDataReader length
            // (matches data.Length).
            MusicPlayArgs? captured = null;
            EventSink.MusicPlay += e => captured = e;

            Dispatch(new byte[] { 0x6D, 0x10, 0x2A }, 1);

            captured.Should().NotBeNull();
            captured.Value.Index.Should().Be(0x2A);
        }

        [Fact]
        public void PlayMusic_0x6D_StopSentinel_RaisesMusicStop_NotMusicPlay()
        {
            bool playFired = false;
            bool stopFired = false;
            EventSink.MusicPlay += _ => playFired = true;
            EventSink.MusicStop += _ => stopFired = true;

            // 6D 1F FF = stop music sentinel
            Dispatch(new byte[] { 0x6D, 0x1F, 0xFF }, 1);

            stopFired.Should().BeTrue();
            playFired.Should().BeFalse();
        }

        [Fact]
        public void PlayMusic_0x6D_LongForm_RaisesMusicPlay()
        {
            // Long form: 6D <index_hi> <index_lo>. Length != 3 path reads ushort BE
            // starting at offset=1. Pad with a trailing byte so p.Length=4 bypasses
            // the short-form branch.
            MusicPlayArgs? captured = null;
            EventSink.MusicPlay += e => captured = e;

            Dispatch(new byte[] { 0x6D, 0x12, 0x34, 0x00 }, 1);

            captured.Should().NotBeNull();
            captured.Value.Index.Should().Be(0x1234);
        }

        // ---- 0xFD LoginDelay ----

        [Fact]
        public void LoginDelay_0xFD_RaisesLoginDelayReceived()
        {
            // Gated on !world.InGame. Fresh World has Player=null so InGame=false.
            LoginDelayReceivedArgs? captured = null;
            EventSink.LoginDelayReceived += e => captured = e;

            Dispatch(new byte[] { 0xFD, 0x07 }, 1);

            captured.Should().NotBeNull();
            captured.Value.Delay.Should().Be(0x07);
        }

        // ---- 0x82 / 0x85 / 0x53 LoginRejected ----

        [Fact]
        public void LoginRejected_0x82_RaisesLoginRejected_WithPacketIdAndReason()
        {
            LoginRejectedArgs? captured = null;
            EventSink.LoginRejected += e => captured = e;

            Dispatch(new byte[] { 0x82, 0x03 }, 1);

            captured.Should().NotBeNull();
            captured.Value.PacketId.Should().Be(0x82);
            captured.Value.Reason.Should().Be(0x03);
        }

        // ---- 0xA8 ServerListReceived (variable length, offset=3) ----

        [Fact]
        public void ServerListReceived_0xA8_EmptyList_RaisesServerListReceived()
        {
            // Variable-length packet. Layout: A8 | len(2) | flags(1) | count(2) | entries...
            // offset=3 skips packet id + length bytes.
            ServerListReceivedArgs? captured = null;
            EventSink.ServerListReceived += e => captured = e;

            byte[] packet =
            {
                0xA8,
                0x00, 0x06,       // length placeholder (not read by handler)
                0xCC,             // flags
                0x00, 0x00        // count = 0
            };

            Dispatch(packet, 3);

            captured.Should().NotBeNull();
            captured.Value.Flags.Should().Be(0xCC);
            captured.Value.Servers.Count.Should().Be(0);
        }

        // ---- 0xBF 0x06 PartyPacket inner codes ----

        [Fact]
        public void Party_0xBF06_Invite_Code07_RaisesPartyInviteReceived()
        {
            // BF | len(2) | cmd(2)=0006 | inner code(1)=07 | inviter(4)
            PartyInviteReceivedArgs? captured = null;
            EventSink.PartyInviteReceived += e => captured = e;

            byte[] packet =
            {
                0xBF,
                0x00, 0x0A,                  // length placeholder
                0x00, 0x06,                  // 0xBF sub-cmd = 0x06 party
                0x07,                        // inner code 0x07
                0xDE, 0xAD, 0xBE, 0xEF       // inviter
            };

            Dispatch(packet, 3);

            captured.Should().NotBeNull();
            captured.Value.Inviter.Should().Be(0xDEADBEEFu);
        }

        [Fact]
        public void Party_0xBF06_Add_Code01_RaisesPartyListUpdated_IsAddTrue()
        {
            PartyListUpdatedArgs? captured = null;
            EventSink.PartyListUpdated += e => captured = e;

            // count=2, then 2 serials (no removedSerial because IsAdd=true)
            byte[] packet =
            {
                0xBF,
                0x00, 0x10,
                0x00, 0x06,
                0x01,                                // add
                0x02,                                // count
                0x00, 0x00, 0x00, 0x0A,              // serial 0
                0x00, 0x00, 0x00, 0x0B               // serial 1
            };

            Dispatch(packet, 3);

            captured.Should().NotBeNull();
            captured.Value.IsAdd.Should().BeTrue();
            captured.Value.RemovedSerial.Should().Be(0u);
            captured.Value.Serials.Should().Equal(new uint[] { 0x0Au, 0x0Bu });
        }

        [Fact]
        public void Party_0xBF06_Remove_Code02_RaisesPartyListUpdated_WithRemovedSerial()
        {
            PartyListUpdatedArgs? captured = null;
            EventSink.PartyListUpdated += e => captured = e;

            // count=2, then removedSerial, then 2 serials
            byte[] packet =
            {
                0xBF,
                0x00, 0x14,
                0x00, 0x06,
                0x02,                                // remove
                0x02,                                // count
                0x00, 0x00, 0x00, 0xFF,              // removed
                0x00, 0x00, 0x00, 0x0A,              // serial 0
                0x00, 0x00, 0x00, 0x0B               // serial 1
            };

            Dispatch(packet, 3);

            captured.Should().NotBeNull();
            captured.Value.IsAdd.Should().BeFalse();
            captured.Value.RemovedSerial.Should().Be(0xFFu);
            captured.Value.Serials.Should().Equal(new uint[] { 0x0Au, 0x0Bu });
        }

        [Fact]
        public void Party_0xBF06_Chat_Code03_RaisesPartyChatMessage()
        {
            PartyChatMessageArgs? captured = null;
            EventSink.PartyChatMessage += e => captured = e;

            // Chat: code 0x03 | serial(4) | unicodeBE string (null-terminated)
            // "Hi" in unicode BE: 00 48 00 69 00 00
            byte[] packet =
            {
                0xBF,
                0x00, 0x10,
                0x00, 0x06,
                0x03,
                0x00, 0x00, 0x00, 0x42,              // serial
                0x00, 0x48, 0x00, 0x69, 0x00, 0x00   // "Hi\0"
            };

            Dispatch(packet, 3);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x42u);
            captured.Value.Text.Should().Be("Hi");
        }

        // ---- 0xBF 0x18 MapPatchesEnabled ----

        [Fact]
        public void MapPatches_0xBF18_RaisesMapPatchesEnabled_WithParsedEntries()
        {
            MapPatchesEnabledArgs? captured = null;
            EventSink.MapPatchesEnabled += e => captured = e;

            // 0xBF | len(2) | cmd=0x0018 | count(4) | per-map (mapPatch, staticPatch) pairs
            byte[] packet =
            {
                0xBF,
                0x00, 0x15,
                0x00, 0x18,
                0x00, 0x00, 0x00, 0x02,              // patchesCount=2
                0x00, 0x00, 0x00, 0x0A,              // map[0].mapPatchCount
                0x00, 0x00, 0x00, 0x0B,              // map[0].staticPatchCount
                0x00, 0x00, 0x00, 0x0C,              // map[1].mapPatchCount
                0x00, 0x00, 0x00, 0x0D               // map[1].staticPatchCount
            };

            Dispatch(packet, 3);

            captured.Should().NotBeNull();
            captured.Value.PatchesCount.Should().Be(2);
            captured.Value.Entries.Count.Should().Be(2);
            captured.Value.Entries[0].Should().Be(new MapPatchEntry(0x0A, 0x0B));
            captured.Value.Entries[1].Should().Be(new MapPatchEntry(0x0C, 0x0D));
        }
    }
}
