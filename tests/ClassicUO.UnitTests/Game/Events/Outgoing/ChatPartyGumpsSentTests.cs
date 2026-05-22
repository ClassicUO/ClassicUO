// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Game.Data;
using ClassicUO.Game.Events.Outgoing;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Events.Outgoing
{
    [Collection("EventSinkSerial")]
    public class ChatPartyGumpsSentTests
    {
        public ChatPartyGumpsSentTests()
        {
            OutgoingEventSink.ClearAll();
        }

        // ---- Chat / Social ----

        [Fact]
        public void RaiseEmoteActionSent_Should_Invoke_Subscriber()
        {
            EmoteActionSentArgs? captured = null;
            OutgoingEventSink.EmoteActionSent += e => captured = e;

            var expected = new EmoteActionSentArgs("waves");
            OutgoingEventSink.RaiseEmoteActionSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseAsciiSpeechRequestSent_Should_Invoke_Subscriber()
        {
            AsciiSpeechRequestSentArgs? captured = null;
            OutgoingEventSink.AsciiSpeechRequestSent += e => captured = e;

            var expected = new AsciiSpeechRequestSentArgs("hi", MessageType.Regular, (byte)3, (ushort)0x0035);
            OutgoingEventSink.RaiseAsciiSpeechRequestSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseUnicodeSpeechRequestSent_Should_Invoke_Subscriber()
        {
            UnicodeSpeechRequestSentArgs? captured = null;
            OutgoingEventSink.UnicodeSpeechRequestSent += e => captured = e;

            var expected = new UnicodeSpeechRequestSentArgs("hello", MessageType.Whisper, (byte)1, (ushort)0x0040, "ENU");
            OutgoingEventSink.RaiseUnicodeSpeechRequestSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseChatJoinCommandSent_Should_Invoke_Subscriber()
        {
            ChatJoinCommandSentArgs? captured = null;
            OutgoingEventSink.ChatJoinCommandSent += e => captured = e;

            var expected = new ChatJoinCommandSentArgs("Lobby", "secret");
            OutgoingEventSink.RaiseChatJoinCommandSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseChatCreateChannelCommandSent_Should_Invoke_Subscriber()
        {
            ChatCreateChannelCommandSentArgs? captured = null;
            OutgoingEventSink.ChatCreateChannelCommandSent += e => captured = e;

            var expected = new ChatCreateChannelCommandSentArgs("MyRoom", "pw");
            OutgoingEventSink.RaiseChatCreateChannelCommandSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseChatLeaveChannelCommandSent_Should_Invoke_Subscriber()
        {
            bool fired = false;
            OutgoingEventSink.ChatLeaveChannelCommandSent += _ => fired = true;

            OutgoingEventSink.RaiseChatLeaveChannelCommandSent(new ChatLeaveChannelCommandSentArgs());

            fired.Should().BeTrue();
        }

        [Fact]
        public void RaiseChatMessageCommandSent_Should_Invoke_Subscriber()
        {
            ChatMessageCommandSentArgs? captured = null;
            OutgoingEventSink.ChatMessageCommandSent += e => captured = e;

            var expected = new ChatMessageCommandSentArgs("hello world");
            OutgoingEventSink.RaiseChatMessageCommandSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseOpenChatSent_Should_Invoke_Subscriber()
        {
            OpenChatSentArgs? captured = null;
            OutgoingEventSink.OpenChatSent += e => captured = e;

            var expected = new OpenChatSentArgs("TestChan");
            OutgoingEventSink.RaiseOpenChatSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseMapMessageSent_Should_Invoke_Subscriber()
        {
            MapMessageSentArgs? captured = null;
            OutgoingEventSink.MapMessageSent += e => captured = e;

            var expected = new MapMessageSentArgs(0x40000001u, (byte)1, (byte)2, (ushort)123, (ushort)456);
            OutgoingEventSink.RaiseMapMessageSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseRazorAckSent_Should_Invoke_Subscriber()
        {
            bool fired = false;
            OutgoingEventSink.RazorAckSent += _ => fired = true;

            OutgoingEventSink.RaiseRazorAckSent(new RazorAckSentArgs());

            fired.Should().BeTrue();
        }

        // ---- Party ----

        [Fact]
        public void RaisePartyInviteRequestSent_Should_Invoke_Subscriber()
        {
            bool fired = false;
            OutgoingEventSink.PartyInviteRequestSent += _ => fired = true;

            OutgoingEventSink.RaisePartyInviteRequestSent(new PartyInviteRequestSentArgs());

            fired.Should().BeTrue();
        }

        [Fact]
        public void RaisePartyRemoveRequestSent_Should_Invoke_Subscriber()
        {
            PartyRemoveRequestSentArgs? captured = null;
            OutgoingEventSink.PartyRemoveRequestSent += e => captured = e;

            var expected = new PartyRemoveRequestSentArgs(0xDEADBEEFu);
            OutgoingEventSink.RaisePartyRemoveRequestSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaisePartyChangeLootTypeRequestSent_Should_Invoke_Subscriber()
        {
            PartyChangeLootTypeRequestSentArgs? captured = null;
            OutgoingEventSink.PartyChangeLootTypeRequestSent += e => captured = e;

            var expected = new PartyChangeLootTypeRequestSentArgs(true);
            OutgoingEventSink.RaisePartyChangeLootTypeRequestSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaisePartyAcceptSent_Should_Invoke_Subscriber()
        {
            PartyAcceptSentArgs? captured = null;
            OutgoingEventSink.PartyAcceptSent += e => captured = e;

            var expected = new PartyAcceptSentArgs(0x12345678u);
            OutgoingEventSink.RaisePartyAcceptSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaisePartyDeclineSent_Should_Invoke_Subscriber()
        {
            PartyDeclineSentArgs? captured = null;
            OutgoingEventSink.PartyDeclineSent += e => captured = e;

            var expected = new PartyDeclineSentArgs(0xABCDEF01u);
            OutgoingEventSink.RaisePartyDeclineSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaisePartyMessageSent_Should_Invoke_Subscriber()
        {
            PartyMessageSentArgs? captured = null;
            OutgoingEventSink.PartyMessageSent += e => captured = e;

            var expected = new PartyMessageSentArgs("hi team", 0x00000005u);
            OutgoingEventSink.RaisePartyMessageSent(expected);

            captured.Should().Be(expected);
        }

        // ---- Trade / Vendor ----

        [Fact]
        public void RaiseTradeResponseSent_Should_Invoke_Subscriber()
        {
            TradeResponseSentArgs? captured = null;
            OutgoingEventSink.TradeResponseSent += e => captured = e;

            var expected = new TradeResponseSentArgs(0x40000010u, 2, true);
            OutgoingEventSink.RaiseTradeResponseSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseTradeUpdateGoldSent_Should_Invoke_Subscriber()
        {
            TradeUpdateGoldSentArgs? captured = null;
            OutgoingEventSink.TradeUpdateGoldSent += e => captured = e;

            var expected = new TradeUpdateGoldSentArgs(0x40000020u, 1234u, 56u);
            OutgoingEventSink.RaiseTradeUpdateGoldSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseBuyRequestSent_Should_Invoke_Subscriber()
        {
            BuyRequestSentArgs? captured = null;
            OutgoingEventSink.BuyRequestSent += e => captured = e;

            var items = new[]
            {
                Tuple.Create(0x40000001u, (ushort)1),
                Tuple.Create(0x40000002u, (ushort)5),
            };
            var expected = new BuyRequestSentArgs(0x40000099u, items);
            OutgoingEventSink.RaiseBuyRequestSent(expected);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x40000099u);
            captured.Value.Items.Should().BeSameAs(items);
        }

        [Fact]
        public void RaiseSellRequestSent_Should_Invoke_Subscriber()
        {
            SellRequestSentArgs? captured = null;
            OutgoingEventSink.SellRequestSent += e => captured = e;

            var items = new[]
            {
                Tuple.Create(0x40000003u, (ushort)2),
            };
            var expected = new SellRequestSentArgs(0x400000AAu, items);
            OutgoingEventSink.RaiseSellRequestSent(expected);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x400000AAu);
            captured.Value.Items.Should().BeSameAs(items);
        }

        // ---- Gumps / Menus ----

        [Fact]
        public void RaiseGumpResponseSent_Should_Invoke_Subscriber()
        {
            GumpResponseSentArgs? captured = null;
            OutgoingEventSink.GumpResponseSent += e => captured = e;

            var switches = new uint[] { 1, 2, 3 };
            var entries = new[] { Tuple.Create((ushort)100, "abc") };
            var expected = new GumpResponseSentArgs(0x10u, 0x20u, 7, switches, entries);
            OutgoingEventSink.RaiseGumpResponseSent(expected);

            captured.Should().NotBeNull();
            captured.Value.Local.Should().Be(0x10u);
            captured.Value.Server.Should().Be(0x20u);
            captured.Value.Button.Should().Be(7);
            captured.Value.Switches.Should().BeSameAs(switches);
            captured.Value.Entries.Should().BeSameAs(entries);
        }

        [Fact]
        public void RaiseVirtueGumpResponseSent_Should_Invoke_Subscriber()
        {
            VirtueGumpResponseSentArgs? captured = null;
            OutgoingEventSink.VirtueGumpResponseSent += e => captured = e;

            var expected = new VirtueGumpResponseSentArgs(0x40000030u, 5u);
            OutgoingEventSink.RaiseVirtueGumpResponseSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseMenuResponseSent_Should_Invoke_Subscriber()
        {
            MenuResponseSentArgs? captured = null;
            OutgoingEventSink.MenuResponseSent += e => captured = e;

            var expected = new MenuResponseSentArgs(0x40000040u, (ushort)0x100, 3, (ushort)0x200, (ushort)0x300);
            OutgoingEventSink.RaiseMenuResponseSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseGrayMenuResponseSent_Should_Invoke_Subscriber()
        {
            GrayMenuResponseSentArgs? captured = null;
            OutgoingEventSink.GrayMenuResponseSent += e => captured = e;

            var expected = new GrayMenuResponseSentArgs(0x40000050u, (ushort)0x400, (ushort)0x500);
            OutgoingEventSink.RaiseGrayMenuResponseSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseRequestPopupMenuSent_Should_Invoke_Subscriber()
        {
            RequestPopupMenuSentArgs? captured = null;
            OutgoingEventSink.RequestPopupMenuSent += e => captured = e;

            var expected = new RequestPopupMenuSentArgs(0x40000060u);
            OutgoingEventSink.RaiseRequestPopupMenuSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaisePopupMenuSelectionSent_Should_Invoke_Subscriber()
        {
            PopupMenuSelectionSentArgs? captured = null;
            OutgoingEventSink.PopupMenuSelectionSent += e => captured = e;

            var expected = new PopupMenuSelectionSentArgs(0x40000070u, (ushort)42);
            OutgoingEventSink.RaisePopupMenuSelectionSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseTextEntryDialogResponseSent_Should_Invoke_Subscriber()
        {
            TextEntryDialogResponseSentArgs? captured = null;
            OutgoingEventSink.TextEntryDialogResponseSent += e => captured = e;

            var expected = new TextEntryDialogResponseSentArgs(0x40000080u, (byte)1, (byte)2, "answer", true);
            OutgoingEventSink.RaiseTextEntryDialogResponseSent(expected);

            captured.Should().Be(expected);
        }

        // ---- Prompts ----

        [Fact]
        public void RaiseAsciiPromptResponseSent_Should_Invoke_Subscriber()
        {
            AsciiPromptResponseSentArgs? captured = null;
            OutgoingEventSink.AsciiPromptResponseSent += e => captured = e;

            var expected = new AsciiPromptResponseSentArgs("response", false);
            OutgoingEventSink.RaiseAsciiPromptResponseSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseUnicodePromptResponseSent_Should_Invoke_Subscriber()
        {
            UnicodePromptResponseSentArgs? captured = null;
            OutgoingEventSink.UnicodePromptResponseSent += e => captured = e;

            var expected = new UnicodePromptResponseSentArgs("hello", "ENU", true);
            OutgoingEventSink.RaiseUnicodePromptResponseSent(expected);

            captured.Should().Be(expected);
        }

        // ---- ClearAll covers new categories ----

        [Fact]
        public void ClearAll_Should_Remove_All_New_Subscribers()
        {
            int count = 0;
            OutgoingEventSink.EmoteActionSent += _ => count++;
            OutgoingEventSink.PartyInviteRequestSent += _ => count++;
            OutgoingEventSink.TradeResponseSent += _ => count++;
            OutgoingEventSink.GumpResponseSent += _ => count++;
            OutgoingEventSink.AsciiPromptResponseSent += _ => count++;

            OutgoingEventSink.ClearAll();

            OutgoingEventSink.RaiseEmoteActionSent(new EmoteActionSentArgs("x"));
            OutgoingEventSink.RaisePartyInviteRequestSent(new PartyInviteRequestSentArgs());
            OutgoingEventSink.RaiseTradeResponseSent(new TradeResponseSentArgs(0u, 0, false));
            OutgoingEventSink.RaiseGumpResponseSent(new GumpResponseSentArgs(0u, 0u, 0, Array.Empty<uint>(), Array.Empty<Tuple<ushort, string>>()));
            OutgoingEventSink.RaiseAsciiPromptResponseSent(new AsciiPromptResponseSentArgs("", false));

            count.Should().Be(0);
        }
    }
}
