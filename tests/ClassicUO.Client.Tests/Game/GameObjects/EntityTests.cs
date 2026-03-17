using ClassicUO.Client.Tests;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Client.Tests.Game.GameObjects
{
    public class EntityTests
    {
        private readonly World _world;

        public EntityTests()
        {
            _world = TestHelpers.CreateTestWorld();
        }

        // Use Mobile as concrete Entity since Entity is abstract

        [Fact]
        public void Serial_IsSetCorrectly()
        {
            var mobile = Mobile.Create(_world, 0x1234);

            mobile.Serial.Should().Be(0x1234u);
        }

        [Fact]
        public void Name_DefaultIsNull()
        {
            var mobile = Mobile.Create(_world, 0x0001);

            mobile.Name.Should().BeNull();
        }

        [Fact]
        public void Name_CanBeSetAndRead()
        {
            var mobile = Mobile.Create(_world, 0x0001);
            mobile.Name = "Test Entity";

            mobile.Name.Should().Be("Test Entity");
        }

        [Fact]
        public void Hits_And_HitsMax_CanBeSetAndRead()
        {
            var mobile = Mobile.Create(_world, 0x0001);
            mobile.Hits = 75;
            mobile.HitsMax = 100;

            mobile.Hits.Should().Be(75);
            mobile.HitsMax.Should().Be(100);
        }

        [Fact]
        public void HitsPercentage_CanBeSetAndRead()
        {
            var mobile = Mobile.Create(_world, 0x0001);
            mobile.HitsPercentage = 50;

            mobile.HitsPercentage.Should().Be(50);
        }

        [Fact]
        public void Flags_CanBeSetAndRead()
        {
            var mobile = Mobile.Create(_world, 0x0001);
            mobile.Flags = Flags.Hidden | Flags.Frozen;

            mobile.Flags.Should().Be(Flags.Hidden | Flags.Frozen);
        }

        [Fact]
        public void IsHidden_ReturnsTrueWhenHiddenFlagSet()
        {
            var mobile = Mobile.Create(_world, 0x0001);
            mobile.Flags = Flags.Hidden;

            mobile.IsHidden.Should().BeTrue();
        }

        [Fact]
        public void IsHidden_ReturnsFalseWhenHiddenFlagNotSet()
        {
            var mobile = Mobile.Create(_world, 0x0001);
            mobile.Flags = Flags.Frozen;

            mobile.IsHidden.Should().BeFalse();
        }

        [Fact]
        public void IsHidden_ReturnsTrueWithMultipleFlags()
        {
            var mobile = Mobile.Create(_world, 0x0001);
            mobile.Flags = Flags.Hidden | Flags.Frozen | Flags.WarMode;

            mobile.IsHidden.Should().BeTrue();
        }

        [Fact]
        public void Direction_CanBeSetAndRead()
        {
            var mobile = Mobile.Create(_world, 0x0001);
            mobile.Direction = Direction.East;

            mobile.Direction.Should().Be(Direction.East);
        }

        [Fact]
        public void Direction_DefaultIsNorth()
        {
            var mobile = Mobile.Create(_world, 0x0001);

            mobile.Direction.Should().Be(Direction.North);
        }

        [Fact]
        public void FindItem_ReturnsNull_WhenNoItems()
        {
            var mobile = Mobile.Create(_world, 0x0001);

            mobile.FindItem(0x1234).Should().BeNull();
        }

        [Fact]
        public void FixHue_WithNonZeroHueUnder0x0BB8_SetsHue()
        {
            var mobile = Mobile.Create(_world, 0x0001);
            mobile.FixHue(0x0035);

            mobile.Hue.Should().Be(0x0035);
        }

        [Fact]
        public void FixHue_WithHueAtOrAbove0x0BB8_SetsHueTo1()
        {
            var mobile = Mobile.Create(_world, 0x0001);
            mobile.FixHue(0x0BB8);

            mobile.Hue.Should().Be(1);
        }

        [Fact]
        public void FixHue_WithZeroHue_SetsHueToZero()
        {
            var mobile = Mobile.Create(_world, 0x0001);
            mobile.FixHue(0);

            mobile.Hue.Should().Be(0);
        }

        [Fact]
        public void FixHue_WithHighBitSet_PreservesHighBit_WhenZeroColor()
        {
            var mobile = Mobile.Create(_world, 0x0001);
            // hue = 0x8000 -> fixedColor = (0x8000 & 0x3FFF) = 0 -> result = (0x8000 & 0x8000) = 0x8000
            mobile.FixHue(0x8000);

            mobile.Hue.Should().Be(0x8000);
        }

        [Fact]
        public void FixHue_WithHighBitSetAndNonZeroColor_PreservesHighBit()
        {
            var mobile = Mobile.Create(_world, 0x0001);
            // hue = 0x8035 -> fixedColor = (0x8035 & 0x3FFF) = 0x0035
            // 0x0035 < 0x0BB8 -> fixedColor |= (0x8035 & 0xC000) = 0x8000
            // result = 0x8035
            mobile.FixHue(0x8035);

            mobile.Hue.Should().Be(0x8035);
        }

        [Fact]
        public void Equals_SameSerial_ReturnsTrue()
        {
            var mobile1 = Mobile.Create(_world, 0x0001);
            var mobile2 = Mobile.Create(_world, 0x0001);

            mobile1.Equals(mobile2).Should().BeTrue();
        }

        [Fact]
        public void Equals_DifferentSerial_ReturnsFalse()
        {
            var mobile1 = Mobile.Create(_world, 0x0001);
            var mobile2 = Mobile.Create(_world, 0x0002);

            mobile1.Equals(mobile2).Should().BeFalse();
        }

        [Fact]
        public void GetHashCode_ReturnsSerialAsInt()
        {
            var mobile = Mobile.Create(_world, 0x1234);

            mobile.GetHashCode().Should().Be(0x1234);
        }

        [Fact]
        public void ImplicitUintConversion_ReturnsSerial()
        {
            var mobile = Mobile.Create(_world, 0x1234);
            uint serial = mobile;

            serial.Should().Be(0x1234u);
        }

        [Fact]
        public void ExecuteAnimation_DefaultIsTrue()
        {
            var mobile = Mobile.Create(_world, 0x0001);

            mobile.ExecuteAnimation.Should().BeTrue();
        }

        [Fact]
        public void FindItemByLayer_ReturnsNull_WhenNoItems()
        {
            var mobile = Mobile.Create(_world, 0x0001);

            mobile.FindItemByLayer(Layer.Ring).Should().BeNull();
        }

        [Fact]
        public void HitsRequest_DefaultIsNone()
        {
            var mobile = Mobile.Create(_world, 0x0001);

            mobile.HitsRequest.Should().Be(HitsRequestStatus.None);
        }
    }
}
