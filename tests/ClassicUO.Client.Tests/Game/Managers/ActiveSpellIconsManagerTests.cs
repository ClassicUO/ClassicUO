using ClassicUO.Client.Tests;
using ClassicUO.Game;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Client.Tests.Game.Managers
{
    public class ActiveSpellIconsManagerTests
    {
        private readonly World _world;

        public ActiveSpellIconsManagerTests()
        {
            _world = TestHelpers.CreateTestWorld();
        }

        [Fact]
        public void Add_ThenIsActive_ReturnsTrue()
        {
            _world.ActiveSpellIcons.Add(42);

            _world.ActiveSpellIcons.IsActive(42).Should().BeTrue();
        }

        [Fact]
        public void Add_DuplicateId_DoesNotThrow()
        {
            _world.ActiveSpellIcons.Add(10);

            var act = () => _world.ActiveSpellIcons.Add(10);

            act.Should().NotThrow();
            _world.ActiveSpellIcons.IsActive(10).Should().BeTrue();
        }

        [Fact]
        public void Remove_ThenIsActive_ReturnsFalse()
        {
            _world.ActiveSpellIcons.Add(5);
            _world.ActiveSpellIcons.Remove(5);

            _world.ActiveSpellIcons.IsActive(5).Should().BeFalse();
        }

        [Fact]
        public void Remove_NonExistentId_DoesNotThrow()
        {
            var act = () => _world.ActiveSpellIcons.Remove(999);

            act.Should().NotThrow();
        }

        [Fact]
        public void IsActive_WhenEmpty_ReturnsFalse()
        {
            _world.ActiveSpellIcons.IsActive(1).Should().BeFalse();
        }

        [Fact]
        public void Clear_RemovesAll()
        {
            _world.ActiveSpellIcons.Add(1);
            _world.ActiveSpellIcons.Add(2);
            _world.ActiveSpellIcons.Add(3);

            _world.ActiveSpellIcons.Clear();

            _world.ActiveSpellIcons.IsActive(1).Should().BeFalse();
            _world.ActiveSpellIcons.IsActive(2).Should().BeFalse();
            _world.ActiveSpellIcons.IsActive(3).Should().BeFalse();
        }
    }
}
