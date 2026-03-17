using System;
using ClassicUO.Utility;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Utility.Tests
{
    public class EasingsTests
    {
        private const float Tolerance = 0.01f;

        // Linear

        [Theory]
        [InlineData(0f)]
        [InlineData(0.25f)]
        [InlineData(0.5f)]
        [InlineData(0.75f)]
        [InlineData(1.0f)]
        public void Linear_ReturnsInputValue(float t)
        {
            Easings.Linear(t).Should().BeApproximately(t, 0.0001f);
        }

        // Boundary tests: f(0) ~ 0 and f(1) ~ 1

        [Fact]
        public void InQuad_BoundaryValues()
        {
            Easings.InQuad(0f).Should().BeApproximately(0f, Tolerance);
            Easings.InQuad(1f).Should().BeApproximately(1f, Tolerance);
        }

        [Fact]
        public void OutQuad_BoundaryValues()
        {
            Easings.OutQuad(0f).Should().BeApproximately(0f, Tolerance);
            Easings.OutQuad(1f).Should().BeApproximately(1f, Tolerance);
        }

        [Fact]
        public void InOutQuad_BoundaryValues()
        {
            Easings.InOutQuad(0f).Should().BeApproximately(0f, Tolerance);
            Easings.InOutQuad(1f).Should().BeApproximately(1f, Tolerance);
            Easings.InOutQuad(0.5f).Should().BeApproximately(0.5f, Tolerance);
        }

        [Fact]
        public void InCubic_BoundaryValues()
        {
            Easings.InCubic(0f).Should().BeApproximately(0f, Tolerance);
            Easings.InCubic(1f).Should().BeApproximately(1f, Tolerance);
        }

        [Fact]
        public void OutCubic_BoundaryValues()
        {
            Easings.OutCubic(0f).Should().BeApproximately(0f, Tolerance);
            Easings.OutCubic(1f).Should().BeApproximately(1f, Tolerance);
        }

        [Fact]
        public void InOutCubic_BoundaryValues()
        {
            Easings.InOutCubic(0f).Should().BeApproximately(0f, Tolerance);
            Easings.InOutCubic(1f).Should().BeApproximately(1f, Tolerance);
            Easings.InOutCubic(0.5f).Should().BeApproximately(0.5f, Tolerance);
        }

        [Fact]
        public void InQuart_BoundaryValues()
        {
            Easings.InQuart(0f).Should().BeApproximately(0f, Tolerance);
            Easings.InQuart(1f).Should().BeApproximately(1f, Tolerance);
        }

        [Fact]
        public void OutQuart_BoundaryValues()
        {
            Easings.OutQuart(0f).Should().BeApproximately(0f, Tolerance);
            Easings.OutQuart(1f).Should().BeApproximately(1f, Tolerance);
        }

        [Fact]
        public void InOutQuart_BoundaryValues()
        {
            Easings.InOutQuart(0f).Should().BeApproximately(0f, Tolerance);
            Easings.InOutQuart(1f).Should().BeApproximately(1f, Tolerance);
            Easings.InOutQuart(0.5f).Should().BeApproximately(0.5f, Tolerance);
        }

        [Fact]
        public void InQuint_BoundaryValues()
        {
            Easings.InQuint(0f).Should().BeApproximately(0f, Tolerance);
            Easings.InQuint(1f).Should().BeApproximately(1f, Tolerance);
        }

        [Fact]
        public void OutQuint_BoundaryValues()
        {
            Easings.OutQuint(0f).Should().BeApproximately(0f, Tolerance);
            Easings.OutQuint(1f).Should().BeApproximately(1f, Tolerance);
        }

        [Fact]
        public void InOutQuint_BoundaryValues()
        {
            Easings.InOutQuint(0f).Should().BeApproximately(0f, Tolerance);
            Easings.InOutQuint(1f).Should().BeApproximately(1f, Tolerance);
            Easings.InOutQuint(0.5f).Should().BeApproximately(0.5f, Tolerance);
        }

        // Sine: InSine(0) = -cos(0) = -1, not 0. The implementation returns -cos(t*PI/2).
        // InSine(0) = -cos(0) = -1.0, InSine(1) = -cos(PI/2) = 0. This is NOT a standard [0,1]->[0,1] easing.

        [Fact]
        public void InSine_AtZero()
        {
            // InSine(0) = (float)-Math.Cos(0) = -1
            Easings.InSine(0f).Should().BeApproximately(-1f, Tolerance);
        }

        [Fact]
        public void InSine_AtOne()
        {
            // InSine(1) = (float)-Math.Cos(PI/2) ≈ 0
            Easings.InSine(1f).Should().BeApproximately(0f, Tolerance);
        }

        [Fact]
        public void OutSine_BoundaryValues()
        {
            Easings.OutSine(0f).Should().BeApproximately(0f, Tolerance);
            Easings.OutSine(1f).Should().BeApproximately(1f, Tolerance);
        }

        [Fact]
        public void InOutSine_BoundaryValues()
        {
            Easings.InOutSine(0f).Should().BeApproximately(0f, Tolerance);
            Easings.InOutSine(1f).Should().BeApproximately(1f, Tolerance);
            Easings.InOutSine(0.5f).Should().BeApproximately(0.5f, Tolerance);
        }

        // Expo: InExpo(0) = 2^(-10) ≈ 0.000977, not exactly 0

        [Fact]
        public void InExpo_AtZero()
        {
            // InExpo(0) = 2^(10*(0-1)) = 2^(-10) ≈ 0.000977
            float result = Easings.InExpo(0f);
            result.Should().BeApproximately(0.000977f, 0.001f);
        }

        [Fact]
        public void InExpo_AtOne()
        {
            Easings.InExpo(1f).Should().BeApproximately(1f, Tolerance);
        }

        [Fact]
        public void OutExpo_BoundaryValues()
        {
            Easings.OutExpo(0f).Should().BeApproximately(0f, Tolerance);
            Easings.OutExpo(1f).Should().BeApproximately(1f, Tolerance);
        }

        [Fact]
        public void InOutExpo_BoundaryValues()
        {
            Easings.InOutExpo(0f).Should().BeApproximately(0f, 0.002f);
            Easings.InOutExpo(1f).Should().BeApproximately(1f, 0.002f);
            Easings.InOutExpo(0.5f).Should().BeApproximately(0.5f, Tolerance);
        }

        [Fact]
        public void InCirc_BoundaryValues()
        {
            Easings.InCirc(0f).Should().BeApproximately(0f, Tolerance);
            Easings.InCirc(1f).Should().BeApproximately(1f, Tolerance);
        }

        [Fact]
        public void OutCirc_BoundaryValues()
        {
            Easings.OutCirc(0f).Should().BeApproximately(0f, Tolerance);
            Easings.OutCirc(1f).Should().BeApproximately(1f, Tolerance);
        }

        [Fact]
        public void InOutCirc_BoundaryValues()
        {
            Easings.InOutCirc(0f).Should().BeApproximately(0f, Tolerance);
            Easings.InOutCirc(1f).Should().BeApproximately(1f, Tolerance);
            Easings.InOutCirc(0.5f).Should().BeApproximately(0.5f, Tolerance);
        }

        [Fact]
        public void InElastic_BoundaryValues()
        {
            // InElastic(0) = 1 - OutElastic(1) = 1 - 1 = 0
            Easings.InElastic(0f).Should().BeApproximately(0f, Tolerance);
            Easings.InElastic(1f).Should().BeApproximately(1f, Tolerance);
        }

        [Fact]
        public void OutElastic_BoundaryValues()
        {
            // OutElastic(0) = 2^0 * sin((0 - 0.075) * 2PI/0.3) + 1 = 1 * sin(-PI/2) + 1 = -1 + 1 = 0
            Easings.OutElastic(0f).Should().BeApproximately(0f, Tolerance);
            Easings.OutElastic(1f).Should().BeApproximately(1f, Tolerance);
        }

        [Fact]
        public void InOutElastic_BoundaryValues()
        {
            Easings.InOutElastic(0f).Should().BeApproximately(0f, Tolerance);
            Easings.InOutElastic(1f).Should().BeApproximately(1f, Tolerance);
            Easings.InOutElastic(0.5f).Should().BeApproximately(0.5f, Tolerance);
        }

        // Back easing overshoots, but boundaries should still be 0 and 1

        [Fact]
        public void InBack_BoundaryValues()
        {
            Easings.InBack(0f).Should().BeApproximately(0f, Tolerance);
            Easings.InBack(1f).Should().BeApproximately(1f, Tolerance);
        }

        [Fact]
        public void OutBack_BoundaryValues()
        {
            Easings.OutBack(0f).Should().BeApproximately(0f, Tolerance);
            Easings.OutBack(1f).Should().BeApproximately(1f, Tolerance);
        }

        [Fact]
        public void InOutBack_BoundaryValues()
        {
            Easings.InOutBack(0f).Should().BeApproximately(0f, Tolerance);
            Easings.InOutBack(1f).Should().BeApproximately(1f, Tolerance);
            Easings.InOutBack(0.5f).Should().BeApproximately(0.5f, Tolerance);
        }

        [Fact]
        public void InBounce_BoundaryValues()
        {
            Easings.InBounce(0f).Should().BeApproximately(0f, Tolerance);
            Easings.InBounce(1f).Should().BeApproximately(1f, Tolerance);
        }

        [Fact]
        public void OutBounce_BoundaryValues()
        {
            Easings.OutBounce(0f).Should().BeApproximately(0f, Tolerance);
            Easings.OutBounce(1f).Should().BeApproximately(1f, Tolerance);
        }

        [Fact]
        public void InOutBounce_BoundaryValues()
        {
            Easings.InOutBounce(0f).Should().BeApproximately(0f, Tolerance);
            Easings.InOutBounce(1f).Should().BeApproximately(1f, Tolerance);
            Easings.InOutBounce(0.5f).Should().BeApproximately(0.5f, Tolerance);
        }

        // Theory-based tests for boundary values

        [Theory]
        [InlineData(0f, 0f)]
        [InlineData(0.25f, 0.0625f)]
        [InlineData(0.5f, 0.25f)]
        [InlineData(0.75f, 0.5625f)]
        [InlineData(1.0f, 1.0f)]
        public void InQuad_ReturnsExpectedValues(float t, float expected)
        {
            Easings.InQuad(t).Should().BeApproximately(expected, 0.0001f);
        }

        [Theory]
        [InlineData(0f, 0f)]
        [InlineData(0.25f, 0.015625f)]
        [InlineData(0.5f, 0.125f)]
        [InlineData(0.75f, 0.421875f)]
        [InlineData(1.0f, 1.0f)]
        public void InCubic_ReturnsExpectedValues(float t, float expected)
        {
            Easings.InCubic(t).Should().BeApproximately(expected, 0.0001f);
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(0.25f)]
        [InlineData(0.5f)]
        [InlineData(0.75f)]
        [InlineData(1.0f)]
        public void OutBounce_ReturnsNonNegative(float t)
        {
            Easings.OutBounce(t).Should().BeGreaterOrEqualTo(0f);
        }

        // Verify InBack goes negative (overshoots below 0)

        [Fact]
        public void InBack_MidValueGoesNegative()
        {
            // InBack should dip below 0 for small positive t values
            Easings.InBack(0.1f).Should().BeLessThan(0f);
        }

        // Verify OutElastic overshoots above 1

        [Fact]
        public void OutElastic_MidValueOvershoots()
        {
            // OutElastic typically overshoots above 1 at certain points
            float maxVal = 0f;
            for (float t = 0f; t <= 1f; t += 0.01f)
            {
                float val = Easings.OutElastic(t);
                if (val > maxVal) maxVal = val;
            }
            maxVal.Should().BeGreaterThan(1f);
        }
    }
}
