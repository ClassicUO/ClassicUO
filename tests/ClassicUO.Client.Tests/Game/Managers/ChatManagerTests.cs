using ClassicUO.Client.Tests;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Client.Tests.Game.Managers
{
    public class ChatManagerTests
    {
        private readonly World _world;

        public ChatManagerTests()
        {
            _world = TestHelpers.CreateTestWorld();
        }

        [Fact]
        public void AddChannel_AddsToDictionary()
        {
            _world.ChatManager.AddChannel("General", false);

            _world.ChatManager.Channels.Should().ContainKey("General");
            _world.ChatManager.Channels["General"].Name.Should().Be("General");
            _world.ChatManager.Channels["General"].HasPassword.Should().BeFalse();
        }

        [Fact]
        public void AddChannel_Duplicate_DoesNotThrow()
        {
            _world.ChatManager.AddChannel("General", false);

            var act = () => _world.ChatManager.AddChannel("General", true);

            act.Should().NotThrow();
            // Original entry should remain unchanged
            _world.ChatManager.Channels.Should().HaveCount(1);
        }

        [Fact]
        public void RemoveChannel_RemovesFromDictionary()
        {
            _world.ChatManager.AddChannel("Trade", false);
            _world.ChatManager.RemoveChannel("Trade");

            _world.ChatManager.Channels.Should().NotContainKey("Trade");
        }

        [Fact]
        public void RemoveChannel_NonExistent_DoesNotThrow()
        {
            var act = () => _world.ChatManager.RemoveChannel("NonExistent");

            act.Should().NotThrow();
        }

        [Fact]
        public void Clear_EmptiesChannels()
        {
            _world.ChatManager.AddChannel("General", false);
            _world.ChatManager.AddChannel("Trade", true);

            _world.ChatManager.Clear();

            _world.ChatManager.Channels.Should().BeEmpty();
        }

        [Fact]
        public void GetMessage_ReturnsCorrectMessage_ForValidIndex()
        {
            // Index 0 should return a non-empty resource string
            var message = ChatManager.GetMessage(0);

            message.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void GetMessage_ReturnsEmpty_ForOutOfRangeIndex()
        {
            var message = ChatManager.GetMessage(9999);

            message.Should().BeEmpty();
        }

        [Fact]
        public void ChatIsEnabled_GetSet()
        {
            _world.ChatManager.ChatIsEnabled = ChatStatus.Enabled;

            _world.ChatManager.ChatIsEnabled.Should().Be(ChatStatus.Enabled);
        }

        [Fact]
        public void CurrentChannelName_GetSet()
        {
            _world.ChatManager.CurrentChannelName = "MyChannel";

            _world.ChatManager.CurrentChannelName.Should().Be("MyChannel");
        }

        [Fact]
        public void CurrentChannelName_DefaultsToEmpty()
        {
            _world.ChatManager.CurrentChannelName.Should().BeEmpty();
        }
    }
}
