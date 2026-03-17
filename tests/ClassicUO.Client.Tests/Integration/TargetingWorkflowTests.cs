using ClassicUO.Client.Tests;
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Client.Tests.Integration
{
    public class TargetingWorkflowTests
    {
        private readonly World _world;

        public TargetingWorkflowTests()
        {
            _world = TestHelpers.CreateTestWorld();
        }

        [Fact]
        public void SetTargeting_IsTargetingBecomesTrue()
        {
            _world.TargetManager.IsTargeting.Should().BeFalse();

            _world.TargetManager.SetTargeting(CursorTarget.Object, 1u, TargetType.Neutral);

            _world.TargetManager.IsTargeting.Should().BeTrue();
            _world.TargetManager.TargetingState.Should().Be(CursorTarget.Object);
            _world.TargetManager.TargetingType.Should().Be(TargetType.Neutral);
        }

        [Fact]
        public void SetTargeting_WithCancel_IsTargetingRemainsFalse()
        {
            _world.TargetManager.SetTargeting(CursorTarget.Object, 1u, TargetType.Cancel);

            _world.TargetManager.IsTargeting.Should().BeFalse();
        }

        [Fact]
        public void Reset_ClearsTargetingState()
        {
            _world.TargetManager.SetTargeting(CursorTarget.Object, 1u, TargetType.Harmful);
            _world.TargetManager.IsTargeting.Should().BeTrue();

            _world.TargetManager.Reset();

            _world.TargetManager.IsTargeting.Should().BeFalse();
            _world.TargetManager.TargetingType.Should().Be(TargetType.Neutral);
            _world.TargetManager.MultiTargetInfo.Should().BeNull();
        }

        [Fact]
        public void SetTargetingMulti_SetsMultiTargetInfoAndIsTargeting()
        {
            _world.TargetManager.SetTargetingMulti(1u, 0x1388, 10, 20, 30, 0x0035);

            _world.TargetManager.IsTargeting.Should().BeTrue();
            _world.TargetManager.TargetingState.Should().Be(CursorTarget.MultiPlacement);
            _world.TargetManager.MultiTargetInfo.Should().NotBeNull();
            _world.TargetManager.MultiTargetInfo.Model.Should().Be(0x1388);
            _world.TargetManager.MultiTargetInfo.XOff.Should().Be(10);
            _world.TargetManager.MultiTargetInfo.YOff.Should().Be(20);
            _world.TargetManager.MultiTargetInfo.ZOff.Should().Be(30);
            _world.TargetManager.MultiTargetInfo.Hue.Should().Be(0x0035);
        }

        [Fact]
        public void LastTargetInfo_SetEntity_TracksTargetSerial()
        {
            var mobile = _world.GetOrCreateMobile(0x0005);

            _world.TargetManager.LastTargetInfo.SetEntity(mobile.Serial);

            _world.TargetManager.LastTargetInfo.Serial.Should().Be(mobile.Serial);
            _world.TargetManager.LastTargetInfo.IsEntity.Should().BeTrue();
        }

        [Fact]
        public void LastTargetInfo_SetStatic_TracksStaticTarget()
        {
            _world.TargetManager.LastTargetInfo.SetStatic(0x1234, 100, 200, 10);

            _world.TargetManager.LastTargetInfo.IsStatic.Should().BeTrue();
            _world.TargetManager.LastTargetInfo.IsEntity.Should().BeFalse();
            _world.TargetManager.LastTargetInfo.Graphic.Should().Be(0x1234);
            _world.TargetManager.LastTargetInfo.X.Should().Be(100);
            _world.TargetManager.LastTargetInfo.Y.Should().Be(200);
            _world.TargetManager.LastTargetInfo.Z.Should().Be(10);
        }

        [Fact]
        public void LastTargetInfo_SetLand_TracksLandTarget()
        {
            _world.TargetManager.LastTargetInfo.SetLand(500, 600, 0);

            _world.TargetManager.LastTargetInfo.IsLand.Should().BeTrue();
            _world.TargetManager.LastTargetInfo.IsEntity.Should().BeFalse();
            _world.TargetManager.LastTargetInfo.IsStatic.Should().BeFalse();
            _world.TargetManager.LastTargetInfo.X.Should().Be(500);
            _world.TargetManager.LastTargetInfo.Y.Should().Be(600);
            _world.TargetManager.LastTargetInfo.Z.Should().Be(0);
        }

        [Fact]
        public void LastTargetInfo_Clear_ResetsAll()
        {
            _world.TargetManager.LastTargetInfo.SetEntity(0x0001);
            _world.TargetManager.LastTargetInfo.IsEntity.Should().BeTrue();

            _world.TargetManager.LastTargetInfo.Clear();

            _world.TargetManager.LastTargetInfo.IsEntity.Should().BeFalse();
            _world.TargetManager.LastTargetInfo.Serial.Should().Be(0u);
        }

        [Fact]
        public void TargetingWorkflow_SetThenReset_FullCycle()
        {
            // Start: no targeting
            _world.TargetManager.IsTargeting.Should().BeFalse();

            // Begin targeting
            _world.TargetManager.SetTargeting(CursorTarget.Object, 1u, TargetType.Beneficial);
            _world.TargetManager.IsTargeting.Should().BeTrue();
            _world.TargetManager.TargetingType.Should().Be(TargetType.Beneficial);

            // Record last target
            _world.TargetManager.LastTargetInfo.SetEntity(0x0010);
            _world.TargetManager.LastTargetInfo.Serial.Should().Be(0x0010u);

            // Reset targeting
            _world.TargetManager.Reset();
            _world.TargetManager.IsTargeting.Should().BeFalse();

            // Last target info persists after Reset (it's separate state)
            _world.TargetManager.LastTargetInfo.Serial.Should().Be(0x0010u);
        }

        [Fact]
        public void SelectedTarget_TracksLastSelectedSerial()
        {
            _world.TargetManager.SelectedTarget = 0x0001;
            _world.TargetManager.SelectedTarget.Should().Be(0x0001u);

            _world.TargetManager.SelectedTarget = 0x0002;
            _world.TargetManager.SelectedTarget.Should().Be(0x0002u);
        }

        [Fact]
        public void LastAttack_TracksAttackedSerial()
        {
            _world.TargetManager.LastAttack = 0x0005;
            _world.TargetManager.LastAttack.Should().Be(0x0005u);
        }

        [Fact]
        public void MultipleTargetingModes_CanSwitchBetween()
        {
            // Start with object targeting
            _world.TargetManager.SetTargeting(CursorTarget.Object, 1u, TargetType.Neutral);
            _world.TargetManager.TargetingState.Should().Be(CursorTarget.Object);

            // Switch to position targeting
            _world.TargetManager.SetTargeting(CursorTarget.Position, 2u, TargetType.Harmful);
            _world.TargetManager.TargetingState.Should().Be(CursorTarget.Position);
            _world.TargetManager.TargetingType.Should().Be(TargetType.Harmful);

            // Switch to multi placement
            _world.TargetManager.SetTargetingMulti(3u, 100, 0, 0, 0, 0);
            _world.TargetManager.TargetingState.Should().Be(CursorTarget.MultiPlacement);
        }
    }
}
