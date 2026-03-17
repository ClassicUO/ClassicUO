using ClassicUO.Client.Tests;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Client.Tests.Game.Managers
{
    public class AuraManagerTests
    {
        private readonly World _world;

        public AuraManagerTests()
        {
            _world = TestHelpers.CreateTestWorld();
        }

        [Fact]
        public void AuraManager_NotNull()
        {
            _world.AuraManager.Should().NotBeNull();
        }

        [Fact]
        public void AuraManager_IsAuraManager()
        {
            _world.AuraManager.Should().BeOfType<AuraManager>();
        }
    }
}
