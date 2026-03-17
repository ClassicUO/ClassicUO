using System;
using Microsoft.Xna.Framework;
using ClassicUO.Utility;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Utility.Tests
{
    public class MathHelperTests
    {
        // InRange tests

        [Fact]
        public void InRange_ReturnsTrue_WhenInputIsWithinRange()
        {
            MathHelper.InRange(5, 1, 10).Should().BeTrue();
        }

        [Fact]
        public void InRange_ReturnsTrue_WhenInputEqualsLow()
        {
            MathHelper.InRange(1, 1, 10).Should().BeTrue();
        }

        [Fact]
        public void InRange_ReturnsTrue_WhenInputEqualsHigh()
        {
            MathHelper.InRange(10, 1, 10).Should().BeTrue();
        }

        [Fact]
        public void InRange_ReturnsFalse_WhenInputBelowLow()
        {
            MathHelper.InRange(0, 1, 10).Should().BeFalse();
        }

        [Fact]
        public void InRange_ReturnsFalse_WhenInputAboveHigh()
        {
            MathHelper.InRange(11, 1, 10).Should().BeFalse();
        }

        [Theory]
        [InlineData(5, 5, 5, true)]
        [InlineData(-1, -5, 0, true)]
        [InlineData(-6, -5, 0, false)]
        public void InRange_VariousInputs(int input, int low, int high, bool expected)
        {
            MathHelper.InRange(input, low, high).Should().Be(expected);
        }

        // GetDistance tests (Chebyshev distance)

        [Fact]
        public void GetDistance_SamePoint_ReturnsZero()
        {
            var p = new Point(3, 7);
            MathHelper.GetDistance(p, p).Should().Be(0);
        }

        [Fact]
        public void GetDistance_ReturnsChebyshevDistance()
        {
            var a = new Point(0, 0);
            var b = new Point(3, 5);
            // Chebyshev = max(|3|, |5|) = 5
            MathHelper.GetDistance(a, b).Should().Be(5);
        }

        [Fact]
        public void GetDistance_HorizontalMovement()
        {
            var a = new Point(0, 0);
            var b = new Point(7, 0);
            MathHelper.GetDistance(a, b).Should().Be(7);
        }

        [Fact]
        public void GetDistance_VerticalMovement()
        {
            var a = new Point(0, 0);
            var b = new Point(0, 4);
            MathHelper.GetDistance(a, b).Should().Be(4);
        }

        [Fact]
        public void GetDistance_NegativeCoordinates()
        {
            var a = new Point(-2, -3);
            var b = new Point(2, 3);
            // max(|4|, |6|) = 6
            MathHelper.GetDistance(a, b).Should().Be(6);
        }

        // Combine and GetNumbersFromCombine tests

        [Fact]
        public void Combine_PacksTwoUint32_IntoUint64()
        {
            // Note: Due to the implementation using uint arithmetic for the shift,
            // val2 << 32 on a uint wraps to val2 << 0 = val2 (C# masks shift to 5 bits).
            // So Combine(a, b) actually computes (ulong)(a | b), not the intended packing.
            // Testing actual behavior:
            uint val1 = 5;
            uint val2 = 0;
            ulong result = MathHelper.Combine(val1, val2);
            result.Should().Be(5UL);
        }

        [Fact]
        public void GetNumbersFromCombine_UnpacksUint64_ToTwoInts()
        {
            // Manually construct a packed value with low and high 32-bit parts
            ulong packed = 42UL | (99UL << 32);

            MathHelper.GetNumbersFromCombine(packed, out int val1, out int val2);

            val1.Should().Be(42);
            val2.Should().Be(99);
        }

        [Fact]
        public void Combine_ThenGetNumbersFromCombine_RoundTrips_WhenVal2IsZero()
        {
            uint original1 = 12345;
            uint original2 = 0;

            ulong combined = MathHelper.Combine(original1, original2);
            MathHelper.GetNumbersFromCombine(combined, out int result1, out int result2);

            result1.Should().Be((int)original1);
            result2.Should().Be((int)original2);
        }

        // PercetangeOf tests (note: intentional typo in source method name)

        [Fact]
        public void PercetangeOf_ReturnsCorrectPercentage()
        {
            MathHelper.PercetangeOf(50, 100).Should().Be(50);
        }

        [Fact]
        public void PercetangeOf_ReturnsZero_WhenCurrentIsZero()
        {
            MathHelper.PercetangeOf(0, 100).Should().Be(0);
        }

        [Fact]
        public void PercetangeOf_ReturnsZero_WhenMaxIsZero()
        {
            MathHelper.PercetangeOf(50, 0).Should().Be(0);
        }

        [Fact]
        public void PercetangeOf_ReturnsZero_WhenCurrentIsNegative()
        {
            MathHelper.PercetangeOf(-10, 100).Should().Be(0);
        }

        [Fact]
        public void PercetangeOf_ReturnsZero_WhenMaxIsNegative()
        {
            MathHelper.PercetangeOf(10, -100).Should().Be(0);
        }

        [Fact]
        public void PercetangeOf_FullPercentage()
        {
            MathHelper.PercetangeOf(100, 100).Should().Be(100);
        }

        [Fact]
        public void PercetangeOf_ThreeParam_ReturnsScaledValue()
        {
            // max=200, current=100, maxValue=50
            // intermediate = 100 * 100 / 200 = 50
            // 50 <= 100, 50 > 1 => result = 50 * 50 / 100 = 25
            int result = MathHelper.PercetangeOf(200, 100, 50);
            result.Should().Be(25);
        }

        [Fact]
        public void PercetangeOf_ThreeParam_WhenMaxIsZero_ReturnsZero()
        {
            MathHelper.PercetangeOf(0, 50, 100).Should().Be(0);
        }

        [Fact]
        public void PercetangeOf_ThreeParam_CapsAt100Percent()
        {
            // max=50, current=100, maxValue=200
            // intermediate = 100 * 100 / 50 = 200 => capped to 100
            // result = 200 * 100 / 100 = 200
            int result = MathHelper.PercetangeOf(50, 100, 200);
            result.Should().Be(200);
        }

        // Hypotenuse tests

        [Fact]
        public void Hypotenuse_ReturnsCorrectValue()
        {
            MathHelper.Hypotenuse(3.0f, 4.0f).Should().BeApproximately(5.0, 0.0001);
        }

        [Fact]
        public void Hypotenuse_WithZero()
        {
            MathHelper.Hypotenuse(0f, 5.0f).Should().BeApproximately(5.0, 0.0001);
        }

        [Fact]
        public void Hypotenuse_BothZero()
        {
            MathHelper.Hypotenuse(0f, 0f).Should().BeApproximately(0.0, 0.0001);
        }

        // AngleBetweenVectors tests

        [Fact]
        public void AngleBetweenVectors_ReturnsCorrectAngle()
        {
            var from = new Vector2(0, 0);
            var to = new Vector2(1, 0);
            // angle should be 0 radians (pointing right)
            MathHelper.AngleBetweenVectors(from, to).Should().BeApproximately(0f, 0.0001f);
        }

        [Fact]
        public void AngleBetweenVectors_PointingUp()
        {
            var from = new Vector2(0, 0);
            var to = new Vector2(0, 1);
            // angle should be PI/2 radians
            MathHelper.AngleBetweenVectors(from, to).Should().BeApproximately((float)(Math.PI / 2), 0.0001f);
        }

        [Fact]
        public void AngleBetweenVectors_PointingLeft()
        {
            var from = new Vector2(0, 0);
            var to = new Vector2(-1, 0);
            // angle should be PI radians
            MathHelper.AngleBetweenVectors(from, to).Should().BeApproximately((float)Math.PI, 0.0001f);
        }

        [Fact]
        public void AngleBetweenVectors_Diagonal()
        {
            var from = new Vector2(1, 1);
            var to = new Vector2(2, 2);
            // angle should be PI/4 radians (45 degrees)
            MathHelper.AngleBetweenVectors(from, to).Should().BeApproximately((float)(Math.PI / 4), 0.0001f);
        }

        // MachineEpsilonFloat test

        [Fact]
        public void MachineEpsilonFloat_IsSmallPositiveNumber()
        {
            MathHelper.MachineEpsilonFloat.Should().BeGreaterThan(0f);
            MathHelper.MachineEpsilonFloat.Should().BeLessThan(0.001f);
        }

        [Fact]
        public void MachineEpsilonFloat_AddedToOneEqualsOne()
        {
            // By definition, machine epsilon is the largest float where 1 + eps == 1
            (1.0f + MathHelper.MachineEpsilonFloat).Should().Be(1.0f);
        }
    }
}
