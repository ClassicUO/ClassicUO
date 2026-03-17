using ClassicUO.Client.Tests;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Client.Tests.Game.Managers
{
    public class NameOverHeadManagerTests
    {
        private readonly World _world;

        public NameOverHeadManagerTests()
        {
            _world = TestHelpers.CreateTestWorld();
        }

        [Fact]
        public void NameOverHeadManager_NotNull()
        {
            _world.NameOverHeadManager.Should().NotBeNull();
        }

        [Fact]
        public void IsAllowed_ReturnsFalse_ForNullEntity()
        {
            _world.NameOverHeadManager.IsAllowed(null).Should().BeFalse();
        }

        [Fact]
        public void TypeAllowed_FlagsEnum_HasExpectedValues()
        {
            NameOverheadTypeAllowed.None.Should().Be(0);
            NameOverheadTypeAllowed.Items.Should().Be((NameOverheadTypeAllowed)(1 << 0));
            NameOverheadTypeAllowed.Corpses.Should().Be((NameOverheadTypeAllowed)(1 << 1));
        }

        [Fact]
        public void TypeAllowed_AllMobiles_IsComposite()
        {
            var allMobiles = NameOverheadTypeAllowed.AllMobiles;
            allMobiles.HasFlag(NameOverheadTypeAllowed.Innocent).Should().BeTrue();
            allMobiles.HasFlag(NameOverheadTypeAllowed.Criminal).Should().BeTrue();
            allMobiles.HasFlag(NameOverheadTypeAllowed.Murderer).Should().BeTrue();
        }

        [Fact]
        public void TypeAllowed_All_IncludesEverything()
        {
            var all = NameOverheadTypeAllowed.All;
            all.HasFlag(NameOverheadTypeAllowed.Items).Should().BeTrue();
            all.HasFlag(NameOverheadTypeAllowed.Corpses).Should().BeTrue();
            all.HasFlag(NameOverheadTypeAllowed.AllMobiles).Should().BeTrue();
        }
    }
}
