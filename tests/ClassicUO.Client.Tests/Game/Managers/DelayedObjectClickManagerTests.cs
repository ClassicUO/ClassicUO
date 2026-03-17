using ClassicUO.Client.Tests;
using ClassicUO.Game;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Client.Tests.Game.Managers
{
    public class DelayedObjectClickManagerTests
    {
        private readonly World _world;

        public DelayedObjectClickManagerTests()
        {
            _world = TestHelpers.CreateTestWorld();
        }

        [Fact]
        public void IsEnabled_StartsFalse()
        {
            _world.DelayedObjectClickManager.IsEnabled.Should().BeFalse();
        }

        [Fact]
        public void Set_SetsAllProperties()
        {
            _world.DelayedObjectClickManager.Set(42u, 100, 200, 5000u);

            _world.DelayedObjectClickManager.Serial.Should().Be(42u);
            _world.DelayedObjectClickManager.X.Should().Be(100);
            _world.DelayedObjectClickManager.Y.Should().Be(200);
            _world.DelayedObjectClickManager.Timer.Should().Be(5000u);
            _world.DelayedObjectClickManager.IsEnabled.Should().BeTrue();
        }

        [Fact]
        public void Clear_ResetsIsEnabledToFalse()
        {
            _world.DelayedObjectClickManager.Set(42u, 100, 200, 5000u);

            _world.DelayedObjectClickManager.Clear();

            _world.DelayedObjectClickManager.IsEnabled.Should().BeFalse();
            _world.DelayedObjectClickManager.Serial.Should().Be(0xFFFF_FFFFu);
            _world.DelayedObjectClickManager.Timer.Should().Be(0u);
        }

        [Fact]
        public void ClearBySerial_ClearsMatchingSerial()
        {
            _world.DelayedObjectClickManager.Set(42u, 100, 200, 5000u);

            _world.DelayedObjectClickManager.Clear(42u);

            _world.DelayedObjectClickManager.IsEnabled.Should().BeFalse();
            _world.DelayedObjectClickManager.Serial.Should().Be(0u);
            _world.DelayedObjectClickManager.X.Should().Be(0);
            _world.DelayedObjectClickManager.Y.Should().Be(0);
            _world.DelayedObjectClickManager.LastMouseX.Should().Be(0);
            _world.DelayedObjectClickManager.LastMouseY.Should().Be(0);
        }

        [Fact]
        public void ClearBySerial_DoesNotClearNonMatchingSerial()
        {
            _world.DelayedObjectClickManager.Set(42u, 100, 200, 5000u);

            _world.DelayedObjectClickManager.Clear(99u);

            // Should remain unchanged
            _world.DelayedObjectClickManager.IsEnabled.Should().BeTrue();
            _world.DelayedObjectClickManager.Serial.Should().Be(42u);
            _world.DelayedObjectClickManager.X.Should().Be(100);
            _world.DelayedObjectClickManager.Y.Should().Be(200);
        }
    }
}
