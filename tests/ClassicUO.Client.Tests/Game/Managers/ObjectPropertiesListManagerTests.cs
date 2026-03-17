using ClassicUO.Client.Tests;
using ClassicUO.Game;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Client.Tests.Game.Managers
{
    public class ObjectPropertiesListManagerTests
    {
        private readonly World _world;

        public ObjectPropertiesListManagerTests()
        {
            _world = TestHelpers.CreateTestWorld();
        }

        [Fact]
        public void Add_SetsNameAndData()
        {
            _world.OPL.Add(100u, 1u, "TestItem", "TestData", 1050045);

            _world.OPL.TryGetNameAndData(100u, out var name, out var data).Should().BeTrue();
            name.Should().Be("TestItem");
            data.Should().Be("TestData");
        }

        [Fact]
        public void Contains_ReturnsTrue_ForExistingSerial()
        {
            _world.OPL.Add(200u, 1u, "Item", "Data", 0);

            _world.OPL.Contains(200u).Should().BeTrue();
        }

        [Fact]
        public void Contains_ReturnsFalse_ForNonExistingSerial()
        {
            // Note: Contains calls PacketHandlers.AddMegaClilocRequest for missing serials,
            // but this is safe since it only adds to a static list.
            _world.OPL.Contains(999u).Should().BeFalse();
        }

        [Fact]
        public void IsRevisionEquals_MatchingRevision_ReturnsTrue()
        {
            _world.OPL.Add(300u, 50u, "Item", "Data", 0);

            _world.OPL.IsRevisionEquals(300u, 50u).Should().BeTrue();
        }

        [Fact]
        public void IsRevisionEquals_NonMatchingRevision_ReturnsFalse()
        {
            _world.OPL.Add(300u, 50u, "Item", "Data", 0);

            _world.OPL.IsRevisionEquals(300u, 99u).Should().BeFalse();
        }

        [Fact]
        public void IsRevisionEquals_MaskedRevision_ReturnsTrue()
        {
            // The mask is 0x40000000. If stored revision is 50, then
            // revision with mask applied: 50 | 0x40000000 should match
            // because (revision & ~0x40000000) == stored revision.
            _world.OPL.Add(400u, 50u, "Item", "Data", 0);

            uint maskedRevision = 50u | 0x40000000u;
            _world.OPL.IsRevisionEquals(400u, maskedRevision).Should().BeTrue();
        }

        [Fact]
        public void IsRevisionEquals_MissingSerial_ReturnsFalse()
        {
            _world.OPL.IsRevisionEquals(999u, 1u).Should().BeFalse();
        }

        [Fact]
        public void TryGetRevision_ReturnsTrue_WithCorrectRevision()
        {
            _world.OPL.Add(500u, 77u, "Item", "Data", 0);

            _world.OPL.TryGetRevision(500u, out var revision).Should().BeTrue();
            revision.Should().Be(77u);
        }

        [Fact]
        public void TryGetRevision_ReturnsFalse_ForMissing()
        {
            _world.OPL.TryGetRevision(999u, out var revision).Should().BeFalse();
            revision.Should().Be(0u);
        }

        [Fact]
        public void TryGetNameAndData_ReturnsTrue_WithCorrectData()
        {
            _world.OPL.Add(600u, 1u, "Sword", "Damage: 10", 1050045);

            _world.OPL.TryGetNameAndData(600u, out var name, out var data).Should().BeTrue();
            name.Should().Be("Sword");
            data.Should().Be("Damage: 10");
        }

        [Fact]
        public void TryGetNameAndData_ReturnsFalse_ForMissing()
        {
            _world.OPL.TryGetNameAndData(999u, out var name, out var data).Should().BeFalse();
            name.Should().BeNull();
            data.Should().BeNull();
        }

        [Fact]
        public void GetNameCliloc_ReturnsCorrectValue()
        {
            _world.OPL.Add(700u, 1u, "Item", "Data", 1050045);

            _world.OPL.GetNameCliloc(700u).Should().Be(1050045);
        }

        [Fact]
        public void GetNameCliloc_ReturnsZero_ForMissing()
        {
            _world.OPL.GetNameCliloc(999u).Should().Be(0);
        }

        [Fact]
        public void Remove_RemovesEntry()
        {
            _world.OPL.Add(800u, 1u, "Item", "Data", 0);
            _world.OPL.Remove(800u);

            _world.OPL.TryGetNameAndData(800u, out _, out _).Should().BeFalse();
        }

        [Fact]
        public void Clear_RemovesAll()
        {
            _world.OPL.Add(1u, 1u, "A", "D1", 0);
            _world.OPL.Add(2u, 1u, "B", "D2", 0);

            _world.OPL.Clear();

            _world.OPL.TryGetNameAndData(1u, out _, out _).Should().BeFalse();
            _world.OPL.TryGetNameAndData(2u, out _, out _).Should().BeFalse();
        }
    }
}
