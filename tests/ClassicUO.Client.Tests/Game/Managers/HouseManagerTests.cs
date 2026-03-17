using ClassicUO.Client.Tests;
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Client.Tests.Game.Managers
{
    public class HouseManagerTests
    {
        private readonly World _world;

        public HouseManagerTests()
        {
            _world = TestHelpers.CreateTestWorld();
        }

        [Fact]
        public void Exists_ReturnsFalse_WhenEmpty()
        {
            _world.HouseManager.Exists(1u).Should().BeFalse();
        }

        [Fact]
        public void Add_ThenExists_ReturnsTrue()
        {
            var house = new House(_world, 1u, 0u, false);
            _world.HouseManager.Add(1u, house);

            _world.HouseManager.Exists(1u).Should().BeTrue();
        }

        [Fact]
        public void TryGetHouse_ReturnsTrue_WithCorrectHouse()
        {
            var house = new House(_world, 42u, 100u, true);
            _world.HouseManager.Add(42u, house);

            _world.HouseManager.TryGetHouse(42u, out var result).Should().BeTrue();
            result.Should().BeSameAs(house);
            result.Serial.Should().Be(42u);
            result.Revision.Should().Be(100u);
        }

        [Fact]
        public void TryGetHouse_ReturnsFalse_WhenNotFound()
        {
            _world.HouseManager.TryGetHouse(999u, out var result).Should().BeFalse();
            result.Should().BeNull();
        }

        [Fact]
        public void Remove_RemovesHouse()
        {
            var house = new House(_world, 10u, 0u, false);
            _world.HouseManager.Add(10u, house);

            _world.HouseManager.Remove(10u);

            _world.HouseManager.Exists(10u).Should().BeFalse();
        }

        [Fact]
        public void Clear_RemovesAll()
        {
            _world.HouseManager.Add(1u, new House(_world, 1u, 0u, false));
            _world.HouseManager.Add(2u, new House(_world, 2u, 0u, false));
            _world.HouseManager.Add(3u, new House(_world, 3u, 0u, false));

            _world.HouseManager.Clear();

            _world.HouseManager.Exists(1u).Should().BeFalse();
            _world.HouseManager.Exists(2u).Should().BeFalse();
            _world.HouseManager.Exists(3u).Should().BeFalse();
        }

        [Fact]
        public void Houses_ReturnsCollection()
        {
            _world.HouseManager.Houses.Should().NotBeNull();
            _world.HouseManager.Houses.Count.Should().Be(0);

            _world.HouseManager.Add(1u, new House(_world, 1u, 0u, false));
            _world.HouseManager.Add(2u, new House(_world, 2u, 0u, false));

            _world.HouseManager.Houses.Count.Should().Be(2);
        }
    }
}
