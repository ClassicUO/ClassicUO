// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Game;
using ClassicUO.Game.Events;
using ClassicUO.Game.Managers;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Subscribers
{
    // Subscriber-side behavioral tests for ChatManager.
    //
    // We pass a null World — the handlers under test never dereference it
    // (only the skipped GameActions.Print paths do), and avoiding `new World()`
    // keeps us from re-wiring every other manager's EventSink subscriptions
    // (which collides with PacketHandlersTests running in parallel against the
    // same static EventSink). EventSink.ClearAll() in the ctor wipes any stale
    // subscriptions before each test.
    //
    // Skipped paths (documented inline below):
    //   - OnConferenceJoined / OnConferenceLeft / OnTextReceived /
    //     OnSystemMessage: each calls GameActions.Print, which routes through
    //     MessageManager.HandleMessage and dereferences ProfileManager.CurrentProfile
    //     (null in unit-test context). No safe pre-Print observable mutation
    //     to test in isolation.
    //   - OnUserAdded / OnUserRemoved / OnClearAllPlayers: handlers are
    //     placeholder no-ops with no observable state to assert. Skipped per
    //     the task brief.
    public class ChatManagerTests : IDisposable
    {
        public ChatManagerTests()
        {
            EventSink.ClearAll();
        }

        public void Dispose()
        {
            EventSink.ClearAll();
        }

        private static ChatManager NewManager()
        {
            // World wires Subscribe() centrally via RegisterListener now; in
            // unit tests we wire it ourselves so the EventSink handlers run.
            var manager = new ChatManager(world: null);
            manager.Subscribe();
            return manager;
        }

        // ---- OnConferenceCreated ----

        [Fact]
        public void ConferenceCreated_Adds_Channel_And_Sets_CurrentChannelName()
        {
            var manager = NewManager();

            EventSink.RaiseChatConferenceCreated(new ChatConferenceCreatedArgs("General", true));

            manager.CurrentChannelName.Should().Be("General");
            manager.Channels.Should().ContainKey("General");
            manager.Channels["General"].Name.Should().Be("General");
            manager.Channels["General"].HasPassword.Should().BeTrue();
        }

        [Fact]
        public void ConferenceCreated_Is_Idempotent_On_Same_Name()
        {
            var manager = NewManager();

            EventSink.RaiseChatConferenceCreated(new ChatConferenceCreatedArgs("Trade", false));
            var first = manager.Channels["Trade"];
            EventSink.RaiseChatConferenceCreated(new ChatConferenceCreatedArgs("Trade", false));

            manager.Channels.Count.Should().Be(1);
            manager.Channels["Trade"].Should().BeSameAs(first);
        }

        // ---- OnConferenceDestroyed ----

        [Fact]
        public void ConferenceDestroyed_Removes_Channel()
        {
            var manager = NewManager();
            manager.AddChannel("PvP", false);
            manager.Channels.Should().ContainKey("PvP");

            EventSink.RaiseChatConferenceDestroyed(new ChatConferenceDestroyedArgs("PvP"));

            manager.Channels.Should().NotContainKey("PvP");
        }

        [Fact]
        public void ConferenceDestroyed_Unknown_Channel_Is_NoOp()
        {
            var manager = NewManager();
            manager.AddChannel("Trade", false);

            EventSink.RaiseChatConferenceDestroyed(new ChatConferenceDestroyedArgs("DoesNotExist"));

            manager.Channels.Should().ContainKey("Trade");
            manager.Channels.Count.Should().Be(1);
        }

        // ---- OnUsernameRequest ----

        [Fact]
        public void UsernameRequest_Sets_ChatStatus_EnabledUserRequest()
        {
            var manager = NewManager();
            manager.ChatIsEnabled.Should().Be(ChatStatus.Disabled);

            EventSink.RaiseChatUsernameRequest(new ChatUsernameRequestArgs());

            manager.ChatIsEnabled.Should().Be(ChatStatus.EnabledUserRequest);
        }

        // ---- OnUsernameAccepted ----

        [Fact]
        public void UsernameAccepted_Sets_ChatStatus_Enabled()
        {
            var manager = NewManager();

            // Note: handler also sends a packet via NetClient.Socket which
            // no-ops when !IsConnected (the default in the unit-test process).
            EventSink.RaiseChatUsernameAccepted(new ChatUsernameAcceptedArgs("Player"));

            manager.ChatIsEnabled.Should().Be(ChatStatus.Enabled);
        }

        // ---- OnChatClosed ----

        [Fact]
        public void ChatClosed_Clears_Channels_And_Disables()
        {
            var manager = NewManager();
            manager.AddChannel("General", false);
            manager.AddChannel("Trade", true);
            manager.ChatIsEnabled = ChatStatus.Enabled;

            EventSink.RaiseChatClosed(new ChatClosedArgs());

            manager.Channels.Should().BeEmpty();
            manager.ChatIsEnabled.Should().Be(ChatStatus.Disabled);
        }

        // ---- Unsubscribe stops the wiring ----

        [Fact]
        public void Unsubscribe_Stops_Channels_From_Updating()
        {
            var manager = NewManager();
            manager.Unsubscribe();

            EventSink.RaiseChatConferenceCreated(new ChatConferenceCreatedArgs("Ghost", false));

            manager.Channels.Should().NotContainKey("Ghost");
            manager.CurrentChannelName.Should().Be(string.Empty);
        }
    }
}
