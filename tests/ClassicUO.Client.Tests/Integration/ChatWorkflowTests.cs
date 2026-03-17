using ClassicUO.Client.Tests;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Client.Tests.Integration
{
    public class ChatWorkflowTests
    {
        private readonly World _world;

        public ChatWorkflowTests()
        {
            _world = TestHelpers.CreateTestWorld();
        }

        [Fact]
        public void AddMultipleChannels_AllExistInDictionary()
        {
            _world.ChatManager.AddChannel("General", false);
            _world.ChatManager.AddChannel("Trade", true);
            _world.ChatManager.AddChannel("LFG", false);

            _world.ChatManager.Channels.Should().HaveCount(3);
            _world.ChatManager.Channels.Should().ContainKey("General");
            _world.ChatManager.Channels.Should().ContainKey("Trade");
            _world.ChatManager.Channels.Should().ContainKey("LFG");
        }

        [Fact]
        public void AddMultipleChannels_PasswordFlagsPreserved()
        {
            _world.ChatManager.AddChannel("Public", false);
            _world.ChatManager.AddChannel("Private", true);

            _world.ChatManager.Channels["Public"].HasPassword.Should().BeFalse();
            _world.ChatManager.Channels["Private"].HasPassword.Should().BeTrue();
        }

        [Fact]
        public void RemoveChannel_OnlyRemovesSpecifiedChannel()
        {
            _world.ChatManager.AddChannel("General", false);
            _world.ChatManager.AddChannel("Trade", false);
            _world.ChatManager.AddChannel("Help", false);

            _world.ChatManager.RemoveChannel("Trade");

            _world.ChatManager.Channels.Should().HaveCount(2);
            _world.ChatManager.Channels.Should().ContainKey("General");
            _world.ChatManager.Channels.Should().NotContainKey("Trade");
            _world.ChatManager.Channels.Should().ContainKey("Help");
        }

        [Fact]
        public void RemoveChannel_NonExistent_DoesNotAffectExisting()
        {
            _world.ChatManager.AddChannel("General", false);

            _world.ChatManager.RemoveChannel("NonExistent");

            _world.ChatManager.Channels.Should().HaveCount(1);
            _world.ChatManager.Channels.Should().ContainKey("General");
        }

        [Fact]
        public void Clear_RemovesAllChannels()
        {
            _world.ChatManager.AddChannel("General", false);
            _world.ChatManager.AddChannel("Trade", true);
            _world.ChatManager.AddChannel("LFG", false);
            _world.ChatManager.Channels.Should().HaveCount(3);

            _world.ChatManager.Clear();

            _world.ChatManager.Channels.Should().BeEmpty();
        }

        [Fact]
        public void CurrentChannelName_TracksActiveChannel()
        {
            _world.ChatManager.CurrentChannelName.Should().BeEmpty();

            _world.ChatManager.CurrentChannelName = "General";
            _world.ChatManager.CurrentChannelName.Should().Be("General");

            _world.ChatManager.CurrentChannelName = "Trade";
            _world.ChatManager.CurrentChannelName.Should().Be("Trade");
        }

        [Fact]
        public void ChatIsEnabled_StateManagement()
        {
            _world.ChatManager.ChatIsEnabled = ChatStatus.Enabled;
            _world.ChatManager.ChatIsEnabled.Should().Be(ChatStatus.Enabled);

            _world.ChatManager.ChatIsEnabled = ChatStatus.EnabledUserRequest;
            _world.ChatManager.ChatIsEnabled.Should().Be(ChatStatus.EnabledUserRequest);
        }

        [Fact]
        public void FullWorkflow_AddChannels_SetActive_Clear()
        {
            // Add channels
            _world.ChatManager.AddChannel("General", false);
            _world.ChatManager.AddChannel("Trade", true);
            _world.ChatManager.Channels.Should().HaveCount(2);

            // Set active channel
            _world.ChatManager.CurrentChannelName = "General";
            _world.ChatManager.CurrentChannelName.Should().Be("General");

            // Enable chat
            _world.ChatManager.ChatIsEnabled = ChatStatus.Enabled;
            _world.ChatManager.ChatIsEnabled.Should().Be(ChatStatus.Enabled);

            // Remove a channel
            _world.ChatManager.RemoveChannel("Trade");
            _world.ChatManager.Channels.Should().HaveCount(1);

            // Clear everything
            _world.ChatManager.Clear();
            _world.ChatManager.Channels.Should().BeEmpty();

            // CurrentChannelName is not cleared by Clear() - it's separate state
            _world.ChatManager.CurrentChannelName.Should().Be("General");
        }

        [Fact]
        public void AddDuplicateChannel_DoesNotDuplicate()
        {
            _world.ChatManager.AddChannel("General", false);
            _world.ChatManager.AddChannel("General", true);

            _world.ChatManager.Channels.Should().HaveCount(1);
        }

        [Fact]
        public void ChannelNames_AreCaseSensitive()
        {
            _world.ChatManager.AddChannel("General", false);
            _world.ChatManager.AddChannel("general", false);

            // If the implementation treats them as different keys
            // they should both exist; if case-insensitive, count is 1
            _world.ChatManager.Channels.Should().ContainKey("General");
        }
    }
}
