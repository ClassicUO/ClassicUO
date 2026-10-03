using System;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Utility.Easings
{
    public class BoundaryValues
    {
        private const float Exact = 0f;
        private const float Precision = 1e-6f;
        private const float LoosePrecision = 0.01f;

        [Theory]
        [InlineData(0f)]
        [InlineData(0.25f)]
        [InlineData(0.5f)]
        [InlineData(0.75f)]
        [InlineData(1f)]
        public void Linear_Should_Return_Identity(float t)
        {
            ClassicUO.Utility.Easings.Linear(t)
                .Should()
                .Be(t);
        }

        [Theory]
        [InlineData("InQuad", 0f, Exact)]
        [InlineData("OutQuad", 0f, Exact)]
        [InlineData("InCubic", 0f, Exact)]
        [InlineData("OutCubic", 0f, Exact)]
        [InlineData("InQuart", 0f, Exact)]
        [InlineData("OutQuart", 0f, Exact)]
        [InlineData("InQuint", 0f, Exact)]
        [InlineData("OutQuint", 0f, Exact)]
        [InlineData("InSine", -1f, Precision)]
        [InlineData("OutSine", 0f, Precision)]
        [InlineData("InExpo", 0f, LoosePrecision)]
        [InlineData("OutExpo", 0f, Exact)]
        [InlineData("InCirc", 0f, Precision)]
        [InlineData("OutCirc", 0f, Precision)]
        [InlineData("InElastic", 0f, LoosePrecision)]
        [InlineData("OutElastic", 0f, Precision)]
        [InlineData("InBack", 0f, Exact)]
        [InlineData("OutBack", 0f, Exact)]
        [InlineData("InBounce", 0f, Exact)]
        [InlineData("OutBounce", 0f, Exact)]
        public void Easing_At_Zero_Should_Return_Expected(string name, float expected, float precision)
        {
            var result = GetEasing(name)(0f);

            if (precision == Exact)
                result.Should().Be(expected);
            else
                result.Should().BeApproximately(expected, precision);
        }

        [Theory]
        [InlineData("InQuad", 1f, Exact)]
        [InlineData("OutQuad", 1f, Exact)]
        [InlineData("InCubic", 1f, Exact)]
        [InlineData("OutCubic", 1f, Exact)]
        [InlineData("InQuart", 1f, Exact)]
        [InlineData("OutQuart", 1f, Exact)]
        [InlineData("InQuint", 1f, Exact)]
        [InlineData("OutQuint", 1f, Exact)]
        [InlineData("InSine", 0f, Precision)]
        [InlineData("OutSine", 1f, Precision)]
        [InlineData("InExpo", 1f, Exact)]
        [InlineData("OutExpo", 1f, LoosePrecision)]
        [InlineData("InCirc", 1f, Precision)]
        [InlineData("OutCirc", 1f, Precision)]
        [InlineData("InElastic", 1f, Precision)]
        [InlineData("OutElastic", 1f, LoosePrecision)]
        [InlineData("InBack", 1f, Precision)]
        [InlineData("OutBack", 1f, Exact)]
        [InlineData("InBounce", 1f, Precision)]
        [InlineData("OutBounce", 1f, Precision)]
        public void Easing_At_One_Should_Return_Expected(string name, float expected, float precision)
        {
            var result = GetEasing(name)(1f);

            if (precision == Exact)
                result.Should().Be(expected);
            else
                result.Should().BeApproximately(expected, precision);
        }

        [Theory]
        [InlineData("InOutQuad", Precision)]
        [InlineData("InOutCubic", Precision)]
        [InlineData("InOutQuart", Precision)]
        [InlineData("InOutQuint", Precision)]
        [InlineData("InOutSine", Precision)]
        [InlineData("InOutExpo", LoosePrecision)]
        [InlineData("InOutCirc", Precision)]
        [InlineData("InOutElastic", LoosePrecision)]
        [InlineData("InOutBack", Precision)]
        [InlineData("InOutBounce", LoosePrecision)]
        public void InOutEasing_At_Half_Should_Return_Half(string name, float precision)
        {
            GetEasing(name)(0.5f)
                .Should()
                .BeApproximately(0.5f, precision);
        }

        private static Func<float, float> GetEasing(string name) => name switch
        {
            "InQuad" => ClassicUO.Utility.Easings.InQuad,
            "OutQuad" => ClassicUO.Utility.Easings.OutQuad,
            "InCubic" => ClassicUO.Utility.Easings.InCubic,
            "OutCubic" => ClassicUO.Utility.Easings.OutCubic,
            "InQuart" => ClassicUO.Utility.Easings.InQuart,
            "OutQuart" => ClassicUO.Utility.Easings.OutQuart,
            "InQuint" => ClassicUO.Utility.Easings.InQuint,
            "OutQuint" => ClassicUO.Utility.Easings.OutQuint,
            "InSine" => ClassicUO.Utility.Easings.InSine,
            "OutSine" => ClassicUO.Utility.Easings.OutSine,
            "InExpo" => ClassicUO.Utility.Easings.InExpo,
            "OutExpo" => ClassicUO.Utility.Easings.OutExpo,
            "InCirc" => ClassicUO.Utility.Easings.InCirc,
            "OutCirc" => ClassicUO.Utility.Easings.OutCirc,
            "InElastic" => ClassicUO.Utility.Easings.InElastic,
            "OutElastic" => ClassicUO.Utility.Easings.OutElastic,
            "InBack" => ClassicUO.Utility.Easings.InBack,
            "OutBack" => ClassicUO.Utility.Easings.OutBack,
            "InBounce" => ClassicUO.Utility.Easings.InBounce,
            "OutBounce" => ClassicUO.Utility.Easings.OutBounce,
            "InOutQuad" => ClassicUO.Utility.Easings.InOutQuad,
            "InOutCubic" => ClassicUO.Utility.Easings.InOutCubic,
            "InOutQuart" => ClassicUO.Utility.Easings.InOutQuart,
            "InOutQuint" => ClassicUO.Utility.Easings.InOutQuint,
            "InOutSine" => ClassicUO.Utility.Easings.InOutSine,
            "InOutExpo" => ClassicUO.Utility.Easings.InOutExpo,
            "InOutCirc" => ClassicUO.Utility.Easings.InOutCirc,
            "InOutElastic" => ClassicUO.Utility.Easings.InOutElastic,
            "InOutBack" => ClassicUO.Utility.Easings.InOutBack,
            "InOutBounce" => ClassicUO.Utility.Easings.InOutBounce,
            _ => throw new ArgumentException($"Unknown easing function: {name}")
        };
    }
}
