using ClassicUO.Client.Tests;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Client.Tests.Game.Managers
{
    public class TargetManagerTests
    {
        private readonly World _world;

        public TargetManagerTests()
        {
            _world = TestHelpers.CreateTestWorld();
        }

        [Fact]
        public void IsTargeting_InitiallyFalse()
        {
            _world.TargetManager.IsTargeting.Should().BeFalse();
        }

        [Fact]
        public void TargetingState_InitiallyInvalid()
        {
            _world.TargetManager.TargetingState.Should().Be(CursorTarget.Invalid);
        }

        [Fact]
        public void SetTargeting_SetsIsTargetingTrue()
        {
            _world.TargetManager.SetTargeting(CursorTarget.Object, 1u, TargetType.Neutral);

            _world.TargetManager.IsTargeting.Should().BeTrue();
        }

        [Fact]
        public void SetTargeting_SetsTargetingState()
        {
            _world.TargetManager.SetTargeting(CursorTarget.Object, 1u, TargetType.Neutral);

            _world.TargetManager.TargetingState.Should().Be(CursorTarget.Object);
        }

        [Fact]
        public void SetTargeting_SetsTargetingType()
        {
            _world.TargetManager.SetTargeting(CursorTarget.Object, 1u, TargetType.Harmful);

            _world.TargetManager.TargetingType.Should().Be(TargetType.Harmful);
        }

        [Fact]
        public void SetTargeting_WithInvalid_DoesNotSetIsTargeting()
        {
            _world.TargetManager.SetTargeting(CursorTarget.Invalid, 1u, TargetType.Neutral);

            _world.TargetManager.IsTargeting.Should().BeFalse();
        }

        [Fact]
        public void SetTargeting_WithCancelType_SetsIsTargetingFalse()
        {
            _world.TargetManager.SetTargeting(CursorTarget.Object, 1u, TargetType.Cancel);

            _world.TargetManager.IsTargeting.Should().BeFalse();
        }

        [Fact]
        public void Reset_ClearsState()
        {
            _world.TargetManager.SetTargeting(CursorTarget.Object, 1u, TargetType.Neutral);

            _world.TargetManager.Reset();

            _world.TargetManager.IsTargeting.Should().BeFalse();
            _world.TargetManager.TargetingType.Should().Be(TargetType.Neutral);
            _world.TargetManager.MultiTargetInfo.Should().BeNull();
        }

        [Fact]
        public void LastAttack_GetSet()
        {
            _world.TargetManager.LastAttack.Should().Be(0u);

            _world.TargetManager.LastAttack = 12345u;
            _world.TargetManager.LastAttack.Should().Be(12345u);
        }

        [Fact]
        public void SelectedTarget_GetSet()
        {
            _world.TargetManager.SelectedTarget.Should().Be(0u);

            _world.TargetManager.SelectedTarget = 67890u;
            _world.TargetManager.SelectedTarget.Should().Be(67890u);
        }

        [Fact]
        public void NewTargetSystemSerial_GetSet()
        {
            _world.TargetManager.NewTargetSystemSerial.Should().Be(0u);

            _world.TargetManager.NewTargetSystemSerial = 111u;
            _world.TargetManager.NewTargetSystemSerial.Should().Be(111u);
        }

        [Fact]
        public void LastTargetInfo_IsNotNull()
        {
            _world.TargetManager.LastTargetInfo.Should().NotBeNull();
        }

        [Fact]
        public void MultiTargetInfo_InitiallyNull()
        {
            _world.TargetManager.MultiTargetInfo.Should().BeNull();
        }

        [Fact]
        public void SetTargetingMulti_SetsMultiTargetInfo()
        {
            _world.TargetManager.SetTargetingMulti(1u, 100, 10, 20, 30, 5);

            _world.TargetManager.MultiTargetInfo.Should().NotBeNull();
            _world.TargetManager.MultiTargetInfo.Model.Should().Be(100);
            _world.TargetManager.MultiTargetInfo.XOff.Should().Be(10);
            _world.TargetManager.MultiTargetInfo.YOff.Should().Be(20);
            _world.TargetManager.MultiTargetInfo.ZOff.Should().Be(30);
            _world.TargetManager.MultiTargetInfo.Hue.Should().Be(5);
        }

        [Fact]
        public void SetTargetingMulti_SetsTargetingStateToMultiPlacement()
        {
            _world.TargetManager.SetTargetingMulti(1u, 100, 0, 0, 0, 0);

            _world.TargetManager.TargetingState.Should().Be(CursorTarget.MultiPlacement);
            _world.TargetManager.IsTargeting.Should().BeTrue();
        }
    }
}
