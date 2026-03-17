using ClassicUO.Client.Tests;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Client.Tests.Game.Managers
{
    public class CorpseManagerTests
    {
        private readonly World _world;

        public CorpseManagerTests()
        {
            _world = TestHelpers.CreateTestWorld();
        }

        [Fact]
        public void Add_ThenExists_ReturnsTrue()
        {
            _world.CorpseManager.Add(1u, 2u, Direction.North, false);

            _world.CorpseManager.Exists(1u, 0u).Should().BeTrue();
        }

        [Fact]
        public void Exists_ByObjectSerial_ReturnsTrue()
        {
            _world.CorpseManager.Add(1u, 2u, Direction.North, false);

            _world.CorpseManager.Exists(0u, 2u).Should().BeTrue();
        }

        [Fact]
        public void Add_Duplicate_DoesNotAddTwice()
        {
            _world.CorpseManager.Add(1u, 2u, Direction.North, false);
            _world.CorpseManager.Add(1u, 3u, Direction.South, true);

            // After removing corpse serial 1, the second entry (obj=3) should not exist
            // because the duplicate was never added.
            _world.CorpseManager.Exists(0u, 3u).Should().BeFalse();
        }

        [Fact]
        public void Remove_ByCorpseSerial()
        {
            _world.CorpseManager.Add(10u, 20u, Direction.East, false);

            // Remove with corpse=0 so it doesn't try to look up the item
            // Actually, Remove matches on corpse OR obj serial
            _world.CorpseManager.Remove(10u, 0u);

            _world.CorpseManager.Exists(10u, 20u).Should().BeFalse();
        }

        [Fact]
        public void Exists_ReturnsFalse_AfterRemove()
        {
            _world.CorpseManager.Add(5u, 6u, Direction.West, false);
            _world.CorpseManager.Remove(0u, 6u);

            _world.CorpseManager.Exists(5u, 6u).Should().BeFalse();
        }

        [Fact]
        public void Clear_EmptiesAll()
        {
            _world.CorpseManager.Add(1u, 2u, Direction.North, false);
            _world.CorpseManager.Add(3u, 4u, Direction.South, true);

            _world.CorpseManager.Clear();

            _world.CorpseManager.Exists(1u, 2u).Should().BeFalse();
            _world.CorpseManager.Exists(3u, 4u).Should().BeFalse();
        }

        [Fact]
        public void GetCorpseObject_ReturnsNull_WhenNoMatch()
        {
            // No corpses added, Items dict is empty
            _world.CorpseManager.GetCorpseObject(999u).Should().BeNull();
        }

        [Fact]
        public void GetCorpseObject_ReturnsNull_WhenCorpseExistsButItemNotInWorld()
        {
            _world.CorpseManager.Add(100u, 200u, Direction.North, false);

            // The corpse serial 100 is not in World.Items, so should return null
            _world.CorpseManager.GetCorpseObject(200u).Should().BeNull();
        }
    }
}
