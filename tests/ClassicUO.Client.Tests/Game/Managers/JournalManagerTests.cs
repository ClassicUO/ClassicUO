using ClassicUO.Client.Tests;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using FluentAssertions;
using System;
using Xunit;

namespace ClassicUO.Client.Tests.Game.Managers
{
    public class JournalManagerTests : IDisposable
    {
        private readonly World _world;

        public JournalManagerTests()
        {
            _world = TestHelpers.CreateTestWorld();
            // Clear static entries from any previous test runs
            JournalManager.Entries.Clear();
        }

        public void Dispose()
        {
            // Clean up static state
            JournalManager.Entries.Clear();
            _world.Journal.CloseWriter();
        }

        [Fact]
        public void Entries_IsNotNull()
        {
            JournalManager.Entries.Should().NotBeNull();
        }

        [Fact]
        public void Entries_IsEmpty_Initially()
        {
            JournalManager.Entries.Count.Should().Be(0);
        }

        [Fact]
        public void Add_AddsEntry_WithCorrectText()
        {
            _world.Journal.Add("Hello World", 0x35, "TestPlayer", null, TextType.CLIENT);

            JournalManager.Entries.Count.Should().Be(1);
            JournalManager.Entries[0].Text.Should().Be("Hello World");
        }

        [Fact]
        public void Add_AddsEntry_WithCorrectName()
        {
            _world.Journal.Add("Some text", 0x35, "PlayerName", null, TextType.CLIENT);

            JournalManager.Entries[0].Name.Should().Be("PlayerName");
        }

        [Fact]
        public void Add_AddsEntry_WithCorrectHue()
        {
            _world.Journal.Add("Some text", 0x44, "Player", null, TextType.CLIENT);

            JournalManager.Entries[0].Hue.Should().Be(0x44);
        }

        [Fact]
        public void Add_AddsEntry_WithCorrectTextType()
        {
            _world.Journal.Add("Some text", 0x35, "Player", null, TextType.GUILD_ALLY);

            JournalManager.Entries[0].TextType.Should().Be(TextType.GUILD_ALLY);
        }

        [Fact]
        public void Add_AddsEntry_WithMessageType()
        {
            _world.Journal.Add("Some text", 0x35, "Player", null, TextType.CLIENT, messageType: MessageType.System);

            JournalManager.Entries[0].MessageType.Should().Be(MessageType.System);
        }

        [Fact]
        public void Add_SetsTimeToNow()
        {
            var before = DateTime.Now;
            _world.Journal.Add("Some text", 0x35, "Player", null, TextType.CLIENT);
            var after = DateTime.Now;

            JournalManager.Entries[0].Time.Should().BeOnOrAfter(before);
            JournalManager.Entries[0].Time.Should().BeOnOrBefore(after);
        }

        [Fact]
        public void Add_MultipleEntries_AllAdded()
        {
            _world.Journal.Add("First", 0x35, "Player", null, TextType.CLIENT);
            _world.Journal.Add("Second", 0x36, "Player", null, TextType.CLIENT);
            _world.Journal.Add("Third", 0x37, "Player", null, TextType.CLIENT);

            JournalManager.Entries.Count.Should().Be(3);
        }

        [Fact]
        public void EntryAdded_EventFires()
        {
            JournalEntry received = null;
            _world.Journal.EntryAdded += (sender, entry) => received = entry;

            _world.Journal.Add("Event test", 0x35, "Player", null, TextType.CLIENT);

            received.Should().NotBeNull();
            received.Text.Should().Be("Event test");
        }
    }
}
